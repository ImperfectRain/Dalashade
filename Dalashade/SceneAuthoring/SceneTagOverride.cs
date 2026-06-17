using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade.SceneAuthoring;

public sealed class SceneTagOverrideSet
{
    public int Version { get; set; } = 1;

    public List<SceneTagOverride> Entries { get; set; } = [];
}

public sealed class SceneTagPresetSet
{
    public int Version { get; set; } = 1;

    public List<SceneTagPreset> Presets { get; set; } = [];
}

public sealed class SceneTagPreset
{
    public string Tag { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> Categories { get; set; } = [];

    public List<string> Effects { get; set; } = [];

    public bool IsBuiltIn { get; set; }
}

public sealed class SceneTagOverride
{
    public bool Enabled { get; set; } = true;

    public string Scope { get; set; } = "territory";

    public uint TerritoryId { get; set; }

    public string TerritoryName { get; set; } = string.Empty;

    public string Mode { get; set; } = "merge";

    public string PrimaryBiomeOverride { get; set; } = string.Empty;

    public Dictionary<string, List<string>> AddedTags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, List<string>> RemovedTags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Added(string category) => TagsFor(AddedTags, category);

    public IReadOnlyList<string> Removed(string category) => TagsFor(RemovedTags, category);

    public bool HasEdits =>
        !string.IsNullOrWhiteSpace(PrimaryBiomeOverride)
        || AddedTags.Values.Any(tags => tags.Any(tag => !string.IsNullOrWhiteSpace(tag)))
        || RemovedTags.Values.Any(tags => tags.Any(tag => !string.IsNullOrWhiteSpace(tag)));

    private static IReadOnlyList<string> TagsFor(Dictionary<string, List<string>> source, string category)
    {
        return source.TryGetValue(category, out var tags)
            ? tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray()
            : Array.Empty<string>();
    }
}

public sealed record SceneAuthoringState(
    bool Enabled,
    string StoragePath,
    SceneTagOverride? ActiveOverride,
    SceneTags DetectedTags,
    SceneTags EffectiveTags,
    IReadOnlyDictionary<string, IReadOnlyList<string>> AddedTags,
    IReadOnlyDictionary<string, IReadOnlyList<string>> RemovedTags,
    IReadOnlyList<string> Warnings,
    string Fingerprint,
    string Message)
{
    public static SceneAuthoringState Disabled(SceneTags tags, string storagePath) => new(
        false,
        storagePath,
        null,
        tags,
        tags,
        EmptyMap,
        EmptyMap,
        Array.Empty<string>(),
        "disabled",
        "Scene authoring overrides are disabled.");

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
}
