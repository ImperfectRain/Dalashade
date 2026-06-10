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
    RelativeAdd,
    ScaleAll,
    AddAll,
    ScaleComponent,
    AddComponent,
    MoveTowardNeutral
}

public enum ShaderValueShape
{
    Scalar,
    Vector2,
    Vector3,
    Vector4
}

public readonly record struct ShaderVariableKey(string? Section, string Key);

public readonly record struct ShaderValue(float X, float Y = 0f, float Z = 0f, float W = 0f)
{
    public float ComponentAt(int index)
    {
        return index switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            3 => W,
            _ => X
        };
    }

    public ShaderValue WithComponent(int index, float value)
    {
        return index switch
        {
            0 => this with { X = value },
            1 => this with { Y = value },
            2 => this with { Z = value },
            3 => this with { W = value },
            _ => this
        };
    }
}

public sealed record ShaderAdjustment(Func<string, ShaderAdjustmentResult?> Apply, string ReasonCategory, EffectRole Role, float AuthorityAdjustmentStrength);

public sealed record ShaderAdjustmentResult(string NewValue, bool HitMin, bool HitMax, string? Warning = null);

public sealed record ShaderVariableDefinition(
    string? Section,
    string Key,
    ShaderValueShape Shape,
    ShaderValueMode Mode,
    EffectRole Role,
    string ReasonCategory,
    float Min,
    float Max,
    Func<VisualProfile, ShaderValue> Amount,
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
        return CreateAdjustments(profile, configuration, GenerationAuthorityPolicy.Empty);
    }

    public IReadOnlyDictionary<ShaderVariableKey, ShaderAdjustment> CreateAdjustments(VisualProfile profile, Configuration configuration, GenerationAuthorityPolicy authorityPolicy)
    {
        var adjustments = new Dictionary<ShaderVariableKey, ShaderAdjustment>(ShaderVariableKeyComparer.Instance);

        foreach (var definition in CreateDefinitions(configuration))
        {
            Add(adjustments, definition, profile, authorityPolicy, definition.Section);
            if (configuration.ShaderMatchingMode == ShaderMatchingMode.KnownFallbacks && definition.AllowFallback)
            {
                Add(adjustments, definition, profile, authorityPolicy, null);
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

        AddNonImmerseBloomDefinitions(definitions);
        AddNonImmerseSharpenDefinitions(definitions);
        AddNonImmerseColorDefinitions(definitions);
    }

    private static void AddNonImmerseBloomDefinitions(List<ShaderVariableDefinition> definitions)
    {
        AddScale(definitions, "GaussianBloom.fx", "GaussianBloomStrength", "Bloom", 0f, 10f, profile => profile.Bloom);
        AddScale(definitions, "GaussianBloom.fx", "GaussianBloomRadius", "Bloom radius", 0.01f, 10f, profile => profile.BloomRadius);
        AddScale(definitions, "GaussianBloom.fx", "GaussianBloomSaturation", "Bloom saturation", 0f, 10f, profile => profile.Saturation);
        AddRelative(definitions, "GaussianBloom.fx", "Threshold", "Bloom threshold", 0f, 10f, profile => (profile.BloomThreshold - 1f) * 0.35f);
        AddAdd(definitions, "GaussianBloom.fx", "Exposure", "Bloom exposure", -5f, 5f, profile => profile.Exposure - 1f);

        AddScale(definitions, "qUINT_bloom.fx", "BLOOM_INTENSITY", "Bloom", 0f, 10f, profile => profile.Bloom);
        AddScale(definitions, "qUINT_bloom.fx", "BLOOM_ADAPT_STRENGTH", "Bloom", 0f, 10f, profile => profile.Bloom);
        AddScale(definitions, "qUINT_bloom.fx", "BLOOM_SAT", "Bloom saturation", 0f, 10f, profile => profile.Saturation);
        AddAdd(definitions, "qUINT_bloom.fx", "BLOOM_ADAPT_EXPOSURE", "Bloom exposure", -5f, 5f, profile => profile.Exposure - 1f);
        for (var i = 1; i <= 7; i++)
        {
            AddScale(definitions, "qUINT_bloom.fx", $"BLOOM_LAYER_MULT_{i}", "Bloom layer", 0f, 10f, profile => profile.Bloom);
        }

        AddVectorScaleAll(definitions, "BloomingHDR.fx", "Bloom_Intensity", ShaderValueShape.Vector2, "Bloom", 0f, 10f, profile => profile.Bloom);
        AddScale(definitions, "BloomingHDR.fx", "HDR_Adjust", "Bloom HDR", 0f, 10f, profile => profile.Bloom);
        AddScale(definitions, "BloomingHDR.fx", "BloomSensitivity", "Bloom", 0f, 10f, profile => profile.Bloom);
        AddScale(definitions, "BloomingHDR.fx", "Bloom_Spread", "Bloom radius", 0.01f, 100f, profile => profile.BloomRadius);
        AddScale(definitions, "BloomingHDR.fx", "Spread", "Bloom radius", 0.01f, 100f, profile => profile.BloomRadius);
        AddAdd(definitions, "BloomingHDR.fx", "Exposure", "Bloom exposure", -5f, 5f, profile => profile.Exposure - 1f);
        AddAdd(definitions, "BloomingHDR.fx", "Exp", "Bloom exposure", -5f, 5f, profile => profile.Exposure - 1f);
        AddRelative(definitions, "BloomingHDR.fx", "WP", "Bloom white point", 0.01f, 10f, profile => (profile.WhitePoint - 1f) * 0.35f);

        AddScale(definitions, "Pirate_Bloom.fx", "BLOOM_STRENGTH", "Bloom", 0f, 10f, profile => profile.Bloom);
        AddScale(definitions, "Pirate_Bloom.fx", "BLOOM_RADIUS", "Bloom radius", 0.01f, 50f, profile => profile.BloomRadius);
        AddScale(definitions, "Pirate_Bloom.fx", "BLOOM_SATURATION", "Bloom saturation", 0f, 10f, profile => profile.Saturation);
        AddRelative(definitions, "Pirate_Bloom.fx", "BLOOM_THRESHOLD", "Bloom threshold", 0f, 10f, profile => (profile.BloomThreshold - 1f) * 0.35f);
    }

    private static void AddNonImmerseSharpenDefinitions(List<ShaderVariableDefinition> definitions)
    {
        AddScale(definitions, "FilmicSharpen.fx", "Strength", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "FineSharp.fx", "sstr", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "FineSharp.fx", "cstr", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "FineSharp.fx", "lstr", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "FineSharp.fx", "pstr", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "LumaSharpen.fx", "sharp_strength", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "LumaSharpen.fx", "sharp_clamp", "Sharpen clamp", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "CAS.fx", "Sharpening", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddAdd(definitions, "CAS.fx", "Contrast", "Sharpen contrast", -5f, 5f, profile => (profile.Contrast - 1f) * 0.25f);
        AddScale(definitions, "HighPassSharpen.fx", "HighPassSharpStrength", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "HighPassSharpen.fx", "HighPassSharpRadius", "Sharpen radius", 0.01f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "HighPassSharpen.fx", "HighPassLightIntensity", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "HighPassSharpen.fx", "HighPassDarkIntensity", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "AdaptiveSharpen.fx", "curve_height", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "AdaptiveSharpen.fx", "scale_lim", "Sharpening", 0f, 10f, profile => profile.Sharpness);
        AddScale(definitions, "AdaptiveSharpen.fx", "scale_cs", "Sharpening", 0f, 10f, profile => profile.Sharpness);
    }

    private static void AddNonImmerseColorDefinitions(List<ShaderVariableDefinition> definitions)
    {
        AddScale(definitions, "MultiLUT.fx", "fLUT_AmountChroma", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MultiLUT.fx", "fLUT_AmountChroma2", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MultiLUT.fx", "fLUT_AmountChroma3", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MultiLUT.fx", "fLUT_AmountLuma", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MultiLUT.fx", "fLUT_AmountLuma2", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MultiLUT.fx", "fLUT_AmountLuma3", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MultiLUT.fx", "fLUT_Intensity", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MultiLUT.fx", "fLUT_Intensity2", "LUT", 0f, 1f, profile => profile.LutStrength);
        AddScale(definitions, "MultiLUT.fx", "fLUT_Intensity3", "LUT", 0f, 1f, profile => profile.LutStrength);

        AddColorPreservation(definitions, "DPX.fx", "Strength", 0f, 1f);
        AddColorPreservation(definitions, "DPX.fx", "Colorfulness", 0f, 10f);
        AddColorPreservation(definitions, "DPX.fx", "Saturation", 0f, 10f);
        AddAdd(definitions, "DPX.fx", "Contrast", "Color grade contrast", -5f, 5f, profile => (profile.Contrast - 1f) * 0.25f);

        AddColorPreservation(definitions, "Vibrance.fx", "Vibrance", -1f, 1f);
        AddColorPreservation(definitions, "Colourfulness.fx", "colourfulness", 0f, 10f);

        AddColorPreservation(definitions, "Technicolor2.fx", "Strength", 0f, 1f);
        AddAdd(definitions, "Technicolor2.fx", "Brightness", "Color grade brightness", -5f, 5f, profile => (profile.Exposure - 1f) * 0.25f);
        AddAdd(definitions, "Technicolor2.fx", "Saturation", "Color grade saturation", -5f, 5f, profile => (profile.Saturation - 1f) * 0.25f);
        AddVectorMoveTowardNeutral(definitions, "Technicolor2.fx", "ColorStrength", ShaderValueShape.Vector3, "Color grade preservation", -1f, 1f, profile => 1f - profile.ColorGradePreservation);
    }

    private static void AddColorPreservation(List<ShaderVariableDefinition> definitions, string section, string key, float min, float max)
    {
        AddScale(definitions, section, key, "Color grade preservation", min, max, profile => profile.ColorGradePreservation);
    }

    private static void AddPremiumDefinitions(List<ShaderVariableDefinition> definitions)
    {
        AddReGradePlus(definitions, "MartysMods_REGRADE+.fx", "ReGrade+");
        AddCombinedRtgi(definitions, "MartysMods_RTGI.fx");
        AddFftBloom(definitions, "MartysMods_FFTBLOOM.fx");

        AddScale(definitions, "MartysMods_RTGI_DIFFUSE.fx", "RT_AO_AMOUNT", "RTGI AO", 0f, 10f, profile => profile.Rtgi, true);
        AddScale(definitions, "MartysMods_RTGI_DIFFUSE.fx", "RT_IL_AMOUNT", "RTGI indirect light", 0f, 10f, profile => profile.Rtgi, true);
        AddScale(definitions, "MartysMods_RTGI_DIFFUSE.fx", "RT_AMBIENT_LEVEL", "RTGI ambient", 0f, 10f, profile => profile.DepthEffects, true);
        AddScale(definitions, "MartysMods_RTGI_SPECULAR.fx", "RT_ROUGHNESS", "RTGI specular", 0f, 1f, profile => profile.Rtgi, true);

        AddScale(definitions, "MartysMods_RELIGHT.fx", "RELIGHT_INTENSITY", "ReLight", 0f, 10f, profile => profile.ReLight, true);
        AddScale(definitions, "MartysMods_RELIGHT.fx", "RELIGHT_RANGE", "ReLight", 0f, 10f, profile => profile.DepthEffects, true);
        AddScale(definitions, "MartysMods_RELIGHT.fx", "AMBIENT_INT", "ReLight ambient", 0f, 10f, profile => profile.ReLight, true);
        AddScale(definitions, "MartysMods_RELIGHT.fx", "LIGHT0_INT", "ReLight primary light", 0f, 10f, profile => profile.ReLight, true);
        AddScale(definitions, "MartysMods_RELIGHT.fx", "LIGHT1_INT", "ReLight secondary light", 0f, 10f, profile => profile.ReLight, true);
        AddScale(definitions, "MartysMods_RELIGHT.fx", "SHADOW_Q", "ReLight shadow quality", 0f, 10f, profile => profile.DepthEffects, true);
        AddScale(definitions, "MartysMods_RELIGHT.fx", "SSS_Q", "ReLight SSS quality", 0f, 10f, profile => profile.DepthEffects, true);
        AddScale(definitions, "MartysMods_RELIGHT.fx", "USE_SSS", "ReLight SSS toggle", 0f, 1f, _ => 1f, true);
    }

    private static void AddCombinedRtgi(List<ShaderVariableDefinition> definitions, string section)
    {
        AddScale(definitions, section, "RT_AO_AMOUNT", "RTGI AO", 0f, 10f, profile => profile.Rtgi, true);
        AddScale(definitions, section, "RT_IL_AMOUNT", "RTGI indirect light", 0f, 10f, profile => profile.Rtgi, true);
        AddScale(definitions, section, "RT_AMBIENT_LEVEL", "RTGI ambient", 0f, 10f, profile => profile.DepthEffects, true);
        AddScale(definitions, section, "RT_SPEC_AMOUNT", "RTGI specular", 0f, 10f, profile => profile.Rtgi, true);
        AddScale(definitions, section, "RT_ROUGHNESS", "RTGI roughness", 0f, 1f, profile => profile.Rtgi, true);
        AddScale(definitions, section, "RT_FADE_DEPTH", "RTGI fade depth", 0.001f, 1f, profile => profile.AoFadeDistance, true);
        AddScale(definitions, section, "RT_Z_THICKNESS", "RTGI depth thickness", 0.001f, 10f, profile => profile.DepthEffects, true);
    }

    private static void AddFftBloom(List<ShaderVariableDefinition> definitions, string section)
    {
        AddScale(definitions, section, "HDR_BLOOM_INT", "FFTBloom intensity", 0f, 10f, profile => profile.Bloom, true);
        AddAdd(definitions, section, "HDR_EXPOSURE", "FFTBloom exposure", -5f, 5f, profile => profile.Exposure - 1f, true);
        AddScale(definitions, section, "HDR_BLOOM_RADIUS", "FFTBloom radius", 0.01f, 10f, profile => profile.BloomRadius, true);
        AddScale(definitions, section, "HDR_BLOOM_HAZYNESS", "FFTBloom haziness", 0f, 10f, profile => profile.BloomDirt, true);
        AddRelative(definitions, section, "HDR_WHITEPOINT", "FFTBloom white point", 0.01f, 10f, profile => (profile.WhitePoint - 1f) * 0.35f, true);
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
        AddAdd(definitions, section, "E_TEMP", reason, 4500f, 8500f, profile => profile.Temperature * 1800f, true);
        AddAdd(definitions, section, "E_TINT", reason, -1f, 1f, profile => profile.Tint * 0.50f, true);
        AddScale(definitions, section, "E_SHADOWS_HUE", "ReGrade+ tonal color preservation", -1f, 1f, profile => profile.ColorGradePreservation, true);
        AddScale(definitions, section, "E_SHADOWS_SAT", "ReGrade+ tonal color preservation", -1f, 1f, profile => profile.ColorGradePreservation, true);
        AddScale(definitions, section, "E_MIDTONES_HUE", "ReGrade+ tonal color preservation", -1f, 1f, profile => profile.ColorGradePreservation, true);
        AddScale(definitions, section, "E_MIDTONES_SAT", "ReGrade+ tonal color preservation", -1f, 1f, profile => profile.ColorGradePreservation, true);
        AddScale(definitions, section, "E_HIGHLIGHTS_HUE", "ReGrade+ tonal color preservation", -1f, 1f, profile => profile.ColorGradePreservation, true);
        AddScale(definitions, section, "E_HIGHLIGHTS_SAT", "ReGrade+ tonal color preservation", -1f, 1f, profile => profile.ColorGradePreservation, true);

        AddColoristaPreservation(definitions, section, "E_COLORISTA_HSL_RED_V2");
        AddColoristaPreservation(definitions, section, "E_COLORISTA_HSL_ORANGE_V2");
        AddColoristaPreservation(definitions, section, "E_COLORISTA_HSL_YELLOW_V2");
        AddColoristaPreservation(definitions, section, "E_COLORISTA_HSL_GREEN_V2");
        AddColoristaPreservation(definitions, section, "E_COLORISTA_HSL_CYAN_V2");
        AddColoristaPreservation(definitions, section, "E_COLORISTA_HSL_BLUE_V2");
        AddColoristaPreservation(definitions, section, "E_COLORISTA_HSL_PURPLE_V2");
        AddColoristaPreservation(definitions, section, "E_COLORISTA_HSL_MAGENTA_V2");
    }

    private static void AddColoristaPreservation(List<ShaderVariableDefinition> definitions, string section, string key)
    {
        AddVectorMoveTowardNeutral(
            definitions,
            section,
            key,
            ShaderValueShape.Vector3,
            "ReGrade+ Colorista preservation",
            -1f,
            1f,
            profile => 1f - profile.ColorGradePreservation,
            true);
    }

    private static void AddScale(List<ShaderVariableDefinition> definitions, string? section, string key, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        AddScalar(definitions, section, key, ShaderValueMode.Scale, reason, min, max, amount, allowFallback);
    }

    private static void AddAdd(List<ShaderVariableDefinition> definitions, string? section, string key, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        AddScalar(definitions, section, key, ShaderValueMode.Add, reason, min, max, amount, allowFallback);
    }

    private static void AddInvert(List<ShaderVariableDefinition> definitions, string? section, string key, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        AddScalar(definitions, section, key, ShaderValueMode.InvertAdd, reason, min, max, amount, allowFallback);
    }

    private static void AddRelative(List<ShaderVariableDefinition> definitions, string? section, string key, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        AddScalar(definitions, section, key, ShaderValueMode.RelativeAdd, reason, min, max, amount, allowFallback);
    }

    private static void AddVectorScaleAll(List<ShaderVariableDefinition> definitions, string? section, string key, ShaderValueShape shape, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        AddVector(definitions, section, key, shape, ShaderValueMode.ScaleAll, reason, min, max, profile => new ShaderValue(amount(profile)), allowFallback);
    }

    private static void AddVectorAddAll(List<ShaderVariableDefinition> definitions, string? section, string key, ShaderValueShape shape, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        AddVector(definitions, section, key, shape, ShaderValueMode.AddAll, reason, min, max, profile => new ShaderValue(amount(profile)), allowFallback);
    }

    private static void AddVectorMoveTowardNeutral(List<ShaderVariableDefinition> definitions, string? section, string key, ShaderValueShape shape, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback = false)
    {
        AddVector(definitions, section, key, shape, ShaderValueMode.MoveTowardNeutral, reason, min, max, profile => new ShaderValue(amount(profile)), allowFallback);
    }

    private static void AddScalar(List<ShaderVariableDefinition> definitions, string? section, string key, ShaderValueMode mode, string reason, float min, float max, Func<VisualProfile, float> amount, bool allowFallback)
    {
        definitions.Add(new ShaderVariableDefinition(section, key, ShaderValueShape.Scalar, mode, InferRole(reason), reason, min, max, profile => new ShaderValue(amount(profile)), allowFallback));
    }

    private static void AddVector(List<ShaderVariableDefinition> definitions, string? section, string key, ShaderValueShape shape, ShaderValueMode mode, string reason, float min, float max, Func<VisualProfile, ShaderValue> amount, bool allowFallback = false)
    {
        if (shape == ShaderValueShape.Scalar)
        {
            throw new ArgumentException("Vector definitions must use a vector shape.", nameof(shape));
        }

        definitions.Add(new ShaderVariableDefinition(section, key, shape, mode, InferRole(reason), reason, min, max, amount, allowFallback));
    }

    private static void Add(Dictionary<ShaderVariableKey, ShaderAdjustment> adjustments, ShaderVariableDefinition definition, VisualProfile profile, GenerationAuthorityPolicy authorityPolicy, string? section)
    {
        var adjustmentStrength = authorityPolicy.GetAdjustmentStrength(section, definition.Role);
        adjustments[new ShaderVariableKey(section, definition.Key)] = new ShaderAdjustment(value => Apply(value, definition, profile, adjustmentStrength), definition.ReasonCategory, definition.Role, adjustmentStrength);
    }

    private static ShaderAdjustmentResult? Apply(string rawValue, ShaderVariableDefinition definition, VisualProfile profile, float adjustmentStrength)
    {
        if (definition.Shape == ShaderValueShape.Scalar)
        {
            if (!TryParseSingle(rawValue, out var current))
            {
                return null;
            }

            var amount = ApplyStrength(definition.Amount(profile).X, definition.Mode, adjustmentStrength);
            var result = ApplyScalarMode(current, amount, definition.Mode, definition.Min, definition.Max);
            var warning = CreateSafetyWarning(definition, result.UnclampedValue);
            return new ShaderAdjustmentResult(Format(result.Value), result.HitMin, result.HitMax, warning);
        }

        var componentCount = GetComponentCount(definition.Shape);
        if (!TryParseVector(rawValue, componentCount, out var currentVector))
        {
            return null;
        }

        var amountVector = ApplyStrength(definition.Amount(profile), definition.Mode, adjustmentStrength, componentCount);
        var vectorResult = ApplyVectorMode(currentVector, amountVector, definition.Mode, definition.Min, definition.Max, componentCount);
        var vectorWarning = CreateSafetyWarning(definition, vectorResult.UnclampedValue, componentCount);
        return new ShaderAdjustmentResult(FormatVector(vectorResult.Value, componentCount), vectorResult.HitMin, vectorResult.HitMax, vectorWarning);
    }

    private static EffectRole InferRole(string reason)
    {
        if (reason.Contains("bloom", StringComparison.OrdinalIgnoreCase))
        {
            return EffectRole.Bloom;
        }

        if (reason.Contains("diffusion", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("haze", StringComparison.OrdinalIgnoreCase))
        {
            return EffectRole.Diffusion;
        }

        if (reason.Contains("dof", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("depth of field", StringComparison.OrdinalIgnoreCase))
        {
            return EffectRole.Dof;
        }

        if (reason.Contains("film grain", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("grain", StringComparison.OrdinalIgnoreCase))
        {
            return EffectRole.FilmGrain;
        }

        if (reason.Contains("vignette", StringComparison.OrdinalIgnoreCase))
        {
            return EffectRole.Vignette;
        }

        if (reason.Contains("sharpen", StringComparison.OrdinalIgnoreCase))
        {
            return EffectRole.Sharpen;
        }

        if (reason.Contains("ambient occlusion", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("ao", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("rtgi", StringComparison.OrdinalIgnoreCase))
        {
            return EffectRole.AoGi;
        }

        if (reason.Contains("color", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("colour", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("grade", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("exposure", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("saturation", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("contrast", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("temperature", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("tint", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("lut", StringComparison.OrdinalIgnoreCase))
        {
            return EffectRole.ColorGrade;
        }

        return EffectRole.Unknown;
    }

    private static float ApplyStrength(float amount, ShaderValueMode mode, float strength)
    {
        if (strength >= 0.999f)
        {
            return amount;
        }

        return mode switch
        {
            ShaderValueMode.Scale or ShaderValueMode.ScaleAll or ShaderValueMode.ScaleComponent => 1f + ((amount - 1f) * strength),
            _ => amount * strength
        };
    }

    private static ShaderValue ApplyStrength(ShaderValue amount, ShaderValueMode mode, float strength, int componentCount)
    {
        if (strength >= 0.999f)
        {
            return amount;
        }

        var result = amount;
        for (var i = 0; i < componentCount; i++)
        {
            result = result.WithComponent(i, ApplyStrength(amount.ComponentAt(i), mode, strength));
        }

        return result;
    }

    private static (float Value, float UnclampedValue, bool HitMin, bool HitMax) ApplyScalarMode(float current, float amount, ShaderValueMode mode, float min, float max)
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
        return (clamped, value, hitMin, hitMax);
    }

    private static (ShaderValue Value, ShaderValue UnclampedValue, bool HitMin, bool HitMax) ApplyVectorMode(ShaderValue current, ShaderValue amount, ShaderValueMode mode, float min, float max, int componentCount)
    {
        var value = new ShaderValue();
        var unclamped = new ShaderValue();
        var hitMin = false;
        var hitMax = false;

        for (var i = 0; i < componentCount; i++)
        {
            var currentComponent = current.ComponentAt(i);
            var amountComponent = amount.ComponentAt(i);
            var next = mode switch
            {
                ShaderValueMode.Scale => currentComponent * amountComponent,
                ShaderValueMode.Add => currentComponent + amountComponent,
                ShaderValueMode.InvertAdd => currentComponent - amountComponent,
                ShaderValueMode.RelativeAdd => currentComponent + amountComponent,
                ShaderValueMode.ScaleAll => currentComponent * amount.X,
                ShaderValueMode.AddAll => currentComponent + amount.X,
                ShaderValueMode.ScaleComponent => currentComponent * amountComponent,
                ShaderValueMode.AddComponent => currentComponent + amountComponent,
                ShaderValueMode.MoveTowardNeutral => currentComponent + ((0f - currentComponent) * amount.X),
                _ => currentComponent
            };

            unclamped = unclamped.WithComponent(i, next);
            value = value.WithComponent(i, Clamp(next, min, max));
            hitMin |= next < min;
            hitMax |= next > max;
        }

        return (value, unclamped, hitMin, hitMax);
    }

    private static string? CreateSafetyWarning(ShaderVariableDefinition definition, ShaderValue value, int componentCount)
    {
        if (!IsTemperatureLikeKey(definition.Key))
        {
            return null;
        }

        for (var i = 0; i < componentCount; i++)
        {
            var component = value.ComponentAt(i);
            if (component is < 1000f or > 20000f)
            {
                return $"{definition.Section ?? "Any section"} / {definition.Key} wanted to write {Format(component)}, which is outside the temperature safety range.";
            }
        }

        return null;
    }

    private static string? CreateSafetyWarning(ShaderVariableDefinition definition, float value)
    {
        if (!IsTemperatureLikeKey(definition.Key))
        {
            return null;
        }

        return value is < 1000f or > 20000f
            ? $"{definition.Section ?? "Any section"} / {definition.Key} wanted to write {Format(value)}, which is outside the temperature safety range."
            : null;
    }

    private static bool IsTemperatureLikeKey(string key)
    {
        return key.Contains("TEMP", StringComparison.OrdinalIgnoreCase)
               || key.Contains("TEMPERATURE", StringComparison.OrdinalIgnoreCase)
               || key.Contains("KELVIN", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseSingle(string rawValue, out float value)
    {
        return float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseVector(string rawValue, int componentCount, out ShaderValue value)
    {
        value = default;
        var trimmed = rawValue.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '(' && trimmed[^1] == ')')
        {
            trimmed = trimmed[1..^1];
        }

        var parts = trimmed.Split(',');
        if (parts.Length != componentCount)
        {
            return false;
        }

        for (var i = 0; i < parts.Length; i++)
        {
            if (!TryParseSingle(parts[i], out var component))
            {
                return false;
            }

            value = value.WithComponent(i, component);
        }

        return true;
    }

    private static int GetComponentCount(ShaderValueShape shape)
    {
        return shape switch
        {
            ShaderValueShape.Vector2 => 2,
            ShaderValueShape.Vector3 => 3,
            ShaderValueShape.Vector4 => 4,
            _ => 1
        };
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));

    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string FormatVector(ShaderValue value, int componentCount)
    {
        var components = new string[componentCount];
        for (var i = 0; i < componentCount; i++)
        {
            components[i] = Format(value.ComponentAt(i));
        }

        return string.Join(",", components);
    }
}
