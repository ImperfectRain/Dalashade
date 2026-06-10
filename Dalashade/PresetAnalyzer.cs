using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Dalashade;

public enum EffectRole
{
    Unknown,
    ColorGrade,
    Tonemap,
    Bloom,
    AoGi,
    Sharpen,
    AntiAliasing,
    Deband,
    Clarity,
    Lut,
    Diffusion,
    Dof,
    FilmGrain,
    Vignette,
    UiUtility
}

public enum SupportLevel
{
    Unsupported,
    DetectedOnly,
    PartiallyControlled,
    FullyControlled
}

public enum EffectRisk
{
    Safe,
    Moderate,
    High,
    GPoseOnly,
    UtilityIgnore
}

public enum PresetRiskLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

public enum TechniqueActivationState
{
    Active,
    Inactive,
    Unknown
}

public sealed record PresetTechnique(
    string TechniqueName,
    string ShaderFile,
    string Section,
    TechniqueActivationState ActivationState,
    EffectRole Role,
    EffectRisk Risk,
    SupportLevel SupportLevel);

public sealed record TechniqueActivationMap(bool TechniquesLineFound, IReadOnlySet<string> ActiveTechniqueKeys);

public sealed record EffectAuthority(
    EffectRole Role,
    string PrimaryShader,
    IReadOnlyList<string> SecondaryShaders,
    IReadOnlyList<string> SuppressedOrWarnedShaders);

public sealed record PresetRiskReport(
    PresetRiskLevel Level,
    IReadOnlyList<PresetTechnique> ActiveSupportedEffects,
    IReadOnlyList<PresetTechnique> ActivePartiallySupportedEffects,
    IReadOnlyList<PresetTechnique> ActiveDetectedOnlyEffects,
    IReadOnlyList<PresetTechnique> ActiveUnsupportedEffects,
    IReadOnlyList<PresetTechnique> HighRiskActiveEffects,
    IReadOnlyList<PresetTechnique> InactiveSupportedEffects,
    IReadOnlyList<string> MultipleAuthorityWarnings,
    PresetCompatibilityMode RecommendedCompatibilityMode,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EffectAuthority> Authorities)
{
    public static PresetRiskReport Empty { get; } = new(
        PresetRiskLevel.Low,
        Array.Empty<PresetTechnique>(),
        Array.Empty<PresetTechnique>(),
        Array.Empty<PresetTechnique>(),
        Array.Empty<PresetTechnique>(),
        Array.Empty<PresetTechnique>(),
        Array.Empty<PresetTechnique>(),
        Array.Empty<string>(),
        PresetCompatibilityMode.AdaptiveBalanced,
        Array.Empty<string>(),
        Array.Empty<EffectAuthority>());
}

public sealed record PresetAnalysisResult(
    bool Success,
    string Message,
    IReadOnlyList<PresetTechnique> Techniques,
    PresetRiskReport Report)
{
    public static PresetAnalysisResult Skipped(string message) => new(false, message, Array.Empty<PresetTechnique>(), PresetRiskReport.Empty);
}

public sealed record TechniqueEntry(string TechniqueName, string ShaderFile)
{
    public string Section => ShaderFile;
    public string DisplayName => string.IsNullOrWhiteSpace(TechniqueName) ? ShaderFile : $"{TechniqueName}@{ShaderFile}";
}

public sealed class PresetAnalyzer
{
    private static readonly EffectRole[] AuthorityRoles =
    {
        EffectRole.ColorGrade,
        EffectRole.Bloom,
        EffectRole.Sharpen,
        EffectRole.AoGi,
        EffectRole.Diffusion,
        EffectRole.Dof,
        EffectRole.FilmGrain,
        EffectRole.Vignette,
        EffectRole.Lut,
        EffectRole.Deband,
        EffectRole.AntiAliasing
    };

    private readonly ShaderVariableMapper mapper = new();

    public PresetAnalysisResult Analyze(Configuration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.BasePresetPath))
            {
                return PresetAnalysisResult.Skipped("Base preset path is empty.");
            }

            var basePresetPath = Path.GetFullPath(configuration.BasePresetPath);
            if (!File.Exists(basePresetPath))
            {
                return PresetAnalysisResult.Skipped("Base preset was not found.");
            }

            var lines = File.ReadAllLines(basePresetPath);
            var definitions = mapper.CreateDefinitions(configuration);
            var controlledSections = definitions
                .Where(definition => !string.IsNullOrWhiteSpace(definition.Section))
                .GroupBy(definition => definition.Section!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            var sections = ParseSections(lines);
            var activeEntries = ParseTechniqueEntries(lines, "Techniques");
            var sortedEntries = ParseTechniqueEntries(lines, "TechniqueSorting");
            var activationKnown = ContainsPresetKey(lines, "Techniques");
            var activeKeys = activeEntries.SelectMany(GetTechniqueKeys).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var entries = MergeTechniqueEntries(activeEntries, sortedEntries, sections);
            var techniques = entries
                .Select(entry => ClassifyTechnique(entry, GetTechniqueActivationState(activationKnown, activeKeys, entry), controlledSections))
                .GroupBy(TechniqueDedupeKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(technique => ActivationSortOrder(technique.ActivationState))
                    .ThenBy(technique => technique.SupportLevel)
                    .First())
                .OrderByDescending(technique => ActivationSortOrder(technique.ActivationState))
                .ThenBy(technique => technique.Role)
                .ThenBy(technique => technique.ShaderFile, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var report = BuildReport(techniques, lines, configuration.CompatibilityMode);

            return new PresetAnalysisResult(
                true,
                $"Preset risk: {report.Level}. Recommended mode: {FormatCompatibilityMode(report.RecommendedCompatibilityMode)}.",
                techniques,
                report);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return PresetAnalysisResult.Skipped($"Preset analysis failed: {ex.Message}");
        }
    }

    public static TechniqueActivationMap ParseTechniqueActivationMap(IEnumerable<string> lines)
    {
        var activeTechniques = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in ParseTechniqueEntries(lines, "Techniques"))
        {
            activeTechniques.Add(entry.DisplayName);
            activeTechniques.Add(entry.TechniqueName);
            activeTechniques.Add(entry.ShaderFile);
        }

        return new TechniqueActivationMap(ContainsPresetKey(lines, "Techniques"), activeTechniques);
    }

    public static TechniqueActivationState GetTechniqueActivationState(TechniqueActivationMap activationMap, string section)
    {
        if (!activationMap.TechniquesLineFound || string.IsNullOrWhiteSpace(section))
        {
            return TechniqueActivationState.Unknown;
        }

        return activationMap.ActiveTechniqueKeys.Contains(section)
            ? TechniqueActivationState.Active
            : TechniqueActivationState.Inactive;
    }

    private static TechniqueActivationState GetTechniqueActivationState(bool activationKnown, IReadOnlySet<string> activeKeys, TechniqueEntry entry)
    {
        if (!activationKnown)
        {
            return TechniqueActivationState.Unknown;
        }

        return activeKeys.Contains(entry.DisplayName) || activeKeys.Contains(entry.ShaderFile) || activeKeys.Contains(entry.TechniqueName)
            ? TechniqueActivationState.Active
            : TechniqueActivationState.Inactive;
    }

    private static PresetTechnique ClassifyTechnique(TechniqueEntry entry, TechniqueActivationState activationState, IReadOnlyDictionary<string, int> controlledSections)
    {
        var role = ClassifyRole(entry);
        var risk = ClassifyRisk(entry, role);
        var support = ClassifySupport(entry, role, controlledSections);

        return new PresetTechnique(entry.TechniqueName, entry.ShaderFile, entry.Section, activationState, role, risk, support);
    }

    private static EffectRole ClassifyRole(TechniqueEntry entry)
    {
        var text = $"{entry.TechniqueName} {entry.ShaderFile}".ToLowerInvariant();

        if (ContainsAny(text, "keepui", "restoreui", "launchpad", "insight", "displaydepth", "chromakey", "splitscreen", "aspectratio", "composition", "clipboard", "verticalpreviewer", "uimask", "crashpad"))
        {
            return EffectRole.UiUtility;
        }

        if (ContainsAny(text, "dof", "depthoffield", "depth_of_field", "tiltshift"))
        {
            return EffectRole.Dof;
        }

        if (ContainsAny(text, "filmgrain", "simplegrain", "smartnoise", "gr8mmfilm", "noise"))
        {
            return EffectRole.FilmGrain;
        }

        if (text.Contains("vignette"))
        {
            return EffectRole.Vignette;
        }

        if (ContainsAny(text, "lensdiffusion", "solaris", "exposurefusion", "ambientlight", "halation", "dehaze", "blooming_hdr", "watchdogs", "reflectivebump", "localcontrast"))
        {
            return EffectRole.Diffusion;
        }

        if (ContainsAny(text, "bloom", "magicbloom", "gaussian", "fftbloom", "convolutionbloom", "neobloom", "pirate_bloom", "ambientlight", "arcane"))
        {
            return EffectRole.Bloom;
        }

        if (ContainsAny(text, "mxao", "ssao", "ssdo", "rtgi", "newgi", "specgi", "xegtao", "gi", "hbao", "qmxao"))
        {
            return EffectRole.AoGi;
        }

        if (ContainsAny(text, "smaa", "fxaa", "taa", "nfaa", "dlaa", "anti_alias", "antialias", "biaa"))
        {
            return EffectRole.AntiAliasing;
        }

        if (ContainsAny(text, "deband", "debnd", "undither"))
        {
            return EffectRole.Deband;
        }

        if (ContainsAny(text, "sharp", "cas", "finesharp"))
        {
            return EffectRole.Sharpen;
        }

        if (text.Contains("clarity"))
        {
            return EffectRole.Clarity;
        }

        if (ContainsAny(text, "lut", "multilut", "colorlookup"))
        {
            return EffectRole.Lut;
        }

        if (ContainsAny(text, "tonemap", "reinhard", "filmicpass", "filmicgrade", "hdr", "eyeadaption", "fakehdr"))
        {
            return EffectRole.Tonemap;
        }

        if (ContainsAny(text, "regrade", "lightroom", "technicolor", "dpx", "vibrance", "colourfulness", "levels", "curves", "liftgammagain", "color", "tint", "sepia", "prod80", "pd80", "adaptivecolorgrading", "adaptivegrading"))
        {
            return EffectRole.ColorGrade;
        }

        return EffectRole.Unknown;
    }

    private static EffectRisk ClassifyRisk(TechniqueEntry entry, EffectRole role)
    {
        var text = $"{entry.TechniqueName} {entry.ShaderFile}".ToLowerInvariant();

        if (role == EffectRole.UiUtility)
        {
            return EffectRisk.UtilityIgnore;
        }

        if (role == EffectRole.Dof)
        {
            return EffectRisk.GPoseOnly;
        }

        if (ContainsAny(text, "regrade+", "technicolor", "lensdiffusion", "solaris", "exposurefusion", "adaptivetint", "adaptivecolorgrading", "watchdogs", "chromaticaberration", "prism", "reflectivebump", "hslshift", "huefx"))
        {
            return EffectRisk.High;
        }

        if (ContainsAny(text, "ambientlight", "dpx", "colourfulness", "vibrance", "gaussian", "bloominghdr", "localcontrast", "filmgrain", "vignette", "ssdo", "ssao", "mxao 3.4", "mxao 4"))
        {
            return EffectRisk.Moderate;
        }

        return EffectRisk.Safe;
    }

    private static SupportLevel ClassifySupport(TechniqueEntry entry, EffectRole role, IReadOnlyDictionary<string, int> controlledSections)
    {
        if (role == EffectRole.UiUtility)
        {
            return SupportLevel.DetectedOnly;
        }

        if (controlledSections.TryGetValue(entry.Section, out var count))
        {
            return count >= 3 ? SupportLevel.FullyControlled : SupportLevel.PartiallyControlled;
        }

        if (IsKnownDetectedOnly(entry, role))
        {
            return SupportLevel.DetectedOnly;
        }

        return SupportLevel.Unsupported;
    }

    private static bool IsKnownDetectedOnly(TechniqueEntry entry, EffectRole role)
    {
        if (role != EffectRole.Unknown)
        {
            return true;
        }

        var text = $"{entry.TechniqueName} {entry.ShaderFile}".ToLowerInvariant();
        return ContainsAny(text, "ffxiv", "marty", "quint", "prod80", "pd80", "pirate", "ppfx", "dh_");
    }

    private static PresetRiskReport BuildReport(IReadOnlyList<PresetTechnique> techniques, IReadOnlyList<string> lines, PresetCompatibilityMode mode)
    {
        var active = techniques.Where(technique => technique.ActivationState == TechniqueActivationState.Active).ToArray();
        var activeSupported = active.Where(technique => technique.SupportLevel == SupportLevel.FullyControlled).ToArray();
        var activePartial = active.Where(technique => technique.SupportLevel == SupportLevel.PartiallyControlled).ToArray();
        var activeDetectedOnly = active.Where(technique => technique.SupportLevel == SupportLevel.DetectedOnly).ToArray();
        var activeUnsupported = active.Where(technique => technique.SupportLevel == SupportLevel.Unsupported).ToArray();
        var highRiskActive = DeduplicateTechniques(active.Where(technique => technique.Risk is EffectRisk.High or EffectRisk.GPoseOnly)).ToArray();
        var inactiveSupported = techniques
            .Where(technique => technique.ActivationState != TechniqueActivationState.Active && technique.SupportLevel is SupportLevel.FullyControlled or SupportLevel.PartiallyControlled)
            .GroupBy(TechniqueDedupeKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        var authorities = BuildAuthorities(active);
        var multipleAuthorityWarnings = BuildMultipleAuthorityWarnings(active, mode);
        var warnings = BuildWarnings(active, activeUnsupported, highRiskActive, multipleAuthorityWarnings, lines, mode);
        var level = ScoreRiskLevel(active, activeUnsupported, highRiskActive, multipleAuthorityWarnings, warnings);
        var recommended = RecommendMode(level, active);

        return new PresetRiskReport(
            level,
            DeduplicateTechniques(activeSupported).ToArray(),
            DeduplicateTechniques(activePartial).ToArray(),
            DeduplicateTechniques(activeDetectedOnly).ToArray(),
            DeduplicateTechniques(activeUnsupported).ToArray(),
            highRiskActive,
            inactiveSupported,
            multipleAuthorityWarnings,
            recommended,
            warnings,
            authorities);
    }

    private static IReadOnlyList<EffectAuthority> BuildAuthorities(IReadOnlyList<PresetTechnique> active)
    {
        var authorities = new List<EffectAuthority>();
        foreach (var role in AuthorityRoles)
        {
            var roleTechniques = active.Where(technique => technique.Role == role).ToArray();
            if (roleTechniques.Length == 0)
            {
                continue;
            }

            var ordered = roleTechniques
                .OrderBy(technique => AuthorityPriority(technique))
                .ThenBy(technique => technique.ShaderFile, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var primary = ordered[0];
            var secondary = ordered.Skip(1)
                .Select(FormatTechnique)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var warned = ordered
                .Where(technique => technique.Risk is EffectRisk.High or EffectRisk.GPoseOnly || technique.SupportLevel == SupportLevel.Unsupported)
                .Select(FormatTechnique)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            authorities.Add(new EffectAuthority(role, FormatTechnique(primary), secondary, warned));
        }

        return authorities;
    }

    private static int AuthorityPriority(PresetTechnique technique)
    {
        var text = $"{technique.TechniqueName} {technique.ShaderFile}".ToLowerInvariant();
        var supportOffset = technique.SupportLevel switch
        {
            SupportLevel.FullyControlled => 0,
            SupportLevel.PartiallyControlled => 10,
            SupportLevel.DetectedOnly => 30,
            _ => 50
        };

        var shaderOffset = 20;
        if (ContainsAny(text, "martysmods_regrade", "martysmods_regrade", "magicbloom", "martysmods_fftbloom", "martysmods_mxao", "martysmods_rtgi", "martysmods_sharpen", "martysmods_smaa", "deband.fx", "quint_lightroom", "multilut", "lut.fx"))
        {
            shaderOffset = 0;
        }
        else if (ContainsAny(text, "lightroom", "gaussianbloom", "filmicsharpen", "finesharp", "lumasharpen", "cas", "colourfulness", "vibrance", "dpx"))
        {
            shaderOffset = 8;
        }

        return supportOffset + shaderOffset;
    }

    private static IReadOnlyList<string> BuildMultipleAuthorityWarnings(IReadOnlyList<PresetTechnique> active, PresetCompatibilityMode mode)
    {
        var warnings = new List<string>();
        foreach (var role in AuthorityRoles)
        {
            var count = active.Count(technique => technique.Role == role && technique.Risk != EffectRisk.UtilityIgnore);
            if (count > 1)
            {
                var policy = CompatibilityRolePolicies.Get(role);
                if (policy == null)
                {
                    warnings.Add($"Multiple active {FormatRole(role)} effects ({count}) may compete for the same visual role.");
                }
                else if (policy.MultipleActiveEffectsAllowed)
                {
                    warnings.Add($"Multiple active {FormatRole(role)} effects ({count}) are allowed by the selected compatibility policy; review the stack if the look feels too heavy.");
                }
                else if (mode == PresetCompatibilityMode.GameplaySanitize && policy.GameplaySanitizeMayReduce)
                {
                    warnings.Add($"Multiple active {FormatRole(role)} effects ({count}) may compete for the same visual role; Gameplay sanitize can dampen secondary adjustments.");
                }
                else
                {
                    warnings.Add($"Multiple active {FormatRole(role)} effects ({count}) may compete for the same visual role.");
                }
            }
        }

        return warnings;
    }

    private static IEnumerable<PresetTechnique> DeduplicateTechniques(IEnumerable<PresetTechnique> techniques)
    {
        return techniques
            .GroupBy(TechniqueDedupeKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
    }

    private static string TechniqueDedupeKey(PresetTechnique technique)
    {
        return $"{technique.TechniqueName}\u001f{technique.ShaderFile}";
    }

    private static IReadOnlyList<string> BuildWarnings(
        IReadOnlyList<PresetTechnique> active,
        IReadOnlyList<PresetTechnique> activeUnsupported,
        IReadOnlyList<PresetTechnique> highRiskActive,
        IReadOnlyList<string> multipleAuthorityWarnings,
        IReadOnlyList<string> lines,
        PresetCompatibilityMode mode)
    {
        var warnings = new List<string>();

        if (activeUnsupported.Count > 0)
        {
            warnings.Add($"{activeUnsupported.Count} active effect(s) are not classified as controllable yet.");
            foreach (var group in activeUnsupported.GroupBy(technique => technique.Role).OrderBy(group => group.Key))
            {
                if (CompatibilityRolePolicies.TryGet(group.Key, out var policy) && policy.UnsupportedActiveEffectsWarnOnly)
                {
                    warnings.Add($"{group.Count()} unsupported active {FormatRole(group.Key)} effect(s) are warn-only under the selected role policy.");
                }
                else if (mode == PresetCompatibilityMode.GameplaySanitize
                         && CompatibilityRolePolicies.TryGet(group.Key, out var sanitizePolicy)
                         && sanitizePolicy.GameplaySanitizeMayReduce)
                {
                    warnings.Add($"{group.Count()} unsupported active {FormatRole(group.Key)} effect(s) may still dominate the image; Gameplay sanitize can only reduce controlled secondary adjustments.");
                }
            }
        }

        foreach (var technique in highRiskActive.Take(8))
        {
            if (mode == PresetCompatibilityMode.GposePreserve
                && CompatibilityRolePolicies.TryGet(technique.Role, out var policy)
                && policy.GposePreserveLeavesAlone)
            {
                warnings.Add($"{FormatTechnique(technique)} is {FormatRisk(technique.Risk)} for gameplay-style adaptation, but GPose preserve leaves this role alone.");
            }
            else
            {
                warnings.Add($"{FormatTechnique(technique)} is {FormatRisk(technique.Risk)} for gameplay-style adaptation.");
            }
        }

        warnings.AddRange(multipleAuthorityWarnings);
        warnings.AddRange(FindReGradePlusRiskWarnings(lines));

        if (active.Count == 0 && lines.All(line => !IsPresetKey(line, "Techniques")))
        {
            warnings.Add("Active technique state could not be confirmed because the preset does not contain a Techniques= line.");
        }

        if (active.Count > 10)
        {
            warnings.Add($"Preset has {active.Count} active effects; broad stacks should be reviewed before sanitize modes are added.");
        }

        return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static PresetRiskLevel ScoreRiskLevel(
        IReadOnlyList<PresetTechnique> active,
        IReadOnlyList<PresetTechnique> activeUnsupported,
        IReadOnlyList<PresetTechnique> highRiskActive,
        IReadOnlyList<string> multipleAuthorityWarnings,
        IReadOnlyList<string> warnings)
    {
        var score = 0;
        score += activeUnsupported.Count(technique => technique.Role is EffectRole.ColorGrade or EffectRole.Tonemap or EffectRole.Diffusion) * 2;
        score += highRiskActive.Count * 2;
        score += multipleAuthorityWarnings.Count;
        score += warnings.Count(warning => warning.Contains("ReGrade+", StringComparison.OrdinalIgnoreCase)) * 2;
        score += Math.Min(3, active.Count(technique => technique.SupportLevel == SupportLevel.DetectedOnly && technique.Risk == EffectRisk.Moderate));
        if (active.Count > 10)
        {
            score++;
        }

        return score switch
        {
            >= 10 => PresetRiskLevel.VeryHigh,
            >= 6 => PresetRiskLevel.High,
            >= 3 => PresetRiskLevel.Medium,
            _ => PresetRiskLevel.Low
        };
    }

    private static PresetCompatibilityMode RecommendMode(PresetRiskLevel level, IReadOnlyList<PresetTechnique> active)
    {
        if (active.Any(technique => technique.Risk == EffectRisk.GPoseOnly) && level >= PresetRiskLevel.High)
        {
            return PresetCompatibilityMode.GposePreserve;
        }

        return level switch
        {
            PresetRiskLevel.Low => PresetCompatibilityMode.AdaptiveBalanced,
            PresetRiskLevel.Medium => PresetCompatibilityMode.AdaptiveBalanced,
            PresetRiskLevel.High => PresetCompatibilityMode.GameplaySanitize,
            PresetRiskLevel.VeryHigh => PresetCompatibilityMode.GameplaySanitize,
            _ => PresetCompatibilityMode.AdaptiveBalanced
        };
    }

    private static IReadOnlyList<string> FindReGradePlusRiskWarnings(IReadOnlyList<string> lines)
    {
        var warnings = new List<string>();
        var currentSection = string.Empty;
        foreach (var line in lines)
        {
            if (TryReadSection(line, out var section))
            {
                currentSection = section;
                continue;
            }

            if (!currentSection.Equals("MartysMods_REGRADE+.fx", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var rawValue = line[(separatorIndex + 1)..].Trim();
            if (IsRiskyReGradePlusScalar(key, rawValue, out var scalarWarning))
            {
                warnings.Add(scalarWarning);
            }
            else if (IsRiskyReGradePlusVector(key, rawValue, out var vectorWarning))
            {
                warnings.Add(vectorWarning);
            }
        }

        return warnings;
    }

    private static bool IsRiskyReGradePlusScalar(string key, string rawValue, out string warning)
    {
        warning = string.Empty;
        var riskyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "E_SHADOWS_HUE",
            "E_SHADOWS_SAT",
            "E_MIDTONES_HUE",
            "E_MIDTONES_SAT",
            "E_HIGHLIGHTS_HUE",
            "E_HIGHLIGHTS_SAT"
        };

        if (!riskyKeys.Contains(key) || !float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var threshold = key.EndsWith("_SAT", StringComparison.OrdinalIgnoreCase) ? 0.20f : 0.05f;
        if (MathF.Abs(value) <= threshold)
        {
            return false;
        }

        warning = $"ReGrade+ {key} is {value:0.###}; strong tonal hue/saturation shifts can cause color casts.";
        return true;
    }

    private static bool IsRiskyReGradePlusVector(string key, string rawValue, out string warning)
    {
        warning = string.Empty;
        if (!key.StartsWith("E_COLORISTA_HSL_", StringComparison.OrdinalIgnoreCase) || !key.EndsWith("_V2", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var trimmed = rawValue.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '(' && trimmed[^1] == ')')
        {
            trimmed = trimmed[1..^1];
        }

        var values = trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0f)
            .ToArray();
        if (values.Length == 0 || values.All(value => MathF.Abs(value) <= 0.05f))
        {
            return false;
        }

        warning = $"ReGrade+ {key} has non-neutral Colorista HSL values; compatibility mode can reduce this value during generation.";
        return true;
    }

    private static IReadOnlyList<TechniqueEntry> MergeTechniqueEntries(
        IReadOnlyList<TechniqueEntry> activeEntries,
        IReadOnlyList<TechniqueEntry> sortedEntries,
        IReadOnlySet<string> sections)
    {
        var entries = new List<TechniqueEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in activeEntries.Concat(sortedEntries))
        {
            if (seen.Add(entry.DisplayName))
            {
                entries.Add(entry);
            }
        }

        foreach (var section in sections)
        {
            if (seen.Add(section))
            {
                entries.Add(new TechniqueEntry(Path.GetFileNameWithoutExtension(section), section));
            }
        }

        return entries;
    }

    private static IEnumerable<string> GetTechniqueKeys(TechniqueEntry entry)
    {
        yield return entry.DisplayName;
        yield return entry.TechniqueName;
        yield return entry.ShaderFile;
    }

    private static HashSet<string> ParseSections(IEnumerable<string> lines)
    {
        var sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            if (TryReadSection(line, out var section))
            {
                sections.Add(section);
            }
        }

        return sections;
    }

    private static IReadOnlyList<TechniqueEntry> ParseTechniqueEntries(IEnumerable<string> lines, string targetKey)
    {
        foreach (var line in lines)
        {
            if (!IsPresetKey(line, targetKey, out var separatorIndex))
            {
                continue;
            }

            return line[(separatorIndex + 1)..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseTechniqueEntry)
                .Where(entry => !string.IsNullOrWhiteSpace(entry.ShaderFile))
                .ToArray();
        }

        return Array.Empty<TechniqueEntry>();
    }

    private static TechniqueEntry ParseTechniqueEntry(string value)
    {
        var shaderSeparator = value.LastIndexOf('@');
        if (shaderSeparator >= 0 && shaderSeparator < value.Length - 1)
        {
            return new TechniqueEntry(value[..shaderSeparator].Trim(), value[(shaderSeparator + 1)..].Trim());
        }

        var trimmed = value.Trim();
        return new TechniqueEntry(Path.GetFileNameWithoutExtension(trimmed), trimmed);
    }

    private static bool ContainsPresetKey(IEnumerable<string> lines, string targetKey)
    {
        return lines.Any(line => IsPresetKey(line, targetKey));
    }

    private static bool IsPresetKey(string line, string targetKey)
    {
        return IsPresetKey(line, targetKey, out _);
    }

    private static bool IsPresetKey(string line, string targetKey, out int separatorIndex)
    {
        separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return false;
        }

        var key = line[..separatorIndex].Trim();
        return string.Equals(key, targetKey, StringComparison.OrdinalIgnoreCase);
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

    public static string FormatRole(EffectRole role)
    {
        return role switch
        {
            EffectRole.AoGi => "AO/GI",
            EffectRole.AntiAliasing => "Anti-aliasing",
            EffectRole.ColorGrade => "Color grade",
            EffectRole.UiUtility => "UI/Utility",
            EffectRole.Dof => "DOF",
            EffectRole.Lut => "LUT",
            _ => role.ToString()
        };
    }

    public static string FormatCompatibilityMode(PresetCompatibilityMode mode)
    {
        return mode switch
        {
            PresetCompatibilityMode.PreserveBase => "Preserve base",
            PresetCompatibilityMode.AdaptiveBalanced => "Adaptive balanced",
            PresetCompatibilityMode.GameplaySanitize => "Gameplay sanitize",
            PresetCompatibilityMode.CinematicPreserve => "Cinematic preserve",
            PresetCompatibilityMode.GposePreserve => "GPose preserve",
            _ => mode.ToString()
        };
    }

    public static string FormatTechnique(PresetTechnique technique)
    {
        return string.IsNullOrWhiteSpace(technique.TechniqueName)
            ? technique.ShaderFile
            : $"{technique.TechniqueName}@{technique.ShaderFile}";
    }

    public static string FormatRisk(EffectRisk risk)
    {
        return risk switch
        {
            EffectRisk.GPoseOnly => "GPose-only",
            EffectRisk.UtilityIgnore => "utility/ignored",
            _ => risk.ToString().ToLowerInvariant()
        };
    }

    public static string FormatActivationState(TechniqueActivationState activationState)
    {
        return activationState.ToString().ToLowerInvariant();
    }

    private static int ActivationSortOrder(TechniqueActivationState activationState)
    {
        return activationState switch
        {
            TechniqueActivationState.Active => 2,
            TechniqueActivationState.Unknown => 1,
            _ => 0
        };
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}
