using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dalashade;

public sealed record ImageAnalysisResult(
    bool Available,
    string SourcePath,
    DateTimeOffset SourceTimestamp,
    float AverageLuminance,
    float Contrast,
    float AverageSaturation,
    float ShadowClipping,
    float HighlightClipping)
{
    public static ImageAnalysisResult Empty { get; } = new(false, string.Empty, DateTimeOffset.MinValue, 0f, 0f, 0f, 0f, 0f);

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
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

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
            var latest = FindLatestScreenshot(configuration.ScreenshotFolderPath);
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

            Current = Analyze(latest.FullName, latest.LastWriteTimeUtc);
            lastSourcePath = latest.FullName;
            lastSourceWriteTime = latest.LastWriteTimeUtc;
            LastMessage = $"Analyzed {latest.Name}: {Current.ProfileBucket}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or ExternalException or OutOfMemoryException)
        {
            Current = ImageAnalysisResult.Empty;
            LastMessage = $"Image analysis skipped this file: {ex.Message}";
        }
    }

    private static FileInfo? FindLatestScreenshot(string folderPath)
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

    private static ImageAnalysisResult Analyze(string imagePath, DateTime sourceWriteTimeUtc)
    {
        using var stream = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.Position = 0;

        using var bitmap = new Bitmap(memory);

        var stepX = Math.Max(1, bitmap.Width / 160);
        var stepY = Math.Max(1, bitmap.Height / 90);
        var count = 0;
        double luminanceSum = 0;
        double luminanceSquaredSum = 0;
        double saturationSum = 0;
        var shadowCount = 0;
        var highlightCount = 0;

        for (var y = 0; y < bitmap.Height; y += stepY)
        {
            for (var x = 0; x < bitmap.Width; x += stepX)
            {
                var pixel = bitmap.GetPixel(x, y);
                var r = pixel.R / 255f;
                var g = pixel.G / 255f;
                var b = pixel.B / 255f;
                var luminance = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
                var max = Math.Max(r, Math.Max(g, b));
                var min = Math.Min(r, Math.Min(g, b));
                var saturation = max <= 0.0001f ? 0f : (max - min) / max;

                luminanceSum += luminance;
                luminanceSquaredSum += luminance * luminance;
                saturationSum += saturation;
                count++;

                if (luminance < 0.035f)
                {
                    shadowCount++;
                }
                else if (luminance > 0.965f)
                {
                    highlightCount++;
                }
            }
        }

        var averageLuminance = (float)(luminanceSum / count);
        var variance = Math.Max(0, (luminanceSquaredSum / count) - (averageLuminance * averageLuminance));
        var contrast = (float)Math.Sqrt(variance);
        var averageSaturation = (float)(saturationSum / count);

        return new ImageAnalysisResult(
            true,
            imagePath,
            new DateTimeOffset(sourceWriteTimeUtc, TimeSpan.Zero),
            averageLuminance,
            contrast,
            averageSaturation,
            shadowCount / (float)count,
            highlightCount / (float)count);
    }
}
