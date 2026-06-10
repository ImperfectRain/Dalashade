using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Dalashade;

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
    float LuminanceP95)
{
    public static ImageAnalysisResult Empty { get; } = new(false, string.Empty, DateTimeOffset.MinValue, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

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
                $"l{AverageLuminance:0.00}:c{Contrast:0.00}:s{AverageSaturation:0.00}:sh{ShadowClipping:0.00}:hi{HighlightClipping:0.00}:p05{LuminanceP05:0.00}:p50{LuminanceP50:0.00}:p95{LuminanceP95:0.00}:w{Warmth:0.00}:g{GreenBias:0.00}");
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

                luminanceSum += luminance * weight;
                luminanceSquaredSum += luminance * luminance * weight;
                saturationSum += saturation * weight;
                warmthSum += (r - b) * weight;
                greenBiasSum += (g - ((r + b) * 0.5f)) * weight;
                weightSum += weight;
                luminanceSamples.Add(new WeightedLuminanceSample(luminance, weight));

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
            percentiles.P95);
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
