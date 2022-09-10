using Chromatic_Sensitivity.ColorControl;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
  public class ChromaticSensitivity : Mod
  {
    public static ChromaticSensitivitySettings Settings;
    private const string AlienRacesPackageId = "erdelf.humanoidalienraces";
    public static bool AlienRacesEnabled;
    public static ISkinColorManager SkinColorManager;
    public static ColorHelper ColorHelper = new ColorHelper();
    public static IGraphicHandler GraphicHandler = new DefaultGraphicHandler();

    public ChromaticSensitivity(ModContentPack content) : base(content)
    {
      Log.Verbose("Feel the rainbow");

      // initialize settings
      Settings = GetSettings<ChromaticSensitivitySettings>();
      AlienRacesEnabled = LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == AlienRacesPackageId);
      Log.Verbose($"AlienRacesEnabled: {AlienRacesEnabled}");
      SkinColorManager = SkinColorManagerFactory.DefaultSkinColorManager;
      
#if DEBUG
	Harmony.DEBUG = true;
#endif

      Harmony harmony = new Harmony("Taggerung.ChromaticSensitivity");
      harmony.PatchAll();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
      base.DoSettingsWindowContents(inRect);
      Settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
      return "Chromatic Sensitivity";
    }
  }
}