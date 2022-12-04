using HarmonyLib;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  [HarmonyPatch(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics))]
  internal class HarmonyPatch_PawnGraphicSet
  {
    /**
     * Annoyingly the graphic data used to build graphics for non-humanlikes is on the PawnKindLifeStage so can't be changed per pawn.
     * It uses a seeded Rand to pick the same thing each time instead of having it saved.
     * So instead we recolor it after the fact, we assume that this only needs to happen on non-humanoid pawns
     * The color changing in the hediff could be moved here but I prefer to keep the Harmony patch as small as possible.
     */
    static void Postfix(ref Pawn ___pawn, ref Graphic ___nakedGraphic, ref Graphic ___furCoveredGraphic)
    {
      if (___pawn.RaceProps.Humanlike // Humanlikes are already taken care of in the Hediff
          || ___pawn.health.hediffSet.GetFirstHediffOfDef(ChromaticDefOf.Taggerung_ChromaticSensitivity) is not Hediff_ChromaticSensitivity hediff
          || (hediff.SkinColor == null && hediff.HairColor == null)) return;

      if (hediff.SkinColor.HasValue && ___nakedGraphic.Color.IndistinguishableFrom(hediff.SkinColor.Value))
      {
        ___nakedGraphic = ___nakedGraphic.GetColoredVersion(___nakedGraphic.Shader, hediff.SkinColor.Value, ___nakedGraphic.ColorTwo);
        Log.Verbose($"Set nakedgraphic color to {hediff.SkinColor.Value} for {___pawn.ThingID}");
      }

      if (!hediff.HairColor.HasValue ||
          !___furCoveredGraphic.Color.IndistinguishableFrom(hediff.HairColor.Value)) return;
      ___furCoveredGraphic = ___furCoveredGraphic.GetColoredVersion(___furCoveredGraphic.Shader, hediff.HairColor.Value, ___furCoveredGraphic.ColorTwo);
      Log.Verbose($"Set furcovered color to {hediff.HairColor.Value} for {___pawn.ThingID}");
    }
  }
}
