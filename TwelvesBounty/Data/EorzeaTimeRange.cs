using System;

namespace TwelvesBounty.Data;

[Serializable]
public class EorzeaTimeRange {
	public EorzeaTime Start { get; set; } = new EorzeaTime();
	public EorzeaTime End { get; set; } = new EorzeaTime();

	public bool Contains(EorzeaTime time) {
		if (Start.Milliseconds <= End.Milliseconds) {
			return time.Milliseconds >= Start.Milliseconds && time.Milliseconds <= End.Milliseconds;
		} else {
			return time.Milliseconds >= Start.Milliseconds || time.Milliseconds <= End.Milliseconds;
		}
	}

	public override string ToString() {
		return $"{Start} - {End}";
	}
}
