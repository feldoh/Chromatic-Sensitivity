using RimWorld;
using Verse;

namespace Chromatic_Sensitivity
{
  [DefOf]
  public static class ChromaticDefOf
  {
    public static ThingDef Taggerung_Chromosia;
    public static ThingDef Plant_Taggerung_Chromosia;
    public static HediffDef Taggerung_ChromaticSensitivity;
    public static HediffDef Taggerung_ChromaticSurroundings_Red;
    public static HediffDef Taggerung_ChromaticSurroundings_Green;
    public static HediffDef Taggerung_ChromaticSurroundings_Blue;
    
    static ChromaticDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (ChromaticDefOf));
  }
}
