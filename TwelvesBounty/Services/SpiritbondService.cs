using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections;
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
		public bool IsMaterializeOpen { get => Plugin.GameGui.GetAddonByName("Materialize") != nint.Zero; }
		public bool IsMaterializeDialogOpen { get => Plugin.GameGui.GetAddonByName("MaterializeDialog") != nint.Zero; }

		public IEnumerable ExtractMateriaTask() {
			while (IsEquippedSpiritbondReady) {
				if (!IsMaterializeOpen) {
					Throttle.ExecuteConditional(throttle, () => {
						OpenMaterialize();
					});
					yield return null;
				} else if (IsMaterializeDialogOpen) {
					Throttle.ExecuteConditional(throttle, () => {
						ConfirmMaterializeDialog();
					});
					yield return null;
				} else {
					Throttle.ExecuteConditional(throttle, () => {
						ExtractFirstMateria();
					});
					yield return null;
				}
			}

			while (IsMaterializeOpen) {
				Throttle.ExecuteConditional(throttle, () => {
					CloseMaterialize();
				});
				yield return null;
			}
		}

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

		public bool ExtractMateria() {
			return Throttle.ExecuteConditional(throttle, () => {
				if (IsMaterializeDialogOpen) {
					Plugin.PluginLog.Debug($"ConfirmMaterializeDialog");
					ConfirmMaterializeDialog();
					return true;
				}

				ExtractFirstMateria();
				return false;
			});
		}

		private bool ConfirmMaterializeDialog() {
			var addon = (AddonMaterializeDialog*)Plugin.GameGui.GetAddonByName("MaterializeDialog");
			if (addon == null) return false;

			if (!addon->YesButton->IsEnabled) return false;

			var resNode = addon->YesButton->OwnerNode->AtkResNode;
			var e = resNode.AtkEventManager.Event;
			addon->ReceiveEvent(e->Type, (int)e->Param, e);
			return true;
		}

		private bool ExtractFirstMateria() {
			var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("Materialize");
			if (addon == null) return false;

			var values = stackalloc AtkValue[1] {
					new() {
						Type = ValueType.Int,
						Int = 2,
					},
				};
			addon->FireCallback(1, values);
			return true;
		}
	}
}
