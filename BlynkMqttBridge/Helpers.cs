using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlynkMqttBridge
{
	public class Helpers
	{
		public enum LogLevel
		{
			Always = 0,
			Debug = 1,
			Verbose = 2
		}

		public static LogLevel LoggingLevel { get; set; } = LogLevel.Always;

		public static void Log(string text, ConsoleColor color = ConsoleColor.White, string prefix = "", LogLevel level = LogLevel.Always)
		{
			if (level <= LoggingLevel)
			{
				if (prefix.Length > 0)
					prefix = " " + prefix;

				Console.ForegroundColor = color;
				Console.Write("[" + DateTime.Now.ToString("MM/dd/yyyy HH:mm") + "]" + prefix + " ");
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(text + Environment.NewLine);
			}
		}
	}
}
