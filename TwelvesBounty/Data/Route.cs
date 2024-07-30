using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TwelvesBounty.Data;

[Serializable]
public class Route {
	public Guid Id { get; set; } = Guid.Empty;
	public string Name { get; set; } = string.Empty;
	public List<GatheringNodeGroup> Groups { get; set; } = [];
}
