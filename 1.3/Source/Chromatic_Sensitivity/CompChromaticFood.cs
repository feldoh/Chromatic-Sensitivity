using Verse;

namespace Chromatic_Sensitivity
{
  public class CompChromaticFood : ThingComp
  {
    public CompProperties_ChromaticFood Props => (CompProperties_ChromaticFood) props;

    public override void PostIngested(Pawn ingester)
    {
      base.PostIngested(ingester);
      (ingester.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Taggerung_ChromaticSensitivity")) as
        Hediff_ChromaticSensitivity)?.FoodIngested(parent, Props.GetForcedColor());
    }
  }
}