using System;
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

        ImGui.TextUnformatted($"Territory: {context.TerritoryName} ({context.TerritoryId})");
        ImGui.TextUnformatted($"World: {context.WorldCategory}");
        ImGui.TextUnformatted($"Weather: {context.WeatherName}");
        ImGui.TextUnformatted($"Night: {context.IsNight}");
        ImGui.TextUnformatted($"Combat: {context.InCombat}");
        ImGui.TextUnformatted($"Cutscene: {context.InCutscene}");

        ImGui.Separator();

        ImGui.TextUnformatted($"Exposure: {profile.Exposure:0.###}");
        ImGui.TextUnformatted($"Contrast: {profile.Contrast:0.###}");
        ImGui.TextUnformatted($"Saturation: {profile.Saturation:0.###}");
        ImGui.TextUnformatted($"Bloom: {profile.Bloom:0.###}");
        ImGui.TextUnformatted($"Ambient Occlusion: {profile.AmbientOcclusion:0.###}");
        ImGui.TextUnformatted($"Sharpness: {profile.Sharpness:0.###}");
        ImGui.TextUnformatted($"Clarity: {profile.Clarity:0.###}");
        ImGui.TextUnformatted($"Shadow Lift: {profile.ShadowLift:0.###}");

        ImGui.Separator();

        var image = plugin.CurrentImageAnalysis;
        ImGui.TextWrapped(plugin.ImageAnalysisMessage);
        if (image.Available)
        {
            ImGui.TextUnformatted($"Image Luma: {image.AverageLuminance:0.###}");
            ImGui.TextUnformatted($"Image Contrast: {image.Contrast:0.###}");
            ImGui.TextUnformatted($"Image Saturation: {image.AverageSaturation:0.###}");
            ImGui.TextUnformatted($"Shadow Clip: {image.ShadowClipping:P1}");
            ImGui.TextUnformatted($"Highlight Clip: {image.HighlightClipping:P1}");
        }

        ImGui.Separator();

        ImGui.TextWrapped(plugin.LastWriteResult.Message);
        ImGui.TextWrapped("Dalashade only edits variables that already exist in the preset. Keep iMMERSE and any Pro/Ultimate shaders installed through ReShade; this plugin does not ship those files.");
    }
}
