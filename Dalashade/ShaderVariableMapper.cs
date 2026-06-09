using System;
using System.Collections.Generic;
using System.Globalization;

namespace Dalashade;

public sealed class ShaderVariableMapper
{
    public IReadOnlyDictionary<string, Func<string, string?>> CreateAdjustments(VisualProfile profile, Configuration configuration)
    {
        var exposureDelta = profile.Exposure - 1f;
        var contrastDelta = profile.Contrast - 1f;
        var saturationDelta = profile.Saturation - 1f;
        var shadowLiftDelta = profile.ShadowLift;
        var temperatureDelta = profile.Temperature;
        var tintDelta = profile.Tint;

        var adjustments = new Dictionary<string, Func<string, string?>>(StringComparer.OrdinalIgnoreCase)
        {
            // Free iMMERSE
            ["MXAO_SSAO_AMOUNT"] = value => Scale(value, profile.AmbientOcclusion, 0f, 4f),
            ["SHARP_AMT"] = value => Scale(value, profile.Sharpness, 0f, 4f),
            ["SMAA_THRESHOLD"] = value => Add(value, profile.Sharpness < 0.95f ? 0.02f : 0f, 0.02f, 0.50f),

            // Common free ReShade/qUINT-style variables. These only apply if present.
            ["Exposure"] = value => Scale(value, profile.Exposure, -5f, 5f),
            ["Gamma"] = value => Add(value, shadowLiftDelta * 0.35f, 0.25f, 4f),
            ["Contrast"] = value => Scale(value, profile.Contrast, -5f, 5f),
            ["Saturation"] = value => Scale(value, profile.Saturation, -5f, 5f),
            ["BloomIntensity"] = value => Scale(value, profile.Bloom, 0f, 10f),
            ["BloomThreshold"] = value => Add(value, profile.Bloom < 1f ? (1f - profile.Bloom) * 0.5f : 0f, 0f, 10f),
            ["Sharpness"] = value => Scale(value, profile.Sharpness, 0f, 10f),
            ["Clarity"] = value => Scale(value, profile.Clarity, 0f, 10f),

            // MagicBloom, installed in the user's ReShade setup.
            ["fBloom_Intensity"] = value => Scale(value, profile.Bloom, 0f, 10f),
            ["fBloom_Threshold"] = value => Add(value, profile.Bloom < 1f ? (1f - profile.Bloom) * 0.5f : 0f, 0f, 10f),
            ["fExposure"] = value => Add(value, exposureDelta, -5f, 5f),
            ["fSaturation"] = value => Add(value, saturationDelta, -5f, 5f)
        };

        if (configuration.UsePremiumImmerseEffects)
        {
            // iMMERSE Pro: Clarity
            adjustments["TEXTURE_INTENSITY"] = value => Scale(value, profile.Clarity, 0f, 2f);
            adjustments["HDR_INTENSITY"] = value => Scale(value, profile.Clarity, 0f, 2f);
            adjustments["TEXTURE_INTENSITY_FG"] = value => Scale(value, profile.Clarity, 0f, 2f);
            adjustments["HDR_INTENSITY_FG"] = value => Scale(value, profile.Clarity, 0f, 2f);
            adjustments["TEXTURE_INTENSITY_BG"] = value => Scale(value, profile.Clarity, 0f, 2f);
            adjustments["HDR_INTENSITY_BG"] = value => Scale(value, profile.Clarity, 0f, 2f);

            // iMMERSE Pro: ReGrade
            adjustments["GRADE_EXPOSURE"] = value => Add(value, exposureDelta, -5f, 5f);
            adjustments["GRADE_CONTRAST"] = value => Add(value, contrastDelta * 0.5f, -5f, 5f);
            adjustments["GRADE_GAMMA"] = value => Add(value, shadowLiftDelta * 0.5f, -5f, 5f);
            adjustments["GRADE_SATURATION"] = value => Add(value, saturationDelta * 0.5f, -5f, 5f);
            adjustments["GRADE_VIBRANCE"] = value => Add(value, saturationDelta * 0.5f, -5f, 5f);
            adjustments["INPUT_COLOR_TEMPERATURE"] = value => Add(value, temperatureDelta * 1800f, 3000f, 12000f);
            adjustments["INPUT_COLOR_LAB_A"] = value => Add(value, tintDelta * 0.40f, -1f, 1f);
            adjustments["INPUT_COLOR_LAB_B"] = value => Add(value, temperatureDelta * 0.40f, -1f, 1f);
            adjustments["TONECURVE_SHADOWS"] = value => Add(value, shadowLiftDelta * 0.25f, -1f, 1f);
            adjustments["TONECURVE_DARKS"] = value => Add(value, shadowLiftDelta * 0.15f, -1f, 1f);

            // iMMERSE Ultimate: ReGrade+
            adjustments["E_EXPOSURE"] = value => Add(value, exposureDelta, -5f, 5f);
            adjustments["E_CONTRAST"] = value => Add(value, contrastDelta * 0.5f, -5f, 5f);
            adjustments["E_GAMMA"] = value => Add(value, shadowLiftDelta * 0.5f, -5f, 5f);
            adjustments["E_SATURATION"] = value => Add(value, saturationDelta * 0.5f, -5f, 5f);
            adjustments["E_VIBRANCE"] = value => Add(value, saturationDelta * 0.5f, -5f, 5f);
            adjustments["E_TEMP"] = value => Add(value, temperatureDelta * 0.50f, -1f, 1f);
            adjustments["E_TINT"] = value => Add(value, tintDelta * 0.50f, -1f, 1f);

            // iMMERSE RTGI diffuse/specular. These only apply once those sections exist in a preset.
            adjustments["RT_AO_AMOUNT"] = value => Scale(value, profile.AmbientOcclusion, 0f, 10f);
            adjustments["RT_IL_AMOUNT"] = value => Scale(value, profile.AmbientOcclusion, 0f, 10f);
            adjustments["RT_AMBIENT_LEVEL"] = value => Scale(value, profile.AmbientOcclusion, 0f, 10f);
            adjustments["RT_ROUGHNESS"] = value => Scale(value, profile.AmbientOcclusion, 0f, 1f);
        }

        return adjustments;
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
