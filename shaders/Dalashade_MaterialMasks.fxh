#ifndef DALASHADE_MATERIAL_MASKS_FXH
#define DALASHADE_MATERIAL_MASKS_FXH

// Shared screen-space helpers for Dalashade material debug views.
// These are heuristic masks, not real FFXIV material IDs.

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

float Dalashade_GetFoliageMask(float3 color, float2 uv, float sceneFoliage)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float green = max(Dalashade_HueMask(hue, 0.30, 0.13), Dalashade_HueMask(hue, 0.23, 0.09));
    float organicLuma = saturate(1.0 - smoothstep(0.78, 1.0, luma)) * saturate(smoothstep(0.05, 0.18, luma));
    float localTexture = saturate(Dalashade_GetEdgeStrength(uv) * 1.8 + 0.15);
    float skyReject = saturate(1.0 - Dalashade_GetSmoothness(uv) * smoothstep(0.45, 0.95, luma));
    return saturate(sceneFoliage) * saturate(green * sat * organicLuma * localTexture * (0.55 + 0.45 * skyReject));
}

float Dalashade_GetWaterSpecularMask(float3 color, float2 uv, float sceneWaterSpecular)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float cyanBlue = max(Dalashade_HueMask(hue, 0.52, 0.12), Dalashade_HueMask(hue, 0.60, 0.12));
    float glint = smoothstep(0.62, 0.96, luma) * saturate(Dalashade_GetEdgeStrength(uv) * 1.4 + sat * 0.6);
    float skyReject = saturate(1.0 - Dalashade_GetSmoothness(uv) * smoothstep(0.55, 0.95, luma) * smoothstep(0.35, 0.9, Dalashade_GetDepthFactor(uv)));
    return saturate(sceneWaterSpecular) * saturate((cyanBlue * sat * 0.55 + glint * 0.65) * (0.35 + 0.65 * skyReject));
}

float Dalashade_GetSandDustMask(float3 color, float2 uv, float sceneSandDust)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float warmGround = max(Dalashade_HueMask(hue, 0.10, 0.09), Dalashade_HueMask(hue, 0.14, 0.08));
    float sandTone = smoothstep(0.24, 0.72, luma) * saturate(1.0 - smoothstep(0.82, 1.0, luma)) * smoothstep(0.08, 0.34, sat);
    float fireReject = saturate(1.0 - Dalashade_HueMask(hue, 0.04, 0.05) * smoothstep(0.55, 1.0, sat) * smoothstep(0.55, 1.0, luma));
    float skinReject = saturate(1.0 - Dalashade_HueMask(hue, 0.055, 0.045) * Dalashade_RangeMask(luma, 0.25, 0.72) * smoothstep(0.10, 0.36, sat));
    return saturate(sceneSandDust) * saturate(warmGround * sandTone * fireReject * skinReject * (0.75 + 0.25 * Dalashade_GetEdgeStrength(uv)));
}

float Dalashade_GetSnowIceMask(float3 color, float2 uv, float sceneSnowIce)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float cold = max(Dalashade_HueMask(hue, 0.56, 0.18), 1.0 - sat);
    float brightColdSurface = smoothstep(0.48, 0.92, luma) * saturate(1.0 - smoothstep(0.42, 0.78, sat)) * cold;
    float cloudReject = saturate(1.0 - Dalashade_GetSmoothness(uv) * smoothstep(0.60, 0.98, luma) * smoothstep(0.45, 1.0, Dalashade_GetDepthFactor(uv)));
    return saturate(sceneSnowIce) * saturate(brightColdSurface * (0.45 + 0.55 * cloudReject));
}

float Dalashade_GetSkyFogMask(float3 color, float2 uv, float sceneSkyCloudFog)
{
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float smoothness = Dalashade_GetSmoothness(uv);
    float farDepth = smoothstep(0.32, 0.96, Dalashade_GetDepthFactor(uv));
    float atmosphericColor = max(Dalashade_HueMask(Dalashade_GetHue(color), 0.58, 0.18), saturate(1.0 - sat));
    float smoothAtmosphere = smoothness * saturate(0.35 + farDepth * 0.65) * saturate(0.45 + atmosphericColor * 0.55);
    return saturate(sceneSkyCloudFog) * saturate(smoothAtmosphere * smoothstep(0.12, 0.82, luma));
}

float Dalashade_GetMetalIndustrialMask(float3 color, float2 uv, float sceneMetalIndustrial)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float coolNeutral = max(Dalashade_HueMask(hue, 0.58, 0.16), saturate(1.0 - sat));
    float hardEdge = smoothstep(0.10, 0.45, Dalashade_GetEdgeStrength(uv));
    float midTone = Dalashade_RangeMask(luma, 0.12, 0.78);
    return saturate(sceneMetalIndustrial) * saturate(coolNeutral * saturate(1.0 - sat * 0.55) * hardEdge * midTone);
}

float Dalashade_GetCrystalAetherMask(float3 color, float2 uv, float sceneCrystalAether)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float aetherHue = max(max(Dalashade_HueMask(hue, 0.52, 0.09), Dalashade_HueMask(hue, 0.68, 0.11)), Dalashade_HueMask(hue, 0.82, 0.11));
    float luminous = smoothstep(0.34, 0.92, luma) * smoothstep(0.28, 0.85, sat);
    return saturate(sceneCrystalAether) * saturate(aetherHue * luminous * (0.65 + 0.35 * Dalashade_GetEdgeStrength(uv)));
}

float Dalashade_GetNeonGlassMask(float3 color, float2 uv, float sceneNeonGlass)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float neonHue = max(max(Dalashade_HueMask(hue, 0.50, 0.07), Dalashade_HueMask(hue, 0.83, 0.08)), Dalashade_HueMask(hue, 0.16, 0.06));
    float brightStrip = smoothstep(0.48, 0.95, luma) * smoothstep(0.45, 0.95, sat) * smoothstep(0.08, 0.38, Dalashade_GetEdgeStrength(uv));
    return saturate(sceneNeonGlass) * saturate(neonHue * brightStrip);
}

float Dalashade_GetFireLavaHeatMask(float3 color, float2 uv, float sceneFireLavaHeat)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float fireHue = max(Dalashade_HueMask(hue, 0.03, 0.06), Dalashade_HueMask(hue, 0.10, 0.07));
    float luminousWarm = smoothstep(0.42, 0.95, luma) * smoothstep(0.38, 0.95, sat);
    return saturate(sceneFireLavaHeat) * saturate(fireHue * luminousWarm * (0.75 + 0.25 * Dalashade_GetEdgeStrength(uv)));
}

float Dalashade_GetSkinProtectionMask(float3 color, float2 uv, float sceneSkinProtection)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float skinHue = max(Dalashade_HueMask(hue, 0.055, 0.040), Dalashade_HueMask(hue, 0.085, 0.035));
    float smoothWarmMid = skinHue * Dalashade_RangeMask(luma, 0.22, 0.78) * Dalashade_RangeMask(sat, 0.08, 0.46) * Dalashade_GetSmoothness(uv);
    float foregroundBias = saturate(1.0 - smoothstep(0.35, 0.90, Dalashade_GetDepthFactor(uv)));
    return saturate(sceneSkinProtection) * saturate(smoothWarmMid * foregroundBias);
}

float Dalashade_GetVoidDarknessMask(float3 color, float2 uv, float sceneVoidDarkness)
{
    float hue = Dalashade_GetHue(color);
    float luma = Dalashade_GetLuminance(color);
    float sat = Dalashade_GetSaturation(color);
    float voidHue = max(Dalashade_HueMask(hue, 0.70, 0.12), Dalashade_HueMask(hue, 0.82, 0.12));
    float darkColor = saturate(1.0 - smoothstep(0.18, 0.52, luma)) * smoothstep(0.18, 0.75, sat);
    return saturate(sceneVoidDarkness) * saturate(voidHue * darkColor * (0.65 + 0.35 * Dalashade_GetSmoothness(uv)));
}

float3 Dalashade_GetMaterialOverviewColor(
    float foliageMask,
    float waterSpecularMask,
    float sandDustMask,
    float snowIceMask,
    float skyCloudFogMask,
    float metalIndustrialMask,
    float crystalAetherMask,
    float neonGlassMask,
    float fireLavaHeatMask,
    float skinProtectionMask,
    float voidDarknessMask)
{
    float3 color = float3(0.0, 0.0, 0.0);
    color += foliageMask * float3(0.08, 0.95, 0.18);
    color += waterSpecularMask * float3(0.00, 0.85, 1.00);
    color += sandDustMask * float3(1.00, 0.66, 0.12);
    color += snowIceMask * float3(0.78, 0.92, 1.00);
    color += skyCloudFogMask * float3(0.18, 0.42, 1.00);
    color += metalIndustrialMask * float3(0.45, 0.56, 0.66);
    color += crystalAetherMask * float3(0.55, 0.22, 1.00);
    color += neonGlassMask * float3(1.00, 0.00, 0.85);
    color += fireLavaHeatMask * float3(1.00, 0.18, 0.04);
    color += skinProtectionMask * float3(1.00, 0.54, 0.42);
    color += voidDarknessMask * float3(0.38, 0.05, 0.75);

    float combined = saturate(
        foliageMask + waterSpecularMask + sandDustMask + snowIceMask + skyCloudFogMask +
        metalIndustrialMask + crystalAetherMask + neonGlassMask + fireLavaHeatMask +
        skinProtectionMask + voidDarknessMask);
    color = lerp(color, float3(1.0, 1.0, 1.0), smoothstep(0.72, 1.0, combined) * 0.35);
    return saturate(color);
}

#endif
