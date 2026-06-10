using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record GenerationAuthorityRolePolicy(
    EffectRole Role,
    string? PrimarySection,
    IReadOnlyList<string> SecondarySections,
    IReadOnlyList<string> WarnedOnlySections,
    IReadOnlyList<string> SanitizeEligibleSections,
    CompatibilityRolePolicy Policy,
    float SecondaryAdjustmentStrength)
{
    public bool DampensSecondaries => SecondaryAdjustmentStrength < 0.999f && SecondarySections.Count > 0;
}

public sealed record CompatibilityRolePolicy(
    EffectRole Role,
    bool MultipleActiveEffectsAllowed,
    bool UnsupportedActiveEffectsWarnOnly,
    bool GameplaySanitizeMayReduce,
    bool GposePreserveLeavesAlone,
    float AdaptiveBalancedSecondaryStrength,
    float GameplaySanitizeSecondaryStrength)
{
    public float GetSecondaryStrength(PresetCompatibilityMode mode)
    {
        return mode switch
        {
            PresetCompatibilityMode.AdaptiveBalanced when GameplaySanitizeMayReduce => AdaptiveBalancedSecondaryStrength,
            PresetCompatibilityMode.GameplaySanitize when GameplaySanitizeMayReduce => GameplaySanitizeSecondaryStrength,
            _ => 1f
        };
    }
}

public static class CompatibilityRolePolicies
{
    public static IReadOnlyList<CompatibilityRolePolicy> All { get; } =
    [
        new(EffectRole.ColorGrade, false, false, true, true, 0.75f, 0.35f),
        new(EffectRole.Bloom, false, true, true, true, 0.75f, 0.35f),
        new(EffectRole.Sharpen, false, true, true, true, 0.75f, 0.40f),
        new(EffectRole.AoGi, false, true, true, true, 0.75f, 0.40f),
        new(EffectRole.Diffusion, true, true, true, true, 0.85f, 0.60f),
        new(EffectRole.Dof, true, true, false, true, 1.00f, 1.00f),
        new(EffectRole.FilmGrain, true, true, true, true, 0.90f, 0.70f),
        new(EffectRole.Vignette, true, true, true, true, 0.90f, 0.70f)
    ];

    public static bool TryGet(EffectRole role, out CompatibilityRolePolicy policy)
    {
        policy = All.FirstOrDefault(item => item.Role == role)!;
        return policy != null;
    }

    public static CompatibilityRolePolicy? Get(EffectRole role)
    {
        return All.FirstOrDefault(item => item.Role == role);
    }
}

public sealed class GenerationAuthorityPolicy
{
    private readonly Dictionary<string, Dictionary<EffectRole, float>> sectionStrengths;

    private GenerationAuthorityPolicy(IReadOnlyList<GenerationAuthorityRolePolicy> roles)
    {
        Roles = roles;
        sectionStrengths = new Dictionary<string, Dictionary<EffectRole, float>>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            foreach (var section in role.SecondarySections)
            {
                if (!sectionStrengths.TryGetValue(section, out var roleStrengths))
                {
                    roleStrengths = new Dictionary<EffectRole, float>();
                    sectionStrengths[section] = roleStrengths;
                }

                roleStrengths[role.Role] = role.SecondaryAdjustmentStrength;
            }
        }
    }

    public static GenerationAuthorityPolicy Empty { get; } = new(Array.Empty<GenerationAuthorityRolePolicy>());

    public IReadOnlyList<GenerationAuthorityRolePolicy> Roles { get; }

    public float GetAdjustmentStrength(string? section, EffectRole role)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return 1f;
        }

        return sectionStrengths.TryGetValue(section, out var roleStrengths)
               && roleStrengths.TryGetValue(role, out var strength)
            ? strength
            : 1f;
    }

    public bool IsSecondaryDampened(string section, EffectRole role)
    {
        return GetAdjustmentStrength(section, role) < 0.999f;
    }

    public static GenerationAuthorityPolicy From(PresetAnalysisResult analysis, PresetCompatibilityMode mode)
    {
        if (!analysis.Success)
        {
            return Empty;
        }

        var roles = analysis.Report.Authorities
            .Where(authority => CompatibilityRolePolicies.TryGet(authority.Role, out _))
            .Select(authority => new GenerationAuthorityRolePolicy(
                authority.Role,
                ExtractSection(authority.PrimaryShader),
                authority.SecondaryShaders.Select(ExtractSection).WhereNotNull().Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                authority.SuppressedOrWarnedShaders.Select(ExtractSection).WhereNotNull().Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                GetSanitizeEligibleSections(authority, mode),
                CompatibilityRolePolicies.Get(authority.Role)!,
                GetSecondaryStrength(authority.Role, mode)))
            .ToArray();

        return roles.Length == 0 ? Empty : new GenerationAuthorityPolicy(roles);
    }

    private static IReadOnlyList<string> GetSanitizeEligibleSections(EffectAuthority authority, PresetCompatibilityMode mode)
    {
        if (mode is not (PresetCompatibilityMode.AdaptiveBalanced or PresetCompatibilityMode.GameplaySanitize)
            || !CompatibilityRolePolicies.TryGet(authority.Role, out var policy)
            || !policy.GameplaySanitizeMayReduce)
        {
            return Array.Empty<string>();
        }

        return authority.SecondaryShaders
            .Select(ExtractSection)
            .WhereNotNull()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static float GetSecondaryStrength(EffectRole role, PresetCompatibilityMode mode)
    {
        if (!CompatibilityRolePolicies.TryGet(role, out var policy))
        {
            return 1f;
        }

        return policy.GetSecondaryStrength(mode);
    }

    private static string? ExtractSection(string shader)
    {
        if (string.IsNullOrWhiteSpace(shader))
        {
            return null;
        }

        var separatorIndex = shader.LastIndexOf('@');
        return separatorIndex >= 0 && separatorIndex < shader.Length - 1
            ? shader[(separatorIndex + 1)..].Trim()
            : shader.Trim();
    }
}

internal static class EnumerableExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> values)
        where T : class
    {
        foreach (var value in values)
        {
            if (value != null)
            {
                yield return value;
            }
        }
    }
}
