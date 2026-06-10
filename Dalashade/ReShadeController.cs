using System;
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

        var virtualKey = configuration.ReloadHotkeyVirtualKey;
        if (virtualKey <= 0)
        {
            return ReloadResult.Skipped("Reload hotkey is not configured.");
        }

        var reshadeIniPath = TryFindReShadeIni(configuration);
        if (configuration.SyncReloadHotkeyToReShadeIni && reshadeIniPath != null)
        {
            var syncResult = TrySyncReloadHotkey(reshadeIniPath, virtualKey);
            if (!syncResult.Success)
            {
                return syncResult;
            }
        }

        Thread.Sleep(150);
        return SendKey(virtualKey)
            ? new ReloadResult(true, $"Reload hotkey sent ({FormatVirtualKey(virtualKey)}).")
            : ReloadResult.Skipped($"Could not send reload hotkey ({FormatVirtualKey(virtualKey)}).");
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

    private static ReloadResult TrySyncReloadHotkey(string reshadeIniPath, int virtualKey)
    {
        try
        {
            var lines = File.ReadAllLines(reshadeIniPath);
            var expected = $"KeyReload={virtualKey},0,0,0";
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

            return new ReloadResult(true, $"ReShade reload hotkey synced to {FormatVirtualKey(virtualKey)}.");
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

    private static bool SendKey(int virtualKey)
    {
        var inputs = new INPUT[2];
        inputs[0].type = InputKeyboard;
        inputs[0].u.ki.wVk = (ushort)virtualKey;
        inputs[1].type = InputKeyboard;
        inputs[1].u.ki.wVk = (ushort)virtualKey;
        inputs[1].u.ki.dwFlags = KeyEventKeyUp;

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) == inputs.Length;
    }

    private static string FormatVirtualKey(int virtualKey)
    {
        return virtualKey switch
        {
            112 => "F1",
            113 => "F2",
            114 => "F3",
            115 => "F4",
            116 => "F5",
            117 => "F6",
            118 => "F7",
            119 => "F8",
            120 => "F9",
            121 => "F10",
            122 => "F11",
            123 => "F12",
            _ => $"VK {virtualKey}"
        };
    }

    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;

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
