using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwelvesBounty {
	public class SpiritbondManager {
		private unsafe List<ushort> EquippedSpiritbond {
			get {
				var items = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems)->Items;
				return Enumerable.Range(0, 13)
					.Select(n => items[n].Spiritbond)
					.ToList();
			}
		}

		public bool IsSpiritbondReady() {
			return EquippedSpiritbond.Any(value => value == 10000);
		}

		public bool IsMaterializeOpen() => Plugin.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero;
		public bool IsMaterializeDialogOpen() => Plugin.GameGui.GetAddonByName("MaterializeDialog", 1) != IntPtr.Zero;
		private unsafe bool UseMateriaExtraction() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 14);
		public unsafe void OpenMaterialize() {
			if (!IsMaterializeOpen()) {
				UseMateriaExtraction();
			}
		}
		public unsafe void CloseMaterialize() {
			if (IsMaterializeOpen()) {
				UseMateriaExtraction();
			}
		}
		public unsafe void ConfirmMaterializeDialog() {
			var ptr = Plugin.GameGui.GetAddonByName("MaterializeDialog", 1);
			if (ptr == IntPtr.Zero) {
				return;
			}
			var atkUnitBase = (AtkUnitBase*)ptr;
		}
	}
}
