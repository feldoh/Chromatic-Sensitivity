using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	public class Hediff_ChromaticSensitivity : HediffWithComps
	{
		#region Properties

		public HediffDef_ChromaticSensitivity Def => def as HediffDef_ChromaticSensitivity;

		#endregion Properties

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
						TextureAtlasHelper.MakeReadableTextureInstance((Texture2D)food.Graphic.MatSingle.mainTexture)
							.GetDominantColor());
			}

			var newCol = comp.ingredients.Aggregate(startingColor, MoveColourTowardsIngredientColour);
			Log.Message($"New col {newCol.r} {newCol.g} {newCol.b}");
			return newCol;
		}

		/**
		 * List of colors to exclude from possible consideration
		 * Used to avoid the box counting as the dominant color for things like Rice, Corn etc.
		 */
		private static readonly HashSet<int> ExcludedColors = new HashSet<int>()
		{
			140 | 101 << 8 | 49 << 16, // Raw Food Boxes
			0 // Outline; pure black
		};

		private static Color? ExtractBestColor(Texture2D texture2D)
		{
			Color32? bestColor = null;
			var bestKey = 0;
			var commonality = 0;
			foreach (var countedColor in texture2D.GetPixels32()
				         .Where(p => p.a > 5) // Ignore anything that's basically transparent 
				         .GroupBy(p => p.r | p.g << 8 | p.b << 16))
			{
				var newCommonality = countedColor.Count();
				if (newCommonality <= commonality && !ExcludedColors.Contains(bestKey)) continue;
				bestColor = countedColor.First();
				bestKey = countedColor.Key;
				commonality = newCommonality;
				Log.Message($"Found {commonality} pixels of color: {bestColor}");
			}

			return bestColor is Color32 chosen
				? new Color32(chosen.r, chosen.g, chosen.b, byte.MaxValue)
				: (Color?) null;
		}

		private Color MoveColourTowardsIngredientColour(Color color, ThingDef ingredient)
		{
			return ingredient.stuffProps?.color is Color stuffColor
				? MoveColorsCloser(color, stuffColor)
				: ExtractBestColor(TextureAtlasHelper.MakeReadableTextureInstance((Texture2D)ingredient.graphic.MatSingle.mainTexture)) is Color newColor
					? MoveColorsCloser(color, newColor)
					: color;
		}
		
		#endregion Helpers

		public void FoodIngested(Thing food, Color? forcedColor)
		{
			var startingColor = pawn.story.SkinColor;
			pawn.story.skinColorOverride = forcedColor.HasValue
				? MoveColorsCloser(startingColor, forcedColor.Value)
				: MoveTowardsColorFromFood(food, startingColor) ?? startingColor;

			Log.Message($"Colour changed from ({startingColor.r}, {startingColor.b}, {startingColor.g}) to ({pawn.story.skinColorOverride.Value.r}, {pawn.story.skinColorOverride.Value.b}, {pawn.story.skinColorOverride.Value.g}");
			pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(pawn);

			if (pawn.Awake() && pawn.story.SkinColor.b > 0.9 && startingColor.b < pawn.story.SkinColor.b)
				MoteMaker.ThrowText(pawn.DrawPos, pawn.Map,
					"TextMote_ChromaticSensitivity_FeelingBlue".Translate(), 6.5f);
		}
	}
}