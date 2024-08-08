using Dalamud.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using TwelvesBounty.Data;

namespace TwelvesBounty;

[Serializable]
public class Configuration : IPluginConfiguration {
	public int Version { get; set; } = 1;
	public List<Route> Routes { get; set; } = [];
	public float WaypointRadius { get; set; } = 25.0f;
	public float NodeRadius { get; set; } = 1.0f;
	public float WalkRadius { get; set; } = 25.0f;

	public void Save() {
		Plugin.PluginInterface.SavePluginConfig(this);
	}

	public static Configuration LoadConfiguration(string filename) {
		var configText = File.ReadAllText(filename);
		var configJson = JObject.Parse(configText);
		if (configJson != null) {
			if ((int?)configJson["Version"] == 0) {
				Plugin.PluginLog.Debug("Migrating config from v0 to v1");
				configJson["Version"] = 1;
				foreach (var route in configJson["Routes"]!) {
					foreach (var group in route["Groups"]!) {
						if (group["Uptime"]!.Type == JTokenType.Null) {
							group["Uptime"] = new JArray();
						} else if (group["Uptime"]!.Type != JTokenType.Array) {
							var array = new JArray {
								group["Uptime"]!
							};
							group["Uptime"] = array;
						}
					}
				}
			}

			if ((int?)configJson["Version"] == 1) {
				Plugin.PluginLog.Debug("Loading config v1");
				var config = configJson.ToObject<Configuration>();
				if (config != null) {
					return config!;
				}
			}
		}

		Plugin.PluginLog.Debug("Loading default configuration");
		return new Configuration();
	}
}
