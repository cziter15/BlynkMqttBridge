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
		public class _StraightType
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

		private class _OnOffType : _StraightType
		{
			public override string fromBlynk(TopicEntry entry, string value)
			{
				float floatval = 0.0f;
				float.TryParse(value, out floatval);
				return floatval > 0.0f ? "1" : "0";
			}
		}

		private class _LedType : _OnOffType
		{
			public override string toBlynk(TopicEntry entry, string value)
			{
				float floatval = 0.0f;
				float.TryParse(value, out floatval);
				return floatval > 0.0f ? "255" : "0";
			}
		}

		private class _OnOffSegmentedType : _OnOffType
		{
			public override string fromBlynk(TopicEntry entry, string value)
			{
				return value == "1" ? "1" : "0";
			}
			public override string toBlynk(TopicEntry entry, string value)
			{
				return value == "1" ? "1" : "2";
			}
		}

		private class _TerminalType : _StraightType
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

		private class _StringMap : _StraightType
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

		public static _StraightType LedType = new _LedType();
		public static _StraightType OnOffType = new _OnOffType();
		public static _StraightType OnOffSegmented = new _OnOffSegmentedType();
		public static _StraightType StraightType = new _StraightType();
		public static _StraightType TerminalType = new _TerminalType();
		public static _StraightType StringMap = new _StringMap();

		public static _StraightType TypeFromName(string typename)
		{
			switch (typename)
			{
				case "LedType": return LedType;
				case "OnOffType": return OnOffType;
				case "OnOffSegmented": return OnOffSegmented;
				case "StraightType": return StraightType;
				case "TerminalType": return TerminalType;
				case "StringMap": return StringMap;
			}

			return null;
		}
	}
}
