using System;
using System.Collections.Generic;
using System.Linq;
using Dalashade.SceneAuthoring;

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
    float Night,
    float Moonlight,
    float ArtificialLight,
    float AmbientDarkness,
    float NightAtmosphere,
    float Daylight,
    float Sunlight,
    float OpenSkyLight,
    float SurfaceHeat,
    float DayAtmosphere,
    float DayReflection,
    float DayHighlightPressure,
    float CombatPressure,
    float CinematicPermission,
    IReadOnlyList<SceneIntentContribution> Contributions)
{
    public static SceneIntent Neutral { get; } = new(
        0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
        0f, 0f, 0f,
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
    private float night;
    private float moonlight;
    private float artificialLight;
    private float ambientDarkness;
    private float nightAtmosphere;
    private float daylight;
    private float sunlight;
    private float openSkyLight;
    private float surfaceHeat;
    private float dayAtmosphere;
    private float dayReflection;
    private float dayHighlightPressure;
    private float combatPressure;
    private float cinematicPermission;

    public SceneIntent Build(GameContext context, SceneTags tags, ImageAnalysisResult imageAnalysis, Configuration configuration, IReadOnlyList<SceneTagPreset>? tagRegistry = null)
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
        AddTagRegistry(tags, tagRegistry);

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
            Clamp01(night),
            Clamp01(moonlight),
            Clamp01(artificialLight),
            Clamp01(ambientDarkness),
            Clamp01(nightAtmosphere),
            Clamp01(daylight),
            Clamp01(sunlight),
            Clamp01(openSkyLight),
            Clamp01(surfaceHeat),
            Clamp01(dayAtmosphere),
            Clamp01(dayReflection),
            Clamp01(dayHighlightPressure),
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
            Add("Night", nameof(SceneIntent.Night), 1.00f, "Night is a global context layer.");
            Add("Night", nameof(SceneIntent.AmbientDarkness), 0.42f, "Night lowers ambient fill and keeps unlit areas darker.");
            Add("Night", nameof(SceneIntent.Readability), 0.08f, "Night gets only mild global readability; separation should come from local light hierarchy.");
            Add("Night", nameof(SceneIntent.ShadowProtection), 0.26f, "Night needs selective dark-detail protection without gray wash.");
            Add("Night", nameof(SceneIntent.Atmosphere), 0.20f, "Night atmosphere should be preserved.");

            if (SupportsOpenSkyNight(tags))
            {
                Add("Moonlit night", nameof(SceneIntent.Moonlight), 0.42f, "Open-sky, lunar, snow, coastal, desert, or cosmic nights allow cool moonlit separation.");
                Add("Moonlit night", nameof(SceneIntent.NightAtmosphere), 0.12f, "Moonlit scenes get subtle atmospheric depth without full-frame haze.");
                Add("Moonlit night", nameof(SceneIntent.HighlightProtection), 0.08f, "Moonlit and snow-lit highlights need restraint.");
            }

            if (SupportsArtificialLight(tags))
            {
                Add("Lamplit night", nameof(SceneIntent.ArtificialLight), 0.42f, "Settlement, coastal, high-tech, industrial, ancient, or magical nights can emphasize localized artificial light.");
                Add("Lamplit night", nameof(SceneIntent.HighlightProtection), 0.10f, "Local lamps, windows, neon, and crystals need highlight control before bloom.");
            }

            if (tags.IsRain || tags.IsFog || tags.IsStorm || tags.IsGloom || tags.IsCloudy || tags.IsOvercast)
            {
                Add("Weather night", nameof(SceneIntent.NightAtmosphere), tags.IsFog ? 0.30f : tags.IsStorm ? 0.26f : 0.16f, "Night weather adds atmosphere while preserving dark baseline.");
            }

            if (tags.BiomeKey is "forest" or "jungle" or "swamp")
            {
                Add("Wild canopy night", nameof(SceneIntent.AmbientDarkness), 0.16f, "Forest and jungle nights should preserve dark trunks and background depth.");
                Add("Wild canopy night", nameof(SceneIntent.FoliageDensity), 0.08f, "Canopy nights increase foliage-aware shader restraint.");
                Add("Wild canopy night", nameof(SceneIntent.ShadowProtection), -0.08f, "Canopy nights avoid maxing shadow lift.");
            }

            if (tags.BiomeKey is "highTech" or "imperial")
            {
                Add("Industrial night", nameof(SceneIntent.ArtificialLight), 0.18f, "Urban, neon, and industrial nights should let constructed light sources read clearly.");
                Add("Industrial night", nameof(SceneIntent.AmbientDarkness), 0.08f, "Hard-surface nights keep darker unlit structure.");
            }

            if (tags.BiomeKey is "snow" or "alpine" or "lunar")
            {
                Add("Cold night", nameof(SceneIntent.Moonlight), 0.18f, "Snow, alpine, and lunar nights support stronger cool moonlit separation.");
                Add("Cold night", nameof(SceneIntent.Cold), 0.08f, "Cold night surfaces need crisp cool identity.");
            }

            if (tags.BiomeKey is "desert" or "wasteland")
            {
                Add("Desert night", nameof(SceneIntent.AmbientDarkness), 0.10f, "Desert nights stay darker than daytime heat scenes.");
                Add("Desert night", nameof(SceneIntent.Moonlight), 0.12f, "Open desert nights can carry cool sky separation while heat remains a material/weather signal.");
            }
        }

        if (configuration.AutoAdjustAtNight && tags.IsDay)
        {
            Add("Day", nameof(SceneIntent.Daylight), 1.00f, "Day is a global context layer, not a brightness lift.");
            Add("Day", nameof(SceneIntent.DayAtmosphere), 0.10f, "Day scenes can preserve clean air and weather identity.");

            if (SupportsSunlitDay(tags))
            {
                Add("Sunlit day", nameof(SceneIntent.Sunlight), 0.42f, "Clear daytime scenes can use direct-sun hierarchy without global exposure lift.");
                Add("Sunlit day", nameof(SceneIntent.DayHighlightPressure), 0.18f, "Bright daylight needs highlight and bloom restraint before adding glow.");
            }

            if (SupportsOpenSkyDay(tags))
            {
                Add("Open-sky day", nameof(SceneIntent.OpenSkyLight), 0.42f, "Open daytime scenes expose sky light for water, snow, sand, and field materials.");
                Add("Open-sky day", nameof(SceneIntent.DayReflection), 0.16f, "Open sky can support reflection and sheen only on valid receivers.");
            }

            if (SupportsCoastalDay(tags))
            {
                Add("Coastal day", nameof(SceneIntent.DayReflection), 0.28f, "Coastal daylight supports water and specular response on validated water surfaces.");
                Add("Coastal day", nameof(SceneIntent.DayHighlightPressure), 0.18f, "Water, sand, and white coastal surfaces need restrained highlights.");
                Add("Coastal day", nameof(SceneIntent.DayAtmosphere), 0.08f, "Coastal daytime air should stay clean and open rather than hazy.");
            }

            if (tags.BiomeKey is "forest" or "jungle" or "swamp")
            {
                Add("Canopy day", nameof(SceneIntent.OpenSkyLight), 0.16f, "Canopy daylight should arrive as sky openings rather than full-frame lift.");
                Add("Canopy day", nameof(SceneIntent.DayAtmosphere), 0.10f, "Forest and jungle daylight keeps humid/canopy air local and controlled.");
                Add("Canopy day", nameof(SceneIntent.FoliageDensity), 0.06f, "Daytime foliage still needs material-aware restraint.");
            }

            if (tags.BiomeKey is "desert" or "wasteland" || tags.MoodTags.Any(tag => tag is "heat" or "dry" or "badlands"))
            {
                Add("Desert day", nameof(SceneIntent.SurfaceHeat), 0.42f, "Sunlit dry terrain supports heat shimmer and dust on distance-weighted materials.");
                Add("Desert day", nameof(SceneIntent.DayHighlightPressure), 0.18f, "Sand and dry sky need highlight protection.");
                Add("Desert day", nameof(SceneIntent.DayAtmosphere), 0.08f, "Dry daytime scenes can carry warm air without fog-like wash.");
            }

            if (tags.BiomeKey is "snow" or "alpine" || tags.MoodTags.Any(tag => tag is "snow" or "ice" or "cold"))
            {
                Add("Snow day", nameof(SceneIntent.OpenSkyLight), 0.22f, "Snow daylight receives broad sky light but must protect whites.");
                Add("Snow day", nameof(SceneIntent.DayHighlightPressure), 0.32f, "Daylit snow and ice need strong white protection.");
                Add("Snow day", nameof(SceneIntent.DayReflection), 0.10f, "Snow and ice can support crisp sheen only where material masks agree.");
            }

            if (tags.BiomeKey is "highTech" or "imperial" || tags.MoodTags.Any(tag => tag is "industrial" or "metallic" or "neon" or "highTech"))
            {
                Add("Industrial day", nameof(SceneIntent.DayReflection), 0.12f, "Constructed daytime surfaces can keep controlled polish without generic gloss.");
                Add("Industrial day", nameof(SceneIntent.DayHighlightPressure), 0.12f, "Glass, metal, and neon daylight need highlight control.");
            }

            if (tags.BiomeKey is "aetherial" or "fae" or "cosmic" or "lunar" || tags.MoodTags.Any(tag => tag is "aetherial" or "magical" or "crystal" or "luminous" or "neon"))
            {
                Add("Aether day", nameof(SceneIntent.DayReflection), 0.10f, "Aetherial daylight preserves luminous material identity without broad bloom.");
                Add("Aether day", nameof(SceneIntent.DayAtmosphere), 0.08f, "Magical daytime spaces can carry subtle atmospheric color.");
            }

            if (tags.IsFog || tags.IsOvercast || tags.IsCloudy)
            {
                Add(tags.IsOvercast ? "Overcast day" : "Misty day", nameof(SceneIntent.DayAtmosphere), tags.IsFog ? 0.24f : 0.16f, "Fog, mist, and overcast daylight shape air without becoming night fog.");
                Add(tags.IsOvercast ? "Overcast day" : "Misty day", nameof(SceneIntent.Sunlight), -0.16f, "Cloud cover reduces direct sun pressure while preserving day context.");
            }

            if (tags.IsRain || tags.IsStorm)
            {
                Add("Storm day", nameof(SceneIntent.DayAtmosphere), 0.18f, "Rain and storm daylight add wet diffusion while keeping readability.");
                Add("Storm day", nameof(SceneIntent.DayReflection), 0.18f, "Wet daytime surfaces can receive reflection only through wet/material masks.");
            }

            if (tags.IsInteriorLike || tags.IsDungeonLike || tags.IsRaidLike)
            {
                Add(tags.IsDungeonLike || tags.IsRaidLike ? "Dungeon day" : "Interior day", nameof(SceneIntent.OpenSkyLight), -0.30f, "Interior and duty daytime should not over-assert exterior sky light.");
                Add(tags.IsDungeonLike || tags.IsRaidLike ? "Dungeon day" : "Interior day", nameof(SceneIntent.Sunlight), -0.22f, "Interior and dungeon day context avoids generic sunlit grading.");
            }
        }

        if (configuration.AutoAdjustAtNight && tags.IsDawnOrDusk)
        {
            Add("Dawn/dusk", nameof(SceneIntent.Atmosphere), 0.12f, "Low sun supports mild atmosphere.");
            Add("Dawn/dusk", nameof(SceneIntent.DayAtmosphere), 0.10f, "Transition light keeps a low-air identity without pretending to be full daylight.");
            Add("Dawn/dusk", nameof(SceneIntent.OpenSkyLight), 0.14f, "Dawn and dusk need sky-color context for weather and grade lanes.");
            Add("Dawn/dusk", nameof(SceneIntent.HighlightProtection), 0.06f, "Low sun highlights get light restraint without flattening the scene.");
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
            Add("Fog weather", tags.IsNight ? nameof(SceneIntent.NightAtmosphere) : nameof(SceneIntent.DayAtmosphere), 0.18f, "Fog feeds the active time-of-day air lane.");
        }

        if (tags.IsCloudy || tags.IsOvercast)
        {
            Add(tags.IsOvercast ? "Overcast weather" : "Cloud weather", nameof(SceneIntent.Haze), tags.IsOvercast ? 0.38f : 0.30f, "Clouds are a mild haze signal and overcast is a stronger air signal.");
            Add(tags.IsOvercast ? "Overcast weather" : "Cloud weather", nameof(SceneIntent.Atmosphere), tags.IsOvercast ? 0.16f : 0.10f, "Cloud and overcast tags feed a broad air lane without becoming fog.");
            Add(tags.IsOvercast ? "Overcast weather" : "Cloud weather", tags.IsNight ? nameof(SceneIntent.NightAtmosphere) : nameof(SceneIntent.DayAtmosphere), tags.IsOvercast ? 0.14f : 0.10f, "Cloud cover gets routed through the active time-of-day atmosphere lane.");
            Add(tags.IsOvercast ? "Overcast weather" : "Cloud weather", nameof(SceneIntent.HighlightProtection), tags.IsOvercast ? 0.22f : 0.18f, "Clouds can flatten highlights.");
            if (tags.IsDay)
            {
                Add(tags.IsOvercast ? "Overcast weather" : "Cloud weather", nameof(SceneIntent.Sunlight), tags.IsOvercast ? -0.12f : -0.06f, "Cloud cover softens direct sun pressure.");
            }
        }

        if (tags.IsGloom)
        {
            Add("Gloom weather", nameof(SceneIntent.Haze), 0.16f, "Gloom is a dark mood signal, not full fog.");
            Add("Gloom weather", nameof(SceneIntent.ShadowProtection), 0.24f, "Gloom needs some dark-detail protection without flattening blacks.");
            Add("Gloom weather", nameof(SceneIntent.Atmosphere), 0.22f, "Gloom atmosphere should be preserved.");
            Add("Gloom weather", nameof(SceneIntent.AmbientDarkness), 0.16f, "Gloom deepens ambient darkness instead of turning into gray fog.");
            Add("Gloom weather", nameof(SceneIntent.NightAtmosphere), 0.10f, "Gloom can use the darker air path even outside strict night.");
        }

        if (tags.IsRain)
        {
            Add("Rain weather", nameof(SceneIntent.Wetness), 0.65f, "Rain creates wet-surface behavior.");
            Add("Rain weather", nameof(SceneIntent.HighlightProtection), 0.35f, "Wet specular highlights need protection.");
            Add("Rain weather", nameof(SceneIntent.Haze), 0.24f, "Rain adds a light damp air signal.");
            Add("Rain weather", nameof(SceneIntent.Atmosphere), 0.14f, "Rain should be visible as air, not only as wetness.");
            Add("Rain weather", tags.IsNight ? nameof(SceneIntent.NightAtmosphere) : nameof(SceneIntent.DayAtmosphere), 0.12f, "Rain feeds the active time-of-day atmosphere lane.");
        }

        if (tags.IsStorm)
        {
            Add("Storm weather", nameof(SceneIntent.Wetness), 0.75f, "Storms are strong wet-scene signals.");
            Add("Storm weather", nameof(SceneIntent.HighlightProtection), 0.55f, "Storm highlights and lightning need protection.");
            Add("Storm weather", nameof(SceneIntent.Readability), 0.20f, "Storms need extra readability.");
            Add("Storm weather", nameof(SceneIntent.Cold), 0.25f, "Storms bias cooler.");
            Add("Storm weather", nameof(SceneIntent.Haze), 0.34f, "Storms add visible weather mass without forcing full fog.");
            Add("Storm weather", nameof(SceneIntent.Atmosphere), 0.20f, "Storm atmosphere should be available to first-party air shaders.");
            Add("Storm weather", nameof(SceneIntent.AmbientDarkness), 0.10f, "Storms darken broad ambient fill while readability gates remain active.");
            Add("Storm weather", tags.IsNight ? nameof(SceneIntent.NightAtmosphere) : nameof(SceneIntent.DayAtmosphere), 0.16f, "Storms feed the active time-of-day atmosphere lane.");
        }

        if (tags.IsSnow)
        {
            Add("Snow weather", nameof(SceneIntent.Cold), 0.75f, "Snow is the primary cold signal.");
            Add("Snow weather", nameof(SceneIntent.HighlightProtection), 0.65f, "Snow risks blown white detail.");
            Add("Snow weather", nameof(SceneIntent.Haze), 0.20f, "Snow adds cold air thickness while preserving white detail.");
            Add("Snow weather", nameof(SceneIntent.Atmosphere), 0.14f, "Snow should be visible as atmospheric cold, not only color temperature.");
            Add("Snow weather", tags.IsNight ? nameof(SceneIntent.NightAtmosphere) : nameof(SceneIntent.DayAtmosphere), 0.12f, "Snow feeds the active time-of-day atmosphere lane.");
        }

        if (tags.IsDustStorm || tags.IsHeatWave)
        {
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.Heat), 0.65f, "Dust and heat are hot-scene signals.");
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.Haze), tags.IsNight && tags.IsHeatWave && !tags.IsDustStorm ? 0.34f : 0.45f, "Dust and heat add distance-biased glare/haze.");
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.HighlightProtection), tags.IsNight && tags.IsHeatWave && !tags.IsDustStorm ? 0.32f : 0.45f, "Night heat needs less highlight restraint than daytime glare.");
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", nameof(SceneIntent.Atmosphere), tags.IsDustStorm ? 0.18f : 0.10f, "Dust and heat route through air-shaping lanes instead of only color grading.");
            Add(tags.IsDustStorm ? "Dust weather" : "Heat weather", tags.IsNight ? nameof(SceneIntent.NightAtmosphere) : nameof(SceneIntent.DayAtmosphere), tags.IsDustStorm ? 0.12f : 0.08f, "Dust and heat feed the active time-of-day atmosphere lane.");
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
                Add("Biome", nameof(SceneIntent.AmbientDarkness), (tags.BiomeKey == "void" ? 0.30f : 0.18f) * styleScale, "Caves and void spaces preserve black-depth mood.");
                Add("Biome", nameof(SceneIntent.NightAtmosphere), 0.14f * styleScale, "Dark enclosed spaces use the night-air lane for depth even outside clock night.");
                if (tags.BiomeKey == "void")
                {
                    Add("Biome", nameof(SceneIntent.CosmicMood), 0.26f * styleScale, "Void spaces can use the cosmic/umbral atmosphere family.");
                }
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
                Add("Biome", nameof(SceneIntent.IndustrialHardness), 0.30f * styleScale, "High-tech spaces share the constructed hard-surface atmosphere family.");
                Add("Neon mood", nameof(SceneIntent.Atmosphere), 0.06f * styleScale, "High-tech neon spaces keep a controlled ambient glow.");
                break;
            case "imperial":
                Add("Biome", nameof(SceneIntent.IndustrialHardness), 0.68f * styleScale, "Imperial spaces have hard industrial contrast.");
                Add("Biome", nameof(SceneIntent.Readability), 0.12f * styleScale, "Industrial spaces benefit from clarity.");
                Add("Biome", nameof(SceneIntent.ShadowProtection), 0.10f * styleScale, "Industrial spaces preserve hard-surface black depth.");
                Add("Industrial mood", nameof(SceneIntent.Atmosphere), -0.04f * styleScale, "Industrial spaces favor structure over haze.");
                break;
            case "ancient":
                Add("Biome", nameof(SceneIntent.IndustrialHardness), 0.28f * styleScale, "Ancient/ruin spaces need structural clarity.");
                Add("Biome", nameof(SceneIntent.Readability), 0.12f * styleScale, "Ruins benefit from readable structure.");
                Add("Biome", nameof(SceneIntent.Atmosphere), 0.04f * styleScale, "Ruins can keep light age and depth atmosphere.");
                Add("Biome", nameof(SceneIntent.ShadowProtection), 0.12f * styleScale, "Ruins preserve carved surface detail without global lift.");
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
                Add("Biome", nameof(SceneIntent.Atmosphere), 0.12f * styleScale, "Hot biomes can carry heat air without broad haze.");
                break;
        }
    }

    private void AddScreenshotAnalysis(ImageAnalysisResult imageAnalysis, Configuration configuration)
    {
        if (!configuration.AutoAdjustFromScreenshots || !imageAnalysis.Available)
        {
            return;
        }

        var strength = Clamp(configuration.ScreenshotAnalysisStrength, 0f, 2f);
        if (strength <= 0f)
        {
            return;
        }

        if (imageAnalysis.HighlightClipping > 0.04f || imageAnalysis.AverageLuminance > 0.72f)
        {
            Add("Screenshot analysis", nameof(SceneIntent.HighlightProtection), Scale01(imageAnalysis.HighlightClipping, 0.12f) * 0.35f * strength, "Current image analysis found bright or clipped highlights.");
        }

        if (imageAnalysis.ShadowClipping > 0.08f || imageAnalysis.AverageLuminance < 0.30f)
        {
            Add("Screenshot analysis", nameof(SceneIntent.ShadowProtection), Scale01(imageAnalysis.ShadowClipping, 0.30f) * 0.35f * strength, "Current image analysis found dark or clipped shadows.");
            Add("Screenshot analysis", nameof(SceneIntent.Readability), Scale01(0.30f - imageAnalysis.AverageLuminance, 0.30f) * 0.20f * strength, "Current image analysis found low luminance.");
        }

        if (imageAnalysis.Contrast < 0.14f)
        {
            Add("Screenshot analysis", nameof(SceneIntent.Readability), 0.12f * strength, "Current image analysis found low contrast.");
        }

        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.SkyAir, nameof(SceneIntent.OpenSkyLight), 0.20f, strength, "Screenshot opinion found likely visible sky or air.");
        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.SkyAir, nameof(SceneIntent.DayAtmosphere), 0.12f, strength, "Screenshot opinion found likely broad sky/air gradients.");
        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.WaterSurface, nameof(SceneIntent.DayReflection), 0.18f, strength, "Screenshot opinion found likely water surface color.");
        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.WaterSurface, nameof(SceneIntent.Wetness), 0.12f, strength, "Screenshot opinion found likely water surface color.");
        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.Foliage, nameof(SceneIntent.FoliageDensity), 0.20f, strength, "Screenshot opinion found likely foliage.");
        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.SandDust, nameof(SceneIntent.SurfaceHeat), 0.14f, strength, "Screenshot opinion found likely sand or warm ground.");
        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.SnowIce, nameof(SceneIntent.Cold), 0.16f, strength, "Screenshot opinion found likely snow or ice.");
        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.NeonAether, nameof(SceneIntent.MagicGlow), 0.12f, strength, "Screenshot opinion found likely neon or aether color.");
        AddScreenshotOpinion(imageAnalysis, ImageSceneOpinionKeys.NeonAether, nameof(SceneIntent.NeonGlow), 0.10f, strength, "Screenshot opinion found likely neon or aether color.");
    }

    private void AddScreenshotOpinion(ImageAnalysisResult imageAnalysis, string opinionKey, string intent, float amount, float strength, string reason)
    {
        var confidence = imageAnalysis.OpinionConfidence(opinionKey);
        if (confidence < 0.18f)
        {
            return;
        }

        Add("Screenshot opinion", intent, confidence * amount * strength, reason);
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

    private void AddTagRegistry(SceneTags tags, IReadOnlyList<SceneTagPreset>? tagRegistry)
    {
        if (tagRegistry is null || tagRegistry.Count == 0)
        {
            return;
        }

        var active = BuildActiveRegistryTags(tags);
        foreach (var preset in tagRegistry)
        {
            if (!PresetApplies(preset, active))
            {
                continue;
            }

            foreach (var tuning in preset.Tunings.Where(tuning =>
                         tuning.Enabled
                         && string.Equals(tuning.Target, SceneTagTuningTargets.SceneIntent, StringComparison.OrdinalIgnoreCase)
                         && SceneAuthoringService.SceneIntentTuningChannels.Contains(tuning.Channel, StringComparer.OrdinalIgnoreCase)))
            {
                Add(
                    $"Tag registry: {preset.Tag}",
                    tuning.Channel,
                    tuning.Amount,
                    string.IsNullOrWhiteSpace(tuning.Reason) ? "User-editable tag registry tuning." : tuning.Reason);
            }
        }
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
            case nameof(SceneIntent.Night):
                night += amount;
                break;
            case nameof(SceneIntent.Moonlight):
                moonlight += amount;
                break;
            case nameof(SceneIntent.ArtificialLight):
                artificialLight += amount;
                break;
            case nameof(SceneIntent.AmbientDarkness):
                ambientDarkness += amount;
                break;
            case nameof(SceneIntent.NightAtmosphere):
                nightAtmosphere += amount;
                break;
            case nameof(SceneIntent.Daylight):
                daylight += amount;
                break;
            case nameof(SceneIntent.Sunlight):
                sunlight += amount;
                break;
            case nameof(SceneIntent.OpenSkyLight):
                openSkyLight += amount;
                break;
            case nameof(SceneIntent.SurfaceHeat):
                surfaceHeat += amount;
                break;
            case nameof(SceneIntent.DayAtmosphere):
                dayAtmosphere += amount;
                break;
            case nameof(SceneIntent.DayReflection):
                dayReflection += amount;
                break;
            case nameof(SceneIntent.DayHighlightPressure):
                dayHighlightPressure += amount;
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

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));

    private static float Scale01(float value, float range) => Clamp01(value / range);

    private static float BiomeStyleScale(SceneTags tags) => 0.55f + (Clamp01(tags.BiomeConfidence) * 0.45f);

    private static bool SupportsSunlitDay(SceneTags tags)
    {
        return tags.IsClear
               || tags.MoodTags.Any(tag => tag is "sunlit" or "clean" or "colorful" or "warm")
               || tags.BiomeKey is "coastal" or "tropical" or "desert" or "wasteland";
    }

    private static bool SupportsOpenSkyDay(SceneTags tags)
    {
        return tags.BiomeKey is "coastal" or "tropical" or "desert" or "wasteland" or "snow" or "alpine" or "lunar" or "cosmic" or "steppe"
               || (tags.IsFieldLike && tags.BiomeKey is not "forest" and not "jungle" and not "swamp" and not "cave")
               || tags.MoodTags.Any(tag => tag is "coastal" or "seaside" or "water" or "moonlit" or "cosmic");
    }

    private static bool SupportsCoastalDay(SceneTags tags)
    {
        return tags.BiomeKey is "coastal" or "tropical"
               || tags.MoodTags.Any(tag => tag is "coastal" or "seaside" or "water" or "specular" or "beach");
    }

    private static bool SupportsOpenSkyNight(SceneTags tags)
    {
        return tags.BiomeKey is "coastal" or "tropical" or "desert" or "wasteland" or "snow" or "alpine" or "lunar" or "cosmic" or "steppe"
               || (tags.IsFieldLike && tags.BiomeKey is not "forest" and not "jungle" and not "swamp" and not "cave")
               || tags.MoodTags.Contains("moonlit", StringComparer.OrdinalIgnoreCase)
               || tags.MoodTags.Contains("stars", StringComparer.OrdinalIgnoreCase);
    }

    private static bool SupportsArtificialLight(SceneTags tags)
    {
        return tags.IsCityLike
               || tags.BiomeKey is "coastal" or "tropical" or "highTech" or "imperial" or "ancient" or "fae" or "aetherial" or "cosmic" or "lunar" or "volcanic" or "fire"
               || tags.MoodTags.Any(tag => tag is "neon" or "urban" or "luminous" or "magical" or "aetherial" or "fire" or "warm");
    }

    private static bool PresetApplies(SceneTagPreset preset, IReadOnlyDictionary<string, HashSet<string>> active)
    {
        return preset.Categories.Any(category =>
            active.TryGetValue(category, out var tags)
            && tags.Contains(preset.Tag));
    }

    private static IReadOnlyDictionary<string, HashSet<string>> BuildActiveRegistryTags(SceneTags tags)
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

        Add(SceneAuthoringService.AreaCategory, tags.AreaKey);
        if (tags.IsCityLike) Add(SceneAuthoringService.AreaCategory, "city");
        if (tags.IsFieldLike) Add(SceneAuthoringService.AreaCategory, "field");
        if (tags.IsInteriorLike) Add(SceneAuthoringService.AreaCategory, "interior");
        if (tags.IsDungeonLike) Add(SceneAuthoringService.AreaCategory, "dungeon");
        if (tags.IsRaidLike) Add(SceneAuthoringService.AreaCategory, "raid");

        Add(SceneAuthoringService.BiomeCategory, tags.BiomeKey);

        Add(SceneAuthoringService.WeatherCategory, tags.WeatherKey);
        if (tags.IsRain) Add(SceneAuthoringService.WeatherCategory, "rain");
        if (tags.IsFog) Add(SceneAuthoringService.WeatherCategory, "fog");
        if (tags.IsCloudy) Add(SceneAuthoringService.WeatherCategory, "clouds");
        if (tags.IsOvercast) Add(SceneAuthoringService.WeatherCategory, "overcast");
        if (tags.IsGloom) Add(SceneAuthoringService.WeatherCategory, "gloom");
        if (tags.IsSnow) Add(SceneAuthoringService.WeatherCategory, "snow");
        if (tags.IsStorm) Add(SceneAuthoringService.WeatherCategory, "storm");
        if (tags.IsDustStorm) Add(SceneAuthoringService.WeatherCategory, "dust");
        if (tags.IsHeatWave) Add(SceneAuthoringService.WeatherCategory, "heat");
        if (tags.IsClear) Add(SceneAuthoringService.WeatherCategory, "clear");

        if (tags.IsNight) Add(SceneAuthoringService.TimeCategory, "night");
        if (tags.IsDay) Add(SceneAuthoringService.TimeCategory, "day");
        if (tags.IsDawnOrDusk) Add(SceneAuthoringService.TimeCategory, "dawnDusk");

        foreach (var mood in tags.MoodTags)
        {
            Add(SceneAuthoringService.MoodCategory, mood);
            Add(SceneAuthoringService.SecondaryCategory, mood);
            Add(SceneAuthoringService.MaterialCategory, mood);
            Add(SceneAuthoringService.ArtDirectionCategory, mood);
        }

        return active;
    }
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
            FilterSuppressedTags(tags, "active", BuildActiveTags(context, tags)),
            FilterSuppressedTags(tags, "weather", BuildActiveWeatherTags(tags)),
            FilterSuppressedTags(tags, "secondary", BuildSecondaryTags(tags)),
            FilterSuppressedTags(tags, "mood", tags.MoodTags),
            FilterSuppressedTags(tags, "material", BuildMaterialTags(tags)),
            FilterSuppressedTags(tags, "area", BuildAreaContextTags(tags)),
            BuildGameplayStateTags(context, tags),
            FilterSuppressedTags(tags, "artDirection", BuildArtDirectionTags(tags)),
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

    private static IReadOnlyList<string> FilterSuppressedTags(SceneTags tags, string category, IReadOnlyList<string> values)
    {
        if (!tags.SuppressedAuthoringTags.TryGetValue(category, out var suppressed) || suppressed.Count == 0)
        {
            return values;
        }

        return values
            .Where(value => !suppressed.Contains(value, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    private static IReadOnlyList<string> BuildActiveTags(GameContext context, SceneTags tags)
    {
        var result = new List<string>();
        if (tags.IsNight) result.Add("Night");
        if (tags.IsDay) result.Add("Day");
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
        result.AddRange(BuildNightTags(tags));
        result.AddRange(BuildDayTags(tags));
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
        result.AddRange(BuildNightTags(tags));
        result.AddRange(BuildDayTags(tags));
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
        result.AddRange(BuildNightTags(tags));
        if (tags.IsDay) result.Add("day");
        result.AddRange(BuildDayTags(tags));
        if (tags.IsDawnOrDusk) result.Add("goldenHour");
        if (tags.IsGloom) result.Add("haunted");
        return NormalizeTags(result);
    }

    private static IReadOnlyList<string> BuildDayTags(SceneTags tags)
    {
        if (!tags.IsDay)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        var openSky = tags.BiomeKey is "coastal" or "tropical" or "desert" or "wasteland" or "snow" or "alpine" or "lunar" or "cosmic" or "steppe"
                      || (tags.IsFieldLike && tags.BiomeKey is not "forest" and not "jungle" and not "swamp" and not "cave");
        var canopy = tags.BiomeKey is "forest" or "jungle" or "swamp" || tags.MoodTags.Contains("canopyLight", StringComparer.OrdinalIgnoreCase);
        var settlement = tags.IsCityLike || tags.BiomeKey is "highTech" or "imperial" || tags.MoodTags.Contains("urban", StringComparer.OrdinalIgnoreCase);
        var sunlit = tags.IsClear || tags.MoodTags.Any(tag => tag is "sunlit" or "clean" or "colorful" or "warm");

        if (sunlit)
        {
            result.Add("sunlitDay");
        }

        if (openSky)
        {
            result.Add("openSkyDay");
        }

        if (tags.BiomeKey is "coastal" or "tropical" || tags.MoodTags.Any(tag => tag is "coastal" or "seaside" or "water" or "specular" or "beach"))
        {
            result.Add("coastalDay");
        }

        if (canopy)
        {
            result.Add("canopyDay");
        }

        if (settlement)
        {
            result.Add("settlementDay");
        }

        if (tags.BiomeKey is "highTech" or "imperial" || tags.MoodTags.Any(tag => tag is "industrial" or "metallic" or "neon" or "highTech"))
        {
            result.Add("industrialDay");
        }

        if (tags.BiomeKey is "snow" or "alpine" || tags.MoodTags.Any(tag => tag is "snow" or "alpine" or "ice" or "cold"))
        {
            result.Add("snowDay");
            result.Add("coldDay");
        }

        if (tags.BiomeKey is "desert" or "wasteland" || tags.MoodTags.Any(tag => tag is "desert" or "badlands" or "dry" or "heat"))
        {
            result.Add("desertDay");
            result.Add("heatDay");
        }

        if (tags.IsFog || tags.MoodTags.Contains("mist", StringComparer.OrdinalIgnoreCase))
        {
            result.Add("mistyDay");
        }

        if (tags.IsOvercast || tags.IsCloudy)
        {
            result.Add("overcastDay");
        }

        if (tags.IsStorm || tags.IsRain)
        {
            result.Add("stormDay");
        }

        if (tags.BiomeKey is "aetherial" or "fae" or "cosmic" or "lunar" || tags.MoodTags.Any(tag => tag is "aetherial" or "magical" or "crystal" or "luminous"))
        {
            result.Add("aetherDay");
        }

        if (tags.BiomeKey is "highTech" || tags.MoodTags.Any(tag => tag is "neon" or "highTech"))
        {
            result.Add("highTechDay");
        }

        if (tags.IsInteriorLike)
        {
            result.Add("interiorDay");
        }

        if (tags.IsDungeonLike || tags.IsRaidLike)
        {
            result.Add("dungeonDay");
        }

        if (sunlit && (tags.BiomeKey is "coastal" or "tropical" or "desert" or "wasteland" || tags.MoodTags.Any(tag => tag is "sunlit" or "warm")))
        {
            result.Add("goldenDay");
        }

        return NormalizeTags(result);
    }

    private static IReadOnlyList<string> BuildNightTags(SceneTags tags)
    {
        if (!tags.IsNight)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        var openSky = tags.BiomeKey is "coastal" or "tropical" or "desert" or "wasteland" or "snow" or "alpine" or "lunar" or "cosmic" or "steppe"
                      || (tags.IsFieldLike && tags.BiomeKey is not "forest" and not "jungle" and not "swamp" and not "cave");
        var canopy = tags.BiomeKey is "forest" or "jungle" or "swamp" || tags.MoodTags.Contains("canopyLight", StringComparer.OrdinalIgnoreCase);
        var settlement = tags.IsCityLike || tags.BiomeKey is "highTech" or "imperial" || tags.MoodTags.Contains("urban", StringComparer.OrdinalIgnoreCase);
        var lamplit = settlement
                      || tags.BiomeKey is "coastal" or "tropical" or "ancient" or "fae" or "aetherial" or "cosmic" or "lunar" or "volcanic" or "fire"
                      || tags.MoodTags.Any(tag => tag is "neon" or "luminous" or "magical");

        if (openSky)
        {
            result.Add("openSkyNight");
        }
        else if (!settlement && !tags.IsInteriorLike && !tags.IsDungeonLike && !tags.IsRaidLike)
        {
            result.Add("wildNight");
        }
        if (openSky || tags.MoodTags.Contains("moonlit", StringComparer.OrdinalIgnoreCase) || tags.BiomeKey is "lunar" or "cosmic" or "snow" or "alpine")
        {
            result.Add("moonlitNight");
        }

        if (lamplit)
        {
            result.Add("lamplitNight");
        }

        if (settlement)
        {
            result.Add("settlementNight");
        }

        if (canopy)
        {
            result.Add("canopyNight");
        }

        if (tags.IsFog || tags.IsOvercast || tags.MoodTags.Contains("mist", StringComparer.OrdinalIgnoreCase))
        {
            result.Add("mistyNight");
        }

        if (tags.IsStorm || tags.IsRain)
        {
            result.Add("stormNight");
        }

        if (tags.BiomeKey is "coastal" or "tropical" || tags.MoodTags.Any(tag => tag is "coastal" or "seaside" or "water" or "specular"))
        {
            result.Add("coastalNight");
        }

        if (tags.BiomeKey is "highTech" or "imperial" || tags.MoodTags.Any(tag => tag is "industrial" or "metallic" or "neon" or "highTech"))
        {
            result.Add("industrialNight");
        }

        if (tags.BiomeKey is "snow" or "alpine" || tags.MoodTags.Any(tag => tag is "snow" or "alpine" or "ice" or "cold"))
        {
            result.Add("snowNight");
            result.Add("coldNight");
        }

        if (tags.BiomeKey is "desert" or "wasteland" || tags.MoodTags.Any(tag => tag is "desert" or "badlands" or "dry" or "heat"))
        {
            result.Add("desertNight");
        }

        if (tags.BiomeKey is "aetherial" or "fae" or "cosmic" or "lunar" or "highTech" || tags.MoodTags.Any(tag => tag is "aetherial" or "magical" or "crystal" or "luminous" or "neon"))
        {
            result.Add("aetherNight");
        }

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
        return tag is "coastal" or "tropical" or "seaside" or "beach" or "rainforest" or "lush" or "verdant" or "desert" or "badlands" or "alpine" or "lunar" or "cosmic" or "alien" or "aetherial" or "fae" or "ancient" or "ruins" or "structured" or "highTech" or "neon" or "urban" or "imperial" or "industrial" or "moonlit";
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
