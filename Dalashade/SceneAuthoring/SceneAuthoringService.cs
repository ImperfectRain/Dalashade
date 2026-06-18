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

    public static readonly IReadOnlyList<string> SceneIntentTuningChannels =
    [
        nameof(SceneIntent.Readability),
        nameof(SceneIntent.Atmosphere),
        nameof(SceneIntent.HighlightProtection),
        nameof(SceneIntent.ShadowProtection),
        nameof(SceneIntent.Haze),
        nameof(SceneIntent.Wetness),
        nameof(SceneIntent.Cold),
        nameof(SceneIntent.Heat),
        nameof(SceneIntent.MagicGlow),
        nameof(SceneIntent.NeonGlow),
        nameof(SceneIntent.FoliageDensity),
        nameof(SceneIntent.IndustrialHardness),
        nameof(SceneIntent.CosmicMood),
        nameof(SceneIntent.Night),
        nameof(SceneIntent.Moonlight),
        nameof(SceneIntent.ArtificialLight),
        nameof(SceneIntent.AmbientDarkness),
        nameof(SceneIntent.NightAtmosphere),
        nameof(SceneIntent.Daylight),
        nameof(SceneIntent.Sunlight),
        nameof(SceneIntent.OpenSkyLight),
        nameof(SceneIntent.SurfaceHeat),
        nameof(SceneIntent.DayAtmosphere),
        nameof(SceneIntent.DayReflection),
        nameof(SceneIntent.DayHighlightPressure),
        nameof(SceneIntent.CombatPressure),
        nameof(SceneIntent.CinematicPermission)
    ];

    public static readonly IReadOnlyList<string> TuningTargets =
    [
        SceneTagTuningTargets.SceneIntent,
        SceneTagTuningTargets.MaterialIntent
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

    public string RegistryFingerprint => BuildRegistryFingerprint();

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

    public IReadOnlyList<SceneTagPreset> AllTagPresets() => tagPresets.Presets.ToArray();

    public IReadOnlyList<string> TuningChannelsForTarget(string target)
    {
        return string.Equals(target, SceneTagTuningTargets.MaterialIntent, StringComparison.OrdinalIgnoreCase)
            ? MaterialIntent.ChannelNames
            : SceneIntentTuningChannels;
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

    public void AddTagTuning(string category, string tag)
    {
        var preset = FindPreset(category, tag);
        if (preset is null)
        {
            return;
        }

        preset.Tunings.Add(new SceneTagTuning
        {
            Target = SceneTagTuningTargets.SceneIntent,
            Channel = nameof(SceneIntent.Atmosphere),
            Amount = 0.05f,
            Reason = "User-authored tag registry tuning."
        });
        preset.Tunings = preset.Tunings.Select(NormalizeTuning).ToList();
        SaveTagPresets();
    }

    public void UpdateTagTuning(string category, string tag, int index, SceneTagTuning tuning)
    {
        var preset = FindPreset(category, tag);
        if (preset is null || index < 0 || index >= preset.Tunings.Count)
        {
            return;
        }

        preset.Tunings[index] = NormalizeTuning(tuning);
        SaveTagPresets();
    }

    public void RemoveTagTuning(string category, string tag, int index)
    {
        var preset = FindPreset(category, tag);
        if (preset is null || index < 0 || index >= preset.Tunings.Count)
        {
            return;
        }

        preset.Tunings.RemoveAt(index);
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
        var shouldBackfillTunings = tagPresets.Version < 2;
        NormalizeTagPresetSet();
        MergeMissingDefaultTagPresets(shouldBackfillTunings);
        tagPresets.Version = 2;
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
        var shouldBackfillTunings = tagPresets.Version < 2;
        NormalizeTagPresetSet();
        MergeMissingDefaultTagPresets(shouldBackfillTunings);
        tagPresets.Version = 2;
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
        tagPresets.Version = tagPresets.Version <= 0 ? 1 : tagPresets.Version;
        tagPresets.Presets = tagPresets.Presets
            .Where(preset => !string.IsNullOrWhiteSpace(preset.Tag))
            .Select(NormalizePreset)
            .GroupBy(preset => $"{preset.Tag}:{string.Join(",", preset.Categories)}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(preset => preset.Tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void MergeMissingDefaultTagPresets(bool backfillTunings = false)
    {
        foreach (var defaultPreset in CreateDefaultTagPresetSet().Presets)
        {
            var existing = tagPresets.Presets.FirstOrDefault(preset =>
                string.Equals(preset.Tag, defaultPreset.Tag, StringComparison.OrdinalIgnoreCase)
                && preset.Categories.Any(category => defaultPreset.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)));
            if (existing is null)
            {
                tagPresets.Presets.Add(defaultPreset);
            }
            else if (backfillTunings && existing.Tunings.Count == 0)
            {
                existing.Tunings = defaultPreset.Tunings;
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
        preset.Tunings = (preset.Tunings ?? [])
            .Select(NormalizeTuning)
            .Where(tuning => !string.IsNullOrWhiteSpace(tuning.Channel))
            .ToList();
        return preset;
    }

    private static SceneTagTuning NormalizeTuning(SceneTagTuning tuning)
    {
        var target = TuningTargets.FirstOrDefault(candidate => string.Equals(candidate, tuning.Target, StringComparison.OrdinalIgnoreCase))
                     ?? (tuning.Target ?? string.Empty).Trim();
        var channelList = string.Equals(target, SceneTagTuningTargets.MaterialIntent, StringComparison.OrdinalIgnoreCase)
            ? MaterialIntent.ChannelNames
            : SceneIntentTuningChannels;
        var channel = channelList.FirstOrDefault(candidate => string.Equals(candidate, tuning.Channel, StringComparison.OrdinalIgnoreCase))
                      ?? (tuning.Channel ?? string.Empty).Trim();
        return new SceneTagTuning
        {
            Enabled = tuning.Enabled,
            Target = string.IsNullOrWhiteSpace(target) ? SceneTagTuningTargets.SceneIntent : target,
            Channel = channel,
            Amount = float.IsFinite(tuning.Amount) ? tuning.Amount : 0f,
            Reason = string.IsNullOrWhiteSpace(tuning.Reason) ? "Tag registry tuning." : tuning.Reason.Trim()
        };
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
                    Tunings = DefaultTunings(category, tag).ToList(),
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

    private static IEnumerable<SceneTagTuning> DefaultTunings(string category, string tag)
    {
        SceneTagTuning Scene(string channel, float amount, string reason) => new()
        {
            Target = SceneTagTuningTargets.SceneIntent,
            Channel = channel,
            Amount = amount,
            Reason = reason
        };

        SceneTagTuning Material(string channel, float amount, string reason) => new()
        {
            Target = SceneTagTuningTargets.MaterialIntent,
            Channel = channel,
            Amount = amount,
            Reason = reason
        };

        if (category == BiomeCategory)
        {
            foreach (var tuning in tag switch
            {
                "coastal" or "tropical" =>
                [
                    Scene(nameof(SceneIntent.Wetness), 0.08f, "Coastal/tropical tags support wet and specular scene response."),
                    Scene(nameof(SceneIntent.DayReflection), 0.10f, "Coastal/tropical tags support valid daylight reflection lanes."),
                    Scene(nameof(SceneIntent.DayHighlightPressure), 0.06f, "Coastal/tropical tags protect water, sand, and white surfaces."),
                    Material(MaterialIntent.WaterSpecularChannel, 0.14f, "Coastal/tropical tags raise water/specular material plausibility."),
                    Material(MaterialIntent.SandDustChannel, 0.06f, "Beach/coastal tags can imply sand without desert heat.")
                ],
                "forest" or "jungle" or "swamp" =>
                [
                    Scene(nameof(SceneIntent.FoliageDensity), 0.14f, "Forest/jungle/swamp tags support foliage-aware shader lanes."),
                    Scene(nameof(SceneIntent.Atmosphere), 0.05f, "Dense vegetation can preserve local atmosphere."),
                    Material(MaterialIntent.FoliageChannel, 0.16f, "Forest/jungle/swamp tags raise foliage material plausibility.")
                ],
                "desert" or "wasteland" =>
                [
                    Scene(nameof(SceneIntent.Heat), 0.12f, "Dry biomes support heat identity."),
                    Scene(nameof(SceneIntent.SurfaceHeat), 0.12f, "Dry biomes support sunlit hot-surface lanes."),
                    Scene(nameof(SceneIntent.DayHighlightPressure), 0.08f, "Sand and dry sky need highlight restraint."),
                    Material(MaterialIntent.SandDustChannel, 0.18f, "Dry biomes raise sand/dust material plausibility.")
                ],
                "snow" or "alpine" =>
                [
                    Scene(nameof(SceneIntent.Cold), 0.14f, "Snow/alpine tags support cold identity."),
                    Scene(nameof(SceneIntent.HighlightProtection), 0.10f, "Snow/alpine tags protect white highlights."),
                    Material(MaterialIntent.SnowIceChannel, 0.18f, "Snow/alpine tags raise snow/ice material plausibility.")
                ],
                "highTech" =>
                [
                    Scene(nameof(SceneIntent.NeonGlow), 0.10f, "High-tech tags support neon/glass response."),
                    Scene(nameof(SceneIntent.IndustrialHardness), 0.08f, "High-tech tags support constructed hard-surface identity."),
                    Material(MaterialIntent.NeonGlassChannel, 0.16f, "High-tech tags raise neon/glass material plausibility."),
                    Material(MaterialIntent.MetalIndustrialChannel, 0.10f, "High-tech tags raise metal/industrial material plausibility.")
                ],
                "imperial" =>
                [
                    Scene(nameof(SceneIntent.IndustrialHardness), 0.12f, "Imperial tags support hard constructed structure."),
                    Material(MaterialIntent.MetalIndustrialChannel, 0.18f, "Imperial tags raise metal/industrial material plausibility.")
                ],
                "aetherial" or "fae" or "lightFlooded" or "lunar" or "cosmic" =>
                [
                    Scene(nameof(SceneIntent.MagicGlow), 0.12f, "Aetherial/fae/lunar/cosmic tags support magical material response."),
                    Scene(nameof(SceneIntent.CosmicMood), tag is "lunar" or "cosmic" ? 0.12f : 0.04f, "Otherworldly tags support cosmic scene identity."),
                    Material(MaterialIntent.CrystalAetherChannel, 0.18f, "Otherworldly tags raise crystal/aether material plausibility.")
                ],
                "ancient" or "cave" =>
                [
                    Scene(nameof(SceneIntent.IndustrialHardness), 0.06f, "Ancient/cave tags support structured hard surfaces."),
                    Material(MaterialIntent.StoneRuinsChannel, 0.16f, "Ancient/cave tags raise stone/ruin material plausibility.")
                ],
                "volcanic" or "fire" =>
                [
                    Scene(nameof(SceneIntent.Heat), 0.16f, "Volcanic/fire tags support heat identity."),
                    Scene(nameof(SceneIntent.SurfaceHeat), 0.12f, "Volcanic/fire tags support hot-surface response."),
                    Material(MaterialIntent.FireLavaHeatChannel, 0.18f, "Volcanic/fire tags raise fire/lava/heat material plausibility.")
                ],
                _ => Array.Empty<SceneTagTuning>()
            })
            {
                yield return tuning;
            }
        }

        if (category == WeatherCategory)
        {
            foreach (var tuning in tag switch
            {
                "rain" or "storm" =>
                [
                    Scene(nameof(SceneIntent.Wetness), 0.12f, "Rain/storm tags support wet-surface response."),
                    Scene(nameof(SceneIntent.Haze), 0.04f, "Rain/storm tags support mild atmospheric diffusion."),
                    Material(MaterialIntent.WaterSpecularChannel, 0.08f, "Rain/storm tags raise wet/specular material plausibility.")
                ],
                "fog" or "clouds" or "overcast" =>
                [
                    Scene(nameof(SceneIntent.Haze), 0.10f, "Fog/cloud/overcast tags support atmospheric veil."),
                    Scene(nameof(SceneIntent.HighlightProtection), 0.06f, "Cloud/fog tags protect bright atmospheric regions."),
                    Material(MaterialIntent.SkyCloudFogChannel, 0.12f, "Fog/cloud/overcast tags raise sky/cloud/fog material plausibility.")
                ],
                "snow" =>
                [
                    Scene(nameof(SceneIntent.Cold), 0.12f, "Snow weather supports cold identity."),
                    Scene(nameof(SceneIntent.HighlightProtection), 0.10f, "Snow weather protects white highlights."),
                    Material(MaterialIntent.SnowIceChannel, 0.16f, "Snow weather raises snow/ice material plausibility.")
                ],
                "dust" or "heat" =>
                [
                    Scene(nameof(SceneIntent.Heat), 0.10f, "Dust/heat weather supports dry heat identity."),
                    Scene(nameof(SceneIntent.Haze), 0.08f, "Dust/heat weather supports distance air."),
                    Material(MaterialIntent.SandDustChannel, 0.12f, "Dust/heat weather raises sand/dust material plausibility.")
                ],
                "gloom" =>
                [
                    Scene(nameof(SceneIntent.AmbientDarkness), 0.08f, "Gloom supports darker ambient hierarchy."),
                    Scene(nameof(SceneIntent.ShadowProtection), 0.06f, "Gloom gets restrained dark-detail protection.")
                ],
                _ => Array.Empty<SceneTagTuning>()
            })
            {
                yield return tuning;
            }
        }

        if (category == TimeCategory)
        {
            foreach (var tuning in tag switch
            {
                "night" =>
                [
                    Scene(nameof(SceneIntent.Night), 0.18f, "Night tag supports night context."),
                    Scene(nameof(SceneIntent.AmbientDarkness), 0.08f, "Night tag supports darker ambient hierarchy."),
                    Scene(nameof(SceneIntent.NightAtmosphere), 0.05f, "Night tag supports night air lanes.")
                ],
                "day" =>
                [
                    Scene(nameof(SceneIntent.Daylight), 0.18f, "Day tag supports day context."),
                    Scene(nameof(SceneIntent.OpenSkyLight), 0.06f, "Day tag supports open sky light when receivers are valid.")
                ],
                "dawnDusk" =>
                [
                    Scene(nameof(SceneIntent.DayAtmosphere), 0.08f, "Dawn/dusk tag supports transition air."),
                    Scene(nameof(SceneIntent.HighlightProtection), 0.04f, "Dawn/dusk tag protects low-sun highlights.")
                ],
                _ => Array.Empty<SceneTagTuning>()
            })
            {
                yield return tuning;
            }
        }

        if (category == MaterialCategory)
        {
            foreach (var tuning in tag switch
            {
                "water" or "specular" or "wet" =>
                [
                    Scene(nameof(SceneIntent.Wetness), 0.08f, "Water/specular/wet tags support wet response."),
                    Material(MaterialIntent.WaterSpecularChannel, 0.16f, "Water/specular/wet tags raise water/specular material plausibility.")
                ],
                "foliage" =>
                [
                    Scene(nameof(SceneIntent.FoliageDensity), 0.10f, "Foliage tag supports foliage-aware lanes."),
                    Material(MaterialIntent.FoliageChannel, 0.16f, "Foliage tag raises foliage material plausibility.")
                ],
                "dry" or "dust" =>
                [
                    Scene(nameof(SceneIntent.Heat), 0.06f, "Dry/dust tags support heat identity."),
                    Material(MaterialIntent.SandDustChannel, 0.14f, "Dry/dust tags raise sand/dust material plausibility.")
                ],
                "snow" or "ice" or "cold" =>
                [
                    Scene(nameof(SceneIntent.Cold), 0.08f, "Snow/ice/cold tags support cold identity."),
                    Material(MaterialIntent.SnowIceChannel, 0.16f, "Snow/ice/cold tags raise snow/ice material plausibility.")
                ],
                "metallic" or "steel" =>
                [
                    Scene(nameof(SceneIntent.IndustrialHardness), 0.08f, "Metal/steel tags support hard-surface lanes."),
                    Material(MaterialIntent.MetalIndustrialChannel, 0.16f, "Metal/steel tags raise metal/industrial material plausibility.")
                ],
                "stone" =>
                [
                    Material(MaterialIntent.StoneRuinsChannel, 0.16f, "Stone tag raises stone/ruin material plausibility.")
                ],
                "crystal" =>
                [
                    Scene(nameof(SceneIntent.MagicGlow), 0.08f, "Crystal tag supports magical material response."),
                    Material(MaterialIntent.CrystalAetherChannel, 0.16f, "Crystal tag raises crystal/aether material plausibility.")
                ],
                "fire" or "heat" =>
                [
                    Scene(nameof(SceneIntent.Heat), 0.10f, "Fire/heat tags support heat identity."),
                    Material(MaterialIntent.FireLavaHeatChannel, 0.16f, "Fire/heat tags raise fire/lava/heat material plausibility.")
                ],
                _ => Array.Empty<SceneTagTuning>()
            })
            {
                yield return tuning;
            }
        }
    }

    private string SceneOverridesExportPath() => Path.Combine(Path.GetDirectoryName(OverrideFilePath) ?? ResolveRoot(string.Empty), "Exports", "scene-overrides-export.json");

    private string TagPresetsExportPath() => Path.Combine(Path.GetDirectoryName(TagPresetFilePath) ?? ResolveRoot(string.Empty), "Exports", "tag-presets-export.json");

    private string BuildRegistryFingerprint()
    {
        return string.Join(
            "|",
            tagPresets.Presets
                .OrderBy(preset => string.Join(",", preset.Categories), StringComparer.OrdinalIgnoreCase)
                .ThenBy(preset => preset.Tag, StringComparer.OrdinalIgnoreCase)
                .Select(preset => string.Join(
                    ":",
                    string.Join(",", preset.Categories.OrderBy(category => category, StringComparer.OrdinalIgnoreCase)),
                    preset.Tag,
                    string.Join(
                        ",",
                        preset.Tunings.Select(tuning => $"{tuning.Enabled}:{tuning.Target}:{tuning.Channel}:{tuning.Amount:0.###}:{tuning.Reason}")
                            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)))));
    }

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
