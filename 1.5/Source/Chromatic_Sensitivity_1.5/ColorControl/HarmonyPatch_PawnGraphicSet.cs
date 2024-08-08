using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  [HarmonyPatch]
  internal class HarmonyPatch_PawnGraphicSet
  {
    /**
     * Annoyingly the graphic data used to build graphics for non-humanlikes is on the PawnKindLifeStage so can't be changed per pawn.
     * It uses a seeded Rand to pick the same thing each time instead of having it saved.
     * So instead we recolor it after the fact, we assume that this only needs to happen on non-humanoid pawns
     * The color changing in the hediff could be moved here but I prefer to keep the Harmony patch as small as possible.
     */

    [HarmonyTargetMethods]
    static IEnumerable<MethodInfo> TargetMethods()
    {
      yield return AccessTools.Method(typeof(PawnRenderNode_AnimalPart), nameof(PawnRenderNode_AnimalPart.GraphicFor));
      yield return AccessTools.Method(typeof(PawnRenderNode_Body), nameof(PawnRenderNode_Body.GraphicFor));
    }

    [HarmonyPostfix]
    static void Postfix(ref Graphic __result, Pawn pawn, PawnRenderNode __instance)
    {
      if (__result == null
          || pawn.RaceProps.Humanlike // Humanlikes are already taken care of in the Hediff
          || pawn.health.hediffSet.GetFirstHediffOfDef(ChromaticDefOf.Taggerung_ChromaticSensitivity) is not Hediff_ChromaticSensitivity hediff
          || (hediff.SkinColor == null && hediff.HairColor == null)) return;

      if (hediff.SkinColor.HasValue && __instance.Props.colorType != PawnRenderNodeProperties.AttachmentColorType.Hair && !__result.Color.IndistinguishableFrom(hediff.SkinColor.Value))
      {
        __result = __result.GetColoredVersion(__result.Shader, hediff.SkinColor.Value, __result.ColorTwo);
        Log.Verbose($"Set skin color to {hediff.SkinColor.Value} for {pawn.ThingID}");
      }

      if (!hediff.HairColor.HasValue
          || __instance.Props.colorType != PawnRenderNodeProperties.AttachmentColorType.Hair
          || __result.Color.IndistinguishableFrom(hediff.HairColor.Value)) return;
      __result = __result.GetColoredVersion(__result.Shader, hediff.HairColor.Value, __result.ColorTwo);
      Log.Verbose($"Set furcovered color to {hediff.HairColor.Value} for {pawn.ThingID}");
    }
  }
}
