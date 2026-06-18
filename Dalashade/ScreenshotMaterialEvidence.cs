using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record ScreenshotMaterialEvidence(
    float FoliageVisible,
    float GrassTerrainVisible,
    float WaterVisible,
    float SandVisible,
    float SnowVisible,
    float StoneVisible,
    float MetalVisible,
    float SkyVisible,
    float AetherOrNeonVisible,
    float SkinOrCharacterVisible,
    float Confidence,
    IReadOnlyList<string> Evidence)
{
    public static ScreenshotMaterialEvidence Neutral(string reason) => new(
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        0f,
        string.IsNullOrWhiteSpace(reason) ? Array.Empty<string>() : new[] { reason });

    public float ValueFor(string channel) => channel switch
    {
        nameof(FoliageVisible) => FoliageVisible,
        nameof(GrassTerrainVisible) => GrassTerrainVisible,
        nameof(WaterVisible) => WaterVisible,
        nameof(SandVisible) => SandVisible,
        nameof(SnowVisible) => SnowVisible,
        nameof(StoneVisible) => StoneVisible,
        nameof(MetalVisible) => MetalVisible,
        nameof(SkyVisible) => SkyVisible,
        nameof(AetherOrNeonVisible) => AetherOrNeonVisible,
        nameof(SkinOrCharacterVisible) => SkinOrCharacterVisible,
        _ => 0f
    };
}

public sealed record MaterialEvidenceMismatch(
    string Channel,
    float VisibleEvidence,
    float CurrentIntent,
    float Severity,
    string Message);

public sealed record ScreenshotMaterialEvidenceDiagnostics(
    ScreenshotMaterialEvidence Evidence,
    IReadOnlyList<MaterialEvidenceMismatch> Mismatches)
{
    public static ScreenshotMaterialEvidenceDiagnostics Neutral(string reason) => new(
        ScreenshotMaterialEvidence.Neutral(reason),
        Array.Empty<MaterialEvidenceMismatch>());
}

public static class ScreenshotMaterialEvidenceAnalyzer
{
    public static ScreenshotMaterialEvidence Analyze(ImageAnalysisResult image, SceneTags tags, GameContext context)
    {
        if (!image.Available)
        {
            return ScreenshotMaterialEvidence.Neutral("Screenshot material evidence unavailable: no current screenshot analysis.");
        }

        var evidence = new List<string>();
        var upper = Region(image, ImageAnalysisRegion.UpperThird);
        var middle = Region(image, ImageAnalysisRegion.MiddleThird);
        var lower = Region(image, ImageAnalysisRegion.LowerThird);
        var center = Region(image, ImageAnalysisRegion.Center);
        var preliminarySkin = Clamp01((Family(center, ColorFamily.Orange) * WarmSkinTone(center) * 1.25f)
                                      + (Family(center, ColorFamily.Red) * WarmSkinTone(center) * 0.55f)
                                      + image.OpinionConfidence(ImageSceneOpinionKeys.SkinProtection) * 0.35f);
        var hardSurfacePressure = MathF.Max(GrayHard(middle), GrayHard(lower));

        var foliageRegion = WeightedRegionSignal(
            Family(middle, ColorFamily.Green) * Textured(middle),
            Family(lower, ColorFamily.Green) * Textured(lower),
            Family(center, ColorFamily.Green) * Textured(center),
            0.42f,
            0.42f,
            0.16f);
        var foliage = Clamp01(foliageRegion * 1.45f + image.OpinionConfidence(ImageSceneOpinionKeys.Foliage) * 0.35f);
        if (preliminarySkin > 0.55f && !HasAny(tags, "foliage", "grassland", "lush", "verdant", "forest", "jungle", "swamp"))
        {
            foliage *= 0.55f;
        }
        if (foliage > 0.18f)
        {
            evidence.Add($"Lower/middle green textured coverage supports foliage ({foliage:0.##}).");
        }

        var grass = Clamp01((Family(lower, ColorFamily.Green) * Textured(lower) * 1.55f)
                           + (tags.IsFieldLike ? 0.08f : 0f)
                           + (HasAny(tags, "grassland", "lush", "verdant") ? 0.08f : 0f));
        if (preliminarySkin > 0.55f && !HasAny(tags, "grassland", "lush", "verdant", "forest", "jungle", "swamp"))
        {
            grass *= 0.50f;
        }
        if (grass > 0.18f)
        {
            evidence.Add($"Lower-third green texture supports grass terrain ({grass:0.##}).");
        }

        var waterContext = tags.BiomeKey is "coastal" or "tropical" or "underwater"
                           || HasAny(tags, "water", "coastal", "seaside", "beach", "specular", "wet")
                           || tags.IsRain
                           || context.ContentName.Contains("sea", StringComparison.OrdinalIgnoreCase);
        var lowerCyanBlue = MathF.Max(Family(lower, ColorFamily.Cyan), Family(lower, ColorFamily.Blue));
        var smoothWaterCandidate = lowerCyanBlue * Smooth(lower) * (waterContext ? 1.35f : 0.42f);
        var water = Clamp01(smoothWaterCandidate + image.OpinionConfidence(ImageSceneOpinionKeys.WaterSurface) * (waterContext ? 0.30f : 0.06f));
        if (water > 0.16f)
        {
            evidence.Add(waterContext
                ? $"Lower smooth cyan/blue with water/coastal context supports water ({water:0.##})."
                : $"Lower smooth cyan/blue weakly supports water without context ({water:0.##}).");
        }

        var warmLower = MathF.Max(Family(lower, ColorFamily.Yellow), MathF.Max(Family(lower, ColorFamily.Orange), Family(middle, ColorFamily.Yellow) * 0.65f));
        var sandContext = tags.BiomeKey is "desert" or "wasteland" or "coastal" or "tropical"
                          || HasAny(tags, "desert", "badlands", "dry", "dust", "sand", "beach", "coastal");
        var lowerWarmTerrain = warmLower * Textured(lower);
        var likelySandTerrainWithoutContext = Family(lower, ColorFamily.Yellow) > 0.22f
                                             && Family(lower, ColorFamily.Orange) > 0.15f
                                             && Family(lower, ColorFamily.Green) < 0.12f
                                             && preliminarySkin < 0.28f
                                             && hardSurfacePressure < 0.50f
                                             && lower.BrightTendency > 0.10f;
        var sand = Clamp01(lowerWarmTerrain * (sandContext ? 1.45f : 0.85f)
                           + image.OpinionConfidence(ImageSceneOpinionKeys.SandDust) * (sandContext ? 0.22f : 0.06f));
        if (!sandContext && preliminarySkin > 0.18f && Family(center, ColorFamily.Orange) > 0.35f)
        {
            sand *= 0.45f;
        }

        if (!sandContext && MathF.Max(foliage, grass) > 0.45f)
        {
            sand *= 0.45f;
        }

        if (!sandContext && hardSurfacePressure > 0.42f)
        {
            sand *= 0.55f;
        }

        if (!sandContext && !likelySandTerrainWithoutContext)
        {
            sand = MathF.Min(sand, 0.34f);
        }
        if (sand > 0.16f)
        {
            evidence.Add(sandContext
                ? $"Warm lower textured regions with dry/coastal context support sand or dust ({sand:0.##})."
                : $"Warm lower textured regions weakly support sand or dust ({sand:0.##}).");
        }

        var snowContext = tags.IsSnow || tags.BiomeKey is "snow" or "alpine" or "lunar" || HasAny(tags, "snow", "ice", "cold", "alpine");
        var paleBright = MathF.Max(PaleBright(lower), MathF.Max(PaleBright(middle) * 0.8f, PaleBright(upper) * 0.55f));
        var paleTerrain = MathF.Max(PaleBright(lower) * Textured(lower), PaleBright(middle) * Textured(middle));
        var snow = Clamp01(paleBright * (snowContext ? 1.35f : 0.65f)
                           + paleTerrain * (snowContext ? 0.75f : 0.25f)
                           + image.OpinionConfidence(ImageSceneOpinionKeys.SnowIce) * (snowContext ? 0.45f : 0.22f));
        if (snow > 0.16f)
        {
            evidence.Add(snowContext
                ? $"Bright low-saturation regions with snow/cold context support snow or ice ({snow:0.##})."
                : $"Bright low-saturation regions weakly support snow or ice ({snow:0.##}).");
        }

        var grayBrownTexture = (GrayHard(middle) * 0.45f) + (GrayHard(lower) * 0.35f) + (BrownHard(middle) * 0.2f);
        var stone = Clamp01(grayBrownTexture * 1.35f + (tags.BiomeKey is "ancient" or "cave" ? 0.12f : 0f) + (HasAny(tags, "stone", "ruins", "ancient") ? 0.10f : 0f));
        if (stone > 0.16f)
        {
            evidence.Add($"Gray/brown textured hard-surface regions support stone or structures ({stone:0.##}).");
        }

        var metalContext = tags.BiomeKey is "highTech" or "imperial" || HasAny(tags, "metallic", "industrial", "highTech", "urban", "magitek");
        var metalCandidate = MathF.Max(GrayHard(center), GrayHard(middle)) * LowSaturationSpecular(center);
        var metal = Clamp01(metalCandidate * (metalContext ? 1.35f : 0.75f) + (metalContext ? 0.08f : 0f));
        if (metal > 0.14f)
        {
            evidence.Add(metalContext
                ? $"Low-saturation hard/specular regions with industrial context support metal ({metal:0.##})."
                : $"Low-saturation hard/specular regions weakly support metal ({metal:0.##}).");
        }

        var skyCandidate = MathF.Max(MathF.Max(Family(upper, ColorFamily.Blue), Family(upper, ColorFamily.Cyan)), PaleBright(upper) * 0.72f) * Smooth(upper);
        var sky = Clamp01(skyCandidate * 1.45f + image.OpinionConfidence(ImageSceneOpinionKeys.SkyAir) * 0.35f);
        if (sky > 0.16f)
        {
            evidence.Add($"Upper smooth blue/white/gray regions support sky, cloud, or fog ({sky:0.##}).");
        }

        var aetherContext = tags.BiomeKey is "aetherial" or "fae" or "cosmic" or "lunar" or "highTech"
                            || HasAny(tags, "aetherial", "magical", "crystal", "luminous", "neon", "highTech", "cosmic");
        var saturatedCool = MathF.Max(
            Family(center, ColorFamily.Cyan),
            MathF.Max(Family(center, ColorFamily.Blue), MathF.Max(Family(center, ColorFamily.Purple), Family(middle, ColorFamily.Purple)))) * Saturated(center, middle);
        var aether = Clamp01(saturatedCool * (aetherContext ? 1.45f : 0.55f) + image.OpinionConfidence(ImageSceneOpinionKeys.NeonAether) * (aetherContext ? 0.40f : 0.16f));
        if (aether > 0.16f)
        {
            evidence.Add(aetherContext
                ? $"Saturated cool glow with aether/neon context supports aether or neon ({aether:0.##})."
                : $"Saturated cool glow supports possible aether or neon but may be ambiguous ({aether:0.##}).");
        }

        var skin = preliminarySkin;
        if (skin > 0.16f)
        {
            evidence.Add($"Center-heavy warm skin-tone regions support character/skin presence ({skin:0.##}).");
        }

        var confidence = 0.78f;
        var penalties = new List<string>();
        var bottomUiRisk = lower.SmoothTendency > 0.74f && lower.AverageLuminance > 0.46f && lower.Contrast < 0.20f;
        if (bottomUiRisk)
        {
            confidence -= 0.18f;
            penalties.Add("lower region looks smooth/flat enough for UI or overlay contamination");
        }

        if (MathF.Max(water, aether) > 0.30f && MathF.Min(water, aether) > 0.20f)
        {
            confidence -= 0.12f;
            penalties.Add("cyan/blue evidence is ambiguous between water and aether/neon");
        }

        if (skin > 0.30f)
        {
            confidence -= skin > 0.55f ? 0.24f : 0.12f;
            penalties.Add("center skin/character evidence dominates material sampling");
        }

        if (Family(upper, ColorFamily.Green) > 0.24f && foliage < 0.16f)
        {
            confidence -= 0.08f;
            penalties.Add("green evidence appears mostly in the wrong screen region for foliage");
        }

        if (image.Contrast < 0.08f || image.AverageSaturation < 0.06f)
        {
            confidence -= 0.10f;
            penalties.Add("screenshot has low contrast or low saturation");
        }

        var strongEvidenceCount = new[] { foliage, grass, water, sand, snow, stone, metal, sky, aether, skin }.Count(value => value > 0.45f);
        if (strongEvidenceCount >= 5)
        {
            confidence -= 0.10f;
            penalties.Add("too many material families are high at once for a stable scene-level read");
        }

        foreach (var penalty in penalties)
        {
            evidence.Add($"Confidence penalty: {penalty}.");
        }

        if (evidence.Count == 0)
        {
            evidence.Add("No material family exceeded conservative screenshot evidence thresholds.");
        }

        return new ScreenshotMaterialEvidence(
            Clamp01(foliage),
            Clamp01(grass),
            Clamp01(water),
            Clamp01(sand),
            Clamp01(snow),
            Clamp01(stone),
            Clamp01(metal),
            Clamp01(sky),
            Clamp01(aether),
            Clamp01(skin),
            Clamp01(confidence),
            evidence.ToArray());
    }

    public static IReadOnlyList<MaterialEvidenceMismatch> Compare(ScreenshotMaterialEvidence evidence, MaterialIntent intent)
    {
        var mismatches = new List<MaterialEvidenceMismatch>();
        AddLowIntentMismatch(
            mismatches,
            MaterialIntent.FoliageChannel,
            MathF.Max(evidence.FoliageVisible, evidence.GrassTerrainVisible),
            intent.Foliage,
            0.42f,
            "Visible foliage/grass evidence is high but Foliage MaterialIntent is low.");
        AddModestIntentMismatch(
            mismatches,
            MaterialIntent.FoliageChannel,
            MathF.Max(evidence.FoliageVisible, evidence.GrassTerrainVisible),
            intent.Foliage,
            0.65f,
            0.28f,
            "Visible foliage/grass evidence is high but Foliage MaterialIntent is only modest; inspect foliage competition or suppression.");
        AddLowIntentMismatch(
            mismatches,
            MaterialIntent.WaterSpecularChannel,
            evidence.WaterVisible,
            intent.WaterSpecular,
            0.38f,
            "Visible water evidence is high but water/specular MaterialIntent is low.");
        AddLowIntentMismatch(
            mismatches,
            MaterialIntent.SandDustChannel,
            evidence.SandVisible,
            intent.SandDust,
            0.36f,
            "Visible sand/dust evidence is high but SandDust MaterialIntent is low.");
        AddLowIntentMismatch(
            mismatches,
            MaterialIntent.SnowIceChannel,
            evidence.SnowVisible,
            intent.SnowIce,
            0.36f,
            "Visible snow/ice evidence is high but SnowIce MaterialIntent is low.");

        if (evidence.AetherOrNeonVisible >= 0.34f && evidence.WaterVisible >= 0.28f)
        {
            mismatches.Add(new MaterialEvidenceMismatch(
                "CyanAmbiguity",
                MathF.Min(evidence.AetherOrNeonVisible, evidence.WaterVisible),
                MathF.Max(intent.CrystalAether, MathF.Max(intent.NeonGlass, intent.WaterSpecular)),
                Clamp01((evidence.AetherOrNeonVisible + evidence.WaterVisible) * 0.5f),
                "Aether/neon and water screenshot evidence are both high; cyan/blue material interpretation is ambiguous."));
        }

        var receiverRisk = MathF.Max(intent.WaterSpecular, MathF.Max(intent.StoneRuins, intent.MetalIndustrial));
        if (evidence.SkyVisible >= 0.48f && receiverRisk >= 0.32f)
        {
            mismatches.Add(new MaterialEvidenceMismatch(
                "SkySafety",
                evidence.SkyVisible,
                receiverRisk,
                Clamp01((evidence.SkyVisible + receiverRisk) * 0.5f),
                "Sky evidence is high while reflection/AO receiver-like MaterialIntent is also high; inspect sky safety and receiver gating."));
        }

        return mismatches
            .OrderByDescending(mismatch => mismatch.Severity)
            .ToArray();
    }

    public static ScreenshotMaterialEvidenceDiagnostics BuildDiagnostics(ImageAnalysisResult image, SceneTags tags, GameContext context, MaterialIntent intent)
    {
        var evidence = Analyze(image, tags, context);
        return new ScreenshotMaterialEvidenceDiagnostics(evidence, Compare(evidence, intent));
    }

    private static void AddLowIntentMismatch(List<MaterialEvidenceMismatch> mismatches, string channel, float visible, float currentIntent, float threshold, string message)
    {
        if (visible < threshold || currentIntent >= 0.18f)
        {
            return;
        }

        mismatches.Add(new MaterialEvidenceMismatch(
            channel,
            visible,
            currentIntent,
            Clamp01((visible - currentIntent) / MathF.Max(0.001f, 1f - currentIntent)),
            message));
    }

    private static void AddModestIntentMismatch(List<MaterialEvidenceMismatch> mismatches, string channel, float visible, float currentIntent, float visibleThreshold, float intentCeiling, string message)
    {
        if (visible < visibleThreshold || currentIntent < 0.18f || currentIntent >= intentCeiling)
        {
            return;
        }

        mismatches.Add(new MaterialEvidenceMismatch(
            channel,
            visible,
            currentIntent,
            Clamp01((visible - currentIntent) * 0.48f),
            message));
    }

    private static ImageRegionStats Region(ImageAnalysisResult image, ImageAnalysisRegion region)
    {
        return image.Regions.TryGetValue(region, out var value) ? value : ImageRegionStats.Empty(region);
    }

    private static float Family(ImageRegionStats stats, ColorFamily family)
    {
        return stats.ColorFamilies.TryGetValue(family, out var value) ? value.Confidence : 0f;
    }

    private static float Smooth(ImageRegionStats stats)
    {
        return Clamp01(stats.SmoothTendency);
    }

    private static float Textured(ImageRegionStats stats)
    {
        return Clamp01((1f - stats.SmoothTendency) * 0.72f + stats.Contrast * 1.05f);
    }

    private static float Saturated(ImageRegionStats primary, ImageRegionStats fallback)
    {
        return Clamp01(MathF.Max(primary.AverageSaturation, fallback.AverageSaturation) * 1.35f);
    }

    private static float PaleBright(ImageRegionStats stats)
    {
        return Clamp01(stats.BrightTendency * (1f - stats.AverageSaturation * 0.72f) * (0.55f + stats.SmoothTendency * 0.45f));
    }

    private static float GrayHard(ImageRegionStats stats)
    {
        return Clamp01((1f - stats.AverageSaturation) * Textured(stats) * (0.45f + stats.AverageLuminance * 0.55f));
    }

    private static float BrownHard(ImageRegionStats stats)
    {
        return Clamp01(MathF.Max(Family(stats, ColorFamily.Orange), Family(stats, ColorFamily.Yellow) * 0.75f) * Textured(stats) * (1f - stats.BrightTendency * 0.35f));
    }

    private static float LowSaturationSpecular(ImageRegionStats stats)
    {
        return Clamp01((1f - stats.AverageSaturation) * (stats.BrightTendency * 0.55f + stats.Contrast * 0.70f));
    }

    private static float WarmSkinTone(ImageRegionStats stats)
    {
        return Clamp01(stats.AverageSaturation * 1.1f * (1f - MathF.Abs(stats.AverageLuminance - 0.56f) * 1.35f) * (0.55f + stats.SmoothTendency * 0.45f));
    }

    private static float WeightedRegionSignal(float first, float second, float third, float firstWeight, float secondWeight, float thirdWeight)
    {
        return first * firstWeight + second * secondWeight + third * thirdWeight;
    }

    private static bool HasAny(SceneTags tags, params string[] values)
    {
        return values.Any(value =>
            string.Equals(tags.BiomeKey, value, StringComparison.OrdinalIgnoreCase)
            || string.Equals(tags.AreaKey, value, StringComparison.OrdinalIgnoreCase)
            || tags.MoodTags.Contains(value, StringComparer.OrdinalIgnoreCase));
    }

    private static float Clamp01(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }
}
