using System.Linq;
using Verse;

namespace Chromatic_Sensitivity
{
	[StaticConstructorOnStartup]
	public static class Chromatizer
	{
		static Chromatizer()
		{
			foreach (var ingestible in DefDatabase<ThingDef>.AllDefs.Where(def =>
				         def.IsIngestible && typeof(ThingWithComps).IsAssignableFrom(def.thingClass)))
			{
				if (ingestible.HasComp(typeof(CompChromaticFood))) continue;
				ingestible.comps.Add(new CompProperties_ChromaticFood());
			}
		}
	}
}