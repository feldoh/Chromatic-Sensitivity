using System.Linq;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl;

[StaticConstructorOnStartup]
public static class Chromatizer
{
    static Chromatizer()
    {
        ChromaticSensitivity.Settings.ExposeData();
        ColorHelper colorExtractor = new();
        foreach (ThingDef ingestible in DefDatabase<ThingDef>.AllDefs.Where(def => def.IsIngestible && typeof(ThingWithComps).IsAssignableFrom(def.thingClass)))
        {
            if (ingestible.HasComp(typeof(CompChromaticFood)))
            {
                continue;
            }

            CompProperties_ChromaticFood compPropertiesChromaticFood = new();
            Color? maybeDominantColor = colorExtractor.ExtractDominantColor(ingestible);
            // Pre-calculate dominant color for things without an overriden color with a valid texture to avoid runtime texture parsing.
            if (maybeDominantColor is Color dominantColor)
            {
                compPropertiesChromaticFood.forcedColor = dominantColor;
                Log.Verbose($"Chromatized: {ingestible.defName}, set color to {dominantColor}");
            }

            ingestible.comps.Add(compPropertiesChromaticFood);
        }
    }
}
