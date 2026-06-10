using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalashade;

public sealed record BasePresetLibraryItem(string FileName, string FullPath, DateTime LastModified);

public sealed record BasePresetLibraryScan(bool Success, string Message, IReadOnlyList<BasePresetLibraryItem> Items)
{
    public static BasePresetLibraryScan Skipped(string message) => new(false, message, Array.Empty<BasePresetLibraryItem>());
}

public sealed class BasePresetLibrary
{
    public BasePresetLibraryScan Scan(string folderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return BasePresetLibraryScan.Skipped("Base preset folder is empty.");
            }

            Directory.CreateDirectory(folderPath);
            var directory = new DirectoryInfo(folderPath);
            var items = directory
                .EnumerateFiles("*.ini", SearchOption.TopDirectoryOnly)
                .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                .Select(file => new BasePresetLibraryItem(file.Name, file.FullName, file.LastWriteTime))
                .ToArray();

            var message = items.Length == 0
                ? "No .ini presets found. Place base presets in the Base folder."
                : $"Found {items.Length} base preset(s).";
            return new BasePresetLibraryScan(true, message, items);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return BasePresetLibraryScan.Skipped($"Base preset scan failed: {ex.Message}");
        }
    }
}
