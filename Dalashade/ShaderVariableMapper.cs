using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dalashade;

public enum ShaderValueMode
{
    Scale,
    Add,
    InvertAdd,
    RelativeAdd
}

public readonly record struct ShaderVariableKey(string? Section, string Key);

public sealed record ShaderAdjustment(Func<string, ShaderAdjustmentResult?> Apply, string ReasonCategory);

public sealed record ShaderAdjustmentResult(string NewValue, bool HitMin, bool HitMax);

public sealed record ShaderVariableDefinition(
    string? Section,
    string Key,
    ShaderValueMode Mode,
    string ReasonCategory,
    float Min,
    float Max,
    Func<VisualProfile, float> Amount,
    bool AllowFallback = false);

public sealed class ShaderVariableKeyComparer : IEqualityComparer<ShaderVariableKey>
{
    public static ShaderVariableKeyComparer Instance { get; } = new();

    public bool Equals(ShaderVariableKey x, ShaderVariableKey y)
    {
        return string.Equals(x.Section, y.Section, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(ShaderVariableKey obj)
    {
        return HashCode.Combine(
            obj.Section == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Section),
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key));
    }
}

public sealed class ShaderVariableMapper
{
    public IReadOnlyDictionary<ShaderVariableKey, ShaderAdjustment> CreateAdjustments(VisualProfile profile, Configuration configuration)
    {
        var adjustments = new Dictionary<ShaderVariableKey, ShaderAdjustment>(ShaderVariableKeyComparer.Instance);

        foreach (var definition in CreateDefinitions(configuration))
        {
            Add(adjustments, definition, profile, definition.Section);
            if (configuration.ShaderMatchingMode == ShaderMatchingMode.KnownFallbacks && definition.AllowFallback)
            {
                Add(adjustments, definition, profile, null);
            }
        }

        return adjustments;
    }

    public IReadOnlyList<ShaderVariableDefinition> CreateDefinitions(Configuration configuration)
    {
        var definitions = new List<ShaderVariableDefinition>();

        AddCoreDefinitions(definitions);
        AddCommonOptionalDefinitions(definitions);

        if (configuration.UsePremiumImmerseEffects)
        {
            AddPremiumDefinitions(definitions);
        }

        return definitions;
    }

    private static void AddCoreDefinitions(List<ShaderVariableDefinition> definitions)
    {
        AddScale(definitions, "MartysMods_MXAO.fx", "MXAO_SSAO_AMOUNT", "Ambient occlusion", 0f, 4f, profile => profile.AmbientOcclusion, true);
        AddScale(definitions, "MartysMods_MXAO.fx", "MXAO_SAMPLE_RADIUS", "AO radius", 0.10f, 10f, profile => profile.AoRadius, true);
        AddScale(definitions, "MartysMods_MXAO.fx", "MXAO_FADE_DEPTH", "AO fade", 0.02f, 1f, profile => profile.AoFadeDistance, true);

        AddScale(definitions, "MartysMods_SHARPEN.fx", "SHARP_AMT", "Sharpening", 0f, 4f, profile => profile.Sharpness, true);

        AddScale(definitions, "MagicBloom.fx", "fBloom_Intensity", "Bloom", 0f, 10f, profile => profile.Bloom, true);
        AddRelative(definitions, "MagicBloom.fx", "fBloom_Threshold", "Bloom threshold", 0f, 10f, profile => (profile.BloomThreshold - 1f) * 0.35f, true);
        AddScale(definitions, "MagicBloom.fx", "fDirt_Intensity", "Bloom dirt", 0f, 10f, profile => profile.BloomDirt, true);
        AddAdd(definitions, "MagicBloom.fx", "fExposure", "Exposure", -5f, 5f, profile => profile.Exposure - 1f, true);
        AddAdd(definitions, "MagicBloom.fx", "fSaturation", "Saturation", -5f, 5f, profile => profile.Saturation - 1f, true);

        AddScale(definitions, "Deband.fx", "range", "Deband", 1f, 96f, profile => profile.DebandStrength, true);
        AddRelative(definitions, "Deband.fx", "t1", "Deband", 0.0001f, 0.10f, profile => (profile.DebandStrength - 1f) * 0.003f, true);
        AddRelative(definitions, "Deband.fx", "t2", "Deband", 0.0001f, 0.20f, profile => (profile.DebandStrength - 1f) * 0.015f, true);

        AddScale(definitions, "MartysMods_CLARITY.fx", "TEXTURE_INTENSITY", "Clarity", 0f, 2f, profile => profile.Clarity, true);
        AddScale(definitions, "MartysMods_CLARITY.fx", "HDR_INTENSITY", "Clarity", 0f, 2f, profile => profile.Clarity, true);
        AddScale(definitions, "MartysMods_CLARITY.fx", "TEXTURE_INTENSITY_FG", "Clarity", 0f, 2f, profile => profile.Clarity, true);
        AddScale(definitions, "MartysMods_CLARITY.fx", "HDR_INTENSITY_FG", "Clarity", 0f, 2f, profile => profile.Clarity, true);
        AddScale(definitions, "MartysMods_CLARITY.fx", "TEXTURE_INTENSITY_BG", "Clarity", 0f, 2f, profile => profile.Clarity, true);
        AddScale(definitions, "MartysMods_CLARITY.fx", "HDR_INTENSITY_BG", "Clarity", 0f, 2f, profile => profile.Clarity, true);
        AddScale(definitions, "MartysMods_CLARITY.fx", "EFFECT_RADIUS", "Clarity radius", 0.05f, 2f, profile => profile.DepthEffects, true);

        AddReGrade(definitions, "MartysMods_REGRADE.fx", "ReGrade");
    }

    private static void AddCommonOptionalDefinitions(List<ShaderVariableDefinition> definitions)
    {
        AddAdd(definitions, "qUINT_lightroom.fx", "Exposure", "Free color", -5f, 5f, profile => profile.Exposure - 1f);
        AddAdd(definitions, "qUINT_lightroom.fx", "Gamma", "Free color", 0.25f, 4f, profile => profile.ShadowLift * 0.35f);
        AddAdd(definitions, "qUINT_lightroom.fx", "Contrast", "Free color", -5f, 5f, profile => (profile.Contrast - 1f) * 0.5f);
        AddAdd(definitions, "qUINT_lightroom.fx", "Saturation", "Free color", -5f, 5f, profile => (profile.Saturation - 1f) * 0.5f);
        AddScale(definitions, "qUINT_lightroom.fx", "BloomIntensity", "Bloom", 0f, 10f, profile => profile.Bloom);
        AddRelative(definitions, "qUINT_lightroom.fx", "BloomThreshold", "Bloom threshold", 0f, 10f, profile => (profile.BloomThreshold - 1f) * 0.35f);
        AddScale(definitions, "qUINT_lightroom.fx", "Sharpness", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "qUINT_lightroom.fx", "Clarity", "Clarity", 0f, 10f, profile => profile.Clarity);

        AddAdd(definitions, "prod80_04_ColorTemperature.fx", "Kelvin", "Free color", 1000f, 40000f, profile => profile.Temperature * 1800f);
        AddAdd(definitions, "prod80_04_ColorTemperature.fx", "Temperature", "Free color", -1f, 1f, profile => profile.Temperature * 0.5f);
        AddAdd(definitions, "prod80_04_ColorTemperature.fx", "Tint", "Free color", -1f, 1f, profile => profile.Tint * 0.5f);

        AddInvert(definitions, "MartysMods_SMAA.fx", "SMAA_THRESHOLD", "Anti-aliasing", 0.01f, 0.50f, profile => (profile.AntiAliasingStrength - 1f) * 0.03f, true);
        AddInvert(definitions, "SMAA.fx", "SMAA_THRESHOLD", "Anti-aliasing", 0.01f, 0.50f, profile => (profile.AntiAliasingStrength - 1f) * 0.03f);

        AddScale(definitions, "qUINT_deband.fx", "DEBAND_RADIUS", "Deband", 1f, 128f, profile => profile.DebandStrength);
        AddRelative(definitions, "qUINT_deband.fx", "DEBAND_THRESHOLD", "Deband", 0.0001f, 0.20f, profile => (profile.DebandStrength - 1f) * 0.015f);

        AddScale(definitions, "LUT.fx", "fLUT_AmountChroma", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "LUT.fx", "fLUT_AmountLuma", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MartysMods_LUTMANAGER.fx", "LUT_INTENSITY", "LUT", 0f, 1f, profile => profile.LutStrength);
    }

    private static void AddPremiumDefinitions(List<ShaderVariableDefinition> definitions)
    {
        AddReGradePlus(definitions, "MartysMods_REGRADE+.fx", "ReGrade+");

        AddScale(definitions, "MartysMods_RTGI_DIFFUSE.fx", "RT_AO_AMOUNT", "RTGI AO", 0f, 10f, profile => profile.Rtgi, true);
        AddScale(definitions, "MartysMods_RTGI_DIFFUSE.fx", "RT_IL_AMOUNT", "RTGI indirect light", 0f, 10f, profile => profile.Rtgi, true);
        AddScale(definitions, "MartysMods_RTGI_DIFFUSE.fx", "RT_AMBIENT_LEVEL", "RTGI ambient", 0f, 10f, profile => profile.DepthEffects, true);
        AddScale(definitions, "MartysMods_RTGI_SPECULAR.fx", "RT_ROUGHNESS", "RTGI specular", 0f, 1f, profile => profile.Rtgi, true);

        AddScale(definitions, "MartysMods_RELIGHT.fx", "RELIGHT_INTENSITY", "ReLight", 0f, 10f, profile => profile.ReLight, true);
        AddScale(definitions, "MartysMods_RELIGHT.fx", "RELIGHT_RANGE", "ReLight", 0f, 10f, profile => profile.DepthEffects, true);
    }

    private static void AddReGrade(List<ShaderVariableDefinition> definitions, string section, string reason)
    {
        AddAdd(definitions, section, "GRADE_EXPOSURE", reason, -5f, 5f, profile => profile.Exposure - 1f, true);
        AddAdd(definitions, section, "GRADE_CONTRAST", reason, -5f, 5f, profile => (profile.Contrast - 1f) * 0.5f, true);
        AddAdd(definitions, section, "GRADE_GAMMA", reason, -5f, 5f, profile => profile.ShadowLift * 0.5f, true);
        AddAdd(definitions, section, "GRADE_FILMIC_GAMMA", reason, -5f, 5f, profile => (profile.MidtoneContrast - 1f) * 0.25f, true);
        AddAdd(definitions, section, "GRADE_SATURATION", reason, -5f, 5f, profile => (profile.Saturation - 1f) * 0.5f, true);
        AddAdd(definitions, section, "GRADE_VIBRANCE", reason, -5f, 5f, profile => (profile.Saturation - 1f) * 0.5f, true);
        AddAdd(definitions, section, "INPUT_COLOR_TEMPERATURE", reason, 3000f, 12000f, profile => profile.Temperature * 1800f, true);
        AddAdd(definitions, section, "INPUT_COLOR_LAB_A", reason, -1f, 1f, profile => profile.Tint * 0.40f, true);
        AddAdd(definitions, section, "INPUT_COLOR_LAB_B", reason, -1f, 1f, profile => profile.Temperature * 0.40f, true);
        AddAdd(definitions, section, "TONECURVE_SHADOWS", reason, -1f, 1f, profile => profile.ShadowLift * 0.25f, true);
        AddAdd(definitions, section, "TONECURVE_DARKS", reason, -1f, 1f, profile => profile.ShadowLift * 0.15f, true);
        AddAdd(definitions, section, "TONECURVE_HIGHLIGHTS", reason, -1f, 1f, profile => -(profile.HighlightRecovery - 1f) * 0.25f, true);
        AddAdd(definitions, section, "TONECURVE_LIGHTS", reason, -1f, 1f, profile => -(profile.HighlightRecovery - 1f) * 0.15f, true);
        AddRelative(definitions, section, "INPUT_BLACK_LVL", reason, 0f, 255f, profile => (profile.BlackPoint - 1f) * 12f, true);
        AddRelative(definitions, section, "INPUT_WHITE_LVL", reason, 0f, 255f, profile => (profile.WhitePoint - 1f) * 24f, true);
        AddRelative(definitions, section, "OUTPUT_BLACK_LVL", reason, 0f, 255f, profile => (profile.BlackPoint - 1f) * 12f, true);
        AddRelative(definitions, section, "OUTPUT_WHITE_LVL", reason, 0f, 255f, profile => (profile.WhitePoint - 1f) * 24f, true);
    }

    private static void AddReGradePlus(List<ShaderVariableDefinition> definitions, string section, string reason)
    {
        AddAdd(definitions, section, "E_EXPOSURE", reason, -5f, 5f, profile => profile.Exposure - 1f, true);
        AddAdd(definitions, section, "E_CONTRAST", reason, -5f, 5f, profile => (profile.Contrast - 1f) * 0.5f, true);
        AddAdd(definitions, section, "E_GAMMA", reason, -5f, 5f, profile => profile.ShadowLift * 0.5f, true);
        AddAdd(definitions, section, "E_SATURATION", reason, -5f, 5f, profile => (profile.Saturation - 1f) * 0.5f, true);
        AddAdd(definitions, section, "E_VIBRANCE", reason, -5f, 5f, profile => (profile.Saturation - 1f) * 0.5f, true);
        AddAdd(definitions, section, "E_TEMP", reason, -1f, 1f, profile => profile.Temperature * 0.50f, true);
        AddAdd(definitions, section, "E_TINT", reason, -1f, 1f, profile => profile.Tint * 0.50f, true);
    }

    private static void AddScale(List<ShaderVariableDefinition> definitions, string? section, string key, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        definitions.Add(new ShaderVariableDefinition(section, key, ShaderValueMode.Scale, reason, min, max, amount, allowFallback));
    }

    private static void AddAdd(List<ShaderVariableDefinition> definitions, string? section, string key, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        definitions.Add(new ShaderVariableDefinition(section, key, ShaderValueMode.Add, reason, min, max, amount, allowFallback));
    }

    private static void AddInvert(List<ShaderVariableDefinition> definitions, string? section, string key, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        definitions.Add(new ShaderVariableDefinition(section, key, ShaderValueMode.InvertAdd, reason, min, max, amount, allowFallback));
    }

    private static void AddRelative(List<ShaderVariableDefinition> definitions, string? section, string key, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        definitions.Add(new ShaderVariableDefinition(section, key, ShaderValueMode.RelativeAdd, reason, min, max, amount, allowFallback));
    }

    private static void Add(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, ShaderVariableDefinition definition, VisualProfile profile, string? section)
    {
        adjustments[new ShaderVariableKey(section, definition.Key)] = new ShaderAdjustment(value => Apply(value, definition, profile), definition.ReasonCategory);
    }

    private static ShaderAdjustmentResult? Apply(string rawValue, ShaderVariableDefinition definition, VisualProfile profile)
    {
        if (!TryParseSingle(rawValue, out var current))
        {
            return null;
        }

        var result = ApplyMode(current, definition.Amount(profile), definition.Mode, definition.Min, definition.Max);
        return new ShaderAdjustmentResult(Format(result.Value), result.HitMin, result.HitMax);
    }

    private static (float Value, bool HitMin, bool HitMax) ApplyMode(float current, float amount, ShaderValueMode mode, float min, float max)
    {
        var value = mode switch
        {
            ShaderValueMode.Scale => current * amount,
            ShaderValueMode.Add => current + amount,
            ShaderValueMode.InvertAdd => current - amount,
            ShaderValueMode.RelativeAdd => current + amount,
            _ => current
        };

        var clamped = Clamp(value, min, max);
        var hitMin = value < min;
        var hitMax = value > max;
        return (clamped, hitMin, hitMax);
    }

    private static bool TryParseSingle(string rawValue, out float value)
    {
        return float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));

    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
