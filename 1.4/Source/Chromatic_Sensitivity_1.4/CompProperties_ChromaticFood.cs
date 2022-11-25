using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
  public class CompProperties_ChromaticFood : CompProperties
  {
    public Color forcedColor = new Color(-1f, -1f, -1f, 0f);
    public ChromaticColorType chromaticColorType = ChromaticColorType.Dominant;

    // Used as a multiplier to determine how much a color should affect the ingesting pawn.
    public int chromaticIntensity = 1;

    public CompProperties_ChromaticFood() => compClass = typeof(CompChromaticFood);

    public Color? GetForcedColor() => forcedColor.a <= 0 ? (Color?)null : forcedColor;
  }
}
