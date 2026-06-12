using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dalashade;

public enum SmartSharpenAuthorityLevel
{
    Passive = 0,
    Secondary = 1,
    Primary = 2
}

public sealed record SmartSharpenAuthorityDiagnostic(
    SmartSharpenAuthorityLevel Level,
    IReadOnlyList<string> OtherActiveSharpeners,
    string Reason)
{
    public float ShaderValue => (float)Level;

    public static SmartSharpenAuthorityDiagnostic Unknown { get; } = new(
        SmartSharpenAuthorityLevel.Secondary,
        Array.Empty<string>(),
        "Preset analysis unavailable; SmartSharpen defaults to secondary-strength safety.");
}

public static class SmartSharpenAuthority
{
    public const string Section = "Dalashade_SmartSharpen.fx";
    public const string ReasonCategory = "Dalashade SmartSharpen authority";

    private static readonly IReadOnlyDictionary<string, Func<SceneIntent, SmartSharpenAuthorityDiagnostic, float>> SmartSharpenVariables =
        new Dictionary<string, Func<SceneIntent, SmartSharpenAuthorityDiagnostic, float>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dalashade_SharpenAuthority"] = (_, authority) => authority.ShaderValue,
            ["SharpenStrength"] = (intent, authority) => ScaleByAuthority(0.34f, authority, primary: 1.00f, secondary: 0.58f, passive: 0.12f) * FoliageTextureScale(intent),
            ["EdgeClarityStrength"] = (intent, authority) => ScaleByAuthority(0.44f + intent.Readability * 0.10f + intent.CombatPressure * 0.08f, authority, primary: 1.00f, secondary: 0.74f, passive: 0.20f),
            ["StructuralClarityStrength"] = (intent, authority) => ScaleByAuthority(0.50f + intent.Readability * 0.12f + intent.CombatPressure * 0.10f, authority, primary: 1.00f, secondary: 0.78f, passive: 0.22f),
            ["TextureDetailStrength"] = (intent, authority) => ScaleByAuthority(0.20f, authority, primary: 1.00f, secondary: 0.42f, passive: 0.08f) * FoliageTextureScale(intent),
            ["AntiCrunchStrength"] = (intent, authority) => Clamp01(0.72f + intent.FoliageDensity * 0.16f + intent.HighlightProtection * 0.10f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.08f : 0f)),
            ["DepthDampenStrength"] = (intent, authority) => Clamp01(0.62f + intent.Haze * 0.10f + intent.FoliageDensity * 0.10f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.08f : 0f)),
            ["FarDepthDampenStrength"] = (intent, authority) => Clamp01(0.72f + intent.Haze * 0.10f + intent.FoliageDensity * 0.12f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.08f : 0f)),
            ["FoliageDampenStrength"] = (intent, authority) => Clamp01(0.76f + intent.FoliageDensity * 0.18f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.06f : 0f)),
            ["HighlightDampenStrength"] = (intent, authority) => Clamp01(0.76f + intent.HighlightProtection * 0.14f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.06f : 0f)),
            ["HaloDampenStrength"] = (intent, authority) => Clamp01(0.74f + intent.HighlightProtection * 0.16f + intent.Wetness * 0.08f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.08f : 0f)),
            ["SkyDampenStrength"] = (intent, authority) => Clamp01(0.74f + intent.Atmosphere * 0.08f + intent.Haze * 0.12f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.06f : 0f)),
            ["HazeDampenStrength"] = (intent, authority) => Clamp01(0.70f + intent.Haze * 0.16f + intent.Wetness * 0.08f),
            ["CombatDampenStrength"] = (intent, authority) => Clamp01(0.62f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.10f : 0f)),
            ["LumaOnlyStrength"] = (intent, authority) => Clamp01(0.72f + intent.FoliageDensity * 0.12f + intent.HighlightProtection * 0.08f + (authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.08f : 0f))
        };

    public static IReadOnlyCollection<string> WritableVariables => SmartSharpenVariables.Keys.ToArray();

    public static SmartSharpenAuthorityDiagnostic Analyze(PresetAnalysisResult analysis)
    {
        if (!analysis.Success)
        {
            return SmartSharpenAuthorityDiagnostic.Unknown;
        }

        var activeSharpeners = analysis.Techniques
            .Where(technique => technique.ActivationState == TechniqueActivationState.Active && technique.Role == EffectRole.Sharpen)
            .GroupBy(technique => PresetAnalyzer.FormatTechnique(technique), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        var otherSharpeners = activeSharpeners
            .Where(technique => !IsSmartSharpen(technique))
            .Select(PresetAnalyzer.FormatTechnique)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (otherSharpeners.Length > 0)
        {
            return new SmartSharpenAuthorityDiagnostic(
                SmartSharpenAuthorityLevel.Secondary,
                otherSharpeners,
                $"Other active sharpen/clarity authority detected: {string.Join(", ", otherSharpeners.Take(4))}.");
        }

        return new SmartSharpenAuthorityDiagnostic(
            SmartSharpenAuthorityLevel.Primary,
            Array.Empty<string>(),
            "No other active sharpen authority was detected; SmartSharpen may act as the primary adaptive clarity pass.");
    }

    public static bool TryGetAdjustment(string section, string key, SceneIntent intent, SmartSharpenAuthorityDiagnostic authority, out ShaderAdjustment adjustment)
    {
        if (!IsSmartSharpenSection(section) || !SmartSharpenVariables.TryGetValue(key, out var valueAccessor))
        {
            adjustment = null!;
            return false;
        }

        var role = key.StartsWith("Dalashade_", StringComparison.OrdinalIgnoreCase)
            ? EffectRole.UiUtility
            : EffectRole.Sharpen;
        adjustment = new ShaderAdjustment(
            _ => new ShaderAdjustmentResult(Format(ClampForKey(key, valueAccessor(intent, authority))), false, false, BuildWarning(key, authority)),
            ReasonCategory,
            role,
            authority.Level == SmartSharpenAuthorityLevel.Secondary ? 0.60f : 1f);
        return true;
    }

    public static bool IsSmartSharpenSection(string section)
    {
        return string.Equals(section, Section, StringComparison.OrdinalIgnoreCase)
               || section.Contains("Dalashade_SmartSharpen", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSmartSharpen(PresetTechnique technique)
    {
        return IsSmartSharpenSection(technique.Section)
               || technique.TechniqueName.Contains("Dalashade_SmartSharpen", StringComparison.OrdinalIgnoreCase);
    }

    private static float ScaleByAuthority(float value, SmartSharpenAuthorityDiagnostic authority, float primary, float secondary, float passive)
    {
        return Clamp01(value * (authority.Level switch
        {
            SmartSharpenAuthorityLevel.Primary => primary,
            SmartSharpenAuthorityLevel.Secondary => secondary,
            _ => passive
        }));
    }

    private static float FoliageTextureScale(SceneIntent intent)
    {
        return Clamp01(1f - intent.FoliageDensity * 0.48f - intent.Haze * 0.24f - intent.Wetness * 0.16f);
    }

    private static float ClampForKey(string key, float value)
    {
        if (string.Equals(key, "Dalashade_SharpenAuthority", StringComparison.OrdinalIgnoreCase))
        {
            return MathF.Min(2f, MathF.Max(0f, value));
        }

        return Clamp01(value);
    }

    private static string? BuildWarning(string key, SmartSharpenAuthorityDiagnostic authority)
    {
        if (authority.Level != SmartSharpenAuthorityLevel.Secondary || key.StartsWith("Dalashade_", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return "SmartSharpen secondary authority dampening applied because another active sharpen effect is present.";
    }

    private static float Clamp01(float value) => MathF.Min(1f, MathF.Max(0f, value));

    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
