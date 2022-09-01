using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
  public class CompProperties_ChromaticFood : CompProperties
  {
    public Color forcedColor = new Color(-1f, -1f, -1f, 0f);
    public CompProperties_ChromaticFood() => compClass = typeof (CompChromaticFood);
    
    public Color? GetForcedColor()
    {
      return forcedColor.a <= 0 ? (Color?) null : forcedColor;
    }
  }
}
