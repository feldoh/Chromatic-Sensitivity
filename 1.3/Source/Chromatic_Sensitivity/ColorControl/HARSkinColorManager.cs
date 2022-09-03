using AlienRace;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl
{
  class HARSkinColorManager : ISkinColorManager
  {
    private readonly IGraphicHandler _graphicHandler;

    public HARSkinColorManager(): this(null) {}
    public HARSkinColorManager(IGraphicHandler graphicHandler)
    {
      _graphicHandler = graphicHandler ?? ChromaticSensitivity.GraphicHandler;
    }
    
    public Color? GetSkinColor(Pawn pawn)
    {
      Log.Verbose($"Trying HAR skin color Get");
      var channels = pawn.TryGetComp<AlienPartGenerator.AlienComp>()?.ColorChannels;
      if (channels == null) return null;
      if (!channels.TryGetValue("skin", out var skinColorChannels)) channels.TryGetValue("base", out skinColorChannels);
      if (skinColorChannels == null) return null;
      Log.Verbose($"Found probable HAR skin color channel ({skinColorChannels.first.ToString()}, {skinColorChannels.second.ToString()})");
      return skinColorChannels.first;
    }

    public bool SetSkinColor(Pawn pawn, Color color)
    {
      Log.Verbose($"Trying HAR skin color Set");
      var channels = pawn.TryGetComp<AlienPartGenerator.AlienComp>()?.ColorChannels;
      if (channels == null) return false;
      var channelName = channels.ContainsKey("skin") ? "skin" : "base";
      if (!channels.ContainsKey(channelName)) return false;
      channels[channelName].first = color;
      Log.Verbose($"Updating probable HAR skin color channel {channelName} to {color}");
      _graphicHandler.RefreshPawnGraphics(pawn);
      return true;
    }
  }
}