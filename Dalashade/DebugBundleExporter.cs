using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dalashade;

public sealed record DebugBundleExportResult(
    bool Success,
    string Message,
    string FolderPath,
    string ZipPath,
    IReadOnlyList<string> IncludedFiles,
    IReadOnlyList<string> SkippedFiles)
{
    public static DebugBundleExportResult Skipped(string message) => new(false, message, string.Empty, string.Empty, Array.Empty<string>(), Array.Empty<string>());
}

public sealed class DebugBundleExporter
{
    private const string BundleFormatVersion = "1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string[] FirstPartyShaderFiles =
    [
        "Dalashade_AdaptiveGrade.fx",
        "Dalashade_AtmosphereBloom.fx",
        "Dalashade_WeatherAtmosphere.fx",
        "Dalashade_SmartSharpen.fx",
        "Dalashade_MaterialDebug.fx",
        "Dalashade_SceneGI.fx",
        "Dalashade_SurfaceReflection.fx",
        "Dalashade_MaterialMasks.fxh"
    ];

    public DebugBundleExportResult Export(
        Configuration configuration,
        GameContext context,
        TagStackDiagnostics diagnostics,
        ImageAnalysisResult imageAnalysis,
        ImageAnalysisResult masterStyle,
        VisualProfile profile,
        MaterialIntent materialIntent,
        PresetAnalysisResult analysis,
        ShaderSupportScan shaderSupport,
        PresetWriteResult writeResult,
        CompatibilityReportExportResult freshReport,
        string effectiveBasePresetPath,
        string outputRoot,
        string pluginConfigDirectory)
    {
        var included = new List<string>();
        var skipped = new List<string>();
        try
        {
            Directory.CreateDirectory(outputRoot);
            var timestamp = DateTimeOffset.Now;
            var folderName = $"Dalashade_DebugBundle_{timestamp:yyyyMMdd_HHmmss}";
            var folderPath = Path.Combine(outputRoot, folderName);
            Directory.CreateDirectory(folderPath);

            CopyCompatibilityReport(folderPath, freshReport, included, skipped);
            CopyOrExplain(effectiveBasePresetPath, Path.Combine(folderPath, "base-preset.ini"), Path.Combine(folderPath, "base-preset-missing.txt"), "Base preset", included, skipped);
            CopyOrExplain(configuration.GeneratedPresetPath, Path.Combine(folderPath, "generated-preset.ini"), Path.Combine(folderPath, "generated-preset-missing.txt"), "Generated preset", included, skipped);
            CopyActivePreset(configuration, folderPath, included, skipped);

            WriteJson(Path.Combine(folderPath, "plugin-config.json"), configuration, included);
            WriteJson(Path.Combine(folderPath, "scene-context.json"), BuildSceneContextDump(context, diagnostics), included);
            WriteJson(Path.Combine(folderPath, "scene-intent.json"), BuildSceneIntentDump(diagnostics.Intent), included);
            WriteJson(Path.Combine(folderPath, "material-intent.json"), BuildMaterialIntentDump(diagnostics, imageAnalysis, materialIntent, writeResult), included);
            WriteText(Path.Combine(folderPath, "material-parity-audit.md"), BuildMaterialParityAudit(freshReport), included);
            WriteText(Path.Combine(folderPath, "shader-stack-summary.md"), BuildShaderStackSummary(analysis), included);
            WriteText(Path.Combine(folderPath, "installed-dalashade-shaders.txt"), BuildInstalledShaderStatus(configuration, included, skipped), included);
            WriteText(Path.Combine(folderPath, "paths-and-environment.txt"), BuildPathsAndEnvironment(configuration, pluginConfigDirectory, timestamp), included);
            WriteText(Path.Combine(folderPath, "README.txt"), BuildReadme(), included);

            var manifest = new
            {
                GeneratedTimestamp = timestamp,
                DalashadeVersion = GetPluginVersion(),
                BundleFormatVersion,
                FolderName = folderName,
                IncludedFiles = included.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                MissingOrSkipped = skipped.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()
            };
            WriteJson(Path.Combine(folderPath, "manifest.json"), manifest, included);

            var zipPath = TryCreateZip(folderPath, skipped);
            var message = string.IsNullOrWhiteSpace(zipPath)
                ? $"Debug bundle exported: {folderPath}"
                : $"Debug bundle exported: {folderPath} and {zipPath}";
            if (skipped.Count > 0)
            {
                message += $" ({skipped.Count} skipped item(s); see manifest.json).";
            }

            return new DebugBundleExportResult(true, message, folderPath, zipPath ?? string.Empty, included.ToArray(), skipped.ToArray());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new DebugBundleExportResult(false, $"Debug bundle export failed: {ex.Message}", string.Empty, string.Empty, included.ToArray(), skipped.ToArray());
        }
    }

    private static object BuildSceneContextDump(GameContext context, TagStackDiagnostics diagnostics)
    {
        var combinedTags = diagnostics.SecondaryTags.Concat(diagnostics.ArtDirectionTags).ToArray();
        return new
        {
            context.TerritoryId,
            context.TerritoryName,
            context.WeatherId,
            context.WeatherName,
            diagnostics.WeatherKey,
            context.TimeBucket,
            EorzeaHour = context.EorzeaHour,
            context.InCombat,
            context.InDuty,
            context.InCutscene,
            context.InGpose,
            diagnostics.AreaKey,
            diagnostics.BiomeKey,
            diagnostics.BiomeConfidence,
            diagnostics.BiomeReason,
            diagnostics.ActiveWeatherTags,
            diagnostics.SecondaryTags,
            diagnostics.MoodTags,
            diagnostics.MaterialTags,
            diagnostics.AreaContextTags,
            diagnostics.GameplayStateTags,
            diagnostics.ArtDirectionTags,
            NightTags = combinedTags.Where(IsNightTag).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            DayTags = combinedTags.Where(IsDayTag).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            diagnostics.ActiveTags
        };
    }

    private static object BuildSceneIntentDump(SceneIntent intent)
    {
        return new
        {
            Channels = new
            {
                intent.Readability,
                intent.Atmosphere,
                intent.HighlightProtection,
                intent.ShadowProtection,
                intent.Haze,
                intent.Wetness,
                intent.Cold,
                intent.Heat,
                intent.MagicGlow,
                intent.NeonGlow,
                intent.FoliageDensity,
                intent.IndustrialHardness,
                intent.CosmicMood,
                intent.Night,
                intent.Moonlight,
                intent.ArtificialLight,
                intent.AmbientDarkness,
                intent.NightAtmosphere,
                intent.Daylight,
                intent.Sunlight,
                intent.OpenSkyLight,
                intent.SurfaceHeat,
                intent.DayAtmosphere,
                intent.DayReflection,
                intent.DayHighlightPressure,
                intent.CombatPressure,
                intent.CinematicPermission
            },
            Contributions = intent.Contributions.Select(contribution => new
            {
                contribution.Source,
                contribution.Intent,
                contribution.Amount,
                contribution.Reason
            }).ToArray()
        };
    }

    private static object BuildMaterialIntentDump(TagStackDiagnostics diagnostics, ImageAnalysisResult imageAnalysis, MaterialIntent currentMaterialIntent, PresetWriteResult writeResult)
    {
        var profile = MaterialProfileBuilder.Build(diagnostics, imageAnalysis);
        var rawIntent = MaterialIntentBuilder.Build(diagnostics, imageAnalysis, profile);
        return new
        {
            MaterialProfile = new
            {
                profile.Family,
                profile.ProfileTags,
                TopPriors = profile.TopPriors(MaterialIntent.ChannelNames.Count).Select(item => new { item.Channel, item.Value }).ToArray(),
                Suppressions = profile.Contributions.Where(contribution => contribution.Amount < 0f).Select(contribution => new
                {
                    contribution.Channel,
                    contribution.Amount,
                    contribution.Source,
                    contribution.Reason
                }).ToArray(),
                Contributions = profile.Contributions.Select(contribution => new
                {
                    contribution.Channel,
                    contribution.Amount,
                    contribution.Source,
                    contribution.Reason
                }).ToArray()
            },
            RawMaterialIntent = MaterialIntentValues(rawIntent),
            CurrentMaterialIntent = MaterialIntentValues(currentMaterialIntent),
            PositiveContributions = rawIntent.Contributions.Where(contribution => contribution.Amount > 0f).Select(MaterialContributionDump).ToArray(),
            NegativeContributions = rawIntent.Contributions.Where(contribution => contribution.Amount < 0f).Select(MaterialContributionDump).ToArray(),
            ShaderUniformOutput = writeResult.Changes
                .Where(change => string.Equals(change.ReasonCategory, CustomShaderVariableMapper.MaterialReasonCategory, StringComparison.OrdinalIgnoreCase))
                .Select(change => new
                {
                    change.Section,
                    change.Key,
                    change.NewValue,
                    change.ActivationState,
                    change.Warning
                })
                .ToArray()
        };
    }

    private static object MaterialContributionDump(MaterialIntentContribution contribution)
    {
        return new
        {
            contribution.Channel,
            contribution.Amount,
            contribution.Source,
            contribution.Reason
        };
    }

    private static IReadOnlyDictionary<string, float> MaterialIntentValues(MaterialIntent intent)
    {
        return MaterialIntent.ChannelNames.ToDictionary(channel => channel, intent.ValueFor, StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildMaterialParityAudit(CompatibilityReportExportResult freshReport)
    {
        if (!freshReport.Success || string.IsNullOrWhiteSpace(freshReport.Path) || !File.Exists(freshReport.Path))
        {
            return "# Material Parity Audit\n\nFresh compatibility report was unavailable, so the parity audit section could not be copied.\n";
        }

        var report = File.ReadAllText(freshReport.Path);
        var section = ExtractMarkdownSection(report, "## Material Parity Audit");
        return string.IsNullOrWhiteSpace(section)
            ? "# Material Parity Audit\n\nThe fresh compatibility report did not contain a Material Parity Audit section.\n"
            : section;
    }

    private static string BuildShaderStackSummary(PresetAnalysisResult analysis)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Shader Stack Summary");
        builder.AppendLine();
        builder.AppendLine($"- Analysis success: {analysis.Success}");
        builder.AppendLine($"- Message: {analysis.Message}");
        builder.AppendLine($"- Risk: {analysis.Report.Level}");
        builder.AppendLine($"- Recommended compatibility mode: {PresetAnalyzer.FormatCompatibilityMode(analysis.Report.RecommendedCompatibilityMode)}");
        builder.AppendLine();
        AppendTechniques(builder, "Active controlled effects", analysis.Report.ActiveSupportedEffects);
        AppendTechniques(builder, "Active partially controlled effects", analysis.Report.ActivePartiallySupportedEffects);
        AppendTechniques(builder, "Active unknown/detected-only effects", analysis.Report.ActiveDetectedOnlyEffects.Concat(analysis.Report.ActiveUnsupportedEffects).ToArray());
        AppendAuthorities(builder, analysis.Report.Authorities);
        builder.AppendLine("## Role Warnings");
        builder.AppendLine();
        if (analysis.Report.Warnings.Count == 0 && analysis.Report.MultipleAuthorityWarnings.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var warning in analysis.Report.Warnings.Concat(analysis.Report.MultipleAuthorityWarnings))
            {
                builder.AppendLine($"- {warning}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Recommended Gameplay Load Order");
        builder.AppendLine();
        foreach (var item in RecommendedLoadOrder())
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
        builder.AppendLine("## Actual Load Order");
        builder.AppendLine();
        if (analysis.Techniques.Count == 0)
        {
            builder.AppendLine("- unavailable");
        }
        else
        {
            foreach (var technique in analysis.Techniques)
            {
                builder.AppendLine($"- {technique.TechniqueName}@{technique.ShaderFile} ({technique.ActivationState}, {technique.Role}, {technique.SupportLevel})");
            }
        }

        return builder.ToString();
    }

    private static string BuildInstalledShaderStatus(Configuration configuration, List<string> included, List<string> skipped)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Dalashade first-party shader install status");
        builder.AppendLine();
        var shaderPaths = FindReShadeShaderPaths(configuration).ToArray();
        builder.AppendLine($"Detected shader search paths: {(shaderPaths.Length == 0 ? "none" : string.Join("; ", shaderPaths))}");
        builder.AppendLine();
        foreach (var fileName in FirstPartyShaderFiles)
        {
            var found = shaderPaths
                .Select(path => Path.Combine(path, fileName))
                .FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(found))
            {
                builder.AppendLine($"{fileName}: missing");
                skipped.Add($"installed-dalashade-shaders/{fileName}: missing from detected ReShade shader paths");
                continue;
            }

            var info = new FileInfo(found);
            builder.AppendLine($"{fileName}: present");
            builder.AppendLine($"  path: {info.FullName}");
            builder.AppendLine($"  modified: {info.LastWriteTimeUtc:O}");
            builder.AppendLine($"  size: {info.Length}");
            builder.AppendLine($"  sha256: {Sha256(info.FullName)}");
        }

        return builder.ToString();
    }

    private static string BuildPathsAndEnvironment(Configuration configuration, string pluginConfigDirectory, DateTimeOffset timestamp)
    {
        var reShadeIniPath = FindReShadeIni(configuration);
        var gamePath = TryFindGamePath();
        var shaderPaths = FindReShadeShaderPaths(configuration).ToArray();
        var builder = new StringBuilder();
        builder.AppendLine($"Timestamp: {timestamp:O}");
        builder.AppendLine($"Dalashade version: {GetPluginVersion()}");
        builder.AppendLine($"XIV game path: {gamePath ?? "unknown"}");
        builder.AppendLine($"ReShade.ini path: {reShadeIniPath ?? "unknown"}");
        builder.AppendLine($"ReShade shader paths: {(shaderPaths.Length == 0 ? "unknown" : string.Join("; ", shaderPaths))}");
        builder.AppendLine($"Base preset path: {configuration.BasePresetPath}");
        builder.AppendLine($"Generated preset path: {configuration.GeneratedPresetPath}");
        builder.AppendLine($"Active ReShade preset path: {FindActivePresetPath(configuration) ?? "unknown"}");
        builder.AppendLine($"Dalashade config directory: {pluginConfigDirectory}");
        builder.AppendLine($"Dalamud plugin config directory: {pluginConfigDirectory}");
        builder.AppendLine($"NormalField enabled: {configuration.EnableNormalField}");
        builder.AppendLine($"NormalField diagnostics: {configuration.EnableNormalFieldDiagnostics}");
        builder.AppendLine($"NormalField shader mapping: {configuration.EnableNormalFieldShaderMapping}");
        builder.AppendLine($"NormalField strength/depth/detail/material: {configuration.NormalFieldStrength:0.###} / {configuration.NormalFieldDepthStrength:0.###} / {configuration.NormalFieldDetailStrength:0.###} / {configuration.NormalFieldMaterialInfluence:0.###}");
        builder.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        builder.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
        return builder.ToString();
    }

    private static string BuildReadme()
    {
        return """
Dalashade Debug Bundle

This folder was generated by Export Debug Bundle. It contains current Dalashade diagnostics, preset copies, scene context, SceneIntent, MaterialIntent, shader stack summaries, and first-party shader install status.

The material and water entries are inferred heuristics, not true FFXIV engine material IDs. Debug shader modes should be used in ReShade to inspect pixel-level mask behavior.

Preset and plugin config files are included intentionally for debugging. Full ReShade/Dalamud logs are not included by default.
""";
    }

    private static void CopyCompatibilityReport(string folderPath, CompatibilityReportExportResult freshReport, List<string> included, List<string> skipped)
    {
        var target = Path.Combine(folderPath, "compatibility-report.md");
        if (freshReport.Success && !string.IsNullOrWhiteSpace(freshReport.Path) && File.Exists(freshReport.Path))
        {
            File.Copy(freshReport.Path, target, true);
            included.Add("compatibility-report.md");
            return;
        }

        File.WriteAllText(target, $"Compatibility report unavailable: {freshReport.Message}", Encoding.UTF8);
        included.Add("compatibility-report.md");
        skipped.Add($"compatibility-report.md: {freshReport.Message}");
    }

    private static void CopyActivePreset(Configuration configuration, string folderPath, List<string> included, List<string> skipped)
    {
        var activePresetPath = FindActivePresetPath(configuration);
        if (!string.IsNullOrWhiteSpace(activePresetPath) && File.Exists(activePresetPath))
        {
            File.Copy(activePresetPath, Path.Combine(folderPath, "active-preset.ini"), true);
            included.Add("active-preset.ini");
            return;
        }

        var reason = string.IsNullOrWhiteSpace(activePresetPath)
            ? "Dalashade could not determine the active/current ReShade preset from ReShade.ini."
            : $"Active/current ReShade preset was not found: {activePresetPath}";
        File.WriteAllText(Path.Combine(folderPath, "active-preset-unavailable.txt"), reason, Encoding.UTF8);
        included.Add("active-preset-unavailable.txt");
        skipped.Add($"active-preset.ini: {reason}");
    }

    private static void CopyOrExplain(string sourcePath, string targetPath, string missingPath, string label, List<string> included, List<string> skipped)
    {
        if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
        {
            File.Copy(sourcePath, targetPath, true);
            included.Add(Path.GetFileName(targetPath));
            return;
        }

        var reason = string.IsNullOrWhiteSpace(sourcePath)
            ? $"{label} path is empty."
            : $"{label} was not found: {sourcePath}";
        File.WriteAllText(missingPath, reason, Encoding.UTF8);
        included.Add(Path.GetFileName(missingPath));
        skipped.Add($"{Path.GetFileName(targetPath)}: {reason}");
    }

    private static void WriteJson(string path, object value, List<string> included)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8);
        included.Add(Path.GetFileName(path));
    }

    private static void WriteText(string path, string value, List<string> included)
    {
        File.WriteAllText(path, value, Encoding.UTF8);
        included.Add(Path.GetFileName(path));
    }

    private static string? TryCreateZip(string folderPath, List<string> skipped)
    {
        var zipPath = $"{folderPath}.zip";
        try
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(folderPath, zipPath, CompressionLevel.Optimal, false);
            return zipPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            skipped.Add($"zip: {ex.Message}");
            return null;
        }
    }

    private static string ExtractMarkdownSection(string markdown, string heading)
    {
        var start = markdown.IndexOf(heading, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return string.Empty;
        }

        var next = markdown.IndexOf("\n## ", start + heading.Length, StringComparison.OrdinalIgnoreCase);
        return next < 0 ? markdown[start..] : markdown[start..next];
    }

    private static void AppendTechniques(StringBuilder builder, string title, IReadOnlyList<PresetTechnique> techniques)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        if (techniques.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var technique in techniques)
            {
                builder.AppendLine($"- {technique.TechniqueName}@{technique.ShaderFile} ({technique.Role}, {technique.SupportLevel})");
            }
        }

        builder.AppendLine();
    }

    private static void AppendAuthorities(StringBuilder builder, IReadOnlyList<EffectAuthority> authorities)
    {
        builder.AppendLine("## Effect Authorities");
        builder.AppendLine();
        if (authorities.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var authority in authorities)
            {
                builder.AppendLine($"- {authority.Role}: primary `{authority.PrimaryShader}`, secondary `{FormatList(authority.SecondaryShaders)}`, warned `{FormatList(authority.SuppressedOrWarnedShaders)}`");
            }
        }

        builder.AppendLine();
    }

    private static IReadOnlyList<string> RecommendedLoadOrder() =>
    [
        "iMMERSE Launchpad",
        "Deband",
        "iMMERSE Pro Clarity",
        "iMMERSE Pro ReGrade",
        "Dalashade_AdaptiveGrade",
        "Dalashade_SceneGI",
        "Dalashade_SurfaceReflection",
        "MagicBloom",
        "Dalashade_AtmosphereBloom",
        "Dalashade_WeatherAtmosphere",
        "iMMERSE Sharpen",
        "Dalashade_SmartSharpen",
        "Dalashade_MaterialDebug"
    ];

    private static string FormatList(IReadOnlyList<string> values) => values.Count == 0 ? "none" : string.Join(", ", values);

    private static string? FindActivePresetPath(Configuration configuration)
    {
        var reShadeIniPath = FindReShadeIni(configuration);
        if (string.IsNullOrWhiteSpace(reShadeIniPath) || !File.Exists(reShadeIniPath))
        {
            return null;
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
            if (!key.Contains("Preset", StringComparison.OrdinalIgnoreCase) || !key.Contains("Path", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = line[(separatorIndex + 1)..].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var candidate = Path.IsPathRooted(value) ? value : Path.Combine(iniDirectory, value);
            return Path.GetFullPath(candidate);
        }

        return null;
    }

    private static IEnumerable<string> FindReShadeShaderPaths(Configuration configuration)
    {
        var reShadeIniPath = FindReShadeIni(configuration);
        if (!string.IsNullOrWhiteSpace(reShadeIniPath) && File.Exists(reShadeIniPath))
        {
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
                    var candidate = Path.IsPathRooted(normalized) ? normalized : Path.Combine(iniDirectory, normalized);
                    if (Directory.Exists(candidate))
                    {
                        yield return Path.GetFullPath(candidate);
                    }
                }
            }

            var defaultShaders = Path.Combine(iniDirectory, "reshade-shaders", "Shaders");
            if (Directory.Exists(defaultShaders))
            {
                yield return Path.GetFullPath(defaultShaders);
            }
        }
    }

    private static string? FindReShadeIni(Configuration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.ReShadeIniPath) && File.Exists(configuration.ReShadeIniPath))
        {
            return Path.GetFullPath(configuration.ReShadeIniPath);
        }

        foreach (var candidate in new[] { configuration.GeneratedPresetPath, configuration.BasePresetPath })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var directory = File.Exists(candidate) ? Path.GetDirectoryName(Path.GetFullPath(candidate)) : Path.GetDirectoryName(Path.GetFullPath(candidate));
            while (!string.IsNullOrWhiteSpace(directory))
            {
                var reShadeIniPath = Path.Combine(directory, "ReShade.ini");
                if (File.Exists(reShadeIniPath))
                {
                    return reShadeIniPath;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }
        }

        var gamePath = TryFindGamePath();
        if (!string.IsNullOrWhiteSpace(gamePath))
        {
            var reShadeIniPath = Path.Combine(gamePath, "game", "ReShade.ini");
            if (File.Exists(reShadeIniPath))
            {
                return reShadeIniPath;
            }
        }

        return null;
    }

    private static string? TryFindGamePath()
    {
        var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "launcherConfigV3.json");
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            return document.RootElement.TryGetProperty("GamePath", out var gamePathElement)
                ? gamePathElement.GetString()
                : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetPluginVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
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
}
