using System;

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
    public VisualProfile Create(GameContext context, ImageAnalysisResult imageAnalysis, ImageAnalysisResult masterStyle, Configuration configuration)
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

        if (configuration.AutoAdjustAtNight && context.IsNight)
        {
            exposure += 0.08f;
            shadowLift += 0.10f;
            ao -= 0.15f;
        }

        if (configuration.AutoAdjustForWeather && IsSoftWeather(context.WeatherName))
        {
            contrast += 0.05f;
            bloom -= 0.10f;
            sharpness -= 0.10f;
            saturation -= 0.05f;
        }

        if (configuration.AutoAdjustForTerritory)
        {
            ApplyTerritory(context.WorldCategory, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift);
        }

        if (configuration.AutoAdjustInCombat && context.InCombat)
        {
            ao -= 0.30f;
            bloom -= 0.20f;
            sharpness -= 0.10f;
            clarity -= 0.15f;
        }

        if (configuration.AutoAdjustInCutscenes && context.InCutscene)
        {
            ao += 0.15f;
            bloom += 0.10f;
            contrast += 0.05f;
        }

        if (configuration.AutoAdjustFromScreenshots && imageAnalysis.Available)
        {
            ApplyImageAnalysis(imageAnalysis, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift);
        }

        if (configuration.MatchMasterPresetStyle && masterStyle.Available)
        {
            ApplyMasterStyle(imageAnalysis, masterStyle, configuration, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift, ref temperature, ref tint);
        }

        ApplyStyle(configuration.Style, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness);
        ApplyPerformanceBudget(configuration.PerformanceBudget, context, ref ao);

        return new VisualProfile(
            Clamp(exposure, 0.85f, 1.20f),
            Clamp(contrast, 0.85f, 1.20f),
            Clamp(saturation, 0.85f, 1.15f),
            Clamp(bloom, 0.50f, 1.25f),
            Clamp(ao, 0.25f, 1.25f),
            Clamp(sharpness, 0.50f, 1.15f),
            Clamp(clarity, 0.50f, 1.15f),
            Clamp(shadowLift, 0f, 0.25f),
            Clamp(temperature, -0.18f, 0.18f),
            Clamp(tint, -0.12f, 0.12f));
    }

    private static bool IsSoftWeather(string weatherName)
    {
        return weatherName.Contains("Fog", StringComparison.OrdinalIgnoreCase)
               || weatherName.Contains("Rain", StringComparison.OrdinalIgnoreCase)
               || weatherName.Contains("Showers", StringComparison.OrdinalIgnoreCase)
               || weatherName.Contains("Cloud", StringComparison.OrdinalIgnoreCase);
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

    private static void ApplyTerritory(WorldCategory category, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float shadowLift)
    {
        switch (category)
        {
            case WorldCategory.City:
                bloom -= 0.05f;
                sharpness -= 0.03f;
                ao -= 0.05f;
                shadowLift += 0.01f;
                break;
            case WorldCategory.Duty:
                bloom -= 0.12f;
                ao -= 0.12f;
                clarity -= 0.08f;
                sharpness -= 0.05f;
                break;
            case WorldCategory.Interior:
                exposure += 0.04f;
                contrast -= 0.03f;
                saturation += 0.02f;
                shadowLift += 0.03f;
                break;
            case WorldCategory.Field:
                contrast += 0.04f;
                saturation += 0.025f;
                bloom += 0.015f;
                break;
        }
    }

    private static void ApplyImageAnalysis(ImageAnalysisResult image, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float shadowLift)
    {
        if (image.AverageLuminance < 0.25f)
        {
            exposure += Scale01(0.25f - image.AverageLuminance, 0.25f) * 0.10f;
            shadowLift += Scale01(image.ShadowClipping, 0.30f) * 0.12f;
            contrast -= Scale01(image.ShadowClipping, 0.25f) * 0.08f;
            ao -= Scale01(image.ShadowClipping, 0.25f) * 0.12f;
        }

        if (image.AverageLuminance > 0.72f || image.HighlightClipping > 0.04f)
        {
            exposure -= Scale01(image.AverageLuminance - 0.72f, 0.28f) * 0.08f;
            bloom -= Scale01(image.HighlightClipping, 0.12f) * 0.18f;
            contrast -= Scale01(image.HighlightClipping, 0.12f) * 0.05f;
        }

        if (image.AverageSaturation < 0.24f)
        {
            saturation += 0.04f;
        }
        else if (image.AverageSaturation > 0.62f)
        {
            saturation -= 0.05f;
            bloom -= 0.03f;
        }

        if (image.Contrast < 0.14f)
        {
            contrast += 0.05f;
            clarity += 0.04f;
        }
        else if (image.Contrast > 0.32f)
        {
            contrast -= 0.05f;
            sharpness -= 0.04f;
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

        exposure += Clamp(luminanceDelta * 0.35f, -0.10f, 0.10f) * strength;
        contrast += Clamp(contrastDelta * 0.70f, -0.12f, 0.12f) * strength;
        saturation += Clamp(saturationDelta * 0.45f, -0.10f, 0.10f) * strength;
        temperature += Clamp(warmthDelta * 0.60f, -0.16f, 0.16f) * strength;
        tint += Clamp(greenDelta * 0.45f, -0.10f, 0.10f) * strength;

        if (target.HighlightClipping > 0.07f && target.AverageLuminance > 0.58f)
        {
            bloom += 0.05f * strength;
        }
        else if (current.Available && current.HighlightClipping > target.HighlightClipping + 0.04f)
        {
            bloom -= 0.08f * strength;
        }

        if (target.ShadowClipping < 0.08f && current.Available && current.ShadowClipping > target.ShadowClipping + 0.08f)
        {
            shadowLift += 0.08f * strength;
            ao -= 0.06f * strength;
        }

        if (target.Contrast > 0.26f && target.AverageSaturation > 0.42f)
        {
            clarity += 0.04f * strength;
            sharpness += 0.02f * strength;
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
