using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
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

		public enum RouteStateEnum {
			Idle,
			Begin,
			BeginGroup,
			BeginNode,
			ApproachNodeApprox,
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
		public RouteStateEnum State { get; set; } = RouteStateEnum.Idle;
		public DateTime Timeout { get; set; } = DateTime.MinValue;

		private ServiceInstances Services { get; init; } = services;

		public void Start(Route route) {
			if (route.Groups.Count == 0) {
				Plugin.PluginLog.Warning("Could not start route with no groups");
				return;
			}

			var now = Services.TimeService.EorzeaTime;
			var startingGroup = route.Groups.FirstOrDefault(group => group.Uptime == null || group.Uptime.Contains(now));
			startingGroup ??= route.Groups.OrderBy(group => now.TimeUntil(group.Uptime!.Start)).First();
			var startingGroupIndex = route.Groups.IndexOf(startingGroup);

			Start(route, startingGroupIndex, 0);
		}

		public void Start(Route route, int startingGroupIndex, int startingNodeIndex) {
			if (route.Groups.Count == 0) {
				return;
			}

			Route = route;
			Status = "Starting...";
			CurrentGroupIndex = startingGroupIndex;
			CurrentNodeIndex = startingNodeIndex;
			GatheredThisLoop = false;
			TargetNode = null;
			TargetPosition = null;
			State = RouteStateEnum.Begin;
		}

		public void Stop() {
			Route = null;
			Status = "Idle";
			CurrentGroupIndex = 0;
			CurrentNodeIndex = 0;
			GatheredThisLoop = false;
			TargetNode = null;
			TargetPosition = null;
			State = RouteStateEnum.Idle;

			if (Services.NavmeshIPC.IsRunning()) {
				Services.NavmeshIPC.Stop();
			}
		}

		public void Update() {
			if (Services.ActionService.IsPlayerBusy || DateTime.Now < Timeout) {
				return;
			}

			while (ExecutionEnum.Continue == State switch {
				RouteStateEnum.Idle => ExecutionEnum.Yield,
				RouteStateEnum.Begin => BeginState(),
				RouteStateEnum.BeginGroup => BeginGroupState(),
				RouteStateEnum.BeginNode => BeginNodeState(),
				RouteStateEnum.ApproachNodeApprox => ApproachNodeApproxState(),
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

			// TODO teleport to correct map
			ChangeState(RouteStateEnum.BeginNode);
			return ExecutionEnum.Continue;
		}

		private ExecutionEnum BeginNodeState() {
			// TODO check for repair
			// TODO check for spiritbond
			// TODO check for aetheric reduction

			var group = Route!.Groups[CurrentGroupIndex];
			if (CurrentNodeIndex >= group.GatheringNodes.Count) {
				ChangeState(RouteStateEnum.EndGroup);
				return ExecutionEnum.Continue;
			}

			var node = group.GatheringNodes[CurrentNodeIndex];
			TargetPosition = node.AveragePosition;
			ChangeState(RouteStateEnum.ApproachNodeApprox);
			return ExecutionEnum.Continue;
		}

		private ExecutionEnum ApproachNodeApproxState() {
			var group = Route!.Groups[CurrentGroupIndex];
			var node = group.GatheringNodes[CurrentNodeIndex];
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
			} else {
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
					Plugin.PluginLog.Debug($"Node {node.DataId:X}(#{CurrentGroupIndex}.{CurrentNodeIndex}) is down");
					// TODO wait for uptime
					// TODO disambiguate between waiting for uptime & uptime expired
					//if (group.Uptime != null && group.Uptime.Contains(Services.TimeService.EorzeaTime)) {
					//	Status = "Waiting for node to spawn...";
					//	return ExecutionEnum.Yield;
					//}
					ChangeState(RouteStateEnum.EndNode);
					return ExecutionEnum.Continue;
				}
			}
		}

		private ExecutionEnum ApproachNodeState() {
			if (!Services.NavigationService.WithinRadius(TargetPosition!.Value, InteractRadius)) {
				if (!(Services.NavmeshIPC.IsRunning() || Services.NavmeshIPC.PathfindInProgress())) {
					var fly =
						Plugin.Condition[ConditionFlag.Mounted] ||
						Plugin.Condition[ConditionFlag.Diving] ||
						Services.NavigationService.WithinRadius(TargetPosition!.Value, WalkRadius);
					Services.NavigationService.ExecutePathfind(TargetPosition!.Value, fly);
				}
				Status = "Approaching node...";
				return ExecutionEnum.Yield;
			} else {
				if (Services.NavmeshIPC.IsRunning() || Services.NavmeshIPC.PathfindInProgress()) {
					Services.NavmeshIPC.Stop();
				}
				ChangeState(RouteStateEnum.InteractNode);
				return ExecutionEnum.Continue;
			}
		}

		private ExecutionEnum InteractNodeState() {
			if (!TargetNode!.IsTargetable) {
				GatheredThisLoop = true;
				ChangeState(RouteStateEnum.EndNode);
				return ExecutionEnum.Continue;
			} else if (Plugin.Condition[ConditionFlag.Mounted]) {
				Services.ActionService.Dismount();
				Timeout = DateTime.Now.AddMilliseconds(DismountTime);
				Status = "Dismounting...";
				return ExecutionEnum.Yield;
			} else if (Plugin.Condition[ConditionFlag.NormalConditions]) {
				Services.ActionService.OpenObjectInteraction(TargetNode!);
				Status = "Interacting with node...";
				return ExecutionEnum.Yield;
			} else {
				Status = "Waiting for player to become idle...";
				return ExecutionEnum.Yield;
			}
		}

		private ExecutionEnum EndNodeState() {
			++CurrentNodeIndex;
			ChangeState(RouteStateEnum.BeginNode);
			return ExecutionEnum.Continue;
		}

		private ExecutionEnum EndGroupState() {
			if (Route!.Groups[CurrentGroupIndex].Repeat) {
				if (GatheredThisLoop) {
					// TODO make sure this works when a group times out while navigating to the first node
					CurrentNodeIndex = 0;
					GatheredThisLoop = false;
					ChangeState(RouteStateEnum.BeginGroup);
					return ExecutionEnum.Continue;
				} else {
					Plugin.PluginLog.Warning("Completed repeat group without gathering; stopping route");
					Stop();
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
				Stop();
				return ExecutionEnum.Yield;
			}
		}

		private void ChangeState(RouteStateEnum state) {
			Plugin.PluginLog.Debug($"{State} => {state}");
			State = state;
		}
	}
}
