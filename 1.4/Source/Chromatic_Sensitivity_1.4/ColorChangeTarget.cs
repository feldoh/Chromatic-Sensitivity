using Chromatic_Sensitivity;
using Chromatic_Sensitivity.ColorControl;
using UnityEngine;
using Verse;

public enum ColorChangeTarget
{
  Skin,
  Hair
}

public static class ChromaticColorTargetExtensions
{
  public static ChromaticColorType IngestionChromaticColorType(this ColorChangeTarget chromaticColorChangeTarget) =>
    chromaticColorChangeTarget switch
    {
      ColorChangeTarget.Skin => ChromaticSensitivity.Settings.IngestionSkinEffect,
      ColorChangeTarget.Hair => ChromaticSensitivity.Settings.IngestionHairEffect,
      _ => ChromaticColorType.None
    };

  public static ChromaticColorType PeriodicChromaticColorType(this ColorChangeTarget chromaticColorChangeTarget) =>
    chromaticColorChangeTarget switch
    {
      ColorChangeTarget.Skin => ChromaticSensitivity.Settings.PeriodicSkinEffect,
      ColorChangeTarget.Hair => ChromaticSensitivity.Settings.PeriodicHairEffect,
      _ => ChromaticColorType.None
    };
  
  public static Color? GetColor(this ColorChangeTarget chromaticColorChangeTarget, ISkinColorManager colorManager, Pawn pawn) =>
    chromaticColorChangeTarget switch
    {
      ColorChangeTarget.Skin => colorManager.GetSkinColor(pawn),
      ColorChangeTarget.Hair => colorManager.GetHairColor(pawn),
      _ => null
    };
  
  public static bool SetColor(this ColorChangeTarget chromaticColorChangeTarget, ISkinColorManager colorManager, Pawn pawn, Color targetColor) =>
    chromaticColorChangeTarget switch
    {
      ColorChangeTarget.Skin => colorManager.SetSkinColor(pawn, targetColor),
      ColorChangeTarget.Hair => colorManager.SetHairColor(pawn, targetColor),
      _ => false
    };
}
