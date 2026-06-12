#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"

// Dalashade Material Debug is an optional false-color visualizer.
// It shows screen-space material heuristic influence gated by MaterialIntent uniforms.
// It is not true FFXIV material-ID detection and is disabled by default.

uniform float Dalashade_MaterialFoliage <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Foliage";
> = 0.0;

uniform float Dalashade_MaterialWaterSpecular <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water/Specular";
> = 0.0;

uniform float Dalashade_MaterialSandDust <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sand/Dust";
> = 0.0;

uniform float Dalashade_MaterialSnowIce <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Snow/Ice";
> = 0.0;

uniform float Dalashade_MaterialMetalIndustrial <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Metal/Industrial";
> = 0.0;

uniform float Dalashade_MaterialCrystalAether <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Crystal/Aether";
> = 0.0;

uniform float Dalashade_MaterialNeonGlass <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Neon/Glass";
> = 0.0;

uniform float Dalashade_MaterialFireLavaHeat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Fire/Lava/Heat";
> = 0.0;

uniform float Dalashade_MaterialSkyCloudFog <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sky/Cloud/Fog";
> = 0.0;

uniform float Dalashade_MaterialSkinProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Skin Protection";
> = 0.0;

uniform float Dalashade_MaterialVoidDarkness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Void/Darkness";
> = 0.0;

uniform int Dalashade_MaterialDebugMode <
    ui_type = "combo";
    ui_items = "Off/pass-through\0Overview\0Foliage\0Water/specular\0Sand/dust\0Snow/ice\0Sky/fog\0Metal/industrial\0Crystal/aether\0Neon/glass\0Fire/lava/heat\0Skin-protection\0Void/darkness\0Combined confidence\0";
    ui_label = "Dalashade Material Debug Mode";
    ui_tooltip = "False-color material heuristic visualizer. These are inferred screen-space masks, not true engine material IDs.";
> = 0;

uniform float Dalashade_MaterialDebugOpacity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Debug Opacity";
> = 0.65;

uniform int Dalashade_MaterialDebugOverlayMode <
    ui_type = "combo";
    ui_items = "Full debug replacement\0Alpha blend over image\0Additive/tint overlay\0";
    ui_label = "Dalashade Material Debug Overlay Mode";
> = 1;

uniform float Dalashade_MaterialDebugStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Debug Strength";
> = 1.0;

float3 Dalashade_ApplyDebugOverlay(float3 source, float3 debugColor, float confidence)
{
    float opacity = saturate(Dalashade_MaterialDebugOpacity) * saturate(Dalashade_MaterialDebugStrength);
    float alpha = saturate(confidence * opacity);

    if (Dalashade_MaterialDebugOverlayMode == 0)
    {
        return debugColor;
    }

    if (Dalashade_MaterialDebugOverlayMode == 2)
    {
        return saturate(source + debugColor * alpha * 0.85);
    }

    return lerp(source, debugColor, alpha);
}

float4 Dalashade_MaterialDebugPass(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    float3 source = tex2D(ReShade::BackBuffer, texcoord).rgb;
    int mode = Dalashade_MaterialDebugMode;

    if (mode <= 0 || Dalashade_MaterialDebugStrength <= 0.0)
    {
        return float4(source, 1.0);
    }

    float foliageMask = Dalashade_GetFoliageMask(source, texcoord, Dalashade_MaterialFoliage);
    float waterSpecularMask = Dalashade_GetWaterSpecularMask(source, texcoord, Dalashade_MaterialWaterSpecular);
    float sandDustMask = Dalashade_GetSandDustMask(source, texcoord, Dalashade_MaterialSandDust);
    float snowIceMask = Dalashade_GetSnowIceMask(source, texcoord, Dalashade_MaterialSnowIce);
    float skyCloudFogMask = Dalashade_GetSkyFogMask(source, texcoord, Dalashade_MaterialSkyCloudFog);
    float metalIndustrialMask = Dalashade_GetMetalIndustrialMask(source, texcoord, Dalashade_MaterialMetalIndustrial);
    float crystalAetherMask = Dalashade_GetCrystalAetherMask(source, texcoord, Dalashade_MaterialCrystalAether);
    float neonGlassMask = Dalashade_GetNeonGlassMask(source, texcoord, Dalashade_MaterialNeonGlass);
    float fireLavaHeatMask = Dalashade_GetFireLavaHeatMask(source, texcoord, Dalashade_MaterialFireLavaHeat);
    float skinProtectionMask = Dalashade_GetSkinProtectionMask(source, texcoord, Dalashade_MaterialSkinProtection);
    float voidDarknessMask = Dalashade_GetVoidDarknessMask(source, texcoord, Dalashade_MaterialVoidDarkness);

    float combinedConfidence = saturate(
        foliageMask + waterSpecularMask + sandDustMask + snowIceMask + skyCloudFogMask +
        metalIndustrialMask + crystalAetherMask + neonGlassMask + fireLavaHeatMask +
        skinProtectionMask + voidDarknessMask);

    float confidence = combinedConfidence;
    float3 debugColor = Dalashade_GetMaterialOverviewColor(
        foliageMask,
        waterSpecularMask,
        sandDustMask,
        snowIceMask,
        skyCloudFogMask,
        metalIndustrialMask,
        crystalAetherMask,
        neonGlassMask,
        fireLavaHeatMask,
        skinProtectionMask,
        voidDarknessMask);

    if (mode == 2)
    {
        confidence = foliageMask;
        debugColor = float3(0.08, 0.95, 0.18) * confidence;
    }
    else if (mode == 3)
    {
        confidence = waterSpecularMask;
        debugColor = float3(0.00, 0.85, 1.00) * confidence;
    }
    else if (mode == 4)
    {
        confidence = sandDustMask;
        debugColor = float3(1.00, 0.66, 0.12) * confidence;
    }
    else if (mode == 5)
    {
        confidence = snowIceMask;
        debugColor = float3(0.78, 0.92, 1.00) * confidence;
    }
    else if (mode == 6)
    {
        confidence = skyCloudFogMask;
        debugColor = float3(0.18, 0.42, 1.00) * confidence;
    }
    else if (mode == 7)
    {
        confidence = metalIndustrialMask;
        debugColor = float3(0.45, 0.56, 0.66) * confidence;
    }
    else if (mode == 8)
    {
        confidence = crystalAetherMask;
        debugColor = float3(0.55, 0.22, 1.00) * confidence;
    }
    else if (mode == 9)
    {
        confidence = neonGlassMask;
        debugColor = float3(1.00, 0.00, 0.85) * confidence;
    }
    else if (mode == 10)
    {
        confidence = fireLavaHeatMask;
        debugColor = float3(1.00, 0.18, 0.04) * confidence;
    }
    else if (mode == 11)
    {
        confidence = skinProtectionMask;
        debugColor = float3(1.00, 0.54, 0.42) * confidence;
    }
    else if (mode == 12)
    {
        confidence = voidDarknessMask;
        debugColor = float3(0.38, 0.05, 0.75) * confidence;
    }
    else if (mode == 13)
    {
        confidence = combinedConfidence;
        debugColor = confidence.xxx;
    }

    return float4(Dalashade_ApplyDebugOverlay(source, saturate(debugColor), confidence), 1.0);
}

technique Dalashade_MaterialDebug
{
    pass MaterialDebug
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_MaterialDebugPass;
    }
}
