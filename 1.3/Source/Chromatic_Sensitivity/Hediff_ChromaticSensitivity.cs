using System.Linq;
using Chromatic_Sensitivity.ColorControl;
using RimWorld;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
  public class Hediff_ChromaticSensitivity : HediffWithComps
  {
    private readonly ColorExtractor _colorExtractor;

    #region Properties

    public HediffDef_ChromaticSensitivity Def => def as HediffDef_ChromaticSensitivity;

    #endregion Properties

    public Hediff_ChromaticSensitivity() : this(new ColorExtractor()) { }

    public Hediff_ChromaticSensitivity(ColorExtractor colorExtractor)
    {
      _colorExtractor = colorExtractor;
    }

    #region Helpers

    private Color MoveColorsCloser(Color currentColor, Color targetColor)
    {
      return Color.Lerp(currentColor, targetColor, Severity);
    }

    public override float Severity => ChromaticSensitivity.Settings.Severity;

    private Color? MoveTowardsColorFromFood(Thing food, Color startingColor)
    {
      var comp = food.TryGetComp<CompIngredients>();
      if (comp == null || (comp.ingredients?.Count ?? 0) <= 0)
      {
        return food.Stuff?.stuffProps?.color is Color stuffColor
          ? MoveColorsCloser(startingColor, stuffColor)
          : MoveColorsCloser(startingColor,
            _colorExtractor.ExtractDominantColor(food) ?? startingColor);
      }

      var newCol = comp.ingredients.Aggregate(startingColor, MoveColorTowardsIngredientColor);
      Log.Verbose($"New col {newCol.r} {newCol.g} {newCol.b}");
      return newCol;
    }

    private Color MoveColorTowardsIngredientColor(Color color, ThingDef ingredient)
    {
      return ingredient.stuffProps?.color is Color stuffColor
        ? MoveColorsCloser(color, stuffColor)
        : _colorExtractor.ExtractDominantColor(ingredient) is Color newColor
          ? MoveColorsCloser(color, newColor)
          : color;
    }
    
    #endregion Helpers
 
    public void FoodIngested(Thing food, Color? compForcedColor)
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
        : compForcedColor;
      
      var newColor = forcedColor.HasValue
        ? MoveColorsCloser(startingColor.Value, forcedColor.Value)
        : MoveTowardsColorFromFood(food, startingColor.Value) ?? startingColor.Value;
      
      if (newColor.Equals(startingColor.Value))
      {
        MaybeGainBoringChromaticFoodThought(newColor);
        return;
      }
      ChromaticSensitivity.SkinColorManager.SetSkinColor(pawn, newColor);
      
      Log.Verbose($"Color changed from ({startingColor}) to ({newColor})");
      pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
      PortraitsCache.SetDirty(pawn);
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