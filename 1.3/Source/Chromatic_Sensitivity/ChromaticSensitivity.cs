using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity
{
	public class ChromaticSensitivity : Mod
	{
		public static ChromaticSensitivitySettings Settings;

		public ChromaticSensitivity(ModContentPack content) : base(content)
		{
			Log.Verbose("Feel the rainbow");

			// initialize settings
			Settings = GetSettings<ChromaticSensitivitySettings>();
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