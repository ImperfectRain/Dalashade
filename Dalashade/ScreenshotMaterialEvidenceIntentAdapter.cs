using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public static class ScreenshotMaterialEvidenceIntentAdapter
{
    public static IReadOnlyList<MaterialIntentContribution> BuildContributions(
        Configuration configuration,
        TagStackDiagnostics diagnostics,
        ScreenshotMaterialEvidence evidence)
    {
        if (!configuration.EnableScreenshotMaterialEvidenceInfluence
            || configuration.ScreenshotMaterialEvidenceStrength <= 0f
            || evidence.Confidence <= 0f)
        {
            return Array.Empty<MaterialIntentContribution>();
        }

        var context = new Context(diagnostics);
        var strength = Clamp(configuration.ScreenshotMaterialEvidenceStrength, 0f, 1f) * Clamp01(evidence.Confidence);
        var contributions = new List<MaterialIntentContribution>();

        var foliageVisible = MathF.Max(evidence.FoliageVisible, evidence.GrassTerrainVisible);
        if (foliageVisible >= 0.30f)
        {
            var cap = 0.22f;
            if ((context.HasAny("desert", "snow", "ice", "interior", "dungeon", "highTech", "imperial") || context.AreaKey is "city" or "interior" or "dungeon")
                && foliageVisible < 0.70f)
            {
                cap *= 0.55f;
            }

            Add(contributions, MaterialIntent.FoliageChannel, foliageVisible, cap, strength, "Screenshot evidence: visible foliage", "Visible foliage/grass scene evidence raises Foliage only as a capped scene prior.");
        }

        if (evidence.WaterVisible >= 0.30f)
        {
            var waterContext = context.HasAny("water", "wet", "rain", "coastal", "seaside", "beach", "tropical", "underwater")
                               || context.BiomeKey is "coastal" or "tropical" or "underwater"
                               || context.ContainsAny("sea", "coast", "beach", "costa", "la noscea", "ruby sea");
            var cyanAmbiguous = evidence.AetherOrNeonVisible >= 0.34f && evidence.AetherOrNeonVisible >= evidence.WaterVisible * 0.85f;

            if ((waterContext || evidence.WaterVisible >= 0.48f) && !cyanAmbiguous)
            {
                Add(contributions, MaterialIntent.WaterSpecularChannel, evidence.WaterVisible, waterContext ? 0.16f : 0.09f, strength, "Screenshot evidence: visible water", "Lower-screen water evidence raises WaterSpecular only as a capped scene prior.");
            }
            else if (cyanAmbiguous)
            {
                AddSuppression(contributions, MaterialIntent.WaterSpecularChannel, MathF.Min(0.06f, evidence.WaterVisible * 0.08f * strength), "Screenshot evidence: cyan ambiguity suppressed water", "Cyan/blue evidence overlaps aether/neon, so screenshot evidence dampens water instead of raising it.");
            }
        }

        if (evidence.SandVisible >= 0.34f)
        {
            var sandContext = context.HasAny("desert", "badlands", "dry", "dust", "sand", "beach", "coastal", "heat")
                              || context.BiomeKey is "desert" or "wasteland" or "coastal" or "tropical";
            if (sandContext || evidence.SkinOrCharacterVisible < 0.45f)
            {
                Add(contributions, MaterialIntent.SandDustChannel, evidence.SandVisible, sandContext ? 0.16f : 0.08f, strength, "Screenshot evidence: visible sand", "Warm lower terrain evidence raises SandDust only as a capped scene prior.");
            }
        }

        if (evidence.SkinOrCharacterVisible >= 0.45f && evidence.SandVisible >= 0.28f && !context.HasAny("desert", "sand", "beach", "coastal"))
        {
            AddSuppression(contributions, MaterialIntent.SandDustChannel, MathF.Min(0.08f, evidence.SkinOrCharacterVisible * 0.08f * strength), "Screenshot evidence: skin suppressed sand", "Center-heavy skin/character evidence dampens warm-tone sand interpretation.");
        }

        if (evidence.SnowVisible >= 0.34f)
        {
            var snowContext = context.HasAny("snow", "ice", "cold", "alpine")
                              || context.BiomeKey is "snow" or "alpine" or "lunar";
            Add(contributions, MaterialIntent.SnowIceChannel, evidence.SnowVisible, snowContext ? 0.18f : 0.10f, strength, "Screenshot evidence: visible snow", "Bright cold terrain evidence raises SnowIce only as a capped scene prior.");
        }

        if (evidence.StoneVisible >= 0.38f)
        {
            Add(contributions, MaterialIntent.StoneRuinsChannel, evidence.StoneVisible, 0.14f, strength, "Screenshot evidence: visible stone", "Hard gray/brown scene evidence raises StoneRuins only as a capped scene prior.");
        }

        if (evidence.MetalVisible >= 0.30f)
        {
            Add(contributions, MaterialIntent.MetalIndustrialChannel, evidence.MetalVisible, 0.12f, strength, "Screenshot evidence: visible metal", "Low-saturation hard/specular scene evidence raises MetalIndustrial only as a capped scene prior.");
        }

        if (evidence.AetherOrNeonVisible >= 0.34f)
        {
            var aetherContext = context.HasAny("aetherial", "crystal", "magical", "luminous", "cosmic", "fae")
                                || context.BiomeKey is "aetherial" or "fae" or "cosmic" or "lunar" or "lightFlooded";
            var neonContext = context.HasAny("neon", "highTech", "urban", "clean", "electrope")
                              || context.BiomeKey is "highTech";
            var crystalCap = aetherContext ? 0.14f : 0.08f;
            var neonCap = neonContext ? 0.14f : 0.08f;
            Add(contributions, MaterialIntent.CrystalAetherChannel, evidence.AetherOrNeonVisible, crystalCap, strength, "Screenshot evidence: visible aether", "Saturated cool glow raises CrystalAether only as a capped scene prior.");
            Add(contributions, MaterialIntent.NeonGlassChannel, evidence.AetherOrNeonVisible, neonCap, strength, "Screenshot evidence: visible neon", "Saturated cool glow raises NeonGlass only as a capped scene prior.");
        }

        if (evidence.SkinOrCharacterVisible >= 0.28f)
        {
            Add(contributions, MaterialIntent.SkinProtectionChannel, evidence.SkinOrCharacterVisible, 0.10f, strength, "Screenshot evidence: visible character", "Center-heavy skin/character evidence raises SkinProtection instead of material receiver channels.");
        }

        return contributions;
    }

    private static void Add(List<MaterialIntentContribution> contributions, string channel, float evidenceValue, float cap, float strength, string source, string reason)
    {
        var amount = MathF.Min(cap, evidenceValue * cap) * strength;
        if (amount <= 0.001f)
        {
            return;
        }

        contributions.Add(new MaterialIntentContribution(channel, source, amount, $"{reason} Cap {cap:0.##}, strength-scaled by screenshot evidence confidence."));
    }

    private static void AddSuppression(List<MaterialIntentContribution> contributions, string channel, float amount, string source, string reason)
    {
        if (amount <= 0.001f)
        {
            return;
        }

        contributions.Add(new MaterialIntentContribution(channel, source, -amount, reason));
    }

    private sealed class Context
    {
        private readonly HashSet<string> tags = new(StringComparer.OrdinalIgnoreCase);
        private readonly string searchableText;

        public Context(TagStackDiagnostics diagnostics)
        {
            BiomeKey = diagnostics.BiomeKey;
            AreaKey = diagnostics.AreaKey;
            AddTags(diagnostics.ActiveTags);
            AddTags(diagnostics.ActiveWeatherTags);
            AddTags(diagnostics.SecondaryTags);
            AddTags(diagnostics.MoodTags);
            AddTags(diagnostics.MaterialTags);
            AddTags(diagnostics.AreaContextTags);
            AddTags(diagnostics.GameplayStateTags);
            AddTags(diagnostics.ArtDirectionTags);
            tags.Add(diagnostics.BiomeKey);
            tags.Add(diagnostics.WeatherKey);
            tags.Add(diagnostics.AreaKey);

            searchableText = string.Join(
                " ",
                diagnostics.TerritoryName,
                diagnostics.WeatherName,
                diagnostics.BiomeKey,
                diagnostics.BiomeReason,
                diagnostics.AreaKey).ToLowerInvariant();
        }

        public string BiomeKey { get; }
        public string AreaKey { get; }

        public bool HasAny(params string[] candidates) => candidates.Any(tags.Contains);

        public bool ContainsAny(params string[] fragments) => fragments.Any(fragment => searchableText.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        private void AddTags(IEnumerable<string> sourceTags)
        {
            foreach (var tag in sourceTags.Where(tag => !string.IsNullOrWhiteSpace(tag)))
            {
                tags.Add(tag);
            }
        }
    }

    private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    private static float Clamp(float value, float min, float max) => Math.Min(max, Math.Max(min, value));
}
