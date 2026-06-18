#include "ReShade.fxh"
#include "Dalashade_FrameData.fxh"

// Dalashade ContactTone is a local grounding/readability pass.
// It is not GI, SSAO, relighting, reflection, or colored bounce.

uniform float Dalashade_ContactToneEnabled <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ContactTone Enabled";
> = 1.0;

uniform float Dalashade_ContactToneStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ContactTone Strength";
    ui_tooltip = "Grounding/readability strength. This darkens and separates local contact seams; it does not add GI or colored bounce.";
> = 0.42;

uniform float Dalashade_ContactToneRadius <
    ui_type = "slider";
    ui_min = 0.20; ui_max = 2.0;
    ui_label = "ContactTone Radius";
> = 0.62;

uniform float Dalashade_ContactToneEdgeStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ContactTone Edge Strength";
> = 0.48;

uniform float Dalashade_ContactToneStructureStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ContactTone Structure Strength";
> = 0.44;

uniform float Dalashade_ContactToneContrastStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ContactTone Contrast Strength";
> = 0.34;

uniform float Dalashade_StandaloneStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Standalone Stack Strength";
    ui_tooltip = "0 keeps ContactTone supportive. 1 makes grounding stronger while retaining sky, skin, water, snow, and highlight safety.";
> = 0.0;

uniform int Dalashade_ContactToneDebugMode <
    ui_type = "combo";
    ui_items = "Off\0Contact mask\0Depth edge component\0Surface/normal edge component\0Receiver/safety mask\0Suppression mask\0Final contribution\0";
    ui_label = "ContactTone Debug Mode";
> = 0;

uniform float Dalashade_ContactToneDebugOpacity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "ContactTone Debug Opacity";
> = 0.75;

uniform float Dalashade_Readability < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Readability"; > = 0.0;
uniform float Dalashade_Atmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Atmosphere"; > = 0.0;
uniform float Dalashade_HighlightProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Highlight Protection"; > = 0.0;
uniform float Dalashade_ShadowProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Shadow Protection"; > = 0.0;
uniform float Dalashade_Wetness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Wetness"; > = 0.0;
uniform float Dalashade_FoliageDensity < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Foliage Density"; > = 0.0;
uniform float Dalashade_IndustrialHardness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Industrial Hardness"; > = 0.0;
uniform float Dalashade_CombatPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Combat Pressure"; > = 0.0;
uniform float Dalashade_CinematicPermission < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Intent Cinematic Permission"; > = 0.0;

uniform float Dalashade_MaterialFoliage < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Foliage"; > = 0.0;
uniform float Dalashade_MaterialWaterPlane < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Water Plane"; > = 0.0;
uniform float Dalashade_MaterialSpecularGlint < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Specular Glint"; > = 0.0;
uniform float Dalashade_MaterialSandDust < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sand Dust"; > = 0.0;
uniform float Dalashade_MaterialSnowIce < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Snow Ice"; > = 0.0;
uniform float Dalashade_MaterialStoneRuins < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Stone Ruins"; > = 0.0;
uniform float Dalashade_MaterialMetalIndustrial < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Metal Industrial"; > = 0.0;
uniform float Dalashade_MaterialSkyCloudFog < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Sky Cloud Fog"; > = 0.0;
uniform float Dalashade_MaterialSkinProtection < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Skin Protection"; > = 0.0;
uniform float Dalashade_MaterialVoidDarkness < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Material Void Darkness"; > = 0.0;
uniform float Dalashade_WaterContext < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Scene Water Context"; > = 0.0;
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

float Dalashade_ContactToneLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float Dalashade_ContactToneDepthEdge(float2 uv, float depth, float radius)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float d1 = ReShade::GetLinearizedDepth(saturate(uv + float2(texel.x, 0.0)));
    float d2 = ReShade::GetLinearizedDepth(saturate(uv - float2(texel.x, 0.0)));
    float d3 = ReShade::GetLinearizedDepth(saturate(uv + float2(0.0, texel.y)));
    float d4 = ReShade::GetLinearizedDepth(saturate(uv - float2(0.0, texel.y)));
    float nearEdge = max(max(abs(depth - d1), abs(depth - d2)), max(abs(depth - d3), abs(depth - d4)));
    float contactBias = max(max(depth - d1, depth - d2), max(depth - d3, depth - d4));
    return saturate(smoothstep(0.0008, 0.035, nearEdge) * 0.70 + smoothstep(0.0006, 0.020, contactBias) * 0.42);
}

float Dalashade_ContactToneLocalContrast(float2 uv, float centerLuma, float radius)
{
    float2 texel = BUFFER_PIXEL_SIZE * radius;
    float l1 = Dalashade_ContactToneLuma(tex2D(ReShade::BackBuffer, saturate(uv + float2(texel.x, 0.0))).rgb);
    float l2 = Dalashade_ContactToneLuma(tex2D(ReShade::BackBuffer, saturate(uv - float2(texel.x, 0.0))).rgb);
    float l3 = Dalashade_ContactToneLuma(tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, texel.y))).rgb);
    float l4 = Dalashade_ContactToneLuma(tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, texel.y))).rgb);
    float neighborLuma = (l1 + l2 + l3 + l4) * 0.25;
    float variation = abs(l1 - l2) + abs(l3 - l4) + abs(centerLuma - neighborLuma);
    return saturate(smoothstep(0.025, 0.22, variation) * (1.0 - smoothstep(0.84, 1.0, centerLuma)));
}

float3 Dalashade_ContactToneDebugOutput(float3 color, float3 result, float3 debugColor, float opacity)
{
    return lerp(result, debugColor, saturate(opacity));
}

float4 Dalashade_ContactTonePS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    float depth = ReShade::GetLinearizedDepth(texcoord);
    float luma = Dalashade_ContactToneLuma(color);

    Dalashade_FrameSceneSettings sceneSettings = Dalashade_FrameScene_DefaultSettings();
    sceneSettings.Readability = Dalashade_Readability;
    sceneSettings.Atmosphere = Dalashade_Atmosphere;
    sceneSettings.HighlightProtection = Dalashade_HighlightProtection;
    sceneSettings.ShadowProtection = Dalashade_ShadowProtection;
    sceneSettings.Wetness = max(Dalashade_Wetness, Dalashade_WetSurfaceContext);
    sceneSettings.FoliageDensity = Dalashade_FoliageDensity;
    sceneSettings.IndustrialHardness = Dalashade_IndustrialHardness;
    sceneSettings.CombatPressure = Dalashade_CombatPressure;
    sceneSettings.CinematicPermission = Dalashade_CinematicPermission;
    sceneSettings.StandaloneStrength = Dalashade_StandaloneStrength;
    Dalashade_FrameSceneData scene = Dalashade_ResolveFrameSceneData(sceneSettings);

    Dalashade_FrameDataSettings frameSettings = Dalashade_FrameData_DefaultSettings();
    frameSettings.MaterialFoliage = Dalashade_MaterialFoliage;
    frameSettings.MaterialWaterSpecular = max(Dalashade_MaterialWaterPlane, Dalashade_MaterialSpecularGlint);
    frameSettings.MaterialWaterPlane = max(Dalashade_MaterialWaterPlane, Dalashade_WaterContext);
    frameSettings.MaterialSpecularGlint = Dalashade_MaterialSpecularGlint;
    frameSettings.MaterialSandDust = Dalashade_MaterialSandDust;
    frameSettings.MaterialSnowIce = Dalashade_MaterialSnowIce;
    frameSettings.MaterialStoneRuins = Dalashade_MaterialStoneRuins;
    frameSettings.MaterialMetalIndustrial = Dalashade_MaterialMetalIndustrial;
    frameSettings.MaterialCrystalAether = 0.0;
    frameSettings.MaterialNeonGlass = 0.0;
    frameSettings.MaterialFireLavaHeat = 0.0;
    frameSettings.MaterialSkyCloudFog = Dalashade_MaterialSkyCloudFog;
    frameSettings.MaterialSkinProtection = Dalashade_MaterialSkinProtection;
    frameSettings.MaterialVoidDarkness = Dalashade_MaterialVoidDarkness;
    frameSettings.WaterContext = Dalashade_WaterContext;
    frameSettings.WetSurfaceContext = max(Dalashade_Wetness, Dalashade_WetSurfaceContext);
    frameSettings.HighlightProtection = Dalashade_HighlightProtection;
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

    float radius = Dalashade_ContactToneRadius * (1.0 + scene.StandaloneSafe * 0.16);
    float depthEdge = Dalashade_ContactToneDepthEdge(texcoord, depth, radius);
    float localContrast = Dalashade_ContactToneLocalContrast(texcoord, luma, radius * 1.25);
    float surfaceEdge = saturate(surface.EdgeDiscontinuity * (0.58 + surface.NormalConfidence * 0.34 + surface.DepthConfidence * 0.22));
    float surfaceContact = saturate(surface.AOReceiverSupport * 0.42 + surface.GroundCandidate * 0.28 + surface.StructureCandidate * 0.28 + surface.WallCandidate * 0.18);
    float hardSurface = saturate(frame.MaterialSurfaceHardness * 0.42 + frame.MaterialStoneRuins * 0.34 + frame.MaterialMetalIndustrial * 0.30 + scene.Industrial * 0.16);
    float receiver = saturate(frame.ReceiverAO * 0.34 + frame.ReceiverStructure * 0.30 + frame.ReceiverBroad * 0.12 + surfaceContact * 0.38 + hardSurface * 0.18);
    float foliageGrounding = saturate((frame.MaterialFoliage * 0.24 + scene.ForestCanopy * 0.18) * (1.0 - frame.SafetyFoliageNoiseReject * 0.48));
    receiver = saturate(receiver + foliageGrounding);

    float safetySuppression = saturate(
        frame.SafetySkyReject * 0.98
        + frame.MaterialSkyCloudFog * 0.92
        + frame.SafetySkinReject * 0.88
        + frame.MaterialSkinProtection * 0.82
        + frame.SafetyWaterAOReject * 0.78
        + frame.WaterReceiver * 0.52
        + frame.SafetySnowProtect * 0.66
        + frame.SafetyBrightSandProtect * 0.52
        + frame.SafetyHighlightProtect * 0.45
        + frame.SafetyUIDepthRisk * 0.72);
    float wetDampen = saturate(1.0 - max(frame.WaterSurface, scene.WetAir * 0.24) * 0.42);
    float readabilityDampen = saturate(1.0 - scene.CombatPressure * 0.30 - scene.Readability * 0.16);
    float tonalRange = Dalashade_RangeMask(luma, 0.045, 0.86);
    float contactMask = saturate((depthEdge * Dalashade_ContactToneEdgeStrength + surfaceEdge * 0.42 + surfaceContact * Dalashade_ContactToneStructureStrength + localContrast * Dalashade_ContactToneContrastStrength) * receiver);
    contactMask *= saturate(1.0 - safetySuppression);
    contactMask *= wetDampen * readabilityDampen * tonalRange * saturate(0.56 + surface.NormalConfidence * 0.30 + frame.SafetyDepthConfidence * 0.22);

    float visibleStrength = Dalashade_ContactToneStrength * Dalashade_ContactToneEnabled * (0.92 + scene.ShadowProtection * 0.16 + scene.StandaloneSafe * 0.28 + scene.CinematicPermission * 0.08);
    visibleStrength *= readabilityDampen;
    float toneAmount = saturate(contactMask * visibleStrength);
    float contrastAmount = saturate(contactMask * Dalashade_ContactToneContrastStrength * visibleStrength * 0.46);
    float3 grounded = color * (1.0 - toneAmount * (0.42 + hardSurface * 0.18 + scene.StandaloneSafe * 0.10));
    grounded = lerp(grounded, saturate((grounded - 0.5) * (1.0 + contrastAmount * 0.32) + 0.5), contrastAmount);
    float3 result = saturate(grounded);

    int mode = Dalashade_ContactToneDebugMode;
    if (mode > 0)
    {
        float3 debugColor = float3(0.0, 0.0, 0.0);
        if (mode == 1)
        {
            debugColor = contactMask.xxx;
        }
        else if (mode == 2)
        {
            debugColor = float3(depthEdge, localContrast, frame.SafetyDepthConfidence);
        }
        else if (mode == 3)
        {
            debugColor = float3(surfaceEdge, surfaceContact, surface.NormalConfidence);
        }
        else if (mode == 4)
        {
            debugColor = float3(receiver, saturate(1.0 - safetySuppression), wetDampen);
        }
        else if (mode == 5)
        {
            debugColor = float3(safetySuppression, scene.CombatPressure, scene.Readability);
        }
        else if (mode == 6)
        {
            debugColor = saturate(abs(result - color) * 8.0 + float3(contactMask * 0.20, toneAmount * 0.35, contrastAmount * 0.45));
        }

        return float4(Dalashade_ContactToneDebugOutput(color, result, saturate(debugColor), Dalashade_ContactToneDebugOpacity), 1.0);
    }

    return float4(result, 1.0);
}

technique Dalashade_ContactTone
{
    pass ContactTone
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_ContactTonePS;
    }
}
