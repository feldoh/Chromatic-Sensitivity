using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Chromatic_Sensitivity;

public class IncidentWorker_ChromosiaSprout : IncidentWorker
{
    private static readonly IntRange CountRange = new(3, 7);
    private const int MinRoomCells = 64;
    private const int SpawnRadius = 6;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms))
        {
            return false;
        }

        Map target = (Map)parms.target;
        return PlantUtility.GrowthSeasonNow(target, ChromaticDefOf.Plant_Taggerung_Chromosia) && TryFindRootCell(target, out _);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        if (!TryFindRootCell(map, out IntVec3 cell))
        {
            return false;
        }

        Thing thing1 = null;
        int randomInRange = CountRange.RandomInRange;
        for (int index = 0; index < randomInRange && CellFinder.TryRandomClosewalkCellNear(cell, map, SpawnRadius, out IntVec3 result, x => CanSpawnAt(x, map)); ++index)
        {
            result.GetPlant(map)?.Destroy();
            Thing thing2 = GenSpawn.Spawn(ChromaticDefOf.Plant_Taggerung_Chromosia, result, map);
            if (thing1 == null)
            {
                thing1 = thing2;
            }
        }

        if (thing1 == null)
        {
            return false;
        }

        SendStandardLetter(parms, thing1);
        return true;
    }

    private bool TryFindRootCell(Map map, out IntVec3 cell)
    {
        return CellFinderLoose.TryFindRandomNotEdgeCellWith(10, x => CanSpawnAt(x, map) && x.GetRoom(map).CellCount >= MinRoomCells, map, out cell);
    }

    private bool CanSpawnAt(IntVec3 c, Map map)
    {
        if (
            !c.Standable(map)
            || c.Fogged(map)
            || map.fertilityGrid.FertilityAt(c) < (double)ChromaticDefOf.Plant_Taggerung_Chromosia.plant.fertilityMin
            || !c.GetRoom(map).PsychologicallyOutdoors
            || c.GetEdifice(map) != null
            || !PlantUtility.GrowthSeasonNow(map, ChromaticDefOf.Plant_Taggerung_Chromosia)
        )
        {
            return false;
        }

        Plant plant = c.GetPlant(map);
        if (plant != null && plant.def.plant.growDays > 10.0)
        {
            return false;
        }

        List<Thing> thingList = c.GetThingList(map);
        return thingList.All(t => t.def != ChromaticDefOf.Plant_Taggerung_Chromosia && t.def != ThingDefOf.Plant_Ambrosia);
    }
}
