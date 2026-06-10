using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dalashade;

public sealed class MasterStyleService
{
    private readonly Dictionary<string, CachedMasterImage> cache = new(StringComparer.OrdinalIgnoreCase);
    private string lastFolderPath = string.Empty;
    private bool lastIncludeSubfolders;
    private int lastMaxImages;
    private MasterStyleMode lastMode;
    private string lastCurrentMetricsKey = string.Empty;
    private DateTime lastNewestWriteTime = DateTime.MinValue;

    public ImageAnalysisResult Current { get; private set; } = ImageAnalysisResult.Empty;
    public string LastMessage { get; private set; } = "Master style has not been analyzed yet.";

    public void Refresh(Configuration configuration, ImageAnalysisResult currentScene, bool force = false)
    {
        if (!configuration.MatchMasterPresetStyle)
        {
            Current = ImageAnalysisResult.Empty;
            LastMessage = "Master style matching is disabled.";
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.MasterPresetFolderPath))
        {
            Current = ImageAnalysisResult.Empty;
            LastMessage = "Master preset folder is empty.";
            return;
        }

        try
        {
            var directory = new DirectoryInfo(configuration.MasterPresetFolderPath);
            if (!directory.Exists)
            {
                Current = ImageAnalysisResult.Empty;
                LastMessage = "Master preset folder was not found.";
                return;
            }

            var searchOption = configuration.MasterPresetIncludeSubfolders
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var images = directory.EnumerateFiles("*.*", searchOption)
                .Where(file => ImageAnalysisService.SupportedExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(Math.Max(1, configuration.MasterPresetMaxImages))
                .ToArray();

            if (images.Length == 0)
            {
                Current = ImageAnalysisResult.Empty;
                LastMessage = "No master style images found.";
                return;
            }

            var newestWriteTime = images.Max(file => file.LastWriteTimeUtc);
            if (!force
                && lastFolderPath == directory.FullName
                && lastIncludeSubfolders == configuration.MasterPresetIncludeSubfolders
                && lastMaxImages == configuration.MasterPresetMaxImages
                && lastMode == configuration.MasterStyleMode
                && lastCurrentMetricsKey == currentScene.MetricsKey
                && lastNewestWriteTime == newestWriteTime)
            {
                return;
            }

            PruneCache(images, configuration.ImageSamplingMode);

            var analyzed = images
                .Select(file => TryAnalyze(file, configuration.ImageSamplingMode, cache))
                .Where(result => result.Available)
                .ToArray();

            if (analyzed.Length == 0)
            {
                Current = ImageAnalysisResult.Empty;
                LastMessage = "Master style images could not be analyzed.";
                return;
            }

            Current = SelectMasterStyle(analyzed, configuration, currentScene, directory.FullName, newestWriteTime);
            LastMessage = $"Master style: {analyzed.Length} image(s), {configuration.MasterStyleMode}, {Current.ProfileBucket}, {Current.MetricsKey}.";

            lastFolderPath = directory.FullName;
            lastIncludeSubfolders = configuration.MasterPresetIncludeSubfolders;
            lastMaxImages = configuration.MasterPresetMaxImages;
            lastMode = configuration.MasterStyleMode;
            lastCurrentMetricsKey = currentScene.MetricsKey;
            lastNewestWriteTime = newestWriteTime;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or ExternalException)
        {
            Current = ImageAnalysisResult.Empty;
            LastMessage = $"Master style skipped: {ex.Message}";
        }
    }

    private static ImageAnalysisResult TryAnalyze(FileInfo file, ImageSamplingMode samplingMode, Dictionary<string, CachedMasterImage> cache)
    {
        try
        {
            var cacheKey = $"{samplingMode}:{file.FullName}";
            if (cache.TryGetValue(cacheKey, out var cached)
                && cached.LastWriteTimeUtc == file.LastWriteTimeUtc
                && cached.Length == file.Length)
            {
                return cached.Result;
            }

            var result = ImageAnalysisService.Analyze(file.FullName, file.LastWriteTimeUtc, samplingMode);
            cache[cacheKey] = new CachedMasterImage(file.LastWriteTimeUtc, file.Length, result);
            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or ExternalException or OutOfMemoryException)
        {
            return ImageAnalysisResult.Empty;
        }
    }

    private void PruneCache(FileInfo[] currentImages, ImageSamplingMode samplingMode)
    {
        var liveKeys = currentImages
            .Select(file => $"{samplingMode}:{file.FullName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var key in cache.Keys.Where(key => key.StartsWith($"{samplingMode}:", StringComparison.OrdinalIgnoreCase) && !liveKeys.Contains(key)).ToArray())
        {
            cache.Remove(key);
        }
    }

    private static ImageAnalysisResult SelectMasterStyle(ImageAnalysisResult[] images, Configuration configuration, ImageAnalysisResult currentScene, string folderPath, DateTime newestWriteTime)
    {
        return configuration.MasterStyleMode switch
        {
            MasterStyleMode.NewestImageOnly => images.OrderByDescending(image => image.SourceTimestamp).First(),
            MasterStyleMode.MedianFolder => Median(images, folderPath, newestWriteTime),
            MasterStyleMode.ClosestToCurrentScene when currentScene.Available => images.OrderBy(image => Distance(image, currentScene)).First(),
            _ => Average(images, folderPath, newestWriteTime)
        };
    }

    private static ImageAnalysisResult Average(ImageAnalysisResult[] images, string folderPath, DateTime newestWriteTime)
    {
        return new ImageAnalysisResult(
            true,
            folderPath,
            new DateTimeOffset(newestWriteTime, TimeSpan.Zero),
            images.Average(image => image.AverageLuminance),
            images.Average(image => image.Contrast),
            images.Average(image => image.AverageSaturation),
            images.Average(image => image.ShadowClipping),
            images.Average(image => image.HighlightClipping),
            images.Average(image => image.Warmth),
            images.Average(image => image.GreenBias),
            images.Average(image => image.LuminanceP05),
            images.Average(image => image.LuminanceP25),
            images.Average(image => image.LuminanceP50),
            images.Average(image => image.LuminanceP75),
            images.Average(image => image.LuminanceP95),
            AverageColor(images.Select(image => image.ShadowColor)),
            AverageColor(images.Select(image => image.MidtoneColor)),
            AverageColor(images.Select(image => image.HighlightColor)),
            AverageFamilies(images));
    }

    private static ImageAnalysisResult Median(ImageAnalysisResult[] images, string folderPath, DateTime newestWriteTime)
    {
        return new ImageAnalysisResult(
            true,
            folderPath,
            new DateTimeOffset(newestWriteTime, TimeSpan.Zero),
            MedianValue(images.Select(image => image.AverageLuminance)),
            MedianValue(images.Select(image => image.Contrast)),
            MedianValue(images.Select(image => image.AverageSaturation)),
            MedianValue(images.Select(image => image.ShadowClipping)),
            MedianValue(images.Select(image => image.HighlightClipping)),
            MedianValue(images.Select(image => image.Warmth)),
            MedianValue(images.Select(image => image.GreenBias)),
            MedianValue(images.Select(image => image.LuminanceP05)),
            MedianValue(images.Select(image => image.LuminanceP25)),
            MedianValue(images.Select(image => image.LuminanceP50)),
            MedianValue(images.Select(image => image.LuminanceP75)),
            MedianValue(images.Select(image => image.LuminanceP95)),
            MedianColor(images.Select(image => image.ShadowColor)),
            MedianColor(images.Select(image => image.MidtoneColor)),
            MedianColor(images.Select(image => image.HighlightColor)),
            MedianFamilies(images));
    }

    private static IReadOnlyDictionary<ColorFamily, ColorFamilyStats> AverageFamilies(ImageAnalysisResult[] images)
    {
        return Enum.GetValues<ColorFamily>().ToDictionary(
            family => family,
            family => AverageFamily(images.Select(image => image.ColorFamilies.TryGetValue(family, out var stats) ? stats : ColorFamilyStats.Empty(family)), family));
    }

    private static IReadOnlyDictionary<ColorFamily, ColorFamilyStats> MedianFamilies(ImageAnalysisResult[] images)
    {
        return Enum.GetValues<ColorFamily>().ToDictionary(
            family => family,
            family =>
            {
                var stats = images.Select(image => image.ColorFamilies.TryGetValue(family, out var value) ? value : ColorFamilyStats.Empty(family)).ToArray();
                return new ColorFamilyStats(
                    family,
                    MedianValue(stats.Select(value => value.Hue)),
                    MedianValue(stats.Select(value => value.Saturation)),
                    MedianValue(stats.Select(value => value.Luminance)),
                    MedianValue(stats.Select(value => value.Coverage)),
                    MedianValue(stats.Select(value => value.Confidence)));
            });
    }

    private static ColorFamilyStats AverageFamily(IEnumerable<ColorFamilyStats> stats, ColorFamily family)
    {
        double hueX = 0;
        double hueY = 0;
        double hueWeight = 0;
        double saturationSum = 0;
        double luminanceSum = 0;
        double coverageSum = 0;
        double confidenceSum = 0;
        var count = 0;

        foreach (var value in stats)
        {
            var chromaWeight = Math.Max(0.05f, value.Saturation) * Math.Max(0.05f, value.Confidence);
            var angle = value.Hue * MathF.Tau;
            hueX += Math.Cos(angle) * chromaWeight;
            hueY += Math.Sin(angle) * chromaWeight;
            hueWeight += chromaWeight;
            saturationSum += value.Saturation;
            luminanceSum += value.Luminance;
            coverageSum += value.Coverage;
            confidenceSum += value.Confidence;
            count++;
        }

        if (count == 0)
        {
            return ColorFamilyStats.Empty(family);
        }

        var hue = ColorFamilyStats.GetFamilyHueCenter(family);
        if (hueWeight > 0)
        {
            var angle = Math.Atan2(hueY, hueX);
            if (angle < 0)
            {
                angle += MathF.Tau;
            }

            hue = (float)(angle / MathF.Tau);
        }

        return new ColorFamilyStats(
            family,
            hue,
            (float)(saturationSum / count),
            (float)(luminanceSum / count),
            (float)(coverageSum / count),
            (float)(confidenceSum / count));
    }

    private static TonalColorBias AverageColor(IEnumerable<TonalColorBias> colors)
    {
        double hueX = 0;
        double hueY = 0;
        double hueWeight = 0;
        double saturationSum = 0;
        double warmthSum = 0;
        double tintSum = 0;
        var count = 0;

        foreach (var color in colors)
        {
            var chromaWeight = Math.Max(0.05f, color.Saturation);
            var angle = color.Hue * MathF.Tau;
            hueX += Math.Cos(angle) * chromaWeight;
            hueY += Math.Sin(angle) * chromaWeight;
            hueWeight += chromaWeight;
            saturationSum += color.Saturation;
            warmthSum += color.Warmth;
            tintSum += color.Tint;
            count++;
        }

        if (count == 0)
        {
            return TonalColorBias.Empty;
        }

        var hue = 0f;
        if (hueWeight > 0)
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
            (float)(saturationSum / count),
            (float)(warmthSum / count),
            (float)(tintSum / count));
    }

    private static TonalColorBias MedianColor(IEnumerable<TonalColorBias> colors)
    {
        var array = colors.ToArray();
        if (array.Length == 0)
        {
            return TonalColorBias.Empty;
        }

        return new TonalColorBias(
            MedianValue(array.Select(color => color.Hue)),
            MedianValue(array.Select(color => color.Saturation)),
            MedianValue(array.Select(color => color.Warmth)),
            MedianValue(array.Select(color => color.Tint)));
    }

    private static float MedianValue(IEnumerable<float> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        if (sorted.Length == 0)
        {
            return 0f;
        }

        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) * 0.5f;
    }

    private static float Distance(ImageAnalysisResult left, ImageAnalysisResult right)
    {
        return Squared(left.AverageLuminance - right.AverageLuminance) * 2.0f
               + Squared(left.Contrast - right.Contrast) * 1.4f
               + Squared(left.AverageSaturation - right.AverageSaturation) * 1.2f
               + Squared(left.ShadowClipping - right.ShadowClipping)
               + Squared(left.HighlightClipping - right.HighlightClipping)
               + Squared(left.ShadowFloor - right.ShadowFloor)
               + Squared(left.MidtoneLevel - right.MidtoneLevel)
               + Squared(left.HighlightCeiling - right.HighlightCeiling)
               + Squared(left.ContrastSpread - right.ContrastSpread) * 1.2f
               + Squared(left.Warmth - right.Warmth) * 0.8f
               + Squared(left.GreenBias - right.GreenBias) * 0.8f
               + Squared(HueDistance(left.ShadowColor.Hue, right.ShadowColor.Hue)) * 0.25f
               + Squared(HueDistance(left.MidtoneColor.Hue, right.MidtoneColor.Hue)) * 0.35f
               + Squared(HueDistance(left.HighlightColor.Hue, right.HighlightColor.Hue)) * 0.25f
               + Squared(left.ShadowColor.Saturation - right.ShadowColor.Saturation) * 0.35f
               + Squared(left.MidtoneColor.Saturation - right.MidtoneColor.Saturation) * 0.45f
               + Squared(left.HighlightColor.Saturation - right.HighlightColor.Saturation) * 0.35f
               + ColorFamilyDistance(left, right) * 0.50f;
    }

    private static float ColorFamilyDistance(ImageAnalysisResult left, ImageAnalysisResult right)
    {
        var distance = 0f;
        foreach (var family in Enum.GetValues<ColorFamily>())
        {
            var leftStats = left.ColorFamilies.TryGetValue(family, out var leftValue) ? leftValue : ColorFamilyStats.Empty(family);
            var rightStats = right.ColorFamilies.TryGetValue(family, out var rightValue) ? rightValue : ColorFamilyStats.Empty(family);
            var confidence = MathF.Min(leftStats.Confidence, rightStats.Confidence);

            distance += confidence
                        * (Squared(HueDistance(leftStats.Hue, rightStats.Hue)) * 0.8f
                           + Squared(leftStats.Saturation - rightStats.Saturation)
                           + Squared(leftStats.Luminance - rightStats.Luminance) * 0.6f);
        }

        return distance;
    }

    private static float HueDistance(float left, float right)
    {
        var delta = Math.Abs(left - right);
        return delta > 0.5f ? 1f - delta : delta;
    }

    private static float Squared(float value) => value * value;
}

internal sealed record CachedMasterImage(DateTime LastWriteTimeUtc, long Length, ImageAnalysisResult Result);
