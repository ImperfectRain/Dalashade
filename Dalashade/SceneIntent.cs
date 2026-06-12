using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record SceneIntent(
    float Readability,
    float Atmosphere,
    float HighlightProtection,
    float ShadowProtection,
    float Haze,
    float Wetness,
    float Cold,
    float Heat,
    float MagicGlow,
    float NeonGlow,
    float FoliageDensity,
    float IndustrialHardness,
    float CosmicMood,
    float CombatPressure,
    float CinematicPermission,
    IReadOnlyList<SceneIntentContribution> Contributions)
{
    public static SceneIntent Neutral { get; } = new(
        0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f, 0f, 0f, 0f,
        Array.Empty<SceneIntentContribution>());
}

public sealed record SceneIntentContribution(string Source, string Intent, float Amount, string Reason);

public sealed class SceneIntentBuilder
{
    private readonly List<SceneIntentContribution> contributions = new();
    private float readability;
    private float atmosphere;
    private float highlightProtection;
    private float shadowProtection;
    private float haze;
    private float wetness;
    private float cold;
    private float heat;
    private float magicGlow;
    private float neonGlow;
    private float foliageDensity;
    private float industrialHardness;
    private float cosmicMood;
    private float combatPressure;
    private float cinematicPermission;

    public SceneIntent Build(GameContext context, SceneTags tags, ImageAnalysisResult imageAnalysis, Configuration configuration)
    {
        Add("Base", nameof(SceneIntent.Atmosphere), 0.25f, "Baseline atmosphere preservation.");
        Add("Cutscene/GPose state", nameof(SceneIntent.CinematicPermission), tags.CinematicAllowed ? 0.45f : 0.10f, tags.CinematicAllowed ? "Cinematic treatment is allowed." : "Cinematic treatment is constrained.");
        AddPresentationState(context, configuration);

        AddGameplayState(tags, configuration);
        AddTime(tags, configuration);
        AddWeather(tags, configuration);
        AddBiome(tags, configuration);
        AddScreenshotAnalysis(imageAnalysis, configuration);
        AddStyle(configuration.Style);
        AddPerformanceBudget(configuration.PerformanceBudget);

        _ = context;
        return new SceneIntent(
            Clamp01(readability),
            Clamp01(atmosphere),
            Clamp01(highlightProtection),
            Clamp01(shadowProtection),
            Clamp01(haze),
            Clamp01(wetness),
            Clamp01(cold),
            Clamp01(heat),
            Clamp01(magicGlow),
            Clamp01(neonGlow),
            Clamp01(foliageDensity),
            Clamp01(industrialHardness),
            Clamp01(cosmicMood),
            Clamp01(combatPressure),
            Clamp01(cinematicPermission),
            contributions.ToArray());
    }

    private void AddGameplayState(SceneTags tags, Configuration configuration)
    {
        if (configuration.AutoAdjustInCombat && tags.NeedsCombatClarity)
        {
            Add("Combat", nameof(SceneIntent.Readability), 0.80f, "Active combat needs strong readability.");
            Add("Combat", nameof(SceneIntent.CombatPressure), 1.00f, "Combat should dominate safety decisions.");
            Add("Combat", nameof(SceneIntent.CinematicPermission), -0.35f, "Combat reduces cinematic permission.");
            Add("Combat", nameof(SceneIntent.Atmosphere), -0.20f, "Combat dampens heavy atmosphere so mechanics stay readable.");
            Add("Combat", nameof(SceneIntent.Haze), -0.12f, "Combat reduces haze pressure without removing weather tags.");
            Add("Combat", nameof(SceneIntent.MagicGlow), -0.10f, "Combat reduces stylized glow pressure.");
            Add("Combat", nameof(SceneIntent.NeonGlow), -0.08f, "Combat reduces stylized neon glow pressure.");
        }
        else if (configuration.AutoAdjustInCombat && tags.NeedsDutyReadability)
        {
            Add("Duty", nameof(SceneIntent.Readability), 0.35f, "Duty content outside combat needs mild readability.");
            Add("Duty", nameof(SceneIntent.CombatPressure), 0.25f, "Duty content adds light gameplay pressure.");
            Add("Duty", nameof(SceneIntent.CinematicPermission), -0.10f, "Duty readability slightly reduces cinematic permission.");
            Add("Duty", nameof(SceneIntent.Atmosphere), -0.06f, "Duty readability slightly dampens atmosphere.");
        }

        if (tags.IsDungeonLike || tags.IsRaidLike)
        {
            Add(tags.IsRaidLike ? "Raid area" : "Dungeon area", nameof(SceneIntent.Readability), tags.NeedsCombatClarity ? 0.15f : 0.25f, "Duty-style areas need readable gameplay spaces.");
        }
    }

    private void AddPresentationState(GameContext context, Configuration configuration)
    {
        if (context.InGpose)
        {
            Add("GPose", nameof(SceneIntent.CinematicPermission), 0.35f, "GPose permits stronger cinematic treatment.");
            Add("GPose", nameof(SceneIntent.Atmosphere), 0.14f, "GPose can preserve more atmosphere.");
            return;
        }

        if (configuration.AutoAdjustInCutscenes && context.InCutscene && !context.InCombat)
        {
            Add("Cutscene", nameof(SceneIntent.CinematicPermission), 0.22f, "Cutscenes can use more cinematic treatment than normal gameplay.");
            Add("Cutscene", nameof(SceneIntent.Atmosphere), 0.08f, "Cutscenes can preserve a little more atmosphere.");
        }
    }

    private void AddTime(SceneTags tags, Configuration configuration)
    {
        if (configuration.AutoAdjustAtNight && tags.IsNight)
        {
            Add("Night", nameof(SceneIntent.Readability), 0.18f, "Night scenes need mild readability.");
            Add("Night", nameof(SceneIntent.ShadowProtection), 0.45f, "Night scenes risk crushed shadows.");
            Add("Night", nameof(SceneIntent.Atmosphere), 0.20f, "Night atmosphere should be preserved.");
        }

        if (configuration.AutoAdjustAtNight && tags.IsDawnOrDusk)
        {
            Add("Dawn/dusk", nameof(SceneIntent.Atmosphere), 0.12f, "Low sun supports mild atmosphere.");
        }
    }

    private void AddWeather(SceneTags tags, Configuration configuration)
    {
        if (!configuration.AutoAdjustForWeather)
        {
            return;
        }

        if (tags.IsFog)
        {
            Add("Fog weather", nameof(SceneIntent.Haze), 0.75f, "Fog and mist are strong haze signals.");
            Add("Fog weather", nameof(SceneIntent.HighlightProtection), 0.25f, "Fog can bloom bright detail.");
            Add("Fog weather", nameof(SceneIntent.Atmosphere), 0.25f, "Fog atmosphere should not be erased.");
        }

        if (tags.IsCloudy || tags.IsOvercast)
        {
            Add(tags.IsOvercast ? "Overcast weather" : "Cloud weather", nameof(SceneIntent.Haze), 0.30f, "Clouds are a mild haze signal.");
            Add(tags.IsOvercast ? "Overcast weather" : "Cloud weather", nameof(SceneIntent.HighlightProtection), 0.18f, "Clouds can flatten highlights.");
        }

        if (tags.IsGloom)
        {
            Add("Gloom weather", nameof(SceneIntent.Haze), 0.16f, "Gloom is a dark mood signal, not full fog.");
            Add("Gloom weather", nameof(SceneIntent.ShadowProtection), 0.24f, "Gloom needs some dark-detail protection without flattening blacks.");
            Add("Gloom weather", nameof(SceneIntent.Atmosphere), 0.22f, "Gloom atmosphere should be preserved.");
        }

        if (tags.IsRain)
        {
            Add("Rain weather", nameof(SceneIntent.Wetness), 0.65f, "Rain creates wet-surface behavior.");
            Add("Rain weather", nameof(SceneIntent.HighlightProtection), 0.35f, "Wet specular highlights need protection.");
        }

        if (tags.IsStorm)
        {
            Add("Storm weather", nameof(SceneIntent.Wetness), 0.75f, "Storms are strong wet-scene signals.");
            Add("Storm weather", nameof(SceneIntent.HighlightProtection), 0.55f, "Storm highlights and lightning need protection.");
            Add("Storm weather", nameof(SceneIntent.Readability), 0.20f, "Storms need extra readability.");
            Add("Storm weather", nameof(SceneIntent.Cold), 0.25f, "Storms bias cooler.");
        }

        if (tags.IsSnow)
        {
            Add("Snow weather", nameof(SceneIntent.Cold), 0.75f, "Snow is the primary cold signal.");
            Add("Snow weather", nameof(SceneIntent.HighlightProtection), 0.65f, "Snow risks blown white detail.");
        }

        if (tags.IsDustStorm || tags.IsHeatWave)
        {
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.Heat), 0.65f, "Dust and heat are hot-scene signals.");
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.Haze), tags.IsNight && tags.IsHeatWave && !tags.IsDustStorm ? 0.34f : 0.45f, "Dust and heat add distance-biased glare/haze.");
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.HighlightProtection), tags.IsNight && tags.IsHeatWave && !tags.IsDustStorm ? 0.32f : 0.45f, "Night heat needs less highlight restraint than daytime glare.");
        }
    }

    private void AddBiome(SceneTags tags, Configuration configuration)
    {
        if (!configuration.AutoAdjustForTerritory)
        {
            return;
        }

        var styleScale = BiomeStyleScale(tags);

        switch (tags.BiomeKey)
        {
            case "forest":
            case "jungle":
            case "swamp":
                Add("Biome", nameof(SceneIntent.FoliageDensity), (tags.BiomeKey == "jungle" ? 0.76f : 0.70f) * styleScale, "Dense foliage changes depth and sharpness needs.");
                Add("Biome", nameof(SceneIntent.ShadowProtection), (tags.IsNight && tags.BiomeKey == "jungle" ? 0.14f : 0.25f) * styleScale, "Foliage-heavy scenes need selective dark-detail protection.");
                Add("Biome", nameof(SceneIntent.Atmosphere), (tags.IsNight && tags.BiomeKey == "jungle" ? 0.06f : 0.08f) * styleScale, "Dense foliage can keep mild environmental atmosphere.");
                if (tags.MoodTags.Contains("canopyLight", StringComparer.OrdinalIgnoreCase))
                {
                    Add("Art direction", nameof(SceneIntent.MagicGlow), 0.08f * styleScale, "Canopy-light tags allow subtle sky openings without global wash.");
                }
                if (tags.IsNight && tags.BiomeKey == "jungle")
                {
                    Add("Jungle night", nameof(SceneIntent.Haze), -0.10f * styleScale, "Jungle night should keep background depth instead of adding a gray veil.");
                    Add("Jungle night", nameof(SceneIntent.ShadowProtection), -0.16f * styleScale, "Jungle night preserves dark trunks and background depth.");
                }
                break;
            case "desert":
            case "wasteland":
                Add("Biome", nameof(SceneIntent.Heat), 0.48f * styleScale, "Desert and wasteland biomes are hot-scene signals.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), (tags.IsNight ? 0.24f : 0.38f) * styleScale, "Dry bright biomes need glare control, with less restraint at night.");
                Add("Biome", nameof(SceneIntent.Haze), (tags.IsNight ? 0.06f : 0.10f) * styleScale, "Dry terrain adds light dust atmosphere without acting like fog.");
                break;
            case "snow":
            case "alpine":
                Add("Biome", nameof(SceneIntent.Cold), (tags.IsSnow ? 0.18f : 0.45f) * styleScale, tags.IsSnow ? "Snow weather already supplied the main cold signal." : "Snow/alpine biome adds cold pressure.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), (tags.IsSnow ? 0.12f : 0.34f) * styleScale, tags.IsSnow ? "Snow biome highlight contribution is dampened." : "Snow/alpine biome needs highlight protection.");
                break;
            case "cave":
            case "void":
                Add("Biome", nameof(SceneIntent.ShadowProtection), 0.42f * styleScale, "Dark biomes need some shadow detail without washing out depth.");
                Add("Biome", nameof(SceneIntent.Atmosphere), 0.20f * styleScale, "Dark biomes should retain mood.");
                break;
            case "aetherial":
            case "fae":
            case "lightFlooded":
                Add("Biome", nameof(SceneIntent.MagicGlow), 0.55f * styleScale, "Magical biomes should preserve stylized glow.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.22f * styleScale, "Magical glow needs highlight control.");
                if (tags.MoodTags.Contains("dreamlike", StringComparer.OrdinalIgnoreCase))
                {
                    Add("Fae mood", nameof(SceneIntent.Atmosphere), 0.10f * styleScale, "Dreamlike fae zones keep a little more atmosphere.");
                }
                break;
            case "cosmic":
            case "lunar":
                Add("Biome", nameof(SceneIntent.MagicGlow), 0.38f * styleScale, "Cosmic/lunar spaces preserve some magical glow.");
                Add("Biome", nameof(SceneIntent.CosmicMood), 0.74f * styleScale, "Cosmic/lunar spaces get their own mood channel.");
                Add("Biome", nameof(SceneIntent.Cold), (tags.BiomeKey == "lunar" ? 0.24f : 0.10f) * styleScale, "Cosmic and lunar scenes bias cooler.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.20f * styleScale, "Cosmic highlights need control.");
                Add("Biome", nameof(SceneIntent.Atmosphere), 0.10f * styleScale, "Otherworldly zones can keep controlled depth atmosphere.");
                break;
            case "highTech":
                Add("Biome", nameof(SceneIntent.NeonGlow), 0.78f * styleScale, "High-tech zones should preserve neon accents.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.38f * styleScale, "Neon and glossy surfaces need highlight protection.");
                Add("Neon mood", nameof(SceneIntent.Atmosphere), 0.06f * styleScale, "High-tech neon spaces keep a controlled ambient glow.");
                break;
            case "imperial":
                Add("Biome", nameof(SceneIntent.IndustrialHardness), 0.68f * styleScale, "Imperial spaces have hard industrial contrast.");
                Add("Biome", nameof(SceneIntent.Readability), 0.12f * styleScale, "Industrial spaces benefit from clarity.");
                Add("Industrial mood", nameof(SceneIntent.Atmosphere), -0.04f * styleScale, "Industrial spaces favor structure over haze.");
                break;
            case "ancient":
                Add("Biome", nameof(SceneIntent.IndustrialHardness), 0.28f * styleScale, "Ancient/ruin spaces need structural clarity.");
                Add("Biome", nameof(SceneIntent.Readability), 0.12f * styleScale, "Ruins benefit from readable structure.");
                Add("Biome", nameof(SceneIntent.Atmosphere), 0.04f * styleScale, "Ruins can keep light age and depth atmosphere.");
                break;
            case "coastal":
            case "tropical":
            case "underwater":
                Add("Biome", nameof(SceneIntent.HighlightProtection), (tags.IsNight ? 0.24f : 0.38f) * styleScale, "Water and bright surfaces need specular control.");
                if (tags.BiomeKey is "coastal" or "tropical")
                {
                    Add("Biome", nameof(SceneIntent.FoliageDensity), (tags.BiomeKey == "tropical" ? 0.30f : 0.20f) * styleScale, "Coastal and tropical field zones can carry mild foliage density.");
                    Add("Biome", nameof(SceneIntent.Heat), (tags.IsNight ? 0.04f : 0.10f) * styleScale, "Sunlit coastal and tropical scenes can lean gently warm without becoming dusty.");
                    Add("Biome", nameof(SceneIntent.Atmosphere), 0.06f * styleScale, "Coastal scenes keep clean open-air atmosphere without haze.");
                }
                break;
            case "volcanic":
            case "fire":
                Add("Biome", nameof(SceneIntent.Heat), 0.55f * styleScale, "Fire and volcanic biomes are hot-scene signals.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.35f * styleScale, "Hot highlights need control.");
                break;
        }
    }

    private void AddScreenshotAnalysis(ImageAnalysisResult imageAnalysis, Configuration configuration)
    {
        if (!configuration.AutoAdjustFromScreenshots || !imageAnalysis.Available)
        {
            return;
        }

        if (imageAnalysis.HighlightClipping > 0.04f || imageAnalysis.AverageLuminance > 0.72f)
        {
            Add("Screenshot analysis", nameof(SceneIntent.HighlightProtection), Scale01(imageAnalysis.HighlightClipping, 0.12f) * 0.35f, "Current image analysis found bright or clipped highlights.");
        }

        if (imageAnalysis.ShadowClipping > 0.08f || imageAnalysis.AverageLuminance < 0.30f)
        {
            Add("Screenshot analysis", nameof(SceneIntent.ShadowProtection), Scale01(imageAnalysis.ShadowClipping, 0.30f) * 0.35f, "Current image analysis found dark or clipped shadows.");
            Add("Screenshot analysis", nameof(SceneIntent.Readability), Scale01(0.30f - imageAnalysis.AverageLuminance, 0.30f) * 0.20f, "Current image analysis found low luminance.");
        }

        if (imageAnalysis.Contrast < 0.14f)
        {
            Add("Screenshot analysis", nameof(SceneIntent.Readability), 0.12f, "Current image analysis found low contrast.");
        }
    }

    private void AddStyle(TargetStyle style)
    {
        switch (style)
        {
            case TargetStyle.Gameplay:
                Add("Target style", nameof(SceneIntent.Readability), 0.15f, "Gameplay style favors readability.");
                Add("Target style", nameof(SceneIntent.CinematicPermission), -0.10f, "Gameplay style reduces cinematic pressure.");
                break;
            case TargetStyle.Cinematic:
                Add("Target style", nameof(SceneIntent.Atmosphere), 0.18f, "Cinematic style preserves more atmosphere.");
                Add("Target style", nameof(SceneIntent.CinematicPermission), 0.20f, "Cinematic style allows stronger image shaping.");
                break;
        }
    }

    private void AddPerformanceBudget(PerformanceBudget budget)
    {
        if (budget == PerformanceBudget.Ultra)
        {
            return;
        }

        var amount = budget switch
        {
            PerformanceBudget.Low => 1.0f,
            PerformanceBudget.Medium => 0.45f,
            PerformanceBudget.High => 0.18f,
            _ => 0.45f
        };

        Add("Performance budget", nameof(SceneIntent.Readability), 0.12f * amount, "Lower budgets favor readable inexpensive effects.");
        Add("Performance budget", nameof(SceneIntent.CombatPressure), 0.10f * amount, "Lower budgets add a little safety pressure.");
        Add("Performance budget", nameof(SceneIntent.CinematicPermission), -0.06f * amount, "Lower budgets reduce expensive-looking cinematic pressure.");
        Add("Performance budget", nameof(SceneIntent.MagicGlow), -0.05f * amount, "Lower budgets soften extra stylized glow rather than flattening the whole scene.");
        Add("Performance budget", nameof(SceneIntent.NeonGlow), -0.04f * amount, "Lower budgets soften extra neon glow rather than flattening the whole scene.");
        Add("Performance budget", nameof(SceneIntent.Haze), -0.04f * amount, "Lower budgets reduce heavy haze pressure conservatively.");
    }

    private void Add(string source, string intent, float amount, string reason)
    {
        if (MathF.Abs(amount) <= 0.0005f)
        {
            return;
        }

        switch (intent)
        {
            case nameof(SceneIntent.Readability):
                readability += amount;
                break;
            case nameof(SceneIntent.Atmosphere):
                atmosphere += amount;
                break;
            case nameof(SceneIntent.HighlightProtection):
                highlightProtection += amount;
                break;
            case nameof(SceneIntent.ShadowProtection):
                shadowProtection += amount;
                break;
            case nameof(SceneIntent.Haze):
                haze += amount;
                break;
            case nameof(SceneIntent.Wetness):
                wetness += amount;
                break;
            case nameof(SceneIntent.Cold):
                cold += amount;
                break;
            case nameof(SceneIntent.Heat):
                heat += amount;
                break;
            case nameof(SceneIntent.MagicGlow):
                magicGlow += amount;
                break;
            case nameof(SceneIntent.NeonGlow):
                neonGlow += amount;
                break;
            case nameof(SceneIntent.FoliageDensity):
                foliageDensity += amount;
                break;
            case nameof(SceneIntent.IndustrialHardness):
                industrialHardness += amount;
                break;
            case nameof(SceneIntent.CosmicMood):
                cosmicMood += amount;
                break;
            case nameof(SceneIntent.CombatPressure):
                combatPressure += amount;
                break;
            case nameof(SceneIntent.CinematicPermission):
                cinematicPermission += amount;
                break;
        }

        contributions.Add(new SceneIntentContribution(source, intent, amount, reason));
    }

    private static float Clamp01(float value) => MathF.Min(1f, MathF.Max(0f, value));

    private static float Scale01(float value, float range) => Clamp01(value / range);

    private static float BiomeStyleScale(SceneTags tags) => 0.55f + (Clamp01(tags.BiomeConfidence) * 0.45f);
}

public sealed record TagStackContribution(
    string Variable,
    string Source,
    string Change,
    float Before,
    float After,
    bool Dampened = false,
    bool BudgetApplied = false);

public sealed record TagStackDiagnostics(
    uint TerritoryId,
    string TerritoryName,
    uint? WeatherId,
    string WeatherName,
    IReadOnlyList<string> ActiveTags,
    IReadOnlyList<string> ActiveWeatherTags,
    IReadOnlyList<string> SecondaryTags,
    IReadOnlyList<string> MoodTags,
    IReadOnlyList<string> MaterialTags,
    IReadOnlyList<string> AreaContextTags,
    IReadOnlyList<string> GameplayStateTags,
    IReadOnlyList<string> ArtDirectionTags,
    string WeatherKey,
    string BiomeKey,
    float BiomeConfidence,
    string BiomeReason,
    string AreaKey,
    bool InCombat,
    bool InDuty,
    bool InCutscene,
    bool InGpose,
    SceneIntent Intent,
    IReadOnlyList<SceneIntentContribution> IntentContributions,
    IReadOnlyList<TagStackContribution> Contributions)
{
    public static TagStackDiagnostics Empty { get; } = new(
        0,
        "Unknown",
        null,
        "Unknown",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        "unknown",
        "unknown",
        0f,
        "No scene tags have been classified yet.",
        "unknown",
        false,
        false,
        false,
        false,
        SceneIntent.Neutral,
        Array.Empty<SceneIntentContribution>(),
        Array.Empty<TagStackContribution>());

    public static TagStackDiagnostics Create(GameContext context, SceneTags tags, SceneIntent intent, IReadOnlyList<TagStackContribution> contributions)
    {
        return new TagStackDiagnostics(
            context.TerritoryId,
            context.TerritoryName,
            context.WeatherId,
            context.WeatherName,
            BuildActiveTags(context, tags),
            BuildActiveWeatherTags(tags),
            BuildSecondaryTags(tags),
            tags.MoodTags,
            BuildMaterialTags(tags),
            BuildAreaContextTags(tags),
            BuildGameplayStateTags(context, tags),
            BuildArtDirectionTags(tags),
            tags.WeatherKey,
            tags.BiomeKey,
            tags.BiomeConfidence,
            tags.BiomeReason,
            tags.AreaKey,
            context.InCombat,
            context.InDuty,
            context.InCutscene,
            context.InGpose,
            intent,
            intent.Contributions,
            contributions);
    }

    private static IReadOnlyList<string> BuildActiveTags(GameContext context, SceneTags tags)
    {
        var result = new List<string>();
        if (tags.IsNight) result.Add("Night");
        if (tags.IsDawnOrDusk) result.Add("DawnDusk");
        if (tags.IsRain) result.Add("RainWeather");
        if (tags.IsFog) result.Add("FogWeather");
        if (tags.IsCloudy) result.Add("CloudWeather");
        if (tags.IsOvercast) result.Add("OvercastWeather");
        if (tags.IsGloom) result.Add("GloomWeather");
        if (tags.IsSnow) result.Add("SnowWeather");
        if (tags.IsStorm) result.Add("StormWeather");
        if (tags.IsDustStorm) result.Add("DustStormWeather");
        if (tags.IsHeatWave) result.Add("HeatWaveWeather");
        foreach (var mood in tags.MoodTags)
        {
            result.Add($"{mood}Mood");
        }
        if (tags.IsCityLike) result.Add("City");
        if (tags.IsDungeonLike) result.Add("Dungeon");
        if (tags.IsRaidLike) result.Add("Raid");
        if (tags.IsInteriorLike) result.Add("Interior");
        if (tags.IsFieldLike) result.Add("Field");
        if (tags.NeedsCombatClarity) result.Add("Combat");
        if (tags.NeedsDutyReadability) result.Add("DutyReadability");
        if (context.InCutscene) result.Add("Cutscene");
        if (context.InGpose) result.Add("GPose");
        if (!string.IsNullOrWhiteSpace(tags.BiomeKey) && tags.BiomeKey != "neutral" && tags.BiomeKey != "unknown")
        {
            result.Add($"{tags.BiomeKey}Biome");
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<string> BuildSecondaryTags(SceneTags tags)
    {
        var result = new List<string>();
        if (tags.BiomeKey is "coastal") result.AddRange(new[] { "seaside", "beach", "tropical" });
        if (tags.BiomeKey is "tropical") result.AddRange(new[] { "coastal", "seaside", "lush" });
        if (tags.BiomeKey is "jungle") result.AddRange(new[] { "rainforest", "lush", "verdant" });
        if (tags.BiomeKey is "desert") result.AddRange(new[] { "badlands", "dry" });
        if (tags.BiomeKey is "snow") result.AddRange(new[] { "alpine", "ice" });
        if (tags.BiomeKey is "lunar") result.AddRange(new[] { "moonlit", "cosmic" });
        if (tags.BiomeKey is "cosmic") result.AddRange(new[] { "alien", "aetherial" });
        if (tags.BiomeKey is "highTech") result.AddRange(new[] { "neon", "urban" });
        if (tags.BiomeKey is "imperial") result.AddRange(new[] { "industrial", "magitek" });
        if (tags.BiomeKey is "ancient") result.AddRange(new[] { "ruins", "stone" });
        result.AddRange(tags.MoodTags.Where(IsSecondaryMoodTag));
        return NormalizeTags(result);
    }

    private static IReadOnlyList<string> BuildMaterialTags(SceneTags tags)
    {
        var result = new List<string>();
        result.AddRange(tags.MoodTags.Where(IsMaterialTag));
        if (tags.IsRain) result.AddRange(new[] { "wet", "specular" });
        if (tags.IsDustStorm) result.Add("dust");
        if (tags.IsSnow) result.AddRange(new[] { "snow", "ice" });
        return NormalizeTags(result);
    }

    private static IReadOnlyList<string> BuildAreaContextTags(SceneTags tags)
    {
        var result = new List<string> { tags.AreaKey };
        if (tags.IsCityLike) result.Add("city");
        if (tags.IsFieldLike) result.Add("field");
        if (tags.IsInteriorLike) result.Add("interior");
        if (tags.IsDungeonLike) result.Add("dungeon");
        if (tags.IsRaidLike) result.Add("raid");
        return NormalizeTags(result);
    }

    private static IReadOnlyList<string> BuildGameplayStateTags(GameContext context, SceneTags tags)
    {
        var result = new List<string>();
        if (tags.NeedsCombatClarity) result.Add("combatReadable");
        if (tags.NeedsDutyReadability) result.Add("dutyReadable");
        if (tags.NeedsGameplayClarity) result.Add("gameplayRestrained");
        if (tags.CinematicAllowed) result.Add("cinematicAllowed");
        if (context.InCutscene) result.Add("cutscene");
        if (context.InGpose) result.Add("gpose");
        return NormalizeTags(result);
    }

    private static IReadOnlyList<string> BuildArtDirectionTags(SceneTags tags)
    {
        var result = new List<string>();
        result.AddRange(tags.MoodTags.Where(IsArtDirectionTag));
        if (tags.IsNight) result.Add("night");
        if (tags.IsDawnOrDusk) result.Add("goldenHour");
        if (tags.IsGloom) result.Add("haunted");
        return NormalizeTags(result);
    }

    private static IReadOnlyList<string> BuildActiveWeatherTags(SceneTags tags)
    {
        var result = new List<string>();
        if (tags.IsRain) result.Add("rain");
        if (tags.IsFog) result.Add("fog");
        if (tags.IsCloudy) result.Add("clouds");
        if (tags.IsOvercast) result.Add("overcast");
        if (tags.IsGloom) result.Add("gloom");
        if (tags.IsSnow) result.Add("snow");
        if (tags.IsStorm) result.Add("storm");
        if (tags.IsDustStorm) result.Add("dust");
        if (tags.IsHeatWave) result.Add("heat");
        if (tags.IsClear) result.Add("clear");
        return result;
    }

    private static bool IsSecondaryMoodTag(string tag)
    {
        return tag is "coastal" or "tropical" or "seaside" or "beach" or "rainforest" or "lush" or "verdant" or "desert" or "badlands" or "alpine" or "lunar" or "cosmic" or "alien" or "aetherial" or "fae" or "ancient" or "ruins" or "highTech" or "neon" or "urban" or "imperial" or "industrial" or "moonlit";
    }

    private static bool IsMaterialTag(string tag)
    {
        return tag is "water" or "specular" or "wet" or "dry" or "dust" or "snow" or "ice" or "cold" or "metallic" or "steel" or "stone" or "crystal" or "foliage" or "fire" or "heat";
    }

    private static bool IsArtDirectionTag(string tag)
    {
        return tag is "clean" or "sunlit" or "colorful" or "canopyLight" or "humid" or "dreamlike" or "magical" or "pastel" or "haunted" or "gloom" or "dark" or "smoky" or "luminous" or "highDepth" or "structured" or "warm" or "cool" or "crisp" or "highKey" or "highContrast" or "softLight" or "sunScorched";
    }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag) && tag != "unknown")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
