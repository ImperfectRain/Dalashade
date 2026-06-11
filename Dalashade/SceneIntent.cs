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
        Add("Cutscene/GPose state", nameof(SceneIntent.CinematicPermission), tags.CinematicAllowed ? 0.55f : 0.10f, tags.CinematicAllowed ? "Cinematic treatment is allowed." : "Cinematic treatment is constrained.");

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
        }
        else if (configuration.AutoAdjustInCombat && tags.NeedsDutyReadability)
        {
            Add("Duty", nameof(SceneIntent.Readability), 0.35f, "Duty content outside combat needs mild readability.");
            Add("Duty", nameof(SceneIntent.CombatPressure), 0.25f, "Duty content adds light gameplay pressure.");
            Add("Duty", nameof(SceneIntent.CinematicPermission), -0.10f, "Duty readability slightly reduces cinematic permission.");
        }

        if (tags.IsDungeonLike || tags.IsRaidLike)
        {
            Add(tags.IsRaidLike ? "Raid area" : "Dungeon area", nameof(SceneIntent.Readability), tags.NeedsCombatClarity ? 0.15f : 0.25f, "Duty-style areas need readable gameplay spaces.");
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
            Add("Gloom weather", nameof(SceneIntent.Haze), 0.35f, "Gloom is a dark haze/mood signal.");
            Add("Gloom weather", nameof(SceneIntent.ShadowProtection), 0.35f, "Gloom risks crushed darks.");
            Add("Gloom weather", nameof(SceneIntent.Atmosphere), 0.25f, "Gloom atmosphere should be preserved.");
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
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.Haze), 0.45f, "Dust and heat add glare/haze.");
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.HighlightProtection), 0.45f, "Dust and heat need glare control.");
        }
    }

    private void AddBiome(SceneTags tags, Configuration configuration)
    {
        if (!configuration.AutoAdjustForTerritory)
        {
            return;
        }

        switch (tags.BiomeKey)
        {
            case "forest":
            case "jungle":
            case "swamp":
                Add("Biome", nameof(SceneIntent.FoliageDensity), 0.70f, "Dense foliage changes depth and sharpness needs.");
                Add("Biome", nameof(SceneIntent.ShadowProtection), 0.25f, "Foliage-heavy scenes risk dense shadows.");
                break;
            case "desert":
            case "wasteland":
                Add("Biome", nameof(SceneIntent.Heat), 0.45f, "Desert and wasteland biomes are hot-scene signals.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.35f, "Dry bright biomes need glare control.");
                break;
            case "snow":
            case "alpine":
                Add("Biome", nameof(SceneIntent.Cold), tags.IsSnow ? 0.18f : 0.45f, tags.IsSnow ? "Snow weather already supplied the main cold signal." : "Snow/alpine biome adds cold pressure.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), tags.IsSnow ? 0.12f : 0.30f, tags.IsSnow ? "Snow biome highlight contribution is dampened." : "Snow/alpine biome needs highlight protection.");
                break;
            case "cave":
            case "void":
                Add("Biome", nameof(SceneIntent.ShadowProtection), 0.55f, "Dark biomes need shadow protection.");
                Add("Biome", nameof(SceneIntent.Atmosphere), 0.20f, "Dark biomes should retain mood.");
                break;
            case "aetherial":
            case "fae":
            case "lightFlooded":
                Add("Biome", nameof(SceneIntent.MagicGlow), 0.55f, "Magical biomes should preserve stylized glow.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.20f, "Magical glow needs highlight control.");
                break;
            case "cosmic":
            case "lunar":
                Add("Biome", nameof(SceneIntent.MagicGlow), 0.35f, "Cosmic/lunar spaces preserve some magical glow.");
                Add("Biome", nameof(SceneIntent.CosmicMood), 0.70f, "Cosmic/lunar spaces get their own mood channel.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.20f, "Cosmic highlights need control.");
                break;
            case "highTech":
                Add("Biome", nameof(SceneIntent.NeonGlow), 0.75f, "High-tech zones should preserve neon accents.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.35f, "Neon and glossy surfaces need highlight protection.");
                break;
            case "imperial":
                Add("Biome", nameof(SceneIntent.IndustrialHardness), 0.65f, "Imperial spaces have hard industrial contrast.");
                Add("Biome", nameof(SceneIntent.Readability), 0.12f, "Industrial spaces benefit from clarity.");
                break;
            case "ancient":
                Add("Biome", nameof(SceneIntent.IndustrialHardness), 0.25f, "Ancient/ruin spaces need structural clarity.");
                Add("Biome", nameof(SceneIntent.Readability), 0.12f, "Ruins benefit from readable structure.");
                break;
            case "coastal":
            case "tropical":
            case "underwater":
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.30f, "Water and bright surfaces need specular control.");
                break;
            case "volcanic":
            case "fire":
                Add("Biome", nameof(SceneIntent.Heat), 0.55f, "Fire and volcanic biomes are hot-scene signals.");
                Add("Biome", nameof(SceneIntent.HighlightProtection), 0.35f, "Hot highlights need control.");
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
        if (budget != PerformanceBudget.Low)
        {
            return;
        }

        Add("Performance budget", nameof(SceneIntent.Readability), 0.12f, "Low budget favors readable inexpensive effects.");
        Add("Performance budget", nameof(SceneIntent.CombatPressure), 0.10f, "Low budget increases safety pressure.");
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
    IReadOnlyList<string> ActiveTags,
    string WeatherKey,
    string BiomeKey,
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
        Array.Empty<string>(),
        "unknown",
        "unknown",
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
            BuildActiveTags(context, tags),
            tags.WeatherKey,
            tags.BiomeKey,
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
}
