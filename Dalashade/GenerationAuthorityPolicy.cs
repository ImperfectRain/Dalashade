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
    float SecondaryAdjustmentStrength)
{
    public bool DampensSecondaries => SecondaryAdjustmentStrength < 0.999f && SecondarySections.Count > 0;
}

public sealed class GenerationAuthorityPolicy
{
    private static readonly EffectRole[] FirstPassRoles =
    {
        EffectRole.ColorGrade,
        EffectRole.Bloom,
        EffectRole.Sharpen,
        EffectRole.AoGi
    };

    private readonly Dictionary<EffectRole, GenerationAuthorityRolePolicy> rolePolicies;
    private readonly Dictionary<string, Dictionary<EffectRole, float>> sectionStrengths;

    private GenerationAuthorityPolicy(IReadOnlyList<GenerationAuthorityRolePolicy> roles)
    {
        Roles = roles;
        rolePolicies = roles.ToDictionary(role => role.Role);
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

        var secondaryStrength = GetSecondaryStrength(mode);
        var roles = analysis.Report.Authorities
            .Where(authority => FirstPassRoles.Contains(authority.Role))
            .Select(authority => new GenerationAuthorityRolePolicy(
                authority.Role,
                ExtractSection(authority.PrimaryShader),
                authority.SecondaryShaders.Select(ExtractSection).WhereNotNull().Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                authority.SuppressedOrWarnedShaders.Select(ExtractSection).WhereNotNull().Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                GetSanitizeEligibleSections(authority, mode),
                secondaryStrength))
            .ToArray();

        return roles.Length == 0 ? Empty : new GenerationAuthorityPolicy(roles);
    }

    private static IReadOnlyList<string> GetSanitizeEligibleSections(EffectAuthority authority, PresetCompatibilityMode mode)
    {
        if (mode is not (PresetCompatibilityMode.AdaptiveBalanced or PresetCompatibilityMode.GameplaySanitize))
        {
            return Array.Empty<string>();
        }

        return authority.SecondaryShaders
            .Select(ExtractSection)
            .WhereNotNull()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static float GetSecondaryStrength(PresetCompatibilityMode mode)
    {
        return mode switch
        {
            PresetCompatibilityMode.AdaptiveBalanced => 0.75f,
            PresetCompatibilityMode.GameplaySanitize => 0.50f,
            _ => 1f
        };
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
