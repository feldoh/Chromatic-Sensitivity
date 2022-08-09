using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	public class ChromaticSensitivitySettings : ModSettings
	{
		public bool AllowInfection = true;
		public bool VerboseLogging = false;

		/**
		 * List of colors to exclude from possible consideration
		 * Used to avoid the box counting as the dominant color for things like Rice, Corn etc.
		 */
		public HashSet<int> ExcludedColors = new HashSet<int>(DefaultExcludedColors);
		
		private static readonly HashSet<int> DefaultExcludedColors = new HashSet<int>()
		{
			140 | 101 << 8 | 49 << 16, // Raw Food Boxes
			0 // Outline; pure black
		};

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.CheckboxLabeled("Allow Chromatic Sensitivity Infection", ref AllowInfection);
			ExcludedColors =
				RGBStringToColorSet(options.TextEntryLabeled(
					"Semicolon separated list of RGB values to ignore e.g. '(255,255,255);(0,0,0)' for white and black",
					ColorSetToRGBString(ExcludedColors), 2));
			options.Gap();
			options.CheckboxLabeled("Enable Verbose logging", ref VerboseLogging);
			options.Gap();

			options.End();
		}

		private string ColorSetToRGBString(HashSet<int> colors)
		{
			var sb = new StringBuilder();
			return colors.Aggregate(sb, (builder, c) =>
			{
				builder.AppendWithSeparator($"({255 & c},{((255 << 8) & c) >> 8},{((255 << 16) & c) >> 16})", ";");
				return builder;
			}).ToString();
		}
		
		private HashSet<int> RGBStringToColorSet(string rgbStrings)
		{
			return rgbStrings.Split(';').Select(s =>
			{
				var rgb = s.Trim().Trim('(', ')').Split(new[] { ',' }, 3);
				return int.Parse(rgb[0]) | int.Parse(rgb[1]) << 8 | int.Parse(rgb[2]) << 16;
			}).ToHashSet();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref VerboseLogging, "VerboseLogging", false);
			Scribe_Values.Look(ref AllowInfection, "AllowInfection", false);
			Scribe_Collections.Look(ref ExcludedColors, false, "ExcludedColors", LookMode.Value);
			if ((ExcludedColors?.Count ?? 0) == 0)
			{
				ExcludedColors = DefaultExcludedColors;
			}
		}
		
	}
}