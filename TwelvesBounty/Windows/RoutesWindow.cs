using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

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
		var targetObjectId = Plugin.ClientState.LocalPlayer?.TargetObject?.DataId ?? 0;
		using (var disabled = ImRaii.Disabled(targetObjectId == 0)) {
			if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Search, "Find targeted route")) {
				activeRoute = Configuration.Routes.Find(route =>
					route.WaypointGroups.Any(group =>
						group.MapId == Plugin.ClientState.MapId &&
						group.NodeObjectIds.Contains(targetObjectId)
					)
				);
			}
		}

		if (RouteManager.Running) {
			if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Stop, "Stop route")) {
				RouteManager.Stop();
			}
		} else {
			using var disabled = ImRaii.Disabled(activeRoute == null);
			if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Play, "Start route")) {
				RouteManager.Start(activeRoute!);
			}
		}

		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add route")) {
			var route = new Route() {
				Id = Guid.NewGuid(),
				WaypointGroups = [new WaypointGroup() {
					Waypoint = Plugin.ClientState.LocalPlayer?.Position ?? Vector3.Zero,
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
		using var table = ImRaii.Table("RoutesList", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY);
		var routes = Configuration.Routes
			.OrderBy(route => route.MapName)
			.ThenBy(route => route.Name);
		foreach (var route in routes) {
			DrawRouteListItem(route);
		}
	}

	private void DrawRouteListItem(Route route) {
		using var id = ImRaii.PushId(route.Id.ToString());
		using var color = ImRaii.PushColor(ImGuiCol.Text, activeColor, RouteManager.RunningRoute == route);

		ImGui.TableNextRow();

		ImGui.TableNextColumn();
		ImGui.Text(route.MapName);

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

		for (var i = 0; i < activeRoute.WaypointGroups.Count; ++i) {

			DrawRouteWaypointGroup(i);
		}

		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add waypoint")) {
			activeRoute.WaypointGroups.Add(new WaypointGroup() {
				Waypoint = Plugin.ClientState.LocalPlayer?.Position ?? Vector3.Zero,
				MapId = Plugin.ClientState.MapId,
			});
			Configuration.Save();
		}
	}

	private void DrawRouteWaypointGroup(int i) {
		var group = activeRoute!.WaypointGroups[i];
		var isRunningRoute = RouteManager.RunningRoute == activeRoute;
		var isRunningGroup = isRunningRoute && RouteManager.RunningWaypointGroup == i;

		using var border = ImRaii.PushColor(ImGuiCol.TableBorderStrong, activeColor, isRunningGroup);
		using var table = ImRaii.Table($"WaypointGroup_{i}", 2, ImGuiTableFlags.Borders);
		ImGui.TableSetupColumn("0", ImGuiTableColumnFlags.WidthFixed);
		ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);

		ImGui.TableNextRow();
		ImGui.TableNextColumn();

		using (var disabled = ImRaii.Disabled(RouteManager.Running)) {
			if (ImGuiComponents.IconButton("Start", FontAwesomeIcon.Play)) {
				RouteManager.Start(activeRoute, i);
			}
		}
		using (var disabled = ImRaii.Disabled(i == 0)) {
			if (ImGuiComponents.IconButton("MoveUp", FontAwesomeIcon.ArrowUp)) {
				activeRoute.WaypointGroups.RemoveAt(i);
				activeRoute.WaypointGroups.Insert(i - 1, group);
				Configuration.Save();
			}
		}
		using (var disabled = ImRaii.Disabled(i == activeRoute.WaypointGroups.Count - 1)) {
			if (ImGuiComponents.IconButton("MoveDown", FontAwesomeIcon.ArrowDown)) {
				activeRoute.WaypointGroups.RemoveAt(i);
				activeRoute.WaypointGroups.Insert(i + 1, group);
				Configuration.Save();
			}
		}
		if (ImGuiComponents.IconButton("Remove", FontAwesomeIcon.Trash)) {
			activeRoute.WaypointGroups.RemoveAt(i);
			Configuration.Save();
		}

		ImGui.TableNextColumn();
		using (var detailsTable = ImRaii.Table($"Details", 2, ImGuiTableFlags.SizingFixedFit)) {
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text("Waypoint");

			ImGui.TableNextColumn();
			using (var disabled = ImRaii.Disabled(Plugin.ClientState.LocalPlayer == null)) {
				if (ImGuiComponents.IconButton("SetWaypoint", FontAwesomeIcon.MapMarkerAlt)) {
					group.Waypoint = Plugin.ClientState.LocalPlayer!.Position;
					group.MapId = Plugin.ClientState.MapId;
					Configuration.Save();
				}
			}
			ImGui.SameLine();
			using (var color = ImRaii.PushColor(ImGuiCol.Text, activeColor, isRunningGroup && RouteManager.RunningNode == null)) {
				ImGui.Text($"{group.Waypoint} @ {group.MapName}");
			}

			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text("Nodes");
			ImGui.TableNextColumn();

			for (var j = 0; j < group.NodeObjectIds.Count; ++j) {
				var nodeId = group.NodeObjectIds[j];
				var isRunningNode = isRunningGroup && RouteManager.RunningNode == nodeId;
				if (ImGuiComponents.IconButton($"Remove_{j}", FontAwesomeIcon.Times)) {
					group.NodeObjectIds.RemoveAt(j);
					Configuration.Save();
				}
				ImGui.SameLine();
				using var color = ImRaii.PushColor(ImGuiCol.Text, activeColor, isRunningNode);
				ImGui.Text($"{nodeId.ToString("X")}");
			}

			var targetObjectId = Plugin.ClientState.LocalPlayer?.TargetObject?.DataId ?? 0;
			using (var disabled = ImRaii.Disabled(targetObjectId == 0 || group.NodeObjectIds.Contains(targetObjectId))) {
				if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Crosshairs, targetObjectId == 0 ? "Add node" : $"Add {targetObjectId.ToString("X")}")) {
					group.NodeObjectIds.Add(targetObjectId);
					Configuration.Save();
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
