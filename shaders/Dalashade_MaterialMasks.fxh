#ifndef DALASHADE_MATERIAL_MASKS_FXH
#define DALASHADE_MATERIAL_MASKS_FXH

// Shared screen-space helpers for Dalashade material debug views.
// These functions infer likely material influence from color, contrast, smoothness, screen position, and optional depth.
// They are not true FFXIV material IDs.

struct Dalashade_MaterialSignals
{
    float3 Color;
    float2 Uv;
    float Luma;
    float Saturation;
    float Hue;
    float Edge;
    float Smoothness;
    float Depth;
    float FarDepth;
    float UpperScreen;
};

struct Dalashade_MaterialMasks
{
    float FoliageStrong;
    float OrganicGreenSurface;
    float WaterSpecular;
    float SandDust;
    float SnowIce;
    float StoneRuins;
    float MetalIndustrial;
    float CrystalAether;
    float NeonGlass;
    float FireLavaHeat;
    float SkyCloudFog;
    float SkinProtection;
    float VoidDarkness;
    float CombinedConfidence;
};

float Dalashade_GetLuminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float Dalashade_GetSaturation(float3 color)
{
    float maxChannel = max(color.r, max(color.g, color.b));
    float minChannel = min(color.r, min(color.g, color.b));
    return maxChannel <= 0.0001 ? 0.0 : saturate((maxChannel - minChannel) / maxChannel);
}

float Dalashade_GetHue(float3 color)
{
    float maxChannel = max(color.r, max(color.g, color.b));
    float minChannel = min(color.r, min(color.g, color.b));
    float chroma = maxChannel - minChannel;

    if (chroma <= 0.0001)
    {
        return 0.0;
    }

    float hue = 0.0;
    if (maxChannel == color.r)
    {
        hue = (color.g - color.b) / chroma;
    }
    else if (maxChannel == color.g)
    {
        hue = 2.0 + (color.b - color.r) / chroma;
    }
    else
    {
        hue = 4.0 + (color.r - color.g) / chroma;
    }

    return frac(hue / 6.0);
}

float Dalashade_GetHueDistance(float hue, float targetHue)
{
    float distance = abs(hue - targetHue);
    return min(distance, 1.0 - distance);
}

float Dalashade_HueMask(float hue, float targetHue, float width)
{
    return saturate(1.0 - Dalashade_GetHueDistance(hue, targetHue) / max(width, 0.0001));
}

float Dalashade_RangeMask(float value, float low, float high)
{
    return saturate(saturate((value - low) / max(high - low, 0.0001)) * saturate((high - value) / max(high - low, 0.0001)) * 4.0);
}

float Dalashade_GetEdgeStrength(float2 uv)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float center = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv).rgb);
    float left = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv - float2(texel.x, 0.0)).rgb);
    float right = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv + float2(texel.x, 0.0)).rgb);
    float up = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv - float2(0.0, texel.y)).rgb);
    float down = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv + float2(0.0, texel.y)).rgb);
    return saturate((abs(center - left) + abs(center - right) + abs(center - up) + abs(center - down)) * 2.5);
}

float Dalashade_GetSmoothness(float2 uv)
{
    return saturate(1.0 - Dalashade_GetEdgeStrength(uv) * 2.2);
}

float Dalashade_GetDepthFactor(float2 uv)
{
    return saturate(ReShade::GetLinearizedDepth(uv));
}

Dalashade_MaterialSignals Dalashade_GetMaterialSignals(float3 color, float2 uv)
{
    Dalashade_MaterialSignals signals;
    signals.Color = saturate(color);
    signals.Uv = uv;
    signals.Luma = Dalashade_GetLuminance(signals.Color);
    signals.Saturation = Dalashade_GetSaturation(signals.Color);
    signals.Hue = Dalashade_GetHue(signals.Color);
    signals.Edge = Dalashade_GetEdgeStrength(uv);
    signals.Smoothness = saturate(1.0 - signals.Edge * 2.2);
    signals.Depth = Dalashade_GetDepthFactor(uv);
    signals.FarDepth = smoothstep(0.38, 0.96, signals.Depth);
    signals.UpperScreen = smoothstep(0.72, 0.12, uv.y);
    return signals;
}

float Dalashade_GetRawFoliageStrong(Dalashade_MaterialSignals s)
{
    float green = max(Dalashade_HueMask(s.Hue, 0.30, 0.12), Dalashade_HueMask(s.Hue, 0.23, 0.08));
    float leafTone = Dalashade_RangeMask(s.Luma, 0.08, 0.78) * smoothstep(0.16, 0.52, s.Saturation);
    float fineOrganicTexture = smoothstep(0.06, 0.28, s.Edge) * saturate(1.0 - s.Smoothness * smoothstep(0.46, 0.88, s.Luma));
    float skyReject = saturate(1.0 - s.Smoothness * s.UpperScreen * smoothstep(0.38, 0.92, s.Luma));
    return saturate(green * leafTone * fineOrganicTexture * skyReject);
}

float Dalashade_GetRawOrganicGreenSurface(Dalashade_MaterialSignals s)
{
    float green = max(Dalashade_HueMask(s.Hue, 0.30, 0.15), Dalashade_HueMask(s.Hue, 0.23, 0.10));
    float surfaceTone = Dalashade_RangeMask(s.Luma, 0.05, 0.70) * smoothstep(0.08, 0.36, s.Saturation);
    float hardSurfaceTexture = smoothstep(0.12, 0.45, s.Edge);
    return saturate(green * surfaceTone * hardSurfaceTexture * 0.55);
}

float Dalashade_GetRawSkyCloudFog(Dalashade_MaterialSignals s, float foliageStrong, float organicGreenSurface, float hardSurface)
{
    float blueSky = Dalashade_HueMask(s.Hue, 0.57, 0.15) * smoothstep(0.28, 0.86, s.Luma) * smoothstep(0.10, 0.52, s.Saturation);
    float brightCloud = smoothstep(0.58, 0.96, s.Luma) * saturate(1.0 - smoothstep(0.34, 0.74, s.Saturation));
    float warmSky = max(Dalashade_HueMask(s.Hue, 0.04, 0.06), Dalashade_HueMask(s.Hue, 0.11, 0.08)) * smoothstep(0.35, 0.90, s.Luma) * smoothstep(0.08, 0.44, s.Saturation);
    float grayAtmosphere = saturate(1.0 - s.Saturation) * smoothstep(0.18, 0.86, s.Luma);
    float nightSky = smoothstep(0.70, 1.0, s.Smoothness) * saturate(1.0 - smoothstep(0.13, 0.36, s.Luma)) * saturate(0.25 + s.UpperScreen * 0.75);
    float smoothAtmosphere = s.Smoothness * saturate(0.35 + s.UpperScreen * 0.35 + s.FarDepth * 0.30);
    float canopyGap = smoothAtmosphere * s.UpperScreen * max(blueSky, brightCloud) * saturate(1.0 - foliageStrong * 0.55);

    float skyCandidate = max(max(blueSky, brightCloud), max(warmSky, max(grayAtmosphere * 0.75, nightSky)));
    skyCandidate = max(skyCandidate, canopyGap);
    skyCandidate *= smoothstep(0.44, 0.88, s.Smoothness) * saturate(0.45 + s.UpperScreen * 0.40 + s.FarDepth * 0.25);

    float surfaceReject = saturate(hardSurface * 0.65 + foliageStrong * 0.85 + organicGreenSurface * 0.42);
    float lowScreenPenalty = lerp(0.58, 1.0, saturate(s.UpperScreen + s.FarDepth * 0.65));
    return saturate(skyCandidate * lowScreenPenalty * (1.0 - surfaceReject * 0.72));
}

float Dalashade_GetWaterSpecularMask(float3 color, float2 uv, float sceneWaterSpecular)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float cyanBlue = max(Dalashade_HueMask(s.Hue, 0.52, 0.11), Dalashade_HueMask(s.Hue, 0.60, 0.12));
    float glint = smoothstep(0.62, 0.96, s.Luma) * saturate(s.Edge * 1.3 + s.Saturation * 0.55);
    float lowScreenWater = smoothstep(0.18, 0.88, s.Uv.y);
    float skyLike = s.Smoothness * s.UpperScreen * smoothstep(0.40, 0.95, s.Luma);
    return saturate(sceneWaterSpecular) * saturate((cyanBlue * s.Saturation * 0.50 + glint * 0.65) * (0.40 + lowScreenWater * 0.60) * (1.0 - skyLike * 0.70));
}

float Dalashade_GetSandDustMask(float3 color, float2 uv, float sceneSandDust)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float warmGround = max(Dalashade_HueMask(s.Hue, 0.10, 0.09), Dalashade_HueMask(s.Hue, 0.14, 0.08));
    float sandTone = smoothstep(0.24, 0.72, s.Luma) * saturate(1.0 - smoothstep(0.82, 1.0, s.Luma)) * smoothstep(0.08, 0.34, s.Saturation);
    float skyReject = s.Smoothness * s.UpperScreen * smoothstep(0.36, 0.92, s.Luma);
    float fireReject = Dalashade_HueMask(s.Hue, 0.04, 0.05) * smoothstep(0.55, 1.0, s.Saturation) * smoothstep(0.55, 1.0, s.Luma);
    float skinReject = Dalashade_HueMask(s.Hue, 0.055, 0.045) * Dalashade_RangeMask(s.Luma, 0.25, 0.72) * smoothstep(0.10, 0.36, s.Saturation);
    return saturate(sceneSandDust) * saturate(warmGround * sandTone * (1.0 - skyReject * 0.65) * (1.0 - fireReject * 0.82) * (1.0 - skinReject * 0.62) * (0.72 + 0.28 * s.Edge));
}

float Dalashade_GetSnowIceMask(float3 color, float2 uv, float sceneSnowIce)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float cold = max(Dalashade_HueMask(s.Hue, 0.56, 0.18), saturate(1.0 - s.Saturation));
    float brightColdSurface = smoothstep(0.48, 0.92, s.Luma) * saturate(1.0 - smoothstep(0.42, 0.78, s.Saturation)) * cold;
    float cloudLike = s.Smoothness * s.UpperScreen * smoothstep(0.60, 0.98, s.Luma);
    float surfaceTexture = smoothstep(0.035, 0.22, s.Edge);
    return saturate(sceneSnowIce) * saturate(brightColdSurface * (0.38 + 0.62 * surfaceTexture) * (1.0 - cloudLike * 0.68));
}

float Dalashade_GetSkyFogMask(float3 color, float2 uv, float sceneSkyCloudFog)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float foliageStrong = Dalashade_GetRawFoliageStrong(s);
    float organicGreen = Dalashade_GetRawOrganicGreenSurface(s);
    float hardSurface = smoothstep(0.11, 0.40, s.Edge) * Dalashade_RangeMask(s.Luma, 0.08, 0.82);
    return saturate(sceneSkyCloudFog) * Dalashade_GetRawSkyCloudFog(s, foliageStrong, organicGreen, hardSurface);
}

float Dalashade_GetFoliageMask(float3 color, float2 uv, float sceneFoliage)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float strong = Dalashade_GetRawFoliageStrong(s);
    float organicGreen = Dalashade_GetRawOrganicGreenSurface(s);
    float hardSurface = smoothstep(0.18, 0.48, s.Edge) * saturate(1.0 - strong);
    float skyFog = Dalashade_GetRawSkyCloudFog(s, strong, organicGreen, hardSurface);
    float strongFoliage = strong * (1.0 - skyFog * 0.88) * (1.0 - hardSurface * 0.28);
    float weakInfluence = organicGreen * 0.32 * (1.0 - skyFog * 0.60);
    return saturate(sceneFoliage) * saturate(strongFoliage + weakInfluence);
}

float Dalashade_GetFoliageStrongMask(float3 color, float2 uv, float sceneFoliage)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float strong = Dalashade_GetRawFoliageStrong(s);
    float hardSurface = smoothstep(0.18, 0.48, s.Edge) * saturate(1.0 - strong);
    float skyFog = Dalashade_GetRawSkyCloudFog(s, strong, Dalashade_GetRawOrganicGreenSurface(s), hardSurface);
    return saturate(sceneFoliage) * saturate(strong * (1.0 - skyFog * 0.90) * (1.0 - hardSurface * 0.35));
}

float Dalashade_GetOrganicGreenSurfaceMask(float3 color, float2 uv, float sceneFoliage)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float strong = Dalashade_GetRawFoliageStrong(s);
    float organicGreen = Dalashade_GetRawOrganicGreenSurface(s);
    float hardSurface = smoothstep(0.16, 0.42, s.Edge);
    float skyFog = Dalashade_GetRawSkyCloudFog(s, strong, organicGreen, hardSurface);
    return saturate(sceneFoliage) * saturate(organicGreen * (1.0 - strong * 0.55) * (1.0 - skyFog * 0.72));
}

float Dalashade_GetStoneRuinsMask(float3 color, float2 uv, float sceneStoneRuins)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float neutralStone = saturate(1.0 - s.Saturation * 0.85) * Dalashade_RangeMask(s.Luma, 0.10, 0.72);
    float hardTexture = smoothstep(0.10, 0.44, s.Edge);
    float skyReject = s.Smoothness * s.UpperScreen * smoothstep(0.35, 0.90, s.Luma);
    return saturate(sceneStoneRuins) * saturate(neutralStone * hardTexture * (1.0 - skyReject * 0.82));
}

float Dalashade_GetMetalIndustrialMask(float3 color, float2 uv, float sceneMetalIndustrial)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float coolNeutral = max(Dalashade_HueMask(s.Hue, 0.58, 0.16), saturate(1.0 - s.Saturation));
    float hardEdge = smoothstep(0.10, 0.45, s.Edge);
    float midTone = Dalashade_RangeMask(s.Luma, 0.12, 0.78);
    float waterReject = Dalashade_HueMask(s.Hue, 0.54, 0.10) * s.Smoothness * smoothstep(0.25, 0.85, s.Luma);
    return saturate(sceneMetalIndustrial) * saturate(coolNeutral * saturate(1.0 - s.Saturation * 0.55) * hardEdge * midTone * (1.0 - waterReject * 0.45));
}

float Dalashade_GetCrystalAetherMask(float3 color, float2 uv, float sceneCrystalAether)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float aetherHue = max(max(Dalashade_HueMask(s.Hue, 0.52, 0.09), Dalashade_HueMask(s.Hue, 0.68, 0.11)), Dalashade_HueMask(s.Hue, 0.82, 0.11));
    float luminous = smoothstep(0.34, 0.92, s.Luma) * smoothstep(0.28, 0.85, s.Saturation);
    float skyWaterReject = s.Smoothness * max(s.UpperScreen, Dalashade_HueMask(s.Hue, 0.56, 0.09)) * smoothstep(0.36, 0.86, s.Luma);
    return saturate(sceneCrystalAether) * saturate(aetherHue * luminous * (0.65 + 0.35 * s.Edge) * (1.0 - skyWaterReject * 0.45));
}

float Dalashade_GetNeonGlassMask(float3 color, float2 uv, float sceneNeonGlass)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float neonHue = max(max(Dalashade_HueMask(s.Hue, 0.50, 0.07), Dalashade_HueMask(s.Hue, 0.83, 0.08)), Dalashade_HueMask(s.Hue, 0.16, 0.06));
    float brightStrip = smoothstep(0.48, 0.95, s.Luma) * smoothstep(0.45, 0.95, s.Saturation) * smoothstep(0.08, 0.38, s.Edge);
    return saturate(sceneNeonGlass) * saturate(neonHue * brightStrip);
}

float Dalashade_GetFireLavaHeatMask(float3 color, float2 uv, float sceneFireLavaHeat)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float fireHue = max(Dalashade_HueMask(s.Hue, 0.03, 0.06), Dalashade_HueMask(s.Hue, 0.10, 0.07));
    float luminousWarm = smoothstep(0.42, 0.95, s.Luma) * smoothstep(0.38, 0.95, s.Saturation);
    float sandSkinReject = Dalashade_RangeMask(s.Luma, 0.22, 0.74) * smoothstep(0.08, 0.38, s.Saturation) * (1.0 - smoothstep(0.12, 0.42, s.Edge));
    return saturate(sceneFireLavaHeat) * saturate(fireHue * luminousWarm * (0.75 + 0.25 * s.Edge) * (1.0 - sandSkinReject * 0.40));
}

float Dalashade_GetSkinProtectionMask(float3 color, float2 uv, float sceneSkinProtection)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float skinHue = max(Dalashade_HueMask(s.Hue, 0.055, 0.040), Dalashade_HueMask(s.Hue, 0.085, 0.035));
    float smoothWarmMid = skinHue * Dalashade_RangeMask(s.Luma, 0.22, 0.78) * Dalashade_RangeMask(s.Saturation, 0.08, 0.46) * s.Smoothness;
    float foregroundBias = saturate(1.0 - smoothstep(0.35, 0.90, s.Depth));
    return saturate(sceneSkinProtection) * saturate(smoothWarmMid * foregroundBias);
}

float Dalashade_GetVoidDarknessMask(float3 color, float2 uv, float sceneVoidDarkness)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv);
    float voidHue = max(Dalashade_HueMask(s.Hue, 0.70, 0.12), Dalashade_HueMask(s.Hue, 0.82, 0.12));
    float darkColor = saturate(1.0 - smoothstep(0.18, 0.52, s.Luma)) * smoothstep(0.18, 0.75, s.Saturation);
    return saturate(sceneVoidDarkness) * saturate(voidHue * darkColor * (0.65 + 0.35 * s.Smoothness));
}

Dalashade_MaterialMasks Dalashade_GetAllMaterialMasks(
    float3 color,
    float2 uv,
    float sceneFoliage,
    float sceneWaterSpecular,
    float sceneSandDust,
    float sceneSnowIce,
    float sceneStoneRuins,
    float sceneMetalIndustrial,
    float sceneCrystalAether,
    float sceneNeonGlass,
    float sceneFireLavaHeat,
    float sceneSkyCloudFog,
    float sceneSkinProtection,
    float sceneVoidDarkness)
{
    Dalashade_MaterialMasks masks;
    masks.FoliageStrong = Dalashade_GetFoliageStrongMask(color, uv, sceneFoliage);
    masks.OrganicGreenSurface = Dalashade_GetOrganicGreenSurfaceMask(color, uv, sceneFoliage);
    masks.WaterSpecular = Dalashade_GetWaterSpecularMask(color, uv, sceneWaterSpecular);
    masks.SandDust = Dalashade_GetSandDustMask(color, uv, sceneSandDust);
    masks.SnowIce = Dalashade_GetSnowIceMask(color, uv, sceneSnowIce);
    masks.StoneRuins = Dalashade_GetStoneRuinsMask(color, uv, sceneStoneRuins);
    masks.MetalIndustrial = Dalashade_GetMetalIndustrialMask(color, uv, sceneMetalIndustrial);
    masks.CrystalAether = Dalashade_GetCrystalAetherMask(color, uv, sceneCrystalAether);
    masks.NeonGlass = Dalashade_GetNeonGlassMask(color, uv, sceneNeonGlass);
    masks.FireLavaHeat = Dalashade_GetFireLavaHeatMask(color, uv, sceneFireLavaHeat);
    masks.SkyCloudFog = Dalashade_GetSkyFogMask(color, uv, sceneSkyCloudFog);
    masks.SkinProtection = Dalashade_GetSkinProtectionMask(color, uv, sceneSkinProtection);
    masks.VoidDarkness = Dalashade_GetVoidDarknessMask(color, uv, sceneVoidDarkness);

    // Conflict suppression separates strong material matches from weak influence.
    masks.SkyCloudFog *= saturate(1.0 - masks.FoliageStrong * 0.75 - masks.StoneRuins * 0.55 - masks.MetalIndustrial * 0.35 - masks.WaterSpecular * 0.45);
    masks.FoliageStrong *= saturate(1.0 - masks.SkyCloudFog * 0.85 - masks.StoneRuins * 0.35);
    masks.WaterSpecular *= saturate(1.0 - masks.SkyCloudFog * 0.50);
    masks.SandDust *= saturate(1.0 - masks.FireLavaHeat * 0.65 - masks.SkinProtection * 0.45 - masks.SkyCloudFog * 0.60);
    masks.SnowIce *= saturate(1.0 - masks.SkyCloudFog * 0.55);
    masks.CrystalAether *= saturate(1.0 - masks.WaterSpecular * 0.35 - masks.NeonGlass * 0.30);

    masks.CombinedConfidence = saturate(
        masks.FoliageStrong + masks.WaterSpecular + masks.SandDust + masks.SnowIce +
        masks.StoneRuins + masks.MetalIndustrial + masks.CrystalAether + masks.NeonGlass +
        masks.FireLavaHeat + masks.SkyCloudFog + masks.SkinProtection + masks.VoidDarkness);

    return masks;
}

float3 Dalashade_GetMaterialOverviewColor(Dalashade_MaterialMasks masks)
{
    float3 color = float3(0.0, 0.0, 0.0);
    color += masks.FoliageStrong * float3(0.05, 0.95, 0.16);
    color += masks.OrganicGreenSurface * float3(0.28, 0.48, 0.12);
    color += masks.WaterSpecular * float3(0.00, 0.85, 1.00);
    color += masks.SandDust * float3(1.00, 0.66, 0.12);
    color += masks.SnowIce * float3(0.78, 0.92, 1.00);
    color += masks.SkyCloudFog * float3(0.18, 0.42, 1.00);
    color += masks.StoneRuins * float3(0.42, 0.40, 0.34);
    color += masks.MetalIndustrial * float3(0.45, 0.56, 0.66);
    color += masks.CrystalAether * float3(0.55, 0.22, 1.00);
    color += masks.NeonGlass * float3(1.00, 0.00, 0.85);
    color += masks.FireLavaHeat * float3(1.00, 0.18, 0.04);
    color += masks.SkinProtection * float3(1.00, 0.54, 0.42);
    color += masks.VoidDarkness * float3(0.38, 0.05, 0.75);
    color = lerp(color, float3(1.0, 1.0, 1.0), smoothstep(0.78, 1.0, masks.CombinedConfidence) * 0.25);
    return saturate(color);
}

#endif
