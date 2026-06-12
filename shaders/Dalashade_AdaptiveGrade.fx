#include "ReShade.fxh"

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

uniform float Dalashade_MaterialFoliage <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Material Foliage";
    ui_tooltip = "Inferred foliage likelihood. Supports richer greens while preserving dark trunks.";
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

uniform float Dalashade_ManualStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Manual Overall Strength";
    ui_tooltip = "Manual fallback strength for testing without Dalashade. Defaults are intentionally subtle.";
> = 0.35;

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
    float materialFoliage = saturate(Dalashade_MaterialFoliage);
    float materialSandDust = saturate(Dalashade_MaterialSandDust);
    float materialSnowIce = saturate(Dalashade_MaterialSnowIce);
    float materialMetalIndustrial = saturate(Dalashade_MaterialMetalIndustrial);
    float materialCrystal = saturate(Dalashade_MaterialCrystalAether);
    float materialSkin = saturate(Dalashade_MaterialSkinProtection);
    float materialVoid = saturate(Dalashade_MaterialVoidDarkness);
    float manualStrength = saturate(Dalashade_ManualStrength);
    float safety = 1.0 - saturate(readability * 0.42 + combat * 0.58);
    float foliageRichness = max(foliage, materialFoliage) * atmosphere * safety;
    float heatIdentity = max(heat, materialSandDust);
    float coldIdentity = max(cold, materialSnowIce);
    float hardSurfaceIdentity = max(industrial, materialMetalIndustrial);
    float aetherIdentity = max(magicGlow, materialCrystal);
    float authoredIdentity = max(max(foliage, max(heat, cold)), max(max(neonGlow, magicGlow), max(industrial, cosmic)));
    float gradeStrength = manualStrength * (0.44 + atmosphere * 0.18 + cinematic * 0.18 + authoredIdentity * 0.08) * (0.55 + safety * 0.45);

    float luma = Dalashade_Luma(source);
    float highlightMask = smoothstep(0.62, 0.98, luma);
    float shadowMask = 1.0 - smoothstep(0.05, 0.34, luma);

    // Build a mild intent-driven grade target. Manual controls add test pressure, not a new architecture path.
    float exposureTrim = Dalashade_ManualExposure
        + (shadowProtection * 0.020)
        - (highlightProtection * 0.026)
        - (combat * 0.010)
        - (hardSurfaceIdentity * 0.006)
        - (cosmic * 0.004);
    float contrastAmount = Dalashade_ManualContrast
        + (cinematic * safety * 0.052)
        + (atmosphere * safety * 0.018)
        + (foliageRichness * 0.014)
        + (hardSurfaceIdentity * safety * 0.045)
        + (cosmic * safety * 0.020)
        - (readability * 0.026);
    float saturationAmount = Dalashade_ManualSaturation
        + (cinematic * safety * 0.035)
        + (max(aetherIdentity, neonGlow) * safety * 0.025)
        + (foliageRichness * 0.030)
        + (cosmic * safety * 0.014)
        - (hardSurfaceIdentity * 0.040)
        - (materialSkin * 0.020)
        - (readability * 0.032)
        - (combat * 0.026);
    float temperature = Dalashade_ManualTemperature + (heatIdentity * 0.074) - (coldIdentity * 0.065) - (cosmic * 0.040) - (hardSurfaceIdentity * 0.018);
    float tint = Dalashade_ManualTint + (aetherIdentity * 0.030) - (neonGlow * 0.012) + (cosmic * 0.020) - (hardSurfaceIdentity * 0.010);

    float skinTintGuard = 1.0 - materialSkin * 0.34;
    saturationAmount *= 1.0 - materialSkin * 0.24;
    temperature *= skinTintGuard;
    tint *= skinTintGuard;

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
    float foliageColor = foliageRichness * greenSignal * (1.0 - highlightMask * 0.45);
    graded = lerp(graded, graded * float3(0.965, 1.055, 0.940), foliageColor * 0.18);
    graded = lerp(graded, graded * float3(1.038, 1.008, 0.948), heatIdentity * warmSignal * 0.048 * safety * (1.0 - materialSkin * 0.35));
    graded = lerp(graded, graded * float3(0.940, 0.970, 1.055), max(coldIdentity, cosmic) * coolSignal * 0.060 * safety);
    graded = lerp(graded, float3(Dalashade_Luma(graded), Dalashade_Luma(graded), Dalashade_Luma(graded)), hardSurfaceIdentity * 0.045);
    graded = lerp(graded, graded * float3(0.982, 0.995, 1.025), materialMetalIndustrial * coolSignal * 0.040 * safety);
    graded = lerp(graded, graded * float3(0.985, 0.978, 1.035), materialCrystal * max(coolSignal, greenSignal) * 0.034 * safety);

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
    float rolloff = min((highlightProtection * 0.17 + coldIdentity * 0.060 + heatIdentity * 0.040 + hardSurfaceIdentity * 0.018 + cosmic * 0.020) * highlightMask, 0.25);
    graded = lerp(graded, graded / (1.0 + graded), rolloff * manualStrength);

    float selectiveShadowLift = (0.060 - combat * 0.022) * (1.0 - max(foliage, materialFoliage) * 0.38) * (1.0 - hardSurfaceIdentity * 0.22) * (1.0 - materialVoid * 0.52);
    float lift = min(shadowProtection * shadowMask * selectiveShadowLift, 0.072);
    graded += lift * manualStrength * (1.0 - source);

    // Preserve black depth in forests, industrial zones, and gloom-heavy scenes by recovering contrast in the deepest shadows.
    float blackDepth = shadowMask * (max(foliage, materialFoliage) * 0.040 + hardSurfaceIdentity * 0.030 + cosmic * 0.014 + materialVoid * 0.060) * (1.0 - combat * 0.45);
    graded = lerp(graded, graded * (1.0 - blackDepth), saturate(1.0 - readability * 0.40));

    // Skin protection reins in extreme shifts on smooth warm midtones without flattening the whole grade.
    float skinLikeMidtone = materialSkin
        * smoothstep(0.18, 0.58, luma)
        * (1.0 - smoothstep(0.78, 0.98, luma))
        * smoothstep(0.02, 0.22, source.r - source.b)
        * (1.0 - smoothstep(0.10, 0.42, max(max(source.r, source.g), source.b) - min(min(source.r, source.g), source.b)));
    graded = lerp(graded, source + (graded - source) * 0.62, skinLikeMidtone * 0.50);

    // Guardrails prevent the grade from crushing or blowing out relative to the input.
    graded = min(graded, source + 0.18);
    graded = max(graded, source - 0.16);
    graded = clamp(graded, 0.015, 0.985);

    float3 result = lerp(source, graded, saturate(gradeStrength));

    if (Dalashade_ShowDebugMask)
    {
        return float4(saturate(rolloff * 5.0), saturate(lift * 8.0), saturate(cinematicBias * 12.0 + (1.0 - safety) * 0.25), 1.0);
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
