using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalashade;

public sealed record ShaderFileLocationResult(
    string FileName,
    string? FullPath,
    IReadOnlyList<string> SearchPaths,
    string Message)
{
    public bool Found => !string.IsNullOrWhiteSpace(FullPath);
}

public static class ShaderFileLocator
{
    public static ShaderFileLocationResult Find(Configuration configuration, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new ShaderFileLocationResult(fileName, null, Array.Empty<string>(), "No shader file name supplied.");
        }

        var searchPaths = FindReShadeShaderPaths(configuration)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var root in searchPaths)
        {
            var candidate = TryGetFullPath(Path.Combine(root, fileName));
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                return new ShaderFileLocationResult(fileName, candidate, searchPaths, $"Found in {root}.");
            }
        }

        return new ShaderFileLocationResult(
            fileName,
            null,
            searchPaths,
            searchPaths.Length == 0
                ? "No ReShade shader search paths could be inferred."
                : $"Not found in {searchPaths.Length} inferred ReShade shader search path(s).");
    }

    public static IReadOnlyList<string> FindReShadeShaderPaths(Configuration configuration)
    {
        var paths = new List<string>();
        var reShadeIniPath = FindReShadeIni(configuration);
        if (!string.IsNullOrWhiteSpace(reShadeIniPath) && File.Exists(reShadeIniPath))
        {
            var iniDirectory = Path.GetDirectoryName(reShadeIniPath) ?? string.Empty;
            foreach (var line in File.ReadLines(reShadeIniPath))
            {
                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                if (!key.Contains("EffectSearchPaths", StringComparison.OrdinalIgnoreCase)
                    && !key.Contains("EffectSearchPath", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var rawPath in line[(separatorIndex + 1)..].Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var normalized = rawPath.Trim('"');
                    if (string.IsNullOrWhiteSpace(normalized))
                    {
                        continue;
                    }

                    AddIfDirectory(paths, Path.IsPathRooted(normalized) ? normalized : Path.Combine(iniDirectory, normalized));
                }
            }

            AddIfDirectory(paths, Path.Combine(iniDirectory, "reshade-shaders", "Shaders"));
        }

        foreach (var presetPath in new[] { configuration.GeneratedPresetPath, configuration.BasePresetPath })
        {
            var fullPresetPath = TryGetFullPath(presetPath);
            if (string.IsNullOrWhiteSpace(fullPresetPath))
            {
                continue;
            }

            var directory = File.Exists(fullPresetPath) ? Path.GetDirectoryName(fullPresetPath) : Path.GetDirectoryName(fullPresetPath);
            while (!string.IsNullOrWhiteSpace(directory))
            {
                AddIfDirectory(paths, Path.Combine(directory, "reshade-shaders", "Shaders"));
                directory = Directory.GetParent(directory)?.FullName;
            }
        }

        return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string? FindReShadeIni(Configuration configuration)
    {
        var configuredReShadeIni = TryGetFullPath(configuration.ReShadeIniPath);
        if (!string.IsNullOrWhiteSpace(configuredReShadeIni) && File.Exists(configuredReShadeIni))
        {
            return configuredReShadeIni;
        }

        foreach (var candidate in new[] { configuration.GeneratedPresetPath, configuration.BasePresetPath })
        {
            var fullCandidate = TryGetFullPath(candidate);
            if (string.IsNullOrWhiteSpace(fullCandidate))
            {
                continue;
            }

            var directory = File.Exists(fullCandidate) ? Path.GetDirectoryName(fullCandidate) : Path.GetDirectoryName(fullCandidate);
            while (!string.IsNullOrWhiteSpace(directory))
            {
                var reShadeIniPath = Path.Combine(directory, "ReShade.ini");
                if (File.Exists(reShadeIniPath))
                {
                    return reShadeIniPath;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }
        }

        return null;
    }

    private static void AddIfDirectory(List<string> paths, string? path)
    {
        var fullPath = TryGetFullPath(path);
        if (!string.IsNullOrWhiteSpace(fullPath) && Directory.Exists(fullPath))
        {
            paths.Add(fullPath);
        }
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
