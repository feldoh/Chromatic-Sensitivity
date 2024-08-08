using HarmonyLib;
using RimWorld;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
    [HarmonyPatch(typeof(Toils_LayDown), "ApplyBedThoughts")]
    internal class HarmonyPatch_BedThoughts
    {
        [HarmonyPostfix]
        public static void ApplyBedThoughts(Pawn actor, Building_Bed bed)
        {
            if (bed is null || actor.health.hediffSet.GetFirstHediffOfDef(ChromaticDefOf.Taggerung_ChromaticSensitivity) is not Hediff_ChromaticSensitivity hediff) return;
            actor.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(ChromaticDefOf.Taggerung_FavoriteChromaticBedroom);
            hediff.MaybeGainThoughtIfColourAroundPointIsFavorite(bed.Position, bed.Map, ChromaticDefOf.Taggerung_FavoriteChromaticBedroom);
            actor.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(ChromaticDefOf.Taggerung_FavoriteChromaticBed);
            hediff.MaybeGainThoughtIfColourOfThingIsFavorite(bed, ChromaticDefOf.Taggerung_FavoriteChromaticBed);
        }
    }
}
