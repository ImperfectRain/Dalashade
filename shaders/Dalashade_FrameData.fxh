#ifndef DALASHADE_FRAME_DATA_FXH
#define DALASHADE_FRAME_DATA_FXH

#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"
#include "Dalashade_NormalField.fxh"

// Dalashade FrameData is an internal contract wrapper over the existing
// inline resolvers. It does not own material, water, safety, receiver, or
// normal formulas.

struct Dalashade_FrameSafety
{
    float SkyReject;
    float SkinReject;
    float HighlightProtect;
    float BrightSandProtect;
    float SnowProtect;
    float FoliageNoiseReject;
    float UIDepthRisk;
    float DepthConfidence;
};

struct Dalashade_FrameWater
{
    float WaterPixelConfidence;
    float WaterReceiver;
    float WaterSource;
    float SkySource;
    float WetShoreline;
    float SpecularGlint;
    float HorizonOnly;
    float WaterSkyConflict;
    float Confidence;
};

struct Dalashade_FrameMaterial
{
    float Foliage;
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
    float SurfaceSmoothness;
    float SurfaceHardness;
};

struct Dalashade_FrameReceivers
{
    float BroadReceiver;
    float ReflectionReceiver;
    float AOReceiver;
    float StructureReceiver;
    float LightSourceConfidence;
};

struct Dalashade_FrameBaseData
{
    Dalashade_FrameSafety Safety;
    Dalashade_FrameWater Water;
    Dalashade_FrameMaterial Material;
    Dalashade_FrameReceivers Receivers;
};

struct Dalashade_FrameSurfaceData
{
    float3 Normal;
    float NormalConfidence;
    float DepthConfidence;
    float EdgeDiscontinuity;
    float GroundCandidate;
    float StructureCandidate;
    float WallCandidate;
    float ReflectionReceiverSupport;
    float AOReceiverSupport;
};

struct Dalashade_FrameDataSettings
{
    float MaterialFoliage;
    float MaterialWaterSpecular;
    float MaterialWaterPlane;
    float MaterialSpecularGlint;
    float MaterialSandDust;
    float MaterialSnowIce;
    float MaterialStoneRuins;
    float MaterialMetalIndustrial;
    float MaterialCrystalAether;
    float MaterialNeonGlass;
    float MaterialFireLavaHeat;
    float MaterialSkyCloudFog;
    float MaterialSkinProtection;
    float MaterialVoidDarkness;

    float WaterContext;
    float CoastalContext;
    float OpenOceanContext;
    float ShallowWaterContext;
    float WetSurfaceContext;

    float HighlightProtection;
    float DepthAssistEnabled;
    float DepthAssistStrength;
    float DepthAssistConfidenceFloor;

    float NormalFieldEnabled;
    float NormalFieldStrength;
    float NormalDepthStrength;
    float NormalDetailStrength;
    float NormalMaterialInfluence;
    float NormalWaterSuppression;
    float NormalSkinSuppression;
    float NormalSkySuppression;
};

Dalashade_FrameDataSettings Dalashade_FrameData_DefaultSettings()
{
    Dalashade_FrameDataSettings settings;

    settings.MaterialFoliage = 0.0;
    settings.MaterialWaterSpecular = 0.0;
    settings.MaterialWaterPlane = 0.0;
    settings.MaterialSpecularGlint = 0.0;
    settings.MaterialSandDust = 0.0;
    settings.MaterialSnowIce = 0.0;
    settings.MaterialStoneRuins = 0.0;
    settings.MaterialMetalIndustrial = 0.0;
    settings.MaterialCrystalAether = 0.0;
    settings.MaterialNeonGlass = 0.0;
    settings.MaterialFireLavaHeat = 0.0;
    settings.MaterialSkyCloudFog = 0.0;
    settings.MaterialSkinProtection = 0.0;
    settings.MaterialVoidDarkness = 0.0;

    settings.WaterContext = 0.0;
    settings.CoastalContext = 0.0;
    settings.OpenOceanContext = 0.0;
    settings.ShallowWaterContext = 0.0;
    settings.WetSurfaceContext = 0.0;

    settings.HighlightProtection = 0.0;
    settings.DepthAssistEnabled = 0.0;
    settings.DepthAssistStrength = 0.0;
    settings.DepthAssistConfidenceFloor = 0.0;

    settings.NormalFieldEnabled = 0.0;
    settings.NormalFieldStrength = 0.0;
    settings.NormalDepthStrength = 0.50;
    settings.NormalDetailStrength = 0.25;
    settings.NormalMaterialInfluence = 0.50;
    settings.NormalWaterSuppression = 0.80;
    settings.NormalSkinSuppression = 0.90;
    settings.NormalSkySuppression = 0.95;

    return settings;
}

Dalashade_MaterialResolve Dalashade_FrameData_ResolveCanonicalMaterial(
    float3 color,
    float2 uv,
    Dalashade_FrameDataSettings settings)
{
    return Dalashade_ResolveMaterials(
        color,
        uv,
        settings.MaterialFoliage,
        settings.MaterialWaterSpecular,
        settings.MaterialWaterPlane,
        settings.MaterialSpecularGlint,
        settings.MaterialSandDust,
        settings.MaterialSnowIce,
        settings.MaterialStoneRuins,
        settings.MaterialMetalIndustrial,
        settings.MaterialCrystalAether,
        settings.MaterialNeonGlass,
        settings.MaterialFireLavaHeat,
        settings.MaterialSkyCloudFog,
        settings.MaterialSkinProtection,
        settings.MaterialVoidDarkness,
        settings.DepthAssistEnabled,
        settings.DepthAssistStrength,
        settings.DepthAssistConfidenceFloor);
}

Dalashade_WaterResolve Dalashade_FrameData_ResolveCanonicalWater(
    float3 color,
    float2 uv,
    Dalashade_FrameDataSettings settings)
{
    return Dalashade_ResolveWater(
        color,
        uv,
        settings.WaterContext,
        settings.CoastalContext,
        settings.OpenOceanContext,
        settings.ShallowWaterContext,
        settings.WetSurfaceContext,
        settings.MaterialWaterPlane,
        settings.MaterialSpecularGlint,
        settings.MaterialSandDust,
        settings.MaterialSkyCloudFog,
        settings.MaterialSkinProtection,
        settings.DepthAssistEnabled,
        settings.DepthAssistStrength,
        settings.DepthAssistConfidenceFloor);
}

Dalashade_SafetyResolve Dalashade_FrameData_ResolveCanonicalSafety(
    float3 color,
    float2 uv,
    Dalashade_MaterialResolve material,
    Dalashade_WaterResolve water,
    Dalashade_FrameDataSettings settings)
{
    return Dalashade_ResolveSafety(
        color,
        uv,
        material,
        water,
        settings.HighlightProtection,
        settings.DepthAssistEnabled,
        settings.DepthAssistStrength,
        settings.DepthAssistConfidenceFloor);
}

Dalashade_FrameBaseData Dalashade_ResolveFrameBaseData(
    float3 color,
    float2 uv,
    Dalashade_FrameDataSettings settings)
{
    Dalashade_MaterialResolve material = Dalashade_FrameData_ResolveCanonicalMaterial(color, uv, settings);
    Dalashade_WaterResolve water = Dalashade_FrameData_ResolveCanonicalWater(color, uv, settings);
    Dalashade_SafetyResolve safety = Dalashade_FrameData_ResolveCanonicalSafety(color, uv, material, water, settings);

    Dalashade_FrameBaseData data;

    data.Safety.SkyReject = safety.SkyReject;
    data.Safety.SkinReject = safety.SkinReject;
    data.Safety.HighlightProtect = safety.HighlightProtect;
    data.Safety.BrightSandProtect = safety.BrightSandProtect;
    data.Safety.SnowProtect = safety.SnowProtect;
    data.Safety.FoliageNoiseReject = safety.FoliageNoiseReject;
    data.Safety.UIDepthRisk = safety.UIDepthRisk;
    data.Safety.DepthConfidence = safety.DepthConfidence;

    data.Water.WaterPixelConfidence = water.WaterPixelConfidence;
    data.Water.WaterReceiver = water.WaterReceiver;
    data.Water.WaterSource = water.WaterSource;
    data.Water.SkySource = water.SkySource;
    data.Water.WetShoreline = water.WetShoreline;
    data.Water.SpecularGlint = material.SpecularGlint;
    data.Water.HorizonOnly = water.HorizonOnlyConfidence;
    data.Water.WaterSkyConflict = water.WaterSkyConflict;
    data.Water.Confidence = water.Confidence;

    data.Material.Foliage = material.Foliage;
    data.Material.SandDust = material.SandDust;
    data.Material.SnowIce = material.SnowIce;
    data.Material.StoneRuins = material.StoneRuins;
    data.Material.MetalIndustrial = material.MetalIndustrial;
    data.Material.CrystalAether = material.CrystalAether;
    data.Material.NeonGlass = material.NeonGlass;
    data.Material.FireLavaHeat = material.FireLavaHeat;
    data.Material.SkyCloudFog = material.SkyCloudFog;
    data.Material.SkinProtection = material.SkinProtection;
    data.Material.VoidDarkness = material.VoidDarkness;
    data.Material.SurfaceSmoothness = material.SurfaceSmoothness;
    data.Material.SurfaceHardness = material.SurfaceHardness;

    data.Receivers.BroadReceiver = material.ReceiverConfidence;
    data.Receivers.ReflectionReceiver = material.ReflectionReceiverConfidence;
    data.Receivers.AOReceiver = material.AOReceiverConfidence;
    data.Receivers.StructureReceiver = material.StructureReceiverConfidence;
    data.Receivers.LightSourceConfidence = material.LightSourceConfidence;

    return data;
}

Dalashade_FrameSurfaceData Dalashade_ResolveFrameSurfaceData(
    float3 color,
    float2 uv,
    Dalashade_FrameBaseData baseData,
    Dalashade_FrameDataSettings settings)
{
    Dalashade_MaterialResolve material = Dalashade_FrameData_ResolveCanonicalMaterial(color, uv, settings);
    Dalashade_WaterResolve water = Dalashade_FrameData_ResolveCanonicalWater(color, uv, settings);
    Dalashade_SafetyResolve safety = Dalashade_FrameData_ResolveCanonicalSafety(color, uv, material, water, settings);
    Dalashade_NormalField field = Dalashade_ResolveNormalField(
        color,
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

    Dalashade_FrameSurfaceData surface;
    surface.Normal = field.CombinedNormal;
    surface.NormalConfidence = field.NormalConfidence;
    surface.DepthConfidence = field.DepthConfidence;
    surface.EdgeDiscontinuity = field.EdgeDiscontinuity;
    surface.GroundCandidate = field.GroundPlaneCandidate;
    surface.StructureCandidate = field.StructureCandidate;
    surface.WallCandidate = field.WallPlaneCandidate;
    surface.ReflectionReceiverSupport = field.ReflectionReceiver;
    surface.AOReceiverSupport = field.AOReceiver;

    return surface;
}

#endif
