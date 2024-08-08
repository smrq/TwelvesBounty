using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TwelvesBounty.Exec;
using TwelvesBounty.Services;
using TwelvesBounty.Windows;

namespace TwelvesBounty;

public sealed class Plugin : IDalamudPlugin {
	[PluginService] internal static IAetheryteList AetheryteList { get; private set; } = null!;
	[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] internal static ICondition Condition { get; private set; } = null!;
	[PluginService] internal static IClientState ClientState { get; private set; } = null!;
	[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
	[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
	[PluginService] internal static IFramework Framework { get; private set; } = null!;
	[PluginService] internal static IGameGui GameGui { get; private set; } = null!;
	[PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
	[PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;

	private const string CommandName = "/ptb";

	public Configuration Configuration { get; init; }

	public readonly WindowSystem WindowSystem = new("TwelvesBounty");
	private ServiceInstances Services { get; init; }
	private RouteManager RouteManager { get; init; }
	private RoutesWindow RoutesWindow { get; init; }

	public Plugin() {
		Configuration = Configuration.LoadConfiguration(PluginInterface.ConfigFile.FullName);
		Services = new ServiceInstances();
		RouteManager = new RouteManager(Services);
		RoutesWindow = new RoutesWindow(Configuration, RouteManager, Services);

		WindowSystem.AddWindow(RoutesWindow);

		CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
			HelpMessage = "Opens the main window"
		});

		PluginInterface.UiBuilder.Draw += DrawUI;
		PluginInterface.UiBuilder.OpenMainUi += ToggleRoutesWindow;

		Framework.Update += (IFramework framework) => RouteManager.Update();

		ToggleRoutesWindow();
	}

	public void Dispose() {
		WindowSystem.RemoveAllWindows();

		RoutesWindow.Dispose();

		CommandManager.RemoveHandler(CommandName);
	}

	private void OnCommand(string command, string args) {
		ToggleRoutesWindow();
	}

	private void DrawUI() => WindowSystem.Draw();

	public void ToggleRoutesWindow() => RoutesWindow.Toggle();
}
