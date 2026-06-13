using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dalashade;

public sealed class CustomShaderVariableMapper
{
    public const string ReasonCategory = "Dalashade custom shader scene intent";
    public const string MaterialReasonCategory = "Dalashade custom shader material intent";
    public const string SceneGIReasonCategory = "Dalashade custom shader SceneGI tuning";
    public const string SurfaceReflectionReasonCategory = "Dalashade custom shader SurfaceReflection tuning";

    private static readonly IReadOnlyDictionary<string, Func<SceneIntent, float>> Variables =
        new Dictionary<string, Func<SceneIntent, float>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dalashade_Readability"] = intent => intent.Readability,
            ["Dalashade_IntentReadability"] = intent => intent.Readability,
            ["Dalashade_Atmosphere"] = intent => intent.Atmosphere,
            ["Dalashade_IntentAtmosphere"] = intent => intent.Atmosphere,
            ["Dalashade_HighlightProtection"] = intent => intent.HighlightProtection,
            ["Dalashade_IntentHighlightProtection"] = intent => intent.HighlightProtection,
            ["Dalashade_ShadowProtection"] = intent => intent.ShadowProtection,
            ["Dalashade_IntentShadowProtection"] = intent => intent.ShadowProtection,
            ["Dalashade_Haze"] = intent => intent.Haze,
            ["Dalashade_IntentHaze"] = intent => intent.Haze,
            ["Dalashade_Wetness"] = intent => intent.Wetness,
            ["Dalashade_IntentWetness"] = intent => intent.Wetness,
            ["Dalashade_Cold"] = intent => intent.Cold,
            ["Dalashade_IntentCold"] = intent => intent.Cold,
            ["Dalashade_Heat"] = intent => intent.Heat,
            ["Dalashade_IntentHeat"] = intent => intent.Heat,
            ["Dalashade_MagicGlow"] = intent => intent.MagicGlow,
            ["Dalashade_IntentMagicGlow"] = intent => intent.MagicGlow,
            ["Dalashade_NeonGlow"] = intent => intent.NeonGlow,
            ["Dalashade_IntentNeonGlow"] = intent => intent.NeonGlow,
            ["Dalashade_FoliageDensity"] = intent => intent.FoliageDensity,
            ["Dalashade_IntentFoliageDensity"] = intent => intent.FoliageDensity,
            ["Dalashade_IndustrialHardness"] = intent => intent.IndustrialHardness,
            ["Dalashade_IntentIndustrialHardness"] = intent => intent.IndustrialHardness,
            ["Dalashade_CosmicMood"] = intent => intent.CosmicMood,
            ["Dalashade_IntentCosmicMood"] = intent => intent.CosmicMood,
            ["Dalashade_Night"] = intent => intent.Night,
            ["Dalashade_Moonlight"] = intent => intent.Moonlight,
            ["Dalashade_ArtificialLight"] = intent => intent.ArtificialLight,
            ["Dalashade_AmbientDarkness"] = intent => intent.AmbientDarkness,
            ["Dalashade_NightAtmosphere"] = intent => intent.NightAtmosphere,
            ["Dalashade_WetSurfaceContext"] = intent => intent.Wetness,
            ["Dalashade_CombatPressure"] = intent => intent.CombatPressure,
            ["Dalashade_IntentCombatPressure"] = intent => intent.CombatPressure,
            ["Dalashade_CinematicPermission"] = intent => intent.CinematicPermission,
            ["Dalashade_IntentCinematicPermission"] = intent => intent.CinematicPermission
        };

    private static readonly IReadOnlyDictionary<string, Func<Configuration, float>> SceneGIVariables =
        new Dictionary<string, Func<Configuration, float>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dalashade_GIEnabled"] = configuration => configuration.EnableDalashadeSceneGIShaderVariables ? 1f : 0f,
            ["Dalashade_GIStrength"] = configuration => configuration.DalashadeSceneGIStrength,
            ["Dalashade_GIRadius"] = _ => 0.65f,
            ["Dalashade_GIBounceStrength"] = configuration => configuration.DalashadeSceneGIBounceStrength,
            ["Dalashade_GIAOIntensity"] = configuration => configuration.DalashadeSceneGIAOIntensity,
            ["Dalashade_GIAORadius"] = _ => 0.45f,
            ["Dalashade_GINightLightStrength"] = configuration => configuration.DalashadeSceneGINightLightStrength,
            ["Dalashade_GIMaterialInfluence"] = configuration => configuration.DalashadeSceneGIMaterialInfluence,
            ["Dalashade_GISkyReject"] = _ => 1.0f,
            ["Dalashade_GISkinProtect"] = _ => 1.0f,
            ["Dalashade_GIDebugMode"] = configuration => configuration.DalashadeSceneGIDebugMode,
            ["Dalashade_GIDebugOutputMode"] = configuration => configuration.DalashadeSceneGIDebugOutputMode,
            ["Dalashade_GIDebugOpacity"] = configuration => configuration.DalashadeSceneGIDebugOpacity,
            ["Dalashade_GIDebugBoost"] = configuration => configuration.DalashadeSceneGIDebugBoost
        };

    private static readonly IReadOnlyDictionary<string, Func<Configuration, float>> SurfaceReflectionVariables =
        new Dictionary<string, Func<Configuration, float>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dalashade_SurfaceReflectionEnabled"] = configuration => configuration.EnableDalashadeSurfaceReflectionShaderVariables ? 1f : 0f,
            ["Dalashade_SurfaceReflectionStrength"] = configuration => configuration.DalashadeSurfaceReflectionStrength,
            ["Dalashade_WaterSheenStrength"] = configuration => configuration.DalashadeSurfaceReflectionWaterSheenStrength,
            ["Dalashade_WaterReflectionStrength"] = _ => 0.45f,
            ["Dalashade_WaterSheenRadius"] = _ => 1.35f,
            ["Dalashade_SpecularGlintStrength"] = configuration => configuration.DalashadeSurfaceReflectionSpecularGlintStrength,
            ["Dalashade_SpecularReflectionStrength"] = _ => 0.30f,
            ["Dalashade_WetReflectionStrength"] = configuration => configuration.DalashadeSurfaceReflectionWetStrength,
            ["Dalashade_AetherReflectionStrength"] = configuration => configuration.DalashadeSurfaceReflectionAetherNeonStrength,
            ["Dalashade_NeonReflectionStrength"] = configuration => configuration.DalashadeSurfaceReflectionAetherNeonStrength,
            ["Dalashade_IceSheenStrength"] = _ => 0.24f,
            ["Dalashade_SurfaceReflectionSkyReject"] = _ => 1.0f,
            ["Dalashade_SurfaceReflectionSkinProtect"] = _ => 1.0f,
            ["Dalashade_ReflectionSampleOffset"] = _ => 0.018f,
            ["Dalashade_ReflectionSoftness"] = _ => 0.50f,
            ["Dalashade_ReflectionDepthReject"] = _ => 0.65f,
            ["Dalashade_SurfaceReflectionDebugMode"] = configuration => configuration.DalashadeSurfaceReflectionDebugMode,
            ["Dalashade_SurfaceReflectionDebugOutputMode"] = _ => 0f,
            ["Dalashade_SurfaceReflectionDebugOpacity"] = configuration => configuration.DalashadeSurfaceReflectionDebugOpacity,
            ["Dalashade_SurfaceReflectionDebugBoost"] = _ => 2.25f
        };

    private static readonly IReadOnlyDictionary<string, Func<MaterialIntent, Configuration, float>> MaterialVariables =
        new Dictionary<string, Func<MaterialIntent, Configuration, float>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dalashade_MaterialFoliage"] = (intent, _) => intent.Foliage,
            ["Dalashade_MaterialWaterSpecular"] = (intent, _) => intent.WaterSpecular,
            ["Dalashade_MaterialWaterPlane"] = (intent, _) => intent.WaterSpecular,
            ["Dalashade_MaterialSpecularGlint"] = (intent, _) => intent.WaterSpecular,
            ["Dalashade_WaterContext"] = (intent, _) => intent.WaterSpecular,
            ["Dalashade_CoastalContext"] = (intent, _) => intent.WaterSpecular,
            ["Dalashade_OpenOceanContext"] = (intent, _) => intent.WaterSpecular * 0.85f,
            ["Dalashade_ShallowWaterContext"] = (intent, _) => MathF.Max(intent.WaterSpecular * 0.72f, MathF.Min(intent.WaterSpecular, intent.SandDust) * 0.20f),
            ["Dalashade_MaterialSandDust"] = (intent, _) => intent.SandDust,
            ["Dalashade_MaterialSnowIce"] = (intent, _) => intent.SnowIce,
            ["Dalashade_MaterialStoneRuins"] = (intent, _) => intent.StoneRuins,
            ["Dalashade_MaterialMetalIndustrial"] = (intent, _) => intent.MetalIndustrial,
            ["Dalashade_MaterialCrystalAether"] = (intent, _) => intent.CrystalAether,
            ["Dalashade_MaterialNeonGlass"] = (intent, _) => intent.NeonGlass,
            ["Dalashade_MaterialFireLavaHeat"] = (intent, _) => intent.FireLavaHeat,
            ["Dalashade_MaterialSkyCloudFog"] = (intent, _) => intent.SkyCloudFog,
            ["Dalashade_MaterialSkinProtection"] = (intent, _) => intent.SkinProtection,
            ["Dalashade_MaterialVoidDarkness"] = (intent, _) => intent.VoidDarkness
        };

    private static readonly HashSet<string> SmartSharpenMaterialVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection"
    ];

    private static readonly HashSet<string> WeatherAtmosphereMaterialVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialSkyCloudFog"
    ];

    private static readonly HashSet<string> AtmosphereBloomMaterialVariables =
    [
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog"
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

    private static readonly HashSet<string> SceneGIMaterialVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    private static readonly HashSet<string> SurfaceReflectionMaterialVariables =
    [
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_WaterContext",
        "Dalashade_CoastalContext",
        "Dalashade_OpenOceanContext",
        "Dalashade_ShallowWaterContext",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection"
    ];

    private static readonly HashSet<string> MaterialDebugVariables =
    [
        "Dalashade_MaterialFoliage",
        "Dalashade_MaterialWaterSpecular",
        "Dalashade_MaterialWaterPlane",
        "Dalashade_MaterialSpecularGlint",
        "Dalashade_WaterContext",
        "Dalashade_CoastalContext",
        "Dalashade_OpenOceanContext",
        "Dalashade_ShallowWaterContext",
        "Dalashade_MaterialSandDust",
        "Dalashade_MaterialSnowIce",
        "Dalashade_MaterialStoneRuins",
        "Dalashade_MaterialMetalIndustrial",
        "Dalashade_MaterialCrystalAether",
        "Dalashade_MaterialNeonGlass",
        "Dalashade_MaterialFireLavaHeat",
        "Dalashade_MaterialSkyCloudFog",
        "Dalashade_MaterialSkinProtection",
        "Dalashade_MaterialVoidDarkness"
    ];

    private static readonly HashSet<string> ShaderOwnedVariables =
    [
        "Dalashade_EnableDepthAssist",
        "Dalashade_DepthAssistStrength",
        "Dalashade_DepthAssistConfidenceFloor",
        "Dalashade_DepthConfidenceFloor"
    ];

    public static IReadOnlyCollection<string> KnownVariableNames => Variables.Keys
        .Concat(SceneGIVariables.Keys)
        .Concat(SurfaceReflectionVariables.Keys)
        .Concat(MaterialVariables.Keys)
        .Concat(ShaderOwnedVariables)
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

        if (configuration.EnableDalashadeSceneGIShaderVariables
            && IsSceneGISection(section)
            && SceneGIVariables.TryGetValue(key, out var giAccessor))
        {
            adjustment = new ShaderAdjustment(
                _ => FormatSceneGIValue(key, giAccessor(configuration)),
                SceneGIReasonCategory,
                EffectRole.AoGi,
                1f);
            return true;
        }

        if (configuration.EnableDalashadeSurfaceReflectionShaderVariables
            && IsSurfaceReflectionSection(section)
            && SurfaceReflectionVariables.TryGetValue(key, out var surfaceReflectionAccessor))
        {
            adjustment = new ShaderAdjustment(
                _ => FormatSurfaceReflectionValue(key, surfaceReflectionAccessor(configuration)),
                SurfaceReflectionReasonCategory,
                EffectRole.Diffusion,
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
                   || SceneGIVariables.ContainsKey(key)
                   || SurfaceReflectionVariables.ContainsKey(key)
                   || MaterialVariables.ContainsKey(key)
                   || ShaderOwnedVariables.Contains(key)
                   || SmartSharpenAuthority.WritableVariables.Contains(key, StringComparer.OrdinalIgnoreCase));
    }

    public static bool IsKnownMaterialIntentVariable(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
               && MaterialVariables.ContainsKey(key);
    }

    public static bool IsKnownSceneIntentVariable(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
               && Variables.ContainsKey(key);
    }

    public static bool IsKnownShaderOwnedVariable(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
               && (ShaderOwnedVariables.Contains(key)
                   || SmartSharpenAuthority.WritableVariables.Contains(key, StringComparer.OrdinalIgnoreCase));
    }

    private static float Clamp01(float value) => MathF.Min(1f, MathF.Max(0f, value));

    private static float MaterialOutput(string key, float value, Configuration configuration)
    {
        return Clamp01(value) * Clamp01(configuration.MaterialIntentStrength);
    }

    private static ShaderAdjustmentResult FormatSceneGIValue(string key, float value)
    {
        if (string.Equals(key, "Dalashade_GIDebugMode", StringComparison.OrdinalIgnoreCase))
        {
            var rounded = (int)MathF.Round(value);
            var clamped = Math.Min(12, Math.Max(0, rounded));
            return new ShaderAdjustmentResult(clamped.ToString(CultureInfo.InvariantCulture), rounded < 0, rounded > 12);
        }

        if (string.Equals(key, "Dalashade_GIDebugOutputMode", StringComparison.OrdinalIgnoreCase))
        {
            var rounded = (int)MathF.Round(value);
            var clamped = Math.Min(4, Math.Max(0, rounded));
            return new ShaderAdjustmentResult(clamped.ToString(CultureInfo.InvariantCulture), rounded < 0, rounded > 4);
        }

        if (string.Equals(key, "Dalashade_GIEnabled", StringComparison.OrdinalIgnoreCase))
        {
            var enabled = value >= 0.5f ? 1 : 0;
            return new ShaderAdjustmentResult(enabled.ToString(CultureInfo.InvariantCulture), false, false);
        }

        if (IsSceneGINormalizedVariable(key))
        {
            var clamped = Clamp01(value);
            return new ShaderAdjustmentResult(Format(clamped), value < 0f, value > 1f);
        }

        if (IsSceneGIRadiusVariable(key))
        {
            var clamped = MathF.Min(8f, MathF.Max(0f, value));
            return new ShaderAdjustmentResult(Format(clamped), value < 0f, value > 8f);
        }

        if (string.Equals(key, "Dalashade_GIDebugBoost", StringComparison.OrdinalIgnoreCase))
        {
            var clamped = MathF.Min(8f, MathF.Max(0.25f, value));
            return new ShaderAdjustmentResult(Format(clamped), value < 0.25f, value > 8f);
        }

        return new ShaderAdjustmentResult(Format(value), false, false);
    }

    private static bool IsSceneGINormalizedVariable(string key)
    {
        return string.Equals(key, "Dalashade_GIStrength", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "Dalashade_GIBounceStrength", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "Dalashade_GIAOIntensity", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "Dalashade_GINightLightStrength", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "Dalashade_GIMaterialInfluence", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "Dalashade_GISkyReject", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "Dalashade_GISkinProtect", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "Dalashade_GIDebugOpacity", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSceneGIRadiusVariable(string key)
    {
        return string.Equals(key, "Dalashade_GIRadius", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "Dalashade_GIAORadius", StringComparison.OrdinalIgnoreCase);
    }

    private static ShaderAdjustmentResult FormatSurfaceReflectionValue(string key, float value)
    {
        if (string.Equals(key, "Dalashade_SurfaceReflectionDebugMode", StringComparison.OrdinalIgnoreCase))
        {
            var rounded = (int)MathF.Round(value);
            var clamped = Math.Min(10, Math.Max(0, rounded));
            return new ShaderAdjustmentResult(clamped.ToString(CultureInfo.InvariantCulture), rounded < 0, rounded > 10);
        }

        if (string.Equals(key, "Dalashade_SurfaceReflectionDebugOutputMode", StringComparison.OrdinalIgnoreCase))
        {
            var rounded = (int)MathF.Round(value);
            var clamped = Math.Min(4, Math.Max(0, rounded));
            return new ShaderAdjustmentResult(clamped.ToString(CultureInfo.InvariantCulture), rounded < 0, rounded > 4);
        }

        if (string.Equals(key, "Dalashade_SurfaceReflectionEnabled", StringComparison.OrdinalIgnoreCase))
        {
            var enabled = value >= 0.5f ? 1 : 0;
            return new ShaderAdjustmentResult(enabled.ToString(CultureInfo.InvariantCulture), false, false);
        }

        if (string.Equals(key, "Dalashade_WaterSheenRadius", StringComparison.OrdinalIgnoreCase))
        {
            var clamped = MathF.Min(8f, MathF.Max(0.25f, value));
            return new ShaderAdjustmentResult(Format(clamped), value < 0.25f, value > 8f);
        }

        if (string.Equals(key, "Dalashade_ReflectionSampleOffset", StringComparison.OrdinalIgnoreCase))
        {
            var clamped = MathF.Min(0.08f, MathF.Max(0.002f, value));
            return new ShaderAdjustmentResult(Format(clamped), value < 0.002f, value > 0.08f);
        }

        if (string.Equals(key, "Dalashade_SurfaceReflectionDebugBoost", StringComparison.OrdinalIgnoreCase))
        {
            var clamped = MathF.Min(8f, MathF.Max(0.25f, value));
            return new ShaderAdjustmentResult(Format(clamped), value < 0.25f, value > 8f);
        }

        var normalized = Clamp01(value);
        return new ShaderAdjustmentResult(Format(normalized), value < 0f, value > 1f);
    }

    private static bool IsSupportedMaterialSectionVariable(string section, string key)
    {
        return (SmartSharpenAuthority.IsSmartSharpenSection(section) && SmartSharpenMaterialVariables.Contains(key))
               || (IsWeatherAtmosphereSection(section) && WeatherAtmosphereMaterialVariables.Contains(key))
               || (IsAtmosphereBloomSection(section) && AtmosphereBloomMaterialVariables.Contains(key))
               || (IsAdaptiveGradeSection(section) && AdaptiveGradeMaterialVariables.Contains(key))
               || (IsSceneGISection(section) && SceneGIMaterialVariables.Contains(key))
               || (IsSurfaceReflectionSection(section) && SurfaceReflectionMaterialVariables.Contains(key))
               || (IsMaterialDebugSection(section) && MaterialDebugVariables.Contains(key));
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

    private static bool IsMaterialDebugSection(string section)
    {
        return string.Equals(section, "Dalashade_MaterialDebug.fx", StringComparison.OrdinalIgnoreCase)
               || section.Contains("Dalashade_MaterialDebug", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSceneGISection(string section)
    {
        return string.Equals(section, "Dalashade_SceneGI.fx", StringComparison.OrdinalIgnoreCase)
               || section.Contains("Dalashade_SceneGI", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSurfaceReflectionSection(string section)
    {
        return string.Equals(section, "Dalashade_SurfaceReflection.fx", StringComparison.OrdinalIgnoreCase)
               || section.Contains("Dalashade_SurfaceReflection", StringComparison.OrdinalIgnoreCase);
    }

    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
