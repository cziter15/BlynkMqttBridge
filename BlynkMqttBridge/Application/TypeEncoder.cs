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

using BlynkMqttBridge.Application;

namespace BlynkMqttBridge
{
	public class TypeEncoder
	{
		public class TStraightType
		{
			public virtual string fromBlynk(TopicEntry entry, string value)
			{
				return value;
			}

			public virtual string toBlynk(TopicEntry entry, string value)
			{
				return value;
			}
		}

		private class TOnOffType : TStraightType
		{
			public override string fromBlynk(TopicEntry entry, string value)
			{
				float floatval = 0.0f;
				float.TryParse(value, out floatval);
				return floatval > 0.0f ? "1" : "0";
			}
		}

		private class TLedType : TOnOffType
		{
			public override string toBlynk(TopicEntry entry, string value)
			{
				float floatval = 0.0f;
				float.TryParse(value, out floatval);
				return floatval > 0.0f ? "255" : "0";
			}
		}

		private class TSubOrAddOne : TStraightType
		{
			public override string fromBlynk(TopicEntry entry, string value)
			{
				int intval = 0;
				int.TryParse(value, out intval);

				return (intval - 1).ToString();
			}
			public override string toBlynk(TopicEntry entry, string value)
			{
				int intval = 0;
				int.TryParse(value, out intval);

				return (intval + 1).ToString();
			}
		}

		private class TTerminalType : TStraightType
		{
			public override string fromBlynk(TopicEntry entry, string value)
			{
				return value;
			}
			public override string toBlynk(TopicEntry entry, string value)
			{
				value = value.Replace(", ", "\n");
				return value + "\n";
			}
		}

		private class TStringMap : TStraightType
		{
			public override string fromBlynk(TopicEntry entry, string value)
			{
				string valueToFind = value;
				foreach (string s in entry.ExtraData.Split(','))
				{
					string[] strings = s.Split('=');

					if (strings.Length == 2 && strings[1] == value)
					{
						return strings[0];
					}
				}

				return "0";

			}
			public override string toBlynk(TopicEntry entry, string value)
			{
				string valueToFind = value;
				foreach (string s in entry.ExtraData.Split(','))
				{
					string[] strings = s.Split('=');

					if (strings.Length == 2 && strings[0] == value)
					{
						return strings[1];
					}
				}

				return "0";
			}
		}

		public static TStraightType LedType = new TLedType();
		public static TStraightType OnOffType = new TOnOffType();
		public static TStraightType SubOrAddOne = new TSubOrAddOne();
		public static TStraightType StraightType = new TStraightType();
		public static TStraightType TerminalType = new TTerminalType();
		public static TStraightType StringMap = new TStringMap();
					  
		public static TStraightType TypeFromName(string typename)
		{
			switch (typename)
			{
				case "LedType": return LedType;
				case "OnOffType": return OnOffType;
				case "SubOrAddOne": return SubOrAddOne;
				case "StraightType": return StraightType;
				case "TerminalType": return TerminalType;
				case "StringMap": return StringMap;
			}

			return null;
		}
	}
}
