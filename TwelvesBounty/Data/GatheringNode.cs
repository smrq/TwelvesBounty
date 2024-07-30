using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TwelvesBounty.Data;

[Serializable]
public class GatheringNode {
	public ulong DataId { get; set; } = 0;
	public List<Vector3> Positions { get; set; } = [];
	public Vector3 AveragePosition {
		get {
			if (Positions.Count == 0) {
				return Vector3.Zero;
			}
			return new(
				Positions.Average(pos => pos.X),
				Positions.Average(pos => pos.Y),
				Positions.Average(pos => pos.Z)
			);
		}
	}
}
