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
    bool IsSnow,
    bool IsStorm,
    bool IsClear,
    bool IsCityLike,
    bool IsDungeonLike,
    bool IsRaidLike,
    bool IsFieldLike,
    bool IsInteriorLike,
    bool NeedsGameplayClarity,
    bool CinematicAllowed,
    string BiomeKey)
{
    public static SceneTags Empty { get; } = new(false, false, false, false, false, false, true, false, false, false, false, false, false, true, "unknown");

    public string WeatherKey
    {
        get
        {
            if (IsStorm) return "storm";
            if (IsSnow) return "snow";
            if (IsRain) return "rain";
            if (IsFog) return "fog";
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
    public static SceneTags Classify(GameContext context)
    {
        var weather = context.WeatherName.ToLowerInvariant();
        var content = $"{context.ContentName} {context.ContentType}".ToLowerInvariant();
        var territory = context.TerritoryName.ToLowerInvariant();

        var isRain = ContainsAny(weather, "rain", "showers");
        var isFog = ContainsAny(weather, "fog", "gloom", "cloud", "overcast", "mist");
        var isSnow = ContainsAny(weather, "snow", "blizzard");
        var isStorm = ContainsAny(weather, "storm", "thunder", "gales");

        var isDungeon = context.InDuty && ContainsAny(content, "dungeon", "deep dungeon", "variant", "criterion");
        var isRaid = context.InDuty && ContainsAny(content, "raid", "trial", "ultimate", "savage", "unreal");
        var isCity = context.InSanctuary && !context.InDuty;
        var isInterior = context.WorldCategory == WorldCategory.Interior || isDungeon || isRaid;
        var isField = !context.InDuty && !isCity && !isInterior;
        var needsGameplayClarity = context.InCombat || context.InDuty;
        var biome = InferBiome(territory, weather, content, isSnow, isFog);

        return new SceneTags(
            context.TimeBucket == TimeBucket.Night,
            context.TimeBucket is TimeBucket.Dawn or TimeBucket.Dusk,
            isRain,
            isFog,
            isSnow,
            isStorm,
            !isRain && !isFog && !isSnow && !isStorm,
            isCity,
            isDungeon,
            isRaid,
            isField,
            isInterior,
            needsGameplayClarity,
            !context.InCombat && (!context.InCutscene || context.InGpose),
            biome);
    }

    private static string InferBiome(string territory, string weather, string content, bool isSnow, bool isFog)
    {
        var text = $"{territory} {weather} {content}";
        if (isSnow || ContainsAny(text, "snow", "ice", "frost", "glacier", "coerthas", "garlemald")) return "snow";
        if (ContainsAny(text, "forest", "shroud", "woods", "jungle", "rak'tika", "yak t'el")) return "forest";
        if (ContainsAny(text, "desert", "thanalan", "sagolii", "amh araeng")) return "desert";
        if (ContainsAny(text, "cave", "cavern", "mine", "tunnel", "subterrane")) return "cave";
        if (ContainsAny(text, "void", "darkness", "abyss", "ascian")) return "void";
        if (ContainsAny(text, "aether", "crystal", "ultima thule", "elpis")) return "aetherial";
        if (ContainsAny(text, "ocean", "beach", "sea", "limsa", "mist")) return "coastal";
        if (ContainsAny(text, "fire", "lava", "volcano", "embers")) return "fire";
        return isFog ? "overcast" : "neutral";
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
