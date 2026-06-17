using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalashade.SceneAuthoring;

namespace Dalashade.Windows;

public sealed class SceneAuthoringWindow : Window, IDisposable
{
    private static readonly Vector4 AddedColor = new(0.50f, 0.90f, 0.62f, 1.0f);
    private static readonly Vector4 RemovedColor = new(1.0f, 0.58f, 0.42f, 1.0f);
    private static readonly Vector4 DetectedColor = new(0.78f, 0.78f, 0.78f, 1.0f);
    private static readonly Vector4 MutedColor = new(0.55f, 0.55f, 0.55f, 1.0f);
    private static readonly Vector4 HeaderColor = new(0.78f, 0.86f, 1.0f, 1.0f);

    private readonly Plugin plugin;
    private readonly Dictionary<string, int> selectedTagIndices = new(StringComparer.OrdinalIgnoreCase);
    private int selectedPresetCategoryIndex;
    private int selectedPresetTagIndex;
    private string newPresetTag = string.Empty;

    public SceneAuthoringWindow(Plugin plugin)
        : base("Dalashade Scene Authoring###DalashadeSceneAuthoring")
    {
        Size = new Vector2(780, 560);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 360),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        DrawEnableRow();

        if (!ImGui.BeginTabBar("SceneAuthoringTabs"))
        {
            return;
        }

        if (ImGui.BeginTabItem("Current Scene"))
        {
            DrawCurrentScene();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Tag Registry"))
        {
            DrawTagPresetsEditor();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawEnableRow()
    {
        var configuration = plugin.Configuration;
        var enabled = configuration.EnableSceneAuthoringOverrides;
        if (ImGui.Checkbox("Enable scene authoring overrides", ref enabled))
        {
            configuration.EnableSceneAuthoringOverrides = enabled;
            configuration.Save();
            plugin.RefreshSceneAuthoringState();
        }

        ImGui.TextWrapped(enabled
            ? "Overrides and tag registry tunings are applied between automatic scene detection and visual profile generation."
            : "Overrides and tag registry tunings are inactive. Dalashade is using the automatic scene/tag classifier.");
    }

    private void DrawCurrentScene()
    {
        plugin.RefreshSceneAuthoringState();
        var context = plugin.CurrentContext;
        var state = plugin.CurrentSceneAuthoringState;
        var detected = TagStackDiagnostics.Create(context, state.DetectedTags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());
        var effective = TagStackDiagnostics.Create(context, state.EffectiveTags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());

        DrawSceneSummary(context, state, detected, effective);
        ImGui.Separator();
        DrawPrimaryBiomeEditor(detected.BiomeKey, effective.BiomeKey, state);
        DrawLegend();
        ImGui.Separator();

        DrawTagGroup("Area Tags", SceneAuthoringService.AreaCategory, detected.AreaContextTags, effective.AreaContextTags, state);
        DrawTagGroup("Biome Tags", SceneAuthoringService.BiomeCategory, BuildBiomeTags(detected), BuildBiomeTags(effective), state);
        DrawTagGroup("Weather Tags", SceneAuthoringService.WeatherCategory, detected.ActiveWeatherTags, effective.ActiveWeatherTags, state);
        DrawTagGroup("Time Tags", SceneAuthoringService.TimeCategory, BuildTimeTags(detected), BuildTimeTags(effective), state);
        DrawTagGroup("Secondary Tags", SceneAuthoringService.SecondaryCategory, detected.SecondaryTags, effective.SecondaryTags, state);
        DrawTagGroup("Material Tags", SceneAuthoringService.MaterialCategory, detected.MaterialTags, effective.MaterialTags, state);
        DrawTagGroup("Art-Direction Tags", SceneAuthoringService.ArtDirectionCategory, detected.ArtDirectionTags, effective.ArtDirectionTags, state);
        DrawTagGroup("Mood Tags", SceneAuthoringService.MoodCategory, detected.MoodTags, effective.MoodTags, state);
    }

    private void DrawSceneSummary(GameContext context, SceneAuthoringState state, TagStackDiagnostics detected, TagStackDiagnostics effective)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, HeaderColor);
        ImGui.TextUnformatted($"{context.TerritoryName} ({context.TerritoryId})");
        ImGui.PopStyleColor();

        ImGui.TextUnformatted($"Weather: {context.WeatherName}  |  Time: {context.TimeBucket}");
        ImGui.TextUnformatted($"Detected: {detected.AreaKey} / {detected.BiomeKey}");
        ImGui.TextUnformatted($"Effective: {effective.AreaKey} / {effective.BiomeKey}");
        ImGui.TextWrapped(state.Message);

        var addedCount = state.AddedTags.Values.Sum(tags => tags.Count);
        var removedCount = state.RemovedTags.Values.Sum(tags => tags.Count);
        ImGui.TextUnformatted($"Overrides: +{addedCount} added, -{removedCount} removed");
        foreach (var warning in state.Warnings)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, RemovedColor);
            ImGui.TextWrapped($"Warning: {warning}");
            ImGui.PopStyleColor();
        }

        if (ImGui.Button("Reset Current Scene###SceneAuthoringResetCurrent"))
        {
            plugin.ResetCurrentSceneAuthoringOverride();
        }

        ImGui.SameLine();
        if (ImGui.Button("Use Detected Tags###SceneAuthoringUseDetected"))
        {
            plugin.ResetCurrentSceneAuthoringOverride();
        }

        ImGui.SameLine();
        ImGui.TextDisabled("Storage");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(state.StoragePath);
        }
    }

    private void DrawPrimaryBiomeEditor(string detectedBiome, string effectiveBiome, SceneAuthoringState state)
    {
        var biomeTags = plugin.SceneAuthoringKnownTags(SceneAuthoringService.BiomeCategory);
        if (biomeTags.Count == 0)
        {
            return;
        }

        var overrideBiome = state.ActiveOverride?.PrimaryBiomeOverride ?? string.Empty;
        var options = new[] { "Use detected biome" }.Concat(biomeTags).ToArray();
        var selected = string.IsNullOrWhiteSpace(overrideBiome)
            ? 0
            : Math.Max(0, Array.FindIndex(options, option => string.Equals(option, overrideBiome, StringComparison.OrdinalIgnoreCase)));
        if (ImGui.Combo("Primary biome", ref selected, string.Join('\0', options) + '\0'))
        {
            if (selected == 0)
            {
                plugin.ClearSceneAuthoringPrimaryBiomeOverride();
            }
            else
            {
                plugin.SetSceneAuthoringPrimaryBiome(options[selected]);
            }
        }

        ImGui.SameLine();
        ImGui.TextDisabled(string.IsNullOrWhiteSpace(overrideBiome)
            ? $"automatic: {detectedBiome}"
            : $"override: {overrideBiome} -> effective: {effectiveBiome}");
    }

    private void DrawTagGroup(string title, string category, IReadOnlyList<string> detectedTags, IReadOnlyList<string> effectiveTags, SceneAuthoringState state)
    {
        var added = TagsFromMap(state.AddedTags, category);
        var removed = TagsFromMap(state.RemovedTags, category);
        var summary = added.Count == 0 && removed.Count == 0
            ? $"{effectiveTags.Count} automatic"
            : $"{effectiveTags.Count} active, +{added.Count}/-{removed.Count}";

        if (!UiSection.Draw($"SceneAuthoring{category}", title, category is SceneAuthoringService.AreaCategory or SceneAuthoringService.BiomeCategory, summary, () =>
            {
                DrawTagStateTable(category, detectedTags, effectiveTags, added, removed);
                DrawAddTagControl(category);
            }))
        {
            return;
        }
    }

    private void DrawLegend()
    {
        DrawInlineStatus("automatic", DetectedColor);
        ImGui.SameLine();
        DrawInlineStatus("added override", AddedColor);
        ImGui.SameLine();
        DrawInlineStatus("removed override", RemovedColor);
    }

    private static void DrawInlineStatus(string label, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(label);
        ImGui.PopStyleColor();
    }

    private void DrawTagStateTable(string category, IReadOnlyList<string> detectedTags, IReadOnlyList<string> effectiveTags, IReadOnlyList<string> addedTags, IReadOnlyList<string> removedTags)
    {
        var rows = BuildTagRows(detectedTags, effectiveTags, addedTags, removedTags);
        if (rows.Count == 0)
        {
            ImGui.TextDisabled("No tags in this category.");
            return;
        }

        if (!ImGui.BeginTable($"SceneAuthoringTagTable{category}", 4))
        {
            return;
        }

        ImGui.TableSetupColumn("Tag");
        ImGui.TableSetupColumn("Source");
        ImGui.TableSetupColumn("Applies");
        ImGui.TableSetupColumn("Action");
        ImGui.TableHeadersRow();

        foreach (var row in rows)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.PushStyleColor(ImGuiCol.Text, row.Color);
            ImGui.TextUnformatted(row.Tag);
            ImGui.PopStyleColor();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(row.SourceLabel);

            ImGui.TableNextColumn();
            if (row.Applies)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, AddedColor);
                ImGui.TextUnformatted("yes");
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, RemovedColor);
                ImGui.TextUnformatted("no");
                ImGui.PopStyleColor();
            }

            ImGui.TableNextColumn();
            if (row.OverrideState is TagOverrideState.None)
            {
                if (ImGui.SmallButton($"Remove###{category}{row.Tag}RemoveDetected"))
                {
                    plugin.RemoveSceneAuthoringTag(category, row.Tag);
                }
            }
            else
            {
                if (ImGui.SmallButton($"Clear override###{category}{row.Tag}ClearOverride"))
                {
                    plugin.ClearSceneAuthoringTagOverride(category, row.Tag);
                }
            }
        }

        ImGui.EndTable();
    }

    private void DrawAddTagControl(string category)
    {
        var knownTags = plugin.SceneAuthoringKnownTags(category);
        if (knownTags.Count == 0)
        {
            return;
        }

        selectedTagIndices.TryGetValue(category, out var selected);
        selected = Math.Clamp(selected, 0, knownTags.Count - 1);
        if (ImGui.Combo($"Add tag###{category}AddTagCombo", ref selected, string.Join('\0', knownTags) + '\0'))
        {
            selectedTagIndices[category] = selected;
        }

        ImGui.SameLine();
        if (ImGui.Button($"Add###{category}AddTagButton"))
        {
            plugin.AddSceneAuthoringTag(category, knownTags[selected]);
        }
    }

    private static IReadOnlyList<TagStateRow> BuildTagRows(IReadOnlyList<string> detectedTags, IReadOnlyList<string> effectiveTags, IReadOnlyList<string> addedTags, IReadOnlyList<string> removedTags)
    {
        var detected = new HashSet<string>(detectedTags, StringComparer.OrdinalIgnoreCase);
        var effective = new HashSet<string>(effectiveTags, StringComparer.OrdinalIgnoreCase);
        var added = new HashSet<string>(addedTags, StringComparer.OrdinalIgnoreCase);
        var removed = new HashSet<string>(removedTags, StringComparer.OrdinalIgnoreCase);
        var all = detected
            .Concat(effective)
            .Concat(added)
            .Concat(removed)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase);

        return all.Select(tag =>
        {
            var state = added.Contains(tag)
                ? TagOverrideState.Added
                : removed.Contains(tag)
                    ? TagOverrideState.Removed
                    : TagOverrideState.None;
            var color = state switch
            {
                TagOverrideState.Added => AddedColor,
                TagOverrideState.Removed => RemovedColor,
                _ => DetectedColor
            };
            var source = state switch
            {
                TagOverrideState.Added => "override added",
                TagOverrideState.Removed => "override removed",
                _ => detected.Contains(tag) ? "automatic" : "effective"
            };

            return new TagStateRow(tag, source, state is TagOverrideState.Removed ? false : effective.Contains(tag), state, color);
        }).ToArray();
    }

    private static IReadOnlyList<string> TagsFromMap(IReadOnlyDictionary<string, IReadOnlyList<string>> source, string category)
    {
        return source.TryGetValue(category, out var tags) ? tags : Array.Empty<string>();
    }

    private static IReadOnlyList<string> BuildBiomeTags(TagStackDiagnostics diagnostics)
    {
        return string.IsNullOrWhiteSpace(diagnostics.BiomeKey) || diagnostics.BiomeKey is "unknown"
            ? Array.Empty<string>()
            : new[] { diagnostics.BiomeKey };
    }

    private static IReadOnlyList<string> BuildTimeTags(TagStackDiagnostics diagnostics)
    {
        return diagnostics.ActiveTags
            .Where(tag => tag is "Night" or "Day" or "DawnDusk")
            .Select(tag => tag is "DawnDusk" ? "dawnDusk" : tag.ToLowerInvariant())
            .ToArray();
    }

    private void DrawTagPresetsEditor()
    {
        ImGui.TextWrapped("Tag registry presets define what active tags contribute to SceneIntent and MaterialIntent. They apply only while scene authoring is enabled, and edits stay in the plugin config folder.");
        DrawImportExportControls();
        ImGui.Separator();

        var categories = SceneAuthoringService.EditableCategories.ToArray();
        selectedPresetCategoryIndex = Math.Clamp(selectedPresetCategoryIndex, 0, categories.Length - 1);
        ImGui.SetNextItemWidth(MathF.Min(520f, ImGui.GetContentRegionAvail().X * 0.70f));
        if (ImGui.Combo("Category###SceneAuthoringPresetCategory", ref selectedPresetCategoryIndex, string.Join('\0', categories) + '\0'))
        {
            selectedPresetTagIndex = 0;
        }

        var category = categories[selectedPresetCategoryIndex];
        var presets = plugin.SceneAuthoringTagPresets(category);
        if (presets.Count == 0)
        {
            ImGui.TextDisabled("No tag presets in this category.");
        }
        else
        {
            selectedPresetTagIndex = Math.Clamp(selectedPresetTagIndex, 0, presets.Count - 1);
            var presetLabels = presets.Select(preset => string.IsNullOrWhiteSpace(preset.DisplayName) ? preset.Tag : $"{preset.DisplayName} ({preset.Tag})").ToArray();
            ImGui.SetNextItemWidth(MathF.Min(520f, ImGui.GetContentRegionAvail().X * 0.70f));
            ImGui.Combo("Tag preset###SceneAuthoringPresetTag", ref selectedPresetTagIndex, string.Join('\0', presetLabels) + '\0');
            DrawPresetEditor(category, presets[selectedPresetTagIndex]);
        }

        ImGui.Separator();
        DrawAddCustomPreset(category);
    }

    private void DrawImportExportControls()
    {
        if (ImGui.Button("Export Overrides###SceneAuthoringExportOverrides"))
        {
            plugin.ExportSceneAuthoringOverrides();
        }

        ImGui.SameLine();
        if (ImGui.Button("Import Overrides###SceneAuthoringImportOverrides"))
        {
            plugin.ImportSceneAuthoringOverrides();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Overrides###SceneAuthoringResetOverrides"))
        {
            plugin.ResetAllSceneAuthoringOverrides();
        }

        if (ImGui.Button("Export Registry###SceneAuthoringExportTagPresets"))
        {
            plugin.ExportSceneAuthoringTagPresets();
        }

        ImGui.SameLine();
        if (ImGui.Button("Import Registry###SceneAuthoringImportTagPresets"))
        {
            plugin.ImportSceneAuthoringTagPresets();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Registry###SceneAuthoringResetTagPresets"))
        {
            plugin.ResetSceneAuthoringTagPresets();
        }

        ImGui.TextWrapped(plugin.SceneAuthoringImportExportMessage);
    }

    private void DrawPresetEditor(string category, SceneTagPreset preset)
    {
        ImGui.TextUnformatted($"Tag key: {preset.Tag}");
        ImGui.TextUnformatted(preset.IsBuiltIn ? "Source: built-in registry default" : "Source: user-created registry preset");

        var displayName = preset.DisplayName;
        ImGui.SetNextItemWidth(MathF.Min(620f, ImGui.GetContentRegionAvail().X * 0.72f));
        if (ImGui.InputText("Display name###SceneAuthoringPresetDisplayName", ref displayName, 128))
        {
            plugin.UpdateSceneAuthoringTagPreset(category, preset.Tag, displayName, preset.Description);
        }

        var description = preset.Description;
        ImGui.SetNextItemWidth(MathF.Min(620f, ImGui.GetContentRegionAvail().X * 0.72f));
        if (ImGui.InputText("Description###SceneAuthoringPresetDescription", ref description, 512))
        {
            plugin.UpdateSceneAuthoringTagPreset(category, preset.Tag, preset.DisplayName, description);
        }

        if (ImGui.Button("Reset Selected Preset###SceneAuthoringResetSelectedPreset"))
        {
            plugin.ResetSceneAuthoringTagPreset(category, preset.Tag);
            selectedPresetTagIndex = 0;
        }

        ImGui.Separator();
        DrawTuningEditor(category, preset);

        ImGui.TextUnformatted("Known influence");
        if (preset.Effects.Count == 0)
        {
            ImGui.TextDisabled("No known influence metadata.");
        }
        else
        {
            foreach (var effect in preset.Effects)
            {
                ImGui.BulletText(effect);
            }
        }
    }

    private void DrawTuningEditor(string category, SceneTagPreset preset)
    {
        ImGui.TextUnformatted("Applied tuning");
        if (preset.Tunings.Count == 0)
        {
            ImGui.TextDisabled("No tuning rows. This tag can still be used for scene overrides, but it has no registry-driven visual effect.");
        }
        else if (ImGui.BeginTable($"SceneAuthoringTunings{category}{preset.Tag}", 6, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("On", ImGuiTableColumnFlags.WidthFixed, 34f);
            ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 96f);
            ImGui.TableSetupColumn("Channel", ImGuiTableColumnFlags.WidthFixed, 170f);
            ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.WidthFixed, 92f);
            ImGui.TableSetupColumn("Reason");
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 64f);
            ImGui.TableHeadersRow();

            for (var i = 0; i < preset.Tunings.Count; i++)
            {
                var tuning = preset.Tunings[i];
                var changed = false;
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                var enabled = tuning.Enabled;
                if (ImGui.Checkbox($"###{category}{preset.Tag}{i}TuningEnabled", ref enabled))
                {
                    tuning.Enabled = enabled;
                    changed = true;
                }

                ImGui.TableNextColumn();
                var targets = SceneAuthoringService.TuningTargets.ToArray();
                var targetLabels = new[] { "Scene", "Material" };
                var targetIndex = IndexOf(targets, tuning.Target);
                ImGui.SetNextItemWidth(-1f);
                if (ImGui.Combo($"###{category}{preset.Tag}{i}TuningTarget", ref targetIndex, string.Join('\0', targetLabels) + '\0'))
                {
                    tuning.Target = targets[targetIndex];
                    var channelsForTarget = plugin.SceneAuthoringTuningChannels(tuning.Target);
                    tuning.Channel = channelsForTarget.Contains(tuning.Channel, StringComparer.OrdinalIgnoreCase)
                        ? tuning.Channel
                        : channelsForTarget.FirstOrDefault() ?? string.Empty;
                    changed = true;
                }

                ImGui.TableNextColumn();
                var channels = plugin.SceneAuthoringTuningChannels(tuning.Target).ToArray();
                if (channels.Length > 0)
                {
                    var channelIndex = IndexOf(channels, tuning.Channel);
                    ImGui.SetNextItemWidth(-1f);
                    if (ImGui.Combo($"###{category}{preset.Tag}{i}TuningChannel", ref channelIndex, string.Join('\0', channels) + '\0'))
                    {
                        tuning.Channel = channels[channelIndex];
                        changed = true;
                    }
                }
                else
                {
                    ImGui.TextDisabled("No channels");
                }

                ImGui.TableNextColumn();
                var amount = tuning.Amount;
                ImGui.SetNextItemWidth(-1f);
                if (ImGui.SliderFloat($"###{category}{preset.Tag}{i}TuningAmount", ref amount, -1.0f, 1.0f, "%.3f"))
                {
                    tuning.Amount = amount;
                    changed = true;
                }

                ImGui.TableNextColumn();
                var reason = tuning.Reason;
                ImGui.SetNextItemWidth(-1f);
                if (ImGui.InputText($"###{category}{preset.Tag}{i}TuningReason", ref reason, 256))
                {
                    tuning.Reason = reason;
                    changed = true;
                }

                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"Remove###{category}{preset.Tag}{i}RemoveTuning"))
                {
                    plugin.RemoveSceneAuthoringTagTuning(category, preset.Tag, i);
                    ImGui.EndTable();
                    return;
                }

                if (changed)
                {
                    plugin.UpdateSceneAuthoringTagTuning(category, preset.Tag, i, tuning);
                }
            }

            ImGui.EndTable();
        }

        if (ImGui.Button($"Add Tuning Row###SceneAuthoringAddTuning{category}{preset.Tag}"))
        {
            plugin.AddSceneAuthoringTagTuning(category, preset.Tag);
        }

        ImGui.TextDisabled("Scene = shared scene lanes. Material = first-party material uniforms when material mapping is enabled.");
    }

    private void DrawAddCustomPreset(string category)
    {
        ImGui.SetNextItemWidth(MathF.Min(520f, ImGui.GetContentRegionAvail().X * 0.66f));
        ImGui.InputText("New custom tag###SceneAuthoringNewPresetTag", ref newPresetTag, 96);
        ImGui.SameLine();
        if (ImGui.Button("Add Custom Tag###SceneAuthoringAddCustomPreset"))
        {
            plugin.AddSceneAuthoringTagPreset(category, newPresetTag);
            newPresetTag = string.Empty;
            selectedPresetTagIndex = 0;
        }
    }

    private enum TagOverrideState
    {
        None,
        Added,
        Removed
    }

    private sealed record TagStateRow(string Tag, string SourceLabel, bool Applies, TagOverrideState OverrideState, Vector4 Color);

    private static int IndexOf(IReadOnlyList<string> values, string value)
    {
        var index = values
            .Select((item, i) => (item, i))
            .FirstOrDefault(pair => string.Equals(pair.item, value, StringComparison.OrdinalIgnoreCase))
            .i;
        return Math.Clamp(index, 0, Math.Max(0, values.Count - 1));
    }
}
