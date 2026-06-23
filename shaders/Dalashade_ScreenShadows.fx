#include "ReShade.fxh"
#include "Dalashade_FrameData.fxh"

// Dalashade ScreenShadows is a first-pass source-aware screen-space shadow impression.
// It cannot see off-screen lights or true world-space light positions.

uniform float Dalashade_ScreenShadowsEnabled <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ScreenShadows Enabled";
> = 0.0;

uniform float Dalashade_ScreenShadowsStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ScreenShadows Strength";
> = 0.28;

uniform float Dalashade_ScreenShadowsReach <
    ui_type = "slider";
    ui_min = 0.20; ui_max = 2.0;
    ui_label = "ScreenShadows Reach";
> = 0.62;

uniform float Dalashade_ScreenShadowsSoftness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ScreenShadows Softness";
> = 0.55;

uniform float Dalashade_ScreenShadowsSourceSensitivity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ScreenShadows Source Sensitivity";
> = 0.52;

uniform float Dalashade_ScreenShadowsDalapadInfluence <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ScreenShadows Dalapad Assist";
> = 0.45;

uniform int Dalashade_ScreenShadowsDebugMode <
    ui_type = "combo";
    ui_items = "Off\0Shadow mask\0Light source mask\0Occluder mask\0Receiver safety\0Dalapad evidence\0Final contribution\0";
    ui_label = "ScreenShadows Debug Mode";
> = 0;

uniform int Dalashade_ScreenShadowsDebugOutputMode <
    ui_type = "combo";
    ui_items = "Full replacement\0Alpha overlay over original\0Side-by-side split\0Contribution over black\0Amplified difference\0";
    ui_label = "ScreenShadows Debug Output";
> = 0;

uniform float Dalashade_ScreenShadowsDebugOpacity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ScreenShadows Debug Opacity";
> = 0.75;

uniform float Dalashade_ScreenShadowsDebugBoost <
    ui_type = "slider";
    ui_min = 0.25; ui_max = 8.0;
    ui_label = "ScreenShadows Debug Boost";
> = 2.50;

uniform float Dalashade_StandaloneStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Standalone Stack Strength"; > = 0.0;
uniform int Dalashade_FirstPartyPerformanceTier < ui_type = "combo"; ui_items = "Quality\0Balanced\0Performance\0"; ui_label = "First-Party Performance Tier"; > = 0;

uniform float Dalashade_IntentReadability < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Readability"; > = 0.0;
uniform float Dalashade_IntentAtmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Atmosphere"; > = 0.0;
uniform float Dalashade_IntentHighlightProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Highlight Protection"; > = 0.0;
uniform float Dalashade_IntentShadowProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Shadow Protection"; > = 0.0;
uniform float Dalashade_IntentWetness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Wetness"; > = 0.0;
uniform float Dalashade_IntentMagicGlow < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Magic Glow"; > = 0.0;
uniform float Dalashade_IntentNeonGlow < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Neon Glow"; > = 0.0;
uniform float Dalashade_IntentFoliageDensity < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Foliage Density"; > = 0.0;
uniform float Dalashade_IntentIndustrialHardness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Industrial Hardness"; > = 0.0;
uniform float Dalashade_IntentCombatPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Combat Pressure"; > = 0.0;
uniform float Dalashade_IntentCinematicPermission < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Cinematic Permission"; > = 0.0;
uniform float Dalashade_Night < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Night"; > = 0.0;
uniform float Dalashade_Moonlight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Moonlight"; > = 0.0;
uniform float Dalashade_ArtificialLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Artificial Light"; > = 0.0;
uniform float Dalashade_AmbientDarkness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Ambient Darkness"; > = 0.0;
uniform float Dalashade_NightAtmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Night Atmosphere"; > = 0.0;
uniform float Dalashade_Daylight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Daylight"; > = 0.0;
uniform float Dalashade_Sunlight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Sunlight"; > = 0.0;
uniform float Dalashade_OpenSkyLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Open Sky Light"; > = 0.0;
uniform float Dalashade_DayHighlightPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Day Highlight Pressure"; > = 0.0;

uniform float Dalashade_MaterialFoliage < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Foliage"; > = 0.0;
uniform float Dalashade_MaterialWaterPlane < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Water Plane"; > = 0.0;
uniform float Dalashade_MaterialSpecularGlint < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Specular Glint"; > = 0.0;
uniform float Dalashade_MaterialSandDust < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sand Dust"; > = 0.0;
uniform float Dalashade_MaterialSnowIce < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Snow Ice"; > = 0.0;
uniform float Dalashade_MaterialStoneRuins < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Stone Ruins"; > = 0.0;
uniform float Dalashade_MaterialMetalIndustrial < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Metal Industrial"; > = 0.0;
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

uniform bool Dalashade_EnableDepthAssist < ui_label = "Enable Depth Assist"; > = false;
uniform float Dalashade_DepthAssistStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Strength"; > = 0.0;
uniform float Dalashade_DepthAssistConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Assist Confidence Floor"; > = 0.0;
uniform float Dalashade_DepthConfidenceFloor < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Depth Confidence Floor"; > = 0.0;

uniform float Dalashade_NormalFieldEnabled < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Enabled"; > = 0.0;
uniform float Dalashade_NormalFieldStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Strength"; > = 0.0;
uniform float Dalashade_NormalDepthStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Depth Strength"; > = 0.0;
uniform float Dalashade_NormalDetailStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Detail Strength"; > = 0.0;
uniform float Dalashade_NormalMaterialInfluence < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Material Influence"; > = 0.0;
uniform float Dalashade_NormalWaterSuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Water Suppression"; > = 0.80;
uniform float Dalashade_NormalSkinSuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Skin Suppression"; > = 0.90;
uniform float Dalashade_NormalSkySuppression < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "NormalField Sky/Fog Suppression"; > = 0.95;

uniform float Dalashade_DalapadSceneGINormalAssist < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalapad SceneGI Normal Assist"; > = 0.0;
uniform float Dalashade_DalapadSceneGINormalStrength < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalapad SceneGI Normal Strength"; > = 0.0;

float Dalashade_ScreenShadowsLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float Dalashade_ScreenShadowsSourceCandidate(float3 sampleColor, float materialBias, float sensitivity)
{
    float luma = Dalashade_ScreenShadowsLuma(sampleColor);
    float chroma = max(max(sampleColor.r, sampleColor.g), sampleColor.b) - min(min(sampleColor.r, sampleColor.g), sampleColor.b);
    float bright = smoothstep(lerp(0.64, 0.38, saturate(sensitivity)), 1.0, luma);
    float saturated = smoothstep(0.08, 0.42, chroma) * smoothstep(0.18, 0.86, luma);
    float warm = smoothstep(0.05, 0.40, sampleColor.r - max(sampleColor.g, sampleColor.b) * 0.56);
    float cool = smoothstep(0.05, 0.42, max(sampleColor.b - sampleColor.r * 0.45, sampleColor.g - sampleColor.r * 0.40));
    return saturate(max(bright, max(saturated, max(warm, cool)) * (0.45 + materialBias * 0.55)));
}

float4 Dalashade_ScreenShadowsTap(float2 uv, float depth, float2 dir, float reach, float softness, float sourceBias)
{
    float2 texel = BUFFER_PIXEL_SIZE * reach;
    float2 occluderUv = saturate(uv - dir * texel * 0.90);
    float2 sourceUv = saturate(uv + dir * texel * 2.20);
    float occluderDepth = ReShade::GetLinearizedDepth(occluderUv);
    float sourceDepth = ReShade::GetLinearizedDepth(sourceUv);
    float3 sourceColor = tex2D(ReShade::BackBuffer, sourceUv).rgb;
    float bias = 0.0008 + depth * 0.0025;
    float edgeWidth = lerp(0.014, 0.050, saturate(softness));
    float occluder = smoothstep(bias, bias + edgeWidth, depth - occluderDepth);
    float source = Dalashade_ScreenShadowsSourceCandidate(sourceColor, sourceBias, Dalashade_ScreenShadowsSourceSensitivity);
    float continuity = saturate(1.0 - abs(sourceDepth - depth) * lerp(8.0, 3.8, saturate(softness)));
    return float4(saturate(occluder * source * continuity), source, occluder, 1.0);
}

float3 Dalashade_ScreenShadowsDebugOutput(float2 texcoord, float3 originalColor, float3 resultColor, float3 debugColor, float debugMask)
{
    float opacity = saturate(Dalashade_ScreenShadowsDebugOpacity);
    int outputMode = Dalashade_ScreenShadowsDebugOutputMode;
    float boost = max(Dalashade_ScreenShadowsDebugBoost, 0.001);
    float3 cleanDebug = saturate(float3(1.0, 1.0, 1.0) - exp(-max(debugColor, float3(0.0, 0.0, 0.0)) * boost));

    if (outputMode == 1)
    {
        return lerp(originalColor, cleanDebug, saturate(debugMask * opacity));
    }

    if (outputMode == 2)
    {
        float split = step(texcoord.x, 0.5);
        return lerp(originalColor, cleanDebug, split);
    }

    if (outputMode == 3)
    {
        return cleanDebug;
    }

    if (outputMode == 4)
    {
        float3 amplified = abs(resultColor - originalColor) * 18.0 + cleanDebug * 0.30;
        return saturate(float3(1.0, 1.0, 1.0) - exp(-amplified * boost));
    }

    return cleanDebug;
}

float4 Dalashade_ScreenShadowsPS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    if (Dalashade_ScreenShadowsEnabled <= 0.0 || Dalashade_ScreenShadowsStrength <= 0.0)
    {
        return float4(color, 1.0);
    }

    float depth = ReShade::GetLinearizedDepth(texcoord);
    float luma = Dalashade_ScreenShadowsLuma(color);
    float bright = smoothstep(0.66, 0.98, luma);
    float shadow = 1.0 - smoothstep(0.08, 0.44, luma);

    Dalashade_FrameSceneSettings sceneSettings = Dalashade_FrameScene_DefaultSettings();
    sceneSettings.Readability = Dalashade_IntentReadability;
    sceneSettings.Atmosphere = Dalashade_IntentAtmosphere;
    sceneSettings.HighlightProtection = Dalashade_IntentHighlightProtection;
    sceneSettings.ShadowProtection = Dalashade_IntentShadowProtection;
    sceneSettings.Wetness = Dalashade_IntentWetness;
    sceneSettings.MagicGlow = Dalashade_IntentMagicGlow;
    sceneSettings.NeonGlow = Dalashade_IntentNeonGlow;
    sceneSettings.FoliageDensity = Dalashade_IntentFoliageDensity;
    sceneSettings.IndustrialHardness = Dalashade_IntentIndustrialHardness;
    sceneSettings.CinematicPermission = Dalashade_IntentCinematicPermission;
    sceneSettings.CombatPressure = Dalashade_IntentCombatPressure;
    sceneSettings.Night = Dalashade_Night;
    sceneSettings.Moonlight = Dalashade_Moonlight;
    sceneSettings.ArtificialLight = Dalashade_ArtificialLight;
    sceneSettings.AmbientDarkness = Dalashade_AmbientDarkness;
    sceneSettings.NightAtmosphere = Dalashade_NightAtmosphere;
    sceneSettings.Daylight = Dalashade_Daylight;
    sceneSettings.Sunlight = Dalashade_Sunlight;
    sceneSettings.OpenSkyLight = Dalashade_OpenSkyLight;
    sceneSettings.DayHighlightPressure = Dalashade_DayHighlightPressure;
    sceneSettings.StandaloneStrength = Dalashade_StandaloneStrength;
    Dalashade_FrameSceneData scene = Dalashade_ResolveFrameSceneData(sceneSettings);

    Dalashade_FrameDataSettings frameSettings = Dalashade_FrameData_DefaultSettings();
    frameSettings.MaterialFoliage = Dalashade_MaterialFoliage;
    frameSettings.MaterialWaterPlane = Dalashade_MaterialWaterPlane;
    frameSettings.MaterialSpecularGlint = Dalashade_MaterialSpecularGlint;
    frameSettings.MaterialSandDust = Dalashade_MaterialSandDust;
    frameSettings.MaterialSnowIce = Dalashade_MaterialSnowIce;
    frameSettings.MaterialStoneRuins = Dalashade_MaterialStoneRuins;
    frameSettings.MaterialMetalIndustrial = Dalashade_MaterialMetalIndustrial;
    frameSettings.MaterialCrystalAether = Dalashade_MaterialCrystalAether;
    frameSettings.MaterialNeonGlass = Dalashade_MaterialNeonGlass;
    frameSettings.MaterialFireLavaHeat = Dalashade_MaterialFireLavaHeat;
    frameSettings.MaterialSkyCloudFog = Dalashade_MaterialSkyCloudFog;
    frameSettings.MaterialSkinProtection = Dalashade_MaterialSkinProtection;
    frameSettings.MaterialVoidDarkness = Dalashade_MaterialVoidDarkness;
    frameSettings.WaterContext = Dalashade_WaterContext;
    frameSettings.CoastalContext = Dalashade_CoastalContext;
    frameSettings.OpenOceanContext = Dalashade_OpenOceanContext;
    frameSettings.ShallowWaterContext = Dalashade_ShallowWaterContext;
    frameSettings.WetSurfaceContext = max(Dalashade_IntentWetness, Dalashade_WetSurfaceContext);
    frameSettings.HighlightProtection = Dalashade_IntentHighlightProtection;
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
    frameSettings.DalapadSurfaceDataEnabled = Dalashade_DalapadSurfaceDataEnabled;
    frameSettings.DalapadSurfaceDataStrength = Dalashade_DalapadSurfaceDataStrength;

    Dalashade_FrameBaseData frame = Dalashade_ResolveFrameBaseData(color, texcoord, frameSettings);
    Dalashade_FrameSurfaceData surface = Dalashade_ResolveFrameSurfaceData(color, texcoord, frame, frameSettings);
    Dalashade_DalapadScalarResult emissiveEvidence = Dalashade_DalapadPinnedEmissiveEvidence(texcoord, frameSettings.DalapadSurfaceDataEnabled, Dalashade_ScreenShadowsDalapadInfluence);
    Dalashade_DalapadScalarResult albedoEvidence = Dalashade_DalapadPinnedAlbedoEvidence(texcoord, frameSettings.DalapadSurfaceDataEnabled, Dalashade_ScreenShadowsDalapadInfluence);
    float dalapadEvidence = saturate(max(emissiveEvidence.Confidence, albedoEvidence.Confidence * 0.45) * Dalashade_ScreenShadowsDalapadInfluence);

    float skyReject = saturate(frame.SafetySkyReject + frame.MaterialSkyCloudFog * 0.76);
    float skinProtect = saturate(frame.SafetySkinReject + frame.MaterialSkinProtection * 0.62);
    float sourceBias = saturate(
        scene.NightLocalLight * 0.38
        + scene.AetherTech * 0.18
        + frame.SourceLightConfidence * 0.34
        + frame.MaterialFireLavaHeat * 0.35
        + frame.MaterialCrystalAether * 0.24
        + frame.MaterialNeonGlass * 0.24
        + frame.WaterSpecularGlint * 0.12
        + dalapadEvidence * 0.20);

    float receiverSafety = saturate(
        (1.0 - skyReject * 0.94)
        * (1.0 - skinProtect * 0.88)
        * (1.0 - frame.SafetyHighlightProtect * 0.54)
        * (1.0 - frame.SafetyWaterAOReject * 0.58)
        * (1.0 - frame.MaterialVoidDarkness * 0.64)
        * (1.0 - bright * 0.30));
    receiverSafety *= saturate(0.44 + frame.ReceiverAO * 0.28 + frame.ReceiverStructure * 0.22 + surface.AOReceiverSupport * 0.14 + surface.StructureCandidate * 0.10 + shadow * 0.12);

    float reach = max(Dalashade_ScreenShadowsReach, 0.20);
    float softness = saturate(Dalashade_ScreenShadowsSoftness);
    float4 gathered = float4(0.0, 0.0, 0.0, 0.0001);
    gathered += Dalashade_ScreenShadowsTap(texcoord, depth, normalize(float2(1.0, 0.0)), reach, softness, sourceBias);
    gathered += Dalashade_ScreenShadowsTap(texcoord, depth, normalize(float2(-1.0, 0.0)), reach, softness, sourceBias);
    gathered += Dalashade_ScreenShadowsTap(texcoord, depth, normalize(float2(0.0, 1.0)), reach, softness, sourceBias);
    gathered += Dalashade_ScreenShadowsTap(texcoord, depth, normalize(float2(0.0, -1.0)), reach, softness, sourceBias);

    if (Dalashade_FirstPartyPerformanceTier < 2)
    {
        gathered += Dalashade_ScreenShadowsTap(texcoord, depth, normalize(float2(0.74, 0.68)), reach * 1.35, softness, sourceBias) * 0.72;
        gathered += Dalashade_ScreenShadowsTap(texcoord, depth, normalize(float2(-0.74, 0.68)), reach * 1.35, softness, sourceBias) * 0.72;
    }

    float shadowMask = saturate(gathered.r / max(gathered.a, 0.0001));
    float sourceMask = saturate(gathered.g / max(gathered.a, 0.0001));
    float occluderMask = saturate(gathered.b / max(gathered.a, 0.0001));
    float sceneAllowance = saturate(0.52 + scene.NightLocalLight * 0.30 + scene.DayOpenAir * 0.10 + scene.CinematicPermission * 0.16 - scene.CombatPressure * 0.18 - scene.Readability * 0.10);
    shadowMask = saturate((shadowMask + dalapadEvidence * 0.06) * receiverSafety * sceneAllowance);
    float contribution = shadowMask * Dalashade_ScreenShadowsStrength * (0.10 + Dalashade_StandaloneStrength * 0.025);
    float3 result = saturate(color * (1.0 - contribution));

    int mode = Dalashade_ScreenShadowsDebugMode;
    if (mode > 0)
    {
        float3 debugColor = float3(0.0, 0.0, 0.0);
        float debugMask = shadowMask;
        if (mode == 1)
        {
            debugColor = shadowMask.xxx;
        }
        else if (mode == 2)
        {
            debugColor = float3(sourceMask, sourceMask * 0.46, sourceMask * 0.12);
            debugMask = sourceMask;
        }
        else if (mode == 3)
        {
            debugColor = float3(occluderMask * 0.24, occluderMask * 0.66, occluderMask);
            debugMask = occluderMask;
        }
        else if (mode == 4)
        {
            debugColor = float3(receiverSafety, frame.ReceiverAO, surface.AOReceiverSupport);
            debugMask = receiverSafety;
        }
        else if (mode == 5)
        {
            debugColor = float3(emissiveEvidence.Confidence, albedoEvidence.Confidence * 0.45, dalapadEvidence);
            debugMask = dalapadEvidence;
        }
        else if (mode == 6)
        {
            debugColor = float3(contribution * 8.0, shadowMask, receiverSafety);
            debugMask = saturate(contribution * 8.0 + shadowMask * 0.25);
        }

        return float4(Dalashade_ScreenShadowsDebugOutput(texcoord, color, result, debugColor, debugMask), 1.0);
    }

    return float4(result, 1.0);
}

technique Dalashade_ScreenShadows
{
    pass ScreenShadows
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_ScreenShadowsPS;
    }
}
