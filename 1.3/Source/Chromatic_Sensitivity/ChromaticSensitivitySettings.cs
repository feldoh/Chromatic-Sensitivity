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
	public class ChromaticSensitivitySettings : ModSettings
	{
		private const float RowHeight = 22f;
		private const float TweakedColorRowHeight = 32f;
		private const float Indent = 9f;
		private static Vector2 _scrollPosition;
		private Listing_Standard _options = new Listing_Standard();

		public bool AllowInfection;
		public bool VerboseLogging;
		public float Severity;

		private byte _red = 125;
		private byte _green = 125;
		private byte _blue = 125;
		private string _colorName = "helpful name for color";
		private string _exportPath = "C:\\RimworldExport";

		/**
		 * List of colors to exclude from possible consideration
		 * Used to avoid the box counting as the dominant color for things like Rice, Corn etc.
		 */
		public Dictionary<int, string> ExcludedColors = new Dictionary<int, string>(DefaultExcludedColors);

		public Dictionary<string, Color> ThingDefColors = new Dictionary<string, Color>(DefaultThingDefColors);

		private static readonly Dictionary<string, Color> DefaultThingDefColors = new Dictionary<string, Color>
		{
			{ "Meat_Megaspider", new Color32(128, 128, 75, Byte.MaxValue) }
		};

		private static readonly Dictionary<int, string> DefaultExcludedColors = new Dictionary<int, string>
		{
			{ 140 | 101 << 8 | 49 << 16, "Raw Food Boxes" },
			{ 165 | 125 << 8 | 57 << 16, "Raw Food Boxes 2" },
			{ 123 | 85 << 8 | 33 << 16, "Raw Food Boxes 3" },
			{ 0, "Outline; pure black" }
		};

		public void DoWindowContents(Rect wrect)
		{
			var viewPort = wrect.GetInnerRect();
			_options.Begin(wrect);

			_options.CheckboxLabeled("Enable Verbose logging", ref VerboseLogging);
			_options.CheckboxLabeled("Allow infection", ref AllowInfection,
				"Allow the scenario to add the Chromatic_Sensitivity hediff to pawns");
			_options.Gap();

			var severityRect = _options.GetRect(RowHeight);
			Severity = Widgets.HorizontalSlider(severityRect, Severity * 100, 0f, 100.0f, false,
				$"Severity Percent: {Severity * 100}", "0", "100",
				0.5f) / 100f;

			UpdateHediffDef();

			_options.Gap();
			_exportPath = _options.TextEntryLabeled("Export path", _exportPath);
			if (_options.ButtonText("Dump All"))
			{
				DumpAllTexturesWithSelectedColors(_exportPath);
			}

			_options.Label("Chromatic sensitivity colour tweaking");
			_options.Label("<color=#FF0000>Red</color>");
			var rectR = _options.GetRect(RowHeight);
			_red = (byte)Widgets.HorizontalSlider(rectR, _red, 0f, 255f, false, _red.ToString(CultureInfo.InvariantCulture),
				"0",
				"255", 1f);

			_options.Label("<color=#00FF00>Green</color>");
			var rectG = _options.GetRect(RowHeight);
			_green = (byte)Widgets.HorizontalSlider(rectG, _green, 0f, 255f, false,
				_green.ToString(CultureInfo.InvariantCulture), "0", "255", 1f);

			_options.Label("<color=#0000FF>Blue</color>");
			var rectB = _options.GetRect(RowHeight);
			_blue = (byte)Widgets.HorizontalSlider(rectB, _blue, 0f, 255f, false,
				_blue.ToString(CultureInfo.InvariantCulture),
				"0", "255", 1f);

			_options.Gap();
			_options.Label($"Selected color is: <color=#{CurrentColorAsHexString()}>{CurrentColorAsHexString()}</color>");
			_colorName = _options.TextEntryLabeled("Name", _colorName);
			if (_options.ButtonTextLabeled("Add color to excluded list", "add"))
			{
				ExcludedColors.SetOrAdd(ColorExtractor.CompactColor(new Color32(_red, _green, _blue, byte.MaxValue)),
					_colorName);
			}

			if (_options.ButtonTextLabeled("Use selected color for def with given name", "add"))
			{
				ThingDefColors.SetOrAdd(_colorName, new Color32(_red, _green, _blue, byte.MaxValue));
			}

			_options.Gap();
			_options.Label("Tweaked Colors");

			var scrollRect = viewPort.BottomPartPixels(viewPort.yMax - _options.CurHeight);
			var tweakColorsRect = new Rect(0, _options.CurHeight, scrollRect.width - 32,
				TweakedColorRowHeight * (ExcludedColors.Count + ThingDefColors.Count) + 100f);
			_options.Indent(Indent);
			Widgets.BeginScrollView(scrollRect, ref _scrollPosition, tweakColorsRect);
			var tweakedColourListing = new Listing_Standard();
			tweakedColourListing.Begin(tweakColorsRect);
			tweakedColourListing.Label("ThingDef Color Overrides");
			tweakedColourListing.Indent(Indent);
			foreach (var colorForDef in ThingDefColors.ToList()
				         .Where(colorForDef => tweakedColourListing.ButtonTextLabeled(
					         $"Using <color=#{AsHexString(colorForDef.Value)}>{AsHexString(colorForDef.Value)}</color> for {colorForDef.Key}",
					         "remove")))
			{
				ThingDefColors.Remove(colorForDef.Key);
			}

			tweakedColourListing.Outdent(Indent);
			tweakedColourListing.GapLine();
			tweakedColourListing.Label("Colors to avoid unless there is no better option");
			tweakedColourListing.Indent(Indent);
			foreach (var unpacked in from excludedColor in ExcludedColors.ToList()
			         let unpacked = ColorExtractor.UnpackColor(excludedColor.Key)
			         where tweakedColourListing.ButtonTextLabeled(
				         $"Excluded {excludedColor.Value} <color=#{AsHexString(unpacked)}>{unpacked.ToString()}</color>",
				         "remove")
			         select unpacked)
			{
				ExcludedColors.Remove(
					ColorExtractor.CompactColor(new Color32(unpacked.r, unpacked.g, unpacked.b, byte.MaxValue)));
			}

			tweakedColourListing.End();
			Widgets.EndScrollView();
			_options.End();
		}

		private void UpdateHediffDef()
		{
			var hediffDef = DefDatabase<HediffDef>.GetNamed("Taggerung_ChromaticSensitivity", false);
			if (hediffDef == null) return;
			hediffDef.scenarioCanAdd = AllowInfection;
			if (!(Math.Abs(Severity - hediffDef.initialSeverity) > 0.005)) return;
			foreach (var p in PawnsFinder.AllMapsAndWorld_Alive)
				if (p.health.hediffSet.GetFirstHediffOfDef(
					    HediffDef.Named("Taggerung_ChromaticSensitivity")) is Hediff hediff)
					hediff.Severity = Severity;
			hediffDef.initialSeverity = Severity;
		}

		private static string AsHexString(Color32 color)
		{
			return $"{(int)color.r:X2}{(int)color.g:X2}{(int)color.b:X2}";
		}

		private string CurrentColorAsHexString()
		{
			return $"{(int)_red:X2}{(int)_green:X2}{(int)_blue:X2}";
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
			Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
			Scribe_Values.Look(ref AllowInfection, "AllowInfection");
			Scribe_Values.Look(ref Severity, "Severity", 0.05f);
			Scribe_Collections.Look(ref ExcludedColors, "ExcludedColors", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref ThingDefColors, "ThingDefColors", LookMode.Value, LookMode.Value);
			if ((ExcludedColors?.Count ?? 0) == 0) ExcludedColors = DefaultExcludedColors;
			if ((ThingDefColors?.Count ?? 0) == 0) ThingDefColors = DefaultThingDefColors;
			UpdateHediffDef();
		}
	}
}