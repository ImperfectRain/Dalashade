using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dalashade.SceneAuthoring;

namespace Dalashade;

public sealed record CompatibilityReportExportResult(bool Success, string Message, string Path)
{
    public static CompatibilityReportExportResult Skipped(string message) => new(false, message, string.Empty);
}

public sealed class CompatibilityReportExporter
{
    public const string MaterialIntentDiagnosticsTableHeader = "| Channel | Profile prior | Tag/other evidence | Screenshot material evidence | Final value | Top suppressions/caps |";
    private sealed record GeneratedPresetSectionValues(bool SectionPresent, IReadOnlyDictionary<string, string> Values, string Warning);
    private sealed record GeneratedPresetVariableValue(string Section, string Key, string Value);
    private sealed record MaterialParityChannel(string Uniform, string Label);
    private sealed record MaterialParityShader(string FileName, string Technique, string Role, IReadOnlySet<string> WrittenUniforms, IReadOnlySet<string> ExpectedUniforms, IReadOnlySet<string> DebugUniforms);

    private static readonly string[] ProductionShaderFiles =
    [
        "Dalashade_AdaptiveGrade.fx",
        "Dalashade_AtmosphereBloom.fx",
        "Dalashade_WeatherAtmosphere.fx",
        "Dalashade_SmartSharpen.fx",
        "Dalashade_SceneGI.fx",
        "Dalashade_ContactTone.fx",
        "Dalashade_SurfaceReflection.fx"
    ];

    private static readonly string[] FrameDataDebugVariables =
    [
        "Dalashade_FrameDataDebugMode",
        "Dalashade_FrameDataDebugBoost",
        "Dalashade_FrameDataDebugOpacity"
    ];

    private static readonly IReadOnlyList<MaterialParityChannel> SharedMaterialParityChannels =
    [
        new("Dalashade_MaterialFoliage", "Foliage"),
        new("Dalashade_MaterialWaterPlane", "WaterPlane"),
        new("Dalashade_MaterialSpecularGlint", "SpecularGlint"),
        new("Dalashade_MaterialWaterSpecular", "WaterSpecular legacy"),
        new("Dalashade_MaterialSandDust", "SandDust"),
        new("Dalashade_MaterialSnowIce", "SnowIce"),
        new("Dalashade_MaterialStoneRuins", "StoneRuins"),
        new("Dalashade_MaterialMetalIndustrial", "MetalIndustrial"),
        new("Dalashade_MaterialCrystalAether", "CrystalAether"),
        new("Dalashade_MaterialNeonGlass", "NeonGlass"),
        new("Dalashade_MaterialFireLavaHeat", "FireLavaHeat"),
        new("Dalashade_MaterialSkyCloudFog", "SkyCloudFog"),
        new("Dalashade_MaterialSkinProtection", "SkinProtection"),
        new("Dalashade_MaterialVoidDarkness", "VoidDarkness"),
        new("Dalashade_WaterContext", "WaterContext"),
        new("Dalashade_CoastalContext", "CoastalContext"),
        new("Dalashade_OpenOceanContext", "OpenOceanContext"),
        new("Dalashade_ShallowWaterContext", "ShallowWaterContext"),
        new("Dalashade_WetSurfaceContext", "WetSurfaceContext"),
        new("Dalashade_RainWetContext", "RainWetContext"),
        new("Dalashade_EnableDepthAssist", "Depth assist enable"),
        new("Dalashade_DepthAssistStrength", "Depth assist strength"),
        new("Dalashade_DepthAssistConfidenceFloor", "Depth assist confidence floor"),
        new("Dalashade_DepthConfidenceFloor", "Depth confidence alias")
    ];

    private static readonly IReadOnlyList<MaterialParityShader> MaterialParityShaders =
    [
        new(
            "Dalashade_AdaptiveGrade.fx",
            "Dalashade_AdaptiveGrade",
            "Scene-aware color/tone protection",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set()),
        new(
            "Dalashade_AtmosphereBloom.fx",
            "Dalashade_AtmosphereBloom",
            "Selective material glow/bloom eligibility",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection")),
        new(
            "Dalashade_WeatherAtmosphere.fx",
            "Dalashade_WeatherAtmosphere",
            "Weather/air/haze material shaping",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection")),
        new(
            "Dalashade_SmartSharpen.fx",
            "Dalashade_SmartSharpen",
            "Material-aware sharpening suppression",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection")),
        new(
            "Dalashade_MaterialDebug.fx",
            "Dalashade_MaterialDebug",
            "Truth viewer for shared material masks",
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray()),
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray()),
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray())),
        new(
            "Dalashade_NormalDebug.fx",
            "Dalashade_NormalDebug",
            "Truth viewer for shared NormalField diagnostics",
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray()),
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray()),
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray())),
        new(
            "Dalashade_SceneGI.fx",
            "Dalashade_SceneGI",
            "Screen-space AO, bounce, and light pooling",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness")),
        new(
            "Dalashade_ContactTone.fx",
            "Dalashade_ContactTone",
            "Local contact tone, grounding, and readability contrast",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection")),
        new(
            "Dalashade_SurfaceReflection.fx",
            "Dalashade_SurfaceReflection",
            "Water resolver, reflection receivers, and glints",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"))
    ];

    private static readonly IReadOnlySet<string> SceneGIIntentNames = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(SceneIntent.Atmosphere),
        nameof(SceneIntent.ShadowProtection),
        nameof(SceneIntent.Wetness),
        nameof(SceneIntent.MagicGlow),
        nameof(SceneIntent.NeonGlow),
        nameof(SceneIntent.FoliageDensity),
        nameof(SceneIntent.IndustrialHardness),
        nameof(SceneIntent.CosmicMood),
        nameof(SceneIntent.Night),
        nameof(SceneIntent.Moonlight),
        nameof(SceneIntent.ArtificialLight),
        nameof(SceneIntent.AmbientDarkness),
        nameof(SceneIntent.NightAtmosphere),
        nameof(SceneIntent.Daylight),
        nameof(SceneIntent.Sunlight),
        nameof(SceneIntent.OpenSkyLight),
        nameof(SceneIntent.SurfaceHeat),
        nameof(SceneIntent.DayAtmosphere),
        nameof(SceneIntent.DayReflection),
        nameof(SceneIntent.DayHighlightPressure),
        nameof(SceneIntent.CombatPressure),
        nameof(SceneIntent.CinematicPermission)
    };

    private static readonly IReadOnlySet<string> SceneGIMaterialNames = new HashSet<string>(StringComparer.Ordinal)
    {
        MaterialIntent.FoliageChannel,
        MaterialIntent.WaterSpecularChannel,
        MaterialIntent.SandDustChannel,
        MaterialIntent.SnowIceChannel,
        MaterialIntent.StoneRuinsChannel,
        MaterialIntent.MetalIndustrialChannel,
        MaterialIntent.CrystalAetherChannel,
        MaterialIntent.NeonGlassChannel,
        MaterialIntent.FireLavaHeatChannel,
        MaterialIntent.SkyCloudFogChannel,
        MaterialIntent.SkinProtectionChannel,
        MaterialIntent.VoidDarknessChannel
    };

    private static readonly IReadOnlySet<string> ContactToneMaterialNames = new HashSet<string>(StringComparer.Ordinal)
    {
        MaterialIntent.FoliageChannel,
        MaterialIntent.WaterSpecularChannel,
        MaterialIntent.SandDustChannel,
        MaterialIntent.SnowIceChannel,
        MaterialIntent.StoneRuinsChannel,
        MaterialIntent.MetalIndustrialChannel,
        MaterialIntent.SkyCloudFogChannel,
        MaterialIntent.SkinProtectionChannel,
        MaterialIntent.VoidDarknessChannel
    };

    private static readonly IReadOnlySet<string> SurfaceReflectionMaterialNames = new HashSet<string>(StringComparer.Ordinal)
    {
        MaterialIntent.WaterSpecularChannel,
        MaterialIntent.SandDustChannel,
        MaterialIntent.SnowIceChannel,
        MaterialIntent.MetalIndustrialChannel,
        MaterialIntent.CrystalAetherChannel,
        MaterialIntent.NeonGlassChannel,
        MaterialIntent.FireLavaHeatChannel,
        MaterialIntent.SkyCloudFogChannel,
        MaterialIntent.SkinProtectionChannel
    };

    public CompatibilityReportExportResult Export(
        Configuration configuration,
        PresetAnalysisResult analysis,
        ShaderSupportScan shaderSupport,
        VisualProfile profile,
        MasterStyleDiagnostics masterDiagnostics,
        TagStackDiagnostics tagStackDiagnostics,
        SceneAuthoringState sceneAuthoringState,
        ImageAnalysisResult currentImage,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence,
        DalapadDiagnostics dalapadDiagnostics,
        IReadOnlyList<SceneTagPreset>? activeTagRegistry,
        ImageAnalysisResult masterStyle,
        PresetWriteResult writeResult,
        string effectiveBasePresetPath,
        string outputDirectory)
    {
        var stage = "starting compatibility report export";
        try
        {
            if (!analysis.Success)
            {
                return CompatibilityReportExportResult.Skipped("Preset analysis has not succeeded yet.");
            }

            stage = "resolving report output path";
            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var path = ResolveReportExportPath(outputDirectory, GetDefaultPluginConfigDirectory(), $"Dalashade_CompatibilityReport_{timestamp}.md");
            var reportDirectory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(reportDirectory))
            {
                reportDirectory = ResolveSafeDirectory(null, GetDefaultPluginConfigDirectory(), "Reports");
                path = Path.Combine(reportDirectory, Path.GetFileName(path));
            }

            stage = "creating report output directory";
            Directory.CreateDirectory(reportDirectory);

            stage = "building report content";
            var reportContent = BuildReport(configuration, analysis, shaderSupport, profile, masterDiagnostics, tagStackDiagnostics, sceneAuthoringState, currentImage, screenshotMaterialEvidence, dalapadDiagnostics, activeTagRegistry, masterStyle, writeResult, effectiveBasePresetPath);
            stage = "writing report file";
            File.WriteAllText(path, reportContent, Encoding.UTF8);
            if (!File.Exists(path))
            {
                return CompatibilityReportExportResult.Skipped($"Compatibility report export failed: file was not created at {path}");
            }

            return new CompatibilityReportExportResult(true, $"Compatibility report exported: {path}", path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CompatibilityReportExportResult.Skipped($"Compatibility report export failed while {stage}: {ex.Message}");
        }
    }

    private static string BuildReport(
        Configuration configuration,
        PresetAnalysisResult analysis,
        ShaderSupportScan shaderSupport,
        VisualProfile profile,
        MasterStyleDiagnostics masterDiagnostics,
        TagStackDiagnostics tagStackDiagnostics,
        SceneAuthoringState sceneAuthoringState,
        ImageAnalysisResult currentImage,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence,
        DalapadDiagnostics dalapadDiagnostics,
        IReadOnlyList<SceneTagPreset>? activeTagRegistry,
        ImageAnalysisResult masterStyle,
        PresetWriteResult writeResult,
        string effectiveBasePresetPath)
    {
        var report = analysis.Report;
        var builder = new StringBuilder();

        builder.AppendLine("# Dalashade Compatibility Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"Base preset: `{effectiveBasePresetPath}`");
        builder.AppendLine($"Generated preset: `{configuration.GeneratedPresetPath}`");
        builder.AppendLine($"Selected compatibility mode: {PresetAnalyzer.FormatCompatibilityMode(configuration.CompatibilityMode)}");
        builder.AppendLine($"Recommended compatibility mode: {PresetAnalyzer.FormatCompatibilityMode(report.RecommendedCompatibilityMode)}");
        builder.AppendLine($"Preset risk: {report.Level}");
        builder.AppendLine();

        AppendTechniqueList(builder, "Active Controlled Effects", report.ActiveSupportedEffects);
        AppendTechniqueList(builder, "Active Partially Controlled Effects", report.ActivePartiallySupportedEffects);
        AppendTechniqueList(builder, "Active Detected-Only Effects", report.ActiveDetectedOnlyEffects);
        AppendTechniqueList(builder, "Active Unknown Effects", report.ActiveUnsupportedEffects);
        AppendTechniqueList(builder, "High-Risk Active Effects", report.HighRiskActiveEffects);
        AppendTechniqueList(builder, "Inactive Supported Effects", report.InactiveSupportedEffects);
        var authorityPolicy = GenerationAuthorityPolicy.From(analysis, configuration.CompatibilityMode);
        AppendRolePolicies(builder, configuration.CompatibilityMode, authorityPolicy);
        AppendAuthorities(builder, report.Authorities, authorityPolicy);
        AppendLines(builder, "Warnings", report.Warnings);
        AppendLines(builder, "Multiple Authority Warnings", report.MultipleAuthorityWarnings);
        var reportMaterialProfile = MaterialProfileBuilder.Build(tagStackDiagnostics, currentImage, configuration.ScreenshotAnalysisStrength);
        var reportScreenshotEvidenceContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(configuration, tagStackDiagnostics, screenshotMaterialEvidence.Evidence);
        var reportTagRegistry = configuration.EnableSceneAuthoringOverrides ? activeTagRegistry : null;
        var reportMaterialIntent = configuration.EnableMaterialIntent
            ? MaterialIntentBuilder.Build(tagStackDiagnostics, currentImage, reportMaterialProfile, reportTagRegistry, screenshotStrength: configuration.ScreenshotAnalysisStrength, screenshotMaterialEvidenceContributions: reportScreenshotEvidenceContributions).WithStrength(configuration.MaterialIntentStrength)
            : MaterialIntent.Neutral;
        AppendTagStackDiagnostics(builder, tagStackDiagnostics);
        AppendSceneAuthoringDiagnostics(builder, configuration, sceneAuthoringState);
        AppendScreenshotAnalysisDiagnostics(builder, configuration, currentImage);
        AppendScreenshotMaterialEvidenceDiagnostics(builder, configuration, screenshotMaterialEvidence, reportMaterialIntent);
        AppendMaterialIntentDiagnostics(builder, configuration, tagStackDiagnostics, currentImage, screenshotMaterialEvidence, reportTagRegistry, writeResult);
        AppendMaterialCalibrationDiagnostics(builder, configuration, tagStackDiagnostics, reportMaterialProfile, screenshotMaterialEvidence, reportMaterialIntent, reportTagRegistry, shaderSupport, writeResult);
        AppendNormalFieldDiagnostics(builder, configuration, analysis, writeResult);
        AppendFrameDataDiagnostics(builder, configuration, analysis, writeResult);
        AppendDalapadDiagnostics(builder, dalapadDiagnostics);
        AppendFirstPartyDepthAssistDiagnostics(builder, configuration, writeResult);
        AppendMasterStyleDiagnostics(builder, configuration, masterDiagnostics);
        AppendColorFamilyAdjustments(builder, profile);
        AppendColorFamilyComparison(builder, currentImage, masterStyle, profile);
        AppendMappingValidation(builder, configuration, analysis, shaderSupport, effectiveBasePresetPath);
        AppendCustomShaderDiagnostics(builder, configuration, analysis, shaderSupport, writeResult, tagStackDiagnostics, currentImage, screenshotMaterialEvidence);
        AppendMaterialParityAudit(builder, configuration);
        AppendShaderSupport(builder, shaderSupport);
        AppendChangedVariables(builder, writeResult);
        AppendSanitizeActions(builder, writeResult);

        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine("- This report is diagnostic. Exporting it does not change generated preset behavior.");
        builder.AppendLine("- Detected-only effects are recognized by role/risk but are not directly controlled yet.");
        builder.AppendLine("- Unknown effects are active shaders Dalashade does not understand well enough yet.");

        return builder.ToString();
    }

    private static void AppendScreenshotAnalysisDiagnostics(StringBuilder builder, Configuration configuration, ImageAnalysisResult currentImage)
    {
        builder.AppendLine("## Screenshot Analysis");
        builder.AppendLine();
        builder.AppendLine($"- Enabled: {(configuration.AutoAdjustFromScreenshots ? "yes" : "no")}");
        builder.AppendLine($"- Strength: {configuration.ScreenshotAnalysisStrength:0.##}");
        builder.AppendLine($"- Sampling mode: {configuration.ImageSamplingMode}");
        if (!currentImage.Available)
        {
            builder.AppendLine("- Current image: unavailable");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"- Source: `{currentImage.SourcePath}`");
        builder.AppendLine($"- Source timestamp: {currentImage.SourceTimestamp:O}");
        builder.AppendLine($"- Bucket: {currentImage.ProfileBucket}");
        builder.AppendLine($"- Summary: {currentImage.OpinionSummary}");
        builder.AppendLine($"- Luminance / contrast / saturation: {currentImage.AverageLuminance:0.###} / {currentImage.Contrast:0.###} / {currentImage.AverageSaturation:0.###}");
        builder.AppendLine($"- Shadow clip / highlight clip: {currentImage.ShadowClipping:P1} / {currentImage.HighlightClipping:P1}");
        builder.AppendLine($"- Tonal P05/P50/P95: {currentImage.LuminanceP05:0.###} / {currentImage.LuminanceP50:0.###} / {currentImage.LuminanceP95:0.###}");
        builder.AppendLine();

        builder.AppendLine("### Screenshot Opinions");
        builder.AppendLine();
        var opinions = currentImage.Opinions.OrderByDescending(opinion => opinion.Confidence).ToArray();
        if (opinions.Length == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var opinion in opinions)
            {
                builder.AppendLine($"- {opinion.Label}: confidence {opinion.Confidence:0.##}; target {opinion.Target}; {opinion.Reason}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("### Screenshot Regions");
        builder.AppendLine();
        builder.AppendLine("| Region | Luma | Contrast | Saturation | Bright | Dark | Smooth | Top colors |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |");
        foreach (var region in Enum.GetValues<ImageAnalysisRegion>())
        {
            var stats = currentImage.Regions.TryGetValue(region, out var value) ? value : ImageRegionStats.Empty(region);
            builder.AppendLine($"| {region} | {stats.AverageLuminance:0.###} | {stats.Contrast:0.###} | {stats.AverageSaturation:0.###} | {stats.BrightTendency:0.###} | {stats.DarkTendency:0.###} | {stats.SmoothTendency:0.###} | {FormatImageColorFamilies(stats.ColorFamilies)} |");
        }

        builder.AppendLine();
    }

    private static string FormatImageColorFamilies(IReadOnlyDictionary<ColorFamily, ColorFamilyStats> families)
    {
        var visible = families.Values
            .Where(family => family.Confidence > 0.05f)
            .OrderByDescending(family => family.Confidence)
            .Take(4)
            .Select(family => $"{family.Family} {family.Confidence:0.##}")
            .ToArray();
        return visible.Length == 0 ? "none" : string.Join(", ", visible);
    }

    private static void AppendScreenshotMaterialEvidenceDiagnostics(StringBuilder builder, Configuration configuration, ScreenshotMaterialEvidenceDiagnostics diagnostics, MaterialIntent currentMaterialIntent)
    {
        builder.AppendLine("## Screenshot Material Evidence");
        builder.AppendLine();
        builder.AppendLine(configuration.EnableScreenshotMaterialEvidenceInfluence
            ? $"- Influence enabled: this layer can contribute capped scene-level MaterialIntent priors at strength {configuration.ScreenshotMaterialEvidenceStrength:0.##}; shader-side masks still decide per-pixel material behavior."
            : "- Influence disabled: this layer is diagnostic-only and does not change MaterialIntent, generated preset values, shader variables, shader code, or technique/load-order behavior.");
        builder.AppendLine("- Evidence is broad scene-level screenshot evidence, not true engine material ID detection and not per-pixel classification.");
        builder.AppendLine($"- Confidence: {diagnostics.Evidence.Confidence:0.###}");
        builder.AppendLine();

        builder.AppendLine("| Evidence channel | Visible evidence | Current MaterialIntent comparison |");
        builder.AppendLine("| --- | ---: | --- |");
        foreach (var row in ScreenshotMaterialEvidenceRows(diagnostics.Evidence, currentMaterialIntent))
        {
            builder.AppendLine($"| {row.Label} | {row.Visible:0.###} | {row.IntentLabel} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Evidence Notes");
        builder.AppendLine();
        foreach (var item in diagnostics.Evidence.Evidence)
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
        builder.AppendLine("### Material Evidence Mismatch Warnings");
        builder.AppendLine();
        if (diagnostics.Mismatches.Count == 0)
        {
            builder.AppendLine("- None.");
        }
        else
        {
            foreach (var mismatch in diagnostics.Mismatches)
            {
                builder.AppendLine($"- {mismatch.Channel}: severity {mismatch.Severity:0.###}; visible {mismatch.VisibleEvidence:0.###}; current intent {mismatch.CurrentIntent:0.###}. {mismatch.Message}");
            }
        }

        builder.AppendLine();
    }

    private static IReadOnlyList<(string Label, float Visible, string IntentLabel)> ScreenshotMaterialEvidenceRows(ScreenshotMaterialEvidence evidence, MaterialIntent intent)
    {
        return
        [
            ("FoliageVisible", evidence.FoliageVisible, $"{MaterialIntent.FoliageChannel} {intent.Foliage:0.###}"),
            ("GrassTerrainVisible", evidence.GrassTerrainVisible, $"{MaterialIntent.FoliageChannel} {intent.Foliage:0.###}"),
            ("WaterVisible", evidence.WaterVisible, $"{MaterialIntent.WaterSpecularChannel} {intent.WaterSpecular:0.###}"),
            ("SandVisible", evidence.SandVisible, $"{MaterialIntent.SandDustChannel} {intent.SandDust:0.###}"),
            ("SnowVisible", evidence.SnowVisible, $"{MaterialIntent.SnowIceChannel} {intent.SnowIce:0.###}"),
            ("StoneVisible", evidence.StoneVisible, $"{MaterialIntent.StoneRuinsChannel} {intent.StoneRuins:0.###}"),
            ("MetalVisible", evidence.MetalVisible, $"{MaterialIntent.MetalIndustrialChannel} {intent.MetalIndustrial:0.###}"),
            ("SkyVisible", evidence.SkyVisible, $"{MaterialIntent.SkyCloudFogChannel} {intent.SkyCloudFog:0.###}"),
            ("AetherOrNeonVisible", evidence.AetherOrNeonVisible, $"{MaterialIntent.CrystalAetherChannel} {intent.CrystalAether:0.###}; {MaterialIntent.NeonGlassChannel} {intent.NeonGlass:0.###}"),
            ("SkinOrCharacterVisible", evidence.SkinOrCharacterVisible, $"{MaterialIntent.SkinProtectionChannel} {intent.SkinProtection:0.###}")
        ];
    }

    private static void AppendMaterialCalibrationDiagnostics(
        StringBuilder builder,
        Configuration configuration,
        TagStackDiagnostics tagStackDiagnostics,
        MaterialProfile materialProfile,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence,
        MaterialIntent materialIntent,
        IReadOnlyList<SceneTagPreset>? activeTagRegistry,
        ShaderSupportScan shaderSupport,
        PresetWriteResult writeResult)
    {
        var calibration = MaterialCalibrationDiagnosticsBuilder.Build(
            configuration,
            tagStackDiagnostics,
            materialProfile,
            screenshotMaterialEvidence,
            materialIntent,
            activeTagRegistry,
            shaderSupport,
            writeResult);

        builder.AppendLine("## Material Calibration");
        builder.AppendLine();
        builder.AppendLine("- Purpose: one-place comparison of SceneTags/MaterialProfile, tag registry tuning, screenshot material evidence, current MaterialIntent, shader mapping availability, and mismatch warnings.");
        builder.AppendLine("- Scope: diagnostics only. This section does not change generated preset values, shader code, FrameData, MaterialMasks, NormalField, or technique activation behavior.");
        builder.AppendLine();
        builder.AppendLine("| Channel | Scene/profile prior | Tag registry | Screenshot evidence | Current MaterialIntent | Shader mapping | Shader keys/sections | Warnings |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- | --- | --- |");
        foreach (var channel in calibration.Channels)
        {
            var mapping = channel.ShaderMappingEnabled
                ? channel.ShaderMappingAvailable ? "enabled; key found" : "enabled; key missing"
                : channel.ShaderMappingAvailable ? "disabled; key found" : "disabled; key missing";
            builder.AppendLine($"| {channel.Channel} | {channel.ProfilePrior:0.###} | {channel.TagRegistryContribution:+0.###;-0.###;0} | {channel.ScreenshotEvidence:0.###} | {channel.MaterialIntent:0.###} | {mapping} | {FormatCalibrationShaderTargets(channel)} | {FormatCalibrationWarnings(channel.Warnings)} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Scene Matrix Checklist");
        builder.AppendLine();
        foreach (var row in calibration.SceneMatrix)
        {
            builder.AppendLine($"- {row}");
        }

        builder.AppendLine();
    }

    private static void AppendCustomShaderDiagnostics(
        StringBuilder builder,
        Configuration configuration,
        PresetAnalysisResult analysis,
        ShaderSupportScan shaderSupport,
        PresetWriteResult writeResult,
        TagStackDiagnostics tagStackDiagnostics,
        ImageAnalysisResult currentImage,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence)
    {
        builder.AppendLine("## Dalashade Custom Shader Diagnostics");
        builder.AppendLine();
        var diagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(configuration, shaderSupport, writeResult, analysis);
        builder.AppendLine($"- Custom shader support: {(diagnostics.SupportEnabled ? "enabled" : "disabled")}");
        builder.AppendLine($"- Auto-inject known sections into generated preset: {(diagnostics.AutoInjectionEnabled ? "enabled" : "disabled")}");
        builder.AppendLine($"- Generated preset only injection: {(diagnostics.GeneratedPresetOnlyInjection ? "yes" : "no")}");
        builder.AppendLine($"- Generated-preset-only sections injected: {(diagnostics.SectionInjected ? "yes" : "no")}");
        builder.AppendLine($"- Generated-preset-only variables injected: {(diagnostics.VariablesInjected ? "yes" : "no")}");
        builder.AppendLine(configuration.SyncDalashadeTechniqueActivation
            ? "- Technique activation sync: enabled for Dalashade production techniques in the generated preset only."
            : "- Technique activation: manual; generated-preset injection does not append custom shaders to `Techniques=` unless activation sync is enabled.");
        builder.AppendLine($"- Generated-preset injected sections: {FormatInlineList(writeResult.CustomShaderInjection.Sections)}");
        builder.AppendLine($"- Generated-preset injected variables: {FormatInlineList(writeResult.CustomShaderInjection.Variables)}");
        builder.AppendLine($"- Generated-preset injected techniques: {FormatInlineList(writeResult.CustomShaderInjection.Techniques)}");
        builder.AppendLine($"- Base preset custom shader section present: {(diagnostics.SectionFound ? "yes" : "no")}");
        builder.AppendLine($"- Base preset known custom variables found: {(diagnostics.KnownVariablesFound ? "yes" : "no")}");
        builder.AppendLine($"- Known variable classes detected: {FormatCustomShaderVariableClasses(diagnostics.KnownVariables.Select(variable => variable.Key))}");
        builder.AppendLine($"- SceneIntent values written into generated preset: {(diagnostics.ValuesWritten ? "yes" : "no")}");
        builder.AppendLine($"- Variables detected but unchanged: {(diagnostics.VariablesDetectedButUnchanged ? "yes" : "no")}");
        builder.AppendLine($"- SmartSharpen authority: {diagnostics.SmartSharpenAuthority.Level.ToString().ToLowerInvariant()} ({diagnostics.SmartSharpenAuthority.ShaderValue:0})");
        builder.AppendLine($"- Other active sharpeners: {FormatInlineList(diagnostics.SmartSharpenAuthority.OtherActiveSharpeners)}");
        builder.AppendLine($"- SmartSharpen authority reason: {diagnostics.SmartSharpenAuthority.Reason}");
        var materialDebugTechnique = FindMaterialDebugTechnique(analysis);
        builder.AppendLine($"- Material debug shader listed: {(materialDebugTechnique is null ? "no" : "yes")}");
        builder.AppendLine($"- Material debug technique activation: {(materialDebugTechnique is null ? "absent" : PresetAnalyzer.FormatActivationState(materialDebugTechnique.ActivationState))}");
        builder.AppendLine("- Split water/specular debug support: available when `Dalashade_MaterialDebug.fx` and `Dalashade_MaterialMasks.fxh` are installed. `WaterPlane` and `SpecularGlint` are shader-side heuristic masks derived from the existing WaterSpecular scene likelihood.");
        AppendSceneGIDiagnostics(builder, configuration, analysis, writeResult, tagStackDiagnostics, currentImage, screenshotMaterialEvidence);
        AppendContactToneDiagnostics(builder, configuration, analysis, writeResult, tagStackDiagnostics, currentImage, screenshotMaterialEvidence);
        AppendSurfaceReflectionDiagnostics(builder, configuration, analysis, writeResult, tagStackDiagnostics, currentImage, screenshotMaterialEvidence);
        builder.AppendLine("- Material debug controls: shader-owned in ReShade UI; Dalashade does not write debug mode, overlay mode, opacity, or strength.");
        builder.AppendLine($"- First-party custom shader status: {FormatFirstPartyCustomShaderStatus(analysis)}");
        builder.AppendLine("- Variable ownership: SceneIntent variables are Dalashade-controlled, MaterialIntent channel uniforms are Dalashade-controlled only when material shader mapping is enabled, NormalField uniforms are Dalashade-controlled only when NormalField shader mapping is enabled, SceneGI, ContactTone, and SurfaceReflection debug controls can be written by their separate generated-variable toggles, and other shader-owned controls are recognized/injected but not actively written by Dalashade.");
        builder.AppendLine("- Manual shader install/activation: Dalashade does not copy `.fx` files into ReShade or enable techniques. Install needed Dalashade `.fx` files in a ReShade shader search folder separately, then enable wanted custom shader techniques in ReShade.");
        builder.AppendLine("- Variable writes require matching Dalashade custom shader section/key lines in generated preset content. Those lines can come from the base preset or from generated-preset-only injection.");
        builder.AppendLine("- Static bridge status:");
        foreach (var message in diagnostics.StatusMessages)
        {
            builder.AppendLine($"  - {message}");
        }

        builder.AppendLine("- Base preset custom shader sections found:");
        if (diagnostics.Sections.Count == 0)
        {
            builder.AppendLine("  - None");
        }
        else
        {
            foreach (var section in diagnostics.Sections)
            {
                builder.AppendLine($"  - `{section.Section}` | technique listed={(section.TechniqueAppearsInTechniques ? "yes" : "no")} | activation={PresetAnalyzer.FormatActivationState(section.ActivationState)}");
            }
        }

        builder.AppendLine("- Base preset custom shader variables detected:");
        if (diagnostics.KnownVariables.Count == 0)
        {
            builder.AppendLine("  - None");
        }
        else
        {
            foreach (var item in diagnostics.KnownVariables)
            {
                builder.AppendLine($"  - `{item.Section}` / `{item.Key}` | activation={PresetAnalyzer.FormatActivationState(item.ActivationState)} | controllable={(item.Controllable ? "yes" : "no")} | written={(item.Written ? "yes" : "no")}");
            }
        }

        builder.AppendLine("- Generated preset custom shader variables written:");
        if (diagnostics.WrittenVariables.Count == 0)
        {
            builder.AppendLine("  - None");
        }
        else
        {
            foreach (var change in diagnostics.WrittenVariables)
            {
                builder.AppendLine($"  - `{change.Section}` / `{change.Key}`: {change.OldValue} -> {change.NewValue}");
            }
        }

        builder.AppendLine();
    }

    private static void AppendMaterialParityAudit(StringBuilder builder, Configuration configuration)
    {
        var sourceByShader = MaterialParityShaders.ToDictionary(
            shader => shader.FileName,
            shader => ReadShaderSource(configuration, shader.FileName),
            StringComparer.OrdinalIgnoreCase);

        builder.AppendLine("## Material Parity Audit");
        builder.AppendLine();
        builder.AppendLine("- This diagnostics-only audit checks whether first-party Dalashade shaders participate in the shared material/context vocabulary.");
        builder.AppendLine("- `Generated writes` means Dalashade can inject or write that uniform into the generated preset for that shader section. `Declares` and `Used` are read from local `.fx` source when available.");
        builder.AppendLine("- Shaders are allowed to specialize. Channels outside a shader's responsibility are reported as `Not applicable by design` rather than silently passing.");
        builder.AppendLine();

        foreach (var shader in MaterialParityShaders)
        {
            var source = sourceByShader[shader.FileName];
            var sourceAvailable = !string.IsNullOrWhiteSpace(source);
            var usesSharedResolver = sourceAvailable && UsesSharedMaterialResolver(source);
            var usesWaterResolver = sourceAvailable && source.Contains("Dalashade_ResolveWater", StringComparison.Ordinal);
            var usesSafetyResolver = sourceAvailable && source.Contains("Dalashade_ResolveSafety", StringComparison.Ordinal);
            var includesNormalField = sourceAvailable && source.Contains("Dalashade_NormalField.fxh", StringComparison.Ordinal);
            var usesNormalField = sourceAvailable && source.Contains("Dalashade_ResolveNormalField", StringComparison.Ordinal);
            var usesDepthNormal = sourceAvailable && source.Contains("Dalashade_GetDepthNormal", StringComparison.Ordinal);
            var usesDetailNormal = sourceAvailable && source.Contains("Dalashade_GetImageGradientNormal", StringComparison.Ordinal);
            var hasLocalMaterialLogic = sourceAvailable && HasLocalMaterialLogic(source, usesSharedResolver);
            var debugExposes = sourceAvailable
                               && (shader.Technique.Equals("Dalashade_MaterialDebug", StringComparison.OrdinalIgnoreCase)
                                   || shader.Technique.Equals("Dalashade_NormalDebug", StringComparison.OrdinalIgnoreCase)
                                   || source.Contains("Dalashade_MaterialDebugMode", StringComparison.Ordinal)
                                   || source.Contains("DebugMode", StringComparison.Ordinal));

            builder.AppendLine($"### {shader.Technique}");
            builder.AppendLine();
            builder.AppendLine($"- Shader file: `{shader.FileName}`");
            builder.AppendLine($"- Role: {shader.Role}");
            builder.AppendLine($"- Source available for declaration/use scan: {(sourceAvailable ? "yes" : "no")}");
            builder.AppendLine($"- Shared material resolver consumed: {(usesSharedResolver ? "yes" : sourceAvailable ? "no" : "unknown")}");
            builder.AppendLine($"- Water resolver consumed: {(usesWaterResolver ? "yes" : sourceAvailable ? "no" : "unknown")}");
            builder.AppendLine($"- Safety resolver consumed: {(usesSafetyResolver ? "yes" : sourceAvailable ? "no" : "unknown")}");
            builder.AppendLine($"- Normal field consumed: {FormatSourceScanStatus(includesNormalField || usesNormalField, sourceAvailable)}");
            builder.AppendLine($"- Depth normal consumed: {FormatSourceScanStatus(usesDepthNormal, sourceAvailable)}");
            builder.AppendLine($"- Detail normal consumed: {FormatSourceScanStatus(usesDetailNormal, sourceAvailable)}");
            builder.AppendLine($"- Local material override logic: {(hasLocalMaterialLogic ? "yes" : sourceAvailable ? "no" : "unknown")}");
            builder.AppendLine($"- Debug view exposes consumed material result: {(debugExposes ? "yes" : "no")}");
            builder.AppendLine();
            builder.AppendLine("| Channel | Declares | Generated writes | Expected use | Used | Parity status |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

            foreach (var channel in SharedMaterialParityChannels)
            {
                var declares = sourceAvailable && source.Contains(channel.Uniform, StringComparison.Ordinal);
                var writes = shader.WrittenUniforms.Contains(channel.Uniform);
                var expected = shader.ExpectedUniforms.Contains(channel.Uniform)
                               || (IsDepthAssistUniform(channel.Uniform) && (declares || writes));
                var used = sourceAvailable && CountOccurrences(source, channel.Uniform) > 1;
                builder.AppendLine($"| `{channel.Uniform}` | {YesNo(declares)} | {YesNo(writes)} | {YesNo(expected)} | {YesNo(used)} | {FormatParityStatus(declares, writes, expected, used, usesSharedResolver)} |");
            }

            builder.AppendLine();
        }

        builder.AppendLine("### Shared Material Channel Coverage");
        builder.AppendLine();
        builder.Append("| Channel |");
        foreach (var shader in MaterialParityShaders)
        {
            builder.Append($" {ShortShaderName(shader.Technique)} |");
        }

        builder.AppendLine();
        builder.Append("| --- |");
        foreach (var _ in MaterialParityShaders)
        {
            builder.Append(" --- |");
        }

        builder.AppendLine();

        foreach (var channel in SharedMaterialParityChannels)
        {
            builder.Append($"| `{channel.Uniform}` |");
            foreach (var shader in MaterialParityShaders)
            {
                var source = sourceByShader[shader.FileName];
                var sourceAvailable = !string.IsNullOrWhiteSpace(source);
                var declares = sourceAvailable && source.Contains(channel.Uniform, StringComparison.Ordinal);
                var writes = shader.WrittenUniforms.Contains(channel.Uniform);
                var expected = shader.ExpectedUniforms.Contains(channel.Uniform)
                               || (IsDepthAssistUniform(channel.Uniform) && (declares || writes));
                var used = sourceAvailable && CountOccurrences(source, channel.Uniform) > 1;
                builder.Append($" {FormatCoverageCell(declares, writes, expected, used)} |");
            }

            builder.AppendLine();
        }

        builder.AppendLine();
        builder.AppendLine("### Material Parity Notes");
        builder.AppendLine();
        builder.AppendLine("- SurfaceReflection currently has the advanced water context branch: `WaterContext`, `CoastalContext`, `OpenOceanContext`, `ShallowWaterContext`, and `WetSurfaceContext`.");
        builder.AppendLine("- Shaders that only receive `WaterPlane`, `SpecularGlint`, or legacy `WaterSpecular` are intentionally older water/specular consumers until they are explicitly upgraded.");
        builder.AppendLine("- `Dalashade_MaterialDebug.fx` is the truth viewer for shared shader-side masks. Production shaders may refine final masks, but this audit flags silent local-only material detection.");
        builder.AppendLine("- `Dalashade_RainWetContext` is listed as a planned vocabulary slot; it should remain `N/A` until a shader and mapper explicitly support it.");
        builder.AppendLine();
    }

    private static void AppendSceneGIDiagnostics(
        StringBuilder builder,
        Configuration configuration,
        PresetAnalysisResult analysis,
        PresetWriteResult writeResult,
        TagStackDiagnostics tagStackDiagnostics,
        ImageAnalysisResult currentImage,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence)
    {
        var technique = FindFirstPartyTechnique(analysis, "Dalashade_SceneGI");
        var writeLabel = configuration.EnableDalashadeSceneGIShaderVariables ? "written" : "configured";
        builder.AppendLine();
        builder.AppendLine("### SceneGI");
        builder.AppendLine();
        builder.AppendLine("- Responsibility: adaptive screen-space indirect lighting, AO, material bounce, night light pooling, and source-to-receiver propagation.");
        builder.AppendLine($"- Shader listed/detected: {(technique is null ? "no" : "yes")}");
        builder.AppendLine($"- Technique activation: {(technique is null ? "absent" : PresetAnalyzer.FormatActivationState(technique.ActivationState))}");
        builder.AppendLine($"- Generated variable writes enabled: {(configuration.EnableDalashadeSceneGIShaderVariables ? "yes" : "no")}");
        builder.AppendLine($"- GI strength {writeLabel}: {Math.Clamp(configuration.DalashadeSceneGIStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- AO intensity {writeLabel}: {Math.Clamp(configuration.DalashadeSceneGIAOIntensity, 0f, 1f):0.###}");
        builder.AppendLine($"- Bounce strength {writeLabel}: {Math.Clamp(configuration.DalashadeSceneGIBounceStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Night light strength {writeLabel}: {Math.Clamp(configuration.DalashadeSceneGINightLightStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Material influence {writeLabel}: {Math.Clamp(configuration.DalashadeSceneGIMaterialInfluence, 0f, 1f):0.###}");
        var sceneGIDebugWriteLabel = configuration.EnableDalashadeSceneGIShaderVariables ? "written" : "configured";
        builder.AppendLine($"- SceneGI debug mode {sceneGIDebugWriteLabel} value: {ClampInt(configuration.DalashadeSceneGIDebugMode, 0, 18)} ({FormatSceneGIDebugMode(configuration.DalashadeSceneGIDebugMode)}).");
        builder.AppendLine($"- SceneGI debug output mode {sceneGIDebugWriteLabel} value: {ClampInt(configuration.DalashadeSceneGIDebugOutputMode, 0, 4)} ({FormatSceneGIDebugOutputMode(configuration.DalashadeSceneGIDebugOutputMode)}).");
        builder.AppendLine($"- SceneGI debug opacity {sceneGIDebugWriteLabel} value: {Math.Clamp(configuration.DalashadeSceneGIDebugOpacity, 0f, 1f):0.###}.");
        builder.AppendLine($"- SceneGI debug boost {sceneGIDebugWriteLabel} value: {Math.Clamp(configuration.DalashadeSceneGIDebugBoost, 0.25f, 8f):0.###}. Debug boost affects diagnostic masks only, not normal GI output.");
        builder.AppendLine($"- Dominant SceneIntent drivers: {FormatDominantSceneDrivers(tagStackDiagnostics, SceneGIIntentNames)}");
        builder.AppendLine($"- Dominant MaterialIntent drivers: {FormatDominantMaterialDrivers(configuration, tagStackDiagnostics, currentImage, screenshotMaterialEvidence, SceneGIMaterialNames)}");
        builder.AppendLine($"- Generated SceneGI variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.SceneGIReasonCategory, "Dalashade_SceneGI")}");
        builder.AppendLine($"- Generated SceneGI material variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.MaterialReasonCategory, "Dalashade_SceneGI")}");
        builder.AppendLine("- Technique activation is manual unless `SyncDalashadeTechniqueActivation` is enabled; sync follows the SceneGI variable-write option.");
    }

    private static void AppendContactToneDiagnostics(
        StringBuilder builder,
        Configuration configuration,
        PresetAnalysisResult analysis,
        PresetWriteResult writeResult,
        TagStackDiagnostics tagStackDiagnostics,
        ImageAnalysisResult currentImage,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence)
    {
        var technique = FindFirstPartyTechnique(analysis, "Dalashade_ContactTone");
        var writeLabel = configuration.EnableDalashadeContactToneShaderVariables ? "written" : "configured";
        builder.AppendLine();
        builder.AppendLine("### ContactTone");
        builder.AppendLine();
        builder.AppendLine("- Responsibility: local contact tone, grounded edge darkening, and material readability contrast. It does not own GI bounce, reflections, weather air, or bloom.");
        builder.AppendLine($"- Shader listed/detected: {(technique is null ? "no" : "yes")}");
        builder.AppendLine($"- Technique activation: {(technique is null ? "absent" : PresetAnalyzer.FormatActivationState(technique.ActivationState))}");
        builder.AppendLine($"- Generated variable writes enabled: {(configuration.EnableDalashadeContactToneShaderVariables ? "yes" : "no")}");
        builder.AppendLine($"- ContactTone strength {writeLabel}: {Math.Clamp(configuration.DalashadeContactToneStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- ContactTone radius {writeLabel}: {Math.Clamp(configuration.DalashadeContactToneRadius, 0.20f, 2.0f):0.###}");
        builder.AppendLine($"- Depth edge strength {writeLabel}: {Math.Clamp(configuration.DalashadeContactToneEdgeStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Structure strength {writeLabel}: {Math.Clamp(configuration.DalashadeContactToneStructureStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Local contrast strength {writeLabel}: {Math.Clamp(configuration.DalashadeContactToneContrastStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- ContactTone debug mode {writeLabel} value: {ClampInt(configuration.DalashadeContactToneDebugMode, 0, 6)} ({FormatContactToneDebugMode(configuration.DalashadeContactToneDebugMode)}).");
        builder.AppendLine($"- ContactTone debug opacity {writeLabel} value: {Math.Clamp(configuration.DalashadeContactToneDebugOpacity, 0f, 1f):0.###}.");
        builder.AppendLine($"- Dominant SceneIntent drivers: {FormatDominantSceneDrivers(tagStackDiagnostics, SceneGIIntentNames)}");
        builder.AppendLine($"- Dominant MaterialIntent drivers: {FormatDominantMaterialDrivers(configuration, tagStackDiagnostics, currentImage, screenshotMaterialEvidence, ContactToneMaterialNames)}");
        builder.AppendLine($"- Generated ContactTone variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.ContactToneReasonCategory, "Dalashade_ContactTone")}");
        builder.AppendLine($"- Generated ContactTone material variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.MaterialReasonCategory, "Dalashade_ContactTone")}");
        builder.AppendLine("- Technique activation is manual unless `SyncDalashadeTechniqueActivation` is enabled; sync follows the ContactTone variable-write option.");
    }

    private static void AppendSurfaceReflectionDiagnostics(
        StringBuilder builder,
        Configuration configuration,
        PresetAnalysisResult analysis,
        PresetWriteResult writeResult,
        TagStackDiagnostics tagStackDiagnostics,
        ImageAnalysisResult currentImage,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence)
    {
        var technique = FindFirstPartyTechnique(analysis, "Dalashade_SurfaceReflection");
        var writeLabel = configuration.EnableDalashadeSurfaceReflectionShaderVariables ? "written" : "configured";
        builder.AppendLine();
        builder.AppendLine("### SurfaceReflection");
        builder.AppendLine();
        builder.AppendLine("- Responsibility: material-aware water sheen, wet reflection impression, specular glints, aether/neon streaks, icy sheen, and polished-surface response.");
        builder.AppendLine($"- Shader listed/detected: {(technique is null ? "no" : "yes")}");
        builder.AppendLine($"- Technique activation: {(technique is null ? "absent" : PresetAnalyzer.FormatActivationState(technique.ActivationState))}");
        builder.AppendLine($"- Generated variable writes enabled: {(configuration.EnableDalashadeSurfaceReflectionShaderVariables ? "yes" : "no")}");
        builder.AppendLine($"- Reflection strength {writeLabel}: {Math.Clamp(configuration.DalashadeSurfaceReflectionStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Water sheen strength {writeLabel}: {Math.Clamp(configuration.DalashadeSurfaceReflectionWaterSheenStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Water reflection strength {writeLabel}: 0.45");
        builder.AppendLine($"- Specular glint strength {writeLabel}: {Math.Clamp(configuration.DalashadeSurfaceReflectionSpecularGlintStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Specular reflection strength {writeLabel}: 0.30");
        builder.AppendLine($"- Wet reflection strength {writeLabel}: {Math.Clamp(configuration.DalashadeSurfaceReflectionWetStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Aether/neon reflection strength {writeLabel}: {Math.Clamp(configuration.DalashadeSurfaceReflectionAetherNeonStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Reflection sample offset {writeLabel}: 0.018");
        builder.AppendLine($"- SurfaceReflection debug mode {writeLabel} value: {ClampInt(configuration.DalashadeSurfaceReflectionDebugMode, 0, 14)} ({FormatSurfaceReflectionDebugMode(configuration.DalashadeSurfaceReflectionDebugMode)}).");
        builder.AppendLine($"- SurfaceReflection debug output mode {writeLabel} value: 0 ({FormatSurfaceReflectionDebugOutputMode(0)}).");
        builder.AppendLine($"- SurfaceReflection debug opacity {writeLabel} value: {Math.Clamp(configuration.DalashadeSurfaceReflectionDebugOpacity, 0f, 1f):0.###}.");
        var materialProfile = MaterialProfileBuilder.Build(tagStackDiagnostics, currentImage, configuration.ScreenshotAnalysisStrength);
        var screenshotEvidenceContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(configuration, tagStackDiagnostics, screenshotMaterialEvidence.Evidence);
        var materialIntent = configuration.EnableMaterialIntent
            ? MaterialIntentBuilder.Build(tagStackDiagnostics, currentImage, materialProfile, screenshotStrength: configuration.ScreenshotAnalysisStrength, screenshotMaterialEvidenceContributions: screenshotEvidenceContributions).WithStrength(configuration.MaterialIntentStrength)
            : MaterialIntent.Neutral;
        builder.AppendLine($"- Water resolver context: WaterContext={materialIntent.WaterSpecular:0.###}, CoastalContext={materialIntent.WaterSpecular:0.###}, OpenOceanContext={materialIntent.WaterSpecular * 0.85f:0.###}, ShallowWaterContext={Math.Max(materialIntent.WaterSpecular * 0.72f, Math.Min(materialIntent.WaterSpecular, materialIntent.SandDust) * 0.20f):0.###}, WetSurfaceContext={tagStackDiagnostics.Intent.Wetness:0.###}.");
        builder.AppendLine("- Water resolver note: scene context values are generated-preset priors; shader-side `Dalashade_ResolveWater` still performs per-pixel water classification and rejects sky, sand, skin, and isolated glints.");
        builder.AppendLine($"- Dominant MaterialIntent drivers: {FormatDominantMaterialDrivers(configuration, tagStackDiagnostics, currentImage, screenshotMaterialEvidence, SurfaceReflectionMaterialNames)}");
        builder.AppendLine($"- Generated SurfaceReflection variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.SurfaceReflectionReasonCategory, "Dalashade_SurfaceReflection")}");
        builder.AppendLine($"- Generated SurfaceReflection material variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.MaterialReasonCategory, "Dalashade_SurfaceReflection")}");
        builder.AppendLine("- Technique activation is manual unless `SyncDalashadeTechniqueActivation` is enabled; sync follows the SurfaceReflection variable-write option.");
    }

    private static void AppendMaterialIntentDiagnostics(
        StringBuilder builder,
        Configuration configuration,
        TagStackDiagnostics tagStackDiagnostics,
        ImageAnalysisResult currentImage,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence,
        IReadOnlyList<SceneTagPreset>? activeTagRegistry,
        PresetWriteResult writeResult)
    {
        if (!configuration.EnableMaterialIntentDiagnostics)
        {
            return;
        }

        builder.AppendLine("## Material Intent Diagnostics");
        builder.AppendLine();
        if (!configuration.EnableMaterialIntent)
        {
            builder.AppendLine("- MaterialIntent is disabled in configuration.");
            builder.AppendLine("- No MaterialIntent values were calculated for this report.");
            builder.AppendLine();
            return;
        }

        var profile = MaterialProfileBuilder.Build(tagStackDiagnostics, currentImage, configuration.ScreenshotAnalysisStrength);
        var screenshotEvidenceContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(configuration, tagStackDiagnostics, screenshotMaterialEvidence.Evidence);
        var registry = MaterialTagRegistryTuningAnalyzer.Build(tagStackDiagnostics, configuration.EnableSceneAuthoringOverrides ? activeTagRegistry : null);
        var intent = MaterialIntentBuilder.Build(tagStackDiagnostics, currentImage, profile, configuration.EnableSceneAuthoringOverrides ? activeTagRegistry : null, screenshotStrength: configuration.ScreenshotAnalysisStrength, screenshotMaterialEvidenceContributions: screenshotEvidenceContributions).WithStrength(configuration.MaterialIntentStrength);
        var writtenUniforms = writeResult.Changes
            .Where(change => string.Equals(change.ReasonCategory, CustomShaderVariableMapper.MaterialReasonCategory, StringComparison.OrdinalIgnoreCase))
            .OrderBy(change => change.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        builder.AppendLine("- MaterialIntent is an inferred material-likelihood diagnostic layer. It uses tags, territory/weather text, gameplay context, screenshot metrics, and existing SceneIntent context; it does not detect true engine material IDs.");
        builder.AppendLine("- MaterialProfile is a scene-level plausibility layer between SceneTags/MaterialIntent and shader-side pixel masks. Shader masks still decide per-pixel `RawCandidate`, `SceneGatedCandidate`, and `FinalMask` behavior.");
        builder.AppendLine("- MaterialIntent does not change SceneIntent or VisualProfile. Generated shader variables are written only when MaterialIntent shader mapping is explicitly enabled and matching Dalashade custom shader keys exist.");
        builder.AppendLine($"- MaterialIntent strength: {configuration.MaterialIntentStrength:0.###}");
        builder.AppendLine($"- Screenshot material evidence influence: {(configuration.EnableScreenshotMaterialEvidenceInfluence ? $"enabled at {configuration.ScreenshotMaterialEvidenceStrength:0.##}" : "disabled")}");
        builder.AppendLine($"- Tag registry material caps: per tag +/-{MaterialTagRegistryTuningAnalyzer.PerTagContributionCap:0.##}; per channel total +/-{MaterialTagRegistryTuningAnalyzer.PerChannelContributionCap:0.##}");
        builder.AppendLine($"- Shader mapping: {(configuration.EnableMaterialIntentShaderMapping ? "enabled" : "disabled")}");
        builder.AppendLine("- Material debug overlay controls: shader-owned in ReShade UI. Reports show MaterialIntent values and written material channel uniforms only.");
        builder.AppendLine("- Safety switch: disable `EnableMaterialIntentShaderMapping` to stop all MaterialIntent uniform writes on the next generation. Disable `EnableMaterialIntent` to return neutral material diagnostics.");
        if (!configuration.EnableMaterialIntentShaderMapping)
        {
            builder.AppendLine("- Diagnostics only, no visual shader mapping. Disabled mapping skips MaterialIntent uniforms instead of writing zeroes.");
        }
        else if (configuration.MaterialIntentStrength <= 0f)
        {
            builder.AppendLine("- MaterialIntent shader mapping is enabled but strength is 0.0, so MaterialIntent uniforms are skipped.");
        }
        else if (writtenUniforms.Length == 0)
        {
            builder.AppendLine("- MaterialIntent shader mapping is enabled, but no matching known MaterialIntent uniforms were written. Missing uniforms or missing Dalashade custom shader sections are skipped safely.");
        }
        else
        {
            builder.AppendLine($"- MaterialIntent uniforms written: {writtenUniforms.Length}. Final material channel values are raw inferred value multiplied by MaterialIntent strength.");
        }

        builder.AppendLine();
        builder.AppendLine("### MaterialProfile Scene Plausibility");
        builder.AppendLine();
        builder.AppendLine($"- Selected family: {profile.Family}");
        builder.AppendLine($"- Profile tags: {FormatPlainList(profile.ProfileTags)}");
        builder.AppendLine($"- Top material priors: {FormatMaterialProfilePriors(profile)}");
        builder.AppendLine();
        builder.AppendLine("| Channel | Prior | Positive profile reasons | Profile suppressions |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var channel in MaterialIntent.ChannelNames)
        {
            builder.AppendLine($"| {channel} | {profile.ValueFor(channel):0.###} | {FormatMaterialProfileContributions(profile.Contributions, channel, positive: true)} | {FormatMaterialProfileContributions(profile.Contributions, channel, positive: false)} |");
        }

        builder.AppendLine();
        AppendMaterialTagRegistryDiagnostics(builder, configuration, registry.Diagnostics);

        builder.AppendLine();
        builder.AppendLine(MaterialIntentDiagnosticsTableHeader);
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var channel in MaterialIntent.ChannelNames)
        {
            var profilePrior = profile.ValueFor(channel);
            var evidence = FormatMaterialNonProfileEvidence(intent.Contributions, channel);
            var screenshotEvidenceText = FormatMaterialScreenshotEvidence(intent.Contributions, channel);
            var suppressions = FormatMaterialContributions(intent.Contributions, channel, positive: false);
            builder.AppendLine($"| {channel} | {profilePrior:0.###} | {evidence} | {screenshotEvidenceText} | {intent.ValueFor(channel):0.###} | {suppressions} |");
        }

        builder.AppendLine();
        builder.AppendLine("### MaterialIntent Suppression And False-Positive Notes");
        builder.AppendLine();
        var suppressedChannels = MaterialIntent.ChannelNames
            .Select(channel => new
            {
                Channel = channel,
                Value = intent.ValueFor(channel),
                PositiveCount = intent.Contributions.Count(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal) && contribution.Amount > 0f),
                NegativeCount = intent.Contributions.Count(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal) && contribution.Amount < 0f)
            })
            .Where(item => item.NegativeCount > 0 || item.Value <= 0.05f)
            .ToArray();
        if (suppressedChannels.Length == 0)
        {
            builder.AppendLine("- No notable suppressions recorded.");
        }
        else
        {
            foreach (var item in suppressedChannels)
            {
                var note = item.Value <= 0.05f && item.PositiveCount == 0
                    ? "low/no evidence"
                    : item.NegativeCount > 0
                        ? "suppression evidence present"
                        : "low final likelihood";
                builder.AppendLine($"- {item.Channel}: {item.Value:0.###} ({note}). Review the contribution table above for false-positive checks.");
            }
        }

        builder.AppendLine();
        builder.AppendLine("### MaterialIntent Shader Uniform Output");
        builder.AppendLine();
        if (writtenUniforms.Length == 0)
        {
            builder.AppendLine("- None.");
        }
        else
        {
            foreach (var change in writtenUniforms)
            {
                builder.AppendLine($"- `{change.Section}` / `{change.Key}`: {change.OldValue} -> {change.NewValue} | reason={change.ReasonCategory}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("### MaterialMasks V2 Calibration Notes");
        builder.AppendLine();
        builder.AppendLine("- Shader-side mask vocabulary: `RawCandidate` is local pixel evidence, `SceneGatedCandidate` is raw evidence scaled by MaterialProfile/MaterialIntent plausibility, and `FinalMask` is the conflict-resolved mask used by a production shader.");
        builder.AppendLine("- Water/specular split: `WaterPlane` is broad likely water surface, `SpecularGlint` is thin reflective highlight evidence, and `WaterSpecular` remains the combined backward-compatible mask/uniform family.");
        builder.AppendLine("- Depth assist: optional, shader-owned, and disabled by default. It can improve sky/water/snow/foreground separation only when the ReShade depth buffer is valid; color, texture, smoothness, screen-region, and scene gates still work without depth.");
        builder.AppendLine("- Depth confidence means usable signal confidence for material-mask heuristics, not guaranteed correct FFXIV engine depth. DLSS/upscaling, dynamic resolution, depth-buffer restrictions, or UI/depth mismatches can make depth unreliable.");
        builder.AppendLine($"- Sections receiving MaterialIntent uniforms: {FormatMaterialUniformSections(writtenUniforms)}");
        builder.AppendLine($"- Shader-owned depth-assist controls injected: {FormatInjectedDepthAssistVariables(writeResult)}");
        builder.AppendLine("- First-party shader material responsibilities: SmartSharpen suppresses unsafe sharpening, AtmosphereBloom gates local glow, WeatherAtmosphere shapes air/haze, AdaptiveGrade applies subtle material protection, SceneGI provides optional screen-space AO/bounce/light-pooling, and MaterialDebug visualizes masks only when manually enabled.");
        builder.AppendLine("- Likely failure sources to inspect: scene profile plausibility, MaterialIntent strength/gating, raw pixel heuristic, final conflict suppression, optional depth assist, then the specific production shader debug view.");

        builder.AppendLine();
    }

    private static void AppendMaterialTagRegistryDiagnostics(StringBuilder builder, Configuration configuration, MaterialTagRegistryDiagnostics diagnostics)
    {
        builder.AppendLine("### Tag Registry Material Tunings");
        builder.AppendLine();
        if (!configuration.EnableSceneAuthoringOverrides)
        {
            builder.AppendLine("- Scene authoring/tag registry is disabled, so registry material tunings are not applied.");
            return;
        }

        builder.AppendLine($"- Caps: per-tag contribution +/-{MaterialTagRegistryTuningAnalyzer.PerTagContributionCap:0.##}; per-channel registry total +/-{MaterialTagRegistryTuningAnalyzer.PerChannelContributionCap:0.##}.");
        builder.AppendLine("- Invalid rows are ignored. Disabled rows and inactive tags are listed for audit but do not contribute.");
        builder.AppendLine();
        builder.AppendLine("| Channel | Final registry contribution | Capped |");
        builder.AppendLine("| --- | ---: | --- |");
        var visibleChannels = diagnostics.Channels
            .Where(channel => Math.Abs(channel.FinalContribution) > 0.0001f || channel.Capped)
            .ToArray();
        if (visibleChannels.Length == 0)
        {
            builder.AppendLine("| None | 0 | no |");
        }
        else
        {
            foreach (var channel in visibleChannels)
            {
                builder.AppendLine($"| {channel.Channel} | {channel.FinalContribution:+0.###;-0.###;0} | {(channel.Capped ? "yes" : "no")} |");
            }
        }

        AppendRegistryTuningRows(builder, "Active registry tunings", diagnostics.ActiveTunings);
        AppendRegistryTuningRows(builder, "Capped registry tunings", diagnostics.CappedTunings);
        AppendRegistryTuningRows(builder, "Invalid registry tunings", diagnostics.InvalidTunings);
        AppendRegistryTuningRows(builder, "Inactive registry tunings", diagnostics.InactiveTunings.Take(12).ToArray());
        if (diagnostics.InactiveTunings.Count > 12)
        {
            builder.AppendLine($"- Inactive registry tunings truncated: showing 12 of {diagnostics.InactiveTunings.Count}.");
        }
    }

    private static void AppendRegistryTuningRows(StringBuilder builder, string title, IReadOnlyList<MaterialTagRegistryTuningDiagnostic> rows)
    {
        builder.AppendLine();
        builder.AppendLine($"#### {title}");
        builder.AppendLine();
        if (rows.Count == 0)
        {
            builder.AppendLine("- None.");
            return;
        }

        builder.AppendLine("| Status | Category | Tag | Channel | Requested | Applied | Reason / message |");
        builder.AppendLine("| --- | --- | --- | --- | ---: | ---: | --- |");
        foreach (var row in rows)
        {
            builder.AppendLine($"| {EscapeTable(row.Status)} | {EscapeTable(row.Category)} | {EscapeTable(row.Tag)} | {EscapeTable(row.Channel)} | {row.RequestedAmount:+0.###;-0.###;0} | {row.AppliedAmount:+0.###;-0.###;0} | {EscapeTable(row.Reason)} {EscapeTable(row.Message)} |");
        }
    }

    private static void AppendNormalFieldDiagnostics(StringBuilder builder, Configuration configuration, PresetAnalysisResult analysis, PresetWriteResult writeResult)
    {
        builder.AppendLine("## Normal Field Diagnostics");
        builder.AppendLine();
        var normalDebug = FindFirstPartyTechnique(analysis, "Dalashade_NormalDebug");
        var normalFieldIncludeAvailable = !string.IsNullOrWhiteSpace(ReadShaderSource(configuration, "Dalashade_NormalField.fxh"));
        builder.AppendLine($"- Enabled: {(configuration.EnableNormalField ? "yes" : "no")}");
        builder.AppendLine($"- Diagnostics enabled: {(configuration.EnableNormalFieldDiagnostics ? "yes" : "no")}");
        builder.AppendLine($"- Shader mapping enabled: {(configuration.EnableNormalFieldShaderMapping ? "yes" : "no")}");
        builder.AppendLine($"- Normal field strength: {Math.Clamp(configuration.NormalFieldStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Depth strength: {Math.Clamp(configuration.NormalFieldDepthStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Detail strength: {Math.Clamp(configuration.NormalFieldDetailStrength, 0f, 1f):0.###}");
        builder.AppendLine($"- Material influence: {Math.Clamp(configuration.NormalFieldMaterialInfluence, 0f, 1f):0.###}");
        builder.AppendLine($"- Water suppression: {Math.Clamp(configuration.NormalFieldWaterSuppression, 0f, 1f):0.###}");
        builder.AppendLine($"- Skin suppression: {Math.Clamp(configuration.NormalFieldSkinSuppression, 0f, 1f):0.###}");
        builder.AppendLine($"- Sky suppression: {Math.Clamp(configuration.NormalFieldSkySuppression, 0f, 1f):0.###}");
        builder.AppendLine($"- Debug mode: {Math.Clamp(configuration.NormalFieldDebugMode, 0, 20)}");
        builder.AppendLine($"- Debug boost: {Math.Clamp(configuration.NormalFieldDebugBoost, 0.25f, 8f):0.###}");
        builder.AppendLine($"- NormalDebug shader section present: {(normalDebug is null ? "no" : "yes")}");
        builder.AppendLine($"- NormalDebug technique active in analyzed preset: {(normalDebug?.ActivationState == TechniqueActivationState.Active ? "yes" : "no")}");
        builder.AppendLine("- NormalDebug live ReShade UI state observed: no; this report only analyzes generated/active preset text.");
        builder.AppendLine($"- NormalField include installed locally for diagnostics scan: {(normalFieldIncludeAvailable ? "yes" : "no")}");
        builder.AppendLine("- Current note: `NormalField shader files are not required unless the optional NormalDebug shader is installed/enabled.`");

        if (!configuration.EnableNormalField)
        {
            builder.AppendLine("- NormalField disabled; production shaders are unaffected.");
        }
        else if (!configuration.EnableNormalFieldShaderMapping)
        {
            builder.AppendLine("- NormalField diagnostics enabled, but generated-preset shader mapping is disabled.");
        }
        else if (configuration.NormalFieldStrength <= 0f)
        {
            builder.AppendLine("- Shader mapping is enabled but NormalField strength is 0.0, so shader-side normal influence remains disabled.");
        }
        if (normalDebug is null)
        {
            builder.AppendLine("- NormalDebug shader file not found. This is not an error unless you are trying to debug NormalField.");
        }

        var writtenUniforms = writeResult.Changes
            .Where(change => string.Equals(change.ReasonCategory, CustomShaderVariableMapper.NormalFieldReasonCategory, StringComparison.OrdinalIgnoreCase))
            .OrderBy(change => change.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        builder.AppendLine($"- NormalField uniforms written: {(writtenUniforms.Length == 0 ? "none" : string.Join(", ", writtenUniforms.Select(change => $"`{change.Section}:{change.Key}`")))}");
        builder.AppendLine("- NormalField is an inferred screen-space surface field from depth, luma/color gradients, material context, water context, and safety gates. It is not true FFXIV G-buffer or material normal access.");
        builder.AppendLine();
        builder.AppendLine("### Production Shader Consumption");
        builder.AppendLine();
        builder.AppendLine($"- SceneGI: {FormatNormalFieldProductionConsumption(configuration, "Dalashade_SceneGI.fx")}");
        builder.AppendLine($"- SurfaceReflection: {FormatNormalFieldProductionConsumption(configuration, "Dalashade_SurfaceReflection.fx")}");
        builder.AppendLine($"- SmartSharpen: {FormatNormalFieldProductionConsumption(configuration, "Dalashade_SmartSharpen.fx")}");
        builder.AppendLine($"- WeatherAtmosphere: {FormatNormalFieldProductionConsumption(configuration, "Dalashade_WeatherAtmosphere.fx")}");
        builder.AppendLine($"- AtmosphereBloom: {FormatNormalFieldProductionConsumption(configuration, "Dalashade_AtmosphereBloom.fx")}");
        builder.AppendLine($"- AdaptiveGrade: {FormatNormalFieldProductionConsumption(configuration, "Dalashade_AdaptiveGrade.fx")}");
        builder.AppendLine($"- ContactTone: {FormatNormalFieldProductionConsumption(configuration, "Dalashade_ContactTone.fx")}");
        builder.AppendLine("- WeatherParticles/LightHierarchy/CombatClarity: not applicable unless present.");
        builder.AppendLine();
    }

    private static void AppendSceneAuthoringDiagnostics(StringBuilder builder, Configuration configuration, SceneAuthoringState state)
    {
        builder.AppendLine("## Scene Authoring");
        builder.AppendLine();
        builder.AppendLine($"- Config enabled: {configuration.EnableSceneAuthoringOverrides}");
        builder.AppendLine($"- State enabled: {state.Enabled}");
        builder.AppendLine($"- Message: {state.Message}");
        builder.AppendLine($"- Override file: `{state.StoragePath}`");
        builder.AppendLine($"- Active override: {(state.ActiveOverride is null ? "none" : $"{state.ActiveOverride.Scope}:{state.ActiveOverride.TerritoryId} {state.ActiveOverride.TerritoryName}")}");
        builder.AppendLine($"- Fingerprint: `{state.Fingerprint}`");
        builder.AppendLine($"- Detected area/weather/biome: {state.DetectedTags.AreaKey} / {state.DetectedTags.WeatherKey} / {state.DetectedTags.BiomeKey}");
        builder.AppendLine($"- Effective area/weather/biome: {state.EffectiveTags.AreaKey} / {state.EffectiveTags.WeatherKey} / {state.EffectiveTags.BiomeKey}");
        builder.AppendLine($"- Detected mood tags: {FormatPlainList(state.DetectedTags.MoodTags)}");
        builder.AppendLine($"- Effective mood tags: {FormatPlainList(state.EffectiveTags.MoodTags)}");
        builder.AppendLine($"- Added overrides: {FormatTagOverrideMap(state.AddedTags)}");
        builder.AppendLine($"- Removed overrides: {FormatTagOverrideMap(state.RemovedTags)}");
        builder.AppendLine($"- Suppressed diagnostic tags: {FormatTagOverrideMap(state.EffectiveTags.SuppressedAuthoringTags)}");
        AppendLines(builder, "Scene Authoring Warnings", state.Warnings);
    }

    private static void AppendFrameDataDiagnostics(StringBuilder builder, Configuration configuration, PresetAnalysisResult analysis, PresetWriteResult writeResult)
    {
        builder.AppendLine("## FrameData Diagnostics");
        builder.AppendLine();
        var frameDataIncludeAvailable = !string.IsNullOrWhiteSpace(ReadShaderSource(configuration, "Dalashade_FrameData.fxh"));
        var frameDataDebugSourceAvailable = !string.IsNullOrWhiteSpace(ReadShaderSource(configuration, "Dalashade_FrameDataDebug.fx"));
        var frameDataDebug = FindFirstPartyTechnique(analysis, "Dalashade_FrameDataDebug");
        var generatedValues = ReadGeneratedPresetSectionValues(configuration.GeneratedPresetPath, "Dalashade_FrameDataDebug");
        var frameDataConsumers = GetFrameDataProductionConsumerLabels(configuration);
        var frameDataMigrations = GetFrameDataProductionConsumerFiles(configuration);

        builder.AppendLine("- FrameDataMode: Inline");
        builder.AppendLine("- FrameDataPrepass: NotImplemented");
        builder.AppendLine($"- ProductionFrameDataConsumers: {FormatPlainList(frameDataConsumers)}");
        builder.AppendLine($"- ProductionShadersMigratedToFrameData: {FormatPlainList(frameDataMigrations)}");
        builder.AppendLine($"- FrameData include source available for report scan: {YesNo(frameDataIncludeAvailable)}");
        builder.AppendLine($"- FrameDataDebug shader source available for report scan: {YesNo(frameDataDebugSourceAvailable)}");
        builder.AppendLine($"- Generated preset contains FrameDataDebug section: {YesNo(generatedValues.SectionPresent)}");
        builder.AppendLine($"- FrameDataDebug technique present in preset analysis: {YesNo(frameDataDebug is not null)}");
        builder.AppendLine($"- FrameDataDebug technique active: {YesNo(frameDataDebug?.ActivationState == TechniqueActivationState.Active)}");
        if (!string.IsNullOrWhiteSpace(generatedValues.Warning))
        {
            builder.AppendLine($"- Generated preset FrameDataDebug scan warning: {generatedValues.Warning}");
        }

        foreach (var variable in FrameDataDebugVariables)
        {
            builder.AppendLine($"- `{variable}`: {FormatGeneratedPresetValue(generatedValues, variable)}");
        }

        var frameDataDebugWrites = writeResult.Changes
            .Where(change => FrameDataDebugVariables.Contains(change.Key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(change => change.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        builder.AppendLine($"- FrameDataDebug variables changed this generation: {(frameDataDebugWrites.Length == 0 ? "none" : string.Join(", ", frameDataDebugWrites.Select(change => $"`{change.Section}:{change.Key}`")))}");
        builder.AppendLine("- Current note: production first-party shaders use inline FrameData. No prepass or render target exists yet.");
        builder.AppendLine();
        builder.AppendLine("### Production Shader FrameData Scan");
        builder.AppendLine();
        foreach (var fileName in ProductionShaderFiles)
        {
            builder.AppendLine($"- {fileName}: {FormatFrameDataProductionConsumption(configuration, fileName)}");
        }

        builder.AppendLine();
    }

    private static void AppendFirstPartyDepthAssistDiagnostics(StringBuilder builder, Configuration configuration, PresetWriteResult writeResult)
    {
        builder.AppendLine("## First-Party Depth Assist Diagnostics");
        builder.AppendLine();
        var written = writeResult.Changes
            .Where(change => IsDepthAssistUniform(change.Key)
                             && string.Equals(change.ReasonCategory, CustomShaderVariableMapper.FirstPartyDepthAssistReasonCategory, StringComparison.OrdinalIgnoreCase))
            .OrderBy(change => change.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var generatedValues = ReadGeneratedPresetVariables(configuration.GeneratedPresetPath, ["Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"]);

        builder.AppendLine($"- EnableFirstPartyDepthAssist: {YesNo(configuration.EnableFirstPartyDepthAssist)}");
        builder.AppendLine($"- Generated-preset writes enabled: {YesNo(configuration.EnableDalashadeCustomShaders)}");
        builder.AppendLine($"- Custom shader section injection enabled: {YesNo(configuration.AutoInjectDalashadeCustomShaderSections)}");
        builder.AppendLine($"- Depth-assist variables written to first-party sections: {(written.Length == 0 ? "none" : string.Join(", ", written.Select(change => $"`{change.Section}:{change.Key}`").Distinct(StringComparer.OrdinalIgnoreCase)))}");
        builder.AppendLine($"- Sections receiving depth assist: {FormatDepthAssistSections(written)}");
        builder.AppendLine($"- Generated preset sections with depth-assist values: {FormatGeneratedPresetVariableSections(generatedValues)}");
        builder.AppendLine("- Known variables: `Dalashade_EnableDepthAssist`, `Dalashade_DepthAssistStrength`, `Dalashade_DepthAssistConfidenceFloor`, `Dalashade_DepthConfidenceFloor`.");
        builder.AppendLine("- Current note: depth assist is opt-in, generated-preset-only, and does not activate techniques. It can improve or worsen resolver confidence depending on ReShade depth reliability.");
        builder.AppendLine();
    }

    private static void AppendDalapadDiagnostics(StringBuilder builder, DalapadDiagnostics diagnostics)
    {
        builder.AppendLine("## Dalapad Diagnostics");
        builder.AppendLine();
        builder.AppendLine("- Display name: Dalapad");
        builder.AppendLine("- Purpose: optional external surface-data addon research probe.");
        builder.AppendLine("- Runtime behavior: diagnostic-only metadata inspection.");
        builder.AppendLine($"- Probed: {YesNo(diagnostics.Probed)}");
        builder.AppendLine($"- Status: {diagnostics.Status}");
        builder.AppendLine($"- Summary: {diagnostics.Summary}");
        if (diagnostics.ProbeTimestamp != DateTimeOffset.MinValue)
        {
            builder.AppendLine($"- Probe timestamp: {diagnostics.ProbeTimestamp:O}");
        }

        builder.AppendLine($"- Runtime assembly: {FormatOptionalInlineCode(diagnostics.RuntimeAssembly)}");
        builder.AppendLine($"- RenderTargetManager type: {FormatOptionalInlineCode(diagnostics.RenderTargetManagerTypeName)}");
        builder.AppendLine($"- RenderTargetManager type found: {YesNo(diagnostics.RenderTargetManagerTypeFound)}");
        builder.AppendLine($"- Instance metadata found: {YesNo(diagnostics.InstanceMethodFound)}");
        builder.AppendLine($"- GBuffer metadata found: {YesNo(diagnostics.GBufferMemberFound)}");
        builder.AppendLine($"- DepthStencil metadata found: {YesNo(diagnostics.DepthStencilMemberFound)}");
        builder.AppendLine($"- Texture metadata found: {YesNo(diagnostics.TextureTypeFound)}");
        builder.AppendLine($"- Addon contract version: `{diagnostics.AddonContractVersion}`");
        builder.AppendLine($"- IPC contract version: `{diagnostics.IpcContractVersion}`");
        builder.AppendLine();

        builder.AppendLine("| Capability | Available | Detail |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var capability in diagnostics.Capabilities)
        {
            builder.AppendLine($"| {capability.Name} | {YesNo(capability.Available)} | {capability.Detail} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Addon Resource Contract");
        builder.AppendLine();
        builder.AppendLine("| Resource | Kind | Expected Source | Diagnostic Use | Availability Flag |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var resource in diagnostics.AddonResources)
        {
            builder.AppendLine($"| {EscapeTable(resource.Name)} | {EscapeTable(resource.Kind)} | {EscapeTable(resource.ExpectedSource)} | {EscapeTable(resource.DiagnosticOnlyUse)} | {EscapeTable(resource.AvailabilityFlag)} |");
        }

        builder.AppendLine();
        builder.AppendLine("### IPC Status");
        builder.AppendLine();
        builder.AppendLine($"- Status: {diagnostics.IpcStatus.Status}");
        builder.AppendLine($"- Summary: {diagnostics.IpcStatus.Summary}");
        builder.AppendLine($"- Status file: {FormatOptionalInlineCode(diagnostics.IpcStatus.StatusFilePath)}");
        builder.AppendLine($"- Status file found: {YesNo(diagnostics.IpcStatus.StatusFileFound)}");
        builder.AppendLine($"- Bridge reported: {YesNo(diagnostics.IpcStatus.BridgeReported)}");
        builder.AppendLine($"- Contract compatible: {YesNo(diagnostics.IpcStatus.ContractCompatible)}");
        builder.AppendLine($"- Bridge version: {FormatOptionalInlineCode(diagnostics.IpcStatus.BridgeVersion)}");
        builder.AppendLine($"- Addon process: {FormatOptionalInlineCode(diagnostics.IpcStatus.AddonProcess)}");
        if (diagnostics.IpcStatus.LastUpdateUtc.HasValue)
        {
            builder.AppendLine($"- Last update: {diagnostics.IpcStatus.LastUpdateUtc.Value:O}");
        }

        builder.AppendLine($"- Reported resources: {(diagnostics.IpcStatus.ReportedResources.Count == 0 ? "none" : string.Join(", ", diagnostics.IpcStatus.ReportedResources.Select(resource => $"`{EscapeTable(resource)}`")))}");
        builder.AppendLine($"- IPC warnings: {(diagnostics.IpcStatus.Warnings.Count == 0 ? "none" : string.Join("; ", diagnostics.IpcStatus.Warnings.Select(EscapeTable)))}");
        AppendDalapadResourceCatalog(builder, "Status-File Resource Catalog", diagnostics.IpcStatus.ResourceCatalog);

        builder.AppendLine();
        builder.AppendLine("### Control Pipe Health");
        builder.AppendLine();
        builder.AppendLine($"- Pipe: `{diagnostics.ControlPipeStatus.PipeName}`");
        builder.AppendLine($"- Attempted: {YesNo(diagnostics.ControlPipeStatus.Attempted)}");
        builder.AppendLine($"- Listening: {YesNo(diagnostics.ControlPipeStatus.PipeListening)}");
        builder.AppendLine($"- Response received: {YesNo(diagnostics.ControlPipeStatus.ResponseReceived)}");
        builder.AppendLine($"- Contract compatible: {YesNo(diagnostics.ControlPipeStatus.ContractCompatible)}");
        builder.AppendLine($"- Status: {EscapeTable(diagnostics.ControlPipeStatus.Status)}");
        builder.AppendLine($"- Summary: {EscapeTable(diagnostics.ControlPipeStatus.Summary)}");
        builder.AppendLine($"- Bridge version: {FormatOptionalInlineCode(diagnostics.ControlPipeStatus.BridgeVersion)}");
        builder.AppendLine($"- Response type: {FormatOptionalInlineCode(diagnostics.ControlPipeStatus.ResponseType)}");
        builder.AppendLine($"- Request id: {FormatOptionalInlineCode(diagnostics.ControlPipeStatus.RequestId)}");
        builder.AppendLine($"- Elapsed: {diagnostics.ControlPipeStatus.ElapsedMilliseconds} ms");
        builder.AppendLine($"- Pipe warnings: {(diagnostics.ControlPipeStatus.Warnings.Count == 0 ? "none" : string.Join("; ", diagnostics.ControlPipeStatus.Warnings.Select(EscapeTable)))}");
        builder.AppendLine();
        builder.AppendLine("| Capability | Enabled |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Supports status file | {YesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsStatusFile)} |");
        builder.AppendLine($"| Supports control pipe | {YesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsControlPipe)} |");
        builder.AppendLine($"| Supports realtime uniforms | {YesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsRealtimeUniforms)} |");
        builder.AppendLine($"| Supports resource catalog | {YesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsResourceCatalog)} |");
        builder.AppendLine($"| Supports debug visualization | {YesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsDebugVisualization)} |");
        builder.AppendLine($"| Reads render targets | {YesNo(diagnostics.ControlPipeStatus.Capabilities.ReadsRenderTargets)} |");
        builder.AppendLine($"| Copies render targets | {YesNo(diagnostics.ControlPipeStatus.Capabilities.CopiesRenderTargets)} |");
        builder.AppendLine($"| Registers shader resources | {YesNo(diagnostics.ControlPipeStatus.Capabilities.RegistersShaderResources)} |");
        builder.AppendLine($"| Moves realtime shader values | {YesNo(diagnostics.ControlPipeStatus.Capabilities.MovesRealtimeShaderValues)} |");
        AppendDalapadResourceCatalog(builder, "Control-Pipe Resource Catalog", diagnostics.ControlPipeStatus.ResourceCatalog);
        AppendDalapadDebugVisualization(builder, "Status-File Debug Visualization", diagnostics.IpcStatus.DebugVisualization);
        AppendDalapadDebugVisualization(builder, "Control-Pipe Debug Visualization", diagnostics.ControlPipeStatus.DebugVisualization);
        AppendDalapadResourceShapeProbe(builder, diagnostics.ResourceShapeProbe);
        builder.AppendLine();
        AppendLines(builder, "Dalapad Health Check Next Steps", BuildDalapadHealthNextSteps(diagnostics));

        builder.AppendLine();
        builder.AppendLine("### IPC Endpoints");
        builder.AppendLine();
        builder.AppendLine("| Endpoint | Kind | Direction | Address | Purpose | Safety Boundary |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var endpoint in diagnostics.IpcEndpoints)
        {
            builder.AppendLine($"| {EscapeTable(endpoint.Name)} | {EscapeTable(endpoint.Kind)} | {EscapeTable(endpoint.Direction)} | {EscapeTable(endpoint.Address)} | {EscapeTable(endpoint.Purpose)} | {EscapeTable(endpoint.SafetyBoundary)} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Realtime Adaptation Groundwork");
        builder.AppendLine();
        builder.AppendLine("| Channel | Direction | Payload | Priority | Safety Boundary |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var channel in diagnostics.RealtimeChannels)
        {
            builder.AppendLine($"| {EscapeTable(channel.Name)} | {EscapeTable(channel.Direction)} | {EscapeTable(channel.Payload)} | {EscapeTable(channel.Priority)} | {EscapeTable(channel.SafetyBoundary)} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Diagnostic Routes");
        builder.AppendLine();
        builder.AppendLine("| Route | Producer | Output | Purpose |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var route in diagnostics.DiagnosticRoutes)
        {
            builder.AppendLine($"| {EscapeTable(route.Name)} | {EscapeTable(route.Producer)} | {EscapeTable(route.Output)} | {EscapeTable(route.Purpose)} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Implementation Options");
        builder.AppendLine();
        builder.AppendLine("| Option | Feasibility | Risk | Summary |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var option in diagnostics.ImplementationOptions)
        {
            builder.AppendLine($"| {EscapeTable(option.Name)} | {EscapeTable(option.Feasibility)} | {EscapeTable(option.Risk)} | {EscapeTable(option.Summary)} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Next Backend Steps");
        builder.AppendLine();
        builder.AppendLine("| Stage | Goal | Safety Boundary | Exit Criteria |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var step in diagnostics.NextBackendSteps)
        {
            builder.AppendLine($"| {EscapeTable(step.Stage)} | {EscapeTable(step.Goal)} | {EscapeTable(step.SafetyBoundary)} | {EscapeTable(step.ExitCriteria)} |");
        }

        builder.AppendLine();
        AppendLines(builder, "Dalapad Safety Notes", diagnostics.Notes);
        AppendLines(builder, "Dalapad Removal Notes", diagnostics.RemovalNotes);
    }

    private static void AppendDalapadResourceCatalog(StringBuilder builder, string title, IReadOnlyList<DalapadResourceCatalogEntry> resources)
    {
        builder.AppendLine();
        builder.AppendLine($"### {title}");
        builder.AppendLine();
        if (resources.Count == 0)
        {
            builder.AppendLine("- No resource catalog rows reported.");
            return;
        }

        builder.AppendLine("| Resource | Available | Source | Size | Format | Freshness | Confidence | Safety | Metadata Source | Reason |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var resource in resources)
        {
            builder.AppendLine($"| {EscapeTable(resource.Name)} | {YesNo(resource.Available)} | {EscapeTable(resource.Source)} | {resource.Width}x{resource.Height} | {EscapeTable(resource.Format)} | {EscapeTable(resource.Freshness)} | {resource.Confidence:0.###} | {EscapeTable(resource.SafetyState)} | {EscapeTable(resource.MetadataSource)} | {EscapeTable(resource.Reason)} |");
        }
    }

    private static void AppendDalapadResourceShapeProbe(StringBuilder builder, DalapadResourceShapeProbe probe)
    {
        builder.AppendLine();
        builder.AppendLine("### Developer Resource Shape Probe");
        builder.AppendLine();
        builder.AppendLine($"- Enabled: {YesNo(probe.Enabled)}");
        builder.AppendLine($"- Attempted: {YesNo(probe.Attempted)}");
        builder.AppendLine($"- Instance invoked: {YesNo(probe.InstanceInvoked)}");
        builder.AppendLine($"- Status: {EscapeTable(probe.Status)}");
        builder.AppendLine($"- Summary: {EscapeTable(probe.Summary)}");
        if (probe.Timestamp != DateTimeOffset.MinValue)
        {
            builder.AppendLine($"- Probe timestamp: {probe.Timestamp:O}");
        }

        builder.AppendLine($"- Warnings: {(probe.Warnings.Count == 0 ? "none" : string.Join("; ", probe.Warnings.Select(EscapeTable)))}");
        if (probe.Resources.Count == 0)
        {
            builder.AppendLine("- No shape rows reported.");
            return;
        }

        builder.AppendLine();
        builder.AppendLine("| Resource | Candidate | Pointer | Pointer Fingerprint | Source | Size | Format | Freshness | Confidence | Safety | Metadata Source | Reason |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var resource in probe.Resources)
        {
            builder.AppendLine($"| {EscapeTable(resource.Name)} | {YesNo(resource.CandidateFound)} | {YesNo(resource.PointerObserved)} | {EscapeTable(resource.PointerFingerprint)} | {EscapeTable(resource.Source)} | {resource.Width}x{resource.Height} | {EscapeTable(resource.Format)} | {EscapeTable(resource.Freshness)} | {resource.Confidence:0.###} | {EscapeTable(resource.SafetyState)} | {EscapeTable(resource.MetadataSource)} | {EscapeTable(resource.Reason)} |");
        }
    }

    private static void AppendDalapadDebugVisualization(StringBuilder builder, string title, DalapadDebugVisualizationStatus debug)
    {
        builder.AppendLine();
        builder.AppendLine($"### {title}");
        builder.AppendLine();
        builder.AppendLine($"- Version: {FormatOptionalInlineCode(debug.Version)}");
        builder.AppendLine($"- Enabled: {YesNo(debug.Enabled)}");
        builder.AppendLine($"- Status: {EscapeTable(debug.Status)}");
        builder.AppendLine($"- Source: {FormatOptionalInlineCode(debug.Source)}");
        builder.AppendLine($"- Shader: {FormatOptionalInlineCode(debug.Shader)}");
        builder.AppendLine($"- Texture name: {FormatOptionalInlineCode(debug.TextureName)}");
        builder.AppendLine($"- Shader texture found: {YesNo(debug.ShaderTextureFound)}");
        builder.AppendLine($"- Synthetic texture uploaded: {YesNo(debug.SyntheticTextureUploaded)}");
        builder.AppendLine($"- Uses synthetic texture: {YesNo(debug.UsesSyntheticTexture)}");
        builder.AppendLine($"- Size: {debug.Width}x{debug.Height}");
        builder.AppendLine($"- Frame counter: {debug.FrameCounter}");
        builder.AppendLine($"- Frame age: {debug.FrameAge}");
        builder.AppendLine($"- Observed render candidates: {debug.ObservedSourceCount}");
        builder.AppendLine($"- Copied render candidates: {debug.CopiedSourceCount}");
        builder.AppendLine($"- Reads render targets: {YesNo(debug.ReadsRenderTargets)}");
        builder.AppendLine($"- Copies render targets: {YesNo(debug.CopiesRenderTargets)}");
        builder.AppendLine($"- Registers game resources: {YesNo(debug.RegistersGameResources)}");
        builder.AppendLine($"- Reason: {EscapeTable(debug.Reason)}");

        if (debug.PinnedCandidates.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("| Pinned Candidate | Semantic | Source | Source Semantic | Hint | Observed | Copied | Size | Confidence |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (var candidate in debug.PinnedCandidates)
            {
                builder.AppendLine($"| {EscapeTable(candidate.Label)} | {FormatOptionalInlineCode(candidate.Semantic)} | {EscapeTable(candidate.Source)} | {FormatOptionalInlineCode(candidate.SourceSemantic)} | {EscapeTable(candidate.ClassificationHint)} | {YesNo(candidate.Observed)} | {YesNo(candidate.Copied)} | {candidate.Width}x{candidate.Height} | {candidate.Confidence:0.###} |");
            }
        }
    }

    private static IReadOnlyList<string> BuildDalapadHealthNextSteps(DalapadDiagnostics diagnostics)
    {
        if (!diagnostics.IpcStatus.StatusFileFound)
        {
            return new[] { "Load the separate Dalapad addon prototype and confirm it writes dalapad-status.json." };
        }

        if (!diagnostics.IpcStatus.ContractCompatible)
        {
            return new[] { "Rebuild the addon against the current 0.1-ipc-diagnostic status-file contract." };
        }

        if (!diagnostics.ControlPipeStatus.PipeListening)
        {
            return new[] { "Rebuild and reload the addon with the diagnostic control pipe enabled." };
        }

        if (!diagnostics.ControlPipeStatus.ResponseReceived)
        {
            return new[] { "Check the addon pipe worker; the pipe accepted a connection but did not return capability JSON." };
        }

        if (!diagnostics.ControlPipeStatus.ContractCompatible)
        {
            return new[] { "Update the addon and plugin to the same Dalapad.Control.v1 pipe contract." };
        }

        if (diagnostics.ControlPipeStatus.Capabilities.ReadsRenderTargets
            || diagnostics.ControlPipeStatus.Capabilities.CopiesRenderTargets
            || diagnostics.ControlPipeStatus.Capabilities.RegistersShaderResources
            || diagnostics.ControlPipeStatus.Capabilities.MovesRealtimeShaderValues)
        {
            return new[] { "Unexpected advanced capabilities are enabled. Keep this build diagnostic-only until resource validation is explicitly started." };
        }

        if (!diagnostics.ControlPipeStatus.Capabilities.SupportsResourceCatalog)
        {
            return new[]
            {
                "Status-file IPC and control-pipe capability negotiation are healthy.",
                "Next safe step is a metadata-only resource catalog; do not send texture handles or shader values yet."
            };
        }

        if (diagnostics.IpcStatus.ResourceCatalog.Count == 0 && diagnostics.ControlPipeStatus.ResourceCatalog.Count == 0)
        {
            return new[] { "Resource catalog capability is enabled, but no catalog rows were reported. Check the addon status payload and QueryStatus response." };
        }

        if (!diagnostics.ResourceShapeProbe.Attempted)
        {
            return new[]
            {
                "Status-file IPC, control-pipe capability negotiation, and metadata-only resource catalog are healthy.",
                "Enable and run the developer-only resource shape probe next; keep texture copies, shader resources, IPC handles, and FrameData disabled."
            };
        }

        if (diagnostics.ResourceShapeProbe.Resources.All(resource => !resource.PointerObserved))
        {
            return new[]
            {
                "Developer resource shape probe ran without observing candidate pointers.",
                "Capture a debug bundle in-game and inspect the shape probe warnings before attempting any native bridge work."
            };
        }

        if (!diagnostics.ControlPipeStatus.DebugVisualization.SyntheticTextureUploaded)
        {
            return new[]
            {
                "Resource shape observation is healthy, but the synthetic debug visualization bridge has not uploaded yet.",
                "Install/reload Dalapad_Debug.fx and confirm the addon reports Dalapad_DebugTexture found before judging render-layer copy behavior."
            };
        }

        if (diagnostics.ControlPipeStatus.DebugVisualization.CopiedSourceCount == 0)
        {
            return new[]
            {
                "Synthetic debug visualization is healthy, but no render-layer candidate has been copied into Dalapad_Debug.fx yet.",
                diagnostics.ControlPipeStatus.DebugVisualization.ObservedSourceCount > 0
                    ? "The addon is observing render-target candidates; check format support, effect-begin callbacks, and copy barriers."
                    : "The addon has not observed render-target candidates yet; test while actively in a rendered scene with Dalapad_Debug.fx enabled."
            };
        }

        return new[]
        {
            "Status-file IPC, control-pipe capability negotiation, metadata-only resource catalog, developer resource shape probe, and debug render-layer copies are healthy enough for repeated observation.",
            "Next safe step is lifecycle testing across login, zone change, resolution change, and reload; keep FrameData and generated preset behavior disabled."
        };
    }

    private static void AppendTagStackDiagnostics(StringBuilder builder, TagStackDiagnostics diagnostics)
    {
        builder.AppendLine("## Scene Tags And Stack Diagnostics");
        builder.AppendLine();
        builder.AppendLine($"- Territory: {diagnostics.TerritoryName} ({diagnostics.TerritoryId})");
        builder.AppendLine($"- Weather: {diagnostics.WeatherName} ({(diagnostics.WeatherId.HasValue ? diagnostics.WeatherId.Value.ToString() : "unknown")})");
        builder.AppendLine($"- Weather key: {diagnostics.WeatherKey}");
        builder.AppendLine($"- Active weather tags: {(diagnostics.ActiveWeatherTags.Count == 0 ? "none" : string.Join(", ", diagnostics.ActiveWeatherTags))}");
        builder.AppendLine($"- Biome key: {diagnostics.BiomeKey}");
        builder.AppendLine($"- Biome confidence: {diagnostics.BiomeConfidence:P0}");
        builder.AppendLine($"- Biome reason: {diagnostics.BiomeReason}");
        builder.AppendLine($"- Secondary tags: {FormatPlainList(diagnostics.SecondaryTags)}");
        builder.AppendLine($"- Night tags: {FormatPlainList(diagnostics.SecondaryTags.Concat(diagnostics.ArtDirectionTags).Where(IsNightTag).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())}");
        builder.AppendLine($"- Day tags: {FormatPlainList(diagnostics.SecondaryTags.Concat(diagnostics.ArtDirectionTags).Where(IsDayTag).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())}");
        builder.AppendLine($"- Mood tags: {(diagnostics.MoodTags.Count == 0 ? "none" : string.Join(", ", diagnostics.MoodTags))}");
        builder.AppendLine($"- Material tags: {FormatPlainList(diagnostics.MaterialTags)}");
        builder.AppendLine($"- Area/context tags: {FormatPlainList(diagnostics.AreaContextTags)}");
        builder.AppendLine($"- Gameplay-state tags: {FormatPlainList(diagnostics.GameplayStateTags)}");
        builder.AppendLine($"- Art-direction tags: {FormatPlainList(diagnostics.ArtDirectionTags)}");
        builder.AppendLine($"- Area key: {diagnostics.AreaKey}");
        builder.AppendLine($"- Combat: {diagnostics.InCombat}");
        builder.AppendLine($"- Duty: {diagnostics.InDuty}");
        builder.AppendLine($"- Cutscene: {diagnostics.InCutscene}");
        builder.AppendLine($"- GPose: {diagnostics.InGpose}");
        builder.AppendLine($"- Active tags: {(diagnostics.ActiveTags.Count == 0 ? "none" : string.Join(", ", diagnostics.ActiveTags))}");
        builder.AppendLine();
        builder.AppendLine("### Scene Intent");
        builder.AppendLine();
        builder.AppendLine($"- Readability: {diagnostics.Intent.Readability:0.###}");
        builder.AppendLine($"- Atmosphere: {diagnostics.Intent.Atmosphere:0.###}");
        builder.AppendLine($"- Highlight protection: {diagnostics.Intent.HighlightProtection:0.###}");
        builder.AppendLine($"- Shadow protection: {diagnostics.Intent.ShadowProtection:0.###}");
        builder.AppendLine($"- Haze: {diagnostics.Intent.Haze:0.###}");
        builder.AppendLine($"- Wetness: {diagnostics.Intent.Wetness:0.###}");
        builder.AppendLine($"- Cold: {diagnostics.Intent.Cold:0.###}");
        builder.AppendLine($"- Heat: {diagnostics.Intent.Heat:0.###}");
        builder.AppendLine($"- Magic glow: {diagnostics.Intent.MagicGlow:0.###}");
        builder.AppendLine($"- Neon glow: {diagnostics.Intent.NeonGlow:0.###}");
        builder.AppendLine($"- Foliage density: {diagnostics.Intent.FoliageDensity:0.###}");
        builder.AppendLine($"- Industrial hardness: {diagnostics.Intent.IndustrialHardness:0.###}");
        builder.AppendLine($"- Cosmic mood: {diagnostics.Intent.CosmicMood:0.###}");
        builder.AppendLine($"- Night: {diagnostics.Intent.Night:0.###}");
        builder.AppendLine($"- Moonlight: {diagnostics.Intent.Moonlight:0.###}");
        builder.AppendLine($"- Artificial light: {diagnostics.Intent.ArtificialLight:0.###}");
        builder.AppendLine($"- Ambient darkness: {diagnostics.Intent.AmbientDarkness:0.###}");
        builder.AppendLine($"- Night atmosphere: {diagnostics.Intent.NightAtmosphere:0.###}");
        builder.AppendLine($"- Daylight: {diagnostics.Intent.Daylight:0.###}");
        builder.AppendLine($"- Sunlight: {diagnostics.Intent.Sunlight:0.###}");
        builder.AppendLine($"- Open sky light: {diagnostics.Intent.OpenSkyLight:0.###}");
        builder.AppendLine($"- Surface heat: {diagnostics.Intent.SurfaceHeat:0.###}");
        builder.AppendLine($"- Day atmosphere: {diagnostics.Intent.DayAtmosphere:0.###}");
        builder.AppendLine($"- Day reflection: {diagnostics.Intent.DayReflection:0.###}");
        builder.AppendLine($"- Day highlight pressure: {diagnostics.Intent.DayHighlightPressure:0.###}");
        builder.AppendLine($"- Combat pressure: {diagnostics.Intent.CombatPressure:0.###}");
        builder.AppendLine($"- Cinematic permission: {diagnostics.Intent.CinematicPermission:0.###}");
        builder.AppendLine();
        builder.AppendLine("### Scene Intent Contributions");
        builder.AppendLine();
        if (diagnostics.IntentContributions.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var group in diagnostics.IntentContributions.GroupBy(CategorizeSceneIntentContribution).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"- {group.Key}");
                foreach (var contribution in group)
                {
                    builder.AppendLine($"  - {contribution.Intent}: {contribution.Amount:+0.###;-0.###;0} from {contribution.Source} - {contribution.Reason}");
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("### Stack Budget Contributions");
        builder.AppendLine();
        if (diagnostics.Contributions.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var contribution in diagnostics.Contributions)
        {
            var flags = contribution.BudgetApplied ? "budget applied" : "informational";
            if (contribution.Dampened)
            {
                flags += ", dampened";
            }

            builder.AppendLine($"- {contribution.Variable}: {contribution.Source} {contribution.Change} | {contribution.Before:0.###} -> {contribution.After:0.###} | {flags}");
        }

        builder.AppendLine();
    }

    private static void AppendColorFamilyComparison(StringBuilder builder, ImageAnalysisResult currentImage, ImageAnalysisResult masterStyle, VisualProfile profile)
    {
        builder.AppendLine("## Master Style Color-Family Comparison");
        builder.AppendLine();
        if (!currentImage.Available || !masterStyle.Available)
        {
            builder.AppendLine("- Current or master image analysis is unavailable.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Family | Current H/S/L/C | Master H/S/L/C | Generated H/S/L |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var row in ColorFamilyComparisonRows.Build(currentImage, masterStyle, profile))
        {
            builder.AppendLine($"| {row.Family} | {row.Current.Hue:0.###} / {row.Current.Saturation:0.###} / {row.Current.Luminance:0.###} / {row.Current.Confidence:0.##} | {row.Master.Hue:0.###} / {row.Master.Saturation:0.###} / {row.Master.Luminance:0.###} / {row.Master.Confidence:0.##} | {row.Adjustment.Hue:+0.000;-0.000;0.000} / {row.Adjustment.Saturation:+0.000;-0.000;0.000} / {row.Adjustment.Luminance:+0.000;-0.000;0.000} |");
        }

        builder.AppendLine();
    }

    private static void AppendColorFamilyAdjustments(StringBuilder builder, VisualProfile profile)
    {
        builder.AppendLine("## Master Style Color Family Adjustments");
        builder.AppendLine();
        var strongest = profile.StrongestColorFamilyAdjustments(8);
        if (strongest.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var adjustment in strongest)
        {
            builder.AppendLine($"- {adjustment.Family}: hue={adjustment.Hue:+0.000;-0.000;0.000} | saturation={adjustment.Saturation:+0.000;-0.000;0.000} | luminance={adjustment.Luminance:+0.000;-0.000;0.000} | confidence={adjustment.Confidence:0.00}");
        }

        builder.AppendLine();
    }

    private static void AppendMasterStyleDiagnostics(StringBuilder builder, Configuration configuration, MasterStyleDiagnostics diagnostics)
    {
        builder.AppendLine("## Master Style Diagnostics");
        builder.AppendLine();
        builder.AppendLine($"- Enabled: {diagnostics.Enabled}");
        builder.AppendLine($"- Master available: {diagnostics.MasterAvailable}");
        builder.AppendLine($"- Current image available: {diagnostics.CurrentImageAvailable}");
        builder.AppendLine($"- Master image count: {diagnostics.MasterImageCount}");
        builder.AppendLine($"- Mode: {diagnostics.MasterMode}");
        builder.AppendLine($"- Raw strength: {diagnostics.RawStrength}%");
        builder.AppendLine($"- Effective strength: {diagnostics.EffectiveStrength:0.###}");
        builder.AppendLine($"- Scene similarity multiplier: {diagnostics.SceneSimilarityMultiplier:0.###}");
        builder.AppendLine($"- Compatibility multiplier: {diagnostics.CompatibilityModeMultiplier:0.###}");
        builder.AppendLine($"- Tonal match strength: {configuration.MasterTonalMatchStrength:0.###}");
        builder.AppendLine($"- Tonal color strength: {configuration.MasterTonalColorStrength:0.###}");
        builder.AppendLine($"- Color-family strength: {configuration.MasterColorFamilyStrength:0.###}");
        builder.AppendLine($"- Max hue/saturation/luminance shifts: {configuration.MasterMaxHueShift:0.###} / {configuration.MasterMaxSaturationShift:0.###} / {configuration.MasterMaxLuminanceShift:0.###}");
        builder.AppendLine($"- Status: {diagnostics.Status}");
        builder.AppendLine();
        builder.AppendLine("### Tonal Deltas");
        builder.AppendLine();
        builder.AppendLine($"- Exposure: {diagnostics.TonalDeltas.Exposure:+0.000;-0.000;0.000}");
        builder.AppendLine($"- ShadowLift: {diagnostics.TonalDeltas.ShadowLift:+0.000;-0.000;0.000}");
        builder.AppendLine($"- BlackPoint: {diagnostics.TonalDeltas.BlackPoint:+0.000;-0.000;0.000}");
        builder.AppendLine($"- WhitePoint: {diagnostics.TonalDeltas.WhitePoint:+0.000;-0.000;0.000}");
        builder.AppendLine($"- HighlightRecovery: {diagnostics.TonalDeltas.HighlightRecovery:+0.000;-0.000;0.000}");
        builder.AppendLine($"- Contrast: {diagnostics.TonalDeltas.Contrast:+0.000;-0.000;0.000}");
        builder.AppendLine($"- MidtoneContrast: {diagnostics.TonalDeltas.MidtoneContrast:+0.000;-0.000;0.000}");
        builder.AppendLine();
    }

    private static void AppendTechniqueList(StringBuilder builder, string title, IReadOnlyList<PresetTechnique> techniques)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        if (techniques.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var technique in DeduplicateTechniques(techniques))
        {
            builder.AppendLine($"- {PresetAnalyzer.FormatTechnique(technique)} | activation={PresetAnalyzer.FormatActivationState(technique.ActivationState)} | role={PresetAnalyzer.FormatRole(technique.Role)} | risk={PresetAnalyzer.FormatRisk(technique.Risk)} | support={technique.SupportLevel}");
        }

        builder.AppendLine();
    }

    private static void AppendAuthorities(StringBuilder builder, IReadOnlyList<EffectAuthority> authorities, GenerationAuthorityPolicy authorityPolicy)
    {
        builder.AppendLine("## Effect Authorities");
        builder.AppendLine();
        if (authorities.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var authority in authorities)
        {
            var rolePolicy = authorityPolicy.Roles.FirstOrDefault(role => role.Role == authority.Role);
            var dampening = rolePolicy is { DampensSecondaries: true }
                ? $"secondary dampening={rolePolicy.SecondaryAdjustmentStrength:0.##}x"
                : "secondary dampening=not applied";

            builder.AppendLine($"- {PresetAnalyzer.FormatRole(authority.Role)}: primary={authority.PrimaryShader} | {dampening}");
            foreach (var secondary in authority.SecondaryShaders.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"  - secondary={secondary}");
            }
            foreach (var warned in authority.SuppressedOrWarnedShaders.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"  - warned={warned}");
            }
        }

        builder.AppendLine();
    }

    private static void AppendRolePolicies(StringBuilder builder, PresetCompatibilityMode mode, GenerationAuthorityPolicy authorityPolicy)
    {
        builder.AppendLine("## Selected Role Policies");
        builder.AppendLine();
        foreach (var policy in CompatibilityRolePolicies.All)
        {
            var rolePolicy = authorityPolicy.Roles.FirstOrDefault(role => role.Role == policy.Role);
            var activeStrength = rolePolicy?.SecondaryAdjustmentStrength ?? policy.GetSecondaryStrength(mode);
            var multiple = policy.MultipleActiveEffectsAllowed ? "multiple allowed" : "single primary preferred";
            var unsupported = policy.UnsupportedActiveEffectsWarnOnly ? "unsupported warn-only" : "unsupported escalates risk";
            var sanitize = policy.GameplaySanitizeMayReduce ? $"gameplay sanitize may reduce secondaries ({activeStrength:0.##}x)" : "gameplay sanitize does not reduce";
            var gpose = policy.GposePreserveLeavesAlone ? "GPose preserve leaves alone" : "GPose preserve may still adapt";
            builder.AppendLine($"- {PresetAnalyzer.FormatRole(policy.Role)}: {multiple}; {unsupported}; {sanitize}; {gpose}");
        }

        builder.AppendLine();
    }

    private static void AppendLines(StringBuilder builder, string title, IReadOnlyList<string> lines)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        if (lines.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var line in lines.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- {line}");
        }

        builder.AppendLine();
    }

    private static void AppendShaderSupport(StringBuilder builder, ShaderSupportScan shaderSupport)
    {
        builder.AppendLine("## Shader Support Scan");
        builder.AppendLine();
        builder.AppendLine(shaderSupport.Message);
        builder.AppendLine();
        foreach (var item in shaderSupport.Items)
        {
            builder.AppendLine($"- {item.Section} / {item.Key} | {item.ReasonCategory} | {PresetAnalyzer.FormatActivationState(item.ActivationState)}");
        }

        if (shaderSupport.Items.Count == 0)
        {
            builder.AppendLine("- None");
        }

        builder.AppendLine();
    }

    private static void AppendMappingValidation(StringBuilder builder, Configuration configuration, PresetAnalysisResult analysis, ShaderSupportScan shaderSupport, string effectiveBasePresetPath)
    {
        builder.AppendLine("## Mapping Validation");
        builder.AppendLine();
        var activeTechniques = analysis.Techniques
            .Where(technique => technique.ActivationState == TechniqueActivationState.Active)
            .GroupBy(technique => technique.Section, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(technique => technique.Section, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (activeTechniques.Length == 0)
        {
            builder.AppendLine("- No active shaders were confirmed from the Techniques= line.");
            builder.AppendLine();
            return;
        }

        var definitions = new ShaderVariableMapper().CreateDefinitions(configuration)
            .Where(definition => !string.IsNullOrWhiteSpace(definition.Section))
            .GroupBy(definition => definition.Section!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(definition => definition.Key).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var presetKeys = ReadPresetKeys(effectiveBasePresetPath, out var keyScanWarning);
        if (!string.IsNullOrWhiteSpace(keyScanWarning))
        {
            builder.AppendLine($"- {keyScanWarning}");
            builder.AppendLine();
        }

        var mappedBySection = shaderSupport.Items
            .GroupBy(item => item.Section, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Key).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var martyControlled = activeTechniques.Count(technique => IsMartyOrImmerse(technique.Section) && technique.SupportLevel is SupportLevel.FullyControlled or SupportLevel.PartiallyControlled);
        var freeControlled = activeTechniques.Count(technique => !IsMartyOrImmerse(technique.Section) && technique.SupportLevel is SupportLevel.FullyControlled or SupportLevel.PartiallyControlled);
        var detectedOnly = activeTechniques.Count(technique => technique.SupportLevel == SupportLevel.DetectedOnly);
        var highRiskGposeOnly = activeTechniques.Count(technique => technique.Risk is EffectRisk.High or EffectRisk.GPoseOnly);
        var confidenceScore = CalculateMappingConfidence(activeTechniques);

        builder.AppendLine($"- iMMERSE/Marty controlled sections: {martyControlled}");
        builder.AppendLine($"- Free shader controlled sections: {freeControlled}");
        builder.AppendLine($"- Detected-only active sections: {detectedOnly}");
        builder.AppendLine($"- High-risk/GPose-only active sections: {highRiskGposeOnly}");
        builder.AppendLine($"- Mapping confidence: {FormatMappingConfidence(confidenceScore)} ({confidenceScore:P0})");
        builder.AppendLine();
        builder.AppendLine("| Active shader | Support | Role | Risk | Mapped keys found | Known keys missing | Present keys unmapped | Status |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (var technique in activeTechniques)
        {
            definitions.TryGetValue(technique.Section, out var knownKeys);
            mappedBySection.TryGetValue(technique.Section, out var mappedKeys);
            presetKeys.TryGetValue(technique.Section, out var presentKeys);
            knownKeys ??= Array.Empty<string>();
            mappedKeys ??= Array.Empty<string>();
            presentKeys ??= Array.Empty<string>();
            var missingKnown = knownKeys.Except(presentKeys, StringComparer.OrdinalIgnoreCase).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray();
            var unmappedPresent = presentKeys.Except(knownKeys, StringComparer.OrdinalIgnoreCase).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray();

            builder.AppendLine($"| {EscapeTable(technique.Section)} | {technique.SupportLevel} | {PresetAnalyzer.FormatRole(technique.Role)} | {PresetAnalyzer.FormatRisk(technique.Risk)} | {FormatKeyList(mappedKeys)} | {FormatKeyList(missingKnown)} | {FormatKeyList(unmappedPresent)} | {FormatSupportStatus(technique)} |");
        }

        builder.AppendLine();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadPresetKeys(string path, out string? warning)
    {
        warning = null;
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            warning = "Preset key scan unavailable: base preset path not found.";
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        }

        var currentSection = string.Empty;
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
            {
                currentSection = trimmed[1..^1];
                if (!result.ContainsKey(currentSection))
                {
                    result[currentSection] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0 || string.IsNullOrWhiteSpace(currentSection))
            {
                continue;
            }

            result[currentSection].Add(trimmed[..separatorIndex].Trim());
        }

        return result.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static GeneratedPresetSectionValues ReadGeneratedPresetSectionValues(string? presetPath, string sectionNeedle)
    {
        if (string.IsNullOrWhiteSpace(presetPath) || string.IsNullOrWhiteSpace(sectionNeedle))
        {
            return new GeneratedPresetSectionValues(false, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), "generated preset path or section name empty");
        }

        if (!File.Exists(presetPath))
        {
            return new GeneratedPresetSectionValues(false, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), "generated preset file not found");
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sectionPresent = false;
        var inTargetSection = false;
        try
        {
            foreach (var line in File.ReadLines(presetPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
                {
                    var section = trimmed[1..^1];
                    inTargetSection = section.Contains(sectionNeedle, StringComparison.OrdinalIgnoreCase);
                    sectionPresent |= inTargetSection;
                    continue;
                }

                if (!inTargetSection)
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                values[trimmed[..separatorIndex].Trim()] = trimmed[(separatorIndex + 1)..].Trim();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new GeneratedPresetSectionValues(sectionPresent, values, $"generated preset scan failed: {ex.Message}");
        }

        return new GeneratedPresetSectionValues(sectionPresent, values, string.Empty);
    }

    private static GeneratedPresetVariableValue[] ReadGeneratedPresetVariables(string? presetPath, IReadOnlyCollection<string> keys)
    {
        if (string.IsNullOrWhiteSpace(presetPath) || keys.Count == 0 || !File.Exists(presetPath))
        {
            return [];
        }

        var wanted = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        var values = new List<GeneratedPresetVariableValue>();
        var section = string.Empty;
        try
        {
            foreach (var line in File.ReadLines(presetPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
                {
                    section = trimmed[1..^1];
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0 || string.IsNullOrWhiteSpace(section))
                {
                    continue;
                }

                var key = trimmed[..separatorIndex].Trim();
                if (!wanted.Contains(key))
                {
                    continue;
                }

                values.Add(new GeneratedPresetVariableValue(section, key, trimmed[(separatorIndex + 1)..].Trim()));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return [];
        }

        return values.ToArray();
    }

    private static IReadOnlySet<string> Set(params string[] values)
    {
        return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadShaderSource(string fileName)
    {
        return ReadShaderSource(null, fileName);
    }

    private static string ReadShaderSource(Configuration? configuration, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        foreach (var root in CandidateSourceRoots())
        {
            string path;
            try
            {
                path = Path.Combine(root, "shaders", fileName);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                continue;
            }

            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                return File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return string.Empty;
            }
        }

        if (configuration is not null)
        {
            foreach (var root in FindReShadeShaderPaths(configuration))
            {
                string path;
                try
                {
                    path = Path.Combine(root, fileName);
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
                {
                    continue;
                }

                try
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    return File.ReadAllText(path);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    return string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> CandidateSourceRoots()
    {
        foreach (var root in CandidateParentDirectories(TryGetCurrentDirectory()))
        {
            yield return root;
        }

        foreach (var root in CandidateParentDirectories(AppContext.BaseDirectory))
        {
            yield return root;
        }
    }

    private static string? TryGetCurrentDirectory()
    {
        try
        {
            return Directory.GetCurrentDirectory();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static IEnumerable<string> CandidateParentDirectories(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            yield break;
        }

        DirectoryInfo? directory;
        try
        {
            directory = new DirectoryInfo(root);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            yield break;
        }

        for (; directory is not null; directory = directory.Parent)
        {
            if (!string.IsNullOrWhiteSpace(directory.FullName))
            {
                yield return directory.FullName;
            }
        }
    }

    private static bool UsesSharedMaterialResolver(string source)
    {
        return source.Contains("Dalashade_ResolveMaterials", StringComparison.Ordinal);
    }

    private static bool HasLocalMaterialLogic(string source, bool usesSharedResolver)
    {
        if (usesSharedResolver)
        {
            return false;
        }

        return usesSharedResolver
               && (source.Contains("materialWater", StringComparison.Ordinal)
                   || source.Contains("materialFoliage", StringComparison.Ordinal)
                   || source.Contains("materialSnow", StringComparison.Ordinal)
                   || source.Contains("materialSky", StringComparison.Ordinal)
                   || source.Contains("waterReceiver", StringComparison.Ordinal)
                   || source.Contains("emissive", StringComparison.OrdinalIgnoreCase));
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string FormatParityStatus(bool declares, bool writes, bool expected, bool used, bool usesSharedResolver)
    {
        if (!expected)
        {
            return "Not applicable by design";
        }

        if (writes && declares && used && usesSharedResolver)
        {
            return "Full parity";
        }

        if (declares && !writes)
        {
            return "Declared but not written";
        }

        if (writes && !declares)
        {
            return "Written but not declared";
        }

        if (declares && writes && !used)
        {
            return "Declared/written but not consumed";
        }

        if (used && !usesSharedResolver)
        {
            return "Local-only detection";
        }

        return "Missing";
    }

    private static string FormatCoverageCell(bool declares, bool writes, bool expected, bool used)
    {
        if (!expected)
        {
            return "N/A";
        }

        if (writes && declares && used)
        {
            return "W+D+U";
        }

        if (writes && declares)
        {
            return "W+D";
        }

        if (declares && !writes)
        {
            return "D only";
        }

        if (writes && !declares)
        {
            return "W only";
        }

        return "Missing";
    }

    private static string ShortShaderName(string technique)
    {
        const string prefix = "Dalashade_";
        return technique.StartsWith(prefix, StringComparison.Ordinal) ? technique[prefix.Length..] : technique;
    }

    private static bool IsDepthAssistUniform(string uniform)
    {
        return string.Equals(uniform, "Dalashade_EnableDepthAssist", StringComparison.Ordinal)
               || string.Equals(uniform, "Dalashade_DepthAssistStrength", StringComparison.Ordinal)
               || string.Equals(uniform, "Dalashade_DepthAssistConfidenceFloor", StringComparison.Ordinal)
               || string.Equals(uniform, "Dalashade_DepthConfidenceFloor", StringComparison.Ordinal);
    }

    private static string FormatSourceScanStatus(bool detected, bool sourceAvailable)
    {
        return detected ? "yes" : sourceAvailable ? "no" : "unknown";
    }

    private static string FormatNormalFieldProductionConsumption(Configuration configuration, string fileName)
    {
        var source = ReadShaderSource(configuration, fileName);
        if (string.IsNullOrWhiteSpace(source))
        {
            return "unknown (source unavailable)";
        }

        var includesNormalField = source.Contains("Dalashade_NormalField.fxh", StringComparison.Ordinal);
        var resolvesNormalField = source.Contains("Dalashade_ResolveNormalField", StringComparison.Ordinal);
        var usesDepthNormal = source.Contains("Dalashade_GetDepthNormal", StringComparison.Ordinal);
        var usesDetailNormal = source.Contains("Dalashade_GetImageGradientNormal", StringComparison.Ordinal);
        return includesNormalField || resolvesNormalField || usesDepthNormal || usesDetailNormal
            ? $"yes (include={YesNo(includesNormalField)}, resolve={YesNo(resolvesNormalField)}, depth={YesNo(usesDepthNormal)}, detail={YesNo(usesDetailNormal)})"
            : "no, not yet";
    }

    private static string FormatFrameDataProductionConsumption(Configuration configuration, string fileName)
    {
        var source = ReadShaderSource(configuration, fileName);
        if (string.IsNullOrWhiteSpace(source))
        {
            return "unknown (source unavailable)";
        }

        var includesFrameData = source.Contains("Dalashade_FrameData.fxh", StringComparison.Ordinal);
        var resolvesFrameBase = source.Contains("Dalashade_ResolveFrameBaseData", StringComparison.Ordinal);
        var resolvesFrameSurface = source.Contains("Dalashade_ResolveFrameSurfaceData", StringComparison.Ordinal);
        var usesInlineResolvers = source.Contains("Dalashade_ResolveMaterials", StringComparison.Ordinal)
                                  || source.Contains("Dalashade_ResolveWater", StringComparison.Ordinal)
                                  || source.Contains("Dalashade_ResolveSafety", StringComparison.Ordinal)
                                  || source.Contains("Dalashade_ResolveNormalField", StringComparison.Ordinal);
        return includesFrameData || resolvesFrameBase || resolvesFrameSurface
            ? $"migrated (include={YesNo(includesFrameData)}, base={YesNo(resolvesFrameBase)}, surface={YesNo(resolvesFrameSurface)}, inline-resolvers={YesNo(usesInlineResolvers)})"
            : $"not migrated (inline-resolvers={YesNo(usesInlineResolvers)})";
    }

    private static string[] GetFrameDataProductionConsumerFiles(Configuration configuration)
    {
        return ProductionShaderFiles
            .Where(fileName => IsFrameDataProductionConsumer(configuration, fileName))
            .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] GetFrameDataProductionConsumerLabels(Configuration configuration)
    {
        return GetFrameDataProductionConsumerFiles(configuration)
            .Select(FormatFrameDataConsumerLabel)
            .ToArray();
    }

    private static bool IsFrameDataProductionConsumer(Configuration configuration, string fileName)
    {
        var source = ReadShaderSource(configuration, fileName);
        return !string.IsNullOrWhiteSpace(source)
               && (source.Contains("Dalashade_FrameData.fxh", StringComparison.Ordinal)
                   || source.Contains("Dalashade_ResolveFrameBaseData", StringComparison.Ordinal)
                   || source.Contains("Dalashade_ResolveFrameSurfaceData", StringComparison.Ordinal));
    }

    private static string FormatFrameDataConsumerLabel(string fileName)
    {
        var label = fileName.StartsWith("Dalashade_", StringComparison.OrdinalIgnoreCase)
            ? fileName["Dalashade_".Length..]
            : fileName;
        return label.EndsWith(".fx", StringComparison.OrdinalIgnoreCase)
            ? label[..^3]
            : label;
    }

    private static string FormatGeneratedPresetValue(GeneratedPresetSectionValues values, string key)
    {
        return values.Values.TryGetValue(key, out var value)
            ? $"`{value}`"
            : "missing";
    }

    private static string FormatDepthAssistSections(IReadOnlyList<ChangedShaderVariable> written)
    {
        var sections = written
            .Select(change => change.Section)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(section => section, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return sections.Length == 0 ? "none" : string.Join(", ", sections.Select(section => $"`{section}`"));
    }

    private static string FormatGeneratedPresetVariableSections(IReadOnlyList<GeneratedPresetVariableValue> values)
    {
        var sections = values
            .Select(value => value.Section)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(section => section, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return sections.Length == 0 ? "none" : string.Join(", ", sections.Select(section => $"`{section}`"));
    }

    private static string YesNo(bool value) => value ? "yes" : "no";

    private static string FormatOptionalInlineCode(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "none"
            : $"`{value.Replace("`", "'", StringComparison.Ordinal)}`";
    }

    private static float CalculateMappingConfidence(IReadOnlyList<PresetTechnique> activeTechniques)
    {
        if (activeTechniques.Count == 0)
        {
            return 0f;
        }

        var score = activeTechniques.Sum(technique => technique.SupportLevel switch
        {
            SupportLevel.FullyControlled => 1.00f,
            SupportLevel.PartiallyControlled => 0.65f,
            SupportLevel.DetectedOnly => 0.30f,
            _ => 0f
        });
        score -= activeTechniques.Count(technique => technique.Risk is EffectRisk.High or EffectRisk.GPoseOnly) * 0.10f;
        return MathF.Min(1f, MathF.Max(0f, score / activeTechniques.Count));
    }

    private static string FormatMappingConfidence(float score)
    {
        return score switch
        {
            >= 0.85f => "Excellent",
            >= 0.65f => "Good",
            >= 0.45f => "Mixed",
            >= 0.25f => "Risky",
            _ => "Poor"
        };
    }

    private static string FormatSupportStatus(PresetTechnique technique)
    {
        return technique.SupportLevel switch
        {
            SupportLevel.FullyControlled => "strict-supported",
            SupportLevel.PartiallyControlled => "strict-supported partial",
            SupportLevel.DetectedOnly => "detected-only",
            _ => "unsupported"
        };
    }

    private static bool IsMartyOrImmerse(string section)
    {
        return section.Contains("marty", StringComparison.OrdinalIgnoreCase)
               || section.Contains("immerse", StringComparison.OrdinalIgnoreCase)
               || section.Contains("martysmods", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatKeyList(IReadOnlyList<string> keys)
    {
        if (keys.Count == 0)
        {
            return "none";
        }

        var display = keys.Take(12).Select(EscapeTable).ToArray();
        var suffix = keys.Count > display.Length ? $" +{keys.Count - display.Length} more" : string.Empty;
        return string.Join(", ", display) + suffix;
    }

    private static string FormatInlineList(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", values.Select(value => $"`{value}`"));
    }

    private static string FormatPlainList(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? "none" : string.Join(", ", values);
    }

    private static string FormatTagOverrideMap(IReadOnlyDictionary<string, IReadOnlyList<string>> values)
    {
        if (values.Count == 0)
        {
            return "none";
        }

        return string.Join(
            "; ",
            values
                .Where(pair => pair.Value.Count > 0)
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}: {FormatPlainList(pair.Value)}"));
    }

    private static bool IsNightTag(string tag)
    {
        return tag.Contains("Night", StringComparison.OrdinalIgnoreCase)
               || string.Equals(tag, "night", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDayTag(string tag)
    {
        return tag.Contains("Day", StringComparison.OrdinalIgnoreCase)
               || string.Equals(tag, "day", StringComparison.OrdinalIgnoreCase);
    }

    private static int ClampInt(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    private static string FormatSceneGIDebugMode(int mode)
    {
        return ClampInt(mode, 0, 18) switch
        {
            0 => "Off / normal output",
            1 => "AO only",
            2 => "Bounce only",
            3 => "Night light pooling only",
            4 => "Material influence",
            5 => "Sky rejection",
            6 => "Skin protection",
            7 => "Final GI influence",
            8 => "Depth-normal confidence",
            9 => "Emissive source mask",
            10 => "Bounce receiver mask",
            11 => "Adaptive limits / safety clamp",
            12 => "Layered AO breakdown",
            13 => "Clamp pressure",
            14 => "SSGI diffuse gather",
            15 => "Material bounce lanes",
            16 => "Sky-safe receivers",
            17 => "Emissive pooling lanes",
            18 => "Dalapad normal assist",
            _ => "Unknown"
        };
    }

    private static string FormatSceneGIDebugOutputMode(int mode)
    {
        return ClampInt(mode, 0, 4) switch
        {
            0 => "Full replacement diagnostic",
            1 => "Alpha overlay over original scene",
            2 => "Side-by-side split",
            3 => "Contribution over black",
            4 => "Amplified difference view",
            _ => "Unknown"
        };
    }

    private static string FormatSurfaceReflectionDebugMode(int mode)
    {
        return ClampInt(mode, 0, 14) switch
        {
            0 => "Normal output",
            1 => "WaterPlane sheen mask",
            2 => "SpecularGlint mask",
            3 => "Wet reflection mask",
            4 => "Aether/neon reflection mask",
            5 => "Sky rejection",
            6 => "Skin protection",
            7 => "Final reflection influence",
            8 => "Contribution over black",
            9 => "Reflection source mask",
            10 => "Reflection receiver mask",
            11 => "Water projected reflection",
            12 => "Wet hard projected reflection",
            13 => "Metal/aether projected reflection",
            14 => "Pseudo SSR contribution",
            _ => "Unknown"
        };
    }

    private static string FormatContactToneDebugMode(int mode)
    {
        return ClampInt(mode, 0, 6) switch
        {
            0 => "Off / normal output",
            1 => "Contact mask",
            2 => "Depth edge component",
            3 => "Surface/normal edge component",
            4 => "Receiver/safety mask",
            5 => "Suppression mask",
            6 => "Final contribution",
            _ => "Unknown"
        };
    }

    private static string FormatSurfaceReflectionDebugOutputMode(int mode)
    {
        return ClampInt(mode, 0, 4) switch
        {
            0 => "Full replacement diagnostic",
            1 => "Alpha overlay over original scene",
            2 => "Side-by-side split",
            3 => "Contribution over black",
            4 => "Amplified difference view",
            _ => "Unknown"
        };
    }

    private static PresetTechnique? FindMaterialDebugTechnique(PresetAnalysisResult analysis)
    {
        return FindFirstPartyTechnique(analysis, "Dalashade_MaterialDebug");
    }

    private static PresetTechnique? FindFirstPartyTechnique(PresetAnalysisResult analysis, string shaderKey)
    {
        return analysis.Techniques
            .Where(technique => ContainsShaderKey(technique, shaderKey))
            .OrderByDescending(technique => technique.ActivationState == TechniqueActivationState.Active)
            .ThenByDescending(technique => technique.ActivationState == TechniqueActivationState.Inactive)
            .FirstOrDefault();
    }

    private static string FormatFirstPartyCustomShaderStatus(PresetAnalysisResult analysis)
    {
        var statuses = new[]
        {
            FormatFirstPartyShaderStatus(analysis, "WeatherAtmosphere", "Dalashade_WeatherAtmosphere"),
            FormatFirstPartyShaderStatus(analysis, "AdaptiveGrade", "Dalashade_AdaptiveGrade"),
            FormatFirstPartyShaderStatus(analysis, "AtmosphereBloom", "Dalashade_AtmosphereBloom"),
            FormatFirstPartyShaderStatus(analysis, "SmartSharpen", "Dalashade_SmartSharpen"),
            FormatFirstPartyShaderStatus(analysis, "MaterialDebug", "Dalashade_MaterialDebug"),
            FormatFirstPartyShaderStatus(analysis, "NormalDebug", "Dalashade_NormalDebug"),
            FormatFirstPartyShaderStatus(analysis, "SceneGI", "Dalashade_SceneGI"),
            FormatFirstPartyShaderStatus(analysis, "ContactTone", "Dalashade_ContactTone"),
            FormatFirstPartyShaderStatus(analysis, "SurfaceReflection", "Dalashade_SurfaceReflection")
        };

        return string.Join("; ", statuses);
    }

    private static string FormatCustomShaderVariableClasses(IEnumerable<string> keys)
    {
        var uniqueKeys = keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (uniqueKeys.Length == 0)
        {
            return "none";
        }

        var sceneIntent = uniqueKeys.Count(CustomShaderVariableMapper.IsKnownSceneIntentVariable);
        var materialIntent = uniqueKeys.Count(CustomShaderVariableMapper.IsKnownMaterialIntentVariable);
        var normalField = uniqueKeys.Count(CustomShaderVariableMapper.IsKnownNormalFieldVariable);
        var shaderOwned = uniqueKeys.Count(CustomShaderVariableMapper.IsKnownShaderOwnedVariable);
        var otherKnown = uniqueKeys.Length - sceneIntent - materialIntent - normalField - shaderOwned;
        return $"SceneIntent={sceneIntent}, MaterialIntent={materialIntent}, NormalField={normalField}, shader-owned={shaderOwned}, other-known={Math.Max(0, otherKnown)}";
    }

    private static string FormatFirstPartyShaderStatus(PresetAnalysisResult analysis, string label, string shaderKey)
    {
        var matches = analysis.Techniques
            .Where(technique => ContainsShaderKey(technique, shaderKey))
            .ToArray();
        if (matches.Length == 0)
        {
            return $"{label}=absent";
        }

        var active = matches.Any(technique => technique.ActivationState == TechniqueActivationState.Active);
        var listed = string.Join("/", matches.Select(technique => PresetAnalyzer.FormatActivationState(technique.ActivationState)).Distinct(StringComparer.OrdinalIgnoreCase));
        return $"{label}={(active ? "active" : "listed")}({listed})";
    }

    private static bool ContainsShaderKey(PresetTechnique technique, string shaderKey)
    {
        return technique.TechniqueName.Contains(shaderKey, StringComparison.OrdinalIgnoreCase)
               || technique.ShaderFile.Contains(shaderKey, StringComparison.OrdinalIgnoreCase)
               || technique.Section.Contains(shaderKey, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDominantSceneDrivers(TagStackDiagnostics diagnostics, IReadOnlySet<string> intentNames)
    {
        var selected = diagnostics.IntentContributions
            .Where(contribution => contribution.Amount > 0f && intentNames.Contains(contribution.Intent))
            .OrderByDescending(contribution => Math.Abs(contribution.Amount))
            .Take(5)
            .Select(contribution => $"{contribution.Intent} {contribution.Amount:+0.##;-0.##;0} from {contribution.Source}")
            .ToArray();

        return selected.Length == 0 ? "none" : string.Join(", ", selected);
    }

    private static string FormatDominantMaterialDrivers(
        Configuration configuration,
        TagStackDiagnostics diagnostics,
        ImageAnalysisResult currentImage,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence,
        IReadOnlySet<string> channelNames)
    {
        if (!configuration.EnableMaterialIntent)
        {
            return "MaterialIntent disabled";
        }

        var profile = MaterialProfileBuilder.Build(diagnostics, currentImage, configuration.ScreenshotAnalysisStrength);
        var screenshotEvidenceContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(configuration, diagnostics, screenshotMaterialEvidence.Evidence);
        var intent = MaterialIntentBuilder.Build(diagnostics, currentImage, profile, screenshotStrength: configuration.ScreenshotAnalysisStrength, screenshotMaterialEvidenceContributions: screenshotEvidenceContributions).WithStrength(configuration.MaterialIntentStrength);
        var selected = MaterialIntent.ChannelNames
            .Where(channelNames.Contains)
            .Select(channel => new
            {
                Channel = channel,
                Value = intent.ValueFor(channel)
            })
            .Where(item => item.Value > 0.01f)
            .OrderByDescending(item => item.Value)
            .Take(6)
            .Select(item => $"{item.Channel} {item.Value:0.###}")
            .ToArray();

        return selected.Length == 0 ? "none" : string.Join(", ", selected);
    }

    private static string FormatChangedKeys(PresetWriteResult writeResult, string reasonCategory, string shaderKey)
    {
        var keys = writeResult.Changes
            .Where(change => string.Equals(change.ReasonCategory, reasonCategory, StringComparison.OrdinalIgnoreCase)
                             && change.Section.Contains(shaderKey, StringComparison.OrdinalIgnoreCase))
            .Select(change => change.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return keys.Length == 0 ? "none" : string.Join(", ", keys.Select(key => $"`{key}`"));
    }

    private static string FormatMaterialUniformSections(IReadOnlyList<ChangedShaderVariable> writtenUniforms)
    {
        var sections = writtenUniforms
            .Select(change => change.Section)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(section => section, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return sections.Length == 0 ? "none" : string.Join(", ", sections.Select(section => $"`{section}`"));
    }

    private static string FormatInjectedDepthAssistVariables(PresetWriteResult writeResult)
    {
        var variables = writeResult.CustomShaderInjection.Variables
            .Where(variable => variable.Contains("DepthAssist", StringComparison.OrdinalIgnoreCase)
                               || variable.Contains("EnableDepthAssist", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(variable => variable, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return variables.Length == 0 ? "none" : string.Join(", ", variables.Select(variable => $"`{variable}`"));
    }

    private static string FormatMaterialContributions(IReadOnlyList<MaterialIntentContribution> contributions, string channel, bool positive)
    {
        var selected = contributions
            .Where(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal)
                                   && (positive ? contribution.Amount > 0f : contribution.Amount < 0f))
            .OrderByDescending(contribution => Math.Abs(contribution.Amount))
            .Take(3)
            .Select(contribution => $"{EscapeTable(contribution.Source)} {contribution.Amount:+0.##;-0.##;0}: {EscapeTable(contribution.Reason)}")
            .ToArray();

        return selected.Length == 0 ? "none" : string.Join("<br>", selected);
    }

    private static string FormatMaterialNonProfileEvidence(IReadOnlyList<MaterialIntentContribution> contributions, string channel)
    {
        var selected = contributions
            .Where(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal)
                                   && contribution.Amount > 0f
                                   && !contribution.Source.StartsWith("MaterialProfile", StringComparison.OrdinalIgnoreCase)
                                   && !contribution.Source.StartsWith("Screenshot evidence:", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(contribution => Math.Abs(contribution.Amount))
            .Take(3)
            .Select(contribution => $"{EscapeTable(contribution.Source)} +{contribution.Amount:0.##}: {EscapeTable(contribution.Reason)}")
            .ToArray();

        return selected.Length == 0 ? "none" : string.Join("<br>", selected);
    }

    private static string FormatMaterialScreenshotEvidence(IReadOnlyList<MaterialIntentContribution> contributions, string channel)
    {
        var selected = contributions
            .Where(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal)
                                   && contribution.Amount > 0f
                                   && contribution.Source.StartsWith("Screenshot evidence:", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(contribution => Math.Abs(contribution.Amount))
            .Take(3)
            .Select(contribution => $"{EscapeTable(contribution.Source)} +{contribution.Amount:0.##}: {EscapeTable(contribution.Reason)}")
            .ToArray();

        return selected.Length == 0 ? "none" : string.Join("<br>", selected);
    }

    private static string FormatCalibrationShaderTargets(MaterialCalibrationChannelDiagnostic channel)
    {
        var keys = channel.ShaderKeys.Count == 0 ? "none" : string.Join(", ", channel.ShaderKeys.Select(key => $"`{EscapeTable(key)}`"));
        var sections = channel.ShaderSections.Count == 0 ? "no detected section" : string.Join(", ", channel.ShaderSections.Select(section => $"`{EscapeTable(section)}`"));
        return $"{keys}<br>{sections}";
    }

    private static string FormatCalibrationWarnings(IReadOnlyList<MaterialCalibrationWarning> warnings)
    {
        if (warnings.Count == 0)
        {
            return "none";
        }

        return string.Join("<br>", warnings.Select(warning => $"{EscapeTable(warning.Severity)}: {EscapeTable(warning.Message)}"));
    }

    private static string FormatMaterialProfilePriors(MaterialProfile profile)
    {
        var selected = profile.TopPriors(5)
            .Select(item => $"{EscapeTable(item.Channel)} {item.Value:0.###}")
            .ToArray();

        return selected.Length == 0 ? "none" : string.Join(", ", selected);
    }

    private static string FormatMaterialProfileContributions(IReadOnlyList<MaterialProfileContribution> contributions, string channel, bool positive)
    {
        var selected = contributions
            .Where(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal)
                                   && (positive ? contribution.Amount > 0f : contribution.Amount < 0f))
            .OrderByDescending(contribution => Math.Abs(contribution.Amount))
            .Take(3)
            .Select(contribution => $"{EscapeTable(contribution.Source)} {contribution.Amount:+0.##;-0.##;0}: {EscapeTable(contribution.Reason)}")
            .ToArray();

        return selected.Length == 0 ? "none" : string.Join("<br>", selected);
    }

    private static string CategorizeSceneIntentContribution(SceneIntentContribution contribution)
    {
        var source = contribution.Source;
        if (source.Contains("weather", StringComparison.OrdinalIgnoreCase) || source is "Rain" or "Storm")
        {
            return "weather tags";
        }

        if (source.Contains("Biome", StringComparison.OrdinalIgnoreCase)
            || source.Contains("mood", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Jungle", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Art direction", StringComparison.OrdinalIgnoreCase))
        {
            return "biome, material, and art-direction tags";
        }

        if (source.Contains("Night", StringComparison.OrdinalIgnoreCase) || source.Contains("Day", StringComparison.OrdinalIgnoreCase) || source.Contains("Dawn", StringComparison.OrdinalIgnoreCase))
        {
            return "time-of-day tags";
        }

        if (source.Contains("Combat", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Duty", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Raid", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Dungeon", StringComparison.OrdinalIgnoreCase)
            || source.Contains("GPose", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Cutscene", StringComparison.OrdinalIgnoreCase))
        {
            return "area and gameplay-state tags";
        }

        if (source.Contains("Screenshot", StringComparison.OrdinalIgnoreCase))
        {
            return "screenshot analysis";
        }

        if (source.Contains("Performance", StringComparison.OrdinalIgnoreCase))
        {
            return "performance budget";
        }

        if (source.Contains("style", StringComparison.OrdinalIgnoreCase))
        {
            return "target style";
        }

        return "baseline";
    }

    private static string EscapeTable(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static void AppendChangedVariables(StringBuilder builder, PresetWriteResult writeResult)
    {
        builder.AppendLine("## Changed Variables");
        builder.AppendLine();
        builder.AppendLine(writeResult.Message);
        builder.AppendLine();
        if (writeResult.Changes.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var change in writeResult.Changes)
        {
            var activation = PresetAnalyzer.FormatActivationState(change.ActivationState);
            var clamp = change.HitMin ? "min clamp" : change.HitMax ? "max clamp" : "not clamped";
            var dampening = change.AuthorityAdjustmentStrength < 0.999f ? $" | authority dampening={change.AuthorityAdjustmentStrength:0.##}x" : string.Empty;
            var warning = string.IsNullOrWhiteSpace(change.Warning) ? string.Empty : $" | warning={change.Warning}";
            builder.AppendLine($"- {change.Section} / {change.Key}: {change.OldValue} -> {change.NewValue} | {change.ReasonCategory} | {activation} | {clamp}{dampening}{warning}");
        }

        builder.AppendLine();
    }

    private static void AppendSanitizeActions(StringBuilder builder, PresetWriteResult writeResult)
    {
        builder.AppendLine("## Sanitize Actions");
        builder.AppendLine();
        if (writeResult.SanitizeActions.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var action in writeResult.SanitizeActions)
        {
            builder.AppendLine($"- {action.Section} / {action.Key}: {action.OldValue} -> {action.NewValue} | {action.ActionType} | {PresetAnalyzer.FormatRole(action.Role)} | {action.Reason} | {PresetAnalyzer.FormatActivationState(action.ActivationState)}");
        }

        builder.AppendLine();
    }

    private static string MakeSafeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var safe = new string(fileName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "UnknownPreset" : safe;
    }

    private static string ResolveSafeDirectory(string? candidate, string? fallbackDirectory, string childFolderName)
    {
        var root = !string.IsNullOrWhiteSpace(candidate)
            ? candidate.Trim()
            : fallbackDirectory;

        if (string.IsNullOrWhiteSpace(root))
        {
            root = GetDefaultPluginConfigDirectory();
        }

        var resolved = Path.GetFullPath(root);
        if (string.IsNullOrWhiteSpace(candidate) && !string.IsNullOrWhiteSpace(childFolderName))
        {
            resolved = Path.Combine(resolved, MakeSafeFileName(childFolderName));
        }

        return resolved;
    }

    private static string ResolveReportExportPath(string? candidatePath, string fallbackDirectory, string defaultFileName)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            var reportsDirectory = ResolveSafeDirectory(null, fallbackDirectory, "Reports");
            return Path.Combine(reportsDirectory, MakeSafeFileName(defaultFileName));
        }

        var trimmed = candidatePath.Trim();
        var rooted = Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed)
            : Path.GetFullPath(Path.Combine(ResolveSafeDirectory(fallbackDirectory, GetDefaultPluginConfigDirectory(), string.Empty), trimmed));

        var extension = Path.GetExtension(rooted);
        if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            return rooted;
        }

        return Path.Combine(rooted, MakeSafeFileName(defaultFileName));
    }

    private static string GetDefaultPluginConfigDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(appData)
            ? Path.Combine(Environment.CurrentDirectory, "Dalashade")
            : Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Dalashade");
    }

    private static IEnumerable<string> FindReShadeShaderPaths(Configuration configuration)
    {
        var reShadeIniPath = FindReShadeIni(configuration);
        if (string.IsNullOrWhiteSpace(reShadeIniPath) || !File.Exists(reShadeIniPath))
        {
            yield break;
        }

        var iniDirectory = Path.GetDirectoryName(reShadeIniPath) ?? string.Empty;
        foreach (var line in File.ReadLines(reShadeIniPath))
        {
            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            if (!key.Contains("EffectSearchPaths", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("EffectSearchPath", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var rawPath in line[(separatorIndex + 1)..].Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var normalized = rawPath.Trim('"');
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                var candidate = Path.IsPathRooted(normalized) ? normalized : Path.Combine(iniDirectory, normalized);
                var fullPath = TryGetFullPath(candidate);
                if (!string.IsNullOrWhiteSpace(fullPath) && Directory.Exists(fullPath))
                {
                    yield return fullPath;
                }
            }
        }

        var defaultShaders = Path.Combine(iniDirectory, "reshade-shaders", "Shaders");
        var fullDefaultShaders = TryGetFullPath(defaultShaders);
        if (!string.IsNullOrWhiteSpace(fullDefaultShaders) && Directory.Exists(fullDefaultShaders))
        {
            yield return fullDefaultShaders;
        }
    }

    private static string? FindReShadeIni(Configuration configuration)
    {
        var configuredReShadeIni = TryGetFullPath(configuration.ReShadeIniPath);
        if (!string.IsNullOrWhiteSpace(configuredReShadeIni) && File.Exists(configuredReShadeIni))
        {
            return configuredReShadeIni;
        }

        foreach (var candidate in new[] { configuration.GeneratedPresetPath, configuration.BasePresetPath })
        {
            var fullCandidate = TryGetFullPath(candidate);
            if (string.IsNullOrWhiteSpace(fullCandidate))
            {
                continue;
            }

            var directory = Path.GetDirectoryName(fullCandidate);
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            var reShadeIni = Path.Combine(directory, "ReShade.ini");
            if (File.Exists(reShadeIni))
            {
                return reShadeIni;
            }
        }

        return null;
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static IEnumerable<PresetTechnique> DeduplicateTechniques(IEnumerable<PresetTechnique> techniques)
    {
        return techniques
            .GroupBy(technique => $"{technique.TechniqueName}\u001f{technique.ShaderFile}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
    }
}
