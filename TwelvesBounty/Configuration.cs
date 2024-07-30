using Dalamud.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using TwelvesBounty.Data;

namespace TwelvesBounty;

[Serializable]
public class Configuration : IPluginConfiguration {
	public int Version { get; set; } = 0;
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
				var config = configJson.ToObject<Configuration>();
				if (config != null) {
					return config!;
				}
			}
		}
		return new Configuration();
	}
}
