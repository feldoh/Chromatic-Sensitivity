using Chromatic_Sensitivity;

namespace Chromatic_Sensitivity
{
  public enum ChromaticColorType
  {
    Dominant,
    Random,
    None 
  }
  
  public static class ChromaticColorTypeExtensions
  {
    public static bool IsRandom(this ChromaticColorType chromaticColorType, ChromaticColorType globalSetting = ChromaticColorType.None)
    {
      return chromaticColorType == ChromaticColorType.Random || globalSetting == ChromaticColorType.Random;
    }
  }
}
