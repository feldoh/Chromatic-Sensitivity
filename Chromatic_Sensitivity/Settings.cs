using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	public class Settings : ModSettings
	{
		//Use Mod.settings.setting to refer to this setting.
		public bool AllowInfection = true;

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);
			
			options.CheckboxLabeled("Allow Chromatic Sensitivity Infection", ref AllowInfection);
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref AllowInfection, "AllowInfection", false);
		}
	}
}