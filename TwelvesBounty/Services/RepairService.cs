using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TwelvesBounty.Services {
	public unsafe class RepairService(Throttle throttle) {
		private readonly Throttle throttle = throttle;

		public List<ushort?> EquippedCondition {
			get {
				var equipped = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
				return Enumerable.Range(0, (int)equipped->Size)
					.Select<int, ushort?>(n => {
						var item = equipped->GetInventorySlot(n);
						if (item != null && item->ItemId > 0) {
							return item->Condition;
						}
						return null;
					})
					.ToList();
			}
		}

		public bool CanRepair {
			get {
				var equipped = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
				return Enumerable.Range(0, (int)equipped->Size).Any(n => {
					var item = equipped->GetInventorySlot(n);
					return item != null &&
						item->ItemId > 0 &&
						item->Condition <= 30000 &&
						CanRepairItem(item->ItemId);
				});
			}
		}

		public bool IsRepairOpen { get => Plugin.GameGui.GetAddonByName("Repair") != nint.Zero; }

		private bool CanRepairItem(uint itemId) {
			var itemSheet = Plugin.DataManager.GetExcelSheet<Item>()!;
			var classJobSheet = Plugin.DataManager.GetExcelSheet<ClassJob>()!;

			var itemRow = itemSheet.GetRow(itemId);
			// ?? throw new InvalidOperationException($"Invalid item id {itemId}");

			var jobId = itemRow.ClassJobRepair.RowId;
			if (jobId == 0) {
				return false;
			}

			var jobRow = classJobSheet.GetRow(itemRow.ClassJobRepair.RowId);
			// ?? throw new InvalidOperationException($"Invalid job id {jobId}");
			var jobLevel = PlayerState.Instance()->ClassJobLevels[jobRow.ExpArrayIndex];
			if (Math.Max(itemRow.LevelEquip - 10, 1) > jobLevel) {
				return false;
			}

			var repairItem = itemRow.ItemRepair.Value!.Item;
			if (!HasDarkMatter(repairItem.RowId)) {
				return false;
			}

			return true;
		}

		private bool HasDarkMatter(uint minimumId) {
			var repairSheet = Plugin.DataManager.GetExcelSheet<ItemRepairResource>()!;
			return repairSheet.Any(row =>
				row.Item.RowId >= minimumId &&
				InventoryManager.Instance()->GetInventoryItemCount(row.Item.RowId) > 0
			);
		}

		public IEnumerable RepairTask() {
			while (CanRepair) {
				if (!IsRepairOpen) {
					Throttle.ExecuteConditional(throttle, () => {
						OpenRepair();
					});
					yield return null;
				} else {
					Throttle.ExecuteConditional(throttle, () => {
						RepairAll();
					});
					yield return null;
				}
			}

			while (IsRepairOpen) {
				Throttle.ExecuteConditional(throttle, () => {
					CloseRepair();
				});
				yield return null;
			}
		}

		private bool OpenRepair() {
			if (!IsRepairOpen) {
				return ToggleRepair();
			} else {
				return true;
			}
		}

		private bool CloseRepair() {
			if (IsRepairOpen) {
				return ToggleRepair();
			} else {
				return true;
			}
		}

		private bool ToggleRepair() {
			return Throttle.ExecuteConditional(throttle, () => {
				Plugin.PluginLog.Debug($"ToggleRepair");
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 6);
			});
		}

		private bool RepairAll() {
			var addon = (AddonRepair*)Plugin.GameGui.GetAddonByName("Repair");
			if (addon == null) return false;
			if (!addon->AtkUnitBase.IsVisible) return false;
			if (!addon->RepairAllButton->IsEnabled) return false;

			var resNode = addon->RepairAllButton->OwnerNode->AtkResNode;
			var e = resNode.AtkEventManager.Event;
			addon->ReceiveEvent(e->State.EventType, (int)e->Param, e);
			return true;
		}
	}
}
