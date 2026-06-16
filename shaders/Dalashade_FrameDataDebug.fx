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

uniform float Dalashade_HighlightProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Highlight Protection";
> = 0.0;

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

    settings.HighlightProtection = Dalashade_HighlightProtection;
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
            baseData.SafetySkyReject,
            baseData.SafetySkinReject,
            max(baseData.SafetyHighlightProtect, max(baseData.SafetyBrightSandProtect, baseData.SafetySnowProtect))) * boost);
    }

    if (mode == 2)
    {
        return saturate(float3(
            baseData.WaterPixelConfidence,
            baseData.WaterReceiver,
            max(baseData.WaterWetShoreline, baseData.WaterSpecularGlint)) * boost);
    }

    if (mode == 3)
    {
        return saturate(float3(
            max(baseData.MaterialSandDust, max(baseData.MaterialStoneRuins, baseData.MaterialFireLavaHeat)),
            max(baseData.MaterialFoliage, baseData.MaterialSnowIce),
            max(baseData.MaterialCrystalAether, max(baseData.MaterialNeonGlass, baseData.MaterialVoidDarkness))) * boost);
    }

    if (mode == 4)
    {
        return saturate(float3(
            baseData.ReceiverReflection,
            baseData.ReceiverAO,
            baseData.ReceiverStructure) * boost);
    }

    if (mode == 5)
    {
        Dalashade_FrameSurfaceData surface = Dalashade_ResolveFrameSurfaceData(source, uv, baseData, settings);
        float3 normalColor = Dalashade_NormalField_EncodeNormal(surface.Normal);
        return lerp(float3(0.5, 0.5, 0.5), normalColor, saturate(surface.NormalConfidence * boost));
    }

    if (mode == 6)
    {
        float sourceSupport = max(baseData.WaterSource, max(baseData.WaterSkySource, baseData.SourceLightConfidence));
        float receiverSupport = max(baseData.WaterReceiver, baseData.ReceiverReflection);
        float structureSupport = max(baseData.ReceiverAO, baseData.ReceiverStructure);
        return saturate(float3(sourceSupport, receiverSupport, structureSupport) * boost);
    }

    if (mode == 7)
    {
        return saturate(float3(
            baseData.WaterSkyConflict,
            baseData.WaterPixelConfidence,
            max(baseData.WaterSkySource, baseData.SafetySkyReject)) * boost);
    }

    if (mode == 8)
    {
        return saturate(float3(
            baseData.WaterPixelConfidence,
            max(baseData.MaterialCrystalAether, baseData.MaterialNeonGlass),
            max(baseData.MaterialMetalIndustrial, baseData.ReceiverStructure)) * boost);
    }

    Dalashade_MaterialResolve material = Dalashade_FrameData_ResolveCanonicalMaterial(source, uv, settings);
    Dalashade_WaterResolve water = Dalashade_FrameData_ResolveCanonicalWater(source, uv, material, settings);
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

    float materialDiff = abs(baseData.MaterialFoliage - material.Foliage)
        + abs(baseData.MaterialSandDust - material.SandDust)
        + abs(baseData.MaterialSnowIce - material.SnowIce)
        + abs(baseData.MaterialCrystalAether - material.CrystalAether)
        + abs(baseData.MaterialNeonGlass - material.NeonGlass);
    float waterDiff = abs(baseData.WaterReceiver - water.WaterReceiver)
        + abs(baseData.WaterSource - water.WaterSource)
        + abs(baseData.WaterShallow - water.ShallowWater)
        + abs(baseData.WaterHorizon - water.WaterHorizon)
        + abs(baseData.WaterHorizonOnly - water.HorizonOnlyConfidence)
        + abs(baseData.WaterSandReject - water.SandReject)
        + abs(baseData.WaterSkyConflict - water.WaterSkyConflict);
    float safetyDiff = abs(baseData.SafetySkyReject - safety.SkyReject)
        + abs(baseData.SafetySkinReject - safety.SkinReject)
        + abs(baseData.SafetyHighlightProtect - safety.HighlightProtect)
        + abs(baseData.SafetyWaterAOReject - safety.WaterAOReject);
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
