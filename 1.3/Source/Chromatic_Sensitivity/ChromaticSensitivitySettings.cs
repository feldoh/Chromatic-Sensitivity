using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	[StaticConstructorOnStartup]
	public class ChromaticSensitivitySettings : ModSettings
	{
		public bool AllowInfection;
		public bool VerboseLogging;
		public float Severity;

		private byte red = 125;
		private byte green = 125;
		private byte blue = 125;
		private string colorName = "helpful name for color";
		private string exportPath = "C:\\RimworldExport"; 

		/**
		 * List of colors to exclude from possible consideration
		 * Used to avoid the box counting as the dominant color for things like Rice, Corn etc.
		 */
		public Dictionary<int, string> ExcludedColors = new Dictionary<int, string>(DefaultExcludedColors);

		private static readonly Dictionary<int, string> DefaultExcludedColors = new Dictionary<int, string>()
		{
			{ 140 | 101 << 8 | 49 << 16, "Raw Food Boxes" },
			{ 165 | 125 << 8 | 57 << 16, "Raw Food Boxes 2" },
			{ 123 | 85 << 8 | 33 << 16, "Raw Food Boxes 3" },
			{ 0, "Outline; pure black" }
		};

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.Label("Add colors to exclude from consideration for chromatic sensitivity");
			options.Label("<color=#FF0000>Red</color>");
			var rectR = options.GetRect(22f);
			red = (byte)Widgets.HorizontalSlider(rectR, red, 0f, 255f, false, red.ToString(CultureInfo.InvariantCulture), "0",
				"255", 1f);

			options.Label("<color=#00FF00>Green</color>");
			var rectG = options.GetRect(22f);
			green = (byte)Widgets.HorizontalSlider(rectG, green, 0f, 255f, false,
				green.ToString(CultureInfo.InvariantCulture), "0", "255", 1f);

			options.Label("<color=#0000FF>Blue</color>");
			var rectB = options.GetRect(22f);
			blue = (byte)Widgets.HorizontalSlider(rectB, blue, 0f, 255f, false, blue.ToString(CultureInfo.InvariantCulture),
				"0", "255", 1f);

			options.Gap();
			colorName = options.TextEntryLabeled(
				$"<color=#{(int)red:X2}{(int)green:X2}{(int)blue:X2}>Color from sliders</color>", colorName);
			options.Gap();
			if (options.ButtonTextLabeled("Add color to excluded list", "add"))
			{
				ExcludedColors.Add(ColorExtractor.CompactColor(new Color32(red, green, blue, byte.MaxValue)), colorName);
			}

			options.Gap();
			options.Label("Currently Excluded Colors");
			foreach (var excludedColor in ExcludedColors.ToList())
			{
				var unpacked = ColorExtractor.UnpackColor(excludedColor.Key);
				if (options.ButtonTextLabeled(
					    $"Excluded {excludedColor.Value} <color=#{(int)unpacked.r:X2}{(int)unpacked.g:X2}{(int)unpacked.b:X2}>({unpacked.r},{unpacked.g},{unpacked.b})</color>",
					    "remove"))
				{
					ExcludedColors.Remove(
						ColorExtractor.CompactColor(new Color32(unpacked.r, unpacked.g, unpacked.b, byte.MaxValue)));
				}
			}

			options.Gap();
			options.CheckboxLabeled("Enable Verbose logging", ref VerboseLogging);
			options.CheckboxLabeled("Allow infection", ref AllowInfection);
			options.Gap();

			var severityRect = options.GetRect(22f);
			Severity = Widgets.HorizontalSlider(severityRect, Severity * 100, 0f, 100.0f, false, $"Severity Percent: {Severity * 100}", "0", "100",
				0.5f) / 100f;

			var hediffDef = DefDatabase<HediffDef>.GetNamed("Taggerung_ChromaticSensitivity");
			hediffDef.scenarioCanAdd = AllowInfection;
			if (Math.Abs(Severity - hediffDef.initialSeverity) > 0.005)
			{
				foreach (var p in PawnsFinder.AllMapsAndWorld_Alive)
					if (p.health.hediffSet.GetFirstHediffOfDef(
						    HediffDef.Named("Taggerung_ChromaticSensitivity")) is Hediff hediff)
						hediff.Severity = Severity;
				hediffDef.initialSeverity = Severity;
			}
			
			options.Gap();
			exportPath = options.TextEntryLabeled("Export path", exportPath);
			if (options.ButtonText("Dump All"))
			{
				DumpAllTexturesWithSelectedColors(exportPath);
			}

			options.End();
		}

		private static void DumpAllTexturesWithSelectedColors(string path)
		{
			Directory.CreateDirectory(path);
			var c = new ColorExtractor();
			foreach (var ingestible in DefDatabase<ThingDef>.AllDefsListForReading.Where(def =>
				         def.IsIngestible && typeof(ThingWithComps).IsAssignableFrom(def.thingClass)))
			{
				var dominantColor = c.ExtractDominantColor(ingestible);
				if (!(dominantColor is Color color)) continue;
				var texture2D = (Texture2D)ingestible.graphic.MatSingle.mainTexture;
				if (texture2D == BaseContent.BadTex) continue;
				var texturePngPath = $"{path}\\{ingestible.defName}.png";
				if (File.Exists(texturePngPath)) File.Delete(texturePngPath);
				TextureAtlasHelper.WriteDebugPNG(texture2D, texturePngPath);

				var temporary = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0,
					RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
				temporary.name = "MakeReadableTexture_Temp";
				Graphics.Blit(texture2D, temporary);
				var active = RenderTexture.active;
				RenderTexture.active = temporary;
				var targetTexture = new Texture2D(64, 64);
				targetTexture.ReadPixels(new Rect(0.0f, 0.0f, 64, 64), 0, 0);
				for (var y = 0; y < 64; y++)
				{
					for (var x = 0; x < 64; x++)
					{
						targetTexture.SetPixel(x, y, color);
					}
				}

				targetTexture.Apply();
				RenderTexture.active = active;
				RenderTexture.ReleaseTemporary(temporary);
				var textureColorPath = $"{path}\\{ingestible.defName}_Color.png";
				if (File.Exists(textureColorPath)) File.Delete(textureColorPath);
				TextureAtlasHelper.WriteDebugPNG(targetTexture, textureColorPath);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref VerboseLogging, "VerboseLogging", false);
			Scribe_Values.Look(ref AllowInfection, "AllowInfection", false);
			Scribe_Values.Look(ref Severity, "Severity", 0.05f);
			Scribe_Collections.Look(ref ExcludedColors, "ExcludedColors", LookMode.Value, LookMode.Value);
			if ((ExcludedColors?.Count ?? 0) == 0)
			{
				ExcludedColors = DefaultExcludedColors;
			}
		}
	}
}