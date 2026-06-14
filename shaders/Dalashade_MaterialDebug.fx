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

uniform float Dalashade_WaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Water Context"; > = 0.0;
uniform float Dalashade_CoastalContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Coastal Context"; > = 0.0;
uniform float Dalashade_OpenOceanContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Open Ocean Context"; > = 0.0;
uniform float Dalashade_ShallowWaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Shallow Water Context"; > = 0.0;
uniform float Dalashade_WetSurfaceContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Wet Surface Context"; > = 0.0;

uniform int Dalashade_MaterialDebugMode <
    ui_type = "combo";
    ui_items = "Off/pass-through\0Overview final masks\0Combined confidence\0Raw sky/fog\0Gated sky/fog\0Final sky/fog\0Raw foliage strong\0Organic green surface\0Final foliage influence\0Raw water/specular combined\0Gated water/specular combined\0Final water/specular combined\0Raw snow/ice\0Gated snow/ice\0Final snow/ice\0Raw sand/dust\0Gated sand/dust\0Final sand/dust\0Depth confidence\0Depth-assisted sky/fog\0Stone/ruins\0Metal/industrial\0Crystal/aether\0Neon/glass\0Fire/lava/heat\0Skin-protection\0Void/darkness\0Raw water plane\0Gated water plane\0Final water plane\0Raw specular glint\0Gated specular glint\0Final specular glint\0Water resolver overview\0Raw cyan water\0Raw deep water\0Shallow water\0Deep water\0Water horizon\0Wet shoreline\0Foam/edge\0Water receiver\0Water source\0Sky source vs reject\0Sand/skin reject\0Water coherence\0Shared safety overview\0Shared receiver confidence\0Shared light source confidence\0SurfaceReflection receiver preview\0SceneGI receiver/source preview\0AtmosphereBloom eligibility preview\0WeatherAtmosphere air influence preview\0SmartSharpen dampening preview\0AdaptiveGrade protection preview\0Water/sky conflict\0Water pixel confidence\0Sky pixel confidence\0Water receiver vs horizon\0Receiver confidence split\0";
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
    Dalashade_MaterialCompetition competition = Dalashade_ResolveMaterialCompetition(signals, raw, gated);
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
    Dalashade_WaterResolve water = Dalashade_ResolveWater(
        source,
        texcoord,
        Dalashade_WaterContext,
        Dalashade_CoastalContext,
        Dalashade_OpenOceanContext,
        Dalashade_ShallowWaterContext,
        Dalashade_WetSurfaceContext,
        Dalashade_MaterialWaterPlane,
        Dalashade_MaterialSpecularGlint,
        Dalashade_MaterialSandDust,
        Dalashade_MaterialSkyCloudFog,
        Dalashade_MaterialSkinProtection,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_MaterialResolve material = Dalashade_ResolveMaterials(
        source,
        texcoord,
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
        Dalashade_MaterialVoidDarkness,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_SafetyResolve safety = Dalashade_ResolveSafety(
        source,
        texcoord,
        material,
        water,
        0.0,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));

    float confidence = saturate(max(masks.CombinedConfidence, max(material.ReceiverConfidence, material.LightSourceConfidence)));
    float3 debugColor = Dalashade_GetMaterialDebugColor(material);

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
    else if (mode == 33)
    {
        confidence = water.Confidence;
        debugColor = Dalashade_GetWaterDebugColor(water);
    }
    else if (mode == 34)
    {
        confidence = water.RawCyanWater;
        debugColor = float3(0.00, 1.00, 0.86) * confidence;
    }
    else if (mode == 35)
    {
        confidence = water.RawDeepWater;
        debugColor = float3(0.00, 0.22, 0.60) * confidence;
    }
    else if (mode == 36)
    {
        confidence = water.ShallowWater;
        debugColor = float3(0.00, 1.00, 0.90) * confidence;
    }
    else if (mode == 37)
    {
        confidence = water.DeepWater;
        debugColor = float3(0.00, 0.28, 0.62) * confidence;
    }
    else if (mode == 38)
    {
        confidence = water.WaterHorizon;
        debugColor = float3(0.05, 0.48, 1.00) * confidence;
    }
    else if (mode == 39)
    {
        confidence = water.WetShoreline;
        debugColor = float3(0.55, 1.00, 1.00) * confidence;
    }
    else if (mode == 40)
    {
        confidence = water.FoamOrEdge;
        debugColor = float3(0.92, 1.00, 1.00) * confidence;
    }
    else if (mode == 41)
    {
        confidence = water.WaterReceiver;
        debugColor = float3(0.00, 0.86, 1.00) * confidence;
    }
    else if (mode == 42)
    {
        confidence = water.WaterSource;
        debugColor = float3(0.00, 0.58, 1.00) * confidence;
    }
    else if (mode == 43)
    {
        confidence = max(water.SkySource, water.SkyReject);
        debugColor = saturate(water.SkySource * float3(0.10, 0.30, 1.00) + water.SkyReject * float3(0.80, 0.02, 0.04));
    }
    else if (mode == 44)
    {
        confidence = max(water.SandReject, water.SkinReject);
        debugColor = saturate(water.SandReject * float3(1.00, 0.46, 0.02) + water.SkinReject * float3(1.00, 0.30, 0.52));
    }
    else if (mode == 45)
    {
        confidence = water.WaterCoherence;
        debugColor = float3(0.18, 0.82, 1.00) * confidence;
    }
    else if (mode == 46)
    {
        confidence = saturate(max(max(safety.SkyReject, safety.SkinReject), max(max(safety.HighlightProtect, safety.WaterAOReject), max(safety.FoliageNoiseReject, safety.UIDepthRisk))));
        debugColor = Dalashade_GetSafetyDebugColor(safety);
    }
    else if (mode == 47)
    {
        confidence = material.ReceiverConfidence;
        debugColor = lerp(float3(0.0, 0.20, 0.06), float3(0.16, 1.00, 0.46), confidence) * confidence;
    }
    else if (mode == 48)
    {
        confidence = material.LightSourceConfidence;
        debugColor = lerp(float3(0.12, 0.04, 0.0), float3(1.00, 0.66, 0.16), confidence) * confidence;
    }
    else if (mode == 49)
    {
        float wetHardReceiver = saturate(material.ReceiverConfidence * max(Dalashade_WetSurfaceContext, water.WetShoreline) * (1.0 - safety.SkyReject) * (1.0 - safety.SkinReject));
        float aetherReceiver = saturate((material.CrystalAether + material.NeonGlass) * material.SurfaceSmoothness * (1.0 - safety.SkyReject));
        confidence = saturate(water.WaterReceiver + wetHardReceiver + aetherReceiver);
        debugColor = saturate(water.WaterReceiver * float3(0.0, 0.85, 1.0) + wetHardReceiver * float3(0.28, 0.52, 0.78) + aetherReceiver * float3(0.70, 0.22, 1.0));
    }
    else if (mode == 50)
    {
        float receiver = saturate(material.ReceiverConfidence * (1.0 - safety.SkyReject) * (1.0 - safety.SkinReject));
        float source = saturate(material.LightSourceConfidence + water.WaterSource * 0.35);
        confidence = max(receiver, source);
        debugColor = saturate(receiver * float3(0.08, 0.86, 0.26) + source * float3(1.0, 0.48, 0.08));
    }
    else if (mode == 51)
    {
        float glow = saturate(material.SpecularGlint * 0.60 + water.FoamOrEdge * 0.45 + material.CrystalAether + material.NeonGlass + material.FireLavaHeat);
        float skyDiffuse = saturate(material.SkyCloudFog * (1.0 - safety.HighlightProtect * 0.60));
        confidence = saturate(glow + skyDiffuse * 0.35);
        debugColor = saturate(glow * float3(0.82, 0.58, 1.0) + skyDiffuse * float3(0.16, 0.34, 1.0));
    }
    else if (mode == 52)
    {
        float air = saturate(material.SkyCloudFog + material.Foliage * 0.35 + material.SandDust * 0.45 + material.SnowIce * 0.45 + water.WetShoreline * 0.30 + material.CrystalAether * 0.30);
        confidence = air;
        debugColor = saturate(material.SkyCloudFog * float3(0.18, 0.42, 1.0) + material.Foliage * float3(0.14, 0.76, 0.20) + material.SandDust * float3(1.0, 0.58, 0.08) + material.SnowIce * float3(0.74, 0.92, 1.0) + water.WetShoreline * float3(0.20, 0.82, 1.0) + material.CrystalAether * float3(0.54, 0.18, 1.0));
    }
    else if (mode == 53)
    {
        confidence = saturate(safety.FoliageNoiseReject + safety.WaterAOReject + safety.SkyReject + safety.SkinReject + safety.SnowProtect + material.SpecularGlint * 0.42);
        debugColor = saturate(safety.FoliageNoiseReject * float3(0.08, 0.82, 0.16) + safety.WaterAOReject * float3(0.0, 0.84, 1.0) + safety.SkyReject * float3(0.10, 0.34, 1.0) + safety.SkinReject * float3(1.0, 0.54, 0.42) + safety.SnowProtect * float3(0.74, 0.90, 1.0) + material.SpecularGlint * float3(0.62, 0.86, 1.0));
    }
    else if (mode == 54)
    {
        confidence = saturate(material.Foliage * 0.45 + safety.BrightSandProtect + safety.SnowProtect + safety.SkinReject + material.CrystalAether * 0.45 + material.NeonGlass * 0.35 + material.VoidDarkness * 0.50);
        debugColor = saturate(material.Foliage * float3(0.05, 0.80, 0.14) + safety.BrightSandProtect * float3(1.0, 0.58, 0.08) + safety.SnowProtect * float3(0.78, 0.92, 1.0) + safety.SkinReject * float3(1.0, 0.54, 0.42) + material.CrystalAether * float3(0.55, 0.22, 1.0) + material.NeonGlass * float3(1.0, 0.0, 0.85) + material.VoidDarkness * float3(0.38, 0.05, 0.75));
    }
    else if (mode == 55)
    {
        // Water/sky decision view: red = sky wins, cyan = water wins, yellow = unresolved conflict.
        confidence = max(competition.WaterSkyConflict, max(competition.WaterPixelConfidence, competition.SkyPixelConfidence));
        debugColor = Dalashade_GetWaterSkyConflictDebugColor(competition);
    }
    else if (mode == 56)
    {
        // Actual likely water pixels only; source-only horizon evidence is intentionally excluded.
        confidence = competition.WaterPixelConfidence;
        debugColor = Dalashade_GetWaterPixelConfidenceDebugColor(competition);
    }
    else if (mode == 57)
    {
        // Actual likely sky/cloud/fog pixels only; this is not the broader raw sky score.
        confidence = competition.SkyPixelConfidence;
        debugColor = Dalashade_GetSkyPixelConfidenceDebugColor(competition);
    }
    else if (mode == 58)
    {
        // Separates receiver water from horizon/source-only water and rejected sky.
        confidence = max(competition.WaterReceiverConfidence, max(competition.HorizonOnlyConfidence, competition.SkyPixelConfidence));
        debugColor = Dalashade_GetWaterReceiverHorizonDebugColor(competition);
    }
    else if (mode == 59)
    {
        // Specialized receiver split: cyan = reflection, green = structure, yellow = AO, faint gray = legacy receiver.
        confidence = max(material.ReflectionReceiverConfidence, max(material.AOReceiverConfidence, max(material.StructureReceiverConfidence, material.ReceiverConfidence * 0.35)));
        debugColor = Dalashade_GetReceiverSplitDebugColor(material);
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
