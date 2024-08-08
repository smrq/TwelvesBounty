using FFXIVClientStructs.FFXIV.Client.System.Framework;
using TwelvesBounty.Data;

namespace TwelvesBounty.Services {
	public unsafe class TimeService {
		public EorzeaTime EorzeaTime {
			get {
				return new EorzeaTime(EorzeaTimeRaw * 1000 % (24 * 60 * 60 * 1000));
			}
		}

		public long EorzeaTimeRaw {
			get {
				var framework = Framework.Instance();
				if (framework == null) return 0;

				var seconds = framework->ClientTime.EorzeaTime;
				return seconds;
			}
		}
	}
}
