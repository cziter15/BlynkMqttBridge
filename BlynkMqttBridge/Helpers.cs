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

		public static void SetConsoleColor(string color)
		{
			switch (color)
			{
				case "black": Console.ForegroundColor = ConsoleColor.Black; break;
				case "darkblue": Console.ForegroundColor = ConsoleColor.DarkBlue; break;
				case "darkgreen": Console.ForegroundColor = ConsoleColor.DarkGreen; break;
				case "darkcyan": Console.ForegroundColor = ConsoleColor.DarkCyan; break;
				case "darkred": Console.ForegroundColor = ConsoleColor.DarkRed; break;
				case "darkmagenta": Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
				case "darkyellow": Console.ForegroundColor = ConsoleColor.DarkYellow; break;
				case "gray": Console.ForegroundColor = ConsoleColor.Gray; break;
				case "darkgray": Console.ForegroundColor = ConsoleColor.DarkGray; break;
				case "blue": Console.ForegroundColor = ConsoleColor.Blue; break;
				case "green": Console.ForegroundColor = ConsoleColor.Green; break;
				case "cyan": Console.ForegroundColor = ConsoleColor.Cyan; break;
				case "red": Console.ForegroundColor = ConsoleColor.Red; break;
				case "magenta": Console.ForegroundColor = ConsoleColor.Magenta; break;
				case "yellow": Console.ForegroundColor = ConsoleColor.Yellow; break;
				case "white": Console.ForegroundColor = ConsoleColor.White; break;
			}
		}

		public static void Log(string text, ConsoleColor color = ConsoleColor.White, string prefix = "", LogLevel level = LogLevel.Always)
		{
			if (level <= LoggingLevel)
			{
				if (prefix.Length > 0)
					prefix = " " + prefix;

				Console.ForegroundColor = color;
				Console.Write("[" + DateTime.Now.ToString("MM/dd/yyyy HH:mm") + "]" + prefix + " ");
				Console.ForegroundColor = ConsoleColor.White;

				int lastMarker = 0;

				while (true)
				{
					int colorMarker = text.IndexOf("[[c=", lastMarker);
					if (colorMarker == -1) break;

					Console.Write(text.Substring(lastMarker, colorMarker - lastMarker));

					int colorMarkerEnd = colorMarker > 0 ? text.IndexOf("]]", colorMarker) : -1;

					if (colorMarkerEnd != -1)
					{
						SetConsoleColor(text.Substring(colorMarker + 4, colorMarkerEnd - colorMarker - 4));
						lastMarker = colorMarkerEnd + 2;
					}
					else
					{
						lastMarker = text.Length;
					}
				}

				// Print tail
				if (lastMarker < text.Length)
					Console.Write(text.Substring(lastMarker, text.Length - lastMarker));

				Console.Write(Environment.NewLine);
			}
		}
	}
}
