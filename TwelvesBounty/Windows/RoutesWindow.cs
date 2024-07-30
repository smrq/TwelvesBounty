using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using TwelvesBounty.Data;
using TwelvesBounty.Exec;

namespace TwelvesBounty.Windows;

public class RoutesWindow : Window, IDisposable {
	private Configuration Configuration { get; init; }
	private RouteManager RouteManager { get; init; }

	private Route? activeRoute = null;

	private readonly Vector4 activeColor = new(1.0f, 0.6f, 0.4f, 1.0f);

	public RoutesWindow(Configuration configuration, RouteManager routeManager) : base("Twelve's Bounty###RoutesWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {
		Size = new Vector2(800, 800);
		SizeCondition = ImGuiCond.FirstUseEver;
		Configuration = configuration;
		RouteManager = routeManager;
	}

	public void Dispose() { }

	public override void Draw() {
		using var tabs = ImRaii.TabBar("MainTabs");
		if (tabs) {
			using (var tab = ImRaii.TabItem("Routes")) {
				if (tab) {
					DrawRoutesTab();
				}
			}
			using (var tab = ImRaii.TabItem("Debug")) {
				if (tab) {
					DrawDebugTab();
				}
			}
		}
	}

	private void DrawRoutesTab() {
		using var table = ImRaii.Table("RoutesTab", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp, ImGui.GetContentRegionAvail());

		ImGui.TableNextRow();

		ImGui.TableNextColumn();
		DrawRouteListColumn();

		ImGui.TableNextColumn();
		DrawRouteEditColumn();
	}

	private void DrawRouteListColumn() {
		//var targetObjectId = Plugin.ClientState.LocalPlayer?.TargetObject?.DataId ?? 0;
		//using (var disabled = ImRaii.Disabled(targetObjectId == 0)) {
		//	if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Search, "Find targeted route")) {
		//		activeRoute = Configuration.Routes.Find(route =>
		//			route.Groups.Any(group =>
		//				group.MapId == Plugin.ClientState.MapId &&
		//				group.GatheringNodes.Select(node => node.DataId).Contains(targetObjectId)
		//			)
		//		);
		//	}
		//}

		using (var disabled = ImRaii.Disabled(RouteManager.Route == null)) {
			if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Stop, "Stop route")) {
				RouteManager.Stop();
			}
		}
		ImGui.SameLine();
		using (var color = ImRaii.PushColor(ImGuiCol.Text, activeColor)) {
			ImGui.Text(RouteManager.Status);
		}

		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add route")) {
			var route = new Route() {
				Id = Guid.NewGuid(),
				Groups = [new() {
					MapId = Plugin.ClientState.MapId,
				}]
			};
			Configuration.Routes.Add(route);
			Configuration.Save();
			activeRoute = route;
		}

		DrawRouteList();
	}

	private void DrawRouteList() {
		using var table = ImRaii.Table("RoutesList", 1, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY);
		var routes = Configuration.Routes.OrderBy(route => route.Name);
		foreach (var route in routes) {
			DrawRouteListItem(route);
		}
	}

	private void DrawRouteListItem(Route route) {
		using var id = ImRaii.PushId(route.Id.ToString());
		using var color = ImRaii.PushColor(ImGuiCol.Text, activeColor, route == RouteManager.Route);

		ImGui.TableNextRow();
		ImGui.TableNextColumn();

		if (ImGui.Selectable(route.Name.IsNullOrEmpty() ? "Untitled route" : route.Name, route == activeRoute, ImGuiSelectableFlags.SpanAllColumns)) {
			activeRoute = route;
		}
	}

	private void DrawRouteEditColumn() {
		if (activeRoute == null) {
			return;
		}

		using var table = ImRaii.Table("EditColumn", 1, ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp, ImGui.GetContentRegionAvail());
		ImGui.TableNextRow();
		ImGui.TableNextColumn();

		var name = activeRoute.Name;
		if (ImGui.InputText("Route Name", ref name, 256)) {
			activeRoute.Name = name;
			Configuration.Save();
		}

		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Trash, "Delete route")) {
			Configuration.Routes.Remove(activeRoute);
			Configuration.Save();
			activeRoute = null;
			return;
		}

		ImGui.Spacing();

		for (var i = 0; i < activeRoute.Groups.Count; ++i) {
			DrawRouteGroup(i);
		}

		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add group")) {
			activeRoute.Groups.Add(new () {
				MapId = Plugin.ClientState.MapId,
			});
			Configuration.Save();
		}
	}

	private void DrawRouteGroup(int i) {
		var group = activeRoute!.Groups[i];
		var isRunningRoute = RouteManager.Route == activeRoute;
		var isRunningGroup = isRunningRoute && RouteManager.CurrentGroupIndex == i;

		using var border = ImRaii.PushColor(ImGuiCol.TableBorderStrong, activeColor, isRunningGroup);
		using var table = ImRaii.Table($"WaypointGroup_{i}", 2, ImGuiTableFlags.Borders);
		ImGui.TableSetupColumn("0", ImGuiTableColumnFlags.WidthFixed);
		ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);

		ImGui.TableNextRow();
		ImGui.TableNextColumn();

		using (var disabled = ImRaii.Disabled(RouteManager.Route != null)) {
			if (ImGuiComponents.IconButton("Start", FontAwesomeIcon.Play)) {
				RouteManager.Start(activeRoute, i, 0);
			}
		}
		using (var disabled = ImRaii.Disabled(i == 0)) {
			if (ImGuiComponents.IconButton("MoveUp", FontAwesomeIcon.ArrowUp)) {
				activeRoute.Groups.RemoveAt(i);
				activeRoute.Groups.Insert(i - 1, group);
				Configuration.Save();

				if (RouteManager.CurrentGroupIndex == i) {
					RouteManager.CurrentGroupIndex = i - 1;
				} else if (RouteManager.CurrentGroupIndex == i - 1) {
					RouteManager.CurrentGroupIndex = i;
				}
			}
		}
		using (var disabled = ImRaii.Disabled(i == activeRoute.Groups.Count - 1)) {
			if (ImGuiComponents.IconButton("MoveDown", FontAwesomeIcon.ArrowDown)) {
				activeRoute.Groups.RemoveAt(i);
				activeRoute.Groups.Insert(i + 1, group);
				Configuration.Save();

				if (RouteManager.CurrentGroupIndex == i) {
					RouteManager.CurrentGroupIndex = i + 1;
				} else if (RouteManager.CurrentGroupIndex == i + 1) {
					RouteManager.CurrentGroupIndex = i;
				}
			}
		}
		using (var disabled = ImRaii.Disabled(isRunningGroup)) {
			if (ImGuiComponents.IconButton("Remove", FontAwesomeIcon.Trash)) {
				activeRoute.Groups.RemoveAt(i);
				Configuration.Save();

				if (RouteManager.CurrentGroupIndex > i) {
					--RouteManager.CurrentGroupIndex;
				}
			}
		}

		ImGui.TableNextColumn();
		using (var detailsTable = ImRaii.Table($"Details", 2, ImGuiTableFlags.SizingFixedFit)) {
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text("Map");

			ImGui.TableNextColumn();
			using (var disabled = ImRaii.Disabled(Plugin.ClientState.LocalPlayer == null)) {
				if (ImGuiComponents.IconButton("SetWaypoint", FontAwesomeIcon.MapMarkerAlt)) {
					group.MapId = Plugin.ClientState.MapId;
					Configuration.Save();
				}
			}
			ImGui.SameLine();
			ImGui.Text($"{group.MapName}");

			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text("Nodes");
			ImGui.TableNextColumn();

			for (var j = 0; j < group.GatheringNodes.Count; ++j) {
				var node = group.GatheringNodes[j];
				var isRunningNode = isRunningGroup && RouteManager.CurrentNodeIndex == j;
				if (ImGuiComponents.IconButton($"Remove_{j}", FontAwesomeIcon.Times)) {
					group.GatheringNodes.RemoveAt(j);
					Configuration.Save();
				}
				ImGui.SameLine();
				using var color = ImRaii.PushColor(ImGuiCol.Text, activeColor, isRunningNode);
				ImGui.Text($"{node.DataId:X} (x{node.Positions.Count})");
			}

			if (group.MapId == Plugin.ClientState.MapId) {
				var targetDataId = Plugin.ClientState.LocalPlayer?.TargetObject?.DataId ?? 0;
				var targetPoints = targetDataId == 0 ? [] : Plugin.ObjectTable.Where(obj => obj.DataId == targetDataId).ToList();
				using (var disabled = ImRaii.Disabled(targetDataId == 0 || group.GatheringNodes.Any(node => node.DataId == targetDataId))) {
					if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Crosshairs, targetDataId == 0 ? "Add node" : $"Add {targetDataId:X} (x{targetPoints.Count})")) {
						group.GatheringNodes.Add(new() {
							DataId = targetDataId,
							Positions = targetPoints.Select(p => p.Position).ToList(),
						});
						Configuration.Save();
					}
				}
			}
		}
	}

	private string debugDataId = "";
	private void DrawDebugTab() {
		ImGui.Text("Debug");

		var id = debugDataId;
		if (ImGui.InputText("Data ID", ref id, 16)) {
			debugDataId = id;
		}

		uint dataIdHex = 0;
		try {
			dataIdHex = Convert.ToUInt32(debugDataId, 16);
		} catch { }
		var obj = Plugin.ObjectTable
			.FirstOrDefault(obj => obj.DataId == dataIdHex && obj.IsTargetable);
		var point = obj?.Position;
		if (point != null) {
			var vec = point.Value - Plugin.ClientState.LocalPlayer!.Position;
			var dist = vec.Length();
			ImGui.Text($"XYZ Distance to target: {dist}");

			vec.Y = 0;
			var xydist = vec.Length();
			ImGui.Text($"XZ Distance to target: {xydist}");
		}
	}
}
