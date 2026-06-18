#ifndef DALASHADE_NORMAL_FIELD_FXH
#define DALASHADE_NORMAL_FIELD_FXH

#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"

// Dalashade NormalField is an optional screen-space inferred surface field.
// It is not true FFXIV material normals, G-buffer access, or normal maps.
// Keep this include side-effect free: it provides helper data only.
// WallPlaneCandidate is stricter physical-ish orientation. StructureCandidate
// is broader screen-space structural evidence. Production shaders should
// prefer StructureCandidate for AO/shading support and WallPlaneCandidate only
// when true vertical orientation matters.
// WallPlaneCandidate should not be the primary structure mask.

struct Dalashade_NormalField
{
    float3 DepthNormal;
    float3 DetailNormal;
    float3 TextureGradientReliefNormal;
    float3 TextureReliefNormal;
    float3 CombinedNormal;

    float SurfaceFacing;
    float GroundFacing;
    float WallFacing;
    float GroundPlaneCandidate;
    float StructureCandidate;
    float WallPlaneCandidate;
    float OrientationConfidence;
    float EdgeDiscontinuity;

    float DetailStrength;
    float TextureReliefStrength;
    float TextureRidge;
    float TextureGroove;
    float TextureGrooveLine;
    float TextureCurvatureRidge;
    float TextureCurvatureValley;
    float TextureCoherence;
    float TextureHeightComposite;
    float TextureCompositeConfidence;
    float TextureReliefSafety;
    float NormalConfidence;
    float DepthConfidence;
    float DetailConfidence;
    float TextureReliefConfidence;

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

Dalashade_NormalField Dalashade_NormalField_Default()
{
    Dalashade_NormalField field;

    field.DepthNormal = Dalashade_NormalField_DefaultNormal();
    field.DetailNormal = Dalashade_NormalField_DefaultNormal();
    field.TextureGradientReliefNormal = Dalashade_NormalField_DefaultNormal();
    field.TextureReliefNormal = Dalashade_NormalField_DefaultNormal();
    field.CombinedNormal = Dalashade_NormalField_DefaultNormal();

    field.SurfaceFacing = 1.0;
    field.GroundFacing = 0.0;
    field.WallFacing = 0.0;
    field.GroundPlaneCandidate = 0.0;
    field.StructureCandidate = 0.0;
    field.WallPlaneCandidate = 0.0;
    field.OrientationConfidence = 0.0;
    field.EdgeDiscontinuity = 0.0;

    field.DetailStrength = 0.0;
    field.TextureReliefStrength = 0.0;
    field.TextureRidge = 0.0;
    field.TextureGroove = 0.0;
    field.TextureGrooveLine = 0.0;
    field.TextureCurvatureRidge = 0.0;
    field.TextureCurvatureValley = 0.0;
    field.TextureCoherence = 0.0;
    field.TextureHeightComposite = 0.5;
    field.TextureCompositeConfidence = 0.0;
    field.TextureReliefSafety = 0.0;
    field.NormalConfidence = 0.0;
    field.DepthConfidence = 0.0;
    field.DetailConfidence = 0.0;
    field.TextureReliefConfidence = 0.0;

    field.ShadingReceiver = 0.0;
    field.ReflectionReceiver = 0.0;
    field.AOReceiver = 0.0;

    field.SafetySkySuppression = 0.0;
    field.SafetySkinHighlightSuppression = 0.0;
    field.SafetyWaterNoiseSuppression = 0.0;

    return field;
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
    float depthLeft = saturate(ReShade::GetLinearizedDepth(saturate(uv - float2(texel.x, 0.0))));
    float depthRight = saturate(ReShade::GetLinearizedDepth(saturate(uv + float2(texel.x, 0.0))));
    float depthUp = saturate(ReShade::GetLinearizedDepth(saturate(uv - float2(0.0, texel.y))));
    float depthDown = saturate(ReShade::GetLinearizedDepth(saturate(uv + float2(0.0, texel.y))));

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
    float depthLeft = saturate(ReShade::GetLinearizedDepth(saturate(uv - float2(texel.x, 0.0))));
    float depthRight = saturate(ReShade::GetLinearizedDepth(saturate(uv + float2(texel.x, 0.0))));
    float depthUp = saturate(ReShade::GetLinearizedDepth(saturate(uv - float2(0.0, texel.y))));
    float depthDown = saturate(ReShade::GetLinearizedDepth(saturate(uv + float2(0.0, texel.y))));

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
    float left = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x, 0.0))).rgb);
    float right = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, 0.0))).rgb);
    float up = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y))).rgb);
    float down = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y))).rgb);

    // Deliberately restrained: this is a selective detail hint, not a relief-map pass.
    float dx = (right - left) * 3.35 * strength;
    float dy = (down - up) * 3.35 * strength;
    return Dalashade_NormalField_SafeNormalize(float3(-dx, -dy, 1.0));
}

float Dalashade_GetBroadGradientReject(float2 uv)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float left = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x * 2.0, 0.0))).rgb);
    float right = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x * 2.0, 0.0))).rgb);
    float up = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y * 2.0))).rgb);
    float down = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y * 2.0))).rgb);
    float broadGradient = abs(right - left) + abs(down - up);
    return 1.0 - smoothstep(0.050, 0.220, broadGradient);
}

float Dalashade_GetLocalTextureRelief(
    float2 uv,
    out float ridge,
    out float groove,
    out float reliefDetail)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float center = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, uv).rgb);
    float left = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x, 0.0))).rgb);
    float right = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, 0.0))).rgb);
    float up = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y))).rgb);
    float down = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y))).rgb);
    float upLeft = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - texel)).rgb);
    float upRight = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, -texel.y))).rgb);
    float downLeft = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(-texel.x, texel.y))).rgb);
    float downRight = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + texel)).rgb);

    float nearAverage = (left + right + up + down + upLeft + upRight + downLeft + downRight) * 0.125;
    float highPass = center - nearAverage;
    ridge = smoothstep(0.012, 0.115, highPass);
    groove = smoothstep(0.012, 0.115, -highPass);
    reliefDetail = saturate((abs(right - left) + abs(down - up) + abs(highPass) * 2.0) * 2.8);
    return highPass;
}

float Dalashade_GetTextureGrooveLine(float2 uv)
{
    float2 texel = BUFFER_PIXEL_SIZE;
    float center = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, uv).rgb);
    float left = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x, 0.0))).rgb);
    float right = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, 0.0))).rgb);
    float up = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y))).rgb);
    float down = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y))).rgb);
    float upLeft = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - texel)).rgb);
    float upRight = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, -texel.y))).rgb);
    float downLeft = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(-texel.x, texel.y))).rgb);
    float downRight = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + texel)).rgb);
    float left2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x * 2.0, 0.0))).rgb);
    float right2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x * 2.0, 0.0))).rgb);
    float up2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y * 2.0))).rgb);
    float down2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y * 2.0))).rgb);
    float upLeft2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - texel * 2.0)).rgb);
    float upRight2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x * 2.0, -texel.y * 2.0))).rgb);
    float downLeft2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(-texel.x * 2.0, texel.y * 2.0))).rgb);
    float downRight2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + texel * 2.0)).rgb);

    float verticalContrast = min(left - center, right - center);
    float horizontalContrast = min(up - center, down - center);
    float diagonalAContrast = min(upRight - center, downLeft - center);
    float diagonalBContrast = min(upLeft - center, downRight - center);

    float verticalContinuity = 1.0 - smoothstep(0.030, 0.170, abs(up - center) + abs(down - center));
    float horizontalContinuity = 1.0 - smoothstep(0.030, 0.170, abs(left - center) + abs(right - center));
    float diagonalAContinuity = 1.0 - smoothstep(0.030, 0.170, abs(upLeft - center) + abs(downRight - center));
    float diagonalBContinuity = 1.0 - smoothstep(0.030, 0.170, abs(upRight - center) + abs(downLeft - center));
    float verticalLongContinuity = 1.0 - smoothstep(0.045, 0.190, abs(up2 - center) + abs(down2 - center));
    float horizontalLongContinuity = 1.0 - smoothstep(0.045, 0.190, abs(left2 - center) + abs(right2 - center));
    float diagonalALongContinuity = 1.0 - smoothstep(0.050, 0.205, abs(upLeft2 - center) + abs(downRight2 - center));
    float diagonalBLongContinuity = 1.0 - smoothstep(0.050, 0.205, abs(upRight2 - center) + abs(downLeft2 - center));

    float verticalLine = smoothstep(0.020, 0.120, verticalContrast) * verticalContinuity * (0.50 + verticalLongContinuity * 0.50);
    float horizontalLine = smoothstep(0.020, 0.120, horizontalContrast) * horizontalContinuity * (0.50 + horizontalLongContinuity * 0.50);
    float diagonalALine = smoothstep(0.024, 0.130, diagonalAContrast) * diagonalAContinuity * (0.35 + diagonalALongContinuity * 0.65);
    float diagonalBLine = smoothstep(0.024, 0.130, diagonalBContrast) * diagonalBContinuity * (0.35 + diagonalBLongContinuity * 0.65);
    float line = max(max(verticalLine, horizontalLine), max(diagonalALine, diagonalBLine));

    // Suppress isolated pepper noise: real grooves should have some local valley contrast
    // and at least one coherent line direction.
    float localValley = smoothstep(0.014, 0.090, max(max(verticalContrast, horizontalContrast), max(diagonalAContrast, diagonalBContrast)));
    float microScratchReject = 1.0 - smoothstep(0.38, 0.86, abs(left - right) + abs(up - down) + abs(upLeft - downRight) * 0.50 + abs(upRight - downLeft) * 0.50);
    return saturate(line * localValley * (0.35 + microScratchReject * 0.65));
}

struct Dalashade_TextureReliefComposite
{
    float Ridge;
    float Valley;
    float Coherence;
    float Height;
    float Confidence;
    float3 Normal;
};

struct Dalashade_TextureReliefHeightSample
{
    float Ridge;
    float Valley;
    float Coherence;
    float Height;
    float Confidence;
};

Dalashade_TextureReliefHeightSample Dalashade_GetTextureReliefHeightSample(float2 uv, float grooveLine)
{
    Dalashade_TextureReliefHeightSample heightInfo;
    float2 texel = BUFFER_PIXEL_SIZE;
    float center = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv)).rgb);
    float left = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x, 0.0))).rgb);
    float right = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, 0.0))).rgb);
    float up = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y))).rgb);
    float down = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y))).rgb);
    float upLeft = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - texel)).rgb);
    float upRight = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, -texel.y))).rgb);
    float downLeft = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(-texel.x, texel.y))).rgb);
    float downRight = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + texel)).rgb);
    float left2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x * 2.0, 0.0))).rgb);
    float right2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x * 2.0, 0.0))).rgb);
    float up2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y * 2.0))).rgb);
    float down2 = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y * 2.0))).rgb);

    float nearAverage = (left + right + up + down + upLeft + upRight + downLeft + downRight) * 0.125;
    float farAverage = (left2 + right2 + up2 + down2) * 0.25;
    float highPass = center - nearAverage;
    float structureBand = nearAverage - farAverage;

    float lxx = left - center * 2.0 + right;
    float lyy = up - center * 2.0 + down;
    float lxy = (upRight - upLeft - downRight + downLeft) * 0.25;
    float curvatureDelta = sqrt(max(0.0, (lxx - lyy) * (lxx - lyy) + 4.0 * lxy * lxy));
    float lambdaMin = (lxx + lyy - curvatureDelta) * 0.5;
    float lambdaMax = (lxx + lyy + curvatureDelta) * 0.5;
    float ridgeCurvature = smoothstep(0.018, 0.165, -lambdaMin) * smoothstep(0.010, 0.115, highPass);
    float valleyCurvature = smoothstep(0.018, 0.165, lambdaMax) * smoothstep(0.008, 0.105, -highPass);

    float dx = (right - left) * 0.5;
    float dy = (down - up) * 0.5;
    float diagA = (downRight - upLeft) * 0.5;
    float diagB = (downLeft - upRight) * 0.5;
    float jxx = dx * dx + (diagA * diagA + diagB * diagB) * 0.25;
    float jyy = dy * dy + (diagA * diagA + diagB * diagB) * 0.25;
    float jxy = dx * dy + (diagA * diagA - diagB * diagB) * 0.125;
    float trace = max(jxx + jyy, 0.000001);
    float determinant = jxx * jyy - jxy * jxy;
    float coherence = saturate(sqrt(max(0.0, trace * trace - 4.0 * determinant)) / trace);

    float directionalContrast = max(
        max(abs(left - right), abs(up - down)),
        max(abs(upLeft - downRight), abs(upRight - downLeft)));
    float broadReject = Dalashade_GetBroadGradientReject(uv);
    float isolatedNoiseReject = 1.0 - smoothstep(0.24, 0.68, abs(left - right) + abs(up - down) + abs(upLeft - downRight) * 0.50 + abs(upRight - downLeft) * 0.50);
    float structureScale = smoothstep(0.006, 0.065, abs(structureBand) + directionalContrast * 0.18 + grooveLine * 0.10);
    float energyGate = smoothstep(0.010, 0.112, abs(lambdaMin) + abs(lambdaMax) + abs(highPass) * 0.55 + directionalContrast * 0.18);
    float lineGate = saturate(grooveLine * 0.70 + coherence * energyGate * 0.30);
    float grainReject = 1.0 - smoothstep(0.38, 0.86, abs(highPass) * 3.2 + directionalContrast * (1.0 - coherence));

    heightInfo.Ridge = saturate(ridgeCurvature * broadReject * energyGate * (0.42 + coherence * 0.40 + lineGate * 0.18));
    heightInfo.Valley = saturate(max(valleyCurvature * (0.42 + coherence * 0.36), grooveLine * 0.92) * broadReject * (0.50 + structureScale * 0.50));
    float reliefEvidence = max(heightInfo.Ridge, heightInfo.Valley);
    float pairedEvidence = smoothstep(0.055, 0.38, reliefEvidence + grooveLine * 0.45);
    heightInfo.Coherence = saturate(max(coherence * energyGate * pairedEvidence, grooveLine) * broadReject);
    heightInfo.Confidence = saturate(
        (reliefEvidence * 0.66 + heightInfo.Coherence * 0.22 + structureScale * pairedEvidence * 0.12)
        * broadReject
        * (0.48 + isolatedNoiseReject * 0.24 + grainReject * 0.28));

    float grooveDepression = heightInfo.Valley * (0.42 + grooveLine * 0.18);
    float signedCurvature = heightInfo.Ridge * 0.24 - grooveDepression;
    float signedTexture = clamp(highPass, -0.050, 0.050) * (0.30 + heightInfo.Coherence * 0.24) * (0.25 + structureScale * pairedEvidence * 0.75);
    heightInfo.Height = clamp(0.5 + (signedCurvature + signedTexture) * heightInfo.Confidence, 0.22, 0.78);
    return heightInfo;
}

float Dalashade_CleanTextureReliefHeight(
    float rawHeight,
    float confidence,
    float coherence,
    float ridge,
    float valley,
    float grooveLine,
    float neighborAverage)
{
    float centerStructure = saturate(confidence + grooveLine * 0.70);
    float blurAmount = saturate((1.0 - centerStructure) * 0.62 + (1.0 - coherence) * 0.20);
    float cleanHeight = lerp(rawHeight, neighborAverage, blurAmount * 0.38);

    float groovePreserve = saturate(grooveLine * 0.86 + valley * 0.32);
    float structurePreserve = saturate(max(ridge, valley) * 0.45 + coherence * 0.30);
    return lerp(cleanHeight, rawHeight, saturate(groovePreserve + structurePreserve));
}

Dalashade_TextureReliefComposite Dalashade_GetTextureReliefComposite(float2 uv, float reliefStrength, float reliefConfidence, float grooveLine, float reliefSafetyGate)
{
    Dalashade_TextureReliefComposite composite;
    float2 texel = BUFFER_PIXEL_SIZE;
    Dalashade_TextureReliefHeightSample heightSample = Dalashade_GetTextureReliefHeightSample(uv, grooveLine);
    float2 leftUv = saturate(uv - float2(texel.x, 0.0));
    float2 rightUv = saturate(uv + float2(texel.x, 0.0));
    float2 upUv = saturate(uv - float2(0.0, texel.y));
    float2 downUv = saturate(uv + float2(0.0, texel.y));
    float neighborReliefGate = saturate(reliefSafetyGate);
    float leftGroove = Dalashade_GetTextureGrooveLine(leftUv) * neighborReliefGate;
    float rightGroove = Dalashade_GetTextureGrooveLine(rightUv) * neighborReliefGate;
    float upGroove = Dalashade_GetTextureGrooveLine(upUv) * neighborReliefGate;
    float downGroove = Dalashade_GetTextureGrooveLine(downUv) * neighborReliefGate;
    Dalashade_TextureReliefHeightSample leftSample = Dalashade_GetTextureReliefHeightSample(leftUv, leftGroove);
    Dalashade_TextureReliefHeightSample rightSample = Dalashade_GetTextureReliefHeightSample(rightUv, rightGroove);
    Dalashade_TextureReliefHeightSample upSample = Dalashade_GetTextureReliefHeightSample(upUv, upGroove);
    Dalashade_TextureReliefHeightSample downSample = Dalashade_GetTextureReliefHeightSample(downUv, downGroove);
    float neighborAverage = (leftSample.Height + rightSample.Height + upSample.Height + downSample.Height) * 0.25;

    composite.Ridge = heightSample.Ridge;
    composite.Valley = heightSample.Valley;
    composite.Coherence = heightSample.Coherence;
    composite.Confidence = saturate(heightSample.Confidence * saturate(reliefConfidence));
    composite.Height = lerp(0.5, Dalashade_CleanTextureReliefHeight(
        heightSample.Height,
        heightSample.Confidence,
        heightSample.Coherence,
        heightSample.Ridge,
        heightSample.Valley,
        grooveLine,
        neighborAverage),
        saturate(reliefConfidence));

    float strength = saturate(reliefStrength) * saturate(0.34 + composite.Confidence * 0.66);
    float verticalAverage = (heightSample.Height + upSample.Height + downSample.Height) * 0.333333;
    float horizontalAverage = (heightSample.Height + leftSample.Height + rightSample.Height) * 0.333333;
    float heightLeft = Dalashade_CleanTextureReliefHeight(leftSample.Height, leftSample.Confidence, leftSample.Coherence, leftSample.Ridge, leftSample.Valley, leftGroove, verticalAverage);
    float heightRight = Dalashade_CleanTextureReliefHeight(rightSample.Height, rightSample.Confidence, rightSample.Coherence, rightSample.Ridge, rightSample.Valley, rightGroove, verticalAverage);
    float heightUp = Dalashade_CleanTextureReliefHeight(upSample.Height, upSample.Confidence, upSample.Coherence, upSample.Ridge, upSample.Valley, upGroove, horizontalAverage);
    float heightDown = Dalashade_CleanTextureReliefHeight(downSample.Height, downSample.Confidence, downSample.Coherence, downSample.Ridge, downSample.Valley, downGroove, horizontalAverage);
    float reliefDx = (heightRight - heightLeft) * 11.0 * strength;
    float reliefDy = (heightDown - heightUp) * 11.0 * strength;
    composite.Normal = Dalashade_NormalField_SafeNormalize(float3(-reliefDx, -reliefDy, 1.0));

    return composite;
}

float3 Dalashade_GetTextureReliefNormal(float2 uv, float reliefStrength, float reliefConfidence)
{
    float strength = saturate(reliefStrength) * saturate(reliefConfidence);
    if (strength <= 0.0001)
    {
        return Dalashade_NormalField_DefaultNormal();
    }

    float2 texel = BUFFER_PIXEL_SIZE;
    float left = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x, 0.0))).rgb);
    float right = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, 0.0))).rgb);
    float up = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y))).rgb);
    float down = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y))).rgb);
    float upLeft = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv - texel)).rgb);
    float upRight = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, -texel.y))).rgb);
    float downLeft = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + float2(-texel.x, texel.y))).rgb);
    float downRight = Dalashade_NormalField_Luma(tex2D(ReShade::BackBuffer, saturate(uv + texel)).rgb);

    float sobelX = (right * 2.0 + upRight + downRight) - (left * 2.0 + upLeft + downLeft);
    float sobelY = (down * 2.0 + downLeft + downRight) - (up * 2.0 + upLeft + upRight);
    float dx = sobelX * 1.55 * strength;
    float dy = sobelY * 1.55 * strength;
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

float Dalashade_GetTextureReliefSafetyGate(
    Dalashade_MaterialResolve material,
    Dalashade_WaterResolve water,
    Dalashade_SafetyResolve safety,
    float edge,
    float detail,
    float luma,
    float waterSuppression,
    float skinSuppression,
    float skySuppression)
{
    float atmosphereReject = saturate(max(safety.SkyReject, material.SkyCloudFog) * (0.62 + skySuppression * 0.34));
    float waterReject = saturate(max(water.WaterReceiver, safety.WaterAOReject) * (0.42 + waterSuppression * 0.24));
    float skinReject = saturate(safety.SkinReject * (0.62 + skinSuppression * 0.34));
    float uiReject = saturate(safety.UIDepthRisk * 0.72);
    float foliageReject = saturate((safety.FoliageNoiseReject * 0.72 + material.Foliage * 0.24) * smoothstep(0.10, 0.46, detail));
    float highlightReject = saturate((safety.HighlightProtect * 0.48 + material.SpecularGlint * 0.28) * smoothstep(0.52, 0.96, luma));
    float emissiveReject = saturate((material.LightSourceConfidence * 0.42 + material.NeonGlass * 0.28 + material.CrystalAether * 0.14) * smoothstep(0.50, 0.98, luma));
    float hardEdgeReject = smoothstep(0.72, 1.0, edge) * 0.38;

    return saturate(1.0
        - atmosphereReject
        - waterReject
        - skinReject
        - uiReject
        - foliageReject
        - highlightReject
        - emissiveReject
        - hardEdgeReject);
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
    if (enabled <= 0.0001)
    {
        return Dalashade_NormalField_Default();
    }

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
    float reliefRidge;
    float reliefGroove;
    float reliefDetail;
    Dalashade_GetLocalTextureRelief(uv, reliefRidge, reliefGroove, reliefDetail);
    float grooveLine = Dalashade_GetTextureGrooveLine(uv);
    float textureReliefSafetyGate = Dalashade_GetTextureReliefSafetyGate(
        material,
        water,
        safety,
        edge,
        detail,
        luma,
        waterSuppression,
        skinSuppression,
        skySuppression);
    float safeGrooveLine = grooveLine * textureReliefSafetyGate;
    field.TextureReliefSafety = textureReliefSafetyGate;
    float reliefMaterialGate = saturate(
        material.SurfaceHardness * 0.38
        + material.StoneRuins * 0.32
        + material.MetalIndustrial * 0.28
        + material.SandDust * 0.18
        + material.SnowIce * 0.10
        + material.CrystalAether * 0.10
        + material.Foliage * 0.08
        + material.ReceiverConfidence * 0.16);
    float broadGradientReject = Dalashade_GetBroadGradientReject(uv);
    float wrongRegionReject = saturate(1.0
        - safety.SkyReject * skySuppression
        - safety.SkinReject * skinSuppression
        - water.WaterReceiver * waterSuppression * 0.70
        - safety.HighlightProtect * 0.32
        - safety.UIDepthRisk * 0.45
        - smoothstep(0.70, 1.0, edge) * 0.38);
    field.TextureGrooveLine = safeGrooveLine;
    field.TextureReliefConfidence = saturate(
        enabled
        * saturate(detailStrength)
        * detailEligibility
        * textureReliefSafetyGate
        * broadGradientReject
        * wrongRegionReject
        * (0.18 + reliefMaterialGate + materialInfluence * 0.28)
        * saturate(smoothstep(0.035, 0.34, reliefDetail) * 0.68 + safeGrooveLine * 0.42));
    Dalashade_TextureReliefComposite reliefComposite = Dalashade_GetTextureReliefComposite(uv, detailStrength, field.TextureReliefConfidence, safeGrooveLine, textureReliefSafetyGate);
    field.TextureCurvatureRidge = reliefComposite.Ridge;
    field.TextureCurvatureValley = reliefComposite.Valley;
    field.TextureCoherence = reliefComposite.Coherence;
    field.TextureHeightComposite = reliefComposite.Height;
    field.TextureCompositeConfidence = reliefComposite.Confidence;
    float legacyReliefHint = saturate((reliefDetail * 0.13 + max(reliefRidge, reliefGroove) * 0.05) * field.TextureReliefConfidence);
    field.TextureRidge = saturate(max(legacyReliefHint * 0.30, reliefComposite.Ridge) * (0.72 + safeGrooveLine * 0.12) * textureReliefSafetyGate);
    field.TextureGroove = saturate(max(max(legacyReliefHint * 0.22, safeGrooveLine * 0.82), reliefComposite.Valley) * textureReliefSafetyGate);
    field.TextureReliefStrength = saturate(field.TextureReliefConfidence * max(field.TextureRidge * 0.42, field.TextureGroove) * (0.48 + reliefComposite.Confidence * 0.52) * (1.0 - safety.FoliageNoiseReject * 0.38));
    field.TextureGradientReliefNormal = Dalashade_GetTextureReliefNormal(uv, detailStrength, field.TextureReliefConfidence);
    field.TextureReliefNormal = reliefComposite.Normal;

    float3 depthWeighted = lerp(Dalashade_NormalField_DefaultNormal(), field.DepthNormal, field.DepthConfidence);
    float3 detailWeighted = lerp(Dalashade_NormalField_DefaultNormal(), field.DetailNormal, field.DetailConfidence * 0.55);
    float3 reliefWeighted = lerp(Dalashade_NormalField_DefaultNormal(), field.TextureReliefNormal, field.TextureReliefStrength * 0.42);
    field.CombinedNormal = Dalashade_NormalField_SafeNormalize(
        depthWeighted
        + (detailWeighted - Dalashade_NormalField_DefaultNormal()) * 0.62
        + (reliefWeighted - Dalashade_NormalField_DefaultNormal()) * 0.56);

    field.SurfaceFacing = saturate(field.CombinedNormal.z);
    field.EdgeDiscontinuity = saturate(edge * (0.55 + field.DepthConfidence * 0.45) + safety.UIDepthRisk * 0.45);
    field.DetailStrength = saturate((field.DetailConfidence * 0.70 + field.TextureReliefStrength * 0.44) * saturate(1.0 - field.EdgeDiscontinuity * 0.72));
    field.NormalConfidence = saturate(enabled * (field.DepthConfidence * 0.54 + field.DetailStrength * 0.34 + field.TextureReliefConfidence * 0.20));
    float structureReceiverConfidence = saturate(max(material.StructureReceiverConfidence, material.ReceiverConfidence * 0.35));
    float aoReceiverConfidence = saturate(max(material.AOReceiverConfidence, material.ReceiverConfidence * 0.30));
    float normalGroundTerm = smoothstep(0.58, 0.96, field.CombinedNormal.z);
    float normalWallTerm = smoothstep(0.12, 0.62, 1.0 - abs(field.CombinedNormal.z));
    float usableEdgeStructure = smoothstep(0.025, 0.22, edge) * (1.0 - smoothstep(0.65, 0.96, edge));
    float usableDetailStructure = smoothstep(0.015, 0.20, detail) * (1.0 - smoothstep(0.62, 0.96, detail));
    float hardStructure = saturate(
        structureReceiverConfidence * 0.34
        + material.ReceiverConfidence * 0.14
        + material.SurfaceHardness * 0.30
        + material.StoneRuins * 0.22
        + material.MetalIndustrial * 0.22
        + material.CrystalAether * 0.08
        + material.Foliage * 0.04);
    field.OrientationConfidence = saturate(
        field.DepthConfidence * 0.50
        + field.DetailConfidence * 0.12
        + field.TextureReliefConfidence * 0.08
        + hardStructure * 0.22
        + structureReceiverConfidence * 0.12);
    float softPlaneTrust = saturate(0.25 + field.OrientationConfidence * 0.75);
    float skyGate = saturate(1.0 - safety.SkyReject * 0.85);
    field.StructureCandidate = saturate(
        (usableEdgeStructure * 0.38
        + usableDetailStructure * 0.16
        + field.TextureReliefStrength * 0.04
        + field.TextureGrooveLine * 0.04
        + hardStructure * 0.34
        + structureReceiverConfidence * 0.24
        + material.ReceiverConfidence * 0.08)
        * skyGate
        * (1.0 - water.WaterReceiver * 0.45)
        * (1.0 - safety.SkinReject * 0.80)
        * (1.0 - safety.HighlightProtect * 0.25));
    float groundMaterialHint = saturate(
        material.SandDust * 0.22
        + material.SnowIce * 0.12
        + water.WaterReceiver * 0.18
        + structureReceiverConfidence * 0.10
        + material.ReceiverConfidence * 0.10);
    field.GroundPlaneCandidate = saturate(
        (normalGroundTerm * 0.54 + groundMaterialHint * 0.36)
        * softPlaneTrust
        * skyGate
        * (1.0 - safety.SkinReject * 0.80));
    field.WallPlaneCandidate = saturate(
        (normalWallTerm * 0.72 + field.StructureCandidate * 0.18)
        * field.OrientationConfidence
        * skyGate
        * (1.0 - water.WaterReceiver * 0.65)
        * (1.0 - safety.SkinReject * 0.85)
        * (1.0 - safety.HighlightProtect * 0.25));
    field.GroundFacing = field.GroundPlaneCandidate;
    field.WallFacing = field.WallPlaneCandidate;

    float receiverSafety = saturate(1.0 - safety.SkyReject * skySuppression - safety.SkinReject * skinSuppression);
    float highlightSafety = saturate(1.0 - safety.HighlightProtect * 0.45 - safety.BrightSandProtect * 0.35 - safety.SnowProtect * 0.30);
    float waterReceiver = saturate(water.WaterReceiver * (0.45 + water.WaterCoherence * 0.55));
    float hardSmoothReceiver = saturate((material.MetalIndustrial * 0.32 + material.StoneRuins * 0.20 + material.SurfaceHardness * 0.30) * smoothness);
    float glintReceiver = saturate(material.SpecularGlint * (0.35 + material.MetalIndustrial * 0.20 + material.SnowIce * 0.12));
    float iceReceiver = saturate(material.SnowIce * smoothness * highlightSafety * 0.24);
    float sharedReflectionReceiver = saturate(material.ReflectionReceiverConfidence);
    float waterSpecificReceiver = saturate(water.WaterReceiver);
    float reflectionSemanticSupport = saturate(
        sharedReflectionReceiver * 0.52
        + waterSpecificReceiver * 0.40
        + hardSmoothReceiver * 0.14
        + glintReceiver * 0.10
        + iceReceiver * 0.08);
    float reflectionNormalTrust = saturate(
        0.42
        + field.NormalConfidence * 0.18
        + field.OrientationConfidence * 0.12
        + sharedReflectionReceiver * 0.22
        + waterSpecificReceiver * 0.22);

    float shadingCandidate = saturate(
        field.StructureCandidate * 0.38
        + field.GroundPlaneCandidate * 0.26
        + field.WallPlaneCandidate * 0.12
        + structureReceiverConfidence * 0.22
        + material.ReceiverConfidence * 0.10
        + material.SurfaceHardness * 0.18);
    float shadingTrust = saturate(
        0.28
        + field.NormalConfidence * 0.32
        + field.OrientationConfidence * 0.20
        + structureReceiverConfidence * 0.18
        + material.ReceiverConfidence * 0.08);
    field.ShadingReceiver = saturate(enabled
        * shadingCandidate
        * shadingTrust
        * receiverSafety
        * (1.0 - water.WaterReceiver * waterSuppression * 0.45)
        * (1.0 - safety.HighlightProtect * 0.20));
    field.ReflectionReceiver = saturate(enabled
        * reflectionSemanticSupport
        * reflectionNormalTrust
        * receiverSafety
        * highlightSafety
        * (1.0 - material.Foliage * 0.35)
        * (1.0 - safety.FoliageNoiseReject * 0.30)
        * (1.0 - field.EdgeDiscontinuity * 0.30));
    float orientationForAO = saturate(
        field.GroundPlaneCandidate * 0.28
        + field.WallPlaneCandidate * 0.18
        + field.StructureCandidate * 0.34
        + aoReceiverConfidence * 0.18
        + material.ReceiverConfidence * 0.08
        + material.SurfaceHardness * 0.16);
    float aoCandidate = saturate(
        field.StructureCandidate * 0.42
        + field.GroundPlaneCandidate * 0.24
        + field.WallPlaneCandidate * 0.10
        + aoReceiverConfidence * 0.30
        + material.ReceiverConfidence * 0.08
        + material.SurfaceHardness * 0.18
        + material.StoneRuins * 0.12
        + material.MetalIndustrial * 0.10);
    float aoTrust = saturate(
        0.24
        + field.NormalConfidence * 0.28
        + field.OrientationConfidence * 0.18
        + aoReceiverConfidence * 0.18
        + material.ReceiverConfidence * 0.08
        + material.SurfaceHardness * 0.10);
    float aoSafety = saturate(
        1.0
        - safety.SkyReject * skySuppression
        - safety.SkinReject * skinSuppression
        - water.WaterReceiver * waterSuppression * 0.75
        - safety.HighlightProtect * 0.18
        - safety.FoliageNoiseReject * 0.25
        - field.EdgeDiscontinuity * 0.20);
    field.AOReceiver = saturate(enabled
        * aoCandidate
        * aoTrust
        * orientationForAO
        * aoSafety
        * (1.0 - safety.SnowProtect * 0.45)
        * (1.0 - safety.BrightSandProtect * 0.35));

    field.SafetySkySuppression = saturate(safety.SkyReject * skySuppression);
    field.SafetySkinHighlightSuppression = saturate(safety.SkinReject * skinSuppression + safety.HighlightProtect * 0.35 + safety.BrightSandProtect * 0.20 + safety.SnowProtect * 0.15);
    field.SafetyWaterNoiseSuppression = saturate(water.WaterReceiver * waterSuppression + safety.WaterAOReject * 0.45 + safety.FoliageNoiseReject * 0.35 + field.EdgeDiscontinuity * 0.20);

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
    if (mode == 13)
    {
        return Dalashade_NormalField_EncodeNormal(field.TextureGradientReliefNormal);
    }
    if (mode == 14)
    {
        return saturate(float3(
            field.TextureRidge,
            field.TextureGroove,
            max(field.TextureReliefStrength, field.TextureGrooveLine)) * debugBoost);
    }
    if (mode == 15)
    {
        return float3(0.0, 0.90, 1.0) * saturate(field.TextureGrooveLine * debugBoost);
    }
    if (mode == 16)
    {
        return float3(1.0, 0.72, 0.10) * saturate(field.TextureCurvatureRidge * debugBoost);
    }
    if (mode == 17)
    {
        return float3(0.20, 0.85, 1.0) * saturate(field.TextureCurvatureValley * debugBoost);
    }
    if (mode == 18)
    {
        return saturate(float3(field.TextureCoherence, field.TextureCompositeConfidence, field.TextureGrooveLine) * debugBoost);
    }
    if (mode == 19)
    {
        float height = saturate((field.TextureHeightComposite - 0.5) * debugBoost + 0.5);
        float ridge = saturate(field.TextureCurvatureRidge * debugBoost);
        float valley = saturate(field.TextureCurvatureValley * debugBoost);
        float3 heightView = height.xxx;
        heightView += float3(0.10, 0.07, -0.05) * ridge;
        heightView += float3(-0.14, -0.10, 0.12) * valley;
        return saturate(heightView);
    }
    if (mode == 20)
    {
        return Dalashade_NormalField_EncodeNormal(field.TextureReliefNormal);
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
        return float3(0.90, 0.78, 0.32) * saturate(field.StructureCandidate * debugBoost);
    }
    if (mode == 7)
    {
        return float3(saturate(field.DetailStrength * debugBoost), saturate(field.DetailConfidence * debugBoost), saturate(field.EdgeDiscontinuity));
    }
    if (mode == 8)
    {
        return saturate(field.NormalConfidence * debugBoost).xxx;
    }
    if (mode == 9)
    {
        return float3(0.18, 0.65, 1.0) * saturate(sqrt(saturate(field.ShadingReceiver)) * debugBoost);
    }
    if (mode == 10)
    {
        return float3(0.0, 0.85, 1.0) * saturate(field.ReflectionReceiver * debugBoost);
    }
    if (mode == 11)
    {
        return float3(0.75, 0.75, 0.75) * saturate(sqrt(saturate(field.AOReceiver)) * debugBoost);
    }
    if (mode == 12)
    {
        float safetyBoost = min(debugBoost, 3.0);
        return saturate(float3(
            field.SafetySkySuppression,
            field.SafetySkinHighlightSuppression,
            max(field.SafetyWaterNoiseSuppression, 1.0 - field.TextureReliefSafety)) * safetyBoost);
    }

    return Dalashade_NormalField_EncodeNormal(field.CombinedNormal);
}

#endif
