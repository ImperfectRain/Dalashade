using Dalamud.Game.ClientState.Conditions;
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

public sealed record GameContext(
    uint TerritoryId,
    string TerritoryName,
    WorldCategory WorldCategory,
    bool InCombat,
    bool InCutscene,
    bool IsNight,
    uint? WeatherId,
    string WeatherName)
{
    public static GameContext Empty { get; } = new(0, "Unknown", WorldCategory.Unknown, false, false, false, null, "Unknown");

    public string ProfileKey(Configuration configuration, ImageAnalysisResult imageAnalysis)
    {
        var combat = configuration.AutoAdjustInCombat && InCombat ? "combat" : "safe";
        var cutscene = configuration.AutoAdjustInCutscenes && InCutscene ? "cutscene" : "gameplay";
        var time = configuration.AutoAdjustAtNight && IsNight ? "night" : "day";
        var weather = configuration.AutoAdjustForWeather ? WeatherName : "ignored";
        var territory = configuration.AutoAdjustForTerritory ? WorldCategory.ToString() : "ignored";
        var image = configuration.AutoAdjustFromScreenshots ? imageAnalysis.ProfileBucket : "ignored";

        return $"{TerritoryId}:{territory}:{weather}:{time}:{combat}:{cutscene}:{image}:{configuration.Style}:{configuration.PerformanceBudget}";
    }
}

public sealed class GameContextService
{
    private uint? zoneInitWeatherId;
    private string zoneInitWeatherName = "Unknown";

    public GameContext Current { get; private set; } = GameContext.Empty;

    public void UpdateWeather(uint? weatherId, string weatherName)
    {
        zoneInitWeatherId = weatherId;
        zoneInitWeatherName = string.IsNullOrWhiteSpace(weatherName) ? "Unknown" : weatherName;
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
        var inCutscene = Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Plugin.Condition[ConditionFlag.WatchingCutscene];

        Current = new GameContext(
            territoryId,
            territoryName,
            ClassifyTerritory(territoryName, inCombat),
            inCombat,
            inCutscene,
            IsApproximateEorzeaNight(),
            zoneInitWeatherId,
            zoneInitWeatherName);
    }

    private static bool IsApproximateEorzeaNight()
    {
        var eorzeaHour = (int)((DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 20.571428571 / 3600) % 24);
        return eorzeaHour is >= 18 or < 6;
    }

    private static WorldCategory ClassifyTerritory(string territoryName, bool inCombat)
    {
        if (inCombat)
        {
            return WorldCategory.Duty;
        }

        if (ContainsAny(territoryName, "Limsa Lominsa", "Gridania", "Ul'dah", "Ishgard", "Kugane", "Crystarium", "Old Sharlayan", "Tuliyollal"))
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
