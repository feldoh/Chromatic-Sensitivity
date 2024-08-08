using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  class BasicColorManager : IColorManager
  {
    private readonly IGraphicHandler _graphicHandler;

    public BasicColorManager(): this(null) {}
    public BasicColorManager(IGraphicHandler graphicHandler)
    {
      _graphicHandler = graphicHandler ?? ChromaticSensitivity.GraphicHandler;
    }
    
    public Color? GetSkinColor(Pawn pawn)
    {
      Log.Verbose($"basic skin color get");
      return pawn.story?.SkinColor;
    }

    public bool SetSkinColor(Pawn pawn, Color color)
    {
      Log.Verbose($"basic skin color set");
      if (pawn.story == null) return false;
      pawn.story.skinColorOverride = color;
      _graphicHandler.RefreshPawnGraphics(pawn);
      return true;
    }

    public Color? GetHairColor(Pawn pawn)
    {
      Log.Verbose($"basic hair color get");
      return pawn.story?.HairColor;
    }

    public bool SetHairColor(Pawn pawn, Color color)
    {
      Log.Verbose($"basic hair color set");
      if (pawn.story == null) return false;
      pawn.story.HairColor = color;
      _graphicHandler.RefreshPawnGraphics(pawn);
      return true;
    }
  }
}
