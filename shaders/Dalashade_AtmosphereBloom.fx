#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"

uniform float Dalashade_Atmosphere <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Atmosphere";
    ui_tooltip = "Scene-driven atmosphere allowance. Higher values allow more ambient glow.";
> = 0.0;

uniform float Dalashade_MagicGlow <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Magic Glow";
    ui_tooltip = "Scene-driven aetherial or magical glow pressure.";
> = 0.0;

uniform float Dalashade_NeonGlow <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Neon Glow";
    ui_tooltip = "Scene-driven neon or high-tech glow pressure.";
> = 0.0;

uniform float Dalashade_FoliageDensity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Foliage Density";
    ui_tooltip = "Scene-driven foliage density. Higher values allow subtle canopy/sky-light bloom.";
> = 0.0;

uniform float Dalashade_Wetness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Wetness";
    ui_tooltip = "Scene-driven rain or wet-surface pressure. Higher values allow small specular glow.";
> = 0.0;

uniform float Dalashade_Heat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Heat";
    ui_tooltip = "Scene-driven heat/dust pressure. Higher values allow distant warm atmospheric glow.";
> = 0.0;

uniform float Dalashade_Readability <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Readability";
    ui_tooltip = "Scene-driven readability pressure. Higher values restrain bloom.";
> = 0.0;

uniform float Dalashade_HighlightProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Highlight Protection";
    ui_tooltip = "Scene-driven bright highlight restraint. Higher values raise bloom threshold.";
> = 0.0;

uniform float Dalashade_CombatPressure <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Combat Pressure";
    ui_tooltip = "Scene-driven gameplay pressure. Higher values damp bloom.";
> = 0.0;

uniform float Dalashade_CinematicPermission <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cinematic Permission";
    ui_tooltip = "Scene-driven permission for stronger cinematic glow outside gameplay-critical moments.";
> = 0.0;

uniform float Dalashade_Night <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night";
    ui_tooltip = "Scene-driven nighttime context. Higher values make bloom more selective.";
> = 0.0;

uniform float Dalashade_Moonlight <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Moonlight";
    ui_tooltip = "Scene-driven moonlight influence for subtle cool diffusion.";
> = 0.0;

uniform float Dalashade_ArtificialLight <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Artificial Light";
    ui_tooltip = "Scene-driven lamp, window, neon, fire, or crystal light-pool influence.";
> = 0.0;

uniform float Dalashade_AmbientDarkness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Ambient Darkness";
    ui_tooltip = "Scene-driven dark baseline. Higher values reduce full-screen wash.";
> = 0.0;

uniform float Dalashade_NightAtmosphere <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night Atmosphere";
    ui_tooltip = "Scene-driven nighttime air/mist/storm atmosphere.";
> = 0.0;

uniform float Dalashade_MaterialWaterSpecular <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water/Specular";
    ui_tooltip = "Inferred water or wet/specular likelihood. Adds restrained glint glow and prevents sparkle blowout.";
> = 0.0;

uniform float Dalashade_MaterialCrystalAether <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Crystal/Aether";
    ui_tooltip = "Inferred crystal or aether likelihood. Adds color-selective magical glow.";
> = 0.0;

uniform float Dalashade_MaterialNeonGlass <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Neon/Glass";
    ui_tooltip = "Inferred neon or glass likelihood. Adds small-radius colored neon bloom with strong restraint.";
> = 0.0;

uniform float Dalashade_MaterialFireLavaHeat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Fire/Lava/Heat";
    ui_tooltip = "Inferred fire, lava, or heat likelihood. Adds warm source glow and respects combat dampening.";
> = 0.0;

uniform float Dalashade_MaterialSkyCloudFog <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sky/Cloud/Fog";
    ui_tooltip = "Inferred sky, cloud, fog, or atmosphere likelihood. Allows broad glow while preventing milky wash.";
> = 0.0;

uniform int Dalashade_MaterialDebugMode <
    ui_type = "combo";
    ui_items = "Off\0Overview\0Water/specular\0Crystal/aether\0Neon/glass\0Fire/heat\0Sky/fog\0Final bloom eligibility\0";
    ui_label = "Dalashade Material Debug Mode";
    ui_tooltip = "Shows material-aware bloom influence masks. These masks are inferred likelihoods, not true engine material IDs.";
> = 0;

uniform float Dalashade_MaterialDebugStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Debug Strength";
> = 1.0;

uniform float BloomStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Bloom Strength";
    ui_tooltip = "Manual overall bloom strength. Defaults are intentionally conservative.";
> = 0.32;

uniform float BloomThreshold <
    ui_type = "slider";
    ui_min = 0.45; ui_max = 1.0;
    ui_label = "Bloom Threshold";
> = 0.74;

uniform float DiffusionStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Diffusion Strength";
    ui_tooltip = "Controls the small cheap blur radius used for bloom diffusion.";
> = 0.42;

uniform float MagicGlowStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Magic Glow Strength";
> = 0.48;

uniform float NeonGlowStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Neon Glow Strength";
> = 0.42;

uniform float HighlightRestraint <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Highlight Restraint";
    ui_tooltip = "Manual restraint for full-screen washout and bright highlight bloom.";
> = 0.70;

uniform float CombatDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Combat Dampen Strength";
> = 0.72;

uniform float CinematicBoostStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Cinematic Boost Strength";
> = 0.34;

uniform bool ShowDebugMask <
    ui_label = "Show Debug Mask";
    ui_tooltip = "Shows bloom source and restraint masks. Red is source, green is magic/neon, blue is restraint.";
> = false;

float Dalashade_AtmosphereBloomLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 Dalashade_AtmosphereBloomSource(float2 uv, float threshold, float highlightProtection, float magicGlow, float neonGlow, float canopyGlow, float wetness, float heat, float materialWater, float materialCrystal, float materialNeon, float materialFire, float materialSky, float night, float moonlight, float artificialLight, float ambientDarkness, float nightAtmosphere, float combat, float cinematic)
{
    float3 color = tex2D(ReShade::BackBuffer, uv).rgb;
    float depth = ReShade::GetLinearizedDepth(uv);
    float luma = Dalashade_AtmosphereBloomLuma(color);
    float sourceMask = smoothstep(threshold, 1.0, luma);
    float saturatedAccent = max(max(color.r, color.g), color.b) - min(min(color.r, color.g), color.b);
    float warmMask = smoothstep(0.03, 0.34, color.r - max(color.g, color.b) * 0.72);
    float coolAetherMask = smoothstep(0.06, 0.42, max(color.b, color.g) - color.r * 0.58);
    float neonColorMask = smoothstep(0.10, 0.55, saturatedAccent) * smoothstep(threshold - 0.22, 1.0, luma);
    float smoothSkyMask = materialSky * smoothstep(0.44, 0.92, luma) * (1.0 - smoothstep(0.20, 0.62, saturatedAccent)) * smoothstep(0.18, 0.90, depth);
    float accentMask = smoothstep(threshold - 0.10, 1.0, luma) * max(max(magicGlow * 0.44, neonGlow * saturatedAccent * 0.52), canopyGlow * 0.30);
    float nightLocalLight = artificialLight * smoothstep(threshold - 0.16, 1.0, luma) * (0.42 + saturatedAccent * 0.34 + max(materialCrystal, materialNeon) * 0.24);
    float moonlitDiffusion = moonlight * nightAtmosphere * smoothstep(0.34, 0.78, luma) * smoothstep(0.22, 0.92, depth) * (1.0 - saturatedAccent * 0.35);
    float wetSpecular = max(wetness, materialWater) * smoothstep(0.68, 0.98, luma) * smoothstep(0.035, 0.22, saturatedAccent + luma * 0.15);
    float aetherSource = materialCrystal * coolAetherMask * smoothstep(threshold - 0.18, 1.0, luma) * (0.45 + saturatedAccent * 0.55);
    float neonSource = materialNeon * neonColorMask;
    float fireSource = materialFire * warmMask * smoothstep(threshold - 0.16, 1.0, luma) * (1.0 - combat * 0.58);
    float heatDistance = max(heat, materialFire * 0.55) * smoothstep(0.26, 0.94, depth) * smoothstep(threshold - 0.14, 1.0, luma) * (1.0 - combat * 0.42);
    float skySource = smoothSkyMask * (0.14 + cinematic * 0.08 + nightAtmosphere * 0.05) * (1.0 - highlightProtection * 0.46);
    float source = saturate(sourceMask + accentMask + nightLocalLight * 0.34 + moonlitDiffusion * 0.10 + wetSpecular * 0.22 + aetherSource * 0.36 + neonSource * 0.30 + fireSource * 0.30 + heatDistance * 0.16 + skySource);
    float restraint = 1.0 - saturate((highlightProtection * 0.70 + ambientDarkness * night * 0.18 + materialWater * 0.18 + materialNeon * 0.18 + materialSky * 0.20) * smoothstep(0.76, 1.0, luma));
    return color * source * restraint;
}

float4 Dalashade_AtmosphereBloomPS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    // Single-pass bright-source diffusion. This is intentionally cheap and bounded for gameplay use.
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    float atmosphere = saturate(Dalashade_Atmosphere);
    float magicGlow = saturate(Dalashade_MagicGlow);
    float neonGlow = saturate(Dalashade_NeonGlow);
    float foliage = saturate(Dalashade_FoliageDensity);
    float wetness = saturate(Dalashade_Wetness);
    float heat = saturate(Dalashade_Heat);
    float readability = saturate(Dalashade_Readability);
    float highlightProtection = saturate(Dalashade_HighlightProtection);
    float combat = saturate(Dalashade_CombatPressure);
    float cinematic = saturate(Dalashade_CinematicPermission);
    float night = saturate(Dalashade_Night);
    float moonlight = saturate(Dalashade_Moonlight);
    float artificialLight = saturate(Dalashade_ArtificialLight);
    float ambientDarkness = saturate(Dalashade_AmbientDarkness);
    float nightAtmosphere = saturate(Dalashade_NightAtmosphere);
    float canopyGlow = foliage * atmosphere * (1.0 - combat * 0.55);
    float materialWater = saturate(Dalashade_MaterialWaterSpecular);
    float materialCrystal = saturate(Dalashade_MaterialCrystalAether);
    float materialNeon = saturate(Dalashade_MaterialNeonGlass);
    float materialFire = saturate(Dalashade_MaterialFireLavaHeat);
    float materialSky = saturate(Dalashade_MaterialSkyCloudFog);

    float threshold = BloomThreshold + highlightProtection * 0.135 + combat * 0.040 + readability * 0.030 + ambientDarkness * night * 0.035;
    threshold -= max(max(magicGlow, neonGlow), max(materialCrystal, materialNeon) * 0.72) * 0.030;
    threshold -= artificialLight * 0.018;
    threshold -= moonlight * nightAtmosphere * 0.010;
    threshold -= max(wetness, materialWater) * 0.018;
    threshold -= max(heat, materialFire) * 0.010;
    threshold += materialSky * highlightProtection * 0.020;
    threshold = clamp(threshold, 0.58, 0.94);

    float2 texel = BUFFER_PIXEL_SIZE;
    float radius = lerp(1.0, 3.0, saturate(DiffusionStrength));
    float2 step1 = texel * radius;
    float2 step2 = texel * radius * 2.0;

    // Selective bright-pass source: light emitters, wet highlights, canopy openings, and distant heat glow are favored over full-frame bloom.
    float3 bloom = Dalashade_AtmosphereBloomSource(texcoord, threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.26;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step1.x, 0.0), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.12;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step1.x, 0.0), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.12;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(0.0, step1.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.12;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(0.0, -step1.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.12;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step2.x, step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.065;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step2.x, step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.065;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step2.x, -step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.065;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step2.x, -step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWater, materialCrystal, materialNeon, materialFire, materialSky, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.065;

    float3 magicTint = float3(0.78, 0.58, 1.0);
    float3 neonTint = float3(0.48, 0.92, 1.0);
    float3 warmAtmosphereTint = float3(1.0, 0.90, 0.74);
    float3 canopyTint = float3(0.68, 0.92, 0.54);
    float3 glowTint = lerp(warmAtmosphereTint, magicTint, magicGlow * MagicGlowStrength);
    glowTint = lerp(glowTint, magicTint, materialCrystal * MagicGlowStrength * 0.42);
    glowTint = lerp(glowTint, neonTint, max(neonGlow, materialNeon) * NeonGlowStrength);
    glowTint = lerp(glowTint, canopyTint, canopyGlow * 0.30);
    glowTint = lerp(glowTint, float3(1.0, 0.78, 0.48), max(heat, materialFire) * 0.22);
    glowTint = lerp(glowTint, float3(0.82, 0.92, 1.0), max(wetness, materialWater) * 0.16);
    glowTint = lerp(glowTint, float3(0.70, 0.82, 1.0), moonlight * 0.18);
    glowTint = lerp(glowTint, float3(1.0, 0.82, 0.58), artificialLight * 0.16);

    float combatDampen = 1.0 - saturate(combat * CombatDampenStrength);
    float cinematicBoost = 1.0 + cinematic * CinematicBoostStrength * (1.0 - combat * 0.65) * (0.86 + max(max(materialCrystal, materialNeon), materialFire) * 0.14);
    float readabilityDampen = 1.0 - readability * 0.22;
    float materialSelective = materialWater * 0.04 + materialCrystal * 0.08 + materialNeon * 0.08 + materialFire * 0.05 + materialSky * 0.03;
    float intentStrength = 0.40 + atmosphere * 0.20 + magicGlow * 0.22 + neonGlow * 0.22 + canopyGlow * 0.10 + wetness * 0.08 + heat * 0.05 + artificialLight * 0.08 + moonlight * nightAtmosphere * 0.04 + materialSelective;
    float strength = BloomStrength * intentStrength * combatDampen * cinematicBoost;
    strength *= readabilityDampen * (1.0 - saturate((highlightProtection + ambientDarkness * night * 0.20 + materialSky * 0.18 + materialWater * 0.08 + materialNeon * 0.08) * HighlightRestraint * 0.52));
    strength = clamp(strength, 0.0, 0.32);

    float luma = Dalashade_AtmosphereBloomLuma(color);
    float brightWashGuard = 1.0 - smoothstep(0.72, 1.0, luma) * saturate(highlightProtection * 0.50 + materialWater * 0.12 + materialNeon * 0.16 + materialSky * 0.14);
    float3 glow = bloom * glowTint * strength * brightWashGuard;
    glow = min(glow, 0.18 + max(max(magicGlow, neonGlow), max(materialCrystal, materialNeon)) * 0.05);

    float3 result = color + glow * (1.0 - color * 0.45);
    result = min(result, color + 0.16);
    result = saturate(result);

    if (ShowDebugMask)
    {
        float materialDebugStrength = saturate(Dalashade_MaterialDebugStrength);
        float centerDepth = ReShade::GetLinearizedDepth(texcoord);
        float saturatedAccent = max(max(color.r, color.g), color.b) - min(min(color.r, color.g), color.b);
        float warmMask = smoothstep(0.03, 0.34, color.r - max(color.g, color.b) * 0.72);
        float coolAetherMask = smoothstep(0.06, 0.42, max(color.b, color.g) - color.r * 0.58);
        float neonColorMask = smoothstep(0.10, 0.55, saturatedAccent) * smoothstep(threshold - 0.22, 1.0, luma);
        Dalashade_MaterialMasks materialMasks = Dalashade_GetAllMaterialMasks(
            color,
            texcoord,
            0.0,
            materialWater,
            0.0,
            0.0,
            0.0,
            0.0,
            materialCrystal,
            materialNeon,
            materialFire,
            materialSky,
            0.0,
            0.0);
        float skyMask = materialMasks.SkyCloudFog;
        float waterMask = materialMasks.WaterSpecular;
        float aetherMask = materialMasks.CrystalAether * coolAetherMask * smoothstep(threshold - 0.18, 1.0, luma);
        float neonMask = materialMasks.NeonGlass * neonColorMask;
        float fireMask = materialMasks.FireLavaHeat * warmMask * smoothstep(threshold - 0.16, 1.0, luma) * (1.0 - combat * 0.58);
        float eligibility = saturate(Dalashade_AtmosphereBloomLuma(bloom) * 4.0);
        if (Dalashade_MaterialDebugMode == 1)
        {
            return float4(saturate(waterMask + fireMask) * materialDebugStrength, saturate(aetherMask + neonMask) * materialDebugStrength, saturate(skyMask + eligibility) * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 2)
        {
            return float4(materialWater * materialDebugStrength, waterMask * materialDebugStrength, brightWashGuard * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 3)
        {
            return float4(materialCrystal * materialDebugStrength, coolAetherMask * materialDebugStrength, aetherMask * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 4)
        {
            return float4(materialNeon * materialDebugStrength, neonColorMask * materialDebugStrength, neonMask * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 5)
        {
            return float4(materialFire * materialDebugStrength, fireMask * materialDebugStrength, combatDampen * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 6)
        {
            return float4(materialSky * materialDebugStrength, skyMask * materialDebugStrength, brightWashGuard * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 7)
        {
            return float4(eligibility * materialDebugStrength, strength * 4.0 * materialDebugStrength, brightWashGuard * materialDebugStrength, 1.0);
        }

        float sourceMask = saturate(Dalashade_AtmosphereBloomLuma(bloom) * 4.0);
        float accentMask = saturate(max(max(magicGlow, neonGlow), canopyGlow));
        float restraintMask = saturate(combat * 0.55 + highlightProtection * 0.45);
        return float4(sourceMask, accentMask, restraintMask, 1.0);
    }

    return float4(result, 1.0);
}

technique Dalashade_AtmosphereBloom
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_AtmosphereBloomPS;
    }
}
