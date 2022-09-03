using System;
using System.Linq;
using Chromatic_Sensitivity.ColorControl;
using RimWorld;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
  public class Hediff_ChromaticSensitivity : HediffWithComps
  {
    private readonly ColorHelper _colorHelper;
    private readonly IGraphicHandler _graphicHandler;

    #region Properties

    public HediffDef_ChromaticSensitivity Def => def as HediffDef_ChromaticSensitivity;
    public Color? OriginalColor;

    #endregion Properties

    public Hediff_ChromaticSensitivity() : this(null, null) { }

    public Hediff_ChromaticSensitivity(ColorHelper colorHelper, IGraphicHandler graphicHandler)
    {
      _colorHelper = colorHelper ?? ChromaticSensitivity.ColorHelper;
      _graphicHandler = graphicHandler ?? ChromaticSensitivity.GraphicHandler;
    }

    public override void PostAdd(DamageInfo? damageInfo)
    {
      base.PostAdd(damageInfo); 
      OriginalColor = ChromaticSensitivity.SkinColorManager.GetSkinColor(pawn);
      Log.Verbose($"Saved pawn base color {OriginalColor}");
    }

    public override void PostRemoved()
    {
      var restoredColor = OriginalColor ?? PawnSkinColors.GetSkinColor(pawn.story?.melanin ?? PawnSkinColors.RandomMelanin(pawn.Faction));
      Log.Verbose($"Restoring pawn base color to {restoredColor} (Original: {OriginalColor}) from {ChromaticSensitivity.SkinColorManager.GetSkinColor(pawn)}");
      ChromaticSensitivity.SkinColorManager.SetSkinColor(pawn, restoredColor);
      base.PostRemoved();
    }

    public override void ExposeData()
    {
      base.ExposeData();
      Scribe_Values.Look(ref OriginalColor, "OriginalColor");
    }

    #region Helpers

    private Color MoveColorsCloser(Color currentColor, Color? maybeTargetColor, float amount)
    {
      return maybeTargetColor is Color targetColor ? Color.Lerp(currentColor, targetColor, amount) : currentColor;
    }

    public override float Severity => ChromaticSensitivity.Settings.Severity;

    private Color? MoveTowardsColorFromFood(Thing food, Color startingColor, float amount)
    {
      var comp = food.TryGetComp<CompIngredients>();
      if (comp == null || (comp.ingredients?.Count ?? 0) <= 0)
      {
        return food.Stuff?.stuffProps?.color is Color stuffColor
          ? MoveColorsCloser(startingColor, stuffColor , amount)
          : MoveColorsCloser(startingColor, _colorHelper.ExtractDominantColor(food), amount);
      }

      var newCol = comp.ingredients.Aggregate(startingColor, ColorModifierFromThingDefWithAmount(amount));
      Log.Verbose($"New col {newCol.r} {newCol.g} {newCol.b}");
      return newCol;
    }

    private Func<Color, ThingDef, Color> ColorModifierFromThingDefWithAmount(float amount) => (color, ingredient) => MoveColorTowardsIngredientColor(color, ingredient, amount);

    private Color MoveColorTowardsIngredientColor(Color color, ThingDef ingredient, float amount)
    {
      return ingredient.stuffProps?.color is Color stuffColor
        ? MoveColorsCloser(color, stuffColor, amount)
        : MoveColorsCloser(color, _colorHelper.ExtractDominantColor(ingredient), amount);
    }
    
    #endregion Helpers
 
    public void FoodIngested(Thing food, CompProperties_ChromaticFood compProperties)
    {
      var defName = food.def.defName;
      if (ChromaticSensitivity.Settings.ExcludedDefs.Contains(defName))
      {
        pawn.needs.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("Taggerung_AteNonChromaticFoodChromavore"));
        return;
      }
      
      var startingColor = ChromaticSensitivity.SkinColorManager.GetSkinColor(pawn);
      if (startingColor == null)
      {
        Log.Verbose($"Unable to determine skin color for pawn of def {pawn.def.defName}");
        return;
      }

      var forcedColor = ChromaticSensitivity.Settings.ThingDefColors.TryGetValue(defName, out var defForcedColor)
        ? defForcedColor
        : compProperties.GetForcedColor();
      if (forcedColor == null && compProperties.chromaticColorType == ChromaticColorType.Random) forcedColor = ColorHelper.RandomColor;

      var chromaticIntensity = Mathf.Clamp01(compProperties.chromaticIntensity * Severity);

      var newColor = forcedColor.HasValue
        ? MoveColorsCloser(startingColor.Value, forcedColor.Value, chromaticIntensity)
        : MoveTowardsColorFromFood(food, startingColor.Value, chromaticIntensity) ?? startingColor.Value;
      
      if (newColor.Equals(startingColor.Value))
      {
        MaybeGainBoringChromaticFoodThought(newColor);
        return;
      }
      ChromaticSensitivity.SkinColorManager.SetSkinColor(pawn, newColor);
      _graphicHandler.RefreshPawnGraphics(pawn);
      Log.Verbose($"Color changed from ({startingColor}) to ({newColor})");
      if (!pawn.NonHumanlikeOrWildMan() && pawn.Awake() && newColor.b > 0.9 && startingColor.Value.b < newColor.b)
        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map,
          "TextMote_ChromaticSensitivity_FeelingBlue".Translate(), 6.5f);
      
      MaybeGainChromaticFoodThought(newColor);
    }

    private void MaybeGainChromaticFoodThought(Color color)
    {
      if (Rand.Chance(0.2f))
        pawn.needs.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named(ColorIsSimilarToFavourite(color)
          ? "Taggerung_AteExcitingChromaticFoodChromavore"
          : "Taggerung_AteFoodChromavore"));
    }
    
    /**
     * Chance for the pawn to get bored of food that is of a similar color to them.
     * Pawns will never get bored of food that is similar to their favourite color though.
     * Only applies if ideology is active as there is no chance for them to get excited without a favorite color. 
     */
    private void MaybeGainBoringChromaticFoodThought(Color color)
    {
      if (Rand.Chance(0.1f) && ModsConfig.IdeologyActive && !ColorIsSimilarToFavourite(color))
      {
        pawn.needs.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("Taggerung_AteBoringChromaticFoodChromavore"));
      }
    }

    private bool ColorIsSimilarToFavourite(Color color)
    {
      return pawn?.story?.favoriteColor is Color favoriteColor &&
             Mathf.Abs(color.r - favoriteColor.r) +
             Mathf.Abs(color.g - favoriteColor.g) +
             Mathf.Abs(color.b - favoriteColor.b) < 0.1;
    }
  }
}