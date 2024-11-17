using System;

namespace TwelvesBounty {
	public class Throttle(float throttleSeconds = 0.5f) {
		private DateTime minNextAction;
		private float throttleSeconds = throttleSeconds;

		public bool Execute(Action action) {
			var now = DateTime.Now;
			if (now < minNextAction) {
				return false;
			}

			action();
			minNextAction = now.AddSeconds(throttleSeconds);
			return true;
		}

		public bool Execute(Func<bool> action) {
			var now = DateTime.Now;
			if (now < minNextAction) {
				return false;
			}

			var result = action();
			minNextAction = now.AddSeconds(throttleSeconds);
			return result;
		}

		public static bool ExecuteConditional(Throttle? throttle, Action action) {
			if (throttle != null) {
				return throttle.Execute(action);
			} else {
				action();
				return true;
			}
		}

		public static bool ExecuteConditional(Throttle? throttle, Func<bool> action) {
			if (throttle != null) {
				return throttle.Execute(action);
			} else {
				return action();
			}
		}
	}
}
