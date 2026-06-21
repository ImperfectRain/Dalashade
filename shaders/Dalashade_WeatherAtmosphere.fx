#include "ReShade.fxh"
#include "Dalashade_FrameData.fxh"

uniform float Dalashade_Haze <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Haze";
    ui_tooltip = "Scene-driven haze/fog amount. Dalashade writes this when custom shader support is enabled.";
> = 0.0;

uniform float Dalashade_Wetness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Wetness";
    ui_tooltip = "Scene-driven rain/wet surface amount.";
> = 0.0;

uniform float Dalashade_Cold <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cold";
    ui_tooltip = "Scene-driven snow/cold amount.";
> = 0.0;

uniform float Dalashade_Heat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Heat";
    ui_tooltip = "Scene-driven heat/dust glare amount.";
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

uniform float Dalashade_CombatPressure <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Combat Pressure";
> = 0.0;

uniform float Dalashade_Atmosphere <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Atmosphere";
> = 0.0;

uniform float Dalashade_Readability <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Readability";
    ui_tooltip = "Scene-driven gameplay readability pressure. Higher values damp heavy atmosphere.";
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
    ui_tooltip = "Scene-driven foliage density. Higher values restrain veil haze and add subtle canopy light.";
> = 0.0;

uniform float Dalashade_IndustrialHardness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Industrial Hardness";
    ui_tooltip = "Scene-driven industrial, high-tech, ruin, or constructed-hardness context.";
> = 0.0;

uniform float Dalashade_CosmicMood <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cosmic Mood";
    ui_tooltip = "Scene-driven cosmic, lunar, alien, or aetherial air context.";
> = 0.0;

uniform float Dalashade_CinematicPermission <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cinematic Permission";
    ui_tooltip = "Scene-driven permission for stronger atmosphere outside gameplay-critical moments.";
> = 0.0;

uniform float Dalashade_Night <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night";
    ui_tooltip = "Scene-driven nighttime context. Higher values darken ambient air and preserve unlit depth.";
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
    ui_tooltip = "Scene-driven unlit baseline darkness.";
> = 0.0;

uniform float Dalashade_NightAtmosphere <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night Atmosphere";
    ui_tooltip = "Scene-driven nighttime weather/air mood without generic haze.";
> = 0.0;

uniform float Dalashade_Daylight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Daylight"; ui_tooltip = "Scene-driven daytime context. Does not globally brighten."; > = 0.0;
uniform float Dalashade_Sunlight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Sunlight"; ui_tooltip = "Scene-driven direct sunlight pressure."; > = 0.0;
uniform float Dalashade_OpenSkyLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Open Sky Light"; ui_tooltip = "Scene-driven open-sky daylight for sky, water, snow, and sand."; > = 0.0;
uniform float Dalashade_SurfaceHeat < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Surface Heat"; ui_tooltip = "Scene-driven sunlit heat and warm surface air."; > = 0.0;
uniform float Dalashade_DayAtmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Atmosphere"; ui_tooltip = "Scene-driven daylight air, mist, storm, coastal, or dust atmosphere."; > = 0.0;
uniform float Dalashade_DayReflection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Reflection"; ui_tooltip = "Scene-driven daytime reflection context for valid material receivers."; > = 0.0;
uniform float Dalashade_DayHighlightPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Highlight Pressure"; ui_tooltip = "Scene-driven daytime bright-surface protection."; > = 0.0;

uniform float Dalashade_MaterialFoliage <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Foliage";
    ui_tooltip = "Inferred foliage likelihood. Supports humid canopy atmosphere without gray wash.";
> = 0.0;

uniform float Dalashade_MaterialSandDust <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sand/Dust";
    ui_tooltip = "Inferred sand or dust likelihood. Supports warm distance haze and dust air.";
> = 0.0;

uniform float Dalashade_MaterialSnowIce <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Snow/Ice";
    ui_tooltip = "Inferred snow or ice likelihood. Supports cold air and white highlight protection.";
> = 0.0;

uniform float Dalashade_MaterialStoneRuins <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Stone/Ruins";
    ui_tooltip = "Inferred stone, ruins, or masonry likelihood. Supports interior, ancient, and damp stone atmosphere.";
> = 0.0;

uniform float Dalashade_MaterialMetalIndustrial <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Metal/Industrial";
    ui_tooltip = "Inferred metal or industrial likelihood. Supports high-tech, imperial, and constructed air restraint.";
> = 0.0;

uniform float Dalashade_MaterialWaterSpecular <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water/Specular";
    ui_tooltip = "Inferred water or wet/specular likelihood. Supports coastal mist or wet-air diffusion only when weather supports it.";
> = 0.0;

uniform float Dalashade_MaterialWaterPlane <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water Plane";
    ui_tooltip = "Optional split water-surface likelihood for coastal humidity and water-plane atmosphere.";
> = 0.0;

uniform float Dalashade_MaterialSpecularGlint <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Specular Glint";
    ui_tooltip = "Optional split thin-glint likelihood for small wet highlight response.";
> = 0.0;

uniform float Dalashade_WaterContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Water Context";
    ui_tooltip = "Scene-level water plausibility for coastal or shoreline air. This is not a pixel reflection mask.";
> = 0.0;

uniform float Dalashade_CoastalContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Coastal Context";
    ui_tooltip = "Scene-level coastal plausibility for subtle sea-air depth.";
> = 0.0;

uniform float Dalashade_OpenOceanContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Open Ocean Context";
> = 0.0;

uniform float Dalashade_ShallowWaterContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Shallow Water Context";
> = 0.0;

uniform float Dalashade_WetSurfaceContext <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Wet Surface Context";
> = 0.0;

uniform float Dalashade_MaterialCrystalAether <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Crystal/Aether";
    ui_tooltip = "Inferred crystal or aether likelihood. Supports subtle cosmic/aetherial depth veil.";
> = 0.0;

uniform float Dalashade_MaterialNeonGlass <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Neon/Glass";
    ui_tooltip = "Inferred neon or glass likelihood. Restrains saturated atmosphere while allowing subtle city/aether air.";
> = 0.0;

uniform float Dalashade_MaterialFireLavaHeat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Fire/Lava/Heat";
    ui_tooltip = "Inferred fire, lava, or heat likelihood. Supports warm air without becoming bloom.";
> = 0.0;

uniform float Dalashade_MaterialSkyCloudFog <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sky/Cloud/Fog";
    ui_tooltip = "Inferred sky, cloud, fog, or atmosphere likelihood. Controls actual fog/mist/sky depth behavior.";
> = 0.0;

uniform float Dalashade_MaterialSkinProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Skin Protection";
    ui_tooltip = "Inferred skin/character protection. Restrains atmosphere tinting on skin-like areas.";
> = 0.0;

uniform float Dalashade_MaterialVoidDarkness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Void/Darkness";
    ui_tooltip = "Inferred void, abyss, or darkness likelihood. Supports gloom/void air without gray fog wash.";
> = 0.0;

uniform int Dalashade_MaterialDebugMode <
    ui_type = "combo";
    ui_items = "Off\0Overview\0Foliage humidity\0Sand/dust depth\0Snow/ice air\0Water/wet mist\0Crystal/aether veil\0Sky/fog depth\0Final air influence\0Water plane air\0Specular glint response\0";
    ui_label = "Dalashade Material Debug Mode";
    ui_tooltip = "Shows material-aware air influence masks. These masks are inferred likelihoods, not true engine material IDs.";
> = 0;

uniform float Dalashade_MaterialDebugStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Debug Strength";
> = 1.0;

uniform bool Dalashade_EnableDepthAssist <
    ui_label = "Enable Depth Assist";
    ui_tooltip = "Optional material-mask helper. Disabled by default; atmosphere masks still work without depth.";
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
    ui_tooltip = "Optional inferred normal/surface field gate. WeatherAtmosphere uses this only for subtle atmosphere anchoring.";
> = 0.0;

uniform float Dalashade_NormalFieldStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Strength";
    ui_tooltip = "Global scale for optional NormalField weather shaping.";
> = 0.0;

uniform float Dalashade_NormalDepthStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Depth Strength";
    ui_tooltip = "Depth-normal contribution for optional atmosphere anchoring.";
> = 0.0;

uniform float Dalashade_NormalDetailStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Detail Strength";
    ui_tooltip = "Detail-normal contribution for optional atmosphere anchoring.";
> = 0.0;

uniform float Dalashade_NormalMaterialInfluence <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Material Influence";
    ui_tooltip = "Material-aware scale for optional NormalField weather shaping.";
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
    ui_tooltip = "Manual fallback strength for testing without Dalashade. Keep low for gameplay.";
> = 0.35;

uniform float Dalashade_StandaloneStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Standalone Stack Strength";
    ui_tooltip = "0 keeps WeatherAtmosphere supportive for an existing preset. 1 lets it carry more weather mood while preserving material and gameplay safety.";
> = 0.0;

uniform float Dalashade_ManualHazeBoost <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Manual Haze Boost";
> = 0.0;

uniform float Dalashade_ManualGlowBoost <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Manual Glow Boost";
> = 0.0;

uniform float Dalashade_ManualMood <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Manual Storm/Dark Mood";
> = 0.0;

uniform bool Dalashade_ShowDebugMask <
    ui_label = "Show Debug Mask";
    ui_tooltip = "Visualizes the selected debug mask for tuning.";
> = false;

uniform int Dalashade_DebugView <
    ui_type = "combo";
    ui_items = "Composite\0Depth haze\0Highlight protection\0Weather glow\0Foliage dampening\0Heat/dust\0";
    ui_label = "Debug View";
    ui_tooltip = "Chooses the debug mask shown when Show Debug Mask is enabled.";
> = 0;

float Dalashade_Saturate(float value)
{
    return saturate(value);
}

float3 Dalashade_SafeLerp(float3 a, float3 b, float amount)
{
    return lerp(a, b, Dalashade_Saturate(amount));
}

float3 Dalashade_SoftLighten(float3 color, float3 tint, float amount)
{
    return color + tint * amount * (1.0 - color);
}

float4 Dalashade_WeatherAtmospherePS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    // Sample the current ReShade backbuffer and build stable depth/luma masks.
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    float depth = ReShade::GetLinearizedDepth(texcoord);
    float distant = smoothstep(0.04, 0.86, depth);
    float midDistance = smoothstep(0.015, 0.35, depth);

    // Dalashade-driven intent values stay normalized so manual sliders can still test the shader alone.
    float haze = Dalashade_Saturate(max(Dalashade_Haze, Dalashade_ManualHazeBoost));
    float wetness = Dalashade_Saturate(Dalashade_Wetness);
    float cold = Dalashade_Saturate(Dalashade_Cold);
    float heat = Dalashade_Saturate(Dalashade_Heat);
    float highlightProtection = Dalashade_Saturate(Dalashade_HighlightProtection);
    float shadowProtection = Dalashade_Saturate(Dalashade_ShadowProtection);
    float combat = Dalashade_Saturate(Dalashade_CombatPressure);
    float atmosphere = Dalashade_Saturate(Dalashade_Atmosphere);
    float readability = Dalashade_Saturate(Dalashade_Readability);
    float magicGlow = Dalashade_Saturate(Dalashade_MagicGlow);
    float neonGlow = Dalashade_Saturate(Dalashade_NeonGlow);
    float foliage = Dalashade_Saturate(Dalashade_FoliageDensity);
    float industrialHardness = Dalashade_Saturate(Dalashade_IndustrialHardness);
    float cosmicMood = Dalashade_Saturate(Dalashade_CosmicMood);
    float cinematic = Dalashade_Saturate(Dalashade_CinematicPermission);
    float night = Dalashade_Saturate(Dalashade_Night);
    float moonlight = Dalashade_Saturate(Dalashade_Moonlight);
    float artificialLight = Dalashade_Saturate(Dalashade_ArtificialLight);
    float ambientDarkness = Dalashade_Saturate(Dalashade_AmbientDarkness);
    float nightAtmosphere = Dalashade_Saturate(Dalashade_NightAtmosphere);
    float daylight = Dalashade_Saturate(Dalashade_Daylight);
    float sunlight = Dalashade_Saturate(Dalashade_Sunlight);
    float openSkyLight = Dalashade_Saturate(Dalashade_OpenSkyLight);
    float surfaceHeat = Dalashade_Saturate(Dalashade_SurfaceHeat);
    float dayAtmosphere = Dalashade_Saturate(Dalashade_DayAtmosphere);
    float dayReflection = Dalashade_Saturate(Dalashade_DayReflection);
    float dayHighlightPressure = Dalashade_Saturate(Dalashade_DayHighlightPressure);
    float manualStrength = Dalashade_Saturate(Dalashade_ManualStrength);
    float manualMood = Dalashade_Saturate(Dalashade_ManualMood);
    float manualGlow = Dalashade_Saturate(Dalashade_ManualGlowBoost);
    float materialFoliage = Dalashade_Saturate(Dalashade_MaterialFoliage);
    float materialSandDust = Dalashade_Saturate(Dalashade_MaterialSandDust);
    float materialSnowIce = Dalashade_Saturate(Dalashade_MaterialSnowIce);
    float materialStoneRuins = Dalashade_Saturate(Dalashade_MaterialStoneRuins);
    float materialMetalIndustrial = Dalashade_Saturate(Dalashade_MaterialMetalIndustrial);
    float materialWater = Dalashade_Saturate(Dalashade_MaterialWaterSpecular);
    float materialWaterPlane = Dalashade_Saturate(max(materialWater, Dalashade_MaterialWaterPlane));
    float materialSpecularGlint = Dalashade_Saturate(max(materialWater, Dalashade_MaterialSpecularGlint));
    float materialWaterGate = Dalashade_Saturate(max(materialWaterPlane, materialSpecularGlint));
    float materialCrystal = Dalashade_Saturate(Dalashade_MaterialCrystalAether);
    float materialNeonGlass = Dalashade_Saturate(Dalashade_MaterialNeonGlass);
    float materialFireHeat = Dalashade_Saturate(Dalashade_MaterialFireLavaHeat);
    float materialSkyFog = Dalashade_Saturate(Dalashade_MaterialSkyCloudFog);
    float materialSkin = Dalashade_Saturate(Dalashade_MaterialSkinProtection);
    float materialVoidDarkness = Dalashade_Saturate(Dalashade_MaterialVoidDarkness);

    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
    Dalashade_FrameDataSettings frameSettings = Dalashade_FrameData_DefaultSettings();
    frameSettings.MaterialFoliage = materialFoliage;
    frameSettings.MaterialWaterSpecular = materialWater;
    frameSettings.MaterialWaterPlane = Dalashade_MaterialWaterPlane;
    frameSettings.MaterialSpecularGlint = Dalashade_MaterialSpecularGlint;
    frameSettings.MaterialSandDust = materialSandDust;
    frameSettings.MaterialSnowIce = materialSnowIce;
    frameSettings.MaterialStoneRuins = materialStoneRuins;
    frameSettings.MaterialMetalIndustrial = materialMetalIndustrial;
    frameSettings.MaterialCrystalAether = materialCrystal;
    frameSettings.MaterialNeonGlass = materialNeonGlass;
    frameSettings.MaterialFireLavaHeat = materialFireHeat;
    frameSettings.MaterialSkyCloudFog = materialSkyFog;
    frameSettings.MaterialSkinProtection = materialSkin;
    frameSettings.MaterialVoidDarkness = materialVoidDarkness;
    frameSettings.WaterContext = Dalashade_WaterContext;
    frameSettings.CoastalContext = Dalashade_CoastalContext;
    frameSettings.OpenOceanContext = Dalashade_OpenOceanContext;
    frameSettings.ShallowWaterContext = Dalashade_ShallowWaterContext;
    frameSettings.WetSurfaceContext = Dalashade_WetSurfaceContext;
    frameSettings.HighlightProtection = highlightProtection;
    frameSettings.DepthAssistEnabled = Dalashade_EnableDepthAssist ? 1.0 : 0.0;
    frameSettings.DepthAssistStrength = Dalashade_DepthAssistStrength;
    frameSettings.DepthAssistConfidenceFloor = max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor);
    frameSettings.NormalFieldEnabled = Dalashade_NormalFieldEnabled;
    frameSettings.NormalFieldStrength = Dalashade_NormalFieldStrength;
    frameSettings.NormalDepthStrength = Dalashade_NormalDepthStrength;
    frameSettings.NormalDetailStrength = Dalashade_NormalDetailStrength;
    frameSettings.NormalMaterialInfluence = Dalashade_NormalMaterialInfluence;
    frameSettings.NormalWaterSuppression = Dalashade_NormalWaterSuppression;
    frameSettings.NormalSkinSuppression = Dalashade_NormalSkinSuppression;
    frameSettings.NormalSkySuppression = Dalashade_NormalSkySuppression;

    Dalashade_FrameBaseData frame = Dalashade_ResolveFrameBaseData(color, texcoord, frameSettings);
    Dalashade_FrameSurfaceData surface = Dalashade_ResolveFrameSurfaceData(color, texcoord, frame, frameSettings);
    materialFoliage = max(materialFoliage, frame.MaterialFoliage);
    materialSandDust = max(materialSandDust, frame.MaterialSandDust);
    materialSnowIce = max(materialSnowIce, frame.MaterialSnowIce);
    materialStoneRuins = max(materialStoneRuins, frame.MaterialStoneRuins);
    materialMetalIndustrial = max(materialMetalIndustrial, frame.MaterialMetalIndustrial);
    float waterAtmosphereContext = Dalashade_Saturate(max(frame.WaterPixelConfidence, max(frame.WaterReceiver, max(frame.WaterWetShoreline * 0.72, frame.WaterSource * 0.34))));
    materialWaterPlane = max(materialWaterPlane, max(waterAtmosphereContext * 0.74, max(Dalashade_WaterContext, Dalashade_CoastalContext) * 0.34));
    materialSpecularGlint = max(materialSpecularGlint, frame.WaterSpecularGlint);
    materialWaterGate = Dalashade_Saturate(max(materialWaterPlane, materialSpecularGlint));
    materialCrystal = max(materialCrystal, frame.MaterialCrystalAether);
    materialNeonGlass = max(materialNeonGlass, frame.MaterialNeonGlass);
    materialFireHeat = max(materialFireHeat, frame.MaterialFireLavaHeat);
    materialSkyFog = max(materialSkyFog, max(frame.MaterialSkyCloudFog, frame.SafetySkyReject));
    materialVoidDarkness = max(materialVoidDarkness, frame.MaterialVoidDarkness);
    float materialAetherNeon = Dalashade_Saturate(max(materialCrystal, materialNeonGlass));
    float skinAtmosphereProtect = Dalashade_Saturate(max(materialSkin, frame.SafetySkinReject));
    float highlightAtmosphereProtect = Dalashade_Saturate(max(frame.SafetyHighlightProtect, max(frame.SafetyBrightSandProtect, frame.SafetySnowProtect) * 0.55));
    float foliageNoiseProtect = Dalashade_Saturate(frame.SafetyFoliageNoiseReject);
    float normalFieldInfluence = Dalashade_Saturate(max(Dalashade_NormalFieldEnabled * Dalashade_NormalFieldStrength * Dalashade_NormalMaterialInfluence, surface.SurfaceDataInfluence));
    float normalGroundAnchor = Dalashade_Saturate(
        normalFieldInfluence
        * surface.GroundCandidate
        * (0.35 + surface.NormalConfidence * 0.35 + surface.OrientationConfidence * 0.20)
        * (1.0 - materialSkyFog * 0.72)
        * (1.0 - skinAtmosphereProtect * 0.80));
    float normalStructureAnchor = Dalashade_Saturate(
        normalFieldInfluence
        * surface.StructureCandidate
        * (0.30 + surface.NormalConfidence * 0.32)
        * (1.0 - max(materialWaterGate, materialSkyFog) * 0.55)
        * (1.0 - skinAtmosphereProtect * 0.82));
    float normalEdgeSafety = Dalashade_Saturate(
        normalFieldInfluence
        * surface.EdgeDiscontinuity
        * (0.35 + (1.0 - surface.NormalConfidence) * 0.35 + highlightAtmosphereProtect * 0.20));

    float brightMask = smoothstep(0.54, 0.96, luma);
    float specularMask = smoothstep(0.72, 1.0, luma);
    float shadowMask = 1.0 - smoothstep(0.08, 0.35, luma);

    // Gameplay pressure is the main safety valve. Combat should visibly cut heavy weather while retaining light mood.
    float gameplayDampen = 1.0 - saturate(combat * 0.62 + readability * 0.18);
    float cinematicBoost = 1.0 + cinematic * 0.12;
    float standaloneStrength = Dalashade_Saturate(Dalashade_StandaloneStrength);
    float standaloneAtmosphere = Dalashade_Saturate(standaloneStrength * gameplayDampen * (1.0 - highlightAtmosphereProtect * 0.25));

    // Atmospheric perspective: fog/mist and dust thicken with distance; foreground gameplay space stays mostly untouched.
    float realFogWeather = saturate(max(haze, materialSkyFog * haze));
    float waterMist = materialWaterPlane * max(max(wetness, dayReflection * 0.22), haze * 0.34 + nightAtmosphere * night * 0.12 + dayAtmosphere * 0.10) * smoothstep(0.18, 0.92, depth);
    waterMist *= 1.0 + normalGroundAnchor * 0.08;
    float dustAir = max(max(max(heat, materialSandDust), surfaceHeat * 0.70), materialFireHeat * 0.45) * (0.42 + haze * 0.18 + dayAtmosphere * 0.08 + normalGroundAnchor * 0.04) * smoothstep(0.22, 0.98, depth);
    float snowAir = max(cold, materialSnowIce) * (0.34 + haze * 0.18 + normalGroundAnchor * 0.03) * smoothstep(0.10, 0.92, depth);
    float aetherAir = materialAetherNeon * max(max(magicGlow, atmosphere * 0.45), neonGlow * 0.32) * smoothstep(0.18, 0.96, depth);
    float skyFogAir = materialSkyFog * max(max(realFogWeather, dayAtmosphere * openSkyLight * 0.22), nightAtmosphere * moonlight * 0.20) * smoothstep(0.12, 0.94, depth);
    float humidAir = max(foliage, materialFoliage) * atmosphere * (0.20 + wetness * 0.16 + haze * 0.10 + nightAtmosphere * night * 0.08 + normalStructureAnchor * 0.03) * smoothstep(0.12, 0.78, depth);
    float weatherAmount = max(max(realFogWeather, wetness * 0.62 + waterMist * 0.28), max(max(cold, materialSnowIce) * 0.58, max(heat, materialSandDust) * 0.68));
    float dustSoftness = max(heat, materialSandDust) * (0.50 + haze * 0.28);
    float fogLike = saturate(realFogWeather * (1.0 - max(heat, materialSandDust) * 0.28));
    float heatDistance = smoothstep(0.26, 0.96, depth);
    float distanceWeight = lerp(distant * 0.72 + midDistance * 0.18, heatDistance * heatDistance, max(heat, materialSandDust));
    float farAir = smoothstep(0.22, 0.96, depth);
    float midAir = smoothstep(0.10, 0.70, depth) * (1.0 - smoothstep(0.88, 1.0, depth) * 0.20);
    float skyAir = saturate(materialSkyFog * (0.48 + farAir * 0.42 + openSkyLight * daylight * 0.18));
    float weatherLaneSafety = standaloneAtmosphere
        * (1.0 - skinAtmosphereProtect * 0.72)
        * (1.0 - foliageNoiseProtect * 0.24)
        * (1.0 - normalEdgeSafety * 0.30);
    float clearCoastalAirIdentity = saturate(
        weatherLaneSafety
        * daylight
        * openSkyLight
        * max(max(Dalashade_CoastalContext, Dalashade_WaterContext), frame.WaterPixelConfidence * 0.52)
        * (0.32 + dayReflection * 0.26 + dayAtmosphere * 0.12)
        * (1.0 - wetness * 0.50)
        * (1.0 - haze * 0.42));
    float clearOpenAirIdentity = saturate(
        weatherLaneSafety
        * daylight
        * openSkyLight
        * max(dayAtmosphere, atmosphere * 0.38)
        * (0.34 + sunlight * 0.16 + materialSkyFog * 0.12 + farAir * 0.10)
        * (1.0 - max(max(wetness, fogLike), haze * 0.54))
        * (1.0 - materialSkin * 0.42));
    float rainWetAirIdentity = saturate(
        weatherLaneSafety
        * max(wetness, max(waterMist * 1.8, materialSpecularGlint * 0.42))
        * (0.42 + max(dayAtmosphere, nightAtmosphere) * 0.24 + materialWaterGate * 0.18)
        * (1.0 - highlightAtmosphereProtect * 0.18));
    float coastalNightAirIdentity = saturate(
        weatherLaneSafety
        * night
        * max(max(Dalashade_CoastalContext, Dalashade_WaterContext), max(materialWaterGate, frame.WaterPixelConfidence) * 0.62)
        * max(nightAtmosphere, haze * 0.56)
        * (0.32 + moonlight * 0.20 + artificialLight * 0.14 + materialSkyFog * 0.18)
        * (1.0 - combat * 0.32));
    float stormAirIdentity = saturate(
        weatherLaneSafety
        * max(max(wetness * max(haze, atmosphere), manualMood), nightAtmosphere * max(wetness, materialSkyFog) * 0.72)
        * (0.50 + ambientDarkness * 0.20 + materialSkyFog * 0.18)
        * (1.0 - combat * 0.32));
    float fogMistAirIdentity = saturate(
        weatherLaneSafety
        * max(fogLike, materialSkyFog * max(haze, max(dayAtmosphere, nightAtmosphere) * 0.42))
        * (0.42 + farAir * 0.28 + skyAir * 0.18)
        * (1.0 - max(heat, materialSandDust) * 0.36));
    float dustHeatAirIdentity = saturate(
        weatherLaneSafety
        * max(max(heat, surfaceHeat * 0.74), materialSandDust)
        * (0.40 + haze * 0.20 + daylight * 0.18 + farAir * 0.18)
        * (1.0 - frame.SafetyBrightSandProtect * brightMask * 0.26)
        * (1.0 - materialSkin * 0.56));
    float dustStormAirIdentity = saturate(
        weatherLaneSafety
        * materialSandDust
        * max(haze, dayAtmosphere * 0.42)
        * (0.38 + farAir * 0.24 + heat * 0.12)
        * (1.0 - frame.SafetyBrightSandProtect * brightMask * 0.30)
        * (1.0 - materialSkin * 0.62));
    float heatWaveAirIdentity = saturate(
        weatherLaneSafety
        * max(heat, surfaceHeat)
        * (0.32 + daylight * 0.20 + heatDistance * 0.22)
        * (1.0 - max(wetness, fogLike) * 0.44)
        * (1.0 - materialSkin * 0.58));
    float snowColdAirIdentity = saturate(
        weatherLaneSafety
        * max(cold, max(materialSnowIce, frame.SafetySnowProtect * 0.62))
        * (0.38 + openSkyLight * daylight * 0.22 + materialSkyFog * 0.16 + farAir * 0.18)
        * (1.0 - brightMask * frame.SafetySnowProtect * 0.20));
    float humidCanopyAirIdentity = saturate(
        weatherLaneSafety
        * max(foliage, materialFoliage)
        * (0.34 + wetness * 0.24 + haze * 0.14 + dayAtmosphere * 0.14 + nightAtmosphere * night * 0.08)
        * (1.0 - shadowMask * 0.18)
        * (1.0 - foliageNoiseProtect * 0.42));
    float aetherUmbralAirIdentity = saturate(
        weatherLaneSafety
        * max(max(materialAetherNeon, max(magicGlow, neonGlow) * 0.58), cosmicMood * 0.50)
        * (0.34 + nightAtmosphere * 0.22 + atmosphere * 0.18 + materialSkyFog * 0.10 + cosmicMood * 0.16)
        * (1.0 - materialWaterGate * 0.42)
        * (1.0 - materialSkin * 0.62));
    float cloudOvercastAirIdentity = saturate(
        weatherLaneSafety
        * max(materialSkyFog, haze * 0.72)
        * max(max(dayAtmosphere, nightAtmosphere), atmosphere * 0.26)
        * (0.38 + skyAir * 0.24 + farAir * 0.16)
        * (1.0 - max(dustHeatAirIdentity, stormAirIdentity) * 0.34));
    float transitionAirIdentity = saturate(
        weatherLaneSafety
        * (1.0 - max(daylight, night) * 0.72)
        * max(atmosphere, max(moonlight, openSkyLight) * 0.44)
        * (0.30 + cinematic * 0.16 + materialSkyFog * 0.12 + farAir * 0.14)
        * (1.0 - combat * 0.35));
    float industrialAirIdentity = saturate(
        weatherLaneSafety
        * max(max(industrialHardness, materialMetalIndustrial), materialNeonGlass * 0.62)
        * (0.34 + max(dayAtmosphere, nightAtmosphere) * 0.12 + atmosphere * 0.10)
        * (1.0 - materialWaterGate * 0.42)
        * (1.0 - materialSkin * 0.62));
    float stoneInteriorAirIdentity = saturate(
        weatherLaneSafety
        * max(max(materialStoneRuins, frame.MaterialSurfaceHardness * 0.35), materialMetalIndustrial * 0.36)
        * max(max(ambientDarkness, artificialLight * 0.48), atmosphere * 0.22)
        * (0.34 + night * 0.12 + haze * 0.10)
        * (1.0 - materialSkyFog * 0.66)
        * (1.0 - materialSkin * 0.68));
    float gloomAirIdentity = saturate(
        weatherLaneSafety
        * max(max(manualMood, materialVoidDarkness * 0.76), max(ambientDarkness * night, materialSkyFog * nightAtmosphere) * 0.76)
        * (0.36 + night * 0.22 + atmosphere * 0.12)
        * (1.0 - fogMistAirIdentity * 0.22));
    float standaloneWeatherIdentity = saturate(max(
        max(max(max(max(clearCoastalAirIdentity, clearOpenAirIdentity), coastalNightAirIdentity), rainWetAirIdentity), max(stormAirIdentity, fogMistAirIdentity)),
        max(
            max(max(max(dustHeatAirIdentity, dustStormAirIdentity), max(heatWaveAirIdentity, snowColdAirIdentity)), max(humidCanopyAirIdentity, aetherUmbralAirIdentity)),
            max(max(cloudOvercastAirIdentity, transitionAirIdentity), max(max(industrialAirIdentity, stoneInteriorAirIdentity), gloomAirIdentity)))));
    float foliageHazeRestraint = 1.0 - max(foliage, materialFoliage) * atmosphere * 0.50;
    float depthHaze = distanceWeight * weatherAmount * foliageHazeRestraint;
    depthHaze += dustAir * 0.035 + snowAir * 0.026 + waterMist * 0.020 + aetherAir * 0.026 + skyFogAir * 0.035 + humidAir * 0.012;
    depthHaze += clearCoastalAirIdentity * farAir * 0.016;
    depthHaze += coastalNightAirIdentity * max(midAir, farAir * 0.72) * 0.060;
    depthHaze += rainWetAirIdentity * midAir * 0.036;
    depthHaze += stormAirIdentity * farAir * 0.036;
    depthHaze += fogMistAirIdentity * farAir * 0.060;
    depthHaze += dustHeatAirIdentity * heatDistance * 0.052;
    depthHaze += dustStormAirIdentity * heatDistance * 0.045;
    depthHaze += heatWaveAirIdentity * heatDistance * 0.030;
    depthHaze += snowColdAirIdentity * farAir * 0.040;
    depthHaze += clearOpenAirIdentity * farAir * 0.012;
    depthHaze += humidCanopyAirIdentity * midAir * 0.022;
    depthHaze += aetherUmbralAirIdentity * farAir * 0.032;
    depthHaze += cloudOvercastAirIdentity * farAir * 0.038;
    depthHaze += transitionAirIdentity * farAir * 0.022;
    depthHaze += industrialAirIdentity * midAir * 0.016;
    depthHaze += stoneInteriorAirIdentity * midAir * 0.018;
    depthHaze += gloomAirIdentity * farAir * 0.020;
    depthHaze *= (0.15 + atmosphere * 0.16 + fogLike * 0.07 + dustSoftness * 0.08 + nightAtmosphere * 0.035 + dayAtmosphere * 0.030) * gameplayDampen * cinematicBoost * lerp(1.0, 1.20, standaloneAtmosphere);
    depthHaze *= 1.0 - ambientDarkness * night * (0.22 + max(foliage, materialFoliage) * 0.20);
    depthHaze *= 1.0 - Dalashade_Saturate(skinAtmosphereProtect * 0.45 + highlightAtmosphereProtect * 0.20 + foliageNoiseProtect * 0.10 + normalEdgeSafety * 0.12);
    depthHaze = min(depthHaze, lerp(0.22, 0.32, saturate(fogLike + max(heat, materialSandDust) * 0.45 + materialSkyFog * haze * 0.30)) * lerp(1.0, 1.16, standaloneWeatherIdentity));

    float3 hazeTint = float3(0.63, 0.68, 0.72);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.78, 0.87, 1.00), max(cold, materialSnowIce) * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(1.00, 0.76, 0.50), max(max(heat, materialSandDust), surfaceHeat) * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.75, 0.86, 1.00), openSkyLight * daylight * 0.10);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.70, 0.82, 0.68), humidAir * 0.80);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.62, 0.70, 0.92), materialCrystal * magicGlow * 0.32);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.58, 0.74, 0.92), materialNeonGlass * neonGlow * 0.16);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.50, 0.60, 0.82), moonlight * night * 0.22);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.54, 0.57, 0.66), manualMood * 0.40);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.66, 0.86, 1.00), clearCoastalAirIdentity * 0.28);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.72, 0.86, 1.00), clearOpenAirIdentity * 0.18);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.62, 0.74, 0.82), rainWetAirIdentity * 0.34);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.45, 0.52, 0.64), stormAirIdentity * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.70, 0.74, 0.78), fogMistAirIdentity * 0.36);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(1.00, 0.72, 0.42), dustHeatAirIdentity * 0.46);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.98, 0.68, 0.40), dustStormAirIdentity * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(1.00, 0.76, 0.54), heatWaveAirIdentity * 0.32);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.82, 0.91, 1.00), snowColdAirIdentity * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.62, 0.84, 0.58), humidCanopyAirIdentity * 0.34);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.58, 0.62, 0.98), aetherUmbralAirIdentity * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.34, 0.36, 0.46), gloomAirIdentity * 0.36);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.48, 0.62, 0.72), coastalNightAirIdentity * 0.46);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.62, 0.67, 0.72), cloudOvercastAirIdentity * 0.34);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.92, 0.76, 0.58), transitionAirIdentity * 0.26);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.50, 0.58, 0.68), industrialAirIdentity * 0.34);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.60, 0.56, 0.50), stoneInteriorAirIdentity * 0.30);

    float weatherBlendStrength = min(manualStrength + standaloneWeatherIdentity * 0.24 + coastalNightAirIdentity * 0.08 + fogMistAirIdentity * 0.06 + stormAirIdentity * 0.05 + cloudOvercastAirIdentity * 0.04 + stoneInteriorAirIdentity * 0.03 + max(dustStormAirIdentity, heatWaveAirIdentity) * 0.03, 0.70);
    float3 result = Dalashade_SafeLerp(color, hazeTint, depthHaze * weatherBlendStrength);

    // Wet air scattering: rain/wetness softens bright wet highlights without lifting the entire scene.
    float rainGlow = max(wetness * 0.54, specularMask * max(wetness, max(waterMist, materialSpecularGlint * 0.35)) * 0.78);
    float nightLocalGlow = artificialLight * smoothstep(0.48, 0.96, luma) * (0.35 + max(magicGlow, neonGlow) * 0.45 + materialCrystal * 0.20);
    float moonAirGlow = moonlight * nightAtmosphere * smoothstep(0.30, 0.88, luma) * distant * 0.22;
    float glowIntent = max(max(rainGlow, haze * 0.32), max(magicGlow * 0.40, neonGlow * 0.35));
    glowIntent = max(glowIntent, materialCrystal * magicGlow * 0.34);
    glowIntent = max(glowIntent, nightLocalGlow);
    glowIntent = max(glowIntent, moonAirGlow);
    glowIntent = max(glowIntent, manualGlow * 0.45);
    glowIntent = max(glowIntent, rainWetAirIdentity * specularMask * 0.32);
    glowIntent = max(glowIntent, coastalNightAirIdentity * max(specularMask, materialSpecularGlint * 0.55) * 0.22);
    glowIntent = max(glowIntent, aetherUmbralAirIdentity * max(materialAetherNeon, brightMask) * 0.24);
    glowIntent = max(glowIntent, industrialAirIdentity * max(materialNeonGlass, materialSpecularGlint) * 0.16);
    glowIntent *= gameplayDampen;
    float glowAmount = min((brightMask * 0.70 + specularMask * 0.55) * glowIntent * (0.085 + atmosphere * 0.075) * lerp(1.0, 1.18, max(rainWetAirIdentity, aetherUmbralAirIdentity)), 0.18 * lerp(1.0, 1.14, standaloneWeatherIdentity));
    float3 glowTint = Dalashade_SafeLerp(float3(1.0, 1.0, 1.0), float3(0.72, 0.90, 1.0), max(neonGlow, cold) * 0.35);
    glowTint = Dalashade_SafeLerp(glowTint, float3(1.0, 0.82, 0.55), heat * 0.30);
    glowTint = Dalashade_SafeLerp(glowTint, float3(0.70, 0.82, 1.0), rainWetAirIdentity * 0.24);
    glowTint = Dalashade_SafeLerp(glowTint, float3(0.74, 0.88, 1.0), coastalNightAirIdentity * 0.22);
    glowTint = Dalashade_SafeLerp(glowTint, float3(0.64, 0.58, 1.0), aetherUmbralAirIdentity * 0.36);
    glowTint = Dalashade_SafeLerp(glowTint, float3(0.68, 0.82, 1.0), industrialAirIdentity * 0.16);
    result = Dalashade_SoftLighten(result, glowTint, glowAmount * weatherBlendStrength);

    // Dense rainforest canopies get local green-gold sky light on bright openings, while haze and shadow lift are restrained.
    float canopyOpenings = smoothstep(0.50, 0.90, luma) * (1.0 - shadowMask * 0.70);
    float canopyLight = max(foliage, materialFoliage) * atmosphere * gameplayDampen * canopyOpenings;
    canopyLight *= (0.032 + max(magicGlow, cinematic) * 0.016 + moonlight * night * 0.012 + humidCanopyAirIdentity * 0.018) * lerp(1.0, 1.18, humidCanopyAirIdentity);
    float3 canopyTint = float3(0.60, 0.86, 0.48);
    result = Dalashade_SoftLighten(result, canopyTint, min(canopyLight * weatherBlendStrength, 0.070));

    // Gloom/storm mood darkens and cools the scene; it is not fog, so it preserves black depth.
    float stormMood = Dalashade_Saturate(manualMood + wetness * max(haze, atmosphere) * 0.82 + stormAirIdentity * 0.48 + gloomAirIdentity * 0.42 + cloudOvercastAirIdentity * 0.16 + materialVoidDarkness * 0.20);
    float nightDarken = ambientDarkness * night * shadowMask * (0.030 + max(foliage, materialFoliage) * 0.020 + materialSkyFog * 0.010) * (1.0 - readability * 0.30);
    result *= 1.0 - nightDarken;

    float moodDarken = stormMood * (0.045 + haze * 0.035 + nightAtmosphere * 0.014 + gloomAirIdentity * 0.018) * (1.0 - combat * 0.52) * lerp(1.0, 1.18, max(stormAirIdentity, gloomAirIdentity));
    result *= 1.0 - moodDarken;
    result = Dalashade_SafeLerp(result, result * float3(0.88, 0.92, 1.0), stormMood * 0.12 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(0.82, 0.84, 0.94), gloomAirIdentity * shadowMask * 0.16 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(0.90, 0.965, 1.060), coastalNightAirIdentity * max(midAir, materialWaterGate * 0.34) * 0.12 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(0.94, 0.97, 1.03), cloudOvercastAirIdentity * farAir * 0.10 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(0.92, 0.96, 1.06), industrialAirIdentity * shadowMask * 0.08 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(1.02, 0.98, 0.92), stoneInteriorAirIdentity * max(shadowMask, artificialLight * 0.35) * 0.08 * weatherBlendStrength);

    // Highlight shoulder: bright sand, clouds, snow, and specular water roll off before bloom/grade can clip them.
    float snowHighlightGuard = max(cold, materialSnowIce) * max(highlightProtection, brightMask);
    float coastalGlareGuard = highlightProtection * brightMask * (1.0 - wetness * 0.25) * (0.035 + atmosphere * 0.025 + materialWaterGate * 0.010);
    float highlightRollOff = highlightProtection * brightMask * (0.09 + max(cold, materialSnowIce) * 0.11 + max(heat, materialSandDust) * 0.055);
    highlightRollOff = max(highlightRollOff, snowHighlightGuard * specularMask * 0.10);
    highlightRollOff = max(highlightRollOff, materialSnowIce * brightMask * 0.055);
    highlightRollOff = max(highlightRollOff, coastalGlareGuard);
    highlightRollOff = max(highlightRollOff, rainWetAirIdentity * specularMask * 0.035);
    highlightRollOff = max(highlightRollOff, coastalNightAirIdentity * specularMask * 0.045);
    highlightRollOff = max(highlightRollOff, dustHeatAirIdentity * brightMask * 0.055);
    highlightRollOff = max(highlightRollOff, dustStormAirIdentity * brightMask * 0.052);
    highlightRollOff = max(highlightRollOff, heatWaveAirIdentity * brightMask * 0.036);
    highlightRollOff = max(highlightRollOff, snowColdAirIdentity * brightMask * 0.060);
    highlightRollOff = max(highlightRollOff, cloudOvercastAirIdentity * brightMask * 0.026);
    highlightRollOff = max(highlightRollOff, industrialAirIdentity * specularMask * 0.030);
    result = lerp(result, result / (1.0 + result), min(highlightRollOff * weatherBlendStrength, 0.30));

    // Shadow lift stays selective; foliage-heavy and gloomy scenes keep trunks/background dark instead of milky.
    float shadowLift = shadowProtection * shadowMask * (0.032 - night * 0.010) * (1.0 - combat * 0.35) * (1.0 - max(foliage, materialFoliage) * 0.46) * (1.0 - ambientDarkness * 0.36);
    shadowLift *= 1.0 - saturate(stormAirIdentity * 0.24 + gloomAirIdentity * 0.38 + humidCanopyAirIdentity * 0.18 + coastalNightAirIdentity * 0.16 + stoneInteriorAirIdentity * 0.22 + industrialAirIdentity * 0.12);
    result += shadowLift * weatherBlendStrength;

    // Heat/dust softness is distance-weighted so night desert scenes get air thickness, not a full-screen lift.
    float heatShimmerSoftness = max(heat, materialSandDust) * heatDistance * heatDistance * (0.050 + haze * 0.018 + dustHeatAirIdentity * 0.026) * gameplayDampen;
    float warmLuma = dot(result, float3(0.26, 0.67, 0.07));
    result = Dalashade_SafeLerp(result, float3(warmLuma, warmLuma, warmLuma), heatShimmerSoftness * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(1.040, 0.980, 0.870), dustHeatAirIdentity * heatDistance * 0.11 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(1.050, 0.955, 0.830), dustStormAirIdentity * heatDistance * 0.10 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(1.030, 0.985, 0.915), heatWaveAirIdentity * heatDistance * 0.08 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, max(result, color * float3(0.94, 0.985, 1.055)), snowColdAirIdentity * brightMask * 0.14 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(0.92, 0.92, 1.08), aetherUmbralAirIdentity * farAir * 0.11 * weatherBlendStrength);
    result = Dalashade_SafeLerp(result, result * float3(0.94, 0.98, 1.04), clearOpenAirIdentity * farAir * 0.05 * weatherBlendStrength);

    // Final guardrails keep the shader visible but prevent large grade swings.
    result = min(result, color + 0.24 * lerp(1.0, 1.20, standaloneWeatherIdentity));
    result = max(result, color - 0.20 * lerp(1.0, 1.14, standaloneWeatherIdentity));
    result = saturate(result);

    if (Dalashade_ShowDebugMask)
    {
        float foliageDampen = saturate(foliage * atmosphere * 0.85);
        float heatDust = saturate(heatShimmerSoftness * 8.0 + max(heat, materialSandDust) * heatDistance * 0.35);
        float materialDebugStrength = Dalashade_Saturate(Dalashade_MaterialDebugStrength);
        float materialAir = saturate(
            humidAir + dustAir + snowAir + waterMist + aetherAir + skyFogAir + nightAtmosphere * night * 0.20
            + standaloneWeatherIdentity * 0.42 + industrialAirIdentity * 0.24 + stoneInteriorAirIdentity * 0.22);
        if (Dalashade_MaterialDebugMode == 1)
        {
            return float4(
                saturate(dustAir + waterMist + rainWetAirIdentity + coastalNightAirIdentity * 0.65 + stormAirIdentity * 0.35 + dustStormAirIdentity * 0.45 + heatWaveAirIdentity * 0.28) * materialDebugStrength,
                saturate(humidAir + aetherAir + humidCanopyAirIdentity + clearCoastalAirIdentity * 0.30 + clearOpenAirIdentity * 0.26 + industrialAirIdentity * 0.20 + stoneInteriorAirIdentity * 0.16) * materialDebugStrength,
                saturate(snowAir + skyFogAir + fogMistAirIdentity + snowColdAirIdentity + aetherUmbralAirIdentity * 0.35 + cloudOvercastAirIdentity * 0.28 + transitionAirIdentity * 0.22) * materialDebugStrength,
                1.0);
        }
        if (Dalashade_MaterialDebugMode == 2)
        {
            return float4(frame.MaterialFoliage * materialDebugStrength, frame.SafetyFoliageNoiseReject * materialDebugStrength, humidAir * 5.0 * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 3)
        {
            return float4(frame.MaterialSandDust * materialDebugStrength, dustAir * 4.0 * materialDebugStrength, heatDistance * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 4)
        {
            return float4(frame.MaterialSnowIce * materialDebugStrength, snowAir * 5.0 * materialDebugStrength, highlightRollOff * 5.0 * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 5)
        {
            return float4(frame.WaterWetShoreline * materialDebugStrength, waterMist * 5.0 * materialDebugStrength, frame.WaterSpecularGlint * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 6)
        {
            return float4(frame.MaterialCrystalAether * materialDebugStrength, aetherAir * 5.0 * materialDebugStrength, magicGlow * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 7)
        {
            return float4(frame.MaterialSkyCloudFog * materialDebugStrength, skyFogAir * 5.0 * materialDebugStrength, realFogWeather * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 8)
        {
            return float4(materialAir * materialDebugStrength, saturate(depthHaze * 4.0) * materialDebugStrength, saturate(highlightRollOff * 5.0) * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 9)
        {
            return float4(frame.WaterReceiver * materialDebugStrength, waterMist * 5.0 * materialDebugStrength, materialWaterGate * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 10)
        {
            return float4(frame.WaterSpecularGlint * materialDebugStrength, specularMask * materialDebugStrength, rainGlow * materialDebugStrength, 1.0);
        }

        if (Dalashade_DebugView == 1)
        {
            return float4(saturate(depthHaze * 4.0), saturate(distanceWeight), saturate(haze), 1.0);
        }
        if (Dalashade_DebugView == 2)
        {
            return float4(saturate(highlightRollOff * 5.0), saturate(brightMask), saturate(highlightProtection), 1.0);
        }
        if (Dalashade_DebugView == 3)
        {
            return float4(saturate(rainGlow), saturate(glowAmount * 5.0 + canopyLight * 8.0), saturate(max(magicGlow, neonGlow)), 1.0);
        }
        if (Dalashade_DebugView == 4)
        {
            return float4(saturate(foliageHazeRestraint), saturate(canopyLight * 10.0), foliageDampen, 1.0);
        }
        if (Dalashade_DebugView == 5)
        {
            return float4(heatDust, saturate(heatDistance), saturate(heat), 1.0);
        }

        // Composite red: depth haze, green: weather/canopy light, blue: protection/readability pressure.
        float debugProtection = saturate(max(highlightRollOff, shadowLift * 3.0) + combat * 0.18);
        return float4(saturate(depthHaze * 3.2 + nightDarken * 4.0), saturate(glowAmount * 5.0 + canopyLight * 8.0 + artificialLight * 0.20), saturate(debugProtection + moonlight * 0.20 + nightAtmosphere * 0.16), 1.0);
    }

    return float4(result, 1.0);
}

technique Dalashade_WeatherAtmosphere
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_WeatherAtmospherePS;
    }
}
