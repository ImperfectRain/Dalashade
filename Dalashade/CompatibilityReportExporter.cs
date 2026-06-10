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

            File.WriteAllText(path, BuildReport(configuration, analysis, shaderSupport, writeResult), Encoding.UTF8);
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
        AppendAuthorities(builder, report.Authorities);
        AppendLines(builder, "Warnings", report.Warnings);
        AppendLines(builder, "Multiple Authority Warnings", report.MultipleAuthorityWarnings);
        AppendShaderSupport(builder, shaderSupport);
        AppendChangedVariables(builder, writeResult);

        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine("- This report is diagnostic. Exporting it does not change generated preset behavior.");
        builder.AppendLine("- Detected-only effects are recognized by role/risk but are not directly controlled yet.");
        builder.AppendLine("- Unknown effects are active shaders Dalashade does not understand well enough yet.");

        return builder.ToString();
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
            builder.AppendLine($"- {PresetAnalyzer.FormatTechnique(technique)} | role={PresetAnalyzer.FormatRole(technique.Role)} | risk={PresetAnalyzer.FormatRisk(technique.Risk)} | support={technique.SupportLevel}");
        }

        builder.AppendLine();
    }

    private static void AppendAuthorities(StringBuilder builder, IReadOnlyList<EffectAuthority> authorities)
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
            builder.AppendLine($"- {PresetAnalyzer.FormatRole(authority.Role)}: primary={authority.PrimaryShader}");
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
            var active = item.TechniqueActive ? "active" : "inactive";
            builder.AppendLine($"- {item.Section} / {item.Key} | {item.ReasonCategory} | {active}");
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
            var active = change.TechniqueActive ? "active" : "inactive";
            var clamp = change.HitMin ? "min clamp" : change.HitMax ? "max clamp" : "not clamped";
            var warning = string.IsNullOrWhiteSpace(change.Warning) ? string.Empty : $" | warning={change.Warning}";
            builder.AppendLine($"- {change.Section} / {change.Key}: {change.OldValue} -> {change.NewValue} | {change.ReasonCategory} | {active} | {clamp}{warning}");
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
