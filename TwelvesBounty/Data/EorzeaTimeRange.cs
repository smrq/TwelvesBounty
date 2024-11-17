using System;

namespace TwelvesBounty.Data;

[Serializable]
public class EorzeaTimeRange {
	public EorzeaTime Start { get; set; } = new EorzeaTime();
	public EorzeaTime End { get; set; } = new EorzeaTime();

	public bool Contains(EorzeaTime time, uint startDelay = 0) {
		var start = Start.Milliseconds;
		var end = End.Milliseconds;
		var t = time.Milliseconds;
		var delayedStart = start + startDelay;

		if (start < end && delayedStart >= end) {
			return false;
		}

		if (delayedStart <= end) {
			return t >= delayedStart && t <= end;
		} else {
			return t >= delayedStart || t <= end;
		}
	}

	public override string ToString() {
		return $"{Start} - {End}";
	}
}
