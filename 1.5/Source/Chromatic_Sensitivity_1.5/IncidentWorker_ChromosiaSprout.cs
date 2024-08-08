using System.Linq;
using RimWorld;
using Verse;

namespace Chromatic_Sensitivity
{
  public class IncidentWorker_ChromosiaSprout : IncidentWorker
  {
    private static readonly IntRange CountRange = new(3, 7);
    private const int MinRoomCells = 64;
    private const int SpawnRadius = 6;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
      if (!base.CanFireNowSub(parms))
        return false;
      Map target = (Map)parms.target;
      return target.weatherManager.growthSeasonMemory.GrowthSeasonOutdoorsNow && TryFindRootCell(target, out _);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
      Map map = (Map)parms.target;
      if (!TryFindRootCell(map, out var cell))
        return false;
      Thing thing1 = null;
      var randomInRange = CountRange.RandomInRange;
      for (var index = 0;
           index < randomInRange &&
           CellFinder.TryRandomClosewalkCellNear(cell, map, SpawnRadius, out var result, x => CanSpawnAt(x, map));
           ++index)
      {
        result.GetPlant(map)?.Destroy();
        var thing2 = GenSpawn.Spawn(ChromaticDefOf.Plant_Taggerung_Chromosia, result, map);
        if (thing1 == null)
          thing1 = thing2;
      }

      if (thing1 == null)
        return false;
      SendStandardLetter(parms, thing1);
      return true;
    }

    private bool TryFindRootCell(Map map, out IntVec3 cell) => CellFinderLoose.TryFindRandomNotEdgeCellWith(10,
      x => CanSpawnAt(x, map) && x.GetRoom(map).CellCount >= MinRoomCells, map, out cell);

    private bool CanSpawnAt(IntVec3 c, Map map)
    {
      if (!c.Standable(map) || c.Fogged(map) ||
          map.fertilityGrid.FertilityAt(c) < (double)ChromaticDefOf.Plant_Taggerung_Chromosia.plant.fertilityMin ||
          !c.GetRoom(map).PsychologicallyOutdoors || c.GetEdifice(map) != null || !PlantUtility.GrowthSeasonNow(c, map))
        return false;
      Plant plant = c.GetPlant(map);
      if (plant != null && plant.def.plant.growDays > 10.0)
        return false;
      var thingList = c.GetThingList(map);
      return thingList.All(t =>
        t.def != ChromaticDefOf.Plant_Taggerung_Chromosia && t.def != ThingDefOf.Plant_Ambrosia);
    }
  }
}
