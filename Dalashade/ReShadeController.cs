using System;
using System.Collections.Generic;
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
        return SendKey(keybind)
            ? new ReloadResult(true, $"Reload hotkey sent ({keybind.DisplayName}). {syncMessage}")
            : ReloadResult.Skipped($"Could not send reload hotkey ({keybind.DisplayName}). {syncMessage}");
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

    private static INPUT CreateKeyInput(ushort virtualKey, bool keyUp)
    {
        return new INPUT
        {
            type = InputKeyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    dwFlags = keyUp ? KeyEventKeyUp : 0
                }
            }
        };
    }

    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const ushort VkShift = 0x10;
    private const ushort VkControl = 0x11;
    private const ushort VkMenu = 0x12;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

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
