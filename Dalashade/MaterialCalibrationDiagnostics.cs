using System;
using System.Collections.Generic;
using System.Linq;
using Dalashade.SceneAuthoring;

namespace Dalashade;

public sealed record MaterialCalibrationChannelDiagnostic(
    string Channel,
    float ProfilePrior,
    float TagRegistryContribution,
    float ScreenshotEvidence,
    float MaterialIntent,
    bool ShaderMappingEnabled,
    bool ShaderMappingAvailable,
    IReadOnlyList<string> ShaderKeys,
    IReadOnlyList<string> ShaderSections,
    IReadOnlyList<MaterialCalibrationWarning> Warnings);

public sealed record MaterialCalibrationWarning(
    string Severity,
    string Message);

public sealed record MaterialCalibrationDiagnostics(
    IReadOnlyList<MaterialCalibrationChannelDiagnostic> Channels,
    IReadOnlyList<string> SceneMatrix);

public static class MaterialCalibrationDiagnosticsBuilder
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string High = "High";

    private static readonly IReadOnlyDictionary<string, string[]> ChannelShaderKeys = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [MaterialIntent.FoliageChannel] = ["Dalashade_MaterialFoliage"],
        [MaterialIntent.WaterSpecularChannel] =
        [
            "Dalashade_MaterialWaterSpecular",
            "Dalashade_MaterialWaterPlane",
            "Dalashade_MaterialSpecularGlint",
            "Dalashade_WaterContext",
            "Dalashade_CoastalContext",
            "Dalashade_OpenOceanContext",
            "Dalashade_ShallowWaterContext",
            "Dalashade_WetSurfaceContext"
        ],
        [MaterialIntent.SandDustChannel] = ["Dalashade_MaterialSandDust"],
        [MaterialIntent.SnowIceChannel] = ["Dalashade_MaterialSnowIce"],
        [MaterialIntent.StoneRuinsChannel] = ["Dalashade_MaterialStoneRuins"],
        [MaterialIntent.MetalIndustrialChannel] = ["Dalashade_MaterialMetalIndustrial"],
        [MaterialIntent.CrystalAetherChannel] = ["Dalashade_MaterialCrystalAether"],
        [MaterialIntent.NeonGlassChannel] = ["Dalashade_MaterialNeonGlass"],
        [MaterialIntent.FireLavaHeatChannel] = ["Dalashade_MaterialFireLavaHeat"],
        [MaterialIntent.SkyCloudFogChannel] = ["Dalashade_MaterialSkyCloudFog"],
        [MaterialIntent.SkinProtectionChannel] = ["Dalashade_MaterialSkinProtection"],
        [MaterialIntent.VoidDarknessChannel] = ["Dalashade_MaterialVoidDarkness"]
    };

    private static readonly IReadOnlyList<string> SceneMatrixRows =
    [
        "forest/canopy: foliage high, sky moderate, water variable",
        "coastal: water/sand/sky variable by view",
        "snow: snow/sky/stone variable",
        "desert: sand/stone/sky high",
        "high-tech/aether: metal/aether/neon high, water only if actual water evidence",
        "dungeon/interior: stone/metal/interior high, sky low",
        "combat/UI-heavy: confidence lower, skin/character/UI risk higher"
    ];

    public static MaterialCalibrationDiagnostics Build(
        Configuration configuration,
        TagStackDiagnostics tagStackDiagnostics,
        MaterialProfile profile,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence,
        MaterialIntent materialIntent,
        IReadOnlyList<SceneTagPreset>? activeTagRegistry,
        ShaderSupportScan shaderSupport,
        PresetWriteResult writeResult)
    {
        var registry = MaterialTagRegistryTuningAnalyzer.Build(tagStackDiagnostics, configuration.EnableSceneAuthoringOverrides ? activeTagRegistry : null);
        var mappingEnabled = configuration.EnableMaterialIntent
                             && configuration.EnableMaterialIntentShaderMapping
                             && configuration.MaterialIntentStrength > 0f;
        var channels = MaterialIntent.ChannelNames
            .Where(ChannelShaderKeys.ContainsKey)
            .Select(channel => BuildChannel(configuration, tagStackDiagnostics, profile, screenshotMaterialEvidence, materialIntent, registry.Diagnostics, shaderSupport, writeResult, mappingEnabled, channel))
            .ToArray();

        return new MaterialCalibrationDiagnostics(channels, SceneMatrixRows);
    }

    private static MaterialCalibrationChannelDiagnostic BuildChannel(
        Configuration configuration,
        TagStackDiagnostics tagStackDiagnostics,
        MaterialProfile profile,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence,
        MaterialIntent materialIntent,
        MaterialTagRegistryDiagnostics registryDiagnostics,
        ShaderSupportScan shaderSupport,
        PresetWriteResult writeResult,
        bool mappingEnabled,
        string channel)
    {
        var keys = ChannelShaderKeys[channel];
        var sections = FindShaderSections(shaderSupport, writeResult, keys);
        var shaderMappingAvailable = sections.Count > 0
                                     || writeResult.CustomShaderInjection.Variables.Any(variable => keys.Contains(variable, StringComparer.OrdinalIgnoreCase));
        var screenshotEvidenceValue = ScreenshotEvidenceFor(channel, screenshotMaterialEvidence.Evidence);
        var warnings = BuildWarnings(configuration, tagStackDiagnostics, profile, screenshotMaterialEvidence, materialIntent, channel, shaderMappingAvailable).ToArray();

        return new MaterialCalibrationChannelDiagnostic(
            channel,
            profile.ValueFor(channel),
            registryDiagnostics.Channels.FirstOrDefault(item => string.Equals(item.Channel, channel, StringComparison.Ordinal))?.FinalContribution ?? 0f,
            screenshotEvidenceValue,
            materialIntent.ValueFor(channel),
            mappingEnabled,
            shaderMappingAvailable,
            keys,
            sections,
            warnings);
    }

    private static IReadOnlyList<string> FindShaderSections(ShaderSupportScan shaderSupport, PresetWriteResult writeResult, IReadOnlyList<string> keys)
    {
        return shaderSupport.Items
            .Where(item => keys.Contains(item.Key, StringComparer.OrdinalIgnoreCase)
                           && string.Equals(item.ReasonCategory, CustomShaderVariableMapper.MaterialReasonCategory, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Section)
            .Concat(writeResult.Changes
                .Where(change => keys.Contains(change.Key, StringComparer.OrdinalIgnoreCase)
                                 && string.Equals(change.ReasonCategory, CustomShaderVariableMapper.MaterialReasonCategory, StringComparison.OrdinalIgnoreCase))
                .Select(change => change.Section))
            .Where(section => !string.IsNullOrWhiteSpace(section))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(section => section, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<MaterialCalibrationWarning> BuildWarnings(
        Configuration configuration,
        TagStackDiagnostics tagStackDiagnostics,
        MaterialProfile profile,
        ScreenshotMaterialEvidenceDiagnostics screenshotMaterialEvidence,
        MaterialIntent materialIntent,
        string channel,
        bool shaderMappingAvailable)
    {
        var evidence = screenshotMaterialEvidence.Evidence;
        var intent = materialIntent.ValueFor(channel);
        var profilePrior = profile.ValueFor(channel);
        var screenshot = ScreenshotEvidenceFor(channel, evidence);

        foreach (var mismatch in screenshotMaterialEvidence.Mismatches.Where(mismatch => AppliesToChannel(mismatch.Channel, channel)))
        {
            yield return new MaterialCalibrationWarning(mismatch.Severity >= 0.55f ? High : Warning, mismatch.Message);
        }

        if (channel == MaterialIntent.FoliageChannel && MathF.Max(evidence.FoliageVisible, evidence.GrassTerrainVisible) >= 0.55f && intent < 0.18f)
        {
            yield return new MaterialCalibrationWarning(High, "High visible foliage/grass evidence but Foliage MaterialIntent is low.");
        }

        if (channel == MaterialIntent.WaterSpecularChannel)
        {
            var waterContext = HasAny(tagStackDiagnostics, "water", "wet", "rain", "coastal", "seaside", "beach", "tropical", "underwater")
                               || tagStackDiagnostics.BiomeKey is "coastal" or "tropical" or "underwater"
                               || ContainsTerritoryKeyword(tagStackDiagnostics, "sea", "coast", "beach", "costa", "la noscea", "ruby sea");
            if (evidence.WaterVisible >= 0.45f && intent < 0.18f)
            {
                yield return new MaterialCalibrationWarning(waterContext ? High : Warning, waterContext
                    ? "High visible water evidence in water/coastal context but WaterSpecular MaterialIntent is low."
                    : "High visible water evidence but WaterSpecular MaterialIntent is low; confirm this is not sky or cyan lighting.");
            }

            if (intent >= 0.32f && evidence.AetherOrNeonVisible >= 0.34f && evidence.WaterVisible >= 0.28f)
            {
                yield return new MaterialCalibrationWarning(Warning, "Water intent is high while cyan/aether ambiguity is high; inspect water versus aether/neon classification.");
            }
        }

        if (channel == MaterialIntent.SkyCloudFogChannel && evidence.SkyVisible >= 0.48f)
        {
            var receiverRisk = MathF.Max(materialIntent.WaterSpecular, MathF.Max(materialIntent.StoneRuins, materialIntent.MetalIndustrial));
            if (receiverRisk >= 0.32f)
            {
                yield return new MaterialCalibrationWarning(Warning, "High sky evidence with reflection/AO receiver risk; inspect sky safety and receiver gating.");
            }
        }

        if (channel == MaterialIntent.SnowIceChannel && profilePrior >= 0.45f && evidence.SnowVisible < 0.16f)
        {
            yield return new MaterialCalibrationWarning(Warning, "Snow profile prior is high but screenshot snow evidence is low.");
        }

        if (channel == MaterialIntent.SandDustChannel && evidence.SandVisible >= 0.34f && evidence.SkinOrCharacterVisible >= 0.45f)
        {
            yield return new MaterialCalibrationWarning(Warning, "Sand evidence is high while skin/character evidence is also high; warm-tone ambiguity may be present.");
        }

        if ((channel == MaterialIntent.StoneRuinsChannel || channel == MaterialIntent.MetalIndustrialChannel)
            && screenshot >= 0.45f
            && intent < 0.18f)
        {
            yield return new MaterialCalibrationWarning(Warning, $"{channel} screenshot evidence is high but intent is low; hard-surface scene support may be underrepresented.");
        }

        if ((channel == MaterialIntent.StoneRuinsChannel || channel == MaterialIntent.MetalIndustrialChannel)
            && screenshot >= 0.45f
            && !shaderMappingAvailable
            && configuration.EnableMaterialIntentShaderMapping)
        {
            yield return new MaterialCalibrationWarning(Info, $"{channel} evidence is high, but no matching material shader key was detected in the current preset scan.");
        }

        if (!configuration.EnableMaterialIntentShaderMapping && intent >= 0.25f)
        {
            yield return new MaterialCalibrationWarning(Info, "MaterialIntent is present for diagnostics, but shader mapping is disabled.");
        }
    }

    private static bool AppliesToChannel(string mismatchChannel, string channel)
    {
        return string.Equals(mismatchChannel, channel, StringComparison.OrdinalIgnoreCase)
               || (string.Equals(mismatchChannel, "CyanAmbiguity", StringComparison.OrdinalIgnoreCase)
                   && (channel == MaterialIntent.WaterSpecularChannel || channel == MaterialIntent.CrystalAetherChannel || channel == MaterialIntent.NeonGlassChannel))
               || (string.Equals(mismatchChannel, "SkySafety", StringComparison.OrdinalIgnoreCase)
                   && (channel == MaterialIntent.SkyCloudFogChannel || channel == MaterialIntent.WaterSpecularChannel || channel == MaterialIntent.StoneRuinsChannel || channel == MaterialIntent.MetalIndustrialChannel));
    }

    private static float ScreenshotEvidenceFor(string channel, ScreenshotMaterialEvidence evidence)
    {
        return channel switch
        {
            MaterialIntent.FoliageChannel => MathF.Max(evidence.FoliageVisible, evidence.GrassTerrainVisible),
            MaterialIntent.WaterSpecularChannel => evidence.WaterVisible,
            MaterialIntent.SandDustChannel => evidence.SandVisible,
            MaterialIntent.SnowIceChannel => evidence.SnowVisible,
            MaterialIntent.StoneRuinsChannel => evidence.StoneVisible,
            MaterialIntent.MetalIndustrialChannel => evidence.MetalVisible,
            MaterialIntent.CrystalAetherChannel => evidence.AetherOrNeonVisible,
            MaterialIntent.NeonGlassChannel => evidence.AetherOrNeonVisible,
            MaterialIntent.FireLavaHeatChannel => 0f,
            MaterialIntent.SkyCloudFogChannel => evidence.SkyVisible,
            MaterialIntent.SkinProtectionChannel => evidence.SkinOrCharacterVisible,
            MaterialIntent.VoidDarknessChannel => 0f,
            _ => 0f
        };
    }

    private static bool HasAny(TagStackDiagnostics diagnostics, params string[] candidates)
    {
        var tags = diagnostics.ActiveTags
            .Concat(diagnostics.ActiveWeatherTags)
            .Concat(diagnostics.SecondaryTags)
            .Concat(diagnostics.MoodTags)
            .Concat(diagnostics.MaterialTags)
            .Concat(diagnostics.AreaContextTags)
            .Concat(diagnostics.GameplayStateTags)
            .Concat(diagnostics.ArtDirectionTags)
            .Concat([diagnostics.BiomeKey, diagnostics.WeatherKey, diagnostics.AreaKey])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return candidates.Any(tags.Contains);
    }

    private static bool ContainsTerritoryKeyword(TagStackDiagnostics diagnostics, params string[] keywords)
    {
        var searchableText = string.Join(
            " ",
            diagnostics.TerritoryName,
            diagnostics.WeatherName,
            diagnostics.BiomeKey,
            diagnostics.BiomeReason,
            diagnostics.AreaKey);
        var normalizedText = $" {NormalizeSearchableText(searchableText)} ";
        var tokens = normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var keyword in keywords)
        {
            var normalized = NormalizeSearchableText(keyword);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (normalized.Contains(' ', StringComparison.Ordinal))
            {
                if (normalizedText.Contains($" {normalized} ", StringComparison.Ordinal))
                {
                    return true;
                }

                continue;
            }

            if (tokens.Contains(normalized))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeSearchableText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var chars = text.ToLowerInvariant().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
            {
                chars[i] = ' ';
            }
        }

        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
