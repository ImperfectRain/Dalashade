#include "ReShade.fxh"
#include "Dalashade_FrameData.fxh"

// Dalashade SurfaceReflection is a lightweight material-aware reflection
// impression pass. It includes a restrained pseudo-SSR contribution, but it is
// not full SSR, RTGI, PTGI, or ray tracing. It shapes water sheen, wet-surface
// glints, icy sheen, neon/aether streaks, and polished surface highlights from
// cheap screen-space masks.

uniform float Dalashade_SurfaceReflectionEnabled <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Surface Reflection Enabled";
> = 1.0;

uniform float Dalashade_SurfaceReflectionStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Surface Reflection Strength";
> = 0.32;

uniform float Dalashade_StandaloneStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Standalone Stack Strength";
    ui_tooltip = "0 keeps SurfaceReflection supportive for an existing preset. 1 makes valid water, wet, metal, and aether receivers more visible without weakening source/receiver safety.";
> = 0.0;

uniform float Dalashade_WaterSheenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Water Sheen Strength";
> = 0.38;

uniform float Dalashade_WaterReflectionStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Water Reflection Strength";
> = 0.45;

uniform float Dalashade_WaterSheenRadius <
    ui_type = "slider";
    ui_min = 0.25; ui_max = 4.0;
    ui_label = "Water Sheen Radius";
> = 1.35;

uniform float Dalashade_SpecularGlintStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Specular Glint Strength";
> = 0.32;

uniform float Dalashade_SpecularReflectionStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Specular Reflection Strength";
> = 0.30;

uniform float Dalashade_WetReflectionStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Wet Reflection Strength";
> = 0.30;

uniform float Dalashade_AetherReflectionStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Aether Reflection Strength";
> = 0.36;

uniform float Dalashade_NeonReflectionStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Neon Reflection Strength";
> = 0.34;

uniform float Dalashade_IceSheenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Ice Sheen Strength";
> = 0.24;

uniform float Dalashade_SurfaceReflectionSkyReject <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Surface Reflection Sky Reject";
> = 1.0;

uniform float Dalashade_SurfaceReflectionSkinProtect <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Surface Reflection Skin Protect";
> = 1.0;

uniform float Dalashade_ReflectionSampleOffset <
    ui_type = "slider";
    ui_min = 0.002; ui_max = 0.080;
    ui_label = "Reflection Sample Offset";
> = 0.018;

uniform float Dalashade_ReflectionSoftness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Reflection Softness";
> = 0.50;

uniform float Dalashade_ReflectionDepthReject <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Reflection Depth Reject";
> = 0.65;

uniform int Dalashade_SurfaceReflectionDebugMode <
    ui_type = "combo";
    ui_items = "Normal\0WaterPlane sheen\0SpecularGlint\0Wet reflection\0Aether/neon reflection\0Sky rejection\0Skin protection\0Final reflection influence\0Contribution over black\0Reflection source mask\0Reflection receiver mask\0Water projected reflection\0Wet hard projected reflection\0Metal/aether projected reflection\0Pseudo SSR contribution\0";
    ui_label = "Surface Reflection Debug Mode";
> = 0;

uniform int Dalashade_SurfaceReflectionDebugOutputMode <
    ui_type = "combo";
    ui_items = "Full replacement diagnostic\0Alpha overlay over original\0Side-by-side split\0Contribution over black\0Amplified difference view\0";
    ui_label = "Surface Reflection Debug Output Mode";
> = 0;

uniform float Dalashade_SurfaceReflectionDebugOpacity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Surface Reflection Debug Opacity";
> = 0.75;

uniform float Dalashade_SurfaceReflectionDebugBoost <
    ui_type = "slider";
    ui_min = 0.25; ui_max = 8.0;
    ui_label = "Surface Reflection Debug Boost";
> = 2.25;

uniform float Dalashade_Wetness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Wetness"; > = 0.0;
uniform float Dalashade_HighlightProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Highlight Protection"; > = 0.0;
uniform float Dalashade_Readability < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Readability"; > = 0.0;
uniform float Dalashade_CombatPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Combat Pressure"; > = 0.0;
uniform float Dalashade_MagicGlow < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Magic Glow"; > = 0.0;
uniform float Dalashade_NeonGlow < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Neon Glow"; > = 0.0;
uniform float Dalashade_Night < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Night"; > = 0.0;
uniform float Dalashade_ArtificialLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Artificial Light"; > = 0.0;
uniform float Dalashade_OpenSkyLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Open Sky Light"; > = 0.0;
uniform float Dalashade_DayReflection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Day Reflection"; > = 0.0;
uniform float Dalashade_DayHighlightPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Day Highlight Pressure"; > = 0.0;
uniform float Dalashade_CinematicPermission < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Cinematic Permission"; > = 0.0;

uniform float Dalashade_WaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Water Context"; > = 0.0;
uniform float Dalashade_CoastalContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Coastal Context"; > = 0.0;
uniform float Dalashade_OpenOceanContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Open Ocean Context"; > = 0.0;
uniform float Dalashade_ShallowWaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Shallow Water Context"; > = 0.0;
uniform float Dalashade_WetSurfaceContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Wet Surface Context"; > = 0.0;

uniform float Dalashade_MaterialFoliage < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Foliage"; > = 0.0;
uniform float Dalashade_MaterialWaterSpecular < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Water Specular"; > = 0.0;
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

uniform float Dalashade_NormalFieldEnabled < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Enabled"; > = 0.0;
uniform float Dalashade_NormalFieldStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Strength"; > = 0.0;
uniform float Dalashade_NormalDepthStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Depth Strength"; > = 0.0;
uniform float Dalashade_NormalDetailStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Detail Strength"; > = 0.0;
uniform float Dalashade_NormalMaterialInfluence < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Material Influence"; > = 0.0;
uniform float Dalashade_NormalWaterSuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Water Suppression"; > = 0.80;
uniform float Dalashade_NormalSkinSuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Skin Suppression"; > = 0.90;
uniform float Dalashade_NormalSkySuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Sky/Fog Suppression"; > = 0.95;

uniform bool Dalashade_EnableDepthAssist < ui_label = "Enable Depth Assist"; > = false;
uniform float Dalashade_DepthAssistStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Strength"; > = 0.0;
uniform float Dalashade_DepthAssistConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Confidence Floor"; > = 0.0;

float Dalashade_SurfaceReflectionLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 Dalashade_SurfaceReflectionDepthNormal(float2 uv, float depth)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float dx = ReShade::GetLinearizedDepth(saturate(uv + float2(texel.x, 0.0))) - ReShade::GetLinearizedDepth(saturate(uv - float2(texel.x, 0.0)));
    float dy = ReShade::GetLinearizedDepth(saturate(uv + float2(0.0, texel.y))) - ReShade::GetLinearizedDepth(saturate(uv - float2(0.0, texel.y)));
    return normalize(float3(-dx * 22.0, -dy * 22.0, 1.0 + depth * 0.08));
}

float3 Dalashade_SurfaceReflectionDirectionalSheen(float2 uv, float radius, float depth)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float2 vertical = float2(0.0, texel.y * 1.35);
    float2 horizontal = float2(texel.x * 1.85, 0.0);
    float3 c = tex2D(ReShade::BackBuffer, saturate(uv - vertical)).rgb * 0.28;
    c += tex2D(ReShade::BackBuffer, saturate(uv + vertical)).rgb * 0.18;
    c += tex2D(ReShade::BackBuffer, saturate(uv + horizontal)).rgb * 0.18;
    c += tex2D(ReShade::BackBuffer, saturate(uv - horizontal)).rgb * 0.18;
    c += tex2D(ReShade::BackBuffer, saturate(uv - vertical * 2.0)).rgb * 0.10;
    c += tex2D(ReShade::BackBuffer, saturate(uv + horizontal * 2.0)).rgb * 0.08;

    float sampleDepth = ReShade::GetLinearizedDepth(saturate(uv - vertical));
    float continuity = saturate(1.0 - abs(depth - sampleDepth) * 10.0);
    return saturate(c * (0.62 + continuity * 0.38));
}

float Dalashade_SurfaceReflectionDepthGate(float2 sampleUv, float depth, float depthReject)
{
    float sampleDepth = ReShade::GetLinearizedDepth(saturate(sampleUv));
    float reject = lerp(3.5, 12.0, saturate(depthReject));
    return saturate(1.0 - abs(depth - sampleDepth) * reject);
}

float Dalashade_SurfaceReflectionFresnel(float3 normal, float floorTerm, float curve)
{
    float grazing = saturate(1.0 - max(normal.z, 0.0));
    return saturate(floorTerm + pow(grazing, max(curve, 0.001)) * (1.0 - floorTerm));
}

float Dalashade_SurfaceReflectionSampleSafety(float2 sampleUv)
{
    float2 uv = saturate(sampleUv);
    float sourceEdge = Dalashade_GetEdgeStrength(uv);
    float sourceSmoothness = Dalashade_GetSmoothness(uv);
    float sourceLuma = Dalashade_SurfaceReflectionLuma(tex2D(ReShade::BackBuffer, uv).rgb);
    float silhouetteReject = smoothstep(0.10, 0.48, sourceEdge) * smoothstep(0.04, 0.40, sourceLuma);
    float smoothSkyRisk = smoothstep(0.62, 0.96, sourceSmoothness) * smoothstep(0.04, 0.34, uv.y);

    return saturate(1.0 - silhouetteReject * 0.86 - smoothSkyRisk * 0.42);
}

float Dalashade_SurfaceReflectionStructureSource(float2 sampleUv, float3 sampleColor)
{
    float2 uv = saturate(sampleUv);
    float sourceEdge = Dalashade_GetEdgeStrength(uv);
    float sourceSmoothness = Dalashade_GetSmoothness(uv);
    float sourceLuma = Dalashade_SurfaceReflectionLuma(sampleColor);
    float darkStructure = smoothstep(0.08, 0.42, sourceEdge) * (1.0 - smoothstep(0.18, 0.58, sourceLuma));
    float midStructure = smoothstep(0.10, 0.48, sourceEdge) * smoothstep(0.10, 0.62, sourceLuma) * (1.0 - smoothstep(0.72, 0.96, sourceSmoothness));
    float upperSkyRisk = smoothstep(0.62, 0.96, sourceSmoothness) * smoothstep(0.04, 0.34, uv.y);

    return saturate((darkStructure * 0.82 + midStructure * 0.34) * (1.0 - upperSkyRisk * 0.72));
}

float Dalashade_SurfaceReflectionPseudoSampleSafety(float2 sampleUv, float structureBias)
{
    float2 uv = saturate(sampleUv);
    float sourceEdge = Dalashade_GetEdgeStrength(uv);
    float sourceSmoothness = Dalashade_GetSmoothness(uv);
    float sourceLuma = Dalashade_SurfaceReflectionLuma(tex2D(ReShade::BackBuffer, uv).rgb);
    float silhouetteReject = smoothstep(0.10, 0.48, sourceEdge) * smoothstep(0.04, 0.40, sourceLuma);
    float smoothSkyRisk = smoothstep(0.62, 0.96, sourceSmoothness) * smoothstep(0.04, 0.34, uv.y);

    return saturate(1.0 - silhouetteReject * lerp(0.86, 0.22, saturate(structureBias)) - smoothSkyRisk * 0.58);
}

float3 Dalashade_SampleReflectionColor(float2 uv, float depth, float2 direction, float offset, float softness, float depthReject)
{
    float2 primaryUv = saturate(uv + direction * offset);
    float2 sideA = saturate(uv + direction * offset * (1.0 + softness * 0.45) + float2(BUFFER_PIXEL_SIZE.x, 0.0) * (1.0 + softness * 2.0));
    float2 sideB = saturate(uv + direction * offset * (0.72 + softness * 0.35) - float2(BUFFER_PIXEL_SIZE.x, 0.0) * (1.0 + softness * 2.0));

    float gateA = Dalashade_SurfaceReflectionDepthGate(primaryUv, depth, depthReject) * Dalashade_SurfaceReflectionSampleSafety(primaryUv);
    float gateB = Dalashade_SurfaceReflectionDepthGate(sideA, depth, depthReject) * Dalashade_SurfaceReflectionSampleSafety(sideA) * (0.28 + softness * 0.38);
    float gateC = Dalashade_SurfaceReflectionDepthGate(sideB, depth, depthReject) * Dalashade_SurfaceReflectionSampleSafety(sideB) * (0.22 + softness * 0.28);
    float weight = max(gateA + gateB + gateC, 0.001);

    float3 reflected = tex2D(ReShade::BackBuffer, primaryUv).rgb * gateA;
    reflected += tex2D(ReShade::BackBuffer, sideA).rgb * gateB;
    reflected += tex2D(ReShade::BackBuffer, sideB).rgb * gateC;
    return reflected / weight;
}

float3 Dalashade_SampleReflectionStreak(float2 uv, float depth, float2 direction, float offset, float depthReject)
{
    float2 uvA = saturate(uv + direction * offset);
    float2 uvB = saturate(uv + direction * offset * 1.85);
    float gateA = Dalashade_SurfaceReflectionDepthGate(uvA, depth, depthReject) * Dalashade_SurfaceReflectionSampleSafety(uvA);
    float gateB = Dalashade_SurfaceReflectionDepthGate(uvB, depth, depthReject) * Dalashade_SurfaceReflectionSampleSafety(uvB) * 0.52;
    float3 sampleA = tex2D(ReShade::BackBuffer, uvA).rgb;
    float3 sampleB = tex2D(ReShade::BackBuffer, uvB).rgb;
    float energyA = smoothstep(0.34, 1.06, Dalashade_SurfaceReflectionLuma(sampleA) + Dalashade_GetSaturation(sampleA) * 0.58);
    float energyB = smoothstep(0.34, 1.06, Dalashade_SurfaceReflectionLuma(sampleB) + Dalashade_GetSaturation(sampleB) * 0.58);
    float weight = max(gateA * energyA + gateB * energyB, 0.001);
    return (sampleA * gateA * energyA + sampleB * gateB * energyB) / weight;
}

float Dalashade_SurfaceReflectionSourceEnergy(float3 sampleColor, float waterBias, float hardBias, float aetherBias, float fireBias)
{
    float sampleLuma = Dalashade_SurfaceReflectionLuma(sampleColor);
    float sampleSaturation = Dalashade_GetSaturation(sampleColor);
    float coolSource = saturate((sampleColor.b + sampleColor.g * 0.72 - sampleColor.r * 0.82) * 0.92);
    float aetherSource = saturate((sampleColor.b + sampleColor.r * 0.52 - sampleColor.g * 0.22) * sampleSaturation);
    float warmSource = saturate((sampleColor.r * 1.05 + sampleColor.g * 0.34 - sampleColor.b * 0.72) * sampleSaturation);
    float highlightSource = smoothstep(0.42, 1.0, sampleLuma + sampleSaturation * 0.36);
    float sourceFamily = saturate(
        waterBias * coolSource
        + hardBias * highlightSource
        + aetherBias * aetherSource
        + fireBias * warmSource);

    return saturate(highlightSource * 0.34 + sourceFamily * 0.78);
}

float3 Dalashade_SampleQualifiedReflectionSource(float2 uv, float depth, float2 direction, float offset, float softness, float depthReject, float waterBias, float hardBias, float aetherBias, float fireBias)
{
    float2 primaryUv = saturate(uv + direction * offset);
    float2 sideA = saturate(uv + direction * offset * (1.0 + softness * 0.40) + float2(BUFFER_PIXEL_SIZE.x, 0.0) * (1.0 + softness * 1.65));
    float2 sideB = saturate(uv + direction * offset * (0.68 + softness * 0.30) - float2(BUFFER_PIXEL_SIZE.x, 0.0) * (1.0 + softness * 1.65));

    float3 primaryColor = tex2D(ReShade::BackBuffer, primaryUv).rgb;
    float3 sideAColor = tex2D(ReShade::BackBuffer, sideA).rgb;
    float3 sideBColor = tex2D(ReShade::BackBuffer, sideB).rgb;

    float gateA = Dalashade_SurfaceReflectionDepthGate(primaryUv, depth, depthReject) * Dalashade_SurfaceReflectionSampleSafety(primaryUv);
    float gateB = Dalashade_SurfaceReflectionDepthGate(sideA, depth, depthReject) * Dalashade_SurfaceReflectionSampleSafety(sideA) * (0.30 + softness * 0.34);
    float gateC = Dalashade_SurfaceReflectionDepthGate(sideB, depth, depthReject) * Dalashade_SurfaceReflectionSampleSafety(sideB) * (0.24 + softness * 0.26);

    float energyA = Dalashade_SurfaceReflectionSourceEnergy(primaryColor, waterBias, hardBias, aetherBias, fireBias);
    float energyB = Dalashade_SurfaceReflectionSourceEnergy(sideAColor, waterBias, hardBias, aetherBias, fireBias);
    float energyC = Dalashade_SurfaceReflectionSourceEnergy(sideBColor, waterBias, hardBias, aetherBias, fireBias);
    float weightA = gateA * (0.18 + energyA);
    float weightB = gateB * (0.12 + energyB);
    float weightC = gateC * (0.10 + energyC);
    float weight = max(weightA + weightB + weightC, 0.001);

    return (primaryColor * weightA + sideAColor * weightB + sideBColor * weightC) / weight;
}

float4 Dalashade_SampleWaterMirrorProjection(float2 uv, float depth, float mirrorLine, float stretch, float roughness, float depthReject, float sourceBias, float structureBias)
{
    float distanceBelowLine = max(uv.y - mirrorLine, 0.0);
    float verticalScale = lerp(0.64, 1.18, saturate(stretch));
    float sourceY = mirrorLine - distanceBelowLine * verticalScale;
    float ripple = sin(dot(uv, float2(73.0, 41.0))) * 0.5 + sin(dot(uv, float2(31.0, 97.0))) * 0.5;
    float lateral = ripple * BUFFER_PIXEL_SIZE.x * (1.5 + roughness * 8.0);
    float2 baseUv = float2(uv.x + lateral, sourceY);
    float2 verticalStep = float2(0.0, BUFFER_PIXEL_SIZE.y * (6.0 + roughness * 20.0));
    float2 horizontalStep = float2(BUFFER_PIXEL_SIZE.x * (1.0 + roughness * 7.0), 0.0);

    float2 uvA = saturate(baseUv);
    float2 uvB = saturate(baseUv + verticalStep * 0.85 + horizontalStep * 0.30);
    float2 uvC = saturate(baseUv - verticalStep * 0.65 - horizontalStep * 0.45);
    float2 uvD = saturate(baseUv + verticalStep * 1.80 - horizontalStep * 0.20);
    float2 uvE = saturate(baseUv + horizontalStep * 1.35);
    float2 uvF = saturate(baseUv - horizontalStep * 1.35);

    float3 cA = tex2D(ReShade::BackBuffer, uvA).rgb;
    float3 cB = tex2D(ReShade::BackBuffer, uvB).rgb;
    float3 cC = tex2D(ReShade::BackBuffer, uvC).rgb;
    float3 cD = tex2D(ReShade::BackBuffer, uvD).rgb;
    float3 cE = tex2D(ReShade::BackBuffer, uvE).rgb;
    float3 cF = tex2D(ReShade::BackBuffer, uvF).rgb;

    float sourceBand = smoothstep(0.0, 0.16, mirrorLine - sourceY) * (1.0 - smoothstep(0.70, 1.0, mirrorLine - sourceY));
    float depthA = Dalashade_SurfaceReflectionDepthGate(uvA, depth, depthReject);
    float depthB = Dalashade_SurfaceReflectionDepthGate(uvB, depth, depthReject * 0.82);
    float depthC = Dalashade_SurfaceReflectionDepthGate(uvC, depth, depthReject * 0.82);
    float depthD = Dalashade_SurfaceReflectionDepthGate(uvD, depth, depthReject * 0.68);
    float depthE = Dalashade_SurfaceReflectionDepthGate(uvE, depth, depthReject * 0.74);
    float depthF = Dalashade_SurfaceReflectionDepthGate(uvF, depth, depthReject * 0.74);

    float eA = Dalashade_SurfaceReflectionSourceEnergy(cA, sourceBias, sourceBias * 0.52, sourceBias * 0.38, sourceBias * 0.24) + Dalashade_SurfaceReflectionStructureSource(uvA, cA) * structureBias;
    float eB = Dalashade_SurfaceReflectionSourceEnergy(cB, sourceBias, sourceBias * 0.50, sourceBias * 0.36, sourceBias * 0.22) + Dalashade_SurfaceReflectionStructureSource(uvB, cB) * structureBias * 0.78;
    float eC = Dalashade_SurfaceReflectionSourceEnergy(cC, sourceBias, sourceBias * 0.48, sourceBias * 0.34, sourceBias * 0.22) + Dalashade_SurfaceReflectionStructureSource(uvC, cC) * structureBias * 0.82;
    float eD = Dalashade_SurfaceReflectionSourceEnergy(cD, sourceBias, sourceBias * 0.46, sourceBias * 0.34, sourceBias * 0.20) + Dalashade_SurfaceReflectionStructureSource(uvD, cD) * structureBias * 0.62;
    float eE = Dalashade_SurfaceReflectionSourceEnergy(cE, sourceBias, sourceBias * 0.44, sourceBias * 0.32, sourceBias * 0.18) + Dalashade_SurfaceReflectionStructureSource(uvE, cE) * structureBias * 0.58;
    float eF = Dalashade_SurfaceReflectionSourceEnergy(cF, sourceBias, sourceBias * 0.44, sourceBias * 0.32, sourceBias * 0.18) + Dalashade_SurfaceReflectionStructureSource(uvF, cF) * structureBias * 0.58;

    float safeA = Dalashade_SurfaceReflectionSampleSafety(uvA);
    float safeB = Dalashade_SurfaceReflectionSampleSafety(uvB);
    float safeC = Dalashade_SurfaceReflectionSampleSafety(uvC);
    float safeD = Dalashade_SurfaceReflectionSampleSafety(uvD);
    float safeE = Dalashade_SurfaceReflectionSampleSafety(uvE);
    float safeF = Dalashade_SurfaceReflectionSampleSafety(uvF);

    float wA = safeA * depthA * (0.18 + saturate(eA)) * 1.00;
    float wB = safeB * depthB * (0.14 + saturate(eB)) * 0.82;
    float wC = safeC * depthC * (0.14 + saturate(eC)) * 0.78;
    float wD = safeD * depthD * (0.10 + saturate(eD)) * 0.54;
    float wE = safeE * depthE * (0.10 + saturate(eE)) * 0.48;
    float wF = safeF * depthF * (0.10 + saturate(eF)) * 0.48;
    float weight = max(wA + wB + wC + wD + wE + wF, 0.001);
    float confidence = saturate(weight * sourceBand * (0.20 + sourceBias * 0.22 + structureBias * 0.34) * (1.0 - roughness * 0.20));

    float3 mirrorColor = (cA * wA + cB * wB + cC * wC + cD * wD + cE * wE + cF * wF) / weight;
    return float4(mirrorColor, confidence);
}

float4 Dalashade_SampleWaterColumnProjection(float2 uv, float roughness, float sourceBias, float structureBias)
{
    float ripple = sin(dot(uv, float2(59.0, 37.0))) * 0.5 + sin(dot(uv, float2(23.0, 113.0))) * 0.5;
    float xJitter = ripple * BUFFER_PIXEL_SIZE.x * (1.5 + roughness * 9.0);
    float side = BUFFER_PIXEL_SIZE.x * (1.0 + roughness * 5.0);

    float2 uvA = float2(uv.x + xJitter, uv.y - 0.026);
    float2 uvB = float2(uv.x + xJitter + side * 0.35, uv.y - 0.046);
    float2 uvC = float2(uv.x + xJitter - side * 0.45, uv.y - 0.074);
    float2 uvD = float2(uv.x + xJitter + side * 0.20, uv.y - 0.112);
    float2 uvE = float2(uv.x + xJitter - side * 0.22, uv.y - 0.166);
    float2 uvF = float2(uv.x + xJitter + side * 0.12, uv.y - 0.238);
    float2 uvG = float2(uv.x + xJitter - side * 0.10, uv.y - 0.330);
    float2 uvH = float2(uv.x + xJitter, uv.y - 0.442);

    float inA = step(0.001, uvA.y);
    float inB = step(0.001, uvB.y);
    float inC = step(0.001, uvC.y);
    float inD = step(0.001, uvD.y);
    float inE = step(0.001, uvE.y);
    float inF = step(0.001, uvF.y);
    float inG = step(0.001, uvG.y);
    float inH = step(0.001, uvH.y);

    uvA = saturate(uvA);
    uvB = saturate(uvB);
    uvC = saturate(uvC);
    uvD = saturate(uvD);
    uvE = saturate(uvE);
    uvF = saturate(uvF);
    uvG = saturate(uvG);
    uvH = saturate(uvH);

    float3 cA = tex2D(ReShade::BackBuffer, uvA).rgb;
    float3 cB = tex2D(ReShade::BackBuffer, uvB).rgb;
    float3 cC = tex2D(ReShade::BackBuffer, uvC).rgb;
    float3 cD = tex2D(ReShade::BackBuffer, uvD).rgb;
    float3 cE = tex2D(ReShade::BackBuffer, uvE).rgb;
    float3 cF = tex2D(ReShade::BackBuffer, uvF).rgb;
    float3 cG = tex2D(ReShade::BackBuffer, uvG).rgb;
    float3 cH = tex2D(ReShade::BackBuffer, uvH).rgb;

    float lA = Dalashade_SurfaceReflectionLuma(cA);
    float lB = Dalashade_SurfaceReflectionLuma(cB);
    float lC = Dalashade_SurfaceReflectionLuma(cC);
    float lD = Dalashade_SurfaceReflectionLuma(cD);
    float lE = Dalashade_SurfaceReflectionLuma(cE);
    float lF = Dalashade_SurfaceReflectionLuma(cF);
    float lG = Dalashade_SurfaceReflectionLuma(cG);
    float lH = Dalashade_SurfaceReflectionLuma(cH);

    float eA = Dalashade_GetEdgeStrength(uvA);
    float eB = Dalashade_GetEdgeStrength(uvB);
    float eC = Dalashade_GetEdgeStrength(uvC);
    float eD = Dalashade_GetEdgeStrength(uvD);
    float eE = Dalashade_GetEdgeStrength(uvE);
    float eF = Dalashade_GetEdgeStrength(uvF);
    float eG = Dalashade_GetEdgeStrength(uvG);
    float eH = Dalashade_GetEdgeStrength(uvH);

    float structureA = Dalashade_SurfaceReflectionStructureSource(uvA, cA);
    float structureB = Dalashade_SurfaceReflectionStructureSource(uvB, cB);
    float structureC = Dalashade_SurfaceReflectionStructureSource(uvC, cC);
    float structureD = Dalashade_SurfaceReflectionStructureSource(uvD, cD);
    float structureE = Dalashade_SurfaceReflectionStructureSource(uvE, cE);
    float structureF = Dalashade_SurfaceReflectionStructureSource(uvF, cF);
    float structureG = Dalashade_SurfaceReflectionStructureSource(uvG, cG);
    float structureH = Dalashade_SurfaceReflectionStructureSource(uvH, cH);

    float darkA = smoothstep(0.08, 0.34, eA) * (1.0 - smoothstep(0.16, 0.56, lA));
    float darkB = smoothstep(0.08, 0.34, eB) * (1.0 - smoothstep(0.16, 0.56, lB));
    float darkC = smoothstep(0.08, 0.34, eC) * (1.0 - smoothstep(0.16, 0.56, lC));
    float darkD = smoothstep(0.08, 0.34, eD) * (1.0 - smoothstep(0.16, 0.56, lD));
    float darkE = smoothstep(0.08, 0.34, eE) * (1.0 - smoothstep(0.16, 0.56, lE));
    float darkF = smoothstep(0.08, 0.34, eF) * (1.0 - smoothstep(0.16, 0.56, lF));
    float darkG = smoothstep(0.08, 0.34, eG) * (1.0 - smoothstep(0.16, 0.56, lG));
    float darkH = smoothstep(0.08, 0.34, eH) * (1.0 - smoothstep(0.16, 0.56, lH));

    float sA = saturate(Dalashade_SurfaceReflectionSourceEnergy(cA, sourceBias, sourceBias * 0.42, sourceBias * 0.28, sourceBias * 0.18) + structureA * structureBias * 0.90 + darkA * structureBias * 1.10);
    float sB = saturate(Dalashade_SurfaceReflectionSourceEnergy(cB, sourceBias, sourceBias * 0.42, sourceBias * 0.28, sourceBias * 0.18) + structureB * structureBias * 0.95 + darkB * structureBias * 1.12);
    float sC = saturate(Dalashade_SurfaceReflectionSourceEnergy(cC, sourceBias, sourceBias * 0.40, sourceBias * 0.28, sourceBias * 0.18) + structureC * structureBias * 0.96 + darkC * structureBias * 1.12);
    float sD = saturate(Dalashade_SurfaceReflectionSourceEnergy(cD, sourceBias, sourceBias * 0.38, sourceBias * 0.26, sourceBias * 0.18) + structureD * structureBias * 0.92 + darkD * structureBias * 1.08);
    float sE = saturate(Dalashade_SurfaceReflectionSourceEnergy(cE, sourceBias, sourceBias * 0.36, sourceBias * 0.24, sourceBias * 0.16) + structureE * structureBias * 0.84 + darkE * structureBias * 0.98);
    float sF = saturate(Dalashade_SurfaceReflectionSourceEnergy(cF, sourceBias, sourceBias * 0.34, sourceBias * 0.24, sourceBias * 0.16) + structureF * structureBias * 0.72 + darkF * structureBias * 0.82);
    float sG = saturate(Dalashade_SurfaceReflectionSourceEnergy(cG, sourceBias, sourceBias * 0.30, sourceBias * 0.22, sourceBias * 0.14) + structureG * structureBias * 0.58 + darkG * structureBias * 0.64);
    float sH = saturate(Dalashade_SurfaceReflectionSourceEnergy(cH, sourceBias, sourceBias * 0.26, sourceBias * 0.20, sourceBias * 0.12) + structureH * structureBias * 0.44 + darkH * structureBias * 0.48);

    float wA = inA * sA * 1.00;
    float wB = inB * sB * 0.96;
    float wC = inC * sC * 0.90;
    float wD = inD * sD * 0.78;
    float wE = inE * sE * 0.62;
    float wF = inF * sF * 0.46;
    float wG = inG * sG * 0.30;
    float wH = inH * sH * 0.18;
    float weight = max(wA + wB + wC + wD + wE + wF + wG + wH, 0.001);
    float confidence = saturate(weight * (0.36 + sourceBias * 0.18 + structureBias * 0.42) * (1.0 - roughness * 0.12));

    float3 columnColor = (cA * wA + cB * wB + cC * wC + cD * wD + cE * wE + cF * wF + cG * wG + cH * wH) / weight;
    return float4(columnColor, confidence);
}

float4 Dalashade_SamplePlanarApproxTrace(float2 uv, float depth, float2 direction, float offset, float depthReject, float sourceBias, float structureBias, float roughness)
{
    float2 dir = normalize(direction + float2(0.0001, -0.0002));
    float2 side = float2(BUFFER_PIXEL_SIZE.x * (1.0 + roughness * 2.0), 0.0);
    float2 uvA = saturate(uv + dir * offset * 0.72);
    float2 uvB = saturate(uv + dir * offset * 1.20 + side * 0.25);
    float2 uvC = saturate(uv + dir * offset * 1.90 - side * 0.48);
    float2 uvD = saturate(uv + dir * offset * 2.95 + side * 0.62);
    float2 uvE = saturate(uv + dir * offset * 4.35 - side * 0.36);

    float3 cA = tex2D(ReShade::BackBuffer, uvA).rgb;
    float3 cB = tex2D(ReShade::BackBuffer, uvB).rgb;
    float3 cC = tex2D(ReShade::BackBuffer, uvC).rgb;
    float3 cD = tex2D(ReShade::BackBuffer, uvD).rgb;
    float3 cE = tex2D(ReShade::BackBuffer, uvE).rgb;

    float eA = Dalashade_SurfaceReflectionSourceEnergy(cA, sourceBias, sourceBias * 0.62, sourceBias * 0.42, sourceBias * 0.28) + Dalashade_SurfaceReflectionStructureSource(uvA, cA) * structureBias * 0.68;
    float eB = Dalashade_SurfaceReflectionSourceEnergy(cB, sourceBias, sourceBias * 0.66, sourceBias * 0.45, sourceBias * 0.30) + Dalashade_SurfaceReflectionStructureSource(uvB, cB) * structureBias * 0.82;
    float eC = Dalashade_SurfaceReflectionSourceEnergy(cC, sourceBias, sourceBias * 0.68, sourceBias * 0.48, sourceBias * 0.32) + Dalashade_SurfaceReflectionStructureSource(uvC, cC) * structureBias * 0.92;
    float eD = Dalashade_SurfaceReflectionSourceEnergy(cD, sourceBias, sourceBias * 0.58, sourceBias * 0.48, sourceBias * 0.34) + Dalashade_SurfaceReflectionStructureSource(uvD, cD) * structureBias * 0.84;
    float eE = Dalashade_SurfaceReflectionSourceEnergy(cE, sourceBias, sourceBias * 0.50, sourceBias * 0.44, sourceBias * 0.36) + Dalashade_SurfaceReflectionStructureSource(uvE, cE) * structureBias * 0.68;

    float reject = saturate(depthReject);
    float gA = Dalashade_SurfaceReflectionDepthGate(uvA, depth, reject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvA, structureBias) * (0.16 + saturate(eA));
    float gB = Dalashade_SurfaceReflectionDepthGate(uvB, depth, reject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvB, structureBias) * (0.14 + saturate(eB)) * 0.95;
    float gC = Dalashade_SurfaceReflectionDepthGate(uvC, depth, reject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvC, structureBias) * (0.12 + saturate(eC)) * 0.78;
    float gD = Dalashade_SurfaceReflectionDepthGate(uvD, depth, reject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvD, structureBias) * (0.10 + saturate(eD)) * 0.56;
    float gE = Dalashade_SurfaceReflectionDepthGate(uvE, depth, reject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvE, structureBias) * (0.08 + saturate(eE)) * 0.38;
    float weight = max(gA + gB + gC + gD + gE, 0.001);
    float confidence = saturate(weight * (0.20 + sourceBias * 0.28 + structureBias * 0.36) * (1.0 - saturate(roughness) * 0.18));

    return float4((cA * gA + cB * gB + cC * gC + cD * gD + cE * gE) / weight, confidence);
}

float4 Dalashade_SamplePseudoSSR(float2 uv, float depth, float2 direction, float offset, float depthReject, float sourceBias, float structureBias)
{
    float2 dir = normalize(direction + float2(0.0001, -0.0002));
    float2 uvA = saturate(uv + dir * offset * 0.72);
    float2 uvB = saturate(uv + dir * offset * 1.32);
    float2 uvC = saturate(uv + dir * offset * 2.10);
    float2 uvD = saturate(uv + dir * offset * 3.10);

    float3 cA = tex2D(ReShade::BackBuffer, uvA).rgb;
    float3 cB = tex2D(ReShade::BackBuffer, uvB).rgb;
    float3 cC = tex2D(ReShade::BackBuffer, uvC).rgb;
    float3 cD = tex2D(ReShade::BackBuffer, uvD).rgb;

    float sA = Dalashade_SurfaceReflectionStructureSource(uvA, cA) * structureBias;
    float sB = Dalashade_SurfaceReflectionStructureSource(uvB, cB) * structureBias;
    float sC = Dalashade_SurfaceReflectionStructureSource(uvC, cC) * structureBias;
    float sD = Dalashade_SurfaceReflectionStructureSource(uvD, cD) * structureBias;
    float eA = saturate(Dalashade_SurfaceReflectionSourceEnergy(cA, sourceBias, sourceBias * 0.72, sourceBias * 0.46, sourceBias * 0.32) + sA * 0.78);
    float eB = saturate(Dalashade_SurfaceReflectionSourceEnergy(cB, sourceBias, sourceBias * 0.74, sourceBias * 0.50, sourceBias * 0.34) + sB * 0.82);
    float eC = saturate(Dalashade_SurfaceReflectionSourceEnergy(cC, sourceBias, sourceBias * 0.68, sourceBias * 0.58, sourceBias * 0.38) + sC * 0.76);
    float eD = saturate(Dalashade_SurfaceReflectionSourceEnergy(cD, sourceBias, sourceBias * 0.62, sourceBias * 0.62, sourceBias * 0.42) + sD * 0.68);

    float gA = Dalashade_SurfaceReflectionDepthGate(uvA, depth, depthReject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvA, structureBias) * (0.18 + eA);
    float gB = Dalashade_SurfaceReflectionDepthGate(uvB, depth, depthReject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvB, structureBias) * (0.14 + eB) * 0.86;
    float gC = Dalashade_SurfaceReflectionDepthGate(uvC, depth, depthReject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvC, structureBias) * (0.10 + eC) * 0.66;
    float gD = Dalashade_SurfaceReflectionDepthGate(uvD, depth, depthReject) * Dalashade_SurfaceReflectionPseudoSampleSafety(uvD, structureBias) * (0.08 + eD) * 0.48;
    float weight = max(gA + gB + gC + gD, 0.001);
    float confidence = saturate((gA + gB + gC + gD) * (0.28 + sourceBias * 0.50 + structureBias * 0.34));

    return float4((cA * gA + cB * gB + cC * gC + cD * gD) / weight, confidence);
}

float4 Dalashade_SampleWaterStructureReflection(float2 uv, float depth, float2 direction, float offset, float structureBias, float waterBias)
{
    float2 dir = normalize(direction + float2(0.0001, -0.0003));
    float2 ripple = float2(BUFFER_PIXEL_SIZE.x * 2.2, 0.0);
    float2 uvA = saturate(uv + dir * offset * 1.10);
    float2 uvB = saturate(uv + dir * offset * 2.35 + ripple);
    float2 uvC = saturate(uv + dir * offset * 3.95 - ripple * 0.72);
    float2 uvD = saturate(uv + dir * offset * 5.85 + ripple * 0.42);

    float3 cA = tex2D(ReShade::BackBuffer, uvA).rgb;
    float3 cB = tex2D(ReShade::BackBuffer, uvB).rgb;
    float3 cC = tex2D(ReShade::BackBuffer, uvC).rgb;
    float3 cD = tex2D(ReShade::BackBuffer, uvD).rgb;

    float sA = Dalashade_SurfaceReflectionStructureSource(uvA, cA);
    float sB = Dalashade_SurfaceReflectionStructureSource(uvB, cB);
    float sC = Dalashade_SurfaceReflectionStructureSource(uvC, cC);
    float sD = Dalashade_SurfaceReflectionStructureSource(uvD, cD);

    float darkA = 1.0 - smoothstep(0.18, 0.58, Dalashade_SurfaceReflectionLuma(cA));
    float darkB = 1.0 - smoothstep(0.18, 0.58, Dalashade_SurfaceReflectionLuma(cB));
    float darkC = 1.0 - smoothstep(0.18, 0.58, Dalashade_SurfaceReflectionLuma(cC));
    float darkD = 1.0 - smoothstep(0.18, 0.58, Dalashade_SurfaceReflectionLuma(cD));

    float depthSoftA = saturate(0.74 + Dalashade_SurfaceReflectionDepthGate(uvA, depth, 0.24) * 0.26);
    float depthSoftB = saturate(0.70 + Dalashade_SurfaceReflectionDepthGate(uvB, depth, 0.20) * 0.24);
    float depthSoftC = saturate(0.66 + Dalashade_SurfaceReflectionDepthGate(uvC, depth, 0.18) * 0.22);
    float depthSoftD = saturate(0.60 + Dalashade_SurfaceReflectionDepthGate(uvD, depth, 0.16) * 0.20);

    float bias = saturate(structureBias * (0.45 + waterBias * 0.55));
    float wA = sA * darkA * depthSoftA * (0.34 + bias * 0.58);
    float wB = sB * darkB * depthSoftB * (0.36 + bias * 0.56);
    float wC = sC * darkC * depthSoftC * (0.34 + bias * 0.52);
    float wD = sD * darkD * depthSoftD * (0.28 + bias * 0.46);
    float weight = max(wA + wB + wC + wD, 0.001);
    float confidence = saturate((wA + wB + wC + wD) * (0.42 + bias * 0.46));

    return float4((cA * wA + cB * wB + cC * wC + cD * wD) / weight, confidence);
}

float3 Dalashade_SurfaceReflectionLocalSource(float2 uv, float depth, float radius)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float3 source = float3(0.0, 0.0, 0.0);
    float weight = 0.0;

    float2 offsetA = float2(texel.x, texel.y);
    float2 offsetB = float2(-texel.x, texel.y);
    float2 offsetC = float2(texel.x * 1.65, -texel.y * 0.85);
    float2 offsetD = float2(-texel.x * 1.65, -texel.y * 0.85);

    float3 cA = tex2D(ReShade::BackBuffer, saturate(uv + offsetA)).rgb;
    float3 cB = tex2D(ReShade::BackBuffer, saturate(uv + offsetB)).rgb;
    float3 cC = tex2D(ReShade::BackBuffer, saturate(uv + offsetC)).rgb;
    float3 cD = tex2D(ReShade::BackBuffer, saturate(uv + offsetD)).rgb;

    float dA = ReShade::GetLinearizedDepth(saturate(uv + offsetA));
    float dB = ReShade::GetLinearizedDepth(saturate(uv + offsetB));
    float dC = ReShade::GetLinearizedDepth(saturate(uv + offsetC));
    float dD = ReShade::GetLinearizedDepth(saturate(uv + offsetD));

    float wA = smoothstep(0.34, 1.05, Dalashade_SurfaceReflectionLuma(cA) + Dalashade_GetSaturation(cA) * 0.52) * saturate(1.0 - abs(depth - dA) * 7.0);
    float wB = smoothstep(0.34, 1.05, Dalashade_SurfaceReflectionLuma(cB) + Dalashade_GetSaturation(cB) * 0.52) * saturate(1.0 - abs(depth - dB) * 7.0);
    float wC = smoothstep(0.36, 1.08, Dalashade_SurfaceReflectionLuma(cC) + Dalashade_GetSaturation(cC) * 0.48) * saturate(1.0 - abs(depth - dC) * 5.4);
    float wD = smoothstep(0.36, 1.08, Dalashade_SurfaceReflectionLuma(cD) + Dalashade_GetSaturation(cD) * 0.48) * saturate(1.0 - abs(depth - dD) * 5.4);

    source += cA * wA;
    source += cB * wB;
    source += cC * wC * 0.78;
    source += cD * wD * 0.78;
    weight += wA + wB + wC * 0.78 + wD * 0.78;

    return weight > 0.001 ? source / weight : float3(0.0, 0.0, 0.0);
}

float3 Dalashade_RenderSurfaceReflectionDebug(float2 uv, float3 originalColor, float3 resultColor, float3 debugColor, float debugMask, float3 contribution)
{
    float opacity = saturate(Dalashade_SurfaceReflectionDebugOpacity);
    float boost = max(Dalashade_SurfaceReflectionDebugBoost, 0.001);
    float3 cleanDebug = saturate(float3(1.0, 1.0, 1.0) - exp(-max(debugColor, float3(0.0, 0.0, 0.0)) * boost));

    if (Dalashade_SurfaceReflectionDebugMode == 8 || Dalashade_SurfaceReflectionDebugOutputMode == 3)
    {
        return cleanDebug;
    }

    if (Dalashade_SurfaceReflectionDebugOutputMode == 1)
    {
        return lerp(originalColor, cleanDebug, saturate(debugMask * opacity));
    }

    if (Dalashade_SurfaceReflectionDebugOutputMode == 2)
    {
        return uv.x < 0.5 ? cleanDebug : originalColor;
    }

    if (Dalashade_SurfaceReflectionDebugOutputMode == 4)
    {
        return saturate(abs(resultColor - originalColor) * boost * 6.0 + cleanDebug * 0.18);
    }

    return cleanDebug;
}

float4 Dalashade_SurfaceReflectionPS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    if (Dalashade_SurfaceReflectionEnabled <= 0.0 || Dalashade_SurfaceReflectionStrength <= 0.0)
    {
        return float4(color, 1.0);
    }

    float depth = ReShade::GetLinearizedDepth(texcoord);
    float3 normal = Dalashade_SurfaceReflectionDepthNormal(texcoord, depth);
    float luma = Dalashade_SurfaceReflectionLuma(color);
    float bright = smoothstep(0.62, 0.98, luma);
    float midtone = Dalashade_RangeMask(luma, 0.10, 0.82);
    float shadow = 1.0 - smoothstep(0.10, 0.46, luma);
    float smoothness = Dalashade_GetSmoothness(texcoord);
    float edge = Dalashade_GetEdgeStrength(texcoord);
    float saturation = Dalashade_GetSaturation(color);

    Dalashade_FrameSceneSettings sceneSettings = Dalashade_FrameScene_DefaultSettings();
    sceneSettings.Readability = Dalashade_Readability;
    sceneSettings.HighlightProtection = Dalashade_HighlightProtection;
    sceneSettings.Wetness = Dalashade_Wetness;
    sceneSettings.MagicGlow = Dalashade_MagicGlow;
    sceneSettings.NeonGlow = Dalashade_NeonGlow;
    sceneSettings.CombatPressure = Dalashade_CombatPressure;
    sceneSettings.Night = Dalashade_Night;
    sceneSettings.ArtificialLight = Dalashade_ArtificialLight;
    sceneSettings.OpenSkyLight = Dalashade_OpenSkyLight;
    sceneSettings.DayReflection = Dalashade_DayReflection;
    sceneSettings.DayHighlightPressure = Dalashade_DayHighlightPressure;
    sceneSettings.CinematicPermission = Dalashade_CinematicPermission;
    sceneSettings.StandaloneStrength = Dalashade_StandaloneStrength;
    Dalashade_FrameSceneData scene = Dalashade_ResolveFrameSceneData(sceneSettings);

    Dalashade_FrameDataSettings frameSettings = Dalashade_FrameData_DefaultSettings();
    frameSettings.MaterialFoliage = Dalashade_MaterialFoliage;
    frameSettings.MaterialWaterSpecular = Dalashade_MaterialWaterSpecular;
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
    frameSettings.WetSurfaceContext = Dalashade_WetSurfaceContext;
    frameSettings.HighlightProtection = Dalashade_HighlightProtection;
    frameSettings.DepthAssistEnabled = Dalashade_EnableDepthAssist ? 1.0 : 0.0;
    frameSettings.DepthAssistStrength = Dalashade_DepthAssistStrength;
    frameSettings.DepthAssistConfidenceFloor = Dalashade_DepthAssistConfidenceFloor;
    frameSettings.NormalFieldEnabled = Dalashade_NormalFieldEnabled;
    frameSettings.NormalFieldStrength = Dalashade_NormalFieldStrength;
    frameSettings.NormalDepthStrength = Dalashade_NormalDepthStrength;
    frameSettings.NormalDetailStrength = Dalashade_NormalDetailStrength;
    frameSettings.NormalMaterialInfluence = Dalashade_NormalMaterialInfluence;
    frameSettings.NormalWaterSuppression = Dalashade_NormalWaterSuppression;
    frameSettings.NormalSkinSuppression = Dalashade_NormalSkinSuppression;
    frameSettings.NormalSkySuppression = Dalashade_NormalSkySuppression;
    Dalashade_FrameBaseData frame = Dalashade_ResolveFrameBaseData(color, texcoord, frameSettings);
    Dalashade_FrameSurfaceData surface = Dalashade_ResolveFrameSurfaceData(color, texcoord, frame, frameSettings);

    float skyReject = saturate(frame.MaterialSkyCloudFog * Dalashade_SurfaceReflectionSkyReject);
    float skinProtect = saturate(frame.MaterialSkinProtection * Dalashade_SurfaceReflectionSkinProtect);
    float gameplayDampen = scene.ReflectionDampen;
    float highlightSafety = saturate(scene.HighlightProtection * bright + skinProtect + skyReject * 0.85 + frame.MaterialSandDust * bright * 0.52 + frame.MaterialSnowIce * bright * 0.34);
    float surfaceFacing = saturate(0.42 + max(normal.z, 0.0) * 0.46 + smoothness * 0.18);
    float safeSurface = (1.0 - skyReject) * (1.0 - skinProtect * 0.92) * gameplayDampen;

    float reflectionOffset = Dalashade_ReflectionSampleOffset * (0.80 + Dalashade_WaterSheenRadius * 0.18 + scene.Night * 0.10);
    float reflectionSoftness = saturate(Dalashade_ReflectionSoftness);
    float reflectionDepthReject = saturate(Dalashade_ReflectionDepthReject);
    float2 verticalReflectDirection = normalize(float2(normal.x * 0.12, -1.0));
    float2 glintStreakDirection = normalize(float2(0.88 + normal.x * 0.18, -0.32 - normal.y * 0.12));
    float3 reflectedVertical = Dalashade_SampleReflectionColor(texcoord, depth, verticalReflectDirection, reflectionOffset, reflectionSoftness, reflectionDepthReject);
    float3 reflectedSoft = Dalashade_SampleReflectionColor(texcoord, depth, verticalReflectDirection, reflectionOffset * 1.85, saturate(reflectionSoftness + 0.22), reflectionDepthReject);
    float3 reflectedStreak = Dalashade_SampleReflectionStreak(texcoord, depth, glintStreakDirection, reflectionOffset * 1.45, reflectionDepthReject);
    float verticalDepthContinuity = Dalashade_SurfaceReflectionDepthGate(texcoord + verticalReflectDirection * reflectionOffset, depth, reflectionDepthReject);
    float streakDepthContinuity = Dalashade_SurfaceReflectionDepthGate(texcoord + glintStreakDirection * reflectionOffset * 1.45, depth, reflectionDepthReject);
    skyReject = saturate(max(skyReject, frame.SafetySkyReject * Dalashade_SurfaceReflectionSkyReject));
    skinProtect = saturate(max(skinProtect, frame.SafetySkinReject * Dalashade_SurfaceReflectionSkinProtect));
    safeSurface = (1.0 - skyReject) * (1.0 - skinProtect * 0.92) * gameplayDampen;
    highlightSafety = saturate(max(max(highlightSafety, scene.DayHighlightPressure), frame.SafetyHighlightProtect));
    float normalFieldInfluence = saturate(Dalashade_NormalFieldEnabled * Dalashade_NormalFieldStrength * Dalashade_NormalMaterialInfluence);
    float normalReceiverSafety = saturate(
        safeSurface
        * (1.0 - frame.SafetySkyReject * 0.96)
        * (1.0 - frame.SafetySkinReject * 0.92)
        * (1.0 - frame.SafetyFoliageNoiseReject * 0.48)
        * (1.0 - frame.WaterHorizonOnly * 0.82));
    float normalReflectionSupport = saturate(
        normalFieldInfluence
        * surface.ReflectionReceiverSupport
        * (0.34 + surface.NormalConfidence * 0.24 + surface.OrientationConfidence * 0.18)
        * normalReceiverSafety);
    float normalStructureSupport = saturate(
        normalFieldInfluence
        * surface.StructureCandidate
        * (0.24 + surface.NormalConfidence * 0.20)
        * normalReceiverSafety);
    float normalGroundStability = saturate(
        normalFieldInfluence
        * surface.GroundCandidate
        * (0.24 + surface.NormalConfidence * 0.16 + surface.OrientationConfidence * 0.14)
        * normalReceiverSafety);
    float normalEdgeLeakRisk = saturate(
        normalFieldInfluence
        * surface.EdgeDiscontinuity
        * (0.35 + (1.0 - surface.NormalConfidence) * 0.32 + frame.SafetyHighlightProtect * 0.22));

    float depthSurfaceConfidence = saturate(0.56 + max(normal.z, 0.0) * 0.22 + surface.NormalConfidence * normalFieldInfluence * 0.18 - surface.EdgeDiscontinuity * normalFieldInfluence * 0.24);
    float waterFresnel = Dalashade_SurfaceReflectionFresnel(normal, 0.14, 1.45);
    float wetFresnel = Dalashade_SurfaceReflectionFresnel(normal, 0.08, 1.70);
    float hardFresnel = Dalashade_SurfaceReflectionFresnel(normal, 0.05, 2.10);
    float iceFresnel = Dalashade_SurfaceReflectionFresnel(normal, 0.10, 1.85);

    float warmDryReject = saturate(frame.WaterSandReject + frame.MaterialSandDust * (0.22 + bright * 0.42));
    float structureHardReceiver = saturate(
        frame.ReceiverStructure * 0.34
        + frame.MaterialSurfaceHardness * 0.26
        + frame.MaterialStoneRuins * 0.24
        + frame.MaterialMetalIndustrial * 0.24);
    float hardSurfaceHint = saturate(
        structureHardReceiver * 0.42
        + frame.MaterialMetalIndustrial * 0.28
        + frame.WaterSpecularGlint * 0.16
        + midtone * smoothness * 0.20);
    float hardReflectionFamily = saturate(
        frame.MaterialMetalIndustrial * 0.26
        + frame.MaterialStoneRuins * 0.20
        + frame.MaterialNeonGlass * 0.18
        + frame.MaterialCrystalAether * 0.14
        + frame.MaterialSnowIce * 0.08
        + frame.WaterSpecularGlint * 0.16
        + structureHardReceiver * 0.18);
    float waterSurfaceQuality = saturate(
        frame.WaterReceiver
        * (0.54 + frame.WaterPixelConfidence * 0.28 + frame.WaterShallow * 0.18 + frame.WaterWetShoreline * 0.12)
        * (1.0 - frame.WaterHorizonOnly * 0.60)
        * (1.0 - frame.WaterSandReject * 0.42)
        * depthSurfaceConfidence);
    float wetPolish = saturate(
        max(Dalashade_Wetness, Dalashade_WetSurfaceContext)
        * (0.36 + smoothness * 0.48 + normalGroundStability * 0.16)
        * (hardSurfaceHint * 0.42 + structureHardReceiver * 0.26 + frame.MaterialStoneRuins * 0.16 + frame.MaterialMetalIndustrial * 0.14)
        * (1.0 - bright * 0.34)
        * safeSurface);
    float metalPolish = saturate(
        frame.MaterialMetalIndustrial
        * (0.28 + smoothness * 0.42 + hardSurfaceHint * 0.22 + frame.ReceiverReflection * 0.22)
        * (0.78 + hardFresnel * 0.22)
        * safeSurface);
    float aetherPolish = saturate(
        (frame.MaterialCrystalAether + frame.MaterialNeonGlass)
        * (0.26 + smoothness * 0.32 + frame.ReceiverReflection * 0.22 + hardSurfaceHint * 0.16)
        * (0.72 + hardFresnel * 0.28)
        * safeSurface);
    float icePolish = saturate(
        frame.MaterialSnowIce
        * smoothstep(0.30, 0.96, smoothness + frame.MaterialSurfaceSmoothness * 0.28)
        * midtone
        * (1.0 - bright * 0.58)
        * safeSurface);
    float waterSpecificReceiver = saturate(frame.WaterReceiver);
    float sharedReflectionReceiver = saturate(frame.ReceiverReflection);
    float nonSkyGate = saturate(1.0 - frame.SafetySkyReject * 0.95);
    float nonSkinGate = saturate(1.0 - frame.SafetySkinReject * 0.90);
    float foliageGate = saturate(1.0 - frame.SafetyFoliageNoiseReject * 0.45 - frame.MaterialFoliage * 0.25);
    float receiverSafety = nonSkyGate * nonSkinGate * foliageGate * gameplayDampen;
    float standaloneStrength = saturate(Dalashade_StandaloneStrength);
    float standaloneReflection = saturate(standaloneStrength * receiverSafety * (1.0 - highlightSafety * 0.35));
    float sharedReceiverSupport = saturate(
        sharedReflectionReceiver
        * (1.0 - frame.SafetySkyReject * 0.88)
        * (1.0 - frame.SafetySkinReject * 0.88)
        * (1.0 - frame.SafetyFoliageNoiseReject * 0.35)
        * (1.0 - frame.SafetyHighlightProtect * 0.18)
        * safeSurface);

    float3 sheenSample = Dalashade_SurfaceReflectionDirectionalSheen(texcoord, Dalashade_WaterSheenRadius, depth);
    float3 cyanSheen = lerp(float3(0.04, 0.20, 0.24), float3(0.12, 0.58, 0.68), saturate(saturation + frame.WaterShallow + frame.MaterialWaterPlane));
    float3 waterSkyTint = lerp(cyanSheen, float3(0.48, 0.82, 1.0), saturate(frame.WaterSkySource * 0.32 + scene.OpenSkyLight * 0.22 + frame.WaterHorizon * 0.18));
    float3 waterDepthTint = lerp(waterSkyTint, float3(0.08, 0.42, 0.48), saturate(frame.WaterShallow * 0.36 + frame.WaterWetShoreline * 0.18));
    float waterReceiver = frame.WaterReceiver
        * surfaceFacing
        * verticalDepthContinuity
        * safeSurface
        * (0.82 + waterFresnel * 0.16 + waterSurfaceQuality * 0.10);
    waterReceiver = saturate(
        waterReceiver
        + sharedReceiverSupport * waterSpecificReceiver * surfaceFacing * verticalDepthContinuity * 0.14
        + normalReflectionSupport * waterSpecificReceiver * verticalDepthContinuity * 0.055);
    float waterMicroRipple = saturate((frame.WaterFoamOrEdge * 0.42 + frame.WaterWetShoreline * 0.28 + edge * 0.18) * (1.0 - frame.WaterSandReject * 0.38));
    float waterProjectionNoiseReject = saturate(frame.WaterFoamOrEdge * 0.45 + waterMicroRipple * 0.35 + edge * 0.18);
    float3 waterSheen = lerp(waterDepthTint, sheenSample, 0.48) * waterReceiver * Dalashade_WaterSheenStrength * (1.62 + waterFresnel * 0.26 + waterMicroRipple * 0.14) * lerp(1.0, 1.16, standaloneReflection);
    float reflectedWaterLuma = Dalashade_SurfaceReflectionLuma(reflectedVertical);
    float skyColorSource = saturate(max(frame.WaterSkySource, smoothstep(0.16, 0.88, reflectedWaterLuma + Dalashade_GetSaturation(reflectedVertical) * 0.36) * (0.28 + frame.WaterHorizon * 0.30 + scene.OpenSkyLight * 0.18)));
    float waterSource = saturate(max(frame.WaterSource, frame.WaterWetShoreline * 0.35) + skyColorSource * frame.WaterReceiver * 0.32);
    float waterReflectionMask = waterReceiver * saturate(waterSource + skyColorSource * (0.25 + scene.DayReflection * 0.18) + waterSurfaceQuality * 0.16) * (1.0 - bright * 0.26);
    float3 waterReflection = lerp(reflectedVertical, reflectedSoft, reflectionSoftness * 0.42) * lerp(float3(0.68, 0.98, 1.0), waterDepthTint + 0.18, 0.36) * waterReflectionMask * Dalashade_WaterReflectionStrength * (1.0 + waterFresnel * 0.16 + waterSurfaceQuality * 0.10) * lerp(1.0, 1.20, standaloneReflection);
    float shorelineSheenMask = saturate((frame.WaterWetShoreline * 0.72 + frame.WaterFoamOrEdge * 0.42) * safeSurface * (1.0 - skinProtect * 0.92));
    waterSheen += lerp(float3(0.26, 0.72, 0.86), sheenSample, 0.32) * shorelineSheenMask * Dalashade_WaterSheenStrength * (0.72 + waterMicroRipple * 0.16) * lerp(1.0, 1.14, standaloneReflection);

    float glintShape = smoothstep(0.44, 0.98, luma) * smoothstep(0.025, 0.32, edge) * (1.0 - smoothness * 0.48);
    float specularGlintSource = saturate(max(frame.WaterSpecularGlint * glintShape, frame.WaterFoamOrEdge * 0.38) * streakDepthContinuity * (1.0 - waterReceiver * 0.16) * (1.0 - skinProtect * 0.80) * (1.0 - skyReject * 0.78) * (1.0 - warmDryReject * 0.28));
    float metalReceiver = frame.MaterialMetalIndustrial
        * hardSurfaceHint
        * (0.22 + sharedReflectionReceiver * 0.44 + metalPolish * 0.20 + normalReflectionSupport * 0.10 + smoothstep(0.02, 0.34, edge) * 0.26 + specularGlintSource * 0.32)
        * safeSurface
        * (1.0 - waterReceiver * 0.85)
        * (1.0 - skyReject * 0.96);
    float3 specularGlint = lerp(float3(0.65, 0.86, 1.0), max(color + 0.26, reflectedStreak), 0.44) * specularGlintSource * Dalashade_SpecularGlintStrength * (1.54 + hardFresnel * 0.20 + metalPolish * 0.16);
    float3 specularReflection = reflectedStreak * specularGlintSource * max(metalReceiver, waterReceiver * 0.35) * Dalashade_SpecularReflectionStrength * (0.86 + hardFresnel * 0.14 + metalPolish * 0.12) * lerp(1.0, 1.14, standaloneReflection);

    float receiverWetness = max(Dalashade_Wetness, Dalashade_WetSurfaceContext);
    float wetHardSurfaceReceiver = receiverWetness
        * saturate(hardSurfaceHint + frame.MaterialStoneRuins * 0.20 + frame.ReceiverStructure * 0.18)
        * smoothstep(0.36, 0.94, smoothness)
        * smoothstep(0.0, 0.62, 1.0 - edge)
        * (0.58 + shadow * 0.54)
        * safeSurface
        * (1.0 - bright * 0.42)
        * (1.0 - frame.MaterialSandDust * bright * 0.72)
        * (1.0 - frame.MaterialSnowIce * bright * 0.56)
        * (1.0 - waterReceiver * 0.55);
    wetHardSurfaceReceiver = saturate(
        wetHardSurfaceReceiver
        + sharedReceiverSupport
            * hardSurfaceHint
            * smoothstep(0.30, 0.92, smoothness)
            * (0.36 + frame.MaterialStoneRuins * 0.24 + structureHardReceiver * 0.20)
            * (1.0 - waterSpecificReceiver * 0.35)
            * (0.12 + wetPolish * 0.035)
        + normalReflectionSupport
            * saturate(hardSurfaceHint * 0.42 + sharedReflectionReceiver * 0.38 + structureHardReceiver * 0.20)
            * (1.0 - waterSpecificReceiver * 0.42)
            * (0.040 + normalGroundStability * 0.045 + receiverWetness * 0.040));
    wetHardSurfaceReceiver = saturate(wetHardSurfaceReceiver + wetPolish * (0.18 + wetFresnel * 0.10) * (1.0 - waterReceiver * 0.48));
    float3 wetReflection = lerp(sheenSample, reflectedVertical, 0.60) * wetHardSurfaceReceiver * (specularGlintSource * 0.55 + receiverWetness * 0.42 + wetPolish * 0.22) * Dalashade_WetReflectionStrength * (1.78 + wetFresnel * 0.18) * lerp(1.0, 1.18, standaloneReflection);

    float3 localSource = Dalashade_SurfaceReflectionLocalSource(texcoord, depth, max(Dalashade_WaterSheenRadius, 0.75) * (1.25 + Dalashade_Night * 0.45));
    float localSourceEnergy = smoothstep(0.18, 0.96, Dalashade_SurfaceReflectionLuma(localSource) + Dalashade_GetSaturation(localSource) * 0.62);
    float aetherMask = saturate(frame.MaterialCrystalAether * (0.54 + Dalashade_MagicGlow * 0.56) * smoothstep(0.12, 0.82, luma + saturation * 0.58));
    float neonMask = saturate(frame.MaterialNeonGlass * (0.54 + Dalashade_NeonGlow * 0.56) * smoothstep(0.16, 0.86, luma + saturation * 0.62));
    float fireMask = saturate(frame.MaterialFireLavaHeat * smoothstep(0.20, 0.88, luma + saturation * 0.42));
    float aetherNeonSource = saturate(aetherMask + neonMask + localSourceEnergy * (frame.MaterialCrystalAether + frame.MaterialNeonGlass) * 0.42);
    float fireLampSource = saturate(fireMask + localSourceEnergy * frame.MaterialFireLavaHeat * 0.38 + Dalashade_ArtificialLight * specularGlintSource * 0.22);
    float aetherGlassReceiver = saturate((frame.MaterialCrystalAether + frame.MaterialNeonGlass) * (smoothness * 0.42 + hardSurfaceHint * 0.36 + metalReceiver * 0.28 + sharedReflectionReceiver * 0.18 + normalReflectionSupport * 0.08 + aetherPolish * 0.20) * safeSurface * (1.0 - skyReject * 0.96) * (1.0 - waterReceiver * 0.42));
    float emissiveReflectionMask = saturate((aetherNeonSource + fireLampSource * 0.72) * (aetherGlassReceiver + metalReceiver * 0.46 + wetHardSurfaceReceiver * 0.32 + waterReceiver * 0.18));
    float3 aetherNeonColor = frame.MaterialCrystalAether * float3(0.16, 0.44, 1.0) * Dalashade_AetherReflectionStrength;
    aetherNeonColor += frame.MaterialNeonGlass * float3(0.48, 0.22, 1.0) * Dalashade_NeonReflectionStrength;
    aetherNeonColor += frame.MaterialFireLavaHeat * float3(1.0, 0.30, 0.06) * 0.42;
    float3 aetherStreak = lerp(localSource, reflectedStreak, 0.55);
    float3 aetherNeonReflection = saturate(aetherNeonColor + aetherStreak * (0.58 + aetherPolish * 0.16) + reflectedSoft * 0.12) * emissiveReflectionMask * (1.82 + hardFresnel * 0.18 + aetherPolish * 0.18) * lerp(1.0, 1.16, standaloneReflection);

    float iceReceiver = frame.MaterialSnowIce * smoothstep(0.44, 0.96, smoothness + icePolish * 0.24) * midtone * safeSurface * (1.0 - bright * 0.70) * (1.0 - skyReject * 0.95);
    float3 iceSheen = float3(0.46, 0.72, 1.0) * iceReceiver * (skyColorSource * 0.35 + specularGlintSource * 0.24 + iceFresnel * 0.22 + 0.18) * Dalashade_IceSheenStrength * (1.38 + icePolish * 0.18);

    float waterSurfaceReceiver = saturate(
        waterReceiver
        * receiverSafety
        * (1.0 - frame.WaterHorizonOnly * 0.85));
    float wetMetalGlassReceiver = saturate(
        sharedReflectionReceiver
        * receiverSafety
        * (1.0 - waterSpecificReceiver * 0.25)
        * (hardReflectionFamily * 0.58
            + smoothness * hardSurfaceHint * 0.18
            + receiverWetness * structureHardReceiver * 0.16
            + normalReflectionSupport * 0.08));
    float screenReflectionReceiver = saturate(waterSurfaceReceiver + wetMetalGlassReceiver * 0.65);
    float waterSourceColorSupport = saturate(0.24 + frame.WaterSkySource * 0.30 + frame.WaterSource * 0.24 + frame.WaterPixelConfidence * 0.28);
    float waterQualifiedSourceBias = saturate(frame.WaterSkySource * 0.36 + frame.WaterSource * 0.30 + frame.WaterHorizonOnly * 0.22 + frame.WaterPixelConfidence * 0.22);
    float waterStructureSourceBias = saturate(waterSurfaceReceiver * (0.34 + frame.ReceiverStructure * 0.18 + frame.MaterialMetalIndustrial * 0.12 + frame.WaterPixelConfidence * 0.18 + normalStructureSupport * 0.10));
    float waterTraceObjectBias = saturate(waterStructureSourceBias + frame.ReceiverStructure * waterSurfaceReceiver * 0.18 + normalStructureSupport * waterSurfaceReceiver * 0.12);
    float foregroundSilhouetteRisk = smoothstep(0.54, 0.92, texcoord.y) * smoothstep(0.055, 0.34, edge) * (1.0 - waterSurfaceQuality * 0.35);
    float hardSourceEnergy = saturate(localSourceEnergy * 0.46 + specularGlintSource * 0.34 + aetherNeonSource * 0.30 + fireLampSource * 0.22);
    float hardQualifiedSourceBias = saturate(sharedReflectionReceiver * 0.34 + frame.WaterSpecularGlint * 0.24 + frame.MaterialMetalIndustrial * 0.18 + receiverWetness * 0.16);
    float aetherQualifiedSourceBias = saturate(aetherNeonSource + frame.MaterialCrystalAether * 0.22 + frame.MaterialNeonGlass * 0.22);
    float fireQualifiedSourceBias = saturate(fireLampSource + frame.MaterialFireLavaHeat * 0.24);
    float3 qualifiedWaterSourceColor = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, verticalReflectDirection, reflectionOffset * 1.12, reflectionSoftness, reflectionDepthReject, waterQualifiedSourceBias, 0.10, 0.08, 0.04);
    float3 qualifiedHardSourceColor = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, glintStreakDirection, reflectionOffset * 1.28, saturate(reflectionSoftness * 0.72), reflectionDepthReject, 0.08, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias);
    float qualifiedWaterEnergy = Dalashade_SurfaceReflectionSourceEnergy(qualifiedWaterSourceColor, waterQualifiedSourceBias, 0.10, 0.08, 0.04);
    float qualifiedHardEnergy = Dalashade_SurfaceReflectionSourceEnergy(qualifiedHardSourceColor, 0.08, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias);
    float normalProjectionStability = saturate(0.82 + normalFieldInfluence * (surface.NormalConfidence * 0.10 + surface.OrientationConfidence * 0.08 - normalEdgeLeakRisk * 0.18));
    float2 wetFloorDirection = normalize(float2(normal.x * (0.18 + normalGroundStability * 0.05), -0.62 - normal.y * 0.08));
    float2 hardProjectionDirection = normalize(float2(0.92 + normal.x * (0.22 + normalStructureSupport * 0.04), -0.22 - normal.y * 0.18));
    float3 projectedWaterNear = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, verticalReflectDirection, reflectionOffset * 1.20, reflectionSoftness, reflectionDepthReject, waterQualifiedSourceBias, 0.08, 0.06, 0.03);
    float3 projectedWaterMid = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, verticalReflectDirection, reflectionOffset * 1.72, saturate(reflectionSoftness + 0.08), reflectionDepthReject, waterQualifiedSourceBias, 0.07, 0.06, 0.03);
    float3 projectedWaterFar = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, verticalReflectDirection, reflectionOffset * 2.50, saturate(reflectionSoftness + 0.20), reflectionDepthReject, waterQualifiedSourceBias, 0.06, 0.06, 0.03);
    float3 projectedWaterStack = projectedWaterNear * 0.42 + projectedWaterMid * 0.34 + projectedWaterFar * 0.24;
    float4 planarWaterTrace = Dalashade_SamplePlanarApproxTrace(texcoord, depth, verticalReflectDirection, reflectionOffset * (1.18 + waterFresnel * 0.18), reflectionDepthReject, waterQualifiedSourceBias, waterStructureSourceBias, reflectionSoftness);
    float3 projectedWetFloorSource = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, wetFloorDirection, reflectionOffset * (0.76 + wetFresnel * 0.10), saturate(reflectionSoftness * 0.74), reflectionDepthReject, 0.06, hardQualifiedSourceBias, aetherQualifiedSourceBias * 0.55, fireQualifiedSourceBias * 0.55);
    float3 projectedWetFloorWide = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, wetFloorDirection, reflectionOffset * (1.18 + wetFresnel * 0.16), saturate(reflectionSoftness * 0.92 + 0.08), reflectionDepthReject, 0.05, hardQualifiedSourceBias, aetherQualifiedSourceBias * 0.45, fireQualifiedSourceBias * 0.45);
    float3 projectedHardAetherSource = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, hardProjectionDirection, reflectionOffset * (0.90 + hardFresnel * 0.10), saturate(reflectionSoftness * 0.44), reflectionDepthReject, 0.04, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias);
    float3 projectedHardTight = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, hardProjectionDirection, reflectionOffset * 0.58, saturate(reflectionSoftness * 0.30), reflectionDepthReject, 0.03, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias);
    float projectedWaterEnergy = Dalashade_SurfaceReflectionSourceEnergy(projectedWaterStack, waterQualifiedSourceBias, 0.08, 0.06, 0.03);
    float projectedWetEnergy = Dalashade_SurfaceReflectionSourceEnergy(projectedWetFloorSource, 0.06, hardQualifiedSourceBias, aetherQualifiedSourceBias * 0.55, fireQualifiedSourceBias * 0.55);
    float projectedHardEnergy = max(
        Dalashade_SurfaceReflectionSourceEnergy(projectedHardAetherSource, 0.04, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias),
        Dalashade_SurfaceReflectionSourceEnergy(projectedHardTight, 0.04, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias));
    float3 hardSourceColor = lerp(qualifiedHardSourceColor, reflectedStreak, saturate(specularGlintSource * 0.24));
    hardSourceColor = lerp(hardSourceColor, max(localSource, hardSourceColor), hardSourceEnergy * 0.48);
    float wetFloorProjectionReceiver = saturate((wetHardSurfaceReceiver + wetPolish * 0.22 + wetMetalGlassReceiver * receiverWetness * 0.42 + normalGroundStability * wetHardSurfaceReceiver * 0.055) * (1.0 - waterSurfaceReceiver * 0.45));
    float hardAetherProjectionReceiver = saturate((metalReceiver + aetherGlassReceiver + metalPolish * 0.22 + aetherPolish * 0.26 + wetMetalGlassReceiver * (frame.MaterialMetalIndustrial + frame.MaterialCrystalAether + frame.MaterialNeonGlass) * 0.34 + normalStructureSupport * (metalReceiver + aetherGlassReceiver) * 0.055) * (1.0 - waterSurfaceReceiver * 0.35));
    float waterMirrorLine = saturate(0.50 - scene.OpenSkyLight * 0.035 + frame.WaterHorizon * 0.060 + frame.WaterHorizonOnly * 0.050 + normal.y * 0.025);
    float waterMirrorStretch = saturate(0.52 + waterSurfaceQuality * 0.25 + waterFresnel * 0.18 + standaloneReflection * 0.10);
    float waterMirrorRoughness = saturate(reflectionSoftness * 0.55 + waterMicroRipple * 0.35 + frame.WaterFoamOrEdge * 0.18);
    float waterMirrorSourceBias = saturate(waterSourceColorSupport * 0.42 + waterTraceObjectBias * 0.44 + scene.OpenSkyLight * 0.10 + scene.ArtificialLight * 0.08);
    float waterMirrorStructureBias = saturate(waterTraceObjectBias * 0.70 + frame.ReceiverStructure * waterSurfaceReceiver * 0.18 + normalStructureSupport * waterSurfaceReceiver * 0.18);
    float4 waterMirrorProjection = Dalashade_SampleWaterColumnProjection(
        texcoord,
        waterMirrorRoughness,
        waterMirrorSourceBias,
        waterMirrorStructureBias);
    float waterMirrorDistanceFade = smoothstep(waterMirrorLine + 0.012, waterMirrorLine + 0.18, texcoord.y) * (1.0 - smoothstep(0.94, 1.0, texcoord.y));
    float waterMirrorMask = saturate(
        waterSurfaceReceiver
        * waterMirrorProjection.a
        * waterMirrorDistanceFade
        * (0.48 + waterMirrorSourceBias * 0.40 + waterMirrorStructureBias * 0.52 + waterSurfaceQuality * 0.22)
        * (1.0 - frame.WaterHorizonOnly * 0.36)
        * (1.0 - frame.WaterSandReject * 0.24)
        * (1.0 - waterProjectionNoiseReject * 0.18)
        * (1.0 - normalEdgeLeakRisk * 0.22)
        * (1.0 - foregroundSilhouetteRisk * 0.20)
        * (1.0 - skinProtect * 0.46));
    float waterMirrorVisibility = sqrt(saturate(waterMirrorMask));
    float3 waterMirrorColor = lerp(waterMirrorProjection.rgb, waterDepthTint + 0.08, saturate(0.055 + waterMirrorRoughness * 0.11 + frame.WaterFoamOrEdge * 0.05));
    float3 projectedWaterColor = lerp(lerp(projectedWaterStack, planarWaterTrace.rgb, saturate(planarWaterTrace.a * 0.24)), waterMirrorColor, saturate(waterMirrorMask * 2.20)) * lerp(float3(0.72, 0.98, 1.0), waterDepthTint + 0.22, 0.22);
    float3 projectedWetFloorColor = lerp(lerp(projectedWetFloorSource, projectedWetFloorWide, 0.34), hardSourceColor, 0.30);
    float3 projectedHardAetherColor = lerp(lerp(projectedHardAetherSource, projectedHardTight, saturate(specularGlintSource * 0.28 + aetherPolish * 0.22)), hardSourceColor, saturate(specularGlintSource * 0.22 + aetherNeonSource * 0.24));
    float waterPlanarTraceMask = saturate(
        waterSurfaceReceiver
        * planarWaterTrace.a
        * (0.22 + waterTraceObjectBias * 0.52 + waterSourceColorSupport * 0.14 + waterSurfaceQuality * 0.12)
        * (1.0 - frame.WaterHorizonOnly * 0.52)
        * (1.0 - frame.WaterSandReject * 0.34)
        * (1.0 - waterProjectionNoiseReject * 0.45)
        * (1.0 - normalEdgeLeakRisk * 0.42)
        * (1.0 - foregroundSilhouetteRisk * 0.32)
        * (1.0 - skinProtect * 0.50));
    float waterProjectionAgreement = saturate(
        waterSurfaceReceiver
        * (0.10 + waterSourceColorSupport * 0.14 + projectedWaterEnergy * 0.14 + waterSurfaceQuality * 0.10)
        * (1.0 - waterProjectionNoiseReject * 0.32)
        + waterPlanarTraceMask * 0.28
        + waterMirrorMask * 1.15);
    float wetProjectionAgreement = saturate(wetFloorProjectionReceiver * (0.20 + max(projectedWetEnergy, hardSourceEnergy) * 0.58 + receiverWetness * 0.18 + wetPolish * 0.16));
    float hardProjectionAgreement = saturate(hardAetherProjectionReceiver * (0.18 + max(projectedHardEnergy, qualifiedHardEnergy) * 0.62 + aetherNeonSource * 0.20 + specularGlintSource * 0.12 + max(metalPolish, aetherPolish) * 0.14));
    float waterProjectionStrength = waterProjectionAgreement * Dalashade_WaterReflectionStrength * (0.260 + waterFresnel * 0.040) * normalProjectionStability * lerp(1.0, 1.22, standaloneReflection);
    float wetProjectionStrength = wetProjectionAgreement * Dalashade_WetReflectionStrength * (0.145 + wetFresnel * 0.035) * normalProjectionStability * lerp(1.0, 1.18, standaloneReflection);
    float hardProjectionStrength = hardProjectionAgreement * max(max(Dalashade_SpecularReflectionStrength, Dalashade_AetherReflectionStrength), Dalashade_NeonReflectionStrength) * (0.120 + hardFresnel * 0.030) * normalProjectionStability * lerp(1.0, 1.16, standaloneReflection);
    float3 waterProjectedReflection = (lerp(color, projectedWaterColor, 0.78) - color) * waterProjectionStrength;
    float3 waterMirrorReflection = (lerp(color, waterMirrorColor, 0.86) - color)
        * waterMirrorMask
        * Dalashade_WaterReflectionStrength
        * (0.56 + waterFresnel * 0.120 + waterMirrorVisibility * 0.10)
        * normalProjectionStability
        * lerp(1.0, 1.24, standaloneReflection);
    float3 waterPlanarTraceReflection = (lerp(color, lerp(planarWaterTrace.rgb, waterDepthTint + 0.06, 0.18), 0.74) - color)
        * waterPlanarTraceMask
        * Dalashade_WaterReflectionStrength
        * (0.30 + waterFresnel * 0.055)
        * normalProjectionStability
        * lerp(1.0, 1.18, standaloneReflection);
    float3 wetProjectedReflection = (lerp(color, projectedWetFloorColor, 0.46) - color) * wetProjectionStrength;
    float3 hardProjectedReflection = (lerp(color, projectedHardAetherColor, 0.42) - color) * hardProjectionStrength;
    float pseudoSourceBias = saturate(
        waterQualifiedSourceBias * waterSurfaceReceiver * 0.42
        + hardQualifiedSourceBias * wetMetalGlassReceiver * 0.34
        + aetherQualifiedSourceBias * hardAetherProjectionReceiver * 0.28
        + fireQualifiedSourceBias * hardAetherProjectionReceiver * 0.18);
    float wetStructureSourceBias = saturate(wetFloorProjectionReceiver * (0.18 + frame.ReceiverStructure * 0.20 + frame.MaterialMetalIndustrial * 0.14 + normalStructureSupport * 0.12));
    float hardStructureSourceBias = saturate(hardAetherProjectionReceiver * (0.12 + frame.ReceiverStructure * 0.12 + frame.MaterialMetalIndustrial * 0.18 + normalStructureSupport * 0.12));
    float4 waterStructureSample = Dalashade_SampleWaterStructureReflection(texcoord, depth, verticalReflectDirection, reflectionOffset * (1.85 + waterFresnel * 0.28), waterStructureSourceBias, waterSurfaceQuality);
    float waterStructureMask = saturate(
        waterSurfaceReceiver
        * waterStructureSample.a
        * (0.40 + waterStructureSourceBias * 0.48 + frame.WaterPixelConfidence * 0.22 + waterSurfaceQuality * 0.22)
        * (1.0 - frame.WaterHorizonOnly * 0.48)
        * (1.0 - frame.WaterSandReject * 0.38)
        * (1.0 - normalEdgeLeakRisk * 0.28));
    float3 waterStructureColor = lerp(waterStructureSample.rgb, waterDepthTint * 0.42, 0.18);
    float3 waterStructureReflection = (lerp(color, waterStructureColor, 0.70) - color)
        * waterStructureMask
        * Dalashade_WaterReflectionStrength
        * (0.235 + waterFresnel * 0.040)
        * lerp(1.0, 1.16, standaloneReflection);
    float waterStructureShape = sqrt(saturate(waterStructureMask));
    float waterStructureVisibility = saturate(
        waterStructureShape
        * waterSurfaceReceiver
        * (0.52 + waterSurfaceQuality * 0.26 + waterFresnel * 0.18)
        * (1.0 - frame.WaterHorizonOnly * 0.42)
        * (1.0 - frame.WaterSandReject * 0.32)
        * receiverSafety);
    float3 waterStructureSilhouette = (waterStructureColor - color)
        * waterStructureVisibility
        * Dalashade_WaterReflectionStrength
        * (0.46 + standaloneReflection * 0.10);
    float3 waterStructureRim = (waterDepthTint + 0.08 - color)
        * saturate(waterStructureVisibility * frame.WaterFoamOrEdge * 0.34)
        * Dalashade_WaterReflectionStrength
        * (0.12 + waterFresnel * 0.05);
    float4 pseudoWaterSample = Dalashade_SamplePseudoSSR(texcoord, depth, verticalReflectDirection, reflectionOffset * (1.34 + waterFresnel * 0.10), reflectionDepthReject, saturate(waterQualifiedSourceBias + frame.WaterPixelConfidence * 0.28), waterStructureSourceBias);
    float4 pseudoWetSample = Dalashade_SamplePseudoSSR(texcoord, depth, wetFloorDirection, reflectionOffset * (0.92 + wetFresnel * 0.10), reflectionDepthReject, saturate(hardQualifiedSourceBias + receiverWetness * 0.22 + wetPolish * 0.12), wetStructureSourceBias);
    float4 pseudoHardSample = Dalashade_SamplePseudoSSR(texcoord, depth, hardProjectionDirection, reflectionOffset * (1.06 + hardFresnel * 0.12), reflectionDepthReject, saturate(aetherQualifiedSourceBias + hardQualifiedSourceBias * 0.42 + max(metalPolish, aetherPolish) * 0.12), hardStructureSourceBias);
    float pseudoWaterMask = saturate(
        waterSurfaceReceiver
        * pseudoWaterSample.a
        * (0.30 + waterSourceColorSupport * 0.20 + projectedWaterEnergy * 0.12 + waterTraceObjectBias * 0.46 + waterSurfaceQuality * 0.08)
        * (1.0 - waterProjectionNoiseReject * 0.38)
        * (1.0 - frame.WaterHorizonOnly * 0.40)
        * (1.0 - normalEdgeLeakRisk * 0.36)
        * (1.0 - foregroundSilhouetteRisk * 0.28));
    float pseudoWetMask = saturate(wetFloorProjectionReceiver * pseudoWetSample.a * (0.28 + receiverWetness * 0.24 + max(projectedWetEnergy, hardSourceEnergy) * 0.34 + wetPolish * 0.12));
    float pseudoHardMask = saturate(hardAetherProjectionReceiver * pseudoHardSample.a * (0.26 + aetherNeonSource * 0.28 + specularGlintSource * 0.18 + qualifiedHardEnergy * 0.24 + max(metalPolish, aetherPolish) * 0.10));
    float3 pseudoWaterReflection = (lerp(color, pseudoWaterSample.rgb * lerp(float3(0.78, 0.98, 1.0), waterDepthTint + 0.18, 0.20), 0.68) - color) * pseudoWaterMask * Dalashade_WaterReflectionStrength * (0.245 + waterFresnel * 0.025) * lerp(1.0, 1.18, standaloneReflection);
    float3 pseudoWetReflection = (lerp(color, lerp(pseudoWetSample.rgb, hardSourceColor, 0.24), 0.36) - color) * pseudoWetMask * Dalashade_WetReflectionStrength * (0.125 + wetFresnel * 0.020) * lerp(1.0, 1.14, standaloneReflection);
    float3 pseudoHardReflection = (lerp(color, lerp(pseudoHardSample.rgb, hardSourceColor, 0.38), 0.32) - color) * pseudoHardMask * max(max(Dalashade_SpecularReflectionStrength, Dalashade_AetherReflectionStrength), Dalashade_NeonReflectionStrength) * (0.115 + hardFresnel * 0.018) * lerp(1.0, 1.12, standaloneReflection);
    float pseudoSSRMask = saturate(pseudoWaterMask * 0.70 + pseudoWetMask * 0.55 + pseudoHardMask * 0.62 + waterStructureMask * 0.66);
    float3 pseudoSSRContribution = (pseudoWaterReflection + pseudoWetReflection + pseudoHardReflection + waterStructureReflection)
        * (0.72 + pseudoSourceBias * 0.28)
        * (1.0 - highlightSafety * 0.48)
        * (1.0 - normalEdgeLeakRisk * 0.34)
        * receiverSafety;
    float3 screenReflectionContribution = (waterMirrorReflection * 1.35 + waterProjectedReflection * 1.05 + waterPlanarTraceReflection * 0.50 + wetProjectedReflection + hardProjectedReflection + pseudoSSRContribution * 0.62 + waterStructureSilhouette * 0.62 + waterStructureRim) * (1.0 - highlightSafety * 0.42);

    float nightBoost = 1.0 + scene.Night * (0.18 + scene.ArtificialLight * 0.30) + scene.DayReflection * waterReceiver * 0.12 + scene.CinematicPermission * 0.10;
    float3 contribution = (waterSheen + waterReflection + specularGlint + specularReflection + wetReflection + aetherNeonReflection + iceSheen) * Dalashade_SurfaceReflectionStrength * nightBoost;
    contribution += screenReflectionContribution * Dalashade_SurfaceReflectionStrength * nightBoost;
    contribution *= 1.0 - highlightSafety * 0.56;

    float waterSheenAllowance = frame.WaterReceiver * (0.110 + scene.Wetness * 0.035 + waterSurfaceQuality * 0.020);
    float glintAllowance = frame.WaterSpecularGlint * (0.070 + metalPolish * 0.018);
    float wetAllowance = scene.Wetness * wetHardSurfaceReceiver * (0.078 + wetPolish * 0.014);
    float aetherNeonAllowance = saturate(frame.MaterialCrystalAether + frame.MaterialNeonGlass + frame.MaterialFireLavaHeat) * (0.060 + scene.Night * 0.035 + scene.CinematicPermission * 0.020 + aetherPolish * 0.014);
    float projectionAllowance = saturate(waterProjectionStrength * 0.16 + waterMirrorMask * 0.220 + waterPlanarTraceMask * 0.042 + wetProjectionStrength * 0.15 + hardProjectionStrength * 0.13 + pseudoSSRMask * 0.026 + waterStructureMask * 0.014 + waterStructureVisibility * 0.052);
    float reflectionPositiveAllowance = 0.050 + waterSheenAllowance + glintAllowance + wetAllowance + aetherNeonAllowance + projectionAllowance + frame.MaterialSnowIce * 0.030;
    reflectionPositiveAllowance *= 1.0 - saturate(highlightSafety * 0.38 + scene.CombatPressure * 0.22 + scene.Readability * 0.10);
    reflectionPositiveAllowance *= lerp(1.0, 1.10, standaloneReflection);
    float positiveLimit = reflectionPositiveAllowance;
    float negativeLimit = 0.010 + waterSurfaceReceiver * 0.030 + wetMetalGlassReceiver * 0.012 + projectionAllowance * 0.48 + waterMirrorMask * 0.140 + waterStructureMask * 0.062 + waterStructureVisibility * 0.070;
    float3 result = saturate(color + clamp(contribution, float3(-negativeLimit, -negativeLimit, -negativeLimit), positiveLimit.xxx));
    float waterMirrorShape = saturate(abs(Dalashade_SurfaceReflectionLuma(waterMirrorColor) - luma) * 2.20 + Dalashade_GetSaturation(abs(waterMirrorColor - color)) * 0.52);
    float waterMirrorDisplayStrength = saturate(
        min(Dalashade_WaterReflectionStrength, Dalashade_SurfaceReflectionStrength)
        * (0.52 + Dalashade_WaterReflectionStrength * 0.34 + Dalashade_SurfaceReflectionStrength * 0.28 + standaloneReflection * 0.16));
    float waterMirrorCompositeMask = saturate(
        waterMirrorVisibility
        * waterSurfaceReceiver
        * waterMirrorDisplayStrength
        * (0.38 + waterMirrorShape * 0.50 + waterMirrorStructureBias * 0.28 + waterSurfaceQuality * 0.16 + waterFresnel * 0.08)
        * (1.0 - highlightSafety * 0.26)
        * (1.0 - frame.WaterFoamOrEdge * 0.12));
    float mirrorSourceLuma = Dalashade_SurfaceReflectionLuma(waterMirrorProjection.rgb);
    float mirrorDarkShape = saturate((luma - mirrorSourceLuma) * 2.35 + waterMirrorShape * 0.34 + waterMirrorStructureBias * 0.20);
    float mirrorWarmColor = saturate(waterMirrorProjection.r * 0.95 + waterMirrorProjection.g * 0.36 - waterMirrorProjection.b * 0.52);
    float mirrorCoolColor = saturate(waterMirrorProjection.b * 0.76 + waterMirrorProjection.g * 0.48 - waterMirrorProjection.r * 0.42);
    float3 liftedWarmReflection = max(waterMirrorProjection.rgb, float3(0.34, 0.18, 0.075) * (0.35 + mirrorWarmColor * 0.65));
    float3 liftedCoolReflection = max(waterMirrorProjection.rgb, float3(0.10, 0.30, 0.46) * (0.26 + mirrorCoolColor * 0.50));
    float3 waterMirrorInkColor = lerp(liftedCoolReflection, liftedWarmReflection, saturate(mirrorWarmColor + waterMirrorStructureBias * 0.18));
    waterMirrorInkColor = lerp(waterMirrorInkColor, waterMirrorProjection.rgb, saturate(0.22 + mirrorSourceLuma * 0.68));
    float waterMirrorInkOpacity = saturate(
        waterMirrorCompositeMask
        * (0.74 + waterMirrorShape * 0.52 + mirrorWarmColor * 0.28 + waterMirrorStructureBias * 0.22)
        * (1.0 - smoothstep(0.82, 1.0, luma) * 0.24));
    float3 waterMirrorDarkened = result * (1.0 - mirrorDarkShape * waterMirrorCompositeMask * 0.56);
    float3 waterMirrorColored = lerp(waterMirrorDarkened, waterMirrorInkColor, waterMirrorInkOpacity);
    result = lerp(result, waterMirrorColored, saturate(waterMirrorCompositeMask * (0.82 + waterMirrorShape * 0.30)));
    float projectedReflectionMask = saturate(waterProjectionStrength * 3.5 + waterMirrorMask * 7.5 + waterPlanarTraceMask * 2.8 + wetProjectionStrength * 6.0 + hardProjectionStrength * 7.0 + pseudoSSRMask * 0.56 + waterStructureMask * 0.56);
    float qualifiedSourceMask = saturate(qualifiedWaterEnergy * waterSurfaceReceiver * 0.42 + qualifiedHardEnergy * wetMetalGlassReceiver * 0.54 + projectedReflectionMask * 0.24);
    float reflectionSourceMask = saturate(waterSource * 0.48 + specularGlintSource * 0.70 + aetherNeonSource + fireLampSource * 0.72 + skyColorSource * waterSurfaceReceiver * 0.38 + hardSourceEnergy * wetMetalGlassReceiver * 0.34 + qualifiedSourceMask);
    float reflectionReceiverCombined = saturate(waterSurfaceReceiver + wetHardSurfaceReceiver + metalReceiver + aetherGlassReceiver + iceReceiver + wetMetalGlassReceiver + wetFloorProjectionReceiver + hardAetherProjectionReceiver + normalReflectionSupport * 0.35);
    float reflectionReceiverRejected = saturate(skyReject + skinProtect + warmDryReject * (1.0 - waterSurfaceReceiver) + frame.MaterialSandDust * bright * 0.45);
    float reflectionReceiverMask = reflectionReceiverCombined * (1.0 - reflectionReceiverRejected * 0.78);
    float finalMask = saturate(Dalashade_SurfaceReflectionLuma(abs(result - color)) * 18.0 + waterReflectionMask * 0.42 + waterMirrorMask * 0.38 + waterMirrorCompositeMask * 0.52 + waterStructureMask * 0.26 + specularGlintSource * 0.34 + wetHardSurfaceReceiver * 0.34 + emissiveReflectionMask * 0.42 + iceReceiver * 0.24 + screenReflectionReceiver * 0.22 + projectedReflectionMask * 0.30 + pseudoSSRMask * 0.22);

    int mode = Dalashade_SurfaceReflectionDebugMode;
    if (mode > 0)
    {
        float3 debugColor = float3(0.0, 0.0, 0.0);
        float debugMask = finalMask;
        if (mode == 1)
        {
            debugColor = float3(0.0, waterReceiver * 0.85, waterReceiver);
            debugMask = waterReceiver;
        }
        else if (mode == 2)
        {
            debugColor = float3(specularGlintSource * 0.65, specularGlintSource * 0.82, specularGlintSource);
            debugMask = specularGlintSource;
        }
        else if (mode == 3)
        {
            debugColor = float3(wetHardSurfaceReceiver * 0.25, wetHardSurfaceReceiver * 0.55, wetHardSurfaceReceiver);
            debugMask = wetHardSurfaceReceiver;
        }
        else if (mode == 4)
        {
            debugColor = saturate(aetherNeonSource * float3(0.18, 0.45, 1.0) + neonMask * float3(0.72, 0.12, 1.0) + fireLampSource * float3(1.0, 0.34, 0.04));
            debugMask = saturate(aetherNeonSource + fireLampSource);
        }
        else if (mode == 5)
        {
            debugColor = float3(0.08 * skyReject, 0.34 * skyReject, skyReject);
            debugMask = skyReject;
        }
        else if (mode == 6)
        {
            debugColor = float3(skinProtect, skinProtect * 0.58, skinProtect * 0.42);
            debugMask = skinProtect;
        }
        else if (mode == 7 || mode == 8)
        {
            debugColor = abs(contribution) * 12.0 + float3(waterReflectionMask * 0.12, specularGlintSource * 0.20, emissiveReflectionMask * 0.24);
            debugMask = finalMask;
        }
        else if (mode == 9)
        {
            debugColor = saturate(frame.WaterSource * float3(0.00, 0.75, 1.00) + frame.WaterSkySource * float3(0.10, 0.28, 0.90) + qualifiedWaterEnergy * waterSurfaceReceiver * float3(0.35, 1.0, 1.0) + qualifiedHardEnergy * wetMetalGlassReceiver * float3(0.20, 0.95, 0.35) + specularGlintSource * float3(0.55, 0.78, 1.0) + aetherNeonSource * float3(0.48, 0.20, 1.00) + fireLampSource * float3(1.0, 0.34, 0.04));
            debugMask = reflectionSourceMask;
        }
        else if (mode == 10)
        {
            debugColor = saturate(waterSurfaceReceiver * float3(0.0, 0.78, 1.0) + frame.WaterHorizonOnly * float3(0.06, 0.16, 0.88) + wetMetalGlassReceiver * float3(0.18, 0.80, 0.32) + wetHardSurfaceReceiver * float3(0.28, 0.52, 0.78) + metalReceiver * float3(0.35, 0.55, 0.78) + aetherGlassReceiver * float3(0.74, 0.28, 1.0) + iceReceiver * float3(0.70, 0.88, 1.0) + normalReflectionSupport * float3(0.0, 0.95, 0.72) + normalEdgeLeakRisk * float3(0.95, 0.16, 0.02) + reflectionReceiverRejected * float3(0.55, 0.04, 0.02));
            debugMask = reflectionReceiverMask;
        }
        else if (mode == 11)
        {
            float waterProjectedDebug = saturate(waterProjectionStrength * 10.0);
            float waterMirrorDebug = sqrt(saturate(waterMirrorMask * 8.0));
            float waterPlanarDebug = sqrt(saturate(waterPlanarTraceMask * 7.5));
            float waterStructureDebug = sqrt(saturate(waterStructureMask * 7.0));
            debugColor = saturate(
                waterMirrorDebug * (float3(0.0, 0.70, 1.0) + abs(waterMirrorReflection) * 42.0)
                + waterProjectedDebug * (float3(0.0, 0.52, 0.86) + abs(waterProjectedReflection) * 20.0)
                + waterPlanarDebug * (float3(0.04, 0.48, 1.0) + abs(waterPlanarTraceReflection) * 36.0)
                + waterStructureDebug * (float3(0.06, 0.22, 1.0) + abs(waterStructureReflection + waterStructureSilhouette + waterStructureRim) * 34.0));
            debugMask = saturate(max(max(max(waterMirrorDebug, waterProjectedDebug), waterPlanarDebug), waterStructureDebug));
        }
        else if (mode == 12)
        {
            float wetProjectedDebug = saturate(wetProjectionStrength * 12.0);
            debugColor = wetProjectedDebug * (float3(0.18, 0.82, 0.36) + abs(wetProjectedReflection) * 26.0);
            debugMask = wetProjectedDebug;
        }
        else if (mode == 13)
        {
            float hardProjectedDebug = saturate(hardProjectionStrength * 13.0);
            debugColor = hardProjectedDebug * (float3(0.78, 0.28, 1.0) + abs(hardProjectedReflection) * 28.0);
            debugMask = hardProjectedDebug;
        }
        else if (mode == 14)
        {
            float mirrorWaterView = sqrt(saturate(waterMirrorMask * 6.5));
            float planarWaterView = sqrt(saturate(waterPlanarTraceMask * 6.0));
            float structureWaterView = sqrt(saturate(waterStructureMask * 6.0));
            float pseudoWaterView = sqrt(saturate(max(pseudoWaterMask * 5.0, waterSurfaceReceiver * pseudoWaterSample.a * 3.0)));
            float pseudoWetView = sqrt(saturate(max(pseudoWetMask * 6.0, wetFloorProjectionReceiver * pseudoWetSample.a * 3.5)));
            float pseudoHardView = sqrt(saturate(max(pseudoHardMask * 6.5, hardAetherProjectionReceiver * pseudoHardSample.a * 3.8)));
            debugColor = saturate(
                mirrorWaterView * float3(0.0, 0.72, 1.0)
                + pseudoWaterView * float3(0.0, 0.52, 0.86)
                + planarWaterView * float3(0.02, 0.48, 1.0)
                + structureWaterView * float3(0.05, 0.24, 0.95)
                + pseudoWetView * float3(0.18, 0.86, 0.42)
                + pseudoHardView * float3(0.82, 0.30, 1.0)
                + normalEdgeLeakRisk * float3(0.95, 0.16, 0.02)
                + abs(pseudoSSRContribution) * 34.0);
            debugMask = saturate(max(max(max(max(max(mirrorWaterView, pseudoWaterView), planarWaterView), structureWaterView), pseudoWetView), pseudoHardView));
        }

        return float4(Dalashade_RenderSurfaceReflectionDebug(texcoord, color, result, debugColor, debugMask, contribution), 1.0);
    }

    return float4(result, 1.0);
}

technique Dalashade_SurfaceReflection
{
    pass SurfaceReflection
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_SurfaceReflectionPS;
    }
}
