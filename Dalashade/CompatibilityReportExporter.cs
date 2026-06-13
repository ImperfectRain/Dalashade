using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dalashade;

public sealed record CompatibilityReportExportResult(bool Success, string Message, string Path)
{
    public static CompatibilityReportExportResult Skipped(string message) => new(false, message, string.Empty);
}

public sealed class CompatibilityReportExporter
{
    public const string MaterialIntentDiagnosticsTableHeader = "| Channel | Profile prior | Non-profile evidence | Final value | Top suppressions |";
    private sealed record MaterialParityChannel(string Uniform, string Label);
    private sealed record MaterialParityShader(string FileName, string Technique, string Role, IReadOnlySet<string> WrittenUniforms, IReadOnlySet<string> ExpectedUniforms, IReadOnlySet<string> DebugUniforms);

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
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set()),
        new(
            "Dalashade_AtmosphereBloom.fx",
            "Dalashade_AtmosphereBloom",
            "Selective material glow/bloom eligibility",
            Set("Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog"),
            Set("Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog")),
        new(
            "Dalashade_WeatherAtmosphere.fx",
            "Dalashade_WeatherAtmosphere",
            "Weather/air/haze material shaping",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialSkyCloudFog", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialSkyCloudFog"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialSkyCloudFog")),
        new(
            "Dalashade_SmartSharpen.fx",
            "Dalashade_SmartSharpen",
            "Material-aware sharpening suppression",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSnowIce", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_EnableDepthAssist", "Dalashade_DepthAssistStrength", "Dalashade_DepthAssistConfidenceFloor", "Dalashade_DepthConfidenceFloor"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterSpecular", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSnowIce", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSnowIce", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection")),
        new(
            "Dalashade_MaterialDebug.fx",
            "Dalashade_MaterialDebug",
            "Truth viewer for shared material masks",
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray()),
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray()),
            Set(SharedMaterialParityChannels.Select(channel => channel.Uniform).Where(uniform => !string.Equals(uniform, "Dalashade_RainWetContext", StringComparison.Ordinal)).ToArray())),
        new(
            "Dalashade_SceneGI.fx",
            "Dalashade_SceneGI",
            "Screen-space AO, bounce, and light pooling",
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness"),
            Set("Dalashade_MaterialFoliage", "Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialStoneRuins", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection", "Dalashade_MaterialVoidDarkness")),
        new(
            "Dalashade_SurfaceReflection.fx",
            "Dalashade_SurfaceReflection",
            "Water resolver, reflection receivers, and glints",
            Set("Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection"),
            Set("Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection"),
            Set("Dalashade_MaterialWaterPlane", "Dalashade_MaterialSpecularGlint", "Dalashade_WaterContext", "Dalashade_CoastalContext", "Dalashade_OpenOceanContext", "Dalashade_ShallowWaterContext", "Dalashade_WetSurfaceContext", "Dalashade_MaterialSandDust", "Dalashade_MaterialSnowIce", "Dalashade_MaterialMetalIndustrial", "Dalashade_MaterialCrystalAether", "Dalashade_MaterialNeonGlass", "Dalashade_MaterialFireLavaHeat", "Dalashade_MaterialSkyCloudFog", "Dalashade_MaterialSkinProtection"))
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
        ImageAnalysisResult currentImage,
        ImageAnalysisResult masterStyle,
        PresetWriteResult writeResult,
        string effectiveBasePresetPath,
        string outputDirectory)
    {
        try
        {
            if (!analysis.Success)
            {
                return CompatibilityReportExportResult.Skipped("Preset analysis has not succeeded yet.");
            }

            Directory.CreateDirectory(outputDirectory);
            var presetName = string.IsNullOrWhiteSpace(effectiveBasePresetPath)
                ? "UnknownPreset"
                : Path.GetFileNameWithoutExtension(effectiveBasePresetPath);
            var safePresetName = MakeSafeFileName(presetName);
            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var path = Path.Combine(outputDirectory, $"{safePresetName}-compatibility-{timestamp}.md");

            File.WriteAllText(path, BuildReport(configuration, analysis, shaderSupport, profile, masterDiagnostics, tagStackDiagnostics, currentImage, masterStyle, writeResult, effectiveBasePresetPath), Encoding.UTF8);
            return new CompatibilityReportExportResult(true, $"Compatibility report exported: {path}", path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return CompatibilityReportExportResult.Skipped($"Compatibility report export failed: {ex.Message}");
        }
    }

    private static string BuildReport(
        Configuration configuration,
        PresetAnalysisResult analysis,
        ShaderSupportScan shaderSupport,
        VisualProfile profile,
        MasterStyleDiagnostics masterDiagnostics,
        TagStackDiagnostics tagStackDiagnostics,
        ImageAnalysisResult currentImage,
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
        AppendTagStackDiagnostics(builder, tagStackDiagnostics);
        AppendMaterialIntentDiagnostics(builder, configuration, tagStackDiagnostics, currentImage, writeResult);
        AppendMasterStyleDiagnostics(builder, configuration, masterDiagnostics);
        AppendColorFamilyAdjustments(builder, profile);
        AppendColorFamilyComparison(builder, currentImage, masterStyle, profile);
        AppendMappingValidation(builder, configuration, analysis, shaderSupport, effectiveBasePresetPath);
        AppendCustomShaderDiagnostics(builder, configuration, analysis, shaderSupport, writeResult, tagStackDiagnostics, currentImage);
        AppendMaterialParityAudit(builder);
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

    private static void AppendCustomShaderDiagnostics(
        StringBuilder builder,
        Configuration configuration,
        PresetAnalysisResult analysis,
        ShaderSupportScan shaderSupport,
        PresetWriteResult writeResult,
        TagStackDiagnostics tagStackDiagnostics,
        ImageAnalysisResult currentImage)
    {
        builder.AppendLine("## Dalashade Custom Shader Diagnostics");
        builder.AppendLine();
        var diagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(configuration, shaderSupport, writeResult, analysis);
        builder.AppendLine($"- Custom shader support: {(diagnostics.SupportEnabled ? "enabled" : "disabled")}");
        builder.AppendLine($"- Auto-inject known sections into generated preset: {(diagnostics.AutoInjectionEnabled ? "enabled" : "disabled")}");
        builder.AppendLine($"- Generated preset only injection: {(diagnostics.GeneratedPresetOnlyInjection ? "yes" : "no")}");
        builder.AppendLine($"- Generated-preset-only sections injected: {(diagnostics.SectionInjected ? "yes" : "no")}");
        builder.AppendLine($"- Generated-preset-only variables injected: {(diagnostics.VariablesInjected ? "yes" : "no")}");
        builder.AppendLine("- Technique activation: manual; generated-preset injection does not append custom shaders to `Techniques=`.");
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
        AppendSceneGIDiagnostics(builder, configuration, analysis, writeResult, tagStackDiagnostics, currentImage);
        AppendSurfaceReflectionDiagnostics(builder, configuration, analysis, writeResult, tagStackDiagnostics, currentImage);
        builder.AppendLine("- Material debug controls: shader-owned in ReShade UI; Dalashade does not write debug mode, overlay mode, opacity, or strength.");
        builder.AppendLine($"- First-party custom shader status: {FormatFirstPartyCustomShaderStatus(analysis)}");
        builder.AppendLine("- Variable ownership: SceneIntent variables are Dalashade-controlled, MaterialIntent channel uniforms are Dalashade-controlled only when material shader mapping is enabled, SceneGI and SurfaceReflection debug controls can be written by their separate generated-variable toggles, and other shader-owned controls are recognized/injected but not actively written by Dalashade.");
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

    private static void AppendMaterialParityAudit(StringBuilder builder)
    {
        var sourceByShader = MaterialParityShaders.ToDictionary(
            shader => shader.FileName,
            shader => ReadShaderSource(shader.FileName),
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
            var hasLocalMaterialLogic = sourceAvailable && HasLocalMaterialLogic(source, usesSharedResolver);
            var debugExposes = sourceAvailable
                               && (shader.Technique.Equals("Dalashade_MaterialDebug", StringComparison.OrdinalIgnoreCase)
                                   || source.Contains("Dalashade_MaterialDebugMode", StringComparison.Ordinal)
                                   || source.Contains("DebugMode", StringComparison.Ordinal));

            builder.AppendLine($"### {shader.Technique}");
            builder.AppendLine();
            builder.AppendLine($"- Shader file: `{shader.FileName}`");
            builder.AppendLine($"- Role: {shader.Role}");
            builder.AppendLine($"- Source available for declaration/use scan: {(sourceAvailable ? "yes" : "no")}");
            builder.AppendLine($"- Uses shared resolver from `Dalashade_MaterialMasks.fxh`: {(usesSharedResolver ? "yes" : sourceAvailable ? "no" : "unknown")}");
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
        ImageAnalysisResult currentImage)
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
        builder.AppendLine($"- SceneGI debug mode {sceneGIDebugWriteLabel} value: {ClampInt(configuration.DalashadeSceneGIDebugMode, 0, 12)} ({FormatSceneGIDebugMode(configuration.DalashadeSceneGIDebugMode)}).");
        builder.AppendLine($"- SceneGI debug output mode {sceneGIDebugWriteLabel} value: {ClampInt(configuration.DalashadeSceneGIDebugOutputMode, 0, 4)} ({FormatSceneGIDebugOutputMode(configuration.DalashadeSceneGIDebugOutputMode)}).");
        builder.AppendLine($"- SceneGI debug opacity {sceneGIDebugWriteLabel} value: {Math.Clamp(configuration.DalashadeSceneGIDebugOpacity, 0f, 1f):0.###}.");
        builder.AppendLine($"- SceneGI debug boost {sceneGIDebugWriteLabel} value: {Math.Clamp(configuration.DalashadeSceneGIDebugBoost, 0.25f, 8f):0.###}. Debug boost affects diagnostic masks only, not normal GI output.");
        builder.AppendLine($"- Dominant SceneIntent drivers: {FormatDominantSceneDrivers(tagStackDiagnostics, SceneGIIntentNames)}");
        builder.AppendLine($"- Dominant MaterialIntent drivers: {FormatDominantMaterialDrivers(configuration, tagStackDiagnostics, currentImage, SceneGIMaterialNames)}");
        builder.AppendLine($"- Generated SceneGI variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.SceneGIReasonCategory, "Dalashade_SceneGI")}");
        builder.AppendLine($"- Generated SceneGI material variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.MaterialReasonCategory, "Dalashade_SceneGI")}");
        builder.AppendLine("- Technique activation remains manual; Dalashade never appends `Dalashade_SceneGI` to `Techniques=`.");
    }

    private static void AppendSurfaceReflectionDiagnostics(
        StringBuilder builder,
        Configuration configuration,
        PresetAnalysisResult analysis,
        PresetWriteResult writeResult,
        TagStackDiagnostics tagStackDiagnostics,
        ImageAnalysisResult currentImage)
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
        builder.AppendLine($"- SurfaceReflection debug mode {writeLabel} value: {ClampInt(configuration.DalashadeSurfaceReflectionDebugMode, 0, 10)} ({FormatSurfaceReflectionDebugMode(configuration.DalashadeSurfaceReflectionDebugMode)}).");
        builder.AppendLine($"- SurfaceReflection debug output mode {writeLabel} value: 0 ({FormatSurfaceReflectionDebugOutputMode(0)}).");
        builder.AppendLine($"- SurfaceReflection debug opacity {writeLabel} value: {Math.Clamp(configuration.DalashadeSurfaceReflectionDebugOpacity, 0f, 1f):0.###}.");
        var materialProfile = MaterialProfileBuilder.Build(tagStackDiagnostics, currentImage);
        var materialIntent = configuration.EnableMaterialIntent
            ? MaterialIntentBuilder.Build(tagStackDiagnostics, currentImage, materialProfile).WithStrength(configuration.MaterialIntentStrength)
            : MaterialIntent.Neutral;
        builder.AppendLine($"- Water resolver context: WaterContext={materialIntent.WaterSpecular:0.###}, CoastalContext={materialIntent.WaterSpecular:0.###}, OpenOceanContext={materialIntent.WaterSpecular * 0.85f:0.###}, ShallowWaterContext={Math.Max(materialIntent.WaterSpecular * 0.72f, Math.Min(materialIntent.WaterSpecular, materialIntent.SandDust) * 0.20f):0.###}, WetSurfaceContext={tagStackDiagnostics.Intent.Wetness:0.###}.");
        builder.AppendLine("- Water resolver note: scene context values are generated-preset priors; shader-side `Dalashade_ResolveWater` still performs per-pixel water classification and rejects sky, sand, skin, and isolated glints.");
        builder.AppendLine($"- Dominant MaterialIntent drivers: {FormatDominantMaterialDrivers(configuration, tagStackDiagnostics, currentImage, SurfaceReflectionMaterialNames)}");
        builder.AppendLine($"- Generated SurfaceReflection variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.SurfaceReflectionReasonCategory, "Dalashade_SurfaceReflection")}");
        builder.AppendLine($"- Generated SurfaceReflection material variables written: {FormatChangedKeys(writeResult, CustomShaderVariableMapper.MaterialReasonCategory, "Dalashade_SurfaceReflection")}");
        builder.AppendLine("- Technique activation remains manual; Dalashade never appends `Dalashade_SurfaceReflection` to `Techniques=`.");
    }

    private static void AppendMaterialIntentDiagnostics(StringBuilder builder, Configuration configuration, TagStackDiagnostics tagStackDiagnostics, ImageAnalysisResult currentImage, PresetWriteResult writeResult)
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

        var profile = MaterialProfileBuilder.Build(tagStackDiagnostics, currentImage);
        var intent = MaterialIntentBuilder.Build(tagStackDiagnostics, currentImage, profile).WithStrength(configuration.MaterialIntentStrength);
        var writtenUniforms = writeResult.Changes
            .Where(change => string.Equals(change.ReasonCategory, CustomShaderVariableMapper.MaterialReasonCategory, StringComparison.OrdinalIgnoreCase))
            .OrderBy(change => change.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        builder.AppendLine("- MaterialIntent is an inferred material-likelihood diagnostic layer. It uses tags, territory/weather text, gameplay context, screenshot metrics, and existing SceneIntent context; it does not detect true engine material IDs.");
        builder.AppendLine("- MaterialProfile is a scene-level plausibility layer between SceneTags/MaterialIntent and shader-side pixel masks. Shader masks still decide per-pixel `RawCandidate`, `SceneGatedCandidate`, and `FinalMask` behavior.");
        builder.AppendLine("- MaterialIntent does not change SceneIntent or VisualProfile. Generated shader variables are written only when MaterialIntent shader mapping is explicitly enabled and matching Dalashade custom shader keys exist.");
        builder.AppendLine($"- MaterialIntent strength: {configuration.MaterialIntentStrength:0.###}");
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
        builder.AppendLine(MaterialIntentDiagnosticsTableHeader);
        builder.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (var channel in MaterialIntent.ChannelNames)
        {
            var profilePrior = profile.ValueFor(channel);
            var evidence = FormatMaterialNonProfileEvidence(intent.Contributions, channel);
            var suppressions = FormatMaterialContributions(intent.Contributions, channel, positive: false);
            builder.AppendLine($"| {channel} | {profilePrior:0.###} | {evidence} | {intent.ValueFor(channel):0.###} | {suppressions} |");
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

    private static IReadOnlySet<string> Set(params string[] values)
    {
        return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadShaderSource(string fileName)
    {
        foreach (var root in CandidateSourceRoots())
        {
            var path = Path.Combine(root, "shaders", fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> CandidateSourceRoots()
    {
        var current = Directory.GetCurrentDirectory();
        for (var directory = new DirectoryInfo(current); directory is not null; directory = directory.Parent)
        {
            yield return directory.FullName;
        }

        var baseDirectory = AppContext.BaseDirectory;
        for (var directory = new DirectoryInfo(baseDirectory); directory is not null; directory = directory.Parent)
        {
            yield return directory.FullName;
        }
    }

    private static bool UsesSharedMaterialResolver(string source)
    {
        return source.Contains("Dalashade_GetAllMaterialMasks", StringComparison.Ordinal)
               || source.Contains("Dalashade_ResolveWater", StringComparison.Ordinal)
               || source.Contains("Dalashade_GetWaterPlaneMask", StringComparison.Ordinal)
               || source.Contains("Dalashade_GetMaterialSignals", StringComparison.Ordinal);
    }

    private static bool HasLocalMaterialLogic(string source, bool usesSharedResolver)
    {
        if (source.Contains("Dalashade_ResolveWater", StringComparison.Ordinal))
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

    private static string YesNo(bool value) => value ? "yes" : "no";

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

    private static bool IsNightTag(string tag)
    {
        return tag.Contains("Night", StringComparison.OrdinalIgnoreCase)
               || string.Equals(tag, "night", StringComparison.OrdinalIgnoreCase);
    }

    private static int ClampInt(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    private static string FormatSceneGIDebugMode(int mode)
    {
        return ClampInt(mode, 0, 12) switch
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
        return ClampInt(mode, 0, 10) switch
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
            FormatFirstPartyShaderStatus(analysis, "SceneGI", "Dalashade_SceneGI"),
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
        var shaderOwned = uniqueKeys.Count(CustomShaderVariableMapper.IsKnownShaderOwnedVariable);
        var otherKnown = uniqueKeys.Length - sceneIntent - materialIntent - shaderOwned;
        return $"SceneIntent={sceneIntent}, MaterialIntent={materialIntent}, shader-owned={shaderOwned}, other-known={Math.Max(0, otherKnown)}";
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
        IReadOnlySet<string> channelNames)
    {
        if (!configuration.EnableMaterialIntent)
        {
            return "MaterialIntent disabled";
        }

        var profile = MaterialProfileBuilder.Build(diagnostics, currentImage);
        var intent = MaterialIntentBuilder.Build(diagnostics, currentImage, profile).WithStrength(configuration.MaterialIntentStrength);
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
                                   && !contribution.Source.StartsWith("MaterialProfile", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(contribution => Math.Abs(contribution.Amount))
            .Take(3)
            .Select(contribution => $"{EscapeTable(contribution.Source)} +{contribution.Amount:0.##}: {EscapeTable(contribution.Reason)}")
            .ToArray();

        return selected.Length == 0 ? "none" : string.Join("<br>", selected);
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

        if (source.Contains("Night", StringComparison.OrdinalIgnoreCase) || source.Contains("Dawn", StringComparison.OrdinalIgnoreCase))
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

    private static IEnumerable<PresetTechnique> DeduplicateTechniques(IEnumerable<PresetTechnique> techniques)
    {
        return techniques
            .GroupBy(technique => $"{technique.TechniqueName}\u001f{technique.ShaderFile}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
    }
}
