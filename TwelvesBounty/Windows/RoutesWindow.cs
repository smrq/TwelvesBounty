using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using TwelvesBounty.Data;
using TwelvesBounty.Exec;
using TwelvesBounty.Services;

namespace TwelvesBounty.Windows;

public class RoutesWindow : Window, IDisposable {
	private Configuration Configuration { get; init; }
	private RouteManager RouteManager { get; init; }
	private ServiceInstances Services { get; init; }

	private Route? activeRoute = null;

	private readonly Vector4 activeColor = new(1.0f, 0.6f, 0.4f, 1.0f);
	private string confirmationId = string.Empty;

	public RoutesWindow(Configuration configuration, RouteManager routeManager, ServiceInstances services) : base("Twelve's Bounty##RoutesWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {
		Size = new Vector2(800, 800);
		SizeCondition = ImGuiCond.FirstUseEver;
		Configuration = configuration;
		RouteManager = routeManager;
		Services = services;
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

	private string confirmState = string.Empty;
	private void DrawRouteListColumn() {
		if (RouteManager.Route == null) {
			using var disabled = ImRaii.Disabled(activeRoute == null);
			if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Play, "Start route")) {
				RouteManager.Start(activeRoute!);
			}
		} else {
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
					LinkedGearset = Services.GearsetService.GetEquippedGearsetName(),
				}]
			};
			Configuration.Routes.Add(route);
			Configuration.Save();
			activeRoute = route;
		}

		ImGui.SameLine();

		using (var disabled = ImRaii.Disabled(activeRoute == null)) {
			if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, "Duplicate route")) {
				var route = activeRoute!.Clone();
				Configuration.Routes.Add(route);
				Configuration.Save();
				activeRoute = route;
			}
		}

		ImGui.SameLine();
		using (var disabled = ImRaii.Disabled(activeRoute == null || activeRoute == RouteManager.Route)) {
			if (confirmationId == "Delete route") {
				if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Trash, "Confirm delete")) {
					Configuration.Routes.Remove(activeRoute!);
					activeRoute = null;
					Configuration.Save();
					confirmationId = string.Empty;
				} else if (!ImGui.IsItemHovered()) {
					confirmationId = string.Empty;
				}
			} else {
				if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Trash, "Delete route")) {
					confirmationId = "Delete route";
				}
			}
		}

		using (var table = ImRaii.Table("RoutesList", 1, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY)) {
			var routes = Configuration.Routes.OrderBy(route => route.Name);
			foreach (var route in routes) {
				using var id = ImRaii.PushId(route.Id.ToString());
				using var color = ImRaii.PushColor(ImGuiCol.Text, activeColor, route == RouteManager.Route);

				ImGui.TableNextRow();
				ImGui.TableNextColumn();

				if (ImGui.Selectable(route.Name.IsNullOrEmpty() ? "Untitled route" : route.Name, route == activeRoute, ImGuiSelectableFlags.SpanAllColumns)) {
					activeRoute = route;
				}
			}
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

		ImGui.Spacing();

		for (var i = 0; i < activeRoute.Groups.Count; ++i) {
			DrawRouteGroup(i);
		}

		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add group")) {
			activeRoute.Groups.Add(new () {
				MapId = Plugin.ClientState.MapId,
				LinkedGearset = Services.GearsetService.GetEquippedGearsetName(),
			});
			Configuration.Save();
		}
	}

	private void DrawRouteGroup(int i) {
		var group = activeRoute!.Groups[i];
		var isRunningRoute = RouteManager.Route == activeRoute;
		var isRunningGroup = isRunningRoute && RouteManager.CurrentGroupIndex == i;

		using var border = ImRaii.PushColor(ImGuiCol.TableBorderStrong, activeColor, isRunningGroup);
		using var table = ImRaii.Table($"WaypointGroup_{i}", 3, ImGuiTableFlags.Borders);
		ImGui.TableSetupColumn("0", ImGuiTableColumnFlags.WidthFixed);
		ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);
		ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch);

		ImGui.TableNextRow();
		ImGui.TableNextColumn();

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

		using (var disabled = ImRaii.Disabled(Plugin.ClientState.LocalPlayer == null)) {
			if (ImGuiComponents.IconButton("SetWaypoint", FontAwesomeIcon.MapMarkerAlt)) {
				group.MapId = Plugin.ClientState.MapId;
				Configuration.Save();
			}
		}
		ImGui.SameLine();
		ImGui.Text($"{group.MapName}");

		using (var disabled = ImRaii.Disabled(Plugin.ClientState.LocalPlayer == null)) {
			if (ImGuiComponents.IconButton("SetGearset", FontAwesomeIcon.Tshirt)) {
				group.LinkedGearset = Services.GearsetService.GetEquippedGearsetName();
				Configuration.Save();
			}
		}
		ImGui.SameLine();
		ImGui.Text($"{group.LinkedGearset}");

		if (group.Uptime.Count == 0) {
			if (ImGuiComponents.IconButton("Add time range", FontAwesomeIcon.Clock)) {
				group.Uptime.Add(new EorzeaTimeRange());
				Configuration.Save();
			}
			ImGui.SameLine();
			ImGui.Text($"Always up");
		} else {
			for (var u = 0; u < group.Uptime.Count; u++) {
				var uptime = group.Uptime[u];

				if (ImGuiComponents.IconButton($"RemoveUptime_{u}", FontAwesomeIcon.Times)) {
					group.Uptime.Remove(uptime);
					Configuration.Save();
				}
				ImGui.SameLine();

				using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * 2 * ImGuiHelpers.GlobalScale);
				using var color = ImRaii.PushColor(ImGuiCol.Text, activeColor, group.WithinUptime(Services.TimeService.EorzeaTime));
				DrawTimeInput($"##UptimeStart_{u}", 20 * ImGuiHelpers.GlobalScale, uptime.Start, v => {
					uptime.Start = v;
					Configuration.Save();
				});
				ImGui.SameLine();
				ImGui.Text("-");
				ImGui.SameLine();
				DrawTimeInput($"##UptimeEnd_{u}", 20 * ImGuiHelpers.GlobalScale, uptime.End, v => {
					uptime.End = v;
					Configuration.Save();
				});
				ImGui.SameLine();
				ImGui.Text(" ET");
			}
			if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Clock, "Add time range")) {
				group.Uptime.Add(new EorzeaTimeRange());
				Configuration.Save();
			}
		}

		if (ImGuiComponents.IconButton("ToggleRepeat", FontAwesomeIcon.Repeat)) {
			group.Repeat = !group.Repeat;
			Configuration.Save();
		}
		ImGui.SameLine();
		ImGui.Text(!group.Repeat ? "No repeat" :
			group.Uptime == null ? "Repeat forever" :
			$"Repeat during uptime");

		if (ImGuiComponents.IconButton("Item", FontAwesomeIcon.ShoppingBag)) {
			// todo
		}
		ImGui.SameLine();
		ImGui.Text("No item selected");

		if (ImGuiComponents.IconButton("Rotation", FontAwesomeIcon.Tasks)) {
			ImGui.OpenPopup("RotationPopup");
		}
		using (var popup = ImRaii.Popup("RotationPopup")) {
			if (popup) {
				if (ImGui.MenuItem("External")) {
				}
				if (ImGui.MenuItem("Max yield")) {
				}
				if (ImGui.MenuItem("Max attempts")) {
				}
				if (ImGui.MenuItem("No GP")) {
				}
				if (ImGui.MenuItem("Ephemeral nodes")) {
				}
				if (ImGui.MenuItem("Legendary nodes")) {
				}
				if (ImGui.MenuItem("Crystals")) {
				}
			}
		}
		ImGui.SameLine();
		ImGui.Text("External rotation");

		ImGui.TableNextColumn();

		for (var j = 0; j < group.GatheringNodes.Count; ++j) {
			var node = group.GatheringNodes[j];
			var isRunningNode = isRunningGroup && RouteManager.CurrentNodeIndex == j;
			if (ImGuiComponents.IconButton($"Start_{j}", FontAwesomeIcon.Play)) {
				RouteManager.Start(activeRoute, i, j);
			}
			ImGui.SameLine();
			using var color = ImRaii.PushColor(ImGuiCol.Text, activeColor, isRunningNode);
			ImGui.Text($"{node.DataId:X} (x{node.Positions.Count})");
			ImGui.SameLine();
			if (ImGuiComponents.IconButton($"Remove_{j}", FontAwesomeIcon.Times)) {
				group.GatheringNodes.RemoveAt(j);
				Configuration.Save();
			}
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

	private static void DrawTimeInput(string label, float width, EorzeaTime value, Action<EorzeaTime> onChange) {
		var hour = value.Hour;
		var minute = value.Minute;

		using var id = ImRaii.PushId(label);
		var changed = false;

		using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * 2 * ImGuiHelpers.GlobalScale);
		ImGui.SetNextItemWidth(width);
		changed |= ImGui.DragInt("##hour", ref hour, 0.05f, 0, 23, "%02d", ImGuiSliderFlags.AlwaysClamp);
		ImGui.SameLine();
		ImGui.Text(":");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(width);
		changed |= ImGui.DragInt("##minute", ref minute, 0.2f, 0, 59, "%02d", ImGuiSliderFlags.AlwaysClamp);

		if (changed) {
			onChange(new EorzeaTime(hour, minute));
		}
	}

	private string debugDataId = "";
	private unsafe void DrawDebugTab() {
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

		ImGui.Text($"Player position: {Plugin.ClientState.LocalPlayer!.Position}");

		ImGui.Text($"Eorzea time: {Services.TimeService.EorzeaTimeRaw}");
	}

	private static float MarkerToWorldCoord(float coord, float scale, float offset) {
		return ((coord - 1024f) / (scale / 100f)) - (offset * (scale / 100f));
	}
}
