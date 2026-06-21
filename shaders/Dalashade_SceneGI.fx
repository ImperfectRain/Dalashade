#include "ReShade.fxh"
#include "Dalashade_FrameData.fxh"

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
> = 0.45;

uniform float Dalashade_GIRadius <
    ui_type = "slider";
    ui_min = 0.20; ui_max = 2.0;
    ui_label = "Dalashade GI Radius";
> = 0.65;

uniform float Dalashade_GIBounceStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Bounce Strength";
> = 0.30;

uniform float Dalashade_GIAOIntensity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI AO Intensity";
> = 0.30;

uniform float Dalashade_GIAORadius <
    ui_type = "slider";
    ui_min = 0.20; ui_max = 2.0;
    ui_label = "Dalashade GI AO Radius";
> = 0.45;

uniform float Dalashade_GINightLightStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Night Light Strength";
> = 0.42;

uniform float Dalashade_GIMaterialInfluence <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Material Influence";
> = 0.58;

uniform float Dalashade_StandaloneStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Standalone Stack Strength";
    ui_tooltip = "0 keeps SceneGI supportive for an existing preset. 1 lets it provide more visible GI/contact/bounce while retaining safety gates.";
> = 0.0;

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
    ui_items = "Off\0AO only\0Bounce only\0Night light pooling\0Material influence\0Sky rejection\0Skin protection\0Final GI influence\0Depth-normal confidence\0Emissive source\0Bounce receiver\0Adaptive limits/safety\0Layered AO breakdown\0Clamp pressure\0SSGI diffuse gather\0Material bounce lanes\0Sky-safe receivers\0Emissive pooling lanes\0Dalapad contribution\0Dalapad raw evidence\0";
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
uniform float Dalashade_Night < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Night"; > = 0.0;
uniform float Dalashade_Moonlight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Moonlight"; > = 0.0;
uniform float Dalashade_ArtificialLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Artificial Light"; > = 0.0;
uniform float Dalashade_AmbientDarkness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Ambient Darkness"; > = 0.0;
uniform float Dalashade_NightAtmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Night Atmosphere"; > = 0.0;
uniform float Dalashade_Daylight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Daylight"; > = 0.0;
uniform float Dalashade_Sunlight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Sunlight"; > = 0.0;
uniform float Dalashade_OpenSkyLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Open Sky Light"; > = 0.0;
uniform float Dalashade_SurfaceHeat < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Surface Heat"; > = 0.0;
uniform float Dalashade_DayAtmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Day Atmosphere"; > = 0.0;
uniform float Dalashade_DayReflection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Day Reflection"; > = 0.0;
uniform float Dalashade_DayHighlightPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Day Highlight Pressure"; > = 0.0;
uniform float Dalashade_IntentCombatPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Combat Pressure"; > = 0.0;
uniform float Dalashade_IntentCinematicPermission < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Cinematic Permission"; > = 0.0;

uniform float Dalashade_MaterialFoliage < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Foliage"; > = 0.0;
uniform float Dalashade_MaterialWaterPlane < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Water Plane"; > = 0.0;
uniform float Dalashade_MaterialSpecularGlint < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Specular Glint"; > = 0.0;
uniform float Dalashade_WaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Water Context"; > = 0.0;
uniform float Dalashade_CoastalContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Coastal Context"; > = 0.0;
uniform float Dalashade_OpenOceanContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Open Ocean Context"; > = 0.0;
uniform float Dalashade_ShallowWaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Shallow Water Context"; > = 0.0;
uniform float Dalashade_WetSurfaceContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Wet Surface Context"; > = 0.0;
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

uniform bool Dalashade_EnableDepthAssist < ui_label = "Enable Depth Assist"; > = false;
uniform float Dalashade_DepthAssistStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Strength"; > = 0.0;
uniform float Dalashade_DepthAssistConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Confidence Floor"; > = 0.0;
uniform float Dalashade_DepthConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Confidence Floor"; > = 0.0;

uniform float Dalashade_NormalFieldEnabled < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Enabled"; > = 0.0;
uniform float Dalashade_NormalFieldStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Strength"; > = 0.0;
uniform float Dalashade_NormalDepthStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Depth Strength"; > = 0.0;
uniform float Dalashade_NormalDetailStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Detail Strength"; > = 0.0;
uniform float Dalashade_NormalMaterialInfluence < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Material Influence"; > = 0.0;
uniform float Dalashade_NormalWaterSuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Water Suppression"; > = 0.80;
uniform float Dalashade_NormalSkinSuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Skin Suppression"; > = 0.90;
uniform float Dalashade_NormalSkySuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Sky/Fog Suppression"; > = 0.95;

uniform float Dalashade_DalapadSceneGINormalAssist < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalapad SceneGI Normal Assist"; > = 0.0;
uniform float Dalashade_DalapadSceneGINormalStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalapad SceneGI Normal Strength"; > = 0.35;

float Dalashade_SceneGILuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 Dalashade_SceneGIDepthNormal(float2 uv, float depth)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float dx = ReShade::GetLinearizedDepth(saturate(uv + float2(texel.x, 0.0))) - ReShade::GetLinearizedDepth(saturate(uv - float2(texel.x, 0.0)));
    float dy = ReShade::GetLinearizedDepth(saturate(uv + float2(0.0, texel.y))) - ReShade::GetLinearizedDepth(saturate(uv - float2(0.0, texel.y)));
    return normalize(float3(-dx * 24.0, -dy * 24.0, 1.0 + depth * 0.10));
}

float Dalashade_SceneGIDepthConfidence(float depth, float2 uv)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float left = ReShade::GetLinearizedDepth(saturate(uv - float2(texel.x, 0.0)));
    float right = ReShade::GetLinearizedDepth(saturate(uv + float2(texel.x, 0.0)));
    float up = ReShade::GetLinearizedDepth(saturate(uv - float2(0.0, texel.y)));
    float down = ReShade::GetLinearizedDepth(saturate(uv + float2(0.0, texel.y)));
    float valid = step(0.00001, depth) * step(depth, 0.99999);
    float spread = abs(depth - left) + abs(depth - right) + abs(depth - up) + abs(depth - down);
    return valid * saturate(1.0 - spread * 10.0);
}

float Dalashade_SceneGIAO(float2 uv, float depth, float radius, float normalConfidence)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float d1 = ReShade::GetLinearizedDepth(saturate(uv + float2(texel.x, 0.0)));
    float d2 = ReShade::GetLinearizedDepth(saturate(uv - float2(texel.x, 0.0)));
    float d3 = ReShade::GetLinearizedDepth(saturate(uv + float2(0.0, texel.y)));
    float d4 = ReShade::GetLinearizedDepth(saturate(uv - float2(0.0, texel.y)));
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
    float3 c = tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, 0.0))).rgb;
    c += tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x, 0.0))).rgb;
    c += tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y))).rgb;
    c += tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y))).rgb;
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

float4 Dalashade_SceneGISSGISample(float2 uv, float depth, float3 normal, float2 dir, float radius, float materialEmissiveBias, float sourceLightBias, float receiverConfidence)
{
    float2 sampleUv = saturate(uv + dir * BUFFER_PIXEL_SIZE * radius);
    float3 sampleColor = tex2D(ReShade::BackBuffer, sampleUv).rgb;
    float sampleDepth = ReShade::GetLinearizedDepth(sampleUv);
    float sampleLuma = Dalashade_SceneGILuma(sampleColor);
    float sampleChroma = max(max(sampleColor.r, sampleColor.g), sampleColor.b) - min(min(sampleColor.r, sampleColor.g), sampleColor.b);
    float depthDelta = abs(depth - sampleDepth);
    float continuity = saturate(1.0 - depthDelta * (7.0 + radius * 0.18));
    float nearOccluder = smoothstep(0.0006, 0.030, depth - sampleDepth);
    float sameSurface = saturate(1.0 - depthDelta * 28.0);
    float facing = saturate(0.50 + dot(normal.xy, dir) * 0.36 + normal.z * 0.20);
    float visibleSource = smoothstep(0.055, 0.82, sampleLuma) * (0.52 + sampleChroma * 0.46);
    float emissiveSource = Dalashade_SceneGIEmissiveCandidate(sampleColor, materialEmissiveBias) * (0.70 + sourceLightBias * 0.42);
    float contactEnergy = max(nearOccluder * 0.64, sameSurface * 0.28);
    float weight = saturate((visibleSource * 0.72 + emissiveSource * 0.92) * continuity * facing * (0.34 + contactEnergy) * receiverConfidence);
    return float4(sampleColor * weight, weight);
}

float4 Dalashade_SceneGIScreenDiffuseGather(float2 uv, float depth, float3 normal, float radius, float materialEmissiveBias, float sourceLightBias, float receiverConfidence)
{
    float4 gathered = float4(0.0, 0.0, 0.0, 0.0001);
    gathered += Dalashade_SceneGISSGISample(uv, depth, normal, normalize(float2(1.0, 0.0)), radius * 1.15, materialEmissiveBias, sourceLightBias, receiverConfidence);
    gathered += Dalashade_SceneGISSGISample(uv, depth, normal, normalize(float2(-1.0, 0.0)), radius * 1.15, materialEmissiveBias, sourceLightBias, receiverConfidence);
    gathered += Dalashade_SceneGISSGISample(uv, depth, normal, normalize(float2(0.0, 1.0)), radius * 1.00, materialEmissiveBias, sourceLightBias, receiverConfidence);
    gathered += Dalashade_SceneGISSGISample(uv, depth, normal, normalize(float2(0.0, -1.0)), radius * 1.00, materialEmissiveBias, sourceLightBias, receiverConfidence);
    gathered += Dalashade_SceneGISSGISample(uv, depth, normal, normalize(float2(0.76, 0.52)), radius * 1.75, materialEmissiveBias, sourceLightBias, receiverConfidence);
    gathered += Dalashade_SceneGISSGISample(uv, depth, normal, normalize(float2(-0.76, 0.52)), radius * 1.75, materialEmissiveBias, sourceLightBias, receiverConfidence);
    gathered += Dalashade_SceneGISSGISample(uv, depth, normal, normalize(float2(0.62, -0.78)), radius * 2.30, materialEmissiveBias, sourceLightBias, receiverConfidence);
    gathered += Dalashade_SceneGISSGISample(uv, depth, normal, normalize(float2(-0.62, -0.78)), radius * 2.30, materialEmissiveBias, sourceLightBias, receiverConfidence);

    float3 color = gathered.rgb / gathered.a;
    float confidence = saturate(gathered.a * 0.42);
    return float4(saturate(color), confidence);
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

    Dalashade_FrameSceneSettings sceneSettings = Dalashade_FrameScene_DefaultSettings();
    sceneSettings.Readability = Dalashade_IntentReadability;
    sceneSettings.Atmosphere = Dalashade_IntentAtmosphere;
    sceneSettings.HighlightProtection = Dalashade_IntentHighlightProtection;
    sceneSettings.ShadowProtection = Dalashade_IntentShadowProtection;
    sceneSettings.Haze = Dalashade_IntentHaze;
    sceneSettings.Wetness = Dalashade_IntentWetness;
    sceneSettings.Cold = Dalashade_IntentCold;
    sceneSettings.Heat = Dalashade_IntentHeat;
    sceneSettings.MagicGlow = Dalashade_IntentMagicGlow;
    sceneSettings.NeonGlow = Dalashade_IntentNeonGlow;
    sceneSettings.FoliageDensity = Dalashade_IntentFoliageDensity;
    sceneSettings.IndustrialHardness = Dalashade_IntentIndustrialHardness;
    sceneSettings.CosmicMood = Dalashade_IntentCosmicMood;
    sceneSettings.CinematicPermission = Dalashade_IntentCinematicPermission;
    sceneSettings.CombatPressure = Dalashade_IntentCombatPressure;
    sceneSettings.Night = Dalashade_Night;
    sceneSettings.Moonlight = Dalashade_Moonlight;
    sceneSettings.ArtificialLight = Dalashade_ArtificialLight;
    sceneSettings.AmbientDarkness = Dalashade_AmbientDarkness;
    sceneSettings.NightAtmosphere = Dalashade_NightAtmosphere;
    sceneSettings.Daylight = Dalashade_Daylight;
    sceneSettings.Sunlight = Dalashade_Sunlight;
    sceneSettings.OpenSkyLight = Dalashade_OpenSkyLight;
    sceneSettings.SurfaceHeat = Dalashade_SurfaceHeat;
    sceneSettings.DayAtmosphere = Dalashade_DayAtmosphere;
    sceneSettings.DayReflection = Dalashade_DayReflection;
    sceneSettings.DayHighlightPressure = Dalashade_DayHighlightPressure;
    sceneSettings.StandaloneStrength = Dalashade_StandaloneStrength;
    Dalashade_FrameSceneData scene = Dalashade_ResolveFrameSceneData(sceneSettings);

    Dalashade_FrameDataSettings frameSettings = Dalashade_FrameData_DefaultSettings();
    frameSettings.MaterialFoliage = Dalashade_MaterialFoliage;
    frameSettings.MaterialWaterSpecular = 0.0;
    frameSettings.MaterialWaterPlane = Dalashade_MaterialWaterPlane;
    frameSettings.MaterialSpecularGlint = Dalashade_MaterialSpecularGlint;
    frameSettings.MaterialSandDust = Dalashade_MaterialSandDust;
    frameSettings.MaterialSnowIce = Dalashade_MaterialSnowIce;
    frameSettings.MaterialStoneRuins = Dalashade_MaterialStoneRuins;
    frameSettings.MaterialMetalIndustrial = Dalashade_MaterialMetalIndustrial;
    frameSettings.MaterialCrystalAether = Dalashade_MaterialCrystalAether;
    frameSettings.MaterialNeonGlass = Dalashade_MaterialNeonGlass;
    frameSettings.MaterialFireLavaHeat = Dalashade_MaterialFireLavaHeat;
    frameSettings.MaterialSkyCloudFog = Dalashade_MaterialSkyCloudFog;
    frameSettings.MaterialSkinProtection = Dalashade_MaterialSkinProtection;
    frameSettings.MaterialVoidDarkness = Dalashade_MaterialVoidDarkness;
    frameSettings.WaterContext = Dalashade_WaterContext;
    frameSettings.CoastalContext = Dalashade_CoastalContext;
    frameSettings.OpenOceanContext = Dalashade_OpenOceanContext;
    frameSettings.ShallowWaterContext = Dalashade_ShallowWaterContext;
    frameSettings.WetSurfaceContext = max(Dalashade_IntentWetness, Dalashade_WetSurfaceContext);
    frameSettings.HighlightProtection = Dalashade_IntentHighlightProtection;
    frameSettings.DepthAssistEnabled = Dalashade_EnableDepthAssist ? 1.0 : 0.0;
    frameSettings.DepthAssistStrength = Dalashade_DepthAssistStrength;
    frameSettings.DepthAssistConfidenceFloor = max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor);
    frameSettings.NormalFieldEnabled = Dalashade_NormalFieldEnabled;
    frameSettings.NormalFieldStrength = Dalashade_NormalFieldStrength;
    frameSettings.NormalDepthStrength = Dalashade_NormalDepthStrength;
    frameSettings.NormalDetailStrength = Dalashade_NormalDetailStrength;
    frameSettings.NormalMaterialInfluence = Dalashade_NormalMaterialInfluence;
    frameSettings.NormalWaterSuppression = Dalashade_NormalWaterSuppression;
    frameSettings.NormalSkinSuppression = Dalashade_NormalSkinSuppression;
    frameSettings.NormalSkySuppression = Dalashade_NormalSkySuppression;
    frameSettings.DalapadSurfaceDataEnabled = max(Dalashade_DalapadSurfaceDataEnabled, Dalashade_DalapadSceneGINormalAssist);
    frameSettings.DalapadSurfaceDataStrength = max(Dalashade_DalapadSurfaceDataStrength, Dalashade_DalapadSceneGINormalStrength);
    Dalashade_FrameBaseData frame = Dalashade_ResolveFrameBaseData(color, texcoord, frameSettings);
    Dalashade_FrameSurfaceData surface = Dalashade_ResolveFrameSurfaceData(color, texcoord, frame, frameSettings);
    normal = normalize(lerp(normal, surface.Normal, surface.SurfaceDataInfluence));
    normalConfidence = saturate(max(normalConfidence, surface.NormalConfidence * (0.70 + surface.DalapadInfluence * 0.25)));

    float skyReject = saturate(frame.SafetySkyReject * Dalashade_GISkyReject);
    float skinProtect = saturate(frame.SafetySkinReject * Dalashade_GISkinProtect);
    float normalFieldInfluence = saturate(max(Dalashade_NormalFieldEnabled * Dalashade_NormalFieldStrength * Dalashade_NormalMaterialInfluence, surface.SurfaceDataInfluence));
    float normalReceiverSafety = saturate(
        (1.0 - skyReject * 0.92)
        * (1.0 - skinProtect * 0.88)
        * (1.0 - frame.SafetyWaterAOReject * 0.82)
        * (1.0 - frame.WaterReceiver * 0.62)
        * (1.0 - frame.MaterialSkyCloudFog * 0.72));
    float dalapadContributionSupport = saturate(surface.DalapadInfluence * normalReceiverSafety);
    float dalapadStructureSupport = saturate(surface.DalapadStructureSupport * normalReceiverSafety);
    float normalStructureSupport = saturate(
        normalFieldInfluence
        * surface.StructureCandidate
        * (0.32 + surface.NormalConfidence * 0.30 + surface.OrientationConfidence * 0.18)
        * normalReceiverSafety
        + dalapadStructureSupport * surface.StructureCandidate * 0.18);
    float normalAOContactSupport = saturate(
        normalFieldInfluence
        * surface.AOReceiverSupport
        * (0.36 + surface.NormalConfidence * 0.28 + surface.OrientationConfidence * 0.18)
        * normalReceiverSafety
        + dalapadContributionSupport * surface.AOReceiverSupport * 0.10
        + dalapadStructureSupport * surface.AOReceiverSupport * 0.08);
    float normalGroundContactSupport = saturate(
        normalFieldInfluence
        * surface.GroundCandidate
        * (0.24 + surface.NormalConfidence * 0.20 + surface.OrientationConfidence * 0.18)
        * normalReceiverSafety
        + dalapadContributionSupport * surface.GroundCandidate * 0.06
        + dalapadStructureSupport * surface.GroundCandidate * 0.06);
    float normalEdgeContact = saturate(
        normalFieldInfluence
        * surface.EdgeDiscontinuity
        * (0.28 + surface.NormalConfidence * 0.22)
        * normalReceiverSafety
        * (1.0 - frame.SafetyHighlightProtect * 0.42)
        + dalapadStructureSupport * surface.EdgeDiscontinuity * 0.16);
    float combatDampen = scene.GameplayDampen;
    float readabilityDampen = scene.ReadabilityDampen;
    float standaloneGI = scene.StandaloneSafe;
    float dayPush = saturate(scene.DayOpenAir * 0.68 + scene.DayReflection * 0.18 + scene.DayHighlightPressure * 0.08);
    float wetGILane = saturate(scene.WetAir * 0.55 + frame.WaterWetShoreline * 0.30 + frame.MaterialWaterPlane * 0.12);
    float heatGILane = saturate(scene.HeatAir * 0.58 + frame.MaterialFireLavaHeat * 0.28 + frame.MaterialSandDust * 0.18);
    float coldGILane = saturate(scene.ColdAir * 0.60 + frame.MaterialSnowIce * 0.34 + scene.Moonlight * 0.12);
    float aetherTechGILane = saturate(scene.AetherTech * 0.62 + frame.MaterialCrystalAether * 0.44 + frame.MaterialNeonGlass * 0.36);
    float forestCanopyGILane = saturate(scene.ForestCanopy * 0.64 + frame.MaterialFoliage * 0.34 + frame.SafetyFoliageNoiseReject * 0.14);
    float industrialGILane = saturate(scene.Industrial * 0.58 + frame.MaterialMetalIndustrial * 0.34 + frame.MaterialStoneRuins * 0.12);
    float interiorGILane = saturate(scene.InteriorMood * 0.68 + scene.AmbientDarkness * 0.18 + frame.MaterialStoneRuins * 0.16);
    float authoredGILane = saturate(max(max(interiorGILane, aetherTechGILane), max(max(forestCanopyGILane, industrialGILane), max(heatGILane, coldGILane))) + wetGILane * 0.35);
    float expressiveGILift = 1.0 + authoredGILane * (0.10 + standaloneGI * 0.18) + scene.CinematicPermission * standaloneGI * 0.08;
    float highlightGuard = saturate(frame.SafetyHighlightProtect + scene.DayHighlightPressure * bright * 0.55 + frame.MaterialSnowIce * bright * 0.42 + frame.MaterialSandDust * bright * 0.20);
    float hardSurface = saturate(frame.MaterialStoneRuins * 0.58 + frame.MaterialMetalIndustrial * 0.50 + industrialGILane * 0.26 + interiorGILane * 0.10);
    float emissiveMaterial = saturate(frame.MaterialFireLavaHeat * 0.72 + frame.MaterialCrystalAether * 0.72 + frame.MaterialNeonGlass * 0.70 + frame.WaterSpecularGlint * 0.34 + aetherTechGILane * 0.24 + scene.MagicGlow * 0.12 + scene.NeonGlow * 0.10);
    float sourceLightLane = saturate(frame.SourceLightConfidence * 0.52 + emissiveMaterial * 0.42 + aetherTechGILane * 0.24 + heatGILane * 0.16 + scene.NightLocalLight * 0.10);
    float emissiveSourceMask = Dalashade_SceneGIEmissiveCandidate(color, emissiveMaterial);
    emissiveSourceMask = saturate(max(emissiveSourceMask, sourceLightLane * smoothstep(0.18, 0.82, luma + saturatedAccent * 0.36)));
    emissiveSourceMask = saturate(max(emissiveSourceMask, frame.MaterialFireLavaHeat * 0.80));
    emissiveSourceMask = saturate(max(emissiveSourceMask, frame.MaterialCrystalAether * smoothstep(0.12, 0.72, luma + saturatedAccent * 0.55)));
    emissiveSourceMask = saturate(max(emissiveSourceMask, frame.MaterialNeonGlass * smoothstep(0.14, 0.76, luma + saturatedAccent * 0.60)));
    emissiveSourceMask = saturate(emissiveSourceMask * (1.0 - skyReject * 0.80) * (1.0 - skinProtect * 0.62));
    float receiverSkySafety = saturate((1.0 - skyReject * 0.94) * (1.0 - frame.MaterialSkyCloudFog * 0.76) * (1.0 - frame.WaterHorizonOnly * 0.58));
    float receiverMaterialSafety = saturate(receiverSkySafety * (1.0 - skinProtect * 0.78) * (1.0 - frame.SafetyWaterAOReject * 0.48) * (1.0 - highlightGuard * 0.28));
    float foliageBounceLane = saturate((frame.MaterialFoliage * 0.74 + frame.SafetyFoliageNoiseReject * 0.24 + forestCanopyGILane * 0.34) * receiverMaterialSafety);
    float stoneBounceLane = saturate((frame.MaterialStoneRuins * 0.66 + interiorGILane * 0.26 + hardSurface * 0.18 + normalStructureSupport * 0.10) * receiverMaterialSafety);
    float metalBounceLane = saturate((frame.MaterialMetalIndustrial * 0.64 + industrialGILane * 0.30 + frame.MaterialSurfaceHardness * 0.12 + normalStructureSupport * 0.08) * receiverMaterialSafety);
    float climateBounceLane = saturate((frame.MaterialSnowIce * (0.42 + coldGILane * 0.22) + frame.MaterialSandDust * (0.46 + heatGILane * 0.18)) * receiverMaterialSafety * (1.0 - bright * 0.36));
    float wetBounceLane = saturate((frame.MaterialWaterPlane * 0.22 + frame.WaterWetShoreline * 0.34 + wetGILane * 0.26) * receiverSkySafety * (1.0 - frame.SafetyWaterAOReject * 0.74) * (1.0 - bright * 0.42));
    float baseEmissivePoolingLane = saturate((emissiveMaterial * 0.50 + sourceLightLane * 0.34 + emissiveSourceMask * 0.22) * receiverMaterialSafety);
    float materialBounceLaneEnergy = saturate(max(max(foliageBounceLane, max(stoneBounceLane, metalBounceLane)), max(climateBounceLane, wetBounceLane)) + baseEmissivePoolingLane * 0.40);
    float3 laneMaterialTint = float3(0.0, 0.0, 0.0);
    laneMaterialTint += foliageBounceLane * float3(0.14, 0.32, 0.10);
    laneMaterialTint += stoneBounceLane * float3(0.13, 0.11, 0.09);
    laneMaterialTint += metalBounceLane * float3(0.08, 0.10, 0.14);
    laneMaterialTint += frame.MaterialSandDust * climateBounceLane * float3(0.46, 0.30, 0.09);
    laneMaterialTint += frame.MaterialSnowIce * climateBounceLane * float3(0.19, 0.28, 0.42);
    laneMaterialTint += wetBounceLane * float3(0.03, 0.23, 0.30);
    laneMaterialTint += baseEmissivePoolingLane * float3(0.22, 0.22, 0.36);
    laneMaterialTint = saturate(laneMaterialTint);
    float aoMaterialBoost = saturate(1.08 + hardSurface * 0.48 + forestCanopyGILane * 0.10 + interiorGILane * 0.16 + frame.MaterialVoidDarkness * 0.18 - coldGILane * 0.18 - frame.MaterialSnowIce * 0.34 - frame.MaterialSandDust * bright * 0.42 - frame.WaterSurface * 0.68 - skinProtect * 0.82);

    float aoRadius = Dalashade_GIAORadius * (1.0 + Dalashade_GIRadius);
    float aoMicro = Dalashade_SceneGIAO(texcoord, depth, aoRadius * 0.58, normalConfidence) * 1.42;
    float aoMedium = Dalashade_SceneGIAO(texcoord, depth, aoRadius * 1.18, normalConfidence) * (0.82 + hardSurface * 0.38);
    float aoBroad = Dalashade_SceneGIAO(texcoord, depth, aoRadius * 2.15, normalConfidence) * (0.34 + hardSurface * 0.16 + frame.MaterialFoliage * 0.08);
    float aoStructureBroad = Dalashade_SceneGIAO(texcoord, depth, aoRadius * 3.10, normalConfidence) * (0.18 + hardSurface * 0.22 + interiorGILane * 0.16 + industrialGILane * 0.12);
    float laneContactAO = saturate((frame.ReceiverAO * 0.36 + frame.ReceiverStructure * 0.28 + surface.AOReceiverSupport * normalFieldInfluence * 0.22 + surface.GroundCandidate * normalFieldInfluence * 0.12) * (hardSurface * 0.28 + interiorGILane * 0.22 + forestCanopyGILane * 0.10 + industrialGILane * 0.12) * normalReceiverSafety);
    float ao = saturate(aoMicro * 0.44 + aoMedium * 0.34 + aoBroad * 0.15 + aoStructureBroad * 0.07);
    ao = saturate(ao + normalAOContactSupport * 0.070 + normalGroundContactSupport * 0.042 + normalEdgeContact * 0.034 + laneContactAO * 0.050);
    ao *= Dalashade_GIAOIntensity * aoMaterialBoost * combatDampen * readabilityDampen * lerp(1.0, 1.18, standaloneGI) * expressiveGILift;
    ao *= 1.0 - skyReject;
    ao *= 1.0 - highlightGuard * 0.82;
    ao *= 1.0 - skinProtect * 0.78;

    float3 neighbor = Dalashade_SceneGINeighborAverage(texcoord, 1.25 + Dalashade_GIRadius * 1.35);
    float3 materialTint = float3(0.0, 0.0, 0.0);
    materialTint += frame.MaterialFoliage * float3(0.16, 0.34, 0.10) * (1.0 + forestCanopyGILane * 0.18);
    materialTint += frame.SafetyFoliageNoiseReject * float3(0.08, 0.16, 0.05);
    materialTint += frame.MaterialSandDust * float3(0.48, 0.30, 0.08) * (1.0 + heatGILane * 0.22);
    materialTint += frame.MaterialSnowIce * float3(0.20, 0.29, 0.42) * (1.0 + coldGILane * 0.14);
    materialTint += frame.MaterialWaterPlane * float3(0.03, 0.27, 0.34) * (1.0 + wetGILane * 0.12);
    materialTint += frame.MaterialFireLavaHeat * float3(0.68, 0.22, 0.04) * (1.0 + heatGILane * 0.16);
    materialTint += frame.MaterialCrystalAether * float3(0.19, 0.12, 0.56) * (1.0 + aetherTechGILane * 0.20);
    materialTint += frame.MaterialNeonGlass * float3(0.20, 0.30, 0.58) * (1.0 + aetherTechGILane * 0.18);
    materialTint += frame.MaterialMetalIndustrial * float3(0.09, 0.11, 0.15) * (1.0 + industrialGILane * 0.16);
    materialTint += frame.MaterialStoneRuins * float3(0.12, 0.10, 0.08) * (1.0 + interiorGILane * 0.12);
    materialTint = saturate(materialTint);
    materialTint = saturate(lerp(materialTint, max(materialTint, laneMaterialTint), 0.38 + materialBounceLaneEnergy * 0.20));

    float neighborLuma = Dalashade_SceneGILuma(neighbor);
    float lowFrequencySurface = saturate(Dalashade_RangeMask(neighborLuma, 0.08, 0.82) * (0.72 + shadow * 0.34));
    float aoReceiverConfidence = frame.ReceiverAO;
    float structureReceiverConfidence = frame.ReceiverStructure;
    float sharedMaterialConfidence = saturate(max(aoReceiverConfidence, structureReceiverConfidence) + normalStructureSupport * 0.18 + normalAOContactSupport * 0.12 + forestCanopyGILane * 0.16 + heatGILane * 0.10 + coldGILane * 0.10 + aetherTechGILane * 0.12 + materialBounceLaneEnergy * 0.14);
    float lowFrequencyMaterialRegion = saturate(lowFrequencySurface * (sharedMaterialConfidence * 0.46 + hardSurface * 0.24 + forestCanopyGILane * 0.14 + frame.SafetyFoliageNoiseReject * 0.08 + wetGILane * 0.08 + heatGILane * 0.08 + coldGILane * 0.08 + materialBounceLaneEnergy * 0.12));
    lowFrequencyMaterialRegion *= receiverSkySafety;
    lowFrequencyMaterialRegion *= 1.0 - skinProtect * 0.70;
    lowFrequencyMaterialRegion *= 1.0 - bright * (0.24 + highlightGuard * 0.34);

    float materialInfluence = saturate(Dalashade_GIMaterialInfluence * (sharedMaterialConfidence * 0.72 + lowFrequencyMaterialRegion * 0.48));
    float materialBounceAllowance = saturate(forestCanopyGILane * 0.36 + heatGILane * 0.32 + coldGILane * 0.24 + wetGILane * 0.22 + interiorGILane * 0.30 + industrialGILane * 0.24 + aetherTechGILane * 0.54 + frame.MaterialFireLavaHeat * 0.44 + materialBounceLaneEnergy * 0.22 + dayPush * 0.14);
    float bounceReceiverMask = saturate(midtone * (0.36 + shadow * 0.55) + lowFrequencyMaterialRegion * 0.34 + forestCanopyGILane * 0.16 + interiorGILane * 0.19 + industrialGILane * 0.13 + heatGILane * 0.12 + coldGILane * 0.10 + wetGILane * 0.09 + materialBounceLaneEnergy * 0.12 + normalStructureSupport * 0.070 + normalGroundContactSupport * 0.048);
    bounceReceiverMask *= saturate(0.55 + normalConfidence * 0.25 + max(normal.z, 0.0) * 0.20);
    bounceReceiverMask *= receiverSkySafety;
    bounceReceiverMask *= 1.0 - skinProtect * 0.82;
    bounceReceiverMask *= 1.0 - frame.MaterialVoidDarkness * 0.72;
    bounceReceiverMask *= 1.0 - bright * (0.34 + highlightGuard * 0.38);
    float bounceMask = midtone * (0.44 + shadow * 0.62) * (1.0 - bright * (0.55 + highlightGuard * 0.35));
    bounceMask *= 1.0 - skyReject;
    bounceMask *= 1.0 - skinProtect * 0.85;
    bounceMask *= 1.0 - frame.WaterSpecularGlint * 0.20;
    bounceMask *= 1.0 - frame.MaterialVoidDarkness * 0.82;
    float ssgiReceiverConfidence = saturate((frame.ReceiverAO * 0.36 + frame.ReceiverStructure * 0.30 + hardSurface * 0.22 + lowFrequencyMaterialRegion * 0.18 + wetGILane * 0.12 + materialBounceLaneEnergy * 0.10) * bounceReceiverMask * receiverMaterialSafety * (1.0 - highlightGuard * 0.54));
    float4 ssgiGather = Dalashade_SceneGIScreenDiffuseGather(texcoord, depth, normal, 2.8 + Dalashade_GIRadius * 4.2, emissiveMaterial, sourceLightLane, ssgiReceiverConfidence);
    float3 screenDiffuseSource = ssgiGather.rgb;
    float screenDiffuseMask = ssgiGather.a;
    float3 propagatedSource = Dalashade_SceneGIPropagatedSource(texcoord, depth, 2.0 + Dalashade_GIRadius * 2.2, 5.0 + Dalashade_GIRadius * 4.2, emissiveMaterial);
    propagatedSource = lerp(propagatedSource, max(propagatedSource, screenDiffuseSource), screenDiffuseMask * (0.32 + sourceLightLane * 0.26 + materialBounceAllowance * 0.18));
    float propagatedSourceMask = saturate(Dalashade_SceneGIEmissiveCandidate(propagatedSource, emissiveMaterial) + sourceLightLane * 0.18 + screenDiffuseMask * (0.22 + materialBounceAllowance * 0.18));
    float emissivePoolingLane = saturate(baseEmissivePoolingLane + propagatedSourceMask * bounceReceiverMask * 0.34 + screenDiffuseMask * sourceLightLane * 0.16);
    materialBounceLaneEnergy = saturate(max(materialBounceLaneEnergy, emissivePoolingLane));
    float3 diffuseSceneTint = lerp(neighbor, screenDiffuseSource, screenDiffuseMask * (0.48 + materialBounceAllowance * 0.30 + standaloneGI * 0.10));
    float3 sourceTint = lerp(diffuseSceneTint * (0.44 + materialBounceAllowance * 0.26), propagatedSource, saturate(propagatedSourceMask * 0.80 + emissiveMaterial * 0.34));
    float3 laneBounceTint = max(materialTint, laneMaterialTint + propagatedSource * (0.24 + emissivePoolingLane * 0.18));
    float3 bounceTint = lerp(sourceTint, laneBounceTint + propagatedSource * (0.42 + emissiveMaterial * 0.10) + screenDiffuseSource * screenDiffuseMask * 0.22, saturate(materialInfluence * 1.18 + materialBounceLaneEnergy * 0.16));
    float3 materialBounce = bounceTint * bounceMask * bounceReceiverMask * Dalashade_GIBounceStrength * combatDampen;
    materialBounce *= 0.94 + scene.Atmosphere * 0.18 + scene.DayOpenAir * 0.11 + scene.NightLocalLight * 0.18 + scene.CinematicPermission * 0.30 + materialBounceAllowance * 0.54 + materialBounceLaneEnergy * 0.24 + propagatedSourceMask * 0.56 + screenDiffuseMask * 0.34;
    materialBounce *= expressiveGILift * lerp(1.0, 1.34, standaloneGI);
    float3 bounce = materialBounce;

    float localLight = smoothstep(0.46, 0.95, luma) * smoothstep(0.025, 0.38, saturatedAccent + frame.WaterSpecularGlint * 0.42);
    localLight = max(localLight, emissiveSourceMask);
    localLight = max(localLight, sourceLightLane * smoothstep(0.18, 0.88, luma + saturatedAccent * 0.28));
    localLight = max(localLight, propagatedSourceMask * bounceReceiverMask * (0.84 + emissiveMaterial * 0.20));
    localLight = max(localLight, frame.MaterialFireLavaHeat * bright);
    localLight = max(localLight, frame.MaterialCrystalAether * smoothstep(0.30, 0.88, luma));
    localLight = max(localLight, frame.MaterialNeonGlass * smoothstep(0.36, 0.92, luma));
    localLight = max(localLight, frame.WaterSpecularGlint * smoothstep(0.42, 0.94, luma) * 0.62);
    float nightContext = saturate(scene.NightLocalLight * 0.58 + scene.InteriorMood * 0.22 + scene.AetherTech * 0.16 + scene.ShadowProtection * 0.18 + frame.MaterialCrystalAether * 0.10 + frame.MaterialNeonGlass * 0.10);
    float moonSurface = saturate(normal.z * normalConfidence * (scene.Moonlight * 0.24 + frame.MaterialSkyCloudFog * 0.14 + frame.MaterialSnowIce * 0.20 + frame.WaterWetShoreline * 0.12 + frame.MaterialSandDust * 0.06));
    float nightPool = saturate(localLight * (0.80 + frame.WaterSpecularGlint * 0.34 + emissiveMaterial * 0.58 + scene.ArtificialLight * 0.10) + moonSurface * 0.32);
    nightPool = saturate(nightPool + propagatedSourceMask * bounceReceiverMask * (0.48 + emissiveMaterial * 0.54 + scene.NightLocalLight * 0.12));
    nightPool = saturate(nightPool + emissivePoolingLane * (0.22 + scene.NightLocalLight * 0.18 + aetherTechGILane * 0.14));
    nightPool *= Dalashade_GINightLightStrength * nightContext * combatDampen * expressiveGILift * lerp(1.0, 1.30, standaloneGI) * (1.0 - skyReject * 0.76) * (1.0 - skinProtect * 0.64);
    float3 nightTint = lerp(float3(0.88, 0.78, 0.58), float3(0.58, 0.74, 1.0), saturate(coldGILane + scene.CosmicMood + frame.MaterialCrystalAether));
    nightTint = lerp(nightTint, float3(0.55, 0.92, 1.0), saturate(aetherTechGILane + frame.MaterialNeonGlass) * 0.45);
    nightTint = lerp(nightTint, float3(1.0, 0.52, 0.20), frame.MaterialFireLavaHeat * 0.68);
    nightTint = lerp(nightTint, float3(0.34, 0.90, 1.0), frame.MaterialWaterPlane * 0.18);
    float3 nightLight = nightTint * nightPool * (0.28 + shadow * 0.44 + emissiveMaterial * 0.18);

    float strength = Dalashade_GIStrength * combatDampen * lerp(1.0, 1.16, standaloneGI);
    float3 result = color;
    result *= 1.0 - saturate(ao * strength);
    result += (bounce + nightLight) * strength;
    float nightAllowance = saturate(nightContext * 0.70 + shadow * 0.18);
    float scenePush = saturate(scene.Atmosphere * 0.18 + scene.DayOpenAir * 0.14 + scene.WetAir * 0.06 + scene.CinematicPermission * 0.44 + nightAllowance * 0.52 + dayPush * 0.16);
    float materialPush = saturate(materialBounceAllowance * 0.48 + emissiveMaterial * 0.76 + emissivePoolingLane * 0.20 + propagatedSourceMask * 0.45 + lowFrequencyMaterialRegion * 0.30 + materialBounceLaneEnergy * 0.16 + hardSurface * 0.22);
    float highlightSafety = saturate(highlightGuard + skyReject * 0.80 + skinProtect * 0.76 + frame.MaterialSnowIce * bright * 0.62 + frame.MaterialWaterPlane * bright * 0.34);
    float positiveGIAllowance = 0.115 + scenePush * 0.165 + materialPush * 0.205 + authoredGILane * 0.030;
    positiveGIAllowance *= 1.0 - saturate(scene.CombatPressure * 0.34 + scene.Readability * 0.20 + highlightSafety * 0.46);
    positiveGIAllowance = max(positiveGIAllowance, 0.060 + emissiveMaterial * 0.085 + emissivePoolingLane * 0.035 + propagatedSourceMask * bounceReceiverMask * 0.085);
    positiveGIAllowance *= lerp(1.0, 1.18, standaloneGI);
    float negativeAOAllowance = 0.098 + hardSurface * 0.130 + frame.MaterialStoneRuins * 0.058 + frame.MaterialFoliage * 0.036 + frame.MaterialVoidDarkness * 0.076 + normalAOContactSupport * 0.038 + interiorGILane * 0.026;
    negativeAOAllowance *= 1.0 - saturate(skyReject * 0.88 + skinProtect * 0.80 + frame.MaterialSnowIce * 0.44 + frame.MaterialWaterPlane * 0.48 + highlightGuard * 0.38 + scene.CombatPressure * 0.20);
    negativeAOAllowance = max(negativeAOAllowance, 0.030 + hardSurface * 0.020);
    negativeAOAllowance *= lerp(1.0, 1.12, standaloneGI);
    float3 delta = result - color;
    result = color + min(max(delta, float3(-negativeAOAllowance, -negativeAOAllowance, -negativeAOAllowance)), float3(positiveGIAllowance, positiveGIAllowance, positiveGIAllowance));
    result = saturate(result);

    float safetyClampMask = saturate(highlightSafety + skinProtect + skyReject + scene.CombatPressure * 0.45 + scene.Readability * 0.25);
    float rawPositivePressure = max(max(delta.r / max(positiveGIAllowance, 0.001), delta.g / max(positiveGIAllowance, 0.001)), delta.b / max(positiveGIAllowance, 0.001));
    float rawNegativePressure = max(max(-delta.r / max(negativeAOAllowance, 0.001), -delta.g / max(negativeAOAllowance, 0.001)), -delta.b / max(negativeAOAllowance, 0.001));
    float clampPressure = saturate(max(rawPositivePressure, rawNegativePressure));
    float finalGIContribution = saturate(Dalashade_SceneGILuma(abs(result - color)) * 9.0 + ao * 0.80 + Dalashade_SceneGILuma(bounce) * 4.2 + nightPool * 0.85 + materialBounceLaneEnergy * 0.12 + emissivePoolingLane * 0.18);
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
            float3 frameMaterialDebug = float3(0.0, 0.0, 0.0);
            frameMaterialDebug += frame.MaterialFoliage * float3(0.10, 0.85, 0.16);
            frameMaterialDebug += frame.MaterialSandDust * float3(0.90, 0.58, 0.10);
            frameMaterialDebug += frame.MaterialSnowIce * float3(0.54, 0.82, 1.00);
            frameMaterialDebug += frame.MaterialWaterPlane * float3(0.00, 0.62, 0.86);
            frameMaterialDebug += frame.MaterialCrystalAether * float3(0.46, 0.22, 1.00);
            frameMaterialDebug += frame.MaterialNeonGlass * float3(0.18, 0.82, 1.00);
            frameMaterialDebug += frame.MaterialFireLavaHeat * float3(1.00, 0.28, 0.04);
            frameMaterialDebug += frame.MaterialMetalIndustrial * float3(0.32, 0.38, 0.48);
            frameMaterialDebug += frame.MaterialStoneRuins * float3(0.42, 0.34, 0.26);
            debugColor = lerp(materialInfluenceMask.xxx, saturate(frameMaterialDebug), 0.72);
            debugColor = max(debugColor, float3(normalAOContactSupport * 0.45, normalStructureSupport * 0.55, normalGroundContactSupport * 0.40));
            debugMask = saturate(max(sharedMaterialConfidence, max(max(max(aoReceiverConfidence, structureReceiverConfidence), frame.SourceLightConfidence), max(normalAOContactSupport, normalStructureSupport))));
        }
        else if (mode == 5)
        {
            float skyRejectMask = skyReject;
            debugColor = float3(0.10 * skyRejectMask, 0.42 * skyRejectMask, skyRejectMask);
            debugMask = saturate(max(skyReject, frame.MaterialSkyCloudFog));
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
            debugColor = float3(max(normalConfidence, surface.NormalConfidence), normal.z * 0.5 + 0.5, saturate(depth + surface.OrientationConfidence * 0.25));
            debugMask = max(max(normalConfidence, surface.NormalConfidence), 0.35);
        }
        else if (mode == 9)
        {
            float sourceMask = saturate(emissiveSourceMask + propagatedSourceMask * 0.80);
            debugColor = lerp(float3(sourceMask, sourceMask * 0.25, 0.08), float3(0.10, 0.78, 1.0), saturate(frame.MaterialCrystalAether + frame.MaterialNeonGlass));
            debugColor = lerp(debugColor, float3(1.0, 0.34, 0.05), frame.MaterialFireLavaHeat);
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
            debugColor = saturate(float3(max(safetyClampMask, frame.SafetyHighlightProtect), positiveGIAllowance * 4.0, negativeAOAllowance * 4.0));
            debugMask = saturate(max(max(safetyClampMask, frame.SafetyHighlightProtect), max(positiveGIAllowance * 4.0, negativeAOAllowance * 4.0)));
        }
        else if (mode == 12)
        {
            debugColor = float3(saturate(aoMicro * 4.0 + normalAOContactSupport), saturate(aoMedium * 4.0 + normalGroundContactSupport), saturate(max(aoBroad, aoStructureBroad) * 4.0 + normalStructureSupport + laneContactAO));
            debugMask = saturate(max(max(max(aoMicro, aoMedium), max(aoBroad, aoStructureBroad)), max(max(normalAOContactSupport, normalStructureSupport), laneContactAO)) * 4.0);
        }
        else if (mode == 13)
        {
            debugColor = saturate(float3(rawNegativePressure, safetyClampMask, rawPositivePressure));
            debugMask = saturate(max(clampPressure, safetyClampMask));
        }
        else if (mode == 14)
        {
            debugColor = screenDiffuseSource * (0.28 + screenDiffuseMask * 1.35);
            debugMask = screenDiffuseMask;
        }
        else if (mode == 15)
        {
            debugColor = saturate(
                foliageBounceLane * float3(0.10, 0.82, 0.16)
                + stoneBounceLane * float3(0.42, 0.34, 0.24)
                + metalBounceLane * float3(0.28, 0.36, 0.52)
                + climateBounceLane * lerp(float3(0.86, 0.55, 0.12), float3(0.42, 0.78, 1.0), frame.MaterialSnowIce)
                + wetBounceLane * float3(0.02, 0.58, 0.80));
            debugMask = materialBounceLaneEnergy;
        }
        else if (mode == 16)
        {
            debugColor = float3(receiverSkySafety, receiverMaterialSafety, bounceReceiverMask);
            debugMask = saturate(max(receiverMaterialSafety, bounceReceiverMask));
        }
        else if (mode == 17)
        {
            debugColor = saturate(baseEmissivePoolingLane * float3(1.0, 0.48, 0.08) + emissivePoolingLane * float3(0.16, 0.72, 1.0) + propagatedSourceMask * float3(0.48, 0.12, 0.92));
            debugMask = emissivePoolingLane;
        }
        else if (mode == 18)
        {
            float contributionMask = saturate(max(dalapadContributionSupport, dalapadStructureSupport));
            debugColor = saturate(float3(dalapadContributionSupport, dalapadStructureSupport, normalConfidence * dalapadContributionSupport));
            debugMask = contributionMask;
        }
        else if (mode == 19)
        {
            float evidenceMask = saturate(max(surface.DalapadConfidence, max(surface.DalapadFlatSupport, surface.DalapadStructureSupport)));
            debugColor = saturate(surface.DalapadNormal * 0.5 + 0.5);
            debugColor = lerp(float3(0.0, 0.0, 0.0), debugColor, evidenceMask);
            debugColor = max(debugColor, float3(surface.DalapadChroma, surface.DalapadNeighborDelta * 4.0, surface.DalapadPresence) * evidenceMask * 0.45);
            debugMask = evidenceMask;
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
