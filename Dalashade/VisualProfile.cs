using System;
using System.Collections.Generic;

namespace Dalashade;

public sealed record VisualProfile(
    float Exposure,
    float Contrast,
    float Saturation,
    float Bloom,
    float AmbientOcclusion,
    float Sharpness,
    float Clarity,
    float ShadowLift,
    float Temperature,
    float Tint)
{
    public static VisualProfile Neutral { get; } = new(1f, 1f, 1f, 1f, 1f, 1f, 1f, 0f, 0f, 0f);
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
        var sharpness = 1f;
        var clarity = 1f;
        var shadowLift = 0f;
        var temperature = 0f;
        var tint = 0f;

        ApplyBasePolish(context, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift);
        rules?.Add(new AppliedRule("Base polish", "Small default lift so the generated preset feels like an upgrade, not just a safety mode.", "contrast, saturation, clarity, shadow lift"));

        if (configuration.AutoAdjustAtNight && tags.IsNight)
        {
            exposure *= 1.06f;
            shadowLift += 0.10f;
            ao *= 0.90f;
            rules?.Add(new AppliedRule("Night", "Lift the darks and ease off AO so night stays readable.", "exposure x1.06, shadow lift +0.10, AO x0.90"));
        }

        if (configuration.AutoAdjustAtNight && tags.IsDawnOrDusk)
        {
            bloom *= 1.04f;
            temperature += 0.025f;
            rules?.Add(new AppliedRule("Dawn/dusk", "Low sun can take a little warmth and glow without going full postcard.", "bloom x1.04, warmth +0.025"));
        }

        if (configuration.AutoAdjustForWeather)
        {
            ApplyWeather(context, tags, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, rules);
        }

        if (configuration.AutoAdjustForTerritory)
        {
            ApplyTerritory(tags, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift, rules);
        }

        if (configuration.AutoAdjustInCombat && tags.NeedsGameplayClarity)
        {
            ao *= 0.88f;
            bloom *= 0.75f;
            sharpness *= 0.92f;
            clarity *= 0.90f;
            shadowLift += 0.08f;
            rules?.Add(new AppliedRule("Gameplay clarity", "Combat or duty context gets readability first; cinematic effects back off.", "bloom x0.75, AO x0.88, sharpen x0.92, clarity x0.90"));
        }

        if (context.InGpose)
        {
            ao *= 1.12f;
            bloom *= 1.10f;
            contrast *= 1.04f;
            rules?.Add(new AppliedRule("GPose", "GPose can take the prettier settings because gameplay readability is not fighting for space.", "AO x1.12, bloom x1.10, contrast x1.04"));
        }
        else if (configuration.AutoAdjustInCutscenes && context.InCutscene)
        {
            ao *= 1.08f;
            bloom *= 1.06f;
            contrast *= 1.03f;
            rules?.Add(new AppliedRule("Cutscene", "Cutscene detected, so Dalashade allows a little more drama without pushing too hard.", "AO x1.08, bloom x1.06, contrast x1.03"));
        }

        if (configuration.AutoAdjustFromScreenshots && imageAnalysis.Available)
        {
            ApplyImageAnalysis(imageAnalysis, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift);
            rules?.Add(new AppliedRule("Screenshot feedback", $"Current image reads as {imageAnalysis.ProfileBucket}.", "brightness, clipping, saturation, and contrast correction"));
        }

        if (configuration.MatchMasterPresetStyle && masterStyle.Available)
        {
            ApplyMasterStyle(imageAnalysis, masterStyle, configuration, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift, ref temperature, ref tint);
            rules?.Add(new AppliedRule("Master style", $"Reference look reads as {masterStyle.ProfileBucket}; matching at {configuration.MasterPresetStyleStrength}% strength.", "exposure, contrast, saturation, warmth, tint"));
        }

        ApplyStyle(configuration.Style, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness);
        rules?.Add(new AppliedRule("Style target", $"{configuration.Style} style preference applied.", "style weighting"));
        ApplyPerformanceBudget(configuration.PerformanceBudget, context, ref ao);
        rules?.Add(new AppliedRule("Performance target", $"{configuration.PerformanceBudget} budget applied where needed.", "performance weighting"));

        return new VisualProfile(
            Clamp(exposure, 0.80f, 1.28f),
            Clamp(contrast, 0.80f, 1.28f),
            Clamp(saturation, 0.78f, 1.22f),
            Clamp(bloom, 0.40f, 1.35f),
            Clamp(ao, 0.20f, 1.30f),
            Clamp(sharpness, 0.45f, 1.20f),
            Clamp(clarity, 0.45f, 1.22f),
            Clamp(shadowLift, 0f, 0.35f),
            Clamp(temperature, -0.30f, 0.30f),
            Clamp(tint, -0.20f, 0.20f));
    }

    private static void ApplyWeather(GameContext context, SceneTags tags, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, List<AppliedRule>? rules)
    {
        if (tags.IsFog)
        {
            contrast *= 1.04f;
            bloom *= 0.90f;
            sharpness *= 0.92f;
            rules?.Add(new AppliedRule("Fog/clouds", $"Weather is {context.WeatherName}; reduce haze-amplifying effects and add a little contrast.", "contrast x1.04, bloom x0.90, sharpen x0.92"));
        }

        if (tags.IsRain || tags.IsStorm)
        {
            contrast *= 1.03f;
            saturation *= 0.96f;
            bloom *= 0.90f;
            rules?.Add(new AppliedRule(tags.IsStorm ? "Storm" : "Rain", $"Weather is {context.WeatherName}; keep wet scenes readable and less bloomy.", "contrast x1.03, saturation x0.96, bloom x0.90"));
        }

        if (tags.IsSnow)
        {
            exposure *= 0.97f;
            saturation *= 0.94f;
            bloom *= 0.82f;
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

    private static void ApplyImageAnalysis(ImageAnalysisResult image, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float shadowLift)
    {
        if (image.AverageLuminance < 0.30f)
        {
            exposure += Scale01(0.30f - image.AverageLuminance, 0.30f) * 0.14f;
            shadowLift += Scale01(image.ShadowClipping, 0.30f) * 0.16f;
            contrast -= Scale01(image.ShadowClipping, 0.25f) * 0.10f;
            ao -= Scale01(image.ShadowClipping, 0.25f) * 0.16f;
        }

        if (image.AverageLuminance > 0.72f || image.HighlightClipping > 0.04f)
        {
            exposure -= Scale01(image.AverageLuminance - 0.72f, 0.28f) * 0.11f;
            bloom -= Scale01(image.HighlightClipping, 0.12f) * 0.25f;
            contrast -= Scale01(image.HighlightClipping, 0.12f) * 0.07f;
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

    private static void ApplyMasterStyle(ImageAnalysisResult current, ImageAnalysisResult target, Configuration configuration, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float shadowLift, ref float temperature, ref float tint)
    {
        var strength = Clamp(configuration.MasterPresetStyleStrength / 100f, 0f, 1f);
        var scene = current.Available ? current : ImageAnalysisResult.Empty;

        var luminanceDelta = target.AverageLuminance - (current.Available ? scene.AverageLuminance : 0.50f);
        var contrastDelta = target.Contrast - (current.Available ? scene.Contrast : 0.22f);
        var saturationDelta = target.AverageSaturation - (current.Available ? scene.AverageSaturation : 0.38f);
        var warmthDelta = target.Warmth - (current.Available ? scene.Warmth : 0f);
        var greenDelta = target.GreenBias - (current.Available ? scene.GreenBias : 0f);

        exposure += Clamp(luminanceDelta * 0.55f, -0.16f, 0.16f) * strength;
        contrast += Clamp(contrastDelta * 1.05f, -0.18f, 0.18f) * strength;
        saturation += Clamp(saturationDelta * 0.80f, -0.18f, 0.18f) * strength;
        temperature += Clamp(warmthDelta * 0.95f, -0.24f, 0.24f) * strength;
        tint += Clamp(greenDelta * 0.75f, -0.16f, 0.16f) * strength;

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

    private static void ApplyPerformanceBudget(PerformanceBudget budget, GameContext context, ref float ao)
    {
        if (!context.InCombat)
        {
            return;
        }

        ao *= budget switch
        {
            PerformanceBudget.Low => 0.40f,
            PerformanceBudget.Medium => 0.65f,
            PerformanceBudget.High => 0.85f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.65f
        };
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));

    private static float Lerp(float start, float end, float amount) => start + ((end - start) * amount);

    private static float Scale01(float value, float range) => Clamp(value / range, 0f, 1f);
}

public sealed record ProfileResult(VisualProfile Profile, IReadOnlyList<AppliedRule> Rules);

public sealed record AppliedRule(string Name, string Reason, string Changes);
