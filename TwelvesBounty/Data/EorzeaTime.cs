using System;

namespace TwelvesBounty.Data;

[Serializable]
public class EorzeaTime(long value = 0) {
	public long Value { get; set; } = value;
	public DateTime DateTime {
		get {
			var h = Value / 3600 % 24;
			var m = Value / 60 % 60;
			var s = Value % 60;
			return new DateTime(1, 1, 1, (int)h, (int)m, (int)s);
		}

		set {
			Value = (value.Hour * 3600) +
				(value.Minute * 60) +
				value.Second;
		}
	}

	public long TimeUntil(EorzeaTime target) {
		var targetValue = target.Value;
		if (Value > target.Value) {
			targetValue += 24 * 60 * 60;
		}
		return targetValue - Value;
	}
}
