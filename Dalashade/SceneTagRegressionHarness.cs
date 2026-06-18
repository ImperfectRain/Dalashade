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
        ValidateScreenshotMaterialEvidence(failures);
        ValidateScreenshotMaterialEvidenceControls(failures);
        ValidateScreenshotMaterialEvidenceIntentInfluence(failures);
        ValidateMaterialTagRegistryTuningSafety(failures);
        ValidateGeneratedPresetLoadOrderOptimization(failures);
        ValidateGeneratedPresetDalashadeTechniqueSync(failures);

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
        foreach (var required in new[] { "Profile prior", "Tag/other evidence", "Screenshot material evidence", "Final value", "suppressions/caps" })
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

    private static void ValidateGeneratedPresetLoadOrderOptimization(List<SceneTagRegressionFailure> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "DalashadeLoadOrderRegression", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var basePath = Path.Combine(root, "base.ini");
            var generatedPath = Path.Combine(root, "generated.ini");
            File.WriteAllLines(basePath, new[]
            {
                "Techniques=Dalashade_SmartSharpen@Dalashade_SmartSharpen.fx,Dalashade_SurfaceReflection@Dalashade_SurfaceReflection.fx,Dalashade_ContactTone@Dalashade_ContactTone.fx,Dalashade_AdaptiveGrade@Dalashade_AdaptiveGrade.fx,Dalashade_SceneGI@Dalashade_SceneGI.fx",
                "TechniqueSorting=Dalashade_SmartSharpen@Dalashade_SmartSharpen.fx,Dalashade_SurfaceReflection@Dalashade_SurfaceReflection.fx,Dalashade_ContactTone@Dalashade_ContactTone.fx,Dalashade_AdaptiveGrade@Dalashade_AdaptiveGrade.fx,Dalashade_SceneGI@Dalashade_SceneGI.fx",
                "[Dalashade_SmartSharpen.fx]",
                "SharpenStrength=1.000000",
                "[Dalashade_SurfaceReflection.fx]",
                "Dalashade_SurfaceReflectionStrength=0.320000",
                "[Dalashade_ContactTone.fx]",
                "Dalashade_ContactToneStrength=0.420000",
                "[Dalashade_AdaptiveGrade.fx]",
                "Dalashade_Readability=0.000000",
                "[Dalashade_SceneGI.fx]",
                "Dalashade_GIStrength=0.450000"
            });

            var result = new PresetWriter().WriteGeneratedPreset(
                new Configuration
                {
                    BasePresetPath = basePath,
                    GeneratedPresetPath = generatedPath,
                    OptimizeGeneratedPresetLoadOrder = true,
                    EnableDalashadeCustomShaders = false,
                    WriteBackups = false,
                    CompatibilityMode = PresetCompatibilityMode.AdaptiveBalanced
                },
                VisualProfile.Neutral,
                SceneIntent.Neutral,
                MaterialIntent.Neutral);
            if (!result.Success || !result.TechniqueOrderOptimization.Changed)
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset load-order optimization", $"Expected successful load-order change, got: {result.Message}"));
                return;
            }

            var generated = File.ReadAllLines(generatedPath);
            var techniques = generated.FirstOrDefault(line => line.StartsWith("Techniques=", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            var entries = techniques[(techniques.IndexOf('=') + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (entries.Length != 5)
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset load-order optimization", $"Expected 5 preserved technique entries, got {entries.Length}."));
            }

            var adaptive = Array.FindIndex(entries, entry => entry.Contains("Dalashade_AdaptiveGrade", StringComparison.OrdinalIgnoreCase));
            var sceneGi = Array.FindIndex(entries, entry => entry.Contains("Dalashade_SceneGI", StringComparison.OrdinalIgnoreCase));
            var contactTone = Array.FindIndex(entries, entry => entry.Contains("Dalashade_ContactTone", StringComparison.OrdinalIgnoreCase));
            var reflection = Array.FindIndex(entries, entry => entry.Contains("Dalashade_SurfaceReflection", StringComparison.OrdinalIgnoreCase));
            var sharpen = Array.FindIndex(entries, entry => entry.Contains("Dalashade_SmartSharpen", StringComparison.OrdinalIgnoreCase));
            if (!(adaptive >= 0 && sceneGi > adaptive && contactTone > sceneGi && reflection > contactTone && sharpen > reflection))
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset load-order optimization", $"Expected AdaptiveGrade -> SceneGI -> ContactTone -> SurfaceReflection -> SmartSharpen order, got: {techniques}"));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            failures.Add(new SceneTagRegressionFailure("Generated preset load-order optimization", $"Load-order validation failed: {ex.Message}"));
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

    private static void ValidateScreenshotMaterialEvidence(List<SceneTagRegressionFailure> failures)
    {
        var context = new GameContext(
            7777,
            "Material Evidence Regression Field",
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
        var tags = SceneClassifier.Classify(context) with
        {
            IsFieldLike = true,
            BiomeKey = "unknown",
            MoodTags = Array.Empty<string>()
        };

        var missing = ScreenshotMaterialEvidenceAnalyzer.BuildDiagnostics(ImageAnalysisResult.Empty, tags, context, MaterialIntent.Neutral);
        if (missing.Evidence.Confidence != 0f || missing.Evidence.Evidence.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence missing image", "Missing screenshot did not produce neutral evidence with a diagnostic reason."));
        }

        var greenRegions = Enum.GetValues<ImageAnalysisRegion>()
            .ToDictionary(
                region => region,
                region => new ImageRegionStats(
                    region,
                    region == ImageAnalysisRegion.UpperThird ? 0.58f : 0.42f,
                    region == ImageAnalysisRegion.UpperThird ? 0.12f : 0.42f,
                    region == ImageAnalysisRegion.UpperThird ? 0.18f : 0.54f,
                    region == ImageAnalysisRegion.UpperThird ? 0.35f : 0.20f,
                    0.08f,
                    region == ImageAnalysisRegion.UpperThird ? 0.78f : 0.22f,
                    new Dictionary<ColorFamily, ColorFamilyStats>
                    {
                        [ColorFamily.Green] = new(ColorFamily.Green, 0.33f, region == ImageAnalysisRegion.UpperThird ? 0.16f : 0.62f, 0.42f, region == ImageAnalysisRegion.UpperThird ? 0.03f : 0.35f, region == ImageAnalysisRegion.UpperThird ? 0.05f : 0.55f),
                        [ColorFamily.Blue] = new(ColorFamily.Blue, 0.62f, 0.20f, 0.58f, region == ImageAnalysisRegion.UpperThird ? 0.25f : 0.04f, region == ImageAnalysisRegion.UpperThird ? 0.38f : 0.03f)
                    }));
        var image = new ImageAnalysisResult(
            true,
            "synthetic-foliage.png",
            DateTimeOffset.UtcNow,
            0.42f,
            0.38f,
            0.48f,
            0.01f,
            0.01f,
            -0.04f,
            0.30f,
            0.08f,
            0.22f,
            0.42f,
            0.64f,
            0.88f,
            TonalColorBias.Empty,
            TonalColorBias.Empty,
            TonalColorBias.Empty,
            ColorFamilyStats.EmptyMap,
            greenRegions,
            new[] { new ImageSceneOpinion(ImageSceneOpinionKeys.Foliage, "Foliage", 0.36f, "MaterialIntent/Foliage", "Synthetic regression foliage opinion.") });
        var diagnostics = ScreenshotMaterialEvidenceAnalyzer.BuildDiagnostics(image, tags, context, MaterialIntent.Neutral);
        if (diagnostics.Evidence.FoliageVisible < 0.42f || diagnostics.Evidence.GrassTerrainVisible < 0.35f)
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence foliage", $"Expected foliage/grass evidence from lower green textured regions, got foliage={diagnostics.Evidence.FoliageVisible:0.###}, grass={diagnostics.Evidence.GrassTerrainVisible:0.###}."));
        }

        if (!diagnostics.Mismatches.Any(mismatch => string.Equals(mismatch.Channel, MaterialIntent.FoliageChannel, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence mismatch", "Expected high visible foliage with neutral MaterialIntent to produce a foliage mismatch warning."));
        }

        var modestFoliageIntent = new MaterialIntent(
            0.20f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            Array.Empty<MaterialIntentContribution>());
        var modestFoliageDiagnostics = ScreenshotMaterialEvidenceAnalyzer.BuildDiagnostics(image, tags, context, modestFoliageIntent);
        if (!modestFoliageDiagnostics.Mismatches.Any(mismatch =>
                string.Equals(mismatch.Channel, MaterialIntent.FoliageChannel, StringComparison.OrdinalIgnoreCase)
                && mismatch.Message.Contains("only modest", StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence modest foliage mismatch", "Expected high visible foliage with modest MaterialIntent to produce a softer foliage warning."));
        }

        var warmCharacterRegions = Enum.GetValues<ImageAnalysisRegion>()
            .ToDictionary(
                region => region,
                region => new ImageRegionStats(
                    region,
                    region == ImageAnalysisRegion.LowerThird ? 0.29f : 0.14f,
                    region == ImageAnalysisRegion.LowerThird ? 0.30f : 0.15f,
                    0.34f,
                    region == ImageAnalysisRegion.LowerThird ? 0.12f : 0.00f,
                    0.04f,
                    region == ImageAnalysisRegion.LowerThird ? 0.00f : 0.30f,
                    new Dictionary<ColorFamily, ColorFamilyStats>
                    {
                        [ColorFamily.Orange] = new(ColorFamily.Orange, 0.08f, 0.34f, 0.22f, 0.62f, region is ImageAnalysisRegion.Center or ImageAnalysisRegion.MiddleThird or ImageAnalysisRegion.LowerThird ? 0.93f : 0.70f),
                        [ColorFamily.Yellow] = new(ColorFamily.Yellow, 0.16f, 0.22f, 0.24f, 0.05f, region == ImageAnalysisRegion.LowerThird ? 0.06f : 0.02f),
                        [ColorFamily.Blue] = new(ColorFamily.Blue, 0.62f, 0.20f, 0.18f, 0.06f, 0.16f)
                    }));
        var warmCharacterImage = new ImageAnalysisResult(
            true,
            "synthetic-warm-character-dialogue.png",
            DateTimeOffset.UtcNow,
            0.18f,
            0.26f,
            0.34f,
            0.02f,
            0.01f,
            0.42f,
            0.00f,
            0.04f,
            0.11f,
            0.20f,
            0.36f,
            0.62f,
            TonalColorBias.Empty,
            TonalColorBias.Empty,
            TonalColorBias.Empty,
            ColorFamilyStats.EmptyMap,
            warmCharacterRegions,
            new[] { new ImageSceneOpinion(ImageSceneOpinionKeys.SkinProtection, "Skin protection", 0.45f, "MaterialIntent/SkinProtection", "Synthetic regression skin/character opinion.") });
        var warmCharacterDiagnostics = ScreenshotMaterialEvidenceAnalyzer.BuildDiagnostics(warmCharacterImage, tags, context, MaterialIntent.Neutral);
        if (warmCharacterDiagnostics.Evidence.SandVisible >= 0.36f
            || warmCharacterDiagnostics.Mismatches.Any(mismatch => string.Equals(mismatch.Channel, MaterialIntent.SandDustChannel, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence warm character", $"Warm character/dialogue image should not produce strong SandDust evidence, got sand={warmCharacterDiagnostics.Evidence.SandVisible:0.###}."));
        }

        var blueSkyRegions = Enum.GetValues<ImageAnalysisRegion>()
            .ToDictionary(
                region => region,
                region => new ImageRegionStats(
                    region,
                    region == ImageAnalysisRegion.UpperThird ? 0.70f : 0.36f,
                    region == ImageAnalysisRegion.UpperThird ? 0.08f : 0.18f,
                    region == ImageAnalysisRegion.UpperThird ? 0.28f : 0.18f,
                    region == ImageAnalysisRegion.UpperThird ? 0.58f : 0.12f,
                    0.02f,
                    region == ImageAnalysisRegion.UpperThird ? 0.86f : 0.24f,
                    new Dictionary<ColorFamily, ColorFamilyStats>
                    {
                        [ColorFamily.Blue] = new(ColorFamily.Blue, 0.62f, 0.30f, 0.66f, region == ImageAnalysisRegion.UpperThird ? 0.45f : 0.04f, region == ImageAnalysisRegion.UpperThird ? 0.72f : 0.05f),
                        [ColorFamily.Cyan] = new(ColorFamily.Cyan, 0.50f, 0.22f, 0.62f, region == ImageAnalysisRegion.UpperThird ? 0.25f : 0.04f, region == ImageAnalysisRegion.UpperThird ? 0.42f : 0.04f)
                    }));
        var skyImage = new ImageAnalysisResult(
            true,
            "synthetic-blue-sky.png",
            DateTimeOffset.UtcNow,
            0.54f,
            0.12f,
            0.24f,
            0.00f,
            0.01f,
            -0.12f,
            0.00f,
            0.10f,
            0.30f,
            0.52f,
            0.74f,
            0.92f,
            TonalColorBias.Empty,
            TonalColorBias.Empty,
            TonalColorBias.Empty,
            ColorFamilyStats.EmptyMap,
            blueSkyRegions,
            new[] { new ImageSceneOpinion(ImageSceneOpinionKeys.SkyAir, "Sky air", 0.52f, "SceneIntent/SkyAir", "Synthetic regression sky opinion.") });
        var skyDiagnostics = ScreenshotMaterialEvidenceAnalyzer.BuildDiagnostics(skyImage, tags, context, MaterialIntent.Neutral);
        if (skyDiagnostics.Evidence.SkyVisible < 0.48f || skyDiagnostics.Evidence.WaterVisible >= 0.28f)
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence sky water split", $"Blue sky should stay sky-dominant without strong water evidence, got sky={skyDiagnostics.Evidence.SkyVisible:0.###}, water={skyDiagnostics.Evidence.WaterVisible:0.###}."));
        }
    }

    private static void ValidateGeneratedPresetDalashadeTechniqueSync(List<SceneTagRegressionFailure> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "DalashadeTechniqueSyncRegression", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var basePath = Path.Combine(root, "base.ini");
            var generatedPath = Path.Combine(root, "generated.ini");
            File.WriteAllLines(basePath, new[]
            {
                "Techniques=ThirdPartyBloom@qUINT_bloom.fx",
                "TechniqueSorting=ThirdPartyBloom@qUINT_bloom.fx",
                "[ThirdPartyBloom.fx]",
                "BloomStrength=0.500000"
            });

            var writer = new PresetWriter();
            var enableResult = writer.WriteGeneratedPreset(
                new Configuration
                {
                    BasePresetPath = basePath,
                    GeneratedPresetPath = generatedPath,
                    EnableDalashadeCustomShaders = true,
                    AutoInjectDalashadeCustomShaderSections = true,
                    SyncDalashadeTechniqueActivation = true,
                    EnableDalashadeSceneGIShaderVariables = true,
                    EnableDalashadeContactToneShaderVariables = true,
                    EnableDalashadeSurfaceReflectionShaderVariables = true,
                    WriteBackups = false,
                    CompatibilityMode = PresetCompatibilityMode.AdaptiveBalanced
                },
                VisualProfile.Neutral,
                SceneIntent.Neutral,
                MaterialIntent.Neutral);
            if (!enableResult.Success || !enableResult.CustomShaderInjection.TechniqueInjected)
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset Dalashade technique sync", $"Expected Dalashade technique activation, got: {enableResult.Message}"));
                return;
            }

            var enabledTechniques = ReadPresetEntries(generatedPath, "Techniques");
            if (!enabledTechniques.Any(entry => entry.Contains("ThirdPartyBloom", StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset Dalashade technique sync", "Third-party active technique was removed during Dalashade activation sync."));
            }

            var adaptive = Array.FindIndex(enabledTechniques, entry => entry.Contains("Dalashade_AdaptiveGrade", StringComparison.OrdinalIgnoreCase));
            var sceneGi = Array.FindIndex(enabledTechniques, entry => entry.Contains("Dalashade_SceneGI", StringComparison.OrdinalIgnoreCase));
            var contactTone = Array.FindIndex(enabledTechniques, entry => entry.Contains("Dalashade_ContactTone", StringComparison.OrdinalIgnoreCase));
            var weather = Array.FindIndex(enabledTechniques, entry => entry.Contains("Dalashade_WeatherAtmosphere", StringComparison.OrdinalIgnoreCase));
            var bloom = Array.FindIndex(enabledTechniques, entry => entry.Contains("Dalashade_AtmosphereBloom", StringComparison.OrdinalIgnoreCase));
            var reflection = Array.FindIndex(enabledTechniques, entry => entry.Contains("Dalashade_SurfaceReflection", StringComparison.OrdinalIgnoreCase));
            var sharpen = Array.FindIndex(enabledTechniques, entry => entry.Contains("Dalashade_SmartSharpen", StringComparison.OrdinalIgnoreCase));
            if (!(adaptive >= 0 && sceneGi > adaptive && contactTone > sceneGi && weather > contactTone && bloom > weather && reflection > bloom && sharpen > reflection))
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset Dalashade technique sync", $"Expected Dalashade production technique order, got: {string.Join(",", enabledTechniques)}"));
            }

            var disableResult = writer.WriteGeneratedPreset(
                new Configuration
                {
                    BasePresetPath = generatedPath,
                    GeneratedPresetPath = Path.Combine(root, "disabled.ini"),
                    EnableDalashadeCustomShaders = false,
                    AutoInjectDalashadeCustomShaderSections = false,
                    SyncDalashadeTechniqueActivation = true,
                    WriteBackups = false,
                    CompatibilityMode = PresetCompatibilityMode.AdaptiveBalanced
                },
                VisualProfile.Neutral,
                SceneIntent.Neutral,
                MaterialIntent.Neutral);
            if (!disableResult.Success || !disableResult.CustomShaderInjection.TechniqueDeactivated)
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset Dalashade technique sync", $"Expected Dalashade technique deactivation, got: {disableResult.Message}"));
                return;
            }

            var disabledTechniques = ReadPresetEntries(Path.Combine(root, "disabled.ini"), "Techniques");
            if (disabledTechniques.Any(entry => entry.Contains("Dalashade_", StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset Dalashade technique sync", $"Expected active Dalashade techniques to be removed, got: {string.Join(",", disabledTechniques)}"));
            }

            if (!disabledTechniques.Any(entry => entry.Contains("ThirdPartyBloom", StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add(new SceneTagRegressionFailure("Generated preset Dalashade technique sync", "Third-party active technique was removed during Dalashade deactivation sync."));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            failures.Add(new SceneTagRegressionFailure("Generated preset Dalashade technique sync", $"Technique sync validation failed: {ex.Message}"));
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

    private static void ValidateScreenshotMaterialEvidenceIntentInfluence(List<SceneTagRegressionFailure> failures)
    {
        var context = new GameContext(
            8888,
            "Material Evidence Influence Field",
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
        var tags = SceneClassifier.Classify(context) with
        {
            IsFieldLike = true,
            BiomeKey = "unknown",
            MoodTags = Array.Empty<string>()
        };
        var diagnostics = TagStackDiagnostics.Create(context, tags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());
        var profile = MaterialProfileBuilder.Build(diagnostics, ImageAnalysisResult.Empty);
        var baseline = MaterialIntentBuilder.Build(diagnostics, ImageAnalysisResult.Empty, profile);
        var disabledConfig = new Configuration
        {
            EnableScreenshotMaterialEvidenceInfluence = false,
            ScreenshotMaterialEvidenceStrength = 1f
        };
        var highFoliage = Evidence(foliage: 1f, grass: 1f, confidence: 1f);
        var disabledContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(disabledConfig, diagnostics, highFoliage);
        var disabledIntent = MaterialIntentBuilder.Build(diagnostics, ImageAnalysisResult.Empty, profile, screenshotMaterialEvidenceContributions: disabledContributions);
        ExpectIntentEqual(failures, "Screenshot material evidence influence disabled", baseline, disabledIntent);

        var enabledConfig = new Configuration
        {
            EnableScreenshotMaterialEvidenceInfluence = true,
            ScreenshotMaterialEvidenceStrength = 1f
        };
        var foliageContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, diagnostics, highFoliage);
        ExpectContributionRange(failures, "Screenshot material evidence foliage cap", foliageContributions, MaterialIntent.FoliageChannel, min: 0.20f, max: 0.2201f);
        var lowConfidenceFoliage = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, diagnostics, Evidence(foliage: 1f, grass: 1f, confidence: 0.10f));
        ExpectContributionRange(failures, "Screenshot material evidence low confidence", lowConfidenceFoliage, MaterialIntent.FoliageChannel, min: 0.015f, max: 0.023f);

        var waterContextTags = tags with
        {
            BiomeKey = "coastal",
            MoodTags = new[] { "coastal", "water" }
        };
        var waterDiagnostics = TagStackDiagnostics.Create(context with { TerritoryName = "Eastern La Noscea" }, waterContextTags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());
        var waterContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, waterDiagnostics, Evidence(water: 1f, confidence: 1f));
        ExpectContributionRange(failures, "Screenshot material evidence water cap", waterContributions, MaterialIntent.WaterSpecularChannel, min: 0.15f, max: 0.1601f);

        var skyOnlyContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, diagnostics, Evidence(sky: 1f, confidence: 1f));
        if (skyOnlyContributions.Any(contribution => string.Equals(contribution.Channel, MaterialIntent.WaterSpecularChannel, StringComparison.Ordinal)))
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence sky water guard", "High sky evidence produced a WaterSpecular contribution."));
        }

        var ambiguousCyan = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, diagnostics, Evidence(water: 0.55f, aether: 0.60f, confidence: 1f));
        if (ambiguousCyan.Any(contribution => string.Equals(contribution.Channel, MaterialIntent.WaterSpecularChannel, StringComparison.Ordinal) && contribution.Amount > 0f))
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence cyan water guard", "Aether/neon ambiguity produced a positive WaterSpecular contribution."));
        }

        if (!ambiguousCyan.Any(contribution => string.Equals(contribution.Channel, MaterialIntent.WaterSpecularChannel, StringComparison.Ordinal) && contribution.Amount < 0f))
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence cyan water suppression", "Aether/neon ambiguity did not record a WaterSpecular suppression."));
        }

        var sandContextTags = tags with
        {
            BiomeKey = "desert",
            MoodTags = new[] { "desert", "sand", "dry" }
        };
        var sandDiagnostics = TagStackDiagnostics.Create(context with { TerritoryName = "Thanalan" }, sandContextTags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());
        var sandContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, sandDiagnostics, Evidence(sand: 1f, confidence: 1f));
        ExpectContributionRange(failures, "Screenshot material evidence sand cap", sandContributions, MaterialIntent.SandDustChannel, min: 0.15f, max: 0.1601f);

        var skinSandContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, diagnostics, Evidence(sand: 0.60f, skin: 0.90f, confidence: 1f));
        if (skinSandContributions.Any(contribution => string.Equals(contribution.Channel, MaterialIntent.SandDustChannel, StringComparison.Ordinal) && contribution.Amount > 0f))
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence skin sand guard", "High skin/character evidence produced a positive SandDust contribution."));
        }

        var snowContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, diagnostics, Evidence(snow: 1f, confidence: 1f));
        ExpectContributionRange(failures, "Screenshot material evidence snow cap", snowContributions, MaterialIntent.SnowIceChannel, min: 0.09f, max: 0.1001f);

        var aetherContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, diagnostics, Evidence(aether: 1f, confidence: 1f));
        ExpectContributionRange(failures, "Screenshot material evidence aether cap", aetherContributions, MaterialIntent.CrystalAetherChannel, min: 0.07f, max: 0.0801f);
        ExpectContributionRange(failures, "Screenshot material evidence neon cap", aetherContributions, MaterialIntent.NeonGlassChannel, min: 0.07f, max: 0.0801f);
        if (aetherContributions.Any(contribution => string.Equals(contribution.Channel, MaterialIntent.WaterSpecularChannel, StringComparison.Ordinal)))
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence aether water guard", "Aether/neon evidence produced a WaterSpecular contribution."));
        }
    }

    private static void ValidateScreenshotMaterialEvidenceControls(List<SceneTagRegressionFailure> failures)
    {
        var defaults = new Configuration();
        if (defaults.EnableScreenshotMaterialEvidenceInfluence)
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence controls default", "Screenshot material evidence influence must be disabled by default."));
        }

        if (MathF.Abs(defaults.ScreenshotMaterialEvidenceStrength - 0.35f) > 0.001f)
        {
            failures.Add(new SceneTagRegressionFailure("Screenshot material evidence controls default", $"Expected default strength 0.35, got {defaults.ScreenshotMaterialEvidenceStrength:0.###}."));
        }

        var context = new GameContext(
            8889,
            "Material Evidence Control Field",
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
        var tags = SceneClassifier.Classify(context) with
        {
            IsFieldLike = true,
            BiomeKey = "unknown",
            MoodTags = Array.Empty<string>()
        };
        var diagnostics = TagStackDiagnostics.Create(context, tags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());
        var profile = MaterialProfileBuilder.Build(diagnostics, ImageAnalysisResult.Empty);
        var baseline = MaterialIntentBuilder.Build(diagnostics, ImageAnalysisResult.Empty, profile);
        var evidence = Evidence(foliage: 1f, grass: 1f, confidence: 1f);
        var disabledConfig = new Configuration
        {
            EnableScreenshotMaterialEvidenceInfluence = false,
            ScreenshotMaterialEvidenceStrength = 0.6f
        };
        var disabledContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(disabledConfig, diagnostics, evidence);
        var disabledIntent = MaterialIntentBuilder.Build(diagnostics, ImageAnalysisResult.Empty, profile, screenshotMaterialEvidenceContributions: disabledContributions);
        ExpectIntentEqual(failures, "Screenshot material evidence controls disabled parity", baseline, disabledIntent);

        var enabledConfig = new Configuration
        {
            EnableScreenshotMaterialEvidenceInfluence = true,
            ScreenshotMaterialEvidenceStrength = 0.6f
        };
        var enabledContributions = ScreenshotMaterialEvidenceIntentAdapter.BuildContributions(enabledConfig, diagnostics, evidence);
        ExpectContributionRange(failures, "Screenshot material evidence controls enabled cap", enabledContributions, MaterialIntent.FoliageChannel, min: 0.13f, max: 0.133f);
    }

    private static void ValidateMaterialTagRegistryTuningSafety(List<SceneTagRegressionFailure> failures)
    {
        var context = new GameContext(
            8890,
            "Registry Material Tuning Field",
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
        var tags = SceneClassifier.Classify(context) with
        {
            IsFieldLike = true,
            BiomeKey = "unknown",
            MoodTags = new[] { "customFoliage" }
        };
        var diagnostics = TagStackDiagnostics.Create(context, tags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());
        var profile = MaterialProfileBuilder.Build(diagnostics, ImageAnalysisResult.Empty);
        var baseline = MaterialIntentBuilder.Build(diagnostics, ImageAnalysisResult.Empty, profile);

        var registryDisabled = MaterialIntentBuilder.Build(diagnostics, ImageAnalysisResult.Empty, profile, tagRegistry: null);
        ExpectIntentEqual(failures, "Material tag registry disabled parity", baseline, registryDisabled);

        var invalidChannelRegistry = RegistryPreset("customFoliage", SceneAuthoringService.MoodCategory, new SceneTagTuning
        {
            Target = SceneTagTuningTargets.MaterialIntent,
            Channel = "NotAChannel",
            Amount = 0.10f,
            Reason = "Invalid channel regression."
        });
        var invalidResult = MaterialTagRegistryTuningAnalyzer.Build(diagnostics, invalidChannelRegistry);
        if (invalidResult.Contributions.Count != 0 || invalidResult.Diagnostics.InvalidTunings.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure("Material tag registry invalid channel", "Invalid MaterialIntent channel was not ignored and reported."));
        }

        var invalidAmountRegistry = RegistryPreset("customFoliage", SceneAuthoringService.MoodCategory, new SceneTagTuning
        {
            Target = SceneTagTuningTargets.MaterialIntent,
            Channel = MaterialIntent.FoliageChannel,
            Amount = 1.50f,
            Reason = "Invalid amount regression."
        });
        var invalidAmountResult = MaterialTagRegistryTuningAnalyzer.Build(diagnostics, invalidAmountRegistry);
        if (invalidAmountResult.Contributions.Count != 0 || invalidAmountResult.Diagnostics.InvalidTunings.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure("Material tag registry invalid amount", "Out-of-range material tuning amount was not ignored and reported."));
        }

        var disabledRegistry = RegistryPreset("customFoliage", SceneAuthoringService.MoodCategory, new SceneTagTuning
        {
            Enabled = false,
            Target = SceneTagTuningTargets.MaterialIntent,
            Channel = MaterialIntent.FoliageChannel,
            Amount = 0.10f,
            Reason = "Disabled regression."
        });
        var disabledResult = MaterialTagRegistryTuningAnalyzer.Build(diagnostics, disabledRegistry);
        if (disabledResult.Contributions.Count != 0 || disabledResult.Diagnostics.InactiveTunings.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure("Material tag registry disabled tuning", "Disabled material tuning was not ignored and reported inactive."));
        }

        var perTagCap = RegistryPreset("customFoliage", SceneAuthoringService.MoodCategory, new SceneTagTuning
        {
            Target = SceneTagTuningTargets.MaterialIntent,
            Channel = MaterialIntent.FoliageChannel,
            Amount = 0.80f,
            Reason = "Per-tag cap regression."
        });
        var perTagResult = MaterialTagRegistryTuningAnalyzer.Build(diagnostics, perTagCap);
        ExpectContributionRange(failures, "Material tag registry per-tag cap", perTagResult.Contributions, MaterialIntent.FoliageChannel, min: 0.199f, max: 0.2001f);
        if (perTagResult.Diagnostics.CappedTunings.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure("Material tag registry per-tag cap diagnostics", "Per-tag cap was not reported."));
        }

        var perChannelCap = RegistryPreset(
            "customFoliage",
            SceneAuthoringService.MoodCategory,
            new SceneTagTuning { Target = SceneTagTuningTargets.MaterialIntent, Channel = MaterialIntent.FoliageChannel, Amount = 0.20f, Reason = "First stack." },
            new SceneTagTuning { Target = SceneTagTuningTargets.MaterialIntent, Channel = MaterialIntent.FoliageChannel, Amount = 0.20f, Reason = "Second stack." },
            new SceneTagTuning { Target = SceneTagTuningTargets.MaterialIntent, Channel = MaterialIntent.FoliageChannel, Amount = 0.20f, Reason = "Third stack." });
        var perChannelResult = MaterialTagRegistryTuningAnalyzer.Build(diagnostics, perChannelCap);
        var totalFoliage = perChannelResult.Contributions
            .Where(contribution => string.Equals(contribution.Channel, MaterialIntent.FoliageChannel, StringComparison.Ordinal))
            .Sum(contribution => contribution.Amount);
        if (MathF.Abs(totalFoliage - MaterialTagRegistryTuningAnalyzer.PerChannelContributionCap) > 0.001f)
        {
            failures.Add(new SceneTagRegressionFailure("Material tag registry per-channel cap", $"Expected channel cap {MaterialTagRegistryTuningAnalyzer.PerChannelContributionCap:0.###}, got {totalFoliage:0.###}."));
        }

        var inactiveTagRegistry = RegistryPreset("notActiveHere", SceneAuthoringService.MoodCategory, new SceneTagTuning
        {
            Target = SceneTagTuningTargets.MaterialIntent,
            Channel = MaterialIntent.FoliageChannel,
            Amount = 0.10f,
            Reason = "Inactive tag regression."
        });
        var inactiveTagResult = MaterialTagRegistryTuningAnalyzer.Build(diagnostics, inactiveTagRegistry);
        if (inactiveTagResult.Contributions.Count != 0 || inactiveTagResult.Diagnostics.InactiveTunings.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure("Material tag registry inactive tag", "Inactive tag tuning was not ignored and reported inactive."));
        }

        var activeCustomRegistry = RegistryPreset("customFoliage", SceneAuthoringService.MoodCategory, new SceneTagTuning
        {
            Target = SceneTagTuningTargets.MaterialIntent,
            Channel = MaterialIntent.FoliageChannel,
            Amount = 0.12f,
            Reason = "Active custom tag regression."
        });
        var activeCustomResult = MaterialTagRegistryTuningAnalyzer.Build(diagnostics, activeCustomRegistry);
        ExpectContributionRange(failures, "Material tag registry active custom tag", activeCustomResult.Contributions, MaterialIntent.FoliageChannel, min: 0.119f, max: 0.1201f);

        var removedTags = tags with
        {
            MoodTags = Array.Empty<string>()
        };
        var removedDiagnostics = TagStackDiagnostics.Create(context, removedTags, SceneIntent.Neutral, Array.Empty<TagStackContribution>());
        var removedTagResult = MaterialTagRegistryTuningAnalyzer.Build(removedDiagnostics, activeCustomRegistry);
        if (removedTagResult.Contributions.Count != 0 || removedTagResult.Diagnostics.InactiveTunings.Count == 0)
        {
            failures.Add(new SceneTagRegressionFailure("Material tag registry removed tag", "Removed/inactive tag still applied a material tuning."));
        }
    }

    private static IReadOnlyList<SceneTagPreset> RegistryPreset(string tag, string category, params SceneTagTuning[] tunings)
    {
        return
        [
            new SceneTagPreset
            {
                Tag = tag,
                DisplayName = tag,
                Description = "Regression registry preset.",
                Categories = [category],
                Tunings = tunings.ToList()
            }
        ];
    }

    private static ScreenshotMaterialEvidence Evidence(
        float foliage = 0f,
        float grass = 0f,
        float water = 0f,
        float sand = 0f,
        float snow = 0f,
        float stone = 0f,
        float metal = 0f,
        float sky = 0f,
        float aether = 0f,
        float skin = 0f,
        float confidence = 1f)
    {
        return new ScreenshotMaterialEvidence(
            foliage,
            grass,
            water,
            sand,
            snow,
            stone,
            metal,
            sky,
            aether,
            skin,
            confidence,
            Array.Empty<string>());
    }

    private static void ExpectContributionRange(List<SceneTagRegressionFailure> failures, string caseName, IReadOnlyList<MaterialIntentContribution> contributions, string channel, float min, float max)
    {
        var value = contributions
            .Where(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal) && contribution.Amount > 0f)
            .Sum(contribution => contribution.Amount);
        if (value < min || value > max)
        {
            failures.Add(new SceneTagRegressionFailure(caseName, $"Expected {channel} contribution in [{min:0.###}, {max:0.###}], got {value:0.###}."));
        }
    }

    private static void ExpectIntentEqual(List<SceneTagRegressionFailure> failures, string caseName, MaterialIntent expected, MaterialIntent actual)
    {
        foreach (var channel in MaterialIntent.ChannelNames)
        {
            if (MathF.Abs(expected.ValueFor(channel) - actual.ValueFor(channel)) > 0.0001f)
            {
                failures.Add(new SceneTagRegressionFailure(caseName, $"Expected disabled screenshot evidence to preserve {channel}; before={expected.ValueFor(channel):0.###}, after={actual.ValueFor(channel):0.###}."));
            }
        }
    }

    private static string[] ReadPresetEntries(string presetPath, string key)
    {
        var line = File.ReadAllLines(presetPath).FirstOrDefault(candidate => candidate.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        var separatorIndex = line.IndexOf('=');
        if (separatorIndex < 0)
        {
            return Array.Empty<string>();
        }

        return line[(separatorIndex + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
            Create("Labyrinthos exact research profile", "Labyrinthos", "Clear Skies", false, false, false, false, TimeBucket.Day, PerformanceBudget.Medium, expectedBiome: "aetherial", expectedArea: "field", expectedMoodTags: new[] { "artificial", "greenhouse", "scholarly" }, expectedMaterialMaximums: Materials((MaterialIntent.WaterSpecularChannel, 0.18f))),
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
            failures.Add(new SceneTagRegressionFailure(testCase.Name, "Material report shape missing tag/other evidence contribution."));
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
