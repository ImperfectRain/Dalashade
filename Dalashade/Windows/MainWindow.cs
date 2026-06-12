using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Dalashade.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private static readonly Vector4 WarningColor = new(1.0f, 0.72f, 0.28f, 1.0f);
    private static readonly Vector4 DangerColor = new(1.0f, 0.38f, 0.34f, 1.0f);

    private readonly Plugin plugin;
    private bool showAllMasterColorFamilies;

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

        ImGui.Spacing();

        UiSection.Draw("MainCurrentStatus", "Current Status", true, CurrentStatusSummary(), DrawCurrentStatus);
        UiSection.Draw("MainSceneTags", "Scene Tags", true, SceneTagsSummary(), DrawSceneTags);
        UiSection.Draw("MainBasePreset", "Base Preset", true, BasePresetSummary(), DrawBasePreset, BasePresetWarningColor());
        UiSection.Draw("MainGeneration", "Generation", true, GenerationSummary(), DrawGeneration, GenerationWarningColor());
        UiSection.Draw("MainReShadeReload", "ReShade Reload", true, ReShadeReloadSummary(), DrawReShadeReload, ReShadeReloadWarningColor());

        var compatibilityDefaultOpen = plugin.LastPresetAnalysis.Report.Level is PresetRiskLevel.High or PresetRiskLevel.VeryHigh;
        UiSection.Draw("MainPresetCompatibility", "Preset Compatibility", compatibilityDefaultOpen, PresetCompatibilitySummary(), DrawPresetCompatibility, PresetCompatibilityWarningColor());
        UiSection.Draw("MainChangedVariables", "Changed Variables", false, ChangedVariablesSummary(), DrawChangedVariables, ChangedVariablesWarningColor());
        UiSection.Draw("MainSanitizeActions", "Sanitize Actions", false, SanitizeActionsSummary(), DrawSanitizeActions);
        UiSection.Draw("MainAppliedRules", "Applied Rules", false, $"{plugin.CurrentRules.Count} rules applied", DrawAppliedRules);
        UiSection.Draw("MainScreenshotAnalysis", "Screenshot Analysis", false, ScreenshotSummary(), DrawScreenshotAnalysis);
        UiSection.Draw("MainMasterStyle", "Master Style", false, MasterStyleSummary(), DrawMasterStyle);
        UiSection.Draw("MainRegressionReports", "Regression Reports", false, RegressionSummary(), DrawRegressionReports);
        UiSection.Draw("MainDebugDiagnostics", "Debug / Diagnostics", false, DebugSummary(), DrawDebugDiagnostics);
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
        ImGui.TextWrapped($"Mood tags: {(diagnostics.MoodTags.Count == 0 ? "none" : string.Join(", ", diagnostics.MoodTags))}");
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

        ImGui.TextWrapped(analysis.Message);
        ImGui.TextWrapped(plugin.LastCompatibilityReportExport.Message);
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
        var customDiagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(plugin.Configuration, plugin.LastShaderSupportScan, plugin.LastWriteResult);
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
        ImGui.TextWrapped(plugin.LastCompatibilityReportExport.Message);
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
        var diagnostics = CustomShaderBridgeDiagnosticsBuilder.Build(plugin.Configuration, plugin.LastShaderSupportScan, plugin.LastWriteResult);

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
