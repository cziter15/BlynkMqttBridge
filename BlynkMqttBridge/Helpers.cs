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
			LogColor(color, prefix, level, new[] { ConsoleColor.White }, new[] { text });
		}

		public static void LogColor(ConsoleColor PrefixColor, string prefix, LogLevel level, ConsoleColor[] Colors, params string[] Texts)
		{
			if (level <= LoggingLevel)
			{
				if (prefix.Length > 0)
					prefix = " " + prefix;

				Console.ForegroundColor = PrefixColor;
				Console.Write("[" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + "]" + prefix + " ");
				Console.ForegroundColor = ConsoleColor.White;
				
				for (int i = 0; i < Texts.Length; i++)
				{
					if (i < Colors.Length) Console.ForegroundColor = Colors[i];
					Console.Write(Texts[i]);
				}

				Console.Write(Environment.NewLine);
			}
		}
	}
}
