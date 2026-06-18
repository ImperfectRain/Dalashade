using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalashade.Windows.UiPages;

namespace Dalashade.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private static readonly Vector4 WarningColor = new(1.0f, 0.72f, 0.28f, 1.0f);
    private static readonly Vector4 DangerColor = new(1.0f, 0.38f, 0.34f, 1.0f);

    private readonly Plugin plugin;
    private bool showAllMasterColorFamilies;
    private string developerSearch = string.Empty;

    public MainWindow(Plugin plugin)
        : base("Dalashade###DalashadeMain")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        if (ImGui.Button("Settings"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.SameLine();
        if (ImGui.Button("Scene Authoring"))
        {
            plugin.ToggleSceneAuthoringUi();
        }

        ImGui.Spacing();
        DrawInterfaceModeSwitch("Main");
        ImGui.Spacing();

        if (plugin.Configuration.InterfaceMode == InterfaceMode.User)
        {
            DrawUserMode();
            return;
        }

        DrawDeveloperMode();
    }

    private void DrawDeveloperMode()
    {
        ImGui.InputText("Search developer panels###MainDeveloperSearch", ref developerSearch, 128);

        if (!string.IsNullOrWhiteSpace(developerSearch))
        {
            DrawDeveloperSearchResults();
            return;
        }

        if (!ImGui.BeginTabBar("MainDeveloperTabs"))
        {
            return;
        }

        DrawDeveloperTab("Overview", BuildDeveloperOverviewPages());
        DrawDeveloperTab("Preset Pipeline", BuildDeveloperPresetPipelinePages());
        DrawDeveloperTab("Scene System", BuildDeveloperSceneSystemPages());
        DrawDeveloperTab("Shader Mapping", BuildDeveloperShaderMappingPages());
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
            ImGui.TextUnformatted("No developer panels match the search.");
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
        return BuildDeveloperOverviewPages()
            .Concat(BuildDeveloperPresetPipelinePages())
            .Concat(BuildDeveloperSceneSystemPages())
            .Concat(BuildDeveloperShaderMappingPages())
            .Concat(BuildDeveloperDiagnosticsPages())
            .ToArray();
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperOverviewPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("MainCurrentStatus", "Current Status", true, CurrentStatusSummary, DrawCurrentStatus),
            DeveloperPage("MainBasePreset", "Base Preset", true, BasePresetSummary, DrawBasePreset, BasePresetWarningColor),
            DeveloperPage("MainGeneration", "Generation", true, GenerationSummary, DrawGeneration, GenerationWarningColor),
            DeveloperPage("MainReShadeReload", "ReShade Reload", true, ReShadeReloadSummary, DrawReShadeReload, ReShadeReloadWarningColor),
            DeveloperPage("MainPresetCompatibility", "Preset Compatibility", plugin.LastPresetAnalysis.Report.Level is PresetRiskLevel.High or PresetRiskLevel.VeryHigh, PresetCompatibilitySummary, DrawPresetCompatibility, PresetCompatibilityWarningColor)
        };
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperPresetPipelinePages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("MainBasePreset", "Base Preset", true, BasePresetSummary, DrawBasePreset, BasePresetWarningColor),
            DeveloperPage("MainGeneration", "Generation", true, GenerationSummary, DrawGeneration, GenerationWarningColor),
            DeveloperPage("MainReShadeReload", "ReShade Reload", true, ReShadeReloadSummary, DrawReShadeReload, ReShadeReloadWarningColor),
            DeveloperPage("MainPresetCompatibility", "Preset Compatibility", true, PresetCompatibilitySummary, DrawPresetCompatibility, PresetCompatibilityWarningColor),
            DeveloperPage("MainChangedVariables", "Changed Variables", false, ChangedVariablesSummary, DrawChangedVariables, ChangedVariablesWarningColor),
            DeveloperPage("MainSanitizeActions", "Sanitize Actions", false, SanitizeActionsSummary, DrawSanitizeActions)
        };
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperSceneSystemPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("MainCurrentStatus", "Current Status", true, CurrentStatusSummary, DrawCurrentStatus),
            DeveloperPage("MainSceneTags", "Scene Tags", true, SceneTagsSummary, DrawSceneTags),
            DeveloperPage("MainMaterialIntent", "Material Intent", false, MaterialIntentSummary, DrawMaterialIntent),
            DeveloperPage("MainNormalField", "Normal Field", false, NormalFieldSummary, DrawNormalField),
            DeveloperPage("MainScreenshotAnalysis", "Screenshot Analysis", false, ScreenshotSummary, DrawScreenshotAnalysis),
            DeveloperPage("MainMasterStyle", "Master Style", false, MasterStyleSummary, DrawMasterStyle)
        };
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperShaderMappingPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("MainMaterialIntent", "Material Intent", true, MaterialIntentSummary, DrawMaterialIntent),
            DeveloperPage("MainNormalField", "Normal Field", true, NormalFieldSummary, DrawNormalField),
            DeveloperPage("MainDebugDiagnostics", "Debug / Diagnostics", true, DebugSummary, DrawDebugDiagnostics),
            DeveloperPage("MainChangedVariables", "Changed Variables", false, ChangedVariablesSummary, DrawChangedVariables, ChangedVariablesWarningColor)
        };
    }

    private IReadOnlyList<IDalashadeUiPage> BuildDeveloperDiagnosticsPages()
    {
        return new IDalashadeUiPage[]
        {
            DeveloperPage("MainPresetCompatibility", "Preset Compatibility", true, PresetCompatibilitySummary, DrawPresetCompatibility, PresetCompatibilityWarningColor),
            DeveloperPage("MainChangedVariables", "Changed Variables", false, ChangedVariablesSummary, DrawChangedVariables, ChangedVariablesWarningColor),
            DeveloperPage("MainSanitizeActions", "Sanitize Actions", false, SanitizeActionsSummary, DrawSanitizeActions),
            DeveloperPage("MainAppliedRules", "Applied Rules", false, () => $"{plugin.CurrentRules.Count} rules applied", DrawAppliedRules),
            DeveloperPage("MainRegressionReports", "Regression Reports", false, RegressionSummary, DrawRegressionReports),
            DeveloperPage("MainDebugDiagnostics", "Debug / Diagnostics", false, DebugSummary, DrawDebugDiagnostics)
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
                "MainUserHome",
                "Home",
                true,
                InterfaceModeVisibility.User,
                UserHomeSummary,
                DrawUserHome),
            new DelegateDalashadeUiPage(
                "MainUserSetupGenerate",
                "Setup & Generate",
                false,
                InterfaceModeVisibility.User,
                BasePresetSummary,
                DrawUserSetupGenerate,
                BasePresetWarningColor),
            new DelegateDalashadeUiPage(
                "MainUserLook",
                "Look",
                false,
                InterfaceModeVisibility.User,
                UserLookSummary,
                DrawUserLook),
            new DelegateDalashadeUiPage(
                "MainUserSceneAwareness",
                "Scene Awareness",
                false,
                InterfaceModeVisibility.User,
                UserSceneAwarenessSummary,
                DrawUserSceneAwareness),
            new DelegateDalashadeUiPage(
                "MainUserEffects",
                "Effects",
                false,
                InterfaceModeVisibility.User,
                UserEffectsSummary,
                DrawUserEffects),
            new DelegateDalashadeUiPage(
                "MainUserHealth",
                "Health",
                healthOpen,
                InterfaceModeVisibility.User,
                UserHealthSummary,
                DrawUserHealth,
                PresetCompatibilityWarningColor)
        };
    }

    private void DrawInterfaceModeSwitch(string idPrefix)
    {
        var configuration = plugin.Configuration;

        ImGui.TextUnformatted("Interface mode");
        ImGui.SameLine();

        if (ImGui.RadioButton($"User###{idPrefix}InterfaceUser", configuration.InterfaceMode == InterfaceMode.User))
        {
            configuration.InterfaceMode = InterfaceMode.User;
            configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.RadioButton($"Developer###{idPrefix}InterfaceDeveloper", configuration.InterfaceMode == InterfaceMode.Developer))
        {
            configuration.InterfaceMode = InterfaceMode.Developer;
            configuration.Save();
        }
    }

    private string UserHomeSummary()
    {
        var configuration = plugin.Configuration;
        return $"{configuration.Style}, {configuration.PerformanceBudget}, {configuration.FirstPartyShaderMode}";
    }

    private void DrawUserHome()
    {
        var context = plugin.CurrentContext;
        var configuration = plugin.Configuration;
        var diagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(configuration, plugin.LastShaderSupportScan, plugin.LastWriteResult, plugin.LastPresetAnalysis);
        var readyToGenerate = !string.IsNullOrWhiteSpace(configuration.BasePresetPath)
                              && File.Exists(configuration.BasePresetPath)
                              && !string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath)
                              && !string.Equals(configuration.BasePresetPath, configuration.GeneratedPresetPath, StringComparison.OrdinalIgnoreCase);

        ImGui.TextUnformatted($"{context.TerritoryName} - {context.WeatherName} - {context.TimeBucket}");
        ImGui.TextWrapped($"Active tags: {(plugin.CurrentTags.MoodTags.Count == 0 ? "none" : string.Join(", ", plugin.CurrentTags.MoodTags))}");
        ImGui.TextUnformatted($"Current look: {configuration.Style} / {configuration.PerformanceBudget} / {configuration.FirstPartyShaderMode}");
        ImGui.Separator();
        DrawSetupItem("Ready to generate", readyToGenerate);
        DrawSetupItem("Generated preset updated", plugin.LastWriteResult.Success);
        DrawSetupItem("First-party shader values written", diagnostics.ValuesWritten);

        if (ImGui.Button("Generate Now###MainUserHomeGenerateNow"))
        {
            plugin.GenerateNow();
        }

        ImGui.SameLine();
        if (ImGui.Button("Health Check###MainUserHomeHealthCheck"))
        {
            plugin.ScanPresetCompatibility();
        }

        ImGui.TextWrapped(UserWriteSummary());
    }

    private void DrawUserSetupGenerate()
    {
        DrawBasePreset();
        ImGui.Separator();
        if (ImGui.Button("Generate Now###MainUserSetupGenerateNow"))
        {
            plugin.GenerateNow();
        }

        ImGui.SameLine();
        ImGui.TextWrapped(UserWriteSummary());
    }

    private string UserLookSummary()
    {
        var configuration = plugin.Configuration;
        var master = configuration.MatchMasterPresetStyle ? $", master {configuration.MasterPresetStyleStrength}%" : string.Empty;
        return $"{configuration.Style}, {configuration.PerformanceBudget}, {configuration.FirstPartyShaderMode}{master}";
    }

    private void DrawUserLook()
    {
        var configuration = plugin.Configuration;

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

        DrawCheckbox("Match master preset style", configuration.MatchMasterPresetStyle, value => configuration.MatchMasterPresetStyle = value);
        if (configuration.MatchMasterPresetStyle)
        {
            var diagnostics = plugin.CurrentMasterStyleDiagnostics;
            ImGui.TextWrapped(plugin.MasterStyleMessage);
            ImGui.TextUnformatted($"Effective master style strength: {diagnostics.EffectiveStrength:P0}");
            ImGui.TextUnformatted($"Scene similarity: {diagnostics.SceneSimilarityMultiplier:0.###}");
        }
    }

    private string UserSceneAwarenessSummary()
    {
        var diagnostics = plugin.CurrentTagStackDiagnostics;
        return $"{diagnostics.WeatherKey}, {diagnostics.BiomeKey}, {diagnostics.ActiveTags.Count} active tags";
    }

    private void DrawUserSceneAwareness()
    {
        var configuration = plugin.Configuration;
        var diagnostics = plugin.CurrentTagStackDiagnostics;

        ImGui.TextWrapped($"Weather tags: {FormatTagList(diagnostics.ActiveWeatherTags)}");
        ImGui.TextWrapped($"Material tags: {FormatTagList(diagnostics.MaterialTags)}");
        ImGui.TextWrapped($"Mood tags: {(diagnostics.MoodTags.Count == 0 ? "none" : string.Join(", ", diagnostics.MoodTags))}");
        ImGui.TextWrapped($"Gameplay-state tags: {FormatTagList(diagnostics.GameplayStateTags)}");
        ImGui.Separator();
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
            DrawFloatSlider("Screenshot material hint strength###MainUserScreenshotMaterialHintStrength", configuration.ScreenshotMaterialEvidenceStrength, 0f, 0.6f, value => configuration.ScreenshotMaterialEvidenceStrength = value);
            ImGui.TextWrapped("Scene-level hint only. UI-heavy screenshots can be wrong; missing screenshots produce neutral evidence.");
        }

        if (ImGui.Button("Open Scene Authoring###MainUserOpenSceneAuthoring"))
        {
            plugin.ToggleSceneAuthoringUi();
        }
    }

    private string UserEffectsSummary()
    {
        var configuration = plugin.Configuration;
        if (!configuration.EnableDalashadeCustomShaders)
        {
            return "First-party shader variables disabled";
        }

        var enabled = new[]
        {
            configuration.EnableDalashadeSceneGIShaderVariables,
            configuration.EnableDalashadeSurfaceReflectionShaderVariables,
            configuration.EnableMaterialIntentShaderMapping,
            configuration.EnableNormalFieldShaderMapping,
            configuration.EnableFirstPartyDepthAssist
        }.Count(value => value);

        var hints = configuration.EnableScreenshotMaterialEvidenceInfluence ? ", screenshot hints on" : string.Empty;
        return $"{enabled} optional effect systems enabled{hints}";
    }

    private void DrawUserEffects()
    {
        var configuration = plugin.Configuration;

        DrawCheckbox("Enable Dalashade custom shader variables", configuration.EnableDalashadeCustomShaders, value => configuration.EnableDalashadeCustomShaders = value);
        DrawCheckbox("Auto-inject known Dalashade shader sections", configuration.AutoInjectDalashadeCustomShaderSections, value => configuration.AutoInjectDalashadeCustomShaderSections = value);
        DrawCheckbox("Enable depth assist for first-party Dalashade shaders", configuration.EnableFirstPartyDepthAssist, value => configuration.EnableFirstPartyDepthAssist = value);
        ImGui.TextWrapped("Cards show whether each effect appears wired enough to work. Per-shader debug modes and raw variables are in Developer Mode.");

        DrawToneAndColorCard();
        DrawAtmosphereCard();
        DrawIndirectLightingCard();
        DrawReflectionCard();
        DrawBloomCard();
        DrawSharpeningCard();
    }

    private void DrawToneAndColorCard()
    {
        FeatureStatusCardRenderer.Draw(BuildFeatureStatusCard(
            "Tone and Color",
            "AdaptiveGrade handles broad tone, color, and Standalone identity shaping.",
            "Dalashade_AdaptiveGrade",
            frameData: true));

        var configuration = plugin.Configuration;
        var style = (int)configuration.Style;
        if (ImGui.Combo("Tone target style###UserToneStyle", ref style, "Gameplay\0Balanced\0Cinematic\0"))
        {
            configuration.Style = (TargetStyle)style;
            configuration.Save();
        }

        var firstPartyMode = (int)configuration.FirstPartyShaderMode;
        if (ImGui.Combo("First-party stack mode###UserToneFirstPartyMode", ref firstPartyMode, "Supportive / Enhance Base Preset\0Standalone / First-Party Stack\0"))
        {
            configuration.FirstPartyShaderMode = (FirstPartyShaderMode)firstPartyMode;
            configuration.Save();
        }
    }

    private void DrawAtmosphereCard()
    {
        FeatureStatusCardRenderer.Draw(BuildFeatureStatusCard(
            "Atmosphere and Weather",
            "WeatherAtmosphere steers air, fog, storm, heat, cold, and weather response.",
            "Dalashade_WeatherAtmosphere",
            frameData: true));

        var configuration = plugin.Configuration;
        DrawCheckbox("Auto-adjust for weather###UserAtmosphereWeather", configuration.AutoAdjustForWeather, value => configuration.AutoAdjustForWeather = value);
        DrawCheckbox("Auto-adjust for territory type###UserAtmosphereTerritory", configuration.AutoAdjustForTerritory, value => configuration.AutoAdjustForTerritory = value);
    }

    private void DrawIndirectLightingCard()
    {
        var configuration = plugin.Configuration;
        FeatureStatusCardRenderer.Draw(BuildFeatureStatusCard(
            "Indirect Lighting",
            "SceneGI adds material-aware contact grounding, bounce, and local-light pooling.",
            "Dalashade_SceneGI",
            frameData: true));

        DrawCheckbox("Enable indirect lighting variable writes###UserSceneGIEnabled", configuration.EnableDalashadeSceneGIShaderVariables, value => configuration.EnableDalashadeSceneGIShaderVariables = value);
        DrawFloatSlider("Indirect lighting strength###UserSceneGIStrength", configuration.DalashadeSceneGIStrength, 0f, 1f, value => configuration.DalashadeSceneGIStrength = value);
        DrawFloatSlider("Contact grounding###UserSceneGIAO", configuration.DalashadeSceneGIAOIntensity, 0f, 1f, value => configuration.DalashadeSceneGIAOIntensity = value);
        DrawFloatSlider("Color bounce###UserSceneGIBounce", configuration.DalashadeSceneGIBounceStrength, 0f, 1f, value => configuration.DalashadeSceneGIBounceStrength = value);
    }

    private void DrawReflectionCard()
    {
        var configuration = plugin.Configuration;
        FeatureStatusCardRenderer.Draw(BuildFeatureStatusCard(
            "Reflections",
            "SurfaceReflection controls water sheen, wetness, and material glints.",
            "Dalashade_SurfaceReflection",
            frameData: true));

        DrawCheckbox("Enable reflection variable writes###UserReflectionEnabled", configuration.EnableDalashadeSurfaceReflectionShaderVariables, value => configuration.EnableDalashadeSurfaceReflectionShaderVariables = value);
        DrawFloatSlider("Reflection strength###UserReflectionStrength", configuration.DalashadeSurfaceReflectionStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionStrength = value);
        DrawFloatSlider("Water sheen###UserReflectionWater", configuration.DalashadeSurfaceReflectionWaterSheenStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionWaterSheenStrength = value);
        DrawFloatSlider("Wet surface response###UserReflectionWet", configuration.DalashadeSurfaceReflectionWetStrength, 0f, 1f, value => configuration.DalashadeSurfaceReflectionWetStrength = value);
    }

    private void DrawBloomCard()
    {
        FeatureStatusCardRenderer.Draw(BuildFeatureStatusCard(
            "Bloom and Glow",
            "AtmosphereBloom handles restrained bloom, glow, and source-class response.",
            "Dalashade_AtmosphereBloom",
            frameData: true));

        ImGui.TextWrapped("Bloom-specific tuning is currently shader-owned. Use Developer Mode for raw variables and debug state.");
    }

    private void DrawSharpeningCard()
    {
        FeatureStatusCardRenderer.Draw(BuildFeatureStatusCard(
            "Sharpening",
            "SmartSharpen preserves clarity while respecting sky, skin, water, foliage, and highlight safety.",
            "Dalashade_SmartSharpen",
            frameData: true));

        var configuration = plugin.Configuration;
        var budget = (int)configuration.PerformanceBudget;
        if (ImGui.Combo("Clarity performance budget###UserSharpenBudget", ref budget, "Low\0Medium\0High\0Ultra\0"))
        {
            configuration.PerformanceBudget = (PerformanceBudget)budget;
            configuration.Save();
        }
    }

    private FeatureStatusCard BuildFeatureStatusCard(string title, string description, string family, bool frameData)
    {
        var diagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(plugin.Configuration, plugin.LastShaderSupportScan, plugin.LastWriteResult, plugin.LastPresetAnalysis);
        var technique = FindTechnique(family);
        var shaderFile = ShaderFileLocator.Find(plugin.Configuration, $"{family}.fx");
        var sectionPresent = technique is not null || diagnostics.Sections.Any(section => SectionMatches(section.Section, family));
        var activation = technique?.ActivationState
                         ?? diagnostics.Sections.FirstOrDefault(section => SectionMatches(section.Section, family))?.ActivationState
                         ?? TechniqueActivationState.Unknown;
        var variablesDetected = diagnostics.KnownVariables.Any(variable => SectionMatches(variable.Section, family));
        var variablesWritten = diagnostics.WrittenVariables.Any(variable => SectionMatches(variable.Section, family));
        var status = BuildFeatureStatus(sectionPresent, activation, variablesDetected, variablesWritten);

        return new FeatureStatusCard(
            title,
            description,
            status,
            ShaderFileFound: shaderFile.Found,
            PresetEntryPresent: sectionPresent,
            PresetSectionPresent: sectionPresent,
            TechniqueActivation: activation,
            VariablesDetected: variablesDetected,
            VariablesWritten: variablesWritten,
            UsesFrameData: frameData,
            DepthAssistEnabled: plugin.Configuration.EnableFirstPartyDepthAssist);
    }

    private string BuildFeatureStatus(bool sectionPresent, TechniqueActivationState activation, bool variablesDetected, bool variablesWritten)
    {
        if (!plugin.Configuration.EnableDalashadeCustomShaders)
        {
            return "Needs custom shader variables";
        }

        if (!sectionPresent)
        {
            return plugin.Configuration.AutoInjectDalashadeCustomShaderSections ? "Generate to inject section" : "Needs setup";
        }

        if (activation == TechniqueActivationState.Active)
        {
            return variablesWritten ? "Active" : variablesDetected ? "Active, no new writes" : "Active, variables missing";
        }

        if (activation == TechniqueActivationState.Inactive)
        {
            return "Manual enable needed";
        }

        return "Activation unknown";
    }

    private PresetTechnique? FindTechnique(string family)
    {
        return plugin.LastPresetAnalysis.Techniques.FirstOrDefault(technique =>
            SectionMatches(technique.Section, family)
            || SectionMatches(technique.ShaderFile, family)
            || SectionMatches(technique.TechniqueName, family));
    }

    private static bool SectionMatches(string value, string family)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.Contains(family, StringComparison.OrdinalIgnoreCase);
    }

    private string UserHealthSummary()
    {
        var report = plugin.LastPresetAnalysis.Report;
        return $"Risk {report.Level}, {report.Warnings.Count} warnings";
    }

    private void DrawUserHealth()
    {
        var configuration = plugin.Configuration;
        var report = plugin.LastPresetAnalysis.Report;
        var diagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(configuration, plugin.LastShaderSupportScan, plugin.LastWriteResult, plugin.LastPresetAnalysis);

        DrawSetupItem("Generated at least once", plugin.LastWriteResult.Success);
        DrawSetupItem("Reload attempted", plugin.LastReloadResult.Success);
        DrawSetupItem("Shader variables detected", plugin.LastShaderSupportScan.Items.Count > 0);
        DrawSetupItem("Generated preset sections injected", diagnostics.SectionInjected);
        DrawSetupItem("Technique activation remains manual", !diagnostics.TechniqueInjected);

        if (ImGui.Button("Scan Preset Compatibility###MainUserScanPreset"))
        {
            plugin.ScanPresetCompatibility();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Debug Bundle###MainUserExportDebugBundle"))
        {
            plugin.ExportDebugBundle();
        }

        ImGui.SameLine();
        if (ImGui.Button("Test Reload###MainUserTestReload"))
        {
            plugin.ReloadShadersNow();
        }

        ImGui.TextWrapped(plugin.LastPresetAnalysis.Message);
        ImGui.TextWrapped(plugin.LastDiagnosticsExportMessage);
        ImGui.TextWrapped(plugin.LastReloadResult.Message);

        if (report.Warnings.Count > 0)
        {
            ImGui.Separator();
            ImGui.TextUnformatted("Top warnings");
            foreach (var warning in report.Warnings.Take(6))
            {
                ImGui.BulletText(warning);
            }
        }
    }

    private string CurrentStatusSummary()
    {
        var context = plugin.CurrentContext;
        return $"{context.TerritoryName}, {context.WeatherName}, {context.TimeBucket}";
    }

    private void DrawCurrentStatus()
    {
        var context = plugin.CurrentContext;
        var tags = plugin.CurrentTags;

        ImGui.TextUnformatted($"Territory: {context.TerritoryName} ({context.TerritoryId})");
        ImGui.TextUnformatted($"World: {context.WorldCategory}");
        ImGui.TextUnformatted($"Content: {context.ContentName} ({context.ContentType})");
        ImGui.TextUnformatted($"Weather: {context.WeatherName} ({(context.WeatherId.HasValue ? context.WeatherId.Value.ToString() : "unknown")}, {tags.WeatherKey})");
        ImGui.TextUnformatted($"Time: {context.EorzeaHour:0.0}h ({context.TimeBucket})");
        ImGui.TextUnformatted($"Combat: {context.InCombat}");
        ImGui.TextUnformatted($"Duty: {context.InDuty}");
        ImGui.TextUnformatted($"GPose: {context.InGpose}");
        ImGui.TextUnformatted($"Cutscene: {context.InCutscene}");
        ImGui.TextUnformatted($"Scene Lock: {plugin.Configuration.SceneLockEnabled}");
        ImGui.TextUnformatted($"Scene Tags: {tags.AreaKey}, {tags.BiomeKey} ({tags.BiomeConfidence:P0}), combat={tags.NeedsCombatClarity}, duty={tags.NeedsDutyReadability}, cinematic={tags.CinematicAllowed}");
        ImGui.TextWrapped($"Biome reason: {tags.BiomeReason}");
        ImGui.TextWrapped($"Mood tags: {(tags.MoodTags.Count == 0 ? "none" : string.Join(", ", tags.MoodTags))}");
    }

    private string SceneTagsSummary()
    {
        var diagnostics = plugin.CurrentTagStackDiagnostics;
        return $"{diagnostics.WeatherKey}, {diagnostics.BiomeKey}, {diagnostics.Contributions.Count} budget actions";
    }

    private void DrawSceneTags()
    {
        var diagnostics = plugin.CurrentTagStackDiagnostics;
        ImGui.TextUnformatted($"Territory: {diagnostics.TerritoryName} ({diagnostics.TerritoryId})");
        ImGui.TextUnformatted($"Weather: {diagnostics.WeatherName} ({(diagnostics.WeatherId.HasValue ? diagnostics.WeatherId.Value.ToString() : "unknown")})");
        ImGui.TextUnformatted($"Weather key: {diagnostics.WeatherKey}");
        ImGui.TextWrapped($"Active weather tags: {(diagnostics.ActiveWeatherTags.Count == 0 ? "none" : string.Join(", ", diagnostics.ActiveWeatherTags))}");
        ImGui.TextUnformatted($"Biome key: {diagnostics.BiomeKey}");
        ImGui.TextUnformatted($"Biome confidence: {diagnostics.BiomeConfidence:P0}");
        ImGui.TextWrapped($"Biome reason: {diagnostics.BiomeReason}");
        ImGui.TextWrapped($"Secondary tags: {FormatTagList(diagnostics.SecondaryTags)}");
        ImGui.TextWrapped($"Night tags: {FormatTagList(diagnostics.SecondaryTags.Concat(diagnostics.ArtDirectionTags).Where(IsNightTag).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())}");
        ImGui.TextWrapped($"Day tags: {FormatTagList(diagnostics.SecondaryTags.Concat(diagnostics.ArtDirectionTags).Where(IsDayTag).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())}");
        ImGui.TextWrapped($"Mood tags: {(diagnostics.MoodTags.Count == 0 ? "none" : string.Join(", ", diagnostics.MoodTags))}");
        ImGui.TextWrapped($"Material tags: {FormatTagList(diagnostics.MaterialTags)}");
        ImGui.TextWrapped($"Area/context tags: {FormatTagList(diagnostics.AreaContextTags)}");
        ImGui.TextWrapped($"Gameplay-state tags: {FormatTagList(diagnostics.GameplayStateTags)}");
        ImGui.TextWrapped($"Art-direction tags: {FormatTagList(diagnostics.ArtDirectionTags)}");
        ImGui.TextUnformatted($"Area key: {diagnostics.AreaKey}");
        ImGui.TextUnformatted($"Combat: {diagnostics.InCombat}");
        ImGui.TextUnformatted($"Duty: {diagnostics.InDuty}");
        ImGui.TextUnformatted($"Cutscene: {diagnostics.InCutscene}");
        ImGui.TextUnformatted($"GPose: {diagnostics.InGpose}");
        ImGui.TextWrapped($"Active tags: {(diagnostics.ActiveTags.Count == 0 ? "none" : string.Join(", ", diagnostics.ActiveTags))}");

        if (ImGui.TreeNode("Scene intent###MainSceneIntent"))
        {
            ImGui.TextUnformatted($"Readability: {diagnostics.Intent.Readability:0.###}");
            ImGui.TextUnformatted($"Atmosphere: {diagnostics.Intent.Atmosphere:0.###}");
            ImGui.TextUnformatted($"Highlight Protection: {diagnostics.Intent.HighlightProtection:0.###}");
            ImGui.TextUnformatted($"Shadow Protection: {diagnostics.Intent.ShadowProtection:0.###}");
            ImGui.TextUnformatted($"Haze: {diagnostics.Intent.Haze:0.###}");
            ImGui.TextUnformatted($"Wetness: {diagnostics.Intent.Wetness:0.###}");
            ImGui.TextUnformatted($"Cold: {diagnostics.Intent.Cold:0.###}");
            ImGui.TextUnformatted($"Heat: {diagnostics.Intent.Heat:0.###}");
            ImGui.TextUnformatted($"Magic Glow: {diagnostics.Intent.MagicGlow:0.###}");
            ImGui.TextUnformatted($"Neon Glow: {diagnostics.Intent.NeonGlow:0.###}");
            ImGui.TextUnformatted($"Foliage Density: {diagnostics.Intent.FoliageDensity:0.###}");
            ImGui.TextUnformatted($"Industrial Hardness: {diagnostics.Intent.IndustrialHardness:0.###}");
            ImGui.TextUnformatted($"Cosmic Mood: {diagnostics.Intent.CosmicMood:0.###}");
            ImGui.TextUnformatted($"Night: {diagnostics.Intent.Night:0.###}");
            ImGui.TextUnformatted($"Moonlight: {diagnostics.Intent.Moonlight:0.###}");
            ImGui.TextUnformatted($"Artificial Light: {diagnostics.Intent.ArtificialLight:0.###}");
            ImGui.TextUnformatted($"Ambient Darkness: {diagnostics.Intent.AmbientDarkness:0.###}");
            ImGui.TextUnformatted($"Night Atmosphere: {diagnostics.Intent.NightAtmosphere:0.###}");
            ImGui.TextUnformatted($"Daylight: {diagnostics.Intent.Daylight:0.###}");
            ImGui.TextUnformatted($"Sunlight: {diagnostics.Intent.Sunlight:0.###}");
            ImGui.TextUnformatted($"Open Sky Light: {diagnostics.Intent.OpenSkyLight:0.###}");
            ImGui.TextUnformatted($"Surface Heat: {diagnostics.Intent.SurfaceHeat:0.###}");
            ImGui.TextUnformatted($"Day Atmosphere: {diagnostics.Intent.DayAtmosphere:0.###}");
            ImGui.TextUnformatted($"Day Reflection: {diagnostics.Intent.DayReflection:0.###}");
            ImGui.TextUnformatted($"Day Highlight Pressure: {diagnostics.Intent.DayHighlightPressure:0.###}");
            ImGui.TextUnformatted($"Combat Pressure: {diagnostics.Intent.CombatPressure:0.###}");
            ImGui.TextUnformatted($"Cinematic Permission: {diagnostics.Intent.CinematicPermission:0.###}");
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Scene intent contributors###MainSceneIntentContributors"))
        {
            if (diagnostics.IntentContributions.Count == 0)
            {
                ImGui.TextUnformatted("No scene intent contributions recorded.");
            }

            foreach (var contribution in diagnostics.IntentContributions)
            {
                ImGui.BulletText($"{contribution.Intent}: {contribution.Amount:+0.###;-0.###;0} from {contribution.Source}");
                ImGui.TextWrapped(contribution.Reason);
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Stack budget contributions###MainSceneStackContributions"))
        {
            if (diagnostics.Contributions.Count == 0)
            {
                ImGui.TextUnformatted("No stack budget actions applied.");
            }

            foreach (var contribution in diagnostics.Contributions)
            {
                var flags = contribution.BudgetApplied ? "budget" : "info";
                if (contribution.Dampened)
                {
                    flags += ", dampened";
                }

                ImGui.BulletText($"{contribution.Variable}: {contribution.Before:0.###} -> {contribution.After:0.###} ({contribution.Source}, {contribution.Change}, {flags})");
            }

            ImGui.TreePop();
        }
    }

    private static string FormatTagList(IReadOnlyList<string> tags)
    {
        return tags.Count == 0 ? "none" : string.Join(", ", tags);
    }

    private static bool IsNightTag(string tag)
    {
        return tag.Contains("Night", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tag, "night", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDayTag(string tag)
    {
        return tag.Contains("Day", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tag, "day", StringComparison.OrdinalIgnoreCase);
    }

    private string MaterialIntentSummary()
    {
        var configuration = plugin.Configuration;
        if (!configuration.EnableMaterialIntent)
        {
            return "Disabled";
        }

        if (!configuration.EnableMaterialIntentDiagnostics)
        {
            return $"Enabled, diagnostics hidden, strength {configuration.MaterialIntentStrength:0.##}";
        }

        var strongest = MaterialIntent.ChannelNames
            .Select(channel => new { Channel = channel, Value = plugin.CurrentMaterialIntent.ValueFor(channel) })
            .OrderByDescending(item => item.Value)
            .FirstOrDefault();
        var strongestText = strongest == null || strongest.Value <= 0f ? "no material pressure" : $"{strongest.Channel} {strongest.Value:0.##}";
        return $"Experimental inferred materials, {strongestText}";
    }

    private void DrawMaterialIntent()
    {
        var configuration = plugin.Configuration;
        ImGui.TextUnformatted($"Enabled: {(configuration.EnableMaterialIntent ? "yes" : "no")}");
        ImGui.TextUnformatted($"Diagnostics: {(configuration.EnableMaterialIntentDiagnostics ? "enabled" : "disabled")}");
        ImGui.TextUnformatted($"Shader mapping: {(configuration.EnableMaterialIntentShaderMapping ? "enabled" : "disabled")}");
        ImGui.TextUnformatted($"Strength: {configuration.MaterialIntentStrength:0.###}");
        ImGui.TextUnformatted($"Screenshot evidence influence: {(configuration.EnableScreenshotMaterialEvidenceInfluence ? $"enabled at {configuration.ScreenshotMaterialEvidenceStrength:0.###}" : "disabled")}");
        ImGui.TextUnformatted("Debug overlay controls: owned by each ReShade .fx shader UI");
        ImGui.TextWrapped("Experimental/inferred material likelihood. This is not true engine material ID detection.");

        if (!configuration.EnableMaterialIntent)
        {
            ImGui.TextWrapped("MaterialIntent is disabled. Values are neutral and no calculation is used.");
            return;
        }

        if (!configuration.EnableMaterialIntentDiagnostics)
        {
            ImGui.TextWrapped("MaterialIntent diagnostics are disabled. Enable diagnostics in settings to inspect channel values and contributions.");
            return;
        }

        if (!configuration.EnableMaterialIntentShaderMapping)
        {
            ImGui.TextWrapped("Diagnostics only, no visual shader mapping.");
        }

        DrawScreenshotMaterialEvidence(configuration, plugin.CurrentScreenshotMaterialEvidence, plugin.CurrentMaterialIntent);
        DrawMaterialTagRegistryDiagnostics(configuration, plugin.CurrentMaterialTagRegistryDiagnostics);

        var intent = plugin.CurrentMaterialIntent;
        if (ImGui.BeginTable("MaterialIntentValuesTable", 2))
        {
            ImGui.TableSetupColumn("Channel");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();
            foreach (var channel in MaterialIntent.ChannelNames)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(channel);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{intent.ValueFor(channel):0.###}");
            }

            ImGui.EndTable();
        }

        if (ImGui.TreeNode("MaterialIntent contributions###MainMaterialIntentContributions"))
        {
            foreach (var channel in MaterialIntent.ChannelNames)
            {
                var contributions = intent.Contributions
                    .Where(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal) && Math.Abs(contribution.Amount) > 0.0001f)
                    .OrderByDescending(contribution => Math.Abs(contribution.Amount))
                    .Take(6)
                    .ToArray();
                if (contributions.Length == 0)
                {
                    continue;
                }

                ImGui.BulletText($"{channel}: {intent.ValueFor(channel):0.###}");
                foreach (var contribution in contributions)
                {
                    ImGui.TextWrapped($"  {contribution.Amount:+0.###;-0.###;0} from {contribution.Source}: {contribution.Reason}");
                }
            }

            ImGui.TreePop();
        }
    }

    private static void DrawMaterialTagRegistryDiagnostics(Configuration configuration, MaterialTagRegistryDiagnostics diagnostics)
    {
        if (!ImGui.TreeNode("Tag registry material tunings###MainMaterialTagRegistryTunings"))
        {
            return;
        }

        ImGui.TextWrapped(configuration.EnableSceneAuthoringOverrides
            ? $"Registry material caps: per tag +/-{MaterialTagRegistryTuningAnalyzer.PerTagContributionCap:0.##}, per channel +/-{MaterialTagRegistryTuningAnalyzer.PerChannelContributionCap:0.##}."
            : "Scene authoring/tag registry is disabled, so registry material tunings are not applied.");

        var visibleChannels = diagnostics.Channels
            .Where(channel => Math.Abs(channel.FinalContribution) > 0.0001f || channel.Capped)
            .ToArray();
        if (visibleChannels.Length == 0)
        {
            ImGui.TextUnformatted("Current scene registry contribution: none.");
        }
        else if (ImGui.BeginTable("MaterialTagRegistryChannelTable", 3))
        {
            ImGui.TableSetupColumn("Channel");
            ImGui.TableSetupColumn("Contribution");
            ImGui.TableSetupColumn("Capped");
            ImGui.TableHeadersRow();
            foreach (var channel in visibleChannels)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(channel.Channel);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{channel.FinalContribution:+0.###;-0.###;0}");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(channel.Capped ? "yes" : "no");
            }

            ImGui.EndTable();
        }

        DrawRegistryTuningGroup("Active", diagnostics.ActiveTunings);
        DrawRegistryTuningGroup("Capped", diagnostics.CappedTunings);
        DrawRegistryTuningGroup("Invalid", diagnostics.InvalidTunings);
        DrawRegistryTuningGroup("Inactive", diagnostics.InactiveTunings.Take(10).ToArray());
        if (diagnostics.InactiveTunings.Count > 10)
        {
            ImGui.TextDisabled($"Inactive rows truncated: 10 of {diagnostics.InactiveTunings.Count} shown.");
        }

        ImGui.TreePop();
    }

    private static void DrawRegistryTuningGroup(string label, IReadOnlyList<MaterialTagRegistryTuningDiagnostic> rows)
    {
        if (!ImGui.TreeNode($"{label} registry tunings###MainMaterialTagRegistry{label}"))
        {
            return;
        }

        if (rows.Count == 0)
        {
            ImGui.TextUnformatted("None.");
            ImGui.TreePop();
            return;
        }

        foreach (var row in rows)
        {
            ImGui.BulletText($"{row.Category}/{row.Tag} -> {row.Channel}: {row.AppliedAmount:+0.###;-0.###;0} requested {row.RequestedAmount:+0.###;-0.###;0}");
            ImGui.TextWrapped($"{row.Message} {row.Reason}");
        }

        ImGui.TreePop();
    }

    private static void DrawScreenshotMaterialEvidence(Configuration configuration, ScreenshotMaterialEvidenceDiagnostics diagnostics, MaterialIntent currentIntent)
    {
        var evidence = diagnostics.Evidence;
        if (!ImGui.TreeNode("Screenshot material evidence###MainScreenshotMaterialEvidence"))
        {
            return;
        }

        ImGui.TextWrapped(configuration.EnableScreenshotMaterialEvidenceInfluence
            ? "Screenshot material evidence is enabled as a capped scene-level hint. It can influence MaterialIntent through the existing mapping path, but it does not change shader formulas or identify true game materials."
            : "Screenshot material evidence influence is disabled. These values are diagnostic-only and do not change MaterialIntent, generated presets, shader variables, or shader behavior.");
        ImGui.TextWrapped("UI-heavy screenshots can lower confidence or mislead evidence. Missing screenshots produce neutral evidence. Shader FrameData and MaterialMasks still decide pixel-level behavior.");
        ImGui.TextUnformatted($"Influence strength: {(configuration.EnableScreenshotMaterialEvidenceInfluence ? configuration.ScreenshotMaterialEvidenceStrength.ToString("0.###") : "disabled")}");
        ImGui.TextUnformatted("Caps: foliage/grass +0.22, water +0.16, sand +0.16, snow +0.18, stone +0.14, metal +0.12, aether/neon +0.14 at full strength.");
        ImGui.TextUnformatted($"Confidence: {evidence.Confidence:0.###}");
        if (ImGui.BeginTable("ScreenshotMaterialEvidenceTable", 2))
        {
            ImGui.TableSetupColumn("Evidence");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.FoliageVisible), evidence.FoliageVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.GrassTerrainVisible), evidence.GrassTerrainVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.WaterVisible), evidence.WaterVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.SandVisible), evidence.SandVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.SnowVisible), evidence.SnowVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.StoneVisible), evidence.StoneVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.MetalVisible), evidence.MetalVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.SkyVisible), evidence.SkyVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.AetherOrNeonVisible), evidence.AetherOrNeonVisible);
            DrawEvidenceRow(nameof(ScreenshotMaterialEvidence.SkinOrCharacterVisible), evidence.SkinOrCharacterVisible);
            ImGui.EndTable();
        }

        if (ImGui.BeginTable("ScreenshotMaterialEvidenceIntentComparisonTable", 3))
        {
            ImGui.TableSetupColumn("Evidence");
            ImGui.TableSetupColumn("Visible");
            ImGui.TableSetupColumn("Current intent");
            ImGui.TableHeadersRow();
            DrawEvidenceIntentRow("Foliage / grass", MathF.Max(evidence.FoliageVisible, evidence.GrassTerrainVisible), currentIntent.Foliage);
            DrawEvidenceIntentRow("Water", evidence.WaterVisible, currentIntent.WaterSpecular);
            DrawEvidenceIntentRow("Sand", evidence.SandVisible, currentIntent.SandDust);
            DrawEvidenceIntentRow("Snow", evidence.SnowVisible, currentIntent.SnowIce);
            DrawEvidenceIntentRow("Stone", evidence.StoneVisible, currentIntent.StoneRuins);
            DrawEvidenceIntentRow("Metal", evidence.MetalVisible, currentIntent.MetalIndustrial);
            DrawEvidenceIntentRow("Sky", evidence.SkyVisible, currentIntent.SkyCloudFog);
            DrawEvidenceIntentRow("Aether/neon", evidence.AetherOrNeonVisible, MathF.Max(currentIntent.CrystalAether, currentIntent.NeonGlass));
            DrawEvidenceIntentRow("Skin/character", evidence.SkinOrCharacterVisible, currentIntent.SkinProtection);
            ImGui.EndTable();
        }

        if (diagnostics.Mismatches.Count == 0)
        {
            ImGui.TextUnformatted("Mismatch warnings: none");
        }
        else
        {
            ImGui.TextUnformatted("Mismatch warnings:");
            foreach (var mismatch in diagnostics.Mismatches)
            {
                ImGui.BulletText($"{mismatch.Channel}: severity {mismatch.Severity:0.##}");
                ImGui.TextWrapped($"{mismatch.Message} Visible {mismatch.VisibleEvidence:0.###}, current intent {mismatch.CurrentIntent:0.###}.");
            }
        }

        if (ImGui.TreeNode("Evidence notes###MainScreenshotMaterialEvidenceNotes"))
        {
            foreach (var note in evidence.Evidence)
            {
                ImGui.BulletText(note);
            }

            ImGui.TreePop();
        }

        if (ImGui.Button("Copy evidence block###MainCopyScreenshotMaterialEvidence"))
        {
            ImGui.SetClipboardText(BuildScreenshotMaterialEvidenceBlock(configuration, diagnostics, currentIntent));
        }

        ImGui.TreePop();
    }

    private static void DrawEvidenceRow(string label, float value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{value:0.###}");
    }

    private static void DrawEvidenceIntentRow(string label, float visible, float intent)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{visible:0.###}");
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"{intent:0.###}");
    }

    private static string BuildScreenshotMaterialEvidenceBlock(Configuration configuration, ScreenshotMaterialEvidenceDiagnostics diagnostics, MaterialIntent currentIntent)
    {
        var evidence = diagnostics.Evidence;
        var builder = new StringBuilder();
        builder.AppendLine("Screenshot material evidence");
        builder.AppendLine($"Influence enabled: {configuration.EnableScreenshotMaterialEvidenceInfluence}");
        builder.AppendLine($"Influence strength: {configuration.ScreenshotMaterialEvidenceStrength:0.###}");
        builder.AppendLine($"Confidence: {evidence.Confidence:0.###}");
        builder.AppendLine($"FoliageVisible: {evidence.FoliageVisible:0.###}");
        builder.AppendLine($"GrassTerrainVisible: {evidence.GrassTerrainVisible:0.###}");
        builder.AppendLine($"WaterVisible: {evidence.WaterVisible:0.###}");
        builder.AppendLine($"SandVisible: {evidence.SandVisible:0.###}");
        builder.AppendLine($"SnowVisible: {evidence.SnowVisible:0.###}");
        builder.AppendLine($"StoneVisible: {evidence.StoneVisible:0.###}");
        builder.AppendLine($"MetalVisible: {evidence.MetalVisible:0.###}");
        builder.AppendLine($"SkyVisible: {evidence.SkyVisible:0.###}");
        builder.AppendLine($"AetherOrNeonVisible: {evidence.AetherOrNeonVisible:0.###}");
        builder.AppendLine($"SkinOrCharacterVisible: {evidence.SkinOrCharacterVisible:0.###}");
        builder.AppendLine("Current MaterialIntent");
        foreach (var channel in MaterialIntent.ChannelNames)
        {
            builder.AppendLine($"{channel}: {currentIntent.ValueFor(channel):0.###}");
        }

        builder.AppendLine("Mismatches");
        if (diagnostics.Mismatches.Count == 0)
        {
            builder.AppendLine("none");
        }
        else
        {
            foreach (var mismatch in diagnostics.Mismatches)
            {
                builder.AppendLine($"{mismatch.Channel}: severity {mismatch.Severity:0.###}; visible {mismatch.VisibleEvidence:0.###}; intent {mismatch.CurrentIntent:0.###}; {mismatch.Message}");
            }
        }

        return builder.ToString();
    }

    private string NormalFieldSummary()
    {
        var configuration = plugin.Configuration;
        if (!configuration.EnableNormalField)
        {
            return "Disabled";
        }

        return configuration.EnableNormalFieldShaderMapping
            ? $"Enabled, mapping on, strength {configuration.NormalFieldStrength:0.##}"
            : $"Enabled, diagnostics only, strength {configuration.NormalFieldStrength:0.##}";
    }

    private void DrawNormalField()
    {
        var configuration = plugin.Configuration;
        ImGui.TextUnformatted($"Enabled: {(configuration.EnableNormalField ? "yes" : "no")}");
        ImGui.TextUnformatted($"Diagnostics: {(configuration.EnableNormalFieldDiagnostics ? "enabled" : "disabled")}");
        ImGui.TextUnformatted($"Shader mapping: {(configuration.EnableNormalFieldShaderMapping ? "enabled" : "disabled")}");
        ImGui.TextUnformatted($"Strength: {configuration.NormalFieldStrength:0.###}");
        ImGui.TextUnformatted($"Depth/detail/material: {configuration.NormalFieldDepthStrength:0.###} / {configuration.NormalFieldDetailStrength:0.###} / {configuration.NormalFieldMaterialInfluence:0.###}");
        ImGui.TextUnformatted($"Suppression water/skin/sky: {configuration.NormalFieldWaterSuppression:0.###} / {configuration.NormalFieldSkinSuppression:0.###} / {configuration.NormalFieldSkySuppression:0.###}");
        ImGui.TextUnformatted($"Debug mode/boost: {configuration.NormalFieldDebugMode} / {configuration.NormalFieldDebugBoost:0.###}");
        ImGui.TextWrapped("NormalField is an optional screen-space inferred normal/surface field. It is not true game material normals and is zero-impact unless shader mapping is explicitly enabled and matching uniforms exist.");
    }

    private string BasePresetSummary()
    {
        var configuration = plugin.Configuration;
        if (string.IsNullOrWhiteSpace(configuration.BasePresetPath) || !File.Exists(configuration.BasePresetPath))
        {
            return "Base preset missing";
        }

        var name = Path.GetFileName(configuration.BasePresetPath);
        return string.IsNullOrWhiteSpace(name) ? "Base preset selected" : name;
    }

    private Vector4? BasePresetWarningColor()
    {
        var configuration = plugin.Configuration;
        return string.IsNullOrWhiteSpace(configuration.BasePresetPath) || !File.Exists(configuration.BasePresetPath)
            ? WarningColor
            : null;
    }

    private void DrawBasePreset()
    {
        var configuration = plugin.Configuration;

        DrawSetupItem("Base preset selected", !string.IsNullOrWhiteSpace(configuration.BasePresetPath) && File.Exists(configuration.BasePresetPath));
        DrawSetupItem("Generated preset path selected", !string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath));
        DrawSetupItem("Generated preset is separate", !string.Equals(configuration.BasePresetPath, configuration.GeneratedPresetPath, StringComparison.OrdinalIgnoreCase));
        ImGui.TextWrapped($"Base preset: {configuration.BasePresetPath}");
        ImGui.TextWrapped($"Generated preset: {configuration.GeneratedPresetPath}");
        ImGui.TextWrapped(plugin.LastBasePresetLibraryScan.Message);
    }

    private string GenerationSummary()
    {
        var result = plugin.LastWriteResult;
        if (!result.Success)
        {
            return result.Message;
        }

        return $"{result.ChangedVariables} changes, {result.Changes.Count(change => change.HitMin || change.HitMax)} clamped";
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

    private Vector4? GenerationWarningColor()
    {
        var result = plugin.LastWriteResult;
        if (!result.Success || result.ChangedVariables > 50 || result.Changes.Any(change => !string.IsNullOrWhiteSpace(change.Warning)))
        {
            return WarningColor;
        }

        return null;
    }

    private void DrawGeneration()
    {
        var profile = plugin.CurrentProfile;

        if (ImGui.Button("Generate Now###MainGenerateNow"))
        {
            plugin.GenerateNow();
        }

        ImGui.TextWrapped(plugin.LastWriteResult.Message);
        ImGui.Separator();
        ImGui.TextUnformatted($"Exposure Multiplier: {profile.Exposure:0.###}x");
        ImGui.TextUnformatted($"Contrast Multiplier: {profile.Contrast:0.###}x");
        ImGui.TextUnformatted($"Saturation Multiplier: {profile.Saturation:0.###}x");
        ImGui.TextUnformatted($"Bloom Multiplier: {profile.Bloom:0.###}x");
        ImGui.TextUnformatted($"AO Multiplier: {profile.AmbientOcclusion:0.###}x");
        ImGui.TextUnformatted($"AO Radius: {profile.AoRadius:0.###}x");
        ImGui.TextUnformatted($"AO Fade: {profile.AoFadeDistance:0.###}x");
        ImGui.TextUnformatted($"RTGI Multiplier: {profile.Rtgi:0.###}x");
        ImGui.TextUnformatted($"ReLight Multiplier: {profile.ReLight:0.###}x");
        ImGui.TextUnformatted($"Depth Effect Multiplier: {profile.DepthEffects:0.###}x");
        ImGui.TextUnformatted($"Sharpen Multiplier: {profile.Sharpness:0.###}x");
        ImGui.TextUnformatted($"Sharpen Threshold: {profile.SharpenThreshold:0.###}x");
        ImGui.TextUnformatted($"Clarity Multiplier: {profile.Clarity:0.###}x");
        ImGui.TextUnformatted($"Bloom Radius: {profile.BloomRadius:0.###}x");
        ImGui.TextUnformatted($"Bloom Threshold: {profile.BloomThreshold:0.###}x");
        ImGui.TextUnformatted($"Bloom Dirt: {profile.BloomDirt:0.###}x");
        ImGui.TextUnformatted($"Highlight Recovery: {profile.HighlightRecovery:0.###}x");
        ImGui.TextUnformatted($"White Point: {profile.WhitePoint:0.###}x");
        ImGui.TextUnformatted($"Black Point: {profile.BlackPoint:0.###}x");
        ImGui.TextUnformatted($"Deband Strength: {profile.DebandStrength:0.###}x");
        ImGui.TextUnformatted($"AA Strength: {profile.AntiAliasingStrength:0.###}x");
        ImGui.TextUnformatted($"LUT Strength: {profile.LutStrength:0.###}x");
        ImGui.TextUnformatted($"Color Grade Preservation: {profile.ColorGradePreservation:0.###}x");
        ImGui.TextUnformatted($"Shadow Lift: {profile.ShadowLift:0.###}");
        ImGui.TextUnformatted($"Temperature: {profile.Temperature:0.###}");
        ImGui.TextUnformatted($"Tint: {profile.Tint:0.###}");
        ImGui.TextUnformatted($"Shadow Hue/Sat Bias: {profile.ShadowHueBias:0.###} / {profile.ShadowSaturationBias:0.###}");
        ImGui.TextUnformatted($"Midtone Hue/Sat Bias: {profile.MidtoneHueBias:0.###} / {profile.MidtoneSaturationBias:0.###}");
        ImGui.TextUnformatted($"Highlight Hue/Sat Bias: {profile.HighlightHueBias:0.###} / {profile.HighlightSaturationBias:0.###}");

        if (ImGui.TreeNode("Color family adjustments###MainColorFamilyAdjustments"))
        {
            var strongest = profile.StrongestColorFamilyAdjustments(8);
            if (strongest.Count == 0)
            {
                ImGui.TextUnformatted("No active color family adjustments.");
            }

            foreach (var adjustment in strongest)
            {
                ImGui.BulletText($"{adjustment.Family}: H {adjustment.Hue:+0.000;-0.000;0.000}, S {adjustment.Saturation:+0.000;-0.000;0.000}, L {adjustment.Luminance:+0.000;-0.000;0.000}, confidence {adjustment.Confidence:0.00}");
            }

            ImGui.TreePop();
        }
    }

    private string ReShadeReloadSummary()
    {
        var diagnostics = plugin.LastReloadResult.Diagnostics;
        return $"Hotkey {diagnostics.ConfiguredReloadKey}, ReShade.ini {(diagnostics.ReShadeIniFound ? "found" : "not found")}";
    }

    private Vector4? ReShadeReloadWarningColor()
    {
        return plugin.LastReloadResult.Diagnostics.ReShadeIniFound ? null : WarningColor;
    }

    private void DrawReShadeReload()
    {
        if (ImGui.Button("Test Reload###MainTestReload"))
        {
            plugin.ReloadShadersNow();
        }

        var diagnostics = plugin.LastReloadResult.Diagnostics;
        ImGui.TextWrapped(plugin.LastReloadResult.Message);
        ImGui.TextWrapped($"ReShade.ini: {(diagnostics.ReShadeIniFound ? diagnostics.ReShadeIniPath : "not found")}");
        ImGui.TextWrapped($"KeyReload: {diagnostics.KeyReloadValue}; configured: {diagnostics.ConfiguredReloadKey}; sync: {(diagnostics.HotkeySyncEnabled ? "on" : "off")}");
        ImGui.TextWrapped($"PostMessage: {(diagnostics.PostMessageSucceeded ? "ok" : "failed")}; SendInput: {(diagnostics.SendInputSucceeded ? "ok" : "failed")}");
    }

    private string PresetCompatibilitySummary()
    {
        var report = plugin.LastPresetAnalysis.Report;
        return $"Risk: {report.Level}, {report.Warnings.Count} warnings";
    }

    private Vector4? PresetCompatibilityWarningColor()
    {
        return plugin.LastPresetAnalysis.Report.Level switch
        {
            PresetRiskLevel.VeryHigh => DangerColor,
            PresetRiskLevel.High => WarningColor,
            _ => plugin.LastPresetAnalysis.Report.Warnings.Count > 0 ? WarningColor : null
        };
    }

    private void DrawPresetCompatibility()
    {
        var analysis = plugin.LastPresetAnalysis;
        var report = analysis.Report;

        if (ImGui.Button("Scan Preset Compatibility###MainScanPreset"))
        {
            plugin.ScanPresetCompatibility();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Report###MainExportCompatibilityReport"))
        {
            plugin.ExportCompatibilityReport();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Debug Bundle###MainExportDebugBundle"))
        {
            plugin.ExportDebugBundle();
        }

        ImGui.TextWrapped(analysis.Message);
        ImGui.TextWrapped(plugin.LastDiagnosticsExportMessage);
        ImGui.TextUnformatted($"Risk: {report.Level}");
        ImGui.TextUnformatted($"Selected mode: {PresetAnalyzer.FormatCompatibilityMode(plugin.Configuration.CompatibilityMode)}");
        ImGui.TextUnformatted($"Recommended mode: {PresetAnalyzer.FormatCompatibilityMode(report.RecommendedCompatibilityMode)}");
        ImGui.TextUnformatted($"Active controlled: {report.ActiveSupportedEffects.Count}");
        ImGui.TextUnformatted($"Active partial: {report.ActivePartiallySupportedEffects.Count}");
        ImGui.TextUnformatted($"Active detected-only: {report.ActiveDetectedOnlyEffects.Count}");
        ImGui.TextUnformatted($"Active unsupported: {report.ActiveUnsupportedEffects.Count}");
        ImGui.TextUnformatted($"High-risk active: {report.HighRiskActiveEffects.Count}");

        DrawTechniqueTree("Active controlled effects", report.ActiveSupportedEffects, report.ActivePartiallySupportedEffects);
        DrawTechniqueTree("Active detected-only effects", report.ActiveDetectedOnlyEffects);
        DrawAuthoritiesTree(report.Authorities);
        DrawTechniqueTree("Active unknown/unsupported effects", report.ActiveUnsupportedEffects);
        DrawTechniqueTree("High-risk effects", report.HighRiskActiveEffects);
        DrawWarningsTree(report.Warnings);
    }

    private string ChangedVariablesSummary()
    {
        var changes = plugin.LastWriteResult.Changes;
        var clamped = changes.Count(change => change.HitMin || change.HitMax);
        var warnings = changes.Count(change => !string.IsNullOrWhiteSpace(change.Warning));
        return $"{changes.Count} changes, {clamped} clamped, {warnings} warnings";
    }

    private Vector4? ChangedVariablesWarningColor()
    {
        var changes = plugin.LastWriteResult.Changes;
        return changes.Count > 50 || changes.Any(change => change.HitMin || change.HitMax || !string.IsNullOrWhiteSpace(change.Warning))
            ? WarningColor
            : null;
    }

    private void DrawChangedVariables()
    {
        if (plugin.LastWriteResult.Changes.Count == 0)
        {
            ImGui.TextUnformatted("No changed variables recorded yet.");
        }

        foreach (var change in plugin.LastWriteResult.Changes)
        {
            var activation = PresetAnalyzer.FormatActivationState(change.ActivationState);
            var clamp = change.HitMin ? ", min clamp" : change.HitMax ? ", max clamp" : string.Empty;
            ImGui.BulletText($"{change.Section} / {change.Key}: {change.OldValue} -> {change.NewValue} ({activation}{clamp})");
            ImGui.SameLine();
            ImGui.TextDisabled(change.ReasonCategory);
            if (!string.IsNullOrWhiteSpace(change.Warning))
            {
                ImGui.TextWrapped(change.Warning);
            }
        }
    }

    private string SanitizeActionsSummary()
    {
        return $"{plugin.LastWriteResult.SanitizeActions.Count} actions";
    }

    private void DrawSanitizeActions()
    {
        if (plugin.LastWriteResult.SanitizeActions.Count == 0)
        {
            ImGui.TextUnformatted("No sanitize actions recorded yet.");
        }

        foreach (var action in plugin.LastWriteResult.SanitizeActions)
        {
            var activation = PresetAnalyzer.FormatActivationState(action.ActivationState);
            ImGui.BulletText($"{action.Section} / {action.Key}: {action.OldValue} -> {action.NewValue} ({action.ActionType}, {PresetAnalyzer.FormatRole(action.Role)}, {activation})");
            ImGui.TextDisabled(action.Reason);
        }
    }

    private void DrawAppliedRules()
    {
        foreach (var rule in plugin.CurrentRules)
        {
            ImGui.BulletText($"{rule.Name}: {rule.Changes}");
            ImGui.TextWrapped(rule.Reason);
        }
    }

    private string ScreenshotSummary()
    {
        var image = plugin.CurrentImageAnalysis;
        return image.Available ? $"Available: {image.ProfileBucket}" : plugin.ImageAnalysisMessage;
    }

    private void DrawScreenshotAnalysis()
    {
        var image = plugin.CurrentImageAnalysis;
        ImGui.TextWrapped(plugin.ImageAnalysisMessage);
        if (!image.Available)
        {
            return;
        }

        ImGui.TextWrapped(image.OpinionSummary);
        ImGui.TextUnformatted($"Image Luma: {image.AverageLuminance:0.###}");
        ImGui.TextUnformatted($"Image Contrast: {image.Contrast:0.###}");
        ImGui.TextUnformatted($"Image Saturation: {image.AverageSaturation:0.###}");
        ImGui.TextUnformatted($"Image Warmth: {image.Warmth:0.###}");
        ImGui.TextUnformatted($"Shadow Clip: {image.ShadowClipping:P1}");
        ImGui.TextUnformatted($"Highlight Clip: {image.HighlightClipping:P1}");
        ImGui.TextUnformatted($"Image Tonal P05/P50/P95: {image.LuminanceP05:0.###} / {image.LuminanceP50:0.###} / {image.LuminanceP95:0.###}");
        ImGui.TextUnformatted($"Image Tonal Spread: {image.ContrastSpread:0.###}");
        ImGui.TextUnformatted($"Image Shadow H/S/W/T: {image.ShadowColor.Hue:0.###} / {image.ShadowColor.Saturation:0.###} / {image.ShadowColor.Warmth:0.###} / {image.ShadowColor.Tint:0.###}");
        ImGui.TextUnformatted($"Image Midtone H/S/W/T: {image.MidtoneColor.Hue:0.###} / {image.MidtoneColor.Saturation:0.###} / {image.MidtoneColor.Warmth:0.###} / {image.MidtoneColor.Tint:0.###}");
        ImGui.TextUnformatted($"Image Highlight H/S/W/T: {image.HighlightColor.Hue:0.###} / {image.HighlightColor.Saturation:0.###} / {image.HighlightColor.Warmth:0.###} / {image.HighlightColor.Tint:0.###}");
        if (ImGui.TreeNode("Image color families###MainImageColorFamilies"))
        {
            DrawColorFamilyStats(image.ColorFamilies);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Screenshot opinions###MainScreenshotOpinions"))
        {
            DrawScreenshotOpinions(image);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Screenshot regions###MainScreenshotRegions"))
        {
            DrawScreenshotRegions(image);
            ImGui.TreePop();
        }

        ImGui.TextUnformatted($"Image Metrics: {image.MetricsKey}");
    }

    private string MasterStyleSummary()
    {
        var configuration = plugin.Configuration;
        if (!configuration.MatchMasterPresetStyle)
        {
            return "Disabled";
        }

        return plugin.CurrentMasterStyle.Available
            ? $"Active, raw {configuration.MasterPresetStyleStrength}%, effective {plugin.CurrentMasterStyleDiagnostics.EffectiveStrength:P0}"
            : plugin.MasterStyleMessage;
    }

    private void DrawMasterStyle()
    {
        var configuration = plugin.Configuration;
        var diagnostics = plugin.CurrentMasterStyleDiagnostics;
        var current = plugin.CurrentImageAnalysis;
        var master = plugin.CurrentMasterStyle;
        ImGui.TextUnformatted($"Master style enabled: {(configuration.MatchMasterPresetStyle ? "yes" : "no")}");
        ImGui.TextUnformatted($"Master analysis available: {(master.Available ? "yes" : "no")}");
        ImGui.TextUnformatted($"Current image available: {(current.Available ? "yes" : "no")}");
        ImGui.TextUnformatted($"Master image count: {diagnostics.MasterImageCount}");
        ImGui.TextUnformatted($"Selected master mode: {diagnostics.MasterMode}");
        ImGui.TextUnformatted($"Raw strength: {diagnostics.RawStrength}%");
        ImGui.TextUnformatted($"Effective strength: {diagnostics.EffectiveStrength:0.###}");
        ImGui.TextUnformatted($"Formula: {diagnostics.RawStrength / 100f:0.###} x {diagnostics.SceneSimilarityMultiplier:0.###} x {diagnostics.CompatibilityModeMultiplier:0.###} = {diagnostics.EffectiveStrength:0.###}");
        ImGui.TextUnformatted($"Scene similarity multiplier: {diagnostics.SceneSimilarityMultiplier:0.###}");
        ImGui.TextUnformatted($"Compatibility-mode multiplier: {diagnostics.CompatibilityModeMultiplier:0.###}");
        ImGui.TextWrapped(diagnostics.Status);
        ImGui.TextWrapped(plugin.MasterStyleMessage);
        if (!master.Available)
        {
            return;
        }

        if (current.Available)
        {
            ImGui.Separator();
            ImGui.TextUnformatted("Current vs master tonal percentiles");
            ImGui.TextUnformatted($"P05: {current.LuminanceP05:0.###} -> {master.LuminanceP05:0.###}");
            ImGui.TextUnformatted($"P25: {current.LuminanceP25:0.###} -> {master.LuminanceP25:0.###}");
            ImGui.TextUnformatted($"P50: {current.LuminanceP50:0.###} -> {master.LuminanceP50:0.###}");
            ImGui.TextUnformatted($"P75: {current.LuminanceP75:0.###} -> {master.LuminanceP75:0.###}");
            ImGui.TextUnformatted($"P95: {current.LuminanceP95:0.###} -> {master.LuminanceP95:0.###}");
            ImGui.TextUnformatted($"Contrast spread: {current.ContrastSpread:0.###} -> {master.ContrastSpread:0.###}");
            ImGui.TextUnformatted($"Shadow floor: {current.ShadowFloor:0.###} -> {master.ShadowFloor:0.###}");
            ImGui.TextUnformatted($"Highlight ceiling: {current.HighlightCeiling:0.###} -> {master.HighlightCeiling:0.###}");
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Generated tonal deltas");
        ImGui.TextUnformatted($"Exposure delta: {diagnostics.TonalDeltas.Exposure:+0.000;-0.000;0.000}");
        ImGui.TextUnformatted($"ShadowLift delta: {diagnostics.TonalDeltas.ShadowLift:+0.000;-0.000;0.000}");
        ImGui.TextUnformatted($"BlackPoint delta: {diagnostics.TonalDeltas.BlackPoint:+0.000;-0.000;0.000}");
        ImGui.TextUnformatted($"WhitePoint delta: {diagnostics.TonalDeltas.WhitePoint:+0.000;-0.000;0.000}");
        ImGui.TextUnformatted($"HighlightRecovery delta: {diagnostics.TonalDeltas.HighlightRecovery:+0.000;-0.000;0.000}");
        ImGui.TextUnformatted($"Contrast delta: {diagnostics.TonalDeltas.Contrast:+0.000;-0.000;0.000}");
        ImGui.TextUnformatted($"MidtoneContrast delta: {diagnostics.TonalDeltas.MidtoneContrast:+0.000;-0.000;0.000}");

        ImGui.Separator();
        ImGui.TextUnformatted("Generated tonal color bias");
        ImGui.TextUnformatted($"Shadow hue/sat: {diagnostics.ShadowHueBias:+0.000;-0.000;0.000} / {diagnostics.ShadowSaturationBias:+0.000;-0.000;0.000}");
        ImGui.TextUnformatted($"Midtone hue/sat: {diagnostics.MidtoneHueBias:+0.000;-0.000;0.000} / {diagnostics.MidtoneSaturationBias:+0.000;-0.000;0.000}");
        ImGui.TextUnformatted($"Highlight hue/sat: {diagnostics.HighlightHueBias:+0.000;-0.000;0.000} / {diagnostics.HighlightSaturationBias:+0.000;-0.000;0.000}");

        ImGui.Separator();
        ImGui.TextUnformatted("Strongest color-family adjustments");
        if (diagnostics.StrongestColorFamilyAdjustments.Count == 0)
        {
            ImGui.TextUnformatted("No active color-family adjustments.");
        }

        foreach (var adjustment in diagnostics.StrongestColorFamilyAdjustments)
        {
            ImGui.BulletText($"{adjustment.Family}: confidence {adjustment.Confidence:0.##}, hue {adjustment.Hue:+0.000;-0.000;0.000}, sat {adjustment.Saturation:+0.000;-0.000;0.000}, lum {adjustment.Luminance:+0.000;-0.000;0.000}");
        }

        if (current.Available && master.Available && ImGui.TreeNode("Color-family comparison###MainMasterColorFamilyComparison"))
        {
            DrawColorFamilyComparison(current, master, plugin.CurrentProfile);
            ImGui.TreePop();
        }

        ImGui.Separator();
        ImGui.TextUnformatted($"Master Luma: {master.AverageLuminance:0.###}");
        ImGui.TextUnformatted($"Master Contrast: {master.Contrast:0.###}");
        ImGui.TextUnformatted($"Master Saturation: {master.AverageSaturation:0.###}");
        ImGui.TextUnformatted($"Master Warmth: {master.Warmth:0.###}");
        ImGui.TextUnformatted($"Master Tonal P05/P50/P95: {master.LuminanceP05:0.###} / {master.LuminanceP50:0.###} / {master.LuminanceP95:0.###}");
        ImGui.TextUnformatted($"Master Tonal Spread: {master.ContrastSpread:0.###}");
        ImGui.TextUnformatted($"Master Shadow H/S/W/T: {master.ShadowColor.Hue:0.###} / {master.ShadowColor.Saturation:0.###} / {master.ShadowColor.Warmth:0.###} / {master.ShadowColor.Tint:0.###}");
        ImGui.TextUnformatted($"Master Midtone H/S/W/T: {master.MidtoneColor.Hue:0.###} / {master.MidtoneColor.Saturation:0.###} / {master.MidtoneColor.Warmth:0.###} / {master.MidtoneColor.Tint:0.###}");
        ImGui.TextUnformatted($"Master Highlight H/S/W/T: {master.HighlightColor.Hue:0.###} / {master.HighlightColor.Saturation:0.###} / {master.HighlightColor.Warmth:0.###} / {master.HighlightColor.Tint:0.###}");
        if (ImGui.TreeNode("Master color families###MainMasterColorFamilies"))
        {
            DrawColorFamilyStats(master.ColorFamilies);
            ImGui.TreePop();
        }

        ImGui.TextUnformatted($"Master Metrics: {master.MetricsKey}");
    }

    private string RegressionSummary()
    {
        return plugin.LastPresetRegressionReport.Success
            ? $"Last run: {plugin.LastPresetRegressionReport.PresetCount} presets"
            : plugin.LastPresetRegressionReport.Message;
    }

    private void DrawRegressionReports()
    {
        if (ImGui.Button("Run Preset Regression Reports###MainRunRegressionReports"))
        {
            plugin.RunPresetRegressionReports();
        }

        ImGui.TextWrapped(plugin.LastPresetRegressionReport.Message);
    }

    private string DebugSummary()
    {
        var customDiagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(plugin.Configuration, plugin.LastShaderSupportScan, plugin.LastWriteResult, plugin.LastPresetAnalysis);
        var customSummary = customDiagnostics.SectionFound ? ", custom shader section present" : ", no custom shader section";
        return $"{plugin.LastShaderSupportScan.Items.Count} supported variables detected{customSummary}";
    }

    private void DrawDebugDiagnostics()
    {
        DrawSetupItem("Shader variables detected", plugin.LastShaderSupportScan.Items.Count > 0);
        DrawSetupItem("Generated at least once", plugin.LastWriteResult.Success);
        DrawSetupItem("Reload attempted", plugin.LastReloadResult.Success);
        ImGui.TextUnformatted($"Dalashade custom shader support: {(plugin.Configuration.EnableDalashadeCustomShaders ? "enabled" : "disabled")}");
        DrawCustomShaderDiagnostics();
        ImGui.TextWrapped(plugin.LastDiagnosticsExportMessage);
        ImGui.TextWrapped("Dalashade only edits known variables present in generated preset content. Keep iMMERSE and any Pro/Ultimate shaders installed through ReShade; this plugin does not ship those files.");

        if (ImGui.TreeNode("Detected shader support###MainDetectedShaderSupport"))
        {
            ImGui.TextWrapped(plugin.LastShaderSupportScan.Message);
            foreach (var item in plugin.LastShaderSupportScan.Items)
            {
                ImGui.BulletText($"{item.Section} / {item.Key} ({item.ReasonCategory}, {PresetAnalyzer.FormatActivationState(item.ActivationState)})");
            }

            ImGui.TreePop();
        }
    }

    private void DrawCustomShaderDiagnostics()
    {
        var diagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(plugin.Configuration, plugin.LastShaderSupportScan, plugin.LastWriteResult, plugin.LastPresetAnalysis);

        if (!ImGui.TreeNode("Dalashade custom shaders###MainCustomShaderDiagnostics"))
        {
            return;
        }

        DrawSetupItem("Custom shader support enabled", diagnostics.SupportEnabled);
        DrawSetupItem("Auto-inject generated preset sections enabled", diagnostics.AutoInjectionEnabled);
        DrawSetupItem("Injection is generated preset only", diagnostics.GeneratedPresetOnlyInjection);
        DrawSetupItem("Generated preset sections injected", diagnostics.SectionInjected);
        DrawSetupItem("Generated preset variables injected", diagnostics.VariablesInjected);
        DrawSetupItem("Technique activation remains manual", !diagnostics.TechniqueInjected);
        DrawSetupItem("Base preset contains Dalashade custom shader section", diagnostics.SectionFound);
        DrawSetupItem("Known custom variables found", diagnostics.KnownVariablesFound);
        DrawSetupItem("SceneIntent values written into generated preset", diagnostics.ValuesWritten);
        ImGui.TextWrapped($"SmartSharpen authority: {diagnostics.SmartSharpenAuthority.Level.ToString().ToLowerInvariant()} ({diagnostics.SmartSharpenAuthority.ShaderValue:0})");
        ImGui.TextWrapped(diagnostics.SmartSharpenAuthority.Reason);
        if (diagnostics.SmartSharpenAuthority.OtherActiveSharpeners.Count > 0)
        {
            ImGui.TextWrapped($"Other active sharpeners: {string.Join(", ", diagnostics.SmartSharpenAuthority.OtherActiveSharpeners)}");
        }
        ImGui.TextWrapped("Dalashade writes custom shader variables into existing or generated-preset-injected Dalashade custom shader sections. The base preset is never modified.");
        ImGui.TextWrapped("Technique activation remains manual. Install needed Dalashade .fx files in a ReShade shader search folder separately, then enable wanted techniques in ReShade.");
        ImGui.Separator();

        foreach (var message in diagnostics.StatusMessages)
        {
            ImGui.BulletText(message);
        }

        var injection = plugin.LastWriteResult.CustomShaderInjection;
        if (injection.Sections.Count > 0)
        {
            ImGui.TextWrapped($"Generated preset injected sections: {string.Join(", ", injection.Sections)}");
        }

        if (injection.Variables.Count > 0 && ImGui.TreeNode("Injected custom variables###MainInjectedCustomShaderVariables"))
        {
            foreach (var variable in injection.Variables)
            {
                ImGui.BulletText(variable);
            }

            ImGui.TreePop();
        }

        if (injection.Techniques.Count > 0)
        {
            ImGui.TextWrapped($"Generated preset injected techniques: {string.Join(", ", injection.Techniques)}");
        }
        else if (injection.Attempted)
        {
            ImGui.TextWrapped("Generated preset injected techniques: none. Auto-injection only adds sections and variables.");
        }

        if (diagnostics.Sections.Count == 0)
        {
            ImGui.TextUnformatted("No Dalashade custom shader sections detected in the current base preset.");
        }
        else
        {
            foreach (var section in diagnostics.Sections)
            {
                ImGui.BulletText($"Section: {section.Section} | technique listed={(section.TechniqueAppearsInTechniques ? "yes" : "no")} | activation={PresetAnalyzer.FormatActivationState(section.ActivationState)}");
            }
        }

        if (diagnostics.KnownVariables.Count == 0)
        {
            ImGui.TextUnformatted("No supported Dalashade custom shader variables detected in the current base preset.");
        }
        else if (ImGui.TreeNode("Detected custom variables###MainCustomShaderVariables"))
        {
            foreach (var item in diagnostics.KnownVariables)
            {
                ImGui.BulletText($"{item.Section} / {item.Key} | activation={PresetAnalyzer.FormatActivationState(item.ActivationState)} | controllable={(item.Controllable ? "yes" : "no")} | written={(item.Written ? "yes" : "no")}");
            }

            ImGui.TreePop();
        }

        if (diagnostics.WrittenVariables.Count == 0)
        {
            ImGui.TextUnformatted("No Dalashade custom shader variables written yet.");
        }
        else
        {
            foreach (var change in diagnostics.WrittenVariables)
            {
                ImGui.BulletText($"{change.Section} / {change.Key}: {change.OldValue} -> {change.NewValue}");
            }
        }

        ImGui.TreePop();
    }

    private static void DrawTechniqueTree(string title, params IReadOnlyList<PresetTechnique>[] groups)
    {
        if (!ImGui.TreeNode($"{title}###Main{title.Replace(" ", string.Empty, StringComparison.Ordinal)}"))
        {
            return;
        }

        var any = false;
        foreach (var group in groups)
        {
            foreach (var technique in group)
            {
                any = true;
                ImGui.BulletText($"{PresetAnalyzer.FormatTechnique(technique)} ({PresetAnalyzer.FormatActivationState(technique.ActivationState)}, {PresetAnalyzer.FormatRole(technique.Role)}, {PresetAnalyzer.FormatRisk(technique.Risk)}, {technique.SupportLevel})");
            }
        }

        if (!any)
        {
            ImGui.TextUnformatted("None.");
        }

        ImGui.TreePop();
    }

    private static void DrawAuthoritiesTree(IReadOnlyList<EffectAuthority> authorities)
    {
        if (!ImGui.TreeNode("Effect authorities###MainEffectAuthorities"))
        {
            return;
        }

        foreach (var authority in authorities)
        {
            ImGui.BulletText($"{PresetAnalyzer.FormatRole(authority.Role)}: {authority.PrimaryShader}");
            foreach (var secondary in authority.SecondaryShaders)
            {
                ImGui.TextWrapped($"  secondary: {secondary}");
            }
        }

        if (authorities.Count == 0)
        {
            ImGui.TextUnformatted("None.");
        }

        ImGui.TreePop();
    }

    private static void DrawWarningsTree(IReadOnlyList<string> warnings)
    {
        if (!ImGui.TreeNode("Warnings###MainCompatibilityWarnings"))
        {
            return;
        }

        if (warnings.Count == 0)
        {
            ImGui.TextUnformatted("No preset compatibility warnings yet.");
        }

        foreach (var warning in warnings)
        {
            ImGui.BulletText(warning);
        }

        ImGui.TreePop();
    }

    private static void DrawSetupItem(string label, bool complete)
    {
        ImGui.BulletText($"{(complete ? "OK" : "Missing")} - {label}");
    }

    private void DrawCheckbox(string label, bool currentValue, Action<bool> update)
    {
        var value = currentValue;
        if (ImGui.Checkbox(label, ref value))
        {
            update(value);
            plugin.Configuration.Save();
        }
    }

    private void DrawFloatSlider(string label, float currentValue, float min, float max, Action<float> update)
    {
        var value = currentValue;
        if (ImGui.SliderFloat(label, ref value, min, max, "%.3f"))
        {
            update(value);
            plugin.Configuration.Save();
        }
    }

    private static void DrawColorFamilyStats(IReadOnlyDictionary<ColorFamily, ColorFamilyStats> families)
    {
        foreach (var family in Enum.GetValues<ColorFamily>())
        {
            var stats = families.TryGetValue(family, out var value) ? value : ColorFamilyStats.Empty(family);
            if (stats.Confidence <= 0.02f)
            {
                continue;
            }

            ImGui.BulletText($"{family}: H {stats.Hue:0.###}, S {stats.Saturation:0.###}, L {stats.Luminance:0.###}, coverage {stats.Coverage:P1}, confidence {stats.Confidence:0.##}");
        }
    }

    private static void DrawScreenshotOpinions(ImageAnalysisResult image)
    {
        if (image.Opinions.Count == 0)
        {
            ImGui.TextUnformatted("No confident screenshot opinions.");
            return;
        }

        foreach (var opinion in image.Opinions.OrderByDescending(opinion => opinion.Confidence))
        {
            ImGui.BulletText($"{opinion.Label}: {opinion.Confidence:0.##}");
            ImGui.TextWrapped($"{opinion.Target}. {opinion.Reason}");
        }
    }

    private static void DrawScreenshotRegions(ImageAnalysisResult image)
    {
        foreach (var region in Enum.GetValues<ImageAnalysisRegion>())
        {
            var stats = image.Regions.TryGetValue(region, out var value) ? value : ImageRegionStats.Empty(region);
            ImGui.BulletText($"{region}: L {stats.AverageLuminance:0.###}, C {stats.Contrast:0.###}, S {stats.AverageSaturation:0.###}, bright {stats.BrightTendency:0.##}, dark {stats.DarkTendency:0.##}, smooth {stats.SmoothTendency:0.##}");
            var topFamilies = stats.ColorFamilies.Values
                .Where(family => family.Confidence > 0.05f)
                .OrderByDescending(family => family.Confidence)
                .Take(4)
                .Select(family => $"{family.Family} {family.Confidence:0.##}")
                .ToArray();
            ImGui.TextDisabled(topFamilies.Length == 0 ? "Top colors: none" : $"Top colors: {string.Join(", ", topFamilies)}");
        }
    }

    private void DrawColorFamilyComparison(ImageAnalysisResult current, ImageAnalysisResult master, VisualProfile profile)
    {
        ImGui.Checkbox("Show all color families###MainShowAllMasterColorFamilies", ref showAllMasterColorFamilies);

        var rows = ColorFamilyComparisonRows.Build(current, master, profile, showAllMasterColorFamilies);

        if (rows.Count == 0)
        {
            ImGui.TextUnformatted("No confident color-family matches or generated adjustments.");
            return;
        }

        if (!ImGui.BeginTable("MasterColorFamilyComparisonTable", 4))
        {
            return;
        }

        ImGui.TableSetupColumn("Family");
        ImGui.TableSetupColumn("Current H/S/L/C");
        ImGui.TableSetupColumn("Master H/S/L/C");
        ImGui.TableSetupColumn("Generated H/S/L");
        ImGui.TableHeadersRow();

        foreach (var row in rows)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(row.Family.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{row.Current.Hue:0.###} / {row.Current.Saturation:0.###} / {row.Current.Luminance:0.###} / {row.Current.Confidence:0.##}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{row.Master.Hue:0.###} / {row.Master.Saturation:0.###} / {row.Master.Luminance:0.###} / {row.Master.Confidence:0.##}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{row.Adjustment.Hue:+0.000;-0.000;0.000} / {row.Adjustment.Saturation:+0.000;-0.000;0.000} / {row.Adjustment.Luminance:+0.000;-0.000;0.000}");
        }

        ImGui.EndTable();
    }
}
