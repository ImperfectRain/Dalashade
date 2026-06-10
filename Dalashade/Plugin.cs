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
    private readonly PresetAnalyzer presetAnalyzer = new();
    private readonly CompatibilityReportExporter compatibilityReportExporter = new();
    private readonly ReShadeController reShadeController = new();

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
    public ReloadResult LastReloadResult { get; private set; } = ReloadResult.Skipped("Shaders have not been reloaded yet.");
    public ShaderSupportScan LastShaderSupportScan { get; private set; } = ShaderSupportScan.Skipped("Shader support has not been scanned yet.");
    public PresetAnalysisResult LastPresetAnalysis { get; private set; } = PresetAnalysisResult.Skipped("Preset has not been analyzed yet.");
    public CompatibilityReportExportResult LastCompatibilityReportExport { get; private set; } = CompatibilityReportExportResult.Skipped("No compatibility report has been exported yet.");
    public string DefaultGeneratedPresetPath => Path.Combine(PluginInterface.ConfigDirectory.FullName, "Dalashade_Generated.ini");
    public string CompatibilityReportDirectory => Path.Combine(PluginInterface.ConfigDirectory.FullName, "CompatibilityReports");
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
        masterStyleService.Refresh(Configuration, CurrentImageAnalysis, true);
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration);
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;
        ScanPresetCompatibility();
        LastWriteResult = presetWriter.WriteGeneratedPreset(Configuration, CurrentProfile);
        ReloadShadersIfNeeded();
        lastProfileKey = CreateProfileKey();
        lastWrite = DateTimeOffset.UtcNow;
        return LastWriteResult;
    }

    public ShaderSupportScan ScanShaderSupport()
    {
        ScanPresetCompatibility();
        return LastShaderSupportScan;
    }

    public PresetAnalysisResult ScanPresetCompatibility()
    {
        LastShaderSupportScan = presetWriter.ScanSupportedVariables(Configuration);
        LastPresetAnalysis = presetAnalyzer.Analyze(Configuration);
        return LastPresetAnalysis;
    }

    public CompatibilityReportExportResult ExportCompatibilityReport()
    {
        if (!LastPresetAnalysis.Success)
        {
            ScanPresetCompatibility();
        }

        LastCompatibilityReportExport = compatibilityReportExporter.Export(
            Configuration,
            LastPresetAnalysis,
            LastShaderSupportScan,
            LastWriteResult,
            CompatibilityReportDirectory);
        return LastCompatibilityReportExport;
    }

    public ReloadResult ReloadShadersNow()
    {
        LastReloadResult = reShadeController.ReloadAfterPresetWrite(Configuration);
        return LastReloadResult;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Configuration.Enabled)
        {
            return;
        }

        contextService.Refresh();
        imageAnalysisService.Refresh(Configuration);
        masterStyleService.Refresh(Configuration, CurrentImageAnalysis);
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration);
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;

        if (Configuration.SceneLockEnabled)
        {
            return;
        }

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
            ScanPresetCompatibility();
            ReloadShadersIfNeeded();
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
        var master = Configuration.MatchMasterPresetStyle ? CurrentMasterStyle.MetricsKey : "ignored";
        return $"{CurrentContext.ProfileKey(Configuration, CurrentImageAnalysis, CurrentTags)}:{master}:{Configuration.MasterPresetStyleStrength}:{CreateConfigKey()}";
    }

    private string CreateConfigKey()
    {
        return string.Join(":",
            Configuration.BasePresetPath,
            Configuration.GeneratedPresetPath,
            Configuration.UsePremiumImmerseEffects,
            Configuration.CompatibilityMode,
            Configuration.ShaderMatchingMode,
            Configuration.InactiveShaderWriteMode,
            Configuration.ImageSamplingMode,
            Configuration.MasterStyleMode,
            Configuration.MasterPresetMaxImages,
            Configuration.MasterPresetIncludeSubfolders,
            Configuration.AutoAdjustInCombat,
            Configuration.AutoAdjustAtNight,
            Configuration.AutoAdjustForWeather,
            Configuration.AutoAdjustForTerritory,
            Configuration.AutoAdjustFromScreenshots,
            Configuration.MatchMasterPresetStyle,
            Configuration.AutoAdjustInCutscenes,
            Configuration.Style,
            Configuration.PerformanceBudget);
    }

    private void ReloadShadersIfNeeded()
    {
        if (!LastWriteResult.Success)
        {
            LastReloadResult = ReloadResult.Skipped("Preset was not written, so shaders were not reloaded.");
            return;
        }

        LastReloadResult = reShadeController.ReloadAfterPresetWrite(Configuration);
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
