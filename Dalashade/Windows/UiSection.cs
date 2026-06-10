using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Dalashade.Windows;

internal static class UiSection
{
    public static bool Draw(
        string id,
        string title,
        bool defaultOpen,
        string? summary,
        Action content,
        Vector4? summaryColor = null)
    {
        var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
        var open = ImGui.CollapsingHeader($"{title}###{id}", flags);

        if (!string.IsNullOrWhiteSpace(summary))
        {
            ImGui.SameLine();
            if (summaryColor.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, summaryColor.Value);
            }

            ImGui.TextWrapped(summary);

            if (summaryColor.HasValue)
            {
                ImGui.PopStyleColor();
            }
        }

        if (open)
        {
            ImGui.Indent();
            content();
            ImGui.Unindent();
            ImGui.Spacing();
        }

        return open;
    }
}
