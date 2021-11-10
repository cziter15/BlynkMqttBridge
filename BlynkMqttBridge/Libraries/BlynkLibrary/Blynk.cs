////////////////////////////////////////////////////////////////////////////
//
//  This file is part of BlynkLibrary
//
//  Copyright (c) 2017, Sverre Frøystein
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
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace BlynkMqttBridge.BlynkLibrary
{
	/// <summary>
	/// This is the main Blynk client class. It handles the connection to Blynk and all communication to and from.
	/// </summary>
	public class Blynk
	{
		#region Public definitions
		public static Dictionary<WidgetProperty, string> WProperty = new Dictionary<WidgetProperty, string>()
		{
			{ WidgetProperty.Color,     "color"},
			{ WidgetProperty.Label,     "label"},
			{ WidgetProperty.Max,       "max"},
			{ WidgetProperty.Min,       "min"},
			{ WidgetProperty.OnLabel,   "onLabel"},
			{ WidgetProperty.OffLabel,  "offLabel"},
			{ WidgetProperty.IsEnabled, "isEnabled"},
			{ WidgetProperty.IsOnPlay,  "isOnPlay"}
		};

		/// <summary>
		/// This handler is triggered when a virtual pin is received from Blynk.
		/// </summary>
		public event VirtualPinReceivedHandler VirtualPinReceived;

		/// <summary>
		/// This id the virtual pin received event handler definition.
		/// </summary>
		/// <param name="b">Reference to the Blynk instance sending the event.</param>
		/// <param name="e">The event arguments for the virtual pin.</param>
		public delegate void VirtualPinReceivedHandler(Blynk b, VirtualPinEventArgs e);

		/// <summary>
		/// This handler is triggered when a digital pin is received from Blynk.
		/// </summary>
		public event DigitalPinReceivedHandler DigitalPinReceived;

		/// <summary>
		/// This id the digital pin received event handler definition.
		/// </summary>
		/// <param name="b">Reference to the Blynk instance sending the event.</param>
		/// <param name="e">The event arguments for the digital pin.</param>
		public delegate void DigitalPinReceivedHandler(Blynk b, DigitalPinEventArgs e);

		/// <summary>
		/// This property returns the connected state of the Blynk client.
		/// </summary>
		public bool Connected
		{
			get
			{
				return connected;
			}
		}

		#endregion

		#region Private definitions
		private bool connected = false;
		private const int pingInterval = 5; // 5 seconds ping keep alive interval

		private TcpClient tcpClient = null;

		private string Authentication;
		private string Server;
		private int Port = 8080;
		private Stream tcpStream;
		private int txMessageId;

		private static Timer blynkTimer;

		byte[] rcBuffer = new byte[100];

		#endregion


		/// <summary>
		/// This is the Blynk constructor.
		/// </summary>
		/// <param name="authentication">The Blynk authentication token.</param>
		/// <param name="server">Sever connection string url.</param>
		/// <param name="port">The server port to connect to.</param>
		public Blynk()
		{

		}

		public void setConnection(string authentication, string server, string port)
		{
			stopConnection();

			Helpers.LogColor(ConsoleColor.Red, "[Mqtt-Library]", Helpers.LogLevel.Verbose,
				("setConnection -> server: ", ConsoleColor.White),
				(server + ":" + port, ConsoleColor.Magenta),
				(", token: ", ConsoleColor.White),
				(authentication, ConsoleColor.Cyan)
			);


			Int32.TryParse(port, out Port);
			Authentication = authentication;
			Server = server;

			blynkTimer = new Timer(timer_Tick, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
		}

		private void Connect()
		{
			int connectTimeoutMilliseconds = 1000;

			try
			{
				tcpClient = new TcpClient();
				tcpClient.NoDelay = true;

				Task task = tcpClient.ConnectAsync(Server, Port);
				int index = Task.WaitAny(new[] { task }, connectTimeoutMilliseconds);

				if (tcpClient.Connected)
				{
					tcpStream = tcpClient.GetStream();

					txMessageId = 1;

					List<byte> txMessage = new List<byte>() { 0x02 };

					txMessage.Add((byte)(txMessageId >> 8));
					txMessage.Add((byte)(txMessageId));
					txMessage.Add((byte)(Authentication.Length >> 8));
					txMessage.Add((byte)(Authentication.Length));

					foreach (char c in Authentication)
					{
						txMessage.Add((byte)c);
					}

					tcpStream.Write(txMessage.ToArray(), 0, txMessage.Count);

					readTcpStream();

					connected = true;

					Task.Run(new Action(blynkReceiver));
				}
				else
				{
					throw new Exception("Connect timeout");
				}
			}
			catch (Exception)
			{
				Disconnect();
			}
		}

		private void Disconnect()
		{
			if (tcpStream != null)
				tcpStream.Dispose();

			if (tcpClient != null)
				tcpClient.Close();

			connected = false;
		}

		private void SendPing()
		{
			if (Connected)
			{
				txMessageId++;
				List<byte> txMessage = new List<byte>() { (byte)Command.PING };

				txMessage.Add((byte)(txMessageId >> 8));
				txMessage.Add((byte)(txMessageId));

				txMessage.Add((byte)(0));
				txMessage.Add((byte)(0));

				WriteToTcpStream(txMessage);
			}
		}

		public void SendVirtualPin(VirtualPin vp)
		{
			if (Connected)
			{
				txMessageId++;
				List<byte> txMessage = new List<byte>() { (byte)Command.HARDWARE };

				txMessage.Add((byte)(txMessageId >> 8));
				txMessage.Add((byte)(txMessageId));
				PrepareVirtualWrite(vp, txMessage);

				WriteToTcpStream(txMessage);
			}
		}

		public void SendVirtualPin(int Pin, object Value)
		{
			BlynkLibrary.VirtualPin vp = new BlynkLibrary.VirtualPin();
			vp.Value[0] = Value;
			vp.Pin = Pin;
			SendVirtualPin(vp);
		}

		private static void PrepareVirtualWrite(VirtualPin vp, List<byte> txMessage)
		{
			txMessage.Add((byte)'v');
			txMessage.Add((byte)'w');
			txMessage.Add(0x00);

			txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(vp.Pin.ToString()));

			txMessage.Add(0x00);

			foreach (object o in vp.Value)
			{
				txMessage.AddRange(ASCIIEncoding.UTF8.GetBytes(o.ToString().Replace(',', '.')));
				txMessage.Add(0x00);
			}

			txMessage.RemoveAt(txMessage.Count - 1);

			int msgLength = txMessage.Count - 3;

			txMessage.Insert(3, (byte)((msgLength) >> 8));
			txMessage.Insert(4, (byte)((msgLength)));
		}

		public void SendDigitalPin(DigitalPin dp)
		{
			if (Connected)
			{
				txMessageId++;
				List<byte> txMessage = new List<byte>() { (byte)Command.HARDWARE };

				txMessage.Add((byte)(txMessageId >> 8));
				txMessage.Add((byte)(txMessageId));

				string pin = dp.Pin.ToString();

				int msgLength = pin.Length + 5;

				txMessage.Add((byte)((msgLength) >> 8));
				txMessage.Add((byte)((msgLength)));

				txMessage.Add((byte)'d');
				txMessage.Add((byte)'w');
				txMessage.Add(0x00);

				txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(pin.ToString()));

				txMessage.Add(0x00);

				if (dp.Value)
				{
					txMessage.Add((byte)'1');
				}
				else
				{
					txMessage.Add((byte)'0');
				}

				WriteToTcpStream(txMessage);
			}
		}

		public void SetProperty(VirtualPin vp)
		{
			if (Connected)
			{
				txMessageId++;
				List<byte> txMessage = new List<byte>();
				int startCount = 0;

				foreach (Tuple<object, object> p in vp.Property)
				{
					startCount = txMessage.Count;

					txMessage.Add((byte)Command.SET_WIDGET_PROPERTY);
					txMessage.Add((byte)(txMessageId >> 8));
					txMessage.Add((byte)(txMessageId));

					txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(vp.Pin.ToString()));
					txMessage.Add(0x00);
					txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(p.Item1.ToString()));
					txMessage.Add(0x00);
					txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(p.Item2.ToString()));

					int msgLength = (txMessage.Count - startCount) - 3;

					txMessage.Insert(startCount + 3, (byte)((msgLength) >> 8));
					txMessage.Insert(startCount + 4, (byte)((msgLength)));
				}

				WriteToTcpStream(txMessage);
			}
		}

		public void ReadVirtualPin(VirtualPin vp)
		{
			if (Connected)
			{
				txMessageId++;
				List<byte> txMessage = new List<byte>() { (byte)Command.HARDWARE_SYNC };

				txMessage.Add((byte)(txMessageId >> 8));
				txMessage.Add((byte)(txMessageId));
				txMessage.Add((byte)'v');
				txMessage.Add((byte)'r');
				txMessage.Add(0x00);

				txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(vp.Pin.ToString()));

				int msgLength = txMessage.Count - 3;

				txMessage.Insert(3, (byte)((msgLength) >> 8));
				txMessage.Insert(4, (byte)((msgLength)));

				WriteToTcpStream(txMessage);
			}
		}

		internal void BridgeVirtualWrite(int b, VirtualPin vp)
		{
			if (Connected)
			{
				txMessageId++;
				List<byte> txMessage = new List<byte>() { (byte)Command.BRIDGE };

				txMessage.Add((byte)(txMessageId >> 8));
				txMessage.Add((byte)(txMessageId));
				txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(b.ToString()));
				txMessage.Add((byte)(0x00));

				PrepareVirtualWrite(vp, txMessage);

				WriteToTcpStream(txMessage);
			}
		}
		internal void BridgeDigitalWrite(int b, DigitalPin dp)
		{
			if (Connected)
			{
				txMessageId++;
				List<byte> txMessage = new List<byte>() { (byte)Command.BRIDGE };

				txMessage.Add((byte)(txMessageId >> 8));
				txMessage.Add((byte)(txMessageId));
				txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(b.ToString()));
				txMessage.Add((byte)(0x00));
				txMessage.Add((byte)('d'));
				txMessage.Add((byte)('w'));
				txMessage.Add((byte)(0x00));
				txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(dp.Pin.ToString()));
				txMessage.Add((byte)(0x00));
				txMessage.Add((byte)(dp.Value ? '1' : '0'));

				int msgLength = txMessage.Count - 3;

				txMessage.Insert(3, (byte)((msgLength) >> 8));
				txMessage.Insert(4, (byte)((msgLength)));

				WriteToTcpStream(txMessage);
			}
		}

		internal void BridgeSetAuthToken(int p, string auth)
		{
			if (Connected)
			{
				txMessageId++;
				List<byte> txMessage = new List<byte>() { (byte)Command.BRIDGE };

				txMessage.Add((byte)(txMessageId >> 8));
				txMessage.Add((byte)(txMessageId));
				txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(p.ToString()));
				txMessage.Add((byte)(0x00));
				txMessage.Add((byte)('i'));
				txMessage.Add((byte)(0x00));
				txMessage.AddRange(ASCIIEncoding.ASCII.GetBytes(auth));

				int msgLength = txMessage.Count - 3;

				txMessage.Insert(3, (byte)((msgLength) >> 8));
				txMessage.Insert(4, (byte)((msgLength)));

				WriteToTcpStream(txMessage);
			}
		}

		private void timer_Tick(object state)
		{
			if (Connected)
			{
				SendPing();
			}
			else
			{
				Helpers.Log("Try connect to Blynk server...", ConsoleColor.Yellow, "[Blynk-Library]", Helpers.LogLevel.Verbose);

				Disconnect();
				Connect();

				if (Connected)
					Helpers.Log("Connected to Blynk :)", ConsoleColor.Green, "[Blynk-Library]", Helpers.LogLevel.Verbose);
			}
		}

		private void blynkReceiver()
		{
			while (Connected)
			{
				readTcpStream();
			}
		}

		private void readTcpStream()
		{
			UInt16 rcMessageId = 0;

			int count = tcpStream.Read(rcBuffer, 0, 100);

			if (count > 0)
			{
				List<byte> rcMessage = new List<Byte>(rcBuffer);

				rcMessage = rcMessage.GetRange(0, count);

				while (rcMessage.Count >= 5)
				{
					byte[] lengthBytes = rcMessage.GetRange(3, 2).ToArray();
					lengthBytes = lengthBytes.Reverse().ToArray();

					UInt16 messageLength = 0;

					if (rcMessage[0] != (byte)Command.RESPONSE)
					{
						messageLength = BitConverter.ToUInt16(lengthBytes, 0);
					}

					if (rcMessage.Count >= 5 + messageLength)
					{
						decodeMessage(rcMessage.GetRange(0, 5 + messageLength));

						rcMessage.RemoveRange(0, 5 + messageLength);
					}
					else
					{
						rcMessage.Clear();
					}
				}

				if (rcMessageId != 0)
				{
					SendResponse(rcMessageId);
				}
			}
		}

		private UInt16 decodeMessage(List<byte> rcMessage)
		{
			UInt16 messageLength = (UInt16)(rcMessage.Count - 5);
			Command cmd = (Command)rcMessage[0];
			byte[] receivedIdBytes = rcMessage.GetRange(1, 2).ToArray();

			receivedIdBytes = receivedIdBytes.Reverse().ToArray();

			UInt16 rcMessageId = BitConverter.ToUInt16(receivedIdBytes, 0);

			switch (cmd)
			{
				case Command.RESPONSE:
					break;

				case Command.BRIDGE:
				case Command.HARDWARE:
					var elements = System.Text.Encoding.ASCII.GetString(rcMessage.GetRange(5, messageLength).ToArray()).Split((char)0x00);

					if (elements[0] == "vw")
					{
						var e = new VirtualPinEventArgs();

						e.Data.Pin = byte.Parse(elements[1]);
						e.Data.Value.Clear();

						for (int i = 2; i <= elements.Length - 1; i++)
						{
							int value;
							if (int.TryParse(elements[i], out value))
							{
								e.Data.Value.Add(value);
							}
							else
							{
								e.Data.Value.Add(elements[i]);
							}
						}

						VirtualPinReceived(this, e);
					}

					if (elements[0] == "dw")
					{
						var e = new DigitalPinEventArgs();

						e.Data.Pin = byte.Parse(elements[1]);
						e.Data.Value = int.Parse(elements[2]) == 1;

						DigitalPinReceived(this, e);
					}

					break;

				default:
					break;
			}

			return rcMessageId;
		}

		private void SendResponse(int mId)
		{
			if (Connected)
			{
				List<byte> txMessage = new List<byte>() { (byte)Command.RESPONSE };

				txMessage.Add((byte)(mId >> 8));
				txMessage.Add((byte)(mId));

				txMessage.Add((byte)(0));
				txMessage.Add((byte)(Response.OK));

				WriteToTcpStream(txMessage);
			}
		}

		private void WriteToTcpStream(List<byte> txMessage)
		{
			try
			{
				tcpStream.Write(txMessage.ToArray(), 0, txMessage.Count);
			}
			catch (Exception)
			{
				Disconnect();
			}
		}


		public void stopConnection()
		{
			if (blynkTimer != null)
				blynkTimer.Dispose();

			Disconnect();
		}
	}
}