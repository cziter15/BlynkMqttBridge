using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlynkMqttBridge.Application
{
	public class Helpers
	{
		public static void Log(string text, ConsoleColor color = ConsoleColor.White, string prefix = "")
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
