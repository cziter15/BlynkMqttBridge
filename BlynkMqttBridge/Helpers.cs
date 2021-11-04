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
			LogColor(color, prefix, level, (text, ConsoleColor.White));
		}

		public static void LogColor(ConsoleColor PrefixColor, string prefix, LogLevel level, params (string text, ConsoleColor color)[] Texts)
		{
			if (level <= LoggingLevel)
			{
				if (prefix.Length > 0)
					prefix = " " + prefix;

				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.Write("[" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + "]");

				Console.ForegroundColor = PrefixColor;
				Console.Write(prefix + " ");
				Console.ForegroundColor = ConsoleColor.White;

				foreach (var text in Texts)
				{
					Console.ForegroundColor = text.color;
					Console.Write(text.text);
				}

				Console.Write(Environment.NewLine);
			}
		}
	}
}
