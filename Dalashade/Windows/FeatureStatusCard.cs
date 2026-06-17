using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Dalashade.Windows;

internal sealed record FeatureStatusCard(
    string Title,
    string Description,
    string Status,
    bool ShaderFileFound,
    bool PresetEntryPresent,
    bool PresetSectionPresent,
    TechniqueActivationState TechniqueActivation,
    bool VariablesDetected,
    bool VariablesWritten,
    bool UsesFrameData,
    bool DepthAssistEnabled);

internal static class FeatureStatusCardRenderer
{
    private static readonly Vector4 GoodColor = new(0.44f, 0.86f, 0.52f, 1.0f);
    private static readonly Vector4 WarningColor = new(1.0f, 0.72f, 0.28f, 1.0f);
    private static readonly Vector4 MutedColor = new(0.72f, 0.72f, 0.72f, 1.0f);

    public static void Draw(FeatureStatusCard card)
    {
        ImGui.Separator();
        ImGui.TextUnformatted(card.Title);
        ImGui.SameLine();
        DrawStatusText(card.Status, card.PresetSectionPresent && card.TechniqueActivation == TechniqueActivationState.Active);
        ImGui.TextWrapped(card.Description);

        DrawPill(".fx file", card.ShaderFileFound ? "found" : "not found", card.ShaderFileFound);
        ImGui.SameLine();
        DrawPill("preset entry", card.PresetEntryPresent ? "yes" : "missing", card.PresetEntryPresent);
        ImGui.SameLine();
        DrawPill("section", card.PresetSectionPresent ? "yes" : "missing", card.PresetSectionPresent);
        ImGui.SameLine();
        DrawPill("technique", FormatActivation(card.TechniqueActivation), card.TechniqueActivation == TechniqueActivationState.Active);

        DrawPill("variables", card.VariablesDetected ? "detected" : "missing", card.VariablesDetected);
        ImGui.SameLine();
        DrawPill("written", card.VariablesWritten ? "yes" : "no", card.VariablesWritten);
        ImGui.SameLine();
        DrawPill("FrameData", card.UsesFrameData ? "yes" : "not yet", card.UsesFrameData);
        ImGui.SameLine();
        DrawPill("depth assist", card.DepthAssistEnabled ? "on" : "off", card.DepthAssistEnabled);
    }

    private static void DrawStatusText(string text, bool good)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, good ? GoodColor : WarningColor);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private static void DrawPill(string label, string value, bool good)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, good ? GoodColor : MutedColor);
        ImGui.TextUnformatted($"{label}: {value}");
        ImGui.PopStyleColor();
    }

    private static string FormatActivation(TechniqueActivationState activation)
    {
        return activation switch
        {
            TechniqueActivationState.Active => "active",
            TechniqueActivationState.Inactive => "inactive",
            _ => "unknown"
        };
    }
}
