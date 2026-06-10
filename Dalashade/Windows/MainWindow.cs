using System;
using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Dalashade.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

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
        var context = plugin.CurrentContext;
        var tags = plugin.CurrentTags;
        var profile = plugin.CurrentProfile;

        if (ImGui.Button("Settings"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.SameLine();

        if (ImGui.Button("Generate Now"))
        {
            plugin.GenerateNow();
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Setup check"))
        {
            var configuration = plugin.Configuration;
            DrawSetupItem("Base preset selected", !string.IsNullOrWhiteSpace(configuration.BasePresetPath) && File.Exists(configuration.BasePresetPath));
            DrawSetupItem("Generated preset path selected", !string.IsNullOrWhiteSpace(configuration.GeneratedPresetPath));
            DrawSetupItem("Generated preset is separate", !string.Equals(configuration.BasePresetPath, configuration.GeneratedPresetPath, StringComparison.OrdinalIgnoreCase));
            DrawSetupItem("Shader variables detected", plugin.LastShaderSupportScan.Items.Count > 0);
            DrawSetupItem("Generated at least once", plugin.LastWriteResult.Success);
            DrawSetupItem("Reload attempted", plugin.LastReloadResult.Success);

            if (ImGui.Button("Scan Preset###MainScanPreset"))
            {
                plugin.ScanPresetCompatibility();
            }

            ImGui.SameLine();
            if (ImGui.Button("Export Report###MainExportCompatibilityReport"))
            {
                plugin.ExportCompatibilityReport();
            }

            ImGui.SameLine();
            if (ImGui.Button("Test Reload###MainTestReload"))
            {
                plugin.ReloadShadersNow();
            }
        }

        ImGui.Separator();

        ImGui.TextUnformatted($"Territory: {context.TerritoryName} ({context.TerritoryId})");
        ImGui.TextUnformatted($"World: {context.WorldCategory}");
        ImGui.TextUnformatted($"Content: {context.ContentName} ({context.ContentType})");
        ImGui.TextUnformatted($"Weather: {context.WeatherName} ({tags.WeatherKey})");
        ImGui.TextUnformatted($"Time: {context.EorzeaHour:0.0}h ({context.TimeBucket})");
        ImGui.TextUnformatted($"Combat: {context.InCombat}");
        ImGui.TextUnformatted($"Duty: {context.InDuty}");
        ImGui.TextUnformatted($"GPose: {context.InGpose}");
        ImGui.TextUnformatted($"Cutscene: {context.InCutscene}");
        ImGui.TextUnformatted($"Scene Lock: {plugin.Configuration.SceneLockEnabled}");
        ImGui.TextUnformatted($"Scene Tags: {tags.AreaKey}, {tags.BiomeKey}, clarity={tags.NeedsGameplayClarity}, cinematic={tags.CinematicAllowed}");

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

        ImGui.Separator();

        var image = plugin.CurrentImageAnalysis;
        ImGui.TextWrapped(plugin.ImageAnalysisMessage);
        if (image.Available)
        {
            ImGui.TextUnformatted($"Image Luma: {image.AverageLuminance:0.###}");
            ImGui.TextUnformatted($"Image Contrast: {image.Contrast:0.###}");
            ImGui.TextUnformatted($"Image Saturation: {image.AverageSaturation:0.###}");
            ImGui.TextUnformatted($"Image Warmth: {image.Warmth:0.###}");
            ImGui.TextUnformatted($"Shadow Clip: {image.ShadowClipping:P1}");
            ImGui.TextUnformatted($"Highlight Clip: {image.HighlightClipping:P1}");
            ImGui.TextUnformatted($"Image Metrics: {image.MetricsKey}");
        }

        ImGui.Separator();

        var master = plugin.CurrentMasterStyle;
        ImGui.TextWrapped(plugin.MasterStyleMessage);
        if (master.Available)
        {
            ImGui.TextUnformatted($"Master Luma: {master.AverageLuminance:0.###}");
            ImGui.TextUnformatted($"Master Contrast: {master.Contrast:0.###}");
            ImGui.TextUnformatted($"Master Saturation: {master.AverageSaturation:0.###}");
            ImGui.TextUnformatted($"Master Warmth: {master.Warmth:0.###}");
            ImGui.TextUnformatted($"Master Metrics: {master.MetricsKey}");
        }

        ImGui.Separator();

        ImGui.TextUnformatted("Applied rules");
        foreach (var rule in plugin.CurrentRules)
        {
            ImGui.BulletText($"{rule.Name}: {rule.Changes}");
            ImGui.TextWrapped(rule.Reason);
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Preset compatibility"))
        {
            var analysis = plugin.LastPresetAnalysis;
            var report = analysis.Report;
            ImGui.TextWrapped(analysis.Message);
            ImGui.TextUnformatted($"Risk: {report.Level}");
            ImGui.TextUnformatted($"Selected mode: {PresetAnalyzer.FormatCompatibilityMode(plugin.Configuration.CompatibilityMode)}");
            ImGui.TextUnformatted($"Recommended mode: {PresetAnalyzer.FormatCompatibilityMode(report.RecommendedCompatibilityMode)}");
            ImGui.TextUnformatted($"Active controlled: {report.ActiveSupportedEffects.Count}");
            ImGui.TextUnformatted($"Active partial: {report.ActivePartiallySupportedEffects.Count}");
            ImGui.TextUnformatted($"Active detected-only: {report.ActiveDetectedOnlyEffects.Count}");
            ImGui.TextUnformatted($"Active unsupported: {report.ActiveUnsupportedEffects.Count}");
            ImGui.TextUnformatted($"High-risk active: {report.HighRiskActiveEffects.Count}");

            if (ImGui.TreeNode("Active controlled effects"))
            {
                foreach (var technique in report.ActiveSupportedEffects)
                {
                    ImGui.BulletText($"{PresetAnalyzer.FormatTechnique(technique)} ({PresetAnalyzer.FormatRole(technique.Role)}, fully controlled)");
                }

                foreach (var technique in report.ActivePartiallySupportedEffects)
                {
                    ImGui.BulletText($"{PresetAnalyzer.FormatTechnique(technique)} ({PresetAnalyzer.FormatRole(technique.Role)}, partially controlled)");
                }

                if (report.ActiveSupportedEffects.Count == 0 && report.ActivePartiallySupportedEffects.Count == 0)
                {
                    ImGui.TextUnformatted("No active controlled effects detected yet.");
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Active detected-only effects"))
            {
                foreach (var technique in report.ActiveDetectedOnlyEffects)
                {
                    ImGui.BulletText($"{PresetAnalyzer.FormatTechnique(technique)} ({PresetAnalyzer.FormatRole(technique.Role)}, {PresetAnalyzer.FormatRisk(technique.Risk)})");
                }

                if (report.ActiveDetectedOnlyEffects.Count == 0)
                {
                    ImGui.TextUnformatted("No active detected-only effects.");
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Effect authorities"))
            {
                foreach (var authority in report.Authorities)
                {
                    ImGui.BulletText($"{PresetAnalyzer.FormatRole(authority.Role)}: {authority.PrimaryShader}");
                    foreach (var secondary in authority.SecondaryShaders)
                    {
                        ImGui.TextWrapped($"  secondary: {secondary}");
                    }
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Active unknown/unsupported effects"))
            {
                foreach (var technique in report.ActiveUnsupportedEffects)
                {
                    ImGui.BulletText($"{PresetAnalyzer.FormatTechnique(technique)} ({PresetAnalyzer.FormatRole(technique.Role)}, {PresetAnalyzer.FormatRisk(technique.Risk)})");
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("High-risk effects"))
            {
                foreach (var technique in report.HighRiskActiveEffects)
                {
                    ImGui.BulletText($"{PresetAnalyzer.FormatTechnique(technique)} ({PresetAnalyzer.FormatRole(technique.Role)}, {PresetAnalyzer.FormatRisk(technique.Risk)})");
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Warnings"))
            {
                if (report.Warnings.Count == 0)
                {
                    ImGui.TextUnformatted("No preset compatibility warnings yet.");
                }

                foreach (var warning in report.Warnings)
                {
                    ImGui.BulletText(warning);
                }

                ImGui.TreePop();
            }
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Detected shader support"))
        {
            ImGui.TextWrapped(plugin.LastShaderSupportScan.Message);
            foreach (var item in plugin.LastShaderSupportScan.Items)
            {
                var active = item.TechniqueActive ? "active" : "inactive";
                ImGui.BulletText($"{item.Section} / {item.Key} ({item.ReasonCategory}, {active})");
            }
        }

        if (ImGui.CollapsingHeader("Changed variables"))
        {
            if (plugin.LastWriteResult.Changes.Count == 0)
            {
                ImGui.TextUnformatted("No changed variables recorded yet.");
            }

            foreach (var change in plugin.LastWriteResult.Changes)
            {
                var active = change.TechniqueActive ? "active" : "inactive";
                var clamp = change.HitMin ? ", min clamp" : change.HitMax ? ", max clamp" : string.Empty;
                ImGui.BulletText($"{change.Section} / {change.Key}: {change.OldValue} -> {change.NewValue} ({active}{clamp})");
                ImGui.SameLine();
                ImGui.TextDisabled(change.ReasonCategory);
                if (!string.IsNullOrWhiteSpace(change.Warning))
                {
                    ImGui.TextWrapped(change.Warning);
                }
            }
        }

        if (ImGui.CollapsingHeader("Sanitize actions"))
        {
            if (plugin.LastWriteResult.SanitizeActions.Count == 0)
            {
                ImGui.TextUnformatted("No sanitize actions recorded yet.");
            }

            foreach (var action in plugin.LastWriteResult.SanitizeActions)
            {
                var active = action.TechniqueActive ? "active" : "inactive";
                ImGui.BulletText($"{action.Section} / {action.Key}: {action.OldValue} -> {action.NewValue} ({action.ActionType}, {PresetAnalyzer.FormatRole(action.Role)}, {active})");
                ImGui.TextDisabled(action.Reason);
            }
        }

        ImGui.Separator();

        ImGui.TextWrapped(plugin.LastWriteResult.Message);
        ImGui.TextWrapped(plugin.LastReloadResult.Message);
        ImGui.TextWrapped(plugin.LastCompatibilityReportExport.Message);
        ImGui.TextWrapped("Dalashade only edits variables that already exist in the preset. Keep iMMERSE and any Pro/Ultimate shaders installed through ReShade; this plugin does not ship those files.");
    }

    private static void DrawSetupItem(string label, bool complete)
    {
        ImGui.BulletText($"{(complete ? "OK" : "Missing")} - {label}");
    }
}
