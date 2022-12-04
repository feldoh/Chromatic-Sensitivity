using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  class CompoundColorManager : IColorManager
  {
    private readonly List<IColorManager> _subManagers;

    public CompoundColorManager(List<IColorManager> subManagers)
    {
      _subManagers = subManagers;
    }

    public Color? GetSkinColor(Pawn pawn)
    {
      return _subManagers.Select(manager => manager.GetSkinColor(pawn))
        .Where(color => color.HasValue)
        .FirstOrFallback();
    }

    public bool SetSkinColor(Pawn pawn, Color color)
    {
      return _subManagers.Where(m => m.SetSkinColor(pawn, color)).FirstOrFallback() != null;
    }

    public Color? GetHairColor(Pawn pawn)
    {
      return _subManagers.Select(manager => manager.GetHairColor(pawn))
              .Where(color => color.HasValue)
              .FirstOrFallback();
    }

    public bool SetHairColor(Pawn pawn, Color color)
    {
      return _subManagers.Where(m => m.SetHairColor(pawn, color)).FirstOrFallback() != null;
    }
  }
}
