using System.Linq;
using System.Reflection;
using HarmonyLib;
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

#if DEBUG
			Harmony.DEBUG = true;
#endif

			Harmony harmony = new Harmony("Taggerung.rimworld.Chromatic_Sensitivity.main");
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			var c = new ColorExtractor();
			foreach (var ingestible in DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsIngestible))
			{
				if (!ingestible.HasComp(typeof(CompChromaticFood)))
				{
					ingestible.comps.Add(new CompProperties_ChromaticFood());
				}
#if DEBUG
				var dominantColor = c.ExtractDominantColor(ingestible);
				if (dominantColor is Color color)
				{
					var texture2D = (Texture2D)ingestible.graphic.MatSingle.mainTexture;
					TextureAtlasHelper.WriteDebugPNG(texture2D,
						$"D:\\Modding\\{ingestible.defName}.png");

					RenderTexture temporary = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
					temporary.name = "MakeReadableTexture_Temp";
					Graphics.Blit((Texture) texture2D, temporary);
					RenderTexture active = RenderTexture.active;
					RenderTexture.active = temporary;
					Texture2D targetTexture = new Texture2D(texture2D.width, texture2D.height);
					targetTexture.ReadPixels(new Rect(0.0f, 0.0f, (float) temporary.width, (float) temporary.height), 0, 0);
					for (int y = 0; y < texture2D.height; y++)
					{
						for (int x = 0; x < texture2D.width; x++)
						{
							targetTexture.SetPixel(x, y, color);
						}
					}
					targetTexture.Apply();
					RenderTexture.active = active;
					RenderTexture.ReleaseTemporary(temporary);
					TextureAtlasHelper.WriteDebugPNG(targetTexture,
						$"D:\\Modding\\{ingestible.defName}_Color.png");
				}
#endif
			}
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