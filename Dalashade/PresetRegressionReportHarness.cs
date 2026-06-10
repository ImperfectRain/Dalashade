using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dalashade;

public sealed record PresetRegressionReportResult(
    bool Success,
    string Message,
    string OutputDirectory,
    int PresetCount)
{
    public static PresetRegressionReportResult Skipped(string message) => new(false, message, string.Empty, 0);
}

public sealed class PresetRegressionReportHarness
{
    private readonly PresetAnalyzer analyzer = new();
    private readonly PresetWriter writer = new();

    public PresetRegressionReportResult Run(Configuration configuration, VisualProfile profile, string outputRoot)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.TestPresetFolderPath))
            {
                return PresetRegressionReportResult.Skipped("Test preset folder is empty.");
            }

            var presetFolder = Path.GetFullPath(configuration.TestPresetFolderPath);
            if (!Directory.Exists(presetFolder))
            {
                return PresetRegressionReportResult.Skipped("Test preset folder was not found.");
            }

            var presets = Directory
                .EnumerateFiles(presetFolder, "*.ini", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (presets.Length == 0)
            {
                return PresetRegressionReportResult.Skipped("No .ini presets were found in the selected test folder.");
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var outputDirectory = Path.Combine(outputRoot, $"Regression-{timestamp}");
            var generatedDirectory = Path.Combine(outputDirectory, "_generated");
            Directory.CreateDirectory(outputDirectory);
            Directory.CreateDirectory(generatedDirectory);

            var summaries = new List<PresetRegressionSummary>();
            foreach (var presetPath in presets)
            {
                var presetConfiguration = CreatePresetConfiguration(configuration, presetPath, generatedDirectory);
                var analysis = analyzer.Analyze(presetConfiguration);
                var support = writer.ScanSupportedVariables(presetConfiguration);
                var writeResult = writer.WriteGeneratedPreset(presetConfiguration, profile);
                TryDelete(presetConfiguration.GeneratedPresetPath);

                var summary = PresetRegressionSummary.From(presetPath, presetFolder, presetConfiguration.CompatibilityMode, analysis, support, writeResult);
                summaries.Add(summary);

                var reportName = MakeSafeFileName(Path.GetRelativePath(presetFolder, presetPath));
                var reportPath = CreateUniqueReportPath(outputDirectory, Path.GetFileNameWithoutExtension(reportName));
                File.WriteAllText(reportPath, BuildPresetReport(summary, analysis, support, profile, writeResult), Encoding.UTF8);
            }

            File.WriteAllText(Path.Combine(outputDirectory, "index.md"), BuildIndexReport(configuration, presetFolder, summaries), Encoding.UTF8);
            TryDeleteDirectory(generatedDirectory);

            return new PresetRegressionReportResult(
                true,
                $"Preset regression reports written for {summaries.Count} preset(s): {outputDirectory}",
                outputDirectory,
                summaries.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return PresetRegressionReportResult.Skipped($"Preset regression report failed: {ex.Message}");
        }
    }

    private static Configuration CreatePresetConfiguration(Configuration source, string presetPath, string generatedDirectory)
    {
        return new Configuration
        {
            Version = source.Version,
            BasePresetPath = presetPath,
            GeneratedPresetPath = Path.Combine(generatedDirectory, $"{Guid.NewGuid():N}.ini"),
            UsePremiumImmerseEffects = source.UsePremiumImmerseEffects,
            CompatibilityMode = source.CompatibilityMode,
            ShaderMatchingMode = source.ShaderMatchingMode,
            InactiveShaderWriteMode = source.InactiveShaderWriteMode,
            WriteBackups = false,
            MaxGeneratedPresetBackups = source.MaxGeneratedPresetBackups,
            TestPresetFolderPath = source.TestPresetFolderPath
        };
    }

    private static string BuildPresetReport(
        PresetRegressionSummary summary,
        PresetAnalysisResult analysis,
        ShaderSupportScan support,
        VisualProfile profile,
        PresetWriteResult writeResult)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {summary.RelativePath}");
        builder.AppendLine();
        builder.AppendLine($"Preset path: `{summary.PresetPath}`");
        builder.AppendLine($"Risk level: {summary.RiskLevel}");
        builder.AppendLine($"Selected compatibility mode: {summary.SelectedCompatibilityMode}");
        builder.AppendLine($"Recommended compatibility mode: {summary.RecommendedCompatibilityMode}");
        builder.AppendLine($"Changed variable count: {summary.ChangedVariableCount}");
        builder.AppendLine($"Clamped variable count: {summary.ClampedVariableCount}");
        builder.AppendLine($"Warning count: {summary.WarningCount}");
        builder.AppendLine($"Sanitize action count: {summary.SanitizeActionCount}");
        builder.AppendLine();

        AppendTechniqueGroup(builder, "Active controlled effects", analysis.Report.ActiveSupportedEffects);
        AppendTechniqueGroup(builder, "Active partial effects", analysis.Report.ActivePartiallySupportedEffects);
        AppendTechniqueGroup(builder, "Active detected-only effects", analysis.Report.ActiveDetectedOnlyEffects);
        AppendTechniqueGroup(builder, "Active unsupported effects", analysis.Report.ActiveUnsupportedEffects);
        AppendAuthorities(builder, analysis.Report.Authorities);
        AppendColorFamilyAdjustments(builder, profile);

        builder.AppendLine("## Scan Messages");
        builder.AppendLine();
        builder.AppendLine($"- Analysis: {analysis.Message}");
        builder.AppendLine($"- Shader support: {support.Message}");
        builder.AppendLine($"- Generation simulation: {writeResult.Message}");
        builder.AppendLine();

        return builder.ToString();
    }

    private static void AppendColorFamilyAdjustments(StringBuilder builder, VisualProfile profile)
    {
        builder.AppendLine("## Master style color family adjustments");
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

    private static string BuildIndexReport(Configuration configuration, string presetFolder, IReadOnlyList<PresetRegressionSummary> summaries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Dalashade Preset Regression Reports");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"Preset folder: `{presetFolder}`");
        builder.AppendLine($"Selected compatibility mode: {PresetAnalyzer.FormatCompatibilityMode(configuration.CompatibilityMode)}");
        builder.AppendLine($"Preset count: {summaries.Count}");
        builder.AppendLine();
        builder.AppendLine("| Preset | Risk | Recommended | Changed | Clamped | Warnings | Sanitize |");
        builder.AppendLine("| --- | --- | --- | ---: | ---: | ---: | ---: |");

        foreach (var summary in summaries)
        {
            builder.AppendLine($"| {EscapeTable(summary.RelativePath)} | {summary.RiskLevel} | {summary.RecommendedCompatibilityMode} | {summary.ChangedVariableCount} | {summary.ClampedVariableCount} | {summary.WarningCount} | {summary.SanitizeActionCount} |");
        }

        builder.AppendLine();
        return builder.ToString();
    }

    private static void AppendTechniqueGroup(StringBuilder builder, string title, IReadOnlyList<PresetTechnique> techniques)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        if (techniques.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (var technique in techniques)
        {
            builder.AppendLine($"- {PresetAnalyzer.FormatTechnique(technique)} | activation={PresetAnalyzer.FormatActivationState(technique.ActivationState)} | role={PresetAnalyzer.FormatRole(technique.Role)} | risk={PresetAnalyzer.FormatRisk(technique.Risk)} | support={technique.SupportLevel}");
        }

        builder.AppendLine();
    }

    private static void AppendAuthorities(StringBuilder builder, IReadOnlyList<EffectAuthority> authorities)
    {
        builder.AppendLine("## Authorities");
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
            foreach (var secondary in authority.SecondaryShaders)
            {
                builder.AppendLine($"  - secondary={secondary}");
            }
        }

        builder.AppendLine();
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).ToHashSet();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "preset" : safe;
    }

    private static string EscapeTable(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string CreateUniqueReportPath(string outputDirectory, string baseName)
    {
        var reportPath = Path.Combine(outputDirectory, $"{baseName}.md");
        var suffix = 1;
        while (File.Exists(reportPath))
        {
            reportPath = Path.Combine(outputDirectory, $"{baseName}-{suffix}.md");
            suffix++;
        }

        return reportPath;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

public sealed record PresetRegressionSummary(
    string PresetPath,
    string RelativePath,
    PresetRiskLevel RiskLevel,
    string SelectedCompatibilityMode,
    string RecommendedCompatibilityMode,
    int ChangedVariableCount,
    int ClampedVariableCount,
    int WarningCount,
    int SanitizeActionCount)
{
    public static PresetRegressionSummary From(
        string presetPath,
        string presetFolder,
        PresetCompatibilityMode selectedMode,
        PresetAnalysisResult analysis,
        ShaderSupportScan support,
        PresetWriteResult writeResult)
    {
        _ = support;
        var report = analysis.Report;
        var warningCount = report.Warnings
            .Concat(writeResult.Changes
                .Select(change => change.Warning)
                .Where(warning => !string.IsNullOrWhiteSpace(warning))
                .Select(warning => warning!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new PresetRegressionSummary(
            presetPath,
            Path.GetRelativePath(presetFolder, presetPath),
            report.Level,
            PresetAnalyzer.FormatCompatibilityMode(selectedMode),
            PresetAnalyzer.FormatCompatibilityMode(report.RecommendedCompatibilityMode),
            writeResult.ChangedVariables,
            writeResult.Changes.Count(change => change.HitMin || change.HitMax),
            warningCount,
            writeResult.SanitizeActions.Count);
    }
}
