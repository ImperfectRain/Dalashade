using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace Dalashade;

public sealed record ReloadDiagnostics(
    bool ReShadeIniFound,
    string ReShadeIniPath,
    string KeyReloadValue,
    string ConfiguredReloadKey,
    bool HotkeySyncEnabled,
    bool PostMessageSucceeded,
    bool SendInputSucceeded)
{
    public static ReloadDiagnostics Empty(Configuration configuration) => new(
        false,
        string.Empty,
        "not found",
        Keybind.FromConfiguration(configuration).DisplayName,
        configuration.SyncReloadHotkeyToReShadeIni,
        false,
        false);
}

public sealed record ReloadResult(bool Success, string Message, ReloadDiagnostics Diagnostics)
{
    public static ReloadResult Skipped(string message) => new(false, message, new ReloadDiagnostics(false, string.Empty, "not found", "not configured", false, false, false));
    public static ReloadResult Skipped(string message, Configuration configuration) => new(false, message, ReloadDiagnostics.Empty(configuration));
}

public sealed class ReShadeController
{
    public ReloadResult ReloadAfterPresetWrite(Configuration configuration)
    {
        return Reload(configuration, true);
    }

    public ReloadResult TestReload(Configuration configuration)
    {
        return Reload(configuration, false);
    }

    public string? AutoDetectReShadeIni(Configuration configuration)
    {
        return TryFindReShadeIni(configuration, false);
    }

    private ReloadResult Reload(Configuration configuration, bool respectReloadEnabled)
    {
        if (!configuration.ReloadShadersAfterGeneration)
        {
            if (respectReloadEnabled)
            {
                return ReloadResult.Skipped("Shader reload is disabled.", configuration);
            }
        }

        var keybind = Keybind.FromConfiguration(configuration);
        if (!keybind.IsConfigured)
        {
            return ReloadResult.Skipped("Reload hotkey is not configured.", configuration);
        }

        var reshadeIniPath = TryFindReShadeIni(configuration);
        var liveIniKeybind = reshadeIniPath != null ? TryReadReloadHotkey(reshadeIniPath) : null;
        var liveIniValue = reshadeIniPath != null ? TryReadReloadHotkeyValue(reshadeIniPath) ?? "not found" : "not found";
        var syncMessage = configuration.SyncReloadHotkeyToReShadeIni
            ? "ReShade.ini was not found for hotkey sync."
            : "ReShade.ini hotkey sync is off.";

        if (configuration.SyncReloadHotkeyToReShadeIni && reshadeIniPath != null)
        {
            var syncResult = TrySyncReloadHotkey(reshadeIniPath, keybind);
            if (!syncResult.Success)
            {
                return syncResult;
            }

            syncMessage = $"Synced ReShade.ini to {keybind.DisplayName}.";
            liveIniKeybind = keybind;
            liveIniValue = keybind.ReShadeValue;
        }

        Thread.Sleep(250);
        var attempts = BuildReloadAttempts(liveIniKeybind, keybind);
        var posted = false;
        var injected = false;
        foreach (var attempt in attempts)
        {
            var sendResult = SendKey(attempt);
            posted |= sendResult.PostMessageSucceeded;
            injected |= sendResult.SendInputSucceeded;
            Thread.Sleep(120);
        }

        var diagnostics = new ReloadDiagnostics(
            reshadeIniPath != null,
            reshadeIniPath ?? string.Empty,
            liveIniValue,
            keybind.DisplayName,
            configuration.SyncReloadHotkeyToReShadeIni,
            posted,
            injected);
        var sentKeys = string.Join(", ", attempts.ConvertAll(attempt => attempt.DisplayName));
        var diagnosticMessage = FormatDiagnostics(diagnostics);
        return posted || injected
            ? new ReloadResult(true, $"Reload hotkey sent ({sentKeys}). {syncMessage} {diagnosticMessage}", diagnostics)
            : new ReloadResult(false, $"Could not send reload hotkey ({sentKeys}). {syncMessage} {diagnosticMessage}", diagnostics);
    }

    private static string? TryFindReShadeIni(Configuration configuration, bool includeConfiguredPath = true)
    {
        if (includeConfiguredPath
            && !string.IsNullOrWhiteSpace(configuration.ReShadeIniPath)
            && File.Exists(configuration.ReShadeIniPath))
        {
            return Path.GetFullPath(configuration.ReShadeIniPath);
        }

        var candidates = new[]
        {
            configuration.BasePresetPath,
            configuration.GeneratedPresetPath
        };

        foreach (var gameDirectory in EnumerateGameDirectories())
        {
            var reshadeIniPath = Path.Combine(gameDirectory, "ReShade.ini");
            if (File.Exists(reshadeIniPath))
            {
                return reshadeIniPath;
            }
        }

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var directory = File.Exists(candidate)
                ? Path.GetDirectoryName(Path.GetFullPath(candidate))
                : Directory.Exists(candidate)
                    ? Path.GetFullPath(candidate)
                    : Path.GetDirectoryName(Path.GetFullPath(candidate));

            while (!string.IsNullOrWhiteSpace(directory))
            {
                var reshadeIniPath = Path.Combine(directory, "ReShade.ini");
                if (File.Exists(reshadeIniPath))
                {
                    return reshadeIniPath;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateGameDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in EnumerateXivLauncherGameDirectories())
        {
            if (seen.Add(directory))
            {
                yield return directory;
            }
        }

        var processDirectory = TryGetCurrentProcessDirectory();
        if (!string.IsNullOrWhiteSpace(processDirectory) && seen.Add(processDirectory))
        {
            yield return processDirectory;
        }
    }

    private static IEnumerable<string> EnumerateXivLauncherGameDirectories()
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher",
            "launcherConfigV3.json");
        if (!File.Exists(configPath))
        {
            yield break;
        }

        string? gamePath = null;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            if (document.RootElement.TryGetProperty("GamePath", out var gamePathElement))
            {
                gamePath = gamePathElement.GetString();
            }
        }
        catch (JsonException)
        {
            yield break;
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(gamePath))
        {
            yield break;
        }

        var rootPath = Path.GetFullPath(gamePath);
        var gameDirectory = Path.Combine(rootPath, "game");
        if (Directory.Exists(gameDirectory))
        {
            yield return gameDirectory;
        }

        if (Directory.Exists(rootPath))
        {
            yield return rootPath;
        }
    }

    private static string? TryGetCurrentProcessDirectory()
    {
        try
        {
            var fileName = Process.GetCurrentProcess().MainModule?.FileName;
            return string.IsNullOrWhiteSpace(fileName) ? null : Path.GetDirectoryName(fileName);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static string? TryReadReloadHotkeyValue(string reshadeIniPath)
    {
        try
        {
            foreach (var line in File.ReadLines(reshadeIniPath))
            {
                if (line.StartsWith("KeyReload=", StringComparison.OrdinalIgnoreCase))
                {
                    return line["KeyReload=".Length..].Trim();
                }
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    private static Keybind? TryReadReloadHotkey(string reshadeIniPath)
    {
        try
        {
            foreach (var line in File.ReadLines(reshadeIniPath))
            {
                if (!line.StartsWith("KeyReload=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return TryParseReShadeKeybind(line["KeyReload=".Length..]);
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    private static Keybind? TryParseReShadeKeybind(string value)
    {
        var parts = value.Split(',');
        if (parts.Length < 4 ||
            !int.TryParse(parts[0].Trim(), out var virtualKey) ||
            !int.TryParse(parts[1].Trim(), out var ctrl) ||
            !int.TryParse(parts[2].Trim(), out var shift) ||
            !int.TryParse(parts[3].Trim(), out var alt) ||
            virtualKey <= 0)
        {
            return null;
        }

        return new Keybind(virtualKey, ctrl != 0, shift != 0, alt != 0);
    }

    private static ReloadResult TrySyncReloadHotkey(string reshadeIniPath, Keybind keybind)
    {
        try
        {
            var lines = File.ReadAllLines(reshadeIniPath);
            var expected = $"KeyReload={keybind.ReShadeValue}";
            var replaced = false;

            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("KeyReload=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(lines[i], expected, StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = expected;
                    File.WriteAllLines(reshadeIniPath, lines);
                }

                replaced = true;
                break;
            }

            if (!replaced)
            {
                File.AppendAllText(reshadeIniPath, $"{Environment.NewLine}{expected}{Environment.NewLine}");
            }

            return new ReloadResult(true, $"ReShade reload hotkey synced to {keybind.DisplayName}.", new ReloadDiagnostics(true, reshadeIniPath, keybind.ReShadeValue, keybind.DisplayName, true, false, false));
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ReloadResult(false, $"Could not update ReShade.ini for reload: {ex.Message}", new ReloadDiagnostics(true, reshadeIniPath, "not found", keybind.DisplayName, true, false, false));
        }
        catch (IOException ex)
        {
            return new ReloadResult(false, $"Could not update ReShade.ini for reload: {ex.Message}", new ReloadDiagnostics(true, reshadeIniPath, "not found", keybind.DisplayName, true, false, false));
        }
    }

    private static ReloadSendResult SendKey(Keybind keybind)
    {
        var posted = PostWindowKey(keybind);
        Thread.Sleep(40);
        var injected = SendInputKey(keybind);

        return new ReloadSendResult(posted, injected);
    }

    private static bool SendInputKey(Keybind keybind)
    {
        var modifiers = new List<ushort>();
        if (keybind.Ctrl)
        {
            modifiers.Add(VkControl);
        }

        if (keybind.Shift)
        {
            modifiers.Add(VkShift);
        }

        if (keybind.Alt)
        {
            modifiers.Add(VkMenu);
        }

        var inputs = new List<INPUT>();
        foreach (var modifier in modifiers)
        {
            inputs.Add(CreateKeyInput(modifier, false));
        }

        inputs.Add(CreateKeyInput((ushort)keybind.VirtualKey, false));
        inputs.Add(CreateKeyInput((ushort)keybind.VirtualKey, true));

        for (var i = modifiers.Count - 1; i >= 0; i--)
        {
            inputs.Add(CreateKeyInput(modifiers[i], true));
        }

        return SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>()) == inputs.Count;
    }

    private static bool PostWindowKey(Keybind keybind)
    {
        var windowHandle = Process.GetCurrentProcess().MainWindowHandle;
        if (windowHandle == IntPtr.Zero)
        {
            windowHandle = GetForegroundWindow();
        }

        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        SetForegroundWindow(windowHandle);

        var modifiers = new List<int>();
        if (keybind.Ctrl)
        {
            modifiers.Add(VkControl);
        }

        if (keybind.Shift)
        {
            modifiers.Add(VkShift);
        }

        if (keybind.Alt)
        {
            modifiers.Add(VkMenu);
        }

        var posted = true;
        foreach (var modifier in modifiers)
        {
            posted &= PostKeyMessage(windowHandle, modifier, false);
        }

        posted &= PostKeyMessage(windowHandle, keybind.VirtualKey, false);
        posted &= PostKeyMessage(windowHandle, keybind.VirtualKey, true);

        for (var i = modifiers.Count - 1; i >= 0; i--)
        {
            posted &= PostKeyMessage(windowHandle, modifiers[i], true);
        }

        return posted;
    }

    private static bool PostKeyMessage(IntPtr windowHandle, int virtualKey, bool keyUp)
    {
        var scanCode = MapVirtualKey((uint)virtualKey, MapVkToVsc);
        var repeatCount = 1;
        var lParam = repeatCount | ((int)scanCode << 16);
        if (keyUp)
        {
            lParam |= 1 << 30;
            lParam |= 1 << 31;
        }

        return PostMessage(windowHandle, keyUp ? WmKeyUp : WmKeyDown, (IntPtr)virtualKey, (IntPtr)lParam);
    }

    private static INPUT CreateKeyInput(ushort virtualKey, bool keyUp)
    {
        var scanCode = MapVirtualKey(virtualKey, MapVkToVsc);
        return new INPUT
        {
            type = InputKeyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = (ushort)scanCode,
                    dwFlags = keyUp ? KeyEventKeyUp : 0
                }
            }
        };
    }

    private static List<Keybind> BuildReloadAttempts(Keybind? liveIniKeybind, Keybind configuredKeybind)
    {
        var attempts = new List<Keybind>();
        if (liveIniKeybind.HasValue && liveIniKeybind.Value.IsConfigured)
        {
            attempts.Add(liveIniKeybind.Value);
        }

        if (!attempts.Exists(existing => SameKeybind(existing, configuredKeybind)))
        {
            attempts.Add(configuredKeybind);
        }

        return attempts;
    }

    private static bool SameKeybind(Keybind left, Keybind right)
    {
        return left.VirtualKey == right.VirtualKey &&
               left.Ctrl == right.Ctrl &&
               left.Shift == right.Shift &&
               left.Alt == right.Alt;
    }

    private static string FormatDiagnostics(ReloadDiagnostics diagnostics)
    {
        var path = diagnostics.ReShadeIniFound ? diagnostics.ReShadeIniPath : "not found";
        return $"Diagnostics: ReShade.ini={path}; KeyReload={diagnostics.KeyReloadValue}; configured={diagnostics.ConfiguredReloadKey}; sync={(diagnostics.HotkeySyncEnabled ? "on" : "off")}; PostMessage={(diagnostics.PostMessageSucceeded ? "ok" : "failed")}; SendInput={(diagnostics.SendInputSucceeded ? "ok" : "failed")}.";
    }

    private sealed record ReloadSendResult(bool PostMessageSucceeded, bool SendInputSucceeded);

    private const uint InputKeyboard = 1;
    private const uint MapVkToVsc = 0;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const uint KeyEventKeyUp = 0x0002;
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
