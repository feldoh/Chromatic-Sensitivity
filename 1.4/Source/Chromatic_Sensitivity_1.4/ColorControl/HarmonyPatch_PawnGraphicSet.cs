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
    static void Postfix(ref Pawn ___pawn, ref Graphic ___nakedGraphic)
    {
      if (___pawn.RaceProps.Humanlike // Humanlikes are already taken care of in the Hediff
          || !(___pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Taggerung_ChromaticSensitivity")) is Hediff_ChromaticSensitivity hediff) 
          || hediff.SkinColor == null
          || ___nakedGraphic.Color.IndistinguishableFrom(hediff.SkinColor.Value)) return;
      ___nakedGraphic = ___nakedGraphic.GetColoredVersion(___nakedGraphic.Shader, hediff.SkinColor.Value, ___nakedGraphic.ColorTwo);
      Log.Verbose($"Set nakedgraphic color to {hediff.SkinColor.Value} for {___pawn.ThingID}");
    }
  }
}