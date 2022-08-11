using System.Diagnostics;

namespace Chromatic_Sensitivity
{
	internal static class Log
	{
		[Conditional("DEBUG")]
		public static void Debug(string x)
		{
			Verse.Log.Message(x);
		}
		
		public static void Verbose(string x)
		{
			if (ChromaticSensitivity.Settings?.VerboseLogging == true)
			{
				Verse.Log.Message(x);
			}
			else
			{
				Debug(x);
			}
		}
	}
}
