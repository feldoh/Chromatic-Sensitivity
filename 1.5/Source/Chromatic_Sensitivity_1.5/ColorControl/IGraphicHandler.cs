using RimWorld;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  public interface IGraphicHandler
  {
    void RefreshPawnGraphics(Pawn pawn);
  }

  class DefaultGraphicHandler : IGraphicHandler
  {
    public void RefreshPawnGraphics(Pawn pawn)
    {
      pawn.Drawer.renderer.SetAllGraphicsDirty();
      PortraitsCache.SetDirty(pawn);
    }
  }
}
