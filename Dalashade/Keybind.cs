using System;
using System.Runtime.InteropServices;

namespace Dalashade;

public readonly struct Keybind
{
    public Keybind(int virtualKey, bool ctrl, bool shift, bool alt)
    {
        VirtualKey = virtualKey;
        Ctrl = ctrl;
        Shift = shift;
        Alt = alt;
    }

    public int VirtualKey { get; }
    public bool Ctrl { get; }
    public bool Shift { get; }
    public bool Alt { get; }

    public bool IsConfigured => VirtualKey > 0;

    public string ReShadeValue => $"{VirtualKey},{BoolToInt(Ctrl)},{BoolToInt(Shift)},{BoolToInt(Alt)}";

    public string DisplayName
    {
        get
        {
            if (!IsConfigured)
            {
                return "Not set";
            }

            var prefix = string.Empty;
            if (Ctrl)
            {
                prefix += "Ctrl+";
            }

            if (Shift)
            {
                prefix += "Shift+";
            }

            if (Alt)
            {
                prefix += "Alt+";
            }

            return $"{prefix}{FormatVirtualKey(VirtualKey)}";
        }
    }

    public static Keybind FromConfiguration(Configuration configuration)
    {
        return new Keybind(
            configuration.ReloadHotkeyVirtualKey,
            configuration.ReloadHotkeyCtrl,
            configuration.ReloadHotkeyShift,
            configuration.ReloadHotkeyAlt);
    }

    public void ApplyTo(Configuration configuration)
    {
        configuration.ReloadHotkeyVirtualKey = Math.Max(0, VirtualKey);
        configuration.ReloadHotkeyCtrl = Ctrl;
        configuration.ReloadHotkeyShift = Shift;
        configuration.ReloadHotkeyAlt = Alt;
    }

    public static string FormatVirtualKey(int virtualKey)
    {
        if (virtualKey >= 65 && virtualKey <= 90)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey >= 48 && virtualKey <= 57)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey >= 96 && virtualKey <= 105)
        {
            return $"Num {virtualKey - 96}";
        }

        if (virtualKey >= 112 && virtualKey <= 123)
        {
            return $"F{virtualKey - 111}";
        }

        return virtualKey switch
        {
            1 => "Mouse 1",
            2 => "Mouse 2",
            4 => "Mouse 3",
            5 => "Mouse 4",
            6 => "Mouse 5",
            8 => "Backspace",
            9 => "Tab",
            13 => "Enter",
            19 => "Pause",
            20 => "Caps Lock",
            27 => "Esc",
            32 => "Space",
            33 => "Page Up",
            34 => "Page Down",
            35 => "End",
            36 => "Home",
            37 => "Left",
            38 => "Up",
            39 => "Right",
            40 => "Down",
            44 => "Print Screen",
            45 => "Insert",
            46 => "Delete",
            91 => "Left Win",
            92 => "Right Win",
            106 => "Num *",
            107 => "Num +",
            109 => "Num -",
            110 => "Num .",
            111 => "Num /",
            144 => "Num Lock",
            145 => "Scroll Lock",
            186 => ";",
            187 => "=",
            188 => ",",
            189 => "-",
            190 => ".",
            191 => "/",
            192 => "`",
            219 => "[",
            220 => "\\",
            221 => "]",
            222 => "'",
            _ => $"VK {virtualKey}"
        };
    }

    private static int BoolToInt(bool value) => value ? 1 : 0;
}

public sealed class KeybindCapture
{
    private bool waitingForRelease;

    public void Start()
    {
        FlushPressState();
        waitingForRelease = true;
    }

    public Keybind? Poll()
    {
        if (waitingForRelease)
        {
            if (AnyCandidateDown() || IsModifierDown())
            {
                return null;
            }

            waitingForRelease = false;
        }

        if (WasPressed(27))
        {
            return new Keybind(0, false, false, false);
        }

        for (var virtualKey = 1; virtualKey <= 254; virtualKey++)
        {
            if (IsModifierKey(virtualKey) || virtualKey is 91 or 92)
            {
                continue;
            }

            if (!WasPressed(virtualKey))
            {
                continue;
            }

            return new Keybind(
                virtualKey,
                IsDown(17) || IsDown(162) || IsDown(163),
                IsDown(16) || IsDown(160) || IsDown(161),
                IsDown(18) || IsDown(164) || IsDown(165));
        }

        return null;
    }

    private static bool AnyCandidateDown()
    {
        for (var virtualKey = 1; virtualKey <= 254; virtualKey++)
        {
            if (!IsModifierKey(virtualKey) && IsDown(virtualKey))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsModifierDown()
    {
        return IsDown(16) || IsDown(17) || IsDown(18) ||
               IsDown(160) || IsDown(161) || IsDown(162) || IsDown(163) || IsDown(164) || IsDown(165);
    }

    private static bool IsModifierKey(int virtualKey)
    {
        return virtualKey is 16 or 17 or 18 or 160 or 161 or 162 or 163 or 164 or 165;
    }

    private static void FlushPressState()
    {
        for (var virtualKey = 1; virtualKey <= 254; virtualKey++)
        {
            _ = WasPressed(virtualKey);
        }
    }

    private static bool IsDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private static bool WasPressed(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x0001) != 0;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
