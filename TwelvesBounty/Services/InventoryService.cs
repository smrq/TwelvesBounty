using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TwelvesBounty.Services {
	public class InventoryService(Throttle throttle) {
		private readonly Throttle throttle = throttle;

		private static readonly List<InventoryType> InventoryAll = [
			InventoryType.Inventory1,
			InventoryType.Inventory2,
			InventoryType.Inventory3,
			InventoryType.Inventory4
		];

		public IEnumerable ReduceTask() {
			yield return null;
		}

		public unsafe List<Pointer<InventoryItem>> ReducibleItems {
			get {
				return AllInventorySlots.Where(slot => IsReducible(slot)).ToList();
			}
		}

		public unsafe int EmptySlots {
			get {
				return AllInventorySlots.Count(slot => IsEmpty(slot));
			}
		}

		private unsafe IEnumerable<Pointer<InventoryItem>> AllInventorySlots {
			get {
				return InventoryAll.SelectMany(inventory => {
					var container = InventoryManager.Instance()->GetInventoryContainer(inventory);
					return Enumerable.Range(0, (int)container->Size)
						.Select(i => (Pointer<InventoryItem>)container->GetInventorySlot(i));
				});
			}
		}

		private unsafe bool IsEmpty(InventoryItem* item) {
			return item->ItemId == 0;
		}

		private unsafe bool IsReducible(InventoryItem* item) {
			if ((item->Flags & InventoryItem.ItemFlags.Collectable) == 0) {
				return false;
			}
			var itemSheet = Plugin.DataManager.GetExcelSheet<Item>()!;
			var itemRow = itemSheet.GetRow(item->ItemId);
			return itemRow.AetherialReduce > 0;
		}
	}
}
