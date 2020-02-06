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
using System.Threading;
using BlynkMqttBridge.Application;

namespace BlynkMqttBridge
{
	class Bridge
	{
		private ManualResetEvent bridgeStop = new ManualResetEvent(false);

		private BlynkLibrary.Blynk blynkConn = null;
		private MqttLibrary.MqttHandler mqttConn = null;

		private bool Debug = false;

		private List<string> PendingMqttTopics = new List<string>();
		private List<TopicEntry> TopicList = new List<TopicEntry>();

		public Bridge(List<TopicEntry> Topics, bool DisplayDebug = false)
		{
			TopicList = Topics;
			Debug = DisplayDebug;
		}

		public void SetupBlynk(string token, string server, string port)
		{
			if (blynkConn != null)
				blynkConn.stopConnection();

			int target_port = 8080;
			Int32.TryParse(port, out target_port);

			blynkConn = new BlynkLibrary.Blynk();
			blynkConn.setConnection(token, server, target_port);
			blynkConn.VirtualPinReceived += BlynkConn_VirtualPinReceived;
		}

		public void SetupMqtt(string user, string password, string server, string port)
		{
			if (mqttConn != null)
				mqttConn.stopConnection();

			mqttConn = new MqttLibrary.MqttHandler();
			mqttConn.setConnection(server, port, user, password);
			mqttConn.PublishReceivedEvent += MqttConn_PublishReceivedEvent;
			mqttConn.ConnectionChangeEvent += MqttConn_ConnectionChangeEvent;
		}

		private void MqttConn_ConnectionChangeEvent(bool Connected)
		{
			if (Connected)
			{
				PendingMqttTopics.Clear();

				string[] topics = new string[TopicList.Count];

				for (int i = 0; i < topics.Length; i++)
					topics[i] = TopicList[i].MqttTopic;

				mqttConn.Subscribe(topics);
			}
		}

		private void MqttConn_PublishReceivedEvent(string Topic, string Payload)
		{
			if (PendingMqttTopics.Contains(Topic))
			{
				PendingMqttTopics.Remove(Topic);
			}
			else
			{
				foreach (TopicEntry Entry in TopicList)
				{
					if (Entry.MqttTopic == Topic)
					{
						blynkConn.SendVirtualPin(Entry.BlynkVpin, Entry.Encoder.toBlynk(Payload));

						if (Debug)
							Helpers.Log(
								"T:" + Entry.MqttTopic + " -> VPIN:" + Entry.BlynkVpin + " value: " + Payload, 
								ConsoleColor.Yellow, "[mqtt->blynk]"
							);
						
						break;
					}
				}
			}
		}

		private void BlynkConn_VirtualPinReceived(BlynkLibrary.Blynk b, BlynkLibrary.VirtualPinEventArgs e)
		{
			foreach (TopicEntry Entry in TopicList)
			{
				if (Entry.BlynkVpin == e.Data.Pin)
				{
					string inValue = e.Data.Value[0].ToString();

					PendingMqttTopics.Add(Entry.MqttTopic);
					mqttConn.SendMessage(Entry.MqttTopic, Entry.Encoder.fromBlynk(inValue));

					if (Entry.BlynkAck)
						blynkConn.SendVirtualPin(e.Data.Pin, inValue);

					if (Debug)
						Helpers.Log(
							"VPIN:" + Entry.BlynkVpin + " -> T:" + Entry.MqttTopic + " value: " + inValue, 
							ConsoleColor.Magenta, "[blynk->mqtt]"
						);

					break;
				}
			}
		}

		public void WaitUntilStop()
		{
			bridgeStop.WaitOne();

			if (blynkConn != null)
				blynkConn.stopConnection();
			
			if (mqttConn != null)
				mqttConn.stopConnection();
		}

		public void Stop()
		{
			bridgeStop.Set();
		}
	}
}
