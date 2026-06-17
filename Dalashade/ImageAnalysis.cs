using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Dalashade;

public sealed record TonalColorBias(float Hue, float Saturation, float Warmth, float Tint)
{
    public static TonalColorBias Empty { get; } = new(0f, 0f, 0f, 0f);
}

public enum ColorFamily
{
    Red,
    Orange,
    Yellow,
    Green,
    Cyan,
    Blue,
    Purple,
    Magenta
}

public sealed record ColorFamilyStats(ColorFamily Family, float Hue, float Saturation, float Luminance, float Coverage, float Confidence)
{
    public static ColorFamilyStats Empty(ColorFamily family) => new(family, GetFamilyHueCenter(family), 0f, 0f, 0f, 0f);

    public static IReadOnlyDictionary<ColorFamily, ColorFamilyStats> EmptyMap { get; } = Enum.GetValues<ColorFamily>()
        .ToDictionary(family => family, Empty);

    public static float GetFamilyHueCenter(ColorFamily family)
    {
        return family switch
        {
            ColorFamily.Red => 0.000f,
            ColorFamily.Orange => 0.083f,
            ColorFamily.Yellow => 0.167f,
            ColorFamily.Green => 0.333f,
            ColorFamily.Cyan => 0.500f,
            ColorFamily.Blue => 0.625f,
            ColorFamily.Purple => 0.750f,
            ColorFamily.Magenta => 0.875f,
            _ => 0f
        };
    }
}

public enum ImageAnalysisRegion
{
    UpperThird,
    MiddleThird,
    LowerThird,
    Center
}

public static class ImageSceneOpinionKeys
{
    public const string ShadowRecovery = "shadowRecovery";
    public const string HighlightProtection = "highlightProtection";
    public const string ClarityBoost = "clarityBoost";
    public const string SaturationLift = "saturationLift";
    public const string SaturationRestraint = "saturationRestraint";
    public const string SkyAir = "skyAir";
    public const string WaterSurface = "waterSurface";
    public const string Foliage = "foliage";
    public const string SandDust = "sandDust";
    public const string SnowIce = "snowIce";
    public const string SkinProtection = "skinProtection";
    public const string NeonAether = "neonAether";
}

public sealed record ImageSceneOpinion(
    string Key,
    string Label,
    float Confidence,
    string Target,
    string Reason)
{
    public static IReadOnlyList<ImageSceneOpinion> EmptyList { get; } = Array.Empty<ImageSceneOpinion>();
}

public sealed record ImageRegionStats(
    ImageAnalysisRegion Region,
    float AverageLuminance,
    float Contrast,
    float AverageSaturation,
    float BrightTendency,
    float DarkTendency,
    float SmoothTendency,
    IReadOnlyDictionary<ColorFamily, ColorFamilyStats> ColorFamilies)
{
    public static ImageRegionStats Empty(ImageAnalysisRegion region) => new(region, 0f, 0f, 0f, 0f, 0f, 0f, ColorFamilyStats.EmptyMap);
}

public sealed record ImageAnalysisResult(
    bool Available,
    string SourcePath,
    DateTimeOffset SourceTimestamp,
    float AverageLuminance,
    float Contrast,
    float AverageSaturation,
    float ShadowClipping,
    float HighlightClipping,
    float Warmth,
    float GreenBias,
    float LuminanceP05,
    float LuminanceP25,
    float LuminanceP50,
    float LuminanceP75,
    float LuminanceP95,
    TonalColorBias ShadowColor,
    TonalColorBias MidtoneColor,
    TonalColorBias HighlightColor,
    IReadOnlyDictionary<ColorFamily, ColorFamilyStats> ColorFamilies,
    IReadOnlyDictionary<ImageAnalysisRegion, ImageRegionStats> Regions,
    IReadOnlyList<ImageSceneOpinion> Opinions)
{
    public static IReadOnlyDictionary<ImageAnalysisRegion, ImageRegionStats> EmptyRegionMap { get; } = Enum.GetValues<ImageAnalysisRegion>()
        .ToDictionary(region => region, ImageRegionStats.Empty);

    public static ImageAnalysisResult Empty { get; } = new(false, string.Empty, DateTimeOffset.MinValue, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, TonalColorBias.Empty, TonalColorBias.Empty, TonalColorBias.Empty, ColorFamilyStats.EmptyMap, EmptyRegionMap, ImageSceneOpinion.EmptyList);

    public float ShadowFloor => LuminanceP05;
    public float MidtoneLevel => LuminanceP50;
    public float HighlightCeiling => LuminanceP95;
    public float ContrastSpread => MathF.Max(0f, LuminanceP75 - LuminanceP25);
    public IReadOnlyList<ImageSceneOpinion> StrongOpinions => Opinions
        .Where(opinion => opinion.Confidence >= 0.18f)
        .OrderByDescending(opinion => opinion.Confidence)
        .ToArray();

    public string OpinionSummary
    {
        get
        {
            if (!Available)
            {
                return "No screenshot analysis is available.";
            }

            var strong = StrongOpinions.Take(4).ToArray();
            return strong.Length == 0
                ? "Screenshot analysis found no strong scene opinion."
                : string.Join("; ", strong.Select(opinion => $"{opinion.Label} ({opinion.Confidence:0.##})"));
        }
    }

    public float OpinionConfidence(string key)
    {
        return Opinions.FirstOrDefault(opinion => string.Equals(opinion.Key, key, StringComparison.OrdinalIgnoreCase))?.Confidence ?? 0f;
    }

    public string MetricsKey
    {
        get
        {
            if (!Available)
            {
                return "none";
            }

            return FormattableString.Invariant(
                $"l{AverageLuminance:0.00}:c{Contrast:0.00}:s{AverageSaturation:0.00}:sh{ShadowClipping:0.00}:hi{HighlightClipping:0.00}:p05{LuminanceP05:0.00}:p50{LuminanceP50:0.00}:p95{LuminanceP95:0.00}:w{Warmth:0.00}:g{GreenBias:0.00}:sc{ShadowColor.Hue:0.00}/{ShadowColor.Saturation:0.00}:mc{MidtoneColor.Hue:0.00}/{MidtoneColor.Saturation:0.00}:hc{HighlightColor.Hue:0.00}/{HighlightColor.Saturation:0.00}:cf{ColorFamilyMetricsKey(ColorFamilies)}:rg{RegionMetricsKey(Regions)}:op{OpinionMetricsKey(Opinions)}");
        }
    }

    public string ProfileBucket
    {
        get
        {
            if (!Available)
            {
                return "none";
            }

            var brightness = AverageLuminance switch
            {
                < 0.24f => "dark",
                > 0.76f => "bright",
                _ => "balanced"
            };

            var clipping = ShadowClipping > 0.18f ? "crushed" : HighlightClipping > 0.08f ? "clipped" : "ok";
            var saturation = AverageSaturation > 0.58f ? "saturated" : AverageSaturation < 0.22f ? "muted" : "color";

            return FormattableString.Invariant($"{brightness}:{clipping}:{saturation}");
        }
    }

    private static string ColorFamilyMetricsKey(IReadOnlyDictionary<ColorFamily, ColorFamilyStats> families)
    {
        return string.Join(
            ";",
            Enum.GetValues<ColorFamily>().Select(family =>
            {
                var stats = families.TryGetValue(family, out var value) ? value : ColorFamilyStats.Empty(family);
                return FormattableString.Invariant($"{family.ToString()[0]}{stats.Hue:0.00}/{stats.Saturation:0.00}/{stats.Luminance:0.00}/{stats.Confidence:0.00}");
            }));
    }

    private static string RegionMetricsKey(IReadOnlyDictionary<ImageAnalysisRegion, ImageRegionStats> regions)
    {
        return string.Join(
            ";",
            Enum.GetValues<ImageAnalysisRegion>().Select(region =>
            {
                var stats = regions.TryGetValue(region, out var value) ? value : ImageRegionStats.Empty(region);
                return FormattableString.Invariant($"{region.ToString()[0]}{stats.AverageLuminance:0.00}/{stats.AverageSaturation:0.00}/{stats.SmoothTendency:0.00}");
            }));
    }

    private static string OpinionMetricsKey(IReadOnlyList<ImageSceneOpinion> opinions)
    {
        return string.Join(
            ";",
            opinions
                .OrderBy(opinion => opinion.Key, StringComparer.OrdinalIgnoreCase)
                .Select(opinion => FormattableString.Invariant($"{opinion.Key}:{opinion.Confidence:0.00}")));
    }
}

public sealed class ImageAnalysisService
{
    public static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

    private DateTimeOffset lastSample = DateTimeOffset.MinValue;
    private string lastSourcePath = string.Empty;
    private DateTime lastSourceWriteTime = DateTime.MinValue;

    public ImageAnalysisResult Current { get; private set; } = ImageAnalysisResult.Empty;
    public string LastMessage { get; private set; } = "Image analysis has not run yet.";

    public void Refresh(Configuration configuration, bool force = false)
    {
        if (!configuration.AutoAdjustFromScreenshots)
        {
            Current = ImageAnalysisResult.Empty;
            LastMessage = "Image analysis is disabled.";
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.ScreenshotFolderPath))
        {
            Current = ImageAnalysisResult.Empty;
            LastMessage = "Screenshot folder is empty.";
            return;
        }

        var minimumInterval = TimeSpan.FromSeconds(Math.Max(1, configuration.MinimumSecondsBetweenImageSamples));
        if (!force && DateTimeOffset.UtcNow - lastSample < minimumInterval)
        {
            return;
        }

        lastSample = DateTimeOffset.UtcNow;

        try
        {
            var latest = FindLatestImage(configuration.ScreenshotFolderPath);
            if (latest == null)
            {
                Current = ImageAnalysisResult.Empty;
                LastMessage = "No supported screenshots found.";
                return;
            }

            if (!force && latest.FullName == lastSourcePath && latest.LastWriteTimeUtc == lastSourceWriteTime)
            {
                return;
            }

            Current = Analyze(latest.FullName, latest.LastWriteTimeUtc, configuration.ImageSamplingMode);
            lastSourcePath = latest.FullName;
            lastSourceWriteTime = latest.LastWriteTimeUtc;
            LastMessage = $"Analyzed {latest.Name}: {Current.ProfileBucket}. {Current.OpinionSummary}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
        {
            Current = ImageAnalysisResult.Empty;
            LastMessage = $"Image analysis skipped this file: {ex.Message}";
        }
    }
    

    public static FileInfo? FindLatestImage(string folderPath)
    {
        var directory = new DirectoryInfo(folderPath);
        if (!directory.Exists)
        {
            return null;
        }

        return directory.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
            .Where(file => SupportedExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();
    }

    public static ImageAnalysisResult Analyze(string imagePath, DateTime sourceWriteTimeUtc, ImageSamplingMode samplingMode = ImageSamplingMode.CenterWeighted)
    {
        using var stream = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.Position = 0;

        using var image = Image.Load<Rgba32>(memory);

        var stepX = Math.Max(1, image.Width / 160);
        var stepY = Math.Max(1, image.Height / 90);
        double weightSum = 0;
        double luminanceSum = 0;
        double luminanceSquaredSum = 0;
        double saturationSum = 0;
        double warmthSum = 0;
        double greenBiasSum = 0;
        double shadowWeight = 0;
        double highlightWeight = 0;
        var luminanceSamples = new List<WeightedLuminanceSample>();
        var colorSamples = new List<WeightedColorSample>();
        var regionAccumulators = Enum.GetValues<ImageAnalysisRegion>()
            .ToDictionary(region => region, _ => new RegionAccumulator());

        for (var y = 0; y < image.Height; y += stepY)
        {
            for (var x = 0; x < image.Width; x += stepX)
            {
                var weight = GetSampleWeight(x, y, image.Width, image.Height, samplingMode);
                if (weight <= 0f)
                {
                    continue;
                }

                var pixel = image[x, y];
                var r = pixel.R / 255f;
                var g = pixel.G / 255f;
                var b = pixel.B / 255f;
                var luminance = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
                var max = Math.Max(r, Math.Max(g, b));
                var min = Math.Min(r, Math.Min(g, b));
                var saturation = max <= 0.0001f ? 0f : (max - min) / max;
                var warmth = r - b;
                var tint = g - ((r + b) * 0.5f);

                luminanceSum += luminance * weight;
                luminanceSquaredSum += luminance * luminance * weight;
                saturationSum += saturation * weight;
                warmthSum += warmth * weight;
                greenBiasSum += tint * weight;
                weightSum += weight;
                luminanceSamples.Add(new WeightedLuminanceSample(luminance, weight));
                var hue = GetHue01(r, g, b, max, min);
                colorSamples.Add(new WeightedColorSample(luminance, weight, hue, saturation, warmth, tint));
                foreach (var region in RegionsForSample(x, y, image.Width, image.Height))
                {
                    regionAccumulators[region].Add(new WeightedColorSample(luminance, weight, hue, saturation, warmth, tint));
                }

                if (luminance < 0.035f)
                {
                    shadowWeight += weight;
                }
                else if (luminance > 0.965f)
                {
                    highlightWeight += weight;
                }
            }
        }

        if (weightSum <= 0)
        {
            return ImageAnalysisResult.Empty;
        }

        var averageLuminance = (float)(luminanceSum / weightSum);
        var variance = Math.Max(0, (luminanceSquaredSum / weightSum) - (averageLuminance * averageLuminance));
        var contrast = (float)Math.Sqrt(variance);
        var averageSaturation = (float)(saturationSum / weightSum);
        var percentiles = LuminancePercentiles.From(luminanceSamples, (float)weightSum);
        var shadowColor = AverageTonalColor(colorSamples.Where(sample => sample.Luminance <= percentiles.P25));
        var midtoneColor = AverageTonalColor(colorSamples.Where(sample => sample.Luminance > percentiles.P25 && sample.Luminance < percentiles.P75));
        var highlightColor = AverageTonalColor(colorSamples.Where(sample => sample.Luminance >= percentiles.P75));
        var colorFamilies = AnalyzeColorFamilies(colorSamples, (float)weightSum);

        var regions = regionAccumulators.ToDictionary(pair => pair.Key, pair => pair.Value.ToStats(pair.Key));
        var preliminary = new ImageAnalysisResult(
            true,
            imagePath,
            new DateTimeOffset(sourceWriteTimeUtc, TimeSpan.Zero),
            averageLuminance,
            contrast,
            averageSaturation,
            (float)(shadowWeight / weightSum),
            (float)(highlightWeight / weightSum),
            (float)(warmthSum / weightSum),
            (float)(greenBiasSum / weightSum),
            percentiles.P05,
            percentiles.P25,
            percentiles.P50,
            percentiles.P75,
            percentiles.P95,
            shadowColor,
            midtoneColor,
            highlightColor,
            colorFamilies,
            regions,
            ImageSceneOpinion.EmptyList);

        return preliminary with { Opinions = BuildSceneOpinions(preliminary) };
    }

    private static IReadOnlyList<ImageSceneOpinion> BuildSceneOpinions(ImageAnalysisResult image)
    {
        var opinions = new List<ImageSceneOpinion>();
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.ShadowRecovery,
            "Open dark shadows",
            MathF.Max(Scale01(0.30f - image.AverageLuminance, 0.30f), Scale01(image.ShadowClipping - 0.05f, 0.25f)),
            "VisualProfile.ShadowLift, SceneIntent.ShadowProtection, readability",
            "The screenshot is dark or has crushed shadow mass.");
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.HighlightProtection,
            "Protect bright highlights",
            MathF.Max(Scale01(image.AverageLuminance - 0.70f, 0.30f), Scale01(image.HighlightClipping - 0.02f, 0.12f)),
            "VisualProfile.Exposure/Bloom, SceneIntent.HighlightProtection",
            "The screenshot is bright or already clipping highlights.");
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.ClarityBoost,
            "Recover flat contrast",
            image.HighlightClipping < 0.05f ? Scale01(0.16f - image.Contrast, 0.16f) : 0f,
            "VisualProfile.Contrast/Clarity, SceneIntent.Readability",
            "The screenshot has low contrast without major highlight clipping.");
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.SaturationLift,
            "Lift muted color",
            Scale01(0.25f - image.AverageSaturation, 0.25f),
            "VisualProfile.Saturation",
            "The screenshot is globally muted.");
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.SaturationRestraint,
            "Restrain oversaturation",
            Scale01(image.AverageSaturation - 0.58f, 0.32f),
            "VisualProfile.Saturation/Bloom",
            "The screenshot is already highly saturated.");

        var skyConfidence = RegionConfidence(image, ImageAnalysisRegion.UpperThird, region =>
        {
            var blueCyan = FamilyConfidence(region, ColorFamily.Blue) + FamilyConfidence(region, ColorFamily.Cyan);
            return MathF.Min(1f, (blueCyan * 0.65f) + (region.BrightTendency * 0.35f) + (region.SmoothTendency * 0.25f));
        });
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.SkyAir,
            "Likely visible sky or air",
            skyConfidence,
            "SceneIntent.OpenSkyLight/DayAtmosphere, Material SkyCloudFog",
            "The upper image is smooth and blue, cyan, or bright.");

        var lowerBlue = RegionConfidence(image, ImageAnalysisRegion.LowerThird, region => FamilyConfidence(region, ColorFamily.Blue) + FamilyConfidence(region, ColorFamily.Cyan));
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.WaterSurface,
            "Likely water surface",
            MathF.Min(1f, lowerBlue * 0.75f),
            "SceneIntent.DayReflection/Wetness, Material WaterSpecular",
            "The lower image contains confident blue/cyan surface color.");

        var green = MathF.Max(
            FamilyConfidence(image, ColorFamily.Green),
            RegionConfidence(image, ImageAnalysisRegion.MiddleThird, region => FamilyConfidence(region, ColorFamily.Green)));
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.Foliage,
            "Likely foliage",
            green,
            "SceneIntent.FoliageDensity, Material Foliage",
            "The screenshot contains confident green mid-image or global color evidence.");

        var lowerWarm = RegionConfidence(image, ImageAnalysisRegion.LowerThird, region => FamilyConfidence(region, ColorFamily.Yellow) + FamilyConfidence(region, ColorFamily.Orange));
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.SandDust,
            "Likely sand or warm ground",
            MathF.Min(1f, lowerWarm * 0.75f),
            "SceneIntent.SurfaceHeat, Material SandDust",
            "The lower image contains warm yellow/orange surface color.");

        var snow = RegionConfidence(image, ImageAnalysisRegion.LowerThird, region =>
            region.BrightTendency > 0.18f && region.AverageSaturation < 0.30f
                ? MathF.Min(1f, region.BrightTendency + (0.30f - region.AverageSaturation))
                : 0f);
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.SnowIce,
            "Likely snow or ice",
            snow,
            "SceneIntent.Cold, Material SnowIce",
            "The lower image is bright and low saturation.");

        var skinLike = MathF.Min(1f, (FamilyConfidence(image, ColorFamily.Orange) * 0.75f) + (FamilyConfidence(image, ColorFamily.Red) * 0.35f));
        var centerWarm = RegionConfidence(image, ImageAnalysisRegion.Center, region => (FamilyConfidence(region, ColorFamily.Orange) + (FamilyConfidence(region, ColorFamily.Red) * 0.45f)) * MathF.Min(1f, region.SmoothTendency + 0.25f));
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.SkinProtection,
            "Protect likely character skin",
            image.AverageLuminance is > 0.18f and < 0.82f ? MathF.Max(skinLike, centerWarm) : 0f,
            "Material SkinProtection, grade warmth restraint",
            "The screenshot contains warm moderate-luminance color that may be skin.");

        var neon = MathF.Min(1f, FamilyConfidence(image, ColorFamily.Cyan) + FamilyConfidence(image, ColorFamily.Purple) + FamilyConfidence(image, ColorFamily.Magenta));
        AddOpinion(
            opinions,
            ImageSceneOpinionKeys.NeonAether,
            "Likely neon or aether color",
            image.AverageSaturation > 0.30f ? neon * 0.75f : 0f,
            "SceneIntent.MagicGlow/NeonGlow, Material CrystalAether/NeonGlass",
            "The screenshot contains confident cyan, purple, or magenta color.");

        return opinions
            .Where(opinion => opinion.Confidence > 0.01f)
            .OrderByDescending(opinion => opinion.Confidence)
            .ToArray();
    }

    private static void AddOpinion(List<ImageSceneOpinion> opinions, string key, string label, float confidence, string target, string reason)
    {
        confidence = Clamp01(confidence);
        if (confidence <= 0.01f)
        {
            return;
        }

        opinions.Add(new ImageSceneOpinion(key, label, confidence, target, reason));
    }

    private static float RegionConfidence(ImageAnalysisResult image, ImageAnalysisRegion region, Func<ImageRegionStats, float> score)
    {
        return image.Regions.TryGetValue(region, out var stats)
            ? Clamp01(score(stats))
            : 0f;
    }

    private static float FamilyConfidence(ImageAnalysisResult image, ColorFamily family)
    {
        return image.ColorFamilies.TryGetValue(family, out var stats) ? stats.Confidence : 0f;
    }

    private static float FamilyConfidence(ImageRegionStats stats, ColorFamily family)
    {
        return stats.ColorFamilies.TryGetValue(family, out var familyStats) ? familyStats.Confidence : 0f;
    }

    private static float Scale01(float value, float span)
    {
        if (span <= 0f)
        {
            return value > 0f ? 1f : 0f;
        }

        return Clamp01(value / span);
    }

    private static float Clamp01(float value)
    {
        return MathF.Min(1f, MathF.Max(0f, value));
    }

    private static IEnumerable<ImageAnalysisRegion> RegionsForSample(int x, int y, int width, int height)
    {
        var nx = width <= 1 ? 0.5f : x / (float)(width - 1);
        var ny = height <= 1 ? 0.5f : y / (float)(height - 1);
        if (ny < 0.333f)
        {
            yield return ImageAnalysisRegion.UpperThird;
        }
        else if (ny < 0.667f)
        {
            yield return ImageAnalysisRegion.MiddleThird;
        }
        else
        {
            yield return ImageAnalysisRegion.LowerThird;
        }

        if (nx is >= 0.28f and <= 0.72f && ny is >= 0.24f and <= 0.78f)
        {
            yield return ImageAnalysisRegion.Center;
        }
    }

    internal static IReadOnlyDictionary<ColorFamily, ColorFamilyStats> AnalyzeColorFamilies(IReadOnlyList<WeightedColorSample> samples, float totalWeight)
    {
        if (samples.Count == 0 || totalWeight <= 0f)
        {
            return ColorFamilyStats.EmptyMap;
        }

        var buckets = Enum.GetValues<ColorFamily>()
            .ToDictionary(family => family, _ => new ColorFamilyAccumulator());

        foreach (var sample in samples)
        {
            if (sample.Saturation < 0.08f)
            {
                continue;
            }

            var family = GetColorFamily(sample.Hue);
            buckets[family].Add(sample);
        }

        return buckets.ToDictionary(pair => pair.Key, pair => pair.Value.ToStats(pair.Key, totalWeight));
    }

    private static TonalColorBias AverageTonalColor(IEnumerable<WeightedColorSample> samples)
    {
        double weightSum = 0;
        double chromaWeightSum = 0;
        double hueX = 0;
        double hueY = 0;
        double saturationSum = 0;
        double warmthSum = 0;
        double tintSum = 0;

        foreach (var sample in samples)
        {
            weightSum += sample.Weight;
            saturationSum += sample.Saturation * sample.Weight;
            warmthSum += sample.Warmth * sample.Weight;
            tintSum += sample.Tint * sample.Weight;

            var chromaWeight = sample.Weight * Math.Max(0.05f, sample.Saturation);
            var angle = sample.Hue * MathF.Tau;
            hueX += Math.Cos(angle) * chromaWeight;
            hueY += Math.Sin(angle) * chromaWeight;
            chromaWeightSum += chromaWeight;
        }

        if (weightSum <= 0)
        {
            return TonalColorBias.Empty;
        }

        var hue = 0f;
        if (chromaWeightSum > 0)
        {
            var angle = Math.Atan2(hueY, hueX);
            if (angle < 0)
            {
                angle += MathF.Tau;
            }

            hue = (float)(angle / MathF.Tau);
        }

        return new TonalColorBias(
            hue,
            (float)(saturationSum / weightSum),
            (float)(warmthSum / weightSum),
            (float)(tintSum / weightSum));
    }

    private static float GetHue01(float r, float g, float b, float max, float min)
    {
        var delta = max - min;
        if (delta <= 0.0001f)
        {
            return 0f;
        }

        var hue = max == r
            ? ((g - b) / delta) % 6f
            : max == g
                ? ((b - r) / delta) + 2f
                : ((r - g) / delta) + 4f;

        hue /= 6f;
        if (hue < 0f)
        {
            hue += 1f;
        }

        return hue;
    }

    private static ColorFamily GetColorFamily(float hue)
    {
        var bestFamily = ColorFamily.Red;
        var bestDistance = float.MaxValue;

        foreach (var family in Enum.GetValues<ColorFamily>())
        {
            var distance = HueDistance(hue, ColorFamilyStats.GetFamilyHueCenter(family));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestFamily = family;
            }
        }

        return bestFamily;
    }

    private static float HueDistance(float left, float right)
    {
        var delta = Math.Abs(left - right);
        return delta > 0.5f ? 1f - delta : delta;
    }

    private static float GetSampleWeight(int x, int y, int width, int height, ImageSamplingMode samplingMode)
    {
        var nx = width <= 1 ? 0.5f : x / (float)(width - 1);
        var ny = height <= 1 ? 0.5f : y / (float)(height - 1);

        if (samplingMode == ImageSamplingMode.IgnoreBottomUi && ny > 0.80f)
        {
            return 0f;
        }

        if (samplingMode == ImageSamplingMode.LetterboxSafe && (ny < 0.08f || ny > 0.92f))
        {
            return 0f;
        }

        if (samplingMode == ImageSamplingMode.GposeClean && (ny < 0.06f || ny > 0.94f || nx < 0.03f || nx > 0.97f))
        {
            return 0f;
        }

        if (samplingMode == ImageSamplingMode.FullImage)
        {
            return 1f;
        }

        var dx = nx - 0.5f;
        var dy = ny - 0.45f;
        return MathF.Max(0.15f, 1f - ((dx * dx + dy * dy) * 3.0f));
    }
}

internal readonly record struct WeightedLuminanceSample(float Luminance, float Weight);

internal readonly record struct WeightedColorSample(float Luminance, float Weight, float Hue, float Saturation, float Warmth, float Tint);

internal sealed class ColorFamilyAccumulator
{
    private double weightSum;
    private double hueWeightSum;
    private double hueX;
    private double hueY;
    private double saturationSum;
    private double luminanceSum;

    public void Add(WeightedColorSample sample)
    {
        var chromaWeight = sample.Weight * Math.Max(0.05f, sample.Saturation);
        var angle = sample.Hue * MathF.Tau;

        weightSum += sample.Weight;
        hueWeightSum += chromaWeight;
        hueX += Math.Cos(angle) * chromaWeight;
        hueY += Math.Sin(angle) * chromaWeight;
        saturationSum += sample.Saturation * sample.Weight;
        luminanceSum += sample.Luminance * sample.Weight;
    }

    public ColorFamilyStats ToStats(ColorFamily family, float totalWeight)
    {
        if (weightSum <= 0 || totalWeight <= 0f)
        {
            return ColorFamilyStats.Empty(family);
        }

        var hue = ColorFamilyStats.GetFamilyHueCenter(family);
        if (hueWeightSum > 0)
        {
            var angle = Math.Atan2(hueY, hueX);
            if (angle < 0)
            {
                angle += MathF.Tau;
            }

            hue = (float)(angle / MathF.Tau);
        }

        var saturation = (float)(saturationSum / weightSum);
        var coverage = (float)(weightSum / totalWeight);
        var confidence = MathF.Min(1f, coverage * 8f) * MathF.Min(1f, saturation / 0.35f);

        return new ColorFamilyStats(
            family,
            hue,
            saturation,
            (float)(luminanceSum / weightSum),
            coverage,
            confidence);
    }
}

internal sealed class RegionAccumulator
{
    private readonly List<WeightedLuminanceSample> luminanceSamples = [];
    private readonly List<WeightedColorSample> colorSamples = [];
    private double weightSum;
    private double luminanceSum;
    private double luminanceSquaredSum;
    private double saturationSum;
    private double brightWeight;
    private double darkWeight;

    public void Add(WeightedColorSample sample)
    {
        weightSum += sample.Weight;
        luminanceSum += sample.Luminance * sample.Weight;
        luminanceSquaredSum += sample.Luminance * sample.Luminance * sample.Weight;
        saturationSum += sample.Saturation * sample.Weight;
        if (sample.Luminance > 0.72f)
        {
            brightWeight += sample.Weight;
        }
        else if (sample.Luminance < 0.18f)
        {
            darkWeight += sample.Weight;
        }

        luminanceSamples.Add(new WeightedLuminanceSample(sample.Luminance, sample.Weight));
        colorSamples.Add(sample);
    }

    public ImageRegionStats ToStats(ImageAnalysisRegion region)
    {
        if (weightSum <= 0)
        {
            return ImageRegionStats.Empty(region);
        }

        var averageLuminance = (float)(luminanceSum / weightSum);
        var variance = Math.Max(0, (luminanceSquaredSum / weightSum) - (averageLuminance * averageLuminance));
        var contrast = (float)Math.Sqrt(variance);
        var averageSaturation = (float)(saturationSum / weightSum);
        return new ImageRegionStats(
            region,
            averageLuminance,
            contrast,
            averageSaturation,
            (float)(brightWeight / weightSum),
            (float)(darkWeight / weightSum),
            MathF.Max(0f, 1f - (contrast / 0.22f)),
            ImageAnalysisService.AnalyzeColorFamilies(colorSamples, (float)weightSum));
    }
}

internal readonly record struct LuminancePercentiles(float P05, float P25, float P50, float P75, float P95)
{
    public static LuminancePercentiles From(IReadOnlyList<WeightedLuminanceSample> samples, float weightSum)
    {
        if (samples.Count == 0 || weightSum <= 0f)
        {
            return default;
        }

        var sorted = samples.OrderBy(sample => sample.Luminance).ToArray();
        return new LuminancePercentiles(
            Percentile(sorted, weightSum, 0.05f),
            Percentile(sorted, weightSum, 0.25f),
            Percentile(sorted, weightSum, 0.50f),
            Percentile(sorted, weightSum, 0.75f),
            Percentile(sorted, weightSum, 0.95f));
    }

    private static float Percentile(IReadOnlyList<WeightedLuminanceSample> sorted, float weightSum, float percentile)
    {
        var target = weightSum * percentile;
        var cumulative = 0f;
        foreach (var sample in sorted)
        {
            cumulative += sample.Weight;
            if (cumulative >= target)
            {
                return sample.Luminance;
            }
        }

        return sorted[^1].Luminance;
    }
}
