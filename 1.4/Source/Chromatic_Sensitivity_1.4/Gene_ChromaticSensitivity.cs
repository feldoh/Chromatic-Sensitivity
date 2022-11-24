#nullable enable
using Chromatic_Sensitivity.ColorControl;
using Verse;

namespace Chromatic_Sensitivity;

public class Gene_ChromaticSensitivity : Gene
{
  public const int ColorShuffleInterval = GenTicks.TickLongInterval * 50;

  public override void PostRemove()
  {
    if (GetHediff() is { } firstHediffOfDef)
      pawn.health.RemoveHediff(firstHediffOfDef);
    base.PostRemove();
  }

  public override void PostAdd()
  {
    base.PostAdd();
    ApplyHediff();
    if (!(pawn.story?.SkinColorOverriden ?? false))
    {
      ChromaticSensitivity.SkinColorManager.SetSkinColor(pawn, ColorHelper.RandomColor);
    }
  }

  public override void Reset()
  {
    if (GetHediff() is { } firstHediffOfDef)
      firstHediffOfDef.Severity = ChromaticSensitivity.Settings.Severity;
  }

  public override void Tick()
  {
    base.Tick();
    if (!pawn.Spawned || !pawn.IsHashIntervalTick(GenTicks.TickLongInterval) || Rand.Chance(0.75f)) return;
    if (GetHediff() == null)
    {
      if (Rand.Chance(0.5f)) ApplyHediff();
    }
    else
    {
      pawn.story.HairColor = ColorHelper.RandomColor;
      ChromaticSensitivity.GraphicHandler.RefreshPawnGraphics(pawn);
    }
  }

  public Hediff? GetHediff() => pawn.health.hediffSet.GetFirstHediffOfDef(ChromaticDefOf.Taggerung_ChromaticSensitivity);

  public void ApplyHediff()
  {
    if (GetHediff() != null) return;
    Hediff chromaticSensitivity = HediffMaker.MakeHediff(ChromaticDefOf.Taggerung_ChromaticSensitivity, pawn);
    pawn.health.AddHediff(chromaticSensitivity);
  }
}
