using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  public class ColorHelper
  {
    public static Color RandomColor => new(Rand.Value, Rand.Value, Rand.Value);
    
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
      return ChromaticSensitivity.Settings.ThingDefColors.TryGetValue(defName, out Color colorForDef)
        ? colorForDef
        : null;
    }

    public Color? ExtractDominantColor(Thing thing)
    {
      return GetDefColorOverride(thing.def.defName) ?? thing.TryGetComp<CompChromaticFood>()?.Props?.GetForcedColor() ??
        ExtractDominantColor((Texture2D)thing.Graphic.MatSingle.mainTexture);
    }

    public Color? ExtractDominantColor(Texture2D texture)
    {
      return texture == null || texture == BaseContent.BadTex
        ? null
        : ExtractBestColor(texture.isReadable
          ? texture
          : TextureAtlasHelper.MakeReadableTextureInstance(texture));
    }

    public static int CompactColor(Color32 color)
    {
      return color.r | color.g << 8 | color.b << 16;
    }

    public static Color UnpackColor(int compactedColor) => UnpackColor32(compactedColor);
    
    public static Color32 UnpackColor32(int compactedColor)
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
      var log = new StringBuilder();
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
        if (ChromaticSensitivity.Settings.VerboseLogging)
        {
          log.AppendLine($"New most dominant color ({bestColor}): {commonality} pixels");
        }
      }

      Log.Verbose(log.AppendLine($"Best colour determined as ({bestColor}): {commonality} pixels").ToString());
      return bestColor is { } chosen
        ? new Color32(chosen.r, chosen.g, chosen.b, byte.MaxValue)
        : null;
    }
  }
}
