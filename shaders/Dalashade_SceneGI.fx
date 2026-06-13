#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"

// Dalashade SceneGI is a lightweight screen-space GI-style approximation.
// It is not path tracing, RTGI, or PTGI. It combines shallow depth contact
// occlusion, material-aware ambient tint, and localized night light pooling.

uniform float Dalashade_GIEnabled <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Enabled";
> = 1.0;

uniform float Dalashade_GIStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Strength";
> = 0.35;

uniform float Dalashade_GIRadius <
    ui_type = "slider";
    ui_min = 0.20; ui_max = 2.0;
    ui_label = "Dalashade GI Radius";
> = 0.65;

uniform float Dalashade_GIBounceStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Bounce Strength";
> = 0.20;

uniform float Dalashade_GIAOIntensity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI AO Intensity";
> = 0.25;

uniform float Dalashade_GIAORadius <
    ui_type = "slider";
    ui_min = 0.20; ui_max = 2.0;
    ui_label = "Dalashade GI AO Radius";
> = 0.45;

uniform float Dalashade_GINightLightStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Night Light Strength";
> = 0.30;

uniform float Dalashade_GIMaterialInfluence <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Material Influence";
> = 0.50;

uniform float Dalashade_GISkyReject <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Sky Reject";
> = 1.0;

uniform float Dalashade_GISkinProtect <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Skin Protect";
> = 1.0;

uniform int Dalashade_GIDebugMode <
    ui_type = "combo";
    ui_items = "Off\0AO only\0Bounce only\0Night light pooling\0Material influence\0Sky rejection\0Skin protection\0Final GI influence\0Depth-normal confidence\0Emissive source\0Bounce receiver\0Adaptive limits/safety\0Layered AO breakdown\0";
    ui_label = "Dalashade GI Debug Mode";
> = 0;

uniform int Dalashade_GIDebugOutputMode <
    ui_type = "combo";
    ui_items = "Full replacement\0Alpha overlay over original\0Side-by-side split\0Contribution over black\0Amplified difference\0";
    ui_label = "Dalashade GI Debug Output Mode";
> = 0;

uniform float Dalashade_GIDebugOpacity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Debug Opacity";
> = 0.75;

uniform float Dalashade_GIDebugBoost <
    ui_type = "slider";
    ui_min = 0.25; ui_max = 8.0;
    ui_label = "Dalashade GI Debug Boost";
> = 2.50;

uniform float Dalashade_IntentReadability < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Readability"; > = 0.0;
uniform float Dalashade_IntentAtmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Atmosphere"; > = 0.0;
uniform float Dalashade_IntentHighlightProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Highlight Protection"; > = 0.0;
uniform float Dalashade_IntentShadowProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Shadow Protection"; > = 0.0;
uniform float Dalashade_IntentHaze < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Haze"; > = 0.0;
uniform float Dalashade_IntentWetness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Wetness"; > = 0.0;
uniform float Dalashade_IntentCold < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Cold"; > = 0.0;
uniform float Dalashade_IntentHeat < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Heat"; > = 0.0;
uniform float Dalashade_IntentMagicGlow < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Magic Glow"; > = 0.0;
uniform float Dalashade_IntentNeonGlow < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Neon Glow"; > = 0.0;
uniform float Dalashade_IntentFoliageDensity < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Foliage Density"; > = 0.0;
uniform float Dalashade_IntentIndustrialHardness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Industrial Hardness"; > = 0.0;
uniform float Dalashade_IntentCosmicMood < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Cosmic Mood"; > = 0.0;
uniform float Dalashade_IntentCombatPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Combat Pressure"; > = 0.0;
uniform float Dalashade_IntentCinematicPermission < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Cinematic Permission"; > = 0.0;

uniform float Dalashade_MaterialFoliage < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Foliage"; > = 0.0;
uniform float Dalashade_MaterialWaterPlane < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Water Plane"; > = 0.0;
uniform float Dalashade_MaterialSpecularGlint < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Specular Glint"; > = 0.0;
uniform float Dalashade_MaterialSandDust < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sand Dust"; > = 0.0;
uniform float Dalashade_MaterialSnowIce < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Snow Ice"; > = 0.0;
uniform float Dalashade_MaterialStoneRuins < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Stone Ruins"; > = 0.0;
uniform float Dalashade_MaterialMetalIndustrial < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Metal Industrial"; > = 0.0;
uniform float Dalashade_MaterialCrystalAether < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Crystal Aether"; > = 0.0;
uniform float Dalashade_MaterialNeonGlass < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Neon Glass"; > = 0.0;
uniform float Dalashade_MaterialFireLavaHeat < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Fire Lava Heat"; > = 0.0;
uniform float Dalashade_MaterialSkyCloudFog < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sky Cloud Fog"; > = 0.0;
uniform float Dalashade_MaterialSkinProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Skin Protection"; > = 0.0;
uniform float Dalashade_MaterialVoidDarkness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Void Darkness"; > = 0.0;

float Dalashade_SceneGILuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 Dalashade_SceneGIDepthNormal(float2 uv, float depth)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float dx = ReShade::GetLinearizedDepth(uv + float2(texel.x, 0.0)) - ReShade::GetLinearizedDepth(uv - float2(texel.x, 0.0));
    float dy = ReShade::GetLinearizedDepth(uv + float2(0.0, texel.y)) - ReShade::GetLinearizedDepth(uv - float2(0.0, texel.y));
    return normalize(float3(-dx * 24.0, -dy * 24.0, 1.0 + depth * 0.10));
}

float Dalashade_SceneGIDepthConfidence(float depth, float2 uv)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float left = ReShade::GetLinearizedDepth(uv - float2(texel.x, 0.0));
    float right = ReShade::GetLinearizedDepth(uv + float2(texel.x, 0.0));
    float up = ReShade::GetLinearizedDepth(uv - float2(0.0, texel.y));
    float down = ReShade::GetLinearizedDepth(uv + float2(0.0, texel.y));
    float valid = step(0.00001, depth) * step(depth, 0.99999);
    float spread = abs(depth - left) + abs(depth - right) + abs(depth - up) + abs(depth - down);
    return valid * saturate(1.0 - spread * 10.0);
}

float Dalashade_SceneGIAO(float2 uv, float depth, float radius, float normalConfidence)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float d1 = ReShade::GetLinearizedDepth(uv + float2(texel.x, 0.0));
    float d2 = ReShade::GetLinearizedDepth(uv - float2(texel.x, 0.0));
    float d3 = ReShade::GetLinearizedDepth(uv + float2(0.0, texel.y));
    float d4 = ReShade::GetLinearizedDepth(uv - float2(0.0, texel.y));
    float bias = 0.0015 + depth * 0.0020;
    float ao = 0.0;
    ao += saturate((depth - d1 - bias) * 24.0);
    ao += saturate((depth - d2 - bias) * 24.0);
    ao += saturate((depth - d3 - bias) * 24.0);
    ao += saturate((depth - d4 - bias) * 24.0);
    return saturate(ao * 0.25 * normalConfidence);
}

float3 Dalashade_SceneGINeighborAverage(float2 uv, float radius)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float3 c = tex2D(ReShade::BackBuffer, uv + float2(texel.x, 0.0)).rgb;
    c += tex2D(ReShade::BackBuffer, uv - float2(texel.x, 0.0)).rgb;
    c += tex2D(ReShade::BackBuffer, uv + float2(0.0, texel.y)).rgb;
    c += tex2D(ReShade::BackBuffer, uv - float2(0.0, texel.y)).rgb;
    return c * 0.25;
}

float Dalashade_SceneGIEmissiveCandidate(float3 sampleColor, float materialEmissiveBias)
{
    float sampleLuma = Dalashade_SceneGILuma(sampleColor);
    float sampleChroma = max(max(sampleColor.r, sampleColor.g), sampleColor.b) - min(min(sampleColor.r, sampleColor.g), sampleColor.b);
    float coolMagic = saturate(max(sampleColor.b - sampleColor.r * 0.42, sampleColor.g - sampleColor.r * 0.35));
    float warmFire = saturate(sampleColor.r - max(sampleColor.g, sampleColor.b) * 0.50);
    float saturatedGlow = smoothstep(0.10, 0.42, sampleChroma) * smoothstep(0.18, 0.82, sampleLuma);
    float brightGlow = smoothstep(0.48, 0.96, sampleLuma) * smoothstep(0.04, 0.34, sampleChroma + materialEmissiveBias);
    float magicGlow = smoothstep(0.05, 0.38, coolMagic) * smoothstep(0.13, 0.72, sampleLuma);
    float fireGlow = smoothstep(0.08, 0.42, warmFire) * smoothstep(0.15, 0.86, sampleLuma);
    return saturate(max(max(brightGlow, saturatedGlow * materialEmissiveBias), max(magicGlow, fireGlow) * (0.35 + materialEmissiveBias)));
}

float3 Dalashade_SceneGIPropagatedSource(float2 uv, float depth, float nearRadius, float midRadius, float materialEmissiveBias)
{
    float2 nearTexel = BUFFER_PIXEL_SIZE * nearRadius;
    float2 midTexel = BUFFER_PIXEL_SIZE * midRadius;
    float3 source = float3(0.0, 0.0, 0.0);
    float weight = 0.0001;

    float2 uv1 = uv + float2(nearTexel.x, 0.0);
    float3 c1 = tex2D(ReShade::BackBuffer, uv1).rgb;
    float d1 = ReShade::GetLinearizedDepth(uv1);
    float w1 = Dalashade_SceneGIEmissiveCandidate(c1, materialEmissiveBias) * saturate(1.0 - abs(depth - d1) * 16.0);
    source += c1 * w1;
    weight += w1;

    float2 uv2 = uv - float2(nearTexel.x, 0.0);
    float3 c2 = tex2D(ReShade::BackBuffer, uv2).rgb;
    float d2 = ReShade::GetLinearizedDepth(uv2);
    float w2 = Dalashade_SceneGIEmissiveCandidate(c2, materialEmissiveBias) * saturate(1.0 - abs(depth - d2) * 16.0);
    source += c2 * w2;
    weight += w2;

    float2 uv3 = uv + float2(0.0, nearTexel.y);
    float3 c3 = tex2D(ReShade::BackBuffer, uv3).rgb;
    float d3 = ReShade::GetLinearizedDepth(uv3);
    float w3 = Dalashade_SceneGIEmissiveCandidate(c3, materialEmissiveBias) * saturate(1.0 - abs(depth - d3) * 16.0);
    source += c3 * w3;
    weight += w3;

    float2 uv4 = uv - float2(0.0, nearTexel.y);
    float3 c4 = tex2D(ReShade::BackBuffer, uv4).rgb;
    float d4 = ReShade::GetLinearizedDepth(uv4);
    float w4 = Dalashade_SceneGIEmissiveCandidate(c4, materialEmissiveBias) * saturate(1.0 - abs(depth - d4) * 16.0);
    source += c4 * w4;
    weight += w4;

    float2 uv5 = uv + float2(midTexel.x, midTexel.y);
    float3 c5 = tex2D(ReShade::BackBuffer, uv5).rgb;
    float d5 = ReShade::GetLinearizedDepth(uv5);
    float w5 = Dalashade_SceneGIEmissiveCandidate(c5, materialEmissiveBias) * saturate(1.0 - abs(depth - d5) * 8.0) * 0.62;
    source += c5 * w5;
    weight += w5;

    float2 uv6 = uv + float2(-midTexel.x, midTexel.y);
    float3 c6 = tex2D(ReShade::BackBuffer, uv6).rgb;
    float d6 = ReShade::GetLinearizedDepth(uv6);
    float w6 = Dalashade_SceneGIEmissiveCandidate(c6, materialEmissiveBias) * saturate(1.0 - abs(depth - d6) * 8.0) * 0.62;
    source += c6 * w6;
    weight += w6;

    float2 uv7 = uv + float2(midTexel.x, -midTexel.y);
    float3 c7 = tex2D(ReShade::BackBuffer, uv7).rgb;
    float d7 = ReShade::GetLinearizedDepth(uv7);
    float w7 = Dalashade_SceneGIEmissiveCandidate(c7, materialEmissiveBias) * saturate(1.0 - abs(depth - d7) * 8.0) * 0.62;
    source += c7 * w7;
    weight += w7;

    float2 uv8 = uv + float2(-midTexel.x, -midTexel.y);
    float3 c8 = tex2D(ReShade::BackBuffer, uv8).rgb;
    float d8 = ReShade::GetLinearizedDepth(uv8);
    float w8 = Dalashade_SceneGIEmissiveCandidate(c8, materialEmissiveBias) * saturate(1.0 - abs(depth - d8) * 8.0) * 0.62;
    source += c8 * w8;
    weight += w8;

    return saturate(source / weight);
}

float3 Dalashade_SceneGIDebugOutput(float2 texcoord, float3 originalColor, float3 resultColor, float3 debugColor, float debugMask)
{
    float opacity = saturate(Dalashade_GIDebugOpacity);
    int outputMode = Dalashade_GIDebugOutputMode;
    float boost = max(Dalashade_GIDebugBoost, 0.001);
    float3 cleanDebug = saturate(float3(1.0, 1.0, 1.0) - exp(-max(debugColor, float3(0.0, 0.0, 0.0)) * boost));

    if (outputMode == 1)
    {
        return lerp(originalColor, cleanDebug, saturate(debugMask * opacity));
    }

    if (outputMode == 2)
    {
        float split = step(texcoord.x, 0.5);
        return lerp(originalColor, cleanDebug, split);
    }

    if (outputMode == 3)
    {
        return cleanDebug;
    }

    if (outputMode == 4)
    {
        float3 amplified = abs(resultColor - originalColor) * 14.0 + cleanDebug * 0.30;
        return saturate(float3(1.0, 1.0, 1.0) - exp(-amplified * boost));
    }

    return cleanDebug;
}

float4 Dalashade_SceneGIPS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    if (Dalashade_GIEnabled <= 0.0 || Dalashade_GIStrength <= 0.0)
    {
        return float4(color, 1.0);
    }

    float depth = ReShade::GetLinearizedDepth(texcoord);
    float3 normal = Dalashade_SceneGIDepthNormal(texcoord, depth);
    float normalConfidence = Dalashade_SceneGIDepthConfidence(depth, texcoord);
    float luma = Dalashade_SceneGILuma(color);
    float shadow = 1.0 - smoothstep(0.08, 0.42, luma);
    float midtone = Dalashade_RangeMask(luma, 0.12, 0.78);
    float bright = smoothstep(0.66, 0.98, luma);
    float saturatedAccent = max(max(color.r, color.g), color.b) - min(min(color.r, color.g), color.b);

    Dalashade_MaterialResolve material = Dalashade_ResolveMaterials(
        color,
        texcoord,
        Dalashade_MaterialFoliage,
        0.0,
        Dalashade_MaterialWaterPlane,
        Dalashade_MaterialSpecularGlint,
        Dalashade_MaterialSandDust,
        Dalashade_MaterialSnowIce,
        Dalashade_MaterialStoneRuins,
        Dalashade_MaterialMetalIndustrial,
        Dalashade_MaterialCrystalAether,
        Dalashade_MaterialNeonGlass,
        Dalashade_MaterialFireLavaHeat,
        Dalashade_MaterialSkyCloudFog,
        Dalashade_MaterialSkinProtection,
        Dalashade_MaterialVoidDarkness,
        0.0,
        0.0,
        0.0);
    Dalashade_WaterResolve water = Dalashade_ResolveWater(
        color,
        texcoord,
        Dalashade_MaterialWaterPlane,
        Dalashade_MaterialWaterPlane,
        Dalashade_MaterialWaterPlane,
        Dalashade_MaterialWaterPlane,
        Dalashade_IntentWetness,
        Dalashade_MaterialWaterPlane,
        Dalashade_MaterialSpecularGlint,
        Dalashade_MaterialSandDust,
        Dalashade_MaterialSkyCloudFog,
        Dalashade_MaterialSkinProtection,
        0.0,
        0.0,
        0.0);
    Dalashade_SafetyResolve safety = Dalashade_ResolveSafety(
        color,
        texcoord,
        material,
        water,
        Dalashade_IntentHighlightProtection,
        0.0,
        0.0,
        0.0);

    float skyReject = saturate(safety.SkyReject * Dalashade_GISkyReject);
    float skinProtect = saturate(safety.SkinReject * Dalashade_GISkinProtect);
    float combatDampen = 1.0 - saturate(Dalashade_IntentCombatPressure * 0.58 + Dalashade_IntentReadability * 0.16);
    float readabilityDampen = 1.0 - saturate(Dalashade_IntentReadability * 0.24 + Dalashade_IntentCombatPressure * 0.38);
    float highlightGuard = saturate(safety.HighlightProtect + material.SnowIce * bright * 0.42 + material.SandDust * bright * 0.20);
    float hardSurface = saturate(material.StoneRuins * 0.58 + material.MetalIndustrial * 0.50 + Dalashade_IntentIndustrialHardness * 0.34);
    float emissiveMaterial = saturate(material.FireLavaHeat * 0.72 + material.CrystalAether * 0.72 + material.NeonGlass * 0.70 + material.SpecularGlint * 0.34 + Dalashade_IntentMagicGlow * 0.20 + Dalashade_IntentNeonGlow * 0.18);
    float emissiveSourceMask = Dalashade_SceneGIEmissiveCandidate(color, emissiveMaterial);
    emissiveSourceMask = saturate(max(emissiveSourceMask, material.FireLavaHeat * 0.80));
    emissiveSourceMask = saturate(max(emissiveSourceMask, material.CrystalAether * smoothstep(0.12, 0.72, luma + saturatedAccent * 0.55)));
    emissiveSourceMask = saturate(max(emissiveSourceMask, material.NeonGlass * smoothstep(0.14, 0.76, luma + saturatedAccent * 0.60)));
    emissiveSourceMask = saturate(emissiveSourceMask * (1.0 - skyReject * 0.80) * (1.0 - skinProtect * 0.62));
    float aoMaterialBoost = saturate(1.08 + hardSurface * 0.48 + material.Foliage * 0.12 + material.VoidDarkness * 0.18 - material.SnowIce * 0.48 - material.SandDust * bright * 0.42 - water.WaterSurface * 0.68 - skinProtect * 0.82);

    float aoRadius = Dalashade_GIAORadius * (1.0 + Dalashade_GIRadius);
    float aoMicro = Dalashade_SceneGIAO(texcoord, depth, aoRadius * 0.58, normalConfidence) * 1.42;
    float aoMedium = Dalashade_SceneGIAO(texcoord, depth, aoRadius * 1.18, normalConfidence) * (0.82 + hardSurface * 0.38);
    float aoBroad = Dalashade_SceneGIAO(texcoord, depth, aoRadius * 2.15, normalConfidence) * (0.34 + hardSurface * 0.16 + material.Foliage * 0.08);
    float ao = saturate(aoMicro * 0.48 + aoMedium * 0.36 + aoBroad * 0.16);
    ao *= Dalashade_GIAOIntensity * aoMaterialBoost * combatDampen * readabilityDampen;
    ao *= 1.0 - skyReject;
    ao *= 1.0 - highlightGuard * 0.82;
    ao *= 1.0 - skinProtect * 0.78;

    float3 neighbor = Dalashade_SceneGINeighborAverage(texcoord, 1.25 + Dalashade_GIRadius * 1.35);
    float3 propagatedSource = Dalashade_SceneGIPropagatedSource(texcoord, depth, 2.0 + Dalashade_GIRadius * 2.2, 5.0 + Dalashade_GIRadius * 4.2, emissiveMaterial);
    float propagatedSourceMask = Dalashade_SceneGIEmissiveCandidate(propagatedSource, emissiveMaterial);
    float3 materialTint = float3(0.0, 0.0, 0.0);
    materialTint += material.Foliage * float3(0.16, 0.34, 0.10);
    materialTint += safety.FoliageNoiseReject * float3(0.08, 0.16, 0.05);
    materialTint += material.SandDust * float3(0.48, 0.30, 0.08);
    materialTint += material.SnowIce * float3(0.20, 0.29, 0.42);
    materialTint += material.WaterPlane * float3(0.03, 0.27, 0.34);
    materialTint += material.FireLavaHeat * float3(0.68, 0.22, 0.04);
    materialTint += material.CrystalAether * float3(0.19, 0.12, 0.56);
    materialTint += material.NeonGlass * float3(0.20, 0.30, 0.58);
    materialTint += material.MetalIndustrial * float3(0.09, 0.11, 0.15);
    materialTint += material.StoneRuins * float3(0.12, 0.10, 0.08);
    materialTint = saturate(materialTint);

    float neighborLuma = Dalashade_SceneGILuma(neighbor);
    float lowFrequencySurface = saturate(Dalashade_RangeMask(neighborLuma, 0.08, 0.82) * (0.72 + shadow * 0.34));
    float sharedMaterialConfidence = saturate(material.ReceiverConfidence + material.LightSourceConfidence * 0.35 + material.Foliage * 0.22 + material.SandDust * 0.18 + material.SnowIce * 0.18);
    float lowFrequencyMaterialRegion = saturate(lowFrequencySurface * (sharedMaterialConfidence * 0.46 + hardSurface * 0.24 + material.Foliage * 0.18 + safety.FoliageNoiseReject * 0.08 + water.WaterSurface * 0.10 + material.SandDust * 0.12 + material.SnowIce * 0.10));
    lowFrequencyMaterialRegion *= 1.0 - skyReject * 0.86;
    lowFrequencyMaterialRegion *= 1.0 - skinProtect * 0.70;
    lowFrequencyMaterialRegion *= 1.0 - bright * (0.24 + highlightGuard * 0.34);

    float materialInfluence = saturate(Dalashade_GIMaterialInfluence * (sharedMaterialConfidence * 0.72 + lowFrequencyMaterialRegion * 0.48));
    float materialBounceAllowance = saturate(material.Foliage * 0.30 + material.SandDust * 0.26 + material.SnowIce * 0.20 + water.WetShoreline * 0.18 + material.StoneRuins * 0.22 + material.MetalIndustrial * 0.18 + material.CrystalAether * 0.44 + material.NeonGlass * 0.42 + material.FireLavaHeat * 0.54);
    float bounceReceiverMask = saturate(midtone * (0.36 + shadow * 0.55) + lowFrequencyMaterialRegion * 0.30 + material.Foliage * 0.16 + material.StoneRuins * 0.16 + material.MetalIndustrial * 0.10 + material.SandDust * 0.12 + material.SnowIce * 0.10 + water.WetShoreline * 0.08);
    bounceReceiverMask *= saturate(0.55 + normalConfidence * 0.25 + max(normal.z, 0.0) * 0.20);
    bounceReceiverMask *= 1.0 - skyReject;
    bounceReceiverMask *= 1.0 - skinProtect * 0.82;
    bounceReceiverMask *= 1.0 - material.VoidDarkness * 0.72;
    bounceReceiverMask *= 1.0 - bright * (0.34 + highlightGuard * 0.38);
    float bounceMask = midtone * (0.44 + shadow * 0.62) * (1.0 - bright * (0.55 + highlightGuard * 0.35));
    bounceMask *= 1.0 - skyReject;
    bounceMask *= 1.0 - skinProtect * 0.85;
    bounceMask *= 1.0 - material.SpecularGlint * 0.20;
    bounceMask *= 1.0 - material.VoidDarkness * 0.82;
    float3 sourceTint = lerp(neighbor * (0.42 + materialBounceAllowance * 0.20), propagatedSource, saturate(propagatedSourceMask * 0.72 + emissiveMaterial * 0.28));
    float3 bounceTint = lerp(sourceTint, materialTint + propagatedSource * 0.38, saturate(materialInfluence * 1.18));
    float3 materialBounce = bounceTint * bounceMask * bounceReceiverMask * Dalashade_GIBounceStrength * combatDampen;
    materialBounce *= 0.90 + Dalashade_IntentAtmosphere * 0.30 + Dalashade_IntentCinematicPermission * 0.28 + materialBounceAllowance * 0.44 + propagatedSourceMask * 0.48;
    float3 bounce = materialBounce;

    float localLight = smoothstep(0.46, 0.95, luma) * smoothstep(0.025, 0.38, saturatedAccent + material.SpecularGlint * 0.42);
    localLight = max(localLight, emissiveSourceMask);
    localLight = max(localLight, propagatedSourceMask * bounceReceiverMask * 0.78);
    localLight = max(localLight, material.FireLavaHeat * bright);
    localLight = max(localLight, material.CrystalAether * smoothstep(0.30, 0.88, luma));
    localLight = max(localLight, material.NeonGlass * smoothstep(0.36, 0.92, luma));
    localLight = max(localLight, material.SpecularGlint * smoothstep(0.42, 0.94, luma) * 0.62);
    float nightContext = saturate(Dalashade_IntentShadowProtection * 0.40 + Dalashade_IntentAtmosphere * 0.16 + Dalashade_IntentCosmicMood * 0.22 + material.CrystalAether * 0.16 + material.NeonGlass * 0.18);
    float moonSurface = saturate(normal.z * normalConfidence * (material.SkyCloudFog * 0.22 + material.SnowIce * 0.24 + water.WetShoreline * 0.12 + material.SandDust * 0.06));
    float nightPool = saturate(localLight * (0.76 + material.SpecularGlint * 0.32 + emissiveMaterial * 0.46) + moonSurface * 0.30);
    nightPool = saturate(nightPool + propagatedSourceMask * bounceReceiverMask * (0.38 + emissiveMaterial * 0.42));
    nightPool *= Dalashade_GINightLightStrength * nightContext * combatDampen * (1.0 - skyReject * 0.76) * (1.0 - skinProtect * 0.64);
    float3 nightTint = lerp(float3(0.88, 0.78, 0.58), float3(0.58, 0.74, 1.0), saturate(Dalashade_IntentCold + Dalashade_IntentCosmicMood + material.CrystalAether));
    nightTint = lerp(nightTint, float3(0.55, 0.92, 1.0), saturate(Dalashade_IntentNeonGlow + material.NeonGlass) * 0.45);
    nightTint = lerp(nightTint, float3(1.0, 0.52, 0.20), material.FireLavaHeat * 0.68);
    nightTint = lerp(nightTint, float3(0.34, 0.90, 1.0), material.WaterPlane * 0.18);
    float3 nightLight = nightTint * nightPool * (0.28 + shadow * 0.44 + emissiveMaterial * 0.18);

    float strength = Dalashade_GIStrength * combatDampen;
    float3 result = color;
    result *= 1.0 - saturate(ao * strength);
    result += (bounce + nightLight) * strength;
    float nightAllowance = saturate(nightContext * 0.70 + shadow * 0.18);
    float scenePush = saturate(Dalashade_IntentAtmosphere * 0.34 + Dalashade_IntentCinematicPermission * 0.44 + nightAllowance * 0.52);
    float materialPush = saturate(materialBounceAllowance * 0.48 + emissiveMaterial * 0.76 + propagatedSourceMask * 0.45 + lowFrequencyMaterialRegion * 0.30 + hardSurface * 0.22);
    float highlightSafety = saturate(highlightGuard + skyReject * 0.80 + skinProtect * 0.76 + material.SnowIce * bright * 0.62 + material.WaterPlane * bright * 0.34);
    float positiveGIAllowance = 0.105 + scenePush * 0.145 + materialPush * 0.170;
    positiveGIAllowance *= 1.0 - saturate(Dalashade_IntentCombatPressure * 0.34 + Dalashade_IntentReadability * 0.20 + highlightSafety * 0.46);
    positiveGIAllowance = max(positiveGIAllowance, 0.050 + emissiveMaterial * 0.060 + propagatedSourceMask * bounceReceiverMask * 0.060);
    float negativeAOAllowance = 0.090 + hardSurface * 0.115 + material.StoneRuins * 0.050 + material.Foliage * 0.030 + material.VoidDarkness * 0.070;
    negativeAOAllowance *= 1.0 - saturate(skyReject * 0.88 + skinProtect * 0.80 + material.SnowIce * 0.44 + material.WaterPlane * 0.48 + highlightGuard * 0.38 + Dalashade_IntentCombatPressure * 0.20);
    negativeAOAllowance = max(negativeAOAllowance, 0.030 + hardSurface * 0.020);
    float3 delta = result - color;
    result = color + min(max(delta, float3(-negativeAOAllowance, -negativeAOAllowance, -negativeAOAllowance)), float3(positiveGIAllowance, positiveGIAllowance, positiveGIAllowance));
    result = saturate(result);

    float safetyClampMask = saturate(highlightSafety + skinProtect + skyReject + Dalashade_IntentCombatPressure * 0.45 + Dalashade_IntentReadability * 0.25);
    float finalGIContribution = saturate(Dalashade_SceneGILuma(abs(result - color)) * 9.0 + ao * 0.80 + Dalashade_SceneGILuma(bounce) * 4.2 + nightPool * 0.85);
    float finalInfluence = saturate(finalGIContribution);
    int mode = Dalashade_GIDebugMode;
    if (mode > 0)
    {
        float3 debugColor = float3(0.0, 0.0, 0.0);
        float debugMask = finalInfluence;
        if (mode == 1)
        {
            float aoMask = saturate(ao * 4.0);
            debugColor = aoMask.xxx;
            debugMask = ao;
        }
        else if (mode == 2)
        {
            float3 bounceContribution = saturate(bounce * 7.0);
            debugColor = bounceContribution;
            debugMask = saturate(Dalashade_SceneGILuma(debugColor));
        }
        else if (mode == 3)
        {
            float nightLightMask = saturate(nightPool * 3.0);
            debugColor = nightLightMask.xxx * nightTint;
            debugMask = nightPool;
        }
        else if (mode == 4)
        {
            float materialInfluenceMask = materialInfluence;
            debugColor = lerp(materialInfluenceMask.xxx, Dalashade_GetMaterialDebugColor(material), 0.72);
            debugMask = saturate(max(sharedMaterialConfidence, max(material.ReceiverConfidence, material.LightSourceConfidence)));
        }
        else if (mode == 5)
        {
            float skyRejectMask = skyReject;
            debugColor = float3(0.10 * skyRejectMask, 0.42 * skyRejectMask, skyRejectMask);
            debugMask = saturate(max(skyReject, material.SkyCloudFog));
        }
        else if (mode == 6)
        {
            float skinProtectMask = skinProtect;
            debugColor = float3(skinProtectMask, 0.58 * skinProtectMask, 0.42 * skinProtectMask);
            debugMask = skinProtect;
        }
        else if (mode == 7)
        {
            float3 contribution = saturate(abs(result - color) * 8.0);
            debugColor = max(contribution, float3(ao, Dalashade_SceneGILuma(bounce) * 4.0, nightPool));
            debugMask = finalInfluence;
        }
        else if (mode == 8)
        {
            debugColor = float3(normalConfidence, normal.z * 0.5 + 0.5, depth);
            debugMask = max(normalConfidence, 0.35);
        }
        else if (mode == 9)
        {
            float sourceMask = saturate(emissiveSourceMask + propagatedSourceMask * 0.80);
            debugColor = lerp(float3(sourceMask, sourceMask * 0.25, 0.08), float3(0.10, 0.78, 1.0), saturate(material.CrystalAether + material.NeonGlass));
            debugColor = lerp(debugColor, float3(1.0, 0.34, 0.05), material.FireLavaHeat);
            debugColor *= sourceMask;
            debugMask = sourceMask;
        }
        else if (mode == 10)
        {
            debugColor = float3(lowFrequencyMaterialRegion * 0.35, bounceReceiverMask, propagatedSourceMask * bounceReceiverMask);
            debugMask = bounceReceiverMask;
        }
        else if (mode == 11)
        {
            debugColor = saturate(float3(max(safetyClampMask, safety.HighlightProtect), positiveGIAllowance * 4.0, negativeAOAllowance * 4.0));
            debugMask = saturate(max(max(safetyClampMask, safety.HighlightProtect), max(positiveGIAllowance * 4.0, negativeAOAllowance * 4.0)));
        }
        else if (mode == 12)
        {
            debugColor = float3(saturate(aoMicro * 4.0), saturate(aoMedium * 4.0), saturate(aoBroad * 4.0));
            debugMask = saturate(max(max(aoMicro, aoMedium), aoBroad) * 4.0);
        }

        return float4(Dalashade_SceneGIDebugOutput(texcoord, color, result, debugColor, debugMask), 1.0);
    }

    return float4(result, 1.0);
}

technique Dalashade_SceneGI
{
    pass SceneGI
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_SceneGIPS;
    }
}
