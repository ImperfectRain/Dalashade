using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dalashade;

public sealed class MasterStyleService
{
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

            var analyzed = images
                .Select(file => TryAnalyze(file, configuration.ImageSamplingMode))
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

    private static ImageAnalysisResult TryAnalyze(FileInfo file, ImageSamplingMode samplingMode)
    {
        try
        {
            return ImageAnalysisService.Analyze(file.FullName, file.LastWriteTimeUtc, samplingMode);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or ExternalException or OutOfMemoryException)
        {
            return ImageAnalysisResult.Empty;
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
            images.Average(image => image.GreenBias));
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
            MedianValue(images.Select(image => image.GreenBias)));
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
               + Squared(left.Warmth - right.Warmth) * 0.8f
               + Squared(left.GreenBias - right.GreenBias) * 0.8f;
    }

    private static float Squared(float value) => value * value;
}
