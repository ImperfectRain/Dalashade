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

uniform float Dalashade_CinematicPermission <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Dalashade Cinematic Permission";
    ui_tooltip = "Scene-driven permission for stronger atmosphere outside gameplay-critical moments.";
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
    float cinematic = Dalashade_Saturate(Dalashade_CinematicPermission);
    float manualStrength = Dalashade_Saturate(Dalashade_ManualStrength);
    float manualMood = Dalashade_Saturate(Dalashade_ManualMood);
    float manualGlow = Dalashade_Saturate(Dalashade_ManualGlowBoost);

    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
    float brightMask = smoothstep(0.54, 0.96, luma);
    float specularMask = smoothstep(0.72, 1.0, luma);
    float shadowMask = 1.0 - smoothstep(0.08, 0.35, luma);

    // Gameplay pressure is the main safety valve. Combat should visibly cut heavy weather while retaining light mood.
    float gameplayDampen = 1.0 - saturate(combat * 0.62 + readability * 0.18);
    float cinematicBoost = 1.0 + cinematic * 0.12;

    // Depth haze is bounded and mostly pushed into distance. Foliage-heavy scenes avoid gray veil.
    float weatherAmount = max(max(haze, wetness * 0.62), max(cold * 0.58, heat * 0.68));
    float dustSoftness = heat * (0.45 + haze * 0.35);
    float distanceWeight = lerp(distant * 0.78 + midDistance * 0.22, distant * 0.94 + midDistance * 0.06, heat);
    float foliageHazeRestraint = 1.0 - foliage * atmosphere * 0.32;
    float depthHaze = distanceWeight * weatherAmount * foliageHazeRestraint;
    depthHaze *= (0.16 + atmosphere * 0.18 + dustSoftness * 0.06) * gameplayDampen * cinematicBoost;
    depthHaze = min(depthHaze, 0.28);

    float3 hazeTint = float3(0.63, 0.68, 0.72);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.78, 0.87, 1.00), cold * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(1.00, 0.76, 0.50), heat * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.54, 0.57, 0.66), manualMood * 0.40);

    float3 result = Dalashade_SafeLerp(color, hazeTint, depthHaze * manualStrength);

    // Rain and wet scenes get more visible specular glow, especially in bright highlights.
    float rainGlow = max(wetness * 0.54, specularMask * wetness * 0.78);
    float glowIntent = max(max(rainGlow, haze * 0.32), max(magicGlow * 0.40, neonGlow * 0.35));
    glowIntent = max(glowIntent, manualGlow * 0.45);
    glowIntent *= gameplayDampen;
    float glowAmount = min((brightMask * 0.75 + specularMask * 0.45) * glowIntent * (0.08 + atmosphere * 0.08), 0.18);
    float3 glowTint = Dalashade_SafeLerp(float3(1.0, 1.0, 1.0), float3(0.72, 0.90, 1.0), max(neonGlow, cold) * 0.35);
    glowTint = Dalashade_SafeLerp(glowTint, float3(1.0, 0.82, 0.55), heat * 0.30);
    result = Dalashade_SoftLighten(result, glowTint, glowAmount * manualStrength);

    // Dense rainforest canopies get a tiny green-gold sky-light response on bright openings, not a full-frame wash.
    float canopyLight = foliage * atmosphere * gameplayDampen * smoothstep(0.46, 0.88, luma);
    canopyLight *= 0.026 + max(magicGlow, cinematic) * 0.014;
    float3 canopyTint = float3(0.60, 0.86, 0.48);
    result = Dalashade_SoftLighten(result, canopyTint, min(canopyLight * manualStrength, 0.055));

    // Storm mood gently darkens and cools wet haze; combat dampens it instead of making fights darker.
    float stormMood = Dalashade_Saturate(manualMood + wetness * max(haze, atmosphere) * 0.82);
    float moodDarken = stormMood * (0.045 + haze * 0.035) * (1.0 - combat * 0.52);
    result *= 1.0 - moodDarken;
    result = Dalashade_SafeLerp(result, result * float3(0.90, 0.93, 1.0), stormMood * 0.10 * manualStrength);

    // Snow/cold scenes protect bright whites without flattening the whole frame.
    float snowHighlightGuard = cold * max(highlightProtection, brightMask);
    float highlightRollOff = highlightProtection * brightMask * (0.08 + cold * 0.10 + heat * 0.045);
    highlightRollOff = max(highlightRollOff, snowHighlightGuard * specularMask * 0.10);
    result = lerp(result, result / (1.0 + result), min(highlightRollOff * manualStrength, 0.24));

    // Shadow lift stays modest and is reduced in combat to avoid muddy encounters.
    float shadowLift = shadowProtection * shadowMask * 0.034 * (1.0 - combat * 0.35) * (1.0 - foliage * 0.34);
    result += shadowLift * manualStrength;

    // Heat/dust softness is distance-weighted so night desert scenes do not get a full-screen lift.
    float heatShimmerSoftness = heat * distant * distant * (0.040 + haze * 0.020) * gameplayDampen;
    float warmLuma = dot(result, float3(0.26, 0.67, 0.07));
    result = Dalashade_SafeLerp(result, float3(warmLuma, warmLuma, warmLuma), heatShimmerSoftness * manualStrength);

    // Final guardrails keep the shader visible but prevent large grade swings.
    result = min(result, color + 0.24);
    result = max(result, color - 0.20);
    result = saturate(result);

    if (Dalashade_ShowDebugMask)
    {
        // Red: depth haze, green: glow/canopy light, blue: protection/readability pressure.
        float debugProtection = saturate(max(highlightRollOff, shadowLift * 3.0) + combat * 0.18);
        return float4(saturate(depthHaze * 3.2), saturate(glowAmount * 5.0 + canopyLight * 8.0), debugProtection, 1.0);
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
