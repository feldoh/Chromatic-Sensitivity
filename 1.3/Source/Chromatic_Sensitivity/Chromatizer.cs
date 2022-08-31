using System.Linq;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	[StaticConstructorOnStartup]
	public static class Chromatizer
	{
		static Chromatizer()
		{
			var colorExtractor = new ColorExtractor();
			foreach (var ingestible in DefDatabase<ThingDef>.AllDefs.Where(def =>
				         def.IsIngestible && typeof(ThingWithComps).IsAssignableFrom(def.thingClass)))
			{
				if (ingestible.HasComp(typeof(CompChromaticFood))) continue;
				var compPropertiesChromaticFood = new CompProperties_ChromaticFood();
				var maybeDominantColor = colorExtractor.ExtractDominantColor(ingestible);
				// Pre-calculate dominant color for things without an overriden color with a valid texture to avoid runtime texture parsing. 
				if (maybeDominantColor is Color dominantColor) compPropertiesChromaticFood.forcedColor = dominantColor;
				ingestible.comps.Add(compPropertiesChromaticFood);
			}
			ChromaticSensitivity.Settings.ExposeData();
		}
	}
}