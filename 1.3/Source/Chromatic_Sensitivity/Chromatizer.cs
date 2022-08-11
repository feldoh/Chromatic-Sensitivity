using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Chromatic_Sensitivity
{
	[StaticConstructorOnStartup]
	public static class Chromatizer
	{
		static Chromatizer()
		{
#if DEBUG
			Harmony.DEBUG = true;
#endif

			var harmony = new Harmony("Taggerung.rimworld.Chromatic_Sensitivity.main");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			foreach (var ingestible in DefDatabase<ThingDef>.AllDefs.Where(def =>
				         def.IsIngestible && typeof(ThingWithComps).IsAssignableFrom(def.thingClass)))
			{
				if (ingestible.HasComp(typeof(CompChromaticFood))) continue;
				ingestible.comps.Add(new CompProperties_ChromaticFood());
			}
		}
	}
}