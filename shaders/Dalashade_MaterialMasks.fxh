#ifndef DALASHADE_MATERIAL_MASKS_FXH
#define DALASHADE_MATERIAL_MASKS_FXH

// Shared screen-space helpers for Dalashade material debug views.
// These functions infer likely material influence from local color, contrast,
// smoothness, screen position, scene-level MaterialIntent gates, and optional
// depth assistance. They are not true FFXIV material IDs.

struct Dalashade_MaterialSignals
{
    float3 Color;
    float2 Uv;
    float Luma;
    float Saturation;
    float Hue;
    float Edge;
    float Detail;
    float Smoothness;
    float Depth;
    float FarDepth;
    float UpperScreen;
    float LowerScreen;
    float CenterScreen;
    float DepthAssistEnabled;
    float DepthAssistStrength;
    float DepthConfidence;
    float DepthFarConfidence;
    float DepthInvalidConfidence;
    float DepthNearConfidence;
};

struct Dalashade_RawMaterialCandidates
{
    float FoliageStrong;
    float OrganicGreenSurface;
    float HardSurfaceGreenReject;
    float SurfaceHardTexture;
    float WaterPlane;
    float SpecularGlint;
    float WaterSpecular;
    float SandDust;
    float SnowIce;
    float StoneRuins;
    float MetalIndustrial;
    float CrystalAether;
    float NeonGlass;
    float FireLavaHeat;
    float SkyOpen;
    float CloudBright;
    float FogAtmosphere;
    float NightSky;
    float SkyGapThroughCanopy;
    float SmoothAtmosphere;
    float SkyCloudFog;
    float SkinProtection;
    float VoidDarkness;
};

struct Dalashade_GatedMaterialCandidates
{
    float FoliageStrong;
    float OrganicGreenSurface;
    float WaterPlane;
    float SpecularGlint;
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
};

struct Dalashade_FinalMaterialMasks
{
    float FoliageStrong;
    float OrganicGreenSurface;
    float WaterPlane;
    float SpecularGlint;
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

struct Dalashade_WaterResolve
{
    float RawCyanWater;
    float RawDeepWater;
    float ShallowWater;
    float DeepWater;
    float WaterHorizon;
    float WetShoreline;
    float FoamOrEdge;
    float WaterSurface;
    float WaterReceiver;
    float WaterSource;
    float SkySource;
    float SkyReject;
    float SandReject;
    float SkinReject;
    float WaterCoherence;
    float Confidence;
};

// Back-compat alias for first-party shaders written before the v2 struct split.
#define Dalashade_MaterialMasks Dalashade_FinalMaterialMasks

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

float Dalashade_GetDetailStrength(float2 uv)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float center = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv).rgb);
    float diagonalA = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv + float2(texel.x, texel.y)).rgb);
    float diagonalB = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv + float2(-texel.x, texel.y)).rgb);
    float diagonalC = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv + float2(texel.x, -texel.y)).rgb);
    float diagonalD = Dalashade_GetLuminance(tex2D(ReShade::BackBuffer, uv + float2(-texel.x, -texel.y)).rgb);
    return saturate((abs(center - diagonalA) + abs(center - diagonalB) + abs(center - diagonalC) + abs(center - diagonalD)) * 2.0);
}

float Dalashade_GetSmoothness(float2 uv)
{
    return saturate(1.0 - max(Dalashade_GetEdgeStrength(uv), Dalashade_GetDetailStrength(uv)) * 2.0);
}

float Dalashade_GetDepthFactor(float2 uv)
{
    return saturate(ReShade::GetLinearizedDepth(uv));
}

Dalashade_MaterialSignals Dalashade_GetMaterialSignals(float3 color, float2 uv, float enableDepthAssist, float depthAssistStrength, float depthAssistConfidenceFloor)
{
    Dalashade_MaterialSignals signals;
    signals.Color = saturate(color);
    signals.Uv = uv;
    signals.Luma = Dalashade_GetLuminance(signals.Color);
    signals.Saturation = Dalashade_GetSaturation(signals.Color);
    signals.Hue = Dalashade_GetHue(signals.Color);
    signals.Edge = Dalashade_GetEdgeStrength(uv);
    signals.Detail = Dalashade_GetDetailStrength(uv);
    signals.Smoothness = saturate(1.0 - max(signals.Edge, signals.Detail) * 2.0);
    signals.Depth = Dalashade_GetDepthFactor(uv);
    signals.FarDepth = smoothstep(0.38, 0.96, signals.Depth);
    signals.UpperScreen = 1.0 - smoothstep(0.12, 0.72, uv.y);
    signals.LowerScreen = smoothstep(0.42, 0.96, uv.y);
    signals.CenterScreen = (1.0 - smoothstep(0.20, 0.54, abs(uv.x - 0.5))) * (1.0 - smoothstep(0.16, 0.52, abs(uv.y - 0.5)));
    signals.DepthAssistEnabled = saturate(enableDepthAssist);
    signals.DepthAssistStrength = saturate(depthAssistStrength) * signals.DepthAssistEnabled;

    // ReShade depth can be unavailable, inverted, flat, or disabled. Treat it as
    // optional evidence only; disabled or unreliable depth never collapses masks.
    float validDepth = step(0.00001, signals.Depth) * step(signals.Depth, 0.99999);
    signals.DepthConfidence = signals.DepthAssistStrength * max(saturate(depthAssistConfidenceFloor), validDepth);
    signals.DepthFarConfidence = signals.DepthConfidence * smoothstep(0.42, 0.96, signals.Depth);
    signals.DepthNearConfidence = signals.DepthConfidence * (1.0 - smoothstep(0.08, 0.55, signals.Depth));
    signals.DepthInvalidConfidence = signals.DepthAssistStrength * (1.0 - validDepth);
    return signals;
}

Dalashade_MaterialSignals Dalashade_GetMaterialSignals(float3 color, float2 uv)
{
    return Dalashade_GetMaterialSignals(color, uv, 0.0, 0.0, 0.0);
}

Dalashade_RawMaterialCandidates Dalashade_GetRawMaterialCandidates(Dalashade_MaterialSignals s)
{
    Dalashade_RawMaterialCandidates raw;

    float green = max(Dalashade_HueMask(s.Hue, 0.30, 0.12), Dalashade_HueMask(s.Hue, 0.23, 0.08));
    float yellowGreen = max(green, Dalashade_HueMask(s.Hue, 0.20, 0.07));
    float blueCyan = max(Dalashade_HueMask(s.Hue, 0.52, 0.11), Dalashade_HueMask(s.Hue, 0.60, 0.12));
    float warmBeige = max(Dalashade_HueMask(s.Hue, 0.10, 0.09), Dalashade_HueMask(s.Hue, 0.14, 0.08));
    float fireHue = max(Dalashade_HueMask(s.Hue, 0.03, 0.06), Dalashade_HueMask(s.Hue, 0.10, 0.06));
    float skinHue = max(Dalashade_HueMask(s.Hue, 0.055, 0.040), Dalashade_HueMask(s.Hue, 0.085, 0.035));
    float skyBlue = max(Dalashade_HueMask(s.Hue, 0.55, 0.16), Dalashade_HueMask(s.Hue, 0.60, 0.13));
    float warmSkyHue = max(Dalashade_HueMask(s.Hue, 0.04, 0.07), Dalashade_HueMask(s.Hue, 0.12, 0.08));
    float coldBright = max(Dalashade_HueMask(s.Hue, 0.56, 0.18), saturate(1.0 - s.Saturation));

    float edgeTexture = max(s.Edge, s.Detail);
    float leafTone = Dalashade_RangeMask(s.Luma, 0.07, 0.78) * smoothstep(0.16, 0.54, s.Saturation);
    float fineOrganicTexture = smoothstep(0.045, 0.26, edgeTexture) * saturate(1.0 - s.Smoothness * smoothstep(0.46, 0.88, s.Luma));
    float smoothSkyLike = s.Smoothness * smoothstep(0.30, 0.96, s.Luma) * saturate(0.30 + s.UpperScreen * 0.55 + s.DepthFarConfidence * 0.35 + s.DepthInvalidConfidence * 0.25);
    raw.FoliageStrong = saturate(green * leafTone * fineOrganicTexture * (1.0 - smoothSkyLike * 0.82));

    float organicTone = Dalashade_RangeMask(s.Luma, 0.05, 0.72) * smoothstep(0.08, 0.38, s.Saturation);
    raw.OrganicGreenSurface = saturate(yellowGreen * organicTone * smoothstep(0.10, 0.44, edgeTexture) * (1.0 - raw.FoliageStrong * 0.38) * (1.0 - smoothSkyLike * 0.70));
    raw.SurfaceHardTexture = saturate(smoothstep(0.12, 0.46, edgeTexture) * Dalashade_RangeMask(s.Luma, 0.07, 0.82) * (1.0 - smoothSkyLike * 0.72));
    raw.HardSurfaceGreenReject = saturate(raw.OrganicGreenSurface * raw.SurfaceHardTexture * (1.0 - raw.FoliageStrong * 0.55) * saturate(1.0 - smoothstep(0.34, 0.72, s.Saturation)));

    float clearBlueSky = skyBlue * smoothstep(0.30, 0.88, s.Luma) * smoothstep(0.08, 0.54, s.Saturation) * smoothstep(0.42, 0.92, s.Smoothness);
    float brightCloud = smoothstep(0.58, 0.98, s.Luma) * saturate(1.0 - smoothstep(0.30, 0.72, s.Saturation)) * smoothstep(0.44, 0.94, s.Smoothness);
    float warmDawnDusk = warmSkyHue * smoothstep(0.34, 0.91, s.Luma) * smoothstep(0.08, 0.46, s.Saturation) * smoothstep(0.38, 0.88, s.Smoothness);
    float grayAtmosphere = saturate(1.0 - s.Saturation) * smoothstep(0.16, 0.88, s.Luma) * smoothstep(0.52, 0.96, s.Smoothness);
    float darkNightSky = smoothstep(0.70, 1.0, s.Smoothness) * saturate(1.0 - smoothstep(0.13, 0.38, s.Luma)) * saturate(0.25 + s.UpperScreen * 0.75);
    raw.SmoothAtmosphere = s.Smoothness * saturate(0.24 + s.UpperScreen * 0.36 + s.FarDepth * 0.18 + s.DepthFarConfidence * 0.28 + s.DepthInvalidConfidence * 0.18);
    raw.SkyOpen = saturate(max(clearBlueSky, warmDawnDusk) * saturate(0.48 + s.UpperScreen * 0.34 + s.DepthFarConfidence * 0.20 + s.DepthInvalidConfidence * 0.12));
    raw.CloudBright = saturate(brightCloud * saturate(0.42 + s.UpperScreen * 0.32 + s.DepthFarConfidence * 0.18 + s.DepthInvalidConfidence * 0.12));
    raw.FogAtmosphere = saturate(max(grayAtmosphere, raw.SmoothAtmosphere * 0.62) * (1.0 - raw.SurfaceHardTexture * 0.48));
    raw.NightSky = darkNightSky;
    raw.SkyGapThroughCanopy = saturate(raw.SmoothAtmosphere * s.UpperScreen * max(raw.SkyOpen, raw.CloudBright) * (1.0 - raw.FoliageStrong * 0.58));
    raw.SkyCloudFog = saturate(max(max(raw.SkyOpen, raw.CloudBright), max(raw.FogAtmosphere, max(raw.NightSky, raw.SkyGapThroughCanopy))) * (1.0 - raw.FoliageStrong * 0.58) * (1.0 - raw.HardSurfaceGreenReject * 0.45));

    float fireReject = fireHue * smoothstep(0.55, 1.0, s.Saturation) * smoothstep(0.55, 1.0, s.Luma);
    float skinReject = skinHue * Dalashade_RangeMask(s.Luma, 0.25, 0.72) * smoothstep(0.10, 0.36, s.Saturation) * s.Smoothness;

    float lowerWaterPrior = 0.35 + s.LowerScreen * 0.42 + (1.0 - s.UpperScreen) * 0.18;
    float broadWaterSurface = saturate(s.Smoothness * 0.74 + (1.0 - smoothstep(0.11, 0.38, edgeTexture)) * 0.26);
    float waterTone = blueCyan * smoothstep(0.08, 0.58, s.Saturation) * Dalashade_RangeMask(s.Luma, 0.06, 0.88);
    float warmSurfaceReject = max(warmBeige * smoothstep(0.08, 0.44, s.Saturation), skinReject);
    raw.WaterPlane = saturate(waterTone * broadWaterSurface * lowerWaterPrior
        * (1.0 - raw.SkyCloudFog * (0.58 + s.UpperScreen * 0.28 + s.DepthFarConfidence * 0.18))
        * (1.0 - raw.SurfaceHardTexture * 0.42)
        * (1.0 - warmSurfaceReject * 0.58));

    float glintStructure = smoothstep(0.70, 0.98, s.Luma) * smoothstep(0.035, 0.22, edgeTexture) * (1.0 - s.Smoothness * 0.50);
    float glintHue = saturate(max(blueCyan, saturate(1.0 - s.Saturation) * 0.45) + max(s.Color.r, max(s.Color.g, s.Color.b)) * 0.10);
    raw.SpecularGlint = saturate(glintStructure * glintHue
        * (1.0 - raw.SkyCloudFog * (0.36 + s.UpperScreen * 0.30))
        * (1.0 - skinReject * 0.48)
        * (1.0 - fireReject * 0.30));
    raw.WaterSpecular = saturate(max(raw.WaterPlane, raw.SpecularGlint * 0.88));

    float sandTone = smoothstep(0.24, 0.74, s.Luma) * saturate(1.0 - smoothstep(0.82, 1.0, s.Luma)) * smoothstep(0.08, 0.34, s.Saturation);
    raw.SandDust = saturate(warmBeige * sandTone * (0.58 + s.LowerScreen * 0.34 + s.CenterScreen * 0.08) * (1.0 - raw.SkyCloudFog * 0.70) * (1.0 - fireReject * 0.82) * (1.0 - skinReject * 0.62) * (0.72 + 0.28 * edgeTexture));

    float brightColdSurface = smoothstep(0.48, 0.93, s.Luma) * saturate(1.0 - smoothstep(0.42, 0.78, s.Saturation)) * coldBright;
    float surfaceTexture = smoothstep(0.035, 0.24, edgeTexture);
    float cloudReject = raw.CloudBright * (0.56 + s.UpperScreen * 0.26 + s.DepthFarConfidence * 0.18);
    raw.SnowIce = saturate(brightColdSurface * (0.34 + 0.66 * surfaceTexture) * (0.55 + s.LowerScreen * 0.32 + s.CenterScreen * 0.12 + s.DepthNearConfidence * 0.08) * (1.0 - cloudReject * 0.72));

    float neutralStone = saturate(1.0 - s.Saturation * 0.86) * Dalashade_RangeMask(s.Luma, 0.10, 0.74);
    raw.StoneRuins = saturate(neutralStone * raw.SurfaceHardTexture * (1.0 - raw.SkyCloudFog * 0.82) * (1.0 - raw.SnowIce * 0.24));

    float coolNeutral = max(Dalashade_HueMask(s.Hue, 0.58, 0.16), saturate(1.0 - s.Saturation));
    raw.MetalIndustrial = saturate(coolNeutral * saturate(1.0 - s.Saturation * 0.55) * raw.SurfaceHardTexture * Dalashade_RangeMask(s.Luma, 0.12, 0.80) * (1.0 - raw.WaterPlane * 0.36));

    float aetherHue = max(max(Dalashade_HueMask(s.Hue, 0.52, 0.09), Dalashade_HueMask(s.Hue, 0.68, 0.11)), Dalashade_HueMask(s.Hue, 0.82, 0.11));
    raw.CrystalAether = saturate(aetherHue * smoothstep(0.34, 0.92, s.Luma) * smoothstep(0.28, 0.85, s.Saturation) * (0.62 + 0.38 * edgeTexture) * (1.0 - raw.WaterPlane * 0.38));

    float neonHue = max(max(Dalashade_HueMask(s.Hue, 0.50, 0.07), Dalashade_HueMask(s.Hue, 0.83, 0.08)), Dalashade_HueMask(s.Hue, 0.16, 0.06));
    raw.NeonGlass = saturate(neonHue * smoothstep(0.48, 0.95, s.Luma) * smoothstep(0.45, 0.95, s.Saturation) * smoothstep(0.08, 0.38, edgeTexture));

    raw.FireLavaHeat = saturate(fireHue * smoothstep(0.42, 0.95, s.Luma) * smoothstep(0.38, 0.95, s.Saturation) * (0.75 + 0.25 * edgeTexture) * (1.0 - skinReject * 0.40));
    raw.SkinProtection = saturate(skinHue * Dalashade_RangeMask(s.Luma, 0.22, 0.78) * Dalashade_RangeMask(s.Saturation, 0.08, 0.46) * s.Smoothness * saturate(0.35 + s.CenterScreen * 0.35 + s.DepthNearConfidence * 0.30));
    raw.VoidDarkness = saturate(max(Dalashade_HueMask(s.Hue, 0.70, 0.12), Dalashade_HueMask(s.Hue, 0.82, 0.12)) * saturate(1.0 - smoothstep(0.18, 0.52, s.Luma)) * smoothstep(0.18, 0.75, s.Saturation) * (0.65 + 0.35 * s.Smoothness));

    return raw;
}

Dalashade_GatedMaterialCandidates Dalashade_GetGatedMaterialCandidatesWithWaterSplit(
    Dalashade_RawMaterialCandidates raw,
    float sceneFoliage,
    float sceneWaterSpecular,
    float sceneWaterPlane,
    float sceneSpecularGlint,
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
    Dalashade_GatedMaterialCandidates gated;
    float waterPlaneGate = saturate(max(sceneWaterSpecular, sceneWaterPlane));
    float specularGlintGate = saturate(max(sceneWaterSpecular, sceneSpecularGlint));
    float combinedWaterGate = saturate(max(sceneWaterSpecular, max(sceneWaterPlane, sceneSpecularGlint)));
    gated.FoliageStrong = raw.FoliageStrong * saturate(sceneFoliage);
    gated.OrganicGreenSurface = raw.OrganicGreenSurface * saturate(sceneFoliage);
    gated.WaterPlane = raw.WaterPlane * waterPlaneGate;
    gated.SpecularGlint = raw.SpecularGlint * specularGlintGate;
    gated.WaterSpecular = raw.WaterSpecular * combinedWaterGate;
    gated.SandDust = raw.SandDust * saturate(sceneSandDust);
    gated.SnowIce = raw.SnowIce * saturate(sceneSnowIce);
    gated.StoneRuins = raw.StoneRuins * saturate(sceneStoneRuins);
    gated.MetalIndustrial = raw.MetalIndustrial * saturate(sceneMetalIndustrial);
    gated.CrystalAether = raw.CrystalAether * saturate(sceneCrystalAether);
    gated.NeonGlass = raw.NeonGlass * saturate(sceneNeonGlass);
    gated.FireLavaHeat = raw.FireLavaHeat * saturate(sceneFireLavaHeat);
    gated.SkyCloudFog = raw.SkyCloudFog * saturate(sceneSkyCloudFog);
    gated.SkinProtection = raw.SkinProtection * saturate(sceneSkinProtection);
    gated.VoidDarkness = raw.VoidDarkness * saturate(sceneVoidDarkness);
    return gated;
}

Dalashade_GatedMaterialCandidates Dalashade_GetGatedMaterialCandidates(
    Dalashade_RawMaterialCandidates raw,
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
    return Dalashade_GetGatedMaterialCandidatesWithWaterSplit(
        raw,
        sceneFoliage,
        sceneWaterSpecular,
        sceneWaterSpecular,
        sceneWaterSpecular,
        sceneSandDust,
        sceneSnowIce,
        sceneStoneRuins,
        sceneMetalIndustrial,
        sceneCrystalAether,
        sceneNeonGlass,
        sceneFireLavaHeat,
        sceneSkyCloudFog,
        sceneSkinProtection,
        sceneVoidDarkness);
}

Dalashade_FinalMaterialMasks Dalashade_ResolveFinalMaterialMasks(Dalashade_MaterialSignals s, Dalashade_RawMaterialCandidates raw, Dalashade_GatedMaterialCandidates gated)
{
    Dalashade_FinalMaterialMasks masks;
    masks.FoliageStrong = gated.FoliageStrong;
    masks.OrganicGreenSurface = gated.OrganicGreenSurface;
    masks.WaterPlane = gated.WaterPlane;
    masks.SpecularGlint = gated.SpecularGlint;
    masks.WaterSpecular = gated.WaterSpecular;
    masks.SandDust = gated.SandDust;
    masks.SnowIce = gated.SnowIce;
    masks.StoneRuins = gated.StoneRuins;
    masks.MetalIndustrial = gated.MetalIndustrial;
    masks.CrystalAether = gated.CrystalAether;
    masks.NeonGlass = gated.NeonGlass;
    masks.FireLavaHeat = gated.FireLavaHeat;
    masks.SkyCloudFog = gated.SkyCloudFog;
    masks.SkinProtection = gated.SkinProtection;
    masks.VoidDarkness = gated.VoidDarkness;

    float depthSkyBoost = 1.0 + s.DepthFarConfidence * raw.SmoothAtmosphere * 0.24 + s.DepthInvalidConfidence * raw.SmoothAtmosphere * s.UpperScreen * 0.16;
    masks.SkyCloudFog *= depthSkyBoost;
    masks.SkyCloudFog *= saturate(1.0 - masks.FoliageStrong * 0.72 - masks.StoneRuins * 0.55 - masks.MetalIndustrial * 0.35 - masks.WaterPlane * 0.45 - masks.SpecularGlint * 0.18);
    masks.FoliageStrong *= saturate(1.0 - masks.SkyCloudFog * 0.85 - masks.StoneRuins * 0.42 - raw.HardSurfaceGreenReject * 0.38);
    masks.OrganicGreenSurface *= saturate(1.0 - masks.SkyCloudFog * 0.68);
    masks.WaterPlane *= saturate(1.0 - masks.SkyCloudFog * (0.58 + s.UpperScreen * 0.26 + s.DepthFarConfidence * 0.20));
    masks.SpecularGlint *= saturate(1.0 - masks.SkyCloudFog * (0.34 + s.UpperScreen * 0.20));
    masks.SandDust *= saturate(1.0 - masks.FireLavaHeat * 0.65 - masks.SkinProtection * 0.45 - masks.SkyCloudFog * 0.60);
    masks.SnowIce *= saturate(1.0 - masks.SkyCloudFog * (0.50 + s.UpperScreen * 0.25 + s.DepthFarConfidence * 0.15));
    masks.WaterSpecular = saturate(max(masks.WaterPlane, masks.SpecularGlint * 0.88));
    masks.CrystalAether *= saturate(1.0 - masks.WaterPlane * 0.35 - masks.SpecularGlint * 0.16 - masks.NeonGlass * 0.30);
    masks.VoidDarkness *= saturate(1.0 - masks.SkyCloudFog * 0.24);

    masks.CombinedConfidence = saturate(
        masks.FoliageStrong + masks.OrganicGreenSurface * 0.45 + masks.WaterPlane + masks.SpecularGlint * 0.55 + masks.SandDust + masks.SnowIce +
        masks.StoneRuins + masks.MetalIndustrial + masks.CrystalAether + masks.NeonGlass +
        masks.FireLavaHeat + masks.SkyCloudFog + masks.SkinProtection + masks.VoidDarkness);

    return masks;
}

float Dalashade_GetWaterCoherence(Dalashade_MaterialSignals s)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float3 cA = tex2D(ReShade::BackBuffer, s.Uv + float2(texel.x * 2.0, 0.0)).rgb;
    float3 cB = tex2D(ReShade::BackBuffer, s.Uv - float2(texel.x * 2.0, 0.0)).rgb;
    float3 cC = tex2D(ReShade::BackBuffer, s.Uv + float2(0.0, texel.y * 2.0)).rgb;
    float3 cD = tex2D(ReShade::BackBuffer, s.Uv - float2(0.0, texel.y * 2.0)).rgb;

    float lumaDelta = abs(s.Luma - Dalashade_GetLuminance(cA))
        + abs(s.Luma - Dalashade_GetLuminance(cB))
        + abs(s.Luma - Dalashade_GetLuminance(cC))
        + abs(s.Luma - Dalashade_GetLuminance(cD));
    float satDelta = abs(s.Saturation - Dalashade_GetSaturation(cA))
        + abs(s.Saturation - Dalashade_GetSaturation(cB))
        + abs(s.Saturation - Dalashade_GetSaturation(cC))
        + abs(s.Saturation - Dalashade_GetSaturation(cD));
    float hueDelta = Dalashade_GetHueDistance(s.Hue, Dalashade_GetHue(cA))
        + Dalashade_GetHueDistance(s.Hue, Dalashade_GetHue(cB))
        + Dalashade_GetHueDistance(s.Hue, Dalashade_GetHue(cC))
        + Dalashade_GetHueDistance(s.Hue, Dalashade_GetHue(cD));

    float colorCoherence = saturate(1.0 - lumaDelta * 1.75 - satDelta * 0.55 - hueDelta * 0.72);
    float lowTexture = saturate(s.Smoothness * 0.68 + (1.0 - max(s.Edge, s.Detail)) * 0.32);
    return saturate(colorCoherence * lowTexture);
}

Dalashade_WaterResolve Dalashade_ResolveWater(
    float3 color,
    float2 uv,
    float waterContext,
    float coastalContext,
    float openOceanContext,
    float shallowWaterContext,
    float wetSurfaceContext,
    float materialWaterPlane,
    float materialSpecularGlint,
    float materialSandDust,
    float materialSkyCloudFog,
    float materialSkinProtection,
    float depthAssistEnabled,
    float depthAssistStrength,
    float depthConfidenceFloor)
{
    Dalashade_MaterialSignals s = Dalashade_GetMaterialSignals(color, uv, depthAssistEnabled, depthAssistStrength, depthConfidenceFloor);
    Dalashade_RawMaterialCandidates raw = Dalashade_GetRawMaterialCandidates(s);
    Dalashade_WaterResolve water;

    float blueCyan = max(Dalashade_HueMask(s.Hue, 0.52, 0.13), Dalashade_HueMask(s.Hue, 0.60, 0.14));
    float teal = max(blueCyan, Dalashade_HueMask(s.Hue, 0.46, 0.10));
    float warm = max(Dalashade_HueMask(s.Hue, 0.08, 0.09), Dalashade_HueMask(s.Hue, 0.14, 0.09));
    float fireLike = max(Dalashade_HueMask(s.Hue, 0.03, 0.06), Dalashade_HueMask(s.Hue, 0.10, 0.06)) * smoothstep(0.42, 1.0, s.Luma) * smoothstep(0.36, 0.95, s.Saturation);
    float skinLike = max(Dalashade_HueMask(s.Hue, 0.055, 0.040), Dalashade_HueMask(s.Hue, 0.085, 0.035)) * Dalashade_RangeMask(s.Luma, 0.22, 0.78) * Dalashade_RangeMask(s.Saturation, 0.08, 0.46);

    float sceneWater = saturate(max(waterContext, max(materialWaterPlane, raw.WaterPlane * max(waterContext, 0.28))));
    float sceneCoastal = saturate(max(coastalContext, sceneWater * 0.72));
    float sceneOpenOcean = saturate(max(openOceanContext, sceneCoastal * 0.62));
    float sceneShallow = saturate(max(shallowWaterContext, sceneCoastal * 0.54));
    float sceneWet = saturate(max(wetSurfaceContext, sceneWater * 0.22));
    float waterSceneSupport = saturate(max(sceneWater, max(sceneCoastal, max(sceneOpenOcean, sceneShallow))));

    water.WaterCoherence = Dalashade_GetWaterCoherence(s);
    float lowerOrHorizon = saturate(s.LowerScreen * 0.56 + (1.0 - s.UpperScreen) * 0.30 + s.CenterScreen * 0.10 + s.DepthFarConfidence * 0.10);
    water.SkyReject = saturate(raw.SkyCloudFog * max(materialSkyCloudFog, 0.36) * (0.72 + s.UpperScreen * 0.28) * (1.0 - waterSceneSupport * 0.28));
    water.SandReject = saturate(max(raw.SandDust, materialSandDust) * (0.52 + smoothstep(0.36, 0.88, s.Luma) * 0.34) + warm * smoothstep(0.12, 0.48, s.Saturation) * (1.0 - teal * 0.55));
    water.SkinReject = saturate(max(raw.SkinProtection, max(materialSkinProtection, skinLike)) * (0.72 + s.CenterScreen * 0.18));

    water.RawCyanWater = saturate(teal * smoothstep(0.12, 0.62, s.Saturation) * Dalashade_RangeMask(s.Luma, 0.05, 0.92)
        * (0.40 + water.WaterCoherence * 0.60)
        * (0.52 + lowerOrHorizon * 0.34 + waterSceneSupport * 0.22)
        * (1.0 - water.SkyReject * (0.38 + s.UpperScreen * 0.22))
        * (1.0 - water.SandReject * 0.58)
        * (1.0 - water.SkinReject * 0.82)
        * (1.0 - fireLike * 0.72));

    float deepWaterTone = teal * smoothstep(0.05, 0.34, s.Saturation) * saturate(1.0 - smoothstep(0.78, 1.0, s.Luma)) * smoothstep(0.38, 0.96, s.Smoothness);
    water.RawDeepWater = saturate(deepWaterTone
        * (0.30 + sceneWater * 0.32 + sceneOpenOcean * 0.38 + s.DepthFarConfidence * 0.16)
        * (1.0 - water.SkyReject * (0.46 + s.UpperScreen * 0.24))
        * (1.0 - water.SandReject * 0.48)
        * (1.0 - water.SkinReject * 0.80));

    water.ShallowWater = saturate(water.RawCyanWater * (0.42 + sceneShallow * 0.46 + sceneCoastal * 0.20) * (1.0 - water.SandReject * 0.36));
    water.DeepWater = saturate(water.RawDeepWater * (0.42 + sceneOpenOcean * 0.50 + sceneWater * 0.22));
    water.WaterHorizon = saturate(max(water.RawDeepWater, water.RawCyanWater * 0.55)
        * sceneOpenOcean
        * smoothstep(0.20, 0.82, s.Smoothness)
        * saturate(0.22 + s.UpperScreen * 0.22 + s.DepthFarConfidence * 0.42 + s.DepthInvalidConfidence * 0.16)
        * (1.0 - water.SkyReject * 0.58));

    float edgeOrFoam = smoothstep(0.05, 0.32, max(s.Edge, s.Detail)) * smoothstep(0.42, 0.96, s.Luma);
    water.FoamOrEdge = saturate(edgeOrFoam * max(teal, saturate(1.0 - s.Saturation) * 0.55) * sceneCoastal * (1.0 - water.SkinReject * 0.85) * (1.0 - fireLike * 0.72));
    water.WetShoreline = saturate((water.ShallowWater * 0.55 + water.FoamOrEdge * 0.52)
        * max(sceneCoastal, sceneWet)
        * (0.36 + water.SandReject * 0.38 + s.LowerScreen * 0.18)
        * (1.0 - water.SkyReject * 0.90)
        * (1.0 - water.SkinReject * 0.90));

    water.WaterSurface = saturate(max(water.ShallowWater, water.DeepWater)
        * (0.36 + water.WaterCoherence * 0.48 + smoothstep(0.42, 0.95, s.Smoothness) * 0.22)
        * (1.0 - water.SkyReject * 0.78)
        * (1.0 - water.SandReject * 0.50)
        * (1.0 - water.SkinReject * 0.95));
    water.SkySource = saturate(raw.SkyCloudFog * (0.35 + materialSkyCloudFog * 0.42 + sceneOpenOcean * 0.18) * (1.0 - water.SkinReject * 0.90));
    water.WaterReceiver = saturate(water.WaterSurface
        * (0.54 + waterSceneSupport * 0.38)
        * (0.54 + water.WaterCoherence * 0.46)
        * (1.0 - water.SkyReject * 0.95)
        * (1.0 - water.SandReject * 0.72)
        * (1.0 - water.SkinReject * 0.98));
    water.WaterSource = saturate(max(water.WaterSurface, max(water.WaterHorizon * 0.70, max(water.FoamOrEdge * 0.45, water.SkySource * sceneOpenOcean * 0.42))));
    water.Confidence = saturate(water.WaterReceiver + water.WaterSource * 0.45 + water.WetShoreline * 0.50 + water.FoamOrEdge * 0.38);

    return water;
}

Dalashade_FinalMaterialMasks Dalashade_GetAllMaterialMasksWithDepthAssist(
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
    float sceneVoidDarkness,
    float enableDepthAssist,
    float depthAssistStrength,
    float depthAssistConfidenceFloor)
{
    Dalashade_MaterialSignals signals = Dalashade_GetMaterialSignals(color, uv, enableDepthAssist, depthAssistStrength, depthAssistConfidenceFloor);
    Dalashade_RawMaterialCandidates raw = Dalashade_GetRawMaterialCandidates(signals);
    Dalashade_GatedMaterialCandidates gated = Dalashade_GetGatedMaterialCandidates(
        raw,
        sceneFoliage,
        sceneWaterSpecular,
        sceneSandDust,
        sceneSnowIce,
        sceneStoneRuins,
        sceneMetalIndustrial,
        sceneCrystalAether,
        sceneNeonGlass,
        sceneFireLavaHeat,
        sceneSkyCloudFog,
        sceneSkinProtection,
        sceneVoidDarkness);
    return Dalashade_ResolveFinalMaterialMasks(signals, raw, gated);
}

Dalashade_FinalMaterialMasks Dalashade_GetAllMaterialMasksWithWaterSplitDepthAssist(
    float3 color,
    float2 uv,
    float sceneFoliage,
    float sceneWaterSpecular,
    float sceneWaterPlane,
    float sceneSpecularGlint,
    float sceneSandDust,
    float sceneSnowIce,
    float sceneStoneRuins,
    float sceneMetalIndustrial,
    float sceneCrystalAether,
    float sceneNeonGlass,
    float sceneFireLavaHeat,
    float sceneSkyCloudFog,
    float sceneSkinProtection,
    float sceneVoidDarkness,
    float enableDepthAssist,
    float depthAssistStrength,
    float depthAssistConfidenceFloor)
{
    Dalashade_MaterialSignals signals = Dalashade_GetMaterialSignals(color, uv, enableDepthAssist, depthAssistStrength, depthAssistConfidenceFloor);
    Dalashade_RawMaterialCandidates raw = Dalashade_GetRawMaterialCandidates(signals);
    Dalashade_GatedMaterialCandidates gated = Dalashade_GetGatedMaterialCandidatesWithWaterSplit(
        raw,
        sceneFoliage,
        sceneWaterSpecular,
        sceneWaterPlane,
        sceneSpecularGlint,
        sceneSandDust,
        sceneSnowIce,
        sceneStoneRuins,
        sceneMetalIndustrial,
        sceneCrystalAether,
        sceneNeonGlass,
        sceneFireLavaHeat,
        sceneSkyCloudFog,
        sceneSkinProtection,
        sceneVoidDarkness);
    return Dalashade_ResolveFinalMaterialMasks(signals, raw, gated);
}

Dalashade_FinalMaterialMasks Dalashade_GetAllMaterialMasks(
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
    return Dalashade_GetAllMaterialMasksWithDepthAssist(
        color,
        uv,
        sceneFoliage,
        sceneWaterSpecular,
        sceneSandDust,
        sceneSnowIce,
        sceneStoneRuins,
        sceneMetalIndustrial,
        sceneCrystalAether,
        sceneNeonGlass,
        sceneFireLavaHeat,
        sceneSkyCloudFog,
        sceneSkinProtection,
        sceneVoidDarkness,
        0.0,
        0.0,
        0.0);
}

float Dalashade_GetRawFoliageStrong(Dalashade_MaterialSignals s)
{
    return Dalashade_GetRawMaterialCandidates(s).FoliageStrong;
}

float Dalashade_GetRawOrganicGreenSurface(Dalashade_MaterialSignals s)
{
    return Dalashade_GetRawMaterialCandidates(s).OrganicGreenSurface;
}

float Dalashade_GetRawSkyCloudFog(Dalashade_MaterialSignals s, float foliageStrong, float organicGreenSurface, float hardSurface)
{
    return Dalashade_GetRawMaterialCandidates(s).SkyCloudFog * saturate(1.0 - foliageStrong * 0.22 - organicGreenSurface * 0.10 - hardSurface * 0.18);
}

float Dalashade_GetWaterSpecularMask(float3 color, float2 uv, float sceneWaterSpecular)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, sceneWaterSpecular, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).WaterSpecular;
}

float Dalashade_GetWaterPlaneMask(float3 color, float2 uv, float sceneWaterSpecular)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, sceneWaterSpecular, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).WaterPlane;
}

float Dalashade_GetSpecularGlintMask(float3 color, float2 uv, float sceneWaterSpecular)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, sceneWaterSpecular, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).SpecularGlint;
}

float Dalashade_GetWaterSpecularCombinedMask(float3 color, float2 uv, float sceneWaterSpecular)
{
    return Dalashade_GetWaterSpecularMask(color, uv, sceneWaterSpecular);
}

float Dalashade_GetSandDustMask(float3 color, float2 uv, float sceneSandDust)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, sceneSandDust, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).SandDust;
}

float Dalashade_GetSnowIceMask(float3 color, float2 uv, float sceneSnowIce)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, sceneSnowIce, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).SnowIce;
}

float Dalashade_GetSkyFogMask(float3 color, float2 uv, float sceneSkyCloudFog)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, sceneSkyCloudFog, 0.0, 0.0).SkyCloudFog;
}

float Dalashade_GetFoliageMask(float3 color, float2 uv, float sceneFoliage)
{
    Dalashade_FinalMaterialMasks masks = Dalashade_GetAllMaterialMasks(color, uv, sceneFoliage, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
    return saturate(masks.FoliageStrong + masks.OrganicGreenSurface * 0.32);
}

float Dalashade_GetFoliageStrongMask(float3 color, float2 uv, float sceneFoliage)
{
    return Dalashade_GetAllMaterialMasks(color, uv, sceneFoliage, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).FoliageStrong;
}

float Dalashade_GetOrganicGreenSurfaceMask(float3 color, float2 uv, float sceneFoliage)
{
    return Dalashade_GetAllMaterialMasks(color, uv, sceneFoliage, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).OrganicGreenSurface;
}

float Dalashade_GetStoneRuinsMask(float3 color, float2 uv, float sceneStoneRuins)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, 0.0, sceneStoneRuins, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).StoneRuins;
}

float Dalashade_GetMetalIndustrialMask(float3 color, float2 uv, float sceneMetalIndustrial)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, 0.0, 0.0, sceneMetalIndustrial, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0).MetalIndustrial;
}

float Dalashade_GetCrystalAetherMask(float3 color, float2 uv, float sceneCrystalAether)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, sceneCrystalAether, 0.0, 0.0, 0.0, 0.0, 0.0).CrystalAether;
}

float Dalashade_GetNeonGlassMask(float3 color, float2 uv, float sceneNeonGlass)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, sceneNeonGlass, 0.0, 0.0, 0.0, 0.0).NeonGlass;
}

float Dalashade_GetFireLavaHeatMask(float3 color, float2 uv, float sceneFireLavaHeat)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, sceneFireLavaHeat, 0.0, 0.0, 0.0).FireLavaHeat;
}

float Dalashade_GetSkinProtectionMask(float3 color, float2 uv, float sceneSkinProtection)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, sceneSkinProtection, 0.0).SkinProtection;
}

float Dalashade_GetVoidDarknessMask(float3 color, float2 uv, float sceneVoidDarkness)
{
    return Dalashade_GetAllMaterialMasks(color, uv, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, sceneVoidDarkness).VoidDarkness;
}

float3 Dalashade_GetMaterialOverviewColor(Dalashade_FinalMaterialMasks masks)
{
    float3 color = float3(0.0, 0.0, 0.0);
    color += masks.FoliageStrong * float3(0.05, 0.95, 0.16);
    color += masks.OrganicGreenSurface * float3(0.28, 0.48, 0.12);
    color += masks.WaterPlane * float3(0.00, 0.85, 1.00);
    color += masks.SpecularGlint * float3(0.62, 0.86, 1.00);
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
