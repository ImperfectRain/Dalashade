using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public interface IGeneratedPresetPlan
{
    bool GeneratedPresetOnly { get; }
    IReadOnlyList<string> ProductionTechniqueEntries { get; }
    IReadOnlyList<string> ManualDebugTechniqueEntries { get; }
    IReadOnlyList<string> KnownGeneratedVariables { get; }
    IReadOnlyList<string> DalapadNeutralVariables { get; }
}

public sealed record GeneratedPresetPlan(
    bool GeneratedPresetOnly,
    IReadOnlyList<string> ProductionTechniqueEntries,
    IReadOnlyList<string> ManualDebugTechniqueEntries,
    IReadOnlyList<string> KnownGeneratedVariables,
    IReadOnlyList<string> DalapadNeutralVariables) : IGeneratedPresetPlan
{
    public static GeneratedPresetPlan Create()
    {
        return new GeneratedPresetPlan(
            GeneratedPresetOnly: true,
            ProductionTechniqueEntries: FirstPartyShaderRegistry.ProductionShaders
                .Where(shader => shader.TechniqueSyncEligible)
                .Select(shader => $"{shader.TechniqueName}@{shader.FileName}")
                .ToArray(),
            ManualDebugTechniqueEntries: FirstPartyShaderRegistry.ManualDebugShaders
                .Select(shader => $"{shader.TechniqueName}@{shader.FileName}")
                .ToArray(),
            KnownGeneratedVariables: CustomShaderVariableMapper.KnownVariableNames
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            DalapadNeutralVariables: CustomShaderVariableMapper.KnownVariableNames
                .Where(CustomShaderVariableMapper.IsKnownDalapadSurfaceVariable)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }
}
