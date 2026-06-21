using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalashade;

public sealed record ChangedShaderVariable(
    string Section,
    string Key,
    string OldValue,
    string NewValue,
    string ReasonCategory,
    TechniqueActivationState ActivationState,
    bool HitMin,
    bool HitMax,
    float AuthorityAdjustmentStrength = 1f,
    string? Warning = null);

public sealed record PresetWriteResult(
    bool Success,
    string Message,
    int ChangedVariables,
    IReadOnlyList<ChangedShaderVariable> Changes,
    IReadOnlyList<SanitizedShaderVariable> SanitizeActions,
    CustomShaderInjectionResult CustomShaderInjection,
    TechniqueOrderOptimizationResult TechniqueOrderOptimization)
{
    public static PresetWriteResult Skipped(string message) => new(false, message, 0, Array.Empty<ChangedShaderVariable>(), Array.Empty<SanitizedShaderVariable>(), CustomShaderInjectionResult.Skipped, TechniqueOrderOptimizationResult.Skipped);
}

public sealed record CustomShaderInjectionResult(
    bool Attempted,
    bool GeneratedPresetOnly,
    bool SectionInjected,
    bool VariablesInjected,
    bool TechniqueInjected,
    bool TechniqueDeactivated,
    string Message,
    IReadOnlyList<string> Sections,
    IReadOnlyList<string> Variables,
    IReadOnlyList<string> Techniques)
{
    public static CustomShaderInjectionResult Skipped { get; } = new(false, false, false, false, false, false, "Custom shader section injection not attempted.", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
}

public sealed record TechniqueOrderOptimizationResult(
    bool Attempted,
    bool Changed,
    string Message,
    IReadOnlyList<string> OptimizedKeys,
    int MovedEntryCount)
{
    public static TechniqueOrderOptimizationResult Skipped { get; } = new(false, false, "Technique load-order optimization not attempted.", Array.Empty<string>(), 0);
}

public sealed record ShaderSupportItem(string Section, string Key, bool Controllable, string ReasonCategory, TechniqueActivationState ActivationState);

public sealed record ShaderSupportScan(bool Success, string Message, IReadOnlyList<ShaderSupportItem> Items)
{
    public static ShaderSupportScan Skipped(string message) => new(false, message, Array.Empty<ShaderSupportItem>());
}

public sealed class PresetWriter
{
    private sealed record KnownCustomShaderDefinition(string Section, string Technique, string TechniqueEntry, IReadOnlyList<string> Variables);
    private sealed record TechniqueSyncResult(bool Attempted, bool Activated, bool Deactivated, IReadOnlyList<string> ActiveTechniques, IReadOnlyList<string> KeysChanged);

    private static readonly IReadOnlyList<string> SmartSharpenMaterialIntentShaderVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection"
    ];

    private static readonly IReadOnlyList<string> WeatherAtmosphereMaterialIntentShaderVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_WaterContext",
        "Dalashade_CoastalContext",
        "Dalashade_OpenOceanContext",
        "Dalashade_ShallowWaterContext",
        "Dalashade_WetSurfaceContext",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    private static readonly IReadOnlyList<string> AtmosphereBloomMaterialIntentShaderVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection"
    ];

    private static readonly IReadOnlyList<string> AdaptiveGradeMaterialIntentShaderVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_WaterContext",
        "Dalashade_CoastalContext",
        "Dalashade_OpenOceanContext",
        "Dalashade_ShallowWaterContext",
        "Dalashade_WetSurfaceContext",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    private static readonly IReadOnlyList<string> MaterialDebugShaderVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_WaterContext",
        "Dalashade_CoastalContext",
        "Dalashade_OpenOceanContext",
        "Dalashade_ShallowWaterContext",
        "Dalashade_WetSurfaceContext",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    private static readonly IReadOnlyList<string> NormalFieldShaderVariables =
    [
        "Dalashade_NormalDebugEnabled",
        "Dalashade_NormalDebugMode",
        "Dalashade_NormalDebugBoost",
        "Dalashade_NormalFieldEnabled",
        "Dalashade_NormalFieldStrength",
        "Dalashade_NormalDepthStrength",
        "Dalashade_NormalDetailStrength",
        "Dalashade_NormalMaterialInfluence",
        "Dalashade_NormalWaterSuppression",
        "Dalashade_NormalSkinSuppression",
        "Dalashade_NormalSkySuppression"
    ];

    private static readonly IReadOnlyList<string> FrameDataDebugShaderVariables =
    [
        "Dalashade_HighlightProtection",
        "Dalashade_FrameDataDebugMode",
        "Dalashade_FrameDataDebugBoost",
        "Dalashade_FrameDataDebugOpacity"
    ];

    private static readonly IReadOnlyList<string> FirstPartyShaderModeVariables =
    [
        "Dalashade_StandaloneStrength"
    ];

    private static readonly IReadOnlyList<string> DalapadSurfaceShaderVariables =
    [
        "Dalashade_DalapadEnabled",
        "Dalashade_DalapadSurfaceDataEnabled",
        "Dalashade_DalapadSurfaceDataStrength",
        "Dalashade_DalapadSceneGINormalAssist",
        "Dalashade_DalapadSceneGINormalStrength"
    ];

    private static readonly IReadOnlyList<string> SceneGIMaterialIntentShaderVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_WaterContext",
        "Dalashade_CoastalContext",
        "Dalashade_OpenOceanContext",
        "Dalashade_ShallowWaterContext",
        "Dalashade_WetSurfaceContext",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    private static readonly IReadOnlyList<string> ContactToneMaterialIntentShaderVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_WaterContext",
        "Dalashade_WetSurfaceContext",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    private static readonly IReadOnlyList<string> SurfaceReflectionMaterialIntentShaderVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_WaterContext",
        "Dalashade_CoastalContext",
        "Dalashade_OpenOceanContext",
        "Dalashade_ShallowWaterContext",
        "Dalashade_WetSurfaceContext",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    private static readonly IReadOnlyList<string> DepthAssistShaderOwnedVariables =
    [
        "Dalashade_EnableDepthAssist",
        "Dalashade_DepthAssistStrength",
        "Dalashade_DepthAssistConfidenceFloor",
        "Dalashade_DepthConfidenceFloor"
    ];

    private static readonly IReadOnlyList<KnownCustomShaderDefinition> KnownCustomShaders =
    [
        new(
            "Dalashade_WeatherAtmosphere.fx",
            "Dalashade_WeatherAtmosphere",
            "Dalashade_WeatherAtmosphere@Dalashade_WeatherAtmosphere.fx",
            WithMaterialIntentVariables(
                WeatherAtmosphereMaterialIntentShaderVariables,
                "Dalashade_Haze",
                "Dalashade_Wetness",
                "Dalashade_Cold",
                "Dalashade_Heat",
                "Dalashade_HighlightProtection",
                "Dalashade_ShadowProtection",
                "Dalashade_CombatPressure",
                "Dalashade_Atmosphere",
                "Dalashade_MagicGlow",
                "Dalashade_NeonGlow",
                "Dalashade_FoliageDensity",
                "Dalashade_IndustrialHardness",
                "Dalashade_CosmicMood",
                "Dalashade_Readability",
                "Dalashade_Night",
                "Dalashade_Moonlight",
                "Dalashade_ArtificialLight",
                "Dalashade_AmbientDarkness",
                "Dalashade_NightAtmosphere",
                "Dalashade_Daylight",
                "Dalashade_Sunlight",
                "Dalashade_OpenSkyLight",
                "Dalashade_SurfaceHeat",
                "Dalashade_DayAtmosphere",
                "Dalashade_DayReflection",
                "Dalashade_DayHighlightPressure",
                "Dalashade_CinematicPermission",
                "Dalashade_EnableDepthAssist",
                "Dalashade_DepthAssistStrength",
                "Dalashade_DepthAssistConfidenceFloor",
                "Dalashade_DepthConfidenceFloor")
                .Concat(FirstPartyShaderModeVariables)
                .Concat(DalapadSurfaceShaderVariables)
                .Concat(NormalFieldShaderVariables)
                .ToArray()),
        new(
            "Dalashade_AdaptiveGrade.fx",
            "Dalashade_AdaptiveGrade",
            "Dalashade_AdaptiveGrade@Dalashade_AdaptiveGrade.fx",
            WithMaterialIntentVariables(
                AdaptiveGradeMaterialIntentShaderVariables,
                "Dalashade_Readability",
                "Dalashade_Atmosphere",
                "Dalashade_HighlightProtection",
                "Dalashade_ShadowProtection",
                "Dalashade_Cold",
                "Dalashade_Heat",
                "Dalashade_MagicGlow",
                "Dalashade_NeonGlow",
                "Dalashade_FoliageDensity",
                "Dalashade_IndustrialHardness",
                "Dalashade_CosmicMood",
                "Dalashade_Night",
                "Dalashade_Moonlight",
                "Dalashade_ArtificialLight",
                "Dalashade_AmbientDarkness",
                "Dalashade_NightAtmosphere",
                "Dalashade_Daylight",
                "Dalashade_Sunlight",
                "Dalashade_OpenSkyLight",
                "Dalashade_SurfaceHeat",
                "Dalashade_DayAtmosphere",
                "Dalashade_DayReflection",
                "Dalashade_DayHighlightPressure",
                "Dalashade_CinematicPermission",
                "Dalashade_CombatPressure",
                "Dalashade_EnableDepthAssist",
                "Dalashade_DepthAssistStrength",
                "Dalashade_DepthAssistConfidenceFloor",
                "Dalashade_DepthConfidenceFloor")
                .Concat(FirstPartyShaderModeVariables)
                .Concat(DalapadSurfaceShaderVariables)
                .Concat(NormalFieldShaderVariables)
                .ToArray()),
        new(
            "Dalashade_SmartSharpen.fx",
            "Dalashade_SmartSharpen",
            "Dalashade_SmartSharpen@Dalashade_SmartSharpen.fx",
            WithMaterialIntentVariables(
                "Dalashade_Readability",
                "Dalashade_Haze",
                "Dalashade_Wetness",
                "Dalashade_FoliageDensity",
                "Dalashade_CombatPressure",
                "Dalashade_HighlightProtection",
                "Dalashade_Night",
                "Dalashade_AmbientDarkness",
                "Dalashade_ArtificialLight",
                "Dalashade_Daylight",
                "Dalashade_OpenSkyLight",
                "Dalashade_DayHighlightPressure",
                "Dalashade_SharpenAuthority",
                "SharpenStrength",
                "EdgeClarityStrength",
                "StructuralClarityStrength",
                "TextureDetailStrength",
                "AntiCrunchStrength",
                "DepthDampenStrength",
                "FarDepthDampenStrength",
                "FoliageDampenStrength",
                "HighlightDampenStrength",
                "HaloDampenStrength",
                "SkyDampenStrength",
                "HazeDampenStrength",
                "CombatDampenStrength",
                "LumaOnlyStrength")
                .Concat(FirstPartyShaderModeVariables)
                .Concat(DalapadSurfaceShaderVariables)
                .Concat(NormalFieldShaderVariables)
                .Concat([
                "Dalashade_EnableDepthAssist",
                "Dalashade_DepthAssistStrength",
                "Dalashade_DepthAssistConfidenceFloor",
                "Dalashade_DepthConfidenceFloor"])
                .ToArray()),
        new(
            "Dalashade_AtmosphereBloom.fx",
            "Dalashade_AtmosphereBloom",
            "Dalashade_AtmosphereBloom@Dalashade_AtmosphereBloom.fx",
            WithMaterialIntentVariables(
                AtmosphereBloomMaterialIntentShaderVariables,
                "Dalashade_Atmosphere",
                "Dalashade_MagicGlow",
                "Dalashade_NeonGlow",
                "Dalashade_FoliageDensity",
                "Dalashade_Wetness",
                "Dalashade_Heat",
                "Dalashade_Readability",
                "Dalashade_HighlightProtection",
                "Dalashade_Night",
                "Dalashade_Moonlight",
                "Dalashade_ArtificialLight",
                "Dalashade_AmbientDarkness",
                "Dalashade_NightAtmosphere",
                "Dalashade_Daylight",
                "Dalashade_Sunlight",
                "Dalashade_DayAtmosphere",
                "Dalashade_DayHighlightPressure",
                "Dalashade_CombatPressure",
                "Dalashade_CinematicPermission",
                "CanopyGapBloomStrength",
                "Dalashade_EnableDepthAssist",
                "Dalashade_DepthAssistStrength",
                "Dalashade_DepthAssistConfidenceFloor",
                "Dalashade_DepthConfidenceFloor")
                .Concat(FirstPartyShaderModeVariables)
                .Concat(DalapadSurfaceShaderVariables)
                .Concat(NormalFieldShaderVariables)
                .ToArray()),
        new(
            "Dalashade_MaterialDebug.fx",
            "Dalashade_MaterialDebug",
            "Dalashade_MaterialDebug@Dalashade_MaterialDebug.fx",
            MaterialDebugShaderVariables.Concat(DepthAssistShaderOwnedVariables).ToArray()),
        new(
            "Dalashade_NormalDebug.fx",
            "Dalashade_NormalDebug",
            "Dalashade_NormalDebug@Dalashade_NormalDebug.fx",
            MaterialDebugShaderVariables
                .Concat(DepthAssistShaderOwnedVariables)
                .Concat(NormalFieldShaderVariables)
                .ToArray()),
        new(
            "Dalashade_FrameDataDebug.fx",
            "Dalashade_FrameDataDebug",
            "Dalashade_FrameDataDebug@Dalashade_FrameDataDebug.fx",
            MaterialDebugShaderVariables
                .Concat(DepthAssistShaderOwnedVariables)
                .Concat(NormalFieldShaderVariables)
                .Concat(DalapadSurfaceShaderVariables)
                .Concat(FrameDataDebugShaderVariables)
                .ToArray()),
        new(
            "Dalashade_SceneGI.fx",
            "Dalashade_SceneGI",
            "Dalashade_SceneGI@Dalashade_SceneGI.fx",
            WithMaterialIntentVariables(
                SceneGIMaterialIntentShaderVariables,
                "Dalashade_GIEnabled",
                "Dalashade_GIStrength",
                "Dalashade_GIRadius",
                "Dalashade_GIBounceStrength",
                "Dalashade_GIAOIntensity",
                "Dalashade_GIAORadius",
                "Dalashade_GINightLightStrength",
                "Dalashade_GIMaterialInfluence",
                "Dalashade_GISkyReject",
                "Dalashade_GISkinProtect",
                "Dalashade_GIDebugMode",
                "Dalashade_GIDebugOutputMode",
                "Dalashade_GIDebugOpacity",
                "Dalashade_GIDebugBoost",
                "Dalashade_IntentReadability",
                "Dalashade_IntentAtmosphere",
                "Dalashade_IntentHighlightProtection",
                "Dalashade_IntentShadowProtection",
                "Dalashade_IntentHaze",
                "Dalashade_IntentWetness",
                "Dalashade_IntentCold",
                "Dalashade_IntentHeat",
                "Dalashade_IntentMagicGlow",
                "Dalashade_IntentNeonGlow",
                "Dalashade_IntentFoliageDensity",
                "Dalashade_IntentIndustrialHardness",
                "Dalashade_IntentCosmicMood",
                "Dalashade_Night",
                "Dalashade_Moonlight",
                "Dalashade_ArtificialLight",
                "Dalashade_AmbientDarkness",
                "Dalashade_NightAtmosphere",
                "Dalashade_Daylight",
                "Dalashade_Sunlight",
                "Dalashade_OpenSkyLight",
                "Dalashade_SurfaceHeat",
                "Dalashade_DayAtmosphere",
                "Dalashade_DayReflection",
                "Dalashade_DayHighlightPressure",
                "Dalashade_IntentCombatPressure",
                "Dalashade_IntentCinematicPermission")
                .Concat(FirstPartyShaderModeVariables)
                .Concat(DalapadSurfaceShaderVariables)
                .Concat(NormalFieldShaderVariables)
                .Concat(DepthAssistShaderOwnedVariables)
                .ToArray()),
        new(
            "Dalashade_ContactTone.fx",
            "Dalashade_ContactTone",
            "Dalashade_ContactTone@Dalashade_ContactTone.fx",
            WithMaterialIntentVariables(
                ContactToneMaterialIntentShaderVariables,
                "Dalashade_ContactToneEnabled",
                "Dalashade_ContactToneStrength",
                "Dalashade_ContactToneRadius",
                "Dalashade_ContactToneEdgeStrength",
                "Dalashade_ContactToneStructureStrength",
                "Dalashade_ContactToneContrastStrength",
                "Dalashade_ContactToneDebugMode",
                "Dalashade_ContactToneDebugOpacity",
                "Dalashade_Readability",
                "Dalashade_Atmosphere",
                "Dalashade_HighlightProtection",
                "Dalashade_ShadowProtection",
                "Dalashade_Wetness",
                "Dalashade_FoliageDensity",
                "Dalashade_IndustrialHardness",
                "Dalashade_CombatPressure",
                "Dalashade_CinematicPermission")
                .Concat(FirstPartyShaderModeVariables)
                .Concat(DalapadSurfaceShaderVariables)
                .Concat(NormalFieldShaderVariables)
                .Concat(DepthAssistShaderOwnedVariables)
                .ToArray()),
        new(
            "Dalashade_SurfaceReflection.fx",
            "Dalashade_SurfaceReflection",
            "Dalashade_SurfaceReflection@Dalashade_SurfaceReflection.fx",
            WithMaterialIntentVariables(
                SurfaceReflectionMaterialIntentShaderVariables,
                "Dalashade_SurfaceReflectionEnabled",
                "Dalashade_SurfaceReflectionStrength",
                "Dalashade_WaterSheenStrength",
                "Dalashade_WaterReflectionStrength",
                "Dalashade_WaterSheenRadius",
                "Dalashade_SpecularGlintStrength",
                "Dalashade_SpecularReflectionStrength",
                "Dalashade_WetReflectionStrength",
                "Dalashade_AetherReflectionStrength",
                "Dalashade_NeonReflectionStrength",
                "Dalashade_IceSheenStrength",
                "Dalashade_SurfaceReflectionSkyReject",
                "Dalashade_SurfaceReflectionSkinProtect",
                "Dalashade_ReflectionSampleOffset",
                "Dalashade_ReflectionSoftness",
                "Dalashade_ReflectionDepthReject",
                "Dalashade_SurfaceReflectionDebugMode",
                "Dalashade_SurfaceReflectionDebugOutputMode",
                "Dalashade_SurfaceReflectionDebugOpacity",
                "Dalashade_SurfaceReflectionDebugBoost",
                "Dalashade_WaterContext",
                "Dalashade_CoastalContext",
                "Dalashade_OpenOceanContext",
                "Dalashade_ShallowWaterContext",
                "Dalashade_WetSurfaceContext",
                "Dalashade_Wetness",
                "Dalashade_HighlightProtection",
                "Dalashade_Readability",
                "Dalashade_CombatPressure",
                "Dalashade_MagicGlow",
                "Dalashade_NeonGlow",
                "Dalashade_Night",
                "Dalashade_ArtificialLight",
                "Dalashade_OpenSkyLight",
                "Dalashade_DayReflection",
                "Dalashade_DayHighlightPressure",
                "Dalashade_CinematicPermission")
                .Concat(FirstPartyShaderModeVariables)
                .Concat(DalapadSurfaceShaderVariables)
                .Concat(NormalFieldShaderVariables)
                .Concat(DepthAssistShaderOwnedVariables)
                .ToArray())
    ];

    private readonly ShaderVariableMapper mapper = new();
    private readonly CustomShaderVariableMapper customMapper = new();
    private readonly PresetAnalyzer analyzer = new();
    private readonly SanitizeActionPipeline sanitizeActionPipeline = new();

    public PresetWriteResult WriteGeneratedPreset(Configuration configuration, VisualProfile profile, SceneIntent? sceneIntent = null, MaterialIntent? materialIntent = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.BasePresetPath))
            {
                return PresetWriteResult.Skipped("Base preset path is empty.");
            }

            if (string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath))
            {
                return PresetWriteResult.Skipped("Generated preset path is empty.");
            }

            var basePresetPath = Path.GetFullPath(configuration.BasePresetPath);
            var generatedPresetPath = Path.GetFullPath(configuration.GeneratedPresetPath);

            if (string.Equals(basePresetPath, generatedPresetPath, StringComparison.OrdinalIgnoreCase))
            {
                return PresetWriteResult.Skipped("Generated preset path must be different from the base preset path.");
            }

            if (!File.Exists(basePresetPath))
            {
                return PresetWriteResult.Skipped("Base preset was not found.");
            }

            var generatedDirectory = Path.GetDirectoryName(generatedPresetPath);
            if (string.IsNullOrWhiteSpace(generatedDirectory))
            {
                return PresetWriteResult.Skipped("Generated preset path is invalid.");
            }

            Directory.CreateDirectory(generatedDirectory);

            var lines = File.ReadAllLines(basePresetPath).ToList();
            var injectionResult = InjectCustomShaderSections(configuration, lines);
            var analysis = analyzer.Analyze(configuration);
            var smartSharpenAuthority = SmartSharpenAuthority.Analyze(analysis);
            var authorityPolicy = GenerationAuthorityPolicy.From(analysis, configuration.CompatibilityMode);
            var adjustments = mapper.CreateAdjustments(profile, configuration, authorityPolicy);
            sceneIntent ??= SceneIntent.Neutral;
            materialIntent ??= MaterialIntent.Neutral;
            var activationMap = PresetAnalyzer.ParseTechniqueActivationMap(lines);
            var changes = new List<ChangedShaderVariable>();
            var currentSection = string.Empty;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (TryReadSection(line, out var section))
                {
                    currentSection = section;
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var generatedCustomShaderVariable = IsGeneratedCustomShaderVariable(injectionResult, currentSection, key);
                if (!TryGetAdjustment(adjustments, configuration.ShaderMatchingMode, currentSection, key, out var adjust)
                    && !(configuration.EnableDalashadeCustomShaders && SmartSharpenAuthority.TryGetAdjustment(currentSection, key, sceneIntent, smartSharpenAuthority, out adjust))
                    && !customMapper.TryGetAdjustment(configuration, currentSection, key, sceneIntent, materialIntent, out adjust))
                {
                    continue;
                }

                var activationState = PresetAnalyzer.GetTechniqueActivationState(activationMap, currentSection);
                if (activationState != TechniqueActivationState.Active
                    && configuration.InactiveShaderWriteMode == InactiveShaderWriteMode.Never
                    && !generatedCustomShaderVariable)
                {
                    continue;
                }

                if (activationState == TechniqueActivationState.Inactive
                    && configuration.InactiveShaderWriteMode == InactiveShaderWriteMode.SupportedInactiveSections
                    && !HasExactAdjustment(adjustments, currentSection, key)
                    && !generatedCustomShaderVariable)
                {
                    continue;
                }

                if (activationState == TechniqueActivationState.Unknown
                    && !HasExactAdjustment(adjustments, currentSection, key)
                    && !generatedCustomShaderVariable)
                {
                    continue;
                }

                var currentValue = line[(separatorIndex + 1)..];
                var adjusted = adjust.Apply(currentValue);
                if (adjusted == null)
                {
                    continue;
                }

                var adjustedValue = adjusted.NewValue;
                lines[i] = $"{line[..(separatorIndex + 1)]}{adjustedValue}";
                if (!string.Equals(currentValue, adjustedValue, StringComparison.Ordinal))
                {
                    changes.Add(new ChangedShaderVariable(
                        currentSection,
                        key,
                        currentValue,
                        adjustedValue,
                        adjust.ReasonCategory,
                        activationState,
                        adjusted.HitMin,
                        adjusted.HitMax,
                        adjust.AuthorityAdjustmentStrength,
                        CombineWarnings(adjusted.Warning, activationState == TechniqueActivationState.Unknown ? "Active technique state could not be confirmed." : null)));
                }
            }

            var writableLines = lines.ToArray();
            var sanitizeChanges = sanitizeActionPipeline.Apply(writableLines, configuration, activationMap, authorityPolicy);
            var orderOptimization = OptimizeTechniqueOrder(configuration, writableLines);

            if (configuration.WriteBackups && File.Exists(generatedPresetPath))
            {
                var backupPath = CreateBackupPath(generatedPresetPath);
                File.Copy(generatedPresetPath, backupPath, false);
                PruneBackups(generatedPresetPath, configuration.MaxGeneratedPresetBackups);
            }

            var tempPath = $"{generatedPresetPath}.tmp";
            File.WriteAllLines(tempPath, writableLines);
            ReplaceFile(tempPath, generatedPresetPath);

            var inactiveChanges = changes.Count(change => change.ActivationState == TechniqueActivationState.Inactive);
            var unknownChanges = changes.Count(change => change.ActivationState == TechniqueActivationState.Unknown);
            var clampedChanges = changes.Count(change => change.HitMin || change.HitMax);
            var warningChanges = changes.Count(change => !string.IsNullOrWhiteSpace(change.Warning));
            var dampenedChanges = changes.Count(change => change.AuthorityAdjustmentStrength < 0.999f);
            var sanitizeChangeCount = sanitizeChanges.Count;
            var message = $"Generated preset written with {changes.Count} supported variable change(s).";
            if (inactiveChanges > 0 || unknownChanges > 0 || clampedChanges > 0 || warningChanges > 0 || dampenedChanges > 0 || sanitizeChangeCount > 0)
            {
                message += $" {inactiveChanges} inactive, {unknownChanges} unknown, {clampedChanges} clamped, {warningChanges} warning(s), {dampenedChanges} authority-dampened, {sanitizeChangeCount} sanitize action(s).";
            }

            if (injectionResult.Attempted)
            {
                message += $" {injectionResult.Message}";
            }

            if (orderOptimization.Attempted)
            {
                message += $" {orderOptimization.Message}";
            }

            return new PresetWriteResult(true, message, changes.Count, changes, sanitizeChanges, injectionResult, orderOptimization);
        }
        catch (UnauthorizedAccessException ex)
        {
            return PresetWriteResult.Skipped($"Preset write denied: {ex.Message} Choose a generated preset path in a writable folder, not the protected game/ReShade install folder.");
        }
        catch (IOException ex)
        {
            return PresetWriteResult.Skipped($"Preset write failed: {ex.Message}");
        }
    }

    public ShaderSupportScan ScanSupportedVariables(Configuration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.BasePresetPath))
            {
                return ShaderSupportScan.Skipped("Base preset path is empty.");
            }

            var basePresetPath = Path.GetFullPath(configuration.BasePresetPath);
            if (!File.Exists(basePresetPath))
            {
                return ShaderSupportScan.Skipped("Base preset was not found.");
            }

            var adjustments = mapper.CreateAdjustments(VisualProfile.Neutral, configuration);
            var lines = File.ReadAllLines(basePresetPath);
            var analysis = analyzer.Analyze(configuration);
            var smartSharpenAuthority = SmartSharpenAuthority.Analyze(analysis);
            var activationMap = PresetAnalyzer.ParseTechniqueActivationMap(lines);
            var items = new List<ShaderSupportItem>();
            var seen = new HashSet<ShaderVariableKey>(ShaderVariableKeyComparer.Instance);
            var currentSection = string.Empty;

            foreach (var line in lines)
            {
                if (TryReadSection(line, out var section))
                {
                    currentSection = section;
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                if (!TryGetAdjustment(adjustments, configuration.ShaderMatchingMode, currentSection, key, out var adjust)
                    && !(configuration.EnableDalashadeCustomShaders && SmartSharpenAuthority.TryGetAdjustment(currentSection, key, SceneIntent.Neutral, smartSharpenAuthority, out adjust))
                    && !customMapper.TryGetAdjustment(configuration, currentSection, key, SceneIntent.Neutral, MaterialIntent.Neutral, out adjust))
                {
                    continue;
                }

                var itemKey = new ShaderVariableKey(currentSection, key);
                if (!seen.Add(itemKey))
                {
                    continue;
                }

                items.Add(new ShaderSupportItem(
                    currentSection,
                    key,
                    true,
                    adjust.ReasonCategory,
                    PresetAnalyzer.GetTechniqueActivationState(activationMap, currentSection)));
            }

            var activeCount = items.Count(item => item.ActivationState == TechniqueActivationState.Active);
            var inactiveCount = items.Count(item => item.ActivationState == TechniqueActivationState.Inactive);
            var unknownCount = items.Count(item => item.ActivationState == TechniqueActivationState.Unknown);
            var message = items.Count == 0
                ? "No controllable shader variables detected in the base preset."
                : $"Detected {items.Count} controllable shader variable(s): {activeCount} active, {inactiveCount} inactive, {unknownCount} unknown.";
            return new ShaderSupportScan(true, message, items.OrderBy(item => item.Section).ThenBy(item => item.Key).ToArray());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return ShaderSupportScan.Skipped($"Shader support scan failed: {ex.Message}");
        }
    }

    private static bool TryGetAdjustment(IReadOnlyDictionary<ShaderVariableKey, ShaderAdjustment> adjustments, ShaderMatchingMode matchingMode, string section, string key, out ShaderAdjustment adjustment)
    {
        if (adjustments.TryGetValue(new ShaderVariableKey(section, key), out adjustment!))
        {
            return true;
        }

        if (matchingMode == ShaderMatchingMode.KnownFallbacks && adjustments.TryGetValue(new ShaderVariableKey(null, key), out adjustment!))
        {
            return true;
        }

        if (matchingMode == ShaderMatchingMode.LooseKeys)
        {
            var looseMatch = adjustments.FirstOrDefault(pair => string.Equals(pair.Key.Key, key, StringComparison.OrdinalIgnoreCase));
            if (looseMatch.Value != null)
            {
                adjustment = looseMatch.Value;
                return true;
            }
        }

        adjustment = null!;
        return false;
    }

    private static bool HasExactAdjustment(IReadOnlyDictionary<ShaderVariableKey, ShaderAdjustment> adjustments, string section, string key)
    {
        return adjustments.ContainsKey(new ShaderVariableKey(section, key));
    }

    private static string? CombineWarnings(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return string.IsNullOrWhiteSpace(second) ? null : second;
        }

        if (string.IsNullOrWhiteSpace(second))
        {
            return first;
        }

        return $"{first} {second}";
    }

    private static CustomShaderInjectionResult InjectCustomShaderSections(Configuration configuration, List<string> lines)
    {
        if ((!configuration.EnableDalashadeCustomShaders || !configuration.AutoInjectDalashadeCustomShaderSections)
            && !configuration.SyncDalashadeTechniqueActivation)
        {
            return CustomShaderInjectionResult.Skipped;
        }

        var sectionInjected = false;
        var variablesInjected = false;
        var injectedSections = new List<string>();
        var injectedVariables = new List<string>();

        if (configuration.EnableDalashadeCustomShaders && configuration.AutoInjectDalashadeCustomShaderSections)
        {
            foreach (var shader in KnownCustomShaders)
            {
                var shaderVariables = VariablesForInjection(configuration, shader);
                if (shaderVariables.Count == 0)
                {
                    continue;
                }

                if (!ContainsSection(lines, shader.Section))
                {
                    if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                    {
                        lines.Add(string.Empty);
                    }

                    lines.Add($"[{shader.Section}]");
                    foreach (var variable in shaderVariables)
                    {
                        lines.Add($"{variable}={DefaultInjectedCustomShaderValue(shader.Section, variable)}");
                        injectedVariables.Add($"{shader.Section}/{variable}");
                    }

                    sectionInjected = true;
                    variablesInjected = true;
                    injectedSections.Add(shader.Section);
                }
                else
                {
                    var insertIndex = FindSectionEnd(lines, shader.Section);
                    var existingVariables = ReadSectionKeys(lines, shader.Section);
                    foreach (var variable in shaderVariables)
                    {
                        if (existingVariables.Contains(variable))
                        {
                            continue;
                        }

                        lines.Insert(insertIndex, $"{variable}={DefaultInjectedCustomShaderValue(shader.Section, variable)}");
                        insertIndex++;
                        injectedVariables.Add($"{shader.Section}/{variable}");
                    }

                    variablesInjected = variablesInjected || injectedVariables.Count > 0;
                }
            }
        }

        var techniqueSync = SyncDalashadeTechniqueActivation(configuration, lines);
        var techniqueInjected = techniqueSync.Activated;
        var techniqueDeactivated = techniqueSync.Deactivated;
        var injectedTechniques = techniqueSync.ActiveTechniques;

        var message = sectionInjected || variablesInjected || techniqueInjected || techniqueDeactivated
            ? $"Custom shader injection: section={(sectionInjected ? "yes" : "no")}, variables={(variablesInjected ? "yes" : "no")}, technique={(techniqueInjected ? "yes" : "no")}, deactivated={(techniqueDeactivated ? "yes" : "no")}, generated preset only=yes."
            : "Custom shader injection: known generated preset sections and variables already present; generated preset only=yes.";

        return new CustomShaderInjectionResult(
            true,
            true,
            sectionInjected,
            variablesInjected,
            techniqueInjected,
            techniqueDeactivated,
            message,
            injectedSections.ToArray(),
            injectedVariables.ToArray(),
            injectedTechniques.ToArray());
    }

    private static IReadOnlyList<string> WithMaterialIntentVariables(params string[] variables)
    {
        return WithMaterialIntentVariables(SmartSharpenMaterialIntentShaderVariables, variables);
    }

    private static IReadOnlyList<string> WithMaterialIntentVariables(IReadOnlyList<string> materialVariables, params string[] variables)
    {
        return variables
            .Concat(materialVariables)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> VariablesForInjection(Configuration configuration, KnownCustomShaderDefinition shader)
    {
        return shader.Variables
            .Where(variable => ShouldWriteMaterialIntentVariables(configuration) || !CustomShaderVariableMapper.IsKnownMaterialIntentVariable(variable))
            .Where(variable => ShouldWriteNormalFieldVariables(configuration) || !CustomShaderVariableMapper.IsKnownNormalFieldVariable(variable))
            .Where(variable => ShouldWriteDalapadSurfaceVariables(configuration) || !CustomShaderVariableMapper.IsKnownDalapadSurfaceVariable(variable))
            .ToArray();
    }

    private static bool ShouldWriteNormalFieldVariables(Configuration configuration)
    {
        return configuration.EnableNormalField
               && configuration.EnableNormalFieldShaderMapping
               && configuration.NormalFieldStrength > 0f;
    }

    private static bool ShouldWriteDalapadSurfaceVariables(Configuration configuration)
    {
        return configuration.EnableDalapadShaderIntegration
               && (configuration.EnableDalapadSurfaceData || configuration.EnableDalapadSceneGINormalAssist);
    }

    private static string DefaultInjectedCustomShaderValue(string section, string variable)
    {
        if (CustomShaderVariableMapper.IsKnownDalapadSurfaceVariable(variable))
        {
            return variable switch
            {
                "Dalashade_DalapadSurfaceDataStrength" => "0.750000",
                "Dalashade_DalapadSceneGINormalStrength" => "0.350000",
                _ => "0"
            };
        }

        if (section.Contains("Dalashade_FrameDataDebug", StringComparison.OrdinalIgnoreCase))
        {
            return variable switch
            {
                "Dalashade_FrameDataDebugMode" => "0",
                "Dalashade_FrameDataDebugBoost" => "2.000000",
                "Dalashade_FrameDataDebugOpacity" => "1.000000",
                _ => "0.000000"
            };
        }

        if (section.Contains("Dalashade_SurfaceReflection", StringComparison.OrdinalIgnoreCase))
        {
            return variable switch
            {
                "Dalashade_SurfaceReflectionEnabled" => "0.000000",
                "Dalashade_SurfaceReflectionStrength" => "0.320000",
                "Dalashade_WaterSheenStrength" => "0.380000",
                "Dalashade_WaterReflectionStrength" => "0.450000",
                "Dalashade_WaterSheenRadius" => "1.350000",
                "Dalashade_SpecularGlintStrength" => "0.320000",
                "Dalashade_SpecularReflectionStrength" => "0.300000",
                "Dalashade_WetReflectionStrength" => "0.300000",
                "Dalashade_AetherReflectionStrength" => "0.360000",
                "Dalashade_NeonReflectionStrength" => "0.340000",
                "Dalashade_IceSheenStrength" => "0.240000",
                "Dalashade_SurfaceReflectionSkyReject" => "1.000000",
                "Dalashade_SurfaceReflectionSkinProtect" => "1.000000",
                "Dalashade_ReflectionSampleOffset" => "0.018000",
                "Dalashade_ReflectionSoftness" => "0.500000",
                "Dalashade_ReflectionDepthReject" => "0.650000",
                "Dalashade_SurfaceReflectionDebugMode" => "0",
                "Dalashade_SurfaceReflectionDebugOutputMode" => "0",
                "Dalashade_SurfaceReflectionDebugOpacity" => "0.750000",
                "Dalashade_SurfaceReflectionDebugBoost" => "2.250000",
                _ => "0.000000"
            };
        }

        if (section.Contains("Dalashade_ContactTone", StringComparison.OrdinalIgnoreCase))
        {
            return variable switch
            {
                "Dalashade_ContactToneEnabled" => "0.000000",
                "Dalashade_ContactToneStrength" => "0.420000",
                "Dalashade_ContactToneRadius" => "0.620000",
                "Dalashade_ContactToneEdgeStrength" => "0.480000",
                "Dalashade_ContactToneStructureStrength" => "0.440000",
                "Dalashade_ContactToneContrastStrength" => "0.340000",
                "Dalashade_ContactToneDebugMode" => "0",
                "Dalashade_ContactToneDebugOpacity" => "0.750000",
                _ => "0.000000"
            };
        }

        if (section.Contains("Dalashade_AtmosphereBloom", StringComparison.OrdinalIgnoreCase))
        {
            return variable switch
            {
                "BloomStrength" => "0.320000",
                "BloomThreshold" => "0.740000",
                "DiffusionStrength" => "0.420000",
                "CanopyGapBloomStrength" => "0.340000",
                "MagicGlowStrength" => "0.480000",
                "NeonGlowStrength" => "0.420000",
                "HighlightRestraint" => "0.700000",
                "CombatDampenStrength" => "0.720000",
                "CinematicBoostStrength" => "0.340000",
                "Dalashade_MaterialDebugMode" => "0",
                "Dalashade_MaterialDebugStrength" => "1.000000",
                _ => "0.000000"
            };
        }

        if (!section.Contains("Dalashade_SceneGI", StringComparison.OrdinalIgnoreCase))
        {
            return "0.000000";
        }

        return variable switch
        {
            "Dalashade_GIEnabled" => "1.000000",
            "Dalashade_GIStrength" => "0.450000",
            "Dalashade_GIRadius" => "0.650000",
            "Dalashade_GIBounceStrength" => "0.300000",
            "Dalashade_GIAOIntensity" => "0.300000",
            "Dalashade_GIAORadius" => "0.450000",
            "Dalashade_GINightLightStrength" => "0.420000",
            "Dalashade_GIMaterialInfluence" => "0.580000",
            "Dalashade_GISkyReject" => "1.000000",
            "Dalashade_GISkinProtect" => "1.000000",
            "Dalashade_GIDebugOutputMode" => "0",
            "Dalashade_GIDebugOpacity" => "0.750000",
            "Dalashade_GIDebugBoost" => "2.500000",
            _ => "0.000000"
        };
    }

    private static bool ShouldWriteMaterialIntentVariables(Configuration configuration)
    {
        return configuration.EnableMaterialIntent
               && configuration.EnableMaterialIntentShaderMapping
               && configuration.MaterialIntentStrength > 0f;
    }

    private static bool ContainsSection(IEnumerable<string> lines, string section)
    {
        return lines.Any(line => TryReadSection(line, out var currentSection)
                                 && string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase));
    }

    private static int FindSectionEnd(IReadOnlyList<string> lines, string section)
    {
        var inSection = false;
        var lastContentIndex = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (TryReadSection(lines[i], out var currentSection))
            {
                if (inSection)
                {
                    return lastContentIndex >= 0 ? lastContentIndex + 1 : i;
                }

                inSection = string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase);
                if (inSection)
                {
                    lastContentIndex = i;
                }
                continue;
            }

            if (inSection && !string.IsNullOrWhiteSpace(lines[i]))
            {
                lastContentIndex = i;
            }
        }

        return lastContentIndex >= 0 ? lastContentIndex + 1 : lines.Count;
    }

    private static HashSet<string> ReadSectionKeys(IEnumerable<string> lines, string section)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inSection = false;
        foreach (var line in lines)
        {
            if (TryReadSection(line, out var currentSection))
            {
                if (inSection && !string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                inSection = string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inSection)
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex > 0)
            {
                keys.Add(line[..separatorIndex].Trim());
            }
        }

        return keys;
    }

    private static bool IsGeneratedCustomShaderVariable(CustomShaderInjectionResult injection, string section, string key)
    {
        return injection.Variables.Any(variable =>
            string.Equals(variable, $"{section}/{key}", StringComparison.OrdinalIgnoreCase));
    }

    private static TechniqueSyncResult SyncDalashadeTechniqueActivation(Configuration configuration, List<string> lines)
    {
        if (!configuration.SyncDalashadeTechniqueActivation)
        {
            return new TechniqueSyncResult(false, false, false, Array.Empty<string>(), Array.Empty<string>());
        }

        var activeDefinitions = KnownCustomShaders
            .Where(IsAutoActivatableDalashadeTechnique)
            .Where(shader => ShouldActivateDalashadeTechnique(configuration, shader))
            .ToArray();
        var activeEntries = activeDefinitions
            .Select(shader => ParseTechniqueOrderEntries(shader.TechniqueEntry).First())
            .ToArray();
        var changedKeys = new List<string>();
        var activated = false;
        var deactivated = false;

        var techniquesLineIndex = FindPresetKeyLine(lines, "Techniques");
        if (techniquesLineIndex >= 0)
        {
            var separatorIndex = lines[techniquesLineIndex].IndexOf('=');
            var originalEntries = ParseTechniqueOrderEntries(lines[techniquesLineIndex][(separatorIndex + 1)..]);
            var retainedEntries = originalEntries
                .Where(entry => !IsAutoActivatableDalashadeTechnique(entry))
                .ToList();
            deactivated = originalEntries
                .Where(IsAutoActivatableDalashadeTechnique)
                .Any(entry => !activeEntries.Contains(entry, TechniqueOrderEntryComparer.Instance));
            var beforeAddCount = retainedEntries.Count;
            AddMissingTechniqueEntries(retainedEntries, activeEntries);
            activated = retainedEntries.Count != beforeAddCount;
            var sortedEntries = SortTechniqueEntries(retainedEntries);

            if (!originalEntries.SequenceEqual(sortedEntries, TechniqueOrderEntryComparer.Instance))
            {
                lines[techniquesLineIndex] = $"{lines[techniquesLineIndex][..(separatorIndex + 1)]}{string.Join(",", sortedEntries.Select(entry => entry.Raw))}";
                changedKeys.Add("Techniques");
            }
        }
        else if (activeEntries.Length > 0)
        {
            var sortedEntries = SortTechniqueEntries(activeEntries);
            lines.Insert(FindTopLevelInsertIndex(lines), $"Techniques={string.Join(",", sortedEntries.Select(entry => entry.Raw))}");
            activated = true;
            changedKeys.Add("Techniques");
        }

        if (activeEntries.Length > 0)
        {
            var sortingLineIndex = FindPresetKeyLine(lines, "TechniqueSorting");
            if (sortingLineIndex >= 0)
            {
                var separatorIndex = lines[sortingLineIndex].IndexOf('=');
                var originalEntries = ParseTechniqueOrderEntries(lines[sortingLineIndex][(separatorIndex + 1)..]).ToList();
                var beforeAddCount = originalEntries.Count;
                AddMissingTechniqueEntries(originalEntries, activeEntries);
                var sortedEntries = SortTechniqueEntries(originalEntries);
                if (originalEntries.Count != beforeAddCount
                    || !ParseTechniqueOrderEntries(lines[sortingLineIndex][(separatorIndex + 1)..]).SequenceEqual(sortedEntries, TechniqueOrderEntryComparer.Instance))
                {
                    lines[sortingLineIndex] = $"{lines[sortingLineIndex][..(separatorIndex + 1)]}{string.Join(",", sortedEntries.Select(entry => entry.Raw))}";
                    changedKeys.Add("TechniqueSorting");
                }
            }
            else
            {
                var sortedEntries = SortTechniqueEntries(activeEntries);
                lines.Insert(FindTopLevelInsertIndex(lines), $"TechniqueSorting={string.Join(",", sortedEntries.Select(entry => entry.Raw))}");
                changedKeys.Add("TechniqueSorting");
            }
        }

        return new TechniqueSyncResult(
            true,
            activated,
            deactivated,
            activeDefinitions.Select(shader => shader.TechniqueEntry).ToArray(),
            changedKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static bool ShouldActivateDalashadeTechnique(Configuration configuration, KnownCustomShaderDefinition shader)
    {
        if (!configuration.EnableDalashadeCustomShaders || !configuration.AutoInjectDalashadeCustomShaderSections)
        {
            return false;
        }

        if (shader.Section.Contains("SceneGI", StringComparison.OrdinalIgnoreCase))
        {
            return configuration.EnableDalashadeSceneGIShaderVariables;
        }

        if (shader.Section.Contains("ContactTone", StringComparison.OrdinalIgnoreCase))
        {
            return configuration.EnableDalashadeContactToneShaderVariables;
        }

        if (shader.Section.Contains("SurfaceReflection", StringComparison.OrdinalIgnoreCase))
        {
            return configuration.EnableDalashadeSurfaceReflectionShaderVariables;
        }

        return true;
    }

    private static bool IsAutoActivatableDalashadeTechnique(KnownCustomShaderDefinition shader)
    {
        return !shader.Section.Contains("MaterialDebug", StringComparison.OrdinalIgnoreCase)
               && !shader.Section.Contains("NormalDebug", StringComparison.OrdinalIgnoreCase)
               && !shader.Section.Contains("FrameDataDebug", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAutoActivatableDalashadeTechnique(TechniqueOrderEntry entry)
    {
        return KnownCustomShaders
            .Where(IsAutoActivatableDalashadeTechnique)
            .Any(shader =>
                string.Equals(shader.Technique, entry.TechniqueName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(shader.Section, entry.ShaderFile, StringComparison.OrdinalIgnoreCase)
                || string.Equals(shader.TechniqueEntry, entry.DisplayName, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddMissingTechniqueEntries(ICollection<TechniqueOrderEntry> entries, IEnumerable<TechniqueOrderEntry> additions)
    {
        foreach (var addition in additions)
        {
            if (entries.Contains(addition, TechniqueOrderEntryComparer.Instance))
            {
                continue;
            }

            entries.Add(addition);
        }
    }

    private static IReadOnlyList<TechniqueOrderEntry> SortTechniqueEntries(IEnumerable<TechniqueOrderEntry> entries)
    {
        return entries
            .Select((entry, index) => new TechniqueOrderSortEntry(entry, index, TechniqueOrderPhase(entry)))
            .OrderBy(entry => entry.Phase)
            .ThenBy(entry => entry.OriginalIndex)
            .Select(entry => entry.Entry)
            .ToArray();
    }

    private static int FindPresetKeyLine(IReadOnlyList<string> lines, string wantedKey)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (TryReadPresetKey(lines[i], out var key, out _)
                && string.Equals(key, wantedKey, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindTopLevelInsertIndex(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (TryReadSection(lines[i], out _))
            {
                return i;
            }
        }

        return lines.Count;
    }

    private static TechniqueOrderOptimizationResult OptimizeTechniqueOrder(Configuration configuration, string[] lines)
    {
        if (!configuration.OptimizeGeneratedPresetLoadOrder)
        {
            return TechniqueOrderOptimizationResult.Skipped;
        }

        var optimizedKeys = new List<string>();
        var movedEntries = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (!TryReadPresetKey(lines[i], out var key, out var separatorIndex)
                || !IsTechniqueOrderKey(key))
            {
                continue;
            }

            var originalEntries = ParseTechniqueOrderEntries(lines[i][(separatorIndex + 1)..]);
            if (originalEntries.Count <= 1)
            {
                continue;
            }

            var optimizedEntries = originalEntries
                .Select((entry, index) => new TechniqueOrderSortEntry(entry, index, TechniqueOrderPhase(entry)))
                .OrderBy(entry => entry.Phase)
                .ThenBy(entry => entry.OriginalIndex)
                .Select(entry => entry.Entry)
                .ToArray();
            if (originalEntries.SequenceEqual(optimizedEntries, TechniqueOrderEntryComparer.Instance))
            {
                continue;
            }

            movedEntries += CountMovedEntries(originalEntries, optimizedEntries);
            lines[i] = $"{lines[i][..(separatorIndex + 1)]}{string.Join(",", optimizedEntries.Select(entry => entry.Raw))}";
            optimizedKeys.Add(key);
        }

        if (optimizedKeys.Count == 0)
        {
            return new TechniqueOrderOptimizationResult(true, false, "Technique load-order optimization: already ordered or no sortable technique lists found.", Array.Empty<string>(), 0);
        }

        return new TechniqueOrderOptimizationResult(
            true,
            true,
            $"Technique load-order optimization: reordered {movedEntries} entry position(s) across {string.Join("/", optimizedKeys)} without adding or disabling techniques.",
            optimizedKeys.ToArray(),
            movedEntries);
    }

    private static bool IsTechniqueOrderKey(string key)
    {
        return string.Equals(key, "Techniques", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "TechniqueSorting", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<TechniqueOrderEntry> ParseTechniqueOrderEntries(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(raw =>
            {
                var shaderSeparator = raw.LastIndexOf('@');
                return shaderSeparator >= 0 && shaderSeparator < raw.Length - 1
                    ? new TechniqueOrderEntry(raw, raw[..shaderSeparator].Trim(), raw[(shaderSeparator + 1)..].Trim())
                    : new TechniqueOrderEntry(raw, Path.GetFileNameWithoutExtension(raw.Trim()), raw.Trim());
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ShaderFile))
            .ToArray();
    }

    private static int CountMovedEntries(IReadOnlyList<TechniqueOrderEntry> original, IReadOnlyList<TechniqueOrderEntry> optimized)
    {
        var count = 0;
        for (var i = 0; i < Math.Min(original.Count, optimized.Count); i++)
        {
            if (!TechniqueOrderEntryComparer.Instance.Equals(original[i], optimized[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static int TechniqueOrderPhase(TechniqueOrderEntry entry)
    {
        var text = $"{entry.TechniqueName} {entry.ShaderFile}".ToLowerInvariant();
        if (ContainsAny(text, "dalashade_adaptivegrade"))
        {
            return 20;
        }

        if (ContainsAny(text, "dalashade_scenegi"))
        {
            return 32;
        }

        if (ContainsAny(text, "dalashade_contacttone"))
        {
            return 36;
        }

        if (ContainsAny(text, "dalashade_weatheratmosphere"))
        {
            return 42;
        }

        if (ContainsAny(text, "dalashade_atmospherebloom"))
        {
            return 50;
        }

        if (ContainsAny(text, "dalashade_surfacereflection"))
        {
            return 56;
        }

        if (ContainsAny(text, "dalashade_smartsharpen"))
        {
            return 60;
        }

        if (ContainsAny(text, "dalashade_materialdebug", "dalashade_normaldebug", "dalashade_framedatadebug"))
        {
            return 95;
        }

        if (ContainsAny(text, "deband", "denoise"))
        {
            return 10;
        }

        if (ContainsAny(text, "tonemap", "hdr", "levels", "curves", "color", "colour", "grade", "lut", "prod80", "prod_80", "prod80_04", "prod80_03", "prod80_02", "prod80_01", "regrade", "filmicpass", "lightroom"))
        {
            return 20;
        }

        if (ContainsAny(text, "mxao", "ssao", "rtgi", "globalillumination", "gi.fx", "radiantgi", "quint_mxao", "martymods_mxao"))
        {
            return 30;
        }

        if (ContainsAny(text, "fog", "haze", "ambientlight", "diffusion", "magicbloom"))
        {
            return 42;
        }

        if (ContainsAny(text, "bloom", "glow"))
        {
            return 50;
        }

        if (ContainsAny(text, "reflection", "reflect", "glint", "specular"))
        {
            return 56;
        }

        if (ContainsAny(text, "sharpen", "sharp", "cas.fx", "luma", "clarity", "delc"))
        {
            return 60;
        }

        if (ContainsAny(text, "smaa", "fxaa", "taa", "antialias", "anti-alias"))
        {
            return 62;
        }

        if (ContainsAny(text, "dof", "depthoffield", "cinematicdof", "adof"))
        {
            return 70;
        }

        if (ContainsAny(text, "grain", "noise"))
        {
            return 80;
        }

        if (ContainsAny(text, "vignette", "border", "chromatic", "aberration", "prism", "letterbox"))
        {
            return 85;
        }

        if (ContainsAny(text, "debug", "displaydepth", "ui", "overlay"))
        {
            return 95;
        }

        return 40;
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryReadPresetKey(string line, out string key, out int separatorIndex)
    {
        separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            key = string.Empty;
            return false;
        }

        key = line[..separatorIndex].Trim();
        return !string.IsNullOrWhiteSpace(key);
    }

    private static bool TryReadSection(string line, out string section)
    {
        var trimmed = line.Trim();
        if (trimmed.Length > 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            section = trimmed[1..^1];
            return true;
        }

        section = string.Empty;
        return false;
    }

    private sealed record TechniqueOrderEntry(string Raw, string TechniqueName, string ShaderFile)
    {
        public string DisplayName => string.IsNullOrWhiteSpace(TechniqueName) ? ShaderFile : $"{TechniqueName}@{ShaderFile}";
    }

    private sealed record TechniqueOrderSortEntry(TechniqueOrderEntry Entry, int OriginalIndex, int Phase);

    private sealed class TechniqueOrderEntryComparer : IEqualityComparer<TechniqueOrderEntry>
    {
        public static TechniqueOrderEntryComparer Instance { get; } = new();

        public bool Equals(TechniqueOrderEntry? x, TechniqueOrderEntry? y)
        {
            if (x is null || y is null)
            {
                return x is null && y is null;
            }

            return string.Equals(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(TechniqueOrderEntry obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.DisplayName);
        }
    }

    private static void ReplaceFile(string tempPath, string generatedPresetPath)
    {
        try
        {
            if (File.Exists(generatedPresetPath))
            {
                File.SetAttributes(generatedPresetPath, FileAttributes.Normal);
            }

            File.Move(tempPath, generatedPresetPath, true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static string CreateBackupPath(string generatedPresetPath)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff");
        var backupPath = $"{generatedPresetPath}.{timestamp}.bak";
        var suffix = 1;

        while (File.Exists(backupPath))
        {
            backupPath = $"{generatedPresetPath}.{timestamp}.{suffix}.bak";
            suffix++;
        }

        return backupPath;
    }

    private static void PruneBackups(string generatedPresetPath, int maxBackups)
    {
        if (maxBackups <= 0)
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(generatedPresetPath);
        var fileName = Path.GetFileName(generatedPresetPath);
        if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var directory = new DirectoryInfo(directoryPath);
        if (!directory.Exists)
        {
            return;
        }

        var backups = directory.EnumerateFiles($"{fileName}.*.bak", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(maxBackups)
            .ToArray();

        foreach (var backup in backups)
        {
            try
            {
                backup.Delete();
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
