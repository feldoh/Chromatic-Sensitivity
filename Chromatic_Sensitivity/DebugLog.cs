using System.Diagnostics;

namespace Chromatic_Sensitivity
{
	internal static class Log
	{
		[Conditional("DEBUG")]
		public static void Message(string x)
		{
			Verse.Log.Message(x);
		}
	}
}
