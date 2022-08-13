using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Debug = System.Diagnostics.Debug;

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

		public void FoodIngested(Thing food, Color? forcedColor)
		{
			if (pawn.story?.SkinColor == null) return; // We can only recolor things with skin for now
			var startingColor = pawn.story?.SkinColor ?? pawn.Graphic.color;
			var newColor = forcedColor.HasValue
				? MoveColorsCloser(startingColor, forcedColor.Value)
				: MoveTowardsColorFromFood(food, startingColor) ?? startingColor;
			Debug.Assert(pawn.story != null, "pawn.story != null");
			pawn.story.skinColorOverride = newColor;
			Log.Verbose($"Colour changed from ({startingColor}) to ({pawn.story?.skinColorOverride ?? pawn.Graphic.color})");
			pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(pawn);
			if (!pawn.NonHumanlikeOrWildMan() && pawn.Awake() && newColor.b > 0.9 && startingColor.b < newColor.b)
				MoteMaker.ThrowText(pawn.DrawPos, pawn.Map,
					"TextMote_ChromaticSensitivity_FeelingBlue".Translate(), 6.5f);

		}
	}
}