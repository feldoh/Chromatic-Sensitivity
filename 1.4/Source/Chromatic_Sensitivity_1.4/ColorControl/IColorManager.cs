using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  static class ColorManagerFactory
  {
    private static readonly Lazy<IColorManager> LazyColorManager =
      new(() =>
      {
        var skinColorManagers = new List<IColorManager>();
        if (ChromaticSensitivity.AlienRacesEnabled) skinColorManagers.Add(new HarColorManager());
        skinColorManagers.Add(new BasicColorManager());
        skinColorManagers.Add(new NonHumanlikeColorManager());
        return new CompoundColorManager(skinColorManagers);
      });

    public static IColorManager DefaultColorManager => LazyColorManager.Value;
  }

  public interface IColorManager
  {
    Color? GetSkinColor(Pawn pawn);
    bool SetSkinColor(Pawn pawn, Color color);
    
    Color? GetHairColor(Pawn pawn);
    bool SetHairColor(Pawn pawn, Color color);
  }
}
