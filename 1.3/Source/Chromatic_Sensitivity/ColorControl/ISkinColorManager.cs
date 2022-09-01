using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  static class SkinColorManagerFactory
  {
    private static readonly Lazy<ISkinColorManager> LazySkinColorManager =
      new Lazy<ISkinColorManager>(() =>
      {
        var skinColorManagers = new List<ISkinColorManager>();
        if (ChromaticSensitivity.AlienRacesEnabled) skinColorManagers.Add(new HARSkinColorManager());
        skinColorManagers.Add(new BasicSkinColorManager());
        return new CompoundSkinColorManager(skinColorManagers);
      });

    public static ISkinColorManager DefaultSkinColorManager => LazySkinColorManager.Value;
  }

  public interface ISkinColorManager
  {
    Color? GetSkinColor(Pawn pawn);
    bool SetSkinColor(Pawn pawn, Color color);
  }
}