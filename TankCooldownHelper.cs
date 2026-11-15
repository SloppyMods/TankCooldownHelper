using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using TankCooldownHelper.Core;
using TankCooldownHelper.Services;
using TankCooldownHelper.Services.Ipc;
using TankCooldownHelper.UI;

namespace TankCooldownHelper;

public class TankCooldownHelperPlugin : IDalamudPlugin
{
    public string Name => "Tank Cooldown Helper";
    
    private readonly WindowSystem _windowSystem;
    private readonly MainWindow _mainWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly Configuration _configuration;
    private readonly CombatEventBuffer _combatBuffer;
    private readonly CombatEventCollector _combatEventCollector;
    private readonly DangerCalculator _dangerCalculator;
    private readonly DamageTracker _damageTracker;
    private readonly HealingTracker _healingTracker;
    private readonly Predictor _predictor;
    private readonly DangerSnapshotService _dangerSnapshotService;
    private readonly DangerIpcProvider _dangerIpcProvider;

    public TankCooldownHelperPlugin(IDalamudPluginInterface pluginInterface)
    {
        // Initialize ECommons
        ECommonsMain.Init(pluginInterface, this,
            ECommons.Module.DalamudReflector,
            ECommons.Module.ObjectFunctions);

        // Load configuration
        _configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        MigrateLegacyIntegrationFlag(_configuration);
        
        // Initialize core systems
        _combatBuffer = new CombatEventBuffer(_configuration.TimeWindowSeconds);
        _dangerCalculator = new DangerCalculator(_configuration);
        _damageTracker = new DamageTracker(_combatBuffer);
        _healingTracker = new HealingTracker(_combatBuffer);
        _predictor = new Predictor(_dangerCalculator);
        _dangerSnapshotService = new DangerSnapshotService(
            _configuration,
            _dangerCalculator,
            _damageTracker,
            _healingTracker,
            _predictor);
        _dangerIpcProvider = new DangerIpcProvider(_dangerSnapshotService);
        _combatEventCollector = new CombatEventCollector(_combatBuffer);
        
        // Initialize UI
        _windowSystem = new WindowSystem("TankCooldownHelper");
        _mainWindow = new MainWindow(_configuration, _dangerCalculator, _damageTracker, _healingTracker, _predictor);
        _settingsWindow = new SettingsWindow(_configuration);
        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_settingsWindow);
        
        // Register WindowSystem with Dalamud
        pluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        
        // Register command
        Svc.Commands.AddHandler("/tch", new Dalamud.Game.Command.CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle Tank Cooldown Helper window (/tch config for settings)",
            ShowInHelp = true
        });
        
        // Hook into Framework.Update
        Svc.Framework.Update += OnFrameworkUpdate;
        
        Svc.Log.Info("TankCooldownHelper initialized");
    }

    private void OnCommand(string command, string args)
    {
        if (args.Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            _settingsWindow.Toggle();
        }
        else
        {
            _mainWindow.Toggle();
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Svc.ClientState.IsLoggedIn) return;
        if (Svc.Objects.LocalPlayer is not { } player) return;
        
        _combatBuffer.PruneOldEvents();
        _combatEventCollector.DetectDamageFromHpChanges();
    }

    private static void MigrateLegacyIntegrationFlag(Configuration config)
    {
        if (config.Version < 2 && config.EnablePL3Integration)
        {
            config.EnableParseLord5Integration = true;
            config.Version = 2;
        }
    }

    public void Dispose()
    {
        _dangerIpcProvider.Dispose();
        Svc.Framework.Update -= OnFrameworkUpdate;
        Svc.Commands.RemoveHandler("/tch");
        
        // Unregister WindowSystem from Dalamud
        Svc.PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        
        _windowSystem.RemoveAllWindows();
        _mainWindow.Dispose();
        _settingsWindow.Dispose();
        _combatEventCollector.Dispose();
        
        ECommonsMain.Dispose();
        
        Svc.Log.Info("TankCooldownHelper disposed");
    }
}
