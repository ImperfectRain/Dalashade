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

    // Atmospheric perspective: fog/mist and dust thicken with distance; foreground gameplay space stays mostly untouched.
    float weatherAmount = max(max(haze, wetness * 0.62), max(cold * 0.58, heat * 0.68));
    float dustSoftness = heat * (0.50 + haze * 0.28);
    float fogLike = saturate(haze * (1.0 - heat * 0.28));
    float heatDistance = smoothstep(0.26, 0.96, depth);
    float distanceWeight = lerp(distant * 0.72 + midDistance * 0.18, heatDistance * heatDistance, heat);
    float foliageHazeRestraint = 1.0 - foliage * atmosphere * 0.46;
    float depthHaze = distanceWeight * weatherAmount * foliageHazeRestraint;
    depthHaze *= (0.15 + atmosphere * 0.16 + fogLike * 0.07 + dustSoftness * 0.08) * gameplayDampen * cinematicBoost;
    depthHaze = min(depthHaze, lerp(0.22, 0.31, saturate(fogLike + heat * 0.45)));

    float3 hazeTint = float3(0.63, 0.68, 0.72);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.78, 0.87, 1.00), cold * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(1.00, 0.76, 0.50), heat * 0.42);
    hazeTint = Dalashade_SafeLerp(hazeTint, float3(0.54, 0.57, 0.66), manualMood * 0.40);

    float3 result = Dalashade_SafeLerp(color, hazeTint, depthHaze * manualStrength);

    // Wet air scattering: rain/wetness softens bright wet highlights without lifting the entire scene.
    float rainGlow = max(wetness * 0.54, specularMask * wetness * 0.78);
    float glowIntent = max(max(rainGlow, haze * 0.32), max(magicGlow * 0.40, neonGlow * 0.35));
    glowIntent = max(glowIntent, manualGlow * 0.45);
    glowIntent *= gameplayDampen;
    float glowAmount = min((brightMask * 0.70 + specularMask * 0.55) * glowIntent * (0.085 + atmosphere * 0.075), 0.18);
    float3 glowTint = Dalashade_SafeLerp(float3(1.0, 1.0, 1.0), float3(0.72, 0.90, 1.0), max(neonGlow, cold) * 0.35);
    glowTint = Dalashade_SafeLerp(glowTint, float3(1.0, 0.82, 0.55), heat * 0.30);
    result = Dalashade_SoftLighten(result, glowTint, glowAmount * manualStrength);

    // Dense rainforest canopies get local green-gold sky light on bright openings, while haze and shadow lift are restrained.
    float canopyOpenings = smoothstep(0.50, 0.90, luma) * (1.0 - shadowMask * 0.70);
    float canopyLight = foliage * atmosphere * gameplayDampen * canopyOpenings;
    canopyLight *= 0.032 + max(magicGlow, cinematic) * 0.016;
    float3 canopyTint = float3(0.60, 0.86, 0.48);
    result = Dalashade_SoftLighten(result, canopyTint, min(canopyLight * manualStrength, 0.055));

    // Gloom/storm mood darkens and cools the scene; it is not fog, so it preserves black depth.
    float stormMood = Dalashade_Saturate(manualMood + wetness * max(haze, atmosphere) * 0.82);
    float moodDarken = stormMood * (0.045 + haze * 0.035) * (1.0 - combat * 0.52);
    result *= 1.0 - moodDarken;
    result = Dalashade_SafeLerp(result, result * float3(0.90, 0.93, 1.0), stormMood * 0.10 * manualStrength);

    // Highlight shoulder: bright sand, clouds, snow, and specular water roll off before bloom/grade can clip them.
    float snowHighlightGuard = cold * max(highlightProtection, brightMask);
    float coastalGlareGuard = highlightProtection * brightMask * (1.0 - wetness * 0.25) * (0.035 + atmosphere * 0.025);
    float highlightRollOff = highlightProtection * brightMask * (0.09 + cold * 0.11 + heat * 0.055);
    highlightRollOff = max(highlightRollOff, snowHighlightGuard * specularMask * 0.10);
    highlightRollOff = max(highlightRollOff, coastalGlareGuard);
    result = lerp(result, result / (1.0 + result), min(highlightRollOff * manualStrength, 0.27));

    // Shadow lift stays selective; foliage-heavy and gloomy scenes keep trunks/background dark instead of milky.
    float shadowLift = shadowProtection * shadowMask * 0.032 * (1.0 - combat * 0.35) * (1.0 - foliage * 0.46);
    result += shadowLift * manualStrength;

    // Heat/dust softness is distance-weighted so night desert scenes get air thickness, not a full-screen lift.
    float heatShimmerSoftness = heat * heatDistance * heatDistance * (0.050 + haze * 0.018) * gameplayDampen;
    float warmLuma = dot(result, float3(0.26, 0.67, 0.07));
    result = Dalashade_SafeLerp(result, float3(warmLuma, warmLuma, warmLuma), heatShimmerSoftness * manualStrength);

    // Final guardrails keep the shader visible but prevent large grade swings.
    result = min(result, color + 0.24);
    result = max(result, color - 0.20);
    result = saturate(result);

    if (Dalashade_ShowDebugMask)
    {
        float foliageDampen = saturate(foliage * atmosphere * 0.85);
        float heatDust = saturate(heatShimmerSoftness * 8.0 + heat * heatDistance * 0.35);
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
