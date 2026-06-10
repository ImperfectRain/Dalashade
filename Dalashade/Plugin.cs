using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalashade.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
    private readonly BasePresetLibrary basePresetLibrary = new();
    private readonly PresetWriter presetWriter = new();
    private readonly PresetAnalyzer presetAnalyzer = new();
    private readonly CompatibilityReportExporter compatibilityReportExporter = new();
    private readonly PresetRegressionReportHarness presetRegressionReportHarness = new();
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
    public int MasterStyleImageCount => masterStyleService.LastImageCount;
    public VisualProfile CurrentProfile { get; private set; } = VisualProfile.Neutral;
    public IReadOnlyList<AppliedRule> CurrentRules { get; private set; } = Array.Empty<AppliedRule>();
    public MasterStyleDiagnostics CurrentMasterStyleDiagnostics { get; private set; } = MasterStyleDiagnostics.FromUnavailable(new Configuration(), ImageAnalysisResult.Empty, ImageAnalysisResult.Empty, 0, "Master style has not run yet.");
    public PresetWriteResult LastWriteResult { get; private set; } = PresetWriteResult.Skipped("No preset has been generated yet.");
    public ReloadResult LastReloadResult { get; private set; } = ReloadResult.Skipped("Shaders have not been reloaded yet.");
    public ShaderSupportScan LastShaderSupportScan { get; private set; } = ShaderSupportScan.Skipped("Shader support has not been scanned yet.");
    public PresetAnalysisResult LastPresetAnalysis { get; private set; } = PresetAnalysisResult.Skipped("Preset has not been analyzed yet.");
    public CompatibilityReportExportResult LastCompatibilityReportExport { get; private set; } = CompatibilityReportExportResult.Skipped("No compatibility report has been exported yet.");
    public PresetRegressionReportResult LastPresetRegressionReport { get; private set; } = PresetRegressionReportResult.Skipped("No preset regression report has been run yet.");
    public BasePresetLibraryScan LastBasePresetLibraryScan { get; private set; } = BasePresetLibraryScan.Skipped("Base presets have not been scanned yet.");
    public string DefaultBasePresetFolderPath => Path.Combine(PluginInterface.ConfigDirectory.FullName, "Base");
    public string DefaultGeneratedPresetPath => Path.Combine(PluginInterface.ConfigDirectory.FullName, "Generated", "Dalashade_Generated.ini");
    public string CompatibilityReportDirectory => Path.Combine(PluginInterface.ConfigDirectory.FullName, "CompatibilityReports");
    public string PresetRegressionReportDirectory => Path.Combine(PluginInterface.ConfigDirectory.FullName, "PresetRegressionReports");
    public string DefaultScreenshotFolderPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "My Games",
        "FINAL FANTASY XIV - A Realm Reborn",
        "screenshots");

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        InitializePresetFolders();
        RefreshBasePresetLibrary();

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
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration, MasterStyleImageCount);
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;
        CurrentMasterStyleDiagnostics = result.MasterStyleDiagnostics;
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
            CurrentProfile,
            CurrentMasterStyleDiagnostics,
            CurrentImageAnalysis,
            CurrentMasterStyle,
            LastWriteResult,
            CompatibilityReportDirectory);
        return LastCompatibilityReportExport;
    }

    public PresetRegressionReportResult RunPresetRegressionReports()
    {
        LastPresetRegressionReport = presetRegressionReportHarness.Run(Configuration, CurrentProfile, PresetRegressionReportDirectory);
        return LastPresetRegressionReport;
    }

    public BasePresetLibraryScan RefreshBasePresetLibrary()
    {
        LastBasePresetLibraryScan = basePresetLibrary.Scan(Configuration.BasePresetFolderPath);
        var matchedSelection = LastBasePresetLibraryScan.Items.FirstOrDefault(item =>
            string.Equals(item.FullPath, Configuration.BasePresetPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.FileName, Configuration.SelectedBasePresetFileName, StringComparison.OrdinalIgnoreCase));
        if (matchedSelection != null)
        {
            Configuration.SelectedBasePresetFileName = matchedSelection.FileName;
            Configuration.BasePresetPath = matchedSelection.FullPath;
            Configuration.Save();
        }

        if (Configuration.UseBasePresetFolder
            && LastBasePresetLibraryScan.Items.Count > 0
            && string.IsNullOrWhiteSpace(Configuration.BasePresetPath))
        {
            SelectBasePreset(LastBasePresetLibraryScan.Items[0]);
        }

        return LastBasePresetLibraryScan;
    }

    public void SelectBasePreset(BasePresetLibraryItem item)
    {
        Configuration.SelectedBasePresetFileName = item.FileName;
        Configuration.BasePresetPath = item.FullPath;
        Configuration.Save();
    }

    public void OpenBasePresetFolder()
    {
        try
        {
            Directory.CreateDirectory(Configuration.BasePresetFolderPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = Configuration.BasePresetFolderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Log.Warning(ex, "Could not open base preset folder.");
        }
    }

    public ReloadResult ReloadShadersNow()
    {
        LastReloadResult = reShadeController.TestReload(Configuration);
        return LastReloadResult;
    }

    public ReloadResult AutoDetectReShadeIniPath()
    {
        var path = reShadeController.AutoDetectReShadeIni(Configuration);
        if (string.IsNullOrWhiteSpace(path))
        {
            LastReloadResult = ReloadResult.Skipped("ReShade.ini auto-detect did not find a file.", Configuration);
            return LastReloadResult;
        }

        Configuration.ReShadeIniPath = path;
        Configuration.Save();
        LastReloadResult = new ReloadResult(true, $"ReShade.ini auto-detected: {path}", new ReloadDiagnostics(true, path, "not read", Keybind.FromConfiguration(Configuration).DisplayName, Configuration.SyncReloadHotkeyToReShadeIni, false, false));
        return LastReloadResult;
    }

    public void BrowseReShadeIniPath()
    {
        try
        {
            var path = Configuration.ReShadeIniPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                path = reShadeController.AutoDetectReShadeIni(Configuration);
            }

            var browseTarget = !string.IsNullOrWhiteSpace(path) && File.Exists(path)
                ? $"/select,\"{path}\""
                : PluginInterface.ConfigDirectory.FullName;

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = browseTarget,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Log.Warning(ex, "Could not browse to ReShade.ini.");
        }
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
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration, MasterStyleImageCount);
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;
        CurrentMasterStyleDiagnostics = result.MasterStyleDiagnostics;

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
        if (args.Trim().Equals("regression", StringComparison.OrdinalIgnoreCase))
        {
            RunPresetRegressionReports();
            mainWindow.IsOpen = true;
            return;
        }

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
            Configuration.BasePresetFolderPath,
            Configuration.SelectedBasePresetFileName,
            Configuration.UseBasePresetFolder,
            Configuration.GeneratedPresetPath,
            Configuration.ReShadeIniPath,
            Configuration.UsePremiumImmerseEffects,
            Configuration.CompatibilityMode,
            Configuration.ShaderMatchingMode,
            Configuration.InactiveShaderWriteMode,
            Configuration.ImageSamplingMode,
            Configuration.MasterStyleMode,
            Configuration.MasterPresetMaxImages,
            Configuration.MasterPresetIncludeSubfolders,
            Configuration.MasterStyleTuningPreset,
            Configuration.MasterTonalMatchStrength,
            Configuration.MasterTonalColorStrength,
            Configuration.MasterColorFamilyStrength,
            Configuration.MasterMaxHueShift,
            Configuration.MasterMaxSaturationShift,
            Configuration.MasterMaxLuminanceShift,
            Configuration.MasterSceneSimilarityDampening,
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

    private void InitializePresetFolders()
    {
        var changed = false;
        if (string.IsNullOrWhiteSpace(Configuration.BasePresetFolderPath))
        {
            Configuration.BasePresetFolderPath = DefaultBasePresetFolderPath;
            changed = true;
        }

        Directory.CreateDirectory(Configuration.BasePresetFolderPath);

        if (string.IsNullOrWhiteSpace(Configuration.GeneratedPresetPath))
        {
            Configuration.GeneratedPresetPath = DefaultGeneratedPresetPath;
            changed = true;
        }

        var generatedDirectory = Path.GetDirectoryName(Configuration.GeneratedPresetPath);
        if (!string.IsNullOrWhiteSpace(generatedDirectory))
        {
            Directory.CreateDirectory(generatedDirectory);
        }

        if (Configuration.UseBasePresetFolder
            && !string.IsNullOrWhiteSpace(Configuration.SelectedBasePresetFileName)
            && string.IsNullOrWhiteSpace(Configuration.BasePresetPath))
        {
            var selectedPath = Path.Combine(Configuration.BasePresetFolderPath, Configuration.SelectedBasePresetFileName);
            if (File.Exists(selectedPath))
            {
                Configuration.BasePresetPath = selectedPath;
                changed = true;
            }
        }

        if (changed)
        {
            Configuration.Save();
        }
    }

    private void ReloadShadersIfNeeded()
    {
        if (!LastWriteResult.Success)
        {
            LastReloadResult = ReloadResult.Skipped("Preset was not written, so shaders were not reloaded.", Configuration);
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
