using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalashade.Windows;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dalashade;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/dalashade";

    private readonly ConfigWindow configWindow;
    private readonly MainWindow mainWindow;
    private readonly GameContextService contextService = new();
    private readonly ImageAnalysisService imageAnalysisService = new();
    private readonly MasterStyleService masterStyleService = new();
    private readonly ProfileEngine profileEngine = new();
    private readonly PresetWriter presetWriter = new();

    private DateTimeOffset lastWrite = DateTimeOffset.MinValue;
    private string lastProfileKey = string.Empty;

    public Configuration Configuration { get; }
    public WindowSystem WindowSystem { get; } = new("Dalashade");
    public GameContext CurrentContext => contextService.Current;
    public SceneTags CurrentTags => contextService.CurrentTags;
    public ImageAnalysisResult CurrentImageAnalysis => imageAnalysisService.Current;
    public ImageAnalysisResult CurrentMasterStyle => masterStyleService.Current;
    public string ImageAnalysisMessage => imageAnalysisService.LastMessage;
    public string MasterStyleMessage => masterStyleService.LastMessage;
    public VisualProfile CurrentProfile { get; private set; } = VisualProfile.Neutral;
    public IReadOnlyList<AppliedRule> CurrentRules { get; private set; } = Array.Empty<AppliedRule>();
    public PresetWriteResult LastWriteResult { get; private set; } = PresetWriteResult.Skipped("No preset has been generated yet.");
    public string DefaultGeneratedPresetPath => Path.Combine(PluginInterface.ConfigDirectory.FullName, "Dalashade_Generated.ini");
    public string DefaultScreenshotFolderPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "My Games",
        "FINAL FANTASY XIV - A Realm Reborn",
        "screenshots");

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (string.IsNullOrWhiteSpace(Configuration.GeneratedPresetPath))
        {
            Configuration.GeneratedPresetPath = DefaultGeneratedPresetPath;
            Configuration.Save();
        }

        configWindow = new ConfigWindow(this);
        mainWindow = new MainWindow(this);

        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Dalashade control window."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        ClientState.ZoneInit += OnZoneInit;
        Framework.Update += OnFrameworkUpdate;

        Log.Information("Dalashade loaded.");
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        ClientState.ZoneInit -= OnZoneInit;

        WindowSystem.RemoveAllWindows();
        configWindow.Dispose();
        mainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    public PresetWriteResult GenerateNow()
    {
        contextService.Refresh();
        imageAnalysisService.Refresh(Configuration, true);
        masterStyleService.Refresh(Configuration, true);
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration);
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;
        LastWriteResult = presetWriter.WriteGeneratedPreset(Configuration, CurrentProfile);
        lastProfileKey = CreateProfileKey();
        lastWrite = DateTimeOffset.UtcNow;
        return LastWriteResult;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Configuration.Enabled)
        {
            return;
        }

        contextService.Refresh();
        imageAnalysisService.Refresh(Configuration);
        masterStyleService.Refresh(Configuration);
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration);
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;

        var profileKey = CreateProfileKey();
        if (profileKey == lastProfileKey)
        {
            return;
        }

        var minimumInterval = TimeSpan.FromSeconds(Math.Max(1, Configuration.MinimumSecondsBetweenWrites));
        if (DateTimeOffset.UtcNow - lastWrite < minimumInterval)
        {
            return;
        }

        LastWriteResult = presetWriter.WriteGeneratedPreset(Configuration, CurrentProfile);
        if (LastWriteResult.Success)
        {
            lastProfileKey = profileKey;
            lastWrite = DateTimeOffset.UtcNow;
        }
    }

    private void OnCommand(string command, string args)
    {
        mainWindow.Toggle();
    }

    private string CreateProfileKey()
    {
        var master = Configuration.MatchMasterPresetStyle ? CurrentMasterStyle.ProfileBucket : "ignored";
        return $"{CurrentContext.ProfileKey(Configuration, CurrentImageAnalysis, CurrentTags)}:{master}:{Configuration.MasterPresetStyleStrength}";
    }

    private void OnZoneInit(ZoneInitEventArgs args)
    {
        try
        {
            var contentName = args.ContentFinderCondition.IsValid
                ? args.ContentFinderCondition.Value.Name.ToString()
                : "Unknown";
            var contentType = args.ContentFinderCondition.IsValid
                ? args.ContentFinderCondition.Value.ContentType.Value.Name.ToString()
                : "Unknown";

            contextService.UpdateZoneInfo(args.Weather.RowId, args.Weather.Value.Name.ToString(), contentName, contentType);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not read ZoneInit weather.");
            contextService.UpdateZoneInfo(null, "Unknown", "Unknown", "Unknown");
        }
    }

    public void ToggleConfigUi() => configWindow.Toggle();

    public void ToggleMainUi() => mainWindow.Toggle();
}
