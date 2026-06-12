using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dalashade;

public sealed class CustomShaderVariableMapper
{
    public const string ReasonCategory = "Dalashade custom shader scene intent";
    public const string MaterialReasonCategory = "Dalashade custom shader material intent";

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

    private static readonly IReadOnlyDictionary<string, Func<MaterialIntent, Configuration, float>> MaterialVariables =
        new Dictionary<string, Func<MaterialIntent, Configuration, float>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dalashade_MaterialFoliage"] = (intent, _) => intent.Foliage,
            ["Dalashade_MaterialWaterSpecular"] = (intent, _) => intent.WaterSpecular,
            ["Dalashade_MaterialSandDust"] = (intent, _) => intent.SandDust,
            ["Dalashade_MaterialSnowIce"] = (intent, _) => intent.SnowIce,
            ["Dalashade_MaterialMetalIndustrial"] = (intent, _) => intent.MetalIndustrial,
            ["Dalashade_MaterialCrystalAether"] = (intent, _) => intent.CrystalAether,
            ["Dalashade_MaterialNeonGlass"] = (intent, _) => intent.NeonGlass,
            ["Dalashade_MaterialFireLavaHeat"] = (intent, _) => intent.FireLavaHeat,
            ["Dalashade_MaterialSkyCloudFog"] = (intent, _) => intent.SkyCloudFog,
            ["Dalashade_MaterialSkinProtection"] = (intent, _) => intent.SkinProtection,
            ["Dalashade_MaterialVoidDarkness"] = (intent, _) => intent.VoidDarkness,
            ["Dalashade_MaterialDebugMode"] = (_, configuration) => configuration.EnableMaterialDebugMasks ? configuration.MaterialDebugMaskMode : 0f,
            ["Dalashade_MaterialDebugStrength"] = (_, configuration) => configuration.EnableMaterialDebugMasks ? 1f : 0f
        };

    private static readonly HashSet<string> SmartSharpenMaterialVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialDebugMode",
        "Dalashade_MaterialDebugStrength"
    ];

    private static readonly HashSet<string> WeatherAtmosphereMaterialVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialDebugMode",
        "Dalashade_MaterialDebugStrength"
    ];

    private static readonly HashSet<string> AtmosphereBloomMaterialVariables =
    [
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialDebugMode",
        "Dalashade_MaterialDebugStrength"
    ];

    private static readonly HashSet<string> AdaptiveGradeMaterialVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    public static IReadOnlyCollection<string> KnownVariableNames => Variables.Keys
        .Concat(MaterialVariables.Keys)
        .Concat(SmartSharpenAuthority.WritableVariables)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public bool TryGetAdjustment(Configuration configuration, string section, string key, SceneIntent intent, MaterialIntent materialIntent, out ShaderAdjustment adjustment)
    {
        if (!configuration.EnableDalashadeCustomShaders || !IsCustomShaderSection(section))
        {
            adjustment = null!;
            return false;
        }

        if (Variables.TryGetValue(key, out var valueAccessor))
        {
            adjustment = new ShaderAdjustment(
                _ => new ShaderAdjustmentResult(Format(Clamp01(valueAccessor(intent))), false, false),
                ReasonCategory,
                EffectRole.UiUtility,
                1f);
            return true;
        }

        if (!configuration.EnableMaterialIntent
            || !configuration.EnableMaterialIntentShaderMapping
            || configuration.MaterialIntentStrength <= 0f
            || !IsSupportedMaterialSectionVariable(section, key)
            || !MaterialVariables.TryGetValue(key, out var materialAccessor))
        {
            adjustment = null!;
            return false;
        }

        adjustment = new ShaderAdjustment(
            _ => new ShaderAdjustmentResult(Format(MaterialOutput(key, materialAccessor(materialIntent, configuration), configuration)), false, false),
            MaterialReasonCategory,
            EffectRole.UiUtility,
            1f);
        return true;
    }

    public bool TryGetAdjustment(Configuration configuration, string section, string key, SceneIntent intent, out ShaderAdjustment adjustment)
    {
        return TryGetAdjustment(configuration, section, key, intent, MaterialIntent.Neutral, out adjustment);
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
               && (Variables.ContainsKey(key)
                   || MaterialVariables.ContainsKey(key)
                   || SmartSharpenAuthority.WritableVariables.Contains(key, StringComparer.OrdinalIgnoreCase));
    }

    public static bool IsKnownMaterialIntentVariable(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
               && MaterialVariables.ContainsKey(key);
    }

    private static float Clamp01(float value) => MathF.Min(1f, MathF.Max(0f, value));

    private static float MaterialOutput(string key, float value, Configuration configuration)
    {
        if (string.Equals(key, "Dalashade_MaterialDebugMode", StringComparison.OrdinalIgnoreCase))
        {
            return MathF.Max(0f, value);
        }

        return Clamp01(value) * Clamp01(configuration.MaterialIntentStrength);
    }

    private static bool IsSupportedMaterialSectionVariable(string section, string key)
    {
        return (SmartSharpenAuthority.IsSmartSharpenSection(section) && SmartSharpenMaterialVariables.Contains(key))
               || (IsWeatherAtmosphereSection(section) && WeatherAtmosphereMaterialVariables.Contains(key))
               || (IsAtmosphereBloomSection(section) && AtmosphereBloomMaterialVariables.Contains(key))
               || (IsAdaptiveGradeSection(section) && AdaptiveGradeMaterialVariables.Contains(key));
    }

    private static bool IsWeatherAtmosphereSection(string section)
    {
        return string.Equals(section, "Dalashade_WeatherAtmosphere.fx", StringComparison.OrdinalIgnoreCase)
               || section.Contains("Dalashade_WeatherAtmosphere", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAtmosphereBloomSection(string section)
    {
        return string.Equals(section, "Dalashade_AtmosphereBloom.fx", StringComparison.OrdinalIgnoreCase)
               || section.Contains("Dalashade_AtmosphereBloom", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAdaptiveGradeSection(string section)
    {
        return string.Equals(section, "Dalashade_AdaptiveGrade.fx", StringComparison.OrdinalIgnoreCase)
               || section.Contains("Dalashade_AdaptiveGrade", StringComparison.OrdinalIgnoreCase);
    }

    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
