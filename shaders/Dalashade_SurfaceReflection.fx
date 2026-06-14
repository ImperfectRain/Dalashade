#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"

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
    float dx = ReShade::GetLinearizedDepth(uv + float2(texel.x, 0.0)) - ReShade::GetLinearizedDepth(uv - float2(texel.x, 0.0));
    float dy = ReShade::GetLinearizedDepth(uv + float2(0.0, texel.y)) - ReShade::GetLinearizedDepth(uv - float2(0.0, texel.y));
    return normalize(float3(-dx * 22.0, -dy * 22.0, 1.0 + depth * 0.08));
}

float3 Dalashade_SurfaceReflectionDirectionalSheen(float2 uv, float radius, float depth)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float2 vertical = float2(0.0, texel.y * 1.35);
    float2 horizontal = float2(texel.x * 1.85, 0.0);
    float3 c = tex2D(ReShade::BackBuffer, uv - vertical).rgb * 0.28;
    c += tex2D(ReShade::BackBuffer, uv + vertical).rgb * 0.18;
    c += tex2D(ReShade::BackBuffer, uv + horizontal).rgb * 0.18;
    c += tex2D(ReShade::BackBuffer, uv - horizontal).rgb * 0.18;
    c += tex2D(ReShade::BackBuffer, uv - vertical * 2.0).rgb * 0.10;
    c += tex2D(ReShade::BackBuffer, uv + horizontal * 2.0).rgb * 0.08;

    float sampleDepth = ReShade::GetLinearizedDepth(uv - vertical);
    float continuity = saturate(1.0 - abs(depth - sampleDepth) * 10.0);
    return saturate(c * (0.62 + continuity * 0.38));
}

float Dalashade_SurfaceReflectionDepthGate(float2 sampleUv, float depth, float depthReject)
{
    float sampleDepth = ReShade::GetLinearizedDepth(saturate(sampleUv));
    float reject = lerp(3.5, 12.0, saturate(depthReject));
    return saturate(1.0 - abs(depth - sampleDepth) * reject);
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

float3 Dalashade_SurfaceReflectionLocalSource(float2 uv, float depth, float radius)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float3 source = float3(0.0, 0.0, 0.0);
    float weight = 0.0;

    float2 offsetA = float2(texel.x, texel.y);
    float2 offsetB = float2(-texel.x, texel.y);
    float2 offsetC = float2(texel.x * 1.65, -texel.y * 0.85);
    float2 offsetD = float2(-texel.x * 1.65, -texel.y * 0.85);

    float3 cA = tex2D(ReShade::BackBuffer, uv + offsetA).rgb;
    float3 cB = tex2D(ReShade::BackBuffer, uv + offsetB).rgb;
    float3 cC = tex2D(ReShade::BackBuffer, uv + offsetC).rgb;
    float3 cD = tex2D(ReShade::BackBuffer, uv + offsetD).rgb;

    float dA = ReShade::GetLinearizedDepth(uv + offsetA);
    float dB = ReShade::GetLinearizedDepth(uv + offsetB);
    float dC = ReShade::GetLinearizedDepth(uv + offsetC);
    float dD = ReShade::GetLinearizedDepth(uv + offsetD);

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

    Dalashade_MaterialResolve material = Dalashade_ResolveMaterials(
        color,
        texcoord,
        Dalashade_MaterialFoliage,
        Dalashade_MaterialWaterSpecular,
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
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        Dalashade_DepthAssistConfidenceFloor);

    float skyReject = saturate(material.SkyCloudFog * Dalashade_SurfaceReflectionSkyReject);
    float skinProtect = saturate(material.SkinProtection * Dalashade_SurfaceReflectionSkinProtect);
    float gameplayDampen = 1.0 - saturate(Dalashade_CombatPressure * 0.42 + Dalashade_Readability * 0.18);
    float highlightSafety = saturate(Dalashade_HighlightProtection * bright + skinProtect + skyReject * 0.85 + material.SandDust * bright * 0.52 + material.SnowIce * bright * 0.34);
    float surfaceFacing = saturate(0.42 + max(normal.z, 0.0) * 0.46 + smoothness * 0.18);
    float safeSurface = (1.0 - skyReject) * (1.0 - skinProtect * 0.92) * gameplayDampen;

    float reflectionOffset = Dalashade_ReflectionSampleOffset * (0.80 + Dalashade_WaterSheenRadius * 0.18 + Dalashade_Night * 0.10);
    float reflectionSoftness = saturate(Dalashade_ReflectionSoftness);
    float reflectionDepthReject = saturate(Dalashade_ReflectionDepthReject);
    float2 verticalReflectDirection = normalize(float2(normal.x * 0.12, -1.0));
    float2 glintStreakDirection = normalize(float2(0.88 + normal.x * 0.18, -0.32 - normal.y * 0.12));
    float3 reflectedVertical = Dalashade_SampleReflectionColor(texcoord, depth, verticalReflectDirection, reflectionOffset, reflectionSoftness, reflectionDepthReject);
    float3 reflectedSoft = Dalashade_SampleReflectionColor(texcoord, depth, verticalReflectDirection, reflectionOffset * 1.85, saturate(reflectionSoftness + 0.22), reflectionDepthReject);
    float3 reflectedStreak = Dalashade_SampleReflectionStreak(texcoord, depth, glintStreakDirection, reflectionOffset * 1.45, reflectionDepthReject);
    float verticalDepthContinuity = Dalashade_SurfaceReflectionDepthGate(texcoord + verticalReflectDirection * reflectionOffset, depth, reflectionDepthReject);
    float streakDepthContinuity = Dalashade_SurfaceReflectionDepthGate(texcoord + glintStreakDirection * reflectionOffset * 1.45, depth, reflectionDepthReject);
    Dalashade_WaterResolve water = Dalashade_ResolveWater(
        color,
        texcoord,
        Dalashade_WaterContext,
        Dalashade_CoastalContext,
        Dalashade_OpenOceanContext,
        Dalashade_ShallowWaterContext,
        Dalashade_WetSurfaceContext,
        Dalashade_MaterialWaterPlane,
        Dalashade_MaterialSpecularGlint,
        Dalashade_MaterialSandDust,
        Dalashade_MaterialSkyCloudFog,
        Dalashade_MaterialSkinProtection,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        Dalashade_DepthAssistConfidenceFloor);
    Dalashade_SafetyResolve safety = Dalashade_ResolveSafety(
        color,
        texcoord,
        material,
        water,
        Dalashade_HighlightProtection,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        Dalashade_DepthAssistConfidenceFloor);
    skyReject = saturate(max(skyReject, safety.SkyReject * Dalashade_SurfaceReflectionSkyReject));
    skinProtect = saturate(max(skinProtect, safety.SkinReject * Dalashade_SurfaceReflectionSkinProtect));
    safeSurface = (1.0 - skyReject) * (1.0 - skinProtect * 0.92) * gameplayDampen;
    highlightSafety = saturate(max(max(highlightSafety, Dalashade_DayHighlightPressure), safety.HighlightProtect));

    float warmDryReject = saturate(water.SandReject + material.SandDust * (0.22 + bright * 0.42));
    float structureHardReceiver = saturate(
        material.StructureReceiverConfidence * 0.34
        + material.SurfaceHardness * 0.26
        + material.StoneRuins * 0.24
        + material.MetalIndustrial * 0.24);
    float hardSurfaceHint = saturate(
        structureHardReceiver * 0.42
        + material.MetalIndustrial * 0.28
        + material.SpecularGlint * 0.16
        + midtone * smoothness * 0.20);
    float hardReflectionFamily = saturate(
        material.MetalIndustrial * 0.26
        + material.StoneRuins * 0.20
        + material.NeonGlass * 0.18
        + material.CrystalAether * 0.14
        + material.SnowIce * 0.08
        + material.SpecularGlint * 0.16
        + structureHardReceiver * 0.18);
    float waterSpecificReceiver = saturate(water.WaterReceiver);
    float sharedReflectionReceiver = saturate(material.ReflectionReceiverConfidence);
    float nonSkyGate = saturate(1.0 - safety.SkyReject * 0.95);
    float nonSkinGate = saturate(1.0 - safety.SkinReject * 0.90);
    float foliageGate = saturate(1.0 - safety.FoliageNoiseReject * 0.45 - material.Foliage * 0.25);
    float receiverSafety = nonSkyGate * nonSkinGate * foliageGate * gameplayDampen;
    float sharedReceiverSupport = saturate(
        sharedReflectionReceiver
        * (1.0 - safety.SkyReject * 0.88)
        * (1.0 - safety.SkinReject * 0.88)
        * (1.0 - safety.FoliageNoiseReject * 0.35)
        * (1.0 - safety.HighlightProtect * 0.18)
        * safeSurface);

    float3 sheenSample = Dalashade_SurfaceReflectionDirectionalSheen(texcoord, Dalashade_WaterSheenRadius, depth);
    float3 cyanSheen = lerp(float3(0.04, 0.20, 0.24), float3(0.12, 0.58, 0.68), saturate(saturation + water.ShallowWater + material.WaterPlane));
    float waterReceiver = water.WaterReceiver
        * surfaceFacing
        * verticalDepthContinuity
        * safeSurface;
    waterReceiver = saturate(waterReceiver + sharedReceiverSupport * waterSpecificReceiver * surfaceFacing * verticalDepthContinuity * 0.12);
    float3 waterSheen = lerp(cyanSheen, sheenSample, 0.46) * waterReceiver * Dalashade_WaterSheenStrength * 1.62;
    float reflectedWaterLuma = Dalashade_SurfaceReflectionLuma(reflectedVertical);
    float skyColorSource = saturate(max(water.SkySource, smoothstep(0.16, 0.88, reflectedWaterLuma + Dalashade_GetSaturation(reflectedVertical) * 0.36) * (0.28 + water.WaterHorizon * 0.30 + Dalashade_OpenSkyLight * 0.18)));
    float waterSource = saturate(max(water.WaterSource, water.WetShoreline * 0.35) + skyColorSource * water.WaterReceiver * 0.32);
    float waterReflectionMask = waterReceiver * saturate(waterSource + skyColorSource * (0.25 + Dalashade_DayReflection * 0.18)) * (1.0 - bright * 0.26);
    float3 waterReflection = lerp(reflectedVertical, reflectedSoft, reflectionSoftness * 0.42) * lerp(float3(0.68, 0.98, 1.0), cyanSheen + 0.18, 0.34) * waterReflectionMask * Dalashade_WaterReflectionStrength;
    float shorelineSheenMask = saturate((water.WetShoreline * 0.72 + water.FoamOrEdge * 0.42) * safeSurface * (1.0 - skinProtect * 0.92));
    waterSheen += lerp(float3(0.26, 0.72, 0.86), sheenSample, 0.30) * shorelineSheenMask * Dalashade_WaterSheenStrength * 0.72;

    float glintShape = smoothstep(0.44, 0.98, luma) * smoothstep(0.025, 0.32, edge) * (1.0 - smoothness * 0.48);
    float specularGlintSource = saturate(max(material.SpecularGlint * glintShape, water.FoamOrEdge * 0.38) * streakDepthContinuity * (1.0 - waterReceiver * 0.16) * (1.0 - skinProtect * 0.80) * (1.0 - skyReject * 0.78) * (1.0 - warmDryReject * 0.28));
    float metalReceiver = material.MetalIndustrial
        * hardSurfaceHint
        * (0.22 + sharedReflectionReceiver * 0.44 + smoothstep(0.02, 0.34, edge) * 0.26 + specularGlintSource * 0.32)
        * safeSurface
        * (1.0 - waterReceiver * 0.85)
        * (1.0 - skyReject * 0.96);
    float3 specularGlint = lerp(float3(0.65, 0.86, 1.0), max(color + 0.26, reflectedStreak), 0.42) * specularGlintSource * Dalashade_SpecularGlintStrength * 1.54;
    float3 specularReflection = reflectedStreak * specularGlintSource * max(metalReceiver, waterReceiver * 0.35) * Dalashade_SpecularReflectionStrength * 0.86;

    float receiverWetness = max(Dalashade_Wetness, Dalashade_WetSurfaceContext);
    float wetHardSurfaceReceiver = receiverWetness
        * saturate(hardSurfaceHint + material.StoneRuins * 0.20 + material.StructureReceiverConfidence * 0.18)
        * smoothstep(0.36, 0.94, smoothness)
        * smoothstep(0.0, 0.62, 1.0 - edge)
        * (0.58 + shadow * 0.54)
        * safeSurface
        * (1.0 - bright * 0.42)
        * (1.0 - material.SandDust * bright * 0.72)
        * (1.0 - material.SnowIce * bright * 0.56)
        * (1.0 - waterReceiver * 0.55);
    wetHardSurfaceReceiver = saturate(
        wetHardSurfaceReceiver
        + sharedReceiverSupport
            * hardSurfaceHint
            * smoothstep(0.30, 0.92, smoothness)
            * (0.36 + material.StoneRuins * 0.24 + structureHardReceiver * 0.20)
            * (1.0 - waterSpecificReceiver * 0.35)
            * 0.12);
    float3 wetReflection = lerp(sheenSample, reflectedVertical, 0.58) * wetHardSurfaceReceiver * (specularGlintSource * 0.55 + Dalashade_Wetness * 0.42) * Dalashade_WetReflectionStrength * 1.78;

    float3 localSource = Dalashade_SurfaceReflectionLocalSource(texcoord, depth, max(Dalashade_WaterSheenRadius, 0.75) * (1.25 + Dalashade_Night * 0.45));
    float localSourceEnergy = smoothstep(0.18, 0.96, Dalashade_SurfaceReflectionLuma(localSource) + Dalashade_GetSaturation(localSource) * 0.62);
    float aetherMask = saturate(material.CrystalAether * (0.54 + Dalashade_MagicGlow * 0.56) * smoothstep(0.12, 0.82, luma + saturation * 0.58));
    float neonMask = saturate(material.NeonGlass * (0.54 + Dalashade_NeonGlow * 0.56) * smoothstep(0.16, 0.86, luma + saturation * 0.62));
    float fireMask = saturate(material.FireLavaHeat * smoothstep(0.20, 0.88, luma + saturation * 0.42));
    float aetherNeonSource = saturate(aetherMask + neonMask + localSourceEnergy * (material.CrystalAether + material.NeonGlass) * 0.42);
    float fireLampSource = saturate(fireMask + localSourceEnergy * material.FireLavaHeat * 0.38 + Dalashade_ArtificialLight * specularGlintSource * 0.22);
    float aetherGlassReceiver = saturate((material.CrystalAether + material.NeonGlass) * (smoothness * 0.42 + hardSurfaceHint * 0.36 + metalReceiver * 0.28 + sharedReflectionReceiver * 0.18) * safeSurface * (1.0 - skyReject * 0.96) * (1.0 - waterReceiver * 0.42));
    float emissiveReflectionMask = saturate((aetherNeonSource + fireLampSource * 0.72) * (aetherGlassReceiver + metalReceiver * 0.46 + wetHardSurfaceReceiver * 0.32 + waterReceiver * 0.18));
    float3 aetherNeonColor = material.CrystalAether * float3(0.16, 0.44, 1.0) * Dalashade_AetherReflectionStrength;
    aetherNeonColor += material.NeonGlass * float3(0.48, 0.22, 1.0) * Dalashade_NeonReflectionStrength;
    aetherNeonColor += material.FireLavaHeat * float3(1.0, 0.30, 0.06) * 0.42;
    float3 aetherStreak = lerp(localSource, reflectedStreak, 0.55);
    float3 aetherNeonReflection = saturate(aetherNeonColor + aetherStreak * 0.58 + reflectedSoft * 0.12) * emissiveReflectionMask * 1.82;

    float iceReceiver = material.SnowIce * smoothstep(0.44, 0.96, smoothness) * midtone * safeSurface * (1.0 - bright * 0.70) * (1.0 - skyReject * 0.95);
    float3 iceSheen = float3(0.46, 0.72, 1.0) * iceReceiver * (skyColorSource * 0.35 + specularGlintSource * 0.24 + 0.18) * Dalashade_IceSheenStrength * 1.38;

    float waterSurfaceReceiver = saturate(
        waterReceiver
        * receiverSafety
        * (1.0 - water.HorizonOnlyConfidence * 0.85));
    float wetMetalGlassReceiver = saturate(
        sharedReflectionReceiver
        * receiverSafety
        * (1.0 - waterSpecificReceiver * 0.25)
        * (hardReflectionFamily * 0.58
            + smoothness * hardSurfaceHint * 0.18
            + receiverWetness * structureHardReceiver * 0.16));
    float screenReflectionReceiver = saturate(waterSurfaceReceiver + wetMetalGlassReceiver * 0.65);
    float waterSourceColorSupport = saturate(0.24 + water.SkySource * 0.30 + water.WaterSource * 0.24 + water.WaterPixelConfidence * 0.28);
    float waterQualifiedSourceBias = saturate(water.SkySource * 0.36 + water.WaterSource * 0.30 + water.HorizonOnlyConfidence * 0.22 + water.WaterPixelConfidence * 0.22);
    float hardSourceEnergy = saturate(localSourceEnergy * 0.46 + specularGlintSource * 0.34 + aetherNeonSource * 0.30 + fireLampSource * 0.22);
    float hardQualifiedSourceBias = saturate(sharedReflectionReceiver * 0.34 + material.SpecularGlint * 0.24 + material.MetalIndustrial * 0.18 + receiverWetness * 0.16);
    float aetherQualifiedSourceBias = saturate(aetherNeonSource + material.CrystalAether * 0.22 + material.NeonGlass * 0.22);
    float fireQualifiedSourceBias = saturate(fireLampSource + material.FireLavaHeat * 0.24);
    float3 qualifiedWaterSourceColor = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, verticalReflectDirection, reflectionOffset * 1.12, reflectionSoftness, reflectionDepthReject, waterQualifiedSourceBias, 0.10, 0.08, 0.04);
    float3 qualifiedHardSourceColor = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, glintStreakDirection, reflectionOffset * 1.28, saturate(reflectionSoftness * 0.72), reflectionDepthReject, 0.08, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias);
    float qualifiedWaterEnergy = Dalashade_SurfaceReflectionSourceEnergy(qualifiedWaterSourceColor, waterQualifiedSourceBias, 0.10, 0.08, 0.04);
    float qualifiedHardEnergy = Dalashade_SurfaceReflectionSourceEnergy(qualifiedHardSourceColor, 0.08, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias);
    float2 wetFloorDirection = normalize(float2(normal.x * 0.18, -0.62 - normal.y * 0.08));
    float2 hardProjectionDirection = normalize(float2(0.92 + normal.x * 0.22, -0.22 - normal.y * 0.18));
    float3 projectedWaterNear = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, verticalReflectDirection, reflectionOffset * 1.20, reflectionSoftness, reflectionDepthReject, waterQualifiedSourceBias, 0.08, 0.06, 0.03);
    float3 projectedWaterFar = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, verticalReflectDirection, reflectionOffset * 2.35, saturate(reflectionSoftness + 0.18), reflectionDepthReject, waterQualifiedSourceBias, 0.06, 0.06, 0.03);
    float3 projectedWetFloorSource = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, wetFloorDirection, reflectionOffset * 0.78, saturate(reflectionSoftness * 0.78), reflectionDepthReject, 0.06, hardQualifiedSourceBias, aetherQualifiedSourceBias * 0.55, fireQualifiedSourceBias * 0.55);
    float3 projectedHardAetherSource = Dalashade_SampleQualifiedReflectionSource(texcoord, depth, hardProjectionDirection, reflectionOffset * 0.95, saturate(reflectionSoftness * 0.48), reflectionDepthReject, 0.04, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias);
    float projectedWaterEnergy = Dalashade_SurfaceReflectionSourceEnergy(lerp(projectedWaterNear, projectedWaterFar, 0.42), waterQualifiedSourceBias, 0.08, 0.06, 0.03);
    float projectedWetEnergy = Dalashade_SurfaceReflectionSourceEnergy(projectedWetFloorSource, 0.06, hardQualifiedSourceBias, aetherQualifiedSourceBias * 0.55, fireQualifiedSourceBias * 0.55);
    float projectedHardEnergy = Dalashade_SurfaceReflectionSourceEnergy(projectedHardAetherSource, 0.04, hardQualifiedSourceBias, aetherQualifiedSourceBias, fireQualifiedSourceBias);
    float3 hardSourceColor = lerp(qualifiedHardSourceColor, reflectedStreak, saturate(specularGlintSource * 0.24));
    hardSourceColor = lerp(hardSourceColor, max(localSource, hardSourceColor), hardSourceEnergy * 0.48);
    float wetFloorProjectionReceiver = saturate((wetHardSurfaceReceiver + wetMetalGlassReceiver * receiverWetness * 0.42) * (1.0 - waterSurfaceReceiver * 0.45));
    float hardAetherProjectionReceiver = saturate((metalReceiver + aetherGlassReceiver + wetMetalGlassReceiver * (material.MetalIndustrial + material.CrystalAether + material.NeonGlass) * 0.34) * (1.0 - waterSurfaceReceiver * 0.35));
    float3 projectedWaterColor = lerp(projectedWaterNear, projectedWaterFar, 0.42) * lerp(float3(0.72, 0.98, 1.0), cyanSheen + 0.22, 0.28);
    float3 projectedWetFloorColor = lerp(projectedWetFloorSource, hardSourceColor, 0.28);
    float3 projectedHardAetherColor = lerp(projectedHardAetherSource, hardSourceColor, saturate(specularGlintSource * 0.22 + aetherNeonSource * 0.24));
    float waterProjectionAgreement = saturate(waterSurfaceReceiver * (0.24 + waterSourceColorSupport * 0.48 + projectedWaterEnergy * 0.34));
    float wetProjectionAgreement = saturate(wetFloorProjectionReceiver * (0.20 + max(projectedWetEnergy, hardSourceEnergy) * 0.58 + receiverWetness * 0.18));
    float hardProjectionAgreement = saturate(hardAetherProjectionReceiver * (0.18 + max(projectedHardEnergy, qualifiedHardEnergy) * 0.62 + aetherNeonSource * 0.20 + specularGlintSource * 0.12));
    float waterProjectionStrength = waterProjectionAgreement * Dalashade_WaterReflectionStrength * 0.240;
    float wetProjectionStrength = wetProjectionAgreement * Dalashade_WetReflectionStrength * 0.130;
    float hardProjectionStrength = hardProjectionAgreement * max(max(Dalashade_SpecularReflectionStrength, Dalashade_AetherReflectionStrength), Dalashade_NeonReflectionStrength) * 0.105;
    float3 waterProjectedReflection = (lerp(color, projectedWaterColor, 0.62) - color) * waterProjectionStrength;
    float3 wetProjectedReflection = (lerp(color, projectedWetFloorColor, 0.42) - color) * wetProjectionStrength;
    float3 hardProjectedReflection = (lerp(color, projectedHardAetherColor, 0.38) - color) * hardProjectionStrength;
    float pseudoSourceBias = saturate(
        waterQualifiedSourceBias * waterSurfaceReceiver * 0.42
        + hardQualifiedSourceBias * wetMetalGlassReceiver * 0.34
        + aetherQualifiedSourceBias * hardAetherProjectionReceiver * 0.28
        + fireQualifiedSourceBias * hardAetherProjectionReceiver * 0.18);
    float waterStructureSourceBias = saturate(waterSurfaceReceiver * (0.34 + material.StructureReceiverConfidence * 0.18 + material.MetalIndustrial * 0.12 + water.WaterPixelConfidence * 0.18));
    float wetStructureSourceBias = saturate(wetFloorProjectionReceiver * (0.18 + material.StructureReceiverConfidence * 0.20 + material.MetalIndustrial * 0.14));
    float hardStructureSourceBias = saturate(hardAetherProjectionReceiver * (0.12 + material.StructureReceiverConfidence * 0.12 + material.MetalIndustrial * 0.18));
    float4 pseudoWaterSample = Dalashade_SamplePseudoSSR(texcoord, depth, verticalReflectDirection, reflectionOffset * 1.34, reflectionDepthReject, saturate(waterQualifiedSourceBias + water.WaterPixelConfidence * 0.28), waterStructureSourceBias);
    float4 pseudoWetSample = Dalashade_SamplePseudoSSR(texcoord, depth, wetFloorDirection, reflectionOffset * 0.92, reflectionDepthReject, saturate(hardQualifiedSourceBias + receiverWetness * 0.22), wetStructureSourceBias);
    float4 pseudoHardSample = Dalashade_SamplePseudoSSR(texcoord, depth, hardProjectionDirection, reflectionOffset * 1.10, reflectionDepthReject, saturate(aetherQualifiedSourceBias + hardQualifiedSourceBias * 0.42), hardStructureSourceBias);
    float pseudoWaterMask = saturate(waterSurfaceReceiver * pseudoWaterSample.a * (0.42 + waterSourceColorSupport * 0.34 + projectedWaterEnergy * 0.14 + waterStructureSourceBias * 0.28));
    float pseudoWetMask = saturate(wetFloorProjectionReceiver * pseudoWetSample.a * (0.28 + receiverWetness * 0.24 + max(projectedWetEnergy, hardSourceEnergy) * 0.34));
    float pseudoHardMask = saturate(hardAetherProjectionReceiver * pseudoHardSample.a * (0.26 + aetherNeonSource * 0.28 + specularGlintSource * 0.18 + qualifiedHardEnergy * 0.24));
    float3 pseudoWaterReflection = (lerp(color, pseudoWaterSample.rgb * lerp(float3(0.78, 0.98, 1.0), cyanSheen + 0.18, 0.18), 0.66) - color) * pseudoWaterMask * Dalashade_WaterReflectionStrength * 0.240;
    float3 pseudoWetReflection = (lerp(color, lerp(pseudoWetSample.rgb, hardSourceColor, 0.22), 0.34) - color) * pseudoWetMask * Dalashade_WetReflectionStrength * 0.120;
    float3 pseudoHardReflection = (lerp(color, lerp(pseudoHardSample.rgb, hardSourceColor, 0.36), 0.30) - color) * pseudoHardMask * max(max(Dalashade_SpecularReflectionStrength, Dalashade_AetherReflectionStrength), Dalashade_NeonReflectionStrength) * 0.110;
    float pseudoSSRMask = saturate(pseudoWaterMask * 0.70 + pseudoWetMask * 0.55 + pseudoHardMask * 0.62);
    float3 pseudoSSRContribution = (pseudoWaterReflection + pseudoWetReflection + pseudoHardReflection)
        * (0.72 + pseudoSourceBias * 0.28)
        * (1.0 - highlightSafety * 0.48)
        * receiverSafety;
    float3 screenReflectionContribution = (waterProjectedReflection + wetProjectedReflection + hardProjectedReflection + pseudoSSRContribution) * (1.0 - highlightSafety * 0.42);

    float nightBoost = 1.0 + Dalashade_Night * (0.18 + Dalashade_ArtificialLight * 0.30) + Dalashade_DayReflection * waterReceiver * 0.12 + Dalashade_CinematicPermission * 0.10;
    float3 contribution = (waterSheen + waterReflection + specularGlint + specularReflection + wetReflection + aetherNeonReflection + iceSheen) * Dalashade_SurfaceReflectionStrength * nightBoost;
    contribution += screenReflectionContribution * Dalashade_SurfaceReflectionStrength * nightBoost;
    contribution *= 1.0 - highlightSafety * 0.56;

    float waterSheenAllowance = water.WaterReceiver * (0.110 + Dalashade_Wetness * 0.035);
    float glintAllowance = material.SpecularGlint * 0.070;
    float wetAllowance = Dalashade_Wetness * wetHardSurfaceReceiver * 0.078;
    float aetherNeonAllowance = saturate(material.CrystalAether + material.NeonGlass + material.FireLavaHeat) * (0.060 + Dalashade_Night * 0.035 + Dalashade_CinematicPermission * 0.020);
    float projectionAllowance = saturate(waterProjectionStrength * 0.18 + wetProjectionStrength * 0.14 + hardProjectionStrength * 0.12 + pseudoSSRMask * 0.030);
    float reflectionPositiveAllowance = 0.050 + waterSheenAllowance + glintAllowance + wetAllowance + aetherNeonAllowance + projectionAllowance + material.SnowIce * 0.030;
    reflectionPositiveAllowance *= 1.0 - saturate(highlightSafety * 0.38 + Dalashade_CombatPressure * 0.22 + Dalashade_Readability * 0.10);
    float positiveLimit = reflectionPositiveAllowance;
    float negativeLimit = 0.010 + waterSurfaceReceiver * 0.030 + wetMetalGlassReceiver * 0.012 + projectionAllowance * 0.40;
    float3 result = saturate(color + clamp(contribution, float3(-negativeLimit, -negativeLimit, -negativeLimit), positiveLimit.xxx));
    float projectedReflectionMask = saturate(waterProjectionStrength * 4.2 + wetProjectionStrength * 6.0 + hardProjectionStrength * 7.0 + pseudoSSRMask * 0.72);
    float qualifiedSourceMask = saturate(qualifiedWaterEnergy * waterSurfaceReceiver * 0.42 + qualifiedHardEnergy * wetMetalGlassReceiver * 0.54 + projectedReflectionMask * 0.24);
    float reflectionSourceMask = saturate(waterSource * 0.48 + specularGlintSource * 0.70 + aetherNeonSource + fireLampSource * 0.72 + skyColorSource * waterSurfaceReceiver * 0.38 + hardSourceEnergy * wetMetalGlassReceiver * 0.34 + qualifiedSourceMask);
    float reflectionReceiverCombined = saturate(waterSurfaceReceiver + wetHardSurfaceReceiver + metalReceiver + aetherGlassReceiver + iceReceiver + wetMetalGlassReceiver + wetFloorProjectionReceiver + hardAetherProjectionReceiver);
    float reflectionReceiverRejected = saturate(skyReject + skinProtect + warmDryReject * (1.0 - waterSurfaceReceiver) + material.SandDust * bright * 0.45);
    float reflectionReceiverMask = reflectionReceiverCombined * (1.0 - reflectionReceiverRejected * 0.78);
    float finalMask = saturate(Dalashade_SurfaceReflectionLuma(abs(result - color)) * 18.0 + waterReflectionMask * 0.42 + specularGlintSource * 0.34 + wetHardSurfaceReceiver * 0.34 + emissiveReflectionMask * 0.42 + iceReceiver * 0.24 + screenReflectionReceiver * 0.22 + projectedReflectionMask * 0.30 + pseudoSSRMask * 0.26);

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
            debugColor = saturate(water.WaterSource * float3(0.00, 0.75, 1.00) + water.SkySource * float3(0.10, 0.28, 0.90) + qualifiedWaterEnergy * waterSurfaceReceiver * float3(0.35, 1.0, 1.0) + qualifiedHardEnergy * wetMetalGlassReceiver * float3(0.20, 0.95, 0.35) + specularGlintSource * float3(0.55, 0.78, 1.0) + aetherNeonSource * float3(0.48, 0.20, 1.00) + fireLampSource * float3(1.0, 0.34, 0.04));
            debugMask = reflectionSourceMask;
        }
        else if (mode == 10)
        {
            debugColor = saturate(waterSurfaceReceiver * float3(0.0, 0.78, 1.0) + water.HorizonOnlyConfidence * float3(0.06, 0.16, 0.88) + wetMetalGlassReceiver * float3(0.18, 0.80, 0.32) + wetHardSurfaceReceiver * float3(0.28, 0.52, 0.78) + metalReceiver * float3(0.35, 0.55, 0.78) + aetherGlassReceiver * float3(0.74, 0.28, 1.0) + iceReceiver * float3(0.70, 0.88, 1.0) + reflectionReceiverRejected * float3(0.90, 0.06, 0.04));
            debugMask = reflectionReceiverMask;
        }
        else if (mode == 11)
        {
            float waterProjectedDebug = saturate(waterProjectionStrength * 10.0);
            debugColor = waterProjectedDebug * (float3(0.0, 0.78, 1.0) + abs(waterProjectedReflection) * 24.0);
            debugMask = waterProjectedDebug;
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
            float pseudoWaterView = sqrt(saturate(max(pseudoWaterMask * 5.0, waterSurfaceReceiver * pseudoWaterSample.a * 3.0)));
            float pseudoWetView = sqrt(saturate(max(pseudoWetMask * 6.0, wetFloorProjectionReceiver * pseudoWetSample.a * 3.5)));
            float pseudoHardView = sqrt(saturate(max(pseudoHardMask * 6.5, hardAetherProjectionReceiver * pseudoHardSample.a * 3.8)));
            debugColor = saturate(
                pseudoWaterView * float3(0.0, 0.78, 1.0)
                + pseudoWetView * float3(0.18, 0.86, 0.42)
                + pseudoHardView * float3(0.82, 0.30, 1.0)
                + abs(pseudoSSRContribution) * 34.0);
            debugMask = saturate(max(max(pseudoWaterView, pseudoWetView), pseudoHardView));
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
