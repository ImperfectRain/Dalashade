using System;
using System.IO;
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
        UiSection.Draw("ConfigPaths", "Paths", true, PathsSummary(), DrawPaths);
        UiSection.Draw("ConfigBasePresetLibrary", "Base Preset Library", true, BaseLibrarySummary(), DrawBasePresetLibrary);
        UiSection.Draw("ConfigGenerationBehavior", "Generation Behavior", true, GenerationBehaviorSummary(), DrawGenerationBehavior);
        UiSection.Draw("ConfigMaterialIntent", "Material Intent", false, MaterialIntentSummary(), DrawMaterialIntent);
        UiSection.Draw("ConfigCompatibilityMode", "Compatibility Mode", false, CompatibilitySummary(), DrawCompatibilityMode);
        UiSection.Draw("ConfigShaderMatching", "Shader Matching", false, ShaderMatchingSummary(), DrawShaderMatching, ShaderMatchingWarningColor());
        UiSection.Draw("ConfigReShadeReload", "ReShade Reload", false, ReShadeReloadSummary(), DrawReShadeReloadSection, ReShadeReloadWarningColor());
        UiSection.Draw("ConfigScreenshotAnalysis", "Screenshot Analysis", false, ScreenshotAnalysisSummary(), DrawScreenshotAnalysis);
        UiSection.Draw("ConfigMasterStyleMatching", "Master Style Matching", false, MasterStyleSummary(), DrawMasterStyleMatching);
        UiSection.Draw("ConfigRegressionTesting", "Regression Testing", false, RegressionTestingSummary(), DrawRegressionTesting);
        UiSection.Draw("ConfigAdvancedDebug", "Advanced / Debug", false, AdvancedDebugSummary(), DrawAdvancedDebug);
    }

    private string PathsSummary()
    {
        return string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath)
            ? "Generated preset path missing"
            : $"Generated: {Path.GetFileName(configuration.GeneratedPresetPath)}";
    }

    private void DrawPaths()
    {
        DrawTextInput("Base preset path", configuration.BasePresetPath, value => configuration.BasePresetPath = value);
        DrawTextInput("Generated preset path", configuration.GeneratedPresetPath, value => configuration.GeneratedPresetPath = value);

        if (ImGui.Button("Use default generated path"))
        {
            configuration.GeneratedPresetPath = plugin.DefaultGeneratedPresetPath;
            configuration.Save();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("Recommended writable output");
    }

    private string BaseLibrarySummary()
    {
        return $"{plugin.LastBasePresetLibraryScan.Items.Count} presets found";
    }

    private string GenerationBehaviorSummary()
    {
        return configuration.Enabled
            ? $"{configuration.Style}, {configuration.PerformanceBudget}, writes every {configuration.MinimumSecondsBetweenWrites}s"
            : "Dynamic generation disabled";
    }

    private void DrawGenerationBehavior()
    {
        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enable dynamic preset generation", ref enabled))
        {
            configuration.Enabled = enabled;
            configuration.Save();
        }

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
        DrawCheckbox("Write generated preset backups", configuration.WriteBackups, value => configuration.WriteBackups = value);

        var maxBackups = configuration.MaxGeneratedPresetBackups;
        if (ImGui.SliderInt("Max generated preset backups", ref maxBackups, 1, 50))
        {
            configuration.MaxGeneratedPresetBackups = maxBackups;
            configuration.Save();
        }

        var minimumSeconds = configuration.MinimumSecondsBetweenWrites;
        if (ImGui.SliderInt("Minimum seconds between writes", ref minimumSeconds, 1, 120))
        {
            configuration.MinimumSecondsBetweenWrites = minimumSeconds;
            configuration.Save();
        }

        if (ImGui.Button("Generate Now###ConfigGenerateNow"))
        {
            plugin.GenerateNow();
        }

        ImGui.SameLine();
        ImGui.TextWrapped(plugin.LastWriteResult.Message);
    }

    private string MaterialIntentSummary()
    {
        if (!configuration.EnableMaterialIntent)
        {
            return "Disabled";
        }

        var mapping = configuration.EnableMaterialIntentShaderMapping ? "mapping toggle on" : "diagnostics only";
        return $"Experimental inferred materials, strength {configuration.MaterialIntentStrength:0.##}, {mapping}";
    }

    private void DrawMaterialIntent()
    {
        DrawCheckbox("Enable MaterialIntent inference", configuration.EnableMaterialIntent, value => configuration.EnableMaterialIntent = value);
        DrawCheckbox("Show MaterialIntent diagnostics in reports/UI", configuration.EnableMaterialIntentDiagnostics, value => configuration.EnableMaterialIntentDiagnostics = value);
        DrawCheckbox("Allow MaterialIntent shader variable writes", configuration.EnableMaterialIntentShaderMapping, value => configuration.EnableMaterialIntentShaderMapping = value);
        DrawFloatSlider("MaterialIntent strength", configuration.MaterialIntentStrength, 0f, 1f, value => configuration.MaterialIntentStrength = value);
        DrawCheckbox("Enable material debug mask variables", configuration.EnableMaterialDebugMasks, value => configuration.EnableMaterialDebugMasks = value);

        var debugMode = configuration.MaterialDebugMaskMode;
        if (ImGui.SliderInt("Material debug mask mode", ref debugMode, 0, 12))
        {
            configuration.MaterialDebugMaskMode = debugMode;
            configuration.Save();
        }

        ImGui.TextWrapped("Experimental/inferred: MaterialIntent estimates likely scene material families from tags and screenshot metrics. It is not true FFXIV engine material ID detection.");
        ImGui.TextWrapped("Shader mapping writes MaterialIntent variables only into matching known Dalashade custom shader sections when enabled. Missing uniforms are skipped safely.");
        ImGui.TextWrapped("Current first-party shaders do not consume these material uniforms yet, so normal visuals remain unchanged. Config changes affect generated presets only after regeneration. No live ReShade control is implemented.");
    }

    private string CompatibilitySummary()
    {
        return $"{PresetAnalyzer.FormatCompatibilityMode(configuration.CompatibilityMode)}, risk {plugin.LastPresetAnalysis.Report.Level}";
    }

    private void DrawCompatibilityMode()
    {
        DrawCheckbox("Use installed iMMERSE Pro/Ultimate variables", configuration.UsePremiumImmerseEffects, value => configuration.UsePremiumImmerseEffects = value);

        var compatibilityMode = (int)configuration.CompatibilityMode;
        if (ImGui.Combo("Compatibility mode", ref compatibilityMode, "Preserve base\0Adaptive balanced\0Gameplay sanitize\0Cinematic preserve\0GPose preserve\0"))
        {
            configuration.CompatibilityMode = (PresetCompatibilityMode)compatibilityMode;
            configuration.Save();
        }

        if (ImGui.Button("Scan Preset Compatibility###ConfigScanPresetCompatibility"))
        {
            plugin.ScanPresetCompatibility();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Compatibility Report###ConfigExportCompatibilityReport"))
        {
            plugin.ExportCompatibilityReport();
        }

        ImGui.TextWrapped(plugin.LastPresetAnalysis.Message);
        ImGui.TextWrapped(plugin.LastCompatibilityReportExport.Message);
    }

    private string ShaderMatchingSummary()
    {
        return $"{configuration.ShaderMatchingMode}, inactive writes {configuration.InactiveShaderWriteMode}, custom shaders {(configuration.EnableDalashadeCustomShaders ? "on" : "off")}, injection {(configuration.AutoInjectDalashadeCustomShaderSections ? "on" : "off")}";
    }

    private Vector4? ShaderMatchingWarningColor()
    {
        return configuration.ShaderMatchingMode == ShaderMatchingMode.LooseKeys ? new Vector4(1.0f, 0.72f, 0.28f, 1.0f) : null;
    }

    private void DrawShaderMatching()
    {
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

        DrawCheckbox("Enable Dalashade custom shader variables", configuration.EnableDalashadeCustomShaders, value => configuration.EnableDalashadeCustomShaders = value);
        DrawCheckbox("Auto-inject known Dalashade shader sections into generated preset", configuration.AutoInjectDalashadeCustomShaderSections, value => configuration.AutoInjectDalashadeCustomShaderSections = value);
        ImGui.TextWrapped("When enabled with custom shader variables, Dalashade can add known Dalashade custom shader sections and variables to the generated preset only. The base preset is never modified.");
        ImGui.TextWrapped("This does not install .fx shader files. Install needed Dalashade shaders in ReShade separately so ReShade can compile injected generated-preset sections.");
    }

    private string ReShadeReloadSummary()
    {
        var diagnostics = plugin.LastReloadResult.Diagnostics;
        return $"Hotkey {diagnostics.ConfiguredReloadKey}, ReShade.ini {(diagnostics.ReShadeIniFound ? "found" : "not found")}";
    }

    private Vector4? ReShadeReloadWarningColor()
    {
        return plugin.LastReloadResult.Diagnostics.ReShadeIniFound ? null : new Vector4(1.0f, 0.72f, 0.28f, 1.0f);
    }

    private void DrawReShadeReloadSection()
    {
        DrawCheckbox("Reload shaders after generation", configuration.ReloadShadersAfterGeneration, value => configuration.ReloadShadersAfterGeneration = value);
        DrawCheckbox("Sync reload key to ReShade.ini", configuration.SyncReloadHotkeyToReShadeIni, value => configuration.SyncReloadHotkeyToReShadeIni = value);
        DrawReShadeReloadSettings();
        DrawReloadHotkeyPicker();
        ImGui.TextWrapped(plugin.LastReloadResult.Message);
    }

    private string ScreenshotAnalysisSummary()
    {
        return configuration.AutoAdjustFromScreenshots
            ? $"{configuration.ImageSamplingMode}, every {configuration.MinimumSecondsBetweenImageSamples}s"
            : "Disabled";
    }

    private void DrawScreenshotAnalysis()
    {
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

        var minimumImageSeconds = configuration.MinimumSecondsBetweenImageSamples;
        if (ImGui.SliderInt("Minimum seconds between image samples", ref minimumImageSeconds, 1, 300))
        {
            configuration.MinimumSecondsBetweenImageSamples = minimumImageSeconds;
            configuration.Save();
        }
    }

    private string MasterStyleSummary()
    {
        return configuration.MatchMasterPresetStyle
            ? $"Active, {configuration.MasterPresetStyleStrength}% raw, effective {plugin.CurrentMasterStyleDiagnostics.EffectiveStrength:P0}"
            : "Disabled";
    }

    private void DrawMasterStyleMatching()
    {
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

        var tuningPreset = (int)configuration.MasterStyleTuningPreset;
        if (ImGui.Combo("Master tuning preset", ref tuningPreset, "Subtle\0Balanced\0Strong\0Cinematic\0Aggressive GPose\0Custom\0"))
        {
            configuration.MasterStyleTuningPreset = (MasterStyleTuningPreset)tuningPreset;
            MasterStyleTuningPresets.Apply(configuration, configuration.MasterStyleTuningPreset);
            configuration.Save();
        }

        if (ImGui.Button("Reset tuning to Balanced###ResetMasterTuningBalanced"))
        {
            configuration.MasterStyleTuningPreset = MasterStyleTuningPreset.Balanced;
            MasterStyleTuningPresets.Apply(configuration, configuration.MasterStyleTuningPreset);
            configuration.Save();
        }

        DrawMasterFloatSlider("Tonal match strength", configuration.MasterTonalMatchStrength, 0f, 2f, value => configuration.MasterTonalMatchStrength = value);
        DrawMasterFloatSlider("Tonal color strength", configuration.MasterTonalColorStrength, 0f, 2f, value => configuration.MasterTonalColorStrength = value);
        DrawMasterFloatSlider("Color-family strength", configuration.MasterColorFamilyStrength, 0f, 2f, value => configuration.MasterColorFamilyStrength = value);
        DrawMasterFloatSlider("Max hue shift", configuration.MasterMaxHueShift, 0f, 0.20f, value => configuration.MasterMaxHueShift = value);
        DrawMasterFloatSlider("Max saturation shift", configuration.MasterMaxSaturationShift, 0f, 0.35f, value => configuration.MasterMaxSaturationShift = value);
        DrawMasterFloatSlider("Max luminance shift", configuration.MasterMaxLuminanceShift, 0f, 0.35f, value => configuration.MasterMaxLuminanceShift = value);
        DrawCheckbox("Scene similarity dampening", configuration.MasterSceneSimilarityDampening, value => configuration.MasterSceneSimilarityDampening = value);

        var masterMaxImages = configuration.MasterPresetMaxImages;
        if (ImGui.SliderInt("Master style max images", ref masterMaxImages, 1, 100))
        {
            configuration.MasterPresetMaxImages = masterMaxImages;
            configuration.Save();
        }

        var diagnostics = plugin.CurrentMasterStyleDiagnostics;
        ImGui.TextWrapped($"Effective strength: {diagnostics.EffectiveStrength:0.###}; scene similarity: {diagnostics.SceneSimilarityMultiplier:0.###}; compatibility: {diagnostics.CompatibilityModeMultiplier:0.###}");
        ImGui.TextWrapped(diagnostics.Status);
    }

    private string RegressionTestingSummary()
    {
        return plugin.LastPresetRegressionReport.Success
            ? $"Last run: {plugin.LastPresetRegressionReport.PresetCount} presets"
            : plugin.LastPresetRegressionReport.Message;
    }

    private void DrawRegressionTesting()
    {
        DrawTextInput("Test preset folder", configuration.TestPresetFolderPath, value => configuration.TestPresetFolderPath = value);

        if (ImGui.Button("Run Preset Regression Reports"))
        {
            plugin.RunPresetRegressionReports();
        }

        ImGui.SameLine();
        ImGui.TextWrapped(plugin.LastPresetRegressionReport.Message);
    }

    private string AdvancedDebugSummary()
    {
        return $"{plugin.LastShaderSupportScan.Items.Count} supported variables detected";
    }

    private void DrawAdvancedDebug()
    {
        ImGui.TextWrapped(plugin.LastWriteResult.Message);
        ImGui.TextWrapped(plugin.LastReloadResult.Message);
        ImGui.TextWrapped(plugin.LastCompatibilityReportExport.Message);
        ImGui.TextWrapped(plugin.LastShaderSupportScan.Message);
    }

    private void DrawBasePresetLibrary()
    {
        DrawCheckbox("Use base preset folder", configuration.UseBasePresetFolder, value => configuration.UseBasePresetFolder = value);
        DrawTextInput("Base folder", configuration.BasePresetFolderPath, value =>
        {
            configuration.BasePresetFolderPath = value;
            plugin.RefreshBasePresetLibrary();
        });

        if (ImGui.Button("Open Base Folder"))
        {
            plugin.OpenBasePresetFolder();
        }

        ImGui.SameLine();
        if (ImGui.Button("Refresh Base Presets"))
        {
            plugin.RefreshBasePresetLibrary();
        }

        var scan = plugin.LastBasePresetLibraryScan;
        ImGui.TextWrapped(scan.Message);
        if (scan.Items.Count == 0)
        {
            ImGui.TextWrapped("No .ini presets found. Place base presets in the Base folder.");
            return;
        }

        var selectedLabel = string.IsNullOrWhiteSpace(configuration.SelectedBasePresetFileName)
            ? "Select a base preset"
            : configuration.SelectedBasePresetFileName;
        if (ImGui.BeginCombo("Base preset", selectedLabel))
        {
            foreach (var item in scan.Items)
            {
                var selected = string.Equals(item.FileName, configuration.SelectedBasePresetFileName, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(item.FullPath, configuration.BasePresetPath, StringComparison.OrdinalIgnoreCase);
                var label = $"{item.FileName}###{item.FullPath}";
                if (ImGui.Selectable(label, selected))
                {
                    plugin.SelectBasePreset(item);
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
    }

    private void DrawReShadeReloadSettings()
    {
        DrawTextInput("ReShade.ini path", configuration.ReShadeIniPath, value => configuration.ReShadeIniPath = value);

        if (ImGui.Button("Browse###BrowseReShadeIni"))
        {
            plugin.BrowseReShadeIniPath();
        }

        ImGui.SameLine();
        if (ImGui.Button("Auto-detect###AutoDetectReShadeIni"))
        {
            plugin.AutoDetectReShadeIniPath();
        }

        ImGui.SameLine();
        if (ImGui.Button("Test Reload###ConfigTestReloadInline"))
        {
            plugin.ReloadShadersNow();
        }

        var diagnostics = plugin.LastReloadResult.Diagnostics;
        ImGui.TextWrapped($"ReShade.ini: {(diagnostics.ReShadeIniFound ? diagnostics.ReShadeIniPath : "not found")}");
        ImGui.TextWrapped($"KeyReload: {diagnostics.KeyReloadValue}; configured: {diagnostics.ConfiguredReloadKey}; sync: {(diagnostics.HotkeySyncEnabled ? "on" : "off")}");
        ImGui.TextWrapped($"PostMessage: {(diagnostics.PostMessageSucceeded ? "ok" : "failed")}; SendInput: {(diagnostics.SendInputSucceeded ? "ok" : "failed")}");
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

    private void DrawFloatSlider(string label, float currentValue, float min, float max, Action<float> update)
    {
        var value = currentValue;
        if (ImGui.SliderFloat(label, ref value, min, max, "%.3f"))
        {
            update(value);
            configuration.Save();
        }
    }

    private void DrawMasterFloatSlider(string label, float currentValue, float min, float max, Action<float> update)
    {
        var value = currentValue;
        if (ImGui.SliderFloat(label, ref value, min, max, "%.3f"))
        {
            update(value);
            configuration.MasterStyleTuningPreset = MasterStyleTuningPreset.Custom;
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
