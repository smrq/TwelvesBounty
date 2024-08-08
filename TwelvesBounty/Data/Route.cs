using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TwelvesBounty.Data;

[Serializable]
public class Route {
	public Guid Id { get; set; } = Guid.Empty;
	public string Name { get; set; } = string.Empty;
	public List<GatheringNodeGroup> Groups { get; set; } = [];

	public Route Clone() {
		var serialized = JsonConvert.SerializeObject(this);
		var clone = JsonConvert.DeserializeObject<Route>(serialized)!;
		clone.Id = Guid.NewGuid();
		return clone;
	}
}
