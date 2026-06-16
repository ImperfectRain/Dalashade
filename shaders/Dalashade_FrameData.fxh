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
    float FoamOrEdge;
    float WaterSurface;
    float ShallowWater;
    float WaterHorizon;
    float SpecularGlint;
    float HorizonOnly;
    float SandReject;
    float WaterSkyConflict;
    float Confidence;
};

struct Dalashade_FrameMaterial
{
    float Foliage;
    float WaterSpecular;
    float WaterPlane;
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
    float SafetySkyReject;
    float SafetySkinReject;
    float SafetyHighlightProtect;
    float SafetyBrightSandProtect;
    float SafetySnowProtect;
    float SafetyFoliageNoiseReject;
    float SafetyWaterAOReject;
    float SafetyUIDepthRisk;
    float SafetyDepthConfidence;

    float WaterPixelConfidence;
    float WaterReceiver;
    float WaterSource;
    float WaterSkySource;
    float WaterWetShoreline;
    float WaterFoamOrEdge;
    float WaterSurface;
    float WaterShallow;
    float WaterHorizon;
    float WaterSpecularGlint;
    float WaterHorizonOnly;
    float WaterSandReject;
    float WaterSkyConflict;
    float WaterConfidence;

    float MaterialFoliage;
    float MaterialWaterSpecular;
    float MaterialWaterPlane;
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
    float MaterialSurfaceSmoothness;
    float MaterialSurfaceHardness;

    float ReceiverBroad;
    float ReceiverReflection;
    float ReceiverAO;
    float ReceiverStructure;
    float SourceLightConfidence;
};

struct Dalashade_FrameSurfaceData
{
    float3 Normal;
    float NormalConfidence;
    float OrientationConfidence;
    float DepthConfidence;
    float EdgeDiscontinuity;
    float DetailStrength;
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

struct Dalashade_FrameSceneSettings
{
    float Readability;
    float Atmosphere;
    float HighlightProtection;
    float ShadowProtection;
    float Haze;
    float Wetness;
    float Cold;
    float Heat;
    float MagicGlow;
    float NeonGlow;
    float FoliageDensity;
    float IndustrialHardness;
    float CosmicMood;
    float CinematicPermission;
    float CombatPressure;

    float Night;
    float Moonlight;
    float ArtificialLight;
    float AmbientDarkness;
    float NightAtmosphere;

    float Daylight;
    float Sunlight;
    float OpenSkyLight;
    float SurfaceHeat;
    float DayAtmosphere;
    float DayReflection;
    float DayHighlightPressure;

    float StandaloneStrength;
};

struct Dalashade_FrameSceneData
{
    float Readability;
    float Atmosphere;
    float HighlightProtection;
    float ShadowProtection;
    float Haze;
    float Wetness;
    float Cold;
    float Heat;
    float MagicGlow;
    float NeonGlow;
    float FoliageDensity;
    float IndustrialHardness;
    float CosmicMood;
    float CinematicPermission;
    float CombatPressure;

    float Night;
    float Moonlight;
    float ArtificialLight;
    float AmbientDarkness;
    float NightAtmosphere;

    float Daylight;
    float Sunlight;
    float OpenSkyLight;
    float SurfaceHeat;
    float DayAtmosphere;
    float DayReflection;
    float DayHighlightPressure;

    float StandaloneStrength;
    float GameplayDampen;
    float ReadabilityDampen;
    float ReflectionDampen;
    float StandaloneSafe;

    float DayOpenAir;
    float NightLocalLight;
    float WetAir;
    float HeatAir;
    float ColdAir;
    float AetherTech;
    float ForestCanopy;
    float Industrial;
    float InteriorMood;
};

Dalashade_FrameSceneSettings Dalashade_FrameScene_DefaultSettings()
{
    Dalashade_FrameSceneSettings settings;

    settings.Readability = 0.0;
    settings.Atmosphere = 0.0;
    settings.HighlightProtection = 0.0;
    settings.ShadowProtection = 0.0;
    settings.Haze = 0.0;
    settings.Wetness = 0.0;
    settings.Cold = 0.0;
    settings.Heat = 0.0;
    settings.MagicGlow = 0.0;
    settings.NeonGlow = 0.0;
    settings.FoliageDensity = 0.0;
    settings.IndustrialHardness = 0.0;
    settings.CosmicMood = 0.0;
    settings.CinematicPermission = 0.0;
    settings.CombatPressure = 0.0;

    settings.Night = 0.0;
    settings.Moonlight = 0.0;
    settings.ArtificialLight = 0.0;
    settings.AmbientDarkness = 0.0;
    settings.NightAtmosphere = 0.0;

    settings.Daylight = 0.0;
    settings.Sunlight = 0.0;
    settings.OpenSkyLight = 0.0;
    settings.SurfaceHeat = 0.0;
    settings.DayAtmosphere = 0.0;
    settings.DayReflection = 0.0;
    settings.DayHighlightPressure = 0.0;

    settings.StandaloneStrength = 0.0;

    return settings;
}

Dalashade_FrameSceneData Dalashade_ResolveFrameSceneData(Dalashade_FrameSceneSettings settings)
{
    Dalashade_FrameSceneData scene;

    scene.Readability = saturate(settings.Readability);
    scene.Atmosphere = saturate(settings.Atmosphere);
    scene.HighlightProtection = saturate(settings.HighlightProtection);
    scene.ShadowProtection = saturate(settings.ShadowProtection);
    scene.Haze = saturate(settings.Haze);
    scene.Wetness = saturate(settings.Wetness);
    scene.Cold = saturate(settings.Cold);
    scene.Heat = saturate(settings.Heat);
    scene.MagicGlow = saturate(settings.MagicGlow);
    scene.NeonGlow = saturate(settings.NeonGlow);
    scene.FoliageDensity = saturate(settings.FoliageDensity);
    scene.IndustrialHardness = saturate(settings.IndustrialHardness);
    scene.CosmicMood = saturate(settings.CosmicMood);
    scene.CinematicPermission = saturate(settings.CinematicPermission);
    scene.CombatPressure = saturate(settings.CombatPressure);

    scene.Night = saturate(settings.Night);
    scene.Moonlight = saturate(settings.Moonlight);
    scene.ArtificialLight = saturate(settings.ArtificialLight);
    scene.AmbientDarkness = saturate(settings.AmbientDarkness);
    scene.NightAtmosphere = saturate(settings.NightAtmosphere);

    scene.Daylight = saturate(settings.Daylight);
    scene.Sunlight = saturate(settings.Sunlight);
    scene.OpenSkyLight = saturate(settings.OpenSkyLight);
    scene.SurfaceHeat = saturate(settings.SurfaceHeat);
    scene.DayAtmosphere = saturate(settings.DayAtmosphere);
    scene.DayReflection = saturate(settings.DayReflection);
    scene.DayHighlightPressure = saturate(settings.DayHighlightPressure);

    scene.StandaloneStrength = saturate(settings.StandaloneStrength);
    scene.GameplayDampen = 1.0 - saturate(scene.CombatPressure * 0.58 + scene.Readability * 0.16);
    scene.ReadabilityDampen = 1.0 - saturate(scene.Readability * 0.24 + scene.CombatPressure * 0.38);
    scene.ReflectionDampen = 1.0 - saturate(scene.CombatPressure * 0.42 + scene.Readability * 0.18);
    scene.StandaloneSafe = saturate(scene.StandaloneStrength * scene.GameplayDampen * scene.ReadabilityDampen);

    scene.DayOpenAir = saturate(scene.Daylight * 0.28 + scene.Sunlight * 0.32 + scene.OpenSkyLight * 0.26 + scene.DayAtmosphere * 0.22);
    scene.NightLocalLight = saturate(scene.Night * 0.28 + scene.Moonlight * 0.22 + scene.ArtificialLight * 0.34 + scene.AmbientDarkness * 0.18 + scene.NightAtmosphere * 0.22);
    scene.WetAir = saturate(scene.Wetness * 0.72 + scene.Haze * 0.18 + scene.Atmosphere * 0.12);
    scene.HeatAir = saturate(scene.Heat * 0.62 + scene.SurfaceHeat * 0.46 + scene.DayHighlightPressure * 0.16);
    scene.ColdAir = saturate(scene.Cold * 0.66 + scene.Moonlight * 0.16 + scene.OpenSkyLight * 0.12);
    scene.AetherTech = saturate(scene.MagicGlow * 0.58 + scene.NeonGlow * 0.58 + scene.CosmicMood * 0.22);
    scene.ForestCanopy = saturate(scene.FoliageDensity * 0.74 + scene.Atmosphere * 0.12 + scene.ShadowProtection * 0.12);
    scene.Industrial = saturate(scene.IndustrialHardness * 0.76 + scene.NeonGlow * 0.12);
    scene.InteriorMood = saturate(scene.AmbientDarkness * 0.34 + scene.ArtificialLight * 0.28 + scene.ShadowProtection * 0.22 + scene.Night * 0.10);

    return scene;
}

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
    Dalashade_MaterialResolve material,
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
        material.WaterPlane,
        material.SpecularGlint,
        material.SandDust,
        material.SkyCloudFog,
        material.SkinProtection,
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
    Dalashade_WaterResolve water = Dalashade_FrameData_ResolveCanonicalWater(color, uv, material, settings);
    Dalashade_SafetyResolve safety = Dalashade_FrameData_ResolveCanonicalSafety(color, uv, material, water, settings);

    Dalashade_FrameBaseData data;

    data.SafetySkyReject = safety.SkyReject;
    data.SafetySkinReject = safety.SkinReject;
    data.SafetyHighlightProtect = safety.HighlightProtect;
    data.SafetyBrightSandProtect = safety.BrightSandProtect;
    data.SafetySnowProtect = safety.SnowProtect;
    data.SafetyFoliageNoiseReject = safety.FoliageNoiseReject;
    data.SafetyWaterAOReject = safety.WaterAOReject;
    data.SafetyUIDepthRisk = safety.UIDepthRisk;
    data.SafetyDepthConfidence = safety.DepthConfidence;

    data.WaterPixelConfidence = water.WaterPixelConfidence;
    data.WaterReceiver = water.WaterReceiver;
    data.WaterSource = water.WaterSource;
    data.WaterSkySource = water.SkySource;
    data.WaterWetShoreline = water.WetShoreline;
    data.WaterFoamOrEdge = water.FoamOrEdge;
    data.WaterSurface = water.WaterSurface;
    data.WaterShallow = water.ShallowWater;
    data.WaterHorizon = water.WaterHorizon;
    data.WaterSpecularGlint = material.SpecularGlint;
    data.WaterHorizonOnly = water.HorizonOnlyConfidence;
    data.WaterSandReject = water.SandReject;
    data.WaterSkyConflict = water.WaterSkyConflict;
    data.WaterConfidence = water.Confidence;

    data.MaterialFoliage = material.Foliage;
    data.MaterialWaterSpecular = material.WaterSpecular;
    data.MaterialWaterPlane = material.WaterPlane;
    data.MaterialSandDust = material.SandDust;
    data.MaterialSnowIce = material.SnowIce;
    data.MaterialStoneRuins = material.StoneRuins;
    data.MaterialMetalIndustrial = material.MetalIndustrial;
    data.MaterialCrystalAether = material.CrystalAether;
    data.MaterialNeonGlass = material.NeonGlass;
    data.MaterialFireLavaHeat = material.FireLavaHeat;
    data.MaterialSkyCloudFog = material.SkyCloudFog;
    data.MaterialSkinProtection = material.SkinProtection;
    data.MaterialVoidDarkness = material.VoidDarkness;
    data.MaterialSurfaceSmoothness = material.SurfaceSmoothness;
    data.MaterialSurfaceHardness = material.SurfaceHardness;

    data.ReceiverBroad = material.ReceiverConfidence;
    data.ReceiverReflection = material.ReflectionReceiverConfidence;
    data.ReceiverAO = material.AOReceiverConfidence;
    data.ReceiverStructure = material.StructureReceiverConfidence;
    data.SourceLightConfidence = material.LightSourceConfidence;

    return data;
}

Dalashade_FrameSurfaceData Dalashade_ResolveFrameSurfaceData(
    float3 color,
    float2 uv,
    Dalashade_FrameBaseData baseData,
    Dalashade_FrameDataSettings settings)
{
    // Keep baseData in the signature for future API symmetry. This first pass
    // intentionally recomputes canonical resolves so surface parity can catch
    // wrapper assignment mistakes without depending on cached aggregate fields.
    Dalashade_MaterialResolve material = Dalashade_FrameData_ResolveCanonicalMaterial(color, uv, settings);
    Dalashade_WaterResolve water = Dalashade_FrameData_ResolveCanonicalWater(color, uv, material, settings);
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
    surface.OrientationConfidence = field.OrientationConfidence;
    surface.DepthConfidence = field.DepthConfidence;
    surface.EdgeDiscontinuity = field.EdgeDiscontinuity;
    surface.DetailStrength = field.DetailStrength;
    surface.GroundCandidate = field.GroundPlaneCandidate;
    surface.StructureCandidate = field.StructureCandidate;
    surface.WallCandidate = field.WallPlaneCandidate;
    surface.ReflectionReceiverSupport = field.ReflectionReceiver;
    surface.AOReceiverSupport = field.AOReceiver;

    return surface;
}

#endif
