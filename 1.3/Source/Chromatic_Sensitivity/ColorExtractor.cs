using System.Linq;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	public class ColorExtractor
	{

		public Color? ExtractDominantColor(ThingDef thingDef)
		{
			return ExtractDominantColor((Texture2D)thingDef.graphic.MatSingle.mainTexture);
		}
		
		public Color? ExtractDominantColor(Thing thing)
		{
			return ExtractDominantColor((Texture2D)thing.Graphic.MatSingle.mainTexture);
		}
		
		public Color? ExtractDominantColor(Texture2D texture)
		{
			return ExtractBestColor(texture.isReadable
				? texture
				: TextureAtlasHelper.MakeReadableTextureInstance(texture));
		}
		
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
				if (newCommonality <= commonality && !ChromaticSensitivity.Settings.ExcludedColors.Contains(bestKey)) continue;
				bestColor = countedColor.First();
				bestKey = countedColor.Key;
				commonality = newCommonality;
				if (ChromaticSensitivity.Settings.VerboseLogging)
				{
					Log.Verbose($"New most dominant colour ({bestColor}): {commonality} pixels");
				}
			}

			return bestColor is Color32 chosen
				? new Color32(chosen.r, chosen.g, chosen.b, byte.MaxValue)
				: (Color?) null;
		}
	}
}