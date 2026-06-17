using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Dalashade.SceneAuthoring;

public sealed class SceneAuthoringService
{
    public const string AreaCategory = "area";
    public const string BiomeCategory = "biome";
    public const string WeatherCategory = "weather";
    public const string TimeCategory = "time";
    public const string SecondaryCategory = "secondary";
    public const string MoodCategory = "mood";
    public const string MaterialCategory = "material";
    public const string ArtDirectionCategory = "artDirection";

    public static readonly IReadOnlyList<string> EditableCategories =
    [
        AreaCategory,
        BiomeCategory,
        WeatherCategory,
        TimeCategory,
        SecondaryCategory,
        MoodCategory,
        MaterialCategory,
        ArtDirectionCategory
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> KnownTags = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        [AreaCategory] = ["city", "field", "interior", "dungeon", "raid"],
        [BiomeCategory] = ["neutral", "coastal", "tropical", "forest", "jungle", "swamp", "desert", "wasteland", "snow", "alpine", "steppe", "cave", "void", "aetherial", "fae", "lightFlooded", "lunar", "cosmic", "ancient", "imperial", "highTech", "underwater", "volcanic", "fire", "overcast"],
        [WeatherCategory] = ["clear", "rain", "fog", "clouds", "overcast", "gloom", "snow", "storm", "dust", "heat"],
        [TimeCategory] = ["day", "night", "dawnDusk"],
        [SecondaryCategory] = ["seaside", "beach", "tropical", "rainforest", "lush", "verdant", "badlands", "dry", "alpine", "ice", "moonlit", "cosmic", "alien", "aetherial", "neon", "urban", "industrial", "magitek", "ruins", "stone"],
        [MoodCategory] = ["coastal", "tropical", "seaside", "beach", "water", "specular", "clean", "sunlit", "colorful", "foliage", "rainforest", "lush", "verdant", "humid", "canopyLight", "desert", "badlands", "dry", "heat", "dust", "sunScorched", "snow", "cold", "ice", "crisp", "neon", "highTech", "electrope", "urban", "luminous", "industrial", "metallic", "smoky", "structured", "cosmic", "alien", "aetherial", "stars", "cool", "highDepth", "lunar", "moonlit", "fae", "dreamlike", "magical", "pastel", "ancient", "ruins", "fire", "warm", "wet", "mist", "haze", "clouds", "overcast", "gloom", "haunted", "dark", "open", "grassland", "highKey", "highContrast", "softLight"],
        [MaterialCategory] = ["water", "specular", "wet", "dry", "dust", "snow", "ice", "cold", "metallic", "steel", "stone", "crystal", "foliage", "fire", "heat"],
        [ArtDirectionCategory] = ["clean", "sunlit", "colorful", "canopyLight", "humid", "dreamlike", "magical", "pastel", "haunted", "gloom", "dark", "smoky", "luminous", "highDepth", "structured", "warm", "cool", "crisp", "highKey", "highContrast", "softLight", "sunScorched"]
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private SceneTagOverrideSet overrides = new();
    private SceneTagPresetSet tagPresets = new();

    public SceneAuthoringState CurrentState { get; private set; } = SceneAuthoringState.Disabled(SceneTags.Empty, string.Empty);

    public string OverrideFilePath { get; private set; } = string.Empty;

    public string TagPresetFilePath { get; private set; } = string.Empty;

    public string LastImportExportMessage { get; private set; } = "No scene authoring import/export action has been run yet.";

    public void Load(string rootDirectory)
    {
        var root = ResolveRoot(rootDirectory);
        OverrideFilePath = Path.Combine(root, "scene-overrides.json");
        TagPresetFilePath = Path.Combine(root, "tag-presets.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OverrideFilePath) ?? rootDirectory);
            if (!File.Exists(OverrideFilePath))
            {
                overrides = new SceneTagOverrideSet();
                SaveOverrides();
            }
            else
            {
                overrides = JsonSerializer.Deserialize<SceneTagOverrideSet>(File.ReadAllText(OverrideFilePath), JsonOptions) ?? new SceneTagOverrideSet();
                NormalizeOverrideSet();
            }

            LoadTagPresets();
        }
        catch
        {
            overrides = new SceneTagOverrideSet();
            tagPresets = CreateDefaultTagPresetSet();
        }
    }

    public SceneAuthoringState Apply(Configuration configuration, GameContext context, SceneTags detectedTags)
    {
        if (!configuration.EnableSceneAuthoringOverrides)
        {
            CurrentState = SceneAuthoringState.Disabled(detectedTags, OverrideFilePath);
            return CurrentState;
        }

        var active = FindOverride(context);
        if (active is null || !active.Enabled || !active.HasEdits)
        {
            CurrentState = new SceneAuthoringState(
                true,
                OverrideFilePath,
                active,
                detectedTags,
                detectedTags,
                SceneAuthoringState.EmptyMap,
                SceneAuthoringState.EmptyMap,
                Array.Empty<string>(),
                BuildFingerprint(detectedTags, null),
                active is null ? "No scene override exists for this territory." : "Scene override exists but has no edits.");
            return CurrentState;
        }

        var effective = BuildEffectiveTags(detectedTags, active);
        var warnings = BuildWarnings(detectedTags, effective, active);
        CurrentState = new SceneAuthoringState(
            true,
            OverrideFilePath,
            active,
            detectedTags,
            effective,
            BuildMap(active.AddedTags),
            BuildMap(active.RemovedTags),
            warnings,
            BuildFingerprint(effective, active),
            "Scene authoring override applied.");
        return CurrentState;
    }

    public SceneTagOverride GetOrCreateOverride(GameContext context)
    {
        var existing = FindOverride(context);
        if (existing is not null)
        {
            return existing;
        }

        var created = new SceneTagOverride
        {
            TerritoryId = context.TerritoryId,
            TerritoryName = context.TerritoryName
        };
        overrides.Entries.Add(created);
        SaveOverrides();
        return created;
    }

    public void AddTag(GameContext context, string category, string tag)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedTag = NormalizeTag(tag);
        if (string.IsNullOrWhiteSpace(normalizedCategory) || string.IsNullOrWhiteSpace(normalizedTag))
        {
            return;
        }

        var entry = GetOrCreateOverride(context);
        RemoveFrom(entry.RemovedTags, normalizedCategory, normalizedTag);
        AddTo(entry.AddedTags, normalizedCategory, normalizedTag);
        if (string.Equals(normalizedCategory, BiomeCategory, StringComparison.OrdinalIgnoreCase))
        {
            entry.PrimaryBiomeOverride = normalizedTag;
        }

        SaveOverrides();
    }

    public void RemoveTag(GameContext context, string category, string tag)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedTag = NormalizeTag(tag);
        if (string.IsNullOrWhiteSpace(normalizedCategory) || string.IsNullOrWhiteSpace(normalizedTag))
        {
            return;
        }

        var entry = GetOrCreateOverride(context);
        RemoveFrom(entry.AddedTags, normalizedCategory, normalizedTag);
        AddTo(entry.RemovedTags, normalizedCategory, normalizedTag);
        if (string.Equals(normalizedCategory, BiomeCategory, StringComparison.OrdinalIgnoreCase)
            && string.Equals(entry.PrimaryBiomeOverride, normalizedTag, StringComparison.OrdinalIgnoreCase))
        {
            entry.PrimaryBiomeOverride = string.Empty;
        }

        SaveOverrides();
    }

    public void SetPrimaryBiome(GameContext context, string biome)
    {
        var normalized = NormalizeTag(biome);
        var entry = GetOrCreateOverride(context);
        entry.PrimaryBiomeOverride = normalized;
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            AddTo(entry.AddedTags, BiomeCategory, normalized);
        }

        SaveOverrides();
    }

    public void ClearPrimaryBiomeOverride(GameContext context)
    {
        var entry = GetOrCreateOverride(context);
        if (!string.IsNullOrWhiteSpace(entry.PrimaryBiomeOverride))
        {
            RemoveFrom(entry.AddedTags, BiomeCategory, entry.PrimaryBiomeOverride);
        }

        entry.PrimaryBiomeOverride = string.Empty;
        SaveOverrides();
    }

    public void ClearTagOverride(GameContext context, string category, string tag)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedTag = NormalizeTag(tag);
        if (string.IsNullOrWhiteSpace(normalizedCategory) || string.IsNullOrWhiteSpace(normalizedTag))
        {
            return;
        }

        var entry = GetOrCreateOverride(context);
        RemoveFrom(entry.AddedTags, normalizedCategory, normalizedTag);
        RemoveFrom(entry.RemovedTags, normalizedCategory, normalizedTag);
        if (string.Equals(normalizedCategory, BiomeCategory, StringComparison.OrdinalIgnoreCase)
            && string.Equals(entry.PrimaryBiomeOverride, normalizedTag, StringComparison.OrdinalIgnoreCase))
        {
            entry.PrimaryBiomeOverride = string.Empty;
        }

        SaveOverrides();
    }

    public void ResetCurrentScene(GameContext context)
    {
        overrides.Entries.RemoveAll(entry => IsCurrentTerritory(entry, context));
        SaveOverrides();
    }

    public void SaveOverrides()
    {
        if (string.IsNullOrWhiteSpace(OverrideFilePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OverrideFilePath) ?? string.Empty);
        File.WriteAllText(OverrideFilePath, JsonSerializer.Serialize(overrides, JsonOptions));
    }

    public void ExportOverrides()
    {
        var target = SceneOverridesExportPath();
        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? string.Empty);
        SaveOverrides();
        File.Copy(OverrideFilePath, target, true);
        LastImportExportMessage = $"Scene overrides exported to {target}";
    }

    public void ImportOverrides()
    {
        var source = SceneOverridesExportPath();
        if (!File.Exists(source))
        {
            LastImportExportMessage = $"Scene override import skipped; file not found at {source}";
            return;
        }

        overrides = JsonSerializer.Deserialize<SceneTagOverrideSet>(File.ReadAllText(source), JsonOptions) ?? new SceneTagOverrideSet();
        NormalizeOverrideSet();
        SaveOverrides();
        LastImportExportMessage = $"Scene overrides imported from {source}";
    }

    public void ResetAllOverrides()
    {
        overrides = new SceneTagOverrideSet();
        SaveOverrides();
        LastImportExportMessage = "Scene overrides reset to empty.";
    }

    public IReadOnlyList<string> KnownTagsForCategory(string category)
    {
        var normalizedCategory = NormalizeCategory(category);
        if (string.IsNullOrWhiteSpace(normalizedCategory))
        {
            return Array.Empty<string>();
        }

        return tagPresets.Presets
            .Where(preset => preset.Categories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase))
            .Select(preset => preset.Tag)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<SceneTagPreset> PresetsForCategory(string category)
    {
        var normalizedCategory = NormalizeCategory(category);
        if (string.IsNullOrWhiteSpace(normalizedCategory))
        {
            return Array.Empty<SceneTagPreset>();
        }

        return tagPresets.Presets
            .Where(preset => preset.Categories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase))
            .OrderBy(preset => preset.Tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public SceneTagPreset? FindPreset(string category, string tag)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedTag = NormalizeTag(tag);
        return tagPresets.Presets.FirstOrDefault(preset =>
            preset.Categories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase)
            && string.Equals(preset.Tag, normalizedTag, StringComparison.OrdinalIgnoreCase));
    }

    public void AddCustomTagPreset(string category, string tag)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedTag = NormalizeTag(tag);
        if (string.IsNullOrWhiteSpace(normalizedCategory) || string.IsNullOrWhiteSpace(normalizedTag))
        {
            LastImportExportMessage = "Custom tag was not added because category or tag was empty.";
            return;
        }

        var preset = tagPresets.Presets.FirstOrDefault(item => string.Equals(item.Tag, normalizedTag, StringComparison.OrdinalIgnoreCase));
        if (preset is null)
        {
            preset = new SceneTagPreset
            {
                Tag = normalizedTag,
                DisplayName = normalizedTag,
                Description = "User-created tag. It can be applied to scenes immediately, but visual influence depends on whether the current profile/shader systems know this tag.",
                IsBuiltIn = false
            };
            preset.Effects.Add("Stored in scene authoring metadata.");
            preset.Effects.Add("Available for current-scene tag overrides.");
            tagPresets.Presets.Add(preset);
        }

        if (!preset.Categories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase))
        {
            preset.Categories.Add(normalizedCategory);
            preset.Categories.Sort(StringComparer.OrdinalIgnoreCase);
        }

        SaveTagPresets();
        LastImportExportMessage = $"Custom tag preset `{normalizedTag}` saved.";
    }

    public void UpdateTagPreset(string category, string tag, string displayName, string description)
    {
        var preset = FindPreset(category, tag);
        if (preset is null)
        {
            return;
        }

        preset.DisplayName = string.IsNullOrWhiteSpace(displayName) ? preset.Tag : displayName.Trim();
        preset.Description = description.Trim();
        SaveTagPresets();
    }

    public void ResetTagPreset(string category, string tag)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedTag = NormalizeTag(tag);
        var defaultPreset = CreateDefaultTagPresetSet().Presets.FirstOrDefault(preset =>
            string.Equals(preset.Tag, normalizedTag, StringComparison.OrdinalIgnoreCase)
            && preset.Categories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase));
        if (defaultPreset is null)
        {
            tagPresets.Presets.RemoveAll(preset =>
                !preset.IsBuiltIn
                && string.Equals(preset.Tag, normalizedTag, StringComparison.OrdinalIgnoreCase)
                && preset.Categories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase));
        }
        else
        {
            tagPresets.Presets.RemoveAll(preset =>
                string.Equals(preset.Tag, normalizedTag, StringComparison.OrdinalIgnoreCase)
                && preset.Categories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase));
            tagPresets.Presets.Add(defaultPreset);
        }

        NormalizeTagPresetSet();
        SaveTagPresets();
        LastImportExportMessage = $"Tag preset `{normalizedTag}` reset.";
    }

    public void ExportTagPresets()
    {
        var target = TagPresetsExportPath();
        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? string.Empty);
        SaveTagPresets();
        File.Copy(TagPresetFilePath, target, true);
        LastImportExportMessage = $"Tag presets exported to {target}";
    }

    public void ImportTagPresets()
    {
        var source = TagPresetsExportPath();
        if (!File.Exists(source))
        {
            LastImportExportMessage = $"Tag preset import skipped; file not found at {source}";
            return;
        }

        tagPresets = JsonSerializer.Deserialize<SceneTagPresetSet>(File.ReadAllText(source), JsonOptions) ?? CreateDefaultTagPresetSet();
        NormalizeTagPresetSet();
        SaveTagPresets();
        LastImportExportMessage = $"Tag presets imported from {source}";
    }

    public void ResetTagPresets()
    {
        tagPresets = CreateDefaultTagPresetSet();
        SaveTagPresets();
        LastImportExportMessage = "Tag presets reset to built-in defaults.";
    }

    private void LoadTagPresets()
    {
        if (!File.Exists(TagPresetFilePath))
        {
            tagPresets = CreateDefaultTagPresetSet();
            SaveTagPresets();
            return;
        }

        tagPresets = JsonSerializer.Deserialize<SceneTagPresetSet>(File.ReadAllText(TagPresetFilePath), JsonOptions) ?? CreateDefaultTagPresetSet();
        NormalizeTagPresetSet();
        MergeMissingDefaultTagPresets();
        SaveTagPresets();
    }

    private void SaveTagPresets()
    {
        if (string.IsNullOrWhiteSpace(TagPresetFilePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(TagPresetFilePath) ?? string.Empty);
        File.WriteAllText(TagPresetFilePath, JsonSerializer.Serialize(tagPresets, JsonOptions));
    }

    private void NormalizeTagPresetSet()
    {
        tagPresets.Presets = tagPresets.Presets
            .Where(preset => !string.IsNullOrWhiteSpace(preset.Tag))
            .Select(NormalizePreset)
            .GroupBy(preset => $"{preset.Tag}:{string.Join(",", preset.Categories)}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(preset => preset.Tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void MergeMissingDefaultTagPresets()
    {
        foreach (var defaultPreset in CreateDefaultTagPresetSet().Presets)
        {
            var exists = tagPresets.Presets.Any(preset =>
                string.Equals(preset.Tag, defaultPreset.Tag, StringComparison.OrdinalIgnoreCase)
                && preset.Categories.Any(category => defaultPreset.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)));
            if (!exists)
            {
                tagPresets.Presets.Add(defaultPreset);
            }
        }

        NormalizeTagPresetSet();
    }

    private static SceneTagPreset NormalizePreset(SceneTagPreset preset)
    {
        preset.Tag = NormalizeTag(preset.Tag);
        preset.DisplayName = string.IsNullOrWhiteSpace(preset.DisplayName) ? preset.Tag : preset.DisplayName.Trim();
        preset.Description = preset.Description?.Trim() ?? string.Empty;
        preset.Categories = preset.Categories
            .Select(NormalizeCategory)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();
        preset.Effects = preset.Effects
            .Select(effect => effect.Trim())
            .Where(effect => !string.IsNullOrWhiteSpace(effect))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(effect => effect, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return preset;
    }

    private static SceneTagPresetSet CreateDefaultTagPresetSet()
    {
        var set = new SceneTagPresetSet();
        foreach (var (category, tags) in KnownTags)
        {
            foreach (var tag in tags)
            {
                set.Presets.Add(new SceneTagPreset
                {
                    Tag = tag,
                    DisplayName = tag,
                    Description = DefaultDescription(category, tag),
                    Categories = [category],
                    Effects = DefaultEffects(category, tag).ToList(),
                    IsBuiltIn = true
                });
            }
        }

        return set;
    }

    private static string DefaultDescription(string category, string tag)
    {
        return category switch
        {
            AreaCategory => $"Area context tag. `{tag}` changes the effective structural scene context used by profile generation and diagnostics.",
            BiomeCategory => $"Primary biome option. `{tag}` can replace the detected biome and drive broad scene identity.",
            WeatherCategory => $"Weather tag. `{tag}` changes effective weather flags and downstream weather/atmosphere intent.",
            TimeCategory => $"Time tag. `{tag}` changes effective day/night/dawn-dusk state.",
            SecondaryCategory => $"Derived/supporting identity tag. `{tag}` usually contributes to secondary tag diagnostics and scene/material plausibility.",
            MaterialCategory => $"Material plausibility tag. `{tag}` can influence MaterialProfile and MaterialIntent channels when recognized by the material builders.",
            ArtDirectionCategory => $"Art-direction tag. `{tag}` can influence visual-profile tone, contrast, color, bloom, or atmosphere when recognized by profile rules.",
            _ => $"Scene tag `{tag}`."
        };
    }

    private static IEnumerable<string> DefaultEffects(string category, string tag)
    {
        yield return category switch
        {
            AreaCategory => "SceneTags area/context flags",
            BiomeCategory => "SceneTags.BiomeKey and biome confidence",
            WeatherCategory => "SceneTags weather flags and WeatherKey",
            TimeCategory => "SceneTags time flags",
            SecondaryCategory => "TagStackDiagnostics.SecondaryTags",
            MaterialCategory => "TagStackDiagnostics.MaterialTags",
            ArtDirectionCategory => "TagStackDiagnostics.ArtDirectionTags",
            MoodCategory => "SceneTags.MoodTags",
            _ => "Scene tag metadata"
        };

        if (category is BiomeCategory or SecondaryCategory or MoodCategory)
        {
            yield return "SceneIntent identity lanes when recognized";
            yield return "VisualProfile scene rules when recognized";
        }

        if (category is BiomeCategory or MaterialCategory or MoodCategory)
        {
            yield return "MaterialProfile and MaterialIntent plausibility when recognized";
        }

        if (category is WeatherCategory)
        {
            yield return "Weather/atmosphere profile rules";
        }

        if (category is TimeCategory)
        {
            yield return "Day/night profile rules";
        }

        if (category is ArtDirectionCategory)
        {
            yield return "Tone/color/bloom/contrast profile rules when recognized";
        }

        if (tag.Contains("Night", StringComparison.OrdinalIgnoreCase) || string.Equals(tag, "night", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Night scene lanes";
        }

        if (tag.Contains("Day", StringComparison.OrdinalIgnoreCase) || string.Equals(tag, "day", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Day scene lanes";
        }
    }

    private string SceneOverridesExportPath() => Path.Combine(Path.GetDirectoryName(OverrideFilePath) ?? ResolveRoot(string.Empty), "Exports", "scene-overrides-export.json");

    private string TagPresetsExportPath() => Path.Combine(Path.GetDirectoryName(TagPresetFilePath) ?? ResolveRoot(string.Empty), "Exports", "tag-presets-export.json");

    private SceneTagOverride? FindOverride(GameContext context)
    {
        return overrides.Entries.FirstOrDefault(entry => IsCurrentTerritory(entry, context));
    }

    private static bool IsCurrentTerritory(SceneTagOverride entry, GameContext context)
    {
        return entry.TerritoryId == context.TerritoryId
               && string.Equals(entry.Scope, "territory", StringComparison.OrdinalIgnoreCase);
    }

    private static SceneTags BuildEffectiveTags(SceneTags detected, SceneTagOverride active)
    {
        var moodTags = new HashSet<string>(detected.MoodTags, StringComparer.OrdinalIgnoreCase);
        foreach (var category in new[] { SecondaryCategory, MoodCategory, MaterialCategory, ArtDirectionCategory })
        {
            foreach (var removed in active.Removed(category))
            {
                moodTags.Remove(removed);
            }

            foreach (var added in active.Added(category))
            {
                moodTags.Add(added);
            }
        }

        var areaTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (detected.IsCityLike) areaTags.Add("city");
        if (detected.IsFieldLike) areaTags.Add("field");
        if (detected.IsInteriorLike) areaTags.Add("interior");
        if (detected.IsDungeonLike) areaTags.Add("dungeon");
        if (detected.IsRaidLike) areaTags.Add("raid");
        foreach (var tag in active.Removed(AreaCategory))
        {
            areaTags.Remove(tag);
        }
        foreach (var tag in active.Added(AreaCategory))
        {
            areaTags.Add(tag);
        }

        var weatherTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (detected.IsRain) weatherTags.Add("rain");
        if (detected.IsFog) weatherTags.Add("fog");
        if (detected.IsCloudy) weatherTags.Add("clouds");
        if (detected.IsOvercast) weatherTags.Add("overcast");
        if (detected.IsGloom) weatherTags.Add("gloom");
        if (detected.IsSnow) weatherTags.Add("snow");
        if (detected.IsStorm) weatherTags.Add("storm");
        if (detected.IsDustStorm) weatherTags.Add("dust");
        if (detected.IsHeatWave) weatherTags.Add("heat");
        if (detected.IsClear) weatherTags.Add("clear");
        foreach (var tag in active.Removed(WeatherCategory))
        {
            weatherTags.Remove(tag);
        }
        foreach (var tag in active.Added(WeatherCategory))
        {
            weatherTags.Add(tag);
        }
        if (weatherTags.Count > 1)
        {
            weatherTags.Remove("clear");
        }

        var time = detected.IsNight ? "night" : detected.IsDawnOrDusk ? "dawnDusk" : "day";
        foreach (var removed in active.Removed(TimeCategory))
        {
            if (string.Equals(time, removed, StringComparison.OrdinalIgnoreCase))
            {
                time = "day";
            }
        }
        var addedTime = active.Added(TimeCategory).LastOrDefault();
        if (!string.IsNullOrWhiteSpace(addedTime))
        {
            time = addedTime;
        }

        var biomeOverride = !string.IsNullOrWhiteSpace(active.PrimaryBiomeOverride)
            ? active.PrimaryBiomeOverride
            : active.Added(BiomeCategory).LastOrDefault();
        var removedBiome = active.Removed(BiomeCategory).Contains(detected.BiomeKey, StringComparer.OrdinalIgnoreCase);
        var biomeKey = !string.IsNullOrWhiteSpace(biomeOverride)
            ? biomeOverride
            : removedBiome
                ? "neutral"
                : detected.BiomeKey;
        var confidence = string.Equals(biomeKey, detected.BiomeKey, StringComparison.OrdinalIgnoreCase)
            ? detected.BiomeConfidence
            : MathF.Max(0.80f, detected.BiomeConfidence);
        var reason = string.Equals(biomeKey, detected.BiomeKey, StringComparison.OrdinalIgnoreCase)
            ? $"{detected.BiomeReason} Scene authoring edited secondary tags."
            : $"Scene authoring override changed primary biome from `{detected.BiomeKey}` to `{biomeKey}`.";

        return detected with
        {
            IsNight = string.Equals(time, "night", StringComparison.OrdinalIgnoreCase),
            IsDawnOrDusk = string.Equals(time, "dawnDusk", StringComparison.OrdinalIgnoreCase),
            IsRain = weatherTags.Contains("rain"),
            IsFog = weatherTags.Contains("fog"),
            IsCloudy = weatherTags.Contains("clouds"),
            IsOvercast = weatherTags.Contains("overcast"),
            IsGloom = weatherTags.Contains("gloom"),
            IsSnow = weatherTags.Contains("snow"),
            IsStorm = weatherTags.Contains("storm"),
            IsDustStorm = weatherTags.Contains("dust"),
            IsHeatWave = weatherTags.Contains("heat"),
            IsClear = weatherTags.Count == 0 || weatherTags.Contains("clear"),
            IsCityLike = areaTags.Contains("city"),
            IsDungeonLike = areaTags.Contains("dungeon"),
            IsRaidLike = areaTags.Contains("raid"),
            IsFieldLike = areaTags.Contains("field"),
            IsInteriorLike = areaTags.Contains("interior") || areaTags.Contains("dungeon") || areaTags.Contains("raid"),
            BiomeKey = biomeKey,
            BiomeConfidence = confidence,
            BiomeReason = reason,
            MoodTags = moodTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray(),
            SuppressedAuthoringTags = BuildMap(active.RemovedTags)
        };
    }

    private void NormalizeOverrideSet()
    {
        overrides.Entries.RemoveAll(entry => entry.TerritoryId == 0);
        foreach (var entry in overrides.Entries)
        {
            entry.Scope = string.IsNullOrWhiteSpace(entry.Scope) ? "territory" : entry.Scope;
            entry.Mode = string.IsNullOrWhiteSpace(entry.Mode) ? "merge" : entry.Mode;
            entry.AddedTags = NormalizeMap(entry.AddedTags);
            entry.RemovedTags = NormalizeMap(entry.RemovedTags);
        }
    }

    private static Dictionary<string, List<string>> NormalizeMap(Dictionary<string, List<string>> source)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (category, tags) in source)
        {
            var normalizedCategory = NormalizeCategory(category);
            if (string.IsNullOrWhiteSpace(normalizedCategory))
            {
                continue;
            }

            result[normalizedCategory] = tags
                .Select(NormalizeTag)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return result;
    }

    private static IReadOnlyList<string> BuildWarnings(SceneTags detected, SceneTags effective, SceneTagOverride active)
    {
        var warnings = new List<string>();
        var removed = active.RemovedTags.SelectMany(pair => pair.Value.Select(tag => (Category: pair.Key, Tag: tag))).ToArray();
        var added = active.AddedTags.SelectMany(pair => pair.Value.Select(tag => (Category: pair.Key, Tag: tag))).ToArray();

        if (removed.Any(item => IsReadabilityTag(item.Tag)))
        {
            warnings.Add("One or more readability/combat tags are removed. This can reduce gameplay clarity if those tags are later used by visual systems.");
        }

        if (detected.NeedsGameplayClarity && added.Any(item => IsHeavyMoodTag(item.Tag)))
        {
            warnings.Add("This scene needs gameplay clarity, but cinematic/heavy mood tags were added.");
        }

        if (active.Added(TimeCategory).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            warnings.Add("Multiple time tags are added. The last sorted override wins, which may not match user intent.");
        }

        if (active.Added(WeatherCategory).Any(tag => tag is "clear") && active.Added(WeatherCategory).Any(tag => tag is not "clear"))
        {
            warnings.Add("Clear weather is combined with other weather tags. Clear is suppressed when another weather tag is active.");
        }

        if (effective.IsNight && active.Added(ArtDirectionCategory).Any(tag => tag is "sunlit" or "highKey" or "goldenDay" or "sunScorched"))
        {
            warnings.Add("Night scene has bright day art-direction tags. This may create contradictory scene tuning.");
        }

        if (effective.IsDay && active.Added(ArtDirectionCategory).Any(tag => tag is "moonlit" or "dark" or "haunted"))
        {
            warnings.Add("Day scene has night/dark art-direction tags. This may create contradictory scene tuning.");
        }

        if (effective.BiomeKey is "desert" or "wasteland" && active.Added(MaterialCategory).Any(tag => tag is "snow" or "ice" or "cold"))
        {
            warnings.Add("Desert/wasteland biome has cold material tags. This may be intentional, but it is an unusual override.");
        }

        if (effective.BiomeKey is "snow" or "alpine" && active.Added(MaterialCategory).Any(tag => tag is "heat" or "fire" or "dust"))
        {
            warnings.Add("Snow/alpine biome has heat/dust material tags. This may be intentional, but it is an unusual override.");
        }

        return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool IsReadabilityTag(string tag)
    {
        return tag is "combatReadable" or "dutyReadable" or "gameplayRestrained";
    }

    private static bool IsHeavyMoodTag(string tag)
    {
        return tag is "haunted" or "dark" or "gloom" or "highContrast" or "luminous" or "dreamlike" or "magical";
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildMap(Dictionary<string, List<string>> source)
    {
        return source.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddTo(Dictionary<string, List<string>> map, string category, string tag)
    {
        if (!map.TryGetValue(category, out var tags))
        {
            tags = [];
            map[category] = tags;
        }

        if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(tag);
            tags.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void RemoveFrom(Dictionary<string, List<string>> map, string category, string tag)
    {
        if (!map.TryGetValue(category, out var tags))
        {
            return;
        }

        tags.RemoveAll(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildFingerprint(SceneTags tags, SceneTagOverride? active)
    {
        var overridePart = active is null
            ? "none"
            : string.Join(
                "|",
                active.PrimaryBiomeOverride,
                string.Join(",", active.AddedTags.SelectMany(pair => pair.Value.Select(value => $"+{pair.Key}:{value}")).OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
                string.Join(",", active.RemovedTags.SelectMany(pair => pair.Value.Select(value => $"-{pair.Key}:{value}")).OrderBy(value => value, StringComparer.OrdinalIgnoreCase)));

        return string.Join(
            ":",
            tags.AreaKey,
            tags.BiomeKey,
            tags.WeatherKey,
            tags.IsNight,
            tags.IsDawnOrDusk,
            string.Join(",", tags.MoodTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)),
            overridePart);
    }

    private static string NormalizeCategory(string category)
    {
        var trimmed = category.Trim();
        return EditableCategories.FirstOrDefault(candidate => string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private static string NormalizeTag(string tag) => tag.Trim();

    private static string ResolveRoot(string rootDirectory)
    {
        return Path.Combine(string.IsNullOrWhiteSpace(rootDirectory) ? Environment.CurrentDirectory : rootDirectory, "SceneAuthoring");
    }
}
