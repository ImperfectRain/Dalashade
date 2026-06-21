#ifndef DALASHADE_DALAPAD_FXH
#define DALASHADE_DALAPAD_FXH

// Shared optional Dalapad bridge helpers for first-party shaders.
// These helpers never authorize behavior by themselves: callers must pass a
// shader-local feature gate and strength, and unavailable resources resolve to
// zero-confidence results.

texture2D Dalashade_DalapadPinnedNormalTexture : DALAPAD_PINNED_NORMAL;
sampler Dalashade_DalapadPinnedNormalSampler
{
    Texture = Dalashade_DalapadPinnedNormalTexture;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D Dalashade_DalapadPinnedAlbedoTexture : DALAPAD_PINNED_ALBEDO;
sampler Dalashade_DalapadPinnedAlbedoSampler
{
    Texture = Dalashade_DalapadPinnedAlbedoTexture;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D Dalashade_DalapadPinnedMaskTexture : DALAPAD_PINNED_MASK;
sampler Dalashade_DalapadPinnedMaskSampler
{
    Texture = Dalashade_DalapadPinnedMaskTexture;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D Dalashade_DalapadPinnedNormalAltTexture : DALAPAD_PINNED_NORMAL_ALT;
sampler Dalashade_DalapadPinnedNormalAltSampler
{
    Texture = Dalashade_DalapadPinnedNormalAltTexture;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D Dalashade_DalapadPinnedEmissiveTexture : DALAPAD_PINNED_EMISSIVE;
sampler Dalashade_DalapadPinnedEmissiveSampler
{
    Texture = Dalashade_DalapadPinnedEmissiveTexture;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D Dalashade_DalapadPinnedWaterSurfaceTexture : DALAPAD_PINNED_WATER_SURFACE;
sampler Dalashade_DalapadPinnedWaterSurfaceSampler
{
    Texture = Dalashade_DalapadPinnedWaterSurfaceTexture;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

uniform float Dalashade_DalapadEnabled < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalapad Shader Additions"; > = 0.0;
uniform float Dalashade_DalapadSurfaceDataEnabled < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalapad Surface Data"; > = 0.0;
uniform float Dalashade_DalapadSurfaceDataStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalapad Surface Data Strength"; > = 0.75;

uniform int Dalapad_PinnedNormalAvailable < ui_label = "Dalapad Pinned Normal Available"; > = 0;
uniform int Dalapad_PinnedAlbedoAvailable < ui_label = "Dalapad Pinned Albedo Available"; > = 0;
uniform int Dalapad_PinnedMaskAvailable < ui_label = "Dalapad Pinned Mask Available"; > = 0;
uniform int Dalapad_PinnedNormalAltAvailable < ui_label = "Dalapad Pinned Normal Alt Available"; > = 0;
uniform int Dalapad_PinnedEmissiveAvailable < ui_label = "Dalapad Pinned Emissive Available"; > = 0;
uniform int Dalapad_PinnedWaterSurfaceAvailable < ui_label = "Dalapad Pinned Water Surface Available"; > = 0;

uniform int Dalapad_PinnedNormalWidth < ui_label = "Dalapad Pinned Normal Width"; > = 0;
uniform int Dalapad_PinnedNormalHeight < ui_label = "Dalapad Pinned Normal Height"; > = 0;
uniform int Dalapad_PinnedAlbedoWidth < ui_label = "Dalapad Pinned Albedo Width"; > = 0;
uniform int Dalapad_PinnedAlbedoHeight < ui_label = "Dalapad Pinned Albedo Height"; > = 0;
uniform int Dalapad_PinnedMaskWidth < ui_label = "Dalapad Pinned Mask Width"; > = 0;
uniform int Dalapad_PinnedMaskHeight < ui_label = "Dalapad Pinned Mask Height"; > = 0;
uniform int Dalapad_PinnedNormalAltWidth < ui_label = "Dalapad Pinned Normal Alt Width"; > = 0;
uniform int Dalapad_PinnedNormalAltHeight < ui_label = "Dalapad Pinned Normal Alt Height"; > = 0;
uniform int Dalapad_PinnedEmissiveWidth < ui_label = "Dalapad Pinned Emissive Width"; > = 0;
uniform int Dalapad_PinnedEmissiveHeight < ui_label = "Dalapad Pinned Emissive Height"; > = 0;
uniform int Dalapad_PinnedWaterSurfaceWidth < ui_label = "Dalapad Pinned Water Surface Width"; > = 0;
uniform int Dalapad_PinnedWaterSurfaceHeight < ui_label = "Dalapad Pinned Water Surface Height"; > = 0;

struct Dalashade_DalapadNormalResult
{
    float3 Raw;
    float3 DecodedNormal;
    float Presence;
    float Chroma;
    float NeighborDelta;
    float FlatSupport;
    float StructureSupport;
    float Confidence;
    float Gate;
};

struct Dalashade_DalapadScalarResult
{
    float4 Raw;
    float Luma;
    float Chroma;
    float NeighborDelta;
    float Presence;
    float Confidence;
    float Gate;
};

float Dalashade_DalapadLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float Dalashade_DalapadChroma(float3 color)
{
    return max(max(color.r, color.g), color.b) - min(min(color.r, color.g), color.b);
}

float Dalashade_DalapadAvailability(int available)
{
    return available > 0 ? 1.0 : 0.0;
}

float Dalashade_DalapadDimensionsKnown(int width, int height)
{
    return width > 0 && height > 0 ? 1.0 : 0.0;
}

float Dalashade_DalapadFeatureGate(float featureEnabled, float featureStrength, int available, int width, int height)
{
    // Semantic texture bindings sample through normalized UVs, so dimensions
    // are diagnostic metadata. Availability and user gates decide authorization.
    float dimensionsKnown = Dalashade_DalapadDimensionsKnown(width, height);
    float gate = saturate(Dalashade_DalapadEnabled)
        * saturate(featureEnabled)
        * saturate(featureStrength)
        * Dalashade_DalapadAvailability(available);

    return gate + dimensionsKnown * 0.0;
}

float3 Dalashade_DalapadDecodeNormalLike(float3 encodedNormal)
{
    float3 decoded = encodedNormal * 2.0 - 1.0;
    return decoded * rsqrt(max(dot(decoded, decoded), 0.0001));
}

Dalashade_DalapadNormalResult Dalashade_DalapadEmptyNormalResult(float3 fallbackNormal)
{
    Dalashade_DalapadNormalResult result;
    result.Raw = float3(0.0, 0.0, 0.0);
    result.DecodedNormal = fallbackNormal;
    result.Presence = 0.0;
    result.Chroma = 0.0;
    result.NeighborDelta = 0.0;
    result.FlatSupport = 0.0;
    result.StructureSupport = 0.0;
    result.Confidence = 0.0;
    result.Gate = 0.0;
    return result;
}

float4 Dalashade_DalapadPinnedNormal(float2 uv)
{
    return tex2D(Dalashade_DalapadPinnedNormalSampler, uv);
}

float4 Dalashade_DalapadPinnedAlbedo(float2 uv)
{
    return tex2D(Dalashade_DalapadPinnedAlbedoSampler, uv);
}

float4 Dalashade_DalapadPinnedMask(float2 uv)
{
    return tex2D(Dalashade_DalapadPinnedMaskSampler, uv);
}

float4 Dalashade_DalapadPinnedEmissive(float2 uv)
{
    return tex2D(Dalashade_DalapadPinnedEmissiveSampler, uv);
}

float4 Dalashade_DalapadPinnedWaterSurface(float2 uv)
{
    return tex2D(Dalashade_DalapadPinnedWaterSurfaceSampler, uv);
}

Dalashade_DalapadScalarResult Dalashade_DalapadBuildScalarResult(float4 raw, float3 right, float3 down, float gate)
{
    float luma = Dalashade_DalapadLuma(raw.rgb);
    float chroma = Dalashade_DalapadChroma(raw.rgb);
    float neighborDelta = length(raw.rgb - right) + length(raw.rgb - down);
    float presence = smoothstep(0.025, 0.18, luma + chroma * 0.35 + neighborDelta * 1.8);

    Dalashade_DalapadScalarResult result;
    result.Raw = raw;
    result.Luma = luma;
    result.Chroma = chroma;
    result.NeighborDelta = neighborDelta;
    result.Presence = presence;
    result.Confidence = gate * presence;
    result.Gate = gate;
    return result;
}

Dalashade_DalapadScalarResult Dalashade_DalapadEmptyScalarResult()
{
    Dalashade_DalapadScalarResult result;
    result.Raw = float4(0.0, 0.0, 0.0, 0.0);
    result.Luma = 0.0;
    result.Chroma = 0.0;
    result.NeighborDelta = 0.0;
    result.Presence = 0.0;
    result.Confidence = 0.0;
    result.Gate = 0.0;
    return result;
}

Dalashade_DalapadNormalResult Dalashade_DalapadPinnedNormalAssist(float2 uv, float3 fallbackNormal, float featureEnabled, float featureStrength)
{
    float gate = Dalashade_DalapadFeatureGate(featureEnabled, featureStrength, Dalapad_PinnedNormalAvailable, Dalapad_PinnedNormalWidth, Dalapad_PinnedNormalHeight);
    if (gate <= 0.0)
    {
        return Dalashade_DalapadEmptyNormalResult(fallbackNormal);
    }

    float2 texel = BUFFER_PIXEL_SIZE;
    float3 raw = tex2D(Dalashade_DalapadPinnedNormalSampler, uv).rgb;
    float3 right = tex2D(Dalashade_DalapadPinnedNormalSampler, saturate(uv + float2(texel.x, 0.0))).rgb;
    float3 down = tex2D(Dalashade_DalapadPinnedNormalSampler, saturate(uv + float2(0.0, texel.y))).rgb;
    float luma = Dalashade_DalapadLuma(raw);
    float chroma = Dalashade_DalapadChroma(raw);
    float neighborDelta = length(raw - right) + length(raw - down);
    float decodedLength = dot(raw * 2.0 - 1.0, raw * 2.0 - 1.0);
    float validDecoded = smoothstep(0.035, 0.14, decodedLength);
    float3 bridgeNormal = Dalashade_DalapadDecodeNormalLike(raw);
    float agreement = saturate(dot(fallbackNormal, bridgeNormal) * 0.5 + 0.5);
    float encodedLength = dot(raw, raw);
    float validEncoded = smoothstep(0.06, 0.22, encodedLength);
    float normalVariation = smoothstep(0.015, 0.20, chroma);
    float presence = validEncoded * validDecoded * smoothstep(0.045, 0.18, luma + chroma * 0.24);
    float agreementDampen = lerp(0.84, 1.0, agreement);
    float flatSupport = presence * saturate(0.34 + normalVariation * 0.34) * (1.0 - smoothstep(0.18, 0.58, neighborDelta));
    float structureSupport = presence * smoothstep(0.006, 0.085, neighborDelta);
    float confidence = gate * saturate((0.48 + flatSupport * 0.28 + structureSupport * 0.42) * presence * agreementDampen);

    Dalashade_DalapadNormalResult result;
    result.Raw = raw;
    result.DecodedNormal = bridgeNormal;
    result.Presence = presence;
    result.Chroma = chroma;
    result.NeighborDelta = neighborDelta;
    result.FlatSupport = gate * flatSupport;
    result.StructureSupport = gate * structureSupport;
    result.Confidence = confidence;
    result.Gate = gate;
    return result;
}

Dalashade_DalapadScalarResult Dalashade_DalapadPinnedAlbedoEvidence(float2 uv, float featureEnabled, float featureStrength)
{
    float gate = Dalashade_DalapadFeatureGate(featureEnabled, featureStrength, Dalapad_PinnedAlbedoAvailable, Dalapad_PinnedAlbedoWidth, Dalapad_PinnedAlbedoHeight);
    if (gate <= 0.0)
    {
        return Dalashade_DalapadEmptyScalarResult();
    }

    float2 texel = BUFFER_PIXEL_SIZE;
    float4 raw = tex2D(Dalashade_DalapadPinnedAlbedoSampler, uv);
    float3 right = tex2D(Dalashade_DalapadPinnedAlbedoSampler, saturate(uv + float2(texel.x, 0.0))).rgb;
    float3 down = tex2D(Dalashade_DalapadPinnedAlbedoSampler, saturate(uv + float2(0.0, texel.y))).rgb;
    return Dalashade_DalapadBuildScalarResult(raw, right, down, gate);
}

Dalashade_DalapadScalarResult Dalashade_DalapadPinnedMaskEvidence(float2 uv, float featureEnabled, float featureStrength)
{
    float gate = Dalashade_DalapadFeatureGate(featureEnabled, featureStrength, Dalapad_PinnedMaskAvailable, Dalapad_PinnedMaskWidth, Dalapad_PinnedMaskHeight);
    if (gate <= 0.0)
    {
        return Dalashade_DalapadEmptyScalarResult();
    }

    float2 texel = BUFFER_PIXEL_SIZE;
    float4 raw = tex2D(Dalashade_DalapadPinnedMaskSampler, uv);
    float3 right = tex2D(Dalashade_DalapadPinnedMaskSampler, saturate(uv + float2(texel.x, 0.0))).rgb;
    float3 down = tex2D(Dalashade_DalapadPinnedMaskSampler, saturate(uv + float2(0.0, texel.y))).rgb;
    return Dalashade_DalapadBuildScalarResult(raw, right, down, gate);
}

Dalashade_DalapadScalarResult Dalashade_DalapadPinnedEmissiveEvidence(float2 uv, float featureEnabled, float featureStrength)
{
    float gate = Dalashade_DalapadFeatureGate(featureEnabled, featureStrength, Dalapad_PinnedEmissiveAvailable, Dalapad_PinnedEmissiveWidth, Dalapad_PinnedEmissiveHeight);
    if (gate <= 0.0)
    {
        return Dalashade_DalapadEmptyScalarResult();
    }

    float2 texel = BUFFER_PIXEL_SIZE;
    float4 raw = tex2D(Dalashade_DalapadPinnedEmissiveSampler, uv);
    float3 right = tex2D(Dalashade_DalapadPinnedEmissiveSampler, saturate(uv + float2(texel.x, 0.0))).rgb;
    float3 down = tex2D(Dalashade_DalapadPinnedEmissiveSampler, saturate(uv + float2(0.0, texel.y))).rgb;
    return Dalashade_DalapadBuildScalarResult(raw, right, down, gate);
}

Dalashade_DalapadScalarResult Dalashade_DalapadPinnedWaterSurfaceEvidence(float2 uv, float featureEnabled, float featureStrength)
{
    float gate = Dalashade_DalapadFeatureGate(featureEnabled, featureStrength, Dalapad_PinnedWaterSurfaceAvailable, Dalapad_PinnedWaterSurfaceWidth, Dalapad_PinnedWaterSurfaceHeight);
    if (gate <= 0.0)
    {
        return Dalashade_DalapadEmptyScalarResult();
    }

    float2 texel = BUFFER_PIXEL_SIZE;
    float4 raw = tex2D(Dalashade_DalapadPinnedWaterSurfaceSampler, uv);
    float3 right = tex2D(Dalashade_DalapadPinnedWaterSurfaceSampler, saturate(uv + float2(texel.x, 0.0))).rgb;
    float3 down = tex2D(Dalashade_DalapadPinnedWaterSurfaceSampler, saturate(uv + float2(0.0, texel.y))).rgb;
    return Dalashade_DalapadBuildScalarResult(raw, right, down, gate);
}

#endif
