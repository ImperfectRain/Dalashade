# Compatibility And Diagnostics

This page documents how Dalashade reports preset compatibility and generation diagnostics.

## Implemented Files

| File | Owns |
| --- | --- |
| `Dalashade/PresetAnalyzer.cs` | Preset parsing, activation state, support classification, risk classification, authorities, warnings. |
| `Dalashade/CompatibilityReportExporter.cs` | Markdown compatibility report export. |
| `Dalashade/PresetRegressionReportHarness.cs` | Batch preset regression report generation. |
| `Dalashade/PresetWriter.cs` | Scans supported shader variables for the selected preset through `PresetWriter.ScanSupportedVariables(...)`. |
| `Dalashade/SceneIntent.cs` | Provides scene intent and tag-stack diagnostics. |
| `Dalashade/Windows/MainWindow.cs` | Runtime status, changed variables, sanitize actions, applied rules, diagnostics. |
| `Dalashade/Windows/ConfigWindow.cs` | Scan/export/report commands and settings. |

## Preset Analyzer Purpose

`PresetAnalyzer.Analyze(Configuration configuration)` answers practical questions before generation:

| Question | Result |
| --- | --- |
| Which shader sections exist? | `PresetTechnique` list. |
| Which techniques are active? | `TechniqueActivationState`. |
| Which effects are controlled? | `SupportLevel`. |
| Which effects are risky? | `EffectRisk`. |
| Which effect owns a visual role? | `EffectAuthority` list. |
| What compatibility mode is recommended? | `PresetRiskReport.RecommendedMode`. |

## Support Levels

| Level | Meaning |
| --- | --- |
| `FullyControlled` | Dalashade can safely adjust the important variables for this effect. |
| `PartiallyControlled` | Some useful variables are mapped, but not all. |
| `DetectedOnly` | The shader is recognized but not safely adjusted. |
| `Unsupported` | Dalashade does not know how to handle the shader. |

## Risk Levels

| Risk | Meaning |
| --- | --- |
| `Safe` | Usually suitable for normal gameplay adjustment. |
| `Moderate` | Can affect style or visibility; inspect report. |
| `High` | Likely to cause strong or risky visual changes. |
| `GPoseOnly` | Better suited for screenshots than normal gameplay. |
| `UtilityIgnore` | Utility effects that should generally be ignored. |

## High-Risk And GPose-Only Effects

Common high-risk or GPose-only effect families include DOF, film grain, prism/chromatic aberration, heavy vignette, and screenshot composition tools.

These effects may be detected and reported without being changed during normal generation.

## Multiple Authority Warnings

Authority detection identifies primary and secondary owners for visual roles such as color grade, bloom, sharpen, and AO/GI.

Multiple active authorities can cause competing edits. For some compatibility modes, secondary authorities receive dampened adjustment strength instead of being disabled.

## Changed Variables And Clamps

Changed variables come from `PresetWriter`.

Each change records:

| Field | Meaning |
| --- | --- |
| Section/key | The INI location changed. |
| Old/new value | The written change. |
| Activation state | Active, inactive, or unknown. |
| Reason | Mapper reason or sanitize reason. |
| Clamp state | Whether output was clamped. |

Clamp warnings help identify mappings that may be too aggressive or input values with unusual ranges.

## Sanitize Actions

`SanitizeActionPipeline` is separate from normal adaptive mappings. It applies only where compatibility mode and action rules allow it.

Sanitize actions are reported separately from normal changed variables.

## Generated Reports

Implemented report paths:

| Report | File |
| --- | --- |
| Compatibility report | `CompatibilityReportExporter.Export(...)` |
| Regression reports | `PresetRegressionReportHarness.Run(...)` |

Compatibility reports include preset risk, authorities, role policies, shader support, changed variables, sanitize actions, master diagnostics, scene-tag diagnostics, and mapping validation.

Scene-tag diagnostics include the primary biome, confidence, matched keyword/reason, active weather tags, secondary tags, material tags, area/context tags, gameplay-state tags, art-direction tags, and `SceneIntent` contributions grouped by tag category. Use these sections first when a generated preset has the wrong environmental identity.

Material diagnostics are split into plugin-side scene plausibility and shader-side mask calibration:

| Report item | Meaning |
| --- | --- |
| MaterialProfile family and tags | Scene-level material plausibility such as jungle canopy, coastal waterline, snowfield, neon urban, aetherial landscape, dungeon interior, or raid arena. |
| MaterialIntent values | Optional inferred material likelihood channels. Reports separate profile prior, non-profile evidence, final value, and suppressions. These are not true engine material IDs and do not affect visuals unless MaterialIntent shader mapping is enabled. |
| MaterialIntent shader uniform output | Which first-party Dalashade shader sections received material channel values in the generated preset. |
| MaterialMasks v2 notes | Debug vocabulary for `RawCandidate`, `SceneGatedCandidate`, `FinalMask`, optional depth assist, and likely failure sources. |
| First-party custom shader status | Whether WeatherAtmosphere, AdaptiveGrade, AtmosphereBloom, SmartSharpen, and MaterialDebug appear active, inactive, unknown, or absent in preset analysis. |

Custom shader variable diagnostics separate three ownership classes. SceneIntent variables are Dalashade-controlled when custom shader support is enabled. MaterialIntent channel uniforms are Dalashade-controlled only when MaterialIntent shader mapping is enabled and section-scoped keys exist. Shader-owned controls, including depth assist and debug UI controls, may be known or injected with safe defaults but are not actively written by Dalashade.

Depth assist remains disabled by default. It can help material masks when ReShade depth is valid, but DLSS/upscaling, dynamic resolution, game depth restrictions, or UI/depth mismatches may make it unreliable. Reported depth confidence means usable signal confidence for mask heuristics, not guaranteed correct game depth.

Material calibration failures should be traced in this order: scene profile plausibility, MaterialIntent strength/gating, raw pixel heuristic, final conflict suppression, optional depth assist, then production shader behavior. The master `Dalashade_MaterialDebug.fx` answers what Dalashade thinks a pixel might be; each production shader's local debug mode answers why that shader is affecting or suppressing the pixel.

Regression reports scan a folder of `.ini` presets and write timestamped markdown summaries. They do not require ReShade to be running and should not overwrite user presets.

## UI Diagnostic Panels

The main window includes sections for:

| Section | Purpose |
| --- | --- |
| Current Status | Current game context and generation state. |
| Preset Compatibility | Risk, warnings, supported/unsupported effects, authorities. |
| Changed Variables | Written shader variable changes. |
| Sanitize Actions | Gameplay sanitize changes. |
| Applied Rules | Profile generation rules. |
| Scene Tags | Weather, primary biome, secondary/material/art-direction tags, area/gameplay context, intent values, and stack-budget contributions. |
| Screenshot Analysis | Current screenshot stats. |
| Master Style | Master style diagnostics. |
| Regression Reports | Last regression run status. |
| Debug / Diagnostics | Additional low-level status. |

## What To Check When A Generated Preset Looks Wrong

| Symptom | Check first |
| --- | --- |
| Image turns black and white | Color grade variables, ReGrade+ Colorista changes, LUT strength, and compatibility mode. |
| Preset too dark | Exposure, black point, white point, contrast, shadow lift, master style tonal deltas. |
| Zone has the wrong identity | Scene Tags primary biome, biome reason, confidence, secondary/material/art-direction tags, and SceneIntent contribution groups. |
| Material overlay misses daytime sky | MaterialDebug raw/gated/final sky-fog modes, MaterialProfile SkyCloudFog prior, optional depth assist state, and sky/fog conflict suppression. |
| Foliage overlay marks rocks or trunks too strongly | MaterialDebug raw strong foliage, organic green surface, final foliage influence, StoneRuins prior, and SmartSharpen material dampening debug. |
| Water and sky are confused | MaterialDebug raw/gated/final water plus raw/gated/final sky-fog modes, screen-position/depth-assist state, and WaterSpecular vs SkyCloudFog priors. |
| No visible change | Changed variable count, active ReShade preset, reload diagnostics, and supported shader scan. |
| Too many variables changed | Shader matching mode, inactive shader write mode, and multiple authorities. |
| ReShade reload did not happen | `ReShade.ini` path, reload key sync, configured hotkey, and diagnostics. |

Do not treat a high changed-variable count as automatically good. For broad presets, many changes can mean the matching mode or compatibility policy is too aggressive.
