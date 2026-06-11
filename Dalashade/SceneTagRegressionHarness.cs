using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public sealed record SceneTagRegressionCase(string Name, GameContext Context, Configuration Configuration);

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
            Create("Snow + snow biome + dungeon + combat + low performance", "Snowcloak", "Snow", true, true, false, false, TimeBucket.Day, PerformanceBudget.Low),
            Create("Fog + forest + night", "The Black Shroud", "Fog", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium),
            Create("Rain + city + night", "Limsa Lominsa", "Rain", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, true),
            Create("Desert + dust + day", "Sagolii Desert", "Dust Storms", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium),
            Create("Cave + night + duty", "Cavern", "Clear Skies", true, false, false, false, TimeBucket.Night, PerformanceBudget.Medium),
            Create("HighTech neon + city + night", "Solution Nine", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, true),
            Create("Fae + GPose", "Il Mheg", "Fair Skies", false, false, false, true, TimeBucket.Dusk, PerformanceBudget.High),
            Create("Void + cutscene", "The World of Darkness", "Gloom", false, false, true, false, TimeBucket.Night, PerformanceBudget.Medium),
            Create("Jungle + rain", "Yak T'el", "Rain", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium),
            Create("Coastal + clear + dawn", "The Ruby Sea", "Clear Skies", false, false, false, false, TimeBucket.Dawn, PerformanceBudget.Medium)
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
        bool sanctuary = false)
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

        return new SceneTagRegressionCase(name, context, configuration);
    }

    private static void Validate(SceneTagRegressionCase testCase, ProfileResult result, List<SceneTagRegressionFailure> failures)
    {
        var profile = result.Profile;
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
