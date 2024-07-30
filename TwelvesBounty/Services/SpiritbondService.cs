using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using System.Linq;

namespace TwelvesBounty.Services {
	public unsafe class SpiritbondService(Throttle throttle) {
		private readonly Throttle throttle = throttle;

		public List<ushort> EquippedSpiritbond {
			get {
				var items = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems)->Items;
				return Enumerable.Range(0, 13)
					.Select(n => items[n].Spiritbond)
					.ToList();
			}
		}

		public bool IsEquippedSpiritbondReady { get => EquippedSpiritbond.Any(value => value == 10000); }
		public bool IsMaterializeOpen { get => Plugin.GameGui.GetAddonByName("Materialize", 1) != nint.Zero; }
		public bool IsMaterializeDialogOpen { get => Plugin.GameGui.GetAddonByName("MaterializeDialog", 1) != nint.Zero; }

		public bool OpenMaterialize() {
			if (!IsMaterializeOpen) {
				return ToggleMaterialize();
			} else {
				return true;
			}
		}

		public bool CloseMaterialize() {
			if (IsMaterializeOpen) {
				return ToggleMaterialize();
			} else {
				return true;
			}
		}

		public bool ToggleMaterialize() {
			return Throttle.ExecuteConditional(throttle, () => {
				Plugin.PluginLog.Debug($"ToggleMaterialize");
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 14);
			});
		}

		public bool ConfirmMaterializeDialog() {
			var ptr = Plugin.GameGui.GetAddonByName("MaterializeDialog", 1);
			if (ptr == nint.Zero) {
				return true;
			}
			var atkUnitBase = (AtkUnitBase*)ptr;
			return false;
		}
	}
}
