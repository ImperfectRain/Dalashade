#include "ReShade.fxh"
#include "Dalashade_FrameData.fxh"

uniform float Dalashade_Atmosphere <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Atmosphere";
    ui_tooltip = "Scene-driven atmosphere allowance. Higher values allow more ambient glow.";
> = 0.0;

uniform float Dalashade_MagicGlow <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Magic Glow";
    ui_tooltip = "Scene-driven aetherial or magical glow pressure.";
> = 0.0;

uniform float Dalashade_NeonGlow <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Neon Glow";
    ui_tooltip = "Scene-driven neon or high-tech glow pressure.";
> = 0.0;

uniform float Dalashade_FoliageDensity <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Foliage Density";
    ui_tooltip = "Scene-driven foliage density. Higher values allow subtle canopy/sky-light bloom.";
> = 0.0;

uniform float Dalashade_Wetness <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Wetness";
    ui_tooltip = "Scene-driven rain or wet-surface pressure. Higher values allow small specular glow.";
> = 0.0;

uniform float Dalashade_Heat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Heat";
    ui_tooltip = "Scene-driven heat/dust pressure. Higher values allow distant warm atmospheric glow.";
> = 0.0;

uniform float Dalashade_Readability <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Readability";
    ui_tooltip = "Scene-driven readability pressure. Higher values restrain bloom.";
> = 0.0;

uniform float Dalashade_HighlightProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Highlight Protection";
    ui_tooltip = "Scene-driven bright highlight restraint. Higher values raise bloom threshold.";
> = 0.0;

uniform float Dalashade_CombatPressure <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Combat Pressure";
    ui_tooltip = "Scene-driven gameplay pressure. Higher values damp bloom.";
> = 0.0;

uniform float Dalashade_CinematicPermission <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cinematic Permission";
    ui_tooltip = "Scene-driven permission for stronger cinematic glow outside gameplay-critical moments.";
> = 0.0;

uniform float Dalashade_Night <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night";
    ui_tooltip = "Scene-driven nighttime context. Higher values make bloom more selective.";
> = 0.0;

uniform float Dalashade_Moonlight <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Moonlight";
    ui_tooltip = "Scene-driven moonlight influence for subtle cool diffusion.";
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
    ui_tooltip = "Scene-driven dark baseline. Higher values reduce full-screen wash.";
> = 0.0;

uniform int Dalashade_FirstPartyPerformanceTier <
    ui_type = "combo";
    ui_items = "Quality\0Balanced\0Performance\0";
    ui_label = "First-Party Performance Tier";
    ui_tooltip = "Quality preserves the full bloom source gather. Balanced and Performance trim optional outer diffusion samples.";
> = 0;

uniform float Dalashade_BloomSampleQuality <
    ui_type = "slider";
    ui_min = 0.25; ui_max = 1.0;
    ui_label = "Bloom Sample Quality";
> = 1.0;

uniform float Dalashade_NightAtmosphere <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Night Atmosphere";
    ui_tooltip = "Scene-driven nighttime air/mist/storm atmosphere.";
> = 0.0;

uniform float Dalashade_Daylight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Daylight"; ui_tooltip = "Scene-driven daytime context for bloom restraint."; > = 0.0;
uniform float Dalashade_Sunlight < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Sunlight"; ui_tooltip = "Scene-driven direct sunlight pressure for highlight-safe bloom."; > = 0.0;
uniform float Dalashade_DayAtmosphere < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Atmosphere"; ui_tooltip = "Scene-driven daytime air, mist, storm, or coastal diffusion."; > = 0.0;
uniform float Dalashade_DayHighlightPressure < ui_type = "slider"; ui_min = 0.0; ui_max = 1.0; ui_label = "Dalashade Day Highlight Pressure"; ui_tooltip = "Scene-driven daytime bright-surface bloom restraint."; > = 0.0;

uniform float Dalashade_MaterialWaterSpecular <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water/Specular";
    ui_tooltip = "Inferred water or wet/specular likelihood. Adds restrained glint glow and prevents sparkle blowout.";
> = 0.0;

uniform float Dalashade_MaterialWaterPlane <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Water Plane";
    ui_tooltip = "Optional split water-surface likelihood for restrained coastal/water shimmer.";
> = 0.0;

uniform float Dalashade_MaterialSpecularGlint <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Specular Glint";
    ui_tooltip = "Optional split thin-glint likelihood for tight sparkle and reflective highlight bloom eligibility.";
> = 0.0;

uniform float Dalashade_MaterialFoliage <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Foliage";
    ui_tooltip = "Inferred foliage likelihood. Supports canopy gap bloom without blooming broad sky.";
> = 0.0;

uniform float Dalashade_MaterialCrystalAether <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Crystal/Aether";
    ui_tooltip = "Inferred crystal or aether likelihood. Adds color-selective magical glow.";
> = 0.0;

uniform float Dalashade_MaterialNeonGlass <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Neon/Glass";
    ui_tooltip = "Inferred neon or glass likelihood. Adds small-radius colored neon bloom with strong restraint.";
> = 0.0;

uniform float Dalashade_MaterialFireLavaHeat <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Fire/Lava/Heat";
    ui_tooltip = "Inferred fire, lava, or heat likelihood. Adds warm source glow and respects combat dampening.";
> = 0.0;

uniform float Dalashade_MaterialSkyCloudFog <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Sky/Cloud/Fog";
    ui_tooltip = "Inferred sky, cloud, fog, or atmosphere likelihood. Allows broad glow while preventing milky wash.";
> = 0.0;

uniform float Dalashade_MaterialSkinProtection <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Skin Protection";
    ui_tooltip = "Inferred skin/character protection. Restrains bloom on skin-like areas.";
> = 0.0;

uniform int Dalashade_MaterialDebugMode <
    ui_type = "combo";
    ui_items = "Off\0Overview\0Water/specular\0Crystal/aether\0Neon/glass\0Fire/heat\0Sky/fog\0Final bloom eligibility\0Water plane\0Specular glint\0Canopy gap bloom\0";
    ui_label = "Dalashade Material Debug Mode";
    ui_tooltip = "Shows material-aware bloom influence masks. These masks are inferred likelihoods, not true engine material IDs.";
> = 0;

uniform float Dalashade_MaterialDebugStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Debug Strength";
> = 1.0;

uniform bool Dalashade_EnableDepthAssist <
    ui_label = "Enable Depth Assist";
    ui_tooltip = "Optional material-mask helper. Disabled by default; bloom masks still work without depth.";
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
    ui_tooltip = "Optional inferred normal/surface field gate. AtmosphereBloom uses this only to suppress unstable bloom halos.";
> = 0.0;

uniform float Dalashade_NormalFieldStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Strength";
    ui_tooltip = "Global scale for optional NormalField bloom stabilization.";
> = 0.0;

uniform float Dalashade_NormalDepthStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Depth Strength";
    ui_tooltip = "Depth-normal contribution for optional bloom stability.";
> = 0.0;

uniform float Dalashade_NormalDetailStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Detail Strength";
    ui_tooltip = "Detail-normal contribution for optional bloom stability.";
> = 0.0;

uniform float Dalashade_NormalMaterialInfluence <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "NormalField Material Influence";
    ui_tooltip = "Material-aware scale for optional NormalField bloom stabilization.";
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

uniform float BloomStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Bloom Strength";
    ui_tooltip = "Manual overall bloom strength. Defaults are intentionally conservative.";
> = 0.32;

uniform float Dalashade_StandaloneStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Standalone Stack Strength";
    ui_tooltip = "0 keeps AtmosphereBloom supportive for an existing preset. 1 makes qualified glow sources more visible while preserving skin/highlight safety.";
> = 0.0;

uniform float BloomThreshold <
    ui_type = "slider";
    ui_min = 0.45; ui_max = 1.0;
    ui_label = "Bloom Threshold";
> = 0.74;

uniform float DiffusionStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Diffusion Strength";
    ui_tooltip = "Controls the small cheap blur radius used for bloom diffusion.";
> = 0.42;

uniform float CanopyGapBloomStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Canopy Gap Bloom Strength";
    ui_tooltip = "Bloom for bright sky/light visible through small foliage gaps. Uses local surround checks to avoid blooming the entire sky.";
> = 0.34;

uniform float MagicGlowStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Magic Glow Strength";
> = 0.48;

uniform float NeonGlowStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Neon Glow Strength";
> = 0.42;

uniform float HighlightRestraint <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Highlight Restraint";
    ui_tooltip = "Manual restraint for full-screen washout and bright highlight bloom.";
> = 0.70;

uniform float CombatDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Combat Dampen Strength";
> = 0.72;

uniform float CinematicBoostStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Cinematic Boost Strength";
> = 0.34;

uniform bool ShowDebugMask <
    ui_label = "Show Debug Mask";
    ui_tooltip = "Shows bloom source and restraint masks. Red is source, green is magic/neon, blue is restraint.";
> = false;

float Dalashade_AtmosphereBloomLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float Dalashade_AtmosphereBloomFoliageSurround(float3 sampleColor)
{
    float sampleLuma = Dalashade_AtmosphereBloomLuma(sampleColor);
    float greenLead = sampleColor.g - max(sampleColor.r, sampleColor.b) * 0.74;
    float organicDark = smoothstep(0.02, 0.20, greenLead) * (1.0 - smoothstep(0.42, 0.82, sampleLuma));
    float neutralLeaf = smoothstep(0.08, 0.32, sampleLuma) * smoothstep(0.06, 0.28, sampleColor.g + min(sampleColor.r, sampleColor.b) - sampleColor.b * 0.45);
    return saturate(max(organicDark, neutralLeaf * 0.46));
}

float Dalashade_AtmosphereBloomCanopyGapMask(float2 uv, float threshold, float canopyGlow, float materialSky, float highlightProtection, float combat, float sourceSafety)
{
    float3 center = tex2D(ReShade::BackBuffer, saturate(uv)).rgb;
    float centerLuma = Dalashade_AtmosphereBloomLuma(center);
    float centerChroma = max(max(center.r, center.g), center.b) - min(min(center.r, center.g), center.b);
    float2 nearTexel = BUFFER_PIXEL_SIZE * 1.35;
    float2 farTexel = BUFFER_PIXEL_SIZE * 3.20;

    float3 n1 = tex2D(ReShade::BackBuffer, saturate(uv + float2(nearTexel.x, 0.0))).rgb;
    float3 n2 = tex2D(ReShade::BackBuffer, saturate(uv - float2(nearTexel.x, 0.0))).rgb;
    float3 n3 = tex2D(ReShade::BackBuffer, saturate(uv + float2(0.0, nearTexel.y))).rgb;
    float3 n4 = tex2D(ReShade::BackBuffer, saturate(uv - float2(0.0, nearTexel.y))).rgb;
    float3 f1 = tex2D(ReShade::BackBuffer, saturate(uv + float2(farTexel.x, farTexel.y))).rgb;
    float3 f2 = tex2D(ReShade::BackBuffer, saturate(uv + float2(-farTexel.x, farTexel.y))).rgb;
    float3 f3 = tex2D(ReShade::BackBuffer, saturate(uv + float2(farTexel.x, -farTexel.y))).rgb;
    float3 f4 = tex2D(ReShade::BackBuffer, saturate(uv + float2(-farTexel.x, -farTexel.y))).rgb;

    float ringLuma = (Dalashade_AtmosphereBloomLuma(n1) + Dalashade_AtmosphereBloomLuma(n2) + Dalashade_AtmosphereBloomLuma(n3) + Dalashade_AtmosphereBloomLuma(n4)) * 0.25;
    float farLuma = (Dalashade_AtmosphereBloomLuma(f1) + Dalashade_AtmosphereBloomLuma(f2) + Dalashade_AtmosphereBloomLuma(f3) + Dalashade_AtmosphereBloomLuma(f4)) * 0.25;
    float foliageRing = (Dalashade_AtmosphereBloomFoliageSurround(n1) + Dalashade_AtmosphereBloomFoliageSurround(n2) + Dalashade_AtmosphereBloomFoliageSurround(n3) + Dalashade_AtmosphereBloomFoliageSurround(n4)
        + Dalashade_AtmosphereBloomFoliageSurround(f1) + Dalashade_AtmosphereBloomFoliageSurround(f2) + Dalashade_AtmosphereBloomFoliageSurround(f3) + Dalashade_AtmosphereBloomFoliageSurround(f4)) * 0.125;
    float ringTexture = abs(Dalashade_AtmosphereBloomLuma(n1) - Dalashade_AtmosphereBloomLuma(n2))
        + abs(Dalashade_AtmosphereBloomLuma(n3) - Dalashade_AtmosphereBloomLuma(n4))
        + abs(Dalashade_AtmosphereBloomLuma(f1) - Dalashade_AtmosphereBloomLuma(f4))
        + abs(Dalashade_AtmosphereBloomLuma(f2) - Dalashade_AtmosphereBloomLuma(f3));

    float brightOpening = smoothstep(threshold - 0.20, 0.98, centerLuma) * (1.0 - smoothstep(0.44, 0.82, centerChroma));
    float surroundedByLeaves = smoothstep(0.10, 0.42, foliageRing + ringTexture * 0.18);
    float localGapContrast = smoothstep(0.055, 0.34, centerLuma - ringLuma);
    float broadSkyReject = smoothstep(0.48, 0.82, ringLuma) * smoothstep(0.48, 0.84, farLuma) * (1.0 - smoothstep(0.08, 0.34, ringTexture + foliageRing));
    float skyPermission = saturate(0.36 + materialSky * 0.34 + canopyGlow * 0.30);
    float scenePermission = saturate(canopyGlow * (0.62 + skyPermission * 0.38) * (1.0 - combat * 0.58) * (1.0 - highlightProtection * 0.34) * sourceSafety);
    return saturate(brightOpening * surroundedByLeaves * localGapContrast * (1.0 - broadSkyReject) * scenePermission);
}

float3 Dalashade_AtmosphereBloomSource(
    float2 uv,
    float threshold,
    float highlightProtection,
    float magicGlow,
    float neonGlow,
    float canopyGlow,
    float wetness,
    float heat,
    float materialWaterPlane,
    float materialSpecularGlint,
    float materialCrystal,
    float materialNeon,
    float materialFire,
    float materialSky,
    float materialLightSource,
    float waterSourceContext,
    float skySourceContext,
    float sourceSafety,
    float night,
    float moonlight,
    float artificialLight,
    float ambientDarkness,
    float nightAtmosphere,
    float combat,
    float cinematic)
{
    float3 color = tex2D(ReShade::BackBuffer, uv).rgb;
    float depth = ReShade::GetLinearizedDepth(uv);
    float luma = Dalashade_AtmosphereBloomLuma(color);
    float sourceMask = smoothstep(threshold, 1.0, luma);
    float saturatedAccent = max(max(color.r, color.g), color.b) - min(min(color.r, color.g), color.b);
    float warmMask = smoothstep(0.03, 0.34, color.r - max(color.g, color.b) * 0.72);
    float coolAetherMask = smoothstep(0.06, 0.42, max(color.b, color.g) - color.r * 0.58);
    float neonColorMask = smoothstep(0.10, 0.55, saturatedAccent) * smoothstep(threshold - 0.22, 1.0, luma);
    float smoothSkyMask = max(materialSky, skySourceContext * 0.55) * smoothstep(0.44, 0.92, luma) * (1.0 - smoothstep(0.20, 0.62, saturatedAccent)) * smoothstep(0.18, 0.90, depth);
    float accentMask = smoothstep(threshold - 0.10, 1.0, luma) * max(max(magicGlow * 0.44, neonGlow * saturatedAccent * 0.52), canopyGlow * 0.30);
    float nightLocalLight = artificialLight * smoothstep(threshold - 0.16, 1.0, luma) * (0.42 + saturatedAccent * 0.34 + max(materialCrystal, materialNeon) * 0.24);
    float moonlitDiffusion = moonlight * nightAtmosphere * smoothstep(0.34, 0.78, luma) * smoothstep(0.22, 0.92, depth) * (1.0 - saturatedAccent * 0.35);
    float waterSurfaceGlow = max(materialWaterPlane, waterSourceContext * 0.35) * smoothstep(0.56, 0.90, luma) * smoothstep(0.12, 0.72, depth) * (1.0 - materialSky * 0.35);
    float wetSpecular = max(wetness * 0.72, materialSpecularGlint) * smoothstep(0.68, 0.98, luma) * smoothstep(0.035, 0.22, saturatedAccent + luma * 0.15);
    float aetherSource = materialCrystal * coolAetherMask * smoothstep(threshold - 0.18, 1.0, luma) * (0.45 + saturatedAccent * 0.55);
    float neonSource = materialNeon * neonColorMask;
    float fireSource = materialFire * warmMask * smoothstep(threshold - 0.16, 1.0, luma) * (1.0 - combat * 0.58);
    float qualifiedSource = materialLightSource * smoothstep(threshold - 0.20, 1.0, luma) * (0.30 + saturatedAccent * 0.35 + max(max(materialCrystal, materialNeon), materialFire) * 0.35);
    float heatDistance = max(heat, materialFire * 0.55) * smoothstep(0.26, 0.94, depth) * smoothstep(threshold - 0.14, 1.0, luma) * (1.0 - combat * 0.42);
    float skySource = smoothSkyMask * (0.14 + cinematic * 0.08 + nightAtmosphere * 0.05) * (1.0 - highlightProtection * 0.46);
    float canopyGapSource = Dalashade_AtmosphereBloomCanopyGapMask(uv, threshold, canopyGlow, materialSky, highlightProtection, combat, sourceSafety) * CanopyGapBloomStrength;
    float source = saturate(sourceMask + accentMask + canopyGapSource * 0.56 + nightLocalLight * 0.34 + moonlitDiffusion * 0.10 + waterSurfaceGlow * 0.08 + wetSpecular * 0.24 + aetherSource * 0.36 + neonSource * 0.30 + fireSource * 0.30 + qualifiedSource * 0.22 + heatDistance * 0.16 + skySource);
    float restraint = 1.0 - saturate((highlightProtection * 0.70 + ambientDarkness * night * 0.18 + materialWaterPlane * 0.08 + materialSpecularGlint * 0.20 + materialNeon * 0.18 + materialSky * 0.20) * smoothstep(0.76, 1.0, luma));
    return color * source * restraint * sourceSafety;
}

float4 Dalashade_AtmosphereBloomPS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    // Single-pass bright-source diffusion. This is intentionally cheap and bounded for gameplay use.
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    float atmosphere = saturate(Dalashade_Atmosphere);
    float magicGlow = saturate(Dalashade_MagicGlow);
    float neonGlow = saturate(Dalashade_NeonGlow);
    float foliage = saturate(Dalashade_FoliageDensity);
    float wetness = saturate(Dalashade_Wetness);
    float heat = saturate(Dalashade_Heat);
    float readability = saturate(Dalashade_Readability);
    float highlightProtection = saturate(Dalashade_HighlightProtection);
    float combat = saturate(Dalashade_CombatPressure);
    float cinematic = saturate(Dalashade_CinematicPermission);
    float night = saturate(Dalashade_Night);
    float moonlight = saturate(Dalashade_Moonlight);
    float artificialLight = saturate(Dalashade_ArtificialLight);
    float ambientDarkness = saturate(Dalashade_AmbientDarkness);
    float nightAtmosphere = saturate(Dalashade_NightAtmosphere);
    float daylight = saturate(Dalashade_Daylight);
    float sunlight = saturate(Dalashade_Sunlight);
    float dayAtmosphere = saturate(Dalashade_DayAtmosphere);
    float dayHighlightPressure = saturate(Dalashade_DayHighlightPressure);
    float materialWater = saturate(Dalashade_MaterialWaterSpecular);
    float materialWaterPlane = saturate(max(materialWater, Dalashade_MaterialWaterPlane));
    float materialSpecularGlint = saturate(max(materialWater, Dalashade_MaterialSpecularGlint));
    float materialWaterGate = saturate(max(materialWaterPlane, materialSpecularGlint));
    float materialFoliage = saturate(Dalashade_MaterialFoliage);
    float canopyGlow = max(foliage, materialFoliage) * (atmosphere + dayAtmosphere * 0.22 + daylight * 0.18 + sunlight * 0.10) * (1.0 - combat * 0.55);
    float materialCrystal = saturate(Dalashade_MaterialCrystalAether);
    float materialNeon = saturate(Dalashade_MaterialNeonGlass);
    float materialFire = saturate(Dalashade_MaterialFireLavaHeat);
    float materialSky = saturate(Dalashade_MaterialSkyCloudFog);
    float materialSkin = saturate(Dalashade_MaterialSkinProtection);
    Dalashade_FrameDataSettings frameSettings = Dalashade_FrameData_DefaultSettings();
    frameSettings.MaterialFoliage = max(foliage, materialFoliage);
    frameSettings.MaterialWaterSpecular = materialWater;
    frameSettings.MaterialWaterPlane = Dalashade_MaterialWaterPlane;
    frameSettings.MaterialSpecularGlint = Dalashade_MaterialSpecularGlint;
    frameSettings.MaterialSandDust = 0.0;
    frameSettings.MaterialSnowIce = 0.0;
    frameSettings.MaterialStoneRuins = 0.0;
    frameSettings.MaterialMetalIndustrial = 0.0;
    frameSettings.MaterialCrystalAether = materialCrystal;
    frameSettings.MaterialNeonGlass = materialNeon;
    frameSettings.MaterialFireLavaHeat = materialFire;
    frameSettings.MaterialSkyCloudFog = materialSky;
    frameSettings.MaterialSkinProtection = materialSkin;
    frameSettings.MaterialVoidDarkness = 0.0;
    frameSettings.WaterContext = materialWaterPlane;
    frameSettings.CoastalContext = materialWaterPlane;
    frameSettings.OpenOceanContext = materialWaterPlane;
    frameSettings.ShallowWaterContext = materialWaterPlane;
    frameSettings.WetSurfaceContext = wetness;
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
    materialWaterPlane = frame.WaterSurface;
    materialSpecularGlint = frame.WaterSpecularGlint;
    materialWaterGate = saturate(max(frame.WaterWetShoreline, materialSpecularGlint));
    materialFoliage = frame.MaterialFoliage;
    canopyGlow = max(foliage, materialFoliage) * (atmosphere + dayAtmosphere * 0.22 + daylight * 0.18 + sunlight * 0.10) * (1.0 - combat * 0.55);
    materialCrystal = frame.MaterialCrystalAether;
    materialNeon = frame.MaterialNeonGlass;
    materialFire = frame.MaterialFireLavaHeat;
    materialSky = saturate(max(frame.MaterialSkyCloudFog, frame.SafetySkyReject));
    float materialLightSource = saturate(frame.SourceLightConfidence);
    float waterSourceContext = saturate(frame.WaterSource);
    float skySourceContext = saturate(frame.WaterSkySource);
    float safetySourceRestraint = saturate(
        frame.SafetySkinReject * 0.90
        + frame.SafetyBrightSandProtect * 0.34
        + frame.SafetySnowProtect * 0.34
        + frame.SafetyFoliageNoiseReject * 0.24
        + frame.SafetyHighlightProtect * 0.18);
    float skyBloomRestraint = saturate(frame.SafetySkyReject * (0.20 + highlightProtection * 0.32 + dayHighlightPressure * 0.22));
    float normalFieldInfluence = saturate(max(Dalashade_NormalFieldEnabled * Dalashade_NormalFieldStrength * Dalashade_NormalMaterialInfluence, surface.SurfaceDataInfluence));
    float normalHaloRisk = saturate(normalFieldInfluence * surface.EdgeDiscontinuity * (1.0 - surface.NormalConfidence * 0.35));
    float normalStability = saturate(normalFieldInfluence * surface.StructureCandidate * surface.NormalConfidence);
    float sourceSafety = saturate(1.0 - safetySourceRestraint - skyBloomRestraint * 0.45 - normalHaloRisk * 0.38);

    float threshold = BloomThreshold + max(highlightProtection, dayHighlightPressure) * 0.135 + combat * 0.040 + readability * 0.030 + ambientDarkness * night * 0.035 + sunlight * daylight * 0.025;
    threshold -= max(max(magicGlow, neonGlow), max(materialCrystal, materialNeon) * 0.72) * 0.030;
    threshold -= artificialLight * 0.018;
    threshold -= moonlight * nightAtmosphere * 0.010;
    threshold -= max(wetness, materialWaterGate) * 0.018;
    threshold -= max(heat, materialFire) * 0.010;
    threshold += materialSky * max(highlightProtection, dayHighlightPressure) * 0.020;
    threshold = clamp(threshold, 0.58, 0.94);

    float2 texel = BUFFER_PIXEL_SIZE;
    float radius = lerp(1.0, 3.0, saturate(DiffusionStrength));
    float2 step1 = texel * radius;
    float2 step2 = texel * radius * 2.0;
    float bloomSampleQuality = saturate(Dalashade_BloomSampleQuality);

    // Selective bright-pass source: light emitters, wet highlights, canopy openings, and distant heat glow are favored over full-frame bloom.
    float3 bloom = Dalashade_AtmosphereBloomSource(texcoord, threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.26;
    if (bloomSampleQuality >= 0.45)
    {
        bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step1.x, 0.0), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.12;
        bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step1.x, 0.0), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.12;
        bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(0.0, step1.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.12;
        bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(0.0, -step1.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.12;
    }

    if (bloomSampleQuality >= 0.875)
    {
        bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step2.x, step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.065;
        bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step2.x, step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.065;
        bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step2.x, -step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.065;
        bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step2.x, -step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow, wetness, heat, materialWaterPlane, materialSpecularGlint, materialCrystal, materialNeon, materialFire, materialSky, materialLightSource, waterSourceContext, skySourceContext, sourceSafety, night, moonlight, artificialLight, ambientDarkness, nightAtmosphere, combat, cinematic) * 0.065;
    }

    float3 magicTint = float3(0.78, 0.58, 1.0);
    float3 neonTint = float3(0.48, 0.92, 1.0);
    float3 warmAtmosphereTint = float3(1.0, 0.90, 0.74);
    float3 canopyTint = float3(0.68, 0.92, 0.54);
    float3 glowTint = lerp(warmAtmosphereTint, magicTint, magicGlow * MagicGlowStrength);
    glowTint = lerp(glowTint, magicTint, materialCrystal * MagicGlowStrength * 0.42);
    glowTint = lerp(glowTint, neonTint, max(neonGlow, materialNeon) * NeonGlowStrength);
    glowTint = lerp(glowTint, canopyTint, canopyGlow * 0.30);
    glowTint = lerp(glowTint, float3(1.0, 0.78, 0.48), max(heat, materialFire) * 0.22);
    glowTint = lerp(glowTint, float3(0.82, 0.92, 1.0), max(wetness, materialWaterGate) * 0.16);
    glowTint = lerp(glowTint, float3(0.70, 0.82, 1.0), moonlight * 0.18);
    glowTint = lerp(glowTint, float3(1.0, 0.82, 0.58), artificialLight * 0.16);

    float combatDampen = 1.0 - saturate(combat * CombatDampenStrength);
    float cinematicBoost = 1.0 + cinematic * CinematicBoostStrength * (1.0 - combat * 0.65) * (0.86 + max(max(materialCrystal, materialNeon), materialFire) * 0.14);
    float readabilityDampen = 1.0 - readability * 0.22;
    float standaloneStrength = saturate(Dalashade_StandaloneStrength);
    float standaloneBloom = saturate(standaloneStrength * combatDampen * readabilityDampen * sourceSafety);
    float materialSelective = materialWaterGate * 0.04 + materialCrystal * 0.08 + materialNeon * 0.08 + materialFire * 0.05 + materialSky * 0.03 + materialLightSource * 0.035;
    float canopyGapCenterMask = Dalashade_AtmosphereBloomCanopyGapMask(texcoord, threshold, canopyGlow, materialSky, highlightProtection, combat, sourceSafety) * CanopyGapBloomStrength;
    float intentStrength = 0.40 + atmosphere * 0.20 + magicGlow * 0.22 + neonGlow * 0.22 + canopyGlow * 0.10 + canopyGapCenterMask * 0.08 + wetness * 0.08 + heat * 0.05 + artificialLight * 0.08 + moonlight * nightAtmosphere * 0.04 + dayAtmosphere * 0.04 + materialSelective;
    intentStrength *= lerp(1.0, 1.16, standaloneBloom);
    float strength = BloomStrength * intentStrength * combatDampen * cinematicBoost;
    strength *= readabilityDampen * (1.0 - saturate((highlightProtection + ambientDarkness * night * 0.20 + materialSky * 0.18 + materialWaterGate * 0.08 + materialNeon * 0.08 + safetySourceRestraint * 0.22 + normalHaloRisk * 0.18) * HighlightRestraint * 0.52));
    strength = clamp(strength, 0.0, 0.32 * lerp(1.0, 1.10, standaloneBloom));

    float luma = Dalashade_AtmosphereBloomLuma(color);
    float brightWashGuard = 1.0 - smoothstep(0.72, 1.0, luma) * saturate(highlightProtection * 0.50 + materialWaterGate * 0.12 + materialNeon * 0.16 + materialSky * 0.14 + safetySourceRestraint * 0.20);
    float3 glow = bloom * glowTint * strength * brightWashGuard;
    glow *= saturate(1.0 - normalHaloRisk * 0.28 + normalStability * 0.04);
    glow *= lerp(1.0, 1.18, standaloneBloom * saturate(materialLightSource + materialCrystal + materialNeon + materialFire + materialSpecularGlint));
    glow = min(glow, (0.18 + max(max(magicGlow, neonGlow), max(max(materialCrystal, materialNeon), canopyGapCenterMask)) * 0.05) * lerp(1.0, 1.12, standaloneBloom));

    float3 result = color + glow * (1.0 - color * 0.45);
    result = min(result, color + 0.16 * lerp(1.0, 1.10, standaloneBloom));
    result = saturate(result);

    if (ShowDebugMask)
    {
        float materialDebugStrength = saturate(Dalashade_MaterialDebugStrength);
        float saturatedAccent = max(max(color.r, color.g), color.b) - min(min(color.r, color.g), color.b);
        float warmMask = smoothstep(0.03, 0.34, color.r - max(color.g, color.b) * 0.72);
        float coolAetherMask = smoothstep(0.06, 0.42, max(color.b, color.g) - color.r * 0.58);
        float neonColorMask = smoothstep(0.10, 0.55, saturatedAccent) * smoothstep(threshold - 0.22, 1.0, luma);
        float skyMask = materialSky;
        float waterPlaneMask = frame.WaterSurface;
        float specularGlintMask = frame.WaterSpecularGlint;
        float waterMask = saturate(max(frame.WaterWetShoreline, frame.WaterSpecularGlint));
        float aetherMask = frame.MaterialCrystalAether * coolAetherMask * smoothstep(threshold - 0.18, 1.0, luma);
        float neonMask = frame.MaterialNeonGlass * neonColorMask;
        float fireMask = frame.MaterialFireLavaHeat * warmMask * smoothstep(threshold - 0.16, 1.0, luma) * (1.0 - combat * 0.58);
        float eligibility = saturate(Dalashade_AtmosphereBloomLuma(bloom) * 4.0);
        if (Dalashade_MaterialDebugMode == 1)
        {
            return float4(saturate(waterMask + fireMask) * materialDebugStrength, saturate(aetherMask + neonMask) * materialDebugStrength, saturate(skyMask + eligibility) * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 2)
        {
            return float4(waterPlaneMask * materialDebugStrength, specularGlintMask * materialDebugStrength, waterMask * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 3)
        {
            return float4(materialCrystal * materialDebugStrength, coolAetherMask * materialDebugStrength, aetherMask * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 4)
        {
            return float4(materialNeon * materialDebugStrength, neonColorMask * materialDebugStrength, neonMask * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 5)
        {
            return float4(materialFire * materialDebugStrength, fireMask * materialDebugStrength, combatDampen * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 6)
        {
            return float4(materialSky * materialDebugStrength, skyMask * materialDebugStrength, brightWashGuard * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 7)
        {
            return float4(eligibility * materialDebugStrength, strength * 4.0 * materialDebugStrength, sourceSafety * brightWashGuard * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 8)
        {
            return float4(materialWaterGate * materialDebugStrength, waterPlaneMask * materialDebugStrength, brightWashGuard * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 9)
        {
            return float4(materialWaterGate * materialDebugStrength, specularGlintMask * materialDebugStrength, wetness * materialDebugStrength, 1.0);
        }
        if (Dalashade_MaterialDebugMode == 10)
        {
            return float4(canopyGapCenterMask * materialDebugStrength, canopyGlow * materialDebugStrength, sourceSafety * materialDebugStrength, 1.0);
        }

        float sourceMask = saturate(Dalashade_AtmosphereBloomLuma(bloom) * 4.0);
        float accentMask = saturate(max(max(magicGlow, neonGlow), canopyGlow));
        float restraintMask = saturate(combat * 0.55 + highlightProtection * 0.45);
        return float4(sourceMask, accentMask, restraintMask, 1.0);
    }

    return float4(result, 1.0);
}

technique Dalashade_AtmosphereBloom
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = Dalashade_AtmosphereBloomPS;
    }
}
