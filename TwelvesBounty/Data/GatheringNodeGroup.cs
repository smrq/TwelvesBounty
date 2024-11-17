using Lumina.Excel.Sheets;
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
		public uint ItemId { get; set; } = 0;
		public List<EorzeaTimeRange> Uptime { get; set; } = [];
		public bool Repeat { get; set; } = false;

		[IgnoreDataMember]
		public string MapName {
			get {
				var mapSheet = Plugin.DataManager.GetExcelSheet<Map>()!;
				var map = mapSheet.GetRow(MapId);
				return map.PlaceName.Value.Name.ExtractText();
			}
		}

		public bool WithinUptime(EorzeaTime time, uint spawnDelay = 0) {
			return Uptime.Count == 0 || Uptime.Any(u => u.Contains(time, spawnDelay));
		}

		public long TimeUntilUptime(EorzeaTime time) {
			if (WithinUptime(time)) return 0;
			return Uptime.Min(u => time.TimeUntil(u.Start));
		}
	}
}
