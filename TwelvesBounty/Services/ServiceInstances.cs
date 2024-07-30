using TwelvesBounty.IPC;

namespace TwelvesBounty.Services {
	public class ServiceInstances {
		public ActionService ActionService { get; init; }
		public NavigationService NavigationService { get; init; }
		public SpiritbondService SpiritbondService { get; init; }
		public TimeService TimeService { get; init; }
		public NavmeshIPC NavmeshIPC { get; init; }

		private readonly Throttle actionThrottle = new();

		public ServiceInstances() {
			NavmeshIPC = new NavmeshIPC();
			ActionService = new ActionService(actionThrottle);
			NavigationService = new NavigationService(ActionService, NavmeshIPC);
			SpiritbondService = new SpiritbondService(actionThrottle);
			TimeService = new TimeService();
		}
	}
}
