# BlynkMqttBridge
This C# application works as a two-way bridge between MQTT and Blynk App.
By changing configured VPIN value on Blynk, it will be reposted to MQTT server and vice versa.

# Configuration
Example configuration file is provided in bin/Release directory. Application will look for blynkmqttbridge.ini file every run.
Once ran, it loads topics mapping and types. There is a simple mechanism which allows to convert values, for example Blynk LED value is parsed as 0 to 255, but MQTT bool value is almost always defined as 0 or 1. Simply, when MQTT topic has value "1", then "255" will be published on Blynk side and when "255" value is published on that pin, it will repost value "1" to MQTT. This behaviour is defined in TypeEncoder.cs in Application directory.

# Libraries
This project is using libraries:
- M2Mqtt https://github.com/eclipse/paho.mqtt.m2mqtt / Eclipse Public License - v 1.0
- BlynkLibrary by Sverre Frøystein https://github.com/sverrefroy/BlynkLibrary
- Ini by Larry57 https://gist.github.com/Larry57/5725301

# Special thanks
Damian K aka Traffic
