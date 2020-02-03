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
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace BlynkMqttBridge.MqttLibrary
{
	class MqttHandler
	{
		public delegate void Delegate_PublishReceivedEvent(string Topic, string Payload);
		public Delegate_PublishReceivedEvent PublishReceivedEvent;

		public delegate void Delegate_ConnectionStatusChanged(bool Connected);
		public Delegate_ConnectionStatusChanged ConnectionChangeEvent;

		private MqttClient activeClient = null;

		Timer connectionCheckerTimer = null;

		private string cachedServer;
		private int cachedPort;
		private string cachedUsername;
		private string cachedPassword;

		private bool wasConnected = false;

		private void ConnectInternal()
		{
			try
			{
				activeClient = new MqttClient(cachedServer, cachedPort, false, null, null, MqttSslProtocols.None);
				activeClient.MqttMsgPublishReceived += HandlePublishReceived;
				activeClient.Connect(Guid.NewGuid().ToString(), cachedUsername, cachedPassword, true, 15);
			}
			catch (Exception) { }
		}

		public void setConnection(string server, string port, string username, string password)
		{
			try
			{
				stopConnection();

				cachedServer = server;
				Int32.TryParse(port, out cachedPort);
				cachedUsername = username;
				cachedPassword = password;

				connectionCheckerTimer = new Timer(TimerCallback, null, 0, 2000);

				ConnectionChangeEvent(false);
			}
			catch (Exception) { }
		}

		public void stopConnection()
		{
			TerminateClient();

			if (connectionCheckerTimer != null)
				connectionCheckerTimer.Dispose();
		}

		private void TerminateClient()
		{
			if (activeClient != null)
			{
				activeClient.Disconnect();
				activeClient = null;
			}
		}

		private void TimerCallback(Object o)
		{
			bool Connected = activeClient != null && activeClient.IsConnected;

			if (Connected != wasConnected)
			{
				wasConnected = Connected;

				if (ConnectionChangeEvent != null)
					ConnectionChangeEvent(wasConnected);
			}

			if (!Connected)
			{
				ConnectInternal();
			}
		}

		public void HandlePublishReceived(object sender, MqttMsgPublishEventArgs e)
		{
			if (PublishReceivedEvent != null)
				PublishReceivedEvent(e.Topic, Encoding.UTF8.GetString(e.Message));
		}

		public void Subscribe(string[] topics)
		{
			if (activeClient != null && activeClient.IsConnected)
			{
				byte[] b = new byte[topics.Length];
				activeClient.Subscribe(topics, b);
			}
		}

		public void Unsubscribe(string[] topics)
		{
			if (activeClient != null && activeClient.IsConnected)
				activeClient.Unsubscribe(topics);
		}

		public void SendMessage(string topic, string payload)
		{
			SendMessage(topic, Encoding.UTF8.GetBytes(payload));
		}

		public void SendMessage(string topic, byte[] payload)
		{
			if (activeClient != null && activeClient.IsConnected)
				activeClient.Publish(topic, payload, 0, true);
		}

		public void Dispose()
		{
			if (activeClient != null && activeClient.IsConnected)
			{
				activeClient.Disconnect();
				activeClient = null;
			}
		}
	}
}
