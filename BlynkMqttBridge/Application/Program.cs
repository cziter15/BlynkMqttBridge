////////////////////////////////////////////////////////////////////////////
//
//  This file is part of Blynk-Mqtt bridge
//
//  Copyright (c) 2020, Krzysztof Strehlau
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of 
//  this software and associated documentation files (the "Software"), to deal in 
//  the Software without restriction, including without limitation the rights to use, 
//  copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
//  Software, and to permit persons to whom the Software is furnished to do so, 
//  subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all 
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//  INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//  PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//  HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//  SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using BlynkMqttBridge.Application;
using BlynkMqttBridge.IniLibrary;

namespace BlynkMqttBridge
{
	class Program
	{
		static void Main(string[] args)
		{
			Helpers.Log("----------------------------------------------", ConsoleColor.Green, "[program]");
			Helpers.Log("Strarting. Build: " + Properties.Resources.BuildDate.Trim(), ConsoleColor.Green, "[program]");
			Helpers.Log("----------------------------------------------", ConsoleColor.Green, "[program]");

			foreach (string arg in args)
			{
				if (arg == "-verbose")
				{
					Helpers.LoggingLevel = Helpers.LogLevel.Verbose;
					Helpers.Log("args: Verbose mode enabled.", ConsoleColor.Green, "[program]");
					break;
				}

				if (arg == "-dump")
				{
					Helpers.LoggingLevel = Helpers.LogLevel.Debug;
					Helpers.Log("args: Debug/dump mode enabled.", ConsoleColor.Green, "[program]");
					break;
				}
			}

			List<TopicEntry> Topics = new List<TopicEntry>();

			Ini config = new Ini("blynkmqttbridge.ini");

			foreach (string Section in config.GetSections())
			{
				if (Section.StartsWith("Topic"))
				{
					string s_topic = config.GetValue("mqtt_topic", Section, "");
					string s_topic_out = config.GetValue("mqtt_topic_out", Section, s_topic);
					string s_vpin = config.GetValue("blynk_vpin", Section, "");
					string s_type = config.GetValue("type", Section, "");

					if (s_topic.Length > 0 && s_vpin.Length > 0 && s_type.Length > 0)
					{
						int vpin = 0;
						if (Int32.TryParse(s_vpin, out vpin))
						{
							TypeEncoder._StraightType val_type = TypeEncoder.TypeFromName(s_type);
							if (val_type != null)
							{
								Topics.Add(new TopicEntry(s_topic, s_topic_out, vpin, val_type, s_topic_out.Length == 0));

								Helpers.Log(
									"+t " + s_topic + " vp:" + s_vpin + " t:" + s_type, 
									ConsoleColor.Cyan, 
									"[config]", 
									Helpers.LogLevel.Debug
								);
							}
							else
							{
								Helpers.Log("Invalid value type for topic: " + s_topic, ConsoleColor.Cyan, "[config]");
								return;
							}
						}
						else
						{
							Helpers.Log("Invalid vpin for topic " + s_topic, ConsoleColor.Cyan, "[config]");
							return;
						}
					}
					else
					{
						Helpers.Log("Missing values(s) for some Topic");
						return;
					}
				}
			}

			Bridge bridge = new Bridge(Topics);

			bridge.SetupBlynk(
				config.GetValue("token", "BlynkConnection", ""),
				config.GetValue("address", "BlynkConnection", ""),
				config.GetValue("port", "BlynkConnection", "8080")
			);

			bridge.SetupMqtt(
				config.GetValue("login", "MqttConnection", ""),
				config.GetValue("pass", "MqttConnection", ""),
				config.GetValue("address", "MqttConnection", ""),
				config.GetValue("port", "MqttConnection", "1883")
			);

			Console.CancelKeyPress += (sender, eventArgs) => {
				eventArgs.Cancel = true;
				bridge.Stop();
			};

			bridge.WaitUntilStop();
		}
	}
}
