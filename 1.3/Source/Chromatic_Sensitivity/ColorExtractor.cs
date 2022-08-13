using System.Linq;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	public class ColorExtractor
	{
		public Color? ExtractDominantColor(ThingDef thingDef)
		{
			return GetDefColorOverride(thingDef.defName) ??
			       (thingDef.comps.Find(c => c.compClass == typeof(CompChromaticFood)) is CompProperties_ChromaticFood comp &&
			        comp.GetForcedColor().HasValue
				       ? comp.forcedColor
				       : ExtractDominantColor((Texture2D)thingDef.graphic.MatSingle.mainTexture));
		}

		private Color? GetDefColorOverride(string defName)
		{
			return ChromaticSensitivity.Settings.ThingDefColors.TryGetValue(defName, out var colorForDef)
				? colorForDef
				: (Color?)null;
		}

		public Color? ExtractDominantColor(Thing thing)
		{
			return GetDefColorOverride(thing.def.defName) ?? thing.TryGetComp<CompChromaticFood>()?.Props?.GetForcedColor() ??
				ExtractDominantColor((Texture2D)thing.Graphic.MatSingle.mainTexture);
		}

		public Color? ExtractDominantColor(Texture2D texture)
		{
			return texture == BaseContent.BadTex
				? null
				: ExtractBestColor(texture.isReadable
					? texture
					: TextureAtlasHelper.MakeReadableTextureInstance(texture));
		}

		public static int CompactColor(Color32 color)
		{
			return color.r | color.g << 8 | color.b << 16;
		}

		public static Color32 UnpackColor(int compactedColor)
		{
			return new Color32(
				(byte)(255 & compactedColor),
				(byte)(((255 << 8) & compactedColor) >> 8),
				(byte)(((255 << 16) & compactedColor) >> 16), byte.MaxValue);
		}

		private static Color? ExtractBestColor(Texture2D texture2D)
		{
			Color32? bestColor = null;
			var bestKey = 0;
			var commonality = 0;
			foreach (var countedColor in texture2D.GetPixels32()
				         .Where(p => p.a > 5) // Ignore anything that's basically transparent 
				         .GroupBy(CompactColor))
			{
				var newCommonality = countedColor.Count();
				if (newCommonality <= commonality &&
				    !ChromaticSensitivity.Settings.ExcludedColors.ContainsKey(bestKey)) continue;
				bestColor = countedColor.First();
				bestKey = countedColor.Key;
				commonality = newCommonality;
				Log.Verbose($"New most dominant colour ({bestColor}): {commonality} pixels");
			}

			return bestColor is Color32 chosen
				? new Color32(chosen.r, chosen.g, chosen.b, byte.MaxValue)
				: (Color?)null;
		}
	}
}