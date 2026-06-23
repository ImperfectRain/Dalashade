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
            DeveloperPage("ConfigDalapad", "Dalapad", false, DalapadSummary, DrawDalapadDiagnostics),
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
                "Generate",
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
                "Adaptation",
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
        ImGui.Separator();
        DrawUserGenerationControls("ConfigUserSetup");

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
        DrawCheckbox("Apply base polish", configuration.EnableBasePolish, value => configuration.EnableBasePolish = value);
        ImGui.TextWrapped("Base polish is the small default contrast, saturation, clarity, bloom, and shadow-lift pass before scene-specific adjustments.");

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
            configuration.AutoAdjustFromScreenshots,
            configuration.EnableScreenshotMaterialEvidenceInfluence
        }.Count(value => value);

        return $"{enabledCount}/7 adaptive inputs enabled";
    }

    private void DrawUserSceneAwareness()
    {
        ImGui.TextWrapped("Choose which scene signals are allowed to change the generated look.");
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
        DrawCheckbox("Use screenshot material hints", configuration.EnableScreenshotMaterialEvidenceInfluence, value =>
        {
            configuration.EnableScreenshotMaterialEvidenceInfluence = value;
            if (value)
            {
                configuration.EnableMaterialIntent = true;
                configuration.EnableMaterialIntentDiagnostics = true;
            }
        });
        ImGui.TextWrapped("Uses recent screenshots to gently improve scene material guesses such as foliage, water, sand, and snow. It does not identify true game materials.");
        if (configuration.EnableScreenshotMaterialEvidenceInfluence)
        {
            DrawFloatSlider("Screenshot material hint strength###ConfigUserScreenshotMaterialHintStrength", configuration.ScreenshotMaterialEvidenceStrength, 0f, 0.6f, value => configuration.ScreenshotMaterialEvidenceStrength = value);
            ImGui.TextWrapped("Scene-level hint only. UI-heavy screenshots can be wrong; missing screenshots produce neutral evidence.");
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
            configuration.EnableDalashadeSceneGIContactShadows,
            configuration.EnableDalashadeScreenShadowsShaderVariables,
            configuration.EnableDalashadeContactToneShaderVariables,
            configuration.EnableDalashadeSurfaceReflectionShaderVariables,
            configuration.EnableMaterialIntentShaderMapping,
            configuration.EnableNormalFieldShaderMapping,
            configuration.EnableFirstPartyDepthAssist,
            configuration.EnableDalapadShaderIntegration && configuration.EnableDalapadSurfaceData
        }.Count(value => value);

        return $"{enabled} optional effect systems enabled";
    }

    private void DrawUserEffects()
    {
        DrawCheckbox("Enable first-party Dalashade shader support", configuration.EnableDalashadeCustomShaders, value => configuration.EnableDalashadeCustomShaders = value);
        DrawItemTooltip("Allows Dalashade to write known variables for Dalashade first-party shaders into the generated preset. It does not edit third-party shaders.");
        DrawCheckbox("Allow Dalapad data in first-party shaders", configuration.EnableDalapadShaderIntegration, value => configuration.EnableDalapadShaderIntegration = value);
        DrawItemTooltip("Global opt-in for production shader features that can consume gated Dalapad bridge textures. Debug visualization and layer-copy tests are separate developer tools.");
        if (configuration.EnableDalapadShaderIntegration)
        {
            DrawCheckbox("Use Dalapad surface candidates in first-party effects", configuration.EnableDalapadSurfaceData, value => configuration.EnableDalapadSurfaceData = value);
            DrawFloatSlider("Dalapad surface data strength", configuration.DalapadSurfaceDataStrength, 0f, 1f, value => configuration.DalapadSurfaceDataStrength = value);
        }
        else
        {
            ImGui.TextWrapped("Dalapad surface candidates stay neutral until production Dalapad shader support is enabled.");
        }
        DrawCheckbox("Auto-inject known Dalashade shader sections", configuration.AutoInjectDalashadeCustomShaderSections, value => configuration.AutoInjectDalashadeCustomShaderSections = value);
        DrawItemTooltip("Adds missing known Dalashade first-party shader sections to the generated preset only. The base preset is not modified.");
        DrawCheckbox("Sync Dalashade technique activation", configuration.SyncDalashadeTechniqueActivation, value => configuration.SyncDalashadeTechniqueActivation = value);
        DrawItemTooltip("Optional generated-preset management for production Dalashade techniques. Manual debug shaders, including Dalapad_Debug, are not auto-enabled.");
        DrawCheckbox("SceneGI first-party controls", configuration.EnableDalashadeSceneGIShaderVariables, value => configuration.EnableDalashadeSceneGIShaderVariables = value);
        DrawCheckbox("SceneGI contact-shadow assist", configuration.EnableDalashadeSceneGIContactShadows, value => configuration.EnableDalashadeSceneGIContactShadows = value);
        DrawCheckbox("ScreenShadows first-party controls", configuration.EnableDalashadeScreenShadowsShaderVariables, value => configuration.EnableDalashadeScreenShadowsShaderVariables = value);
        DrawCheckbox("ContactTone first-party controls", configuration.EnableDalashadeContactToneShaderVariables, value => configuration.EnableDalashadeContactToneShaderVariables = value);
        DrawCheckbox("SurfaceReflection first-party controls", configuration.EnableDalashadeSurfaceReflectionShaderVariables, value => configuration.EnableDalashadeSurfaceReflectionShaderVariables = value);
        DrawCheckbox("Enable depth assist for first-party Dalashade shaders", configuration.EnableFirstPartyDepthAssist, value => configuration.EnableFirstPartyDepthAssist = value);
        DrawItemTooltip("Opt-in helper for first-party shader masks when ReShade depth is reliable. It does not enable techniques.");
        DrawCheckbox("Allow inferred material hints in first-party shaders", configuration.EnableMaterialIntentShaderMapping, value =>
        {
            configuration.EnableMaterialIntentShaderMapping = value;
            if (value)
            {
                configuration.EnableMaterialIntent = true;
                configuration.EnableMaterialIntentDiagnostics = true;
            }
        });
        DrawCheckbox("Enable inferred Normal Field", configuration.EnableNormalField, value => configuration.EnableNormalField = value);
        DrawCheckbox("Allow Normal Field in first-party shaders", configuration.EnableNormalFieldShaderMapping, value => configuration.EnableNormalFieldShaderMapping = value);
        if (ImGui.Button("Turn Off Optional First-Party Shader Assists###ConfigUserDisableOptionalFirstParty"))
        {
            DisableOptionalFirstPartySystems();
        }
        ImGui.TextWrapped("These controls write generated-preset values for Dalashade first-party shaders. ReShade still owns shader compile state, final order, and manual debug techniques.");
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

        var diagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(configuration, plugin.LastShaderSupportScan, plugin.LastWriteResult, plugin.LastPresetAnalysis);
        ImGui.Separator();
        ImGui.TextUnformatted("Next steps");
        foreach (var step in BuildUserHealthNextSteps(diagnostics).Take(8))
        {
            ImGui.BulletText(step);
        }
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

        DrawCheckbox("Apply base polish", configuration.EnableBasePolish, value => configuration.EnableBasePolish = value);

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
        var evidence = configuration.EnableScreenshotMaterialEvidenceInfluence ? $", screenshot evidence {configuration.ScreenshotMaterialEvidenceStrength:0.##}" : string.Empty;
        return $"Experimental inferred materials, strength {configuration.MaterialIntentStrength:0.##}, {mapping}{evidence}";
    }

    private void DrawMaterialIntent()
    {
        DrawCheckbox("Enable inferred MaterialIntent", configuration.EnableMaterialIntent, value => configuration.EnableMaterialIntent = value);
        DrawCheckbox("Show MaterialIntent diagnostics in reports/UI", configuration.EnableMaterialIntentDiagnostics, value => configuration.EnableMaterialIntentDiagnostics = value);
        DrawCheckbox("Allow inferred MaterialIntent in first-party shaders", configuration.EnableMaterialIntentShaderMapping, value => configuration.EnableMaterialIntentShaderMapping = value);
        DrawFloatSlider("MaterialIntent strength", configuration.MaterialIntentStrength, 0f, 1f, value => configuration.MaterialIntentStrength = value);
        DrawCheckbox("Let screenshot material evidence influence MaterialIntent", configuration.EnableScreenshotMaterialEvidenceInfluence, value => configuration.EnableScreenshotMaterialEvidenceInfluence = value);
        DrawFloatSlider("Screenshot material evidence strength", configuration.ScreenshotMaterialEvidenceStrength, 0f, 1f, value => configuration.ScreenshotMaterialEvidenceStrength = value);

        ImGui.TextWrapped("Experimental/inferred: MaterialIntent estimates likely scene material families from tags and screenshot metrics. It is not true FFXIV engine material ID detection.");
        ImGui.TextWrapped("Screenshot material evidence is off by default. When enabled, it contributes capped scene-level priors only; shader-side FrameData and MaterialMasks still decide per-pixel behavior.");
        ImGui.TextWrapped("Screenshot evidence can be wrong with UI-heavy screenshots. Missing screenshots produce neutral evidence and do not crash generation.");
        ImGui.TextWrapped("Screenshot material evidence caps at full strength: foliage/grass +0.22, water +0.16, sand +0.16, snow +0.18, stone +0.14, metal +0.12, aether/neon +0.14.");
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
        DrawCheckbox("Enable inferred Normal Field", configuration.EnableNormalField, value => configuration.EnableNormalField = value);
        DrawItemTooltip("Enables Dalashade's optional screen-space inferred normal/surface field. This is not true game material normals and does not access FFXIV's G-buffer.");

        DrawCheckbox("Enable Normal Field Diagnostics", configuration.EnableNormalFieldDiagnostics, value => configuration.EnableNormalFieldDiagnostics = value);
        DrawCheckbox("Allow inferred Normal Field in first-party shaders", configuration.EnableNormalFieldShaderMapping, value => configuration.EnableNormalFieldShaderMapping = value);
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

        var debugMode = Math.Clamp(configuration.NormalFieldDebugMode, 0, 20);
        if (ImGui.SliderInt("Debug mode (Normal Field)", ref debugMode, 0, 20))
        {
            configuration.NormalFieldDebugMode = debugMode;
            configuration.Save();
        }

        DrawFloatSlider("Debug boost (Normal Field)", configuration.NormalFieldDebugBoost, 0.25f, 8f, value => configuration.NormalFieldDebugBoost = value);

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
        return $"{configuration.ShaderMatchingMode}, inactive writes {configuration.InactiveShaderWriteMode}, load order {(configuration.OptimizeGeneratedPresetLoadOrder ? "on" : "off")}, custom shaders {(configuration.EnableDalashadeCustomShaders ? "on" : "off")}, mode {FormatFirstPartyShaderMode(configuration.FirstPartyShaderMode)}, injection {(configuration.AutoInjectDalashadeCustomShaderSections ? "on" : "off")}, technique sync {(configuration.SyncDalashadeTechniqueActivation ? "on" : "off")}";
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

        DrawCheckbox("Optimize generated preset load order", configuration.OptimizeGeneratedPresetLoadOrder, value => configuration.OptimizeGeneratedPresetLoadOrder = value);
        ImGui.TextWrapped("When enabled, Dalashade reorders Techniques= and TechniqueSorting= in the generated preset only. It preserves the same entries and does not enable or disable effects.");

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
        DrawCheckbox("Sync Dalashade technique activation", configuration.SyncDalashadeTechniqueActivation, value => configuration.SyncDalashadeTechniqueActivation = value);
        ImGui.TextWrapped("When enabled, generated presets add or remove Dalashade production techniques from Techniques= based on the plugin shader options. Debug techniques stay manual, and third-party effects are not disabled.");
        DrawCheckbox("Enable Dalapad production shader assist", configuration.EnableDalapadShaderIntegration, value => configuration.EnableDalapadShaderIntegration = value);
        DrawItemTooltip("Global opt-in for shader features that consume Dalapad bridge textures. When off, Dalapad-specific generated-preset values resolve to disabling values.");
        if (configuration.EnableDalapadShaderIntegration)
        {
            DrawCheckbox("Use Dalapad semantic surface candidates in first-party shaders", configuration.EnableDalapadSurfaceData, value => configuration.EnableDalapadSurfaceData = value);
            DrawFloatSlider("Dalapad surface data strength", configuration.DalapadSurfaceDataStrength, 0f, 1f, value => configuration.DalapadSurfaceDataStrength = value);
        }
        else
        {
            ImGui.TextWrapped("Dalapad surface data resolves to zero until Dalapad shader additions are enabled.");
        }

        ImGui.Separator();
        ImGui.TextWrapped("SceneGI: adaptive screen-space indirect lighting and material bounce");
        DrawCheckbox("Enable SceneGI variable writes", configuration.EnableDalashadeSceneGIShaderVariables, value => configuration.EnableDalashadeSceneGIShaderVariables = value);
        DrawFloatSlider("SceneGI strength", configuration.DalashadeSceneGIStrength, 0f, 1f, value => configuration.DalashadeSceneGIStrength = value);
        DrawFloatSlider("SceneGI AO intensity", configuration.DalashadeSceneGIAOIntensity, 0f, 1f, value => configuration.DalashadeSceneGIAOIntensity = value);
        DrawFloatSlider("SceneGI bounce strength", configuration.DalashadeSceneGIBounceStrength, 0f, 1f, value => configuration.DalashadeSceneGIBounceStrength = value);
        DrawFloatSlider("SceneGI night light strength", configuration.DalashadeSceneGINightLightStrength, 0f, 1f, value => configuration.DalashadeSceneGINightLightStrength = value);
        DrawFloatSlider("SceneGI material influence", configuration.DalashadeSceneGIMaterialInfluence, 0f, 1f, value => configuration.DalashadeSceneGIMaterialInfluence = value);
        DrawCheckbox("Enable SceneGI contact shadows", configuration.EnableDalashadeSceneGIContactShadows, value => configuration.EnableDalashadeSceneGIContactShadows = value);
        DrawFloatSlider("SceneGI contact shadow strength", configuration.DalashadeSceneGIContactShadowStrength, 0f, 1f, value => configuration.DalashadeSceneGIContactShadowStrength = value);
        DrawFloatSlider("SceneGI contact shadow radius", configuration.DalashadeSceneGIContactShadowRadius, 0.2f, 2f, value => configuration.DalashadeSceneGIContactShadowRadius = value);
        DrawFloatSlider("SceneGI contact shadow softness", configuration.DalashadeSceneGIContactShadowSoftness, 0f, 1f, value => configuration.DalashadeSceneGIContactShadowSoftness = value);
        ImGui.TextWrapped("SceneGI uses Dalapad surface data through FrameData when the global Dalapad surface-data option is enabled.");
        var sceneGIDebugMode = configuration.DalashadeSceneGIDebugMode;
        if (ImGui.SliderInt("Debug mode (SceneGI)", ref sceneGIDebugMode, 0, 22))
        {
            configuration.DalashadeSceneGIDebugMode = sceneGIDebugMode;
            configuration.Save();
        }
        var sceneGIDebugOutputMode = configuration.DalashadeSceneGIDebugOutputMode;
        if (ImGui.SliderInt("Debug output (SceneGI)", ref sceneGIDebugOutputMode, 0, 4))
        {
            configuration.DalashadeSceneGIDebugOutputMode = sceneGIDebugOutputMode;
            configuration.Save();
        }
        DrawFloatSlider("Debug opacity (SceneGI)", configuration.DalashadeSceneGIDebugOpacity, 0f, 1f, value => configuration.DalashadeSceneGIDebugOpacity = value);
        DrawFloatSlider("Debug boost (SceneGI)", configuration.DalashadeSceneGIDebugBoost, 0.25f, 8f, value => configuration.DalashadeSceneGIDebugBoost = value);
        ImGui.TextWrapped("SceneGI contact shadows are local AO/contact grounding. ScreenShadows below is the separate source-aware shadow technique.");
        ImGui.TextWrapped("SceneGI variable writes require Dalashade custom shader variables and matching generated preset keys. If technique sync is enabled, SceneGI is added to the generated preset when these writes are enabled.");

        ImGui.Separator();
        ImGui.TextWrapped("ScreenShadows: optional source-aware screen-space shadow impressions");
        DrawCheckbox("Enable ScreenShadows variable writes", configuration.EnableDalashadeScreenShadowsShaderVariables, value => configuration.EnableDalashadeScreenShadowsShaderVariables = value);
        DrawFloatSlider("ScreenShadows strength", configuration.DalashadeScreenShadowsStrength, 0f, 1f, value => configuration.DalashadeScreenShadowsStrength = value);
        DrawFloatSlider("ScreenShadows reach", configuration.DalashadeScreenShadowsReach, 0.2f, 2f, value => configuration.DalashadeScreenShadowsReach = value);
        DrawFloatSlider("ScreenShadows softness", configuration.DalashadeScreenShadowsSoftness, 0f, 1f, value => configuration.DalashadeScreenShadowsSoftness = value);
        DrawFloatSlider("ScreenShadows source sensitivity", configuration.DalashadeScreenShadowsSourceSensitivity, 0f, 1f, value => configuration.DalashadeScreenShadowsSourceSensitivity = value);
        DrawFloatSlider("ScreenShadows Dalapad assist", configuration.DalashadeScreenShadowsDalapadInfluence, 0f, 1f, value => configuration.DalashadeScreenShadowsDalapadInfluence = value);
        var screenShadowsDebugMode = configuration.DalashadeScreenShadowsDebugMode;
        if (ImGui.SliderInt("Debug mode (ScreenShadows)", ref screenShadowsDebugMode, 0, 6))
        {
            configuration.DalashadeScreenShadowsDebugMode = screenShadowsDebugMode;
            configuration.Save();
        }
        var screenShadowsDebugOutputMode = configuration.DalashadeScreenShadowsDebugOutputMode;
        if (ImGui.SliderInt("Debug output (ScreenShadows)", ref screenShadowsDebugOutputMode, 0, 4))
        {
            configuration.DalashadeScreenShadowsDebugOutputMode = screenShadowsDebugOutputMode;
            configuration.Save();
        }
        DrawFloatSlider("Debug opacity (ScreenShadows)", configuration.DalashadeScreenShadowsDebugOpacity, 0f, 1f, value => configuration.DalashadeScreenShadowsDebugOpacity = value);
        DrawFloatSlider("Debug boost (ScreenShadows)", configuration.DalashadeScreenShadowsDebugBoost, 0.25f, 8f, value => configuration.DalashadeScreenShadowsDebugBoost = value);
        ImGui.TextWrapped("ScreenShadows is separate from SceneGI contact shadows. It approximates visible-source cast shadows in screen space and stays neutral when disabled or when Dalapad/depth data is missing.");

        ImGui.Separator();
        ImGui.TextWrapped("ContactTone: local grounding, contact shadows, and readability contrast");
        DrawCheckbox("Enable ContactTone variable writes", configuration.EnableDalashadeContactToneShaderVariables, value => configuration.EnableDalashadeContactToneShaderVariables = value);
        DrawFloatSlider("ContactTone strength", configuration.DalashadeContactToneStrength, 0f, 1f, value => configuration.DalashadeContactToneStrength = value);
        DrawFloatSlider("ContactTone radius", configuration.DalashadeContactToneRadius, 0.20f, 2.0f, value => configuration.DalashadeContactToneRadius = value);
        DrawFloatSlider("ContactTone depth edge", configuration.DalashadeContactToneEdgeStrength, 0f, 1f, value => configuration.DalashadeContactToneEdgeStrength = value);
        DrawFloatSlider("ContactTone structure", configuration.DalashadeContactToneStructureStrength, 0f, 1f, value => configuration.DalashadeContactToneStructureStrength = value);
        DrawFloatSlider("ContactTone local contrast", configuration.DalashadeContactToneContrastStrength, 0f, 1f, value => configuration.DalashadeContactToneContrastStrength = value);
        var contactToneDebugMode = configuration.DalashadeContactToneDebugMode;
        if (ImGui.SliderInt("Debug mode (ContactTone)", ref contactToneDebugMode, 0, 6))
        {
            configuration.DalashadeContactToneDebugMode = contactToneDebugMode;
            configuration.Save();
        }
        DrawFloatSlider("Debug opacity (ContactTone)", configuration.DalashadeContactToneDebugOpacity, 0f, 1f, value => configuration.DalashadeContactToneDebugOpacity = value);
        ImGui.TextWrapped("ContactTone is separate from GI: it darkens and clarifies contact edges and grounded material transitions, without color bounce, emissive pooling, reflections, or atmospheric bloom. If technique sync is enabled, it follows this variable-write option.");

        ImGui.Separator();
        ImGui.TextWrapped("SurfaceReflection: material-aware water, wetness, and glint response");
        DrawCheckbox("Enable SurfaceReflection variable writes", configuration.EnableDalashadeSurfaceReflectionShaderVariables, value => configuration.EnableDalashadeSurfaceReflectionShaderVariables = value);
        DrawFloatSlider("SurfaceReflection strength", configuration.DalashadeSurfaceReflectionStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionStrength = value);
        DrawFloatSlider("SurfaceReflection water sheen", configuration.DalashadeSurfaceReflectionWaterSheenStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionWaterSheenStrength = value);
        DrawFloatSlider("SurfaceReflection specular glint", configuration.DalashadeSurfaceReflectionSpecularGlintStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionSpecularGlintStrength = value);
        DrawFloatSlider("SurfaceReflection wet response", configuration.DalashadeSurfaceReflectionWetStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionWetStrength = value);
        DrawFloatSlider("SurfaceReflection aether/neon response", configuration.DalashadeSurfaceReflectionAetherNeonStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionAetherNeonStrength = value);
        var surfaceReflectionDebugMode = configuration.DalashadeSurfaceReflectionDebugMode;
        if (ImGui.SliderInt("Debug mode (SurfaceReflection)", ref surfaceReflectionDebugMode, 0, 14))
        {
            configuration.DalashadeSurfaceReflectionDebugMode = surfaceReflectionDebugMode;
            configuration.Save();
        }
        DrawFloatSlider("Debug opacity (SurfaceReflection)", configuration.DalashadeSurfaceReflectionDebugOpacity, 0f, 1f, value => configuration.DalashadeSurfaceReflectionDebugOpacity = value);
        ImGui.TextWrapped("Debug controls are developer diagnostics. Current sliders write the same integer modes exposed by the shader docs; named dropdown metadata is tracked as a future parity cleanup.");
        ImGui.TextWrapped("SurfaceReflection is a separate optional first-party shader for water sheen, wet glints, and material-aware reflection impressions. If technique sync is enabled, it follows this variable-write option.");
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

    private string DalapadSummary()
    {
        var diagnostics = plugin.CurrentDalapadDiagnostics;
        return diagnostics.Probed
            ? $"{diagnostics.Status}: {diagnostics.Summary}"
            : "Diagnostic-only external surface-data probe has not run yet";
    }

    private void DrawDalapadDiagnostics()
    {
        var diagnostics = plugin.CurrentDalapadDiagnostics;
        ImGui.TextWrapped("Dalapad is an experimental optional surface-data bridge. The default diagnostic pass checks runtime metadata, status-file IPC, and control-pipe capability negotiation; addon debug visualization and first-party shader use are separate opt-in paths gated by Dalapad shader additions.");
        ImGui.Spacing();
        if (ImGui.Button("Probe Dalapad diagnostics"))
        {
            diagnostics = plugin.RefreshDalapadDiagnostics();
        }

        ImGui.Spacing();
        DrawCheckbox("Enable developer-only resource shape probe", configuration.EnableDalapadResourceShapeProbe, value => configuration.EnableDalapadResourceShapeProbe = value);
        ImGui.TextWrapped("This opt-in probe may invoke RenderTargetManager.Instance and report redacted candidate resource shape, nullability, dimensions, and format labels if readable. It does not copy textures, register shader resources, move IPC handles, or affect FrameData.");
        if (ImGui.Button("Run Dalapad shape probe"))
        {
            diagnostics = plugin.RefreshDalapadDiagnostics(configuration.EnableDalapadResourceShapeProbe);
        }

        ImGui.Spacing();
        ImGui.TextUnformatted($"Status: {diagnostics.Status}");
        ImGui.TextWrapped(diagnostics.Summary);
        ImGui.TextUnformatted($"Probed: {(diagnostics.Probed ? "yes" : "no")}");
        if (diagnostics.ProbeTimestamp != DateTimeOffset.MinValue)
        {
            ImGui.TextUnformatted($"Probe timestamp: {diagnostics.ProbeTimestamp:O}");
        }

        ImGui.TextUnformatted($"Runtime assembly: {FormatOptionalUiValue(diagnostics.RuntimeAssembly)}");
        ImGui.TextUnformatted($"RenderTargetManager type: {FormatOptionalUiValue(diagnostics.RenderTargetManagerTypeName)}");
        ImGui.TextUnformatted($"RenderTargetManager type found: {FormatYesNo(diagnostics.RenderTargetManagerTypeFound)}");
        ImGui.TextUnformatted($"Instance metadata found: {FormatYesNo(diagnostics.InstanceMethodFound)}");
        ImGui.TextUnformatted($"GBuffer metadata found: {FormatYesNo(diagnostics.GBufferMemberFound)}");
        ImGui.TextUnformatted($"DepthStencil metadata found: {FormatYesNo(diagnostics.DepthStencilMemberFound)}");
        ImGui.TextUnformatted($"Texture metadata found: {FormatYesNo(diagnostics.TextureTypeFound)}");
        ImGui.TextUnformatted($"Addon contract: {diagnostics.AddonContractVersion}");
        ImGui.TextUnformatted($"IPC contract: {diagnostics.IpcContractVersion}");

        ImGui.Spacing();
        ImGui.TextUnformatted("Capabilities");
        foreach (var capability in diagnostics.Capabilities)
        {
            ImGui.BulletText($"{capability.Name}: {FormatYesNo(capability.Available)} - {capability.Detail}");
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Addon resource contract");
        foreach (var resource in diagnostics.AddonResources)
        {
            ImGui.BulletText($"{resource.Name}: {resource.Kind}, flag {resource.AvailabilityFlag}");
            ImGui.TextWrapped($"Source: {resource.ExpectedSource}");
            ImGui.TextWrapped($"Diagnostic use: {resource.DiagnosticOnlyUse}");
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("IPC status");
        ImGui.TextUnformatted($"Status: {diagnostics.IpcStatus.Status}");
        ImGui.TextWrapped(diagnostics.IpcStatus.Summary);
        ImGui.TextUnformatted($"Status file: {FormatOptionalUiValue(diagnostics.IpcStatus.StatusFilePath)}");
        ImGui.TextUnformatted($"Status file found: {FormatYesNo(diagnostics.IpcStatus.StatusFileFound)}");
        ImGui.TextUnformatted($"Bridge reported: {FormatYesNo(diagnostics.IpcStatus.BridgeReported)}");
        ImGui.TextUnformatted($"Contract compatible: {FormatYesNo(diagnostics.IpcStatus.ContractCompatible)}");
        ImGui.TextUnformatted($"Bridge version: {FormatOptionalUiValue(diagnostics.IpcStatus.BridgeVersion)}");
        ImGui.TextUnformatted($"Addon process: {FormatOptionalUiValue(diagnostics.IpcStatus.AddonProcess)}");
        if (diagnostics.IpcStatus.LastUpdateUtc.HasValue)
        {
            ImGui.TextUnformatted($"Last update: {diagnostics.IpcStatus.LastUpdateUtc.Value:O}");
        }

        if (diagnostics.IpcStatus.ReportedResources.Count > 0)
        {
            ImGui.TextWrapped($"Reported resources: {string.Join(", ", diagnostics.IpcStatus.ReportedResources)}");
        }

        DrawResourceCatalog("Status-file resource catalog", diagnostics.IpcStatus.ResourceCatalog);

        foreach (var warning in diagnostics.IpcStatus.Warnings)
        {
            ImGui.BulletText($"Warning: {warning}");
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Control pipe health");
        ImGui.TextUnformatted($"Pipe: {diagnostics.ControlPipeStatus.PipeName}");
        ImGui.TextUnformatted($"Attempted: {FormatYesNo(diagnostics.ControlPipeStatus.Attempted)}");
        ImGui.TextUnformatted($"Listening: {FormatYesNo(diagnostics.ControlPipeStatus.PipeListening)}");
        ImGui.TextUnformatted($"Response received: {FormatYesNo(diagnostics.ControlPipeStatus.ResponseReceived)}");
        ImGui.TextUnformatted($"Contract compatible: {FormatYesNo(diagnostics.ControlPipeStatus.ContractCompatible)}");
        ImGui.TextUnformatted($"Status: {diagnostics.ControlPipeStatus.Status}");
        ImGui.TextWrapped(diagnostics.ControlPipeStatus.Summary);
        ImGui.TextUnformatted($"Bridge version: {FormatOptionalUiValue(diagnostics.ControlPipeStatus.BridgeVersion)}");
        ImGui.TextUnformatted($"Response type: {FormatOptionalUiValue(diagnostics.ControlPipeStatus.ResponseType)}");
        ImGui.TextUnformatted($"Elapsed: {diagnostics.ControlPipeStatus.ElapsedMilliseconds} ms");
        ImGui.TextUnformatted($"Supports status file: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsStatusFile)}");
        ImGui.TextUnformatted($"Supports control pipe: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsControlPipe)}");
        ImGui.TextUnformatted($"Supports realtime uniforms: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsRealtimeUniforms)}");
        ImGui.TextUnformatted($"Supports resource catalog: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsResourceCatalog)}");
        ImGui.TextUnformatted($"Supports debug visualization: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.SupportsDebugVisualization)}");
        ImGui.TextUnformatted($"Reads render targets: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.ReadsRenderTargets)}");
        ImGui.TextUnformatted($"Copies render targets: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.CopiesRenderTargets)}");
        ImGui.TextUnformatted($"Registers shader resources: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.RegistersShaderResources)}");
        ImGui.TextUnformatted($"Moves realtime shader values: {FormatYesNo(diagnostics.ControlPipeStatus.Capabilities.MovesRealtimeShaderValues)}");
        DrawResourceCatalog("Control-pipe resource catalog", diagnostics.ControlPipeStatus.ResourceCatalog);
        DrawDebugVisualization("Status-file debug visualization", diagnostics.IpcStatus.DebugVisualization);
        DrawDebugVisualization("Control-pipe debug visualization", diagnostics.ControlPipeStatus.DebugVisualization);
        foreach (var warning in diagnostics.ControlPipeStatus.Warnings)
        {
            ImGui.BulletText($"Pipe warning: {warning}");
        }

        DrawResourceShapeProbe(diagnostics.ResourceShapeProbe);

        ImGui.Spacing();
        ImGui.TextUnformatted("Health check next steps");
        foreach (var step in BuildDalapadHealthNextSteps(diagnostics))
        {
            ImGui.BulletText(step);
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("IPC endpoints");
        foreach (var endpoint in diagnostics.IpcEndpoints)
        {
            ImGui.BulletText($"{endpoint.Name}: {endpoint.Kind}, {endpoint.Direction}");
            ImGui.TextWrapped($"Address: {endpoint.Address}");
            ImGui.TextWrapped($"Purpose: {endpoint.Purpose}");
            ImGui.TextWrapped($"Safety: {endpoint.SafetyBoundary}");
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Realtime adaptation groundwork");
        foreach (var channel in diagnostics.RealtimeChannels)
        {
            ImGui.BulletText($"{channel.Name}: {channel.Direction}, {channel.Priority}");
            ImGui.TextWrapped($"Payload: {channel.Payload}");
            ImGui.TextWrapped($"Safety: {channel.SafetyBoundary}");
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Diagnostic routes");
        foreach (var route in diagnostics.DiagnosticRoutes)
        {
            ImGui.BulletText($"{route.Name}: {route.Producer}");
            ImGui.TextWrapped($"Output: {route.Output}");
            ImGui.TextWrapped($"Purpose: {route.Purpose}");
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Implementation options");
        foreach (var option in diagnostics.ImplementationOptions)
        {
            ImGui.BulletText($"{option.Name}: feasibility {option.Feasibility}, risk {option.Risk}. {option.Summary}");
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Next backend steps");
        foreach (var step in diagnostics.NextBackendSteps)
        {
            ImGui.BulletText($"{step.Stage}: {step.Goal}");
            ImGui.TextWrapped($"Safety: {step.SafetyBoundary}");
            ImGui.TextWrapped($"Exit: {step.ExitCriteria}");
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Safety");
        foreach (var note in diagnostics.Notes)
        {
            ImGui.BulletText(note);
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Removal boundary");
        foreach (var note in diagnostics.RemovalNotes)
        {
            ImGui.BulletText(note);
        }
    }

    private static void DrawResourceCatalog(string title, IReadOnlyList<DalapadResourceCatalogEntry> resources)
    {
        ImGui.Spacing();
        ImGui.TextUnformatted(title);
        if (resources.Count == 0)
        {
            ImGui.TextUnformatted("No resource catalog rows reported.");
            return;
        }

        foreach (var resource in resources)
        {
            ImGui.BulletText($"{resource.Name}: available {FormatYesNo(resource.Available)}, {resource.Width}x{resource.Height}, format {FormatOptionalUiValue(resource.Format)}, confidence {resource.Confidence:0.###}");
            ImGui.TextWrapped($"Source: {FormatOptionalUiValue(resource.Source)}");
            ImGui.TextWrapped($"Freshness: {FormatOptionalUiValue(resource.Freshness)}; safety: {FormatOptionalUiValue(resource.SafetyState)}; metadata: {FormatOptionalUiValue(resource.MetadataSource)}");
            ImGui.TextWrapped($"Reason: {FormatOptionalUiValue(resource.Reason)}");
        }
    }

    private static void DrawResourceShapeProbe(DalapadResourceShapeProbe probe)
    {
        ImGui.Spacing();
        ImGui.TextUnformatted("Developer resource shape probe");
        ImGui.TextUnformatted($"Enabled: {FormatYesNo(probe.Enabled)}");
        ImGui.TextUnformatted($"Attempted: {FormatYesNo(probe.Attempted)}");
        ImGui.TextUnformatted($"Instance invoked: {FormatYesNo(probe.InstanceInvoked)}");
        ImGui.TextUnformatted($"Status: {probe.Status}");
        ImGui.TextWrapped(probe.Summary);
        if (probe.Timestamp != DateTimeOffset.MinValue)
        {
            ImGui.TextUnformatted($"Probe timestamp: {probe.Timestamp:O}");
        }

        if (probe.Resources.Count == 0)
        {
            ImGui.TextUnformatted("No shape rows reported.");
        }
        else
        {
            foreach (var resource in probe.Resources)
            {
                ImGui.BulletText($"{resource.Name}: candidate {FormatYesNo(resource.CandidateFound)}, pointer {FormatYesNo(resource.PointerObserved)}, {resource.Width}x{resource.Height}, format {FormatOptionalUiValue(resource.Format)}, confidence {resource.Confidence:0.###}");
                ImGui.TextWrapped($"Source: {FormatOptionalUiValue(resource.Source)}; pointer: {FormatOptionalUiValue(resource.PointerFingerprint)}");
                ImGui.TextWrapped($"Freshness: {FormatOptionalUiValue(resource.Freshness)}; safety: {FormatOptionalUiValue(resource.SafetyState)}; metadata: {FormatOptionalUiValue(resource.MetadataSource)}");
                ImGui.TextWrapped($"Reason: {FormatOptionalUiValue(resource.Reason)}");
            }
        }

        foreach (var warning in probe.Warnings)
        {
            ImGui.BulletText($"Shape warning: {warning}");
        }
    }

    private static void DrawDebugVisualization(string title, DalapadDebugVisualizationStatus debug)
    {
        ImGui.Spacing();
        ImGui.TextUnformatted(title);
        ImGui.TextUnformatted($"Enabled: {FormatYesNo(debug.Enabled)}");
        ImGui.TextUnformatted($"Status: {FormatOptionalUiValue(debug.Status)}");
        ImGui.TextUnformatted($"Shader: {FormatOptionalUiValue(debug.Shader)}");
        ImGui.TextUnformatted($"Texture: {FormatOptionalUiValue(debug.TextureName)}");
        ImGui.TextUnformatted($"Source: {FormatOptionalUiValue(debug.Source)}");
        ImGui.TextUnformatted($"Shader texture found: {FormatYesNo(debug.ShaderTextureFound)}");
        ImGui.TextUnformatted($"Synthetic texture uploaded: {FormatYesNo(debug.SyntheticTextureUploaded)}");
        ImGui.TextUnformatted($"Uses synthetic texture: {FormatYesNo(debug.UsesSyntheticTexture)}");
        ImGui.TextUnformatted($"Size: {debug.Width}x{debug.Height}; frame age: {debug.FrameAge}");
        ImGui.TextUnformatted($"Render candidates: observed {debug.ObservedSourceCount}, copied {debug.CopiedSourceCount}");
        ImGui.TextWrapped($"Reason: {FormatOptionalUiValue(debug.Reason)}");
        ImGui.TextWrapped($"Safety: reads render targets {FormatYesNo(debug.ReadsRenderTargets)}, copies render targets {FormatYesNo(debug.CopiesRenderTargets)}, registers game resources {FormatYesNo(debug.RegistersGameResources)}");
        if (debug.PinnedCandidates.Count > 0)
        {
            ImGui.TextUnformatted("Pinned candidates:");
            foreach (var candidate in debug.PinnedCandidates)
            {
                ImGui.BulletText($"{candidate.Label}: {candidate.Source} -> {candidate.Semantic}; {FormatYesNo(candidate.Copied)} copied; {candidate.Width}x{candidate.Height}; {candidate.ClassificationHint}");
            }
        }
    }

    private static IReadOnlyList<string> BuildDalapadHealthNextSteps(DalapadDiagnostics diagnostics)
    {
        if (!diagnostics.IpcStatus.StatusFileFound)
        {
            return new[] { "Load the separate Dalapad addon prototype and confirm it writes dalapad-status.json." };
        }

        if (!diagnostics.IpcStatus.ContractCompatible)
        {
            return new[] { "Rebuild the addon against the current 0.1-ipc-diagnostic status-file contract." };
        }

        if (!diagnostics.ControlPipeStatus.PipeListening)
        {
            return new[] { "Rebuild and reload the addon with the diagnostic control pipe enabled." };
        }

        if (!diagnostics.ControlPipeStatus.ResponseReceived)
        {
            return new[] { "Check the addon pipe worker; the pipe accepted a connection but did not return capability JSON." };
        }

        if (!diagnostics.ControlPipeStatus.ContractCompatible)
        {
            return new[] { "Update the addon and plugin to the same Dalapad.Control.v1 pipe contract." };
        }

        if (diagnostics.ControlPipeStatus.Capabilities.ReadsRenderTargets
            || diagnostics.ControlPipeStatus.Capabilities.CopiesRenderTargets
            || diagnostics.ControlPipeStatus.Capabilities.RegistersShaderResources
            || diagnostics.ControlPipeStatus.Capabilities.MovesRealtimeShaderValues)
        {
            return new[] { "Unexpected advanced capabilities are enabled. Keep this build diagnostic-only until resource validation is explicitly started." };
        }

        if (!diagnostics.ControlPipeStatus.Capabilities.SupportsResourceCatalog)
        {
            return new[]
            {
                "Status-file IPC and control-pipe capability negotiation are healthy.",
                "Next safe step is a diagnostic resource catalog; do not send raw handles or shader values yet."
            };
        }

        if (diagnostics.IpcStatus.ResourceCatalog.Count == 0 && diagnostics.ControlPipeStatus.ResourceCatalog.Count == 0)
        {
            return new[] { "Resource catalog capability is enabled, but no catalog rows were reported. Check the addon status payload and QueryStatus response." };
        }

        if (!diagnostics.ResourceShapeProbe.Attempted)
        {
            return new[]
            {
                "Status-file IPC, control-pipe capability negotiation, and diagnostic resource catalog are healthy.",
                "Enable and run the developer-only resource shape probe next; keep raw handles, realtime shader values, and FrameData influence disabled."
            };
        }

        if (diagnostics.ResourceShapeProbe.Resources.All(resource => !resource.PointerObserved))
        {
            return new[]
            {
                "Developer resource shape probe ran without observing candidate pointers.",
                "Capture a debug bundle in-game and inspect the shape probe warnings before attempting any native bridge work."
            };
        }

        if (!diagnostics.ControlPipeStatus.DebugVisualization.SyntheticTextureUploaded)
        {
            return new[]
            {
                "Resource shape observation is healthy, but the synthetic debug visualization bridge has not uploaded yet.",
                "Install/reload Dalapad_Debug.fx and confirm the addon reports Dalapad_DebugTexture found before judging render-layer copy behavior."
            };
        }

        if (diagnostics.ControlPipeStatus.DebugVisualization.CopiedSourceCount == 0)
        {
            return new[]
            {
                "Synthetic debug visualization is healthy, but no render-layer candidate has been copied into Dalapad_Debug.fx yet.",
                diagnostics.ControlPipeStatus.DebugVisualization.ObservedSourceCount > 0
                    ? "The addon is observing render-target candidates; check format support, effect-begin callbacks, and copy barriers."
                    : "The addon has not observed render-target candidates yet; test while actively in a rendered scene with Dalapad_Debug.fx enabled."
            };
        }

        return new[]
        {
            "Status-file IPC, control-pipe capability negotiation, resource catalog, developer resource shape probe, and debug render-layer copies are healthy enough for repeated observation.",
            "Next safe step is lifecycle testing across login, zone change, resolution change, and reload; keep broader FrameData influence and realtime values disabled."
        };
    }

    private static string FormatOptionalUiValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }

    private static string FormatYesNo(bool value)
    {
        return value ? "yes" : "no";
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

    private void DrawUserGenerationControls(string idPrefix)
    {
        DrawCheckbox("Enable automatic generation", configuration.Enabled, value => configuration.Enabled = value);
        DrawCheckbox("Reload ReShade after generation", configuration.ReloadShadersAfterGeneration, value => configuration.ReloadShadersAfterGeneration = value);
        DrawCheckbox("Sync reload key to ReShade.ini", configuration.SyncReloadHotkeyToReShadeIni, value => configuration.SyncReloadHotkeyToReShadeIni = value);

        var seconds = configuration.MinimumSecondsBetweenWrites;
        if (ImGui.SliderInt($"Minimum seconds between writes###{idPrefix}WriteInterval", ref seconds, 1, 120))
        {
            configuration.MinimumSecondsBetweenWrites = seconds;
            configuration.Save();
        }

        ImGui.TextWrapped(UserGenerationStateSummary());
        ImGui.TextWrapped($"Last reload: {plugin.LastReloadResult.Message}");
    }

    private void DisableOptionalFirstPartySystems()
    {
        configuration.EnableDalashadeCustomShaders = true;
        configuration.EnableDalashadeSceneGIShaderVariables = false;
        configuration.EnableDalashadeSceneGIContactShadows = false;
        configuration.EnableDalashadeScreenShadowsShaderVariables = false;
        configuration.EnableDalashadeContactToneShaderVariables = false;
        configuration.EnableDalashadeSurfaceReflectionShaderVariables = false;
        configuration.EnableDalapadShaderIntegration = false;
        configuration.EnableDalapadSurfaceData = false;
        configuration.EnableDalapadSceneGINormalAssist = false;
        configuration.EnableMaterialIntentShaderMapping = false;
        configuration.EnableScreenshotMaterialEvidenceInfluence = false;
        configuration.EnableNormalField = false;
        configuration.EnableNormalFieldShaderMapping = false;
        configuration.EnableFirstPartyDepthAssist = false;
        configuration.Save();
    }

    private string UserGenerationStateSummary()
    {
        if (configuration.SceneLockEnabled)
        {
            return "Generation is locked to the current preset until scene lock is turned off.";
        }

        var mode = configuration.Enabled
            ? $"Automatic generation is on and can write at most once every {configuration.MinimumSecondsBetweenWrites} second(s)."
            : "Automatic generation is off; use Generate Now when you want to update the generated preset.";
        var reload = configuration.ReloadShadersAfterGeneration
            ? " ReShade reload is requested after successful writes."
            : " ReShade reload is off, so changes may wait until ReShade reloads manually.";
        return mode + reload;
    }

    private IReadOnlyList<string> BuildUserHealthNextSteps(CustomShaderBridgeDiagnostics diagnostics)
    {
        var steps = new List<string>();
        if (string.IsNullOrWhiteSpace(configuration.BasePresetPath) || !File.Exists(configuration.BasePresetPath))
        {
            steps.Add("Pick a valid base preset.");
        }

        if (string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath))
        {
            steps.Add("Choose where Dalashade should write the generated preset.");
        }

        if (string.Equals(configuration.BasePresetPath, configuration.GeneratedPresetPath, StringComparison.OrdinalIgnoreCase))
        {
            steps.Add("Use a generated preset path that is different from the base preset.");
        }

        if (!plugin.LastWriteResult.Success)
        {
            steps.Add("Press Generate Now after setup is complete.");
        }

        if (configuration.ReloadShadersAfterGeneration && !plugin.LastReloadResult.Success)
        {
            steps.Add("Open Generate and check ReShade.ini/reload settings, then use Test Reload.");
        }

        if (configuration.EnableDalashadeCustomShaders && !diagnostics.SectionInjected && configuration.AutoInjectDalashadeCustomShaderSections)
        {
            steps.Add("Generate once to inject Dalashade shader sections into the generated preset.");
        }

        if (configuration.EnableDalashadeCustomShaders && plugin.LastShaderSupportScan.Items.Count == 0)
        {
            steps.Add("Scan compatibility after installing Dalashade .fx files and enabling the generated preset in ReShade.");
        }

        if (!configuration.EnableDalashadeCustomShaders)
        {
            steps.Add("Enable first-party shader control in Effects if you want Dalashade shader values written.");
        }

        if (plugin.LastPresetAnalysis.Report.Warnings.Count > 0)
        {
            steps.Add("Review the current preset warnings or export a debug bundle for detailed diagnostics.");
        }

        if (steps.Count == 0)
        {
            steps.Add("No immediate setup problems detected.");
        }

        return steps;
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
