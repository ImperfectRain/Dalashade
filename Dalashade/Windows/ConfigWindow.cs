using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalashade.Windows.UiPages;

namespace Dalashade.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private readonly KeybindCapture reloadHotkeyCapture = new();
    private bool capturingReloadHotkey;
    private string developerSearch = string.Empty;

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
        DrawInterfaceModeSwitch("Config");
        ImGui.Spacing();

        if (configuration.InterfaceMode == InterfaceMode.User)
        {
            DrawUserMode();
            return;
        }

        DrawDeveloperMode();
    }

    private void DrawDeveloperMode()
    {
        ImGui.InputText("Search developer settings###ConfigDeveloperSearch", ref developerSearch, 128);

        if (!string.IsNullOrWhiteSpace(developerSearch))
        {
            DrawDeveloperSearchResults();
            return;
        }

        if (!ImGui.BeginTabBar("ConfigDeveloperTabs"))
        {
            return;
        }

        DrawDeveloperTab("Setup", BuildDeveloperSetupPages());
        DrawDeveloperTab("Generation", BuildDeveloperGenerationPages());
        DrawDeveloperTab("Shader Mapping", BuildDeveloperShaderMappingPages());
        DrawDeveloperTab("Scene Data", BuildDeveloperSceneDataPages());
        DrawDeveloperTab("Diagnostics", BuildDeveloperDiagnosticsPages());

        ImGui.EndTabBar();
    }

    private void DrawDeveloperTab(string title, IReadOnlyList<IDalashadeUiPage> pages)
    {
        if (!ImGui.BeginTabItem(title))
        {
            return;
        }

        UiPageRenderer.Draw(pages, InterfaceMode.Developer);
        ImGui.EndTabItem();
    }

    private void DrawDeveloperSearchResults()
    {
        var pages = BuildAllDeveloperPages()
            .Where(page => DeveloperPageMatches(page, developerSearch))
            .GroupBy(page => page.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();

        if (pages.Length == 0)
        {
            ImGui.TextUnformatted("No developer settings match the search.");
            return;
        }

        UiPageRenderer.Draw(pages, InterfaceMode.Developer);
    }

    private static bool DeveloperPageMatches(IDalashadeUiPage page, string query)
    {
        return page.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
               || page.Summary().Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<IDalashadeUiPage> BuildAllDeveloperPages()
    {
        return BuildDeveloperSetupPages()
            .Concat(BuildDeveloperGenerationPages())
            .Concat(BuildDeveloperShaderMappingPages())
            .Concat(BuildDeveloperSceneDataPages())
            .Concat(BuildDeveloperDiagnosticsPages())
            .ToArray();
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperSetupPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("ConfigPaths", "Paths", true, PathsSummary, DrawPaths),
            DeveloperPage("ConfigBasePresetLibrary", "Base Preset Library", true, BaseLibrarySummary, DrawBasePresetLibrary),
            DeveloperPage("ConfigReShadeReload", "ReShade Reload", true, ReShadeReloadSummary, DrawReShadeReloadSection, ReShadeReloadWarningColor)
        };
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperGenerationPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("ConfigGenerationBehavior", "Generation Behavior", true, GenerationBehaviorSummary, DrawGenerationBehavior),
            DeveloperPage("ConfigCompatibilityMode", "Compatibility Mode", false, CompatibilitySummary, DrawCompatibilityMode),
            DeveloperPage("ConfigReShadeReload", "ReShade Reload", false, ReShadeReloadSummary, DrawReShadeReloadSection, ReShadeReloadWarningColor)
        };
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperShaderMappingPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("ConfigShaderMatching", "Shader Matching", true, ShaderMatchingSummary, DrawShaderMatching, ShaderMatchingWarningColor),
            DeveloperPage("ConfigMaterialIntent", "Material Intent", false, MaterialIntentSummary, DrawMaterialIntent),
            DeveloperPage("ConfigNormalField", "Normal Field", false, NormalFieldSummary, DrawNormalField)
        };
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperSceneDataPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("ConfigScreenshotAnalysis", "Screenshot Analysis", true, ScreenshotAnalysisSummary, DrawScreenshotAnalysis),
            DeveloperPage("ConfigMasterStyleMatching", "Master Style Matching", false, MasterStyleSummary, DrawMasterStyleMatching),
            DeveloperPage("ConfigMaterialIntent", "Material Intent", false, MaterialIntentSummary, DrawMaterialIntent),
            DeveloperPage("ConfigNormalField", "Normal Field", false, NormalFieldSummary, DrawNormalField)
        };
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperDiagnosticsPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("ConfigCompatibilityMode", "Compatibility Mode", true, CompatibilitySummary, DrawCompatibilityMode),
            DeveloperPage("ConfigRegressionTesting", "Regression Testing", false, RegressionTestingSummary, DrawRegressionTesting),
            DeveloperPage("ConfigAdvancedDebug", "Advanced / Debug", false, AdvancedDebugSummary, DrawAdvancedDebug)
        };
    }

    private static IDalashadeUiPage DeveloperPage(
        string id,
        string title,
        bool defaultOpen,
        Func<string> summary,
        Action draw,
        Func<Vector4?>? summaryColor = null)
    {
        return new DelegateDalashadeUiPage(id, title, defaultOpen, InterfaceModeVisibility.Developer, summary, draw, summaryColor);
    }

    private void DrawUserMode()
    {
        UiPageRenderer.Draw(BuildUserPages(), InterfaceMode.User);
    }

    private IReadOnlyList<IDalashadeUiPage> BuildUserPages()
    {
        var healthOpen = plugin.LastPresetAnalysis.Report.Level is PresetRiskLevel.High or PresetRiskLevel.VeryHigh;
        return new IDalashadeUiPage[]
        {
            new DelegateDalashadeUiPage(
                "ConfigUserSetup",
                "Setup",
                true,
                InterfaceModeVisibility.User,
                UserSetupSummary,
                DrawUserSetup),
            new DelegateDalashadeUiPage(
                "ConfigUserLook",
                "Look",
                false,
                InterfaceModeVisibility.User,
                UserLookSummary,
                DrawUserLook),
            new DelegateDalashadeUiPage(
                "ConfigUserSceneAwareness",
                "Scene Awareness",
                false,
                InterfaceModeVisibility.User,
                UserSceneAwarenessSummary,
                DrawUserSceneAwareness),
            new DelegateDalashadeUiPage(
                "ConfigUserEffects",
                "Effects",
                false,
                InterfaceModeVisibility.User,
                UserEffectsSummary,
                DrawUserEffects),
            new DelegateDalashadeUiPage(
                "ConfigUserHealth",
                "Health",
                healthOpen,
                InterfaceModeVisibility.User,
                UserHealthSummary,
                DrawUserHealth,
                UserHealthWarningColor)
        };
    }

    private void DrawInterfaceModeSwitch(string idPrefix)
    {
        ImGui.TextUnformatted("Interface mode");
        ImGui.SameLine();

        var userSelected = configuration.InterfaceMode == InterfaceMode.User;
        if (ImGui.RadioButton($"User###{idPrefix}InterfaceUser", userSelected))
        {
            configuration.InterfaceMode = InterfaceMode.User;
            configuration.Save();
        }

        ImGui.SameLine();
        var developerSelected = configuration.InterfaceMode == InterfaceMode.Developer;
        if (ImGui.RadioButton($"Developer###{idPrefix}InterfaceDeveloper", developerSelected))
        {
            configuration.InterfaceMode = InterfaceMode.Developer;
            configuration.Save();
        }

        ImGui.TextWrapped(configuration.InterfaceMode == InterfaceMode.User
            ? "User Mode shows the setup, look, effect, and health controls most users need."
            : "Developer Mode shows the full low-level settings surface for diagnostics, mapping, and shader authoring.");
    }

    private string UserSetupSummary()
    {
        var generatedReady = !string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath);
        var baseReady = !string.IsNullOrWhiteSpace(configuration.BasePresetPath) && File.Exists(configuration.BasePresetPath);
        return baseReady && generatedReady ? "Ready to generate" : "Needs preset paths";
    }

    private void DrawUserSetup()
    {
        DrawSetupItem("Base preset selected", !string.IsNullOrWhiteSpace(configuration.BasePresetPath) && File.Exists(configuration.BasePresetPath));
        DrawSetupItem("Generated preset path selected", !string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath));
        DrawSetupItem("Generated preset is separate", !string.Equals(configuration.BasePresetPath, configuration.GeneratedPresetPath, StringComparison.OrdinalIgnoreCase));

        DrawBasePresetLibrary();
        ImGui.Separator();
        DrawPaths();

        if (ImGui.Button("Generate Now###ConfigUserGenerateNow"))
        {
            plugin.GenerateNow();
        }

        ImGui.SameLine();
        ImGui.TextWrapped(UserWriteSummary());
    }

    private string UserWriteSummary()
    {
        var result = plugin.LastWriteResult;
        if (!result.Success)
        {
            return string.IsNullOrWhiteSpace(result.Message)
                ? "Generated preset has not been updated yet."
                : result.Message;
        }

        var clamped = result.Changes.Count(change => change.HitMin || change.HitMax);
        var warnings = result.Changes.Count(change => !string.IsNullOrWhiteSpace(change.Warning));
        return $"Generated preset updated. {result.ChangedVariables} supported values changed; {clamped} clamped; {warnings} warnings.";
    }

    private string UserLookSummary()
    {
        var master = configuration.MatchMasterPresetStyle ? $", master {configuration.MasterPresetStyleStrength}%" : string.Empty;
        return $"{configuration.Style}, {configuration.PerformanceBudget}, {FormatFirstPartyShaderMode(configuration.FirstPartyShaderMode)}{master}";
    }

    private void DrawUserLook()
    {
        DrawCheckbox("Enable dynamic preset generation", configuration.Enabled, value => configuration.Enabled = value);

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

        var firstPartyMode = (int)configuration.FirstPartyShaderMode;
        if (ImGui.Combo("Dalashade shader mode", ref firstPartyMode, "Supportive / Enhance Base Preset\0Standalone / First-Party Stack\0"))
        {
            configuration.FirstPartyShaderMode = (FirstPartyShaderMode)firstPartyMode;
            configuration.Save();
        }
        DrawItemTooltip("Supportive keeps Dalashade first-party shaders conservative. Standalone lets installed Dalashade first-party shaders carry more of the look while keeping safety gates.");

        DrawCheckbox("Match master preset style", configuration.MatchMasterPresetStyle, value => configuration.MatchMasterPresetStyle = value);
        if (configuration.MatchMasterPresetStyle)
        {
            var masterStrength = configuration.MasterPresetStyleStrength;
            if (ImGui.SliderInt("Master style strength", ref masterStrength, 0, 100))
            {
                configuration.MasterPresetStyleStrength = masterStrength;
                configuration.Save();
            }

            DrawTextInput("Master preset image folder", configuration.MasterPresetFolderPath, value => configuration.MasterPresetFolderPath = value);
        }
    }

    private string UserSceneAwarenessSummary()
    {
        var enabledCount = new[]
        {
            configuration.AutoAdjustInCombat,
            configuration.AutoAdjustAtNight,
            configuration.AutoAdjustForWeather,
            configuration.AutoAdjustForTerritory,
            configuration.AutoAdjustInCutscenes,
            configuration.AutoAdjustFromScreenshots
        }.Count(value => value);

        return $"{enabledCount}/6 adaptive inputs enabled";
    }

    private void DrawUserSceneAwareness()
    {
        DrawCheckbox("Auto-adjust in combat", configuration.AutoAdjustInCombat, value => configuration.AutoAdjustInCombat = value);
        DrawCheckbox("Auto-adjust at night", configuration.AutoAdjustAtNight, value => configuration.AutoAdjustAtNight = value);
        DrawCheckbox("Auto-adjust for weather", configuration.AutoAdjustForWeather, value => configuration.AutoAdjustForWeather = value);
        DrawCheckbox("Auto-adjust for territory type", configuration.AutoAdjustForTerritory, value => configuration.AutoAdjustForTerritory = value);
        DrawCheckbox("Auto-adjust in cutscenes", configuration.AutoAdjustInCutscenes, value => configuration.AutoAdjustInCutscenes = value);
        DrawCheckbox("Use scene authoring overrides", configuration.EnableSceneAuthoringOverrides, value =>
        {
            configuration.EnableSceneAuthoringOverrides = value;
            plugin.RefreshSceneAuthoringState();
        });
        DrawCheckbox("Lock current generated preset", configuration.SceneLockEnabled, value => configuration.SceneLockEnabled = value);
        DrawCheckbox("Auto-adjust from screenshots", configuration.AutoAdjustFromScreenshots, value => configuration.AutoAdjustFromScreenshots = value);
        if (configuration.AutoAdjustFromScreenshots)
        {
            DrawTextInput("Screenshot folder", configuration.ScreenshotFolderPath, value => configuration.ScreenshotFolderPath = value);
            var screenshotStrengthPercent = (int)MathF.Round(configuration.ScreenshotAnalysisStrength * 100f);
            if (ImGui.SliderInt("Screenshot influence", ref screenshotStrengthPercent, 0, 200, "%d%%"))
            {
                configuration.ScreenshotAnalysisStrength = screenshotStrengthPercent / 100f;
                configuration.Save();
            }
        }

        if (ImGui.Button("Open Scene Authoring###ConfigUserOpenSceneAuthoring"))
        {
            plugin.ToggleSceneAuthoringUi();
        }
    }

    private string UserEffectsSummary()
    {
        if (!configuration.EnableDalashadeCustomShaders)
        {
            return "First-party shader variable writes disabled";
        }

        var enabled = new[]
        {
            configuration.EnableDalashadeSceneGIShaderVariables,
            configuration.EnableDalashadeSurfaceReflectionShaderVariables,
            configuration.EnableMaterialIntentShaderMapping,
            configuration.EnableNormalFieldShaderMapping,
            configuration.EnableFirstPartyDepthAssist
        }.Count(value => value);

        return $"{enabled} optional effect systems enabled";
    }

    private void DrawUserEffects()
    {
        DrawCheckbox("Enable Dalashade custom shader variables", configuration.EnableDalashadeCustomShaders, value => configuration.EnableDalashadeCustomShaders = value);
        DrawItemTooltip("Allows Dalashade to write known first-party shader variables into generated presets. Techniques still must be enabled manually in ReShade.");
        DrawCheckbox("Auto-inject known Dalashade shader sections", configuration.AutoInjectDalashadeCustomShaderSections, value => configuration.AutoInjectDalashadeCustomShaderSections = value);
        DrawCheckbox("Enable SceneGI variable writes", configuration.EnableDalashadeSceneGIShaderVariables, value => configuration.EnableDalashadeSceneGIShaderVariables = value);
        DrawCheckbox("Enable SurfaceReflection variable writes", configuration.EnableDalashadeSurfaceReflectionShaderVariables, value => configuration.EnableDalashadeSurfaceReflectionShaderVariables = value);
        DrawCheckbox("Enable depth assist for first-party Dalashade shaders", configuration.EnableFirstPartyDepthAssist, value => configuration.EnableFirstPartyDepthAssist = value);
        DrawItemTooltip("Opt-in helper for first-party shader masks when ReShade depth is reliable. It does not enable techniques.");
        DrawCheckbox("Enable MaterialIntent", configuration.EnableMaterialIntent, value => configuration.EnableMaterialIntent = value);
        DrawCheckbox("Allow MaterialIntent shader variable writes", configuration.EnableMaterialIntentShaderMapping, value => configuration.EnableMaterialIntentShaderMapping = value);
        DrawCheckbox("Enable Normal Field", configuration.EnableNormalField, value => configuration.EnableNormalField = value);
        DrawCheckbox("Enable Normal Field Shader Mapping", configuration.EnableNormalFieldShaderMapping, value => configuration.EnableNormalFieldShaderMapping = value);
        ImGui.TextWrapped("Detailed per-shader strengths, debug modes, and resolver diagnostics are available in Developer Mode.");
    }

    private string UserHealthSummary()
    {
        return $"{PresetAnalyzer.FormatCompatibilityMode(configuration.CompatibilityMode)}, risk {plugin.LastPresetAnalysis.Report.Level}";
    }

    private Vector4? UserHealthWarningColor()
    {
        return plugin.LastPresetAnalysis.Report.Level is PresetRiskLevel.High or PresetRiskLevel.VeryHigh
            ? new Vector4(1.0f, 0.72f, 0.28f, 1.0f)
            : null;
    }

    private void DrawUserHealth()
    {
        DrawSetupItem("Generated at least once", plugin.LastWriteResult.Success);
        DrawSetupItem("Custom shader writes enabled", configuration.EnableDalashadeCustomShaders);
        DrawSetupItem("Generated preset sections can be injected", configuration.AutoInjectDalashadeCustomShaderSections);
        DrawSetupItem("Shader variables detected", plugin.LastShaderSupportScan.Items.Count > 0);
        DrawSetupItem("ReShade.ini found", plugin.LastReloadResult.Diagnostics.ReShadeIniFound);

        if (ImGui.Button("Scan Preset Compatibility###ConfigUserScanPresetCompatibility"))
        {
            plugin.ScanPresetCompatibility();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Debug Bundle###ConfigUserExportDebugBundle"))
        {
            plugin.ExportDebugBundle();
        }

        ImGui.SameLine();
        if (ImGui.Button("Test Reload###ConfigUserTestReload"))
        {
            plugin.ReloadShadersNow();
        }

        ImGui.TextWrapped(plugin.LastPresetAnalysis.Message);
        ImGui.TextWrapped(plugin.LastDiagnosticsExportMessage);
        ImGui.TextWrapped(plugin.LastReloadResult.Message);
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
        var depthAssist = configuration.EnableFirstPartyDepthAssist ? ", first-party depth assist on" : string.Empty;
        return configuration.Enabled
            ? $"{configuration.Style}, {configuration.PerformanceBudget}, writes every {configuration.MinimumSecondsBetweenWrites}s{depthAssist}"
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
        DrawCheckbox("Use scene authoring overrides", configuration.EnableSceneAuthoringOverrides, value =>
        {
            configuration.EnableSceneAuthoringOverrides = value;
            plugin.RefreshSceneAuthoringState();
        });
        DrawCheckbox("Lock current generated preset", configuration.SceneLockEnabled, value => configuration.SceneLockEnabled = value);
        DrawCheckbox("Enable depth assist for first-party Dalashade shaders", configuration.EnableFirstPartyDepthAssist, value => configuration.EnableFirstPartyDepthAssist = value);
        DrawItemTooltip("Requires Dalashade custom shader variable writes. When enabled, generation writes Dalashade_EnableDepthAssist=1 and full depth-assist strength into known first-party Dalashade shader sections that declare those uniforms. This does not enable shader techniques.");
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

        ImGui.TextWrapped("Experimental/inferred: MaterialIntent estimates likely scene material families from tags and screenshot metrics. It is not true FFXIV engine material ID detection.");
        ImGui.TextWrapped("Shader mapping writes MaterialIntent variables only into matching known Dalashade custom shader sections when enabled. Missing uniforms are skipped safely.");
        ImGui.TextWrapped("Debug overlays are shader-owned: install the optional Dalashade .fx files, enable the desired technique manually in ReShade, and select debug modes from the ReShade shader UI.");
    }

    private string NormalFieldSummary()
    {
        if (!configuration.EnableNormalField)
        {
            return "Disabled, no shader writes";
        }

        var mapping = configuration.EnableNormalFieldShaderMapping ? "mapping toggle on" : "diagnostics only";
        return $"Optional screen-space field, strength {configuration.NormalFieldStrength:0.##}, {mapping}";
    }

    private void DrawNormalField()
    {
        DrawCheckbox("Enable Normal Field", configuration.EnableNormalField, value => configuration.EnableNormalField = value);
        DrawItemTooltip("Enables Dalashade's optional screen-space inferred normal/surface field. This is not true game material normals and does not access FFXIV's G-buffer.");

        DrawCheckbox("Enable Normal Field Diagnostics", configuration.EnableNormalFieldDiagnostics, value => configuration.EnableNormalFieldDiagnostics = value);
        DrawCheckbox("Enable Normal Field Shader Mapping", configuration.EnableNormalFieldShaderMapping, value => configuration.EnableNormalFieldShaderMapping = value);
        DrawItemTooltip("Allows Dalashade to write NormalField uniforms into known first-party shader sections. Disabled by default.");

        DrawFloatSlider("Normal Field Strength", configuration.NormalFieldStrength, 0f, 1f, value => configuration.NormalFieldStrength = value);
        DrawItemTooltip("Global scale for the inferred normal field. 0 disables shader-side normal influence even if diagnostics are enabled.");

        DrawFloatSlider("Depth Normal Strength", configuration.NormalFieldDepthStrength, 0f, 1f, value => configuration.NormalFieldDepthStrength = value);
        DrawItemTooltip("Controls coarse surface normals reconstructed from ReShade depth. Requires valid depth to be useful.");

        DrawFloatSlider("Detail Normal Strength", configuration.NormalFieldDetailStrength, 0f, 1f, value => configuration.NormalFieldDetailStrength = value);
        DrawItemTooltip("Controls fake texture/detail normals from screen-space luminance/color gradients. Keep low to avoid embossed shimmer.");

        DrawFloatSlider("Material Influence", configuration.NormalFieldMaterialInfluence, 0f, 1f, value => configuration.NormalFieldMaterialInfluence = value);
        DrawFloatSlider("Water Suppression", configuration.NormalFieldWaterSuppression, 0f, 1f, value => configuration.NormalFieldWaterSuppression = value);
        DrawFloatSlider("Skin Suppression", configuration.NormalFieldSkinSuppression, 0f, 1f, value => configuration.NormalFieldSkinSuppression = value);
        DrawFloatSlider("Sky/Fog Suppression", configuration.NormalFieldSkySuppression, 0f, 1f, value => configuration.NormalFieldSkySuppression = value);

        var debugMode = Math.Clamp(configuration.NormalFieldDebugMode, 0, 12);
        if (ImGui.SliderInt("Normal Field Debug Mode", ref debugMode, 0, 12))
        {
            configuration.NormalFieldDebugMode = debugMode;
            configuration.Save();
        }

        DrawFloatSlider("Normal Field Debug Boost", configuration.NormalFieldDebugBoost, 0.25f, 8f, value => configuration.NormalFieldDebugBoost = value);

        ImGui.TextWrapped("NormalField is optional and inferred from screen-space depth, luma/color gradients, material context, water context, and safety gates. It is not true FFXIV material normal detection.");
        ImGui.TextWrapped("When disabled, NormalField writes are skipped and production shader output is unchanged. Missing shader sections or uniforms are skipped safely.");
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

        ImGui.SameLine();
        if (ImGui.Button("Export Debug Bundle###ConfigExportDebugBundle"))
        {
            plugin.ExportDebugBundle();
        }

        ImGui.TextWrapped(plugin.LastPresetAnalysis.Message);
        ImGui.TextWrapped(plugin.LastDiagnosticsExportMessage);
    }

    private string ShaderMatchingSummary()
    {
        return $"{configuration.ShaderMatchingMode}, inactive writes {configuration.InactiveShaderWriteMode}, custom shaders {(configuration.EnableDalashadeCustomShaders ? "on" : "off")}, mode {FormatFirstPartyShaderMode(configuration.FirstPartyShaderMode)}, injection {(configuration.AutoInjectDalashadeCustomShaderSections ? "on" : "off")}";
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
        var firstPartyMode = (int)configuration.FirstPartyShaderMode;
        if (ImGui.Combo("Dalashade shader mode", ref firstPartyMode, "Supportive / Enhance Base Preset\0Standalone / First-Party Stack\0"))
        {
            configuration.FirstPartyShaderMode = (FirstPartyShaderMode)firstPartyMode;
            configuration.Save();
        }
        DrawItemTooltip("Supportive keeps Dalashade shaders conservative so they enhance an existing preset. Standalone lets Dalashade's first-party shaders carry more of the full visual style when used without a heavy base preset.");
        DrawCheckbox("Auto-inject known Dalashade shader sections into generated preset", configuration.AutoInjectDalashadeCustomShaderSections, value => configuration.AutoInjectDalashadeCustomShaderSections = value);
        ImGui.TextWrapped("When enabled with custom shader variables, Dalashade can add known Dalashade custom shader sections and variables to the generated preset only. The base preset is never modified.");
        ImGui.TextWrapped("This does not install .fx shader files. Install needed Dalashade shaders in ReShade separately so ReShade can compile injected generated-preset sections.");

        ImGui.Separator();
        ImGui.TextWrapped("SceneGI: adaptive screen-space indirect lighting and material bounce");
        DrawCheckbox("Enable SceneGI variable writes", configuration.EnableDalashadeSceneGIShaderVariables, value => configuration.EnableDalashadeSceneGIShaderVariables = value);
        DrawFloatSlider("SceneGI strength", configuration.DalashadeSceneGIStrength, 0f, 1f, value => configuration.DalashadeSceneGIStrength = value);
        DrawFloatSlider("SceneGI AO intensity", configuration.DalashadeSceneGIAOIntensity, 0f, 1f, value => configuration.DalashadeSceneGIAOIntensity = value);
        DrawFloatSlider("SceneGI bounce strength", configuration.DalashadeSceneGIBounceStrength, 0f, 1f, value => configuration.DalashadeSceneGIBounceStrength = value);
        DrawFloatSlider("SceneGI night light strength", configuration.DalashadeSceneGINightLightStrength, 0f, 1f, value => configuration.DalashadeSceneGINightLightStrength = value);
        DrawFloatSlider("SceneGI material influence", configuration.DalashadeSceneGIMaterialInfluence, 0f, 1f, value => configuration.DalashadeSceneGIMaterialInfluence = value);
        var sceneGIDebugMode = configuration.DalashadeSceneGIDebugMode;
        if (ImGui.SliderInt("SceneGI debug mode", ref sceneGIDebugMode, 0, 14))
        {
            configuration.DalashadeSceneGIDebugMode = sceneGIDebugMode;
            configuration.Save();
        }
        var sceneGIDebugOutputMode = configuration.DalashadeSceneGIDebugOutputMode;
        if (ImGui.SliderInt("SceneGI debug output mode", ref sceneGIDebugOutputMode, 0, 4))
        {
            configuration.DalashadeSceneGIDebugOutputMode = sceneGIDebugOutputMode;
            configuration.Save();
        }
        DrawFloatSlider("SceneGI debug opacity", configuration.DalashadeSceneGIDebugOpacity, 0f, 1f, value => configuration.DalashadeSceneGIDebugOpacity = value);
        DrawFloatSlider("SceneGI debug boost", configuration.DalashadeSceneGIDebugBoost, 0.25f, 8f, value => configuration.DalashadeSceneGIDebugBoost = value);
        ImGui.TextWrapped("SceneGI variable writes require Dalashade custom shader variables and matching generated preset keys. The SceneGI technique is never auto-enabled; enable it manually in ReShade after installing the .fx file.");

        ImGui.Separator();
        ImGui.TextWrapped("SurfaceReflection: material-aware water, wetness, and glint response");
        DrawCheckbox("Enable SurfaceReflection variable writes", configuration.EnableDalashadeSurfaceReflectionShaderVariables, value => configuration.EnableDalashadeSurfaceReflectionShaderVariables = value);
        DrawFloatSlider("SurfaceReflection strength", configuration.DalashadeSurfaceReflectionStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionStrength = value);
        DrawFloatSlider("SurfaceReflection water sheen", configuration.DalashadeSurfaceReflectionWaterSheenStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionWaterSheenStrength = value);
        DrawFloatSlider("SurfaceReflection specular glint", configuration.DalashadeSurfaceReflectionSpecularGlintStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionSpecularGlintStrength = value);
        DrawFloatSlider("SurfaceReflection wet response", configuration.DalashadeSurfaceReflectionWetStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionWetStrength = value);
        DrawFloatSlider("SurfaceReflection aether/neon response", configuration.DalashadeSurfaceReflectionAetherNeonStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionAetherNeonStrength = value);
        var surfaceReflectionDebugMode = configuration.DalashadeSurfaceReflectionDebugMode;
        if (ImGui.SliderInt("SurfaceReflection debug mode", ref surfaceReflectionDebugMode, 0, 14))
        {
            configuration.DalashadeSurfaceReflectionDebugMode = surfaceReflectionDebugMode;
            configuration.Save();
        }
        DrawFloatSlider("SurfaceReflection debug opacity", configuration.DalashadeSurfaceReflectionDebugOpacity, 0f, 1f, value => configuration.DalashadeSurfaceReflectionDebugOpacity = value);
        ImGui.TextWrapped("SurfaceReflection is a separate optional first-party shader for water sheen, wet glints, and material-aware reflection impressions. The technique is never auto-enabled.");
        ImGui.TextWrapped("Debug modes show inferred shader-side masks, not true engine material IDs.");
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
            ? $"{configuration.ImageSamplingMode}, {configuration.ScreenshotAnalysisStrength * 100f:0}% strength, every {configuration.MinimumSecondsBetweenImageSamples}s"
            : "Disabled";
    }

    private void DrawScreenshotAnalysis()
    {
        DrawCheckbox("Auto-adjust from screenshots", configuration.AutoAdjustFromScreenshots, value => configuration.AutoAdjustFromScreenshots = value);
        DrawTextInput("Screenshot folder", configuration.ScreenshotFolderPath, value => configuration.ScreenshotFolderPath = value);
        var screenshotStrengthPercent = (int)MathF.Round(configuration.ScreenshotAnalysisStrength * 100f);
        if (ImGui.SliderInt("Screenshot influence", ref screenshotStrengthPercent, 0, 200, "%d%%"))
        {
            configuration.ScreenshotAnalysisStrength = screenshotStrengthPercent / 100f;
            configuration.Save();
        }

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
        ImGui.TextWrapped(plugin.LastDiagnosticsExportMessage);
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

    private static void DrawItemTooltip(string text)
    {
        if (!ImGui.IsItemHovered())
        {
            return;
        }

        ImGui.BeginTooltip();
        ImGui.TextWrapped(text);
        ImGui.EndTooltip();
    }

    private static void DrawSetupItem(string label, bool complete)
    {
        ImGui.BulletText($"{(complete ? "OK" : "Missing")} - {label}");
    }

    private static string FormatFirstPartyShaderMode(FirstPartyShaderMode mode)
    {
        return mode == FirstPartyShaderMode.Standalone ? "Standalone" : "Supportive";
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
