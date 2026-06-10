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
    public CompatibilityReportExportResult Export(
        Configuration configuration,
        PresetAnalysisResult analysis,
        ShaderSupportScan shaderSupport,
        VisualProfile profile,
        MasterStyleDiagnostics masterDiagnostics,
        PresetWriteResult writeResult,
        string outputDirectory)
    {
        try
        {
            if (!analysis.Success)
            {
                return CompatibilityReportExportResult.Skipped("Preset analysis has not succeeded yet.");
            }

            Directory.CreateDirectory(outputDirectory);
            var presetName = string.IsNullOrWhiteSpace(configuration.BasePresetPath)
                ? "UnknownPreset"
                : Path.GetFileNameWithoutExtension(configuration.BasePresetPath);
            var safePresetName = MakeSafeFileName(presetName);
            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var path = Path.Combine(outputDirectory, $"{safePresetName}-compatibility-{timestamp}.md");

            File.WriteAllText(path, BuildReport(configuration, analysis, shaderSupport, profile, masterDiagnostics, writeResult), Encoding.UTF8);
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
        PresetWriteResult writeResult)
    {
        var report = analysis.Report;
        var builder = new StringBuilder();

        builder.AppendLine("# Dalashade Compatibility Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"Base preset: `{configuration.BasePresetPath}`");
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
        AppendMasterStyleDiagnostics(builder, configuration, masterDiagnostics);
        AppendColorFamilyAdjustments(builder, profile);
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
