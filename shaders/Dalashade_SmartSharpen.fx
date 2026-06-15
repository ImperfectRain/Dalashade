#include "ReShade.fxh"
#include "Dalashade_MaterialMasks.fxh"
#include "Dalashade_NormalField.fxh"

uniform float Dalashade_Readability <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Readability";
    ui_tooltip = "Scene-driven readability pressure. Higher values allow a little more readable edge clarity.";
> = 0.0;

uniform float Dalashade_Haze <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Haze";
    ui_tooltip = "Scene-driven fog, dust, cloud, or haze pressure. Higher values reduce sharpening.";
> = 0.0;

uniform float Dalashade_Wetness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Wetness";
    ui_tooltip = "Scene-driven rain or wet-surface pressure. Higher values reduce specular-edge sharpening.";
> = 0.0;

uniform float Dalashade_FoliageDensity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Foliage Density";
    ui_tooltip = "Scene-driven foliage density. Higher values reduce texture-detail sharpening.";
> = 0.0;

uniform float Dalashade_CombatPressure <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Combat Pressure";
    ui_tooltip = "Scene-driven gameplay pressure. Higher values damp heavy sharpening.";
> = 0.0;

uniform float Dalashade_HighlightProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Highlight Protection";
    ui_tooltip = "Scene-driven bright highlight protection. Higher values reduce halo-prone bright edge sharpening.";
> = 0.0;

uniform float Dalashade_Night <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night";
    ui_tooltip = "Scene-driven nighttime context. Higher values reduce dark-noise sharpening.";
> = 0.0;

uniform float Dalashade_AmbientDarkness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Ambient Darkness";
    ui_tooltip = "Scene-driven unlit baseline darkness. Higher values protect deep shadows from crunch.";
> = 0.0;

uniform float Dalashade_ArtificialLight <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Artificial Light";
    ui_tooltip = "Scene-driven local light-pool influence. Higher values preserves lit structural edges.";
> = 0.0;

uniform float Dalashade_Daylight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Daylight"; ui_tooltip = "Scene-driven daytime context for sharpen safety."; > = 0.0;
uniform float Dalashade_OpenSkyLight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Open Sky Light"; ui_tooltip = "Scene-driven open-sky daylight; protects sky, water, and bright surfaces from crunch."; > = 0.0;
uniform float Dalashade_DayHighlightPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Highlight Pressure"; ui_tooltip = "Scene-driven daytime bright-surface halo protection."; > = 0.0;

uniform float Dalashade_MaterialFoliage <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Foliage";
    ui_tooltip = "Inferred foliage likelihood. Reduces leaf, grass, and canopy micro-sharpening.";
> = 0.0;

uniform float Dalashade_MaterialWaterSpecular <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water/Specular";
    ui_tooltip = "Inferred water or wet/specular likelihood. Reduces glint and water shimmer sharpening.";
> = 0.0;

uniform float Dalashade_MaterialWaterPlane <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water Plane";
    ui_tooltip = "Optional split water-surface likelihood. Broad water surfaces reduce shimmer without treating every bright edge as water.";
> = 0.0;

uniform float Dalashade_MaterialSpecularGlint <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Specular Glint";
    ui_tooltip = "Optional split thin-glint likelihood. Protects bright rails, nails, water sparkles, and highlight lines from haloing.";
> = 0.0;

uniform float Dalashade_MaterialSandDust <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sand/Dust";
    ui_tooltip = "Inferred sand or dust likelihood. Reduces gritty bright-sand sharpening.";
> = 0.0;

uniform float Dalashade_MaterialSnowIce <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Snow/Ice";
    ui_tooltip = "Inferred snow or ice likelihood. Reduces bright snow noise and black-on-white halos.";
> = 0.0;

uniform float Dalashade_MaterialStoneRuins <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Stone/Ruins";
    ui_tooltip = "Inferred stone or ruins likelihood. Supports stable structural clarity.";
> = 0.0;

uniform float Dalashade_MaterialMetalIndustrial <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Metal/Industrial";
    ui_tooltip = "Inferred metal or industrial likelihood. Supports stable hard-edge clarity while restraining glints.";
> = 0.0;

uniform float Dalashade_MaterialCrystalAether <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Crystal/Aether";
    ui_tooltip = "Inferred crystal or aether likelihood. Restrains glow-edge halos.";
> = 0.0;

uniform float Dalashade_MaterialNeonGlass <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Neon/Glass";
    ui_tooltip = "Inferred neon or glass likelihood. Restrains saturated halo sharpening.";
> = 0.0;

uniform float Dalashade_MaterialSkyCloudFog <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sky/Cloud/Fog";
    ui_tooltip = "Inferred sky, cloud, fog, or atmospheric-gradient likelihood. Excludes smooth gradients from sharpening.";
> = 0.0;

uniform float Dalashade_MaterialSkinProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Skin Protection";
    ui_tooltip = "Inferred character/skin protection likelihood. Reduces aggressive sharpening on smooth warm foreground regions.";
> = 0.0;

uniform int Dalashade_MaterialDebugMode <
    ui_type = "combo";
    ui_items = "Off\0Overview\0Foliage dampening\0Water/specular dampening\0Snow/ice dampening\0Sky/fog exclusion\0Skin protection dampening\0Water plane dampening\0Specular glint dampening\0Unused\0Final material dampening\0";
    ui_label = "Dalashade Material Debug Mode";
    ui_tooltip = "Shows material-aware influence masks. These masks are inferred likelihoods, not true engine material IDs.";
> = 0;

uniform float Dalashade_MaterialDebugStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Debug Strength";
> = 1.0;

uniform bool Dalashade_EnableDepthAssist <
    ui_label = "Enable Depth Assist";
    ui_tooltip = "Optional material-mask helper. Disabled by default; SmartSharpen masks still work without depth.";
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
    ui_tooltip = "Optional inferred normal/surface field gate. SmartSharpen uses this only for stable-structure and halo safety.";
> = 0.0;

uniform float Dalashade_NormalFieldStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Strength";
    ui_tooltip = "Global scale for optional NormalField sharpen shaping.";
> = 0.0;

uniform float Dalashade_NormalDepthStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Depth Strength";
    ui_tooltip = "Depth-normal contribution for optional sharpen stability.";
> = 0.0;

uniform float Dalashade_NormalDetailStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Detail Strength";
    ui_tooltip = "Detail-normal contribution for optional sharpen stability.";
> = 0.0;

uniform float Dalashade_NormalMaterialInfluence <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Material Influence";
    ui_tooltip = "Material-aware scale for optional NormalField sharpen shaping.";
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

uniform float SharpenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Sharpen Strength";
    ui_tooltip = "Overall conservative sharpen strength. This is clarity sharpening, not anti-aliasing.";
> = 0.36;

uniform float Dalashade_StandaloneStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Standalone Stack Strength";
    ui_tooltip = "0 keeps SmartSharpen supportive for an existing preset. 1 adds modest stable-structure clarity while increasing unsafe-material dampening.";
> = 0.0;

uniform float EdgeClarityStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Edge Clarity Strength";
> = 0.42;

uniform float StructuralClarityStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Structural Clarity Strength";
    ui_tooltip = "Broad silhouette/geometry clarity. This is safer than texture-detail sharpening in foliage.";
> = 0.50;

uniform float TextureDetailStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Texture Detail Strength";
> = 0.24;

uniform float AntiCrunchStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Anti-Crunch Strength";
    ui_tooltip = "Limits oversharpened dark/light edge crunch. Raise if edges look gritty.";
> = 0.70;

uniform float DepthDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Depth Dampen Strength";
> = 0.55;

uniform float FarDepthDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Far Depth Dampen Strength";
    ui_tooltip = "Extra reduction for distant trees, heat haze, fog, and background detail.";
> = 0.72;

uniform float FoliageDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Foliage Dampen Strength";
    ui_tooltip = "Reduces leaf/grass micro-sharpening in forest and jungle scenes.";
> = 0.80;

uniform float HighlightDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Highlight Dampen Strength";
> = 0.72;

uniform float HaloDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Halo Dampen Strength";
    ui_tooltip = "Reduces bright-edge halos around clouds, lamps, water glints, sand, and white architecture.";
> = 0.78;

uniform float SkyDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Sky Dampen Strength";
    ui_tooltip = "Suppresses sharpening on sky, fog, and smooth gradients.";
> = 0.78;

uniform float HazeDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Haze Dampen Strength";
> = 0.68;

uniform float CombatDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Combat Dampen Strength";
> = 0.66;

uniform float LumaOnlyStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Luma Only Strength";
    ui_tooltip = "How much sharpening is applied through luma-safe reconstruction instead of full RGB detail.";
> = 0.80;

uniform float Dalashade_SharpenAuthority <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 2.0;
    ui_label = "Dalashade Sharpen Authority";
    ui_tooltip = "0 passive, 1 secondary when another sharpener is active, 2 primary adaptive sharpen.";
> = 2.0;

uniform bool ShowDebugMask <
    ui_label = "Show Debug Mask";
    ui_tooltip = "Shows the selected SmartSharpen mask.";
> = false;

uniform int DebugView <
    ui_type = "combo";
    ui_items = "Composite\0Structural edge\0Texture detail\0Final sharpen\0Dampening\0Foliage/far-depth\0";
    ui_label = "Debug View";
> = 0;

float Dalashade_SmartSharpenLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 Dalashade_SmartSharpenSample(float2 uv)
{
    return tex2D(ReShade::BackBuffer, uv).rgb;
}

float4 Dalashade_SmartSharpenPS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    // This shader improves already-rendered clarity. It does not fix true aliasing, shimmer, or missing temporal AA.
    float3 center = Dalashade_SmartSharpenSample(texcoord);
    float2 texel = BUFFER_PIXEL_SIZE;

    float3 north = Dalashade_SmartSharpenSample(texcoord + float2(0.0, -texel.y));
    float3 south = Dalashade_SmartSharpenSample(texcoord + float2(0.0, texel.y));
    float3 east = Dalashade_SmartSharpenSample(texcoord + float2(texel.x, 0.0));
    float3 west = Dalashade_SmartSharpenSample(texcoord + float2(-texel.x, 0.0));
    float3 farNorth = Dalashade_SmartSharpenSample(texcoord + float2(0.0, -texel.y * 2.0));
    float3 farSouth = Dalashade_SmartSharpenSample(texcoord + float2(0.0, texel.y * 2.0));
    float3 farEast = Dalashade_SmartSharpenSample(texcoord + float2(texel.x * 2.0, 0.0));
    float3 farWest = Dalashade_SmartSharpenSample(texcoord + float2(-texel.x * 2.0, 0.0));
    float3 blur = (north + south + east + west) * 0.25;
    float3 wideBlur = (center * 0.20) + ((north + south + east + west) * 0.10) + ((farNorth + farSouth + farEast + farWest) * 0.10);

    float centerLuma = Dalashade_SmartSharpenLuma(center);
    float blurLuma = Dalashade_SmartSharpenLuma(blur);
    float wideBlurLuma = Dalashade_SmartSharpenLuma(wideBlur);
    float detailLuma = abs(centerLuma - blurLuma);
    float structuralLuma = abs(centerLuma - wideBlurLuma);
    float structuralEdgeMask = smoothstep(0.026, 0.170, structuralLuma);
    float textureDetailMask = smoothstep(0.004, 0.040, detailLuma) * (1.0 - smoothstep(0.060, 0.150, structuralLuma));
    float colorVariance = max(max(abs(center.r - center.g), abs(center.g - center.b)), abs(center.r - center.b));

    // Intent masks reduce sharpening where clarity usually becomes shimmer, halos, or combat clutter.
    float readability = saturate(Dalashade_Readability);
    float haze = saturate(Dalashade_Haze);
    float wetness = saturate(Dalashade_Wetness);
    float foliage = saturate(Dalashade_FoliageDensity);
    float combat = saturate(Dalashade_CombatPressure);
    float highlightProtection = saturate(Dalashade_HighlightProtection);
    float night = saturate(Dalashade_Night);
    float ambientDarkness = saturate(Dalashade_AmbientDarkness);
    float artificialLight = saturate(Dalashade_ArtificialLight);
    float daylight = saturate(Dalashade_Daylight);
    float openSkyLight = saturate(Dalashade_OpenSkyLight);
    float dayHighlightPressure = saturate(Dalashade_DayHighlightPressure);
    float authority = saturate(Dalashade_SharpenAuthority * 0.5);
    float standaloneStrength = saturate(Dalashade_StandaloneStrength);
    float standaloneSharpen = saturate(standaloneStrength * authority * (1.0 - combat * 0.45));
    float materialFoliage = saturate(Dalashade_MaterialFoliage);
    float materialWaterSpecular = saturate(Dalashade_MaterialWaterSpecular);
    float materialWaterPlaneScene = saturate(Dalashade_MaterialWaterPlane);
    float materialSpecularGlintScene = saturate(Dalashade_MaterialSpecularGlint);
    float materialSandDust = saturate(Dalashade_MaterialSandDust);
    float materialSnowIce = saturate(Dalashade_MaterialSnowIce);
    float materialStoneRuins = saturate(Dalashade_MaterialStoneRuins);
    float materialMetalIndustrial = saturate(Dalashade_MaterialMetalIndustrial);
    float materialCrystalAether = saturate(Dalashade_MaterialCrystalAether);
    float materialNeonGlass = saturate(Dalashade_MaterialNeonGlass);
    float materialSkyCloudFog = saturate(Dalashade_MaterialSkyCloudFog);
    float materialSkinProtection = saturate(Dalashade_MaterialSkinProtection);
    Dalashade_MaterialResolve material = Dalashade_ResolveMaterials(
        center,
        texcoord,
        materialFoliage,
        materialWaterSpecular,
        materialWaterPlaneScene,
        materialSpecularGlintScene,
        materialSandDust,
        materialSnowIce,
        materialStoneRuins,
        materialMetalIndustrial,
        materialCrystalAether,
        materialNeonGlass,
        0.0,
        materialSkyCloudFog,
        materialSkinProtection,
        0.0,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_WaterResolve water = Dalashade_ResolveWater(
        center,
        texcoord,
        materialWaterPlaneScene,
        materialWaterPlaneScene,
        materialWaterPlaneScene,
        materialWaterPlaneScene,
        wetness,
        material.WaterPlane,
        material.SpecularGlint,
        0.0,
        material.SkyCloudFog,
        material.SkinProtection,
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_SafetyResolve safety = Dalashade_ResolveSafety(
        center,
        texcoord,
        material,
        water,
        max(highlightProtection, dayHighlightPressure),
        Dalashade_EnableDepthAssist ? 1.0 : 0.0,
        Dalashade_DepthAssistStrength,
        max(Dalashade_DepthAssistConfidenceFloor, Dalashade_DepthConfidenceFloor));
    Dalashade_NormalField normalField = Dalashade_ResolveNormalField(
        center,
        texcoord,
        material,
        water,
        safety,
        Dalashade_NormalFieldEnabled,
        Dalashade_NormalFieldStrength,
        Dalashade_NormalDepthStrength,
        Dalashade_NormalDetailStrength,
        Dalashade_NormalMaterialInfluence,
        Dalashade_NormalWaterSuppression,
        Dalashade_NormalSkinSuppression,
        Dalashade_NormalSkySuppression);
    float materialFoliageStrong = material.Foliage;
    float materialOrganicGreen = saturate(material.Foliage * material.SurfaceHardness * 0.35);
    float materialFoliagePixel = saturate(materialFoliageStrong + materialOrganicGreen * 0.42);
    float materialWaterPlanePixel = water.WaterSurface;
    float materialSpecularGlintPixel = material.SpecularGlint;
    float materialWaterPixel = saturate(max(water.WaterPixelConfidence, max(water.WaterReceiver, material.WaterSpecular)));
    float materialSandPixel = material.SandDust;
    float materialSnowPixel = material.SnowIce;
    float materialAetherNeonPixel = saturate(max(material.CrystalAether, material.NeonGlass));
    float materialSkyPixel = saturate(max(material.SkyCloudFog, safety.SkyReject));
    float materialSkinPixel = saturate(max(material.SkinProtection, safety.SkinReject));
    float hardStructureSupport = saturate(
        material.StructureReceiverConfidence * 0.48
        + material.SurfaceHardness * 0.22
        + material.StoneRuins * 0.22
        + material.MetalIndustrial * 0.22);
    float normalFieldInfluence = saturate(Dalashade_NormalFieldEnabled * Dalashade_NormalFieldStrength * Dalashade_NormalMaterialInfluence);
    float normalStableStructure = saturate(
        normalFieldInfluence
        * normalField.StructureCandidate
        * (0.30 + normalField.NormalConfidence * 0.42 + normalField.OrientationConfidence * 0.28)
        * (1.0 - max(materialWaterPixel, materialFoliagePixel) * 0.58)
        * (1.0 - materialSkyPixel * 0.90)
        * (1.0 - materialSkinPixel * 0.82));
    float normalHaloRisk = saturate(
        normalFieldInfluence
        * normalField.EdgeDiscontinuity
        * (0.35 + (1.0 - normalField.NormalConfidence) * 0.45 + smoothstep(0.78, 1.0, centerLuma) * 0.20));
    float normalDetailNoiseRisk = saturate(
        normalFieldInfluence
        * normalField.DetailStrength
        * (1.0 - normalField.StructureCandidate * 0.45)
        * max(materialFoliagePixel, max(materialWaterPixel, materialSkyPixel)));

    float brightMask = smoothstep(0.62, 0.96, centerLuma);
    float veryBrightMask = smoothstep(0.78, 1.0, centerLuma);
    float smoothGradientMask = (1.0 - smoothstep(0.008, 0.050, max(detailLuma, structuralLuma))) * (1.0 - smoothstep(0.030, 0.18, colorVariance));
    float skyGradientMask = smoothstep(0.50, 0.94, centerLuma) * smoothGradientMask;
    float specularEdgeMask = max(brightMask * smoothstep(0.014, 0.080, detailLuma), veryBrightMask * smoothstep(0.018, 0.100, structuralLuma));
    float deepShadowMask = ambientDarkness * night * (1.0 - smoothstep(0.05, 0.28, centerLuma));
    float litStructureMask = artificialLight * smoothstep(0.32, 0.82, centerLuma) * structuralEdgeMask * (1.0 - veryBrightMask * 0.42);
    float depth = ReShade::GetLinearizedDepth(texcoord);
    float midDepthMask = smoothstep(0.10, 0.55, depth);
    float farDepthMask = smoothstep(0.28, 0.92, depth);

    // MaterialIntent masks are inferred scene likelihoods. They only damp unsafe sharpening and never boost clarity globally.
    float warmSmoothMask = smoothstep(0.02, 0.22, center.r - center.b)
        * smoothstep(-0.08, 0.18, center.g - center.b)
        * (1.0 - smoothstep(0.060, 0.240, colorVariance))
        * smoothstep(0.16, 0.42, centerLuma)
        * (1.0 - smoothstep(0.78, 0.98, centerLuma));
    float materialFoliagePressure = saturate(materialFoliagePixel * FoliageDampenStrength * (0.22 + textureDetailMask * 0.82 + farDepthMask * 0.24));
    float materialWaterPressure = saturate(materialWaterPixel * (0.34 + wetness * 0.28 + farDepthMask * 0.18 + textureDetailMask * 0.20) * HighlightDampenStrength);
    float materialGlintPressure = saturate(materialSpecularGlintPixel * (specularEdgeMask * 0.78 + veryBrightMask * 0.28) * HaloDampenStrength);
    float materialSandPressure = saturate(max(materialSandPixel, safety.BrightSandProtect) * (brightMask * 0.40 + textureDetailMask * 0.25 + veryBrightMask * 0.25) * HaloDampenStrength);
    float materialSnowPressure = saturate(materialSnowPixel * (brightMask * 0.54 + veryBrightMask * 0.36 + structuralEdgeMask * 0.18) * HaloDampenStrength);
    float materialSkyPressure = saturate(max(materialSkyPixel, materialSkyCloudFog * skyGradientMask * 0.42) * max(skyGradientMask, smoothGradientMask * (0.45 + haze * 0.45)) * SkyDampenStrength);
    float materialSkinPressure = saturate(max(materialSkinPixel, materialSkinProtection * warmSmoothMask * 0.35) * (0.50 + readability * 0.12) * AntiCrunchStrength);
    float materialAetherNeonPressure = saturate(materialAetherNeonPixel * (0.28 + specularEdgeMask * 0.38 + veryBrightMask * 0.24) * HaloDampenStrength);
    float safetyPressure = saturate(
        safety.SkyReject * SkyDampenStrength
        + safety.SkinReject * AntiCrunchStrength * 0.85
        + safety.FoliageNoiseReject * FoliageDampenStrength * 0.72
        + safety.HighlightProtect * HighlightDampenStrength * 0.48
        + safety.BrightSandProtect * HaloDampenStrength * 0.38
        + safety.SnowProtect * HaloDampenStrength * 0.38);
    float materialDampen = saturate(max(max(materialFoliagePressure, max(materialWaterPressure, materialGlintPressure)), max(max(materialSandPressure, materialSnowPressure), max(max(materialSkyPressure, materialSkinPressure), max(materialAetherNeonPressure, safetyPressure * 0.55)))));

    float hazePressure = saturate(max(haze, wetness * 0.72) * HazeDampenStrength);
    float foliageTexturePressure = saturate(foliage * FoliageDampenStrength * (0.42 + textureDetailMask * 0.72 + farDepthMask * 0.24));
    float foliageStructurePressure = saturate(foliage * 0.22 * (1.0 - structuralEdgeMask * 0.45));
    float highlightPressure = saturate((max(highlightProtection, dayHighlightPressure) * 0.72 + wetness * 0.22 + veryBrightMask * 0.36 + openSkyLight * daylight * 0.10) * specularEdgeMask * HighlightDampenStrength);
    float haloPressure = saturate((veryBrightMask * 0.42 + specularEdgeMask * 0.58) * HaloDampenStrength);
    float depthTexturePressure = saturate((midDepthMask * DepthDampenStrength * 0.35 + farDepthMask * FarDepthDampenStrength) * (0.45 + haze * 0.55));
    float skyPressure = saturate(skyGradientMask * SkyDampenStrength);
    float secondaryAuthorityPressure = 1.0 - authority;

    float structuralDampen = saturate(hazePressure * 0.36 + highlightPressure * 0.52 + haloPressure * 0.44 + skyPressure + materialSkyPressure * 0.60 + materialSnowPressure * 0.20 + materialSandPressure * 0.16 + materialAetherNeonPressure * 0.22 + materialSkinPressure * 0.22 + foliageStructurePressure + normalHaloRisk * 0.32 + deepShadowMask * 0.46 + secondaryAuthorityPressure * 0.24);
    float textureDampen = saturate(hazePressure * 0.78 + wetness * 0.25 + foliageTexturePressure + materialFoliagePressure + depthTexturePressure + highlightPressure + haloPressure * 0.74 + skyPressure + materialWaterPressure + materialGlintPressure + materialSandPressure + materialSnowPressure + materialSkyPressure + materialAetherNeonPressure * 0.70 + materialSkinPressure * 0.78 + safetyPressure * 0.35 + normalHaloRisk * 0.42 + normalDetailNoiseRisk * 0.55 + deepShadowMask * 0.82 + secondaryAuthorityPressure * 0.58);
    float standaloneUnsafeMaterial = max(max(materialWaterPressure, materialSkyPressure), max(max(materialFoliagePressure, materialSkinPressure), max(materialGlintPressure, max(materialSnowPressure, materialSandPressure))));
    structuralDampen = saturate(structuralDampen + standaloneSharpen * standaloneUnsafeMaterial * 0.03);
    textureDampen = saturate(textureDampen + standaloneSharpen * standaloneUnsafeMaterial * 0.05);
    float dampen = saturate(max(structuralDampen * 0.72, textureDampen));

    // Structural clarity uses broader low-frequency luma edges: silhouettes, trunks, rocks, buildings, armor, and readable geometry.
    float structuralBoost = (StructuralClarityStrength + EdgeClarityStrength * 0.34) * structuralEdgeMask;
    structuralBoost *= 0.62 + readability * 0.18 + combat * 0.12 + litStructureMask * 0.12 + hardStructureSupport * 0.07 + normalStableStructure * 0.05;
    structuralBoost *= lerp(1.0, 1.12, standaloneSharpen * (1.0 - structuralDampen * 0.55));
    structuralBoost *= 1.0 - structuralDampen * 0.78;

    // Texture detail is a separate, much smaller channel. Foliage, haze, wetness, far depth, and secondary authority suppress it hard.
    float textureBoost = TextureDetailStrength * textureDetailMask;
    textureBoost *= lerp(1.0, 1.04, standaloneSharpen * (1.0 - textureDampen * 0.80));
    textureBoost *= 1.0 - textureDampen * 0.94;
    textureBoost *= 1.0 - saturate(foliage * 0.62 + farDepthMask * 0.42 + deepShadowMask * 0.56 + normalDetailNoiseRisk * 0.32);

    float structuralAmount = clamp(SharpenStrength * structuralBoost * authority, 0.0, lerp(0.145, 0.160, standaloneSharpen));
    float textureAmount = clamp(SharpenStrength * textureBoost * authority, 0.0, lerp(0.052, 0.056, standaloneSharpen));
    float sharpenAmount = structuralAmount + textureAmount;

    float3 structuralDetail = center - wideBlur;
    float3 textureDetail = center - blur;

    // Luma-safe reconstruction avoids chroma ringing; remaining RGB detail is kept tiny and heavily clamped.
    float structuralDeltaLuma = centerLuma - wideBlurLuma;
    float textureDeltaLuma = centerLuma - blurLuma;
    float lumaDelta = structuralDeltaLuma * structuralAmount + textureDeltaLuma * textureAmount;
    float3 lumaOnlyDetail = float3(lumaDelta, lumaDelta, lumaDelta);
    float3 rgbDetail = structuralDetail * structuralAmount + textureDetail * textureAmount;
    float lumaOnly = saturate(LumaOnlyStrength + foliage * 0.10 + highlightPressure * 0.12);
    float3 detail = lerp(rgbDetail, lumaOnlyDetail, lumaOnly);

    // Anti-crunch and halo guards limit high-contrast pushes before they become gritty outlines.
    float crunchGuard = smoothstep(0.11, 0.32, max(detailLuma, structuralLuma)) * AntiCrunchStrength;
    float detailLimit = lerp(0.060, 0.018, saturate(crunchGuard + highlightPressure * 0.60 + haloPressure * 0.55 + skyPressure * 0.70 + normalHaloRisk * 0.36));
    detail = clamp(detail, -detailLimit, detailLimit);

    float3 sharpened = center + detail;

    // Final delta guardrail keeps SmartSharpen secondary-safe even when another sharpener or Clarity is active.
    float deltaLimit = lerp(0.026, 0.065 * lerp(1.0, 1.08, standaloneSharpen), authority) * (1.0 - saturate(foliage * 0.18 + farDepthMask * 0.16 + veryBrightMask * 0.10));
    sharpened = min(sharpened, center + deltaLimit);
    sharpened = max(sharpened, center - deltaLimit);
    sharpened = saturate(sharpened);

    if (ShowDebugMask)
    {
        // Material debug views show inferred dampening influence. They are not true material-ID visualizations.
        float materialDebugStrength = saturate(Dalashade_MaterialDebugStrength);
        if (Dalashade_MaterialDebugMode == 1)
        {
            return float4(saturate(materialFoliagePressure + materialWaterPressure) * materialDebugStrength, saturate(materialSnowPressure + materialSkyPressure + materialSandPressure) * materialDebugStrength, saturate(materialSkinPressure + materialAetherNeonPressure + materialDampen) * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 2)
        {
            return float4(materialFoliageStrong * materialDebugStrength, materialOrganicGreen * materialDebugStrength, materialFoliagePressure * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 3)
        {
            return float4(materialWaterPlanePixel * materialDebugStrength, materialSpecularGlintPixel * materialDebugStrength, saturate(materialWaterPressure + materialGlintPressure) * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 4)
        {
            return float4(materialSnowPixel * materialDebugStrength, brightMask * materialDebugStrength, materialSnowPressure * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 5)
        {
            return float4(materialSkyPixel * materialDebugStrength, skyGradientMask * materialDebugStrength, materialSkyPressure * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 6)
        {
            return float4(materialSkinPixel * materialDebugStrength, warmSmoothMask * materialDebugStrength, materialSkinPressure * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 7)
        {
            return float4(materialWaterPlanePixel * materialDebugStrength, farDepthMask * materialDebugStrength, materialWaterPressure * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 8)
        {
            return float4(materialSpecularGlintPixel * materialDebugStrength, specularEdgeMask * materialDebugStrength, materialGlintPressure * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 10)
        {
            return float4(materialDampen * materialDebugStrength, textureDampen * materialDebugStrength, saturate(structuralDampen + normalHaloRisk) * materialDebugStrength, 1.0);
        }

        if (DebugView == 1)
        {
            return float4(structuralEdgeMask, saturate(structuralAmount * 8.0), saturate(structuralDampen), 1.0);
        }
        if (DebugView == 2)
        {
            return float4(textureDetailMask, saturate(textureAmount * 16.0), saturate(textureDampen), 1.0);
        }
        if (DebugView == 3)
        {
            return float4(saturate(sharpenAmount * 8.0), saturate(structuralAmount * 8.0), saturate(textureAmount * 16.0), 1.0);
        }
        if (DebugView == 4)
        {
            return float4(saturate(haloPressure + highlightPressure), saturate(skyPressure), saturate(dampen), 1.0);
        }
        if (DebugView == 5)
        {
            return float4(saturate(foliageTexturePressure), saturate(farDepthMask), saturate(depthTexturePressure), 1.0);
        }

        return float4(saturate(structuralAmount * 8.0 + litStructureMask), saturate(textureAmount * 16.0), saturate(dampen + deepShadowMask), 1.0);
    }

    return float4(sharpened, 1.0);
}

technique Dalashade_SmartSharpen
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_SmartSharpenPS;
    }
}
