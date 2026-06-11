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
    float SpecularRisk,
    float FoliageDensity,
    float NeonGlow,
    float MagicGlow,
    float CinematicPermission,
    float CombatPressure)
{
    public static SceneIntent Neutral { get; } = new(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

    public static SceneIntent From(GameContext context, SceneTags tags, Configuration configuration)
    {
        var readability = 0f;
        var atmosphere = 0.25f;
        var highlightProtection = 0f;
        var shadowProtection = 0f;
        var haze = 0f;
        var wetness = 0f;
        var cold = 0f;
        var heat = 0f;
        var specularRisk = 0f;
        var foliageDensity = 0f;
        var neonGlow = 0f;
        var magicGlow = 0f;
        var cinematicPermission = tags.CinematicAllowed ? 0.55f : 0.10f;
        var combatPressure = 0f;

        if (configuration.AutoAdjustInCombat && tags.NeedsCombatClarity)
        {
            readability += 0.80f;
            combatPressure += 1.00f;
            cinematicPermission -= 0.35f;
        }
        else if (configuration.AutoAdjustInCombat && tags.NeedsDutyReadability)
        {
            readability += 0.35f;
            combatPressure += 0.25f;
            cinematicPermission -= 0.10f;
        }

        if (configuration.AutoAdjustAtNight && tags.IsNight)
        {
            readability += 0.18f;
            shadowProtection += 0.45f;
            atmosphere += 0.20f;
        }

        if (configuration.AutoAdjustForWeather)
        {
            if (tags.IsFog)
            {
                haze += 0.75f;
                highlightProtection += 0.25f;
                atmosphere += 0.25f;
            }

            if (tags.IsCloudy || tags.IsOvercast)
            {
                haze += 0.30f;
                highlightProtection += 0.18f;
            }

            if (tags.IsGloom)
            {
                haze += 0.35f;
                shadowProtection += 0.35f;
                atmosphere += 0.25f;
            }

            if (tags.IsRain)
            {
                wetness += 0.65f;
                specularRisk += 0.45f;
                highlightProtection += 0.25f;
            }

            if (tags.IsStorm)
            {
                wetness += 0.75f;
                specularRisk += 0.65f;
                highlightProtection += 0.45f;
                readability += 0.20f;
                cold += 0.25f;
            }

            if (tags.IsSnow)
            {
                cold += 0.75f;
                specularRisk += 0.60f;
                highlightProtection += 0.65f;
            }

            if (tags.IsDustStorm || tags.IsHeatWave)
            {
                heat += 0.65f;
                haze += 0.45f;
                highlightProtection += 0.45f;
            }
        }

        if (configuration.AutoAdjustForTerritory)
        {
            switch (tags.BiomeKey)
            {
                case "forest":
                case "jungle":
                case "swamp":
                    foliageDensity += 0.70f;
                    shadowProtection += 0.25f;
                    break;
                case "desert":
                case "wasteland":
                    heat += 0.45f;
                    specularRisk += 0.35f;
                    highlightProtection += 0.35f;
                    break;
                case "snow":
                case "alpine":
                    cold += tags.IsSnow ? 0.18f : 0.45f;
                    highlightProtection += tags.IsSnow ? 0.12f : 0.30f;
                    break;
                case "cave":
                case "void":
                    shadowProtection += 0.55f;
                    atmosphere += 0.20f;
                    break;
                case "aetherial":
                case "fae":
                case "lightFlooded":
                case "cosmic":
                case "lunar":
                    magicGlow += 0.55f;
                    highlightProtection += 0.20f;
                    break;
                case "highTech":
                    neonGlow += 0.75f;
                    specularRisk += 0.35f;
                    highlightProtection += 0.25f;
                    break;
                case "imperial":
                case "ancient":
                    readability += 0.12f;
                    break;
                case "coastal":
                case "tropical":
                case "underwater":
                    specularRisk += 0.35f;
                    break;
                case "volcanic":
                case "fire":
                    heat += 0.55f;
                    highlightProtection += 0.35f;
                    break;
            }

            if (tags.IsDungeonLike || tags.IsRaidLike)
            {
                readability += tags.NeedsCombatClarity ? 0.15f : 0.25f;
            }
        }

        return new SceneIntent(
            Clamp01(readability),
            Clamp01(atmosphere),
            Clamp01(highlightProtection),
            Clamp01(shadowProtection),
            Clamp01(haze),
            Clamp01(wetness),
            Clamp01(cold),
            Clamp01(heat),
            Clamp01(specularRisk),
            Clamp01(foliageDensity),
            Clamp01(neonGlow),
            Clamp01(magicGlow),
            Clamp01(cinematicPermission),
            Clamp01(combatPressure));
    }

    private static float Clamp01(float value) => MathF.Min(1f, MathF.Max(0f, value));
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
