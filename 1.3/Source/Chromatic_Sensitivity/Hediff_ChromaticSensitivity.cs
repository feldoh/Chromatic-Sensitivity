using System.Linq;
using AlienRace;
using RimWorld;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	public class Hediff_ChromaticSensitivity : HediffWithComps
	{
		private readonly ColorExtractor _colorExtractor;

		#region Properties

		public HediffDef_ChromaticSensitivity Def => def as HediffDef_ChromaticSensitivity;

		#endregion Properties

		public Hediff_ChromaticSensitivity() : this(new ColorExtractor()) { }

		public Hediff_ChromaticSensitivity(ColorExtractor colorExtractor)
		{
			_colorExtractor = colorExtractor;
		}

		#region Helpers

		private Color MoveColorsCloser(Color currentColor, Color targetColor)
		{
			return Color.Lerp(currentColor, targetColor, Severity);
		}

		private Color? MoveTowardsColorFromFood(Thing food, Color startingColor)
		{
			var comp = food.TryGetComp<CompIngredients>();
			if (comp == null || (comp.ingredients?.Count ?? 0) <= 0)
			{
				return food.Stuff?.stuffProps?.color is Color stuffColor
					? MoveColorsCloser(startingColor, stuffColor)
					: MoveColorsCloser(startingColor,
						_colorExtractor.ExtractDominantColor(food) ?? startingColor);
			}

			var newCol = comp.ingredients.Aggregate(startingColor, MoveColourTowardsIngredientColour);
			Log.Verbose($"New col {newCol.r} {newCol.g} {newCol.b}");
			return newCol;
		}

		private Color MoveColourTowardsIngredientColour(Color color, ThingDef ingredient)
		{
			return ingredient.stuffProps?.color is Color stuffColor
				? MoveColorsCloser(color, stuffColor)
				: _colorExtractor.ExtractDominantColor(ingredient) is Color newColor
					? MoveColorsCloser(color, newColor)
					: color;
		}
		
		#endregion Helpers

		private Color? GetSkinColorFromHumanoidAlienRacePawn()
		{
			var channels = pawn.TryGetComp<AlienPartGenerator.AlienComp>()?.ColorChannels;
			if (channels == null) return null;
			if (!channels.TryGetValue("skin", out var skinColorChannels)) channels.TryGetValue("base", out skinColorChannels);
			Log.Verbose($"Found probable HAR skin color channel {skinColorChannels}");
			return skinColorChannels?.first;
		}
		
		private Color? GetSkinColor()
		{
			return GetSkinColorFromHumanoidAlienRacePawn() ?? pawn.story?.SkinColor;
		}

		private bool SetSkinColorFromHumanoidAlienRacePawn(Color color)
		{
			var channels = pawn.TryGetComp<AlienPartGenerator.AlienComp>()?.ColorChannels;
			if (channels == null) return false;
			var channelName = channels.ContainsKey("skin") ? "skin" : "base";
			if (!channels.ContainsKey(channelName)) return false;
			channels[channelName].first = color;
			Log.Verbose($"Updating probable HAR skin color channel {channelName} to {color}");
			return true;
		}
		
		private void SetSkinColor(Color color)
		{
			if (SetSkinColorFromHumanoidAlienRacePawn(color) || pawn.story == null) return;
			pawn.story.skinColorOverride = color;
		}
		
		public void FoodIngested(Thing food, Color? forcedColor)
		{
			var startingColor = GetSkinColor();
			if (startingColor == null)
			{
				Log.Verbose($"Unable to determine skin color for pawn of def {pawn.def.defName}");
				return;
			}
			
			var newColor = forcedColor.HasValue
				? MoveColorsCloser(startingColor.Value, forcedColor.Value)
				: MoveTowardsColorFromFood(food, startingColor.Value) ?? startingColor;
			
			SetSkinColor(newColor.Value);
			
			Log.Verbose($"Colour changed from ({startingColor}) to ({newColor.Value})");
			pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(pawn);
			if (!pawn.NonHumanlikeOrWildMan() && pawn.Awake() && newColor.Value.b > 0.9 && startingColor.Value.b < newColor.Value.b)
				MoteMaker.ThrowText(pawn.DrawPos, pawn.Map,
					"TextMote_ChromaticSensitivity_FeelingBlue".Translate(), 6.5f);

		}
	}
}