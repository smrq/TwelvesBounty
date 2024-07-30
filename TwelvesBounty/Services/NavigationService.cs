using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Numerics;
using TwelvesBounty.IPC;

namespace TwelvesBounty.Services {
	public class NavigationService(ActionService actionService, NavmeshIPC navmeshIPC) {
		private readonly ActionService actionService = actionService;
		private readonly NavmeshIPC navmeshIPC = navmeshIPC;

		public bool ExecutePathfind(Vector3 point, bool fly) {
			// navmesh doesn't seem to execute when diving and not mounted, idk
			var flyOrDive = fly || Plugin.Condition[ConditionFlag.Diving];

			if (flyOrDive && !Plugin.Condition[ConditionFlag.Mounted]) {
				actionService.MountChocobo();
				return false;
			}

			if (fly && !Plugin.Condition[ConditionFlag.InFlight] && !Plugin.Condition[ConditionFlag.Diving]) {
				actionService.Jump();
				return false;
			}

			if (!navmeshIPC.IsReady()) {
				// wait for navmesh ready
				return false;
			}

			navmeshIPC.PathfindAndMoveTo(point, flyOrDive);
			return true;
		}

		public unsafe void Teleport(uint aetheryteId) {
			var telepo = Telepo.Instance();
			if (telepo == null) {
				throw new InvalidOperationException("Could not get reference to Telepo");
			}

			if (Plugin.ClientState.LocalPlayer == null) {
				throw new InvalidOperationException("LocalPlayer was null");
			}

			telepo->UpdateAetheryteList();

			var end = telepo->TeleportList.Last;
			for (var p = telepo->TeleportList.First; p != end; ++p) {
				if (p->AetheryteId == aetheryteId) {
					telepo->Teleport(aetheryteId, 0);
					return;
				}
			}

			throw new InvalidOperationException($"Not attuned to aetheryte {aetheryteId}");
		}

		public bool WithinRadius(Vector3 point, float radius) {
			return PlayerDistanceSquared(point) <= radius * radius;
		}

		public float PlayerDistanceSquared(Vector3 point) {
			return (point - Plugin.ClientState.LocalPlayer!.Position).LengthSquared();
		}

		public Vector3 GetInteractPosition(Vector3 objPosition, float radius) {
			//var position = NavmeshIPC.PointOnFloor(objPosition, false, radius);
			var position = navmeshIPC.NearestPoint(objPosition, radius, radius);
			if (position == null) {
				Plugin.PluginLog.Warning($"Could not find interaction point near {objPosition}");
				return objPosition;
			}
			return position.Value;
		}
	}
}
