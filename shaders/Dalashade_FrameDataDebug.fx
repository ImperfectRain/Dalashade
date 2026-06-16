#include "ReShade.fxh"
#include "Dalashade_FrameData.fxh"

// Internal contract visualizer for Dalashade FrameData. This shader is manual
// and disabled by default; mode 0 is pass-through even if the technique is on.

uniform float Dalashade_MaterialFoliage < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Foliage"; > = 0.0;
uniform float Dalashade_MaterialWaterSpecular < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Water/Specular"; > = 0.0;
uniform float Dalashade_MaterialWaterPlane < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Water Plane"; > = 0.0;
uniform float Dalashade_MaterialSpecularGlint < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Specular Glint"; > = 0.0;
uniform float Dalashade_MaterialSandDust < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Sand/Dust"; > = 0.0;
uniform float Dalashade_MaterialSnowIce < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Snow/Ice"; > = 0.0;
uniform float Dalashade_MaterialStoneRuins < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Stone/Ruins"; > = 0.0;
uniform float Dalashade_MaterialMetalIndustrial < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Metal/Industrial"; > = 0.0;
uniform float Dalashade_MaterialCrystalAether < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Crystal/Aether"; > = 0.0;
uniform float Dalashade_MaterialNeonGlass < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Neon/Glass"; > = 0.0;
uniform float Dalashade_MaterialFireLavaHeat < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Fire/Lava/Heat"; > = 0.0;
uniform float Dalashade_MaterialSkyCloudFog < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Sky/Cloud/Fog"; > = 0.0;
uniform float Dalashade_MaterialSkinProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Skin Protection"; > = 0.0;
uniform float Dalashade_MaterialVoidDarkness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Material Void/Darkness"; > = 0.0;

uniform float Dalashade_WaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Water Context"; > = 0.0;
uniform float Dalashade_CoastalContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Coastal Context"; > = 0.0;
uniform float Dalashade_OpenOceanContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Open Ocean Context"; > = 0.0;
uniform float Dalashade_ShallowWaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Shallow Water Context"; > = 0.0;
uniform float Dalashade_WetSurfaceContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Wet Surface Context"; > = 0.0;

uniform bool Dalashade_EnableDepthAssist < ui_label = "Enable Depth Assist"; > = false;
uniform float Dalashade_DepthAssistStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Strength"; > = 0.0;
uniform float Dalashade_DepthAssistConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Confidence Floor"; > = 0.0;
uniform float Dalashade_DepthConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Confidence Floor"; > = 0.0;

uniform float Dalashade_NormalFieldEnabled < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Enabled"; > = 0.0;
uniform float Dalashade_NormalFieldStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Strength"; > = 0.0;
uniform float Dalashade_NormalDepthStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Depth Strength"; > = 0.50;
uniform float Dalashade_NormalDetailStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Detail Strength"; > = 0.25;
uniform float Dalashade_NormalMaterialInfluence < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Material Influence"; > = 0.50;
uniform float Dalashade_NormalWaterSuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Water Suppression"; > = 0.80;
uniform float Dalashade_NormalSkinSuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Skin Suppression"; > = 0.90;
uniform float Dalashade_NormalSkySuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Sky Suppression"; > = 0.95;

uniform int Dalashade_FrameDataDebugMode <
    ui_type = "combo";
    ui_items = "Off/pass-through\0Safety pack\0Water pack\0Material pack\0Receiver pack\0Surface/normal pack\0Source-vs-receiver\0Water-vs-sky conflict\0Aether/metal/water ambiguity\0Inline resolver parity\0";
    ui_label = "Dalashade FrameData Debug Mode";
    ui_tooltip = "Internal FrameData contract visualizer. It verifies wrapper output against canonical material and normal resolvers.";
> = 0;

uniform float Dalashade_FrameDataDebugBoost <
    ui_type = "slider";
    ui_min = 0.25; ui_max = 8.0;
    ui_label = "Dalashade FrameData Debug Boost";
> = 2.0;

uniform float Dalashade_FrameDataDebugOpacity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade FrameData Debug Opacity";
> = 1.0;

Dalashade_FrameDataSettings Dalashade_FrameDataDebugSettings()
{
    Dalashade_FrameDataSettings settings = Dalashade_FrameData_DefaultSettings();

    settings.MaterialFoliage = Dalashade_MaterialFoliage;
    settings.MaterialWaterSpecular = Dalashade_MaterialWaterSpecular;
    settings.MaterialWaterPlane = Dalashade_MaterialWaterPlane;
    settings.MaterialSpecularGlint = Dalashade_MaterialSpecularGlint;
    settings.MaterialSandDust = Dalashade_MaterialSandDust;
    settings.MaterialSnowIce = Dalashade_MaterialSnowIce;
    settings.MaterialStoneRuins = Dalashade_MaterialStoneRuins;
    settings.MaterialMetalIndustrial = Dalashade_MaterialMetalIndustrial;
    settings.MaterialCrystalAether = Dalashade_MaterialCrystalAether;
    settings.MaterialNeonGlass = Dalashade_MaterialNeonGlass;
    settings.MaterialFireLavaHeat = Dalashade_MaterialFireLavaHeat;
    settings.MaterialSkyCloudFog = Dalashade_MaterialSkyCloudFog;
    settings.MaterialSkinProtection = Dalashade_MaterialSkinProtection;
    settings.MaterialVoidDarkness = Dalashade_MaterialVoidDarkness;

    settings.WaterContext = Dalashade_WaterContext;
    settings.CoastalContext = Dalashade_CoastalContext;
    settings.OpenOceanContext = Dalashade_OpenOceanContext;
    settings.ShallowWaterContext = Dalashade_ShallowWaterContext;
    settings.WetSurfaceContext = Dalashade_WetSurfaceContext;

    settings.DepthAssistEnabled = Dalashade_EnableDepthAssist ? 1.0 : 0.0;
    settings.DepthAssistStrength = Dalashade_DepthAssistStrength;
    settings.DepthAssistConfidenceFloor = max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor);

    settings.NormalFieldEnabled = Dalashade_NormalFieldEnabled;
    settings.NormalFieldStrength = Dalashade_NormalFieldStrength;
    settings.NormalDepthStrength = Dalashade_NormalDepthStrength;
    settings.NormalDetailStrength = Dalashade_NormalDetailStrength;
    settings.NormalMaterialInfluence = Dalashade_NormalMaterialInfluence;
    settings.NormalWaterSuppression = Dalashade_NormalWaterSuppression;
    settings.NormalSkinSuppression = Dalashade_NormalSkinSuppression;
    settings.NormalSkySuppression = Dalashade_NormalSkySuppression;

    return settings;
}

float3 Dalashade_FrameDataDebugColor(
    float3 source,
    float2 uv,
    Dalashade_FrameBaseData baseData,
    Dalashade_FrameDataSettings settings,
    int mode)
{
    float boost = clamp(Dalashade_FrameDataDebugBoost, 0.25, 8.0);

    if (mode == 1)
    {
        return saturate(float3(
            baseData.Safety.SkyReject,
            baseData.Safety.SkinReject,
            max(baseData.Safety.HighlightProtect, max(baseData.Safety.BrightSandProtect, baseData.Safety.SnowProtect))) * boost);
    }

    if (mode == 2)
    {
        return saturate(float3(
            baseData.Water.WaterPixelConfidence,
            baseData.Water.WaterReceiver,
            max(baseData.Water.WetShoreline, baseData.Water.SpecularGlint)) * boost);
    }

    if (mode == 3)
    {
        return saturate(float3(
            max(baseData.Material.SandDust, max(baseData.Material.StoneRuins, baseData.Material.FireLavaHeat)),
            max(baseData.Material.Foliage, baseData.Material.SnowIce),
            max(baseData.Material.CrystalAether, max(baseData.Material.NeonGlass, baseData.Material.VoidDarkness))) * boost);
    }

    if (mode == 4)
    {
        return saturate(float3(
            baseData.Receivers.ReflectionReceiver,
            baseData.Receivers.AOReceiver,
            baseData.Receivers.StructureReceiver) * boost);
    }

    if (mode == 5)
    {
        Dalashade_FrameSurfaceData surface = Dalashade_ResolveFrameSurfaceData(source, uv, baseData, settings);
        float3 normalColor = Dalashade_NormalField_EncodeNormal(surface.Normal);
        return lerp(float3(0.5, 0.5, 0.5), normalColor, saturate(surface.NormalConfidence * boost));
    }

    if (mode == 6)
    {
        float sourceSupport = max(baseData.Water.WaterSource, max(baseData.Water.SkySource, baseData.Receivers.LightSourceConfidence));
        float receiverSupport = max(baseData.Water.WaterReceiver, baseData.Receivers.ReflectionReceiver);
        float structureSupport = max(baseData.Receivers.AOReceiver, baseData.Receivers.StructureReceiver);
        return saturate(float3(sourceSupport, receiverSupport, structureSupport) * boost);
    }

    if (mode == 7)
    {
        return saturate(float3(
            baseData.Water.WaterSkyConflict,
            baseData.Water.WaterPixelConfidence,
            max(baseData.Water.SkySource, baseData.Safety.SkyReject)) * boost);
    }

    if (mode == 8)
    {
        return saturate(float3(
            baseData.Water.WaterPixelConfidence,
            max(baseData.Material.CrystalAether, baseData.Material.NeonGlass),
            max(baseData.Material.MetalIndustrial, baseData.Receivers.StructureReceiver)) * boost);
    }

    Dalashade_MaterialResolve material = Dalashade_FrameData_ResolveCanonicalMaterial(source, uv, settings);
    Dalashade_WaterResolve water = Dalashade_FrameData_ResolveCanonicalWater(source, uv, settings);
    Dalashade_SafetyResolve safety = Dalashade_FrameData_ResolveCanonicalSafety(source, uv, material, water, settings);
    Dalashade_NormalField field = Dalashade_ResolveNormalField(
        source,
        uv,
        material,
        water,
        safety,
        settings.NormalFieldEnabled,
        settings.NormalFieldStrength,
        settings.NormalDepthStrength,
        settings.NormalDetailStrength,
        settings.NormalMaterialInfluence,
        settings.NormalWaterSuppression,
        settings.NormalSkinSuppression,
        settings.NormalSkySuppression);
    Dalashade_FrameSurfaceData surfaceParity = Dalashade_ResolveFrameSurfaceData(source, uv, baseData, settings);

    float materialDiff = abs(baseData.Material.Foliage - material.Foliage)
        + abs(baseData.Material.SandDust - material.SandDust)
        + abs(baseData.Material.SnowIce - material.SnowIce)
        + abs(baseData.Material.CrystalAether - material.CrystalAether)
        + abs(baseData.Material.NeonGlass - material.NeonGlass);
    float waterDiff = abs(baseData.Water.WaterReceiver - water.WaterReceiver)
        + abs(baseData.Water.WaterSource - water.WaterSource)
        + abs(baseData.Water.HorizonOnly - water.HorizonOnlyConfidence)
        + abs(baseData.Water.WaterSkyConflict - water.WaterSkyConflict);
    float safetyDiff = abs(baseData.Safety.SkyReject - safety.SkyReject)
        + abs(baseData.Safety.SkinReject - safety.SkinReject)
        + abs(baseData.Safety.HighlightProtect - safety.HighlightProtect);
    float surfaceDiff = abs(surfaceParity.NormalConfidence - field.NormalConfidence)
        + abs(surfaceParity.ReflectionReceiverSupport - field.ReflectionReceiver)
        + abs(surfaceParity.AOReceiverSupport - field.AOReceiver);

    return saturate(float3(materialDiff + waterDiff, safetyDiff, surfaceDiff) * 32.0 * boost);
}

float4 Dalashade_FrameDataDebugPass(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    float3 source = tex2D(ReShade::BackBuffer, texcoord).rgb;
    int mode = Dalashade_FrameDataDebugMode;

    if (mode <= 0)
    {
        return float4(source, 1.0);
    }

    Dalashade_FrameDataSettings settings = Dalashade_FrameDataDebugSettings();
    Dalashade_FrameBaseData baseData = Dalashade_ResolveFrameBaseData(source, texcoord, settings);
    float3 debugColor = Dalashade_FrameDataDebugColor(source, texcoord, baseData, settings, mode);

    return float4(lerp(source, debugColor, saturate(Dalashade_FrameDataDebugOpacity)), 1.0);
}

technique Dalashade_FrameDataDebug
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_FrameDataDebugPass;
    }
}
