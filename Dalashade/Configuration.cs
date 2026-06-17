using Dalamud.Configuration;
using System;

namespace Dalashade;

public enum TargetStyle
{
    Gameplay,
    Balanced,
    Cinematic
}

public enum PerformanceBudget
{
    Low,
    Medium,
    High,
    Ultra
}

public enum ImageSamplingMode
{
    FullImage,
    CenterWeighted,
    IgnoreBottomUi,
    LetterboxSafe,
    GposeClean
}

public enum MasterStyleMode
{
    NewestImageOnly,
    AverageFolder,
    MedianFolder,
    ClosestToCurrentScene
}

public enum ShaderMatchingMode
{
    StrictSections,
    KnownFallbacks,
    LooseKeys
}

public enum InactiveShaderWriteMode
{
    Never,
    SupportedInactiveSections,
    AlwaysMatchingKeys
}

public enum PresetCompatibilityMode
{
    PreserveBase,
    AdaptiveBalanced,
    GameplaySanitize,
    CinematicPreserve,
    GposePreserve
}

public enum MasterStyleTuningPreset
{
    Subtle,
    Balanced,
    Strong,
    Cinematic,
    AggressiveGpose,
    Custom
}

public enum FirstPartyShaderMode
{
    Supportive,
    Standalone
}

public enum InterfaceMode
{
    User,
    Developer
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public string BasePresetPath { get; set; } = string.Empty;
    public string BasePresetFolderPath { get; set; } = string.Empty;
    public string SelectedBasePresetFileName { get; set; } = string.Empty;
    public bool UseBasePresetFolder { get; set; } = true;
    public string GeneratedPresetPath { get; set; } = string.Empty;
    public string ReShadeIniPath { get; set; } = string.Empty;
    public bool ReloadShadersAfterGeneration { get; set; } = true;
    public bool SyncReloadHotkeyToReShadeIni { get; set; } = true;
    public int ReloadHotkeyVirtualKey { get; set; } = 116;
    public bool ReloadHotkeyCtrl { get; set; } = false;
    public bool ReloadHotkeyShift { get; set; } = false;
    public bool ReloadHotkeyAlt { get; set; } = false;
    public InterfaceMode InterfaceMode { get; set; } = InterfaceMode.User;

    public TargetStyle Style { get; set; } = TargetStyle.Balanced;
    public PerformanceBudget PerformanceBudget { get; set; } = PerformanceBudget.Medium;

    public bool Enabled { get; set; } = true;
    public bool AutoAdjustInCombat { get; set; } = true;
    public bool AutoAdjustAtNight { get; set; } = true;
    public bool AutoAdjustForWeather { get; set; } = true;
    public bool AutoAdjustForTerritory { get; set; } = true;
    public bool AutoAdjustFromScreenshots { get; set; } = false;
    public bool EnableSceneAuthoringOverrides { get; set; } = false;
    public bool MatchMasterPresetStyle { get; set; } = false;
    public bool SceneLockEnabled { get; set; } = false;
    public bool EnableFirstPartyDepthAssist { get; set; } = false;
    public bool AutoAdjustInCutscenes { get; set; } = true;
    public bool UsePremiumImmerseEffects { get; set; } = false;
    public bool EnableDalashadeCustomShaders { get; set; } = false;
    public FirstPartyShaderMode FirstPartyShaderMode { get; set; } = FirstPartyShaderMode.Supportive;
    public bool AutoInjectDalashadeCustomShaderSections { get; set; } = false;
    public bool EnableDalashadeSceneGIShaderVariables { get; set; } = false;
    public float DalashadeSceneGIStrength { get; set; } = 0.35f;
    public float DalashadeSceneGIAOIntensity { get; set; } = 0.25f;
    public float DalashadeSceneGIBounceStrength { get; set; } = 0.20f;
    public float DalashadeSceneGINightLightStrength { get; set; } = 0.30f;
    public float DalashadeSceneGIMaterialInfluence { get; set; } = 0.50f;
    public int DalashadeSceneGIDebugMode { get; set; } = 0;
    public int DalashadeSceneGIDebugOutputMode { get; set; } = 0;
    public float DalashadeSceneGIDebugOpacity { get; set; } = 0.75f;
    public float DalashadeSceneGIDebugBoost { get; set; } = 2.50f;
    public bool EnableDalashadeSurfaceReflectionShaderVariables { get; set; } = false;
    public float DalashadeSurfaceReflectionStrength { get; set; } = 0.32f;
    public float DalashadeSurfaceReflectionWaterSheenStrength { get; set; } = 0.38f;
    public float DalashadeSurfaceReflectionSpecularGlintStrength { get; set; } = 0.32f;
    public float DalashadeSurfaceReflectionWetStrength { get; set; } = 0.30f;
    public float DalashadeSurfaceReflectionAetherNeonStrength { get; set; } = 0.35f;
    public int DalashadeSurfaceReflectionDebugMode { get; set; } = 0;
    public float DalashadeSurfaceReflectionDebugOpacity { get; set; } = 0.75f;
    public bool EnableMaterialIntent { get; set; } = false;
    public bool EnableMaterialIntentDiagnostics { get; set; } = true;
    public bool EnableMaterialIntentShaderMapping { get; set; } = false;
    public float MaterialIntentStrength { get; set; } = 0.25f;
    public bool EnableNormalField { get; set; } = false;
    public bool EnableNormalFieldDiagnostics { get; set; } = true;
    public bool EnableNormalFieldShaderMapping { get; set; } = false;
    public float NormalFieldStrength { get; set; } = 0.25f;
    public float NormalFieldDepthStrength { get; set; } = 0.50f;
    public float NormalFieldDetailStrength { get; set; } = 0.25f;
    public float NormalFieldMaterialInfluence { get; set; } = 0.50f;
    public float NormalFieldWaterSuppression { get; set; } = 0.80f;
    public float NormalFieldSkinSuppression { get; set; } = 0.90f;
    public float NormalFieldSkySuppression { get; set; } = 0.95f;
    public int NormalFieldDebugMode { get; set; } = 0;
    public float NormalFieldDebugBoost { get; set; } = 2.0f;
    public PresetCompatibilityMode CompatibilityMode { get; set; } = PresetCompatibilityMode.AdaptiveBalanced;
    public ShaderMatchingMode ShaderMatchingMode { get; set; } = ShaderMatchingMode.StrictSections;
    public InactiveShaderWriteMode InactiveShaderWriteMode { get; set; } = InactiveShaderWriteMode.SupportedInactiveSections;
    public bool WriteBackups { get; set; } = true;
    public int MaxGeneratedPresetBackups { get; set; } = 10;
    public int MinimumSecondsBetweenWrites { get; set; } = 10;
    public int MinimumSecondsBetweenImageSamples { get; set; } = 10;
    public ImageSamplingMode ImageSamplingMode { get; set; } = ImageSamplingMode.CenterWeighted;
    public int MasterPresetStyleStrength { get; set; } = 75;
    public MasterStyleTuningPreset MasterStyleTuningPreset { get; set; } = MasterStyleTuningPreset.Balanced;
    public float MasterTonalMatchStrength { get; set; } = 1.0f;
    public float MasterTonalColorStrength { get; set; } = 0.75f;
    public float MasterColorFamilyStrength { get; set; } = 0.65f;
    public float MasterMaxHueShift { get; set; } = 0.08f;
    public float MasterMaxSaturationShift { get; set; } = 0.15f;
    public float MasterMaxLuminanceShift { get; set; } = 0.12f;
    public bool MasterSceneSimilarityDampening { get; set; } = true;
    public int MasterPresetMaxImages { get; set; } = 24;
    public MasterStyleMode MasterStyleMode { get; set; } = MasterStyleMode.ClosestToCurrentScene;
    public string ScreenshotFolderPath { get; set; } = string.Empty;
    public string MasterPresetFolderPath { get; set; } = string.Empty;
    public bool MasterPresetIncludeSubfolders { get; set; } = false;
    public string TestPresetFolderPath { get; set; } = string.Empty;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
