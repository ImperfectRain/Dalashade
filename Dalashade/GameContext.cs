using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System;

namespace Dalashade;

public enum WorldCategory
{
    Unknown,
    City,
    Field,
    Duty,
    Interior
}

public enum TimeBucket
{
    Dawn,
    Day,
    Dusk,
    Night
}

public sealed record GameContext(
    uint TerritoryId,
    string TerritoryName,
    WorldCategory WorldCategory,
    uint? WeatherId,
    string WeatherName,
    float EorzeaHour,
    TimeBucket TimeBucket,
    bool InCombat,
    bool InCutscene,
    bool InGpose,
    bool InDuty,
    bool InSanctuary,
    string ContentName,
    string ContentType)
{
    public static GameContext Empty { get; } = new(
        0,
        "Unknown",
        WorldCategory.Unknown,
        null,
        "Unknown",
        0f,
        TimeBucket.Day,
        false,
        false,
        false,
        false,
        false,
        "Unknown",
        "Unknown");

    public string ProfileKey(Configuration configuration, ImageAnalysisResult imageAnalysis, SceneTags tags)
    {
        var combat = configuration.AutoAdjustInCombat && InCombat ? "combat" : "safe";
        var cutscene = configuration.AutoAdjustInCutscenes && InCutscene ? "cutscene" : "gameplay";
        var time = configuration.AutoAdjustAtNight ? TimeBucket.ToString() : "ignored";
        var weather = configuration.AutoAdjustForWeather ? tags.WeatherKey : "ignored";
        var territory = configuration.AutoAdjustForTerritory ? tags.AreaKey : "ignored";
        var image = configuration.AutoAdjustFromScreenshots ? imageAnalysis.MetricsKey : "ignored";

        return $"{TerritoryId}:{territory}:{tags.BiomeKey}:{weather}:{time}:{combat}:{cutscene}:{InGpose}:{image}:{configuration.Style}:{configuration.PerformanceBudget}";
    }
}

public sealed record SceneTags(
    bool IsNight,
    bool IsDawnOrDusk,
    bool IsRain,
    bool IsFog,
    bool IsCloudy,
    bool IsOvercast,
    bool IsGloom,
    bool IsSnow,
    bool IsStorm,
    bool IsDustStorm,
    bool IsHeatWave,
    bool IsClear,
    bool IsCityLike,
    bool IsDungeonLike,
    bool IsRaidLike,
    bool IsFieldLike,
    bool IsInteriorLike,
    bool NeedsCombatClarity,
    bool NeedsDutyReadability,
    bool CinematicAllowed,
    string BiomeKey)
{
    public static SceneTags Empty { get; } = new(false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, true, "unknown");

    public bool NeedsGameplayClarity => NeedsCombatClarity || NeedsDutyReadability;

    public string WeatherKey
    {
        get
        {
            if (IsStorm) return "storm";
            if (IsSnow) return "snow";
            if (IsDustStorm) return "dust";
            if (IsHeatWave) return "heat";
            if (IsRain) return "rain";
            if (IsGloom) return "gloom";
            if (IsFog) return "fog";
            if (IsOvercast) return "overcast";
            if (IsCloudy) return "clouds";
            return "clear";
        }
    }

    public string AreaKey
    {
        get
        {
            if (IsRaidLike) return "raid";
            if (IsDungeonLike) return "dungeon";
            if (IsCityLike) return "city";
            if (IsInteriorLike) return "interior";
            if (IsFieldLike) return "field";
            return "unknown";
        }
    }
}

public static class SceneClassifier
{
    private sealed record BiomeKeywordRule(string Biome, string[] Keywords);

    // Territory/content names come from Lumina/Dalamud when available. These rules
    // use curated XIV names first and generic keyword fallbacks for future zones.
    private static readonly BiomeKeywordRule[] BiomeRules =
    {
        new("highTech", new[] { "solution nine", "heritage found", "alexandria", "neon", "electrope" }),
        new("lunar", new[] { "mare lamentorum", "moon", "lunar" }),
        new("cosmic", new[] { "ultima thule", "cosmic", "star", "space" }),
        new("lightFlooded", new[] { "the empty", "lightwarden", "sin eater", "light flooded", "light-flooded" }),
        new("fae", new[] { "il mheg", "fae", "pixie", "voeburt", "dream" }),
        new("imperial", new[] { "garlemald", "castrum", "imperial", "magitek", "factory", "steel", "ceruleum" }),
        new("volcanic", new[] { "volcano", "lava", "ember", "embers" }),
        new("underwater", new[] { "underwater", "ocean floor", "oceanfloor" }),
        new("ancient", new[] { "amaurot", "allagan", "azys lla", "ruin", "ruins", "ancient" }),
        new("aetherial", new[] { "elpis", "aether", "crystal", "lakeland", "crystarium" }),
        new("snow", new[] { "snow", "ice", "frost", "glacier", "coerthas", "snowcloak" }),
        new("alpine", new[] { "mountain", "alpine", "peak", "summit" }),
        new("jungle", new[] { "jungle", "rainforest", "rak'tika", "rak'tika", "yak t'el", "kozama'uka" }),
        new("forest", new[] { "forest", "shroud", "woods", "wood", "gridania", "sylph" }),
        new("swamp", new[] { "swamp", "marsh", "bog", "fen" }),
        new("steppe", new[] { "azim steppe", "steppe", "grassland", "grasslands" }),
        new("desert", new[] { "desert", "thanalan", "sagolii", "amh araeng", "shaaloani" }),
        new("wasteland", new[] { "badlands", "wasteland", "wastes" }),
        new("cave", new[] { "cave", "cavern", "mine", "tunnel", "subterrane" }),
        new("void", new[] { "void", "darkness", "abyss", "ascian" }),
        new("tropical", new[] { "island", "tropical", "tuliyollal" }),
        new("coastal", new[] { "ruby sea", "ocean", "beach", "sea", "limsa", "mist", "coast" }),
        new("fire", new[] { "fire", "flame", "inferno" })
    };

    public static SceneTags Classify(GameContext context)
    {
        var weather = context.WeatherName.ToLowerInvariant();
        var content = $"{context.ContentName} {context.ContentType}".ToLowerInvariant();
        var territory = context.TerritoryName.ToLowerInvariant();

        var isRain = ContainsAny(weather, "rain", "showers", "drizzle");
        var isFog = ContainsAny(weather, "fog", "mist");
        var isCloudy = ContainsAny(weather, "cloud");
        var isOvercast = ContainsAny(weather, "overcast");
        var isGloom = ContainsAny(weather, "gloom", "umbral", "darkness");
        var isSnow = ContainsAny(weather, "snow", "blizzard");
        var isStorm = ContainsAny(weather, "storm", "thunder", "gales");
        var isDustStorm = ContainsAny(weather, "dust", "sandstorm", "sand storm", "dust storm");
        var isHeatWave = ContainsAny(weather, "heat", "heat wave", "heatwave");

        var isDungeon = context.InDuty && ContainsAny(content, "dungeon", "deep dungeon", "variant", "criterion");
        var isRaid = context.InDuty && ContainsAny(content, "raid", "trial", "ultimate", "savage", "unreal");
        var isCity = context.InSanctuary && !context.InDuty;
        var isInterior = context.WorldCategory == WorldCategory.Interior || isDungeon || isRaid;
        var isField = !context.InDuty && !isCity && !isInterior;
        var needsCombatClarity = context.InCombat;
        var needsDutyReadability = context.InDuty && !context.InCombat;
        var biome = InferBiome(territory, weather, content, isSnow, isFog, isCloudy || isOvercast || isGloom);

        return new SceneTags(
            context.TimeBucket == TimeBucket.Night,
            context.TimeBucket is TimeBucket.Dawn or TimeBucket.Dusk,
            isRain,
            isFog,
            isCloudy,
            isOvercast,
            isGloom,
            isSnow,
            isStorm,
            isDustStorm,
            isHeatWave,
            !isRain && !isFog && !isCloudy && !isOvercast && !isGloom && !isSnow && !isStorm && !isDustStorm && !isHeatWave,
            isCity,
            isDungeon,
            isRaid,
            isField,
            isInterior,
            needsCombatClarity,
            needsDutyReadability,
            !context.InCombat && (!context.InCutscene || context.InGpose),
            biome);
    }

    private static string InferBiome(string territory, string weather, string content, bool isSnow, bool isFog, bool isCloudMood)
    {
        var text = $"{territory} {weather} {content}";
        if (isSnow)
        {
            return "snow";
        }

        foreach (var rule in BiomeRules)
        {
            if (ContainsAny(text, rule.Keywords))
            {
                return rule.Biome;
            }
        }

        return isFog || isCloudMood ? "overcast" : "neutral";
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (value.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class GameContextService
{
    private uint? zoneInitWeatherId;
    private string zoneInitWeatherName = "Unknown";
    private string zoneInitContentName = "Unknown";
    private string zoneInitContentType = "Unknown";

    public GameContext Current { get; private set; } = GameContext.Empty;
    public SceneTags CurrentTags { get; private set; } = SceneTags.Empty;

    public void UpdateZoneInfo(uint? weatherId, string weatherName, string contentName, string contentType)
    {
        zoneInitWeatherId = weatherId;
        zoneInitWeatherName = string.IsNullOrWhiteSpace(weatherName) ? "Unknown" : weatherName;
        zoneInitContentName = string.IsNullOrWhiteSpace(contentName) ? "Unknown" : contentName;
        zoneInitContentType = string.IsNullOrWhiteSpace(contentType) ? "Unknown" : contentType;
    }

    public void Refresh()
    {
        var territoryId = Plugin.ClientState.TerritoryType;
        var territoryName = "Unknown";

        if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territory))
        {
            territoryName = territory.PlaceName.Value.Name.ToString();
        }

        var inCombat = Plugin.Condition[ConditionFlag.InCombat];
        var inCutscene = Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                         || Plugin.Condition[ConditionFlag.WatchingCutscene]
                         || Plugin.Condition[ConditionFlag.WatchingCutscene78];
        var inDuty = Plugin.Condition[ConditionFlag.BoundByDuty]
                     || Plugin.Condition[ConditionFlag.BoundByDuty56]
                     || Plugin.Condition[ConditionFlag.BoundByDuty95]
                     || Plugin.DutyState.IsDutyStarted;
        var inGpose = Plugin.ClientState.IsGPosing;
        var inSanctuary = InferSanctuary(territoryName, inDuty);
        var eorzeaHour = GetEorzeaHour();
        var weather = GetCurrentWeather(territoryId);

        Current = new GameContext(
            territoryId,
            territoryName,
            ClassifyTerritory(territoryName, inDuty, inSanctuary),
            weather.Id,
            weather.Name,
            eorzeaHour,
            GetTimeBucket(eorzeaHour),
            inCombat,
            inCutscene,
            inGpose,
            inDuty,
            inSanctuary,
            GetDutyContentName(zoneInitContentName),
            zoneInitContentType);
        CurrentTags = SceneClassifier.Classify(Current);
    }

    private (uint? Id, string Name) GetCurrentWeather(uint territoryId)
    {
        try
        {
            var weatherId = GetCurrentWeatherId(territoryId);
            if (weatherId > 0 && Plugin.DataManager.GetExcelSheet<Weather>().TryGetRow(weatherId, out var weather))
            {
                var weatherName = weather.Name.ToString();
                if (!string.IsNullOrWhiteSpace(weatherName))
                {
                    return (weatherId, weatherName);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Debug(ex, "Could not refresh current weather.");
        }

        return (zoneInitWeatherId, zoneInitWeatherName);
    }

    private static unsafe uint GetCurrentWeatherId(uint territoryId)
    {
        if (territoryId == 0 || territoryId > ushort.MaxValue)
        {
            return 0;
        }

        var weatherManager = WeatherManager.Instance();
        return weatherManager == null
            ? 0
            : (uint)weatherManager->GetWeatherForHour((ushort)territoryId, 0);
    }

    private static string GetDutyContentName(string fallback)
    {
        try
        {
            var condition = Plugin.DutyState.ContentFinderCondition;
            return condition.IsValid ? condition.Value.Name.ToString() : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static float GetEorzeaHour()
    {
        var eorzeaSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 20.571428571;
        return (float)((eorzeaSeconds / 3600) % 24);
    }

    private static TimeBucket GetTimeBucket(float eorzeaHour)
    {
        return eorzeaHour switch
        {
            >= 5f and < 8f => TimeBucket.Dawn,
            >= 8f and < 16.5f => TimeBucket.Day,
            >= 16.5f and < 19f => TimeBucket.Dusk,
            _ => TimeBucket.Night
        };
    }

    private static bool InferSanctuary(string territoryName, bool inDuty)
    {
        return !inDuty && ContainsAny(territoryName, "Limsa Lominsa", "Gridania", "Ul'dah", "Ishgard", "Kugane", "Crystarium", "Old Sharlayan", "Tuliyollal", "Solution Nine");
    }

    private static WorldCategory ClassifyTerritory(string territoryName, bool inDuty, bool inSanctuary)
    {
        if (inDuty)
        {
            return WorldCategory.Duty;
        }

        if (inSanctuary)
        {
            return WorldCategory.City;
        }

        if (ContainsAny(territoryName, "Inn", "Chambers", "Suite", "Room", "House", "Apartment", "Workshop"))
        {
            return WorldCategory.Interior;
        }

        return WorldCategory.Field;
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (value.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
