using System;

namespace TwelvesBounty.Data;

[Serializable]
public class EorzeaTimeRange {
	public EorzeaTime Start { get; set; } = new EorzeaTime();
	public EorzeaTime End { get; set; } = new EorzeaTime();

	public bool Contains(EorzeaTime time) {
		if (Start.Value <= End.Value) {
			return time.Value >= Start.Value && time.Value <= End.Value;
		} else {
			return time.Value >= Start.Value || time.Value <= End.Value;
		}
	}
}
