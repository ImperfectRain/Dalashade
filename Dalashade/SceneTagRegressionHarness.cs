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
    IReadOnlyList<string> ExpectedSecondaryTags,
    string ExpectedMaterialProfileFamily,
    IReadOnlyList<MaterialIntentExpectation> ExpectedMaterialMinimums,
    IReadOnlyList<MaterialIntentExpectation> ExpectedMaterialMaximums);

public sealed record MaterialIntentExpectation(string Channel, float Value);

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
        ValidateMaterialReportSnapshot(failures);

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

    private static void ValidateMaterialReportSnapshot(List<SceneTagRegressionFailure> failures)
    {
        var header = CompatibilityReportExporter.MaterialIntentDiagnosticsTableHeader;
        foreach (var required in new[] { "Profile prior", "Non-profile evidence", "Final value", "suppressions" })
        {
            if (!header.Contains(required, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure("Material report snapshot", $"MaterialIntent report header missing '{required}'."));
            }
        }
    }

    private static IReadOnlyList<SceneTagRegressionCase> CreateCases()
    {
        return new[]
        {
            Create("Eastern La Noscea / Costa del Sol coastal", "Eastern La Noscea", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "coastal", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "coastal", "seaside", "water", "specular" }, expectedSecondaryTags: new[] { "beach", "seaside", "tropical" }, expectedMaterialProfileFamily: "coastal/tropical", expectedMaterialMinimums: Materials((MaterialIntent.WaterSpecularChannel, 0.55f), (MaterialIntent.SandDustChannel, 0.28f), (MaterialIntent.SkyCloudFogChannel, 0.12f))),
            Create("Eastern La Noscea / Costa del Sol coastal night", "Eastern La Noscea", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "coastal", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "coastal", "seaside", "water", "specular" }, expectedSecondaryTags: new[] { "beach", "seaside", "tropical", "openSkyNight", "moonlitNight", "lamplitNight", "coastalNight" }, expectedMaterialMinimums: Materials((MaterialIntent.WaterSpecularChannel, 0.55f), (MaterialIntent.SandDustChannel, 0.28f))),
            Create("Rak'tika rainforest clear day", "The Rak'tika Greatwood", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "jungle", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "rainforest", "lush", "verdant", "humid", "canopyLight" }, expectedSecondaryTags: new[] { "rainforest", "lush", "verdant" }, expectedMaterialProfileFamily: "jungle/rainforest", expectedMaterialMinimums: Materials((MaterialIntent.FoliageChannel, 0.70f))),
            Create("Rak'tika rainforest gloom night", "The Rak'tika Greatwood", "Umbral Wind", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "jungle", expectedWeatherTags: new[] { "gloom" }, expectedMoodTags: new[] { "rainforest", "lush", "verdant", "humid", "canopyLight", "gloom" }, expectedSecondaryTags: new[] { "rainforest", "lush", "verdant", "wildNight", "canopyNight" }, expectedMaterialProfileFamily: "jungle/rainforest", expectedMaterialMinimums: Materials((MaterialIntent.FoliageChannel, 0.70f))),
            Create("Amh Araeng heat night", "Amh Araeng", "Heat Waves", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "desert", expectedWeatherTags: new[] { "heat" }, expectedMoodTags: new[] { "desert", "badlands", "dry", "heat" }, expectedSecondaryTags: new[] { "badlands", "dry", "openSkyNight", "moonlitNight", "desertNight" }, expectedMaterialProfileFamily: "desert/badlands", expectedMaterialMinimums: Materials((MaterialIntent.SandDustChannel, 0.70f))),
            Create("Snow + snow biome + dungeon + combat + low performance", "Snowcloak", "Snow", true, true, false, false, TimeBucket.Day, PerformanceBudget.Low, expectedBiome: "snow", expectedWeatherTags: new[] { "snow" }, expectedMoodTags: new[] { "snow", "cold", "ice" }, expectedSecondaryTags: new[] { "alpine", "ice" }, expectedMaterialProfileFamily: "snow/cold", expectedMaterialMinimums: Materials((MaterialIntent.SnowIceChannel, 0.70f))),
            Create("Fog + forest + night", "The Black Shroud", "Fog", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "forest", expectedWeatherTags: new[] { "fog" }, expectedMoodTags: new[] { "foliage", "fog", "mist", "haze" }, expectedSecondaryTags: new[] { "lush", "verdant", "wildNight", "canopyNight", "mistyNight" }),
            Create("Rainy coastal city wet stone", "Limsa Lominsa", "Rain", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, true, expectedBiome: "coastal", expectedWeatherTags: new[] { "rain" }, expectedMoodTags: new[] { "wet", "specular", "water" }, expectedSecondaryTags: new[] { "seaside", "openSkyNight", "moonlitNight", "lamplitNight", "settlementNight", "stormNight", "coastalNight" }, expectedMaterialProfileFamily: "coastal/tropical", expectedMaterialMinimums: Materials((MaterialIntent.WaterSpecularChannel, 0.65f), (MaterialIntent.SkyCloudFogChannel, 0.14f))),
            Create("Desert + dust + day", "Sagolii Desert", "Dust Storms", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "desert", expectedWeatherTags: new[] { "dust" }, expectedMoodTags: new[] { "dust", "dry", "badlands" }, expectedSecondaryTags: new[] { "badlands" }),
            Create("Cave + night + duty", "Cavern", "Clear Skies", true, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "cave", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "dark", "interior" }, expectedSecondaryTags: Array.Empty<string>(), expectedMaterialProfileFamily: "general", expectedMaterialMaximums: Materials((MaterialIntent.SkyCloudFogChannel, 0.20f))),
            Create("HighTech neon + city + night", "Solution Nine", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, true, expectedBiome: "highTech", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "neon", "highTech", "urban" }, expectedSecondaryTags: new[] { "neon", "urban", "lamplitNight", "settlementNight", "industrialNight", "aetherNight" }, expectedMaterialProfileFamily: "neon/high-tech", expectedMaterialMinimums: Materials((MaterialIntent.NeonGlassChannel, 0.70f), (MaterialIntent.MetalIndustrialChannel, 0.55f))),
            Create("Fae + GPose", "Il Mheg", "Fair Skies", false, false, false, true, TimeBucket.Dusk, PerformanceBudget.High, expectedBiome: "fae", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "fae", "dreamlike", "magical" }, expectedSecondaryTags: new[] { "fae" }, expectedMaterialProfileFamily: "aetherial/cosmic", expectedMaterialMinimums: Materials((MaterialIntent.CrystalAetherChannel, 0.55f))),
            Create("Void + cutscene", "The World of Darkness", "Gloom", false, false, true, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "void", expectedWeatherTags: new[] { "gloom" }, expectedMoodTags: new[] { "haunted", "gloom", "dark" }, expectedSecondaryTags: Array.Empty<string>(), expectedMaterialMinimums: Materials((MaterialIntent.VoidDarknessChannel, 0.55f))),
            Create("Jungle + rain", "Yak T'el", "Rain", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "jungle", expectedWeatherTags: new[] { "rain" }, expectedMoodTags: new[] { "rainforest", "lush", "verdant", "wet" }, expectedSecondaryTags: new[] { "rainforest", "lush", "verdant" }),
            Create("Coastal + clear + dawn", "The Ruby Sea", "Clear Skies", false, false, false, false, TimeBucket.Dawn, PerformanceBudget.Medium, expectedBiome: "coastal", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "coastal", "water", "specular" }, expectedSecondaryTags: new[] { "seaside" }),
            Create("Ultima Thule cosmic", "Ultima Thule", "Fair Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "cosmic", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "cosmic", "alien", "aetherial" }, expectedSecondaryTags: new[] { "alien", "aetherial", "openSkyNight", "moonlitNight", "lamplitNight", "aetherNight" }, expectedMaterialProfileFamily: "aetherial/cosmic", expectedMaterialMinimums: Materials((MaterialIntent.CrystalAetherChannel, 0.65f), (MaterialIntent.SkyCloudFogChannel, 0.25f))),
            Create("Mare Lamentorum lunar", "Mare Lamentorum", "Fair Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "lunar", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "lunar", "moonlit", "cold" }, expectedSecondaryTags: new[] { "moonlit", "cosmic", "openSkyNight", "moonlitNight", "lamplitNight", "snowNight", "coldNight", "aetherNight" }),
            Create("Garlemald imperial industrial", "Garlemald", "Clouds", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "imperial", expectedWeatherTags: new[] { "clouds" }, expectedMoodTags: new[] { "imperial", "industrial", "metallic" }, expectedSecondaryTags: new[] { "industrial" }, expectedMaterialProfileFamily: "snow/cold", expectedMaterialMinimums: Materials((MaterialIntent.SnowIceChannel, 0.25f), (MaterialIntent.MetalIndustrialChannel, 0.70f))),
            Create("East Shroud night forest material false-positive guard", "East Shroud", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "forest", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "foliage" }, expectedSecondaryTags: new[] { "lush", "verdant", "wildNight", "canopyNight" }, expectedMaterialMinimums: Materials((MaterialIntent.FoliageChannel, 0.55f), (MaterialIntent.SkyCloudFogChannel, 0.08f)), expectedMaterialMaximums: Materials((MaterialIntent.VoidDarknessChannel, 0.05f))),
            Create("Allagan hard-surface material", "Azys Lla", "Fair Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "ancient", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "ancient", "ruins", "structured" }, expectedSecondaryTags: new[] { "ruins", "structured" }, expectedMaterialProfileFamily: "ancient/ruins", expectedMaterialMinimums: Materials((MaterialIntent.StoneRuinsChannel, 0.35f), (MaterialIntent.MetalIndustrialChannel, 0.20f), (MaterialIntent.CrystalAetherChannel, 0.15f))),
            Create("Heritage Found neon material", "Heritage Found", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "highTech", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "neon", "highTech", "urban" }, expectedSecondaryTags: new[] { "neon", "urban", "lamplitNight", "settlementNight", "industrialNight", "aetherNight" }, expectedMaterialProfileFamily: "neon/high-tech", expectedMaterialMinimums: Materials((MaterialIntent.NeonGlassChannel, 0.70f), (MaterialIntent.MetalIndustrialChannel, 0.55f)))
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
        IReadOnlyList<string>? expectedSecondaryTags = null,
        string expectedMaterialProfileFamily = "",
        IReadOnlyList<MaterialIntentExpectation>? expectedMaterialMinimums = null,
        IReadOnlyList<MaterialIntentExpectation>? expectedMaterialMaximums = null)
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
            expectedSecondaryTags ?? Array.Empty<string>(),
            expectedMaterialProfileFamily,
            expectedMaterialMinimums ?? Array.Empty<MaterialIntentExpectation>(),
            expectedMaterialMaximums ?? Array.Empty<MaterialIntentExpectation>());
    }

    private static IReadOnlyList<MaterialIntentExpectation> Materials(params (string Channel, float Value)[] expectations)
    {
        return expectations
            .Select(expectation => new MaterialIntentExpectation(expectation.Channel, expectation.Value))
            .ToArray();
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

        var materialProfile = MaterialProfileBuilder.Build(diagnostics, ImageAnalysisResult.Empty);
        var materialIntent = MaterialIntentBuilder.Build(diagnostics, ImageAnalysisResult.Empty, materialProfile);
        if (!string.IsNullOrWhiteSpace(testCase.ExpectedMaterialProfileFamily) && !string.Equals(materialProfile.Family, testCase.ExpectedMaterialProfileFamily, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Expected material profile family {testCase.ExpectedMaterialProfileFamily}, got {materialProfile.Family}."));
        }

        if (testCase.ExpectedMaterialMinimums.Count > 0 || testCase.ExpectedMaterialMaximums.Count > 0)
        {
            ValidateMaterialReportShape(testCase, materialProfile, materialIntent, failures);
        }

        foreach (var expected in testCase.ExpectedMaterialMinimums)
        {
            var actual = materialIntent.ValueFor(expected.Channel);
            if (actual < expected.Value)
            {
                failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Expected material {expected.Channel} >= {expected.Value:0.###}, got {actual:0.###}."));
            }
        }

        foreach (var expected in testCase.ExpectedMaterialMaximums)
        {
            var actual = materialIntent.ValueFor(expected.Channel);
            if (actual > expected.Value)
            {
                failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Expected material {expected.Channel} <= {expected.Value:0.###}, got {actual:0.###}."));
            }
        }
    }

    private static void ValidateMaterialReportShape(SceneTagRegressionCase testCase, MaterialProfile materialProfile, MaterialIntent materialIntent, List<SceneTagRegressionFailure> failures)
    {
        if (materialProfile.TopPriors(1).Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, "Material report shape missing MaterialProfile prior."));
        }

        if (!materialIntent.Contributions.Any(contribution => contribution.Source.StartsWith("MaterialProfile prior", StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, "Material report shape missing profile-prior contribution."));
        }

        if (!materialIntent.Contributions.Any(contribution => contribution.Amount > 0f && !contribution.Source.StartsWith("MaterialProfile", StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, "Material report shape missing non-profile evidence contribution."));
        }

        if (!MaterialIntent.ChannelNames.Any(channel => materialIntent.ValueFor(channel) > 0.001f))
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, "Material report shape missing final MaterialIntent values."));
        }

    }
}
