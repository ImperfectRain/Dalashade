using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record FirstPartyShaderMetadata(
    string Family,
    string DisplayName,
    string FileName,
    string TechniqueName,
    string SectionName,
    string Role,
    bool ProductionShader,
    bool ManualDebugShader,
    bool TechniqueSyncEligible,
    IReadOnlyList<string> IncludeFiles,
    IReadOnlyList<string> KnownGeneratedUniforms,
    IReadOnlyList<string> PerformanceTierUniforms,
    IReadOnlyList<string> DebugUniforms,
    IReadOnlyList<string> Notes);

public static class FirstPartyShaderRegistry
{
    public static IReadOnlyList<FirstPartyShaderMetadata> All { get; } =
    [
        new(
            Family: "AdaptiveGrade",
            DisplayName: "Adaptive Grade",
            FileName: "Dalashade_AdaptiveGrade.fx",
            TechniqueName: "Dalashade_AdaptiveGrade",
            SectionName: "Dalashade_AdaptiveGrade",
            Role: "Scene-aware color/tone protection",
            ProductionShader: true,
            ManualDebugShader: false,
            TechniqueSyncEligible: true,
            IncludeFiles: ["Dalashade_FrameData.fxh", "Dalashade_MaterialMasks.fxh", "Dalashade_NormalField.fxh"],
            KnownGeneratedUniforms: SharedProductionUniforms.Concat(MaterialUniforms).Concat(DepthAssistUniforms).Concat(NormalFieldUniforms).ToArray(),
            PerformanceTierUniforms: ["Dalashade_FirstPartyPerformanceTier", "Dalashade_NormalFieldStrength", "Dalashade_NormalDepthStrength", "Dalashade_NormalDetailStrength", "Dalashade_NormalMaterialInfluence", "Dalashade_NormalFieldDepthStrength", "Dalashade_NormalFieldDetailStrength", "Dalashade_NormalFieldMaterialInfluence"],
            DebugUniforms: ["Dalashade_AdaptiveGradeDebugMode"],
            Notes: ["Quality should preserve current behavior; lower tiers only reduce shared optional surface-helper influence."]),
        new(
            Family: "AtmosphereBloom",
            DisplayName: "Atmosphere Bloom",
            FileName: "Dalashade_AtmosphereBloom.fx",
            TechniqueName: "Dalashade_AtmosphereBloom",
            SectionName: "Dalashade_AtmosphereBloom",
            Role: "Restrained bloom and glow source shaping",
            ProductionShader: true,
            ManualDebugShader: false,
            TechniqueSyncEligible: true,
            IncludeFiles: ["Dalashade_FrameData.fxh", "Dalashade_MaterialMasks.fxh"],
            KnownGeneratedUniforms: SharedProductionUniforms.Concat(AtmosphereBloomUniforms).Concat(MaterialUniforms).Concat(DepthAssistUniforms).ToArray(),
            PerformanceTierUniforms: ["Dalashade_FirstPartyPerformanceTier", "Dalashade_BloomSampleQuality"],
            DebugUniforms: [],
            Notes: ["Balanced/Performance reduce optional bloom sample quality only."]),
        new(
            Family: "WeatherAtmosphere",
            DisplayName: "Weather Atmosphere",
            FileName: "Dalashade_WeatherAtmosphere.fx",
            TechniqueName: "Dalashade_WeatherAtmosphere",
            SectionName: "Dalashade_WeatherAtmosphere",
            Role: "Weather, air, fog, heat, cold, and scene-atmosphere response",
            ProductionShader: true,
            ManualDebugShader: false,
            TechniqueSyncEligible: true,
            IncludeFiles: ["Dalashade_FrameData.fxh", "Dalashade_MaterialMasks.fxh", "Dalashade_NormalField.fxh"],
            KnownGeneratedUniforms: SharedProductionUniforms.Concat(MaterialUniforms).Concat(DepthAssistUniforms).Concat(NormalFieldUniforms).ToArray(),
            PerformanceTierUniforms: ["Dalashade_FirstPartyPerformanceTier", "Dalashade_NormalFieldStrength", "Dalashade_NormalDepthStrength", "Dalashade_NormalDetailStrength", "Dalashade_NormalMaterialInfluence", "Dalashade_NormalFieldDepthStrength", "Dalashade_NormalFieldDetailStrength", "Dalashade_NormalFieldMaterialInfluence"],
            DebugUniforms: [],
            Notes: ["Lower tiers only reduce shared optional surface-helper influence."]),
        new(
            Family: "SmartSharpen",
            DisplayName: "Smart Sharpen",
            FileName: "Dalashade_SmartSharpen.fx",
            TechniqueName: "Dalashade_SmartSharpen",
            SectionName: "Dalashade_SmartSharpen",
            Role: "Material-aware sharpening and safety suppression",
            ProductionShader: true,
            ManualDebugShader: false,
            TechniqueSyncEligible: true,
            IncludeFiles: ["Dalashade_FrameData.fxh", "Dalashade_MaterialMasks.fxh", "Dalashade_NormalField.fxh"],
            KnownGeneratedUniforms: SharedProductionUniforms.Concat(MaterialUniforms).Concat(DepthAssistUniforms).Concat(NormalFieldUniforms).Concat(SmartSharpenUniforms).ToArray(),
            PerformanceTierUniforms: ["Dalashade_FirstPartyPerformanceTier", "Dalashade_NormalFieldStrength", "Dalashade_NormalDepthStrength", "Dalashade_NormalDetailStrength", "Dalashade_NormalMaterialInfluence", "Dalashade_NormalFieldDepthStrength", "Dalashade_NormalFieldDetailStrength", "Dalashade_NormalFieldMaterialInfluence"],
            DebugUniforms: [],
            Notes: ["SmartSharpen has separate authority handling for third-party sharpeners."]),
        new(
            Family: "SceneGI",
            DisplayName: "SceneGI",
            FileName: "Dalashade_SceneGI.fx",
            TechniqueName: "Dalashade_SceneGI",
            SectionName: "Dalashade_SceneGI",
            Role: "Screen-space AO, optional contact shadows, bounce, and light pooling",
            ProductionShader: true,
            ManualDebugShader: false,
            TechniqueSyncEligible: true,
            IncludeFiles: ["Dalashade_FrameData.fxh", "Dalashade_MaterialMasks.fxh", "Dalashade_NormalField.fxh", "Dalashade_Dalapad.fxh"],
            KnownGeneratedUniforms: SharedProductionUniforms.Concat(SceneGIUniforms).Concat(DalapadUniforms).Concat(MaterialUniforms).Concat(DepthAssistUniforms).Concat(NormalFieldUniforms).ToArray(),
            PerformanceTierUniforms: ["Dalashade_FirstPartyPerformanceTier", "Dalashade_GISampleCountScale", "Dalashade_GISampleDistanceScale", "Dalashade_GIRadius", "Dalashade_GIAORadius", "Dalashade_GIContactShadowRadius", "Dalashade_NormalFieldStrength", "Dalashade_NormalDepthStrength", "Dalashade_NormalDetailStrength", "Dalashade_NormalMaterialInfluence", "Dalashade_NormalFieldDepthStrength", "Dalashade_NormalFieldDetailStrength", "Dalashade_NormalFieldMaterialInfluence"],
            DebugUniforms: ["Dalashade_GIDebugMode", "Dalashade_GIDebugOutputMode", "Dalashade_GIDebugOpacity", "Dalashade_GIDebugBoost"],
            Notes: ["Dalapad production assist must flow through FrameData/Dalapad gates; debug modes are diagnostic output only."]),
        new(
            Family: "ScreenShadows",
            DisplayName: "Screen Shadows",
            FileName: "Dalashade_ScreenShadows.fx",
            TechniqueName: "Dalashade_ScreenShadows",
            SectionName: "Dalashade_ScreenShadows",
            Role: "Optional source-aware screen-space shadow impressions",
            ProductionShader: true,
            ManualDebugShader: false,
            TechniqueSyncEligible: true,
            IncludeFiles: ["Dalashade_FrameData.fxh", "Dalashade_MaterialMasks.fxh", "Dalashade_NormalField.fxh", "Dalashade_Dalapad.fxh"],
            KnownGeneratedUniforms: SharedProductionUniforms.Concat(ScreenShadowsUniforms).Concat(DalapadUniforms).Concat(MaterialUniforms).Concat(DepthAssistUniforms).Concat(NormalFieldUniforms).ToArray(),
            PerformanceTierUniforms: ["Dalashade_FirstPartyPerformanceTier", "Dalashade_ScreenShadowsReach", "Dalashade_NormalFieldStrength", "Dalashade_NormalDepthStrength", "Dalashade_NormalDetailStrength", "Dalashade_NormalMaterialInfluence", "Dalashade_NormalFieldDepthStrength", "Dalashade_NormalFieldDetailStrength", "Dalashade_NormalFieldMaterialInfluence"],
            DebugUniforms: ["Dalashade_ScreenShadowsDebugMode", "Dalashade_ScreenShadowsDebugOutputMode", "Dalashade_ScreenShadowsDebugOpacity", "Dalashade_ScreenShadowsDebugBoost"],
            Notes: ["ScreenShadows is optional and approximate; it should stay neutral when disabled or when Dalapad/depth data is unavailable."]),
        new(
            Family: "ContactTone",
            DisplayName: "Contact Tone",
            FileName: "Dalashade_ContactTone.fx",
            TechniqueName: "Dalashade_ContactTone",
            SectionName: "Dalashade_ContactTone",
            Role: "Local contact tone, grounding, and readability contrast",
            ProductionShader: true,
            ManualDebugShader: false,
            TechniqueSyncEligible: true,
            IncludeFiles: ["Dalashade_FrameData.fxh", "Dalashade_MaterialMasks.fxh", "Dalashade_NormalField.fxh"],
            KnownGeneratedUniforms: SharedProductionUniforms.Concat(ContactToneUniforms).Concat(MaterialUniforms).Concat(DepthAssistUniforms).Concat(NormalFieldUniforms).ToArray(),
            PerformanceTierUniforms: ["Dalashade_FirstPartyPerformanceTier", "Dalashade_ContactToneRadius", "Dalashade_NormalFieldStrength", "Dalashade_NormalDepthStrength", "Dalashade_NormalDetailStrength", "Dalashade_NormalMaterialInfluence", "Dalashade_NormalFieldDepthStrength", "Dalashade_NormalFieldDetailStrength", "Dalashade_NormalFieldMaterialInfluence"],
            DebugUniforms: ["Dalashade_ContactToneDebugMode", "Dalashade_ContactToneDebugOpacity"],
            Notes: ["ContactTone does not own GI bounce, reflections, weather air, or bloom."]),
        new(
            Family: "SurfaceReflection",
            DisplayName: "Surface Reflection",
            FileName: "Dalashade_SurfaceReflection.fx",
            TechniqueName: "Dalashade_SurfaceReflection",
            SectionName: "Dalashade_SurfaceReflection",
            Role: "Water resolver, reflection receivers, wetness, and glints",
            ProductionShader: true,
            ManualDebugShader: false,
            TechniqueSyncEligible: true,
            IncludeFiles: ["Dalashade_FrameData.fxh", "Dalashade_MaterialMasks.fxh", "Dalashade_NormalField.fxh"],
            KnownGeneratedUniforms: SharedProductionUniforms.Concat(SurfaceReflectionUniforms).Concat(MaterialUniforms).Concat(DepthAssistUniforms).Concat(NormalFieldUniforms).ToArray(),
            PerformanceTierUniforms: ["Dalashade_FirstPartyPerformanceTier", "Dalashade_ReflectionSampleQuality", "Dalashade_ReflectionSampleOffset", "Dalashade_ReflectionSoftness", "Dalashade_NormalFieldStrength", "Dalashade_NormalDepthStrength", "Dalashade_NormalDetailStrength", "Dalashade_NormalMaterialInfluence", "Dalashade_NormalFieldDepthStrength", "Dalashade_NormalFieldDetailStrength", "Dalashade_NormalFieldMaterialInfluence"],
            DebugUniforms: ["Dalashade_SurfaceReflectionDebugMode", "Dalashade_SurfaceReflectionDebugOutputMode", "Dalashade_SurfaceReflectionDebugOpacity", "Dalashade_SurfaceReflectionDebugBoost"],
            Notes: ["This is the highest-risk first-party visual shader and should remain metadata-only in this registry."]),
        new(
            Family: "MaterialDebug",
            DisplayName: "Material Debug",
            FileName: "Dalashade_MaterialDebug.fx",
            TechniqueName: "Dalashade_MaterialDebug",
            SectionName: "Dalashade_MaterialDebug",
            Role: "Manual truth viewer for shared material masks",
            ProductionShader: false,
            ManualDebugShader: true,
            TechniqueSyncEligible: false,
            IncludeFiles: ["Dalashade_MaterialMasks.fxh"],
            KnownGeneratedUniforms: MaterialUniforms.Concat(DepthAssistUniforms).ToArray(),
            PerformanceTierUniforms: [],
            DebugUniforms: ["Dalashade_MaterialDebugMode", "Dalashade_MaterialDebugOpacity", "Dalashade_MaterialDebugBoost"],
            Notes: ["Manual debug shader; should not be auto-enabled by technique sync."]),
        new(
            Family: "NormalDebug",
            DisplayName: "Normal Debug",
            FileName: "Dalashade_NormalDebug.fx",
            TechniqueName: "Dalashade_NormalDebug",
            SectionName: "Dalashade_NormalDebug",
            Role: "Manual truth viewer for NormalField diagnostics",
            ProductionShader: false,
            ManualDebugShader: true,
            TechniqueSyncEligible: false,
            IncludeFiles: ["Dalashade_NormalField.fxh", "Dalashade_MaterialMasks.fxh"],
            KnownGeneratedUniforms: MaterialUniforms.Concat(NormalFieldUniforms).Concat(DepthAssistUniforms).ToArray(),
            PerformanceTierUniforms: [],
            DebugUniforms: ["Dalashade_NormalDebugEnabled", "Dalashade_NormalDebugMode", "Dalashade_NormalDebugBoost", "Dalashade_NormalFieldDebugMode", "Dalashade_NormalFieldDebugBoost"],
            Notes: ["Manual debug shader; should not be auto-enabled by technique sync."]),
        new(
            Family: "FrameDataDebug",
            DisplayName: "FrameData Debug",
            FileName: "Dalashade_FrameDataDebug.fx",
            TechniqueName: "Dalashade_FrameDataDebug",
            SectionName: "Dalashade_FrameDataDebug",
            Role: "Manual truth viewer for FrameData normalized fields",
            ProductionShader: false,
            ManualDebugShader: true,
            TechniqueSyncEligible: false,
            IncludeFiles: ["Dalashade_FrameData.fxh"],
            KnownGeneratedUniforms: FrameDataDebugUniforms.Concat(DalapadUniforms).Concat(MaterialUniforms).Concat(NormalFieldUniforms).Concat(DepthAssistUniforms).ToArray(),
            PerformanceTierUniforms: [],
            DebugUniforms: FrameDataDebugUniforms,
            Notes: ["Manual debug shader; should not be auto-enabled by technique sync."]),
        new(
            Family: "DalapadDebug",
            DisplayName: "Dalapad Debug",
            FileName: "Dalapad_Debug.fx",
            TechniqueName: "Dalapad_Debug",
            SectionName: "Dalapad_Debug",
            Role: "Manual developer inspection of Dalapad semantic texture candidates",
            ProductionShader: false,
            ManualDebugShader: true,
            TechniqueSyncEligible: false,
            IncludeFiles: ["Dalashade_Dalapad.fxh"],
            KnownGeneratedUniforms: DalapadUniforms,
            PerformanceTierUniforms: [],
            DebugUniforms: ["Dalapad_DebugMode", "Dalapad_DebugSource", "Dalapad_DebugOpacity"],
            Notes: ["Manual debug shader; debug scan/copy paths can cost FPS and are separate from production assist."])
    ];

    public static IReadOnlyList<FirstPartyShaderMetadata> ProductionShaders { get; } =
        All.Where(shader => shader.ProductionShader).ToArray();

    public static IReadOnlyList<FirstPartyShaderMetadata> ManualDebugShaders { get; } =
        All.Where(shader => shader.ManualDebugShader).ToArray();

    public static IReadOnlyList<string> AllShaderFiles { get; } =
        All.Select(shader => shader.FileName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static IReadOnlyList<string> ProductionShaderFiles { get; } =
        ProductionShaders.Select(shader => shader.FileName).ToArray();

    public static IReadOnlyList<string> KnownPerformanceTierUniforms { get; } =
        All.SelectMany(shader => shader.PerformanceTierUniforms).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();

    public static bool IsManualDebugShaderFile(string fileName)
    {
        return ManualDebugShaders.Any(shader => string.Equals(shader.FileName, fileName, StringComparison.OrdinalIgnoreCase));
    }

    public static FirstPartyShaderMetadata? FindByTechnique(string techniqueName)
    {
        return All.FirstOrDefault(shader => string.Equals(shader.TechniqueName, techniqueName, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] SharedProductionUniforms =>
    [
        "Dalashade_StandaloneStrength",
        "Dalashade_FirstPartyMode",
        "Dalashade_FirstPartyPerformanceTier"
    ];

    private static string[] DepthAssistUniforms =>
    [
        "Dalashade_EnableDepthAssist",
        "Dalashade_DepthAssistStrength",
        "Dalashade_DepthAssistConfidenceFloor",
        "Dalashade_DepthConfidenceFloor"
    ];

    private static string[] FrameDataDebugUniforms =>
    [
        "Dalashade_FrameDataDebugMode",
        "Dalashade_FrameDataDebugBoost",
        "Dalashade_FrameDataDebugOpacity"
    ];

    private static string[] SceneGIUniforms =>
    [
        "Dalashade_GIEnabled",
        "Dalashade_GIStrength",
        "Dalashade_GIRadius",
        "Dalashade_GIBounceStrength",
        "Dalashade_GIAOIntensity",
        "Dalashade_GIAORadius",
        "Dalashade_GINightLightStrength",
        "Dalashade_GIMaterialInfluence",
        "Dalashade_GIContactShadowsEnabled",
        "Dalashade_GIContactShadowStrength",
        "Dalashade_GIContactShadowRadius",
        "Dalashade_GIContactShadowSoftness",
        "Dalashade_GISkyReject",
        "Dalashade_GISkinProtect",
        "Dalashade_GISampleCountScale",
        "Dalashade_GISampleDistanceScale",
        "Dalashade_GIDebugMode",
        "Dalashade_GIDebugOutputMode",
        "Dalashade_GIDebugOpacity",
        "Dalashade_GIDebugBoost"
    ];

    private static string[] ScreenShadowsUniforms =>
    [
        "Dalashade_ScreenShadowsEnabled",
        "Dalashade_ScreenShadowsStrength",
        "Dalashade_ScreenShadowsReach",
        "Dalashade_ScreenShadowsSoftness",
        "Dalashade_ScreenShadowsSourceSensitivity",
        "Dalashade_ScreenShadowsDalapadInfluence",
        "Dalashade_ScreenShadowsDebugMode",
        "Dalashade_ScreenShadowsDebugOutputMode",
        "Dalashade_ScreenShadowsDebugOpacity",
        "Dalashade_ScreenShadowsDebugBoost"
    ];

    private static string[] ContactToneUniforms =>
    [
        "Dalashade_ContactToneEnabled",
        "Dalashade_ContactToneStrength",
        "Dalashade_ContactToneRadius",
        "Dalashade_ContactToneEdgeStrength",
        "Dalashade_ContactToneStructureStrength",
        "Dalashade_ContactToneContrastStrength",
        "Dalashade_ContactToneDebugMode",
        "Dalashade_ContactToneDebugOpacity"
    ];

    private static string[] SurfaceReflectionUniforms =>
    [
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
        "Dalashade_ReflectionSampleQuality",
        "Dalashade_ReflectionDepthReject",
        "Dalashade_SurfaceReflectionDebugMode",
        "Dalashade_SurfaceReflectionDebugOutputMode",
        "Dalashade_SurfaceReflectionDebugOpacity",
        "Dalashade_SurfaceReflectionDebugBoost"
    ];

    private static string[] AtmosphereBloomUniforms =>
    [
        "Dalashade_BloomSampleQuality"
    ];

    private static string[] NormalFieldUniforms =>
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
        "Dalashade_NormalSkySuppression",
        "Dalashade_NormalFieldDepthStrength",
        "Dalashade_NormalFieldDetailStrength",
        "Dalashade_NormalFieldMaterialInfluence",
        "Dalashade_NormalFieldWaterSuppression",
        "Dalashade_NormalFieldSkinSuppression",
        "Dalashade_NormalFieldSkySuppression",
        "Dalashade_NormalFieldDebugMode",
        "Dalashade_NormalFieldDebugBoost"
    ];

    private static string[] DalapadUniforms =>
    [
        "Dalashade_DalapadEnabled",
        "Dalashade_DalapadSurfaceDataEnabled",
        "Dalashade_DalapadSurfaceDataStrength",
        "Dalashade_DalapadSceneGINormalAssist",
        "Dalashade_DalapadSceneGINormalStrength"
    ];

    private static string[] SmartSharpenUniforms =>
    [
        "SharpenMicro",
        "SharpenTexture",
        "SharpenEdges",
        "SharpenThreshold",
        "SharpenHighlightProtection",
        "SharpenFineDetail",
        "Dalashade_SmartSharpenAuthority"
    ];

    private static string[] MaterialUniforms =>
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
}
