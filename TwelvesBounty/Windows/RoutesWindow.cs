using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;

namespace TwelvesBounty.Windows;

public class RoutesWindow : Window, IDisposable {
	private Configuration Configuration { get; init; }
	private RouteManager RouteManager { get; init; }

	private Route? activeRoute = null;

	public RoutesWindow(Configuration configuration, RouteManager routeManager) : base("Twelve's Bounty###RoutesWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {
		Size = new Vector2(800, 800);
		SizeCondition = ImGuiCond.FirstUseEver;
		Configuration = configuration;
		RouteManager = routeManager;
	}

	public void Dispose() { }

	public override void Draw() {
		using var _ = ImRaii.Table("MainColumns", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp, ImGui.GetContentRegionAvail());

		ImGui.TableNextRow();

		ImGui.TableNextColumn();
		DrawRouteListColumn();

		ImGui.TableNextColumn();
		DrawRouteEditColumn();
	}

	private void DrawRouteListColumn() {
		var targetObjectId = Plugin.ClientState.LocalPlayer?.TargetObject?.DataId ?? 0;
		using (var disabled = targetObjectId == 0 ? ImRaii.Disabled() : null) {
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
			using (var disabled = activeRoute == null ? ImRaii.Disabled() : null) {
				if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Play, "Start route")) {
					RouteManager.Start(activeRoute!);
				}
			}
		}

		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add route")) {
			Configuration.Routes.Add(new Route() {
				Id = Guid.NewGuid(),
				WaypointGroups = [new WaypointGroup() {
					Waypoint = Plugin.ClientState.LocalPlayer?.Position ?? Vector3.Zero,
					MapId = Plugin.ClientState.MapId,
				}]
			});
			Configuration.Save();
		}

		DrawRouteList();
	}

	private void DrawRouteList() {
		using var _ = ImRaii.Table("RoutesList", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY);
		foreach (var route in Configuration.Routes) {
			DrawRouteListItem(route);
		}
	}

	private void DrawRouteListItem(Route route) {
		using var _ = ImRaii.PushId(route.Id.ToString());
		ImGui.TableNextRow();

		ImGui.TableNextColumn();
		ImGui.Text(route.Id.ToString().Substring(0, 8));

		ImGui.TableNextColumn();
		if (ImGui.Selectable(route.Name.IsNullOrEmpty() ? "Untitled route" : route.Name, route == activeRoute, ImGuiSelectableFlags.SpanAllColumns)) {
			activeRoute = route;
		}
	}

	private void DrawRouteEditColumn() {
		if (activeRoute == null) {
			return;
		}

		var name = activeRoute.Name;
		if (ImGui.InputText("Route Name", ref name, 256)) {
			activeRoute.Name = name;
			Configuration.Save();
		}

		ImGui.SameLine();
		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Trash, "Delete route")) {
			Configuration.Routes.Remove(activeRoute);
			Configuration.Save();
			activeRoute = null;
			return;
		}

		ImGui.Spacing();

		for (var i = 0; i < activeRoute.WaypointGroups.Count; ++i) {
			var group = activeRoute.WaypointGroups[i];
			using var _ = ImRaii.Table($"WaypointGroup_{i}", 2, ImGuiTableFlags.BordersOuter);
			ImGui.TableSetupColumn("0", ImGuiTableColumnFlags.WidthFixed);
			ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);

			ImGui.TableNextRow();
			ImGui.TableNextColumn();

			if (ImGuiComponents.IconButton("Remove", FontAwesomeIcon.Trash)) {
				activeRoute.WaypointGroups.RemoveAt(i);
				Configuration.Save();
			}
			using (var disabled = i == 0 ? ImRaii.Disabled() : null) {
				if (ImGuiComponents.IconButton("MoveUp", FontAwesomeIcon.ArrowUp)) {
					activeRoute.WaypointGroups.RemoveAt(i);
					activeRoute.WaypointGroups.Insert(i - 1, group);
					Configuration.Save();
				}
			}
			using (var disabled = i == activeRoute.WaypointGroups.Count - 1 ? ImRaii.Disabled() : null) {
				if (ImGuiComponents.IconButton("MoveDown", FontAwesomeIcon.ArrowDown)) {
					activeRoute.WaypointGroups.RemoveAt(i);
					activeRoute.WaypointGroups.Insert(i + 1, group);
					Configuration.Save();
				}
			}

			ImGui.TableNextColumn();
			using (var detailsTable = ImRaii.Table($"Details", 2, ImGuiTableFlags.SizingFixedFit)) {
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text("Waypoint");

				ImGui.TableNextColumn();
				using (var disabled = Plugin.ClientState.LocalPlayer == null ? ImRaii.Disabled() : null) {
					if (ImGuiComponents.IconButton("SetWaypoint", FontAwesomeIcon.MapMarkerAlt)) {
						group.Waypoint = Plugin.ClientState.LocalPlayer!.Position;
						group.MapId = Plugin.ClientState.MapId;
						Configuration.Save();
					}
				}
				ImGui.SameLine();
				ImGui.Text($"{group.Waypoint} @ {group.MapId}");

				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text("Nodes");
				ImGui.TableNextColumn();

				for (var j = 0; j < group.NodeObjectIds.Count; ++j) {
					var nodeId = group.NodeObjectIds[j];
					if (ImGuiComponents.IconButton($"Remove_{j}", FontAwesomeIcon.Times)) {
						group.NodeObjectIds.RemoveAt(j);
						Configuration.Save();
					}
					ImGui.SameLine();
					ImGui.Text($"{nodeId.ToString("X")}");
				}

				var targetObjectId = Plugin.ClientState.LocalPlayer?.TargetObject?.DataId ?? 0;
				using (var disabled = targetObjectId == 0 || group.NodeObjectIds.Contains(targetObjectId) ? ImRaii.Disabled() : null) {
					if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Crosshairs, targetObjectId == 0 ? "Add node" : $"Add {targetObjectId.ToString("X")}")) {
						group.NodeObjectIds.Add(targetObjectId);
						Configuration.Save();
					}
				}
			}
		}

		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add waypoint")) {
			activeRoute.WaypointGroups.Add(new WaypointGroup() {
				Waypoint = Plugin.ClientState.LocalPlayer?.Position ?? Vector3.Zero,
				MapId = Plugin.ClientState.MapId,
			});
			Configuration.Save();
		}
	}
}
