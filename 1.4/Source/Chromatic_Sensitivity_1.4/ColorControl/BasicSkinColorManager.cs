using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  class BasicSkinColorManager : ISkinColorManager
  {
    private readonly IGraphicHandler _graphicHandler;

    public BasicSkinColorManager(): this(null) {}
    public BasicSkinColorManager(IGraphicHandler graphicHandler)
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
  }
}