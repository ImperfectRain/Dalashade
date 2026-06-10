using System;
using System.Collections.Generic;
using System.Globalization;

namespace Dalashade;

public readonly record struct ShaderVariableKey(string? Section, string Key);

public sealed record ShaderAdjustment(Func<string, string?> Apply, string ReasonCategory);

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
        var exposureDelta = profile.Exposure - 1f;
        var contrastDelta = profile.Contrast - 1f;
        var saturationDelta = profile.Saturation - 1f;
        var shadowLiftDelta = profile.ShadowLift;
        var temperatureDelta = profile.Temperature;
        var tintDelta = profile.Tint;

        var adjustments = new Dictionary<ShaderVariableKey, ShaderAdjustment>(ShaderVariableKeyComparer.Instance);

        Add(adjustments, "MartysMods_MXAO.fx", "MXAO_SSAO_AMOUNT", value => Scale(value, profile.AmbientOcclusion, 0f, 4f), "Ambient occlusion");
        Add(adjustments, null, "MXAO_SSAO_AMOUNT", value => Scale(value, profile.AmbientOcclusion, 0f, 4f), "Ambient occlusion");

        Add(adjustments, "MartysMods_SHARPEN.fx", "SHARP_AMT", value => Scale(value, profile.Sharpness, 0f, 4f), "Sharpening");
        Add(adjustments, null, "SHARP_AMT", value => Scale(value, profile.Sharpness, 0f, 4f), "Sharpening");

        Add(adjustments, "MartysMods_SMAA.fx", "SMAA_THRESHOLD", value => Add(value, profile.Sharpness < 0.95f ? 0.02f : 0f, 0.02f, 0.50f), "Anti-aliasing");
        Add(adjustments, null, "SMAA_THRESHOLD", value => Add(value, profile.Sharpness < 0.95f ? 0.02f : 0f, 0.02f, 0.50f), "Anti-aliasing");

        Add(adjustments, "MagicBloom.fx", "fBloom_Intensity", value => Scale(value, profile.Bloom, 0f, 10f), "Bloom");
        Add(adjustments, "MagicBloom.fx", "fBloom_Threshold", value => Add(value, profile.Bloom < 1f ? (1f - profile.Bloom) * 0.5f : 0f, 0f, 10f), "Bloom");
        Add(adjustments, "MagicBloom.fx", "fExposure", value => Add(value, exposureDelta, -5f, 5f), "Exposure");
        Add(adjustments, "MagicBloom.fx", "fSaturation", value => Add(value, saturationDelta, -5f, 5f), "Saturation");
        Add(adjustments, null, "fBloom_Intensity", value => Scale(value, profile.Bloom, 0f, 10f), "Bloom");
        Add(adjustments, null, "fBloom_Threshold", value => Add(value, profile.Bloom < 1f ? (1f - profile.Bloom) * 0.5f : 0f, 0f, 10f), "Bloom");

        AddColorSuite(adjustments, "qUINT_lightroom.fx", profile, shadowLiftDelta, "Free color");
        AddColorSuite(adjustments, "prod80_04_ColorTemperature.fx", profile, shadowLiftDelta, "Free color");

        if (configuration.UsePremiumImmerseEffects)
        {
            AddClarity(adjustments, profile);
            AddReGrade(adjustments, "MartysMods_REGRADE.fx", exposureDelta, contrastDelta, saturationDelta, shadowLiftDelta, temperatureDelta, tintDelta);
            AddReGradePlus(adjustments, "MartysMods_REGRADE+.fx", exposureDelta, contrastDelta, saturationDelta, shadowLiftDelta, temperatureDelta, tintDelta);
            AddRtgi(adjustments, profile);
            AddReLight(adjustments, profile);
        }

        return adjustments;
    }

    public IReadOnlySet<ShaderVariableKey> CreateSupportedKeys(Configuration configuration)
    {
        return new HashSet<ShaderVariableKey>(CreateAdjustments(VisualProfile.Neutral, configuration).Keys, ShaderVariableKeyComparer.Instance);
    }

    private static void AddColorSuite(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, string section, VisualProfile profile, float shadowLiftDelta, string reason)
    {
        Add(adjustments, section, "Exposure", value => Scale(value, profile.Exposure, -5f, 5f), reason);
        Add(adjustments, section, "Gamma", value => Add(value, shadowLiftDelta * 0.35f, 0.25f, 4f), reason);
        Add(adjustments, section, "Contrast", value => Scale(value, profile.Contrast, -5f, 5f), reason);
        Add(adjustments, section, "Saturation", value => Scale(value, profile.Saturation, -5f, 5f), reason);
        Add(adjustments, section, "BloomIntensity", value => Scale(value, profile.Bloom, 0f, 10f), reason);
        Add(adjustments, section, "BloomThreshold", value => Add(value, profile.Bloom < 1f ? (1f - profile.Bloom) * 0.5f : 0f, 0f, 10f), reason);
        Add(adjustments, section, "Sharpness", value => Scale(value, profile.Sharpness, 0f, 10f), reason);
        Add(adjustments, section, "Clarity", value => Scale(value, profile.Clarity, 0f, 10f), reason);
    }

    private static void AddClarity(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, VisualProfile profile)
    {
        var keys = new[]
        {
            "TEXTURE_INTENSITY",
            "HDR_INTENSITY",
            "TEXTURE_INTENSITY_FG",
            "HDR_INTENSITY_FG",
            "TEXTURE_INTENSITY_BG",
            "HDR_INTENSITY_BG"
        };

        foreach (var key in keys)
        {
            Add(adjustments, "MartysMods_CLARITY.fx", key, value => Scale(value, profile.Clarity, 0f, 2f), "Clarity");
            Add(adjustments, null, key, value => Scale(value, profile.Clarity, 0f, 2f), "Clarity");
        }
    }

    private static void AddReGrade(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, string section, float exposureDelta, float contrastDelta, float saturationDelta, float shadowLiftDelta, float temperatureDelta, float tintDelta)
    {
        Add(adjustments, section, "GRADE_EXPOSURE", value => Add(value, exposureDelta, -5f, 5f), "ReGrade");
        Add(adjustments, section, "GRADE_CONTRAST", value => Add(value, contrastDelta * 0.5f, -5f, 5f), "ReGrade");
        Add(adjustments, section, "GRADE_GAMMA", value => Add(value, shadowLiftDelta * 0.5f, -5f, 5f), "ReGrade");
        Add(adjustments, section, "GRADE_SATURATION", value => Add(value, saturationDelta * 0.5f, -5f, 5f), "ReGrade");
        Add(adjustments, section, "GRADE_VIBRANCE", value => Add(value, saturationDelta * 0.5f, -5f, 5f), "ReGrade");
        Add(adjustments, section, "INPUT_COLOR_TEMPERATURE", value => Add(value, temperatureDelta * 1800f, 3000f, 12000f), "ReGrade");
        Add(adjustments, section, "INPUT_COLOR_LAB_A", value => Add(value, tintDelta * 0.40f, -1f, 1f), "ReGrade");
        Add(adjustments, section, "INPUT_COLOR_LAB_B", value => Add(value, temperatureDelta * 0.40f, -1f, 1f), "ReGrade");
        Add(adjustments, section, "TONECURVE_SHADOWS", value => Add(value, shadowLiftDelta * 0.25f, -1f, 1f), "ReGrade");
        Add(adjustments, section, "TONECURVE_DARKS", value => Add(value, shadowLiftDelta * 0.15f, -1f, 1f), "ReGrade");
    }

    private static void AddReGradePlus(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, string section, float exposureDelta, float contrastDelta, float saturationDelta, float shadowLiftDelta, float temperatureDelta, float tintDelta)
    {
        Add(adjustments, section, "E_EXPOSURE", value => Add(value, exposureDelta, -5f, 5f), "ReGrade+");
        Add(adjustments, section, "E_CONTRAST", value => Add(value, contrastDelta * 0.5f, -5f, 5f), "ReGrade+");
        Add(adjustments, section, "E_GAMMA", value => Add(value, shadowLiftDelta * 0.5f, -5f, 5f), "ReGrade+");
        Add(adjustments, section, "E_SATURATION", value => Add(value, saturationDelta * 0.5f, -5f, 5f), "ReGrade+");
        Add(adjustments, section, "E_VIBRANCE", value => Add(value, saturationDelta * 0.5f, -5f, 5f), "ReGrade+");
        Add(adjustments, section, "E_TEMP", value => Add(value, temperatureDelta * 0.50f, -1f, 1f), "ReGrade+");
        Add(adjustments, section, "E_TINT", value => Add(value, tintDelta * 0.50f, -1f, 1f), "ReGrade+");
    }

    private static void AddRtgi(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, VisualProfile profile)
    {
        Add(adjustments, "MartysMods_RTGI_DIFFUSE.fx", "RT_AO_AMOUNT", value => Scale(value, profile.Rtgi, 0f, 10f), "RTGI");
        Add(adjustments, "MartysMods_RTGI_DIFFUSE.fx", "RT_IL_AMOUNT", value => Scale(value, profile.Rtgi, 0f, 10f), "RTGI");
        Add(adjustments, "MartysMods_RTGI_DIFFUSE.fx", "RT_AMBIENT_LEVEL", value => Scale(value, profile.Rtgi, 0f, 10f), "RTGI");
        Add(adjustments, "MartysMods_RTGI_SPECULAR.fx", "RT_ROUGHNESS", value => Scale(value, profile.Rtgi, 0f, 1f), "RTGI");
        Add(adjustments, null, "RT_AO_AMOUNT", value => Scale(value, profile.Rtgi, 0f, 10f), "RTGI");
        Add(adjustments, null, "RT_IL_AMOUNT", value => Scale(value, profile.Rtgi, 0f, 10f), "RTGI");
        Add(adjustments, null, "RT_AMBIENT_LEVEL", value => Scale(value, profile.Rtgi, 0f, 10f), "RTGI");
        Add(adjustments, null, "RT_ROUGHNESS", value => Scale(value, profile.Rtgi, 0f, 1f), "RTGI");
    }

    private static void AddReLight(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, VisualProfile profile)
    {
        Add(adjustments, "MartysMods_RELIGHT.fx", "RELIGHT_INTENSITY", value => Scale(value, profile.ReLight, 0f, 10f), "ReLight");
        Add(adjustments, "MartysMods_RELIGHT.fx", "RELIGHT_RANGE", value => Scale(value, profile.DepthEffects, 0f, 10f), "ReLight");
        Add(adjustments, null, "RELIGHT_INTENSITY", value => Scale(value, profile.ReLight, 0f, 10f), "ReLight");
        Add(adjustments, null, "RELIGHT_RANGE", value => Scale(value, profile.DepthEffects, 0f, 10f), "ReLight");
    }

    private static void Add(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, string? section, string key, Func<string, string?> apply, string reasonCategory)
    {
        adjustments[new ShaderVariableKey(section, key)] = new ShaderAdjustment(apply, reasonCategory);
    }

    private static string? Scale(string rawValue, float multiplier, float min, float max)
    {
        return TryParseSingle(rawValue, out var current)
            ? Format(Clamp(current * multiplier, min, max))
            : null;
    }

    private static string? Add(string rawValue, float delta, float min, float max)
    {
        return TryParseSingle(rawValue, out var current)
            ? Format(Clamp(current + delta, min, max))
            : null;
    }

    private static bool TryParseSingle(string rawValue, out float value)
    {
        return float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));

    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
