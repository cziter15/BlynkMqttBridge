﻿////////////////////////////////////////////////////////////////////////////
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

		private List<string> PendingMqttTopics = new List<string>();
		private List<TopicEntry> TopicList = new List<TopicEntry>();

		public Bridge(List<TopicEntry> Topics)
		{
			TopicList = Topics;
		}

		public void SetupBlynk(string token, string server, string port)
		{
			if (blynkConn != null)
				blynkConn.stopConnection();

			blynkConn = new BlynkLibrary.Blynk();
			blynkConn.VirtualPinReceived += BlynkConn_VirtualPinReceived;
			blynkConn.setConnection(token, server, port);
		}

		public void SetupMqtt(string user, string password, string server, string port)
		{
			if (mqttConn != null)
				mqttConn.stopConnection();

			mqttConn = new MqttLibrary.MqttHandler();
			mqttConn.PublishReceivedEvent += MqttConn_PublishReceivedEvent;
			mqttConn.ConnectionChangeEvent += MqttConn_ConnectionChangeEvent;
			mqttConn.setConnection(server, port, user, password);
		}

		private void MqttConn_ConnectionChangeEvent(bool Connected)
		{
			if (Connected)
			{
				PendingMqttTopics.Clear();

				string[] topics = new string[TopicList.Count];

				for (int i = 0; i < topics.Length; i++)
					topics[i] = TopicList[i].InTopic;

				mqttConn.Subscribe(topics);
			}
		}

		private void MqttConn_PublishReceivedEvent(string Topic, string Payload)
		{
			if (PendingMqttTopics.Contains(Topic))
			{
				PendingMqttTopics.Remove(Topic);
			}
			else if (blynkConn.Connected)
			{
				foreach (TopicEntry Entry in TopicList)
				{
					if (Entry.InTopic == Topic)
					{
						string encoded = Entry.Encoder.toBlynk(Entry, Payload);

						bool skipSource = !Entry.Encoder.getPrintSourceValue();
						bool skipTarget = !Entry.Encoder.getPrintTargetValue();

						blynkConn.SendVirtualPin(Entry.BlynkVpin, encoded);

						Helpers.LogColor(ConsoleColor.Yellow, "[mqtt->blynk]", Helpers.LogLevel.Debug,
							("Using [", ConsoleColor.White),
							(Entry.Encoder.GetType().Name, ConsoleColor.Red),
							("] from MqttTopic ", ConsoleColor.White),
							(Entry.InTopic, ConsoleColor.Cyan),
							(skipSource ? String.Empty : " -> value: ", ConsoleColor.White),
							(skipSource ? String.Empty : Payload.Replace("\n", ""), ConsoleColor.Yellow),
							(" => ", ConsoleColor.Red),
							("to BlynkVPin ", ConsoleColor.White),
							(skipTarget ? String.Empty : Entry.BlynkVpin.ToString(), ConsoleColor.Green),
							(skipTarget ? String.Empty : " -> value: ", ConsoleColor.White),
							(encoded.Replace("\n", ""), ConsoleColor.Yellow)
						);

						break;
					}
				}
			}
		}

		private void BlynkConn_VirtualPinReceived(BlynkLibrary.Blynk b, BlynkLibrary.VirtualPinEventArgs e)
		{
			if (mqttConn.IsConnected())
			{
				foreach (TopicEntry Entry in TopicList)
				{
					if (Entry.BlynkVpin == e.Data.Pin)
					{
						string inValue = e.Data.Value[0].ToString();
						string encoded = Entry.Encoder.fromBlynk(Entry, inValue);

						bool skipSource = !Entry.Encoder.getPrintSourceValue();
						bool skipTarget = !Entry.Encoder.getPrintTargetValue();

						string BlynkOutTopic = Entry.OutTopic.Length > 0 ? Entry.OutTopic : Entry.InTopic;

						PendingMqttTopics.Add(BlynkOutTopic);
						mqttConn.SendMessage(BlynkOutTopic, encoded, !Entry.NoRetain);

						if (Entry.BlynkAck)
							blynkConn.SendVirtualPin(e.Data.Pin, inValue);
						
						Helpers.LogColor(ConsoleColor.Magenta, "[blynk->mqtt]", Helpers.LogLevel.Debug,
							("Using [", ConsoleColor.White),
							(Entry.Encoder.GetType().Name, ConsoleColor.Red),
							("] From BlynkVPin ", ConsoleColor.White),
							(Entry.BlynkVpin.ToString(), ConsoleColor.Green),
							(skipSource ? String.Empty : " -> value: ", ConsoleColor.White),
							(skipSource ? String.Empty : inValue.Replace("\n", ""), ConsoleColor.Yellow),
							(" => ", ConsoleColor.Red),
							("to MqttTopic ", ConsoleColor.White),
							(Entry.InTopic, ConsoleColor.Cyan),
							(skipTarget ? String.Empty : " -> value: ", ConsoleColor.White),
							(skipTarget ? String.Empty : encoded.Replace("\n", ""), ConsoleColor.Yellow)
						);

						break;
					}
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
