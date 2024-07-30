using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TwelvesBounty.Data {
	[Serializable]
	public class GatheringNodeGroup {
		public uint MapId { get; set; } = 0;
		public List<GatheringNode> GatheringNodes { get; set; } = [];
		public EorzeaTimeRange? Uptime { get; set; } = null;
		public bool Repeat { get; set; } = false;

		[IgnoreDataMember]
		public string MapName {
			get {
				var mapSheet = Plugin.DataManager.GetExcelSheet<Map>()!;
				var map = mapSheet.GetRow(MapId);
				return map?.PlaceName.Value?.Name ?? string.Empty;
			}
		}
	}
}
