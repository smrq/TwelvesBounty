using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;
using System;
using System.Linq;
using System.Numerics;
using TwelvesBounty.Data;
using TwelvesBounty.Services;

namespace TwelvesBounty.Exec {
	public class RouteManager(ServiceInstances services) {
		private const float TargetableRadius = 50.0f;
		private const float InteractRadius = 2.0f;
		private const float WalkRadius = 25.0f;
		private const int DismountTime = 500;
		private const int TeleportInitTime = 6000;
		private const int BetweenAreasTime = 1500;

		public enum RouteStateEnum {
			Idle,
			Begin,
			BeginGroup,
			BeginNode,
			ApproachNodeApprox,
			AwaitingUptime,
			ApproachNode,
			InteractNode,
			EndNode,
			EndGroup,
			EndRoute,
		}

		private enum ExecutionEnum {
			Continue,
			Yield,
		}

		public Route? Route { get; set; } = null;
		public string Status { get; set; } = "Idle";
		public int CurrentGroupIndex { get; set; } = 0;
		public int CurrentNodeIndex { get; set; } = 0;
		public bool GatheredThisLoop { get; set; } = false;
		public IGameObject? TargetNode { get; set; } = null;
		public Vector3? TargetPosition { get; set; } = null;
		public bool WaitingForUptime { get; set; } = false;
		public RouteStateEnum State { get; set; } = RouteStateEnum.Idle;
		public DateTime Timeout { get; set; } = DateTime.MinValue;

		private ServiceInstances Services { get; init; } = services;

		public void Start(Route route) {
			if (route.Groups.Count == 0) {
				Plugin.PluginLog.Warning("Could not start route with no groups");
				return;
			}

			var now = Services.TimeService.EorzeaTime;
			var startingGroup = route.Groups.OrderBy(group => group.TimeUntilUptime(now)).First();
			var startingGroupIndex = route.Groups.IndexOf(startingGroup);
			Start(route, startingGroupIndex, 0);
		}

		public void Start(Route route, int startingGroupIndex, int startingNodeIndex) {
			if (route.Groups.Count == 0) {
				return;
			}

			Init();
			Route = route;
			Status = "Starting...";
			CurrentGroupIndex = startingGroupIndex;
			CurrentNodeIndex = startingNodeIndex;
			State = RouteStateEnum.Begin;
		}

		public void Stop(string status = "Idle") {
			Init();
			Status = status;
		}

		private void Init() {
			Route = null;
			Status = "Idle";
			CurrentGroupIndex = 0;
			CurrentNodeIndex = 0;
			GatheredThisLoop = false;
			TargetNode = null;
			TargetPosition = null;
			WaitingForUptime = false;
			State = RouteStateEnum.Idle;

			if (Services.NavmeshIPC.IsRunning()) {
				Services.NavmeshIPC.Stop();
			}
		}

		public void Update() {
			if (Plugin.Condition[ConditionFlag.BetweenAreas]) {
				var t = DateTime.Now.AddMilliseconds(BetweenAreasTime);
				if (t > Timeout) {
					Timeout = t;
				}
			}

			if (Services.ActionService.IsPlayerBusy || DateTime.Now < Timeout) {
				return;
			}

			while (ExecutionEnum.Continue == State switch {
				RouteStateEnum.Idle => ExecutionEnum.Yield,
				RouteStateEnum.Begin => BeginState(),
				RouteStateEnum.BeginGroup => BeginGroupState(),
				RouteStateEnum.BeginNode => BeginNodeState(),
				RouteStateEnum.ApproachNodeApprox => ApproachNodeApproxState(),
				RouteStateEnum.AwaitingUptime => AwaitingUptimeState(),
				RouteStateEnum.ApproachNode => ApproachNodeState(),
				RouteStateEnum.InteractNode => InteractNodeState(),
				RouteStateEnum.EndNode => EndNodeState(),
				RouteStateEnum.EndGroup => EndGroupState(),
				RouteStateEnum.EndRoute => EndRouteState(),
				_ => throw new NotImplementedException(),
			});
		}

		private ExecutionEnum BeginState() {
			ChangeState(RouteStateEnum.BeginGroup);
			return ExecutionEnum.Continue;
		}

		private ExecutionEnum BeginGroupState() {
			if (CurrentGroupIndex >= Route!.Groups.Count) {
				ChangeState(RouteStateEnum.EndRoute);
				return ExecutionEnum.Continue;
			}

			var group = Route!.Groups[CurrentGroupIndex];
			WaitingForUptime = !group.WithinUptime(Services.TimeService.EorzeaTime);

			ChangeState(RouteStateEnum.BeginNode);
			return ExecutionEnum.Continue;
		}

		private ExecutionEnum BeginNodeState() {
			var group = Route!.Groups[CurrentGroupIndex];
			if (CurrentNodeIndex >= group.GatheringNodes.Count) {
				ChangeState(RouteStateEnum.EndGroup);
				return ExecutionEnum.Continue;
			}

			var node = group.GatheringNodes[CurrentNodeIndex];

			var mapId = Route!.Groups[CurrentGroupIndex].MapId;
			if (Plugin.ClientState.MapId != mapId) {
				var aetheryteId = Services.NavigationService.GetNearestAetheryte(mapId, node.AveragePosition);
				if (aetheryteId == null) {
					Plugin.PluginLog.Warning("Could not find aetheryte to teleport to; stopping route");
					Stop("Failed to find aetheryte near node.");
					return ExecutionEnum.Yield;
				}

				Plugin.PluginLog.Debug($"{Plugin.ClientState.MapId} -> {mapId}");

				Services.NavigationService.Teleport(aetheryteId.Value);
				Timeout = DateTime.Now.AddMilliseconds(TeleportInitTime);
				Status = "Teleporting...";
				return ExecutionEnum.Yield;
			}

			if (!group.LinkedGearset.IsNullOrEmpty() && group.LinkedGearset != Services.GearsetService.GetEquippedGearsetName()) {
				Services.GearsetService.EquipGearset(group.LinkedGearset);
				Status = "Equipping gearset...";
				return ExecutionEnum.Yield;
			}

			// TODO check for repair
			// TODO check for spiritbond
			// TODO check for aetheric reduction

			TargetPosition = node.AveragePosition;
			ChangeState(RouteStateEnum.ApproachNodeApprox);
			return ExecutionEnum.Continue;
		}

		private ExecutionEnum ApproachNodeApproxState() {
			var group = Route!.Groups[CurrentGroupIndex];
			var node = group.GatheringNodes[CurrentNodeIndex];

			if (!group.WithinUptime(Services.TimeService.EorzeaTime) && !WaitingForUptime) {
				if (Services.NavmeshIPC.IsRunning() || Services.NavmeshIPC.PathfindInProgress()) {
					Services.NavmeshIPC.Stop();
				}
				ChangeState(RouteStateEnum.EndGroup);
				return ExecutionEnum.Continue;
			}

			var objs = Plugin.ObjectTable.Where(obj => obj.DataId == node.DataId);
			var needsApproach = !objs.Any(obj => obj.IsTargetable) && (
				objs.Count() < node.Positions.Count ||
				objs.Any(obj => !Services.NavigationService.WithinRadius(obj.Position, TargetableRadius))
			);
			if (needsApproach) {
				if (!(Services.NavmeshIPC.IsRunning() || Services.NavmeshIPC.PathfindInProgress())) {
					var fly =
						Plugin.Condition[ConditionFlag.Mounted] ||
						Plugin.Condition[ConditionFlag.Diving] ||
						!Services.NavigationService.WithinRadius(TargetPosition!.Value, WalkRadius);
					Services.NavigationService.ExecutePathfind(TargetPosition!.Value, fly);
				}
				Status = "Approaching node area...";
				return ExecutionEnum.Yield;
			}

			if (Services.NavmeshIPC.IsRunning() || Services.NavmeshIPC.PathfindInProgress()) {
				Services.NavmeshIPC.Stop();
			}
			var targetableObj = objs.FirstOrDefault(obj => obj.IsTargetable);
			if (targetableObj != null) {
				TargetNode = targetableObj;
				TargetPosition = Services.NavigationService.GetInteractPosition(targetableObj.Position, InteractRadius);
				ChangeState(RouteStateEnum.ApproachNode);
				return ExecutionEnum.Continue;
			} else {
				if (!group.WithinUptime(Services.TimeService.EorzeaTime) && WaitingForUptime) {
					ChangeState(RouteStateEnum.AwaitingUptime);
					return ExecutionEnum.Continue;
				}
				Plugin.PluginLog.Debug($"Node {node.DataId:X}(#{CurrentGroupIndex}.{CurrentNodeIndex}) is down");
				ChangeState(RouteStateEnum.EndNode);
				return ExecutionEnum.Continue;
			}
		}

		private ExecutionEnum AwaitingUptimeState() {
			var group = Route!.Groups[CurrentGroupIndex];
			if (group.WithinUptime(Services.TimeService.EorzeaTime)) {
				WaitingForUptime = false;
				ChangeState(RouteStateEnum.ApproachNodeApprox);
				return ExecutionEnum.Continue;
			}

			Status = "Waiting for node to spawn...";
			return ExecutionEnum.Yield;
		}

		private ExecutionEnum ApproachNodeState() {
			var group = Route!.Groups[CurrentGroupIndex];
			if (!group.WithinUptime(Services.TimeService.EorzeaTime)) {
				if (Services.NavmeshIPC.IsRunning() || Services.NavmeshIPC.PathfindInProgress()) {
					Services.NavmeshIPC.Stop();
				}
				ChangeState(RouteStateEnum.EndGroup);
				return ExecutionEnum.Continue;
			}

			if (!Services.NavigationService.WithinRadius(TargetPosition!.Value, InteractRadius)) {
				if (!(Services.NavmeshIPC.IsRunning() || Services.NavmeshIPC.PathfindInProgress())) {
					var fly =
						Plugin.Condition[ConditionFlag.Mounted] ||
						Plugin.Condition[ConditionFlag.Diving] ||
						!Services.NavigationService.WithinRadius(TargetPosition!.Value, WalkRadius);
					Services.NavigationService.ExecutePathfind(TargetPosition!.Value, fly);
				}
				Status = "Approaching node...";
				return ExecutionEnum.Yield;
			}

			if (Services.NavmeshIPC.IsRunning() || Services.NavmeshIPC.PathfindInProgress()) {
				Services.NavmeshIPC.Stop();
			}
			ChangeState(RouteStateEnum.InteractNode);
			return ExecutionEnum.Continue;
		}

		private ExecutionEnum InteractNodeState() {
			// TODO use cordials
			if (!TargetNode!.IsTargetable) {
				GatheredThisLoop = true;
				ChangeState(RouteStateEnum.EndNode);
				return ExecutionEnum.Continue;
			}
			
			if (Plugin.Condition[ConditionFlag.Mounted]) {
				Services.ActionService.Dismount();
				Timeout = DateTime.Now.AddMilliseconds(DismountTime);
				Status = "Dismounting...";
				return ExecutionEnum.Yield;
			}
			
			if (Plugin.Condition[ConditionFlag.NormalConditions]) {
				Services.ActionService.OpenObjectInteraction(TargetNode!);
				Status = "Interacting with node...";
				return ExecutionEnum.Yield;
			}

			Status = "Waiting for player to become idle...";
			return ExecutionEnum.Yield;
		}

		private ExecutionEnum EndNodeState() {
			++CurrentNodeIndex;
			ChangeState(RouteStateEnum.BeginNode);
			return ExecutionEnum.Continue;
		}

		private ExecutionEnum EndGroupState() {
			var group = Route!.Groups[CurrentGroupIndex];
			var withinUptime = group.WithinUptime(Services.TimeService.EorzeaTime);
			if (group.Repeat && withinUptime) {
				if (GatheredThisLoop) {
					CurrentNodeIndex = 0;
					GatheredThisLoop = false;
					ChangeState(RouteStateEnum.BeginGroup);
					return ExecutionEnum.Continue;
				} else {
					Plugin.PluginLog.Warning("Completed repeat group without gathering; stopping route");
					Stop("Failed to find any nodes.");
					return ExecutionEnum.Yield;
				}
			} else {
				CurrentNodeIndex = 0;
				++CurrentGroupIndex;
				ChangeState(RouteStateEnum.BeginGroup);
				return ExecutionEnum.Continue;
			}
		}

		private ExecutionEnum EndRouteState() {
			if (GatheredThisLoop) {
				CurrentGroupIndex = 0;
				GatheredThisLoop = false;
				ChangeState(RouteStateEnum.BeginGroup);
				return ExecutionEnum.Continue;
			} else {
				Plugin.PluginLog.Warning("Completed loop without gathering; stopping route");
				Stop("Failed to find any nodes.");
				return ExecutionEnum.Yield;
			}
		}

		private void ChangeState(RouteStateEnum state) {
			Plugin.PluginLog.Debug($"{State} => {state}");
			State = state;
		}
	}
}
