using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record ColorFamilyAdjustment(ColorFamily Family, float Hue, float Saturation, float Luminance, float Confidence)
{
    public static ColorFamilyAdjustment Empty(ColorFamily family) => new(family, 0f, 0f, 0f, 0f);

    public static IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> EmptyMap { get; } = Enum.GetValues<ColorFamily>()
        .ToDictionary(family => family, Empty);

    public float Score => MathF.Abs(Hue) + MathF.Abs(Saturation) + MathF.Abs(Luminance);
}

public sealed record VisualProfile(
    float Exposure,
    float Contrast,
    float Saturation,
    float Bloom,
    float AmbientOcclusion,
    float Rtgi,
    float ReLight,
    float DepthEffects,
    float Sharpness,
    float SharpenThreshold,
    float Clarity,
    float HighlightRecovery,
    float WhitePoint,
    float BlackPoint,
    float MidtoneContrast,
    float BloomRadius,
    float BloomThreshold,
    float BloomDirt,
    float AoRadius,
    float AoFadeDistance,
    float DebandStrength,
    float AntiAliasingStrength,
    float LutStrength,
    float ColorGradePreservation,
    float ShadowLift,
    float Temperature,
    float Tint,
    float ShadowHueBias,
    float ShadowSaturationBias,
    float MidtoneHueBias,
    float MidtoneSaturationBias,
    float HighlightHueBias,
    float HighlightSaturationBias,
    IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> ColorFamilyAdjustments)
{
    public static VisualProfile Neutral { get; } = new(
        1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
        1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
        1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f, 0f, 0f, ColorFamilyAdjustment.EmptyMap);

    public ColorFamilyAdjustment GetColorFamilyAdjustment(ColorFamily family)
    {
        return ColorFamilyAdjustments.TryGetValue(family, out var adjustment)
            ? adjustment
            : ColorFamilyAdjustment.Empty(family);
    }

    public IReadOnlyList<ColorFamilyAdjustment> StrongestColorFamilyAdjustments(int count)
    {
        return ColorFamilyAdjustments.Values
            .Where(adjustment => adjustment.Score > 0.0005f)
            .OrderByDescending(adjustment => adjustment.Score)
            .ThenBy(adjustment => adjustment.Family)
            .Take(count)
            .ToArray();
    }
}

public sealed class ProfileEngine
{
    public ProfileResult CreateWithRules(GameContext context, SceneTags tags, ImageAnalysisResult imageAnalysis, ImageAnalysisResult masterStyle, Configuration configuration)
    {
        var rules = new List<AppliedRule>();
        var profile = Create(context, tags, imageAnalysis, masterStyle, configuration, rules);

        return new ProfileResult(profile, rules);
    }

    public VisualProfile Create(GameContext context, SceneTags tags, ImageAnalysisResult imageAnalysis, ImageAnalysisResult masterStyle, Configuration configuration)
    {
        return Create(context, tags, imageAnalysis, masterStyle, configuration, null);
    }

    private VisualProfile Create(GameContext context, SceneTags tags, ImageAnalysisResult imageAnalysis, ImageAnalysisResult masterStyle, Configuration configuration, List<AppliedRule>? rules)
    {
        var exposure = 1f;
        var contrast = 1f;
        var saturation = 1f;
        var bloom = 1f;
        var ao = 1f;
        var rtgi = 1f;
        var relight = 1f;
        var depthEffects = 1f;
        var sharpness = 1f;
        var sharpenThreshold = 1f;
        var clarity = 1f;
        var highlightRecovery = 1f;
        var whitePoint = 1f;
        var blackPoint = 1f;
        var midtoneContrast = 1f;
        var bloomRadius = 1f;
        var bloomThreshold = 1f;
        var bloomDirt = 1f;
        var aoRadius = 1f;
        var aoFadeDistance = 1f;
        var debandStrength = 1f;
        var antiAliasingStrength = 1f;
        var lutStrength = 1f;
        var colorGradePreservation = GetColorGradePreservation(configuration.CompatibilityMode);
        var shadowLift = 0f;
        var temperature = 0f;
        var tint = 0f;
        var shadowHueBias = 0f;
        var shadowSaturationBias = 0f;
        var midtoneHueBias = 0f;
        var midtoneSaturationBias = 0f;
        var highlightHueBias = 0f;
        var highlightSaturationBias = 0f;
        var colorFamilyAdjustments = ColorFamilyAdjustment.EmptyMap;

        ApplyBasePolish(context, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift);
        rules?.Add(new AppliedRule("Base polish", "Small default lift so the generated preset feels like an upgrade, not just a safety mode.", "contrast, saturation, clarity, shadow lift"));

        if (configuration.AutoAdjustAtNight && tags.IsNight)
        {
            exposure *= 1.06f;
            shadowLift += 0.10f;
            ao *= 0.90f;
            blackPoint *= 0.96f;
            debandStrength *= 1.10f;
            rules?.Add(new AppliedRule("Night", "Lift the darks and ease off AO so night stays readable.", "exposure x1.06, shadow lift +0.10, AO x0.90"));
        }

        if (configuration.AutoAdjustAtNight && tags.IsDawnOrDusk)
        {
            bloom *= 1.04f;
            bloomRadius *= 1.04f;
            bloomDirt *= 1.02f;
            temperature += 0.025f;
            rules?.Add(new AppliedRule("Dawn/dusk", "Low sun can take a little warmth and glow without going full postcard.", "bloom x1.04, warmth +0.025"));
        }

        if (configuration.AutoAdjustForWeather)
        {
            ApplyWeather(context, tags, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref bloomRadius, ref bloomThreshold, ref bloomDirt, ref highlightRecovery, ref debandStrength, rules);
        }

        if (configuration.AutoAdjustForTerritory)
        {
            ApplyTerritory(tags, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift, rules);
            ApplyBiome(tags, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift, ref temperature, ref bloomRadius, ref bloomThreshold, ref bloomDirt, ref aoRadius, ref debandStrength, rules);
        }

        if (configuration.AutoAdjustInCombat && tags.NeedsGameplayClarity)
        {
            ao *= 0.88f;
            rtgi *= 0.78f;
            relight *= 0.85f;
            depthEffects *= 0.80f;
            bloom *= 0.75f;
            bloomRadius *= 0.80f;
            bloomThreshold *= 1.06f;
            bloomDirt *= 0.55f;
            sharpness *= 0.92f;
            sharpenThreshold *= 1.08f;
            clarity *= 0.90f;
            antiAliasingStrength *= 1.04f;
            lutStrength *= 0.90f;
            shadowLift += 0.08f;
            rules?.Add(new AppliedRule("Gameplay clarity", "Combat or duty context gets readability first; cinematic effects back off.", "bloom x0.75, AO x0.88, RTGI x0.78, depth x0.80"));
        }

        if (context.InGpose)
        {
            ao *= 1.12f;
            bloom *= 1.10f;
            bloomRadius *= 1.08f;
            bloomDirt *= 1.06f;
            contrast *= 1.04f;
            lutStrength *= 1.05f;
            rules?.Add(new AppliedRule("GPose", "GPose can take the prettier settings because gameplay readability is not fighting for space.", "AO x1.12, bloom x1.10, contrast x1.04"));
        }
        else if (configuration.AutoAdjustInCutscenes && context.InCutscene)
        {
            ao *= 1.08f;
            bloom *= 1.06f;
            bloomRadius *= 1.04f;
            bloomDirt *= 1.03f;
            contrast *= 1.03f;
            rules?.Add(new AppliedRule("Cutscene", "Cutscene detected, so Dalashade allows a little more drama without pushing too hard.", "AO x1.08, bloom x1.06, contrast x1.03"));
        }

        if (configuration.AutoAdjustFromScreenshots && imageAnalysis.Available)
        {
            ApplyImageAnalysis(imageAnalysis, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift, ref highlightRecovery, ref whitePoint, ref blackPoint, ref bloomThreshold, ref bloomDirt, ref debandStrength);
            rules?.Add(new AppliedRule("Screenshot feedback", $"Current image reads as {imageAnalysis.ProfileBucket}.", "brightness, clipping, saturation, and contrast correction"));
        }

        if (configuration.MatchMasterPresetStyle && masterStyle.Available)
        {
            var masterTone = ApplyMasterStyle(
                imageAnalysis,
                masterStyle,
                configuration,
                ref exposure,
                ref contrast,
                ref saturation,
                ref bloom,
                ref ao,
                ref sharpness,
                ref clarity,
                ref highlightRecovery,
                ref whitePoint,
                ref blackPoint,
                ref midtoneContrast,
                ref shadowLift,
                ref temperature,
                ref tint,
                ref shadowHueBias,
                ref shadowSaturationBias,
                ref midtoneHueBias,
                ref midtoneSaturationBias,
                ref highlightHueBias,
                ref highlightSaturationBias,
                out colorFamilyAdjustments);
            rules?.Add(new AppliedRule("Master style", $"Reference look reads as {masterStyle.ProfileBucket}; {masterTone.Description}", masterTone.Changes));
        }

        ApplyStyle(configuration.Style, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness);
        rules?.Add(new AppliedRule("Style target", $"{configuration.Style} style preference applied.", "style weighting"));
        rules?.Add(new AppliedRule("Compatibility mode", $"{configuration.CompatibilityMode} mode sets ReGrade+ scalar color preservation to {colorGradePreservation:0.##}.", "ReGrade+ scalar hue/saturation preservation"));
        ApplyPerformanceBudget(configuration.PerformanceBudget, context, ref ao, ref rtgi, ref relight, ref depthEffects, ref bloom, ref bloomRadius, ref sharpness, ref sharpenThreshold, ref clarity, ref antiAliasingStrength, ref lutStrength);
        rules?.Add(new AppliedRule("Performance target", $"{configuration.PerformanceBudget} budget applied where needed.", "AO, RTGI, ReLight, bloom, clarity, sharpening"));

        return new VisualProfile(
            Clamp(exposure, 0.80f, 1.28f),
            Clamp(contrast, 0.80f, 1.28f),
            Clamp(saturation, 0.78f, 1.22f),
            Clamp(bloom, 0.40f, 1.35f),
            Clamp(ao, 0.20f, 1.30f),
            Clamp(rtgi, 0.10f, 1.30f),
            Clamp(relight, 0.10f, 1.25f),
            Clamp(depthEffects, 0.10f, 1.25f),
            Clamp(sharpness, 0.45f, 1.20f),
            Clamp(sharpenThreshold, 0.75f, 1.35f),
            Clamp(clarity, 0.45f, 1.22f),
            Clamp(highlightRecovery, 0.75f, 1.35f),
            Clamp(whitePoint, 0.94f, 1.06f),
            Clamp(blackPoint, 0.90f, 1.10f),
            Clamp(midtoneContrast, 0.80f, 1.25f),
            Clamp(bloomRadius, 0.55f, 1.35f),
            Clamp(bloomThreshold, 0.75f, 1.30f),
            Clamp(bloomDirt, 0.35f, 1.20f),
            Clamp(aoRadius, 0.55f, 1.35f),
            Clamp(aoFadeDistance, 0.70f, 1.30f),
            Clamp(debandStrength, 0.75f, 1.40f),
            Clamp(antiAliasingStrength, 0.85f, 1.25f),
            Clamp(lutStrength, 0.70f, 1.20f),
            Clamp(colorGradePreservation, 0f, 1f),
            Clamp(shadowLift, 0f, 0.35f),
            Clamp(temperature, -0.30f, 0.30f),
            Clamp(tint, -0.20f, 0.20f),
            Clamp(shadowHueBias, -0.05f, 0.05f),
            Clamp(shadowSaturationBias, -0.12f, 0.12f),
            Clamp(midtoneHueBias, -0.05f, 0.05f),
            Clamp(midtoneSaturationBias, -0.14f, 0.14f),
            Clamp(highlightHueBias, -0.04f, 0.04f),
            Clamp(highlightSaturationBias, -0.10f, 0.10f),
            colorFamilyAdjustments);
    }

    private static void ApplyWeather(GameContext context, SceneTags tags, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float bloomRadius, ref float bloomThreshold, ref float bloomDirt, ref float highlightRecovery, ref float debandStrength, List<AppliedRule>? rules)
    {
        if (tags.IsFog)
        {
            contrast *= 1.04f;
            bloom *= 0.90f;
            bloomRadius *= 0.94f;
            bloomDirt *= 0.70f;
            sharpness *= 0.92f;
            debandStrength *= 1.08f;
            rules?.Add(new AppliedRule("Fog/clouds", $"Weather is {context.WeatherName}; reduce haze-amplifying effects and add a little contrast.", "contrast x1.04, bloom x0.90, sharpen x0.92"));
        }

        if (tags.IsRain || tags.IsStorm)
        {
            contrast *= 1.03f;
            saturation *= 0.96f;
            bloom *= 0.90f;
            bloomRadius *= 0.95f;
            bloomDirt *= 0.78f;
            rules?.Add(new AppliedRule(tags.IsStorm ? "Storm" : "Rain", $"Weather is {context.WeatherName}; keep wet scenes readable and less bloomy.", "contrast x1.03, saturation x0.96, bloom x0.90"));
        }

        if (tags.IsSnow)
        {
            exposure *= 0.97f;
            saturation *= 0.94f;
            bloom *= 0.82f;
            bloomRadius *= 0.86f;
            bloomThreshold *= 1.08f;
            bloomDirt *= 0.62f;
            highlightRecovery *= 1.10f;
            rules?.Add(new AppliedRule("Snow", $"Weather is {context.WeatherName}; protect highlights and keep whites from glowing too much.", "exposure x0.97, saturation x0.94, bloom x0.82"));
        }
    }

    private static void ApplyBasePolish(GameContext context, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float shadowLift)
    {
        contrast += 0.035f;
        saturation += 0.025f;
        clarity += 0.035f;
        shadowLift += 0.015f;

        if (!context.InCombat)
        {
            bloom += 0.035f;
            ao += 0.035f;
            sharpness += 0.025f;
        }
    }

    private static void ApplyTerritory(SceneTags tags, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float shadowLift, List<AppliedRule>? rules)
    {
        if (tags.IsCityLike)
        {
            bloom *= 0.95f;
            sharpness *= 0.97f;
            ao *= 0.95f;
            shadowLift += 0.01f;
            rules?.Add(new AppliedRule("City/social area", "Cities get a cleaner image with less harsh AO and sharpening.", "bloom x0.95, AO x0.95, sharpen x0.97"));
        }
        else if (tags.IsDungeonLike || tags.IsRaidLike)
        {
            bloom *= 0.88f;
            ao *= 0.88f;
            clarity *= 0.92f;
            sharpness *= 0.95f;
            shadowLift += 0.04f;
            rules?.Add(new AppliedRule(tags.IsRaidLike ? "Raid/trial" : "Dungeon", "Duty-style areas bias toward readable shadows and restrained post effects.", "bloom x0.88, AO x0.88, clarity x0.92, shadow lift +0.04"));
        }
        else if (tags.IsInteriorLike)
        {
            exposure *= 1.04f;
            contrast *= 0.97f;
            saturation *= 1.02f;
            shadowLift += 0.03f;
            rules?.Add(new AppliedRule("Interior", "Interior lighting gets a little lift so corners do not collapse.", "exposure x1.04, contrast x0.97, shadow lift +0.03"));
        }
        else if (tags.IsFieldLike)
        {
            contrast *= 1.04f;
            saturation *= 1.025f;
            bloom *= 1.015f;
            rules?.Add(new AppliedRule("Field/exploration", "Open-world areas can take a mild scenic polish.", "contrast x1.04, saturation x1.025, bloom x1.015"));
        }
    }

    private static void ApplyBiome(SceneTags tags, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float shadowLift, ref float temperature, ref float bloomRadius, ref float bloomThreshold, ref float bloomDirt, ref float aoRadius, ref float debandStrength, List<AppliedRule>? rules)
    {
        switch (tags.BiomeKey)
        {
            case "snow":
                exposure *= 0.97f;
                bloom *= 0.88f;
                bloomRadius *= 0.88f;
                bloomThreshold *= 1.06f;
                bloomDirt *= 0.65f;
                saturation *= 0.96f;
                rules?.Add(new AppliedRule("Snow/ice biome", "Protect bright whites and keep snowy zones from blooming into mush.", "exposure x0.97, bloom x0.88, saturation x0.96"));
                break;
            case "forest":
                ao *= 0.94f;
                aoRadius *= 0.90f;
                sharpness *= 0.96f;
                shadowLift += 0.025f;
                rules?.Add(new AppliedRule("Forest biome", "Foliage-heavy scenes get softer depth and a touch more shadow readability.", "AO x0.94, sharpen x0.96, shadow lift +0.025"));
                break;
            case "desert":
                exposure *= 0.96f;
                bloom *= 0.90f;
                bloomRadius *= 0.92f;
                bloomDirt *= 0.76f;
                contrast *= 1.03f;
                temperature += 0.025f;
                rules?.Add(new AppliedRule("Desert biome", "Hot bright zones keep shape by easing exposure/bloom and adding a little contrast.", "exposure x0.96, bloom x0.90, warmth +0.025"));
                break;
            case "cave":
                shadowLift += 0.05f;
                ao *= 0.88f;
                aoRadius *= 0.86f;
                bloom *= 0.92f;
                bloomDirt *= 0.70f;
                debandStrength *= 1.08f;
                rules?.Add(new AppliedRule("Cave biome", "Dark enclosed spaces get readable shadows without piling on depth effects.", "shadow lift +0.05, AO x0.88, bloom x0.92"));
                break;
            case "void":
                saturation *= 0.96f;
                shadowLift += 0.035f;
                contrast *= 0.98f;
                rules?.Add(new AppliedRule("Void biome", "Dark purple/void scenes avoid crushed blacks and oversaturated shadows.", "saturation x0.96, shadow lift +0.035"));
                break;
            case "aetherial":
                bloom *= 0.94f;
                bloomRadius *= 0.92f;
                bloomDirt *= 0.75f;
                clarity *= 1.03f;
                rules?.Add(new AppliedRule("Aetherial biome", "Glowy blue/crystal scenes get slightly cleaner edges without extra bloom.", "bloom x0.94, clarity x1.03"));
                break;
            case "coastal":
                bloom *= 0.96f;
                bloomRadius *= 0.95f;
                bloomDirt *= 0.82f;
                saturation *= 1.02f;
                rules?.Add(new AppliedRule("Coastal biome", "Water and beach scenes keep color while protecting specular bloom.", "bloom x0.96, saturation x1.02"));
                break;
            case "fire":
                exposure *= 0.96f;
                bloom *= 0.88f;
                bloomRadius *= 0.88f;
                bloomDirt *= 0.72f;
                contrast *= 1.02f;
                rules?.Add(new AppliedRule("Fire/lava biome", "Very hot highlights get restrained so orange scenes keep detail.", "exposure x0.96, bloom x0.88, contrast x1.02"));
                break;
        }
    }

    private static void ApplyImageAnalysis(ImageAnalysisResult image, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float shadowLift, ref float highlightRecovery, ref float whitePoint, ref float blackPoint, ref float bloomThreshold, ref float bloomDirt, ref float debandStrength)
    {
        if (image.AverageLuminance < 0.30f)
        {
            exposure += Scale01(0.30f - image.AverageLuminance, 0.30f) * 0.14f;
            shadowLift += Scale01(image.ShadowClipping, 0.30f) * 0.16f;
            contrast -= Scale01(image.ShadowClipping, 0.25f) * 0.10f;
            ao -= Scale01(image.ShadowClipping, 0.25f) * 0.16f;
            blackPoint *= 1f - (Scale01(image.ShadowClipping, 0.30f) * 0.05f);
            debandStrength *= 1f + (Scale01(image.ShadowClipping, 0.30f) * 0.10f);
        }

        if (image.AverageLuminance > 0.72f || image.HighlightClipping > 0.04f)
        {
            exposure -= Scale01(image.AverageLuminance - 0.72f, 0.28f) * 0.11f;
            bloom -= Scale01(image.HighlightClipping, 0.12f) * 0.25f;
            contrast -= Scale01(image.HighlightClipping, 0.12f) * 0.07f;
            highlightRecovery *= 1f + (Scale01(image.HighlightClipping, 0.12f) * 0.20f);
            whitePoint *= 1f - (Scale01(image.HighlightClipping, 0.12f) * 0.035f);
            bloomThreshold *= 1f + (Scale01(image.HighlightClipping, 0.12f) * 0.12f);
            bloomDirt *= 1f - (Scale01(image.HighlightClipping, 0.12f) * 0.20f);
        }

        if (image.AverageSaturation < 0.24f)
        {
            saturation += 0.07f;
        }
        else if (image.AverageSaturation > 0.62f)
        {
            saturation -= 0.08f;
            bloom -= 0.05f;
        }

        if (image.Contrast < 0.14f)
        {
            contrast += 0.08f;
            clarity += 0.07f;
        }
        else if (image.Contrast > 0.32f)
        {
            contrast -= 0.07f;
            sharpness -= 0.06f;
        }
    }

    private static MasterStyleToneSummary ApplyMasterStyle(
        ImageAnalysisResult current,
        ImageAnalysisResult target,
        Configuration configuration,
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
        ref float highlightSaturationBias,
        out IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> colorFamilyAdjustments)
    {
        var strength = Clamp(configuration.MasterPresetStyleStrength / 100f, 0f, 1f);
        var colorStrength = strength * MasterStyleColorCompatibilityScale(configuration.CompatibilityMode);
        var scene = current.Available ? current : ImageAnalysisResult.Empty;
        colorFamilyAdjustments = CreateColorFamilyAdjustments(scene, target, colorStrength, current.Available);

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

        exposure += Clamp((luminanceDelta * 0.22f) + (midtoneDelta * 0.30f), -0.10f, 0.10f) * strength;
        shadowLift += Clamp(shadowDelta * 0.50f, -0.045f, 0.085f) * strength;
        blackPoint *= 1f + Clamp(-shadowDelta * 0.22f, -0.040f, 0.045f) * strength;
        whitePoint *= 1f + Clamp(highlightDelta * 0.12f, -0.025f, 0.025f) * strength;
        highlightRecovery *= 1f + Clamp(-highlightDelta * 0.55f, -0.12f, 0.16f) * strength;
        contrast += Clamp(spreadDelta * 0.55f, -0.12f, 0.12f) * strength;
        midtoneContrast += Clamp((spreadDelta * 0.35f) + (midtoneDelta * 0.20f), -0.10f, 0.10f) * strength;
        saturation += Clamp(saturationDelta * 0.80f, -0.18f, 0.18f) * strength;
        temperature += Clamp(warmthDelta * 0.95f, -0.24f, 0.24f) * strength;
        tint += Clamp(greenDelta * 0.75f, -0.16f, 0.16f) * strength;

        var shadowColor = ApplyTonalColorBias(scene.ShadowColor, target.ShadowColor, colorStrength, 0.085f, 0.22f, ref shadowHueBias, ref shadowSaturationBias);
        var midtoneColor = ApplyTonalColorBias(scene.MidtoneColor, target.MidtoneColor, colorStrength, 0.100f, 0.26f, ref midtoneHueBias, ref midtoneSaturationBias);
        var highlightColor = ApplyTonalColorBias(scene.HighlightColor, target.HighlightColor, colorStrength, 0.075f, 0.18f, ref highlightHueBias, ref highlightSaturationBias);

        if (target.HighlightClipping > 0.07f && target.AverageLuminance > 0.58f)
        {
            bloom += 0.10f * strength;
        }
        else if (current.Available && current.HighlightClipping > target.HighlightClipping + 0.04f)
        {
            bloom -= 0.14f * strength;
        }

        if (target.ShadowClipping < 0.08f && current.Available && current.ShadowClipping > target.ShadowClipping + 0.08f)
        {
            shadowLift += 0.12f * strength;
            ao -= 0.10f * strength;
        }

        if (target.Contrast > 0.26f && target.AverageSaturation > 0.42f)
        {
            clarity += 0.08f * strength;
            sharpness += 0.04f * strength;
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

        return new MasterStyleToneSummary(
            $"tonal percentile matching at {configuration.MasterPresetStyleStrength}% strength: {shadowText}, {highlightText}, {contrastText}; tonal color bias scale {colorStrength:0.##}.",
            $"shadow floor {sceneShadowFloor:0.00}->{target.ShadowFloor:0.00}, midtone {sceneMidtone:0.00}->{target.MidtoneLevel:0.00}, highlight {sceneHighlightCeiling:0.00}->{target.HighlightCeiling:0.00}, spread {sceneSpread:0.00}->{target.ContrastSpread:0.00}; shadow {shadowColor}, midtone {midtoneColor}, highlight {highlightColor}; color families {FormatStrongestFamilyAdjustments(colorFamilyAdjustments)}");
    }

    private static IReadOnlyDictionary<ColorFamily, ColorFamilyAdjustment> CreateColorFamilyAdjustments(ImageAnalysisResult current, ImageAnalysisResult target, float strength, bool hasCurrentScene)
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
                    Clamp(hue, -0.020f, 0.020f),
                    Clamp(saturation, -0.040f, 0.040f),
                    Clamp(luminance, -0.030f, 0.030f),
                    confidence);
            });
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

    private static float MasterStyleColorCompatibilityScale(PresetCompatibilityMode mode)
    {
        return mode switch
        {
            PresetCompatibilityMode.PreserveBase => 0f,
            PresetCompatibilityMode.AdaptiveBalanced => 0.55f,
            PresetCompatibilityMode.GameplaySanitize => 0.30f,
            PresetCompatibilityMode.CinematicPreserve => 0.75f,
            PresetCompatibilityMode.GposePreserve => 0.35f,
            _ => 0.55f
        };
    }

    private static void ApplyStyle(TargetStyle style, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness)
    {
        switch (style)
        {
            case TargetStyle.Gameplay:
                bloom = Lerp(1f, bloom, 0.50f);
                ao = Lerp(1f, ao, 0.60f);
                sharpness = Lerp(1f, sharpness, 0.50f);
                contrast = Lerp(1f, contrast, 0.60f);
                break;
            case TargetStyle.Cinematic:
                contrast = 1f + ((contrast - 1f) * 1.25f);
                saturation = 1f + ((saturation - 1f) * 1.10f);
                bloom = 1f + ((bloom - 1f) * 1.25f);
                ao = 1f + ((ao - 1f) * 1.15f);
                exposure = 1f + ((exposure - 1f) * 1.10f);
                break;
        }
    }

    private static void ApplyPerformanceBudget(PerformanceBudget budget, GameContext context, ref float ao, ref float rtgi, ref float relight, ref float depthEffects, ref float bloom, ref float bloomRadius, ref float sharpness, ref float sharpenThreshold, ref float clarity, ref float antiAliasingStrength, ref float lutStrength)
    {
        if (!context.InCombat)
        {
            return;
        }

        var aoScale = budget switch
        {
            PerformanceBudget.Low => 0.40f,
            PerformanceBudget.Medium => 0.65f,
            PerformanceBudget.High => 0.85f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.65f
        };

        ao *= aoScale;
        rtgi *= budget switch
        {
            PerformanceBudget.Low => 0.25f,
            PerformanceBudget.Medium => 0.60f,
            PerformanceBudget.High => 0.85f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.60f
        };
        relight *= budget switch
        {
            PerformanceBudget.Low => 0.35f,
            PerformanceBudget.Medium => 0.70f,
            PerformanceBudget.High => 0.90f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.70f
        };
        depthEffects *= budget switch
        {
            PerformanceBudget.Low => 0.35f,
            PerformanceBudget.Medium => 0.70f,
            PerformanceBudget.High => 0.90f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.70f
        };
        bloom *= budget switch
        {
            PerformanceBudget.Low => 0.65f,
            PerformanceBudget.Medium => 0.80f,
            PerformanceBudget.High => 0.92f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.80f
        };
        bloomRadius *= budget switch
        {
            PerformanceBudget.Low => 0.70f,
            PerformanceBudget.Medium => 0.85f,
            PerformanceBudget.High => 0.95f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.85f
        };
        clarity *= budget switch
        {
            PerformanceBudget.Low => 0.75f,
            PerformanceBudget.Medium => 0.90f,
            PerformanceBudget.High => 0.96f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.90f
        };
        sharpness *= budget switch
        {
            PerformanceBudget.Low => 0.85f,
            PerformanceBudget.Medium => 0.92f,
            PerformanceBudget.High => 0.97f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.92f
        };
        sharpenThreshold *= budget switch
        {
            PerformanceBudget.Low => 1.10f,
            PerformanceBudget.Medium => 1.06f,
            PerformanceBudget.High => 1.02f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 1.06f
        };
        antiAliasingStrength *= budget switch
        {
            PerformanceBudget.Low => 0.96f,
            PerformanceBudget.Medium => 1.00f,
            PerformanceBudget.High => 1.04f,
            PerformanceBudget.Ultra => 1.08f,
            _ => 1.00f
        };
        lutStrength *= budget switch
        {
            PerformanceBudget.Low => 0.82f,
            PerformanceBudget.Medium => 0.92f,
            PerformanceBudget.High => 0.98f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.92f
        };
    }

    private static float GetColorGradePreservation(PresetCompatibilityMode mode)
    {
        return mode switch
        {
            PresetCompatibilityMode.PreserveBase => 1.00f,
            PresetCompatibilityMode.AdaptiveBalanced => 0.60f,
            PresetCompatibilityMode.GameplaySanitize => 0.15f,
            PresetCompatibilityMode.CinematicPreserve => 0.85f,
            PresetCompatibilityMode.GposePreserve => 1.00f,
            _ => 0.60f
        };
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));

    private static float Lerp(float start, float end, float amount) => start + ((end - start) * amount);

    private static float Scale01(float value, float range) => Clamp(value / range, 0f, 1f);
}

public sealed record ProfileResult(VisualProfile Profile, IReadOnlyList<AppliedRule> Rules);

public sealed record AppliedRule(string Name, string Reason, string Changes);

internal sealed record MasterStyleToneSummary(string Description, string Changes);
