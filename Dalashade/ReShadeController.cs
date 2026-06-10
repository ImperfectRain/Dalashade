using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Dalashade;

public sealed record ReloadResult(bool Success, string Message)
{
    public static ReloadResult Skipped(string message) => new(false, message);
}

public sealed class ReShadeController
{
    public ReloadResult ReloadAfterPresetWrite(Configuration configuration)
    {
        if (!configuration.ReloadShadersAfterGeneration)
        {
            return ReloadResult.Skipped("Shader reload is disabled.");
        }

        var keybind = Keybind.FromConfiguration(configuration);
        if (!keybind.IsConfigured)
        {
            return ReloadResult.Skipped("Reload hotkey is not configured.");
        }

        var reshadeIniPath = TryFindReShadeIni(configuration);
        var liveIniKeybind = reshadeIniPath != null ? TryReadReloadHotkey(reshadeIniPath) : null;
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
        }

        Thread.Sleep(250);
        var attempts = BuildReloadAttempts(liveIniKeybind, keybind);
        var sent = false;
        foreach (var attempt in attempts)
        {
            sent |= SendKey(attempt);
            Thread.Sleep(120);
        }

        var sentKeys = string.Join(", ", attempts.ConvertAll(attempt => attempt.DisplayName));
        return sent
            ? new ReloadResult(true, $"Reload hotkey sent ({sentKeys}). {syncMessage}")
            : ReloadResult.Skipped($"Could not send reload hotkey ({sentKeys}). {syncMessage}");
    }

    private static string? TryFindReShadeIni(Configuration configuration)
    {
        var candidates = new[]
        {
            configuration.BasePresetPath,
            configuration.GeneratedPresetPath
        };

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

            return new ReloadResult(true, $"ReShade reload hotkey synced to {keybind.DisplayName}.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ReloadResult.Skipped($"Could not update ReShade.ini for reload: {ex.Message}");
        }
        catch (IOException ex)
        {
            return ReloadResult.Skipped($"Could not update ReShade.ini for reload: {ex.Message}");
        }
    }

    private static bool SendKey(Keybind keybind)
    {
        var posted = PostWindowKey(keybind);
        Thread.Sleep(40);
        var injected = SendInputKey(keybind);

        return posted || injected;
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
