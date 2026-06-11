# Compatibility And Diagnostics

This page documents how Dalashade reports preset compatibility and generation diagnostics.

## Implemented Files

| File | Owns |
| --- | --- |
| `Dalashade/PresetAnalyzer.cs` | Preset parsing, activation state, support classification, risk classification, authorities, warnings. |
| `Dalashade/CompatibilityReportExporter.cs` | Markdown compatibility report export. |
| `Dalashade/PresetRegressionReportHarness.cs` | Batch preset regression report generation. |
| `Dalashade/ShaderSupportScanner.cs` | Scans supported shader variables for the selected preset. |
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

Compatibility reports include preset risk, authorities, role policies, shader support, changed variables, sanitize actions, master diagnostics, and mapping validation.

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
| Screenshot Analysis | Current screenshot stats. |
| Master Style | Master style diagnostics. |
| Regression Reports | Last regression run status. |
| Debug / Diagnostics | Additional low-level status. |

## What To Check When A Generated Preset Looks Wrong

| Symptom | Check first |
| --- | --- |
| Image turns black and white | Color grade variables, ReGrade+ Colorista changes, LUT strength, and compatibility mode. |
| Preset too dark | Exposure, black point, white point, contrast, shadow lift, master style tonal deltas. |
| No visible change | Changed variable count, active ReShade preset, reload diagnostics, and supported shader scan. |
| Too many variables changed | Shader matching mode, inactive shader write mode, and multiple authorities. |
| ReShade reload did not happen | `ReShade.ini` path, reload key sync, configured hotkey, and diagnostics. |

Do not treat a high changed-variable count as automatically good. For broad presets, many changes can mean the matching mode or compatibility policy is too aggressive.
