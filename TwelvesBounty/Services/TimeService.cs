using FFXIVClientStructs.FFXIV.Client.System.Framework;
using TwelvesBounty.Data;

namespace TwelvesBounty.Services {
	public unsafe class TimeService {
		public EorzeaTime EorzeaTime {
			get {
				var framework = Framework.Instance();
				return new EorzeaTime(framework == null ? 0 : framework->ClientTime.EorzeaTime);
			}
		}
	}
}
