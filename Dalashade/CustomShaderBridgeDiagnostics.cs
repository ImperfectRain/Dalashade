using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalashade;

public sealed record CustomShaderSectionDiagnostic(
    string Section,
    bool TechniqueAppearsInTechniques,
    TechniqueActivationState ActivationState);

public sealed record CustomShaderVariableDiagnostic(
    string Section,
    string Key,
    TechniqueActivationState ActivationState,
    bool Controllable,
    bool Written);

public sealed record CustomShaderBridgeDiagnostics(
    bool SupportEnabled,
    bool AutoInjectionEnabled,
    bool BasePresetReadable,
    bool GeneratedPresetOnlyInjection,
    bool SectionInjected,
    bool VariablesInjected,
    bool TechniqueInjected,
    SmartSharpenAuthorityDiagnostic SmartSharpenAuthority,
    IReadOnlyList<CustomShaderSectionDiagnostic> Sections,
    IReadOnlyList<CustomShaderVariableDiagnostic> KnownVariables,
    IReadOnlyList<ChangedShaderVariable> WrittenVariables,
    IReadOnlyList<string> StatusMessages)
{
    public bool SectionFound => Sections.Count > 0;
    public bool KnownVariablesFound => KnownVariables.Count > 0;
    public bool ValuesWritten => WrittenVariables.Count > 0;
    public bool VariablesDetectedButUnchanged => KnownVariablesFound && !ValuesWritten;
    public bool HasInactiveOrUnknownTechnique => Sections.Any(section => section.ActivationState != TechniqueActivationState.Active);
}

public static class CustomShaderBridgeDiagnosticsBuilder
{
    public static CustomShaderBridgeDiagnostics Build(
        Configuration configuration,
        ShaderSupportScan supportScan,
        PresetWriteResult writeResult,
        PresetAnalysisResult? analysis = null)
    {
        var smartSharpenAuthority = analysis is { Success: true }
            ? SmartSharpenAuthority.Analyze(analysis)
            : SmartSharpenAuthorityDiagnostic.Unknown;
        var supportItems = supportScan.Items
            .Where(IsCustomShaderSupportItem)
            .ToArray();
        var writtenVariables = writeResult.Changes
            .Where(IsCustomShaderChange)
            .OrderBy(change => change.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sections = new List<CustomShaderSectionDiagnostic>();
        var variables = new List<CustomShaderVariableDiagnostic>();
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.BasePresetPath) || !File.Exists(configuration.BasePresetPath))
        {
            messages.Add("Base preset path not found; custom shader section scan unavailable.");
            AppendConditionMessages(configuration, writeResult.CustomShaderInjection, sections, variables, writtenVariables, messages);
            return new CustomShaderBridgeDiagnostics(
                configuration.EnableDalashadeCustomShaders,
                configuration.AutoInjectDalashadeCustomShaderSections,
                false,
                writeResult.CustomShaderInjection.GeneratedPresetOnly,
                writeResult.CustomShaderInjection.SectionInjected,
                writeResult.CustomShaderInjection.VariablesInjected,
                writeResult.CustomShaderInjection.TechniqueInjected,
                smartSharpenAuthority,
                Array.Empty<CustomShaderSectionDiagnostic>(),
                Array.Empty<CustomShaderVariableDiagnostic>(),
                writtenVariables,
                messages);
        }

        try
        {
            var lines = File.ReadAllLines(configuration.BasePresetPath);
            var activationMap = PresetAnalyzer.ParseTechniqueActivationMap(lines);
            var sectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var variableKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var currentSection = string.Empty;

            foreach (var line in lines)
            {
                if (TryReadSection(line, out var section))
                {
                    currentSection = section;
                    if (CustomShaderVariableMapper.IsCustomShaderSection(section))
                    {
                        sectionNames.Add(section);
                    }

                    continue;
                }

                if (!CustomShaderVariableMapper.IsCustomShaderSection(currentSection))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                if (CustomShaderVariableMapper.IsKnownCustomShaderVariable(key))
                {
                    variableKeys.Add($"{currentSection}\u001f{key}");
                }
            }

            sections.AddRange(sectionNames
                .Select(section => new CustomShaderSectionDiagnostic(
                    section,
                    activationMap.TechniquesLineFound && activationMap.ActiveTechniqueKeys.Contains(section),
                    PresetAnalyzer.GetTechniqueActivationState(activationMap, section)))
                .OrderBy(section => section.Section, StringComparer.OrdinalIgnoreCase));

            variables.AddRange(variableKeys
                .Select(value =>
                {
                    var parts = value.Split('\u001f');
                    var section = parts[0];
                    var key = parts[1];
                    var activationState = PresetAnalyzer.GetTechniqueActivationState(activationMap, section);
                    return new CustomShaderVariableDiagnostic(
                        section,
                        key,
                        activationState,
                        supportItems.Any(item => string.Equals(item.Section, section, StringComparison.OrdinalIgnoreCase)
                                                 && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase)),
                        writtenVariables.Any(change => string.Equals(change.Section, section, StringComparison.OrdinalIgnoreCase)
                                                       && string.Equals(change.Key, key, StringComparison.OrdinalIgnoreCase)));
                })
                .OrderBy(variable => variable.Section, StringComparer.OrdinalIgnoreCase)
                .ThenBy(variable => variable.Key, StringComparer.OrdinalIgnoreCase));

            AppendConditionMessages(configuration, writeResult.CustomShaderInjection, sections, variables, writtenVariables, messages);
            return new CustomShaderBridgeDiagnostics(
                configuration.EnableDalashadeCustomShaders,
                configuration.AutoInjectDalashadeCustomShaderSections,
                true,
                writeResult.CustomShaderInjection.GeneratedPresetOnly,
                writeResult.CustomShaderInjection.SectionInjected,
                writeResult.CustomShaderInjection.VariablesInjected,
                writeResult.CustomShaderInjection.TechniqueInjected,
                smartSharpenAuthority,
                sections,
                variables,
                writtenVariables,
                messages);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            messages.Add($"Custom shader section scan failed: {ex.Message}");
            AppendConditionMessages(configuration, writeResult.CustomShaderInjection, sections, variables, writtenVariables, messages);
            return new CustomShaderBridgeDiagnostics(
                configuration.EnableDalashadeCustomShaders,
                configuration.AutoInjectDalashadeCustomShaderSections,
                false,
                writeResult.CustomShaderInjection.GeneratedPresetOnly,
                writeResult.CustomShaderInjection.SectionInjected,
                writeResult.CustomShaderInjection.VariablesInjected,
                writeResult.CustomShaderInjection.TechniqueInjected,
                smartSharpenAuthority,
                Array.Empty<CustomShaderSectionDiagnostic>(),
                Array.Empty<CustomShaderVariableDiagnostic>(),
                writtenVariables,
                messages);
        }
    }

    private static void AppendConditionMessages(
        Configuration configuration,
        CustomShaderInjectionResult injection,
        IReadOnlyList<CustomShaderSectionDiagnostic> sections,
        IReadOnlyList<CustomShaderVariableDiagnostic> variables,
        IReadOnlyList<ChangedShaderVariable> writtenVariables,
        List<string> messages)
    {
        if (!configuration.EnableDalashadeCustomShaders)
        {
            messages.Add("Support disabled: enable Dalashade custom shader variables before generation can write SceneIntent values.");
        }

        if (configuration.AutoInjectDalashadeCustomShaderSections)
        {
            messages.Add(injection.Attempted
                ? injection.Message
                : "Generated preset section/variable injection is enabled but has not run yet.");
        }

        if (injection.SectionInjected)
        {
            messages.Add("Generated preset injection added known custom shader section(s); the base preset was not modified.");
        }

        if (injection.VariablesInjected)
        {
            messages.Add("Generated preset injection added known custom shader variable key(s) that can receive SceneIntent values.");
        }

        if (sections.Count == 0 && !injection.SectionInjected)
        {
            messages.Add("Base preset custom shader section: none found.");
        }
        else if (sections.Count == 0 && injection.SectionInjected)
        {
            messages.Add("Base preset custom shader section: none found. Generated preset injection supplied known section(s) and variable key(s).");
        }

        if (sections.Any(section => section.ActivationState == TechniqueActivationState.Inactive))
        {
            messages.Add("Base preset technique inactive: at least one Dalashade custom shader section exists in the base preset and base preset Techniques= does not list it.");
        }

        if (sections.Any(section => section.ActivationState == TechniqueActivationState.Unknown))
        {
            messages.Add("Base preset technique unknown: Techniques= is missing or does not confirm Dalashade custom shader activation.");
        }

        if (injection.Attempted)
        {
            messages.Add("Technique activation remains manual: install the .fx file and enable the desired Dalashade shader in ReShade.");
        }

        if (sections.Count > 0 && variables.Count == 0 && !injection.VariablesInjected)
        {
            messages.Add("Variables missing: Dalashade custom shader section exists, but no known Dalashade_* variables were found.");
        }

        if (variables.Count > 0 && writtenVariables.Count == 0)
        {
            messages.Add("Variables detected but unchanged: check support toggle, activation state, write mode, or whether current values already match SceneIntent.");
        }

        if (writtenVariables.Count > 0)
        {
            messages.Add("Static bridge path active: SceneIntent values were written into generated preset Dalashade custom shader variables.");
        }

        if (writtenVariables.Any(change => string.Equals(change.Section, SmartSharpenAuthority.Section, StringComparison.OrdinalIgnoreCase)))
        {
            messages.Add("SmartSharpen generated tuning written: authority-aware slider values were written into the generated preset.");
        }
    }

    private static bool IsCustomShaderSupportItem(ShaderSupportItem item)
    {
        return string.Equals(item.ReasonCategory, CustomShaderVariableMapper.ReasonCategory, StringComparison.OrdinalIgnoreCase)
               || string.Equals(item.ReasonCategory, SmartSharpenAuthority.ReasonCategory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCustomShaderChange(ChangedShaderVariable change)
    {
        return string.Equals(change.ReasonCategory, CustomShaderVariableMapper.ReasonCategory, StringComparison.OrdinalIgnoreCase)
               || string.Equals(change.ReasonCategory, SmartSharpenAuthority.ReasonCategory, StringComparison.OrdinalIgnoreCase);
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
}
