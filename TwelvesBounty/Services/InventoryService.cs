using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TwelvesBounty.Services {
	public unsafe class InventoryService(Throttle throttle) {
		private readonly Throttle throttle = throttle;

		private static readonly List<InventoryType> InventoryAll = [
			InventoryType.Inventory1,
			InventoryType.Inventory2,
			InventoryType.Inventory3,
			InventoryType.Inventory4
		];

		public List<Pointer<InventoryItem>> ReducibleItems {
			get {
				return AllInventorySlots.Where(slot => IsReducible(slot)).ToList();
			}
		}

		public int EmptySlots {
			get {
				return AllInventorySlots.Count(slot => IsEmpty(slot));
			}
		}

		private IEnumerable<Pointer<InventoryItem>> AllInventorySlots {
			get {
				return InventoryAll.SelectMany(inventory => {
					var container = InventoryManager.Instance()->GetInventoryContainer(inventory);
					return Enumerable.Range(0, (int)container->Size)
						.Select(i => (Pointer<InventoryItem>)container->GetInventorySlot(i));
				});
			}
		}

		private bool IsEmpty(InventoryItem* item) {
			return item->ItemId == 0;
		}

		private bool IsReducible(InventoryItem* item) {
			if ((item->Flags & InventoryItem.ItemFlags.Collectable) == 0) {
				return false;
			}
			var itemSheet = Plugin.DataManager.GetExcelSheet<Item>()!;
			var itemRow = itemSheet.GetRow(item->ItemId);
			return itemRow.AetherialReduce > 0;
		}

		public bool IsReduceOpen { get => Plugin.GameGui.GetAddonByName("PurifyItemSelector") != nint.Zero; }
		public bool IsReduceResultOpen { get => Plugin.GameGui.GetAddonByName("PurifyResult") != nint.Zero; }

		public IEnumerable ReduceTask() {
			while (ReducibleItems.Count > 0) {
				if (Plugin.Condition[ConditionFlag.Occupied39]) {
					// Currently reducing
					yield return null;
				} else if (IsReduceResultOpen) {
					Throttle.ExecuteConditional(throttle, () => {
						ReduceRemainingItems();
					});
					yield return null;
				} else if (IsReduceOpen) {
					Throttle.ExecuteConditional(throttle, () => {
						ReduceFirstItem();
					});
					yield return null;
				} else {
					Throttle.ExecuteConditional(throttle, () => {
						OpenReduce();
					});
					yield return null;
				}
			}

			while (IsReduceOpen) {
				Throttle.ExecuteConditional(throttle, () => {
					CloseReduce();
				});
				yield return null;
			}
		}

		public bool OpenReduce() {
			if (!IsReduceOpen) {
				return ToggleReduce();
			} else {
				return true;
			}
		}

		public bool CloseReduce() {
			if (IsReduceOpen) {
				return ToggleReduce();
			} else {
				return true;
			}
		}

		public bool ToggleReduce() {
			return Throttle.ExecuteConditional(throttle, () => {
				Plugin.PluginLog.Debug($"ToggleReduce");
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 21);
			});
		}

		public bool ReduceFirstItem() {
			var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("PurifyItemSelector");
			if (addon == null) return false;
			if (!addon->IsVisible) return false;

			var values = stackalloc AtkValue[2] {
				new() {
					Type = ValueType.Int,
					Int = 12,
				},
				new() {
					Type = ValueType.UInt,
					UInt = 0u,
				},
			};
			addon->FireCallback(2, values);
			return true;
		}

		private bool ReduceRemainingItems() {
			var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("PurifyResult");
			if (addon == null) return false;
			if (!addon->IsVisible) return false;

			var automaticButton = addon->GetButtonNodeById(19);
			if (!automaticButton->IsEnabled) return false;

			var resNode = automaticButton->OwnerNode->AtkResNode;
			var e = resNode.AtkEventManager.Event;
			addon->ReceiveEvent(e->State.EventType, (int)e->Param, e);
			return true;
		}
	}
}
