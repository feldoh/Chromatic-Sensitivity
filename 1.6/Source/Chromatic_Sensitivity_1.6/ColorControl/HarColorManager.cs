using System.Collections.Generic;
using AlienRace;
using UnityEngine;
using Verse;

namespace Chromatic_Sensitivity.ColorControl;

internal class HarColorManager : IColorManager
{
    private readonly IGraphicHandler _graphicHandler;

    public HarColorManager()
        : this(null) { }

    public HarColorManager(IGraphicHandler graphicHandler)
    {
        _graphicHandler = graphicHandler ?? ChromaticSensitivity.GraphicHandler;
    }

    public Color? GetSkinColor(Pawn pawn)
    {
        Log.Verbose($"Trying HAR skin color Get");
        Dictionary<string, AlienPartGenerator.ExposableValueTuple<Color, Color>> channels = pawn.TryGetComp<AlienPartGenerator.AlienComp>()?.ColorChannels;
        if (channels == null)
        {
            return null;
        }

        if (!channels.TryGetValue("skin", out AlienPartGenerator.ExposableValueTuple<Color, Color> skinColorChannels))
        {
            channels.TryGetValue("base", out skinColorChannels);
        }

        if (skinColorChannels == null)
        {
            return null;
        }

        Log.Verbose($"Found probable HAR skin color channel ({skinColorChannels.first.ToString()}, {skinColorChannels.second.ToString()})");
        return skinColorChannels.first;
    }

    public bool SetSkinColor(Pawn pawn, Color color)
    {
        Log.Verbose($"Trying HAR skin color Set");
        Dictionary<string, AlienPartGenerator.ExposableValueTuple<Color, Color>> channels = pawn.TryGetComp<AlienPartGenerator.AlienComp>()?.ColorChannels;
        if (channels == null)
        {
            return false;
        }

        string channelName = channels.ContainsKey("skin") ? "skin" : "base";
        if (!channels.ContainsKey(channelName))
        {
            return false;
        }

        channels[channelName].first = color;
        Log.Verbose($"Updating probable HAR skin color channel {channelName} to {color}");
        _graphicHandler.RefreshPawnGraphics(pawn);
        return true;
    }

    public Color? GetHairColor(Pawn pawn)
    {
        Log.Verbose($"Trying HAR hair color Get");
        Dictionary<string, AlienPartGenerator.ExposableValueTuple<Color, Color>> channels = pawn.TryGetComp<AlienPartGenerator.AlienComp>()?.ColorChannels;
        if (channels == null)
        {
            return null;
        }

        if (!channels.TryGetValue("hair", out AlienPartGenerator.ExposableValueTuple<Color, Color> hairColorChannels))
        {
            channels.TryGetValue("base", out hairColorChannels);
        }

        if (hairColorChannels == null)
        {
            return null;
        }

        Log.Verbose($"Found probable HAR hair color channel ({hairColorChannels.first.ToString()})");
        return hairColorChannels.first;
    }

    public bool SetHairColor(Pawn pawn, Color color)
    {
        Log.Verbose($"Trying HAR hair color Set");
        Dictionary<string, AlienPartGenerator.ExposableValueTuple<Color, Color>> channels = pawn.TryGetComp<AlienPartGenerator.AlienComp>()?.ColorChannels;
        if (channels == null)
        {
            return false;
        }

        string channelName = channels.ContainsKey("hair") ? "hair" : "base";
        if (!channels.ContainsKey(channelName))
        {
            return false;
        }

        channels[channelName].first = color;
        Log.Verbose($"Updating probable HAR hair color channel {channelName} to {color}");
        _graphicHandler.RefreshPawnGraphics(pawn);
        return true;
    }
}
