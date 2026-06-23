using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalashade.SceneAuthoring;
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
    private readonly SceneAuthoringWindow sceneAuthoringWindow;
    private readonly GameContextService contextService = new();
    private readonly ImageAnalysisService imageAnalysisService = new();
    private readonly MasterStyleService masterStyleService = new();
    private readonly ProfileEngine profileEngine = new();
    private readonly BasePresetLibrary basePresetLibrary = new();
    private readonly PresetWriter presetWriter = new();
    private readonly PresetAnalyzer presetAnalyzer = new();
    private readonly CompatibilityReportExporter compatibilityReportExporter = new();
    private readonly DebugBundleExporter debugBundleExporter = new();
    private readonly PresetRegressionReportHarness presetRegressionReportHarness = new();
    private readonly ReShadeController reShadeController = new();
    private readonly SceneAuthoringService sceneAuthoringService = new();

    private DateTimeOffset lastWrite = DateTimeOffset.MinValue;
    private string lastProfileKey = string.Empty;

    public Configuration Configuration { get; }
    public WindowSystem WindowSystem { get; } = new("Dalashade");
    public GameContext CurrentContext => contextService.Current;
    public SceneTags CurrentDetectedTags => contextService.CurrentTags;
    public SceneTags CurrentTags { get; private set; } = SceneTags.Empty;
    public SceneAuthoringState CurrentSceneAuthoringState { get; private set; } = SceneAuthoringState.Disabled(SceneTags.Empty, string.Empty);
    public ImageAnalysisResult CurrentImageAnalysis => imageAnalysisService.Current;
    public ImageAnalysisResult CurrentMasterStyle => masterStyleService.Current;
    public string ImageAnalysisMessage => imageAnalysisService.LastMessage;
    public string MasterStyleMessage => masterStyleService.LastMessage;
    public int MasterStyleImageCount => masterStyleService.LastImageCount;
    public VisualProfile CurrentProfile { get; private set; } = VisualProfile.Neutral;
    public IReadOnlyList<AppliedRule> CurrentRules { get; private set; } = Array.Empty<AppliedRule>();
    public MasterStyleDiagnostics CurrentMasterStyleDiagnostics { get; private set; } = MasterStyleDiagnostics.FromUnavailable(new Configuration(), ImageAnalysisResult.Empty, ImageAnalysisResult.Empty, 0, "Master style has not run yet.");
    public TagStackDiagnostics CurrentTagStackDiagnostics { get; private set; } = TagStackDiagnostics.Empty;
    public MaterialIntent CurrentMaterialIntent { get; private set; } = MaterialIntent.Neutral;
    public ScreenshotMaterialEvidenceDiagnostics CurrentScreenshotMaterialEvidence { get; private set; } = ScreenshotMaterialEvidenceDiagnostics.Neutral("Screenshot material evidence has not run yet.");
    public MaterialTagRegistryDiagnostics CurrentMaterialTagRegistryDiagnostics { get; private set; } = MaterialTagRegistryDiagnostics.Empty;
    public DalapadDiagnostics CurrentDalapadDiagnostics { get; private set; } = DalapadDiagnostics.NotProbed("Dalapad diagnostics have not run yet.");
    public PresetWriteResult LastWriteResult { get; private set; } = PresetWriteResult.Skipped("No preset has been generated yet.");
    public ReloadResult LastReloadResult { get; private set; } = ReloadResult.Skipped("Shaders have not been reloaded yet.");
    public ShaderSupportScan LastShaderSupportScan { get; private set; } = ShaderSupportScan.Skipped("Shader support has not been scanned yet.");
    public PresetAnalysisResult LastPresetAnalysis { get; private set; } = PresetAnalysisResult.Skipped("Preset has not been analyzed yet.");
    public CompatibilityReportExportResult LastCompatibilityReportExport { get; private set; } = CompatibilityReportExportResult.Skipped("No compatibility report has been exported yet.");
    public DebugBundleExportResult LastDebugBundleExport { get; private set; } = DebugBundleExportResult.Skipped("No debug bundle has been exported yet.");
    public string LastDiagnosticsExportMessage { get; private set; } = "No diagnostics export has been run yet.";
    public PresetRegressionReportResult LastPresetRegressionReport { get; private set; } = PresetRegressionReportResult.Skipped("No preset regression report has been run yet.");
    public BasePresetLibraryScan LastBasePresetLibraryScan { get; private set; } = BasePresetLibraryScan.Skipped("Base presets have not been scanned yet.");
    private string SafePluginConfigDirectory
    {
        get
        {
            var configured = PluginInterface.ConfigDirectory.FullName;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return string.IsNullOrWhiteSpace(appData)
                ? Path.Combine(Environment.CurrentDirectory, "Dalashade")
                : Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Dalashade");
        }
    }

    public string DefaultBasePresetFolderPath => Path.Combine(SafePluginConfigDirectory, "Base");
    public string DefaultGeneratedPresetPath => Path.Combine(SafePluginConfigDirectory, "Generated", "Dalashade_Generated.ini");
    public string CompatibilityReportDirectory => Path.Combine(SafePluginConfigDirectory, "Reports");
    public string DebugBundleDirectory => Path.Combine(SafePluginConfigDirectory, "DebugBundles");
    public string PresetRegressionReportDirectory => Path.Combine(SafePluginConfigDirectory, "PresetRegressionReports");
    public string SceneAuthoringDirectory => Path.Combine(SafePluginConfigDirectory, "SceneAuthoring");
    public string SceneAuthoringImportExportMessage => sceneAuthoringService.LastImportExportMessage;
    public string DefaultScreenshotFolderPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "My Games",
        "FINAL FANTASY XIV - A Realm Reborn",
        "screenshots");

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        InitializePresetFolders();
        sceneAuthoringService.Load(SafePluginConfigDirectory);
        RefreshBasePresetLibrary();

        configWindow = new ConfigWindow(this);
        mainWindow = new MainWindow(this);
        sceneAuthoringWindow = new SceneAuthoringWindow(this);

        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(sceneAuthoringWindow);

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
        sceneAuthoringWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    public PresetWriteResult GenerateNow()
    {
        ResolveEffectiveBasePresetPath(true);
        contextService.Refresh();
        RefreshEffectiveTags();
        imageAnalysisService.Refresh(Configuration, true);
        masterStyleService.Refresh(Configuration, CurrentImageAnalysis, true);
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration, MasterStyleImageCount, ActiveTagRegistry());
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;
        CurrentMasterStyleDiagnostics = result.MasterStyleDiagnostics;
        CurrentTagStackDiagnostics = result.TagStackDiagnostics;
        var materialIntent = RefreshMaterialIntent();
        ScanPresetCompatibility();
        LastWriteResult = presetWriter.WriteGeneratedPreset(Configuration, CurrentProfile, CurrentTagStackDiagnostics.Intent, materialIntent);
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
        ResolveEffectiveBasePresetPath(true);
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

        RefreshMaterialIntent();
        RefreshDalapadDiagnostics(Configuration.EnableDalapadResourceShapeProbe);
        LastCompatibilityReportExport = compatibilityReportExporter.Export(
            Configuration,
            LastPresetAnalysis,
            LastShaderSupportScan,
            CurrentProfile,
            CurrentMasterStyleDiagnostics,
            CurrentTagStackDiagnostics,
            CurrentSceneAuthoringState,
            CurrentImageAnalysis,
            CurrentScreenshotMaterialEvidence,
            CurrentDalapadDiagnostics,
            ActiveTagRegistry(),
            CurrentMasterStyle,
            LastWriteResult,
            ResolveEffectiveBasePresetPath(true),
            CompatibilityReportDirectory);
        LastDiagnosticsExportMessage = LastCompatibilityReportExport.Message;
        return LastCompatibilityReportExport;
    }

    public DebugBundleExportResult ExportDebugBundle()
    {
        contextService.Refresh();
        RefreshEffectiveTags();
        imageAnalysisService.Refresh(Configuration, true);
        masterStyleService.Refresh(Configuration, CurrentImageAnalysis, true);
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration, MasterStyleImageCount, ActiveTagRegistry());
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;
        CurrentMasterStyleDiagnostics = result.MasterStyleDiagnostics;
        CurrentTagStackDiagnostics = result.TagStackDiagnostics;
        var materialIntent = RefreshMaterialIntent();
        RefreshDalapadDiagnostics(Configuration.EnableDalapadResourceShapeProbe);
        ScanPresetCompatibility();
        var freshReport = ExportCompatibilityReport();
        LastDebugBundleExport = debugBundleExporter.Export(
            Configuration,
            CurrentContext,
            CurrentTagStackDiagnostics,
            CurrentSceneAuthoringState,
            CurrentImageAnalysis,
            CurrentScreenshotMaterialEvidence,
            CurrentDalapadDiagnostics,
            ActiveTagRegistry(),
            CurrentMasterStyle,
            CurrentProfile,
            materialIntent,
            LastPresetAnalysis,
            LastShaderSupportScan,
            LastWriteResult,
            freshReport,
            ResolveEffectiveBasePresetPath(true),
            DebugBundleDirectory,
            SafePluginConfigDirectory);
        LastDiagnosticsExportMessage = LastDebugBundleExport.Message;
        return LastDebugBundleExport;
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

    public DalapadDiagnostics RefreshDalapadDiagnostics(bool includeResourceShapeProbe = false)
    {
        CurrentDalapadDiagnostics = DalapadDiagnostics.Probe(SafePluginConfigDirectory, includeResourceShapeProbe);
        return CurrentDalapadDiagnostics;
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
                : SafePluginConfigDirectory;

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
        RefreshEffectiveTags();
        imageAnalysisService.Refresh(Configuration);
        masterStyleService.Refresh(Configuration, CurrentImageAnalysis);
        var result = profileEngine.CreateWithRules(CurrentContext, CurrentTags, CurrentImageAnalysis, CurrentMasterStyle, Configuration, MasterStyleImageCount, ActiveTagRegistry());
        CurrentProfile = result.Profile;
        CurrentRules = result.Rules;
        CurrentMasterStyleDiagnostics = result.MasterStyleDiagnostics;
        CurrentTagStackDiagnostics = result.TagStackDiagnostics;
        var materialIntent = RefreshMaterialIntent();

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

        LastWriteResult = presetWriter.WriteGeneratedPreset(Configuration, CurrentProfile, CurrentTagStackDiagnostics.Intent, materialIntent);
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

        if (args.Trim().Equals("authoring", StringComparison.OrdinalIgnoreCase)
            || args.Trim().Equals("tags", StringComparison.OrdinalIgnoreCase))
        {
            sceneAuthoringWindow.Toggle();
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
            Configuration.EnableDalashadeCustomShaders,
            Configuration.FirstPartyShaderMode,
            Configuration.EnableBasePolish,
            Configuration.AutoInjectDalashadeCustomShaderSections,
            Configuration.EnableFirstPartyDepthAssist,
            Configuration.EnableDalashadeSceneGIShaderVariables,
            Configuration.DalashadeSceneGIStrength,
            Configuration.DalashadeSceneGIAOIntensity,
            Configuration.DalashadeSceneGIBounceStrength,
            Configuration.DalashadeSceneGINightLightStrength,
            Configuration.DalashadeSceneGIMaterialInfluence,
            Configuration.EnableDalashadeSceneGIContactShadows,
            Configuration.DalashadeSceneGIContactShadowStrength,
            Configuration.DalashadeSceneGIContactShadowRadius,
            Configuration.DalashadeSceneGIContactShadowSoftness,
            Configuration.EnableDalashadeScreenShadowsShaderVariables,
            Configuration.DalashadeScreenShadowsStrength,
            Configuration.DalashadeScreenShadowsReach,
            Configuration.DalashadeScreenShadowsSoftness,
            Configuration.DalashadeScreenShadowsSourceSensitivity,
            Configuration.DalashadeScreenShadowsDalapadInfluence,
            Configuration.DalashadeScreenShadowsDebugMode,
            Configuration.DalashadeScreenShadowsDebugOutputMode,
            Configuration.DalashadeScreenShadowsDebugOpacity,
            Configuration.DalashadeScreenShadowsDebugBoost,
            Configuration.EnableDalashadeContactToneShaderVariables,
            Configuration.DalashadeContactToneStrength,
            Configuration.DalashadeContactToneRadius,
            Configuration.DalashadeContactToneEdgeStrength,
            Configuration.DalashadeContactToneStructureStrength,
            Configuration.DalashadeContactToneContrastStrength,
            Configuration.EnableDalashadeSurfaceReflectionShaderVariables,
            Configuration.DalashadeSurfaceReflectionStrength,
            Configuration.DalashadeSurfaceReflectionWaterSheenStrength,
            Configuration.DalashadeSurfaceReflectionSpecularGlintStrength,
            Configuration.DalashadeSurfaceReflectionWetStrength,
            Configuration.DalashadeSurfaceReflectionAetherNeonStrength,
            Configuration.EnableNormalField,
            Configuration.EnableNormalFieldShaderMapping,
            Configuration.NormalFieldStrength,
            Configuration.NormalFieldDepthStrength,
            Configuration.NormalFieldDetailStrength,
            Configuration.NormalFieldMaterialInfluence,
            Configuration.EnableMaterialIntent,
            Configuration.EnableMaterialIntentDiagnostics,
            Configuration.EnableMaterialIntentShaderMapping,
            Configuration.MaterialIntentStrength,
            Configuration.EnableScreenshotMaterialEvidenceInfluence,
            Configuration.ScreenshotMaterialEvidenceStrength,
            Configuration.CompatibilityMode,
            Configuration.ShaderMatchingMode,
            Configuration.InactiveShaderWriteMode,
            Configuration.OptimizeGeneratedPresetLoadOrder,
            Configuration.SyncDalashadeTechniqueActivation,
            Configuration.ImageSamplingMode,
            Configuration.ScreenshotAnalysisStrength,
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
            Configuration.EnableSceneAuthoringOverrides,
            CurrentSceneAuthoringState.Fingerprint,
            Configuration.EnableSceneAuthoringOverrides ? sceneAuthoringService.RegistryFingerprint : "registry-disabled",
            Configuration.Style,
            Configuration.PerformanceBudget);
    }

    private void RefreshEffectiveTags()
    {
        CurrentSceneAuthoringState = sceneAuthoringService.Apply(Configuration, CurrentContext, CurrentDetectedTags);
        CurrentTags = CurrentSceneAuthoringState.EffectiveTags;
    }

    public void AddSceneAuthoringTag(string category, string tag)
    {
        sceneAuthoringService.AddTag(CurrentContext, category, tag);
        RefreshEffectiveTags();
    }

    public void RemoveSceneAuthoringTag(string category, string tag)
    {
        sceneAuthoringService.RemoveTag(CurrentContext, category, tag);
        RefreshEffectiveTags();
    }

    public void ClearSceneAuthoringTagOverride(string category, string tag)
    {
        sceneAuthoringService.ClearTagOverride(CurrentContext, category, tag);
        RefreshEffectiveTags();
    }

    public void SetSceneAuthoringPrimaryBiome(string biome)
    {
        sceneAuthoringService.SetPrimaryBiome(CurrentContext, biome);
        RefreshEffectiveTags();
    }

    public void ClearSceneAuthoringPrimaryBiomeOverride()
    {
        sceneAuthoringService.ClearPrimaryBiomeOverride(CurrentContext);
        RefreshEffectiveTags();
    }

    public void ResetCurrentSceneAuthoringOverride()
    {
        sceneAuthoringService.ResetCurrentScene(CurrentContext);
        RefreshEffectiveTags();
    }

    public void RefreshSceneAuthoringState() => RefreshEffectiveTags();

    public IReadOnlyList<string> SceneAuthoringKnownTags(string category) => sceneAuthoringService.KnownTagsForCategory(category);

    public IReadOnlyList<SceneTagPreset> SceneAuthoringTagPresets(string category) => sceneAuthoringService.PresetsForCategory(category);

    public SceneTagPreset? FindSceneAuthoringTagPreset(string category, string tag) => sceneAuthoringService.FindPreset(category, tag);

    public IReadOnlyList<string> SceneAuthoringTuningChannels(string target) => sceneAuthoringService.TuningChannelsForTarget(target);

    public void AddSceneAuthoringTagPreset(string category, string tag)
    {
        sceneAuthoringService.AddCustomTagPreset(category, tag);
        RefreshEffectiveTags();
    }

    public void UpdateSceneAuthoringTagPreset(string category, string tag, string displayName, string description)
    {
        sceneAuthoringService.UpdateTagPreset(category, tag, displayName, description);
    }

    public void AddSceneAuthoringTagTuning(string category, string tag)
    {
        sceneAuthoringService.AddTagTuning(category, tag);
        RefreshEffectiveTags();
    }

    public void UpdateSceneAuthoringTagTuning(string category, string tag, int index, SceneTagTuning tuning)
    {
        sceneAuthoringService.UpdateTagTuning(category, tag, index, tuning);
        RefreshEffectiveTags();
    }

    public void RemoveSceneAuthoringTagTuning(string category, string tag, int index)
    {
        sceneAuthoringService.RemoveTagTuning(category, tag, index);
        RefreshEffectiveTags();
    }

    public void ResetSceneAuthoringTagPreset(string category, string tag)
    {
        sceneAuthoringService.ResetTagPreset(category, tag);
        RefreshEffectiveTags();
    }

    public void ExportSceneAuthoringOverrides() => sceneAuthoringService.ExportOverrides();

    public void ImportSceneAuthoringOverrides()
    {
        sceneAuthoringService.ImportOverrides();
        RefreshEffectiveTags();
    }

    public void ResetAllSceneAuthoringOverrides()
    {
        sceneAuthoringService.ResetAllOverrides();
        RefreshEffectiveTags();
    }

    public void ExportSceneAuthoringTagPresets() => sceneAuthoringService.ExportTagPresets();

    public void ImportSceneAuthoringTagPresets()
    {
        sceneAuthoringService.ImportTagPresets();
        RefreshEffectiveTags();
    }

    public void ResetSceneAuthoringTagPresets()
    {
        sceneAuthoringService.ResetTagPresets();
        RefreshEffectiveTags();
    }

    private MaterialIntent RefreshMaterialIntent()
    {
        var screenshotEvidence = ScreenshotMaterialEvidenceAnalyzer.Analyze(CurrentImageAnalysis, CurrentTags, CurrentContext);
        var screenshotEvidenceContributions = Configuration.EnableMaterialIntent
            ? ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(Configuration, CurrentTagStackDiagnostics, screenshotEvidence)
            : Array.Empty<MaterialIntentContribution>();
        var tagRegistry = ActiveTagRegistry();
        var registryContributions = MaterialTagRegistryTuningAnalyzer.Build(CurrentTagStackDiagnostics, tagRegistry);
        var rawIntent = Configuration.EnableMaterialIntent
            ? MaterialIntentBuilder.Build(
                CurrentTagStackDiagnostics,
                CurrentImageAnalysis,
                tagRegistry,
                Configuration.ScreenshotAnalysisStrength,
                screenshotEvidenceContributions)
            : MaterialIntent.Neutral;
        CurrentMaterialTagRegistryDiagnostics = registryContributions.Diagnostics;
        CurrentMaterialIntent = rawIntent.WithStrength(Configuration.MaterialIntentStrength);
        CurrentScreenshotMaterialEvidence = new ScreenshotMaterialEvidenceDiagnostics(
            screenshotEvidence,
            ScreenshotMaterialEvidenceAnalyzer.Compare(screenshotEvidence, CurrentMaterialIntent));
        return rawIntent;
    }

    private IReadOnlyList<SceneTagPreset>? ActiveTagRegistry()
    {
        return Configuration.EnableSceneAuthoringOverrides
            ? sceneAuthoringService.AllTagPresets()
            : null;
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

    private string ResolveEffectiveBasePresetPath(bool updateConfiguration)
    {
        var effectivePath = Configuration.BasePresetPath;
        if (Configuration.UseBasePresetFolder && !string.IsNullOrWhiteSpace(Configuration.SelectedBasePresetFileName))
        {
            var selectedPath = Path.Combine(Configuration.BasePresetFolderPath, Configuration.SelectedBasePresetFileName);
            effectivePath = selectedPath;
            if (!string.Equals(Configuration.BasePresetPath, selectedPath, StringComparison.OrdinalIgnoreCase)
                && updateConfiguration)
            {
                Configuration.BasePresetPath = selectedPath;
                Configuration.Save();
            }
        }

        return effectivePath;
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

    public void ToggleSceneAuthoringUi() => sceneAuthoringWindow.Toggle();
}
