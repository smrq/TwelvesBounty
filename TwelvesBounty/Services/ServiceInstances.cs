using System;
using TwelvesBounty.IPC;

namespace TwelvesBounty.Services {
	public class ServiceInstances : IDisposable {
		public NavmeshIPC NavmeshIPC { get; init; }
		public ActionService ActionService { get; init; }
		public GatheringService GatheringService { get; init; }
		public GearsetService GearsetService { get; init; }
		public InventoryService InventoryService { get; init; }
		public NavigationService NavigationService { get; init; }
		public RepairService RepairService { get; init; }
		public SpiritbondService SpiritbondService { get; init; }
		public TimeService TimeService { get; init; }

		private readonly Throttle actionThrottle = new();

		public ServiceInstances() {
			NavmeshIPC = new NavmeshIPC();
			ActionService = new ActionService(actionThrottle);
			GatheringService = new GatheringService();
			GearsetService = new GearsetService();
			InventoryService = new InventoryService(actionThrottle);
			RepairService = new RepairService(actionThrottle);
			SpiritbondService = new SpiritbondService(actionThrottle);
			TimeService = new TimeService();
			NavigationService = new NavigationService(ActionService, NavmeshIPC);
		}

		public void Dispose() {
			GatheringService.Dispose();
		}
	}
}
