using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalashade.SceneAuthoring;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Dalashade;

public sealed record SceneTagRegressionCase(
    string Name,
    GameContext Context,
    Configuration Configuration,
    string ExpectedBiome,
    string ExpectedArea,
    IReadOnlyList<string> ExpectedWeatherTags,
    IReadOnlyList<string> ExpectedMoodTags,
    IReadOnlyList<string> ExpectedSecondaryTags,
    IReadOnlyList<SceneIntentExpectation> ExpectedIntentMinimums,
    string ExpectedMaterialProfileFamily,
    IReadOnlyList<MaterialIntentExpectation> ExpectedMaterialMinimums,
    IReadOnlyList<MaterialIntentExpectation> ExpectedMaterialMaximums);

public sealed record MaterialIntentExpectation(string Channel, float Value);

public sealed record SceneIntentExpectation(string Channel, float Value);

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
        ValidateSceneAuthoringOverrides(failures);
        ValidateImageAnalysisOpinions(failures);

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

    private static void ValidateSceneAuthoringOverrides(List<SceneTagRegressionFailure> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "DalashadeSceneAuthoringRegression", Guid.NewGuid().ToString("N"));
        try
        {
            var context = new GameContext(
                9999,
                "Authoring Regression Territory",
                WorldCategory.Field,
                1,
                "Clear Skies",
                23f,
                TimeBucket.Night,
                false,
                false,
                false,
                false,
                false,
                "Unknown",
                "Unknown");
            var detected = SceneClassifier.Classify(context) with
            {
                BiomeKey = "aetherial",
                BiomeConfidence = 0.95f,
                BiomeReason = "Regression seed.",
                MoodTags = new[] { "aetherial", "clean", "luminous" }
            };
            var disabledConfiguration = new Configuration { EnableSceneAuthoringOverrides = false };
            var enabledConfiguration = new Configuration { EnableSceneAuthoringOverrides = true };
            var service = new SceneAuthoringService();
            service.Load(root);

            var coastalPreset = service.FindPreset(SceneAuthoringService.BiomeCategory, "coastal");
            if (coastalPreset is null || coastalPreset.Tunings.Count == 0)
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring tag registry defaults", "Built-in coastal tag preset did not include default tuning rows."));
            }

            var disabled = service.Apply(disabledConfiguration, context, detected);
            if (!ReferenceEquals(disabled.EffectiveTags, detected) && disabled.EffectiveTags != detected)
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring disabled", "Disabled authoring did not pass detected tags through unchanged."));
            }

            service.AddTag(context, SceneAuthoringService.WeatherCategory, "rain");
            service.RemoveTag(context, SceneAuthoringService.MoodCategory, "clean");
            service.RemoveTag(context, SceneAuthoringService.SecondaryCategory, "aetherNight");
            service.SetPrimaryBiome(context, "forest");
            var applied = service.Apply(enabledConfiguration, context, detected);
            var diagnostics = TagStackDiagnostics.Create(context, applied.EffectiveTags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());

            if (!applied.EffectiveTags.IsRain)
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring add tag", "Added rain weather tag did not apply to effective tags."));
            }

            if (applied.EffectiveTags.MoodTags.Contains("clean", StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring remove mood tag", "Removed mood tag remained in effective tags."));
            }

            if (diagnostics.SecondaryTags.Contains("aetherNight", StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring suppress derived tag", "Removed derived secondary tag was regenerated in diagnostics."));
            }

            if (!string.Equals(applied.EffectiveTags.BiomeKey, "forest", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring primary biome", $"Expected forest biome override, got {applied.EffectiveTags.BiomeKey}."));
            }

            service.ExportOverrides();
            service.ExportTagPresets();
            service.AddCustomTagPreset(SceneAuthoringService.MoodCategory, "regressionCustom");
            if (!service.KnownTagsForCategory(SceneAuthoringService.MoodCategory).Contains("regressionCustom", StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring custom tag preset", "Custom tag preset was not added to known mood tags."));
            }

            service.AddTagTuning(SceneAuthoringService.MoodCategory, "regressionCustom");
            var customPreset = service.FindPreset(SceneAuthoringService.MoodCategory, "regressionCustom");
            if (customPreset is null || customPreset.Tunings.Count == 0)
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring add tag tuning", "Custom tag tuning row was not added."));
            }
            else
            {
                service.UpdateTagTuning(
                    SceneAuthoringService.MoodCategory,
                    "regressionCustom",
                    0,
                    new SceneTagTuning
                    {
                        Target = SceneTagTuningTargets.MaterialIntent,
                        Channel = MaterialIntent.FoliageChannel,
                        Amount = 0.25f,
                        Reason = "Regression tuning."
                    });
                customPreset = service.FindPreset(SceneAuthoringService.MoodCategory, "regressionCustom");
                var updated = customPreset?.Tunings.FirstOrDefault();
                if (updated is null
                    || !string.Equals(updated.Target, SceneTagTuningTargets.MaterialIntent, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(updated.Channel, MaterialIntent.FoliageChannel, StringComparison.OrdinalIgnoreCase)
                    || MathF.Abs(updated.Amount - 0.25f) > 0.001f)
                {
                    failures.Add(new SceneTagRegressionFailure("Scene authoring update tag tuning", "Custom tag tuning row was not updated."));
                }

                service.RemoveTagTuning(SceneAuthoringService.MoodCategory, "regressionCustom", 0);
                customPreset = service.FindPreset(SceneAuthoringService.MoodCategory, "regressionCustom");
                if (customPreset is null || customPreset.Tunings.Count != 0)
                {
                    failures.Add(new SceneTagRegressionFailure("Scene authoring remove tag tuning", "Custom tag tuning row was not removed."));
                }
            }

            service.ResetTagPresets();
            service.ImportTagPresets();
            if (service.KnownTagsForCategory(SceneAuthoringService.MoodCategory).Contains("regressionCustom", StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring tag preset import", "Import did not restore exported tag preset state."));
            }

            var fingerprintWithOverride = applied.Fingerprint;
            service.ClearTagOverride(context, SceneAuthoringService.MoodCategory, "clean");
            service.ClearPrimaryBiomeOverride(context);
            var cleared = service.Apply(enabledConfiguration, context, detected);
            if (string.Equals(fingerprintWithOverride, cleared.Fingerprint, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring cache fingerprint", "Fingerprint did not change after clearing overrides."));
            }

            service.ResetCurrentScene(context);
            var reset = service.Apply(enabledConfiguration, context, detected);
            if (reset.ActiveOverride is not null && reset.ActiveOverride.HasEdits)
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring reset", "Reset current scene left edited override state behind."));
            }

            service.ImportOverrides();
            var imported = service.Apply(enabledConfiguration, context, detected);
            if (!imported.EffectiveTags.IsRain || !string.Equals(imported.EffectiveTags.BiomeKey, "forest", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(new SceneTagRegressionFailure("Scene authoring override import", "Import did not restore exported scene override state."));
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
            catch
            {
                // Regression cleanup is best-effort.
            }
        }
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

    private static void ValidateImageAnalysisOpinions(List<SceneTagRegressionFailure> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "DalashadeImageAnalysisRegression", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var coastalPath = Path.Combine(root, "coastal.png");
            using (var image = new Image<Rgba32>(120, 90))
            {
                for (var y = 0; y < image.Height; y++)
                {
                    for (var x = 0; x < image.Width; x++)
                    {
                        image[x, y] = y < 30
                            ? new Rgba32(90, 170, 245)
                            : y > 58
                                ? new Rgba32(20, 160, 220)
                                : new Rgba32(80, 130, 95);
                    }
                }

                image.SaveAsPng(coastalPath);
            }

            var coastal = ImageAnalysisService.Analyze(coastalPath, File.GetLastWriteTimeUtc(coastalPath), ImageSamplingMode.FullImage);
            ExpectOpinion(failures, "Image analysis coastal synthetic", coastal, ImageSceneOpinionKeys.SkyAir, 0.18f);
            ExpectOpinion(failures, "Image analysis coastal synthetic", coastal, ImageSceneOpinionKeys.WaterSurface, 0.18f);

            var brightPath = Path.Combine(root, "bright.png");
            using (var image = new Image<Rgba32>(48, 48))
            {
                for (var y = 0; y < image.Height; y++)
                {
                    for (var x = 0; x < image.Width; x++)
                    {
                        image[x, y] = new Rgba32(255, 250, 240);
                    }
                }

                image.SaveAsPng(brightPath);
            }

            var bright = ImageAnalysisService.Analyze(brightPath, File.GetLastWriteTimeUtc(brightPath), ImageSamplingMode.FullImage);
            ExpectOpinion(failures, "Image analysis bright synthetic", bright, ImageSceneOpinionKeys.HighlightProtection, 0.18f);

            var context = new GameContext(
                1,
                "Eastern La Noscea",
                WorldCategory.Field,
                1,
                "Clear Skies",
                12f,
                TimeBucket.Day,
                false,
                false,
                false,
                false,
                false,
                "Unknown",
                "Unknown");
            var tags = SceneClassifier.Classify(context);
            var muted = new Configuration
            {
                AutoAdjustFromScreenshots = true,
                ScreenshotAnalysisStrength = 0f,
                AutoAdjustForWeather = true,
                AutoAdjustForTerritory = true,
                PerformanceBudget = PerformanceBudget.Medium
            };
            var active = new Configuration
            {
                AutoAdjustFromScreenshots = true,
                ScreenshotAnalysisStrength = 1f,
                AutoAdjustForWeather = true,
                AutoAdjustForTerritory = true,
                PerformanceBudget = PerformanceBudget.Medium
            };

            var mutedResult = new ProfileEngine().CreateWithRules(context, tags, coastal, ImageAnalysisResult.Empty, muted);
            var activeResult = new ProfileEngine().CreateWithRules(context, tags, coastal, ImageAnalysisResult.Empty, active);
            if (activeResult.TagStackDiagnostics.Intent.DayReflection <= mutedResult.TagStackDiagnostics.Intent.DayReflection)
            {
                failures.Add(new SceneTagRegressionFailure("Image analysis strength", "Water screenshot opinion did not increase DayReflection when strength was enabled."));
            }

            var activeProfile = MaterialProfileBuilder.Build(activeResult.TagStackDiagnostics, coastal, active.ScreenshotAnalysisStrength);
            var mutedProfile = MaterialProfileBuilder.Build(mutedResult.TagStackDiagnostics, coastal, muted.ScreenshotAnalysisStrength);
            if (activeProfile.ValueFor(MaterialIntent.WaterSpecularChannel) <= mutedProfile.ValueFor(MaterialIntent.WaterSpecularChannel))
            {
                failures.Add(new SceneTagRegressionFailure("Image analysis strength", "Water screenshot opinion did not increase WaterSpecular material plausibility when strength was enabled."));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            failures.Add(new SceneTagRegressionFailure("Image analysis synthetic tests", $"Synthetic image validation failed: {ex.Message}"));
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
            catch
            {
                // Regression cleanup is best-effort.
            }
        }
    }

    private static void ExpectOpinion(List<SceneTagRegressionFailure> failures, string caseName, ImageAnalysisResult image, string opinionKey, float minimum)
    {
        var confidence = image.OpinionConfidence(opinionKey);
        if (confidence < minimum)
        {
            failures.Add(new SceneTagRegressionFailure(caseName, $"Expected opinion {opinionKey} >= {minimum:0.##}, got {confidence:0.##}. Summary: {image.OpinionSummary}"));
        }
    }

    private static IReadOnlyList<SceneTagRegressionCase> CreateCases()
    {
        return new[]
        {
            Create("Eastern La Noscea / Costa del Sol coastal", "Eastern La Noscea", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "coastal", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "coastal", "seaside", "water", "specular" }, expectedSecondaryTags: new[] { "beach", "seaside", "tropical", "sunlitDay", "coastalDay", "openSkyDay", "goldenDay" }, expectedIntentMinimums: Intents((nameof(SceneIntent.DayReflection), 0.20f), (nameof(SceneIntent.DayHighlightPressure), 0.20f)), expectedMaterialProfileFamily: "coastal/tropical", expectedMaterialMinimums: Materials((MaterialIntent.WaterSpecularChannel, 0.55f), (MaterialIntent.SandDustChannel, 0.28f), (MaterialIntent.SkyCloudFogChannel, 0.12f))),
            Create("Eastern La Noscea / Costa del Sol coastal night", "Eastern La Noscea", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "coastal", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "coastal", "seaside", "water", "specular" }, expectedSecondaryTags: new[] { "beach", "seaside", "tropical", "openSkyNight", "moonlitNight", "lamplitNight", "coastalNight" }, expectedMaterialMinimums: Materials((MaterialIntent.WaterSpecularChannel, 0.55f), (MaterialIntent.SandDustChannel, 0.28f))),
            Create("Rak'tika rainforest clear day", "The Rak'tika Greatwood", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "jungle", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "rainforest", "lush", "verdant", "humid", "canopyLight" }, expectedSecondaryTags: new[] { "rainforest", "lush", "verdant", "sunlitDay", "canopyDay" }, expectedMaterialProfileFamily: "jungle/rainforest", expectedMaterialMinimums: Materials((MaterialIntent.FoliageChannel, 0.70f))),
            Create("Rak'tika rainforest gloom night", "The Rak'tika Greatwood", "Umbral Wind", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "jungle", expectedWeatherTags: new[] { "gloom" }, expectedMoodTags: new[] { "rainforest", "lush", "verdant", "humid", "canopyLight", "gloom" }, expectedSecondaryTags: new[] { "rainforest", "lush", "verdant", "wildNight", "canopyNight" }, expectedMaterialProfileFamily: "jungle/rainforest", expectedMaterialMinimums: Materials((MaterialIntent.FoliageChannel, 0.70f))),
            Create("Amh Araeng heat night", "Amh Araeng", "Heat Waves", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "desert", expectedWeatherTags: new[] { "heat" }, expectedMoodTags: new[] { "desert", "badlands", "dry", "heat" }, expectedSecondaryTags: new[] { "badlands", "dry", "openSkyNight", "moonlitNight", "desertNight" }, expectedMaterialProfileFamily: "desert/badlands", expectedMaterialMinimums: Materials((MaterialIntent.SandDustChannel, 0.70f))),
            Create("Snow + snow biome + dungeon + combat + low performance", "Snowcloak", "Snow", true, true, false, false, TimeBucket.Day, PerformanceBudget.Low, expectedBiome: "snow", expectedWeatherTags: new[] { "snow" }, expectedMoodTags: new[] { "snow", "cold", "ice" }, expectedSecondaryTags: new[] { "alpine", "ice", "snowDay", "coldDay", "openSkyDay", "interiorDay", "dungeonDay" }, expectedMaterialProfileFamily: "snow/cold", expectedMaterialMinimums: Materials((MaterialIntent.SnowIceChannel, 0.70f))),
            Create("Fog + forest + night", "The Black Shroud", "Fog", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "forest", expectedWeatherTags: new[] { "fog" }, expectedMoodTags: new[] { "foliage", "fog", "mist", "haze" }, expectedSecondaryTags: new[] { "lush", "verdant", "wildNight", "canopyNight", "mistyNight" }),
            Create("Rainy coastal city wet stone", "Limsa Lominsa", "Rain", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, true, expectedBiome: "coastal", expectedWeatherTags: new[] { "rain" }, expectedMoodTags: new[] { "wet", "specular", "water" }, expectedSecondaryTags: new[] { "seaside", "openSkyNight", "moonlitNight", "lamplitNight", "settlementNight", "stormNight", "coastalNight" }, expectedMaterialProfileFamily: "coastal/tropical", expectedMaterialMinimums: Materials((MaterialIntent.WaterSpecularChannel, 0.65f), (MaterialIntent.SkyCloudFogChannel, 0.14f))),
            Create("Desert + dust + day", "Sagolii Desert", "Dust Storms", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "desert", expectedWeatherTags: new[] { "dust" }, expectedMoodTags: new[] { "dust", "dry", "badlands" }, expectedSecondaryTags: new[] { "badlands", "openSkyDay", "desertDay", "heatDay", "sunlitDay", "goldenDay" }, expectedIntentMinimums: Intents((nameof(SceneIntent.SurfaceHeat), 0.30f))),
            Create("Cave + night + duty", "Cavern", "Clear Skies", true, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "cave", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "dark", "interior" }, expectedSecondaryTags: Array.Empty<string>(), expectedMaterialProfileFamily: "general", expectedMaterialMaximums: Materials((MaterialIntent.SkyCloudFogChannel, 0.20f))),
            Create("HighTech neon + city + night", "Solution Nine", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, true, expectedBiome: "highTech", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "neon", "highTech", "urban" }, expectedSecondaryTags: new[] { "neon", "urban", "lamplitNight", "settlementNight", "industrialNight", "aetherNight" }, expectedMaterialProfileFamily: "neon/high-tech", expectedMaterialMinimums: Materials((MaterialIntent.NeonGlassChannel, 0.70f), (MaterialIntent.MetalIndustrialChannel, 0.55f))),
            Create("Fae + GPose", "Il Mheg", "Fair Skies", false, false, false, true, TimeBucket.Dusk, PerformanceBudget.High, expectedBiome: "fae", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "fae", "dreamlike", "magical" }, expectedSecondaryTags: new[] { "fae" }, expectedMaterialProfileFamily: "aetherial/cosmic", expectedMaterialMinimums: Materials((MaterialIntent.CrystalAetherChannel, 0.55f))),
            Create("Void + cutscene", "The World of Darkness", "Gloom", false, false, true, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "void", expectedWeatherTags: new[] { "gloom" }, expectedMoodTags: new[] { "haunted", "gloom", "dark" }, expectedSecondaryTags: Array.Empty<string>(), expectedMaterialMinimums: Materials((MaterialIntent.VoidDarknessChannel, 0.55f))),
            Create("Jungle + rain", "Yak T'el", "Rain", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "jungle", expectedWeatherTags: new[] { "rain" }, expectedMoodTags: new[] { "rainforest", "lush", "verdant", "wet" }, expectedSecondaryTags: new[] { "rainforest", "lush", "verdant", "canopyDay", "stormDay" }, expectedIntentMinimums: Intents((nameof(SceneIntent.DayAtmosphere), 0.20f))),
            Create("Coastal + clear + dawn", "The Ruby Sea", "Clear Skies", false, false, false, false, TimeBucket.Dawn, PerformanceBudget.Medium, expectedBiome: "coastal", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "coastal", "water", "specular" }, expectedSecondaryTags: new[] { "seaside" }),
            Create("Ultima Thule cosmic", "Ultima Thule", "Fair Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "cosmic", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "cosmic", "alien", "aetherial" }, expectedSecondaryTags: new[] { "alien", "aetherial", "openSkyNight", "moonlitNight", "lamplitNight", "aetherNight" }, expectedMaterialProfileFamily: "aetherial/cosmic", expectedMaterialMinimums: Materials((MaterialIntent.CrystalAetherChannel, 0.65f), (MaterialIntent.SkyCloudFogChannel, 0.25f))),
            Create("Mare Lamentorum lunar", "Mare Lamentorum", "Fair Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "lunar", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "lunar", "moonlit", "cold" }, expectedSecondaryTags: new[] { "moonlit", "cosmic", "openSkyNight", "moonlitNight", "lamplitNight", "snowNight", "coldNight", "aetherNight" }),
            Create("Garlemald imperial industrial", "Garlemald", "Clouds", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "imperial", expectedWeatherTags: new[] { "clouds" }, expectedMoodTags: new[] { "imperial", "industrial", "metallic" }, expectedSecondaryTags: new[] { "industrial", "settlementDay", "industrialDay", "overcastDay" }, expectedMaterialProfileFamily: "snow/cold", expectedMaterialMinimums: Materials((MaterialIntent.SnowIceChannel, 0.25f), (MaterialIntent.MetalIndustrialChannel, 0.70f))),
            Create("East Shroud night forest material false-positive guard", "East Shroud", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "forest", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "foliage" }, expectedSecondaryTags: new[] { "lush", "verdant", "wildNight", "canopyNight" }, expectedMaterialMinimums: Materials((MaterialIntent.FoliageChannel, 0.55f), (MaterialIntent.SkyCloudFogChannel, 0.08f)), expectedMaterialMaximums: Materials((MaterialIntent.VoidDarknessChannel, 0.05f))),
            Create("Allagan hard-surface material", "Azys Lla", "Fair Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "ancient", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "ancient", "ruins", "structured" }, expectedSecondaryTags: new[] { "ruins", "structured", "sunlitDay" }, expectedMaterialProfileFamily: "ancient/ruins", expectedMaterialMinimums: Materials((MaterialIntent.StoneRuinsChannel, 0.35f), (MaterialIntent.MetalIndustrialChannel, 0.20f), (MaterialIntent.CrystalAetherChannel, 0.15f))),
            Create("Solution Nine high-tech day", "Solution Nine", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, true, expectedBiome: "highTech", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "neon", "highTech", "urban" }, expectedSecondaryTags: new[] { "neon", "urban", "sunlitDay", "settlementDay", "industrialDay", "highTechDay" }, expectedMaterialProfileFamily: "neon/high-tech", expectedMaterialMinimums: Materials((MaterialIntent.NeonGlassChannel, 0.70f), (MaterialIntent.MetalIndustrialChannel, 0.55f))),
            Create("Heritage Found neon material", "Heritage Found", "Clear Skies", false, false, false, false, TimeBucket.Night, PerformanceBudget.Medium, expectedBiome: "highTech", expectedWeatherTags: new[] { "clear" }, expectedMoodTags: new[] { "neon", "highTech", "urban" }, expectedSecondaryTags: new[] { "neon", "urban", "lamplitNight", "settlementNight", "industrialNight", "aetherNight" }, expectedMaterialProfileFamily: "neon/high-tech", expectedMaterialMinimums: Materials((MaterialIntent.NeonGlassChannel, 0.70f), (MaterialIntent.MetalIndustrialChannel, 0.55f))),
            Create("Idyllshire exact hub profile", "Idyllshire", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "ancient", expectedArea: "city", expectedMoodTags: new[] { "settlement", "ruins", "scholarly" }),
            Create("Rhalgr's Reach exact hub profile", "Rhalgr's Reach", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "desert", expectedArea: "city", expectedMoodTags: new[] { "highland", "settlement", "dry" }),
            Create("Eulmore exact hub profile", "Eulmore", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "coastal", expectedArea: "city", expectedMoodTags: new[] { "coastal", "urban", "decadent" }),
            Create("Radz-at-Han exact hub profile", "Radz-at-Han", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "tropical", expectedArea: "city", expectedMoodTags: new[] { "tropical", "colorful", "alchemical" }),
            Create("Sea of Clouds exact sky profile", "The Sea of Clouds", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "alpine", expectedArea: "field", expectedMoodTags: new[] { "sky", "clouds", "highAltitude" }),
            Create("The Tempest exact underwater profile", "The Tempest", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "underwater", expectedArea: "field", expectedMoodTags: new[] { "underwater", "ancient", "depth" }),
            Create("Labyrinthos exact research profile", "Labyrinthos", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "aetherial", expectedArea: "field", expectedMoodTags: new[] { "artificial", "greenhouse", "scholarly" }),
            Create("Urqopacha exact mountain profile", "Urqopacha", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "alpine", expectedArea: "field", expectedMoodTags: new[] { "highAltitude", "mountain", "warm" }),
            Create("Eureka Pagos exact exploration profile", "The Forbidden Land, Eureka Pagos", "Clear Skies", true, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "snow", expectedArea: "dungeon", expectedMoodTags: new[] { "snow", "ice", "aetherial" }),
            Create("Phaenna exact cosmic profile", "Phaenna", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "aetherial", expectedArea: "field", expectedMoodTags: new[] { "crystal", "cosmic", "glass" })
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
        string expectedArea = "",
        IReadOnlyList<string>? expectedWeatherTags = null,
        IReadOnlyList<string>? expectedMoodTags = null,
        IReadOnlyList<string>? expectedSecondaryTags = null,
        IReadOnlyList<SceneIntentExpectation>? expectedIntentMinimums = null,
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
            expectedArea,
            expectedWeatherTags ?? Array.Empty<string>(),
            expectedMoodTags ?? Array.Empty<string>(),
            expectedSecondaryTags ?? Array.Empty<string>(),
            expectedIntentMinimums ?? Array.Empty<SceneIntentExpectation>(),
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

    private static IReadOnlyList<SceneIntentExpectation> Intents(params (string Channel, float Value)[] expectations)
    {
        return expectations
            .Select(expectation => new SceneIntentExpectation(expectation.Channel, expectation.Value))
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

        if (!string.IsNullOrWhiteSpace(testCase.ExpectedArea) && !string.Equals(diagnostics.AreaKey, testCase.ExpectedArea, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Expected area {testCase.ExpectedArea}, got {diagnostics.AreaKey}."));
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

        foreach (var expected in testCase.ExpectedIntentMinimums)
        {
            var actual = SceneIntentValue(diagnostics.Intent, expected.Channel);
            if (actual < expected.Value)
            {
                failures.Add(new SceneTagRegressionFailure(testCase.Name, $"Expected intent {expected.Channel} >= {expected.Value:0.###}, got {actual:0.###}."));
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

    private static float SceneIntentValue(SceneIntent intent, string channel)
    {
        return channel switch
        {
            nameof(SceneIntent.Daylight) => intent.Daylight,
            nameof(SceneIntent.Sunlight) => intent.Sunlight,
            nameof(SceneIntent.OpenSkyLight) => intent.OpenSkyLight,
            nameof(SceneIntent.SurfaceHeat) => intent.SurfaceHeat,
            nameof(SceneIntent.DayAtmosphere) => intent.DayAtmosphere,
            nameof(SceneIntent.DayReflection) => intent.DayReflection,
            nameof(SceneIntent.DayHighlightPressure) => intent.DayHighlightPressure,
            _ => 0f
        };
    }
}
