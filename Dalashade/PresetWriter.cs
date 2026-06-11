using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalashade;

public sealed record ChangedShaderVariable(
    string Section,
    string Key,
    string OldValue,
    string NewValue,
    string ReasonCategory,
    TechniqueActivationState ActivationState,
    bool HitMin,
    bool HitMax,
    float AuthorityAdjustmentStrength = 1f,
    string? Warning = null);

public sealed record PresetWriteResult(
    bool Success,
    string Message,
    int ChangedVariables,
    IReadOnlyList<ChangedShaderVariable> Changes,
    IReadOnlyList<SanitizedShaderVariable> SanitizeActions,
    CustomShaderInjectionResult CustomShaderInjection)
{
    public static PresetWriteResult Skipped(string message) => new(false, message, 0, Array.Empty<ChangedShaderVariable>(), Array.Empty<SanitizedShaderVariable>(), CustomShaderInjectionResult.Skipped);
}

public sealed record CustomShaderInjectionResult(
    bool Attempted,
    bool GeneratedPresetOnly,
    bool SectionInjected,
    bool VariablesInjected,
    bool TechniqueInjected,
    string Message,
    IReadOnlyList<string> Sections,
    IReadOnlyList<string> Variables,
    IReadOnlyList<string> Techniques)
{
    public static CustomShaderInjectionResult Skipped { get; } = new(false, false, false, false, false, "Custom shader section injection not attempted.", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
}

public sealed record ShaderSupportItem(string Section, string Key, bool Controllable, string ReasonCategory, TechniqueActivationState ActivationState);

public sealed record ShaderSupportScan(bool Success, string Message, IReadOnlyList<ShaderSupportItem> Items)
{
    public static ShaderSupportScan Skipped(string message) => new(false, message, Array.Empty<ShaderSupportItem>());
}

public sealed class PresetWriter
{
    private sealed record KnownCustomShaderDefinition(string Section, string Technique, string TechniqueEntry, IReadOnlyList<string> Variables);

    private static readonly IReadOnlyList<KnownCustomShaderDefinition> KnownCustomShaders =
    [
        new(
            "Dalashade_WeatherAtmosphere.fx",
            "Dalashade_WeatherAtmosphere",
            "Dalashade_WeatherAtmosphere@Dalashade_WeatherAtmosphere.fx",
            [
                "Dalashade_Haze",
                "Dalashade_Wetness",
                "Dalashade_Cold",
                "Dalashade_Heat",
                "Dalashade_HighlightProtection",
                "Dalashade_ShadowProtection",
                "Dalashade_CombatPressure",
                "Dalashade_Atmosphere",
                "Dalashade_MagicGlow",
                "Dalashade_NeonGlow",
                "Dalashade_Readability",
                "Dalashade_CinematicPermission"
            ]),
        new(
            "Dalashade_AdaptiveGrade.fx",
            "Dalashade_AdaptiveGrade",
            "Dalashade_AdaptiveGrade@Dalashade_AdaptiveGrade.fx",
            [
                "Dalashade_Readability",
                "Dalashade_Atmosphere",
                "Dalashade_HighlightProtection",
                "Dalashade_ShadowProtection",
                "Dalashade_Cold",
                "Dalashade_Heat",
                "Dalashade_MagicGlow",
                "Dalashade_NeonGlow",
                "Dalashade_CinematicPermission",
                "Dalashade_CombatPressure"
            ]),
        new(
            "Dalashade_SmartSharpen.fx",
            "Dalashade_SmartSharpen",
            "Dalashade_SmartSharpen@Dalashade_SmartSharpen.fx",
            [
                "Dalashade_Readability",
                "Dalashade_Haze",
                "Dalashade_Wetness",
                "Dalashade_FoliageDensity",
                "Dalashade_CombatPressure",
                "Dalashade_HighlightProtection"
            ]),
        new(
            "Dalashade_AtmosphereBloom.fx",
            "Dalashade_AtmosphereBloom",
            "Dalashade_AtmosphereBloom@Dalashade_AtmosphereBloom.fx",
            [
                "Dalashade_Atmosphere",
                "Dalashade_MagicGlow",
                "Dalashade_NeonGlow",
                "Dalashade_HighlightProtection",
                "Dalashade_CombatPressure",
                "Dalashade_CinematicPermission"
            ])
    ];

    private readonly ShaderVariableMapper mapper = new();
    private readonly CustomShaderVariableMapper customMapper = new();
    private readonly PresetAnalyzer analyzer = new();
    private readonly SanitizeActionPipeline sanitizeActionPipeline = new();

    public PresetWriteResult WriteGeneratedPreset(Configuration configuration, VisualProfile profile, SceneIntent? sceneIntent = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.BasePresetPath))
            {
                return PresetWriteResult.Skipped("Base preset path is empty.");
            }

            if (string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath))
            {
                return PresetWriteResult.Skipped("Generated preset path is empty.");
            }

            var basePresetPath = Path.GetFullPath(configuration.BasePresetPath);
            var generatedPresetPath = Path.GetFullPath(configuration.GeneratedPresetPath);

            if (string.Equals(basePresetPath, generatedPresetPath, StringComparison.OrdinalIgnoreCase))
            {
                return PresetWriteResult.Skipped("Generated preset path must be different from the base preset path.");
            }

            if (!File.Exists(basePresetPath))
            {
                return PresetWriteResult.Skipped("Base preset was not found.");
            }

            var generatedDirectory = Path.GetDirectoryName(generatedPresetPath);
            if (string.IsNullOrWhiteSpace(generatedDirectory))
            {
                return PresetWriteResult.Skipped("Generated preset path is invalid.");
            }

            Directory.CreateDirectory(generatedDirectory);

            var lines = File.ReadAllLines(basePresetPath).ToList();
            var injectionResult = InjectCustomShaderSections(configuration, lines);
            var analysis = analyzer.Analyze(configuration);
            var authorityPolicy = GenerationAuthorityPolicy.From(analysis, configuration.CompatibilityMode);
            var adjustments = mapper.CreateAdjustments(profile, configuration, authorityPolicy);
            sceneIntent ??= SceneIntent.Neutral;
            var activationMap = PresetAnalyzer.ParseTechniqueActivationMap(lines);
            var changes = new List<ChangedShaderVariable>();
            var currentSection = string.Empty;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (TryReadSection(line, out var section))
                {
                    currentSection = section;
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var generatedCustomShaderVariable = IsGeneratedCustomShaderVariable(injectionResult, currentSection, key);
                if (!TryGetAdjustment(adjustments, configuration.ShaderMatchingMode, currentSection, key, out var adjust)
                    && !customMapper.TryGetAdjustment(configuration, currentSection, key, sceneIntent, out adjust))
                {
                    continue;
                }

                var activationState = PresetAnalyzer.GetTechniqueActivationState(activationMap, currentSection);
                if (activationState != TechniqueActivationState.Active
                    && configuration.InactiveShaderWriteMode == InactiveShaderWriteMode.Never
                    && !generatedCustomShaderVariable)
                {
                    continue;
                }

                if (activationState == TechniqueActivationState.Inactive
                    && configuration.InactiveShaderWriteMode == InactiveShaderWriteMode.SupportedInactiveSections
                    && !HasExactAdjustment(adjustments, currentSection, key)
                    && !generatedCustomShaderVariable)
                {
                    continue;
                }

                if (activationState == TechniqueActivationState.Unknown
                    && !HasExactAdjustment(adjustments, currentSection, key)
                    && !generatedCustomShaderVariable)
                {
                    continue;
                }

                var currentValue = line[(separatorIndex + 1)..];
                var adjusted = adjust.Apply(currentValue);
                if (adjusted == null)
                {
                    continue;
                }

                var adjustedValue = adjusted.NewValue;
                lines[i] = $"{line[..(separatorIndex + 1)]}{adjustedValue}";
                if (!string.Equals(currentValue, adjustedValue, StringComparison.Ordinal))
                {
                    changes.Add(new ChangedShaderVariable(
                        currentSection,
                        key,
                        currentValue,
                        adjustedValue,
                        adjust.ReasonCategory,
                        activationState,
                        adjusted.HitMin,
                        adjusted.HitMax,
                        adjust.AuthorityAdjustmentStrength,
                        CombineWarnings(adjusted.Warning, activationState == TechniqueActivationState.Unknown ? "Active technique state could not be confirmed." : null)));
                }
            }

            var writableLines = lines.ToArray();
            var sanitizeChanges = sanitizeActionPipeline.Apply(writableLines, configuration, activationMap, authorityPolicy);

            if (configuration.WriteBackups && File.Exists(generatedPresetPath))
            {
                var backupPath = CreateBackupPath(generatedPresetPath);
                File.Copy(generatedPresetPath, backupPath, false);
                PruneBackups(generatedPresetPath, configuration.MaxGeneratedPresetBackups);
            }

            var tempPath = $"{generatedPresetPath}.tmp";
            File.WriteAllLines(tempPath, writableLines);
            ReplaceFile(tempPath, generatedPresetPath);

            var inactiveChanges = changes.Count(change => change.ActivationState == TechniqueActivationState.Inactive);
            var unknownChanges = changes.Count(change => change.ActivationState == TechniqueActivationState.Unknown);
            var clampedChanges = changes.Count(change => change.HitMin || change.HitMax);
            var warningChanges = changes.Count(change => !string.IsNullOrWhiteSpace(change.Warning));
            var dampenedChanges = changes.Count(change => change.AuthorityAdjustmentStrength < 0.999f);
            var sanitizeChangeCount = sanitizeChanges.Count;
            var message = $"Generated preset written with {changes.Count} supported variable change(s).";
            if (inactiveChanges > 0 || unknownChanges > 0 || clampedChanges > 0 || warningChanges > 0 || dampenedChanges > 0 || sanitizeChangeCount > 0)
            {
                message += $" {inactiveChanges} inactive, {unknownChanges} unknown, {clampedChanges} clamped, {warningChanges} warning(s), {dampenedChanges} authority-dampened, {sanitizeChangeCount} sanitize action(s).";
            }

            if (injectionResult.Attempted)
            {
                message += $" {injectionResult.Message}";
            }

            return new PresetWriteResult(true, message, changes.Count, changes, sanitizeChanges, injectionResult);
        }
        catch (UnauthorizedAccessException ex)
        {
            return PresetWriteResult.Skipped($"Preset write denied: {ex.Message} Choose a generated preset path in a writable folder, not the protected game/ReShade install folder.");
        }
        catch (IOException ex)
        {
            return PresetWriteResult.Skipped($"Preset write failed: {ex.Message}");
        }
    }

    public ShaderSupportScan ScanSupportedVariables(Configuration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.BasePresetPath))
            {
                return ShaderSupportScan.Skipped("Base preset path is empty.");
            }

            var basePresetPath = Path.GetFullPath(configuration.BasePresetPath);
            if (!File.Exists(basePresetPath))
            {
                return ShaderSupportScan.Skipped("Base preset was not found.");
            }

            var adjustments = mapper.CreateAdjustments(VisualProfile.Neutral, configuration);
            var lines = File.ReadAllLines(basePresetPath);
            var activationMap = PresetAnalyzer.ParseTechniqueActivationMap(lines);
            var items = new List<ShaderSupportItem>();
            var seen = new HashSet<ShaderVariableKey>(ShaderVariableKeyComparer.Instance);
            var currentSection = string.Empty;

            foreach (var line in lines)
            {
                if (TryReadSection(line, out var section))
                {
                    currentSection = section;
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                if (!TryGetAdjustment(adjustments, configuration.ShaderMatchingMode, currentSection, key, out var adjust)
                    && !customMapper.TryGetAdjustment(configuration, currentSection, key, SceneIntent.Neutral, out adjust))
                {
                    continue;
                }

                var itemKey = new ShaderVariableKey(currentSection, key);
                if (!seen.Add(itemKey))
                {
                    continue;
                }

                items.Add(new ShaderSupportItem(
                    currentSection,
                    key,
                    true,
                    adjust.ReasonCategory,
                    PresetAnalyzer.GetTechniqueActivationState(activationMap, currentSection)));
            }

            var activeCount = items.Count(item => item.ActivationState == TechniqueActivationState.Active);
            var inactiveCount = items.Count(item => item.ActivationState == TechniqueActivationState.Inactive);
            var unknownCount = items.Count(item => item.ActivationState == TechniqueActivationState.Unknown);
            var message = items.Count == 0
                ? "No controllable shader variables detected in the base preset."
                : $"Detected {items.Count} controllable shader variable(s): {activeCount} active, {inactiveCount} inactive, {unknownCount} unknown.";
            return new ShaderSupportScan(true, message, items.OrderBy(item => item.Section).ThenBy(item => item.Key).ToArray());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return ShaderSupportScan.Skipped($"Shader support scan failed: {ex.Message}");
        }
    }

    private static bool TryGetAdjustment(IReadOnlyDictionary<ShaderVariableKey, ShaderAdjustment> adjustments, ShaderMatchingMode matchingMode, string section, string key, out ShaderAdjustment adjustment)
    {
        if (adjustments.TryGetValue(new ShaderVariableKey(section, key), out adjustment!))
        {
            return true;
        }

        if (matchingMode == ShaderMatchingMode.KnownFallbacks && adjustments.TryGetValue(new ShaderVariableKey(null, key), out adjustment!))
        {
            return true;
        }

        if (matchingMode == ShaderMatchingMode.LooseKeys)
        {
            var looseMatch = adjustments.FirstOrDefault(pair => string.Equals(pair.Key.Key, key, StringComparison.OrdinalIgnoreCase));
            if (looseMatch.Value != null)
            {
                adjustment = looseMatch.Value;
                return true;
            }
        }

        adjustment = null!;
        return false;
    }

    private static bool HasExactAdjustment(IReadOnlyDictionary<ShaderVariableKey, ShaderAdjustment> adjustments, string section, string key)
    {
        return adjustments.ContainsKey(new ShaderVariableKey(section, key));
    }

    private static string? CombineWarnings(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return string.IsNullOrWhiteSpace(second) ? null : second;
        }

        if (string.IsNullOrWhiteSpace(second))
        {
            return first;
        }

        return $"{first} {second}";
    }

    private static CustomShaderInjectionResult InjectCustomShaderSections(Configuration configuration, List<string> lines)
    {
        if (!configuration.EnableDalashadeCustomShaders || !configuration.AutoInjectDalashadeCustomShaderSections)
        {
            return CustomShaderInjectionResult.Skipped;
        }

        var sectionInjected = false;
        var variablesInjected = false;
        var techniqueInjected = false;
        var injectedSections = new List<string>();
        var injectedVariables = new List<string>();
        var injectedTechniques = new List<string>();

        foreach (var shader in KnownCustomShaders)
        {
            if (!ContainsSection(lines, shader.Section))
            {
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                {
                    lines.Add(string.Empty);
                }

                lines.Add($"[{shader.Section}]");
                foreach (var variable in shader.Variables)
                {
                    lines.Add($"{variable}=0.000000");
                    injectedVariables.Add($"{shader.Section}/{variable}");
                }

                sectionInjected = true;
                variablesInjected = true;
                injectedSections.Add(shader.Section);
            }
            else
            {
                var insertIndex = FindSectionEnd(lines, shader.Section);
                var existingVariables = ReadSectionKeys(lines, shader.Section);
                foreach (var variable in shader.Variables)
                {
                    if (existingVariables.Contains(variable))
                    {
                        continue;
                    }

                    lines.Insert(insertIndex, $"{variable}=0.000000");
                    insertIndex++;
                    injectedVariables.Add($"{shader.Section}/{variable}");
                }

                variablesInjected = variablesInjected || injectedVariables.Count > 0;
            }

            // Section and variable injection intentionally does not activate techniques.
            // Users still need to install the .fx file and enable wanted shaders in ReShade.
        }

        var message = sectionInjected || variablesInjected || techniqueInjected
            ? $"Custom shader injection: section={(sectionInjected ? "yes" : "no")}, variables={(variablesInjected ? "yes" : "no")}, technique={(techniqueInjected ? "yes" : "no")}, generated preset only=yes."
            : "Custom shader injection: known generated preset sections and variables already present; generated preset only=yes.";

        return new CustomShaderInjectionResult(
            true,
            true,
            sectionInjected,
            variablesInjected,
            techniqueInjected,
            message,
            injectedSections.ToArray(),
            injectedVariables.ToArray(),
            injectedTechniques.ToArray());
    }

    private static bool ContainsSection(IEnumerable<string> lines, string section)
    {
        return lines.Any(line => TryReadSection(line, out var currentSection)
                                 && string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase));
    }

    private static int FindSectionEnd(IReadOnlyList<string> lines, string section)
    {
        var inSection = false;
        for (var i = 0; i < lines.Count; i++)
        {
            if (TryReadSection(lines[i], out var currentSection))
            {
                if (inSection)
                {
                    return i;
                }

                inSection = string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase);
            }
        }

        return lines.Count;
    }

    private static HashSet<string> ReadSectionKeys(IEnumerable<string> lines, string section)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inSection = false;
        foreach (var line in lines)
        {
            if (TryReadSection(line, out var currentSection))
            {
                if (inSection && !string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                inSection = string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inSection)
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex > 0)
            {
                keys.Add(line[..separatorIndex].Trim());
            }
        }

        return keys;
    }

    private static bool IsGeneratedCustomShaderVariable(CustomShaderInjectionResult injection, string section, string key)
    {
        return injection.Variables.Any(variable =>
            string.Equals(variable, $"{section}/{key}", StringComparison.OrdinalIgnoreCase));
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

    private static void ReplaceFile(string tempPath, string generatedPresetPath)
    {
        try
        {
            if (File.Exists(generatedPresetPath))
            {
                File.SetAttributes(generatedPresetPath, FileAttributes.Normal);
            }

            File.Move(tempPath, generatedPresetPath, true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static string CreateBackupPath(string generatedPresetPath)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff");
        var backupPath = $"{generatedPresetPath}.{timestamp}.bak";
        var suffix = 1;

        while (File.Exists(backupPath))
        {
            backupPath = $"{generatedPresetPath}.{timestamp}.{suffix}.bak";
            suffix++;
        }

        return backupPath;
    }

    private static void PruneBackups(string generatedPresetPath, int maxBackups)
    {
        if (maxBackups <= 0)
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(generatedPresetPath);
        var fileName = Path.GetFileName(generatedPresetPath);
        if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var directory = new DirectoryInfo(directoryPath);
        if (!directory.Exists)
        {
            return;
        }

        var backups = directory.EnumerateFiles($"{fileName}.*.bak", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(maxBackups)
            .ToArray();

        foreach (var backup in backups)
        {
            try
            {
                backup.Delete();
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
