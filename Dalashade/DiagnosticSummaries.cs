using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record DiagnosticHealthSummary(
    string OverallStatus,
    string Summary,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> FirstFilesToOpen);

public sealed record DiagnosticPerformanceSummary(
    FirstPartyPerformanceTier SelectedFirstPartyPerformanceTier,
    bool QualityPreservesCurrentBehavior,
    bool GeneratedPresetWritesEnabled,
    bool CustomShaderSectionInjectionEnabled,
    bool TechniqueActivationSyncEnabled,
    DiagnosticDalapadCostStatusSummary DalapadCostStatus);

public sealed record DiagnosticGeneratedVariableSummary(
    int KnownVariableCount,
    int GeneratedValueCount,
    int ChangedVariableCount,
    IReadOnlyList<string> KnownVariables,
    IReadOnlyList<string> ChangedVariables);

public sealed record DiagnosticDalapadProductionAssistSummary(
    bool Enabled,
    bool GlobalShaderGate,
    bool SurfaceDataGate,
    float SurfaceDataStrength,
    bool SceneGINormalAssistGate,
    float SceneGINormalAssistStrength,
    string ExpectedCostClass);

public sealed record DiagnosticDalapadDebugVisualizationCostSummary(
    bool Enabled,
    string Status,
    bool ReadsRenderTargets,
    bool CopiesRenderTargets,
    int ObservedSourceCount,
    int CopiedSourceCount,
    int CopyFrameInterval,
    int FrameAge,
    int CopiedPinnedCandidateCount,
    string CostClass);

public sealed record DiagnosticDalapadCostStatusSummary(
    DiagnosticDalapadProductionAssistSummary ProductionAssist,
    DiagnosticDalapadDebugVisualizationCostSummary DebugVisualizationCost);

public static class DiagnosticSummaryBuilder
{
    public static DiagnosticDalapadCostStatusSummary BuildDalapadCostStatus(Configuration configuration, DalapadDiagnostics dalapadDiagnostics)
    {
        var debugVisualization = dalapadDiagnostics.ControlPipeStatus.DebugVisualization;
        var dalapadProductionEnabled = configuration.EnableDalapadShaderIntegration
            && (configuration.EnableDalapadSurfaceData || configuration.EnableDalapadSceneGINormalAssist);
        var debugCopyActive = debugVisualization.CopiesRenderTargets && debugVisualization.CopiedSourceCount > 0;

        return new DiagnosticDalapadCostStatusSummary(
            new DiagnosticDalapadProductionAssistSummary(
                Enabled: dalapadProductionEnabled,
                GlobalShaderGate: configuration.EnableDalapadShaderIntegration,
                SurfaceDataGate: configuration.EnableDalapadSurfaceData,
                SurfaceDataStrength: configuration.DalapadSurfaceDataStrength,
                SceneGINormalAssistGate: configuration.EnableDalapadSceneGINormalAssist,
                SceneGINormalAssistStrength: configuration.DalapadSceneGINormalAssistStrength,
                ExpectedCostClass: dalapadProductionEnabled ? "ShaderSamplingAfterGates" : "DisabledNeutralFallback"),
            new DiagnosticDalapadDebugVisualizationCostSummary(
                Enabled: debugVisualization.Enabled,
                Status: debugVisualization.Status,
                ReadsRenderTargets: debugVisualization.ReadsRenderTargets,
                CopiesRenderTargets: debugVisualization.CopiesRenderTargets,
                ObservedSourceCount: debugVisualization.ObservedSourceCount,
                CopiedSourceCount: debugVisualization.CopiedSourceCount,
                CopyFrameInterval: debugVisualization.CopyFrameInterval,
                FrameAge: debugVisualization.FrameAge,
                CopiedPinnedCandidateCount: debugVisualization.PinnedCandidates.Count(candidate => candidate.Copied),
                CostClass: debugCopyActive
                    ? "PotentiallyExpensiveDebugCopy"
                    : debugVisualization.ReadsRenderTargets
                        ? "DebugObservationNoActiveCopy"
                        : "NoDebugRenderTargetCopyReported"));
    }

    public static DiagnosticGeneratedVariableSummary BuildGeneratedVariableSummary(
        IReadOnlyCollection<string> knownVariables,
        IReadOnlyCollection<string> generatedValues,
        IReadOnlyCollection<ChangedShaderVariable> changes)
    {
        return new DiagnosticGeneratedVariableSummary(
            KnownVariableCount: knownVariables.Count,
            GeneratedValueCount: generatedValues.Count,
            ChangedVariableCount: changes.Count,
            KnownVariables: knownVariables.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            ChangedVariables: changes.Select(change => change.Key).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static DiagnosticPerformanceSummary BuildPerformanceSummary(
        Configuration configuration,
        DalapadDiagnostics dalapadDiagnostics)
    {
        return new DiagnosticPerformanceSummary(
            SelectedFirstPartyPerformanceTier: configuration.FirstPartyPerformanceTier,
            QualityPreservesCurrentBehavior: true,
            GeneratedPresetWritesEnabled: configuration.EnableDalashadeCustomShaders,
            CustomShaderSectionInjectionEnabled: configuration.AutoInjectDalashadeCustomShaderSections,
            TechniqueActivationSyncEnabled: configuration.SyncDalashadeTechniqueActivation,
            DalapadCostStatus: BuildDalapadCostStatus(configuration, dalapadDiagnostics));
    }

    public static DiagnosticHealthSummary BuildHealthSummary(
        string overallStatus,
        string summary,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> firstFilesToOpen)
    {
        return new DiagnosticHealthSummary(overallStatus, summary, warnings, firstFilesToOpen);
    }
}
