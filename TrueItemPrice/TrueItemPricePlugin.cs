using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using TrueItemPrice.Windows;

namespace TrueItemPrice;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TrueItemPricePlugin : IDalamudPlugin
{
    private const string CommandName = "/pmycommand";
    public static readonly string PluginVersion = "0.1"; //TODO who to get this to the plugin listing?

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("TrueItemPrice");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public ItemTooltip ItemTooltip { get; }
    public Hooks Hooks { get; }
    public TIPEngine TIPEngine { get; }
    public UniversalisClient  UniversalisClient { get; }

    public TrueItemPricePlugin(IDalamudPluginInterface pluginInterface)
    {
        Service.Initialize(pluginInterface);
        Configuration = Configuration.Get(pluginInterface);

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);
        ItemTooltip = new ItemTooltip(this);
        Hooks = new Hooks(this);
        TIPEngine = new TIPEngine(this);
        UniversalisClient = new UniversalisClient(this);
        UniversalisClient.Initialize();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        pluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button doing the same but for the main ui of the plugin
        pluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [TrueItemPrice] ===A cool log message from Sample Plugin===
        Service.PluginLog.Information($"===A cool log message from {pluginInterface.Manifest.Name}===");
    }


    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
