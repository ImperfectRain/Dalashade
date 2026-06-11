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
    float cinematic = saturate(Dalashade_CinematicPermission);
    float manualStrength = saturate(Dalashade_ManualStrength);
    float safety = 1.0 - saturate(readability * 0.42 + combat * 0.58);
    float gradeStrength = manualStrength * (0.42 + atmosphere * 0.20 + cinematic * 0.20) * (0.55 + safety * 0.45);

    float luma = Dalashade_Luma(source);
    float highlightMask = smoothstep(0.62, 0.98, luma);
    float shadowMask = 1.0 - smoothstep(0.05, 0.34, luma);

    // Build a mild intent-driven grade target. Manual controls add test pressure, not a new architecture path.
    float exposureTrim = Dalashade_ManualExposure
        + (shadowProtection * 0.020)
        - (highlightProtection * 0.026)
        - (combat * 0.010);
    float contrastAmount = Dalashade_ManualContrast
        + (cinematic * safety * 0.052)
        + (atmosphere * safety * 0.018)
        - (readability * 0.026);
    float saturationAmount = Dalashade_ManualSaturation
        + (cinematic * safety * 0.035)
        + (max(magicGlow, neonGlow) * safety * 0.025)
        - (readability * 0.032)
        - (combat * 0.026);
    float temperature = Dalashade_ManualTemperature + (heat * 0.070) - (cold * 0.065);
    float tint = Dalashade_ManualTint + (magicGlow * 0.030) - (neonGlow * 0.012);

    exposureTrim = clamp(exposureTrim, -0.085, 0.075);
    contrastAmount = clamp(contrastAmount, -0.085, 0.115);
    saturationAmount = clamp(saturationAmount, -0.095, 0.115);
    temperature = clamp(temperature, -0.110, 0.120);
    tint = clamp(tint, -0.075, 0.075);

    float3 graded = source + exposureTrim;
    graded = Dalashade_SafeContrast(graded, contrastAmount);
    graded = Dalashade_SafeSaturation(graded, saturationAmount);
    graded = Dalashade_TemperatureTint(graded, temperature, tint);

    // Cinematic bias is intentionally small and automatically weakens under gameplay pressure.
    float3 cinematicTint = lerp(float3(1.0, 0.985, 0.955), float3(0.955, 0.985, 1.0), cold);
    cinematicTint = lerp(cinematicTint, float3(1.0, 0.962, 0.912), heat * 0.65);
    cinematicTint = lerp(cinematicTint, float3(0.95, 0.98, 1.04), neonGlow * 0.30);
    float cinematicBias = cinematic * safety * (0.025 + atmosphere * 0.015);
    graded = lerp(graded, graded * cinematicTint, cinematicBias);

    // Highlight and shadow protection keep the grade usable in gameplay and bright weather.
    float rolloff = min((highlightProtection * 0.15 + cold * 0.05 + heat * 0.035) * highlightMask, 0.20);
    graded = lerp(graded, graded / (1.0 + graded), rolloff * manualStrength);

    float lift = min(shadowProtection * shadowMask * (0.060 - combat * 0.022), 0.075);
    graded += lift * manualStrength * (1.0 - source);

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
