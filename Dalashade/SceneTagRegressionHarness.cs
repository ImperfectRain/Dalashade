using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record SceneTagRegressionCase(
    string Name,
    GameContext Context,
    Configuration Configuration,
    string ExpectedBiome,
    IReadOnlyList<string> ExpectedWeatherTags,
    IReadOnlyList<string> ExpectedMoodTags,
    IReadOnlyList<string> ExpectedSecondaryTags);

public sealed record SceneTagRegressionFailure(string CaseName, string Message);

public sealed record SceneTagRegressionResult(
    bool Success,
    IReadOnlyList<SceneTagRegressionFailure> Failures,
    IReadOnlyList<string> CaseNames)
{
    public static SceneTagRegressionResult Passed(IReadOnlyList<string> caseNames) => new(true, Array.Empty<SceneTagRegressionFailure>(), caseNames);
}

public static class SceneTagRegressionHarness
{
    public static SceneTagRegressionResult Run()
    {
        var engine = new ProfileEngine();
        var failures = new List<SceneTagRegressionFailure>();
        var cases = CreateCases();

        foreach (var testCase in cases)
        {
            var tags = SceneClassifier.Classify(testCase.Context);
            var result = engine.CreateWithRules(
                testCase.Context,
                tags,
                ImageAnalysisResult.Empty,
                ImageAnalysisResult.Empty,
                testCase.Configuration);
            Validate(testCase, result, failures);
        }

        return failures.Count == 0
            ? SceneTagRegressionResult.Passed(cases.Select(testCase => testCase.Name).ToArray())
            : new SceneTagRegressionResult(false, failures, cases.Select(testCase => testCase.Name).ToArray());
    }

    private static IReadOnlyList<SceneTagRegressionCase> CreateCases()
    {
        return new[]
        {
            Create("Eastern La Noscea / Costa del Sol coastal", "Eastern La Noscea", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "coastal", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "coastal", "seaside", "water", "specular" }, expectedSecondaryTags: new[] { "beach", "seaside", "tropical" }),
            Create("Rak'tika rainforest gloom night", "The Rak'tika Greatwood", "Umbral Wind", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "jungle", expectedWeatherTags: new[] { "gloom" }, expectedMoodTags: new[] { "rainforest", "lush", "verdant", "humid", "canopyLight", "gloom" }, expectedSecondaryTags: new[] { "rainforest", "lush", "verdant" }),
            Create("Amh Araeng heat night", "Amh Araeng", "Heat Waves", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "desert", expectedWeatherTags: new[] { "heat" }, expectedMoodTags: new[] { "desert", "badlands", "dry", "heat" }, expectedSecondaryTags: new[] { "badlands", "dry" }),
            Create("Snow + snow biome + dungeon + combat + low performance", "Snowcloak", "Snow", true, true, false, false, TimeBucket.Day, PerformanceBudget.Low, expectedBiome: "snow", expectedWeatherTags: new[] { "snow" }, expectedMoodTags: new[] { "snow", "cold", "ice" }, expectedSecondaryTags: new[] { "alpine", "ice" }),
            Create("Fog + forest + night", "The Black Shroud", "Fog", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "forest", expectedWeatherTags: new[] { "fog" }, expectedMoodTags: new[] { "foliage", "fog", "mist", "haze" }, expectedSecondaryTags: new[] { "lush", "verdant" }),
            Create("Rain + city + night", "Limsa Lominsa", "Rain", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, true, expectedBiome: "coastal", expectedWeatherTags: new[] { "rain" }, expectedMoodTags: new[] { "wet", "specular", "water" }, expectedSecondaryTags: new[] { "seaside" }),
            Create("Desert + dust + day", "Sagolii Desert", "Dust Storms", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "desert", expectedWeatherTags: new[] { "dust" }, expectedMoodTags: new[] { "dust", "dry", "badlands" }, expectedSecondaryTags: new[] { "badlands" }),
            Create("Cave + night + duty", "Cavern", "Clear Skies", true, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "cave", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "dark", "interior" }, expectedSecondaryTags: Array.Empty<string>()),
            Create("HighTech neon + city + night", "Solution Nine", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, true, expectedBiome: "highTech", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "neon", "highTech", "urban" }, expectedSecondaryTags: new[] { "neon", "urban" }),
            Create("Fae + GPose", "Il Mheg", "Fair Skies", false, false, false, true, TimeBucket.Dusk, PerformanceBudget.High, expectedBiome: "fae", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "fae", "dreamlike", "magical" }, expectedSecondaryTags: new[] { "fae" }),
            Create("Void + cutscene", "The World of Darkness", "Gloom", false, false, true, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "void", expectedWeatherTags: new[] { "gloom" }, expectedMoodTags: new[] { "haunted", "gloom", "dark" }, expectedSecondaryTags: Array.Empty<string>()),
            Create("Jungle + rain", "Yak T'el", "Rain", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "jungle", expectedWeatherTags: new[] { "rain" }, expectedMoodTags: new[] { "rainforest", "lush", "verdant", "wet" }, expectedSecondaryTags: new[] { "rainforest", "lush", "verdant" }),
            Create("Coastal + clear + dawn", "The Ruby Sea", "Clear Skies", false, false, false, false, TimeBucket.Dawn, PerformanceBudget.Medium, expectedBiome: "coastal", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "coastal", "water", "specular" }, expectedSecondaryTags: new[] { "seaside" }),
            Create("Ultima Thule cosmic", "Ultima Thule", "Fair Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "cosmic", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "cosmic", "alien", "aetherial" }, expectedSecondaryTags: new[] { "alien", "aetherial" }),
            Create("Mare Lamentorum lunar", "Mare Lamentorum", "Fair Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "lunar", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "lunar", "moonlit", "cold" }, expectedSecondaryTags: new[] { "moonlit", "cosmic" }),
            Create("Garlemald imperial industrial", "Garlemald", "Clouds", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "imperial", expectedWeatherTags: new[] { "clouds" }, expectedMoodTags: new[] { "imperial", "industrial", "metallic" }, expectedSecondaryTags: new[] { "industrial" })
        };
    }

    private static SceneTagRegressionCase Create(
        string name,
        string territoryName,
        string weatherName,
        bool inDuty,
        bool inCombat,
        bool inCutscene,
        bool inGpose,
        TimeBucket timeBucket,
        PerformanceBudget budget,
        bool sanctuary = false,
        string expectedBiome = "",
        IReadOnlyList<string>? expectedWeatherTags = null,
        IReadOnlyList<string>? expectedMoodTags = null,
        IReadOnlyList<string>? expectedSecondaryTags = null)
    {
        var context = new GameContext(
            1,
            territoryName,
            inDuty ? WorldCategory.Duty : sanctuary ? WorldCategory.City : WorldCategory.Field,
            1,
            weatherName,
            timeBucket switch
            {
                TimeBucket.Dawn => 6f,
                TimeBucket.Day => 12f,
                TimeBucket.Dusk => 18f,
                _ => 23f
            },
            timeBucket,
            inCombat,
            inCutscene,
            inGpose,
            inDuty,
            sanctuary,
            inDuty ? "Dungeon" : "Unknown",
            inDuty ? "Dungeon" : "Unknown");
        var configuration = new Configuration
        {
            AutoAdjustInCombat = true,
            AutoAdjustAtNight = true,
            AutoAdjustForWeather = true,
            AutoAdjustForTerritory = true,
            AutoAdjustFromScreenshots = false,
            MatchMasterPresetStyle = false,
            AutoAdjustInCutscenes = true,
            Style = TargetStyle.Balanced,
            PerformanceBudget = budget,
            CompatibilityMode = PresetCompatibilityMode.AdaptiveBalanced
        };

        return new SceneTagRegressionCase(
            name,
            context,
            configuration,
            expectedBiome,
            expectedWeatherTags ?? Array.Empty<string>(),
            expectedMoodTags ?? Array.Empty<string>(),
            expectedSecondaryTags ?? Array.Empty<string>());
    }

    private static void Validate(SceneTagRegressionCase testCase, ProfileResult result, List<SceneTagRegressionFailure> failures)
    {
        var profile = result.Profile;
        var diagnostics = result.TagStackDiagnostics;
        if (!string.IsNullOrWhiteSpace(testCase.ExpectedBiome) && !string.Equals(diagnostics.BiomeKey, testCase.ExpectedBiome, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Expected biome {testCase.ExpectedBiome}, got {diagnostics.BiomeKey}."));
        }

        foreach (var expected in testCase.ExpectedWeatherTags)
        {
            if (!diagnostics.ActiveWeatherTags.Contains(expected, StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Missing expected weather tag {expected}."));
            }
        }

        foreach (var expected in testCase.ExpectedMoodTags)
        {
            if (!diagnostics.MoodTags.Contains(expected, StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Missing expected mood tag {expected}."));
            }
        }

        foreach (var expected in testCase.ExpectedSecondaryTags)
        {
            if (!diagnostics.SecondaryTags.Contains(expected, StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Missing expected secondary tag {expected}."));
            }
        }

        if (profile.Bloom < 0.40f || profile.Bloom > 1.35f)
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Bloom outside final clamp: {profile.Bloom:0.###}"));
        }

        if (profile.AmbientOcclusion < 0.20f || profile.AmbientOcclusion > 1.30f)
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, $"AO outside final clamp: {profile.AmbientOcclusion:0.###}"));
        }

        if (profile.ShadowLift > 0.35f)
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, $"ShadowLift exceeded global cap: {profile.ShadowLift:0.###}"));
        }

        if (profile.Saturation < 0.78f)
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Saturation became globally dull: {profile.Saturation:0.###}"));
        }

        if (result.Rules.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, "No applied rules were recorded."));
        }

        if (result.TagStackDiagnostics.ActiveTags.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, "No active scene tags were recorded."));
        }
    }
}
