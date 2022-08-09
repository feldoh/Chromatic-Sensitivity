using RimWorld;
using Verse;

namespace Chromatic_Sensitivity
{
	public class CompProperties_ChromaticFood : CompProperties
	{
		public ColorDef forcedColorDef = null;
		public CompProperties_ChromaticFood() => compClass = typeof (CompChromaticFood);
	}
}
