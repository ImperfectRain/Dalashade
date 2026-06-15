#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"
#include "Dalashade_NormalField.fxh"

uniform float Dalashade_Readability <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Readability";
    ui_tooltip = "Scene-driven gameplay readability pressure. Higher values damp cinematic grading.";
> = 0.0;

uniform float Dalashade_Atmosphere <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Atmosphere";
> = 0.0;

uniform float Dalashade_HighlightProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Highlight Protection";
> = 0.0;

uniform float Dalashade_ShadowProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Shadow Protection";
> = 0.0;

uniform float Dalashade_Cold <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cold";
> = 0.0;

uniform float Dalashade_Heat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Heat";
> = 0.0;

uniform float Dalashade_MagicGlow <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Magic Glow";
> = 0.0;

uniform float Dalashade_NeonGlow <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Neon Glow";
> = 0.0;

uniform float Dalashade_FoliageDensity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Foliage Density";
    ui_tooltip = "Scene-driven foliage density. Higher values preserve richer greens and restrain gray shadow lift.";
> = 0.0;

uniform float Dalashade_IndustrialHardness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Industrial Hardness";
    ui_tooltip = "Scene-driven industrial/imperial pressure. Higher values favor harder contrast and lower color softness.";
> = 0.0;

uniform float Dalashade_CosmicMood <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cosmic Mood";
    ui_tooltip = "Scene-driven cosmic/lunar pressure. Higher values bias the grade cooler and more otherworldly.";
> = 0.0;

uniform float Dalashade_CinematicPermission <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cinematic Permission";
> = 0.0;

uniform float Dalashade_CombatPressure <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Combat Pressure";
    ui_tooltip = "Scene-driven gameplay pressure. Higher values damp heavy grading.";
> = 0.0;

uniform float Dalashade_Night <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night";
    ui_tooltip = "Scene-driven nighttime context. Higher values favor deeper ambient darkness and stronger light hierarchy.";
> = 0.0;

uniform float Dalashade_Moonlight <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Moonlight";
    ui_tooltip = "Scene-driven open-sky/cold/cosmic moonlight influence.";
> = 0.0;

uniform float Dalashade_ArtificialLight <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Artificial Light";
    ui_tooltip = "Scene-driven lamp, window, neon, fire, or crystal light-pool influence.";
> = 0.0;

uniform float Dalashade_AmbientDarkness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Ambient Darkness";
    ui_tooltip = "Scene-driven unlit baseline darkness. Higher values preserve deeper shadows.";
> = 0.0;

uniform float Dalashade_NightAtmosphere <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night Atmosphere";
    ui_tooltip = "Scene-driven nighttime air/mist/storm atmosphere without generic gray wash.";
> = 0.0;

uniform float Dalashade_Daylight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Daylight"; ui_tooltip = "Scene-driven daytime context. Does not directly lift exposure."; > = 0.0;
uniform float Dalashade_Sunlight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Sunlight"; ui_tooltip = "Scene-driven direct sunlight pressure for tone and highlights."; > = 0.0;
uniform float Dalashade_OpenSkyLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Open Sky Light"; ui_tooltip = "Scene-driven open-sky daylight for broad material protection."; > = 0.0;
uniform float Dalashade_SurfaceHeat < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Surface Heat"; ui_tooltip = "Scene-driven sunlit surface heat for desert/coastal/volcanic identity."; > = 0.0;
uniform float Dalashade_DayAtmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Atmosphere"; ui_tooltip = "Scene-driven daytime air, mist, storm, or coastal diffusion."; > = 0.0;
uniform float Dalashade_DayReflection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Reflection"; ui_tooltip = "Scene-driven daytime reflection permission for valid material receivers."; > = 0.0;
uniform float Dalashade_DayHighlightPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Highlight Pressure"; ui_tooltip = "Scene-driven daytime bright-surface protection."; > = 0.0;

uniform float Dalashade_MaterialFoliage <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Foliage";
    ui_tooltip = "Inferred foliage likelihood. Supports richer greens while preserving dark trunks.";
> = 0.0;

uniform float Dalashade_MaterialWaterSpecular <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water/Specular";
    ui_tooltip = "Legacy inferred water/specular likelihood. Used as compatibility fallback for day highlight protection.";
> = 0.0;

uniform float Dalashade_MaterialWaterPlane <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water Plane";
    ui_tooltip = "Inferred broad water surface likelihood. Used for tonal protection, not reflection.";
> = 0.0;

uniform float Dalashade_MaterialSpecularGlint <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Specular Glint";
    ui_tooltip = "Inferred thin glint likelihood. Used for highlight and chroma restraint.";
> = 0.0;

uniform float Dalashade_MaterialSandDust <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sand/Dust";
    ui_tooltip = "Inferred sand or dust likelihood. Supports warm midtones and highlight rolloff without orange mud.";
> = 0.0;

uniform float Dalashade_MaterialSnowIce <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Snow/Ice";
    ui_tooltip = "Inferred snow or ice likelihood. Supports cool clarity and white rolloff without gray snow.";
> = 0.0;

uniform float Dalashade_MaterialMetalIndustrial <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Metal/Industrial";
    ui_tooltip = "Inferred metal or industrial likelihood. Supports cooler harder contrast without brittle highlights.";
> = 0.0;

uniform float Dalashade_MaterialCrystalAether <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Crystal/Aether";
    ui_tooltip = "Inferred crystal or aether likelihood. Protects saturated glow colors and supports subtle tint.";
> = 0.0;

uniform float Dalashade_MaterialNeonGlass <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Neon/Glass";
    ui_tooltip = "Inferred neon or glass likelihood. Protects cyan/blue/violet identity without creating bloom.";
> = 0.0;

uniform float Dalashade_MaterialFireLavaHeat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Fire/Lava/Heat";
    ui_tooltip = "Inferred fire, lava, or heat likelihood. Preserves warm glow color without creating light.";
> = 0.0;

uniform float Dalashade_MaterialSkyCloudFog <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sky/Cloud/Fog";
    ui_tooltip = "Inferred sky, cloud, fog, or atmosphere likelihood. Used for daytime highlight protection.";
> = 0.0;

uniform float Dalashade_MaterialSkinProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Skin Protection";
    ui_tooltip = "Inferred character/skin protection likelihood. Reduces extreme tint and saturation shifts on smooth foreground midtones.";
> = 0.0;

uniform float Dalashade_MaterialVoidDarkness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Void/Darkness";
    ui_tooltip = "Inferred void or darkness likelihood. Preserves black depth and avoids gray wash.";
> = 0.0;

uniform float Dalashade_WaterContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Water Context";
    ui_tooltip = "Scene-level water prior for shared water resolver tonal protection.";
> = 0.0;

uniform float Dalashade_CoastalContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Coastal Context";
    ui_tooltip = "Scene-level coastal prior for shared water resolver tonal protection.";
> = 0.0;

uniform float Dalashade_OpenOceanContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Open Ocean Context";
    ui_tooltip = "Scene-level open ocean prior for water/sky highlight handling.";
> = 0.0;

uniform float Dalashade_ShallowWaterContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Shallow Water Context";
    ui_tooltip = "Scene-level shallow water prior for turquoise-water tonal protection.";
> = 0.0;

uniform float Dalashade_WetSurfaceContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Wet Surface Context";
    ui_tooltip = "Scene-level wet surface prior. Used only for restraint/protection in AdaptiveGrade.";
> = 0.0;

uniform bool Dalashade_EnableDepthAssist <
    ui_label = "Enable Depth Assist";
    ui_tooltip = "Optional material-mask helper. Disabled by default; grade masks still work without depth.";
> = false;

uniform float Dalashade_DepthAssistStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Depth Assist Strength";
> = 0.0;

uniform float Dalashade_DepthAssistConfidenceFloor <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Depth Assist Confidence Floor";
> = 0.0;

uniform float Dalashade_DepthConfidenceFloor <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Depth Confidence Floor";
    ui_tooltip = "Alias for generated presets that use the shorter depth-confidence name.";
> = 0.0;

uniform float Dalashade_NormalFieldEnabled <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Enabled";
    ui_tooltip = "Optional inferred normal/surface field gate. AdaptiveGrade uses this only for mild tonal-detail protection.";
> = 0.0;

uniform float Dalashade_NormalFieldStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Strength";
    ui_tooltip = "Global scale for optional NormalField influence.";
> = 0.0;

uniform float Dalashade_NormalDepthStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Depth Strength";
    ui_tooltip = "Depth-normal contribution for optional structure/detail protection.";
> = 0.0;

uniform float Dalashade_NormalDetailStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Detail Strength";
    ui_tooltip = "Detail-normal contribution for optional structure/detail protection.";
> = 0.0;

uniform float Dalashade_NormalMaterialInfluence <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Material Influence";
    ui_tooltip = "Material-aware scaling for optional NormalField protection.";
> = 0.0;

uniform float Dalashade_NormalWaterSuppression <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Water Suppression";
    ui_tooltip = "Suppresses fake detail normals on water-like areas.";
> = 0.80;

uniform float Dalashade_NormalSkinSuppression <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Skin Suppression";
    ui_tooltip = "Suppresses fake detail normals on skin-like areas.";
> = 0.90;

uniform float Dalashade_NormalSkySuppression <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Sky/Fog Suppression";
    ui_tooltip = "Suppresses fake detail normals on sky, fog, and atmosphere.";
> = 0.95;

uniform float Dalashade_ManualStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Manual Overall Strength";
    ui_tooltip = "Manual fallback strength for testing without Dalashade. Defaults are intentionally subtle.";
> = 0.35;

uniform float Dalashade_StandaloneStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Standalone Stack Strength";
    ui_tooltip = "0 keeps AdaptiveGrade supportive for an existing preset. 1 lets it carry more tone/color responsibility while preserving material safety.";
> = 0.0;

uniform float Dalashade_ManualExposure <
    ui_type = "slider";
    ui_min = -0.20; ui_max = 0.20;
    ui_label = "Manual Exposure Trim";
> = 0.0;

uniform float Dalashade_ManualContrast <
    ui_type = "slider";
    ui_min = -0.30; ui_max = 0.30;
    ui_label = "Manual Contrast";
> = 0.0;

uniform float Dalashade_ManualSaturation <
    ui_type = "slider";
    ui_min = -0.30; ui_max = 0.30;
    ui_label = "Manual Saturation";
> = 0.0;

uniform float Dalashade_ManualTemperature <
    ui_type = "slider";
    ui_min = -0.25; ui_max = 0.25;
    ui_label = "Manual Temperature";
> = 0.0;

uniform float Dalashade_ManualTint <
    ui_type = "slider";
    ui_min = -0.20; ui_max = 0.20;
    ui_label = "Manual Tint";
> = 0.0;

uniform bool Dalashade_ShowDebugMask <
    ui_label = "Show Debug Mask";
    ui_tooltip = "Red shows highlight rolloff, green shows shadow lift, blue shows cinematic grade pressure.";
> = false;

uniform int Dalashade_AdaptiveGradeDebugMode <
    ui_type = "combo";
    ui_items = "Normal\0Night masks\0Day masks\0Material protection masks\0Highlight rolloff\0Chroma restraint\0Skin protection\0Final grade delta\0";
    ui_label = "AdaptiveGrade Debug Mode";
    ui_tooltip = "Diagnostic replacement masks for AdaptiveGrade. These show heuristic influence, not engine material IDs.";
> = 0;

float Dalashade_Luma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 Dalashade_SafeContrast(float3 color, float amount)
{
    return (color - 0.5) * (1.0 + amount) + 0.5;
}

float3 Dalashade_SafeSaturation(float3 color, float amount)
{
    float luma = Dalashade_Luma(color);
    return lerp(float3(luma, luma, luma), color, 1.0 + amount);
}

float3 Dalashade_TemperatureTint(float3 color, float temperature, float tint)
{
    float3 adjusted = color;
    adjusted.r += temperature * 0.060;
    adjusted.b -= temperature * 0.055;
    adjusted.g += tint * 0.045;
    adjusted.r -= tint * 0.018;
    adjusted.b -= tint * 0.018;
    return adjusted;
}

float4 Dalashade_AdaptiveGradePS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    // Read normalized intent and derive a gameplay safety factor before grade math.
    float3 source = tex2D(ReShade::BackBuffer, texcoord).rgb;
    float readability = saturate(Dalashade_Readability);
    float combat = saturate(Dalashade_CombatPressure);
    float atmosphere = saturate(Dalashade_Atmosphere);
    float highlightProtection = saturate(Dalashade_HighlightProtection);
    float shadowProtection = saturate(Dalashade_ShadowProtection);
    float cold = saturate(Dalashade_Cold);
    float heat = saturate(Dalashade_Heat);
    float magicGlow = saturate(Dalashade_MagicGlow);
    float neonGlow = saturate(Dalashade_NeonGlow);
    float foliage = saturate(Dalashade_FoliageDensity);
    float industrial = saturate(Dalashade_IndustrialHardness);
    float cosmic = saturate(Dalashade_CosmicMood);
    float cinematic = saturate(Dalashade_CinematicPermission);
    float night = saturate(Dalashade_Night);
    float moonlight = saturate(Dalashade_Moonlight);
    float artificialLight = saturate(Dalashade_ArtificialLight);
    float ambientDarkness = saturate(Dalashade_AmbientDarkness);
    float nightAtmosphere = saturate(Dalashade_NightAtmosphere);
    float daylight = saturate(Dalashade_Daylight);
    float sunlight = saturate(Dalashade_Sunlight);
    float openSkyLight = saturate(Dalashade_OpenSkyLight);
    float surfaceHeat = saturate(Dalashade_SurfaceHeat);
    float dayAtmosphere = saturate(Dalashade_DayAtmosphere);
    float dayReflection = saturate(Dalashade_DayReflection);
    float dayHighlightPressure = saturate(Dalashade_DayHighlightPressure);
    float materialFoliage = saturate(Dalashade_MaterialFoliage);
    float materialWaterSpecular = saturate(Dalashade_MaterialWaterSpecular);
    float materialWaterPlane = saturate(Dalashade_MaterialWaterPlane);
    float materialSpecularGlint = saturate(Dalashade_MaterialSpecularGlint);
    float materialSandDust = saturate(Dalashade_MaterialSandDust);
    float materialSnowIce = saturate(Dalashade_MaterialSnowIce);
    float materialMetalIndustrial = saturate(Dalashade_MaterialMetalIndustrial);
    float materialCrystal = saturate(Dalashade_MaterialCrystalAether);
    float materialNeonGlass = saturate(Dalashade_MaterialNeonGlass);
    float materialFireHeat = saturate(Dalashade_MaterialFireLavaHeat);
    float materialSkyCloudFog = saturate(Dalashade_MaterialSkyCloudFog);
    float materialSkin = saturate(Dalashade_MaterialSkinProtection);
    float materialVoid = saturate(Dalashade_MaterialVoidDarkness);
    float waterContext = saturate(Dalashade_WaterContext);
    float coastalContext = saturate(Dalashade_CoastalContext);
    float openOceanContext = saturate(Dalashade_OpenOceanContext);
    float shallowWaterContext = saturate(Dalashade_ShallowWaterContext);
    float wetSurfaceContext = saturate(Dalashade_WetSurfaceContext);
    Dalashade_MaterialResolve material = Dalashade_ResolveMaterials(
        source,
        texcoord,
        materialFoliage,
        materialWaterSpecular,
        materialWaterPlane,
        materialSpecularGlint,
        materialSandDust,
        materialSnowIce,
        0.0,
        materialMetalIndustrial,
        materialCrystal,
        materialNeonGlass,
        materialFireHeat,
        materialSkyCloudFog,
        materialSkin,
        materialVoid,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_WaterResolve water = Dalashade_ResolveWater(
        source,
        texcoord,
        waterContext,
        coastalContext,
        openOceanContext,
        shallowWaterContext,
        wetSurfaceContext,
        material.WaterPlane,
        material.SpecularGlint,
        material.SandDust,
        material.SkyCloudFog,
        material.SkinProtection,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_SafetyResolve safetyResolve = Dalashade_ResolveSafety(
        source,
        texcoord,
        material,
        water,
        highlightProtection,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_NormalField normalField = Dalashade_ResolveNormalField(
        source,
        texcoord,
        material,
        water,
        safetyResolve,
        Dalashade_NormalFieldEnabled,
        Dalashade_NormalFieldStrength,
        Dalashade_NormalDepthStrength,
        Dalashade_NormalDetailStrength,
        Dalashade_NormalMaterialInfluence,
        Dalashade_NormalWaterSuppression,
        Dalashade_NormalSkinSuppression,
        Dalashade_NormalSkySuppression);
    float materialFoliagePixel = material.Foliage;
    float materialSandPixel = material.SandDust;
    float materialSnowPixel = material.SnowIce;
    float materialMetalPixel = material.MetalIndustrial;
    float materialCrystalPixel = saturate(max(material.CrystalAether, max(material.NeonGlass, material.FireLavaHeat * 0.65)));
    float materialWaterPixel = saturate(max(max(material.WaterPlane, water.WaterPixelConfidence), water.WaterSurface));
    float materialSpecularPixel = saturate(max(material.SpecularGlint, water.FoamOrEdge * 0.65));
    float materialSkyPixel = saturate(max(material.SkyCloudFog, safetyResolve.SkyReject));
    float materialSkinPixel = saturate(max(material.SkinProtection, safetyResolve.SkinReject));
    float materialVoidPixel = material.VoidDarkness;
    float waterToneProtect = saturate(max(water.WaterPixelConfidence, water.WaterReceiver * 0.72));
    float skyFogToneProtect = saturate(max(material.SkyCloudFog, safetyResolve.SkyReject));
    float snowToneProtect = saturate(max(materialSnowPixel, safetyResolve.SnowProtect));
    float sandToneProtect = saturate(max(materialSandPixel, safetyResolve.BrightSandProtect));
    float aetherToneProtect = saturate(max(material.CrystalAether, max(material.NeonGlass, material.FireLavaHeat)));
    float normalFieldInfluence = saturate(Dalashade_NormalFieldEnabled * Dalashade_NormalFieldStrength * Dalashade_NormalMaterialInfluence);
    float normalStableStructure = saturate(normalFieldInfluence * normalField.StructureCandidate * (0.35 + normalField.NormalConfidence * 0.65) * (1.0 - materialSkinPixel * 0.70));
    float normalUnstableEdge = saturate(normalFieldInfluence * normalField.EdgeDiscontinuity * (1.0 - normalField.NormalConfidence * 0.45) * (1.0 - materialSkyPixel * 0.75));
    float normalDetailProtection = saturate(normalFieldInfluence * normalField.DetailStrength * (1.0 - normalField.EdgeDiscontinuity * 0.45) * (1.0 - materialSkinPixel * 0.60));
    float manualStrength = saturate(Dalashade_ManualStrength);
    float safety = 1.0 - saturate(readability * 0.42 + combat * 0.58);
    float standaloneStrength = saturate(Dalashade_StandaloneStrength);
    float standaloneSafe = saturate(standaloneStrength * safety * (1.0 - combat * 0.30));
    float foliageRichness = max(foliage, materialFoliage) * atmosphere * safety;
    float heatIdentity = max(heat, materialSandDust);
    float coldIdentity = max(cold, materialSnowIce);
    float hardSurfaceIdentity = max(industrial, materialMetalIndustrial);
    float aetherIdentity = max(max(magicGlow, materialCrystal), materialNeonGlass);
    float fireIdentity = max(heat, materialFireHeat);
    float authoredIdentity = max(max(foliage, max(heat, cold)), max(max(neonGlow, magicGlow), max(industrial, cosmic)));
    float dayIdentity = saturate(daylight * 0.18 + sunlight * 0.16 + openSkyLight * 0.10 + dayAtmosphere * 0.12 + dayReflection * 0.08);
    float gradeStrength = manualStrength * (0.44 + atmosphere * 0.18 + cinematic * 0.18 + authoredIdentity * 0.08 + night * 0.06 + dayIdentity * 0.04) * (0.55 + safety * 0.45);
    gradeStrength *= lerp(1.0, 1.18, standaloneSafe);

    float luma = Dalashade_Luma(source);
    float highlightMask = smoothstep(0.62, 0.98, luma);
    float shadowMask = 1.0 - smoothstep(0.05, 0.34, luma);
    float chroma = max(max(source.r, source.g), source.b) - min(min(source.r, source.g), source.b);
    float brightDaySurface = saturate(max(
        max(water.WaterSurface, water.WetShoreline),
        max(max(materialSandPixel, materialSnowPixel), max(materialSkyPixel, materialSpecularPixel * 0.65))));
    float daylightShoulder = saturate(
        dayHighlightPressure
        * highlightMask
        * brightDaySurface
        * (0.50 + openSkyLight * 0.35 + sunlight * 0.25 + dayReflection * 0.15));
    float highSunChromaRestraint = saturate(
        sunlight
        * openSkyLight
        * highlightMask
        * chroma
        * brightDaySurface
        * (1.0 - materialSkinPixel * 0.85));
    float dayShadowFill = saturate(
        daylight
        * openSkyLight
        * shadowMask
        * (1.0 - combat * 0.55)
        * (1.0 - ambientDarkness * 0.70)
        * (1.0 - max(materialVoid, materialVoidPixel) * 0.75)
        * (1.0 - max(material.Foliage, materialFoliagePixel) * 0.20));
    float coastalDayIdentity = saturate(daylight * (0.30 + dayReflection * 0.28 + openSkyLight * 0.24) * max(max(waterContext, coastalContext), materialWaterPixel));
    float canopyDayIdentity = saturate(daylight * dayAtmosphere * max(foliage, max(materialFoliage, materialFoliagePixel)));
    float desertDayIdentity = saturate(daylight * max(surfaceHeat, heat) * max(materialSandDust, materialSandPixel));
    float snowDayIdentity = saturate(daylight * openSkyLight * max(coldIdentity, materialSnowPixel));
    float highTechDayIdentity = saturate(daylight * max(industrial, materialMetalPixel) * (0.55 + max(neonGlow, materialCrystalPixel) * 0.35));

    // Build a mild intent-driven grade target. Manual controls add test pressure, not a new architecture path.
    float exposureTrim = Dalashade_ManualExposure
        + (shadowProtection * 0.020)
        - dayHighlightPressure * highlightMask * 0.012
        + sunlight * (1.0 - highlightMask) * 0.004
        - (highlightProtection * 0.026)
        - (combat * 0.010)
        - (hardSurfaceIdentity * 0.006)
        - (cosmic * 0.004);
    float contrastAmount = Dalashade_ManualContrast
        + (cinematic * safety * 0.052)
        + (night * safety * 0.026)
        + (ambientDarkness * 0.020)
        + (atmosphere * safety * 0.018)
        + (foliageRichness * 0.014)
        + (hardSurfaceIdentity * safety * 0.045)
        + (cosmic * safety * 0.020)
        - (readability * 0.026)
        - normalUnstableEdge * 0.012
        + normalStableStructure * shadowMask * 0.006;
    float saturationAmount = Dalashade_ManualSaturation
        + (cinematic * safety * 0.035)
        + (max(aetherIdentity, neonGlow) * safety * 0.025)
        + (material.FireLavaHeat * safety * 0.010)
        + (foliageRichness * 0.030)
        + (artificialLight * safety * 0.018)
        + (cosmic * safety * 0.014)
        - (hardSurfaceIdentity * 0.040)
        - (materialSkin * 0.020)
        - (readability * 0.032)
        - (combat * 0.026)
        - normalUnstableEdge * 0.010;
    float temperature = Dalashade_ManualTemperature + (max(heatIdentity, fireIdentity) * 0.074) - (coldIdentity * 0.065) - (cosmic * 0.040) - (hardSurfaceIdentity * 0.018) - (moonlight * 0.048) + (artificialLight * 0.022);
    float tint = Dalashade_ManualTint + (aetherIdentity * 0.030) - (max(neonGlow, material.NeonGlass) * 0.012) + (cosmic * 0.020) - (hardSurfaceIdentity * 0.010) + (moonlight * 0.010);

    contrastAmount += standaloneSafe * (0.010 + night * 0.006 + daylight * 0.004 + hardSurfaceIdentity * 0.004);
    saturationAmount += standaloneSafe * (0.008 + aetherIdentity * 0.004 + foliageRichness * 0.004 - readability * 0.004);
    temperature += standaloneSafe * (heatIdentity * 0.010 - coldIdentity * 0.008);
    tint += standaloneSafe * (aetherIdentity * 0.006 - hardSurfaceIdentity * 0.003);

    float skinTintGuard = 1.0 - max(materialSkin * 0.34, materialSkinPixel * 0.45);
    saturationAmount *= 1.0 - max(materialSkin * 0.24, materialSkinPixel * 0.34);
    temperature *= skinTintGuard;
    tint *= skinTintGuard;
    float materialIdentityGuard = saturate(
        materialSkinPixel * 0.30
        + waterToneProtect * 0.18
        + skyFogToneProtect * 0.16
        + snowToneProtect * 0.14
        + sandToneProtect * 0.10
        + aetherToneProtect * 0.10);
    contrastAmount *= 1.0 - materialIdentityGuard * 0.035;
    saturationAmount *= 1.0 - saturate(materialSkinPixel * 0.12 + skyFogToneProtect * 0.06 + normalUnstableEdge * 0.04);
    temperature *= 1.0 - saturate(waterToneProtect * 0.08 + snowToneProtect * 0.08 + aetherToneProtect * 0.05);
    tint *= 1.0 - saturate(waterToneProtect * 0.06 + normalUnstableEdge * 0.05);

    exposureTrim = clamp(exposureTrim, -0.085, 0.075);
    contrastAmount = clamp(contrastAmount, -0.085, 0.115);
    saturationAmount = clamp(saturationAmount, -0.095, 0.115);
    temperature = clamp(temperature, -0.110, 0.120);
    tint = clamp(tint, -0.075, 0.075);

    float3 graded = source + exposureTrim;
    graded = Dalashade_SafeContrast(graded, contrastAmount);
    graded = Dalashade_SafeSaturation(graded, saturationAmount);
    graded = Dalashade_TemperatureTint(graded, temperature, tint);

    // Biome-aware color response is selective: greens are protected in forests, metal is harder, and cosmic scenes cool shadows without global saturation abuse.
    float greenSignal = saturate((source.g - max(source.r, source.b) * 0.78) * 2.4);
    float warmSignal = saturate((source.r - source.b) * 1.8 + heat * 0.20);
    float coolSignal = saturate((source.b - source.r) * 1.6 + cold * 0.18 + cosmic * 0.28);
    float foliageColor = foliageRichness * max(greenSignal, materialFoliagePixel * 0.68) * (1.0 - highlightMask * 0.45);
    graded = lerp(graded, graded * float3(0.965, 1.055, 0.940), foliageColor * 0.18);
    graded = lerp(graded, graded * float3(1.038, 1.008, 0.948), max(heatIdentity, fireIdentity) * max(warmSignal, materialSandPixel * 0.48) * 0.048 * safety * (1.0 - materialSkin * 0.35));
    graded = lerp(graded, graded * float3(0.940, 0.970, 1.055), max(coldIdentity, cosmic) * max(coolSignal, materialSnowPixel * 0.42) * 0.060 * safety);
    graded = lerp(graded, float3(Dalashade_Luma(graded), Dalashade_Luma(graded), Dalashade_Luma(graded)), hardSurfaceIdentity * 0.045);
    graded = lerp(graded, graded * float3(0.982, 0.995, 1.025), materialMetalIndustrial * max(coolSignal, materialMetalPixel * 0.45) * 0.040 * safety);
    graded = lerp(graded, graded * float3(0.985, 0.978, 1.035), max(materialCrystal, material.NeonGlass) * max(max(coolSignal, greenSignal), materialCrystalPixel * 0.42) * 0.034 * safety);

    // Daytime tonal identity is a contextual layer: it protects bright materials and nudges mids without acting like bloom, fog, or reflection.
    float midtoneDayMask = smoothstep(0.18, 0.62, luma) * (1.0 - highlightMask * 0.62);
    graded = lerp(graded, graded * float3(1.018, 1.006, 0.982), coastalDayIdentity * midtoneDayMask * 0.10 * safety);
    graded = lerp(graded, graded * float3(0.978, 1.010, 1.028), coastalDayIdentity * max(materialWaterPixel, materialSkyPixel * 0.55) * 0.08 * (1.0 - materialSkinPixel * 0.80));
    graded = lerp(graded, graded * float3(0.970, 1.038, 0.948), canopyDayIdentity * max(greenSignal, materialFoliagePixel) * 0.075 * safety * (1.0 - highlightMask * 0.45));
    graded = lerp(graded, graded * float3(1.030, 1.006, 0.945), desertDayIdentity * warmSignal * midtoneDayMask * 0.075 * safety);
    graded = lerp(graded, graded * float3(0.965, 0.986, 1.035), snowDayIdentity * max(materialSnowPixel, coolSignal) * 0.052 * safety * (1.0 - daylightShoulder * 0.35));
    graded = lerp(graded, graded * float3(0.972, 0.988, 1.024), highTechDayIdentity * 0.045 * safety);
    graded += dayShadowFill * 0.012 * manualStrength * (1.0 - source) * (1.0 - materialSkinPixel * 0.55);
    float3 chromaRestrained = Dalashade_SafeSaturation(graded, -0.080 * highSunChromaRestraint);
    chromaRestrained += Dalashade_Luma(graded) - Dalashade_Luma(chromaRestrained);
    graded = lerp(graded, chromaRestrained, 0.40 * manualStrength);
    float3 waterPreserve = lerp(graded, source + (graded - source) * float3(0.88, 0.96, 1.06), 0.45);
    graded = lerp(graded, waterPreserve, waterToneProtect * 0.10 * manualStrength * (1.0 - materialSkinPixel * 0.80));
    float3 glowPreserve = lerp(graded, source + (graded - source) * 0.88, 0.35);
    graded = lerp(graded, max(graded, glowPreserve), max(aetherToneProtect, material.FireLavaHeat) * 0.055 * manualStrength);

    // Night light hierarchy: unlit regions deepen, moonlit mids cool gently, and artificial light affects bright local pools instead of lifting the frame.
    float moonlitSurface = moonlight * smoothstep(0.18, 0.66, luma) * (1.0 - highlightMask * 0.34) * safety;
    float artificialLightPool = artificialLight * smoothstep(0.42, 0.92, luma) * (0.55 + max(max(neonGlow, magicGlow), materialCrystal) * 0.45) * (1.0 - combat * 0.42);
    float nightDarken = ambientDarkness * shadowMask * (0.052 + night * 0.022 + max(materialVoid, materialVoidPixel) * 0.020) * (1.0 - readability * 0.28);
    nightDarken *= 1.0 - artificialLightPool * 0.42;
    graded *= 1.0 - nightDarken;
    graded = lerp(graded, graded * float3(0.90, 0.96, 1.055), moonlitSurface * 0.20);
    graded = lerp(graded, graded * float3(1.060, 0.990, 0.900), artificialLightPool * 0.12 * (1.0 - materialSkin * 0.35));

    // Cinematic bias is intentionally small and automatically weakens under gameplay pressure.
    float3 cinematicTint = lerp(float3(1.0, 0.985, 0.955), float3(0.955, 0.985, 1.0), coldIdentity);
    cinematicTint = lerp(cinematicTint, float3(1.0, 0.962, 0.912), heatIdentity * 0.60);
    cinematicTint = lerp(cinematicTint, float3(0.95, 0.98, 1.04), neonGlow * 0.30);
    cinematicTint = lerp(cinematicTint, float3(0.965, 1.020, 0.950), foliageRichness * 0.28);
    cinematicTint = lerp(cinematicTint, float3(0.920, 0.960, 1.055), cosmic * 0.35);
    cinematicTint = lerp(cinematicTint, float3(0.965, 0.982, 1.010), hardSurfaceIdentity * 0.20);
    cinematicTint = lerp(cinematicTint, float3(0.955, 0.970, 1.045), materialCrystal * 0.16);
    float cinematicBias = cinematic * safety * (0.025 + atmosphere * 0.015);
    cinematicBias *= 1.0 - materialSkin * 0.18;
    graded = lerp(graded, graded * cinematicTint, cinematicBias);

    // Highlight and shadow protection keep the grade usable in gameplay and bright weather.
    float rolloff = min((highlightProtection * 0.17 + coldIdentity * 0.060 + heatIdentity * 0.040 + hardSurfaceIdentity * 0.018 + cosmic * 0.020) * highlightMask + daylightShoulder * 0.14, 0.30);
    graded = lerp(graded, graded / (1.0 + graded), rolloff * manualStrength);

    float selectiveShadowLift = (0.060 - night * 0.020 - combat * 0.022) * (1.0 - max(foliage, materialFoliage) * 0.38) * (1.0 - hardSurfaceIdentity * 0.22) * (1.0 - materialVoid * 0.52);
    float lift = min(shadowProtection * shadowMask * selectiveShadowLift, 0.072);
    graded += lift * manualStrength * (1.0 - source);

    // Preserve black depth in forests, industrial zones, and gloom-heavy scenes by recovering contrast in the deepest shadows.
    float blackDepth = shadowMask * (ambientDarkness * 0.060 + max(foliage, max(materialFoliage, materialFoliagePixel)) * 0.040 + hardSurfaceIdentity * 0.030 + cosmic * 0.014 + max(materialVoid, materialVoidPixel) * 0.060) * (1.0 - combat * 0.45);
    graded = lerp(graded, graded * (1.0 - blackDepth), saturate(1.0 - readability * 0.40));

    // Skin protection reins in extreme shifts on smooth warm midtones without flattening the whole grade.
    float skinLikeMidtone = max(materialSkin * 0.65, materialSkinPixel)
        * smoothstep(0.18, 0.58, luma)
        * (1.0 - smoothstep(0.78, 0.98, luma))
        * smoothstep(0.02, 0.22, source.r - source.b)
        * (1.0 - smoothstep(0.10, 0.42, max(max(source.r, source.g), source.b) - min(min(source.r, source.g), source.b)));
    graded = lerp(graded, source + (graded - source) * 0.62, skinLikeMidtone * 0.50);
    graded = lerp(graded, source + (graded - source) * 0.90, normalUnstableEdge * 0.16);
    graded = lerp(graded, max(graded, source * 0.985), normalStableStructure * shadowMask * 0.055);
    graded = lerp(graded, source + (graded - source) * 0.94, normalDetailProtection * highlightMask * 0.055);

    // Guardrails prevent the grade from crushing or blowing out relative to the input.
    graded = min(graded, source + 0.18);
    graded = max(graded, source - 0.16);
    graded = clamp(graded, 0.015, 0.985);

    float3 result = lerp(source, graded, saturate(gradeStrength));

    int debugMode = Dalashade_AdaptiveGradeDebugMode;
    if (Dalashade_ShowDebugMask && debugMode == 0)
    {
        debugMode = 1;
    }

    if (debugMode == 1)
    {
        return float4(saturate(nightDarken * 8.0 + rolloff * 3.0), saturate(artificialLightPool + lift * 6.0), saturate(moonlitSurface + nightAtmosphere * 0.35 + cinematicBias * 8.0), 1.0);
    }
    if (debugMode == 2)
    {
        return float4(saturate(daylightShoulder * 4.0 + coastalDayIdentity * 0.55), saturate(dayShadowFill * 1.4 + canopyDayIdentity * 0.60 + desertDayIdentity * 0.35), saturate(highSunChromaRestraint * 2.8 + dayAtmosphere * 0.28 + dayReflection * 0.35), 1.0);
    }
    if (debugMode == 3)
    {
        return float4(saturate(max(materialWaterPixel, materialSkyPixel)), saturate(max(materialFoliagePixel, max(materialSandPixel, materialSnowPixel))), saturate(max(materialCrystalPixel, max(materialMetalPixel, materialVoidPixel))), 1.0);
    }
    if (debugMode == 4)
    {
        return float4(saturate(rolloff * 4.0), saturate(daylightShoulder * 4.0), saturate(highlightMask), 1.0);
    }
    if (debugMode == 5)
    {
        return float4(saturate(highSunChromaRestraint * 4.0), saturate(chroma * brightDaySurface), saturate(brightDaySurface), 1.0);
    }
    if (debugMode == 6)
    {
        return float4(saturate(materialSkinPixel), saturate(skinLikeMidtone), saturate(materialSkin), 1.0);
    }
    if (debugMode == 7)
    {
        return float4(saturate(abs(result - source) * 8.0), 1.0);
    }

    return float4(result, 1.0);
}

technique Dalashade_AdaptiveGrade
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_AdaptiveGradePS;
    }
}
