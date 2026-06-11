#include "ReShade.fxh"

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

uniform float Dalashade_ManualStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Manual Overall Strength";
    ui_tooltip = "Manual fallback strength for testing without Dalashade. Keep low for gameplay.";
> = 0.35;

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
    ui_tooltip = "Visualizes the internal depth/haze/glow mask for tuning.";
> = false;

float Dalashade_Saturate(float value)
{
    return saturate(value);
}

float3 Dalashade_SafeLerp(float3 a, float3 b, float amount)
{
    return lerp(a, b, Dalashade_Saturate(amount));
}

float4 Dalashade_WeatherAtmospherePS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    float depth = ReShade::GetLinearizedDepth(texcoord);
    float distant = smoothstep(0.08, 0.92, depth);

    float haze = Dalashade_Saturate(max(Dalashade_Haze, Dalashade_ManualHazeBoost));
    float wetness = Dalashade_Saturate(Dalashade_Wetness);
    float cold = Dalashade_Saturate(Dalashade_Cold);
    float heat = Dalashade_Saturate(Dalashade_Heat);
    float highlightProtection = Dalashade_Saturate(Dalashade_HighlightProtection);
    float shadowProtection = Dalashade_Saturate(Dalashade_ShadowProtection);
    float combat = Dalashade_Saturate(Dalashade_CombatPressure);
    float atmosphere = Dalashade_Saturate(Dalashade_Atmosphere);
    float magicGlow = Dalashade_Saturate(Dalashade_MagicGlow);
    float neonGlow = Dalashade_Saturate(Dalashade_NeonGlow);
    float manualStrength = Dalashade_Saturate(Dalashade_ManualStrength);

    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
    float brightMask = smoothstep(0.62, 0.98, luma);
    float shadowMask = 1.0 - smoothstep(0.08, 0.35, luma);

    float weatherAmount = max(max(haze, wetness * 0.55), max(cold * 0.50, heat * 0.55));
    float depthHaze = distant * weatherAmount * (0.10 + atmosphere * 0.12);
    depthHaze *= 1.0 - (combat * 0.45);
    depthHaze = min(depthHaze, 0.22);

    float3 hazeTint = float3(0.63, 0.68, 0.72);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.78, 0.86, 1.00), cold * 0.35);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(1.00, 0.78, 0.56), heat * 0.35);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.56, 0.58, 0.66), Dalashade_ManualMood * 0.35);

    float3 result = Dalashade_SafeLerp(color, hazeTint, depthHaze * manualStrength);

    float glowIntent = max(max(wetness * 0.35, haze * 0.28), max(magicGlow * 0.32, neonGlow * 0.28));
    glowIntent = max(glowIntent, Dalashade_ManualGlowBoost * 0.35);
    glowIntent *= 1.0 - (combat * 0.50);
    float glowAmount = min(brightMask * glowIntent * (0.06 + atmosphere * 0.06), 0.12);
    float3 glowTint = Dalashade_SafeLerp(float3(1.0, 1.0, 1.0), float3(0.72, 0.90, 1.0), max(neonGlow, cold) * 0.35);
    glowTint = Dalashade_SafeLerp(glowTint, float3(1.0, 0.82, 0.55), heat * 0.30);
    result += glowTint * glowAmount * manualStrength;

    float stormMood = Dalashade_Saturate(Dalashade_ManualMood + wetness * haze * 0.55);
    float moodDarken = stormMood * (0.025 + combat * 0.015);
    result *= 1.0 - moodDarken;

    float highlightRollOff = highlightProtection * brightMask * (0.06 + cold * 0.06 + heat * 0.035);
    result = lerp(result, result / (1.0 + result), min(highlightRollOff * manualStrength, 0.18));

    float shadowLift = shadowProtection * shadowMask * 0.035 * (1.0 - combat * 0.20);
    result += shadowLift * manualStrength;

    float heatShimmerSoftness = heat * distant * 0.025 * (1.0 - combat * 0.45);
    result = Dalashade_SafeLerp(result, float3(luma, luma, luma), heatShimmerSoftness * manualStrength);

    result = min(result, color + 0.18);
    result = max(result, color - 0.16);
    result = saturate(result);

    if (Dalashade_ShowDebugMask)
    {
        return float4(depthHaze, glowAmount, highlightRollOff, 1.0);
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
