using RimWorld;
using Verse;

namespace Chromatic_Sensitivity
{
  [DefOf]
  public static class ChromaticDefOf
  {
    public static ThingDef Taggerung_Chromosia;
    public static ThingDef Plant_Taggerung_Chromosia;
    
    static ChromaticDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (ChromaticDefOf));
  }
}
