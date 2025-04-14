using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TwelvesBounty.Data;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace TwelvesBounty.Services {
	public unsafe class GatheringService : IDisposable {
		private readonly ActionService actionService;

		public bool IsGatheringOpen { get => Plugin.GameGui.GetAddonByName("Gathering") != nint.Zero; }
		public uint LastGatheredId { get; private set; } = 0;

		public GatheringService(ActionService actionService) {
			this.actionService = actionService;
			Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "Gathering", OnGatheringEvent);
		}

		public void Dispose() {
			Plugin.AddonLifecycle.UnregisterListener(OnGatheringEvent);
		}

		public List<uint> GatherItemIds {
			get {
				var addon = (AddonGathering*)Plugin.GameGui.GetAddonByName("Gathering");
				if (addon == null) return [];
				var ids = Enumerable.Range(0, 8)
					.Select(n => addon->AtkValues[(n * 11) + 7].UInt)
					.ToList();
				return ids;
			}
		}

		public void Gather(uint itemId, RotationType rotationType) {
			if (Plugin.Condition[ConditionFlag.Gathering42]) {
				// Currently gathering
				return;
			} else {
				switch (rotationType) {
					case RotationType.External:
						return;

					case RotationType.NoGP:
						if (itemId == 0) return;
						GatherItemId(itemId);
						return;

					case RotationType.BountifulBlessed:
						if (itemId == 0) return;
						if (!actionService.IsYieldUp500Active && Plugin.ClientState.LocalPlayer!.CurrentGp >= 500) {
							actionService.UseYieldUp500();
						} else if (!actionService.IsYieldUp100Active && Plugin.ClientState.LocalPlayer!.CurrentGp >= 100) {
							actionService.UseYieldUp100();
						} else {
							GatherItemId(itemId);
						}
						return;

					default: throw new NotImplementedException();
				}
			}
		}

		public bool GatherItemId(uint id) {
			var index = GatherItemIds.IndexOf(id);
			if (index == -1) return false;
			return GatherIndex(index);
		}

		public bool GatherIndex(int index) {
			var addon = (AddonGathering*)Plugin.GameGui.GetAddonByName("Gathering");
			if (addon == null) return false;
			if (!addon->AtkUnitBase.IsVisible) return false;
			var checkbox = addon->GatheredItemComponentCheckbox[index].Value;
			if (checkbox == null) return false;
			if (!checkbox->IsEnabled) return false;

			var values = stackalloc AtkValue[1] {
				new() {
					Type = ValueType.Int,
					Int = index,
				},
			};
			addon->FireCallback(1, values);
			return true;
		}

		private unsafe void OnGatheringEvent(AddonEvent type, AddonArgs args) {
			if (args is AddonReceiveEventArgs a &&
				a.AtkEventType == (byte)AtkEventType.ButtonClick
			) {
				var addon = (AddonGathering*)a.Addon;
				if (addon == null) return;
				var index = a.EventParam;
				LastGatheredId = addon->AtkValues[(index * 11) + 7].UInt;
			}
		}
	}
}
