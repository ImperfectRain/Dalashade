using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public static class MaterialIntentBuilder
{
    public static MaterialIntent Build(TagStackDiagnostics diagnostics, ImageAnalysisResult imageAnalysis)
    {
        return Build(diagnostics, imageAnalysis, MaterialProfileBuilder.Build(diagnostics, imageAnalysis));
    }

    public static MaterialIntent Build(TagStackDiagnostics diagnostics, ImageAnalysisResult imageAnalysis, MaterialProfile profile)
    {
        var state = new State(diagnostics, profile);
        AddMaterialProfilePriors(state, profile);
        AddFoliage(state);
        AddWaterSpecular(state);
        AddSandDust(state);
        AddSnowIce(state);
        AddStoneRuins(state);
        AddMetalIndustrial(state);
        AddCrystalAether(state);
        AddNeonGlass(state);
        AddFireLavaHeat(state);
        AddSkyCloudFog(state, imageAnalysis);
        AddSkinProtection(state, imageAnalysis);
        AddVoidDarkness(state, imageAnalysis);
        AddSceneIntentHints(state);

        return state.ToIntent();
    }

    private static void AddMaterialProfilePriors(State state, MaterialProfile profile)
    {
        foreach (var channel in MaterialIntent.ChannelNames)
        {
            var value = profile.ValueFor(channel);
            if (value > 0f)
            {
                state.AddProfilePrior(channel, value, "MaterialProfile prior", $"Profile family '{profile.Family}' set scene plausibility before non-profile evidence.");
            }

            var suppressions = profile.Contributions
                .Where(contribution => string.Equals(contribution.Channel, channel, StringComparison.Ordinal) && contribution.Amount < 0f)
                .OrderBy(contribution => contribution.Amount)
                .Take(2);
            foreach (var suppression in suppressions)
            {
                state.AddProfilePrior(channel, suppression.Amount, "MaterialProfile suppression", suppression.Reason);
            }
        }
    }

    private static void AddFoliage(State state)
    {
        if (state.HasBiome("jungle"))
        {
            state.Add(MaterialIntent.FoliageChannel, 0.70f * state.ConfidenceScale, "primary biome", "Jungle/rainforest biomes strongly imply foliage material likelihood.");
        }
        else if (state.HasBiome("forest") || state.HasBiome("swamp"))
        {
            state.Add(MaterialIntent.FoliageChannel, 0.58f * state.ConfidenceScale, "primary biome", "Forest or swamp biomes imply dense vegetation surfaces.");
        }
        else if (state.HasBiome("coastal") || state.HasBiome("tropical"))
        {
            state.Add(MaterialIntent.FoliageChannel, 0.24f * state.ConfidenceScale, "primary biome", "Coastal/tropical fields often carry mild greenery without becoming full forest.");
        }

        if (state.HasAny("foliage", "lush", "verdant", "rainforest", "canopyLight", "humid"))
        {
            state.Add(MaterialIntent.FoliageChannel, 0.22f, "tag stack", "Lush, verdant, humid, or canopy tags increase likely foliage surfaces.");
        }

        if (state.HasAny("desert", "snow", "highTech", "lunar", "cosmic", "imperial"))
        {
            state.Add(MaterialIntent.FoliageChannel, -0.18f, "tag suppression", "Dry, cold, hard-surface, or otherworldly zones reduce likely foliage dominance.");
        }
    }

    private static void AddWaterSpecular(State state)
    {
        if (state.HasBiome("coastal") || state.HasBiome("tropical") || state.HasBiome("underwater"))
        {
            state.Add(MaterialIntent.WaterSpecularChannel, 0.50f * state.ConfidenceScale, "primary biome", "Coastal, tropical, and underwater biomes imply water/specular surfaces.");
        }

        if (state.HasAny("water", "specular", "wet", "seaside", "beach", "coastal", "rain"))
        {
            state.Add(MaterialIntent.WaterSpecularChannel, 0.25f, "tag stack", "Water, wetness, beach, seaside, or rain tags increase reflective material likelihood.");
        }

        if (state.ContainsAny("la noscea", "costa", "bloodshore", "raincatcher", "ruby sea", "limsa", "mist", "beach", "sea", "coast", "isle"))
        {
            state.Add(MaterialIntent.WaterSpecularChannel, 0.18f, "territory keyword", "Territory name is in a coastal or seaside family.");
        }

        if (state.HasAny("desert", "dry", "snow", "interior", "dungeon") && !state.HasAny("wet", "rain", "coastal", "water"))
        {
            state.Add(MaterialIntent.WaterSpecularChannel, -0.16f, "tag suppression", "Dry, snowy, or interior scenes suppress water/specular likelihood unless wet/coastal tags are present.");
        }
    }

    private static void AddSandDust(State state)
    {
        if (state.HasBiome("desert") || state.HasBiome("wasteland"))
        {
            state.Add(MaterialIntent.SandDustChannel, 0.58f * state.ConfidenceScale, "primary biome", "Desert and wasteland biomes imply sand, dust, and dry ground surfaces.");
        }
        else if (state.HasBiome("coastal"))
        {
            state.Add(MaterialIntent.SandDustChannel, 0.22f * state.ConfidenceScale, "primary biome", "Coastal beach zones can imply sand without desert dryness.");
        }

        if (state.HasAny("badlands", "dry", "dust", "heat", "sunScorched", "beach"))
        {
            state.Add(MaterialIntent.SandDustChannel, 0.24f, "tag stack", "Dry, heat, dust, badlands, or beach tags increase sand/dust likelihood.");
        }

        if (state.ContainsAny("thanalan", "amh araeng", "shaaloani", "sagolii", "costa", "beach"))
        {
            state.Add(MaterialIntent.SandDustChannel, 0.18f, "territory keyword", "Territory name is associated with desert, badlands, or beach surfaces.");
        }

        if (state.HasAny("rain", "wet", "snow", "ice"))
        {
            state.Add(MaterialIntent.SandDustChannel, -0.18f, "weather/material suppression", "Wet or frozen conditions reduce loose sand/dust likelihood.");
        }
    }

    private static void AddSnowIce(State state)
    {
        if (state.HasBiome("snow") || state.HasBiome("alpine"))
        {
            state.Add(MaterialIntent.SnowIceChannel, 0.58f * state.ConfidenceScale, "primary biome", "Snow and alpine biomes imply ice, snow, and bright cold surfaces.");
        }

        if (state.HasAny("snow", "ice", "cold", "alpine", "crisp"))
        {
            state.Add(MaterialIntent.SnowIceChannel, 0.28f, "tag stack", "Snow, ice, cold, alpine, or crisp tags increase frozen material likelihood.");
        }

        if (state.ContainsAny("coerthas", "snowcloak", "garlemald", "mare lamentorum", "magna glacies"))
        {
            state.Add(MaterialIntent.SnowIceChannel, 0.18f, "territory keyword", "Territory name is associated with cold, snow, or lunar ice surfaces.");
        }

        if (state.HasAny("desert", "tropical", "heat", "fire", "lava"))
        {
            state.Add(MaterialIntent.SnowIceChannel, -0.20f, "tag suppression", "Hot, desert, tropical, or fire tags reduce frozen material likelihood.");
        }
    }

    private static void AddStoneRuins(State state)
    {
        if (state.HasBiome("ancient") || state.HasBiome("cave"))
        {
            state.Add(MaterialIntent.StoneRuinsChannel, 0.42f * state.ConfidenceScale, "primary biome", "Ancient and cave biomes imply stone, ruin, or carved surfaces.");
        }

        if (state.HasAny("ruins", "stone", "ancient", "structured", "allagan"))
        {
            state.Add(MaterialIntent.StoneRuinsChannel, 0.30f, "tag stack", "Ruins, stone, ancient, or structured tags increase hard stone material likelihood.");
        }

        if (state.ContainsAny("ruin", "allagan", "azys", "amaurot", "temple", "palace", "stone"))
        {
            state.Add(MaterialIntent.StoneRuinsChannel, 0.18f, "territory keyword", "Territory name contains ruin, Allagan, temple, or stone cues.");
        }

        if (state.HasAny("neon", "highTech", "coastal", "water", "lush"))
        {
            state.Add(MaterialIntent.StoneRuinsChannel, -0.12f, "tag suppression", "Neon, wet coastal, and lush scenes reduce stone/ruin dominance.");
        }
    }

    private static void AddMetalIndustrial(State state)
    {
        if (state.HasBiome("imperial") || state.HasBiome("highTech"))
        {
            state.Add(MaterialIntent.MetalIndustrialChannel, 0.52f * state.ConfidenceScale, "primary biome", "Imperial and high-tech biomes imply metal, machinery, glass, and constructed surfaces.");
        }

        if (state.HasAny("industrial", "imperial", "metallic", "steel", "magitek", "factory", "structured", "smoky", "electrope"))
        {
            state.Add(MaterialIntent.MetalIndustrialChannel, 0.28f, "tag stack", "Industrial, metallic, magitek, smoky, or structured tags increase hard-surface material likelihood.");
        }

        if (state.ContainsAny("garlemald", "castrum", "solution nine", "heritage found", "alexandria", "factory", "magitek", "babil", "ceruleum", "allagan", "azys lla"))
        {
            state.Add(MaterialIntent.MetalIndustrialChannel, 0.20f, "territory keyword", "Territory name belongs to an imperial, magitek, high-tech, or Allagan hard-surface family.");
        }

        if (state.HasAny("jungle", "forest", "coastal", "tropical", "fae"))
        {
            state.Add(MaterialIntent.MetalIndustrialChannel, -0.14f, "tag suppression", "Organic, coastal, or fae zones reduce metal/industrial dominance.");
        }
    }

    private static void AddCrystalAether(State state)
    {
        if (state.HasBiome("aetherial") || state.HasBiome("fae") || state.HasBiome("cosmic") || state.HasBiome("lunar") || state.HasBiome("lightFlooded"))
        {
            state.Add(MaterialIntent.CrystalAetherChannel, 0.46f * state.ConfidenceScale, "primary biome", "Aetherial, fae, cosmic, lunar, or light-flooded biomes imply magical or crystalline materials.");
        }

        if (state.HasAny("aetherial", "crystal", "magical", "fae", "dreamlike", "cosmic", "lunar", "alien", "stars", "luminous"))
        {
            state.Add(MaterialIntent.CrystalAetherChannel, 0.30f, "tag stack", "Aetherial, crystal, fae, cosmic, lunar, alien, or luminous tags increase magical material likelihood.");
        }

        if (state.ContainsAny("ultima thule", "elpis", "il mheg", "crystarium", "lakeland", "azys lla", "mare lamentorum", "omphalos"))
        {
            state.Add(MaterialIntent.CrystalAetherChannel, 0.20f, "territory keyword", "Territory name is associated with aetherial, cosmic, fae, or crystalline scene families.");
        }

        if (state.HasAny("imperial", "desert", "industrial") && !state.HasAny("aetherial", "crystal", "magical"))
        {
            state.Add(MaterialIntent.CrystalAetherChannel, -0.12f, "tag suppression", "Purely industrial or dry zones reduce crystal/aether likelihood unless magical tags are present.");
        }
    }

    private static void AddNeonGlass(State state)
    {
        if (state.HasBiome("highTech"))
        {
            state.Add(MaterialIntent.NeonGlassChannel, 0.60f * state.ConfidenceScale, "primary biome", "High-tech biomes imply neon, glass, and luminous constructed materials.");
        }

        if (state.HasAny("neon", "highTech", "urban", "clean", "luminous", "electrope"))
        {
            state.Add(MaterialIntent.NeonGlassChannel, 0.30f, "tag stack", "Neon, high-tech, clean, luminous, urban, or electrope tags increase neon/glass likelihood.");
        }

        if (state.ContainsAny("solution nine", "heritage found", "alexandria", "living memory", "electrope"))
        {
            state.Add(MaterialIntent.NeonGlassChannel, 0.20f, "territory keyword", "Territory name belongs to a neon/high-tech scene family.");
        }

        if (state.HasAny("forest", "jungle", "desert", "snow", "coastal") && !state.HasAny("highTech", "neon"))
        {
            state.Add(MaterialIntent.NeonGlassChannel, -0.18f, "tag suppression", "Natural biomes suppress neon/glass likelihood unless explicit high-tech tags are present.");
        }
    }

    private static void AddFireLavaHeat(State state)
    {
        if (state.HasBiome("fire") || state.HasBiome("volcanic"))
        {
            state.Add(MaterialIntent.FireLavaHeatChannel, 0.62f * state.ConfidenceScale, "primary biome", "Fire and volcanic biomes imply flame, lava, or heat materials.");
        }
        else if (state.HasBiome("desert"))
        {
            state.Add(MaterialIntent.FireLavaHeatChannel, 0.28f * state.ConfidenceScale, "primary biome", "Desert biomes imply heat pressure without true lava/fire materials.");
        }

        if (state.HasAny("fire", "heat", "lava", "volcanic", "sunScorched"))
        {
            state.Add(MaterialIntent.FireLavaHeatChannel, 0.24f, "tag stack", "Fire, heat, lava, volcanic, or sun-scorched tags increase heat material likelihood.");
        }

        if (state.HasAny("snow", "ice", "cold", "wet", "rain"))
        {
            state.Add(MaterialIntent.FireLavaHeatChannel, -0.20f, "tag suppression", "Cold or wet tags reduce fire/lava/heat material likelihood.");
        }
    }

    private static void AddSkyCloudFog(State state, ImageAnalysisResult imageAnalysis)
    {
        if (state.HasAny("fog", "mist", "clouds", "overcast", "storm"))
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.36f, "weather tags", "Fog, mist, cloud, overcast, or storm weather implies sky/cloud/fog material likelihood.");
        }

        if (state.HasBiome("cosmic") || state.HasBiome("lunar"))
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.22f * state.ConfidenceScale, "primary biome", "Cosmic and lunar scenes often have prominent sky or atmospheric backdrops.");
        }

        if (state.HasAny("field", "Night") && !state.HasAny("interior", "dungeon", "cave"))
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.10f, "outdoor time context", "Outdoor night field context weakly suggests visible sky or atmospheric gradients without implying fog.");
        }

        if (state.HasAny("highDepth", "moonlit", "stars", "gloom"))
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.14f, "art direction tags", "High-depth, moonlit, star, or gloom tags can imply atmospheric sky presence.");
        }

        if (imageAnalysis.Available && imageAnalysis.Contrast < 0.16f && imageAnalysis.HighlightClipping < 0.03f)
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.08f, "screenshot analysis", "Low-contrast, unclipped image analysis weakly suggests sky, cloud, fog, or atmospheric gradients.");
        }

        if (TryGetRegion(imageAnalysis, ImageAnalysisRegion.UpperThird, out var upper))
        {
            var upperSkyColor = RegionFamilyConfidence(upper, ColorFamily.Blue)
                                + RegionFamilyConfidence(upper, ColorFamily.Cyan)
                                + MathF.Max(0f, upper.BrightTendency - 0.20f);
            if (upper.SmoothTendency > 0.45f && upperSkyColor > 0.24f)
            {
                state.Add(MaterialIntent.SkyCloudFogChannel, MathF.Min(0.08f, upperSkyColor * 0.04f), "screenshot region", "Smooth upper blue, cyan, or bright region weakly supports sky/cloud plausibility.");
            }
        }

        if (state.HasAny("interior", "dungeon", "cave"))
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, -0.18f, "area suppression", "Interior, dungeon, or cave context reduces likely visible sky/cloud material.");
        }
    }

    private static void AddSkinProtection(State state, ImageAnalysisResult imageAnalysis)
    {
        if (state.HasAny("city", "settlement", "combatReadable", "cinematicAllowed"))
        {
            state.Add(MaterialIntent.SkinProtectionChannel, 0.18f, "area/gameplay tags", "City, settlement, cinematic, or combat-readable contexts often include characters that need skin protection.");
        }

        if (state.HasAny("gpose", "cutscene"))
        {
            state.Add(MaterialIntent.SkinProtectionChannel, 0.18f, "presentation tags", "GPose or cutscene context increases character-facing material safety.");
        }

        if (imageAnalysis.Available)
        {
            var orange = FamilyConfidence(imageAnalysis, ColorFamily.Orange);
            var red = FamilyConfidence(imageAnalysis, ColorFamily.Red);
            var skinLike = MathF.Min(1f, (orange * 0.75f) + (red * 0.35f));
            if (skinLike > 0.20f && imageAnalysis.AverageLuminance is > 0.18f and < 0.82f)
            {
                state.Add(MaterialIntent.SkinProtectionChannel, skinLike * 0.14f, "screenshot analysis", "Moderate warm color-family confidence weakly suggests skin-tone protection risk.");
            }
        }

        if (TryGetRegion(imageAnalysis, ImageAnalysisRegion.Center, out var center))
        {
            var warmCenter = RegionFamilyConfidence(center, ColorFamily.Orange) + (RegionFamilyConfidence(center, ColorFamily.Red) * 0.45f);
            if (warmCenter > 0.18f && center.AverageLuminance is > 0.18f and < 0.82f && center.SmoothTendency > 0.25f)
            {
                state.Add(MaterialIntent.SkinProtectionChannel, MathF.Min(0.06f, warmCenter * 0.05f), "screenshot region", "Smooth warm center-region color weakly supports skin/character protection.");
            }
        }

        if (state.HasAny("void", "dungeon", "raid") && !state.HasAny("cinematicAllowed", "gpose", "cutscene"))
        {
            state.Add(MaterialIntent.SkinProtectionChannel, -0.08f, "gameplay suppression", "Dungeon, raid, or void contexts keep inferred skin protection conservative unless presentation tags are active.");
        }
    }

    private static void AddVoidDarkness(State state, ImageAnalysisResult imageAnalysis)
    {
        var explicitDarkness = state.HasAny("void", "darkness", "abyss", "umbral", "gothic", "haunted", "gloom");
        if (state.HasBiome("void"))
        {
            state.Add(MaterialIntent.VoidDarknessChannel, 0.55f * state.ConfidenceScale, "primary biome", "Void biome directly implies void/darkness material likelihood.");
        }

        if (explicitDarkness)
        {
            state.Add(MaterialIntent.VoidDarknessChannel, 0.24f, "tag stack", "Void, umbral, haunted, gloom, gothic, abyss, or darkness tags support dark material likelihood.");
        }

        if (state.ContainsAny("void", "darkness", "abyss", "ascian", "umbral"))
        {
            state.Add(MaterialIntent.VoidDarknessChannel, 0.20f, "territory/weather keyword", "Territory or weather text contains explicit void/darkness cues.");
        }

        if (imageAnalysis.Available && explicitDarkness && imageAnalysis.AverageLuminance < 0.28f)
        {
            state.Add(MaterialIntent.VoidDarknessChannel, 0.10f, "screenshot analysis", "Dark screenshot analysis reinforces explicit void/gloom/haunted tags.");
        }

        if (state.HasAny("forest", "jungle", "lush", "verdant") && !explicitDarkness)
        {
            state.Add(MaterialIntent.VoidDarknessChannel, -0.18f, "tag suppression", "Normal night forest/jungle tags do not imply void darkness without explicit void, umbral, gothic, or gloom support.");
        }

        if (state.HasAny("coastal", "tropical", "highTech", "city", "clean") && !explicitDarkness)
        {
            state.Add(MaterialIntent.VoidDarknessChannel, -0.16f, "tag suppression", "Bright, clean, coastal, urban, or high-tech identities suppress void/darkness likelihood.");
        }

        if (state.HasAny("Night") && !explicitDarkness)
        {
            state.Add(MaterialIntent.VoidDarknessChannel, -0.06f, "time suppression", "Night alone is not treated as void material evidence.");
        }
    }

    private static void AddSceneIntentHints(State state)
    {
        var intent = state.SceneIntent;
        if (intent.FoliageDensity > 0.35f)
        {
            state.Add(MaterialIntent.FoliageChannel, intent.FoliageDensity * 0.14f, "SceneIntent", "Existing foliage-density intent reinforces inferred foliage likelihood.");
        }

        if (intent.Wetness > 0.25f)
        {
            state.Add(MaterialIntent.WaterSpecularChannel, intent.Wetness * 0.12f, "SceneIntent", "Existing wetness intent reinforces reflective water/specular likelihood.");
        }

        if (intent.Heat > 0.30f)
        {
            state.Add(MaterialIntent.SandDustChannel, intent.Heat * 0.10f, "SceneIntent", "Existing heat intent weakly reinforces dry sand/dust likelihood.");
            state.Add(MaterialIntent.FireLavaHeatChannel, intent.Heat * 0.10f, "SceneIntent", "Existing heat intent reinforces heat material likelihood without implying true lava.");
        }

        if (intent.Cold > 0.30f)
        {
            state.Add(MaterialIntent.SnowIceChannel, intent.Cold * 0.12f, "SceneIntent", "Existing cold intent reinforces snow/ice material likelihood.");
        }

        if (intent.IndustrialHardness > 0.30f)
        {
            state.Add(MaterialIntent.MetalIndustrialChannel, intent.IndustrialHardness * 0.12f, "SceneIntent", "Existing industrial-hardness intent reinforces metal or constructed material likelihood.");
        }

        if (intent.MagicGlow > 0.30f)
        {
            state.Add(MaterialIntent.CrystalAetherChannel, intent.MagicGlow * 0.12f, "SceneIntent", "Existing magic-glow intent reinforces aetherial/crystal material likelihood.");
        }

        if (intent.NeonGlow > 0.30f)
        {
            state.Add(MaterialIntent.NeonGlassChannel, intent.NeonGlow * 0.14f, "SceneIntent", "Existing neon-glow intent reinforces neon/glass material likelihood.");
        }

        if (intent.Haze > 0.30f)
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, intent.Haze * 0.10f, "SceneIntent", "Existing haze intent reinforces sky/cloud/fog likelihood.");
        }
    }

    private static bool TryGetRegion(ImageAnalysisResult imageAnalysis, ImageAnalysisRegion region, out ImageRegionStats stats)
    {
        if (imageAnalysis.Available && imageAnalysis.Regions.TryGetValue(region, out stats!))
        {
            return true;
        }

        stats = ImageRegionStats.Empty(region);
        return false;
    }

    private static float RegionFamilyConfidence(ImageRegionStats stats, ColorFamily family)
    {
        return stats.ColorFamilies.TryGetValue(family, out var familyStats) ? familyStats.Confidence : 0f;
    }


    private static float FamilyConfidence(ImageAnalysisResult imageAnalysis, ColorFamily family)
    {
        return imageAnalysis.ColorFamilies.TryGetValue(family, out var stats) ? stats.Confidence : 0f;
    }

    private sealed class State
    {
        private readonly Dictionary<string, float> positiveEvidence = MaterialIntent.ChannelNames.ToDictionary(channel => channel, _ => 0f, StringComparer.Ordinal);
        private readonly Dictionary<string, float> suppressionEvidence = MaterialIntent.ChannelNames.ToDictionary(channel => channel, _ => 0f, StringComparer.Ordinal);
        private readonly List<MaterialIntentContribution> contributions = [];
        private readonly HashSet<string> tags;
        private readonly string searchableText;
        private readonly TagStackDiagnostics diagnostics;
        private readonly MaterialProfile profile;

        public State(TagStackDiagnostics diagnostics, MaterialProfile profile)
        {
            this.diagnostics = diagnostics;
            this.profile = profile;
            tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

        public float ConfidenceScale => 0.55f + (Clamp01(diagnostics.BiomeConfidence) * 0.45f);

        public SceneIntent SceneIntent => diagnostics.Intent;

        public void AddProfilePrior(string channel, float amount, string source, string reason)
        {
            contributions.Add(new MaterialIntentContribution(channel, source, amount, reason));
        }

        public void Add(string channel, float amount, string source, string reason)
        {
            if (amount >= 0f)
            {
                positiveEvidence[channel] += amount;
            }
            else
            {
                suppressionEvidence[channel] += -amount;
            }

            contributions.Add(new MaterialIntentContribution(channel, source, amount, reason));
        }

        public bool HasBiome(string biome) => string.Equals(diagnostics.BiomeKey, biome, StringComparison.OrdinalIgnoreCase);

        public bool HasAny(params string[] candidates) => candidates.Any(candidate => tags.Contains(candidate));

        public bool ContainsAny(params string[] fragments) => fragments.Any(fragment => searchableText.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        public MaterialIntent ToIntent() => new(
            Value(MaterialIntent.FoliageChannel),
            Value(MaterialIntent.WaterSpecularChannel),
            Value(MaterialIntent.SandDustChannel),
            Value(MaterialIntent.SnowIceChannel),
            Value(MaterialIntent.StoneRuinsChannel),
            Value(MaterialIntent.MetalIndustrialChannel),
            Value(MaterialIntent.CrystalAetherChannel),
            Value(MaterialIntent.NeonGlassChannel),
            Value(MaterialIntent.FireLavaHeatChannel),
            Value(MaterialIntent.SkyCloudFogChannel),
            Value(MaterialIntent.SkinProtectionChannel),
            Value(MaterialIntent.VoidDarknessChannel),
            contributions);

        private void AddTags(IEnumerable<string> sourceTags)
        {
            foreach (var tag in sourceTags.Where(tag => !string.IsNullOrWhiteSpace(tag)))
            {
                tags.Add(tag);
            }
        }

        private float Value(string channel)
        {
            var positive = positiveEvidence.TryGetValue(channel, out var positiveValue) ? positiveValue : 0f;
            var suppression = suppressionEvidence.TryGetValue(channel, out var suppressionValue) ? suppressionValue : 0f;
            var profilePrior = Clamp01(profile.ValueFor(channel));
            var nonProfileEvidence = Clamp01(positive - (suppression * 0.55f));
            var explicitEvidence = positive >= 0.70f;
            var weakProfile = profilePrior < 0.10f;
            var profileCap = explicitEvidence
                ? 1.0f
                : weakProfile
                    ? 0.42f
                    : MathF.Min(0.92f, 0.48f + (profilePrior * 0.62f));
            var blended = (nonProfileEvidence * 0.76f) + (profilePrior * 0.24f);
            var suppressed = blended - (MathF.Max(0f, suppression - 0.18f) * 0.18f);
            return Clamp01(MathF.Min(profileCap, suppressed));
        }
    }

    private static float Clamp01(float value) => MathF.Min(1f, MathF.Max(0f, value));
}
