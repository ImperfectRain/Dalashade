using System;
using System.Collections.Generic;
using System.Linq;

namespace Dalashade;

public static class MaterialProfileBuilder
{
    public static MaterialProfile Build(TagStackDiagnostics diagnostics, ImageAnalysisResult imageAnalysis)
    {
        var state = new State(diagnostics);
        ApplyRepresentativeProfile(state);
        AddAreaContext(state);
        AddWeatherAndTime(state);
        AddScreenshotHints(state, imageAnalysis);
        AddSceneIntentHints(state);
        AddGameplayRestraint(state);
        return state.ToProfile();
    }

    private static void ApplyRepresentativeProfile(State state)
    {
        if (state.HasBiome("jungle") || state.ContainsAny("rak'tika", "raktika", "yak t'el", "kozama'uka", "greatwood"))
        {
            state.SetFamily("jungle/rainforest");
            state.AddTag("jungleCanopy");
            state.AddTag("forestCanopy");
            state.Add(MaterialIntent.FoliageChannel, 0.42f * state.ConfidenceScale, "territory profile", "Jungle/rainforest family strongly raises foliage plausibility.");
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.18f, "territory profile", "Dense exterior canopy still allows daytime sky gaps and humid air.");
            state.Add(MaterialIntent.WaterSpecularChannel, state.HasAny("wet", "rain", "water", "humid") ? 0.12f : -0.08f, "territory profile", "Jungle water/specular stays low unless wet or water tags support it.");
            state.Add(MaterialIntent.SandDustChannel, -0.16f, "territory profile", "Rainforest identity suppresses dry sand/dust.");
            state.Add(MaterialIntent.SnowIceChannel, -0.20f, "territory profile", "Rainforest identity suppresses snow/ice.");
            state.Add(MaterialIntent.MetalIndustrialChannel, -0.16f, "territory profile", "Rainforest identity suppresses industrial material dominance.");
            state.Add(MaterialIntent.NeonGlassChannel, -0.16f, "territory profile", "Rainforest identity suppresses neon/glass unless high-tech tags are explicit.");
            state.Add(MaterialIntent.VoidDarknessChannel, -0.12f, "territory profile", "Jungle night is not void darkness without explicit void/umbral support.");

            if (state.HasAny("ruins", "ancient", "stone", "allagan") || state.ContainsAny("qitana", "ronkan", "temple", "ruin"))
            {
                state.AddTag("mossyStone");
                state.AddTag("ruinsMixedWithFoliage");
                state.Add(MaterialIntent.StoneRuinsChannel, 0.24f, "territory profile", "Jungle ruin cues add mossy stone plausibility without replacing foliage.");
            }

            return;
        }

        if (state.HasBiome("coastal") || state.HasBiome("tropical") || state.ContainsAny("costa", "la noscea", "limsa", "mist", "ruby sea", "beach", "coast"))
        {
            state.SetFamily("coastal/tropical");
            state.AddTag("coastalWaterline");
            state.AddTag("openSkyField");
            state.Add(MaterialIntent.WaterSpecularChannel, 0.36f * state.ConfidenceScale, "territory profile", "Coastal family raises water and specular plausibility.");
            state.Add(MaterialIntent.SandDustChannel, 0.22f * state.ConfidenceScale, "territory profile", "Beaches and coastal sand raise sand/dust plausibility without desert dryness.");
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.24f, "territory profile", "Open coastal exteriors usually include sky/cloud regions.");
            state.Add(MaterialIntent.FoliageChannel, 0.14f, "territory profile", "Tropical/coastal fields often have mild greenery.");
            state.Add(MaterialIntent.SnowIceChannel, -0.20f, "territory profile", "Coastal tropical identity suppresses snow/ice.");
            state.Add(MaterialIntent.VoidDarknessChannel, -0.18f, "territory profile", "Bright coastal identity suppresses void/darkness.");
            return;
        }

        if (state.HasBiome("snow") || state.HasBiome("alpine") || state.ContainsAny("coerthas", "garlemald", "snowcloak", "magna glacies"))
        {
            state.SetFamily("snow/cold");
            state.AddTag("snowfield");
            state.AddTag("openSkyField");
            state.Add(MaterialIntent.SnowIceChannel, 0.40f * state.ConfidenceScale, "territory profile", "Snow/cold family raises snow and ice plausibility.");
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.22f, "territory profile", "Cold exteriors commonly include bright sky, cloud, snowfall, or cold air.");
            state.Add(MaterialIntent.StoneRuinsChannel, 0.14f, "territory profile", "Cold zones often contain rock, ruins, or masonry surfaces.");
            state.Add(MaterialIntent.SandDustChannel, -0.18f, "territory profile", "Snow/cold identity suppresses dry sand/dust.");
            state.Add(MaterialIntent.FireLavaHeatChannel, -0.14f, "territory profile", "Snow/cold identity suppresses heat/fire unless explicit.");

            if (state.HasAny("garlemald", "imperial", "industrial", "magitek") || state.ContainsAny("garlemald", "castrum", "magitek"))
            {
                state.Add(MaterialIntent.MetalIndustrialChannel, 0.20f, "territory profile", "Garlemald/imperial cold zones add metal/industrial plausibility.");
            }

            return;
        }

        if (state.HasBiome("desert") || state.HasBiome("wasteland") || state.ContainsAny("thanalan", "amh araeng", "shaaloani", "sagolii"))
        {
            state.SetFamily("desert/badlands");
            state.AddTag("desertOpen");
            state.AddTag("openSkyField");
            state.Add(MaterialIntent.SandDustChannel, 0.40f * state.ConfidenceScale, "territory profile", "Desert/badlands family raises sand and dust plausibility.");
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.18f, "territory profile", "Open desert scenes often include broad sky, dust, or heat air.");
            state.Add(MaterialIntent.FireLavaHeatChannel, 0.12f, "territory profile", "Desert heat is plausible heat influence but not true fire/lava.");
            state.Add(MaterialIntent.SnowIceChannel, -0.22f, "territory profile", "Desert identity suppresses snow/ice.");
            state.Add(MaterialIntent.WaterSpecularChannel, state.HasAny("oasis", "wet", "rain", "water", "coastal") ? 0.08f : -0.14f, "territory profile", "Desert water/specular stays low unless oasis/wet/water tags support it.");
            return;
        }

        if (state.HasBiome("highTech") || state.ContainsAny("solution nine", "heritage found", "alexandria", "electrope"))
        {
            state.SetFamily("neon/high-tech");
            state.AddTag("neonUrban");
            state.AddTag("industrialInterior");
            state.AddTag("settlementLights");
            state.Add(MaterialIntent.NeonGlassChannel, 0.42f * state.ConfidenceScale, "territory profile", "High-tech family raises neon/glass plausibility.");
            state.Add(MaterialIntent.MetalIndustrialChannel, 0.34f * state.ConfidenceScale, "territory profile", "High-tech family raises constructed metal/industrial plausibility.");
            state.Add(MaterialIntent.SkyCloudFogChannel, state.HasAny("field", "openSkyNight", "city") ? 0.14f : 0.04f, "territory profile", "High-tech exteriors may include sky; interiors stay lower.");
            state.Add(MaterialIntent.FoliageChannel, -0.16f, "territory profile", "High-tech identity suppresses foliage unless explicit.");
            state.Add(MaterialIntent.SandDustChannel, -0.14f, "territory profile", "Clean high-tech identity suppresses sand/dust.");
            state.Add(MaterialIntent.SnowIceChannel, -0.14f, "territory profile", "High-tech identity suppresses snow/ice unless explicit.");
            return;
        }

        if (state.HasBiome("ancient") || state.ContainsAny("allagan", "azys lla", "amaurot", "ruin", "temple"))
        {
            state.SetFamily("ancient/ruins");
            state.AddTag("ruinsMixedWithFoliage");
            state.Add(MaterialIntent.StoneRuinsChannel, 0.30f * state.ConfidenceScale, "territory profile", "Ancient, Allagan, ruin, or temple scenes raise stone/ruin plausibility.");
            state.Add(MaterialIntent.MetalIndustrialChannel, state.HasAny("allagan", "structured", "metallic") || state.ContainsAny("allagan", "azys lla") ? 0.14f : 0.04f, "territory profile", "Allagan/ancient structure can include metal or constructed hard-surface plausibility.");
            state.Add(MaterialIntent.CrystalAetherChannel, state.HasAny("aetherial", "crystal", "luminous") ? 0.14f : 0.06f, "territory profile", "Ancient and Allagan scenes can include restrained aetherial plausibility.");
            state.Add(MaterialIntent.FoliageChannel, state.HasAny("jungle", "forest", "lush", "verdant") ? 0.08f : -0.08f, "territory profile", "Ancient ruins suppress foliage unless explicit forest/jungle tags are present.");
            state.Add(MaterialIntent.SnowIceChannel, -0.08f, "territory profile", "Ancient/ruin identity does not imply snow unless cold tags are explicit.");
            state.Add(MaterialIntent.SandDustChannel, -0.06f, "territory profile", "Ancient/ruin identity keeps mundane sand/dust conservative unless desert tags are explicit.");
            return;
        }

        if (state.HasBiome("cosmic") || state.HasBiome("lunar") || state.HasBiome("fae") || state.HasBiome("aetherial") || state.ContainsAny("ultima thule", "elpis", "il mheg", "mare lamentorum"))
        {
            state.SetFamily("aetherial/cosmic");
            state.AddTag("aetherialLandscape");
            state.AddTag("openSkyField");
            state.Add(MaterialIntent.CrystalAetherChannel, 0.38f * state.ConfidenceScale, "territory profile", "Aetherial/cosmic family raises crystal/aether plausibility.");
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.24f, "territory profile", "Aetherial, lunar, and cosmic scenes often include sky, stars, or atmospheric backdrops.");
            state.Add(MaterialIntent.SandDustChannel, -0.12f, "territory profile", "Aetherial/cosmic identity suppresses mundane sand/dust unless explicit.");
            if (!state.HasBiome("highTech"))
            {
                state.Add(MaterialIntent.MetalIndustrialChannel, -0.10f, "territory profile", "Aetherial/cosmic identity suppresses mundane metal unless explicit.");
            }
        }
    }

    private static void AddAreaContext(State state)
    {
        if (state.HasAny("interior", "dungeon", "cave"))
        {
            state.AddTag("dungeonInterior");
            state.Add(MaterialIntent.SkyCloudFogChannel, -0.22f, "area type", "Interior, dungeon, and cave contexts suppress generic sky unless open-air or fog tags are explicit.");
            state.Add(MaterialIntent.StoneRuinsChannel, 0.12f, "area type", "Interior/dungeon spaces often contain stone, walls, ruins, or hard surfaces.");
        }

        if (state.HasAny("raid", "duty"))
        {
            state.AddTag("raidArena");
            state.Add(MaterialIntent.SkyCloudFogChannel, -0.08f, "content context", "Duty/raid arenas keep generic sky assumptions restrained for gameplay readability.");
        }

        if (state.HasAny("city", "settlement", "urban", "lamplitNight"))
        {
            state.AddTag("settlementLights");
            state.Add(MaterialIntent.SkinProtectionChannel, 0.08f, "area type", "Settlement and city contexts raise character/skin protection plausibility.");
        }

        if (state.HasAny("gpose", "cutscene"))
        {
            state.AddTag("gposeCharacterFocus");
            state.Add(MaterialIntent.SkinProtectionChannel, 0.12f, "presentation state", "GPose/cutscene contexts increase character-facing material safety.");
        }
    }

    private static void AddWeatherAndTime(State state)
    {
        if (state.HasAny("fog", "mist", "haze", "overcast"))
        {
            state.AddTag("fogDominant");
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.18f, "weather/time", "Fog, mist, haze, or overcast weather raises sky/cloud/fog plausibility.");
        }

        if (state.HasAny("rain", "wet", "storm"))
        {
            state.AddTag("wetStone");
            state.Add(MaterialIntent.WaterSpecularChannel, 0.14f, "weather/time", "Rain/storm/wet tags raise specular and wet-surface plausibility.");
        }

        if (state.HasAny("Night", "moonlitNight", "openSkyNight") && !state.HasAny("interior", "dungeon", "cave"))
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.08f, "weather/time", "Outdoor night/open-sky context weakly raises sky or atmospheric gradient plausibility.");
        }
    }

    private static void AddScreenshotHints(State state, ImageAnalysisResult imageAnalysis)
    {
        if (!imageAnalysis.Available)
        {
            return;
        }

        if (imageAnalysis.Contrast < 0.16f && imageAnalysis.HighlightClipping < 0.03f)
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.06f, "screenshot analysis", "Low-contrast unclipped image can indicate sky, cloud, fog, or broad atmosphere.");
        }

        if (FamilyConfidence(imageAnalysis, ColorFamily.Green) > 0.20f)
        {
            state.Add(MaterialIntent.FoliageChannel, 0.06f, "screenshot analysis", "Green color-family confidence weakly supports foliage plausibility.");
        }

        if (FamilyConfidence(imageAnalysis, ColorFamily.Cyan) + FamilyConfidence(imageAnalysis, ColorFamily.Blue) > 0.28f)
        {
            state.Add(MaterialIntent.WaterSpecularChannel, 0.04f, "screenshot analysis", "Blue/cyan color families weakly support water/specular plausibility.");
            state.Add(MaterialIntent.SkyCloudFogChannel, 0.05f, "screenshot analysis", "Blue/cyan color families weakly support sky/cloud plausibility.");
        }

        if (TryGetRegion(imageAnalysis, ImageAnalysisRegion.UpperThird, out var upper))
        {
            var upperSky = RegionFamilyConfidence(upper, ColorFamily.Blue)
                           + RegionFamilyConfidence(upper, ColorFamily.Cyan)
                           + MathF.Max(0f, upper.BrightTendency - 0.20f);
            if (upper.SmoothTendency > 0.45f && upperSky > 0.24f)
            {
                state.Add(MaterialIntent.SkyCloudFogChannel, MathF.Min(0.08f, upperSky * 0.04f), "screenshot region", "Smooth upper blue, cyan, or bright region weakly supports sky/cloud plausibility.");
            }
        }

        if (TryGetRegion(imageAnalysis, ImageAnalysisRegion.LowerThird, out var lower))
        {
            var lowerBlueCyan = RegionFamilyConfidence(lower, ColorFamily.Blue) + RegionFamilyConfidence(lower, ColorFamily.Cyan);
            if (lowerBlueCyan > 0.24f && state.HasAny("coastal", "water", "seaside", "wet", "rain"))
            {
                state.Add(MaterialIntent.WaterSpecularChannel, MathF.Min(0.08f, lowerBlueCyan * 0.05f), "screenshot region", "Lower blue/cyan region plus water/coastal context weakly supports water plausibility.");
            }

            var lowerWarm = RegionFamilyConfidence(lower, ColorFamily.Yellow) + RegionFamilyConfidence(lower, ColorFamily.Orange);
            if (lowerWarm > 0.20f && state.HasAny("desert", "badlands", "coastal", "beach", "dry"))
            {
                state.Add(MaterialIntent.SandDustChannel, MathF.Min(0.08f, lowerWarm * 0.05f), "screenshot region", "Lower warm region plus desert/coastal context weakly supports sand/dust plausibility.");
            }

            if (lower.BrightTendency > 0.25f && lower.AverageSaturation < 0.28f && state.HasAny("snow", "ice", "cold", "alpine"))
            {
                state.Add(MaterialIntent.SnowIceChannel, 0.06f, "screenshot region", "Lower bright low-saturation region plus snow/cold context weakly supports snow/ice plausibility.");
            }
        }

        if (TryGetRegion(imageAnalysis, ImageAnalysisRegion.MiddleThird, out var middle))
        {
            var green = RegionFamilyConfidence(middle, ColorFamily.Green);
            if (green > 0.20f && state.HasAny("field", "jungle", "forest", "foliage", "lush", "verdant"))
            {
                state.Add(MaterialIntent.FoliageChannel, MathF.Min(0.08f, green * 0.05f), "screenshot region", "Middle-region green plus field/forest/jungle context weakly supports foliage plausibility.");
            }
        }
    }

    private static void AddSceneIntentHints(State state)
    {
        var intent = state.SceneIntent;
        if (intent.FoliageDensity > 0.35f)
        {
            state.Add(MaterialIntent.FoliageChannel, intent.FoliageDensity * 0.08f, "SceneIntent", "FoliageDensity reinforces foliage material plausibility.");
        }

        if (intent.Wetness > 0.25f)
        {
            state.Add(MaterialIntent.WaterSpecularChannel, intent.Wetness * 0.08f, "SceneIntent", "Wetness reinforces water/specular plausibility.");
        }

        if (intent.Haze > 0.30f || intent.NightAtmosphere > 0.30f)
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, MathF.Max(intent.Haze, intent.NightAtmosphere) * 0.08f, "SceneIntent", "Haze or night-atmosphere reinforces sky/cloud/fog plausibility.");
        }

        if (intent.MagicGlow > 0.30f)
        {
            state.Add(MaterialIntent.CrystalAetherChannel, intent.MagicGlow * 0.08f, "SceneIntent", "MagicGlow reinforces crystal/aether plausibility.");
        }

        if (intent.NeonGlow > 0.30f)
        {
            state.Add(MaterialIntent.NeonGlassChannel, intent.NeonGlow * 0.08f, "SceneIntent", "NeonGlow reinforces neon/glass plausibility.");
        }
    }

    private static void AddGameplayRestraint(State state)
    {
        if (state.HasAny("combatReadable", "gameplayRestrained") || state.SceneIntent.CombatPressure > 0.35f)
        {
            state.Add(MaterialIntent.SkyCloudFogChannel, -0.06f, "gameplay state", "Combat/gameplay restraint reduces cinematic sky/fog assumptions.");
            state.Add(MaterialIntent.CrystalAetherChannel, -0.04f, "gameplay state", "Combat/gameplay restraint keeps magical material assumptions conservative.");
        }
    }

    private static float FamilyConfidence(ImageAnalysisResult imageAnalysis, ColorFamily family)
    {
        return imageAnalysis.ColorFamilies.TryGetValue(family, out var stats) ? stats.Confidence : 0f;
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

    private sealed class State
    {
        private readonly Dictionary<string, float> values = MaterialIntent.ChannelNames.ToDictionary(channel => channel, _ => 0f, StringComparer.Ordinal);
        private readonly List<MaterialProfileContribution> contributions = [];
        private readonly HashSet<string> tags = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> profileTags = new(StringComparer.OrdinalIgnoreCase);
        private readonly TagStackDiagnostics diagnostics;
        private readonly string searchableText;
        private string family = "general";

        public State(TagStackDiagnostics diagnostics)
        {
            this.diagnostics = diagnostics;
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
            tags.Add(diagnostics.TerritoryName);

            searchableText = string.Join(
                " ",
                diagnostics.TerritoryName,
                diagnostics.WeatherName,
                diagnostics.BiomeKey,
                diagnostics.BiomeReason,
                diagnostics.AreaKey,
                string.Join(" ", tags)).ToLowerInvariant();
        }

        public float ConfidenceScale => 0.55f + (Clamp01(diagnostics.BiomeConfidence) * 0.45f);

        public SceneIntent SceneIntent => diagnostics.Intent;

        public void SetFamily(string value)
        {
            if (string.Equals(family, "general", StringComparison.OrdinalIgnoreCase))
            {
                family = value;
            }
        }

        public void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                profileTags.Add(tag);
            }
        }

        public void Add(string channel, float amount, string source, string reason)
        {
            values[channel] = Clamp01(values[channel] + amount);
            contributions.Add(new MaterialProfileContribution(channel, source, amount, reason));
        }

        public bool HasBiome(string biome) => string.Equals(diagnostics.BiomeKey, biome, StringComparison.OrdinalIgnoreCase);

        public bool HasAny(params string[] candidates) => candidates.Any(candidate => tags.Contains(candidate));

        public bool ContainsAny(params string[] fragments) => fragments.Any(fragment => searchableText.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        public MaterialProfile ToProfile() => new(
            family,
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
            profileTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray(),
            contributions.ToArray());

        private void AddTags(IEnumerable<string> sourceTags)
        {
            foreach (var tag in sourceTags.Where(tag => !string.IsNullOrWhiteSpace(tag)))
            {
                tags.Add(tag);
            }
        }

        private float Value(string channel) => values.TryGetValue(channel, out var value) ? Clamp01(value) : 0f;
    }

    private static float Clamp01(float value) => MathF.Min(1f, MathF.Max(0f, value));
}
