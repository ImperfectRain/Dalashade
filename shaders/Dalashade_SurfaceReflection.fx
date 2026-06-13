#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"

// Dalashade SurfaceReflection is a lightweight material-aware reflection
// impression pass. It is not SSR, RTGI, PTGI, or ray tracing. It shapes water
// sheen, wet-surface glints, icy sheen, neon/aether streaks, and polished
// surface highlights from cheap screen-space masks.

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
    ui_items = "Normal\0WaterPlane sheen\0SpecularGlint\0Wet reflection\0Aether/neon reflection\0Sky rejection\0Skin protection\0Final reflection influence\0Contribution over black\0Reflection source mask\0Reflection receiver mask\0";
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
uniform float Dalashade_CinematicPermission < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Cinematic Permission"; > = 0.0;

uniform float Dalashade_MaterialWaterPlane < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Water Plane"; > = 0.0;
uniform float Dalashade_MaterialSpecularGlint < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Specular Glint"; > = 0.0;
uniform float Dalashade_MaterialSandDust < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sand Dust"; > = 0.0;
uniform float Dalashade_MaterialSnowIce < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Snow Ice"; > = 0.0;
uniform float Dalashade_MaterialMetalIndustrial < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Metal Industrial"; > = 0.0;
uniform float Dalashade_MaterialCrystalAether < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Crystal Aether"; > = 0.0;
uniform float Dalashade_MaterialNeonGlass < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Neon Glass"; > = 0.0;
uniform float Dalashade_MaterialFireLavaHeat < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Fire Lava Heat"; > = 0.0;
uniform float Dalashade_MaterialSkyCloudFog < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sky Cloud Fog"; > = 0.0;
uniform float Dalashade_MaterialSkinProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Skin Protection"; > = 0.0;

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

float3 Dalashade_SampleReflectionColor(float2 uv, float depth, float2 direction, float offset, float softness, float depthReject)
{
    float2 primaryUv = saturate(uv + direction * offset);
    float2 sideA = saturate(uv + direction * offset * (1.0 + softness * 0.45) + float2(BUFFER_PIXEL_SIZE.x, 0.0) * (1.0 + softness * 2.0));
    float2 sideB = saturate(uv + direction * offset * (0.72 + softness * 0.35) - float2(BUFFER_PIXEL_SIZE.x, 0.0) * (1.0 + softness * 2.0));

    float gateA = Dalashade_SurfaceReflectionDepthGate(primaryUv, depth, depthReject);
    float gateB = Dalashade_SurfaceReflectionDepthGate(sideA, depth, depthReject) * (0.28 + softness * 0.38);
    float gateC = Dalashade_SurfaceReflectionDepthGate(sideB, depth, depthReject) * (0.22 + softness * 0.28);
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
    float gateA = Dalashade_SurfaceReflectionDepthGate(uvA, depth, depthReject);
    float gateB = Dalashade_SurfaceReflectionDepthGate(uvB, depth, depthReject) * 0.52;
    float3 sampleA = tex2D(ReShade::BackBuffer, uvA).rgb;
    float3 sampleB = tex2D(ReShade::BackBuffer, uvB).rgb;
    float energyA = smoothstep(0.34, 1.06, Dalashade_SurfaceReflectionLuma(sampleA) + Dalashade_GetSaturation(sampleA) * 0.58);
    float energyB = smoothstep(0.34, 1.06, Dalashade_SurfaceReflectionLuma(sampleB) + Dalashade_GetSaturation(sampleB) * 0.58);
    float weight = max(gateA * energyA + gateB * energyB, 0.001);
    return (sampleA * gateA * energyA + sampleB * gateB * energyB) / weight;
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

    Dalashade_MaterialMasks material = Dalashade_GetAllMaterialMasksWithWaterSplitDepthAssist(
        color,
        texcoord,
        0.0,
        0.0,
        Dalashade_MaterialWaterPlane,
        Dalashade_MaterialSpecularGlint,
        Dalashade_MaterialSandDust,
        Dalashade_MaterialSnowIce,
        0.0,
        Dalashade_MaterialMetalIndustrial,
        Dalashade_MaterialCrystalAether,
        Dalashade_MaterialNeonGlass,
        Dalashade_MaterialFireLavaHeat,
        Dalashade_MaterialSkyCloudFog,
        Dalashade_MaterialSkinProtection,
        0.0,
        0.0,
        0.0,
        0.0);

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

    float3 sheenSample = Dalashade_SurfaceReflectionDirectionalSheen(texcoord, Dalashade_WaterSheenRadius, depth);
    float3 cyanSheen = lerp(float3(0.04, 0.20, 0.24), float3(0.12, 0.58, 0.68), saturate(saturation + material.WaterPlane));
    float waterPlaneMask = material.WaterPlane * smoothstep(0.34, 0.92, smoothness) * surfaceFacing * (1.0 - material.SandDust * 0.76) * (1.0 - skinProtect * 0.86) * (1.0 - skyReject * 0.95);
    float waterReceiver = saturate(waterPlaneMask * (0.70 + smoothness * 0.32) * (1.0 - edge * 0.28));
    float3 waterSheen = lerp(cyanSheen, sheenSample, 0.46) * waterReceiver * Dalashade_WaterSheenStrength * 1.62;
    float reflectedWaterLuma = Dalashade_SurfaceReflectionLuma(reflectedVertical);
    float waterReflectionMask = waterReceiver * smoothstep(0.18, 0.82, reflectedWaterLuma + Dalashade_GetSaturation(reflectedVertical) * 0.36) * (1.0 - bright * 0.26);
    float3 waterReflection = lerp(reflectedVertical, reflectedSoft, reflectionSoftness * 0.42) * lerp(float3(0.68, 0.98, 1.0), cyanSheen + 0.18, 0.34) * waterReflectionMask * Dalashade_WaterReflectionStrength;

    float glintShape = smoothstep(0.44, 0.98, luma) * smoothstep(0.025, 0.32, edge) * (1.0 - smoothness * 0.48);
    float specularMask = saturate(material.SpecularGlint * glintShape * (1.0 - waterPlaneMask * 0.28) * (1.0 - skinProtect * 0.70) * (1.0 - skyReject * 0.70));
    float3 specularGlint = lerp(float3(0.65, 0.86, 1.0), max(color + 0.26, reflectedStreak), 0.42) * specularMask * Dalashade_SpecularGlintStrength * 1.54;
    float3 specularReflection = reflectedStreak * specularMask * Dalashade_SpecularReflectionStrength * 0.86;

    float hardWetSurface = saturate(max(material.MetalIndustrial * 0.48, max(material.SandDust * 0.12, material.SnowIce * 0.08)) + midtone * smoothness * 0.34 + material.SpecularGlint * 0.18);
    float wetMask = Dalashade_Wetness * hardWetSurface * (0.42 + shadow * 0.58 + bright * 0.12) * safeSurface * (1.0 - material.SandDust * bright * 0.62);
    float wetReceiver = wetMask * smoothstep(0.22, 0.92, smoothness) * (1.0 - edge * 0.36) * (0.70 + material.MetalIndustrial * 0.22 + material.SpecularGlint * 0.16);
    float3 wetReflection = lerp(sheenSample, reflectedVertical, 0.58) * wetReceiver * Dalashade_WetReflectionStrength * 1.78;

    float3 localSource = Dalashade_SurfaceReflectionLocalSource(texcoord, depth, max(Dalashade_WaterSheenRadius, 0.75) * (1.25 + Dalashade_Night * 0.45));
    float localSourceEnergy = smoothstep(0.18, 0.96, Dalashade_SurfaceReflectionLuma(localSource) + Dalashade_GetSaturation(localSource) * 0.62);
    float receiverSurface = safeSurface * saturate(smoothness * 0.46 + surfaceFacing * 0.32 + material.MetalIndustrial * 0.28 + waterPlaneMask * 0.34 + material.SnowIce * 0.15 + wetMask * 0.20);
    float aetherMask = saturate(material.CrystalAether * (0.54 + Dalashade_MagicGlow * 0.56) * smoothstep(0.12, 0.82, luma + saturation * 0.58));
    float neonMask = saturate(material.NeonGlass * (0.54 + Dalashade_NeonGlow * 0.56) * smoothstep(0.16, 0.86, luma + saturation * 0.62));
    float fireMask = saturate(material.FireLavaHeat * smoothstep(0.20, 0.88, luma + saturation * 0.42));
    float emissiveSourceMask = saturate(aetherMask + neonMask + fireMask + material.SpecularGlint * smoothstep(0.48, 0.98, luma));
    float emissiveReflectionMask = saturate((emissiveSourceMask * 0.72 + localSourceEnergy * (material.CrystalAether + material.NeonGlass + material.FireLavaHeat) * 0.56) * (0.48 + material.SpecularGlint * 0.30 + waterReceiver * 0.22 + receiverSurface * 0.42) * safeSurface);
    float3 aetherNeonColor = material.CrystalAether * float3(0.16, 0.44, 1.0) * Dalashade_AetherReflectionStrength;
    aetherNeonColor += material.NeonGlass * float3(0.48, 0.22, 1.0) * Dalashade_NeonReflectionStrength;
    aetherNeonColor += material.FireLavaHeat * float3(1.0, 0.30, 0.06) * 0.42;
    float3 aetherStreak = lerp(localSource, reflectedStreak, 0.55);
    float3 aetherNeonReflection = saturate(aetherNeonColor + aetherStreak * 0.58 + reflectedSoft * 0.12) * emissiveReflectionMask * 1.82;

    float iceMask = material.SnowIce * smoothstep(0.30, 0.94, smoothness) * midtone * safeSurface * (1.0 - bright * 0.66);
    float3 iceSheen = float3(0.46, 0.72, 1.0) * iceMask * Dalashade_IceSheenStrength * 1.38;

    float nightBoost = 1.0 + Dalashade_Night * (0.18 + Dalashade_ArtificialLight * 0.30) + Dalashade_CinematicPermission * 0.10;
    float3 contribution = (waterSheen + waterReflection + specularGlint + specularReflection + wetReflection + aetherNeonReflection + iceSheen) * Dalashade_SurfaceReflectionStrength * nightBoost;
    contribution *= 1.0 - highlightSafety * 0.56;

    float waterSheenAllowance = material.WaterPlane * (0.096 + Dalashade_Wetness * 0.030);
    float glintAllowance = material.SpecularGlint * 0.070;
    float wetAllowance = Dalashade_Wetness * wetReceiver * 0.078;
    float aetherNeonAllowance = saturate(material.CrystalAether + material.NeonGlass + material.FireLavaHeat) * (0.060 + Dalashade_Night * 0.035 + Dalashade_CinematicPermission * 0.020);
    float reflectionPositiveAllowance = 0.050 + waterSheenAllowance + glintAllowance + wetAllowance + aetherNeonAllowance + material.SnowIce * 0.030;
    reflectionPositiveAllowance *= 1.0 - saturate(highlightSafety * 0.38 + Dalashade_CombatPressure * 0.22 + Dalashade_Readability * 0.10);
    float positiveLimit = reflectionPositiveAllowance;
    float3 result = saturate(color + min(contribution, positiveLimit.xxx));
    float reflectionSourceMask = saturate(emissiveSourceMask + specularMask * 0.74 + localSourceEnergy * (material.CrystalAether + material.NeonGlass + material.FireLavaHeat) * 0.55);
    float reflectionReceiverMask = saturate(waterReceiver + wetReceiver + receiverSurface * 0.62 + iceMask * 0.30);
    float finalMask = saturate(Dalashade_SurfaceReflectionLuma(abs(result - color)) * 18.0 + waterReflectionMask * 0.42 + specularMask * 0.34 + wetReceiver * 0.34 + emissiveReflectionMask * 0.42 + iceMask * 0.24);

    int mode = Dalashade_SurfaceReflectionDebugMode;
    if (mode > 0)
    {
        float3 debugColor = float3(0.0, 0.0, 0.0);
        float debugMask = finalMask;
        if (mode == 1)
        {
            debugColor = float3(0.0, waterReflectionMask * 0.85, waterReflectionMask);
            debugMask = waterReflectionMask;
        }
        else if (mode == 2)
        {
            debugColor = float3(specularMask * 0.65, specularMask * 0.82, specularMask);
            debugMask = specularMask;
        }
        else if (mode == 3)
        {
            debugColor = float3(wetReceiver * 0.25, wetReceiver * 0.55, wetReceiver);
            debugMask = wetReceiver;
        }
        else if (mode == 4)
        {
            debugColor = saturate(aetherMask * float3(0.18, 0.45, 1.0) + neonMask * float3(0.72, 0.12, 1.0) + fireMask * float3(1.0, 0.34, 0.04));
            debugMask = saturate(aetherMask + neonMask + fireMask);
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
            debugColor = contribution * 12.0 + float3(waterReflectionMask * 0.12, specularMask * 0.20, emissiveReflectionMask * 0.24);
            debugMask = finalMask;
        }
        else if (mode == 9)
        {
            debugColor = saturate(reflectionSourceMask * float3(0.85, 0.55, 1.0) + emissiveSourceMask * float3(0.20, 0.55, 1.0) + specularMask * float3(0.55, 0.78, 1.0));
            debugMask = reflectionSourceMask;
        }
        else if (mode == 10)
        {
            debugColor = saturate(waterReceiver * float3(0.0, 0.76, 1.0) + wetReceiver * float3(0.28, 0.52, 0.78) + receiverSurface * float3(0.24, 0.90, 0.42));
            debugMask = reflectionReceiverMask;
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
