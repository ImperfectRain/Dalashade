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

uniform float Dalashade_MaterialWaterPlane <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water Plane";
    ui_tooltip = "Optional split water-plane plausibility. If left at 0, the combined Water/Specular value still gates water-plane masks.";
> = 0.0;

uniform float Dalashade_MaterialSpecularGlint <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Specular Glint";
    ui_tooltip = "Optional split glint plausibility. If left at 0, the combined Water/Specular value still gates glint masks.";
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

uniform float Dalashade_MaterialStoneRuins <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Stone/Ruins";
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
    ui_items = "Off/pass-through\0Overview final masks\0Combined confidence\0Raw sky/fog\0Gated sky/fog\0Final sky/fog\0Raw foliage strong\0Organic green surface\0Final foliage influence\0Raw water/specular combined\0Gated water/specular combined\0Final water/specular combined\0Raw snow/ice\0Gated snow/ice\0Final snow/ice\0Raw sand/dust\0Gated sand/dust\0Final sand/dust\0Depth confidence\0Depth-assisted sky/fog\0Stone/ruins\0Metal/industrial\0Crystal/aether\0Neon/glass\0Fire/lava/heat\0Skin-protection\0Void/darkness\0Raw water plane\0Gated water plane\0Final water plane\0Raw specular glint\0Gated specular glint\0Final specular glint\0";
    ui_label = "Dalashade Material Debug Mode";
    ui_tooltip = "False-color material heuristic visualizer. Raw modes show pixel evidence, gated modes show scene-scaled evidence, and final modes show conflict-resolved masks. These are not true engine material IDs.";
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

uniform bool Dalashade_EnableDepthAssist <
    ui_label = "Enable Depth Assist";
    ui_tooltip = "Optional helper for material masks. Disabled by default; masks still work without depth.";
> = false;

uniform float Dalashade_DepthAssistStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Depth Assist Strength";
    ui_tooltip = "How much valid depth may boost or suppress ambiguous material masks.";
> = 0.0;

uniform float Dalashade_DepthAssistConfidenceFloor <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Depth Assist Confidence Floor";
    ui_tooltip = "Minimum depth confidence when depth assist is enabled. Keep low unless depth is verified.";
> = 0.0;

uniform float Dalashade_DepthConfidenceFloor <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Depth Confidence Floor";
    ui_tooltip = "Alias for generated presets that use the shorter depth-confidence name.";
> = 0.0;

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

    Dalashade_MaterialSignals signals = Dalashade_GetMaterialSignals(
        source,
        texcoord,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_RawMaterialCandidates raw = Dalashade_GetRawMaterialCandidates(signals);
    Dalashade_GatedMaterialCandidates gated = Dalashade_GetGatedMaterialCandidatesWithWaterSplit(
        raw,
        Dalashade_MaterialFoliage,
        Dalashade_MaterialWaterSpecular,
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
        Dalashade_MaterialVoidDarkness);
    Dalashade_MaterialMasks masks = Dalashade_ResolveFinalMaterialMasks(signals, raw, gated);
    Dalashade_MaterialSignals noDepthSignals = Dalashade_GetMaterialSignals(source, texcoord);
    Dalashade_RawMaterialCandidates noDepthRaw = Dalashade_GetRawMaterialCandidates(noDepthSignals);
    Dalashade_GatedMaterialCandidates noDepthGated = Dalashade_GetGatedMaterialCandidatesWithWaterSplit(
        noDepthRaw,
        Dalashade_MaterialFoliage,
        Dalashade_MaterialWaterSpecular,
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
        Dalashade_MaterialVoidDarkness);
    Dalashade_MaterialMasks noDepthMasks = Dalashade_ResolveFinalMaterialMasks(noDepthSignals, noDepthRaw, noDepthGated);

    float confidence = masks.CombinedConfidence;
    float3 debugColor = Dalashade_GetMaterialOverviewColor(masks);

    if (mode == 2)
    {
        confidence = masks.CombinedConfidence;
        debugColor = confidence.xxx;
    }
    else if (mode == 3)
    {
        confidence = raw.SkyCloudFog;
        debugColor = float3(0.18, 0.42, 1.00) * confidence;
    }
    else if (mode == 4)
    {
        confidence = gated.SkyCloudFog;
        debugColor = float3(0.18, 0.72, 1.00) * confidence;
    }
    else if (mode == 5)
    {
        confidence = masks.SkyCloudFog;
        debugColor = float3(0.18, 0.42, 1.00) * confidence;
    }
    else if (mode == 6)
    {
        confidence = raw.FoliageStrong;
        debugColor = float3(0.05, 0.95, 0.16) * confidence;
    }
    else if (mode == 7)
    {
        confidence = raw.OrganicGreenSurface;
        debugColor = float3(0.45, 0.34, 0.08) * confidence;
    }
    else if (mode == 8)
    {
        confidence = saturate(masks.FoliageStrong + masks.OrganicGreenSurface * 0.55);
        debugColor = masks.FoliageStrong * float3(0.05, 0.95, 0.16) + masks.OrganicGreenSurface * float3(0.45, 0.34, 0.08);
    }
    else if (mode == 9)
    {
        confidence = raw.WaterSpecular;
        debugColor = float3(0.00, 0.85, 1.00) * confidence;
    }
    else if (mode == 10)
    {
        confidence = gated.WaterSpecular;
        debugColor = float3(0.00, 0.85, 1.00) * confidence;
    }
    else if (mode == 11)
    {
        confidence = masks.WaterSpecular;
        debugColor = float3(0.00, 0.85, 1.00) * confidence;
    }
    else if (mode == 12)
    {
        confidence = raw.SnowIce;
        debugColor = float3(0.78, 0.92, 1.00) * confidence;
    }
    else if (mode == 13)
    {
        confidence = gated.SnowIce;
        debugColor = float3(0.78, 0.92, 1.00) * confidence;
    }
    else if (mode == 14)
    {
        confidence = masks.SnowIce;
        debugColor = float3(0.78, 0.92, 1.00) * confidence;
    }
    else if (mode == 15)
    {
        confidence = raw.SandDust;
        debugColor = float3(1.00, 0.66, 0.12) * confidence;
    }
    else if (mode == 16)
    {
        confidence = gated.SandDust;
        debugColor = float3(1.00, 0.66, 0.12) * confidence;
    }
    else if (mode == 17)
    {
        confidence = masks.SandDust;
        debugColor = float3(1.00, 0.66, 0.12) * confidence;
    }
    else if (mode == 18)
    {
        confidence = saturate(signals.DepthConfidence + signals.DepthInvalidConfidence);
        debugColor = float3(signals.DepthNearConfidence, signals.DepthFarConfidence, signals.DepthInvalidConfidence);
    }
    else if (mode == 19)
    {
        float assistedDelta = saturate(abs(masks.SkyCloudFog - noDepthMasks.SkyCloudFog) * 4.0);
        confidence = max(masks.SkyCloudFog, assistedDelta);
        debugColor = float3(noDepthMasks.SkyCloudFog, masks.SkyCloudFog, assistedDelta);
    }
    else if (mode == 20)
    {
        confidence = masks.StoneRuins;
        debugColor = float3(0.42, 0.40, 0.34) * confidence;
    }
    else if (mode == 21)
    {
        confidence = masks.MetalIndustrial;
        debugColor = float3(0.45, 0.56, 0.66) * confidence;
    }
    else if (mode == 22)
    {
        confidence = masks.CrystalAether;
        debugColor = float3(0.55, 0.22, 1.00) * confidence;
    }
    else if (mode == 23)
    {
        confidence = masks.NeonGlass;
        debugColor = float3(1.00, 0.00, 0.85) * confidence;
    }
    else if (mode == 24)
    {
        confidence = masks.FireLavaHeat;
        debugColor = float3(1.00, 0.18, 0.04) * confidence;
    }
    else if (mode == 25)
    {
        confidence = masks.SkinProtection;
        debugColor = float3(1.00, 0.54, 0.42) * confidence;
    }
    else if (mode == 26)
    {
        confidence = masks.VoidDarkness;
        debugColor = float3(0.38, 0.05, 0.75) * confidence;
    }
    else if (mode == 27)
    {
        confidence = raw.WaterPlane;
        debugColor = float3(0.00, 0.85, 1.00) * confidence;
    }
    else if (mode == 28)
    {
        confidence = gated.WaterPlane;
        debugColor = float3(0.00, 0.85, 1.00) * confidence;
    }
    else if (mode == 29)
    {
        confidence = masks.WaterPlane;
        debugColor = float3(0.00, 0.85, 1.00) * confidence;
    }
    else if (mode == 30)
    {
        confidence = raw.SpecularGlint;
        debugColor = float3(0.62, 0.86, 1.00) * confidence;
    }
    else if (mode == 31)
    {
        confidence = gated.SpecularGlint;
        debugColor = float3(0.62, 0.86, 1.00) * confidence;
    }
    else if (mode == 32)
    {
        confidence = masks.SpecularGlint;
        debugColor = float3(0.62, 0.86, 1.00) * confidence;
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
