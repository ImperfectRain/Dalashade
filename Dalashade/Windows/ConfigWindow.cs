using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Dalashade.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private readonly KeybindCapture reloadHotkeyCapture = new();
    private bool capturingReloadHotkey;

    public ConfigWindow(Plugin plugin) : base("Dalashade Settings###DalashadeConfig")
    {
        Size = new Vector2(760, 520);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enable dynamic preset generation", ref enabled))
        {
            configuration.Enabled = enabled;
            configuration.Save();
        }

        DrawTextInput("Base preset path", configuration.BasePresetPath, value => configuration.BasePresetPath = value);
        DrawTextInput("Generated preset path", configuration.GeneratedPresetPath, value => configuration.GeneratedPresetPath = value);

        if (ImGui.Button("Use Dalamud config folder"))
        {
            configuration.GeneratedPresetPath = plugin.DefaultGeneratedPresetPath;
            configuration.Save();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("Recommended writable output");

        DrawCheckbox("Reload shaders after generation", configuration.ReloadShadersAfterGeneration, value => configuration.ReloadShadersAfterGeneration = value);
        DrawCheckbox("Sync reload key to ReShade.ini", configuration.SyncReloadHotkeyToReShadeIni, value => configuration.SyncReloadHotkeyToReShadeIni = value);

        DrawReloadHotkeyPicker();

        ImGui.Separator();

        var style = (int)configuration.Style;
        if (ImGui.Combo("Target style", ref style, "Gameplay\0Balanced\0Cinematic\0"))
        {
            configuration.Style = (TargetStyle)style;
            configuration.Save();
        }

        var budget = (int)configuration.PerformanceBudget;
        if (ImGui.Combo("Performance budget", ref budget, "Low\0Medium\0High\0Ultra\0"))
        {
            configuration.PerformanceBudget = (PerformanceBudget)budget;
            configuration.Save();
        }

        DrawCheckbox("Auto-adjust in combat", configuration.AutoAdjustInCombat, value => configuration.AutoAdjustInCombat = value);
        DrawCheckbox("Auto-adjust at night", configuration.AutoAdjustAtNight, value => configuration.AutoAdjustAtNight = value);
        DrawCheckbox("Auto-adjust for weather", configuration.AutoAdjustForWeather, value => configuration.AutoAdjustForWeather = value);
        DrawCheckbox("Auto-adjust for territory type", configuration.AutoAdjustForTerritory, value => configuration.AutoAdjustForTerritory = value);
        DrawCheckbox("Auto-adjust in cutscenes", configuration.AutoAdjustInCutscenes, value => configuration.AutoAdjustInCutscenes = value);
        DrawCheckbox("Lock current generated preset", configuration.SceneLockEnabled, value => configuration.SceneLockEnabled = value);
        DrawCheckbox("Auto-adjust from screenshots", configuration.AutoAdjustFromScreenshots, value => configuration.AutoAdjustFromScreenshots = value);
        DrawTextInput("Screenshot folder", configuration.ScreenshotFolderPath, value => configuration.ScreenshotFolderPath = value);

        var samplingMode = (int)configuration.ImageSamplingMode;
        if (ImGui.Combo("Screenshot sampling", ref samplingMode, "Full image\0Center-weighted\0Ignore bottom UI\0Letterbox safe\0GPose clean\0"))
        {
            configuration.ImageSamplingMode = (ImageSamplingMode)samplingMode;
            configuration.Save();
        }

        if (ImGui.Button("Use FFXIV screenshot folder"))
        {
            configuration.ScreenshotFolderPath = plugin.DefaultScreenshotFolderPath;
            configuration.Save();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("For screenshot analysis");

        DrawCheckbox("Match master preset style", configuration.MatchMasterPresetStyle, value => configuration.MatchMasterPresetStyle = value);
        DrawTextInput("Master preset image folder", configuration.MasterPresetFolderPath, value => configuration.MasterPresetFolderPath = value);
        DrawCheckbox("Include master preset subfolders", configuration.MasterPresetIncludeSubfolders, value => configuration.MasterPresetIncludeSubfolders = value);

        var masterMode = (int)configuration.MasterStyleMode;
        if (ImGui.Combo("Master style mode", ref masterMode, "Newest image only\0Average folder\0Median folder\0Closest to current scene\0"))
        {
            configuration.MasterStyleMode = (MasterStyleMode)masterMode;
            configuration.Save();
        }

        var masterStrength = configuration.MasterPresetStyleStrength;
        if (ImGui.SliderInt("Master style strength", ref masterStrength, 0, 100))
        {
            configuration.MasterPresetStyleStrength = masterStrength;
            configuration.Save();
        }

        var masterMaxImages = configuration.MasterPresetMaxImages;
        if (ImGui.SliderInt("Master style max images", ref masterMaxImages, 1, 100))
        {
            configuration.MasterPresetMaxImages = masterMaxImages;
            configuration.Save();
        }

        DrawCheckbox("Use installed iMMERSE Pro/Ultimate variables", configuration.UsePremiumImmerseEffects, value => configuration.UsePremiumImmerseEffects = value);

        var compatibilityMode = (int)configuration.CompatibilityMode;
        if (ImGui.Combo("Compatibility mode", ref compatibilityMode, "Preserve base\0Adaptive balanced\0Gameplay sanitize\0Cinematic preserve\0GPose preserve\0"))
        {
            configuration.CompatibilityMode = (PresetCompatibilityMode)compatibilityMode;
            configuration.Save();
        }

        var matchingMode = (int)configuration.ShaderMatchingMode;
        if (ImGui.Combo("Shader matching", ref matchingMode, "Strict sections\0Known fallbacks\0Loose keys\0"))
        {
            configuration.ShaderMatchingMode = (ShaderMatchingMode)matchingMode;
            configuration.Save();
        }

        if (configuration.ShaderMatchingMode == ShaderMatchingMode.LooseKeys)
        {
            ImGui.TextWrapped("Loose key matching may edit variables outside known shader sections. It is useful for testing unsupported presets, but strict matching is safer for normal use.");
        }

        var inactiveWriteMode = (int)configuration.InactiveShaderWriteMode;
        if (ImGui.Combo("Inactive shader writes", ref inactiveWriteMode, "Never\0Supported inactive sections\0Always matching keys\0"))
        {
            configuration.InactiveShaderWriteMode = (InactiveShaderWriteMode)inactiveWriteMode;
            configuration.Save();
        }

        DrawCheckbox("Write generated preset backups", configuration.WriteBackups, value => configuration.WriteBackups = value);

        var maxBackups = configuration.MaxGeneratedPresetBackups;
        if (ImGui.SliderInt("Max generated preset backups", ref maxBackups, 1, 50))
        {
            configuration.MaxGeneratedPresetBackups = maxBackups;
            configuration.Save();
        }

        if (ImGui.Button("Scan Preset Compatibility"))
        {
            plugin.ScanPresetCompatibility();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Compatibility Report"))
        {
            plugin.ExportCompatibilityReport();
        }

        ImGui.SameLine();
        ImGui.TextWrapped(plugin.LastPresetAnalysis.Message);
        ImGui.TextWrapped(plugin.LastCompatibilityReportExport.Message);

        var minimumSeconds = configuration.MinimumSecondsBetweenWrites;
        if (ImGui.SliderInt("Minimum seconds between writes", ref minimumSeconds, 1, 120))
        {
            configuration.MinimumSecondsBetweenWrites = minimumSeconds;
            configuration.Save();
        }

        var minimumImageSeconds = configuration.MinimumSecondsBetweenImageSamples;
        if (ImGui.SliderInt("Minimum seconds between image samples", ref minimumImageSeconds, 1, 300))
        {
            configuration.MinimumSecondsBetweenImageSamples = minimumImageSeconds;
            configuration.Save();
        }

        ImGui.Separator();

        if (ImGui.Button("Generate Now"))
        {
            plugin.GenerateNow();
        }

        ImGui.SameLine();
        if (ImGui.Button("Test Reload"))
        {
            plugin.ReloadShadersNow();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(plugin.LastWriteResult.Message);
        ImGui.TextWrapped(plugin.LastReloadResult.Message);
    }

    private void DrawTextInput(string label, string currentValue, Action<string> update)
    {
        var value = currentValue;
        if (ImGui.InputText(label, ref value, 512))
        {
            update(value);
            configuration.Save();
        }
    }

    private void DrawCheckbox(string label, bool currentValue, Action<bool> update)
    {
        var value = currentValue;
        if (ImGui.Checkbox(label, ref value))
        {
            update(value);
            configuration.Save();
        }
    }

    private void DrawReloadHotkeyPicker()
    {
        var currentHotkey = Keybind.FromConfiguration(configuration);
        ImGui.TextUnformatted($"Reload hotkey: {currentHotkey.DisplayName}");
        ImGui.SameLine();

        var buttonText = capturingReloadHotkey ? "Listening...###ReloadHotkeyPicker" : "Set reload hotkey###ReloadHotkeyPicker";
        if (ImGui.Button(buttonText))
        {
            capturingReloadHotkey = true;
            reloadHotkeyCapture.Start();
        }

        if (capturingReloadHotkey)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted("Press a key or button. Hold Ctrl, Shift, or Alt for a combo. Esc cancels.");

            var captured = reloadHotkeyCapture.Poll();
            if (captured.HasValue)
            {
                if (captured.Value.VirtualKey > 0)
                {
                    captured.Value.ApplyTo(configuration);
                    configuration.Save();
                }

                capturingReloadHotkey = false;
            }
        }
        else
        {
            ImGui.SameLine();
            ImGui.TextUnformatted("Default is F5. Dalashade writes the same combo to ReShade.ini when sync is on.");
        }
    }
}
