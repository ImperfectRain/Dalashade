using System;
using System.Collections.Generic;
using System.Linq;
using Dalashade.SceneAuthoring;

namespace Dalashade;

public sealed record MaterialTagRegistryTuningDiagnostic(
    string Status,
    string Category,
    string Tag,
    string Target,
    string Channel,
    float RequestedAmount,
    float AppliedAmount,
    string Reason,
    string Message);

public sealed record MaterialTagRegistryChannelDiagnostic(
    string Channel,
    float FinalContribution,
    bool Capped);

public sealed record MaterialTagRegistryDiagnostics(
    IReadOnlyList<MaterialTagRegistryTuningDiagnostic> ActiveTunings,
    IReadOnlyList<MaterialTagRegistryTuningDiagnostic> InactiveTunings,
    IReadOnlyList<MaterialTagRegistryTuningDiagnostic> InvalidTunings,
    IReadOnlyList<MaterialTagRegistryTuningDiagnostic> CappedTunings,
    IReadOnlyList<MaterialTagRegistryChannelDiagnostic> Channels)
{
    public static MaterialTagRegistryDiagnostics Empty { get; } = new(
        Array.Empty<MaterialTagRegistryTuningDiagnostic>(),
        Array.Empty<MaterialTagRegistryTuningDiagnostic>(),
        Array.Empty<MaterialTagRegistryTuningDiagnostic>(),
        Array.Empty<MaterialTagRegistryTuningDiagnostic>(),
        MaterialIntent.ChannelNames.Select(channel => new MaterialTagRegistryChannelDiagnostic(channel, 0f, false)).ToArray());
}

public sealed record MaterialTagRegistryContributionResult(
    IReadOnlyList<MaterialIntentContribution> Contributions,
    MaterialTagRegistryDiagnostics Diagnostics)
{
    public static MaterialTagRegistryContributionResult Empty { get; } = new(Array.Empty<MaterialIntentContribution>(), MaterialTagRegistryDiagnostics.Empty);
}

public static class MaterialTagRegistryTuningAnalyzer
{
    public const float PerTagContributionCap = 0.20f;
    public const float PerChannelContributionCap = 0.35f;

    private const float Epsilon = 0.0001f;

    public static MaterialTagRegistryContributionResult Build(TagStackDiagnostics diagnostics, IReadOnlyList<SceneTagPreset>? tagRegistry)
    {
        if (tagRegistry is null || tagRegistry.Count == 0)
        {
            return MaterialTagRegistryContributionResult.Empty;
        }

        var activeTags = BuildActiveRegistryTags(diagnostics);
        var active = new List<MaterialTagRegistryTuningDiagnostic>();
        var inactive = new List<MaterialTagRegistryTuningDiagnostic>();
        var invalid = new List<MaterialTagRegistryTuningDiagnostic>();
        var capped = new List<MaterialTagRegistryTuningDiagnostic>();
        var contributions = new List<MaterialIntentContribution>();
        var channelTotals = MaterialIntent.ChannelNames.ToDictionary(channel => channel, _ => 0f, StringComparer.Ordinal);
        var channelCapped = MaterialIntent.ChannelNames.ToDictionary(channel => channel, _ => false, StringComparer.Ordinal);

        foreach (var preset in tagRegistry)
        {
            var categories = preset.Categories.Count == 0
                ? new[] { string.Empty }
                : preset.Categories.Where(category => !string.IsNullOrWhiteSpace(category)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (categories.Length == 0)
            {
                categories = [string.Empty];
            }

            var presetActive = categories.Any(category =>
                activeTags.TryGetValue(category, out var tags)
                && tags.Contains(preset.Tag));

            foreach (var tuning in preset.Tunings)
            {
                var target = tuning.Target?.Trim() ?? string.Empty;
                var channel = tuning.Channel?.Trim() ?? string.Empty;
                var reason = string.IsNullOrWhiteSpace(tuning.Reason)
                    ? "Reason not provided; add one to make registry material tuning easier to audit."
                    : tuning.Reason.Trim();
                var categoryLabel = string.Join(",", categories.Where(category => !string.IsNullOrWhiteSpace(category)));
                if (string.IsNullOrWhiteSpace(categoryLabel))
                {
                    categoryLabel = "unknown";
                }

                if (!SceneAuthoringService.TuningTargets.Contains(target, StringComparer.OrdinalIgnoreCase))
                {
                    invalid.Add(Row("Invalid", categoryLabel, preset.Tag, target, channel, tuning.Amount, 0f, reason, "Invalid tuning target; only SceneIntent and MaterialIntent are accepted."));
                    continue;
                }

                if (!string.Equals(target, SceneTagTuningTargets.MaterialIntent, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var canonicalChannel = MaterialIntent.ChannelNames.FirstOrDefault(candidate => string.Equals(candidate, channel, StringComparison.OrdinalIgnoreCase));
                if (canonicalChannel is null)
                {
                    invalid.Add(Row("Invalid", categoryLabel, preset.Tag, target, channel, tuning.Amount, 0f, reason, "Invalid MaterialIntent channel; tuning was ignored."));
                    continue;
                }

                if (!float.IsFinite(tuning.Amount) || MathF.Abs(tuning.Amount) > 1f)
                {
                    invalid.Add(Row("Invalid", categoryLabel, preset.Tag, target, canonicalChannel, tuning.Amount, 0f, reason, "Amount is outside the accepted authoring range [-1.0, +1.0]; tuning was ignored."));
                    continue;
                }

                if (!tuning.Enabled)
                {
                    inactive.Add(Row("Inactive", categoryLabel, preset.Tag, target, canonicalChannel, tuning.Amount, 0f, reason, "Tuning is disabled."));
                    continue;
                }

                if (!presetActive)
                {
                    inactive.Add(Row("Inactive", categoryLabel, preset.Tag, target, canonicalChannel, tuning.Amount, 0f, reason, "Tag is not active in the current scene."));
                    continue;
                }

                var perTagAmount = Math.Clamp(tuning.Amount, -PerTagContributionCap, PerTagContributionCap);
                var current = channelTotals[canonicalChannel];
                var desired = Math.Clamp(current + perTagAmount, -PerChannelContributionCap, PerChannelContributionCap);
                var applied = desired - current;
                channelTotals[canonicalChannel] = desired;

                var wasCapped = MathF.Abs(perTagAmount - tuning.Amount) > Epsilon
                                || MathF.Abs(applied - perTagAmount) > Epsilon;
                var message = wasCapped
                    ? $"Applied with safety cap. Per-tag cap is +/-{PerTagContributionCap:0.##}; per-channel registry cap is +/-{PerChannelContributionCap:0.##}."
                    : "Applied to current scene.";
                var row = Row(wasCapped ? "Capped" : "Active", categoryLabel, preset.Tag, target, canonicalChannel, tuning.Amount, applied, reason, message);
                if (wasCapped)
                {
                    capped.Add(row);
                    channelCapped[canonicalChannel] = true;
                }
                else
                {
                    active.Add(row);
                }

                if (MathF.Abs(applied) > Epsilon)
                {
                    contributions.Add(new MaterialIntentContribution(
                        canonicalChannel,
                        $"Tag registry: {preset.Tag}",
                        applied,
                        wasCapped ? $"{reason} {message}" : reason));
                }
            }
        }

        var channels = MaterialIntent.ChannelNames
            .Select(channel => new MaterialTagRegistryChannelDiagnostic(channel, channelTotals[channel], channelCapped[channel]))
            .ToArray();

        return new MaterialTagRegistryContributionResult(
            contributions,
            new MaterialTagRegistryDiagnostics(active, inactive, invalid, capped, channels));
    }

    private static MaterialTagRegistryTuningDiagnostic Row(
        string status,
        string category,
        string tag,
        string target,
        string channel,
        float requestedAmount,
        float appliedAmount,
        string reason,
        string message)
    {
        return new MaterialTagRegistryTuningDiagnostic(
            status,
            category,
            tag,
            string.IsNullOrWhiteSpace(target) ? "missing" : target,
            string.IsNullOrWhiteSpace(channel) ? "missing" : channel,
            requestedAmount,
            appliedAmount,
            reason,
            message);
    }

    private static IReadOnlyDictionary<string, HashSet<string>> BuildActiveRegistryTags(TagStackDiagnostics diagnostics)
    {
        var active = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        void Add(string category, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "unknown")
            {
                return;
            }

            if (!active.TryGetValue(category, out var values))
            {
                values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                active[category] = values;
            }

            values.Add(value);
        }

        Add(SceneAuthoringService.BiomeCategory, diagnostics.BiomeKey);
        Add(SceneAuthoringService.WeatherCategory, diagnostics.WeatherKey);
        Add(SceneAuthoringService.AreaCategory, diagnostics.AreaKey);
        foreach (var tag in diagnostics.ActiveWeatherTags)
        {
            Add(SceneAuthoringService.WeatherCategory, tag);
        }

        foreach (var tag in diagnostics.AreaContextTags)
        {
            Add(SceneAuthoringService.AreaCategory, tag);
        }

        foreach (var tag in diagnostics.SecondaryTags)
        {
            Add(SceneAuthoringService.SecondaryCategory, tag);
        }

        foreach (var tag in diagnostics.MoodTags)
        {
            Add(SceneAuthoringService.MoodCategory, tag);
        }

        foreach (var tag in diagnostics.MaterialTags)
        {
            Add(SceneAuthoringService.MaterialCategory, tag);
        }

        foreach (var tag in diagnostics.ArtDirectionTags)
        {
            Add(SceneAuthoringService.ArtDirectionCategory, tag);
            if (tag is "day" or "night")
            {
                Add(SceneAuthoringService.TimeCategory, tag);
            }
        }

        foreach (var tag in diagnostics.ActiveTags)
        {
            if (string.Equals(tag, "Day", StringComparison.OrdinalIgnoreCase))
            {
                Add(SceneAuthoringService.TimeCategory, "day");
            }
            else if (string.Equals(tag, "Night", StringComparison.OrdinalIgnoreCase))
            {
                Add(SceneAuthoringService.TimeCategory, "night");
            }
            else if (string.Equals(tag, "DawnDusk", StringComparison.OrdinalIgnoreCase))
            {
                Add(SceneAuthoringService.TimeCategory, "dawnDusk");
            }
        }

        return active;
    }
}
