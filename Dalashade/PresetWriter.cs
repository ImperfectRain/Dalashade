using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalashade;

public sealed record ChangedShaderVariable(string Section, string Key, string OldValue, string NewValue, string ReasonCategory);

public sealed record PresetWriteResult(bool Success, string Message, int ChangedVariables, IReadOnlyList<ChangedShaderVariable> Changes)
{
    public static PresetWriteResult Skipped(string message) => new(false, message, 0, Array.Empty<ChangedShaderVariable>());
}

public sealed record ShaderSupportItem(string Section, string Key, bool Controllable, string ReasonCategory);

public sealed record ShaderSupportScan(bool Success, string Message, IReadOnlyList<ShaderSupportItem> Items)
{
    public static ShaderSupportScan Skipped(string message) => new(false, message, Array.Empty<ShaderSupportItem>());
}

public sealed class PresetWriter
{
    private readonly ShaderVariableMapper mapper = new();

    public PresetWriteResult WriteGeneratedPreset(Configuration configuration, VisualProfile profile)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.BasePresetPath))
            {
                return PresetWriteResult.Skipped("Base preset path is empty.");
            }

            if (string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath))
            {
                return PresetWriteResult.Skipped("Generated preset path is empty.");
            }

            var basePresetPath = Path.GetFullPath(configuration.BasePresetPath);
            var generatedPresetPath = Path.GetFullPath(configuration.GeneratedPresetPath);

            if (string.Equals(basePresetPath, generatedPresetPath, StringComparison.OrdinalIgnoreCase))
            {
                return PresetWriteResult.Skipped("Generated preset path must be different from the base preset path.");
            }

            if (!File.Exists(basePresetPath))
            {
                return PresetWriteResult.Skipped("Base preset was not found.");
            }

            var generatedDirectory = Path.GetDirectoryName(generatedPresetPath);
            if (string.IsNullOrWhiteSpace(generatedDirectory))
            {
                return PresetWriteResult.Skipped("Generated preset path is invalid.");
            }

            Directory.CreateDirectory(generatedDirectory);

            var adjustments = mapper.CreateAdjustments(profile, configuration);
            var lines = File.ReadAllLines(basePresetPath);
            var changes = new List<ChangedShaderVariable>();
            var currentSection = string.Empty;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (TryReadSection(line, out var section))
                {
                    currentSection = section;
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                if (!TryGetAdjustment(adjustments, configuration.ShaderMatchingMode, currentSection, key, out var adjust))
                {
                    continue;
                }

                var currentValue = line[(separatorIndex + 1)..];
                var adjustedValue = adjust.Apply(currentValue);
                if (adjustedValue == null)
                {
                    continue;
                }

                lines[i] = $"{line[..(separatorIndex + 1)]}{adjustedValue}";
                if (!string.Equals(currentValue, adjustedValue, StringComparison.Ordinal))
                {
                    changes.Add(new ChangedShaderVariable(currentSection, key, currentValue, adjustedValue, adjust.ReasonCategory));
                }
            }

            if (configuration.WriteBackups && File.Exists(generatedPresetPath))
            {
                var backupPath = CreateBackupPath(generatedPresetPath);
                File.Copy(generatedPresetPath, backupPath, false);
                PruneBackups(generatedPresetPath, configuration.MaxGeneratedPresetBackups);
            }

            var tempPath = $"{generatedPresetPath}.tmp";
            File.WriteAllLines(tempPath, lines);
            ReplaceFile(tempPath, generatedPresetPath);

            return new PresetWriteResult(true, $"Generated preset written with {changes.Count} supported variable change(s).", changes.Count, changes);
        }
        catch (UnauthorizedAccessException ex)
        {
            return PresetWriteResult.Skipped($"Preset write denied: {ex.Message} Choose a generated preset path in a writable folder, not the protected game/ReShade install folder.");
        }
        catch (IOException ex)
        {
            return PresetWriteResult.Skipped($"Preset write failed: {ex.Message}");
        }
    }

    public ShaderSupportScan ScanSupportedVariables(Configuration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.BasePresetPath))
            {
                return ShaderSupportScan.Skipped("Base preset path is empty.");
            }

            var basePresetPath = Path.GetFullPath(configuration.BasePresetPath);
            if (!File.Exists(basePresetPath))
            {
                return ShaderSupportScan.Skipped("Base preset was not found.");
            }

            var adjustments = mapper.CreateAdjustments(VisualProfile.Neutral, configuration);
            var lines = File.ReadAllLines(basePresetPath);
            var items = new List<ShaderSupportItem>();
            var seen = new HashSet<ShaderVariableKey>(ShaderVariableKeyComparer.Instance);
            var currentSection = string.Empty;

            foreach (var line in lines)
            {
                if (TryReadSection(line, out var section))
                {
                    currentSection = section;
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                if (!TryGetAdjustment(adjustments, configuration.ShaderMatchingMode, currentSection, key, out var adjust))
                {
                    continue;
                }

                var itemKey = new ShaderVariableKey(currentSection, key);
                if (!seen.Add(itemKey))
                {
                    continue;
                }

                items.Add(new ShaderSupportItem(currentSection, key, true, adjust.ReasonCategory));
            }

            var message = items.Count == 0
                ? "No controllable shader variables detected in the base preset."
                : $"Detected {items.Count} controllable shader variable(s).";
            return new ShaderSupportScan(true, message, items.OrderBy(item => item.Section).ThenBy(item => item.Key).ToArray());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return ShaderSupportScan.Skipped($"Shader support scan failed: {ex.Message}");
        }
    }

    private static bool TryGetAdjustment(IReadOnlyDictionary<ShaderVariableKey, ShaderAdjustment> adjustments, ShaderMatchingMode matchingMode, string section, string key, out ShaderAdjustment adjustment)
    {
        if (adjustments.TryGetValue(new ShaderVariableKey(section, key), out adjustment!))
        {
            return true;
        }

        if (matchingMode == ShaderMatchingMode.KnownFallbacks && adjustments.TryGetValue(new ShaderVariableKey(null, key), out adjustment!))
        {
            return true;
        }

        if (matchingMode == ShaderMatchingMode.LooseKeys)
        {
            var looseMatch = adjustments.FirstOrDefault(pair => string.Equals(pair.Key.Key, key, StringComparison.OrdinalIgnoreCase));
            if (looseMatch.Value != null)
            {
                adjustment = looseMatch.Value;
                return true;
            }
        }

        adjustment = null!;
        return false;
    }

    private static bool TryReadSection(string line, out string section)
    {
        var trimmed = line.Trim();
        if (trimmed.Length > 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            section = trimmed[1..^1];
            return true;
        }

        section = string.Empty;
        return false;
    }

    private static void ReplaceFile(string tempPath, string generatedPresetPath)
    {
        try
        {
            if (File.Exists(generatedPresetPath))
            {
                File.SetAttributes(generatedPresetPath, FileAttributes.Normal);
            }

            File.Move(tempPath, generatedPresetPath, true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static string CreateBackupPath(string generatedPresetPath)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff");
        var backupPath = $"{generatedPresetPath}.{timestamp}.bak";
        var suffix = 1;

        while (File.Exists(backupPath))
        {
            backupPath = $"{generatedPresetPath}.{timestamp}.{suffix}.bak";
            suffix++;
        }

        return backupPath;
    }

    private static void PruneBackups(string generatedPresetPath, int maxBackups)
    {
        if (maxBackups <= 0)
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(generatedPresetPath);
        var fileName = Path.GetFileName(generatedPresetPath);
        if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var directory = new DirectoryInfo(directoryPath);
        if (!directory.Exists)
        {
            return;
        }

        var backups = directory.EnumerateFiles($"{fileName}.*.bak", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(maxBackups)
            .ToArray();

        foreach (var backup in backups)
        {
            try
            {
                backup.Delete();
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
