using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TwelvesBounty.Data {
	[Serializable]
	public class GatheringNodeGroup {
		public uint MapId { get; set; } = 0;
		public List<GatheringNode> GatheringNodes { get; set; } = [];
		public string LinkedGearset { get; set; } = string.Empty;
		public List<EorzeaTimeRange> Uptime { get; set; } = [];
		public bool Repeat { get; set; } = false;

		[IgnoreDataMember]
		public string MapName {
			get {
				var mapSheet = Plugin.DataManager.GetExcelSheet<Map>()!;
				var map = mapSheet.GetRow(MapId);
				return map?.PlaceName.Value?.Name ?? string.Empty;
			}
		}

		public bool WithinUptime(EorzeaTime time) {
			return Uptime.Count == 0 || Uptime.Any(u => u.Contains(time));
		}

		public long TimeUntilUptime(EorzeaTime time) {
			if (WithinUptime(time)) return 0;
			return Uptime.Min(u => time.TimeUntil(u.Start));
		}
	}
}
