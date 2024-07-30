using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;

namespace TwelvesBounty.Data;

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
			return map?.PlaceName.Value?.Name ?? string.Empty;
		}
	}
}
