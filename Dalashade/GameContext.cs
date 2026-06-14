using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

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
    string BiomeKey,
    float BiomeConfidence,
    string BiomeReason,
    IReadOnlyList<string> MoodTags)
{
    public static SceneTags Empty { get; } = new(false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, true, "unknown", 0f, "No scene tags have been classified yet.", Array.Empty<string>());

    public bool IsDay => !IsNight && !IsDawnOrDusk;

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
    private sealed record BiomeKeywordRule(string Biome, float Confidence, string Reason, string[] MoodTags, string[] Keywords);

    private sealed record BiomeMatch(string Biome, float Confidence, string Reason, IReadOnlyList<string> MoodTags);

    // Territory/content names come from Lumina/Dalamud when available. These rules
    // use curated XIV names first and generic keyword fallbacks for future zones.
    private static readonly BiomeKeywordRule[] BiomeRules =
    {
        new("highTech", 0.98f, "Matched Dawntrail high-tech/neon territory keywords.", new[] { "neon", "highTech", "electrope", "urban", "clean", "luminous" }, new[] { "solution nine", "heritage found", "alexandria", "neon", "electrope", "living memory" }),
        new("cosmic", 0.98f, "Matched cosmic/space territory keywords.", new[] { "cosmic", "alien", "aetherial", "stars", "cool", "highDepth" }, new[] { "ultima thule", "cosmic", "star", "stars", "space", "omphalos" }),
        new("lunar", 0.98f, "Matched lunar territory keywords.", new[] { "lunar", "moonlit", "cold", "cosmic", "cool", "highDepth" }, new[] { "mare lamentorum", "moon", "lunar", "bestways burrow" }),
        new("fae", 0.98f, "Matched fae/dreamlike territory keywords.", new[] { "fae", "dreamlike", "colorful", "magical", "pastel" }, new[] { "il mheg", "voeburt", "fae", "pixie", "dream" }),
        new("imperial", 0.96f, "Matched Garlean, Castrum, magitek, or factory keywords.", new[] { "imperial", "industrial", "metallic", "smoky", "structured", "cold" }, new[] { "garlemald", "castrum", "imperial", "magitek", "factory", "steel", "ceruleum", "magna glacies", "tower of babil" }),
        new("jungle", 0.96f, "Matched jungle/rainforest territory keywords.", new[] { "rainforest", "foliage", "lush", "verdant", "humid", "canopyLight" }, new[] { "rak'tika", "raktika", "greatwood", "yak t'el", "yak tel", "kozama'uka", "kozamauka", "jungle", "rainforest" }),
        new("snow", 0.95f, "Matched snow, ice, Coerthas, or Snowcloak keywords.", new[] { "snow", "alpine", "cold", "ice", "clean", "crisp" }, new[] { "coerthas", "snowcloak", "snow", "ice", "frost", "glacier", "western highlands" }),
        new("desert", 0.95f, "Matched desert and dry-region territory keywords.", new[] { "desert", "badlands", "dry", "heat", "dust", "sunScorched" }, new[] { "thanalan", "sagolii", "amh araeng", "shaaloani", "desert", "badlands" }),
        new("coastal", 0.95f, "Matched sea, coast, beach, La Noscea, Limsa, Mist, or Ruby Sea keywords.", new[] { "coastal", "tropical", "seaside", "beach", "water", "specular", "clean", "sunlit", "colorful", "foliage" }, new[] { "ruby sea", "la noscea", "eastern la noscea", "western la noscea", "lower la noscea", "middle la noscea", "outer la noscea", "upper la noscea", "costa del sol", "bloodshore", "raincatcher", "wineport", "summerford", "isles of umbra", "limsa", "mist", "ocean", "beach", "sea", "coast", "coastal", "isle" }),
        new("lightFlooded", 0.94f, "Matched light-flooded First keywords.", new[] { "highKey", "magic", "aetherial", "clean" }, new[] { "the empty", "lightwarden", "sin eater", "light flooded", "light-flooded" }),
        new("volcanic", 0.92f, "Matched volcanic/lava territory keywords.", new[] { "heat", "fire", "smoky", "highContrast" }, new[] { "volcano", "lava", "ember", "embers" }),
        new("underwater", 0.92f, "Matched underwater or ocean-floor keywords.", new[] { "water", "haze", "cool", "depth" }, new[] { "underwater", "ocean floor", "oceanfloor" }),
        new("ancient", 0.90f, "Matched ancient, ruin, Amaurot, Allagan, or Azys Lla keywords.", new[] { "ancient", "ruins", "structured", "stone", "aetherial" }, new[] { "amaurot", "allagan", "azys lla", "ruin", "ruins", "ancient" }),
        new("aetherial", 0.88f, "Matched aetherial, crystal, Elpis, Lakeland, or Crystarium keywords.", new[] { "aetherial", "magic", "crystal", "clean", "luminous" }, new[] { "elpis", "aether", "crystal", "lakeland", "crystarium" }),
        new("alpine", 0.84f, "Matched mountain/alpine keywords.", new[] { "alpine", "cold", "highAltitude", "crisp" }, new[] { "mountain", "alpine", "peak", "summit" }),
        new("forest", 0.82f, "Matched forest, Shroud, Gridania, or woods keywords.", new[] { "foliage", "lush", "verdant" }, new[] { "forest", "shroud", "woods", "wood", "gridania", "sylph" }),
        new("swamp", 0.82f, "Matched swamp/marsh keywords.", new[] { "wet", "foliage", "humid", "lush" }, new[] { "swamp", "marsh", "bog", "fen" }),
        new("steppe", 0.82f, "Matched steppe/grassland keywords.", new[] { "open", "grassland" }, new[] { "azim steppe", "steppe", "grassland", "grasslands" }),
        new("wasteland", 0.80f, "Matched wasteland/wastes keywords.", new[] { "dry", "badlands" }, new[] { "badlands", "wasteland", "wastes" }),
        new("cave", 0.80f, "Matched cave/cavern/mine keywords.", new[] { "dark", "interior" }, new[] { "cave", "cavern", "mine", "tunnel", "subterrane" }),
        new("void", 0.80f, "Matched void/darkness/abyss keywords.", new[] { "dark", "haunted", "gloom", "magic" }, new[] { "void", "darkness", "abyss", "ascian" }),
        new("tropical", 0.78f, "Matched island/tropical/Tuliyollal keywords.", new[] { "tropical", "coastal", "warm", "sunlit", "colorful", "foliage" }, new[] { "island", "tropical", "tuliyollal" }),
        new("fire", 0.78f, "Matched fire/flame/inferno keywords.", new[] { "heat", "fire" }, new[] { "fire", "flame", "inferno" })
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
        var biome = InferBiome(territory, weather, content, isSnow, isFog, isCloudy || isOvercast);
        var moodTags = BuildMoodTags(biome, isRain, isFog, isCloudy, isOvercast, isGloom, isSnow, isStorm, isDustStorm, isHeatWave);

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
            !context.InCombat && (!needsDutyReadability || context.InCutscene || context.InGpose),
            biome.Biome,
            biome.Confidence,
            biome.Reason,
            moodTags);
    }

    private static BiomeMatch InferBiome(string territory, string weather, string content, bool isSnow, bool isFog, bool isCloudMood)
    {
        var text = NormalizeSearchText($"{territory} {weather} {content}");
        if (isSnow)
        {
            return new BiomeMatch("snow", 1.0f, "Snow or blizzard weather overrides terrain biome to avoid missing active snow scenes.", new[] { "snow", "cold", "ice", "clean", "weatherOverride" });
        }

        foreach (var rule in BiomeRules)
        {
            if (ContainsAny(text, rule.Keywords))
            {
                var matchedKeyword = rule.Keywords.First(keyword => text.Contains(NormalizeSearchText(keyword), StringComparison.OrdinalIgnoreCase));
                var moodTags = AddContextualMoodTags(rule.MoodTags, text);
                var reason = $"{rule.Reason} Matched `{matchedKeyword}`.";
                if (text.Contains("garlemald", StringComparison.OrdinalIgnoreCase))
                {
                    reason += " Garlemald also adds cold/alpine mood context.";
                }

                return new BiomeMatch(rule.Biome, rule.Confidence, reason, moodTags);
            }
        }

        return isFog || isCloudMood
            ? new BiomeMatch("overcast", 0.55f, "No territory biome matched; fog/cloud/overcast weather supplied an overcast mood fallback.", new[] { "overcast", "haze", "softLight" })
            : new BiomeMatch("neutral", 0.25f, "No specific territory, content, or weather biome keyword matched.", Array.Empty<string>());
    }

    private static IReadOnlyList<string> BuildMoodTags(BiomeMatch biome, bool isRain, bool isFog, bool isCloudy, bool isOvercast, bool isGloom, bool isSnow, bool isStorm, bool isDustStorm, bool isHeatWave)
    {
        var moods = new List<string>(biome.MoodTags);
        if (isRain) moods.AddRange(new[] { "wet", "specular" });
        if (isFog) moods.AddRange(new[] { "fog", "mist", "haze" });
        if (isCloudy) moods.Add("clouds");
        if (isOvercast) moods.Add("overcast");
        if (isGloom) moods.AddRange(new[] { "gloom", "haunted", "dark" });
        if (isSnow) moods.Add("cold");
        if (isStorm) moods.Add("storm");
        if (isDustStorm) moods.Add("dust");
        if (isHeatWave) moods.Add("heat");
        return moods
            .Where(mood => !string.IsNullOrWhiteSpace(mood))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(mood => mood, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> AddContextualMoodTags(IReadOnlyList<string> moodTags, string text)
    {
        if (!text.Contains("garlemald", StringComparison.OrdinalIgnoreCase))
        {
            return moodTags;
        }

        return moodTags
            .Concat(new[] { "alpine", "snow" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        var normalizedValue = NormalizeSearchText(value);
        foreach (var candidate in candidates)
        {
            if (normalizedValue.Contains(NormalizeSearchText(candidate), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeSearchText(string value)
    {
        return value
            .ToLowerInvariant()
            .Replace('’', '\'')
            .Replace('`', '\'')
            .Replace("the rak'tika", "rak'tika", StringComparison.OrdinalIgnoreCase);
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
        var weather = GetCurrentWeather(territoryId, eorzeaHour);

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

    private (uint? Id, string Name) GetCurrentWeather(uint territoryId, float eorzeaHour)
    {
        try
        {
            var weatherId = GetCurrentWeatherId(territoryId, eorzeaHour);
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

    private static unsafe uint GetCurrentWeatherId(uint territoryId, float eorzeaHour)
    {
        if (territoryId == 0 || territoryId > ushort.MaxValue)
        {
            return 0;
        }

        var weatherManager = WeatherManager.Instance();
        var weatherHour = (byte)Math.Clamp((int)MathF.Floor(eorzeaHour), 0, 23);
        return weatherManager == null
            ? 0
            : (uint)weatherManager->GetWeatherForHour((ushort)territoryId, weatherHour);
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
