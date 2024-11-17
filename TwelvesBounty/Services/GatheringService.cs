using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace TwelvesBounty.Services {
	public unsafe class GatheringService : IDisposable {
		public bool IsGatheringOpen { get => Plugin.GameGui.GetAddonByName("Gathering") != nint.Zero; }
		public uint LastGatheredId { get; private set; } = 0;

		public GatheringService() {
			Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "Gathering", OnGatheringEvent);
		}

		public void Dispose() {
			Plugin.AddonLifecycle.UnregisterListener(OnGatheringEvent);
		}

		public List<uint> Debug {
			get {
				var addon = (AddonGathering*)Plugin.GameGui.GetAddonByName("Gathering");
				if (addon == null) return [];
				var ids = Enumerable.Range(0, 8)
					.Select(n => addon->AtkValues[(n * 11) + 7].UInt)
					.ToList();
				return ids;
			}
		}

		public IEnumerable GatherTask() {
			while (IsGatheringOpen) {
				Gather();
				yield return null;
			}
		}

		private void Gather() {
			
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
