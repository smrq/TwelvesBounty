using System;

namespace TwelvesBounty.Data;

[Serializable]
public class EorzeaTime {
	public EorzeaTime(long value = 0) {
		Milliseconds = value;
	}

	public EorzeaTime(int hour, int minute) {
		Milliseconds = (hour * 60 * 60 * 1000) +
			(minute * 60 * 1000);
	}

	public long Milliseconds { get; set; }

	public int Hour { get => (int)Milliseconds / (60 * 60 * 1000); }
	public int Minute { get => (int)Milliseconds / (60 * 1000) % 60; }

	public long TimeUntil(EorzeaTime target) {
		var result = target.Milliseconds - Milliseconds;
		while (result < 0) {
			result += 24 * 60 * 60 * 1000;
		}
		return result;
	}

	public override string ToString() {
		return $"{Hour:D2}:{Minute:D2}";
	}
}
