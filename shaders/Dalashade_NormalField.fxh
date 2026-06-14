#ifndef DALASHADE_NORMAL_FIELD_FXH
#define DALASHADE_NORMAL_FIELD_FXH

#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"

// Dalashade NormalField is an optional screen-space inferred surface field.
// It is not true FFXIV material normals, G-buffer access, or normal maps.
// Keep this include side-effect free: it provides helper data only.

struct Dalashade_NormalField
{
    float3 DepthNormal;
    float3 DetailNormal;
    float3 CombinedNormal;

    float SurfaceFacing;
    float GroundFacing;
    float WallFacing;
    float EdgeDiscontinuity;

    float DetailStrength;
    float NormalConfidence;
    float DepthConfidence;
    float DetailConfidence;

    float ShadingReceiver;
    float ReflectionReceiver;
    float AOReceiver;

    float SafetySkySuppression;
    float SafetySkinHighlightSuppression;
    float SafetyWaterNoiseSuppression;
};

float3 Dalashade_NormalField_DefaultNormal()
{
    return float3(0.0, 0.0, 1.0);
}

float3 Dalashade_NormalField_SafeNormalize(float3 value)
{
    float lengthSquared = max(dot(value, value), 0.000001);
    return value * rsqrt(lengthSquared);
}

float3 Dalashade_NormalField_EncodeNormal(float3 normal)
{
    return saturate(Dalashade_NormalField_SafeNormalize(normal) * 0.5 + 0.5);
}

float Dalashade_NormalField_Luma(float3 color)
{
    return dot(saturate(color), float3(0.2126, 0.7152, 0.0722));
}

float Dalashade_NormalField_Chroma(float3 color)
{
    float3 safeColor = saturate(color);
    return max(max(safeColor.r, safeColor.g), safeColor.b) - min(min(safeColor.r, safeColor.g), safeColor.b);
}

float Dalashade_GetNormalDepthConfidence(float2 uv, float depth)
{
    float validDepth = step(0.00001, depth) * step(depth, 0.99999);
    float2 texel = BUFFER_PIXEL_SIZE;
    float depthLeft = saturate(ReShade::GetLinearizedDepth(uv - float2(texel.x, 0.0)));
    float depthRight = saturate(ReShade::GetLinearizedDepth(uv + float2(texel.x, 0.0)));
    float depthUp = saturate(ReShade::GetLinearizedDepth(uv - float2(0.0, texel.y)));
    float depthDown = saturate(ReShade::GetLinearizedDepth(uv + float2(0.0, texel.y)));

    float neighborValid = step(0.00001, depthLeft) * step(depthLeft, 0.99999)
        * step(0.00001, depthRight) * step(depthRight, 0.99999)
        * step(0.00001, depthUp) * step(depthUp, 0.99999)
        * step(0.00001, depthDown) * step(depthDown, 0.99999);
    float depthVariation = abs(depthRight - depthLeft) + abs(depthDown - depthUp);
    float flatDepthRisk = 1.0 - smoothstep(0.00004, 0.0040, depthVariation);
    return saturate(validDepth * neighborValid * (1.0 - flatDepthRisk * 0.80));
}

float3 Dalashade_GetDepthNormal(float2 uv, float depth, float depthStrength)
{
    float confidence = Dalashade_GetNormalDepthConfidence(uv, depth) * saturate(depthStrength);
    if (confidence <= 0.0001)
    {
        return Dalashade_NormalField_DefaultNormal();
    }

    float2 texel = BUFFER_PIXEL_SIZE;
    float depthLeft = saturate(ReShade::GetLinearizedDepth(uv - float2(texel.x, 0.0)));
    float depthRight = saturate(ReShade::GetLinearizedDepth(uv + float2(texel.x, 0.0)));
    float depthUp = saturate(ReShade::GetLinearizedDepth(uv - float2(0.0, texel.y)));
    float depthDown = saturate(ReShade::GetLinearizedDepth(uv + float2(0.0, texel.y)));

    float dx = (depthRight - depthLeft) * 48.0 * confidence;
    float dy = (depthDown - depthUp) * 48.0 * confidence;
    return Dalashade_NormalField_SafeNormalize(float3(-dx, -dy, 1.0));
}

float3 Dalashade_GetImageGradientNormal(float2 uv, float detailStrength)
{
    float strength = saturate(detailStrength);
    if (strength <= 0.0001)
    {
        return Dalashade_NormalField_DefaultNormal();
    }

    float2 texel = BUFFER_PIXEL_SIZE;
    float left = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, uv - float2(texel.x, 0.0)).rgb);
    float right = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, uv + float2(texel.x, 0.0)).rgb);
    float up = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, uv - float2(0.0, texel.y)).rgb);
    float down = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, uv + float2(0.0, texel.y)).rgb);

    // Deliberately restrained: this is a selective detail hint, not a relief-map pass.
    float dx = (right - left) * 3.35 * strength;
    float dy = (down - up) * 3.35 * strength;
    return Dalashade_NormalField_SafeNormalize(float3(-dx, -dy, 1.0));
}

float Dalashade_GetDetailEligibility(
    Dalashade_MaterialResolve material,
    Dalashade_WaterResolve water,
    Dalashade_SafetyResolve safety,
    float edge,
    float detail,
    float smoothness,
    float luma)
{
    float hardSurface = saturate(material.StoneRuins * 0.52 + material.MetalIndustrial * 0.52 + material.SurfaceHardness * 0.42);
    float groundMaterial = saturate(material.SandDust * 0.30 + material.SnowIce * 0.14 + material.CrystalAether * 0.12);
    float controlledTexture = smoothstep(0.015, 0.18, detail) * (1.0 - smoothstep(0.30, 0.72, edge));
    float highlightRisk = smoothstep(0.62, 0.96, luma) * saturate(safety.HighlightProtect + material.SpecularGlint * 0.55);
    float foliageNoiseRisk = saturate(safety.FoliageNoiseReject * 0.65 + material.Foliage * smoothstep(0.18, 0.55, detail) * 0.28);
    float atmosphereRisk = saturate(safety.SkyReject * 0.95 + material.SkyCloudFog * 0.65);
    float surfaceSupport = saturate(0.08 + hardSurface + groundMaterial + material.ReceiverConfidence * 0.24);
    float safetyGate = saturate(1.0
        - atmosphereRisk
        - safety.SkinReject * 0.90
        - safety.WaterAOReject * 0.55
        - water.WaterReceiver * 0.55
        - highlightRisk * 0.45
        - foliageNoiseRisk * 0.35);

    return saturate(surfaceSupport * controlledTexture * safetyGate * (0.55 + smoothness * 0.28));
}

Dalashade_NormalField Dalashade_ResolveNormalField(
    float3 color,
    float2 uv,
    Dalashade_MaterialResolve material,
    Dalashade_WaterResolve water,
    Dalashade_SafetyResolve safety,
    float enableNormalField,
    float normalFieldStrength,
    float depthStrength,
    float detailStrength,
    float materialInfluence,
    float waterSuppression,
    float skinSuppression,
    float skySuppression)
{
    Dalashade_NormalField field;

    float enabled = saturate(enableNormalField) * saturate(normalFieldStrength);
    float depth = saturate(ReShade::GetLinearizedDepth(uv));
    float edge = Dalashade_GetEdgeStrength(uv);
    float detail = Dalashade_GetDetailStrength(uv);
    float smoothness = Dalashade_GetSmoothness(uv);
    float luma = Dalashade_NormalField_Luma(color);

    field.DepthConfidence = Dalashade_GetNormalDepthConfidence(uv, depth) * saturate(depthStrength) * enabled;
    field.DepthNormal = Dalashade_GetDepthNormal(uv, depth, depthStrength);

    float detailEligibility = Dalashade_GetDetailEligibility(material, water, safety, edge, detail, smoothness, luma);
    float materialGate = saturate(0.45 + materialInfluence * (material.SurfaceHardness + material.ReceiverConfidence + material.StoneRuins + material.MetalIndustrial) * 0.28);
    float suppression = saturate(
        water.WaterReceiver * waterSuppression
        + safety.SkinReject * skinSuppression
        + safety.SkyReject * skySuppression
        + safety.HighlightProtect * 0.35);
    field.DetailConfidence = saturate(enabled * saturate(detailStrength) * detailEligibility * materialGate * (1.0 - suppression));
    field.DetailNormal = Dalashade_GetImageGradientNormal(uv, detailStrength * field.DetailConfidence);

    float3 depthWeighted = lerp(Dalashade_NormalField_DefaultNormal(), field.DepthNormal, field.DepthConfidence);
    float3 detailWeighted = lerp(Dalashade_NormalField_DefaultNormal(), field.DetailNormal, field.DetailConfidence * 0.55);
    field.CombinedNormal = Dalashade_NormalField_SafeNormalize(depthWeighted + (detailWeighted - Dalashade_NormalField_DefaultNormal()) * 0.70);

    field.SurfaceFacing = saturate(field.CombinedNormal.z);
    field.EdgeDiscontinuity = saturate(edge * (0.55 + field.DepthConfidence * 0.45) + safety.UIDepthRisk * 0.45);
    field.DetailStrength = field.DetailConfidence * saturate(1.0 - field.EdgeDiscontinuity * 0.72);
    field.NormalConfidence = saturate(enabled * (field.DepthConfidence * 0.58 + field.DetailStrength * 0.42));
    float orientationConfidence = saturate(field.DepthConfidence * 0.75 + field.DetailConfidence * 0.25);
    float skyGate = saturate(1.0 - safety.SkyReject * 0.85);
    field.GroundFacing = saturate(smoothstep(0.58, 0.96, field.CombinedNormal.z) * orientationConfidence * skyGate);
    field.WallFacing = saturate(smoothstep(0.18, 0.72, 1.0 - abs(field.CombinedNormal.z)) * orientationConfidence * skyGate);

    float receiverSafety = saturate(1.0 - safety.SkyReject * skySuppression - safety.SkinReject * skinSuppression);
    float highlightSafety = saturate(1.0 - safety.HighlightProtect * 0.45 - safety.BrightSandProtect * 0.35 - safety.SnowProtect * 0.30);
    float waterReceiver = saturate(water.WaterReceiver * (0.45 + water.WaterCoherence * 0.55));
    float hardSmoothReceiver = saturate((material.MetalIndustrial * 0.32 + material.StoneRuins * 0.20 + material.SurfaceHardness * 0.30) * smoothness);
    float glintReceiver = saturate(material.SpecularGlint * (0.35 + material.MetalIndustrial * 0.20 + material.SnowIce * 0.12));
    float iceReceiver = saturate(material.SnowIce * smoothness * highlightSafety * 0.24);
    float reflectionSupport = saturate(waterReceiver + hardSmoothReceiver + glintReceiver + iceReceiver);

    float structuralReceiver = saturate(
        material.ReceiverConfidence * 0.45
        + field.GroundFacing * 0.25
        + field.WallFacing * 0.25
        + material.SurfaceHardness * 0.20);

    field.ShadingReceiver = saturate(enabled * orientationConfidence * material.ReceiverConfidence * receiverSafety * (0.35 + field.NormalConfidence * 0.65));
    field.ReflectionReceiver = saturate(enabled
        * reflectionSupport
        * receiverSafety
        * highlightSafety
        * (0.32 + orientationConfidence * 0.38 + water.WaterReceiver * 0.30)
        * (1.0 - material.Foliage * 0.35)
        * (1.0 - safety.FoliageNoiseReject * 0.30)
        * (1.0 - field.EdgeDiscontinuity * 0.45));
    field.AOReceiver = saturate(enabled
        * structuralReceiver
        * orientationConfidence
        * (1.0 - water.WaterReceiver * waterSuppression)
        * receiverSafety
        * (1.0 - safety.SnowProtect * 0.45)
        * (1.0 - safety.BrightSandProtect * 0.35)
        * (1.0 - safety.FoliageNoiseReject * 0.45)
        * (1.0 - field.EdgeDiscontinuity * 0.35));

    field.SafetySkySuppression = saturate(safety.SkyReject * skySuppression);
    field.SafetySkinHighlightSuppression = saturate(safety.SkinReject * skinSuppression + safety.HighlightProtect * 0.35 + safety.BrightSandProtect * 0.20 + safety.SnowProtect * 0.15);
    field.SafetyWaterNoiseSuppression = saturate(water.WaterReceiver * waterSuppression + safety.WaterAOReject * 0.45 + safety.FoliageNoiseReject * 0.35 + field.EdgeDiscontinuity * 0.20);

    if (enabled <= 0.0001)
    {
        field.DepthNormal = Dalashade_NormalField_DefaultNormal();
        field.DetailNormal = Dalashade_NormalField_DefaultNormal();
        field.CombinedNormal = Dalashade_NormalField_DefaultNormal();
        field.SurfaceFacing = 1.0;
        field.GroundFacing = 0.0;
        field.WallFacing = 0.0;
        field.EdgeDiscontinuity = 0.0;
        field.DetailStrength = 0.0;
        field.NormalConfidence = 0.0;
        field.DepthConfidence = 0.0;
        field.DetailConfidence = 0.0;
        field.ShadingReceiver = 0.0;
        field.ReflectionReceiver = 0.0;
        field.AOReceiver = 0.0;
        field.SafetySkySuppression = 0.0;
        field.SafetySkinHighlightSuppression = 0.0;
        field.SafetyWaterNoiseSuppression = 0.0;
    }

    return field;
}

float3 Dalashade_GetNormalDebugColor(Dalashade_NormalField field, int mode, float boost)
{
    float debugBoost = clamp(boost, 0.25, 8.0);
    if (mode == 1)
    {
        return Dalashade_NormalField_EncodeNormal(field.DepthNormal);
    }
    if (mode == 2)
    {
        return Dalashade_NormalField_EncodeNormal(field.DetailNormal);
    }
    if (mode == 3 || mode == 0)
    {
        return Dalashade_NormalField_EncodeNormal(field.CombinedNormal);
    }
    if (mode == 4)
    {
        return saturate(field.GroundFacing * debugBoost).xxx;
    }
    if (mode == 5)
    {
        return saturate(field.WallFacing * debugBoost).xxx;
    }
    if (mode == 6)
    {
        return float3(saturate(field.DetailStrength * debugBoost), saturate(field.DetailConfidence * debugBoost), saturate(field.EdgeDiscontinuity));
    }
    if (mode == 7)
    {
        return saturate(field.NormalConfidence * debugBoost).xxx;
    }
    if (mode == 8)
    {
        return float3(0.18, 0.65, 1.0) * saturate(field.ShadingReceiver * debugBoost);
    }
    if (mode == 9)
    {
        return float3(0.0, 0.85, 1.0) * saturate(field.ReflectionReceiver * debugBoost);
    }
    if (mode == 10)
    {
        return float3(0.75, 0.75, 0.75) * saturate(field.AOReceiver * debugBoost);
    }
    if (mode == 11)
    {
        float safetyBoost = min(debugBoost, 3.0);
        return saturate(float3(
            field.SafetySkySuppression,
            field.SafetySkinHighlightSuppression,
            field.SafetyWaterNoiseSuppression) * safetyBoost);
    }

    return Dalashade_NormalField_EncodeNormal(field.CombinedNormal);
}

#endif
