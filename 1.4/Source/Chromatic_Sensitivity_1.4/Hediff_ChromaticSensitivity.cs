using System;
using System.Collections.Generic;
using System.Linq;
using Chromatic_Sensitivity.ColorControl;
using JetBrains.Annotations;
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
        public Color? SkinColor;
        public Color? OriginalHairColor;
        public Color? HairColor;

        #endregion Properties

        [UsedImplicitly]
        public Hediff_ChromaticSensitivity() : this(null, null)
        {
        }

        public Hediff_ChromaticSensitivity(ColorHelper colorHelper, IGraphicHandler graphicHandler)
        {
            _colorHelper = colorHelper ?? ChromaticSensitivity.ColorHelper;
            _graphicHandler = graphicHandler ?? ChromaticSensitivity.GraphicHandler;
        }

        public override void PostAdd(DamageInfo? damageInfo)
        {
            base.PostAdd(damageInfo);
            OriginalColor = ChromaticSensitivity.ColorManager.GetSkinColor(pawn);
            Log.Verbose($"Saved pawn base color {OriginalColor}");
            OriginalHairColor = ChromaticSensitivity.ColorManager.GetHairColor(pawn);
            Log.Verbose($"Saved pawn base hair color {OriginalHairColor}");
        }

        public override void PostRemoved()
        {
            Color restoredSkinColor = OriginalColor ?? pawn.story.SkinColorBase;
            Log.Verbose(
                $"Restoring pawn base color to {restoredSkinColor} (Original: {OriginalColor}) from {ChromaticSensitivity.ColorManager.GetSkinColor(pawn)}");
            ChromaticSensitivity.ColorManager.SetSkinColor(pawn, restoredSkinColor);

            if (OriginalHairColor is { } restoredHairColor)
            {
                Log.Verbose(
                    $"Restoring pawn base color to {restoredHairColor} (Original: {OriginalHairColor}) from {ChromaticSensitivity.ColorManager.GetSkinColor(pawn)}");
                ChromaticSensitivity.ColorManager.SetHairColor(pawn, restoredHairColor);
            }

            base.PostRemoved();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref OriginalColor, "OriginalColor");
            Scribe_Values.Look(ref SkinColor, "SkinColor");
            Scribe_Values.Look(ref OriginalColor, "OriginalHairColor");
            Scribe_Values.Look(ref HairColor, "HairColor");
        }

        public override void Tick()
        {
            base.Tick();
            if (!pawn.Spawned || !pawn.IsHashIntervalTick(GenTicks.TickLongInterval) || Rand.Chance(0.2f) ||
                !ChromaticSensitivity.Settings.AnyPeriodicEffect()) return;
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Color? dominantSurroundingColor = DominantSurroundingColor(pawn.Position, pawn.Map);

                ApplySurroundingEffect(dominantSurroundingColor);
                ApplyPeriodicEffect(ColorChangeTarget.Skin, dominantSurroundingColor);
                ApplyPeriodicEffect(ColorChangeTarget.Hair, dominantSurroundingColor);
            });
        }

        public void ApplyPeriodicEffect(ColorChangeTarget target, Color? dominantSurroundingColor)
        {
            if (target.GetColor(ChromaticSensitivity.ColorManager, pawn) is not { } currentColor) return;
            Log.Verbose($"Applying Periodic Effect {target.PeriodicChromaticColorType()} to {target}");
            if (dominantSurroundingColor.HasValue) MaybeGainFavouriteSurroundingColorThought(dominantSurroundingColor.Value);
            switch (target.PeriodicChromaticColorType())
            {
                case ChromaticColorType.Dominant when dominantSurroundingColor.HasValue:
                    target.SetColor(ChromaticSensitivity.ColorManager, pawn,
                        MoveColorsCloser(currentColor, dominantSurroundingColor.Value, ChromaticSensitivity.Settings.Severity));
                    break;
                case ChromaticColorType.Random:
                    target.SetColor(ChromaticSensitivity.ColorManager, pawn, ColorHelper.RandomColor);
                    break;
                case ChromaticColorType.None:
                default:
                    break;
            }
        }

        public override float Severity => ChromaticSensitivity.Settings.Severity;

        #region Helpers

        public void ApplySurroundingEffectFromDef(HediffDef hediffDef)
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) is { } hediff)
            {
                hediff.Severity += 1;
            }
            else
            {
                Hediff chromaticSensitivity = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(chromaticSensitivity);
            }
        }

        /**
     * Pick a hediff based on the dominant color
     * If the dominant color component isn't dominant by at least 30% then don't apply any hediff
     */
        public void ApplySurroundingEffect(Color? dominantColor)
        {
            if (!pawn.Awake()
                || dominantColor is not { } safeDominantColor
                || (safeDominantColor.r >= 0.745 && safeDominantColor.g >= 0.745 && safeDominantColor.b >= 0.745) // Too pale
                || (safeDominantColor.r < 0.098 && safeDominantColor.g < 0.098 && safeDominantColor.b < 0.098)) // Too dark
                return;
            Color.RGBToHSV(safeDominantColor, out var hue, out var saturation, out var lightness);
            if ((hue < 0.013 || (hue < 0.041 && saturation > 0.85) || hue > 0.941) && safeDominantColor.g < 0.27 &&
                safeDominantColor.b < 0.27 && safeDominantColor.r > (safeDominantColor.b + safeDominantColor.g))
                ApplySurroundingEffectFromDef(ChromaticDefOf.Taggerung_ChromaticSurroundings_Red);
            else if (hue > 0.180 && hue <= 0.472 && (safeDominantColor.g > 1.20 * safeDominantColor.b ||
                                                     safeDominantColor.g > 1.20 * safeDominantColor.r))
                ApplySurroundingEffectFromDef(ChromaticDefOf.Taggerung_ChromaticSurroundings_Green);
            else if (hue > 0.472 && hue < 0.736 && (safeDominantColor.b > 1.20 * safeDominantColor.g ||
                                                    safeDominantColor.b > 1.20 * safeDominantColor.r))
                ApplySurroundingEffectFromDef(ChromaticDefOf.Taggerung_ChromaticSurroundings_Blue);
        }

        public Color? DominantSurroundingColor(
            IntVec3 rootCell,
            Map map)
        {
            Dictionary<Color, int> surroundingColors = new();
            Dictionary<Color, HashSet<string>> thingsOfColor = new();
            Color? mostCommonColor = null;
            int mostCommonColorCount = 0;
            int num = GenRadial.NumCellsInRadius(11.9f);

            var listColorOrigin = ChromaticSensitivity.Settings.VerboseLogging;

            void RecordThingColor(Color color, string name)
            {
                if (!listColorOrigin) return;
                if (!thingsOfColor.ContainsKey(color)) thingsOfColor.SetOrAdd(color, new HashSet<string>());
                thingsOfColor[color].Add(name);
            }

            bool UpdateColorCommonality(Color color, int increment = 1)
            {
                if (color is { r: 1.0f, g: 1.0f, b: 1.0f } or { r: 0.0f, g: 0.0f, b: 0.0f }) return false;
                var colorCount = surroundingColors.TryGetValue(color) + increment;
                if (colorCount > mostCommonColorCount)
                {
                    mostCommonColorCount = colorCount;
                    mostCommonColor = color;
                }

                surroundingColors.SetOrAdd(color, colorCount);
                return true;
            }

            for (var cellIndex = 0; cellIndex < num; ++cellIndex)
            {
                IntVec3 intVec3 = rootCell + GenRadial.RadialPattern[cellIndex];
                if (!intVec3.InBounds(map) || intVec3.Fogged(map) || !GenSight.LineOfSight(rootCell, intVec3, map)) continue;
                foreach (Thing thing in intVec3.GetThingList(map))
                {
                    if (thing.def.defName.Contains("Blood") && thing is not Building && UpdateColorCommonality(thing.DrawColor))
                    {
                        RecordThingColor(thing.DrawColor, thing.def.defName);
                    }
                    else if (thing is Plant { LeaflessNow: false } &&
                             _colorHelper.ExtractDominantColor(thing) is { } plantColor && UpdateColorCommonality(plantColor))
                    {
                        RecordThingColor(plantColor, thing.def.defName);
                    }

                    if (thing is not Building { DrawColor: var drawColor, def: var thingDef }) continue;
                    if (thing.TryGetComp<CompGlower>() is { Glows: true } compGlower)
                    {
                        Color glowColor = compGlower.GlowColor.ToColor;
                        if (UpdateColorCommonality(glowColor, Math.Min(5, (int)compGlower.Props.glowRadius / 2)))
                        {
                            RecordThingColor(glowColor, thingDef.defName + " - Light");
                        }
                    }

                    if (!UpdateColorCommonality(drawColor)) continue;
                    RecordThingColor(drawColor, thingDef.defName);
                }

                if (!ChromaticSensitivity.Settings.ConsiderFloors) continue;
                Color floorColor = map.terrainGrid.ColorAt(intVec3)?.color ?? intVec3.GetTerrain(map).DrawColor;
                if (!UpdateColorCommonality(floorColor)) continue;
                RecordThingColor(floorColor, "_Floor");
            }
#if DEBUG
  foreach ((Color color, var cnt) in surroundingColors)
  {
    Log.Verbose($"Surrounding color {ColorUtility.ToHtmlStringRGB(color)} => {cnt}");
  }
#endif
            Log.Verbose(
                $"Most common surrounding color ({mostCommonColorCount}) => {(mostCommonColor == null ? "N/A" : ColorUtility.ToHtmlStringRGB(mostCommonColor.Value))}\nRelevant things:{thingsOfColor.TryGetValue(mostCommonColor ?? new Color())?.ToLineList() ?? "N/A"}");
            return mostCommonColor;
        }

        private Color MoveColorsCloser(Color currentColor, Color? maybeTargetColor, float amount)
        {
            return maybeTargetColor is { } targetColor ? Color.Lerp(currentColor, targetColor, amount) : currentColor;
        }

        private Color? MoveTowardsColorFromFood(Thing food, Color startingColor, float amount)
        {
            CompIngredients comp = food.TryGetComp<CompIngredients>();
            if (comp == null || (comp.ingredients?.Count ?? 0) <= 0)
            {
                return food.Stuff?.stuffProps?.color is { } stuffColor
                    ? MoveColorsCloser(startingColor, stuffColor, amount)
                    : MoveColorsCloser(startingColor, _colorHelper.ExtractDominantColor(food), amount);
            }

            Color newCol = comp.ingredients.Aggregate(startingColor, ColorModifierFromThingDefWithAmount(amount));
            Log.Verbose($"New col {newCol.r} {newCol.g} {newCol.b}");
            return newCol;
        }

        private Func<Color, ThingDef, Color> ColorModifierFromThingDefWithAmount(float amount) => (color, ingredient) =>
            MoveColorTowardsIngredientColor(color, ingredient, amount);

        private Color MoveColorTowardsIngredientColor(Color color, ThingDef ingredient, float amount)
        {
            return ingredient.stuffProps?.color is { } stuffColor
                ? MoveColorsCloser(color, stuffColor, amount)
                : MoveColorsCloser(color, _colorHelper.ExtractDominantColor(ingredient), amount);
        }

        #endregion Helpers

        public bool ApplyColor(ColorChangeTarget target, Color startingColor, Color newColor, bool applyThought)
        {
            target.SetColor(ChromaticSensitivity.ColorManager, pawn, newColor);
            Log.Verbose($"{target.ToString()} Color changed from ({startingColor}) to ({newColor})");
            switch (target)
            {
                case ColorChangeTarget.Skin:
                    SkinColor = newColor;
                    if (!pawn.NonHumanlikeOrWildMan() && pawn.Awake() && newColor.b > 0.9 && startingColor.b < newColor.b)
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map,
                            "TextMote_ChromaticSensitivity_FeelingBlue".Translate(), 6.5f);
                    break;
                case ColorChangeTarget.Hair:
                    HairColor = newColor;
                    break;
                default:
                    break;
            }

            return applyThought && MaybeGainChromaticFoodThought(newColor);
        }

        public void FoodIngested(Thing food, CompProperties_ChromaticFood compProperties)
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                var defName = food.def.defName;
                if (ChromaticSensitivity.Settings.ExcludedDefs.Contains(defName))
                {
                    pawn.needs.mood?.thoughts?.memories?.TryGainMemory(ChromaticDefOf.Taggerung_AteNonChromaticFoodChromavore);
                    return;
                }

                if (!ChromaticSensitivity.Settings.AnyIngestionEffect()) return;

                var chromaticIntensity = Mathf.Clamp01(compProperties.chromaticIntensity * Severity);
                var forcedColor = ChromaticSensitivity.Settings.ThingDefColors.TryGetValue(defName, out Color defForcedColor)
                    ? defForcedColor
                    : compProperties.GetForcedColor();

                var gainedThought = ApplyIngestionColorChange(ColorChangeTarget.Skin, false, forcedColor,
                    compProperties.chromaticColorType,
                    chromaticIntensity, food);
                ApplyIngestionColorChange(ColorChangeTarget.Hair, gainedThought, forcedColor, compProperties.chromaticColorType,
                    chromaticIntensity, food);

                _graphicHandler.RefreshPawnGraphics(pawn);
            });
        }

        public bool ApplyIngestionColorChange(ColorChangeTarget target, bool alreadyGainedThought, Color? forcedColor,
            ChromaticColorType compEffect, float chromaticIntensity, Thing thingIngested)
        {
            ChromaticColorType globalSetting = target.IngestionChromaticColorType();

            if (globalSetting == ChromaticColorType.None ||
                target.GetColor(ChromaticSensitivity.ColorManager, pawn) is not { } startingColor)
                return alreadyGainedThought;
            Color newColor = (compEffect.IsRandom(globalSetting) ? ColorHelper.RandomColor : forcedColor) is { } finalColor
                ? MoveColorsCloser(startingColor, finalColor, chromaticIntensity)
                : MoveTowardsColorFromFood(thingIngested, startingColor, chromaticIntensity) ?? startingColor;

            return newColor.IndistinguishableFrom(startingColor)
                ? alreadyGainedThought || MaybeGainBoringChromaticFoodThought(newColor)
                : ApplyColor(target, startingColor, newColor, !alreadyGainedThought);
        }

        private bool MaybeGainChromaticFoodThought(Color color)
        {
            if (!Rand.Chance(0.2f)) return false;
            pawn.needs.mood?.thoughts?.memories?.TryGainMemory(ColorIsSimilarToFavourite(color)
                ? ChromaticDefOf.Taggerung_AteExcitingChromaticFoodChromavore
                : ChromaticDefOf.Taggerung_AteFoodChromavore);
            return true;
        }

        private bool MaybeGainFavouriteSurroundingColorThought(Color color)
        {
            if (!ColorIsSimilarToFavourite(color)) return false;
            pawn.needs.mood?.thoughts?.memories?.TryGainMemory(ChromaticDefOf.Taggerung_FavoriteChromaticSurroundings);
            return true;
        }

        public void MaybeGainThoughtIfColourAroundPointIsFavorite(IntVec3 originPoint, Map map, ThoughtDef thoughtDef)
        {
            if (map is null || thoughtDef is null) return;
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                var maybeDominantSurroundingColor = DominantSurroundingColor(originPoint, map);
                if (maybeDominantSurroundingColor is not { } dominantSurroundingColor || !ColorIsSimilarToFavourite(dominantSurroundingColor)) return;
                pawn.needs.mood?.thoughts?.memories?.TryGainMemory(thoughtDef);
            });
        }

        public void MaybeGainThoughtIfColourOfThingIsFavorite(Thing thing, ThoughtDef thoughtDef)
        {
            if (!(ColorIsSimilarToFavourite(thing.DrawColor) || ColorIsSimilarToFavourite(thing.DrawColorTwo))) return;
            pawn.needs.mood?.thoughts?.memories?.TryGainMemory(thoughtDef);
        }

        /**
     * Chance for the pawn to get bored of food that is of a similar color to them.
     * Pawns will never get bored of food that is similar to their favourite color though.
     * Only applies if ideology is active as there is no chance for them to get excited without a favorite color.
     */
        private bool MaybeGainBoringChromaticFoodThought(Color color)
        {
            if (!Rand.Chance(0.1f) || !ModsConfig.IdeologyActive || ColorIsSimilarToFavourite(color)) return false;
            pawn.needs.mood?.thoughts?.memories?.TryGainMemory(ChromaticDefOf.Taggerung_AteBoringChromaticFoodChromavore);
            return true;
        }

        [TweakValue("Taggerung_ChromaticSensitivity", 0.0f, 1.0f)]
        private static float favoriteColourSimilarityLeeway = 0.1f;

        private bool ColorIsSimilarToFavourite(Color color)
        {
            return pawn?.story?.favoriteColor is { } favoriteColor &&
                   Mathf.Abs(color.r - favoriteColor.r) +
                   Mathf.Abs(color.g - favoriteColor.g) +
                   Mathf.Abs(color.b - favoriteColor.b) < favoriteColourSimilarityLeeway;
        }
    }
}
