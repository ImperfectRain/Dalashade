using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record MasterStyleTonalDeltas(
    float Exposure,
    float ShadowLift,
    float BlackPoint,
    float WhitePoint,
    float HighlightRecovery,
    float Contrast,
    float MidtoneContrast)
{
    public static MasterStyleTonalDeltas Empty { get; } = new(0f, 0f, 0f, 0f, 0f, 0f, 0f);
}

public sealed record MasterStyleDiagnostics(
    bool Enabled,
    bool MasterAvailable,
    bool CurrentImageAvailable,
    int MasterImageCount,
    MasterStyleMode MasterMode,
    int RawStrength,
    float EffectiveStrength,
    float SceneSimilarityMultiplier,
    float CompatibilityModeMultiplier,
    MasterStyleTonalDeltas TonalDeltas,
    float ShadowHueBias,
    float ShadowSaturationBias,
    float MidtoneHueBias,
    float MidtoneSaturationBias,
    float HighlightHueBias,
    float HighlightSaturationBias,
    IReadOnlyList<ColorFamilyAdjustment> StrongestColorFamilyAdjustments,
    string Status)
{
    public static MasterStyleDiagnostics FromUnavailable(Configuration configuration, ImageAnalysisResult current, ImageAnalysisResult master, int masterImageCount, string status)
    {
        return new MasterStyleDiagnostics(
            configuration.MatchMasterPresetStyle,
            master.Available,
            current.Available,
            masterImageCount,
            configuration.MasterStyleMode,
            configuration.MasterPresetStyleStrength,
            0f,
            configuration.MasterSceneSimilarityDampening ? 0f : 1f,
            GetCompatibilityModeMultiplier(configuration.CompatibilityMode),
            MasterStyleTonalDeltas.Empty,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            Array.Empty<ColorFamilyAdjustment>(),
            status);
    }

    public static float GetCompatibilityModeMultiplier(PresetCompatibilityMode mode)
    {
        return mode switch
        {
            PresetCompatibilityMode.PreserveBase => 0.50f,
            PresetCompatibilityMode.AdaptiveBalanced => 0.75f,
            PresetCompatibilityMode.GameplaySanitize => 0.35f,
            PresetCompatibilityMode.CinematicPreserve => 0.90f,
            PresetCompatibilityMode.GposePreserve => 1.00f,
            _ => 0.75f
        };
    }

    public static float EstimateSceneSimilarity(ImageAnalysisResult current, ImageAnalysisResult master)
    {
        if (!current.Available || !master.Available)
        {
            return 0.65f;
        }

        var distance =
            MathF.Abs(current.LuminanceP50 - master.LuminanceP50) * 1.25f
            + MathF.Abs(current.ContrastSpread - master.ContrastSpread) * 1.10f
            + MathF.Abs(current.AverageSaturation - master.AverageSaturation) * 0.90f
            + MathF.Abs(current.Warmth - master.Warmth) * 0.55f
            + MathF.Abs(current.GreenBias - master.GreenBias) * 0.55f
            + ColorFamilyDistance(current, master) * 0.75f;

        return Clamp(1f - distance, 0.25f, 1f);
    }

    public static float CalculateEffectiveStrength(Configuration configuration, ImageAnalysisResult current, ImageAnalysisResult master)
    {
        if (!configuration.MatchMasterPresetStyle || !master.Available)
        {
            return 0f;
        }

        var raw = Clamp(configuration.MasterPresetStyleStrength / 100f, 0f, 1f);
        var scene = configuration.MasterSceneSimilarityDampening ? EstimateSceneSimilarity(current, master) : 1f;
        return raw * scene * GetCompatibilityModeMultiplier(configuration.CompatibilityMode);
    }

    private static float ColorFamilyDistance(ImageAnalysisResult current, ImageAnalysisResult master)
    {
        var total = 0f;
        var weight = 0f;
        foreach (var family in Enum.GetValues<ColorFamily>())
        {
            var currentStats = current.ColorFamilies.TryGetValue(family, out var currentValue) ? currentValue : ColorFamilyStats.Empty(family);
            var masterStats = master.ColorFamilies.TryGetValue(family, out var masterValue) ? masterValue : ColorFamilyStats.Empty(family);
            var confidence = MathF.Max(currentStats.Confidence, masterStats.Confidence);
            if (confidence <= 0.02f)
            {
                continue;
            }

            var overlap = MathF.Abs(currentStats.Coverage - masterStats.Coverage)
                          + MathF.Abs(currentStats.Saturation - masterStats.Saturation) * 0.5f
                          + MathF.Abs(currentStats.Luminance - masterStats.Luminance) * 0.35f;
            total += overlap * confidence;
            weight += confidence;
        }

        return weight <= 0f ? 0f : total / weight;
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));
}
