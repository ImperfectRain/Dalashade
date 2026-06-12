using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dalashade;

public sealed class CustomShaderVariableMapper
{
    public const string ReasonCategory = "Dalashade custom shader scene intent";

    private static readonly IReadOnlyDictionary<string, Func<SceneIntent, float>> Variables =
        new Dictionary<string, Func<SceneIntent, float>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dalashade_Readability"] = intent => intent.Readability,
            ["Dalashade_Atmosphere"] = intent => intent.Atmosphere,
            ["Dalashade_HighlightProtection"] = intent => intent.HighlightProtection,
            ["Dalashade_ShadowProtection"] = intent => intent.ShadowProtection,
            ["Dalashade_Haze"] = intent => intent.Haze,
            ["Dalashade_Wetness"] = intent => intent.Wetness,
            ["Dalashade_Cold"] = intent => intent.Cold,
            ["Dalashade_Heat"] = intent => intent.Heat,
            ["Dalashade_MagicGlow"] = intent => intent.MagicGlow,
            ["Dalashade_NeonGlow"] = intent => intent.NeonGlow,
            ["Dalashade_FoliageDensity"] = intent => intent.FoliageDensity,
            ["Dalashade_IndustrialHardness"] = intent => intent.IndustrialHardness,
            ["Dalashade_CosmicMood"] = intent => intent.CosmicMood,
            ["Dalashade_CombatPressure"] = intent => intent.CombatPressure,
            ["Dalashade_CinematicPermission"] = intent => intent.CinematicPermission
        };

    public static IReadOnlyCollection<string> KnownVariableNames => Variables.Keys
        .Concat(SmartSharpenAuthority.WritableVariables)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public bool TryGetAdjustment(Configuration configuration, string section, string key, SceneIntent intent, out ShaderAdjustment adjustment)
    {
        if (!configuration.EnableDalashadeCustomShaders || !IsCustomShaderSection(section) || !Variables.TryGetValue(key, out var valueAccessor))
        {
            adjustment = null!;
            return false;
        }

        adjustment = new ShaderAdjustment(
            _ => new ShaderAdjustmentResult(Format(Clamp01(valueAccessor(intent))), false, false),
            ReasonCategory,
            EffectRole.UiUtility,
            1f);
        return true;
    }

    public static bool IsCustomShaderSection(string section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return false;
        }

        return section.StartsWith("Dalashade", StringComparison.OrdinalIgnoreCase)
               || section.Contains("\\Dalashade", StringComparison.OrdinalIgnoreCase)
               || section.Contains("/Dalashade", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsKnownCustomShaderVariable(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
               && (Variables.ContainsKey(key) || SmartSharpenAuthority.WritableVariables.Contains(key, StringComparer.OrdinalIgnoreCase));
    }

    private static float Clamp01(float value) => MathF.Min(1f, MathF.Max(0f, value));

    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
