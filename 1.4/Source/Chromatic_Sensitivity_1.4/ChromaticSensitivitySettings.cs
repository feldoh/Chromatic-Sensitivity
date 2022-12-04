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
    private readonly Listing_Standard _options = new();
    private const float DefaultSeverity = 0.05f;
    private float? _severity;

    public bool VerboseLogging;
    public bool AllowWhite;
    public bool ConsiderFloors;

    public ChromaticColorType PeriodicSkinEffect = ChromaticColorType.None;
    public ChromaticColorType PeriodicHairEffect = ChromaticColorType.Random;

    public ChromaticColorType IngestionSkinEffect = ChromaticColorType.Dominant;
    public ChromaticColorType IngestionHairEffect = ChromaticColorType.None;

    public float Severity
    {
      get => _severity ?? DefaultSeverity;
      set => _severity = value;
    }

    public bool AnyIngestionEffect() => IngestionHairEffect != ChromaticColorType.None ||
                                        IngestionSkinEffect != ChromaticColorType.None;
    public bool AnyPeriodicEffect() => PeriodicHairEffect != ChromaticColorType.None ||
                                        PeriodicSkinEffect != ChromaticColorType.None;

    private byte _red = 125;
    private byte _green = 125;
    private byte _blue = 125;
    private string _colorName = "helpful name for color";
    private string _exportPath = "C:\\RimworldExport";

    /**
     * List of colors to exclude from possible consideration
     * Used to avoid the box counting as the dominant color for things like Rice, Corn etc.
     */
    public Dictionary<int, string> ExcludedColors = new();

    /**
     * List of explicit overrides for defs where the algorithm picks an undesirable color 
     */
    public Dictionary<string, Color> ThingDefColors = new();

    /**
     * List of defs to not even try to run chromatic actions on e.g. packaged survival meals
     * When eaten gives a small mood debuff for eating such blandly coloured food. 
     */
    public List<string> ExcludedDefs = new();

    private static readonly Dictionary<string, Color> DefaultThingDefColors = new()
    {
      { "Meat_Megaspider", new Color32(128, 128, 75, Byte.MaxValue) },
      { "Plant_Strawberry", new Color32(240, 0, 60, Byte.MaxValue) },
      { "Plant_Ambrosia", new Color32(255, 200, 15, Byte.MaxValue) },
      { "Plant_Daylily", new Color32(198, 143, 55, Byte.MaxValue) },
      { "Plant_Berry", new Color32(82, 109, 165, Byte.MaxValue) },
      { "Plant_Corn", new Color32(255, 255, 100, Byte.MaxValue) },
      { "Plant_Rose", new Color32(239, 136, 190, Byte.MaxValue) }
    };

    private static readonly Lazy<List<string>> DefaultExcludedDefs = new(() => new List<string>()
    {
      "MealSurvivalPack"
    });

    private static readonly Lazy<Dictionary<int, string>> DefaultExcludedColors = new(
      () => new Dictionary<int, string>
      {
        { 140 | 101 << 8 | 49 << 16, "ChromaticSensitivity_RawFoodBoxes".Translate() },
        { 165 | 125 << 8 | 57 << 16, "ChromaticSensitivity_RawFoodBoxes".Translate() },
        { 123 | 85 << 8 | 33 << 16, "ChromaticSensitivity_RawFoodBoxes".Translate() },
        { 0, "ChromaticSensitivity_Outline".Translate() }
      });

    private enum Tab
    {
      Basics,
      Overrides,
      Exclusions
    }

    private static Tab _tab = Tab.Basics;

    public void DoWindowContents(Rect wrect)
    {
      Rect viewPort = DrawTabs(wrect);
      _options.Begin(viewPort);

      switch (_tab)
      {
        case Tab.Basics:
          DrawBasics();
          break;
        case Tab.Overrides:
          DrawOverrides(viewPort);
          break;
        case Tab.Exclusions:
          DrawExclusions(viewPort);
          break;
        default:
          throw new ArgumentException($"Unknown tab selected: {_tab.ToString()}");
      }

      _options.End();
    }

    private void DrawExclusions(Rect viewPort)
    {
      DrawColorPicker();

      if (_options.ButtonTextLabeled("ChromaticSensitivity_ExcludeColor".Translate(),
            "ChromaticSensitivity_Add".Translate()))
      {
        ExcludedColors.SetOrAdd(ColorHelper.CompactColor(GetSelectedColor32()),
          _colorName);
      }

      Listing_Standard scrollableListing = MakeScrollableSubListing(viewPort,
        TweakedColorRowHeight * (ExcludedColors.Count + ExcludedDefs.Count) + 100f);
      scrollableListing.Label("ChromaticSensitivity_ColorExclusionLabel".Translate());
      scrollableListing.Indent(Indent);
      foreach (Color32 unpacked in from excludedColor in ExcludedColors.ToList()
               let unpacked = ColorHelper.UnpackColor32(excludedColor.Key)
               let translatedKey = "ChromaticSensitivity_ExcludingColor".Translate(excludedColor.Value,
                 RGBString(unpacked).Colorize(unpacked)).ToString()
               where scrollableListing.ButtonTextLabeled(translatedKey, "ChromaticSensitivity_Remove".Translate())
               select unpacked)
      {
        ExcludedColors.Remove(
          ColorHelper.CompactColor(new Color32(unpacked.r, unpacked.g, unpacked.b, byte.MaxValue)));
      }

      scrollableListing.Outdent(Indent);
      scrollableListing.GapLine();
      scrollableListing.Label("ChromaticSensitivity_DefExclusionLabel".Translate());
      scrollableListing.Indent(Indent);
      foreach (var excludedDef in from excludedDef in ExcludedDefs.ToList()
               let translatedKey = "ChromaticSensitivity_ExcludingDef".Translate(excludedDef).ToString()
               where scrollableListing.ButtonTextLabeled(translatedKey, "ChromaticSensitivity_Remove".Translate())
               select excludedDef)
      {
        ExcludedDefs.Remove(excludedDef);
      }

      EndScrollableSubListing(scrollableListing);
    }

    private void DrawOverrides(Rect viewPort)
    {
      _options.Label("ChromaticSensitivity_ColorTweaking".Translate());
      DrawColorPicker();

      if (_options.ButtonTextLabeled("ChromaticSensitivity_OverrideColor".Translate(),
            "ChromaticSensitivity_Add".Translate()))
      {
        ThingDefColors.SetOrAdd(_colorName, GetSelectedColor32());
      }

      _options.Gap();
      _options.Label("ChromaticSensitivity_TweakedColors".Translate());

      Listing_Standard scrollableListing =
        MakeScrollableSubListing(viewPort, TweakedColorRowHeight * (ThingDefColors.Count) + 100f);
      scrollableListing.Label("ChromaticSensitivity_ColorOverridesLabel".Translate());
      scrollableListing.Indent(Indent);
      foreach (var colorForDef in ThingDefColors.ToList()
                 .Where(colorForDef =>
                 {
                   var taggedString = "ChromaticSensitivity_UsingColorOverride"
                     .Translate(RGBString(colorForDef.Value).Colorize(colorForDef.Value),
                       colorForDef.Key).ToString();
                   return scrollableListing.ButtonTextLabeled(
                     taggedString,
                     "ChromaticSensitivity_Remove".Translate());
                 }))
      {
        ThingDefColors.Remove(colorForDef.Key);
      }

      EndScrollableSubListing(scrollableListing);
    }

    private Rect DrawTabs(Rect rect)
    {
      List<TabRecord> tabsList = new()
      {
        new TabRecord("ChromaticSensitivity_Settings_Basics".Translate(), () => _tab = Tab.Basics,
          _tab == Tab.Basics),
        new TabRecord("ChromaticSensitivity_Settings_Overrides".Translate(), () => _tab = Tab.Overrides,
          _tab == Tab.Overrides),
        new TabRecord("ChromaticSensitivity_Settings_Exclusions".Translate(), () => _tab = Tab.Exclusions,
          _tab == Tab.Exclusions)
      };

      Rect tabRect = new(rect)
      {
        yMin = 80
      };
      TabDrawer.DrawTabs(tabRect, tabsList);

      return tabRect.GetInnerRect();
    }

    private void EndScrollableSubListing(Listing scrollableListing)
    {
      scrollableListing.Outdent(Indent);
      scrollableListing.GapLine();
      scrollableListing.End();
      Widgets.EndScrollView();
    }

    private Listing_Standard MakeScrollableSubListing(Rect viewPort, float height)
    {
      Rect scrollRect = viewPort.BottomPartPixels(viewPort.yMax - _options.CurHeight);
      Rect tweakColorsRect = new(0, _options.CurHeight, scrollRect.width - 32, height);
      _options.Indent(Indent);
      Widgets.BeginScrollView(scrollRect, ref _scrollPosition, tweakColorsRect);
      Listing_Standard scrollableListing = new();
      scrollableListing.Begin(tweakColorsRect);
      return scrollableListing;
    }

    private void DrawColorPicker()
    {
      _options.Label("ChromaticSensitivity_Red".Translate().Colorize(Color.red));
      Rect rectR = _options.GetRect(RowHeight);
      _red = (byte)Widgets.HorizontalSlider(rectR, _red, 0f, 255f, false, _red.ToString(CultureInfo.InvariantCulture),
        "0",
        "255", 1f);

      _options.Label("ChromaticSensitivity_Green".Translate().Colorize(Color.green));
      Rect rectG = _options.GetRect(RowHeight);
      _green = (byte)Widgets.HorizontalSlider(rectG, _green, 0f, 255f, false,
        _green.ToString(CultureInfo.InvariantCulture), "0", "255", 1f);

      _options.Label("ChromaticSensitivity_Blue".Translate().Colorize(Color.blue));
      Rect rectB = _options.GetRect(RowHeight);
      _blue = (byte)Widgets.HorizontalSlider(rectB, _blue, 0f, 255f, false,
        _blue.ToString(CultureInfo.InvariantCulture),
        "0", "255", 1f);

      _options.Gap();
      _options.Label(
        "ChromaticSensitivity_SelectedColor".Translate(CurrentColorAsHexString()
          .Colorize(GetSelectedColor32())));
      _colorName = _options.TextEntryLabeled($"{"ChromaticSensitivity_ColorName".Translate()}\t", _colorName);
    }

    private void DrawBasics()
    {
      _options.CheckboxLabeled("ChromaticSensitivity_Verbose".Translate(), ref VerboseLogging);
      _options.CheckboxLabeled("ChromaticSensitivity_AllowWhite".Translate(), ref AllowWhite,
        "ChromaticSensitivity_AllowWhiteTooltip".Translate());
      _options.CheckboxLabeled("ChromaticSensitivity_ConsiderFloors".Translate(), ref ConsiderFloors,
        "ChromaticSensitivity_ConsiderFloorsTooltip".Translate());
      _options.Gap();

      _options.Label("ChromaticSensitivity_SeverityPercent_Description".Translate());
      Rect severityRect = _options.GetRect(RowHeight);
      TaggedString severityLabel = "ChromaticSensitivity_SeverityPercent".Translate(Severity * 100);
      Severity = Widgets.HorizontalSlider(severityRect, Severity * 100, 0f, 100.0f, false,
        severityLabel, "0", "100", 0.5f) / 100f;

      _options.Gap();
      _exportPath = _options.TextEntryLabeled("ChromaticSensitivity_ExportPath".Translate() + "\t", _exportPath);
      if (_options.ButtonText("ChromaticSensitivity_DumpAll".Translate(), widthPct: 0.4f))
      {
        DumpAllTexturesWithSelectedColors(_exportPath);
      }

      _options.GapLine();
      Rect dropDownRect = _options.GetRect(RowHeight, 0.8f);
      Widgets.Dropdown(dropDownRect,
        this,
        s => s.PeriodicSkinEffect,
        s => ChromaticColorTypeMenuOptions(s,
          (settings, chromaticColorType) => settings.PeriodicSkinEffect = chromaticColorType),
        "ChromaticSensitivity_PeriodicSkinEffect".Translate(PeriodicSkinEffect.ToString())
          .Truncate(dropDownRect.width));

      _options.GapLine(3f);
      dropDownRect = _options.GetRect(RowHeight, 0.8f);
      Widgets.Dropdown(dropDownRect,
        this,
        s => s.PeriodicHairEffect,
        s => ChromaticColorTypeMenuOptions(s,
          (settings, chromaticColorType) => settings.PeriodicHairEffect = chromaticColorType),
        "ChromaticSensitivity_PeriodicHairEffect".Translate(PeriodicHairEffect.ToString())
          .Truncate(dropDownRect.width));

      _options.GapLine();
      dropDownRect = _options.GetRect(RowHeight, 0.8f);
      Widgets.Dropdown(dropDownRect,
        this,
        s => s.IngestionSkinEffect,
        s => ChromaticColorTypeMenuOptions(s,
          (settings, chromaticColorType) => settings.IngestionSkinEffect = chromaticColorType),
        "ChromaticSensitivity_IngestionSkinEffect".Translate(IngestionSkinEffect.ToString())
          .Truncate(dropDownRect.width));

      _options.GapLine(3f);
      dropDownRect = _options.GetRect(RowHeight, 0.8f);
      Widgets.Dropdown(dropDownRect,
        this,
        s => s.IngestionHairEffect,
        s => ChromaticColorTypeMenuOptions(s,
          (settings, chromaticColorType) => settings.IngestionHairEffect = chromaticColorType),
        "ChromaticSensitivity_IngestionHairEffect".Translate(IngestionHairEffect.ToString())
          .Truncate(dropDownRect.width));
    }

    private static IEnumerable<Widgets.DropdownMenuElement<ChromaticColorType>> ChromaticColorTypeMenuOptions(
      ChromaticSensitivitySettings settings, Action<ChromaticSensitivitySettings, ChromaticColorType> action)
    {
      return ((ChromaticColorType[])Enum.GetValues(typeof(ChromaticColorType)))
        .Select(effect => new Widgets.DropdownMenuElement<ChromaticColorType>
        {
          option = new FloatMenuOption(effect.ToString(), () => action(settings, effect)),
          payload = effect
        });
    }

    private Color32 GetSelectedColor32()
    {
      return new Color32(_red, _green, _blue, byte.MaxValue);
    }

    private string RGBString(Color32 color32)
    {
      return $"({color32.r},{color32.g},{color32.b})";
    }

    private string CurrentColorAsHexString()
    {
      return $"{(int)_red:X2}{(int)_green:X2}{(int)_blue:X2}";
    }

    private static void DumpAllTexturesWithSelectedColors(string path)
    {
      Directory.CreateDirectory(path);
      ColorHelper c = new();
      foreach (ThingDef ingestible in DefDatabase<ThingDef>.AllDefsListForReading.Where(def =>
                 def.IsIngestible && typeof(ThingWithComps).IsAssignableFrom(def.thingClass)))
      {
        var dominantColor = c.ExtractDominantColor(ingestible);
        if (dominantColor is not { } color) continue;
        Texture2D texture2D = (Texture2D)ingestible.graphic.MatSingle.mainTexture;
        if (texture2D == BaseContent.BadTex) continue;
        var texturePngPath = $"{path}\\{ingestible.defName}.png";
        if (File.Exists(texturePngPath)) File.Delete(texturePngPath);
        TextureAtlasHelper.WriteDebugPNG(texture2D, texturePngPath);

        RenderTexture temporary = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0,
          RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        temporary.name = "MakeReadableTexture_Temp";
        Graphics.Blit(texture2D, temporary);
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = temporary;
        Texture2D targetTexture = new(64, 64);
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
      Scribe_Values.Look(ref AllowWhite, "AllowWhite", true);
      Scribe_Values.Look(ref ConsiderFloors, "ConsiderFloors", false);
      Scribe_Values.Look(ref _severity, "Severity", 0.05f);
      Scribe_Values.Look(ref PeriodicSkinEffect, "PeriodicSkinEffect", ChromaticColorType.None);
      Scribe_Values.Look(ref PeriodicHairEffect, "PeriodicHairEffect", ChromaticColorType.Random);
      Scribe_Values.Look(ref IngestionSkinEffect, "IngestionSkinEffect", ChromaticColorType.Dominant);
      Scribe_Values.Look(ref IngestionHairEffect, "IngestionHairEffect", ChromaticColorType.None);
      Scribe_Collections.Look(ref ExcludedDefs, "ExcludedDefs", LookMode.Value);
      Scribe_Collections.Look(ref ExcludedColors, "ExcludedColors", LookMode.Value, LookMode.Value);
      Scribe_Collections.Look(ref ThingDefColors, "ThingDefColors", LookMode.Value, LookMode.Value);
      if ((ExcludedDefs?.Count ?? 0) == 0) ExcludedDefs = DefaultExcludedDefs.Value;
      if ((ExcludedColors?.Count ?? 0) == 0) ExcludedColors = DefaultExcludedColors.Value;
      if ((ThingDefColors?.Count ?? 0) == 0) ThingDefColors = DefaultThingDefColors;
    }
  }
}
