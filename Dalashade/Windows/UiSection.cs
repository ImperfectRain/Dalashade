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
            var inline = summary.Length <= 56 && ImGui.GetContentRegionAvail().X > 260f;
            if (inline)
            {
                ImGui.SameLine();
            }
            else
            {
                ImGui.Indent();
            }

            if (summaryColor.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, summaryColor.Value);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
            }

            ImGui.TextWrapped(summary);

            ImGui.PopStyleColor();

            if (!inline)
            {
                ImGui.Unindent();
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
