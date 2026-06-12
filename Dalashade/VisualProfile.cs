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
    private readonly MasterStyleMatcher masterStyleMatcher = new();

    public ProfileResult CreateWithRules(GameContext context, SceneTags tags, ImageAnalysisResult imageAnalysis, ImageAnalysisResult masterStyle, Configuration configuration, int masterImageCount = 0)
    {
        var rules = new List<AppliedRule>();
        var profile = Create(context, tags, imageAnalysis, masterStyle, configuration, masterImageCount, rules, out var diagnostics, out var tagStackDiagnostics);

        return new ProfileResult(profile, rules, diagnostics, tagStackDiagnostics);
    }

    public VisualProfile Create(GameContext context, SceneTags tags, ImageAnalysisResult imageAnalysis, ImageAnalysisResult masterStyle, Configuration configuration, int masterImageCount = 0)
    {
        return Create(context, tags, imageAnalysis, masterStyle, configuration, masterImageCount, null, out _, out _);
    }

    private VisualProfile Create(GameContext context, SceneTags tags, ImageAnalysisResult imageAnalysis, ImageAnalysisResult masterStyle, Configuration configuration, int masterImageCount, List<AppliedRule>? rules, out MasterStyleDiagnostics masterDiagnostics, out TagStackDiagnostics tagStackDiagnostics)
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
        masterDiagnostics = MasterStyleDiagnostics.FromUnavailable(
            configuration,
            imageAnalysis,
            masterStyle,
            masterImageCount,
            configuration.MatchMasterPresetStyle ? "Master analysis unavailable." : "Master style disabled.");
        var sceneIntent = new SceneIntentBuilder().Build(context, tags, imageAnalysis, configuration);
        var tagContributions = new List<TagStackContribution>();

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
            ApplyWeather(context, tags, ref exposure, ref contrast, ref saturation, ref bloom, ref sharpness, ref bloomRadius, ref bloomThreshold, ref bloomDirt, ref highlightRecovery, ref debandStrength, ref temperature, rules);
        }

        if (configuration.AutoAdjustForTerritory)
        {
            ApplyTerritory(tags, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift, rules);
            ApplyBiome(tags, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref highlightRecovery, ref shadowLift, ref temperature, ref bloomRadius, ref bloomThreshold, ref bloomDirt, ref aoRadius, ref debandStrength, ref midtoneContrast, rules);
        }

        if (configuration.AutoAdjustInCombat && tags.NeedsCombatClarity)
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
            rules?.Add(new AppliedRule("Combat clarity", "Active combat gets readability first; cinematic effects back off more strongly.", "bloom x0.75, AO x0.88, RTGI x0.78, depth x0.80"));
        }
        else if (configuration.AutoAdjustInCombat && tags.NeedsDutyReadability)
        {
            ao *= 0.94f;
            rtgi *= 0.92f;
            relight *= 0.94f;
            depthEffects *= 0.92f;
            bloom *= 0.88f;
            bloomRadius *= 0.90f;
            bloomThreshold *= 1.03f;
            bloomDirt *= 0.72f;
            sharpness *= 0.97f;
            sharpenThreshold *= 1.03f;
            clarity *= 0.96f;
            lutStrength *= 0.96f;
            shadowLift += 0.045f;
            rules?.Add(new AppliedRule("Duty readability", "Duty context outside combat gets a milder readability pass that preserves atmosphere.", "bloom x0.88, AO x0.94, depth x0.92, shadow lift +0.045"));
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

        ApplySceneIntentBudgets(sceneIntent, tags, configuration, ref bloom, ref ao, ref shadowLift, ref bloomDirt, ref saturation, tagContributions, rules);

        if (configuration.AutoAdjustFromScreenshots && imageAnalysis.Available)
        {
            ApplyImageAnalysis(imageAnalysis, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness, ref clarity, ref shadowLift, ref highlightRecovery, ref whitePoint, ref blackPoint, ref bloomThreshold, ref bloomDirt, ref debandStrength);
            rules?.Add(new AppliedRule("Screenshot feedback", $"Current image reads as {imageAnalysis.ProfileBucket}.", "brightness, clipping, saturation, and contrast correction"));
        }

        if (configuration.MatchMasterPresetStyle && masterStyle.Available)
        {
            var masterMatch = masterStyleMatcher.Match(
                imageAnalysis,
                masterStyle,
                configuration,
                masterImageCount);
            masterMatch.Deltas.ApplyTo(
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
                ref highlightSaturationBias);
            colorFamilyAdjustments = masterMatch.ColorFamilyAdjustments;
            masterDiagnostics = masterMatch.Diagnostics;
            rules?.Add(new AppliedRule("Master style", $"Reference look reads as {masterStyle.ProfileBucket}; {masterMatch.Description}", masterMatch.Changes));
            foreach (var rule in masterMatch.AppliedRules)
            {
                rules?.Add(rule);
            }
        }

        ApplyStyle(configuration.Style, ref exposure, ref contrast, ref saturation, ref bloom, ref ao, ref sharpness);
        rules?.Add(new AppliedRule("Style target", $"{configuration.Style} style preference applied.", "style weighting"));
        rules?.Add(new AppliedRule("Compatibility mode", $"{configuration.CompatibilityMode} mode sets ReGrade+ scalar color preservation to {colorGradePreservation:0.##}.", "ReGrade+ scalar hue/saturation preservation"));
        ApplyPerformanceBudget(configuration.PerformanceBudget, context, ref ao, ref rtgi, ref relight, ref depthEffects, ref bloom, ref bloomRadius, ref sharpness, ref sharpenThreshold, ref clarity, ref antiAliasingStrength, ref lutStrength);
        rules?.Add(new AppliedRule("Performance target", $"{configuration.PerformanceBudget} budget applied where needed.", "AO, RTGI, ReLight, bloom, clarity, sharpening"));
        tagStackDiagnostics = TagStackDiagnostics.Create(context, tags, sceneIntent, tagContributions);

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

    private static void ApplyWeather(GameContext context, SceneTags tags, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float sharpness, ref float bloomRadius, ref float bloomThreshold, ref float bloomDirt, ref float highlightRecovery, ref float debandStrength, ref float temperature, List<AppliedRule>? rules)
    {
        if (tags.IsFog)
        {
            contrast *= 1.025f;
            bloom *= 0.88f;
            bloomRadius *= 0.90f;
            bloomDirt *= 0.58f;
            sharpness *= 0.95f;
            debandStrength *= 1.12f;
            rules?.Add(new AppliedRule("Fog/mist weather", $"Weather is {context.WeatherName}; reduce haze-amplifying bloom and soften harsh edges.", "contrast x1.025, bloom x0.88, bloom dirt x0.58, deband x1.12"));
        }

        if (tags.IsCloudy || tags.IsOvercast)
        {
            exposure *= 0.99f;
            contrast *= tags.IsOvercast ? 1.015f : 1.01f;
            bloom *= 0.96f;
            bloomDirt *= 0.86f;
            debandStrength *= 1.04f;
            rules?.Add(new AppliedRule(tags.IsOvercast ? "Overcast weather" : "Cloudy weather", $"Weather is {context.WeatherName}; preserve clarity without treating clouds like full fog.", "exposure x0.99, bloom x0.96, bloom dirt x0.86"));
        }

        if (tags.IsGloom)
        {
            contrast *= 1.02f;
            bloom *= 0.92f;
            bloomDirt *= 0.72f;
            exposure *= 0.995f;
            bloomThreshold *= 1.03f;
            debandStrength *= 1.04f;
            rules?.Add(new AppliedRule("Gloom weather", $"Weather is {context.WeatherName}; treat gloom as a dark mood rather than generic fog.", "contrast x1.02, exposure x0.995, bloom x0.92"));
        }

        if (tags.IsRain)
        {
            contrast *= 1.025f;
            saturation *= 0.975f;
            bloom *= 0.92f;
            bloomRadius *= 0.96f;
            bloomDirt *= 0.68f;
            highlightRecovery *= 1.04f;
            rules?.Add(new AppliedRule("Rain/wet weather", $"Weather is {context.WeatherName}; keep wet specular detail without over-blooming.", "contrast x1.025, saturation x0.975, bloom dirt x0.68"));
        }

        if (tags.IsStorm)
        {
            contrast *= 1.035f;
            saturation *= 0.955f;
            bloom *= 0.84f;
            bloomRadius *= 0.88f;
            bloomThreshold *= 1.07f;
            bloomDirt *= 0.56f;
            highlightRecovery *= 1.12f;
            debandStrength *= 1.10f;
            temperature -= 0.025f;
            rules?.Add(new AppliedRule("Storm weather", $"Weather is {context.WeatherName}; add readability, cool the tone, and protect highlights.", "bloom x0.84, bloom dirt x0.56, highlight recovery x1.12, warmth -0.025"));
        }

        if (tags.IsDustStorm || tags.IsHeatWave)
        {
            var nightHeat = tags.IsNight && tags.IsHeatWave && !tags.IsDustStorm;
            exposure *= nightHeat ? 0.99f : 0.98f;
            contrast *= 1.02f;
            bloom *= 0.88f;
            bloomRadius *= 0.90f;
            bloomThreshold *= nightHeat ? 1.03f : 1.06f;
            bloomDirt *= 0.58f;
            highlightRecovery *= nightHeat ? 1.05f : 1.10f;
            sharpness *= 0.96f;
            temperature += 0.035f;
            rules?.Add(new AppliedRule(tags.IsDustStorm ? "Dust/sand weather" : "Heat weather", $"Weather is {context.WeatherName}; control glare and avoid sharpening haze too hard.", nightHeat ? "night heat: bloom x0.88, highlight recovery x1.05, warmth +0.035" : "bloom x0.88, bloom dirt x0.58, warmth +0.035"));
        }

        if (tags.IsSnow)
        {
            exposure *= 0.985f;
            saturation *= 0.985f;
            bloom *= 0.86f;
            bloomRadius *= 0.84f;
            bloomThreshold *= 1.10f;
            bloomDirt *= 0.50f;
            highlightRecovery *= 1.14f;
            debandStrength *= 1.06f;
            rules?.Add(new AppliedRule("Snow weather", $"Weather is {context.WeatherName}; protect white detail without making snow gray.", "exposure x0.985, saturation x0.985, bloom radius x0.84, bloom dirt x0.50"));
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

    private static void ApplyBiome(SceneTags tags, ref float exposure, ref float contrast, ref float saturation, ref float bloom, ref float ao, ref float sharpness, ref float clarity, ref float highlightRecovery, ref float shadowLift, ref float temperature, ref float bloomRadius, ref float bloomThreshold, ref float bloomDirt, ref float aoRadius, ref float debandStrength, ref float midtoneContrast, List<AppliedRule>? rules)
    {
        var styleScale = BiomeStyleScale(tags);

        switch (tags.BiomeKey)
        {
            case "snow":
                if (tags.IsSnow)
                {
                    bloom *= 0.96f;
                    bloomRadius *= 0.96f;
                    bloomThreshold *= 1.02f;
                    bloomDirt *= 0.88f;
                    rules?.Add(new AppliedRule("Snow/ice biome", "Snow weather is already active, so the snow biome contribution is dampened to avoid double stacking.", "dampened: bloom x0.96, bloom dirt x0.88"));
                }
                else
                {
                    exposure *= StyleMultiplier(0.985f, styleScale);
                    bloom *= StyleMultiplier(0.90f, styleScale);
                    bloomRadius *= StyleMultiplier(0.90f, styleScale);
                    bloomThreshold *= StyleMultiplier(1.07f, styleScale);
                    bloomDirt *= StyleMultiplier(0.66f, styleScale);
                    saturation *= StyleMultiplier(0.985f, styleScale);
                    rules?.Add(new AppliedRule("Snow/ice biome", "Protect bright whites and keep snowy zones from blooming into mush.", "exposure x0.985, bloom x0.90, saturation x0.985"));
                }
                break;
            case "forest":
                ao *= StyleMultiplier(0.94f, styleScale);
                aoRadius *= StyleMultiplier(0.90f, styleScale);
                sharpness *= StyleMultiplier(0.96f, styleScale);
                saturation *= StyleMultiplier(1.012f, styleScale);
                shadowLift += 0.025f * styleScale;
                rules?.Add(new AppliedRule("Forest biome", "Foliage-heavy scenes get softer depth and a touch more shadow readability.", "AO x0.94, sharpen x0.96, shadow lift +0.025"));
                break;
            case "jungle":
                ao *= StyleMultiplier(0.95f, styleScale);
                aoRadius *= StyleMultiplier(0.88f, styleScale);
                sharpness *= StyleMultiplier(0.94f, styleScale);
                saturation *= StyleMultiplier(tags.IsNight ? 1.035f : 1.024f, styleScale);
                midtoneContrast *= StyleMultiplier(tags.IsNight ? 1.018f : 1.006f, styleScale);
                bloom *= StyleMultiplier(tags.IsNight && !tags.NeedsCombatClarity ? 1.018f : 1.006f, styleScale);
                bloomDirt *= StyleMultiplier(0.78f, styleScale);
                shadowLift += (tags.IsNight ? 0.018f : 0.035f) * styleScale;
                rules?.Add(new AppliedRule("Jungle/rainforest biome", "Dense foliage gets selective shadow readability, richer greens, and softer depth without gray wash.", tags.IsNight ? "AO radius x0.88, shadow lift +0.018, saturation x1.025, midtone contrast x1.015" : "AO radius x0.88, sharpen x0.94, saturation x1.015"));
                break;
            case "swamp":
                ao *= 0.92f;
                aoRadius *= 0.88f;
                bloomDirt *= 0.82f;
                shadowLift += 0.035f;
                rules?.Add(new AppliedRule("Swamp/marsh biome", "Dense wet shadows get readability without adding extra bloom dirt.", "AO x0.92, AO radius x0.88, shadow lift +0.035"));
                break;
            case "desert":
                exposure *= StyleMultiplier(tags.IsNight ? 0.985f : 0.96f, styleScale);
                bloom *= StyleMultiplier(0.90f, styleScale);
                bloomRadius *= StyleMultiplier(0.92f, styleScale);
                bloomDirt *= StyleMultiplier(0.76f, styleScale);
                contrast *= StyleMultiplier(1.035f, styleScale);
                temperature += (tags.IsNight ? 0.014f : 0.032f) * styleScale;
                rules?.Add(new AppliedRule("Desert biome", "Hot bright zones keep shape by easing exposure/bloom and adding a little contrast.", "exposure x0.96, bloom x0.90, warmth +0.025"));
                break;
            case "wasteland":
                exposure *= 0.97f;
                bloom *= 0.91f;
                bloomRadius *= 0.92f;
                bloomDirt *= 0.72f;
                contrast *= 1.025f;
                saturation *= 0.985f;
                temperature += 0.018f;
                rules?.Add(new AppliedRule("Wasteland/badlands biome", "Dry glare gets highlight control while keeping terrain contrast.", "exposure x0.97, bloom x0.91, warmth +0.018"));
                break;
            case "steppe":
                contrast *= 1.025f;
                saturation *= 1.015f;
                bloom *= 0.98f;
                rules?.Add(new AppliedRule("Steppe/grassland biome", "Open grassland keeps natural color and mild scenic contrast.", "contrast x1.025, saturation x1.015"));
                break;
            case "alpine":
                bloom *= 0.92f;
                bloomRadius *= 0.92f;
                bloomThreshold *= 1.05f;
                bloomDirt *= 0.72f;
                contrast *= 1.015f;
                rules?.Add(new AppliedRule("Mountain/alpine biome", "Bright high-altitude scenes get controlled bloom and preserved contrast.", "bloom x0.92, bloom threshold x1.05"));
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
                saturation *= 0.985f;
                shadowLift += 0.035f;
                midtoneContrast *= 1.02f;
                rules?.Add(new AppliedRule("Void biome", "Dark purple/void scenes keep dramatic midtones while easing crushed shadows.", "saturation x0.985, shadow lift +0.035, midtone contrast x1.02"));
                break;
            case "aetherial":
                bloom *= 0.94f;
                bloomRadius *= 0.92f;
                bloomDirt *= 0.75f;
                clarity *= 1.03f;
                rules?.Add(new AppliedRule("Aetherial biome", "Glowy blue/crystal scenes get slightly cleaner edges without extra bloom.", "bloom x0.94, clarity x1.03"));
                break;
            case "fae":
                bloom *= tags.NeedsCombatClarity ? 0.94f : 1.04f;
                bloomRadius *= tags.NeedsCombatClarity ? 0.94f : 1.03f;
                saturation *= StyleMultiplier(1.032f, styleScale);
                rules?.Add(new AppliedRule("Fae/dreamlike biome", "Dreamlike zones can keep color and mild glow unless combat is active.", tags.NeedsCombatClarity ? "combat dampened bloom x0.94" : "bloom x1.04, saturation x1.02"));
                break;
            case "lightFlooded":
                exposure *= 0.97f;
                bloom *= 0.88f;
                bloomRadius *= 0.88f;
                bloomThreshold *= 1.08f;
                bloomDirt *= 0.62f;
                highlightRecovery *= 1.12f;
                rules?.Add(new AppliedRule("Light-flooded biome", "High-key light zones prioritize highlight detail and controlled glow.", "exposure x0.97, bloom x0.88, highlight recovery x1.12"));
                break;
            case "cosmic":
            case "lunar":
                bloom *= StyleMultiplier(0.96f, styleScale);
                bloomRadius *= StyleMultiplier(0.95f, styleScale);
                bloomDirt *= StyleMultiplier(0.76f, styleScale);
                clarity *= StyleMultiplier(1.025f, styleScale);
                midtoneContrast *= StyleMultiplier(1.015f, styleScale);
                temperature -= tags.BiomeKey == "lunar" ? 0.020f * styleScale : 0.010f * styleScale;
                rules?.Add(new AppliedRule(tags.BiomeKey == "lunar" ? "Lunar biome" : "Cosmic biome", "Space-like zones keep clean glow without letting bloom dirt take over.", "bloom x0.96, bloom dirt x0.76, clarity x1.02"));
                break;
            case "ancient":
                contrast *= 1.015f;
                clarity *= 1.025f;
                bloomDirt *= 0.84f;
                rules?.Add(new AppliedRule("Ancient/ruin biome", "Ancient stone and Allagan spaces get clearer structure with controlled dirt bloom.", "contrast x1.015, clarity x1.025"));
                break;
            case "imperial":
                saturation *= StyleMultiplier(0.965f, styleScale);
                clarity *= StyleMultiplier(1.045f, styleScale);
                bloom *= StyleMultiplier(0.94f, styleScale);
                bloomDirt *= StyleMultiplier(0.72f, styleScale);
                temperature -= 0.022f * styleScale;
                rules?.Add(new AppliedRule("Imperial/industrial biome", "Steel and magitek scenes get cooler clarity and restrained bloom.", "saturation x0.975, clarity x1.04, warmth -0.018"));
                break;
            case "highTech":
                bloom *= StyleMultiplier(0.96f, styleScale);
                bloomRadius *= StyleMultiplier(0.94f, styleScale);
                bloomDirt *= StyleMultiplier(0.62f, styleScale);
                bloomThreshold *= StyleMultiplier(1.045f, styleScale);
                saturation *= StyleMultiplier(1.026f, styleScale);
                clarity *= StyleMultiplier(1.028f, styleScale);
                contrast *= StyleMultiplier(1.018f, styleScale);
                rules?.Add(new AppliedRule("High-tech/neon biome", "Neon zones preserve accent color while controlling bloom dirt and highlights.", "bloom dirt x0.62, saturation x1.015, clarity x1.02"));
                break;
            case "coastal":
                exposure *= StyleMultiplier(tags.IsNight ? 1.005f : 1.018f, styleScale);
                bloom *= StyleMultiplier(0.97f, styleScale);
                bloomRadius *= StyleMultiplier(0.96f, styleScale);
                bloomThreshold *= StyleMultiplier(1.025f, styleScale);
                bloomDirt *= StyleMultiplier(0.78f, styleScale);
                saturation *= StyleMultiplier(1.035f, styleScale);
                sharpness *= StyleMultiplier(0.99f, styleScale);
                temperature += (tags.IsNight ? 0.006f : 0.018f) * styleScale;
                rules?.Add(new AppliedRule("Coastal biome", "Water, beach, and La Noscea field scenes keep bright color while protecting specular bloom.", "exposure x1.018, bloom threshold x1.025, saturation x1.035"));
                break;
            case "tropical":
                bloom *= StyleMultiplier(0.97f, styleScale);
                bloomDirt *= StyleMultiplier(0.80f, styleScale);
                saturation *= StyleMultiplier(1.038f, styleScale);
                sharpness *= StyleMultiplier(0.98f, styleScale);
                temperature += 0.020f * styleScale;
                rules?.Add(new AppliedRule("Tropical/island biome", "Island scenes preserve color while avoiding over-sharpened water and foliage.", "saturation x1.025, bloom dirt x0.80"));
                break;
            case "underwater":
                bloom *= 0.92f;
                bloomRadius *= 0.90f;
                bloomDirt *= 0.70f;
                sharpness *= 0.94f;
                debandStrength *= 1.10f;
                rules?.Add(new AppliedRule("Underwater/ocean-floor biome", "Underwater haze gets debanding and restrained bloom instead of extra sharpness.", "bloom x0.92, sharpen x0.94, deband x1.10"));
                break;
            case "volcanic":
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
        var pressure = context.InCombat
            ? 1f
            : budget == PerformanceBudget.Low
                ? 0.35f
                : 0f;

        if (pressure <= 0f)
        {
            return;
        }

        var aoTarget = budget switch
        {
            PerformanceBudget.Low => 0.40f,
            PerformanceBudget.Medium => 0.65f,
            PerformanceBudget.High => 0.85f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.65f
        };

        ao *= Lerp(1f, aoTarget, pressure);
        rtgi *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.25f,
            PerformanceBudget.Medium => 0.60f,
            PerformanceBudget.High => 0.85f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.60f
        }, pressure);
        relight *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.35f,
            PerformanceBudget.Medium => 0.70f,
            PerformanceBudget.High => 0.90f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.70f
        }, pressure);
        depthEffects *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.35f,
            PerformanceBudget.Medium => 0.70f,
            PerformanceBudget.High => 0.90f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.70f
        }, pressure);
        bloom *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.65f,
            PerformanceBudget.Medium => 0.80f,
            PerformanceBudget.High => 0.92f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.80f
        }, pressure);
        bloomRadius *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.70f,
            PerformanceBudget.Medium => 0.85f,
            PerformanceBudget.High => 0.95f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.85f
        }, pressure);
        clarity *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.75f,
            PerformanceBudget.Medium => 0.90f,
            PerformanceBudget.High => 0.96f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.90f
        }, pressure);
        sharpness *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.85f,
            PerformanceBudget.Medium => 0.92f,
            PerformanceBudget.High => 0.97f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.92f
        }, pressure);
        sharpenThreshold *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 1.10f,
            PerformanceBudget.Medium => 1.06f,
            PerformanceBudget.High => 1.02f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 1.06f
        }, pressure);
        antiAliasingStrength *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.96f,
            PerformanceBudget.Medium => 1.00f,
            PerformanceBudget.High => 1.04f,
            PerformanceBudget.Ultra => 1.08f,
            _ => 1.00f
        }, pressure);
        lutStrength *= Lerp(1f, budget switch
        {
            PerformanceBudget.Low => 0.82f,
            PerformanceBudget.Medium => 0.92f,
            PerformanceBudget.High => 0.98f,
            PerformanceBudget.Ultra => 1.00f,
            _ => 0.92f
        }, pressure);
    }

    private static void ApplySceneIntentBudgets(
        SceneIntent intent,
        SceneTags tags,
        Configuration configuration,
        ref float bloom,
        ref float ao,
        ref float shadowLift,
        ref float bloomDirt,
        ref float saturation,
        List<TagStackContribution> contributions,
        List<AppliedRule>? rules)
    {
        var bloomFloor = configuration.CompatibilityMode == PresetCompatibilityMode.GameplaySanitize
            ? 0.45f
            : tags.NeedsCombatClarity
                ? 0.55f
                : 0.70f;
        var aoFloor = configuration.CompatibilityMode == PresetCompatibilityMode.GameplaySanitize
            ? 0.48f
            : tags.NeedsCombatClarity
                ? 0.55f
                : 0.75f;
        var shadowLiftCap = tags.NeedsCombatClarity || tags.NeedsDutyReadability
            ? 0.32f
            : intent.ShadowProtection > 0.45f
                ? 0.28f
                : 0.20f;

        ApplyFloor("Bloom", "Scene intent stack budget", bloomFloor, ref bloom, contributions, rules);
        ApplyFloor("AmbientOcclusion", "Scene intent stack budget", aoFloor, ref ao, contributions, rules);
        ApplyCap("ShadowLift", "Scene intent stack budget", shadowLiftCap, ref shadowLift, contributions, rules);

        if (intent.HighlightProtection > 0.40f || intent.Haze > 0.55f || intent.Wetness > 0.55f)
        {
            var before = bloomDirt;
            var floor = 0.42f;
            bloomDirt = MathF.Max(floor, bloomDirt);
            if (MathF.Abs(before - bloomDirt) > 0.0005f)
            {
                contributions.Add(new TagStackContribution("BloomDirt", "Scene intent stack budget", $"floor {floor:0.###}", before, bloomDirt, false, true));
                rules?.Add(new AppliedRule("Bloom dirt stack budget", "Weather or specular-risk tags reduced bloom dirt repeatedly, so Dalashade held a conservative floor before screenshot/master-style correction.", $"bloom dirt floor {floor:0.###}"));
            }
        }

        if (intent.FoliageDensity > 0.50f || intent.NeonGlow > 0.50f)
        {
            var before = saturation;
            var floor = 0.92f;
            saturation = MathF.Max(floor, saturation);
            if (MathF.Abs(before - saturation) > 0.0005f)
            {
                contributions.Add(new TagStackContribution("Saturation", "Scene intent stack budget", $"floor {floor:0.###}", before, saturation, false, true));
                rules?.Add(new AppliedRule("Saturation stack budget", "Color-rich biome tags avoid repeated global desaturation; color-family controls are preferred when available.", $"saturation floor {floor:0.###}"));
            }
        }
    }

    private static void ApplyFloor(string variable, string source, float floor, ref float value, List<TagStackContribution> contributions, List<AppliedRule>? rules)
    {
        var before = value;
        value = MathF.Max(floor, value);
        if (MathF.Abs(before - value) <= 0.0005f)
        {
            return;
        }

        contributions.Add(new TagStackContribution(variable, source, $"floor {floor:0.###}", before, value, false, true));
        rules?.Add(new AppliedRule($"{variable} stack floor", $"{variable} contextual reductions reached the scene-intent floor.", $"{before:0.###} -> {value:0.###}"));
    }

    private static void ApplyCap(string variable, string source, float cap, ref float value, List<TagStackContribution> contributions, List<AppliedRule>? rules)
    {
        var before = value;
        value = MathF.Min(cap, value);
        if (MathF.Abs(before - value) <= 0.0005f)
        {
            return;
        }

        contributions.Add(new TagStackContribution(variable, source, $"cap {cap:0.###}", before, value, false, true));
        rules?.Add(new AppliedRule($"{variable} stack cap", $"{variable} contextual additions reached the scene-intent cap.", $"{before:0.###} -> {value:0.###}"));
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

    private static float BiomeStyleScale(SceneTags tags) => 0.55f + (Clamp(tags.BiomeConfidence, 0f, 1f) * 0.45f);

    private static float StyleMultiplier(float target, float scale) => 1f + ((target - 1f) * scale);
}

public sealed record ProfileResult(VisualProfile Profile, IReadOnlyList<AppliedRule> Rules, MasterStyleDiagnostics MasterStyleDiagnostics, TagStackDiagnostics TagStackDiagnostics);

public sealed record AppliedRule(string Name, string Reason, string Changes);
