using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  class NonHumanlikeColorManager : IColorManager
  {
    private readonly IGraphicHandler _graphicHandler;

    public NonHumanlikeColorManager(): this(null) {}
    public NonHumanlikeColorManager(IGraphicHandler graphicHandler)
    {
      _graphicHandler = graphicHandler ?? ChromaticSensitivity.GraphicHandler;
    }
    
    public Color? GetSkinColor(Pawn pawn)
    {
      Log.Verbose($"non humanlike skin color get, graphic is using shader {pawn.Drawer.renderer.BodyGraphic.Shader.name}");
      Color color = pawn.Drawer.renderer.BodyGraphic.Color;
      
      // White is the default so might just be lies but we let users decide
      return color == Color.white && !ChromaticSensitivity.Settings.AllowWhite ? null : color;
    }

    /**
     * We record the color in the ChromaticSensitivity hediff then simply refresh the graphics
     * non humanlikes are patched in HarmonyPatch_PawnGraphicSet to pull the color from this hediff
     *
     * TODO : This is very round the houses and a bit nasty, maybe the color override should be its own hediff or comp.
     *        Because having the hediff call this to set it's own color field then rely on a patch is very tightly coupled and nasty.
     */
    public bool SetSkinColor(Pawn pawn, Color color)
    {
      if (pawn.RaceProps.Humanlike
          || pawn.health.hediffSet.GetFirstHediffOfDef(ChromaticDefOf.Taggerung_ChromaticSensitivity) is not Hediff_ChromaticSensitivity hediff) return false;
      hediff.SkinColor = color;
      Log.Verbose("non humanlike skin color set");
      pawn.Drawer.renderer.renderTree.SetDirty();
      _graphicHandler.RefreshPawnGraphics(pawn);
      return true;
    }

    public Color? GetHairColor(Pawn pawn)
    {
      Log.Verbose($"non humanlike hair color get, graphic is using shader {ShaderUtility.GetSkinShader(pawn).name}");
      Color? color = pawn.story?.HairColor ?? pawn.DrawColor;
      
      // White is the default so might just be lies but we let users decide
      return color == Color.white && !ChromaticSensitivity.Settings.AllowWhite ? null : color;
    }

    public bool SetHairColor(Pawn pawn, Color color)
    {
      if (pawn.RaceProps.Humanlike
          || pawn.health.hediffSet.GetFirstHediffOfDef(ChromaticDefOf.Taggerung_ChromaticSensitivity) is not Hediff_ChromaticSensitivity hediff) return false;
      hediff.HairColor = color;
      Log.Verbose("non humanlike hair color set");
      pawn.Drawer.renderer.EnsureGraphicsInitialized();
      _graphicHandler.RefreshPawnGraphics(pawn);
      return true;
    }
  }
}
