#include "ReShade.fxh"

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

uniform float BloomStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Bloom Strength";
    ui_tooltip = "Manual overall bloom strength. Defaults are intentionally conservative.";
> = 0.32;

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

float3 Dalashade_AtmosphereBloomSource(float2 uv, float threshold, float highlightProtection, float magicGlow, float neonGlow, float canopyGlow)
{
    float3 color = tex2D(ReShade::BackBuffer, uv).rgb;
    float luma = Dalashade_AtmosphereBloomLuma(color);
    float sourceMask = smoothstep(threshold, 1.0, luma);
    float accentMask = smoothstep(threshold - 0.08, 1.0, luma) * max(max(magicGlow * 0.42, neonGlow * 0.36), canopyGlow * 0.28);
    float restraint = 1.0 - saturate(highlightProtection * smoothstep(0.78, 1.0, luma) * 0.65);
    return color * saturate(sourceMask + accentMask) * restraint;
}

float4 Dalashade_AtmosphereBloomPS(float4 position : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
    // Single-pass bright-source diffusion. This is intentionally cheap and bounded for gameplay use.
    float3 color = tex2D(ReShade::BackBuffer, texcoord).rgb;
    float atmosphere = saturate(Dalashade_Atmosphere);
    float magicGlow = saturate(Dalashade_MagicGlow);
    float neonGlow = saturate(Dalashade_NeonGlow);
    float foliage = saturate(Dalashade_FoliageDensity);
    float highlightProtection = saturate(Dalashade_HighlightProtection);
    float combat = saturate(Dalashade_CombatPressure);
    float cinematic = saturate(Dalashade_CinematicPermission);
    float canopyGlow = foliage * atmosphere * (1.0 - combat * 0.55);

    float threshold = BloomThreshold + highlightProtection * 0.105 + combat * 0.035;
    threshold -= max(magicGlow, neonGlow) * 0.035;
    threshold = clamp(threshold, 0.58, 0.94);

    float2 texel = BUFFER_PIXEL_SIZE;
    float radius = lerp(1.0, 3.0, saturate(DiffusionStrength));
    float2 step1 = texel * radius;
    float2 step2 = texel * radius * 2.0;

    float3 bloom = Dalashade_AtmosphereBloomSource(texcoord, threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.26;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step1.x, 0.0), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.12;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step1.x, 0.0), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.12;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(0.0, step1.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.12;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(0.0, -step1.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.12;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step2.x, step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.065;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step2.x, step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.065;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(step2.x, -step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.065;
    bloom += Dalashade_AtmosphereBloomSource(texcoord + float2(-step2.x, -step2.y), threshold, highlightProtection, magicGlow, neonGlow, canopyGlow) * 0.065;

    float3 magicTint = float3(0.78, 0.58, 1.0);
    float3 neonTint = float3(0.48, 0.92, 1.0);
    float3 warmAtmosphereTint = float3(1.0, 0.90, 0.74);
    float3 canopyTint = float3(0.68, 0.92, 0.54);
    float3 glowTint = lerp(warmAtmosphereTint, magicTint, magicGlow * MagicGlowStrength);
    glowTint = lerp(glowTint, neonTint, neonGlow * NeonGlowStrength);
    glowTint = lerp(glowTint, canopyTint, canopyGlow * 0.30);

    float combatDampen = 1.0 - saturate(combat * CombatDampenStrength);
    float cinematicBoost = 1.0 + cinematic * CinematicBoostStrength * (1.0 - combat * 0.65);
    float intentStrength = 0.42 + atmosphere * 0.22 + magicGlow * 0.20 + neonGlow * 0.18 + canopyGlow * 0.08;
    float strength = BloomStrength * intentStrength * combatDampen * cinematicBoost;
    strength *= 1.0 - saturate(highlightProtection * HighlightRestraint * 0.45);
    strength = clamp(strength, 0.0, 0.32);

    float luma = Dalashade_AtmosphereBloomLuma(color);
    float brightWashGuard = 1.0 - smoothstep(0.72, 1.0, luma) * highlightProtection * 0.50;
    float3 glow = bloom * glowTint * strength * brightWashGuard;
    glow = min(glow, 0.18 + max(magicGlow, neonGlow) * 0.05);

    float3 result = color + glow * (1.0 - color * 0.45);
    result = min(result, color + 0.16);
    result = saturate(result);

    if (ShowDebugMask)
    {
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
