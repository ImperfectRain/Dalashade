using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dalashade;

public sealed class MasterStyleService
{
    private string lastFolderPath = string.Empty;
    private bool lastIncludeSubfolders;
    private int lastMaxImages;
    private DateTime lastNewestWriteTime = DateTime.MinValue;

    public ImageAnalysisResult Current { get; private set; } = ImageAnalysisResult.Empty;
    public string LastMessage { get; private set; } = "Master style has not been analyzed yet.";

    public void Refresh(Configuration configuration, bool force = false)
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
                && lastNewestWriteTime == newestWriteTime)
            {
                return;
            }

            var analyzed = images
                .Select(file => TryAnalyze(file))
                .Where(result => result.Available)
                .ToArray();

            if (analyzed.Length == 0)
            {
                Current = ImageAnalysisResult.Empty;
                LastMessage = "Master style images could not be analyzed.";
                return;
            }

            Current = Average(analyzed, directory.FullName, newestWriteTime);
            LastMessage = $"Master style: {analyzed.Length} image(s), {Current.ProfileBucket}.";

            lastFolderPath = directory.FullName;
            lastIncludeSubfolders = configuration.MasterPresetIncludeSubfolders;
            lastMaxImages = configuration.MasterPresetMaxImages;
            lastNewestWriteTime = newestWriteTime;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or ExternalException)
        {
            Current = ImageAnalysisResult.Empty;
            LastMessage = $"Master style skipped: {ex.Message}";
        }
    }

    private static ImageAnalysisResult TryAnalyze(FileInfo file)
    {
        try
        {
            return ImageAnalysisService.Analyze(file.FullName, file.LastWriteTimeUtc);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or ExternalException or OutOfMemoryException)
        {
            return ImageAnalysisResult.Empty;
        }
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
}
