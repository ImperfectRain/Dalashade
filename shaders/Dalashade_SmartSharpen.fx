#include "ReShade.fxh"

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

uniform float SharpenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Sharpen Strength";
    ui_tooltip = "Overall conservative sharpen strength. This is clarity sharpening, not anti-aliasing.";
> = 0.36;

uniform float EdgeClarityStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Edge Clarity Strength";
> = 0.42;

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

uniform float HighlightDampenStrength <
    ui_type = "slider";
    ui_min = 0.0; ui_max = 1.0;
    ui_label = "Highlight Dampen Strength";
> = 0.72;

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

uniform bool ShowDebugMask <
    ui_label = "Show Debug Mask";
    ui_tooltip = "Red shows sharpen mask, green shows edge clarity, blue shows dampening pressure.";
> = false;

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
    float3 blur = (north + south + east + west) * 0.25;

    float centerLuma = Dalashade_SmartSharpenLuma(center);
    float blurLuma = Dalashade_SmartSharpenLuma(blur);
    float detailLuma = abs(centerLuma - blurLuma);
    float edgeMask = smoothstep(0.010, 0.115, detailLuma);
    float microDetailMask = 1.0 - smoothstep(0.070, 0.180, detailLuma);

    // Intent masks reduce sharpening where clarity usually becomes shimmer, halos, or combat clutter.
    float readability = saturate(Dalashade_Readability);
    float haze = saturate(Dalashade_Haze);
    float wetness = saturate(Dalashade_Wetness);
    float foliage = saturate(Dalashade_FoliageDensity);
    float combat = saturate(Dalashade_CombatPressure);
    float highlightProtection = saturate(Dalashade_HighlightProtection);

    float brightMask = smoothstep(0.62, 0.96, centerLuma);
    float specularEdgeMask = brightMask * smoothstep(0.018, 0.090, detailLuma);
    float depth = ReShade::GetLinearizedDepth(texcoord);
    float farDepthMask = smoothstep(0.20, 0.92, depth);

    float hazePressure = saturate(max(haze, wetness * 0.70) * HazeDampenStrength);
    float foliagePressure = saturate(foliage * (0.30 + microDetailMask * 0.55));
    float highlightPressure = saturate((highlightProtection * 0.75 + wetness * 0.28) * specularEdgeMask * HighlightDampenStrength);
    float depthPressure = saturate(farDepthMask * DepthDampenStrength * (0.45 + haze * 0.55));
    float combatPressure = saturate(combat * CombatDampenStrength);
    float dampen = saturate(hazePressure + foliagePressure * 0.38 + highlightPressure + depthPressure * 0.55 + combatPressure);

    // Readability can add edge clarity, but the same safety masks keep it from crunching fog, leaves, or bright halos.
    float readableEdgeBoost = readability * EdgeClarityStrength * edgeMask * (1.0 - saturate(haze * 0.55 + combat * 0.30));
    float textureDetail = TextureDetailStrength * microDetailMask * (1.0 - saturate(foliage * 0.62 + haze * 0.45 + wetness * 0.30));
    float sharpenAmount = SharpenStrength * (0.30 + readableEdgeBoost * 0.38 + textureDetail * 0.22);
    sharpenAmount *= 1.0 - dampen * 0.86;
    sharpenAmount = clamp(sharpenAmount, 0.0, 0.34);

    float3 detail = center - blur;

    // Anti-crunch limits high-contrast dark/light edge pushes before they become harsh outlines.
    float crunchGuard = smoothstep(0.12, 0.34, abs(detailLuma)) * AntiCrunchStrength;
    float detailLimit = lerp(0.085, 0.035, crunchGuard + highlightPressure * 0.65);
    detail = clamp(detail, -detailLimit, detailLimit);

    float3 sharpened = center + detail * sharpenAmount;

    // Final delta guardrail keeps the prototype subtle even with manual sliders raised.
    sharpened = min(sharpened, center + 0.075);
    sharpened = max(sharpened, center - 0.075);
    sharpened = saturate(sharpened);

    if (ShowDebugMask)
    {
        return float4(saturate(sharpenAmount * 3.0), saturate(readableEdgeBoost * 2.4), saturate(dampen), 1.0);
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
