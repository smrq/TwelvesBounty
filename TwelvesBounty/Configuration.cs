using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace TwelvesBounty;

[Serializable]
public class Configuration : IPluginConfiguration {
	public int Version { get; set; } = 0;
	public List<Route> Routes { get; set; } = [];
	public float WaypointRadius { get; set; } = 25.0f;
	public float NodeRadius { get; set; } = 2.0f;
	public float WalkRadius { get; set; } = 25.0f;

	public void Save() {
		Plugin.PluginInterface.SavePluginConfig(this);
	}
}

[Serializable]
public class Route {
	public Guid Id { get; set; } = Guid.Empty;
	public string Name { get; set; } = string.Empty;
	public List<WaypointGroup> WaypointGroups { get; set; } = [];
}

[Serializable]
public class WaypointGroup {
	public uint MapId { get; set; } = 0;
	public Vector3 Waypoint { get; set; } = Vector3.Zero;
	public List<ulong> NodeObjectIds { get; set; } = [];
}
