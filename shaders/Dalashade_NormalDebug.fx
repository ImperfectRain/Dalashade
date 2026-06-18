#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"
#include "Dalashade_NormalField.fxh"

// Optional NormalField visualizer. This shader is diagnostic only and should
// be enabled manually near the end of the ReShade stack when tuning.

uniform float Dalashade_NormalDebugEnabled <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Enable Normal Debug";
    ui_tooltip = "Shows inferred screen-space NormalField diagnostics. This is not true game material normals or FFXIV G-buffer access.";
> = 0.0;

uniform int Dalashade_NormalDebugMode <
    ui_type = "combo";
    ui_items = "Normal image\0Depth normal RGB\0Detail normal RGB\0Combined normal RGB\0Ground/plane candidate\0Wall-plane candidate\0Structure candidate\0Detail eligibility\0Normal confidence\0Shading receiver\0Reflection receiver\0AO receiver\0Safety/relief suppression\0Legacy relief normal RGB\0Texture ridge/groove/relief\0Texture groove line only\0Curvature ridge\0Curvature valley/groove\0Structure coherence\0Composite relief height\0Composite relief normal RGB\0";
    ui_label = "Normal Debug Mode";
> = 0;

uniform float Dalashade_NormalDebugBoost <
    ui_type = "slider";
    ui_min = 0.25; ui_max = 8.0;
    ui_label = "Normal Debug Boost";
> = 2.0;

uniform float Dalashade_NormalFieldStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Normal Field Strength";
> = 0.25;

uniform float Dalashade_NormalDepthStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Depth Normal Strength";
> = 0.50;

uniform float Dalashade_NormalDetailStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Detail Normal Strength";
> = 0.25;

uniform float Dalashade_NormalMaterialInfluence <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Normal Material Influence";
> = 0.50;

uniform float Dalashade_NormalWaterSuppression <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Water Suppression";
> = 0.80;

uniform float Dalashade_NormalSkinSuppression <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Skin Suppression";
> = 0.90;

uniform float Dalashade_NormalSkySuppression <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Sky/Fog Suppression";
> = 0.95;

uniform float Dalashade_MaterialFoliage < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Foliage"; > = 0.0;
uniform float Dalashade_MaterialWaterSpecular < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Water/Specular"; > = 0.0;
uniform float Dalashade_MaterialWaterPlane < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Water Plane"; > = 0.0;
uniform float Dalashade_MaterialSpecularGlint < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Specular Glint"; > = 0.0;
uniform float Dalashade_MaterialSandDust < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sand/Dust"; > = 0.0;
uniform float Dalashade_MaterialSnowIce < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Snow/Ice"; > = 0.0;
uniform float Dalashade_MaterialStoneRuins < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Stone/Ruins"; > = 0.0;
uniform float Dalashade_MaterialMetalIndustrial < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Metal/Industrial"; > = 0.0;
uniform float Dalashade_MaterialCrystalAether < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Crystal/Aether"; > = 0.0;
uniform float Dalashade_MaterialNeonGlass < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Neon/Glass"; > = 0.0;
uniform float Dalashade_MaterialFireLavaHeat < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Fire/Lava/Heat"; > = 0.0;
uniform float Dalashade_MaterialSkyCloudFog < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sky/Cloud/Fog"; > = 0.0;
uniform float Dalashade_MaterialSkinProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Skin Protection"; > = 0.0;
uniform float Dalashade_MaterialVoidDarkness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Void/Darkness"; > = 0.0;

uniform float Dalashade_WaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Water Context"; > = 0.0;
uniform float Dalashade_CoastalContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Coastal Context"; > = 0.0;
uniform float Dalashade_OpenOceanContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Open Ocean Context"; > = 0.0;
uniform float Dalashade_ShallowWaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Shallow Water Context"; > = 0.0;
uniform float Dalashade_WetSurfaceContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Wet Surface Context"; > = 0.0;

uniform bool Dalashade_EnableDepthAssist <
    ui_label = "Enable Depth Assist";
> = false;

uniform float Dalashade_DepthAssistStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Strength"; > = 0.0;
uniform float Dalashade_DepthAssistConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Confidence Floor"; > = 0.0;
uniform float Dalashade_DepthConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Confidence Floor"; > = 0.0;

float4 Dalashade_NormalDebugPS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    float3 source = tex2D(ReShade::BackBuffer, texcoord).rgb;
    int mode = Dalashade_NormalDebugMode;

    if (Dalashade_NormalDebugEnabled <= 0.0 || mode <= 0)
    {
        return float4(source, 1.0);
    }

    float depthEnabled = Dalashade_EnableDepthAssist ? 1.0 : 0.0;
    float depthFloor = max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor);
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
        depthEnabled,
        Dalashade_DepthAssistStrength,
        depthFloor);
    Dalashade_WaterResolve water = Dalashade_ResolveWater(
        source,
        texcoord,
        Dalashade_WaterContext,
        Dalashade_CoastalContext,
        Dalashade_OpenOceanContext,
        Dalashade_ShallowWaterContext,
        Dalashade_WetSurfaceContext,
        material.WaterPlane,
        material.SpecularGlint,
        material.SandDust,
        material.SkyCloudFog,
        material.SkinProtection,
        depthEnabled,
        Dalashade_DepthAssistStrength,
        depthFloor);
    Dalashade_SafetyResolve safety = Dalashade_ResolveSafety(
        source,
        texcoord,
        material,
        water,
        0.0,
        depthEnabled,
        Dalashade_DepthAssistStrength,
        depthFloor);
    Dalashade_NormalField field = Dalashade_ResolveNormalField(
        source,
        texcoord,
        material,
        water,
        safety,
        Dalashade_NormalDebugEnabled,
        Dalashade_NormalFieldStrength,
        Dalashade_NormalDepthStrength,
        Dalashade_NormalDetailStrength,
        Dalashade_NormalMaterialInfluence,
        Dalashade_NormalWaterSuppression,
        Dalashade_NormalSkinSuppression,
        Dalashade_NormalSkySuppression);

    return float4(Dalashade_GetNormalDebugColor(field, mode, Dalashade_NormalDebugBoost), 1.0);
}

technique Dalashade_NormalDebug
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_NormalDebugPS;
    }
}
