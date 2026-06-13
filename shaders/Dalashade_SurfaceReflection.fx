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

uniform int Dalashade_SurfaceReflectionDebugMode <
    ui_type = "combo";
    ui_items = "Normal\0WaterPlane sheen\0SpecularGlint\0Wet reflection\0Aether/neon reflection\0Sky rejection\0Skin protection\0Final reflection influence\0Contribution over black\0";
    ui_label = "Surface Reflection Debug Mode";
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

float3 Dalashade_SurfaceReflectionDebug(float2 uv, float3 originalColor, float3 debugColor, float debugMask)
{
    float opacity = saturate(Dalashade_SurfaceReflectionDebugOpacity);
    float boost = max(Dalashade_SurfaceReflectionDebugBoost, 0.001);
    float3 cleanDebug = saturate(float3(1.0, 1.0, 1.0) - exp(-max(debugColor, float3(0.0, 0.0, 0.0)) * boost));
    if (Dalashade_SurfaceReflectionDebugMode == 8)
    {
        return cleanDebug;
    }

    return lerp(originalColor, cleanDebug, saturate(debugMask * opacity));
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

    float3 sheenSample = Dalashade_SurfaceReflectionDirectionalSheen(texcoord, Dalashade_WaterSheenRadius, depth);
    float3 cyanSheen = lerp(float3(0.04, 0.20, 0.24), float3(0.12, 0.58, 0.68), saturate(saturation + material.WaterPlane));
    float waterPlaneMask = material.WaterPlane * smoothstep(0.42, 0.96, smoothness) * surfaceFacing * (1.0 - material.SandDust * 0.70) * (1.0 - skyReject * 0.92);
    float3 waterSheen = lerp(cyanSheen, sheenSample, 0.38) * waterPlaneMask * Dalashade_WaterSheenStrength;

    float glintShape = smoothstep(0.50, 0.98, luma) * smoothstep(0.04, 0.34, edge) * (1.0 - smoothness * 0.55);
    float specularMask = saturate(material.SpecularGlint * glintShape * (1.0 - waterPlaneMask * 0.28) * (1.0 - skinProtect * 0.70) * (1.0 - skyReject * 0.70));
    float3 specularGlint = lerp(float3(0.65, 0.84, 1.0), color + 0.20, 0.35) * specularMask * Dalashade_SpecularGlintStrength;

    float hardWetSurface = saturate(max(material.MetalIndustrial * 0.42, max(material.SandDust * 0.10, material.SnowIce * 0.06)) + midtone * smoothness * 0.22);
    float wetMask = Dalashade_Wetness * hardWetSurface * (0.35 + shadow * 0.45 + bright * 0.12) * safeSurface * (1.0 - material.SandDust * bright * 0.58);
    float3 wetReflection = sheenSample * wetMask * Dalashade_WetReflectionStrength;

    float aetherMask = saturate(material.CrystalAether * (0.45 + Dalashade_MagicGlow * 0.44) * smoothstep(0.18, 0.88, luma + saturation * 0.45));
    float neonMask = saturate(material.NeonGlass * (0.45 + Dalashade_NeonGlow * 0.44) * smoothstep(0.24, 0.92, luma + saturation * 0.50));
    float fireMask = saturate(material.FireLavaHeat * smoothstep(0.28, 0.94, luma + saturation * 0.35));
    float emissiveReflectionMask = saturate((aetherMask + neonMask + fireMask * 0.65) * (0.55 + material.SpecularGlint * 0.34 + waterPlaneMask * 0.16) * safeSurface);
    float3 aetherNeonColor = material.CrystalAether * float3(0.18, 0.42, 1.0) * Dalashade_AetherReflectionStrength;
    aetherNeonColor += material.NeonGlass * float3(0.42, 0.25, 1.0) * Dalashade_NeonReflectionStrength;
    aetherNeonColor += material.FireLavaHeat * float3(1.0, 0.28, 0.06) * 0.34;
    float3 aetherNeonReflection = saturate(aetherNeonColor + sheenSample * 0.18) * emissiveReflectionMask;

    float iceMask = material.SnowIce * smoothstep(0.36, 0.96, smoothness) * midtone * safeSurface * (1.0 - bright * 0.58);
    float3 iceSheen = float3(0.46, 0.70, 1.0) * iceMask * Dalashade_IceSheenStrength;

    float nightBoost = 1.0 + Dalashade_Night * (0.10 + Dalashade_ArtificialLight * 0.22) + Dalashade_CinematicPermission * 0.08;
    float3 contribution = (waterSheen + specularGlint + wetReflection + aetherNeonReflection + iceSheen) * Dalashade_SurfaceReflectionStrength * nightBoost;
    contribution *= 1.0 - highlightSafety * 0.62;

    float positiveLimit = 0.045 + material.WaterPlane * 0.055 + material.SpecularGlint * 0.050 + Dalashade_Wetness * 0.035 + material.CrystalAether * 0.050 + material.NeonGlass * 0.045 + material.SnowIce * 0.025;
    positiveLimit *= 1.0 - saturate(highlightSafety * 0.44 + Dalashade_CombatPressure * 0.24 + Dalashade_Readability * 0.12);
    float3 result = saturate(color + min(contribution, positiveLimit.xxx));
    float finalMask = saturate(Dalashade_SurfaceReflectionLuma(abs(result - color)) * 12.0 + waterPlaneMask * 0.35 + specularMask * 0.30 + wetMask * 0.26 + emissiveReflectionMask * 0.34 + iceMask * 0.22);

    int mode = Dalashade_SurfaceReflectionDebugMode;
    if (mode > 0)
    {
        float3 debugColor = float3(0.0, 0.0, 0.0);
        float debugMask = finalMask;
        if (mode == 1)
        {
            debugColor = float3(0.0, waterPlaneMask * 0.85, waterPlaneMask);
            debugMask = waterPlaneMask;
        }
        else if (mode == 2)
        {
            debugColor = float3(specularMask * 0.65, specularMask * 0.82, specularMask);
            debugMask = specularMask;
        }
        else if (mode == 3)
        {
            debugColor = float3(wetMask * 0.25, wetMask * 0.55, wetMask);
            debugMask = wetMask;
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
            debugColor = contribution * 10.0 + float3(waterPlaneMask * 0.10, specularMask * 0.18, emissiveReflectionMask * 0.22);
            debugMask = finalMask;
        }

        return float4(Dalashade_SurfaceReflectionDebug(texcoord, color, debugColor, debugMask), 1.0);
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
