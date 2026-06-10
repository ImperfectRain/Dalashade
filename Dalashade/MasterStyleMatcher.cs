using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record MasterStyleProfileDeltas(
    float Exposure,
    float Contrast,
    float Saturation,
    float Bloom,
    float AmbientOcclusion,
    float Sharpness,
    float Clarity,
    float HighlightRecovery,
    float WhitePoint,
    float BlackPoint,
    float MidtoneContrast,
    float ShadowLift,
    float Temperature,
    float Tint,
    float ShadowHueBias,
    float ShadowSaturationBias,
    float MidtoneHueBias,
    float MidtoneSaturationBias,
    float HighlightHueBias,
    float HighlightSaturationBias)
{
    public void ApplyTo(
        ref float exposure,
        ref float contrast,
        ref float saturation,
        ref float bloom,
        ref float ao,
        ref float sharpness,
        ref float clarity,
        ref float highlightRecovery,
        ref float whitePoint,
        ref float blackPoint,
        ref float midtoneContrast,
        ref float shadowLift,
        ref float temperature,
        ref float tint,
        ref float shadowHueBias,
        ref float shadowSaturationBias,
        ref float midtoneHueBias,
        ref float midtoneSaturationBias,
        ref float highlightHueBias,
        ref float highlightSaturationBias)
    {
        exposure += Exposure;
        contrast += Contrast;
        saturation += Saturation;
        bloom += Bloom;
        ao += AmbientOcclusion;
        sharpness += Sharpness;
        clarity += Clarity;
        highlightRecovery *= 1f + HighlightRecovery;
        whitePoint *= 1f + WhitePoint;
        blackPoint *= 1f + BlackPoint;
        midtoneContrast += MidtoneContrast;
        shadowLift += ShadowLift;
        temperature += Temperature;
        tint += Tint;
        shadowHueBias += ShadowHueBias;
        shadowSaturationBias += ShadowSaturationBias;
        midtoneHueBias += MidtoneHueBias;
        midtoneSaturationBias += MidtoneSaturationBias;
        highlightHueBias += HighlightHueBias;
        highlightSaturationBias += HighlightSaturationBias;
    }
}

public sealed record MasterStyleMatchResult(
    MasterStyleProfileDeltas Deltas,
    MasterStyleDiagnostics Diagnostics,
    IReadOnlyList<AppliedRule> AppliedRules,
    IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> ColorFamilyAdjustments,
    string Description,
    string Changes);

public sealed class MasterStyleMatcher
{
    public MasterStyleMatchResult Match(ImageAnalysisResult current, ImageAnalysisResult target, Configuration configuration, int masterImageCount)
    {
        var rawStrength = Clamp(configuration.MasterPresetStyleStrength / 100f, 0f, 1f);
        var sceneSimilarity = configuration.MasterSceneSimilarityDampening
            ? MasterStyleDiagnostics.EstimateSceneSimilarity(current, target)
            : 1f;
        var compatibilityStrength = MasterStyleDiagnostics.GetCompatibilityModeMultiplier(configuration.CompatibilityMode);
        var strength = rawStrength * sceneSimilarity * compatibilityStrength;
        var tonalStrength = strength * Clamp(configuration.MasterTonalMatchStrength, 0f, 2f);
        var colorStrength = strength * Clamp(configuration.MasterTonalColorStrength, 0f, 2f);
        var familyStrength = strength * Clamp(configuration.MasterColorFamilyStrength, 0f, 2f);
        var scene = current.Available ? current : ImageAnalysisResult.Empty;
        var colorFamilyAdjustments = CreateColorFamilyAdjustments(scene, target, familyStrength, current.Available, configuration);

        var luminanceDelta = target.AverageLuminance - (current.Available ? scene.AverageLuminance : 0.50f);
        var saturationDelta = target.AverageSaturation - (current.Available ? scene.AverageSaturation : 0.38f);
        var warmthDelta = target.Warmth - (current.Available ? scene.Warmth : 0f);
        var greenDelta = target.GreenBias - (current.Available ? scene.GreenBias : 0f);
        var sceneShadowFloor = current.Available ? scene.ShadowFloor : 0.05f;
        var sceneMidtone = current.Available ? scene.MidtoneLevel : 0.50f;
        var sceneHighlightCeiling = current.Available ? scene.HighlightCeiling : 0.95f;
        var sceneSpread = current.Available ? scene.ContrastSpread : 0.50f;
        var shadowDelta = target.ShadowFloor - sceneShadowFloor;
        var midtoneDelta = target.MidtoneLevel - sceneMidtone;
        var highlightDelta = target.HighlightCeiling - sceneHighlightCeiling;
        var spreadDelta = target.ContrastSpread - sceneSpread;

        var exposureDelta = Clamp((luminanceDelta * 0.22f) + (midtoneDelta * 0.30f), -0.10f, 0.10f) * tonalStrength;
        var shadowLiftDelta = Clamp(shadowDelta * 0.50f, -0.045f, 0.085f) * tonalStrength;
        var blackPointDelta = Clamp(-shadowDelta * 0.22f, -0.040f, 0.045f) * tonalStrength;
        var whitePointDelta = Clamp(highlightDelta * 0.12f, -0.025f, 0.025f) * tonalStrength;
        var highlightRecoveryDelta = Clamp(-highlightDelta * 0.55f, -0.12f, 0.16f) * tonalStrength;
        var contrastDelta = Clamp(spreadDelta * 0.55f, -0.12f, 0.12f) * tonalStrength;
        var midtoneContrastDelta = Clamp((spreadDelta * 0.35f) + (midtoneDelta * 0.20f), -0.10f, 0.10f) * tonalStrength;
        var saturationProfileDelta = Clamp(saturationDelta * 0.80f, -0.18f, 0.18f) * tonalStrength;
        var temperatureDelta = Clamp(warmthDelta * 0.95f, -0.24f, 0.24f) * tonalStrength;
        var tintDelta = Clamp(greenDelta * 0.75f, -0.16f, 0.16f) * tonalStrength;
        var bloomDelta = 0f;
        var aoDelta = 0f;
        var sharpnessDelta = 0f;
        var clarityDelta = 0f;
        var shadowHueBias = 0f;
        var shadowSaturationBias = 0f;
        var midtoneHueBias = 0f;
        var midtoneSaturationBias = 0f;
        var highlightHueBias = 0f;
        var highlightSaturationBias = 0f;

        var shadowColor = ApplyTonalColorBias(scene.ShadowColor, target.ShadowColor, colorStrength, 0.085f, 0.22f, ref shadowHueBias, ref shadowSaturationBias);
        var midtoneColor = ApplyTonalColorBias(scene.MidtoneColor, target.MidtoneColor, colorStrength, 0.100f, 0.26f, ref midtoneHueBias, ref midtoneSaturationBias);
        var highlightColor = ApplyTonalColorBias(scene.HighlightColor, target.HighlightColor, colorStrength, 0.075f, 0.18f, ref highlightHueBias, ref highlightSaturationBias);

        if (target.HighlightClipping > 0.07f && target.AverageLuminance > 0.58f)
        {
            bloomDelta += 0.10f * tonalStrength;
        }
        else if (current.Available && current.HighlightClipping > target.HighlightClipping + 0.04f)
        {
            bloomDelta -= 0.14f * tonalStrength;
        }

        if (target.ShadowClipping < 0.08f && current.Available && current.ShadowClipping > target.ShadowClipping + 0.08f)
        {
            shadowLiftDelta += 0.12f * tonalStrength;
            aoDelta -= 0.10f * tonalStrength;
        }

        if (target.Contrast > 0.26f && target.AverageSaturation > 0.42f)
        {
            clarityDelta += 0.08f * tonalStrength;
            sharpnessDelta += 0.04f * tonalStrength;
        }

        var shadowText = shadowDelta switch
        {
            > 0.025f => "shadows lifted",
            < -0.025f => "shadows deepened",
            _ => "shadows held"
        };
        var highlightText = highlightDelta switch
        {
            > 0.025f => "highlights brightened",
            < -0.025f => "highlights recovered",
            _ => "highlights held"
        };
        var contrastText = spreadDelta switch
        {
            > 0.035f => "contrast increased",
            < -0.035f => "contrast softened",
            _ => "contrast held"
        };

        var deltas = new MasterStyleProfileDeltas(
            exposureDelta,
            contrastDelta,
            saturationProfileDelta,
            bloomDelta,
            aoDelta,
            sharpnessDelta,
            clarityDelta,
            highlightRecoveryDelta,
            whitePointDelta,
            blackPointDelta,
            midtoneContrastDelta,
            shadowLiftDelta,
            temperatureDelta,
            tintDelta,
            shadowHueBias,
            shadowSaturationBias,
            midtoneHueBias,
            midtoneSaturationBias,
            highlightHueBias,
            highlightSaturationBias);

        var diagnostics = new MasterStyleDiagnostics(
            true,
            target.Available,
            current.Available,
            masterImageCount,
            configuration.MasterStyleMode,
            configuration.MasterPresetStyleStrength,
            strength,
            sceneSimilarity,
            compatibilityStrength,
            new MasterStyleTonalDeltas(exposureDelta, shadowLiftDelta, blackPointDelta, whitePointDelta, highlightRecoveryDelta, contrastDelta, midtoneContrastDelta),
            shadowHueBias,
            shadowSaturationBias,
            midtoneHueBias,
            midtoneSaturationBias,
            highlightHueBias,
            highlightSaturationBias,
            StrongestColorFamilyAdjustments(colorFamilyAdjustments, 8),
            "Master style active.");

        return new MasterStyleMatchResult(
            deltas,
            diagnostics,
            BuildMasterStyleRules(diagnostics, colorFamilyAdjustments),
            colorFamilyAdjustments,
            $"effective strength {strength:P0}: {shadowText}, {highlightText}, {contrastText}; tonal color bias scale {colorStrength:0.##}.",
            $"shadow floor {sceneShadowFloor:0.00}->{target.ShadowFloor:0.00}, midtone {sceneMidtone:0.00}->{target.MidtoneLevel:0.00}, highlight {sceneHighlightCeiling:0.00}->{target.HighlightCeiling:0.00}, spread {sceneSpread:0.00}->{target.ContrastSpread:0.00}; shadow {shadowColor}, midtone {midtoneColor}, highlight {highlightColor}; color families {FormatStrongestFamilyAdjustments(colorFamilyAdjustments)}");
    }

    private static IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> CreateColorFamilyAdjustments(ImageAnalysisResult current, ImageAnalysisResult target, float strength, bool hasCurrentScene, Configuration configuration)
    {
        if (!hasCurrentScene || strength <= 0f)
        {
            return ColorFamilyAdjustment.EmptyMap;
        }

        return Enum.GetValues<ColorFamily>().ToDictionary(
            family => family,
            family =>
            {
                var currentStats = current.ColorFamilies.TryGetValue(family, out var currentValue) ? currentValue : ColorFamilyStats.Empty(family);
                var targetStats = target.ColorFamilies.TryGetValue(family, out var targetValue) ? targetValue : ColorFamilyStats.Empty(family);
                var confidence = MathF.Min(currentStats.Confidence, targetStats.Confidence);

                if (confidence < 0.10f)
                {
                    return ColorFamilyAdjustment.Empty(family);
                }

                var effectiveStrength = strength * confidence;
                var hue = Clamp(HueDelta(targetStats.Hue, currentStats.Hue) * 0.18f, -0.025f, 0.025f) * effectiveStrength;
                var saturation = Clamp((targetStats.Saturation - currentStats.Saturation) * 0.20f, -0.050f, 0.050f) * effectiveStrength;
                var luminance = Clamp((targetStats.Luminance - currentStats.Luminance) * 0.16f, -0.040f, 0.040f) * effectiveStrength;

                return new ColorFamilyAdjustment(
                    family,
                    Clamp(hue, -configuration.MasterMaxHueShift, configuration.MasterMaxHueShift),
                    Clamp(saturation, -configuration.MasterMaxSaturationShift, configuration.MasterMaxSaturationShift),
                    Clamp(luminance, -configuration.MasterMaxLuminanceShift, configuration.MasterMaxLuminanceShift),
                    confidence);
            });
    }

    private static IReadOnlyList<ColorFamilyAdjustment> StrongestColorFamilyAdjustments(IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> adjustments, int count)
    {
        return adjustments.Values
            .Where(adjustment => adjustment.Score > 0.0005f)
            .OrderByDescending(adjustment => adjustment.Score)
            .ThenBy(adjustment => adjustment.Family)
            .Take(count)
            .ToArray();
    }

    private static IReadOnlyList<AppliedRule> BuildMasterStyleRules(MasterStyleDiagnostics diagnostics, IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> adjustments)
    {
        var rules = new List<AppliedRule>();

        if (diagnostics.TonalDeltas.ShadowLift > 0.005f)
        {
            rules.Add(new AppliedRule("Master style lifted shadows", "Reference shadows are more open than the current scene.", $"shadow lift +{diagnostics.TonalDeltas.ShadowLift:0.###}"));
        }
        else if (diagnostics.TonalDeltas.ShadowLift < -0.005f)
        {
            rules.Add(new AppliedRule("Master style deepened shadows", "Reference shadows sit lower than the current scene.", $"shadow lift {diagnostics.TonalDeltas.ShadowLift:0.###}"));
        }

        if (diagnostics.TonalDeltas.HighlightRecovery > 0.005f || diagnostics.TonalDeltas.WhitePoint < -0.003f)
        {
            rules.Add(new AppliedRule("Master style softened highlights", "Reference highlights need more rolloff than the current scene.", $"highlight recovery +{diagnostics.TonalDeltas.HighlightRecovery:0.###}, white point {diagnostics.TonalDeltas.WhitePoint:+0.###;-0.###;0}"));
        }

        if (diagnostics.TonalDeltas.MidtoneContrast > 0.005f)
        {
            rules.Add(new AppliedRule("Master style increased midtone contrast", "Reference midtones have more separation.", $"midtone contrast +{diagnostics.TonalDeltas.MidtoneContrast:0.###}"));
        }
        else if (diagnostics.TonalDeltas.MidtoneContrast < -0.005f)
        {
            rules.Add(new AppliedRule("Master style softened midtone contrast", "Reference midtones are flatter than the current scene.", $"midtone contrast {diagnostics.TonalDeltas.MidtoneContrast:0.###}"));
        }

        AddTonalColorRule(rules, "shadows", diagnostics.ShadowHueBias, diagnostics.ShadowSaturationBias);
        AddTonalColorRule(rules, "midtones", diagnostics.MidtoneHueBias, diagnostics.MidtoneSaturationBias);
        AddTonalColorRule(rules, "highlights", diagnostics.HighlightHueBias, diagnostics.HighlightSaturationBias);

        foreach (var adjustment in StrongestColorFamilyAdjustments(adjustments, 3))
        {
            if (adjustment.Saturation < -0.006f)
            {
                rules.Add(new AppliedRule($"Master style muted {adjustment.Family.ToString().ToLowerInvariant()}s", "Reference color family is less saturated than the current scene.", $"sat {adjustment.Saturation:0.###}"));
            }
            else if (adjustment.Saturation > 0.006f)
            {
                rules.Add(new AppliedRule($"Master style enriched {adjustment.Family.ToString().ToLowerInvariant()}s", "Reference color family is more saturated than the current scene.", $"sat +{adjustment.Saturation:0.###}"));
            }

            if (MathF.Abs(adjustment.Hue) > 0.006f)
            {
                var direction = adjustment.Hue > 0f ? "forward" : "back";
                rules.Add(new AppliedRule($"Master style shifted {adjustment.Family.ToString().ToLowerInvariant()} hue", $"Reference nudges this color family {direction} around the hue wheel.", $"hue {adjustment.Hue:+0.###;-0.###;0}"));
            }
        }

        return rules;
    }

    private static void AddTonalColorRule(List<AppliedRule> rules, string band, float hueBias, float saturationBias)
    {
        if (MathF.Abs(hueBias) > 0.006f)
        {
            var direction = hueBias > 0f ? "warmed" : "cooled";
            rules.Add(new AppliedRule($"Master style {direction} {band}", $"Reference {band} have a different hue bias.", $"hue {hueBias:+0.###;-0.###;0}"));
        }

        if (MathF.Abs(saturationBias) > 0.008f)
        {
            var verb = saturationBias > 0f ? "enriched" : "muted";
            rules.Add(new AppliedRule($"Master style {verb} {band}", $"Reference {band} have a different saturation bias.", $"sat {saturationBias:+0.###;-0.###;0}"));
        }
    }

    private static string FormatStrongestFamilyAdjustments(IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> adjustments)
    {
        var strongest = adjustments.Values
            .Where(adjustment => adjustment.Score > 0.0005f)
            .OrderByDescending(adjustment => adjustment.Score)
            .ThenBy(adjustment => adjustment.Family)
            .Take(3)
            .Select(adjustment => FormattableString.Invariant($"{adjustment.Family} H{adjustment.Hue:+0.000;-0.000;0.000} S{adjustment.Saturation:+0.000;-0.000;0.000} L{adjustment.Luminance:+0.000;-0.000;0.000} C{adjustment.Confidence:0.00}"))
            .ToArray();

        return strongest.Length == 0 ? "held" : string.Join(", ", strongest);
    }

    private static string ApplyTonalColorBias(
        TonalColorBias current,
        TonalColorBias target,
        float strength,
        float hueScale,
        float saturationScale,
        ref float hueBias,
        ref float saturationBias)
    {
        if (strength <= 0f)
        {
            return "held";
        }

        var chromaWeight = Clamp((current.Saturation + target.Saturation) * 0.5f, 0f, 1f);
        var hueDelta = HueDelta(target.Hue, current.Hue) * chromaWeight;
        var warmthDelta = target.Warmth - current.Warmth;
        var tintDelta = target.Tint - current.Tint;
        var saturationDelta = target.Saturation - current.Saturation;
        var hueAdjustment = Clamp((hueDelta * hueScale) + (warmthDelta * 0.025f) + (tintDelta * 0.018f), -0.030f, 0.030f) * strength;
        var saturationAdjustment = Clamp(saturationDelta * saturationScale, -0.070f, 0.070f) * strength;

        hueBias += hueAdjustment;
        saturationBias += saturationAdjustment;

        return FormattableString.Invariant($"hue {hueAdjustment:+0.000;-0.000;0.000}, sat {saturationAdjustment:+0.000;-0.000;0.000}");
    }

    private static float HueDelta(float target, float current)
    {
        var delta = target - current;
        if (delta > 0.5f)
        {
            delta -= 1f;
        }
        else if (delta < -0.5f)
        {
            delta += 1f;
        }

        return delta;
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));
}
