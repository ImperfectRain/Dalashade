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
        "Dalashade_NormalDebug.fx",
        "Dalashade_SceneGI.fx",
        "Dalashade_SurfaceReflection.fx",
        "Dalashade_MaterialMasks.fxh",
        "Dalashade_NormalField.fxh"
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
        var log = new List<string>();
        var stage = "resolving debug bundle output root";
        try
        {
            var safePluginConfigDirectory = ResolveSafeDirectory(pluginConfigDirectory, GetDefaultPluginConfigDirectory(), string.Empty);
            outputRoot = ResolveDebugBundleRoot(outputRoot, safePluginConfigDirectory);
            Directory.CreateDirectory(outputRoot);
            var timestamp = DateTimeOffset.Now;
            var folderName = $"Dalashade_DebugBundle_{timestamp:yyyyMMdd_HHmmss}";
            var folderPath = Path.Combine(outputRoot, folderName);
            stage = "creating debug bundle folder";
            Directory.CreateDirectory(folderPath);
            log.Add($"timestamp: {timestamp:O}");
            log.Add($"resolved output root: {outputRoot}");
            log.Add($"folder path: {folderPath}");

            TryBundleStep("copy compatibility report", () => CopyCompatibilityReport(folderPath, freshReport, included, skipped), log, skipped);
            TryBundleStep("copy base preset", () => CopyOrExplain(effectiveBasePresetPath, Path.Combine(folderPath, "base-preset.ini"), Path.Combine(folderPath, "base-preset-missing.txt"), "Base preset", included, skipped), log, skipped);
            TryBundleStep("copy generated preset", () => CopyOrExplain(configuration.GeneratedPresetPath, Path.Combine(folderPath, "generated-preset.ini"), Path.Combine(folderPath, "generated-preset-missing.txt"), "Generated preset", included, skipped), log, skipped);
            TryBundleStep("copy active preset", () => CopyActivePreset(configuration, folderPath, included, skipped), log, skipped);

            stage = "write plugin-config.json";
            WriteJson(Path.Combine(folderPath, "plugin-config.json"), configuration, included);
            log.Add($"{stage}: ok");
            stage = "write scene-context.json";
            WriteJson(Path.Combine(folderPath, "scene-context.json"), BuildSceneContextDump(context, diagnostics), included);
            log.Add($"{stage}: ok");
            stage = "write scene-intent.json";
            WriteJson(Path.Combine(folderPath, "scene-intent.json"), BuildSceneIntentDump(diagnostics.Intent), included);
            log.Add($"{stage}: ok");

            TryBundleStep("write material-intent.json", () => WriteJson(Path.Combine(folderPath, "material-intent.json"), BuildMaterialIntentDump(diagnostics, imageAnalysis, materialIntent, writeResult), included), log, skipped);
            TryBundleStep("write normal-field-diagnostics.json", () => WriteJson(Path.Combine(folderPath, "normal-field-diagnostics.json"), BuildNormalFieldDiagnosticsDump(configuration, analysis, writeResult), included), log, skipped);
            TryBundleStep("write material-parity-audit.md", () => WriteText(Path.Combine(folderPath, "material-parity-audit.md"), BuildMaterialParityAudit(freshReport), included), log, skipped);
            TryBundleStep("write shader-stack-summary.md", () => WriteText(Path.Combine(folderPath, "shader-stack-summary.md"), BuildShaderStackSummary(analysis), included), log, skipped);
            TryBundleStep("write installed-dalashade-shaders.txt", () => WriteText(Path.Combine(folderPath, "installed-dalashade-shaders.txt"), BuildInstalledShaderStatus(configuration, included, skipped), included), log, skipped);
            TryBundleStep("write paths-and-environment.txt", () => WriteText(Path.Combine(folderPath, "paths-and-environment.txt"), BuildPathsAndEnvironment(configuration, safePluginConfigDirectory, timestamp), included), log, skipped);
            TryBundleStep("write README.txt", () => WriteText(Path.Combine(folderPath, "README.txt"), BuildReadme(), included), log, skipped);

            TryBundleStep("write bundle-export-log.txt", () => WriteText(Path.Combine(folderPath, "bundle-export-log.txt"), BuildExportLog(log, skipped), included), log, skipped);
            var manifest = new
            {
                GeneratedTimestamp = timestamp,
                DalashadeVersion = GetPluginVersion(),
                BundleFormatVersion,
                FolderName = folderName,
                IncludedFiles = included.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                MissingOrSkipped = skipped.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()
            };
            stage = "write manifest.json";
            WriteJson(Path.Combine(folderPath, "manifest.json"), manifest, included);
            log.Add($"{stage}: ok");

            var zipPath = string.Empty;
            TryBundleStep("create zip", () => zipPath = TryCreateZip(folderPath, skipped) ?? string.Empty, log, skipped);
            var targetText = string.IsNullOrWhiteSpace(zipPath)
                ? folderPath
                : $"{folderPath} and {zipPath}";
            var message = skipped.Count == 0
                ? $"Debug bundle exported: {targetText}"
                : $"Debug bundle exported with skipped items: {targetText}. See manifest.json.";
            if (skipped.Count > 0)
            {
                var compatibilityReportSkipped = skipped.Any(item => item.StartsWith("compatibility-report.md:", StringComparison.OrdinalIgnoreCase));
                if (compatibilityReportSkipped)
                {
                    message += " Compatibility report could not be generated; see compatibility-report-missing.txt.";
                }
            }

            return new DebugBundleExportResult(true, message, folderPath, zipPath ?? string.Empty, included.ToArray(), skipped.ToArray());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new DebugBundleExportResult(false, $"Debug bundle export failed during {stage}: {ex.Message}", string.Empty, string.Empty, included.ToArray(), skipped.ToArray());
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

    private static object BuildNormalFieldDiagnosticsDump(Configuration configuration, PresetAnalysisResult analysis, PresetWriteResult writeResult)
    {
        var normalDebugFile = FindFirstShaderFile(configuration, "Dalashade_NormalDebug.fx");
        var normalFieldInclude = FindFirstShaderFile(configuration, "Dalashade_NormalField.fxh");
        var normalDebugTechniques = analysis.Techniques
            .Where(technique => TechniqueContains(technique, "Dalashade_NormalDebug"))
            .Select(technique => new
            {
                technique.Section,
                technique.TechniqueName,
                technique.ShaderFile,
                technique.ActivationState,
                technique.Role,
                technique.SupportLevel
            })
            .ToArray();
        var skippedReasons = new List<string>();
        if (!configuration.EnableNormalField)
        {
            skippedReasons.Add("NormalField disabled; production shaders are unaffected.");
        }
        else if (!configuration.EnableNormalFieldShaderMapping)
        {
            skippedReasons.Add("NormalField diagnostics enabled, but generated-preset shader mapping is disabled.");
        }
        else if (configuration.NormalFieldStrength <= 0f)
        {
            skippedReasons.Add("NormalField shader mapping is enabled, but NormalFieldStrength is 0.0.");
        }

        if (string.IsNullOrWhiteSpace(normalDebugFile))
        {
            skippedReasons.Add("NormalDebug shader file not found. This is not an error unless you are trying to debug NormalField.");
        }

        if (string.IsNullOrWhiteSpace(normalFieldInclude))
        {
            skippedReasons.Add("NormalField include file not found in detected ReShade shader paths.");
        }

        return new
        {
            Settings = new
            {
                configuration.EnableNormalField,
                configuration.EnableNormalFieldDiagnostics,
                configuration.EnableNormalFieldShaderMapping,
                configuration.NormalFieldStrength,
                configuration.NormalFieldDepthStrength,
                configuration.NormalFieldDetailStrength,
                configuration.NormalFieldMaterialInfluence,
                configuration.NormalFieldWaterSuppression,
                configuration.NormalFieldSkinSuppression,
                configuration.NormalFieldSkySuppression,
                configuration.NormalFieldDebugMode,
                configuration.NormalFieldDebugBoost
            },
            ShaderFiles = new
            {
                NormalDebug = BuildShaderFilePresence(normalDebugFile),
                NormalFieldInclude = BuildShaderFilePresence(normalFieldInclude)
            },
            NormalDebugTechniqueActive = normalDebugTechniques.Any(technique => technique.ActivationState == TechniqueActivationState.Active),
            NormalDebugTechniques = normalDebugTechniques,
            FirstPartyShaderConsumption = BuildNormalFieldShaderConsumption(),
            Mapping = new
            {
                Enabled = configuration.EnableNormalField && configuration.EnableNormalFieldShaderMapping && configuration.NormalFieldStrength > 0f,
                WrittenUniforms = writeResult.Changes
                    .Where(change => string.Equals(change.ReasonCategory, CustomShaderVariableMapper.NormalFieldReasonCategory, StringComparison.OrdinalIgnoreCase))
                    .Select(change => new
                    {
                        change.Section,
                        change.Key,
                        change.NewValue,
                        change.ActivationState,
                        change.Warning
                    })
                    .ToArray(),
                SkippedReasons = skippedReasons.ToArray()
            },
            Note = "NormalField is optional and disabled by default. It is a shared screen-space inferred surface-normal layer, not true FFXIV engine normals or material normal maps."
        };
    }

    private static object BuildShaderFilePresence(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new
            {
                Present = false,
                Path = string.Empty,
                ModifiedUtc = string.Empty,
                Size = 0L,
                Sha256 = string.Empty
            };
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new
            {
                Present = false,
                Path = path,
                ModifiedUtc = string.Empty,
                Size = 0L,
                Sha256 = $"unavailable ({ex.Message})"
            };
        }

        if (!File.Exists(fullPath))
        {
            return new
            {
                Present = false,
                Path = fullPath,
                ModifiedUtc = string.Empty,
                Size = 0L,
                Sha256 = string.Empty
            };
        }

        var info = new FileInfo(fullPath);
        return new
        {
            Present = true,
            Path = info.FullName,
            ModifiedUtc = info.LastWriteTimeUtc,
            Size = info.Length,
            Sha256 = Sha256(info.FullName)
        };
    }

    private static object[] BuildNormalFieldShaderConsumption()
    {
        return FirstPartyShaderFiles
            .Where(fileName => fileName.EndsWith(".fx", StringComparison.OrdinalIgnoreCase))
            .Select(fileName =>
            {
                var source = ReadLocalShaderSource(fileName);
                var sourceAvailable = !string.IsNullOrWhiteSpace(source);
                var includesNormalField = sourceAvailable && source.Contains("Dalashade_NormalField.fxh", StringComparison.Ordinal);
                var resolvesNormalField = sourceAvailable && source.Contains("Dalashade_ResolveNormalField", StringComparison.Ordinal);
                var usesDepthNormal = sourceAvailable && source.Contains("Dalashade_GetDepthNormal", StringComparison.Ordinal);
                var usesDetailNormal = sourceAvailable && source.Contains("Dalashade_GetImageGradientNormal", StringComparison.Ordinal);
                return new
                {
                    Shader = fileName,
                    SourceAvailable = sourceAvailable,
                    NormalFieldConsumed = includesNormalField || resolvesNormalField,
                    DepthNormalConsumed = usesDepthNormal,
                    DetailNormalConsumed = usesDetailNormal,
                    IncludesNormalField = includesNormalField,
                    ResolvesNormalField = resolvesNormalField
                };
            })
            .Cast<object>()
            .ToArray();
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
                .Select(path => TryCombine(path, fileName))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .FirstOrDefault(path => File.Exists(path));
            if (string.IsNullOrWhiteSpace(found))
            {
                builder.AppendLine($"{fileName}: missing");
                skipped.Add($"installed-dalashade-shaders/{fileName}: missing from detected ReShade shader paths");
                continue;
            }

            try
            {
                var info = new FileInfo(found);
                builder.AppendLine($"{fileName}: present");
                builder.AppendLine($"  path: {info.FullName}");
                builder.AppendLine($"  modified: {info.LastWriteTimeUtc:O}");
                builder.AppendLine($"  size: {info.Length}");
                builder.AppendLine($"  sha256: {Sha256(info.FullName)}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                builder.AppendLine($"{fileName}: present, metadata unavailable ({ex.Message})");
                skipped.Add($"installed-dalashade-shaders/{fileName}: metadata unavailable ({ex.Message})");
            }
        }

        return builder.ToString();
    }

    private static string? FindFirstShaderFile(Configuration configuration, string fileName)
    {
        return FindReShadeShaderPaths(configuration)
            .Select(path => TryCombine(path, fileName))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .FirstOrDefault(path => File.Exists(path));
    }

    private static void TryBundleStep(string stage, Action action, List<string> log, List<string> skipped)
    {
        try
        {
            action();
            log.Add($"{stage}: ok");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or JsonException)
        {
            var message = $"{stage}: failed: {ex.GetType().Name}: {ex.Message}";
            log.Add(message);
            skipped.Add(message);
        }
    }

    private static string BuildExportLog(IReadOnlyList<string> log, IReadOnlyList<string> skipped)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Dalashade Debug Bundle Export Log");
        builder.AppendLine();
        foreach (var line in log)
        {
            builder.AppendLine(line);
        }

        builder.AppendLine();
        builder.AppendLine("Skipped/failures:");
        if (skipped.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var item in skipped)
            {
                builder.AppendLine($"- {item}");
            }
        }

        return builder.ToString();
    }

    private static string ResolveDebugBundleRoot(string? outputRoot, string pluginConfigDirectory)
    {
        return ResolveSafeDirectory(outputRoot, pluginConfigDirectory, "DebugBundles");
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

        var resolved = TryGetFullPath(root) ?? GetDefaultPluginConfigDirectory();
        if (string.IsNullOrWhiteSpace(candidate) && !string.IsNullOrWhiteSpace(childFolderName))
        {
            resolved = Path.Combine(resolved, MakeSafePathSegment(childFolderName));
        }

        return resolved;
    }

    private static string MakeSafePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Dalashade" : safe;
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
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            skipped.Add("compatibility-report.md: debug bundle folder path was empty");
            return;
        }

        var target = Path.Combine(folderPath, "compatibility-report.md");
        if (freshReport.Success && !string.IsNullOrWhiteSpace(freshReport.Path) && File.Exists(freshReport.Path))
        {
            File.Copy(freshReport.Path, target, true);
            included.Add("compatibility-report.md");
            return;
        }

        File.WriteAllText(Path.Combine(folderPath, "compatibility-report-missing.txt"), $"Compatibility report unavailable: {freshReport.Message}", Encoding.UTF8);
        included.Add("compatibility-report-missing.txt");
        skipped.Add($"compatibility-report.md: {freshReport.Message}");
    }

    private static void CopyActivePreset(Configuration configuration, string folderPath, List<string> included, List<string> skipped)
    {
        var activePresetPath = FindActivePresetPath(configuration);
        var target = TryCombine(folderPath, "active-preset.ini");
        var missingTarget = TryCombine(folderPath, "active-preset-unavailable.txt");
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(missingTarget))
        {
            skipped.Add("active-preset.ini: destination path was empty");
            return;
        }

        if (!string.IsNullOrWhiteSpace(activePresetPath) && File.Exists(activePresetPath))
        {
            File.Copy(activePresetPath, target, true);
            included.Add("active-preset.ini");
            return;
        }

        var reason = string.IsNullOrWhiteSpace(activePresetPath)
            ? "Dalashade could not determine the active/current ReShade preset from ReShade.ini."
            : $"Active/current ReShade preset was not found: {activePresetPath}";
        File.WriteAllText(missingTarget, reason, Encoding.UTF8);
        included.Add("active-preset-unavailable.txt");
        skipped.Add($"active-preset.ini: {reason}");
    }

    private static void CopyOrExplain(string sourcePath, string targetPath, string missingPath, string label, List<string> included, List<string> skipped)
    {
        if (string.IsNullOrWhiteSpace(targetPath) || string.IsNullOrWhiteSpace(missingPath))
        {
            skipped.Add($"{label}: destination path was empty");
            return;
        }

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
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            skipped.Add("zip: bundle folder path was empty");
            return null;
        }

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
        "Dalashade_NormalDebug",
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
            return TryGetFullPath(candidate);
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

            var directory = File.Exists(fullCandidate) ? Path.GetDirectoryName(fullCandidate) : Path.GetDirectoryName(fullCandidate);
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
        if (string.IsNullOrWhiteSpace(path))
        {
            return "unavailable (path empty)";
        }

        try
        {
            using var stream = File.OpenRead(path);
            var hash = SHA256.HashData(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return $"unavailable ({ex.Message})";
        }
    }

    private static string GetDefaultPluginConfigDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(appData)
            ? Path.Combine(Environment.CurrentDirectory, "Dalashade")
            : Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Dalashade");
    }

    private static string ReadLocalShaderSource(string fileName)
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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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

    private static bool TechniqueContains(PresetTechnique technique, string key)
    {
        return technique.Section.Contains(key, StringComparison.OrdinalIgnoreCase)
               || technique.TechniqueName.Contains(key, StringComparison.OrdinalIgnoreCase)
               || technique.ShaderFile.Contains(key, StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryCombine(string? basePath, string childPath)
    {
        if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(childPath))
        {
            return null;
        }

        try
        {
            return Path.Combine(basePath, childPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return null;
        }
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
