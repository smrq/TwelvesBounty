using System;
using System.Collections.Generic;

namespace TwelvesBounty {
	public class Throttle {
		private DateTime minNextAction;

		public bool Execute(Action action, float throttleSeconds = 0.5f) {
			var now = DateTime.Now;
			if (now < minNextAction) {
				return false;
			}

			action();
			minNextAction = now.AddSeconds(throttleSeconds);
			return true;
		}
	}
}
