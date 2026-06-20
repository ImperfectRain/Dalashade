#include "ReShade.fxh"

// Dalapad debug bridge visualizer. Diagnostic-only.
// The native addon may capture many raw MRT candidates, but this shader only
// binds a small paged scan window to stay under runtime sampler limits.

texture Dalapad_DebugTexture
{
    Width = 256;
    Height = 256;
    Format = RGBA8;
};

sampler Dalapad_DebugSampler
{
    Texture = Dalapad_DebugTexture;
    AddressU = Clamp;
    AddressV = Clamp;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
};

texture2D Dalapad_Scan0Texture : DALAPAD_SCAN0;
sampler Dalapad_Scan0Sampler { Texture = Dalapad_Scan0Texture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };
texture2D Dalapad_Scan1Texture : DALAPAD_SCAN1;
sampler Dalapad_Scan1Sampler { Texture = Dalapad_Scan1Texture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };
texture2D Dalapad_Scan2Texture : DALAPAD_SCAN2;
sampler Dalapad_Scan2Sampler { Texture = Dalapad_Scan2Texture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };
texture2D Dalapad_Scan3Texture : DALAPAD_SCAN3;
sampler Dalapad_Scan3Sampler { Texture = Dalapad_Scan3Texture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };

texture2D Dalapad_DepthTexture : DALAPAD_DEPTH;
sampler Dalapad_DepthSampler { Texture = Dalapad_DepthTexture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };

texture2D Dalapad_PinnedNormalTexture : DALAPAD_PINNED_NORMAL;
sampler Dalapad_PinnedNormalSampler { Texture = Dalapad_PinnedNormalTexture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };
texture2D Dalapad_PinnedAlbedoTexture : DALAPAD_PINNED_ALBEDO;
sampler Dalapad_PinnedAlbedoSampler { Texture = Dalapad_PinnedAlbedoTexture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };
texture2D Dalapad_PinnedMaskTexture : DALAPAD_PINNED_MASK;
sampler Dalapad_PinnedMaskSampler { Texture = Dalapad_PinnedMaskTexture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };
texture2D Dalapad_PinnedNormalAltTexture : DALAPAD_PINNED_NORMAL_ALT;
sampler Dalapad_PinnedNormalAltSampler { Texture = Dalapad_PinnedNormalAltTexture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };
texture2D Dalapad_PinnedEmissiveTexture : DALAPAD_PINNED_EMISSIVE;
sampler Dalapad_PinnedEmissiveSampler { Texture = Dalapad_PinnedEmissiveTexture; AddressU = Clamp; AddressV = Clamp; MinFilter = Point; MagFilter = Point; MipFilter = Point; };

uniform int Dalapad_DebugMode <
    ui_type = "combo";
    ui_items = "Off/pass-through\0Bridge status\0Show selected source\0RGB/channel inspect\0Red channel\0Green channel\0Blue channel\0Alpha channel\0Normal decode\0Luma/edge view\0Missing/stale pattern\0Quad RGB page\0Quad normal page\0Quad luma/edge page\0";
    ui_label = "Dalapad Debug Mode";
    ui_tooltip = "Debug-only Dalapad bridge viewer. It should not be enabled for normal gameplay.";
> = 2;

uniform int Dalapad_DebugSource <
    ui_type = "combo";
    ui_items = "Synthetic bridge test\0Scan slot 0\0Scan slot 1\0Scan slot 2\0Scan slot 3\0Depth candidate\0Pinned dense surface detail\0Pinned albedo/luma\0Pinned surface/object mask\0Pinned alternate surface detail\0Pinned emissive/lighting\0";
    ui_label = "Dalapad Debug Source";
> = 0;

uniform int Dalapad_QuadPage <
    ui_type = "slider";
    ui_min = 0; ui_max = 15;
    ui_label = "Dalapad Quad Page";
    ui_tooltip = "Pages four raw MRT sources at a time. Page 0 is G0 MRT0-3, page 1 is G0 MRT4-7, page 2 is G1 MRT0-3, and so on.";
> = 0;

uniform int Dalapad_DebugAvailable < ui_label = "Dalapad Debug Available"; > = 0;
uniform int Dalapad_DebugWidth < ui_label = "Dalapad Debug Width"; > = 0;
uniform int Dalapad_DebugHeight < ui_label = "Dalapad Debug Height"; > = 0;
uniform int Dalapad_DebugFrameAge < ui_label = "Dalapad Debug Frame Age"; > = 9999;

uniform int Dalapad_Scan0Available < ui_label = "Dalapad Scan0 Available"; > = 0;
uniform int Dalapad_Scan1Available < ui_label = "Dalapad Scan1 Available"; > = 0;
uniform int Dalapad_Scan2Available < ui_label = "Dalapad Scan2 Available"; > = 0;
uniform int Dalapad_Scan3Available < ui_label = "Dalapad Scan3 Available"; > = 0;
uniform int Dalapad_DepthAvailable < ui_label = "Dalapad Depth Available"; > = 0;
uniform int Dalapad_PinnedNormalAvailable < ui_label = "Dalapad Pinned Normal Available"; > = 0;
uniform int Dalapad_PinnedAlbedoAvailable < ui_label = "Dalapad Pinned Albedo Available"; > = 0;
uniform int Dalapad_PinnedMaskAvailable < ui_label = "Dalapad Pinned Mask Available"; > = 0;
uniform int Dalapad_PinnedNormalAltAvailable < ui_label = "Dalapad Pinned Normal Alt Available"; > = 0;
uniform int Dalapad_PinnedEmissiveAvailable < ui_label = "Dalapad Pinned Emissive Available"; > = 0;

uniform int Dalapad_Scan0Width < ui_label = "Dalapad Scan0 Width"; > = 0;
uniform int Dalapad_Scan0Height < ui_label = "Dalapad Scan0 Height"; > = 0;
uniform int Dalapad_Scan1Width < ui_label = "Dalapad Scan1 Width"; > = 0;
uniform int Dalapad_Scan1Height < ui_label = "Dalapad Scan1 Height"; > = 0;
uniform int Dalapad_Scan2Width < ui_label = "Dalapad Scan2 Width"; > = 0;
uniform int Dalapad_Scan2Height < ui_label = "Dalapad Scan2 Height"; > = 0;
uniform int Dalapad_Scan3Width < ui_label = "Dalapad Scan3 Width"; > = 0;
uniform int Dalapad_Scan3Height < ui_label = "Dalapad Scan3 Height"; > = 0;
uniform int Dalapad_DepthWidth < ui_label = "Dalapad Depth Width"; > = 0;
uniform int Dalapad_DepthHeight < ui_label = "Dalapad Depth Height"; > = 0;
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

uniform int Dalapad_ScanPageStart < ui_label = "Dalapad Scan Page Start"; > = 0;

uniform float Dalapad_DebugOpacity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalapad Debug Opacity";
> = 1.0;

float3 Dalapad_DebugChecker(float2 uv, float3 a, float3 b, float scale)
{
    float2 cell = floor(uv * scale);
    float parity = cell.x + cell.y - 2.0 * floor((cell.x + cell.y) * 0.5);
    return lerp(a, b, parity);
}

int Dalapad_DebugSourceAvailable(int source)
{
    if (source == 0) return Dalapad_DebugAvailable;
    if (source == 1) return Dalapad_Scan0Available;
    if (source == 2) return Dalapad_Scan1Available;
    if (source == 3) return Dalapad_Scan2Available;
    if (source == 4) return Dalapad_Scan3Available;
    if (source == 5) return Dalapad_DepthAvailable;
    if (source == 6) return Dalapad_PinnedNormalAvailable;
    if (source == 7) return Dalapad_PinnedAlbedoAvailable;
    if (source == 8) return Dalapad_PinnedMaskAvailable;
    if (source == 9) return Dalapad_PinnedNormalAltAvailable;
    if (source == 10) return Dalapad_PinnedEmissiveAvailable;
    return 0;
}

float4 Dalapad_DebugSourceSample(float2 uv, int source)
{
    if (source == 1) return tex2D(Dalapad_Scan0Sampler, uv);
    if (source == 2) return tex2D(Dalapad_Scan1Sampler, uv);
    if (source == 3) return tex2D(Dalapad_Scan2Sampler, uv);
    if (source == 4) return tex2D(Dalapad_Scan3Sampler, uv);
    if (source == 5)
    {
        float4 depth = tex2D(Dalapad_DepthSampler, uv);
        return float4(depth.r.xxx, 1.0);
    }
    if (source == 6) return tex2D(Dalapad_PinnedNormalSampler, uv);
    if (source == 7) return tex2D(Dalapad_PinnedAlbedoSampler, uv);
    if (source == 8) return tex2D(Dalapad_PinnedMaskSampler, uv);
    if (source == 9) return tex2D(Dalapad_PinnedNormalAltSampler, uv);
    if (source == 10) return tex2D(Dalapad_PinnedEmissiveSampler, uv);
    return tex2D(Dalapad_DebugSampler, uv);
}

float3 Dalapad_DebugStatusColor(float2 uv)
{
    if (Dalapad_DebugAvailable <= 0)
        return Dalapad_DebugChecker(uv, float3(1.0, 0.0, 0.8), float3(0.08, 0.0, 0.08), 24.0);

    if (Dalapad_DebugSourceAvailable(Dalapad_DebugSource) > 0)
        return float3(0.0, 0.8, 1.0);

    float stale = Dalapad_DebugFrameAge > 4 ? 1.0 : 0.0;
    float sizeOk = (Dalapad_DebugWidth > 0 && Dalapad_DebugHeight > 0) ? 1.0 : 0.0;
    return lerp(lerp(float3(1.0, 0.75, 0.0), float3(0.05, 0.9, 0.3), sizeOk), float3(1.0, 0.1, 0.0), stale);
}

float3 Dalapad_DebugMissingSourceColor(float2 uv, int source)
{
    float hue = frac(source * 0.137 + Dalapad_ScanPageStart * 0.017);
    float3 baseColor = lerp(float3(0.25, 0.55, 1.0), float3(1.0, 0.25, 0.6), hue);
    return Dalapad_DebugChecker(uv, baseColor, float3(0.02, 0.02, 0.02), 18.0);
}

float3 Dalapad_DebugLumaEdge(float2 uv, float4 sampleValue, int source)
{
    float2 pixel = ReShade::PixelSize;
    float luma = dot(sampleValue.rgb, float3(0.2126, 0.7152, 0.0722));
    float lumaX = dot(Dalapad_DebugSourceSample(uv + float2(pixel.x, 0.0), source).rgb, float3(0.2126, 0.7152, 0.0722));
    float lumaY = dot(Dalapad_DebugSourceSample(uv + float2(0.0, pixel.y), source).rgb, float3(0.2126, 0.7152, 0.0722));
    float edge = saturate(abs(luma - lumaX) * 12.0 + abs(luma - lumaY) * 12.0);
    return float3(luma, edge, edge);
}

float3 Dalapad_DebugVisualize(float2 uv, int source, int mode)
{
    float4 sampleValue = Dalapad_DebugSourceSample(uv, source);
    if (Dalapad_DebugSourceAvailable(source) <= 0)
        return Dalapad_DebugMissingSourceColor(uv, source);

    if (mode == 8 || mode == 12)
    {
        float3 decodedNormal = sampleValue.xyz * 2.0 - 1.0;
        decodedNormal *= rsqrt(max(dot(decodedNormal, decodedNormal), 0.00001));
        return decodedNormal * 0.5 + 0.5;
    }

    if (mode == 9 || mode == 13)
        return Dalapad_DebugLumaEdge(uv, sampleValue, source);

    if (mode == 4) return sampleValue.r.xxx;
    if (mode == 5) return sampleValue.g.xxx;
    if (mode == 6) return sampleValue.b.xxx;
    if (mode == 7) return sampleValue.a.xxx;
    if (mode == 10) return Dalapad_DebugChecker(uv, float3(1.0, 0.0, 0.8), float3(0.05, 0.05, 0.05), 32.0);
    return sampleValue.rgb;
}

float3 Dalapad_DebugQuadVisualize(float2 uv, int mode)
{
    float2 quad = step(0.5.xx, uv);
    int quadrantIndex = (int)(quad.x + quad.y * 2.0 + 0.5);
    float2 localUv = frac(uv * 2.0);
    return Dalapad_DebugVisualize(localUv, 1 + quadrantIndex, mode);
}

float4 Dalapad_DebugPass(float4 pos : SV_Position, float2 uv : TEXCOORD) : SV_Target
{
    float4 backbuffer = tex2D(ReShade::BackBuffer, uv);
    int mode = Dalapad_DebugMode;
    if (mode <= 0)
        return backbuffer;

    float3 debugColor = 0.0;
    if (mode == 1)
        debugColor = Dalapad_DebugStatusColor(uv);
    else if (mode >= 11 && mode <= 13)
        debugColor = Dalapad_DebugQuadVisualize(uv, mode);
    else
        debugColor = Dalapad_DebugVisualize(uv, clamp(Dalapad_DebugSource, 0, 10), mode);

    return float4(lerp(backbuffer.rgb, saturate(debugColor), saturate(Dalapad_DebugOpacity)), backbuffer.a);
}

technique Dalapad_Debug
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalapad_DebugPass;
    }
}
