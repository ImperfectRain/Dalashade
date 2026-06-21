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
| `Dalashade/DalapadDiagnostics.cs` | Diagnostic-only runtime metadata probe for the optional future Dalapad surface-data addon direction. |
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

Compatibility reports include preset risk, authorities, role policies, shader support, changed variables, sanitize actions, master diagnostics, scene-tag diagnostics, scene-authoring diagnostics, screenshot material evidence, FrameData diagnostics, Dalapad diagnostics, first-party depth-assist diagnostics, and mapping validation.

Scene-tag diagnostics include the primary biome, confidence, matched keyword/reason, active weather tags, secondary tags, material tags, area/context tags, gameplay-state tags, art-direction tags, and `SceneIntent` contributions grouped by tag category. Use these sections first when a generated preset has the wrong environmental identity.

Scene-authoring diagnostics show whether authoring is enabled, the active override identity, detected versus effective area/weather/biome/mood tags, added/removed override maps, suppressed diagnostic tags, conflict warnings, and the authoring fingerprint used for profile cache invalidation. Use this section when a user-authored tag stack appears to be ignored or when derived tags seem to reappear after removal.

Material diagnostics are split into plugin-side scene plausibility and shader-side mask calibration:

| Report item | Meaning |
| --- | --- |
| MaterialProfile family and tags | Scene-level material plausibility such as jungle canopy, coastal waterline, snowfield, neon urban, aetherial landscape, dungeon interior, or raid arena. |
| MaterialIntent values | Optional inferred material likelihood channels. Reports separate profile prior, tag/other evidence, screenshot material evidence, final value, and suppressions/caps. These are not true engine material IDs and do not affect visuals unless MaterialIntent shader mapping is enabled. |
| Tag registry material tunings | Active, inactive, invalid, and capped user/built-in registry rows that target MaterialIntent. Registry material tunings are capped at +/-0.20 per tag and +/-0.35 per channel total, and invalid rows are ignored with diagnostics. |
| MaterialIntent shader uniform output | Which first-party Dalashade shader sections received material channel values in the generated preset. |
| Screenshot material evidence | Broad visible material-family estimates from screenshot regions/opinions, compared against current MaterialIntent values. Influence is off by default; when enabled, capped contributions enter MaterialIntent through the normal mapping path. |
| Material Calibration | Per-channel matrix comparing profile prior, tag registry contribution, screenshot evidence, current MaterialIntent, shader mapping availability, shader keys/sections, and severity-ranked warnings. |
| MaterialMasks v2 notes | Debug vocabulary for `RawCandidate`, `SceneGatedCandidate`, `FinalMask`, optional depth assist, and likely failure sources. |
| First-party custom shader status | Whether WeatherAtmosphere, AdaptiveGrade, AtmosphereBloom, SmartSharpen, MaterialDebug, SceneGI, ContactTone, and SurfaceReflection appear active, inactive, unknown, or absent in preset analysis. |

Custom shader variable diagnostics separate three ownership classes. SceneIntent variables are Dalashade-controlled when custom shader support is enabled. MaterialIntent channel uniforms are Dalashade-controlled only when MaterialIntent shader mapping is enabled and section-scoped keys exist. Shader-owned controls may be known or injected with safe defaults. The bulk first-party depth-assist toggle writes only known depth-assist uniforms in production first-party Dalashade shader sections, including SceneGI, ContactTone, and SurfaceReflection; debug UI controls remain shader-owned/manual.

NormalField diagnostics report optional debug shader presence, generated/active preset technique state, shader source consumption, written NormalField uniforms, and suppression settings. NormalDebug technique activity is preset-analysis-only; Dalashade cannot inspect the live ReShade UI checkbox state after a report or bundle is generated.

FrameData diagnostics report the internal resolver contract state. Current expected status is `FrameDataMode: Inline`, `FrameDataPrepass: NotImplemented`, and production consumers for WeatherAtmosphere, AdaptiveGrade, SmartSharpen, AtmosphereBloom, SceneGI, ContactTone, and SurfaceReflection. The report also shows whether `Dalashade_FrameData.fxh` and `Dalashade_FrameDataDebug.fx` are available to the source scan, whether the generated preset contains a FrameDataDebug section, whether the technique appears active, and the FrameDataDebug debug variables. Production first-party shaders use inline FrameData; no prepass or render target exists.

Dalapad diagnostics report the removable external surface-data addon path. Current expected behavior is diagnostic and opt-in: discover loaded FFXIVClientStructs/RenderTargetManager/texture names when present, read an optional Stage 1 `dalapad-status.json` file, query the short-timeout diagnostic control pipe, report capability rows, addon contract version, IPC status, resource rows, scan/pinned candidate availability, debug visualization status, diagnostic routes, implementation options, realtime-contract placeholders, and staged backend steps. The addon may upload synthetic pixels or bind addon-owned debug candidate copies for `Dalapad_Debug.fx`, but it must not expose raw game handles over IPC or make render-layer data required for normal Dalashade output. First-party shader consumption is optional, generated-preset gated, surface-data gated, and routed through `Dalashade_FrameData.fxh`; missing or disabled Dalapad data must resolve to zero Dalapad confidence and normal/default shader behavior. The repo-local `DalapadAddon/` scaffold is not built or shipped as part of normal plugin output; it documents and prototypes the guarded bridge contract.

SceneGI diagnostics are separate from shader compilation. Dalashade can report whether the `Dalashade_SceneGI` section or technique appears in preset analysis and whether GI variables were written, but ReShade compile success still has to be verified in-game after installing `Dalashade_SceneGI.fx` and shared Dalashade includes.

ContactTone and SurfaceReflection diagnostics are also separate from shader compilation. Dalashade can report whether the `Dalashade_ContactTone` or `Dalashade_SurfaceReflection` section or technique appears and whether their variables were written, but ReShade compile success still has to be verified in-game after installing the `.fx` files and shared Dalashade includes.

SceneGI, ContactTone, and SurfaceReflection each get their own custom shader report section. SceneGI reports GI strength, AO intensity, bounce strength, night light strength, material influence, integer debug mode/output mode, debug opacity/boost, dominant SceneIntent drivers, and dominant MaterialIntent drivers. ContactTone reports contact strength, radius, depth edge, structure, local contrast, integer debug mode, dominant drivers, and written generated variables. SurfaceReflection reports reflection strength, water sheen, specular glint, wet response, aether/neon response, integer debug mode, dominant MaterialIntent drivers, and written generated variables. These sections are diagnostics only; they do not auto-install `.fx` files. Production technique activation can be synced only when `SyncDalashadeTechniqueActivation` and each shader's variable-write option are enabled.

Preset analysis warns about first-party stack-order issues when it can see active techniques: SceneGI before AdaptiveGrade, SurfaceReflection before SceneGI, MaterialDebug before production shaders, sharpeners before GI/reflection, excessive active sharpeners, SurfaceReflection without WaterPlane/SpecularGlint uniforms, and active SceneGI where ReShade depth support cannot be confirmed from preset text.

Depth assist remains disabled by default. It can help material masks when ReShade depth is valid, but DLSS/upscaling, dynamic resolution, game depth restrictions, or UI/depth mismatches may make it unreliable. Reported depth confidence means usable signal confidence for mask heuristics, not guaranteed correct game depth. First-party depth-assist diagnostics report `EnableFirstPartyDepthAssist`, whether generated-preset custom shader writes and section injection are enabled, and which known production first-party sections received `Dalashade_EnableDepthAssist`, `Dalashade_DepthAssistStrength`, `Dalashade_DepthAssistConfidenceFloor`, or `Dalashade_DepthConfidenceFloor`.

Material calibration failures should be traced in this order: scene profile plausibility, MaterialIntent strength/gating, raw pixel heuristic, final conflict suppression, optional depth assist, then production shader behavior. The master `Dalashade_MaterialDebug.fx` answers what Dalashade thinks a pixel might be; each production shader's local debug mode answers why that shader is affecting or suppressing the pixel.

Screenshot material evidence adds an earlier report-only check before changing formulas. It can warn when visible foliage/grass, water, sand, snow, aether/neon, or sky evidence conflicts with the current MaterialIntent. Treat those warnings as calibration leads, not as proof of true material identity.

The Material Calibration scene matrix uses these expected patterns:

| Representative scene | Expected material evidence |
| --- | --- |
| forest/canopy | foliage high, sky moderate, water variable |
| coastal | water/sand/sky variable by view |
| snow | snow/sky/stone variable |
| desert | sand/stone/sky high |
| high-tech/aether | metal/aether/neon high, water only if actual water evidence |
| dungeon/interior | stone/metal/interior high, sky low |
| combat/UI-heavy | confidence lower, skin/character/UI risk higher |

Regression reports scan a folder of `.ini` presets and write timestamped markdown summaries. They do not require ReShade to be running and should not overwrite user presets.

## UI Diagnostic Panels

The main window includes sections for:

| Section | Purpose |
| --- | --- |
| Current Status | Current game context and generation state. |
| Preset Compatibility | Risk, warnings, supported/unsupported effects, authorities. |
| Screenshot Material Evidence | Developer-only raw evidence channels, confidence, current MaterialIntent comparison, mismatch warnings, strength/cap context, and a copyable evidence block. User Mode only exposes the safe `Use screenshot material hints` toggle and limited strength slider. |
| Changed Variables | Written shader variable changes. |
| Sanitize Actions | Gameplay sanitize changes. |
| Applied Rules | Profile generation rules. |
| Scene Tags | Weather, primary biome, secondary/material/art-direction tags, area/gameplay context, intent values, and stack-budget contributions. |
| Screenshot Analysis | Current screenshot stats, named opinions, strength, regions, and top regional color families. |
| Screenshot Material Evidence | Visible material-family evidence, confidence, evidence notes, current MaterialIntent comparison, mismatch warnings, and whether opt-in capped MaterialIntent influence is enabled. |
| Dalapad | Developer-only runtime metadata, optional status-file/control-pipe IPC, resource rows, scan/pinned candidate status, debug visualization state, optional first-party shader integration gates, diagnostic routes, and removal/safety notes. |
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
| Water and sky are confused | MaterialDebug modes 55-65. Start with 55 water/sky conflict, 56 water pixel confidence, 57 sky pixel confidence, and 58 water receiver vs horizon. Use 60-65 to inspect local water proof, constructed/aether rejection, sky dominance, and water proof boost before tuning formulas. |
| Water/specular marks rails, nails, plank seams, or hard silhouette highlights | Check MaterialDebug SpecularGlint modes. Thin reflective geometry can be valid glint evidence, but WaterPlane should remain low on those pixels. |
| No visible change | Changed variable count, active ReShade preset, reload diagnostics, and supported shader scan. |
| Too many variables changed | Shader matching mode, inactive shader write mode, and multiple authorities. |
| ReShade reload did not happen | `ReShade.ini` path, reload key sync, configured hotkey, and diagnostics. |
| NormalDebug appears active in ReShade but report says inactive | Remember that NormalDebug activity is read from analyzed preset text only. The debug bundle cannot observe later live ReShade UI toggles. |
| Dalapad finds candidates but FrameData/SceneGI debug modes show blank | Check that global Dalapad shader additions are enabled, Dalapad surface data is enabled, pinned normal availability is non-zero, the generated preset contains the new keys, and ReShade has reloaded. Blank is correct when any gate is closed because FrameData exposes zero Dalapad influence. |
| Dalapad IPC status is `NotConnected` | Expected unless a separate experimental Dalapad addon prototype has written `Dalapad/dalapad-status.json` under the plugin config. Missing status is neutral. |

Do not treat a high changed-variable count as automatically good. For broad presets, many changes can mean the matching mode or compatibility policy is too aggressive.

## Debug Bundle Export

`Export Debug Bundle` is documented in [DebugBundles.md](DebugBundles.md). It is designed for partial success: missing optional presets, missing shader files, failed hashes, or zip failures should be recorded as skipped items instead of failing the bundle when core diagnostics are written.

Use `bundle-export-log.txt` first when diagnosing export failures. Use `manifest.json` to see which files were included or skipped.
