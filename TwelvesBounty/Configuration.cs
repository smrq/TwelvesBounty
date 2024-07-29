using Dalamud.Configuration;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;

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
}

[Serializable]
public class Route {
	public Guid Id { get; set; } = Guid.Empty;
	public string Name { get; set; } = string.Empty;
	public List<WaypointGroup> WaypointGroups { get; set; } = [];

	[IgnoreDataMember]
	public uint? MapId {
		get {
			if (WaypointGroups.Count == 0) {
				return null;
			}
			return WaypointGroups[0].MapId;
		}
	}

	[IgnoreDataMember]
	public string MapName {
		get {
			if (WaypointGroups.Count == 0) {
				return string.Empty;
			}
			return WaypointGroups[0].MapName;
		}
	}
}

[Serializable]
public class WaypointGroup {
	public uint MapId { get; set; } = 0;
	public Vector3 Waypoint { get; set; } = Vector3.Zero;
	public List<ulong> NodeObjectIds { get; set; } = [];

	[IgnoreDataMember]
	public string MapName {
		get {
			var mapSheet = Plugin.DataManager.GetExcelSheet<Map>()!;
			var map = mapSheet.GetRow(MapId);
			if (map == null) {
				return string.Empty;
			}
			var placeName = map.PlaceName.Value;
			if (placeName == null) {
				return string.Empty;
			}
			return placeName.Name;
		}
	}
}
