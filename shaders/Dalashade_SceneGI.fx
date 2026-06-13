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
    ui_items = "Off\0AO only\0Bounce only\0Night light pooling\0Material influence\0Sky rejection\0Skin protection\0Final GI influence\0Depth-normal confidence\0";
    ui_label = "Dalashade GI Debug Mode";
> = 0;

uniform float Dalashade_GIDebugOpacity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade GI Debug Opacity";
> = 0.75;

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

    Dalashade_MaterialMasks material = Dalashade_GetAllMaterialMasksWithWaterSplitDepthAssist(
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

    float skyReject = saturate(material.SkyCloudFog * Dalashade_GISkyReject);
    float skinProtect = saturate(material.SkinProtection * Dalashade_GISkinProtect);
    float combatDampen = 1.0 - saturate(Dalashade_IntentCombatPressure * 0.58 + Dalashade_IntentReadability * 0.16);
    float highlightGuard = saturate(Dalashade_IntentHighlightProtection * bright + material.SnowIce * bright * 0.72 + material.SandDust * bright * 0.32);
    float waterReject = saturate(material.WaterPlane * 0.78 + material.SpecularGlint * 0.22);
    float aoMaterialBoost = saturate(1.0 + material.StoneRuins * 0.26 + material.MetalIndustrial * 0.24 - material.SnowIce * 0.42 - material.SandDust * bright * 0.32 - waterReject * 0.54 - skinProtect * 0.72);

    float aoRadius = Dalashade_GIAORadius * (1.0 + Dalashade_GIRadius);
    float ao = Dalashade_SceneGIAO(texcoord, depth, aoRadius, normalConfidence);
    ao *= Dalashade_GIAOIntensity * aoMaterialBoost * combatDampen;
    ao *= 1.0 - skyReject;
    ao *= 1.0 - highlightGuard * 0.75;

    float3 neighbor = Dalashade_SceneGINeighborAverage(texcoord, 1.0 + Dalashade_GIRadius);
    float3 materialTint = float3(0.0, 0.0, 0.0);
    materialTint += material.FoliageStrong * float3(0.12, 0.28, 0.10);
    materialTint += material.OrganicGreenSurface * float3(0.10, 0.18, 0.06);
    materialTint += material.SandDust * float3(0.36, 0.24, 0.08);
    materialTint += material.SnowIce * float3(0.18, 0.24, 0.34);
    materialTint += material.WaterPlane * float3(0.04, 0.20, 0.28);
    materialTint += material.FireLavaHeat * float3(0.44, 0.16, 0.04);
    materialTint += material.CrystalAether * float3(0.16, 0.12, 0.36);
    materialTint += material.NeonGlass * float3(0.18, 0.22, 0.40);
    materialTint += material.MetalIndustrial * float3(0.08, 0.10, 0.12);
    materialTint = saturate(materialTint);

    float materialInfluence = saturate(Dalashade_GIMaterialInfluence * material.CombinedConfidence);
    float bounceMask = midtone * (0.38 + shadow * 0.44) * (1.0 - bright * 0.62);
    bounceMask *= 1.0 - skyReject;
    bounceMask *= 1.0 - skinProtect * 0.85;
    bounceMask *= 1.0 - material.VoidDarkness * 0.82;
    float3 bounceTint = lerp(neighbor * 0.35, materialTint, materialInfluence);
    float3 bounce = bounceTint * bounceMask * Dalashade_GIBounceStrength * combatDampen;
    bounce *= 0.55 + Dalashade_IntentAtmosphere * 0.18 + Dalashade_IntentCinematicPermission * 0.16;

    float saturatedAccent = max(max(color.r, color.g), color.b) - min(min(color.r, color.g), color.b);
    float localLight = smoothstep(0.52, 0.96, luma) * smoothstep(0.035, 0.42, saturatedAccent + material.SpecularGlint * 0.32);
    localLight = max(localLight, material.FireLavaHeat * bright);
    localLight = max(localLight, material.CrystalAether * smoothstep(0.30, 0.88, luma));
    localLight = max(localLight, material.NeonGlass * smoothstep(0.36, 0.92, luma));
    float nightContext = saturate(Dalashade_IntentShadowProtection * 0.35 + Dalashade_IntentAtmosphere * 0.12 + Dalashade_IntentCosmicMood * 0.20);
    float moonSurface = saturate(normal.z * normalConfidence * (material.SkyCloudFog * 0.22 + material.SnowIce * 0.20 + material.WaterPlane * 0.10));
    float nightPool = saturate(localLight * (0.52 + material.SpecularGlint * 0.18) + moonSurface * 0.22);
    nightPool *= Dalashade_GINightLightStrength * nightContext * combatDampen * (1.0 - skyReject * 0.72);
    float3 nightTint = lerp(float3(0.88, 0.78, 0.58), float3(0.58, 0.74, 1.0), saturate(Dalashade_IntentCold + Dalashade_IntentCosmicMood + material.CrystalAether));
    nightTint = lerp(nightTint, float3(0.55, 0.92, 1.0), saturate(Dalashade_IntentNeonGlow + material.NeonGlass) * 0.45);
    nightTint = lerp(nightTint, float3(1.0, 0.56, 0.24), material.FireLavaHeat * 0.58);
    float3 nightLight = nightTint * nightPool * (0.18 + shadow * 0.22);

    float strength = Dalashade_GIStrength * combatDampen;
    float3 result = color;
    result *= 1.0 - saturate(ao * strength);
    result += (bounce + nightLight) * strength;
    result = min(result, color + 0.10 + Dalashade_IntentCinematicPermission * 0.04);
    result = max(result, color - 0.10);
    result = saturate(result);

    float finalInfluence = saturate(ao + Dalashade_SceneGILuma(bounce) * 3.0 + nightPool);
    int mode = Dalashade_GIDebugMode;
    if (mode > 0)
    {
        float opacity = saturate(Dalashade_GIDebugOpacity);
        float3 debugColor = float3(0.0, 0.0, 0.0);
        float debugMask = 1.0;
        if (mode == 1)
        {
            debugColor = ao.xxx;
            debugMask = ao;
        }
        else if (mode == 2)
        {
            debugColor = saturate(bounce * 5.0);
            debugMask = saturate(Dalashade_SceneGILuma(debugColor));
        }
        else if (mode == 3)
        {
            debugColor = nightPool.xxx * nightTint;
            debugMask = nightPool;
        }
        else if (mode == 4)
        {
            debugColor = Dalashade_GetMaterialOverviewColor(material);
            debugMask = material.CombinedConfidence;
        }
        else if (mode == 5)
        {
            debugColor = float3(skyReject, material.SkyCloudFog, 1.0 - skyReject);
            debugMask = saturate(max(skyReject, material.SkyCloudFog));
        }
        else if (mode == 6)
        {
            debugColor = float3(skinProtect, 0.42 * skinProtect, 1.0 - skinProtect);
            debugMask = skinProtect;
        }
        else if (mode == 7)
        {
            debugColor = float3(ao, Dalashade_SceneGILuma(bounce) * 4.0, nightPool);
            debugMask = finalInfluence;
        }
        else if (mode == 8)
        {
            debugColor = float3(normalConfidence, normal.z * 0.5 + 0.5, depth);
            debugMask = max(normalConfidence, 0.35);
        }

        return float4(lerp(color, saturate(debugColor), saturate(debugMask * opacity)), 1.0);
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
