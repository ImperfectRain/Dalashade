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
        AppendMasterStyleDiagnostics(builder, configuration, masterDiagnostics);
        AppendColorFamilyAdjustments(builder, profile);
        AppendColorFamilyComparison(builder, currentImage, masterStyle, profile);
        AppendMappingValidation(builder, configuration, analysis, shaderSupport, effectiveBasePresetPath);
        AppendCustomShaderDiagnostics(builder, configuration, analysis, shaderSupport, writeResult);
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

    private static void AppendCustomShaderDiagnostics(StringBuilder builder, Configuration configuration, PresetAnalysisResult analysis, ShaderSupportScan shaderSupport, PresetWriteResult writeResult)
    {
        builder.AppendLine("## Dalashade Custom Shader Diagnostics");
        builder.AppendLine();
        builder.AppendLine($"- Custom shader support: {(configuration.EnableDalashadeCustomShaders ? "enabled" : "disabled")}");
        var customItems = shaderSupport.Items
            .Where(item => string.Equals(item.ReasonCategory, CustomShaderVariableMapper.ReasonCategory, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var customChanges = writeResult.Changes
            .Where(change => string.Equals(change.ReasonCategory, CustomShaderVariableMapper.ReasonCategory, StringComparison.OrdinalIgnoreCase))
            .OrderBy(change => change.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var customSections = analysis.Techniques
            .Select(technique => technique.Section)
            .Where(CustomShaderVariableMapper.IsCustomShaderSection)
            .Concat(customItems.Select(item => item.Section))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(section => section, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        builder.AppendLine("- Custom shader sections found:");
        if (customSections.Length == 0)
        {
            builder.AppendLine("  - None");
        }
        else
        {
            foreach (var section in customSections)
            {
                builder.AppendLine($"  - `{section}`");
            }
        }

        builder.AppendLine("- Custom shader variables written:");
        if (customChanges.Length == 0)
        {
            builder.AppendLine("  - None");
        }
        else
        {
            foreach (var change in customChanges)
            {
                builder.AppendLine($"  - `{change.Section}` / `{change.Key}`: {change.OldValue} -> {change.NewValue}");
            }
        }

        builder.AppendLine();
    }

    private static void AppendTagStackDiagnostics(StringBuilder builder, TagStackDiagnostics diagnostics)
    {
        builder.AppendLine("## Scene Tags And Stack Diagnostics");
        builder.AppendLine();
        builder.AppendLine($"- Weather key: {diagnostics.WeatherKey}");
        builder.AppendLine($"- Biome key: {diagnostics.BiomeKey}");
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
            foreach (var contribution in diagnostics.IntentContributions)
            {
                builder.AppendLine($"- {contribution.Intent}: {contribution.Amount:+0.###;-0.###;0} from {contribution.Source} - {contribution.Reason}");
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
