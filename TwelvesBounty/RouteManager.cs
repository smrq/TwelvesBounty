using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TwelvesBounty {
	public class RouteManager(Configuration configuration, IPC.Navmesh navmeshIPC) {
		private class RouteState(Route route) {
			public Route Route { get; init; } = route;
			public int CurrentWaypointGroup { get; set; } = 0;
			public int LastSuccessfulWaypointGroup { get; set; } = -1;
			public IGameObject? TargetNode { get; set; } = null;
			public HashSet<ulong> VisitedNodes { get; set; } = [];
			public RouteStateEnum State { get; set; } = RouteStateEnum.StartWaypoint;
		}

		private enum RouteStateEnum {
			StartWaypoint,
			StartNavigatingToWaypoint,
			NavigatingToWaypoint,
			StartNavigatingToNode,
			NavigatingToNode,
			InteractingWithNode,
			NextNode,
			NextWaypoint,
		}

		private Configuration Configuration { get; init; } = configuration;
		private IPC.Navmesh NavmeshIPC { get; init; } = navmeshIPC;
		private Throttle Throttle { get; init; } = new Throttle();

		private RouteState? State { get; set; } = null;
		
		public bool Running {
			get {
				return State != null;
			}
		}

		public Route? RunningRoute {
			get {
				return State?.Route;
			}
		}

		public int? RunningWaypointGroup {
			get {
				return State?.CurrentWaypointGroup;
			}
		}

		public ulong? RunningNode {
			get {
				return State?.TargetNode?.DataId;
			}
		}

		public void Start(Route route) {
			State = new RouteState(route);
		}

		public void Stop() {
			State = null;
		}

		public void Update() {
			if (State == null) {
				if (NavmeshIPC.IsRunning()) {
					NavmeshIPC.Stop();
				}
			} else {
				if (Plugin.ClientState.LocalPlayer == null || IsBusy()) {
					return;
				}

				var group = State.Route.WaypointGroups[State.CurrentWaypointGroup];

				switch (State.State) {
					case RouteStateEnum.StartWaypoint:
						{
							var nearest = NearestUnvisitedNode();
							if (nearest != null) {
								State.TargetNode = nearest;
								State.State = RouteStateEnum.StartNavigatingToNode;
								Plugin.PluginLog.Debug($"StartNavigatingToNode {nearest.DataId.ToString("X")} {nearest.Position}");
							} else {
								State.State = RouteStateEnum.StartNavigatingToWaypoint;
								Plugin.PluginLog.Debug($"StartNavigatingToWaypoint {group.Waypoint}");
							}
							break;
						}

					case RouteStateEnum.StartNavigatingToWaypoint:
						if (ExecutePathfind(group.Waypoint, true)) {
							State.State = RouteStateEnum.NavigatingToWaypoint;
							Plugin.PluginLog.Debug($"NavigatingToWaypoint {group.Waypoint}");
						}
						break;

					case RouteStateEnum.NavigatingToWaypoint:
						{
							var nearest = NearestUnvisitedNode();
							if (nearest != null) {
								if (NavmeshIPC.IsRunning()) {
									NavmeshIPC.Stop();
								}
								State.TargetNode = nearest;
								State.State = RouteStateEnum.StartNavigatingToNode;
								Plugin.PluginLog.Debug($"StartNavigatingToNode {nearest.DataId.ToString("X")} {nearest.Position}");
							} else if (WithinRadius(group.Waypoint, Configuration.WaypointRadius)) {
								if (NavmeshIPC.IsRunning()) {
									NavmeshIPC.Stop();
								}
								State.State = RouteStateEnum.NextWaypoint;
								Plugin.PluginLog.Debug($"NextWaypoint");
							}
							//else {
							//	if (!NavmeshIPC.IsRunning()) {
							//		State.State = RouteStateEnum.NextWaypoint;
							//		Plugin.PluginLog.Debug($"NextWaypoint");
							//	}
							//}
							break;
						}

					case RouteStateEnum.StartNavigatingToNode:
						if (ExecutePathfind(State.TargetNode!.Position, !WithinRadius(State.TargetNode!.Position, Configuration.WalkRadius))) {
							State.State = RouteStateEnum.NavigatingToNode;
							Plugin.PluginLog.Debug($"NavigatingToNode {State.TargetNode!.DataId.ToString("X")} {State.TargetNode!.Position}");
						}
						break;

					case RouteStateEnum.NavigatingToNode:
						if (WithinRadius(State.TargetNode!.Position, Configuration.NodeRadius)) {
							if (NavmeshIPC.IsRunning()) {
								NavmeshIPC.Stop();
							}
							State.State = RouteStateEnum.InteractingWithNode;
							Plugin.PluginLog.Debug($"InteractingWithNode {State.TargetNode!.DataId.ToString("X")} {State.TargetNode!.Position}");
						}
						//else {
						//	if (!NavmeshIPC.IsRunning()) {
						//		Plugin.PluginLog.Warning($"Failed to navigate to node {State.TargetNode!.DataId.ToString("X")} at {State.TargetNode!.Position}");
						//		State.State = RouteStateEnum.NextNode;
						//	}
						//}
						break;

					case RouteStateEnum.InteractingWithNode:
						if (!State.TargetNode!.IsTargetable) {
							State.VisitedNodes.Add(State.TargetNode!.DataId);
							State.LastSuccessfulWaypointGroup = State.CurrentWaypointGroup;
							State.State = RouteStateEnum.NextNode;
							Plugin.PluginLog.Debug($"NextNode");
						} else if (!Plugin.Condition[ConditionFlag.NormalConditions]) {
							ExecuteDismount();
						} else {
							ExecuteInteract(State.TargetNode!);
						}
						break;

					case RouteStateEnum.NextNode:
						{
							var nearest = NearestUnvisitedNode();
							if (nearest != null) {
								State.TargetNode = nearest;
								State.State = RouteStateEnum.StartNavigatingToNode;
								Plugin.PluginLog.Debug($"StartNavigatingToNode {nearest.DataId.ToString("X")} {nearest.Position}");
							} else {
								State.State = RouteStateEnum.NextWaypoint;
								Plugin.PluginLog.Debug($"NextWaypoint");
							}
							break;
						}

					case RouteStateEnum.NextWaypoint:
						if (State.CurrentWaypointGroup == State.LastSuccessfulWaypointGroup && !State.VisitedNodes.Any()) {
							Plugin.PluginLog.Warning("Could not find any more nodes; stopping");
							Stop();
						} else {
							State.CurrentWaypointGroup = (State.CurrentWaypointGroup + 1) % State.Route.WaypointGroups.Count;
							State.VisitedNodes = [];
							State.State = RouteStateEnum.StartWaypoint;
							Plugin.PluginLog.Debug($"StartWaypoint {State.CurrentWaypointGroup}");
						}
						break;
				}
			}
		}
		private bool ExecutePathfind(Vector3 point, bool fly) {
			if (fly) {
				if (!Plugin.Condition[ConditionFlag.Mounted]) {
					ExecuteMount();
					return false;
				}
				if (!Plugin.Condition[ConditionFlag.InFlight] && !Plugin.Condition[ConditionFlag.Diving]) {
					ExecuteJump();
					return false;
				}
			}

			if (!NavmeshIPC.IsReady()) {
				// wait for navmesh ready
				return false;
			}
			
			NavmeshIPC.PathfindAndMoveTo(point, fly);
			return true;
		}

		private unsafe bool ExecuteMount() {
			return Throttle.Execute(() => {
				Plugin.PluginLog.Debug($"ExecuteMount");
				ActionManager.Instance()->UseAction(ActionType.Mount, 1); // Chocobo
			});
		}

		private unsafe bool ExecuteJump() {
			return Throttle.Execute(() => {
				Plugin.PluginLog.Debug($"ExecuteJump");
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2);
			});
		}

		private unsafe bool ExecuteDismount() {
			return Throttle.Execute(() => {
				Plugin.PluginLog.Debug($"ExecuteDismount");
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 23);
			});
		}

		private unsafe bool ExecuteInteract(IGameObject obj) {
			return Throttle.Execute(() => {
				Plugin.PluginLog.Debug($"ExecuteInteract {obj.DataId.ToString("X")} {obj.Position}");
				TargetSystem.Instance()->OpenObjectInteraction((GameObject*)obj.Address);
			});
		}

		private static bool WithinRadius(Vector3 point, float radius) {
			return PlayerDistanceSquared(point) <= radius * radius;
		}

		private static float PlayerDistanceSquared(Vector3 point) {
			return (point - Plugin.ClientState.LocalPlayer!.Position).LengthSquared();
		}

		private static bool IsBusy() {
			return Plugin.ClientState.LocalPlayer!.IsCasting
				|| Plugin.Condition[ConditionFlag.Occupied]
				|| Plugin.Condition[ConditionFlag.Occupied30]
				|| Plugin.Condition[ConditionFlag.Occupied33]
				|| Plugin.Condition[ConditionFlag.Occupied38]
				|| Plugin.Condition[ConditionFlag.Occupied39]
				|| Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent]
				|| Plugin.Condition[ConditionFlag.OccupiedInEvent]
				|| Plugin.Condition[ConditionFlag.OccupiedInQuestEvent]
				|| Plugin.Condition[ConditionFlag.OccupiedSummoningBell]
				|| Plugin.Condition[ConditionFlag.WatchingCutscene]
				|| Plugin.Condition[ConditionFlag.WatchingCutscene78]
				|| Plugin.Condition[ConditionFlag.BetweenAreas]
				|| Plugin.Condition[ConditionFlag.BetweenAreas51]
				|| Plugin.Condition[ConditionFlag.InThatPosition]
				|| Plugin.Condition[ConditionFlag.TradeOpen]
				|| Plugin.Condition[ConditionFlag.Crafting]
				|| Plugin.Condition[ConditionFlag.Crafting40]
				|| Plugin.Condition[ConditionFlag.PreparingToCraft]
				|| Plugin.Condition[ConditionFlag.InThatPosition]
				|| Plugin.Condition[ConditionFlag.Unconscious]
				|| Plugin.Condition[ConditionFlag.MeldingMateria]
				|| Plugin.Condition[ConditionFlag.Gathering]
				|| Plugin.Condition[ConditionFlag.OperatingSiegeMachine]
				|| Plugin.Condition[ConditionFlag.CarryingItem]
				|| Plugin.Condition[ConditionFlag.CarryingObject]
				|| Plugin.Condition[ConditionFlag.BeingMoved]
				|| Plugin.Condition[ConditionFlag.Mounted2]
				|| Plugin.Condition[ConditionFlag.Mounting]
				|| Plugin.Condition[ConditionFlag.Mounting71]
				|| Plugin.Condition[ConditionFlag.ParticipatingInCustomMatch]
				|| Plugin.Condition[ConditionFlag.PlayingLordOfVerminion]
				|| Plugin.Condition[ConditionFlag.ChocoboRacing]
				|| Plugin.Condition[ConditionFlag.PlayingMiniGame]
				|| Plugin.Condition[ConditionFlag.Performing]
				|| Plugin.Condition[ConditionFlag.PreparingToCraft]
				|| Plugin.Condition[ConditionFlag.Fishing]
				|| Plugin.Condition[ConditionFlag.Transformed]
				|| Plugin.Condition[ConditionFlag.UsingHousingFunctions]
				|| Plugin.ClientState.LocalPlayer!.IsTargetable != true
				|| Plugin.Condition[ConditionFlag.Unknown57]; // condition 57 is set while mount up animation is playing
		}

		private IGameObject? NearestUnvisitedNode() {
			if (State == null) {
				return null;
			}
			var group = State.Route.WaypointGroups[State.CurrentWaypointGroup];
			return Plugin.ObjectTable
				.Where(obj =>
					obj.IsTargetable &&
					!State.VisitedNodes.Contains(obj.DataId) &&
					group.NodeObjectIds.Contains(obj.DataId))
				.OrderBy(obj => (obj.Position - Plugin.ClientState.LocalPlayer!.Position).LengthSquared())
				.FirstOrDefault();
		}
	}
}
