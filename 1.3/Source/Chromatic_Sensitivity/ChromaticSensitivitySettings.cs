using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Chromatic_Sensitivity.ColorControl;
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
    private readonly Listing_Standard _options = new Listing_Standard();
    private const float DefaultSeverity = 0.05f;
    private float? _severity;

    public bool AllowInfection;
    public bool VerboseLogging;

    public float Severity {
      get => _severity ?? DefaultSeverity;
      set => _severity = value;
    }

    private byte _red = 125;
    private byte _green = 125;
    private byte _blue = 125;
    private string _colorName = "helpful name for color";
    private string _exportPath = "C:\\RimworldExport";
    
    /**
     * List of colors to exclude from possible consideration
     * Used to avoid the box counting as the dominant color for things like Rice, Corn etc.
     */
    public Dictionary<int, string> ExcludedColors = new Dictionary<int, string>();

    /**
     * List of explicit overrides for defs where the algorithm picks an undesirable color 
     */
    public Dictionary<string, Color> ThingDefColors = new Dictionary<string, Color>();
    
    /**
     * List of defs to not even try to run chromatic actions on e.g. packaged survival meals
     * When eaten gives a small mood debuff for eating such blandly coloured food. 
     */
    public List<string> ExcludedDefs = new List<string>();

    private static readonly Dictionary<string, Color> DefaultThingDefColors = new Dictionary<string, Color>
    {
      { "Meat_Megaspider", new Color32(128, 128, 75, Byte.MaxValue) },
      { "Plant_Strawberry", new Color32(240, 0, 60, Byte.MaxValue) },
      { "Plant_Ambrosia", new Color32(255, 200, 15, Byte.MaxValue) },
      { "Plant_Daylily", new Color32(198, 143, 55, Byte.MaxValue) },
      { "Plant_Berry", new Color32(82, 109, 165, Byte.MaxValue) },
      { "Plant_Corn", new Color32(255, 255, 100, Byte.MaxValue) },
      { "Plant_Rose", new Color32(239, 136, 190, Byte.MaxValue) }
    };

    private static readonly Lazy<List<string>> DefaultExcludedDefs = new Lazy<List<string>>(() => new List<string>()
    {
      "MealSurvivalPack"
    });
      
    private static readonly Lazy<Dictionary<int, string>> DefaultExcludedColors = new Lazy<Dictionary<int, string>>(
      () => new Dictionary<int, string>
      {
        { 140 | 101 << 8 | 49 << 16, "ChromaticSensitivity_RawFoodBoxes".Translate() },
        { 165 | 125 << 8 | 57 << 16, "ChromaticSensitivity_RawFoodBoxes".Translate() },
        { 123 | 85 << 8 | 33 << 16, "ChromaticSensitivity_RawFoodBoxes".Translate() },
        { 0, "ChromaticSensitivity_Outline".Translate() }
      });

    public void DoWindowContents(Rect wrect)
    {
      var viewPort = wrect.GetInnerRect();
      _options.Begin(wrect);

      _options.CheckboxLabeled("ChromaticSensitivity_Verbose".Translate(), ref VerboseLogging);
      _options.CheckboxLabeled("ChromaticSensitivity_Infection".Translate(), ref AllowInfection,
        "ChromaticSensitivity_InfectionHelp".Translate());
      _options.Gap();

      var severityRect = _options.GetRect(RowHeight);
      var severityLabel = "ChromaticSensitivity_SeverityPercent".Translate(Severity * 100);
      Severity = Widgets.HorizontalSlider(severityRect, Severity * 100, 0f, 100.0f, false,
        severityLabel, "0", "100", 0.5f) / 100f;

      UpdateHediffDef();

      _options.Gap();
      _exportPath = _options.TextEntryLabeled("ChromaticSensitivity_ExportPath".Translate() + "\t", _exportPath);
      if (_options.ButtonText("ChromaticSensitivity_DumpAll".Translate()))
      {
        DumpAllTexturesWithSelectedColors(_exportPath);
      }

      _options.Label("ChromaticSensitivity_ColorTweaking".Translate());
      _options.Label("ChromaticSensitivity_Red".Translate().Colorize(Color.red));
      var rectR = _options.GetRect(RowHeight);
      _red = (byte)Widgets.HorizontalSlider(rectR, _red, 0f, 255f, false, _red.ToString(CultureInfo.InvariantCulture),
        "0",
        "255", 1f);

      _options.Label("ChromaticSensitivity_Green".Translate().Colorize(Color.green));
      var rectG = _options.GetRect(RowHeight);
      _green = (byte)Widgets.HorizontalSlider(rectG, _green, 0f, 255f, false,
        _green.ToString(CultureInfo.InvariantCulture), "0", "255", 1f);

      _options.Label("ChromaticSensitivity_Blue".Translate().Colorize(Color.blue));
      var rectB = _options.GetRect(RowHeight);
      _blue = (byte)Widgets.HorizontalSlider(rectB, _blue, 0f, 255f, false,
        _blue.ToString(CultureInfo.InvariantCulture),
        "0", "255", 1f);

      _options.Gap();
      _options.Label(
        "ChromaticSensitivity_SelectedColor".Translate(CurrentColorAsHexString()
          .Colorize(GetSelectedColor32())));
      _colorName = _options.TextEntryLabeled($"{"ChromaticSensitivity_ColorName".Translate()}\t", _colorName);
      if (_options.ButtonTextLabeled("ChromaticSensitivity_ExcludeColor".Translate(),
            "ChromaticSensitivity_Add".Translate()))
      {
        ExcludedColors.SetOrAdd(ColorExtractor.CompactColor(GetSelectedColor32()),
          _colorName);
      }

      if (_options.ButtonTextLabeled("ChromaticSensitivity_OverrideColor".Translate(),
            "ChromaticSensitivity_Add".Translate()))
      {
        ThingDefColors.SetOrAdd(_colorName, GetSelectedColor32());
      }

      _options.Gap();
      _options.Label("ChromaticSensitivity_TweakedColors".Translate());

      var scrollRect = viewPort.BottomPartPixels(viewPort.yMax - _options.CurHeight);
      var tweakColorsRect = new Rect(0, _options.CurHeight, scrollRect.width - 32,
        TweakedColorRowHeight * (ExcludedColors.Count + ThingDefColors.Count) + 100f);
      _options.Indent(Indent);
      Widgets.BeginScrollView(scrollRect, ref _scrollPosition, tweakColorsRect);
      var tweakedColorListing = new Listing_Standard();
      tweakedColorListing.Begin(tweakColorsRect);
      tweakedColorListing.Label("ChromaticSensitivity_ColorOverridesLabel".Translate());
      tweakedColorListing.Indent(Indent);
      foreach (var colorForDef in ThingDefColors.ToList()
                 .Where(colorForDef =>
                 {
                   var taggedString = "ChromaticSensitivity_UsingColorOverride"
                     .Translate(RGBString(colorForDef.Value).Colorize(colorForDef.Value),
                       colorForDef.Key).ToString();
                   return tweakedColorListing.ButtonTextLabeled(
                     taggedString,
                     "ChromaticSensitivity_Remove".Translate());
                 }))
      {
        ThingDefColors.Remove(colorForDef.Key);
      }

      tweakedColorListing.Outdent(Indent);
      tweakedColorListing.GapLine();
      tweakedColorListing.Label("ChromaticSensitivity_ColorExclusionLabel".Translate());
      tweakedColorListing.Indent(Indent);
      foreach (var unpacked in from excludedColor in ExcludedColors.ToList()
               let unpacked = ColorExtractor.UnpackColor(excludedColor.Key)
               let translatedKey = "ChromaticSensitivity_ExcludingColor".Translate(excludedColor.Value,
                 RGBString(unpacked).Colorize(unpacked)).ToString()
               where tweakedColorListing.ButtonTextLabeled(translatedKey, "ChromaticSensitivity_Remove".Translate())
               select unpacked)
      {
        ExcludedColors.Remove(
          ColorExtractor.CompactColor(new Color32(unpacked.r, unpacked.g, unpacked.b, byte.MaxValue)));
      }

      tweakedColorListing.Outdent(Indent);
      tweakedColorListing.GapLine();
      tweakedColorListing.Label("ChromaticSensitivity_DefExclusionLabel".Translate());
      tweakedColorListing.Indent(Indent);
      foreach (var excludedDef in from excludedDef in ExcludedDefs.ToList()
               let translatedKey = "ChromaticSensitivity_ExcludingDef".Translate(excludedDef).ToString()
               where tweakedColorListing.ButtonTextLabeled(translatedKey, "ChromaticSensitivity_Remove".Translate())
               select excludedDef)
      {
        ExcludedDefs.Remove(excludedDef);
      }

      tweakedColorListing.End();
      Widgets.EndScrollView();
      _options.End();
    }

    private Color32 GetSelectedColor32()
    {
      return new Color32(_red, _green, _blue, byte.MaxValue);
    }

    private string RGBString(Color32 color32)
    {
      return $"({color32.r},{color32.g},{color32.b})";
    }

    private void UpdateHediffDef()
    {
      var hediffDef = DefDatabase<HediffDef>.GetNamed("Taggerung_ChromaticSensitivity", false);
      if (hediffDef == null) return;
      hediffDef.scenarioCanAdd = AllowInfection;
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
      Scribe_Values.Look(ref _severity, "Severity", 0.05f);
      Scribe_Collections.Look(ref ExcludedDefs, "ExcludedDefs", LookMode.Value);
      Scribe_Collections.Look(ref ExcludedColors, "ExcludedColors", LookMode.Value, LookMode.Value);
      Scribe_Collections.Look(ref ThingDefColors, "ThingDefColors", LookMode.Value, LookMode.Value);
      if ((ExcludedDefs?.Count ?? 0) == 0) ExcludedDefs = DefaultExcludedDefs.Value;
      if ((ExcludedColors?.Count ?? 0) == 0) ExcludedColors = DefaultExcludedColors.Value;
      if ((ThingDefColors?.Count ?? 0) == 0) ThingDefColors = DefaultThingDefColors;
      UpdateHediffDef();
    }
  }
}