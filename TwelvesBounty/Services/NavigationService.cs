using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TwelvesBounty.IPC;

namespace TwelvesBounty.Services {
	public class NavigationService(ActionService actionService, NavmeshIPC navmeshIPC) {
		private readonly ActionService actionService = actionService;
		private readonly NavmeshIPC navmeshIPC = navmeshIPC;

		public IEnumerable ExecutePathfindTask(Vector3 point, bool fly) {
			// navmesh doesn't seem to execute when diving and not mounted, idk
			var flyOrDive = fly || Plugin.Condition[ConditionFlag.Diving];

			while (true) {
				if (flyOrDive && !Plugin.Condition[ConditionFlag.Mounted]) {
					actionService.MountChocobo();
					yield return null;
				} else if (fly && !Plugin.Condition[ConditionFlag.InFlight] && !Plugin.Condition[ConditionFlag.Diving]) {
					actionService.Jump();
					yield return null;
				} else if (!navmeshIPC.IsReady()) {
					// wait for navmesh ready
					yield return null;
				} else {
					navmeshIPC.PathfindAndMoveTo(point, flyOrDive);
					yield break;
				}
			}
		}

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

		private readonly List<uint> aetheryteBlacklist = [
			173, // Tertium
		];

		public uint? GetNearestAetheryte(uint mapId, Vector3 point) {
			var aetheryteSheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>();
			var mapSheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Map>();
			var mapMarkerSheet = Plugin.DataManager.GetSubrowExcelSheet<Lumina.Excel.Sheets.MapMarker>();

			var mapRow = mapSheet.GetRow(mapId);
			var aetherytes = aetheryteSheet.Where(a =>
				a.IsAetheryte &&
				a.RowId > 1 &&
				a.Territory.Value.Map.RowId == mapId &&
				!aetheryteBlacklist.Contains(a.RowId));
			var nearest = aetherytes
				.OrderBy(aetheryte => {
					var marker = mapMarkerSheet.SelectMany(m => m).FirstOrDefault(m => m.DataType == 3 && m.DataKey.RowId == aetheryte.RowId);
					// ?? throw new InvalidOperationException($"Could not find map marker for {aetheryte.RowId}");
					var position = new Vector3(
						MarkerToWorldCoordinate(marker.X, mapRow.SizeFactor, mapRow.OffsetX),
						0,
						MarkerToWorldCoordinate(marker.Y, mapRow.SizeFactor, mapRow.OffsetY)
					);
					return (position - point).LengthSquared();
				})
				.First();
			return nearest.RowId;
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
					Plugin.PluginLog.Debug($"Teleporting to {aetheryteId}");
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

		private static float MarkerToWorldCoordinate(float coord, float scale, float offset) {
			return ((coord - 1024f) / (scale / 100f)) - (offset * (scale / 100f));
		}
	}
}
