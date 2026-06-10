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
    IReadOnlyDictionary<ColorFamily, ColorFamilyStats> ColorFamilies)
{
    public static ImageAnalysisResult Empty { get; } = new(false, string.Empty, DateTimeOffset.MinValue, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, TonalColorBias.Empty, TonalColorBias.Empty, TonalColorBias.Empty, ColorFamilyStats.EmptyMap);

    public float ShadowFloor => LuminanceP05;
    public float MidtoneLevel => LuminanceP50;
    public float HighlightCeiling => LuminanceP95;
    public float ContrastSpread => MathF.Max(0f, LuminanceP75 - LuminanceP25);

    public string MetricsKey
    {
        get
        {
            if (!Available)
            {
                return "none";
            }

            return FormattableString.Invariant(
                $"l{AverageLuminance:0.00}:c{Contrast:0.00}:s{AverageSaturation:0.00}:sh{ShadowClipping:0.00}:hi{HighlightClipping:0.00}:p05{LuminanceP05:0.00}:p50{LuminanceP50:0.00}:p95{LuminanceP95:0.00}:w{Warmth:0.00}:g{GreenBias:0.00}:sc{ShadowColor.Hue:0.00}/{ShadowColor.Saturation:0.00}:mc{MidtoneColor.Hue:0.00}/{MidtoneColor.Saturation:0.00}:hc{HighlightColor.Hue:0.00}/{HighlightColor.Saturation:0.00}:cf{ColorFamilyMetricsKey(ColorFamilies)}");
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
            LastMessage = $"Analyzed {latest.Name}: {Current.ProfileBucket}, {Current.MetricsKey}.";
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
                colorSamples.Add(new WeightedColorSample(luminance, weight, GetHue01(r, g, b, max, min), saturation, warmth, tint));

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

        return new ImageAnalysisResult(
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
            colorFamilies);
    }

    private static IReadOnlyDictionary<ColorFamily, ColorFamilyStats> AnalyzeColorFamilies(IReadOnlyList<WeightedColorSample> samples, float totalWeight)
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
