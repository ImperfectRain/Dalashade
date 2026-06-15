# Codex Session Handoff

Latest verified baseline: `master` after the first-party shader mode pass. `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors. `git diff --check` passed functionally with only Git line-ending normalization warnings.

This guide is for future Codex sessions. It summarizes the current repository shape only; source code remains the implementation authority.

## What Dalashade Does

Dalashade is a Dalamud plugin that generates a separate ReShade preset from a user-selected base preset. It reads current FFXIV context, optional screenshot analysis, optional master-style reference images, and preset compatibility data, then writes conservative adjustments to known shader variables already present in the preset.

It does not modify the base preset in place, bundle paid shaders, install custom shaders automatically, capture live frames, or use a native ReShade bridge. ReShade reload is currently best-effort hotkey automation.

## High-Level Generation Pipeline

1. UI or command entry starts in `Dalashade/Plugin.cs`.
2. `Plugin.GenerateNow()` resolves the effective base preset path.
3. `GameContextService.Refresh()` reads territory, weather, time, duty, combat, cutscene, and GPose state.
4. `SceneClassifier.Classify()` turns raw context into scene tags.
5. `ImageAnalysisService.Refresh()` optionally analyzes the newest screenshot.
6. `MasterStyleService.Refresh()` optionally analyzes reference images.
7. `SceneIntentBuilder.Build(...)` creates normalized scene intent and tag-stack diagnostics.
8. `ProfileEngine.CreateWithRules()` builds the visual profile and applied-rule list.
9. `PresetWriter.ScanSupportedVariables()` and `PresetAnalyzer.Analyze()` scan shader support and compatibility.
10. `PresetWriter.WriteGeneratedPreset()` maps profile values to supported INI section/key edits.
11. `GenerationAuthorityPolicy` and `SanitizeActionPipeline` apply compatibility dampening and limited gameplay sanitize actions.
12. `ReShadeController.ReloadAfterPresetWrite()` optionally attempts a hotkey reload.

## Ownership Map

| Area | File |
| --- | --- |
| Plugin lifecycle and generation orchestration | `Dalashade/Plugin.cs` |
| User settings and modes | `Dalashade/Configuration.cs` |
| Main runtime UI | `Dalashade/Windows/MainWindow.cs` |
| Settings UI | `Dalashade/Windows/ConfigWindow.cs` |
| Context and scene tags | `Dalashade/GameContext.cs` |
| Normalized scene intent and tag diagnostics | `Dalashade/SceneIntent.cs` |
| Screenshot and reference image metrics | `Dalashade/ImageAnalysis.cs` |
| Visual profile rules | `Dalashade/VisualProfile.cs` |
| Master image selection | `Dalashade/MasterStyle.cs` |
| Master style profile deltas | `Dalashade/MasterStyleMatcher.cs` |
| Preset compatibility analysis | `Dalashade/PresetAnalyzer.cs` |
| Generated preset writing and shader support scan | `Dalashade/PresetWriter.cs` |
| Shader variable definitions and value math | `Dalashade/ShaderVariableMapper.cs` |
| Custom Dalashade shader variable writes | `Dalashade/CustomShaderVariableMapper.cs` |
| Custom shader diagnostics | `Dalashade/CustomShaderBridgeDiagnostics.cs` |
| Compatibility report export | `Dalashade/CompatibilityReportExporter.cs` |
| Gameplay sanitize-only reductions | `Dalashade/SanitizeActionPipeline.cs` |
| Authority dampening policy | `Dalashade/GenerationAuthorityPolicy.cs` |
| ReShade reload and diagnostics | `Dalashade/ReShadeController.cs` |
| Release metadata | `Dalashade/Dalashade.csproj`, `Dalashade/Dalashade.json`, `repo.json`, `releases/` |

## Compatibility Philosophy

Compatibility is conservative. Dalashade should detect broad preset risk, report unsupported or high-risk effects, and only write values it can map safely. Multiple visual authorities are handled by dampening secondary authorities in selected modes, not by disabling effects.

`PresetAnalyzer` classifies effects, support levels, risks, activation state, and authorities. `ShaderVariableMapper` owns exact mappings and clamps. `PresetWriter` records changed variables, inactive or unknown writes, clamp hits, warnings, and sanitize actions so users can inspect what actually changed.

## Preservation Rules

- Never overwrite the selected base preset.
- Keep generated output separate from the base preset.
- Do not reorder preset sections, disable techniques, change texture paths, or broadly rewrite INI structure during normal generation.
- Preserve existing user-facing configuration fields unless an explicit migration is requested.
- Do not change `repo.json` or release zips unless the task is explicitly release or install/update work.
- Do not add bridge, IPC, named pipe, native add-on, or live-frame capture behavior unless explicitly requested.

## Shader Support Workflow

When adding shader support:

1. Start with real preset examples and exact `.fx` section names.
2. Inspect `docs/ShaderMapping.md`, `Dalashade/ShaderVariableMapper.cs`, `Dalashade/PresetAnalyzer.cs`, and `Dalashade/PresetWriter.cs`.
3. Add detection only when shader identity is clear.
4. Prefer strict section/key mappings with conservative clamps.
5. Avoid broad `LooseKeys` behavior for generic keys such as `Exposure`, `Strength`, `Contrast`, or `Saturation`.
6. Add report or diagnostic coverage when support level changes.
7. Build and inspect changed-variable output against a real or fixture preset.

## Custom Dalashade Shader Philosophy

Custom Dalashade shader support is optional and manual. The current production first-party shader set is:

- `shaders/Dalashade_AdaptiveGrade.fx`
- `shaders/Dalashade_SceneGI.fx`
- `shaders/Dalashade_SurfaceReflection.fx`
- `shaders/Dalashade_AtmosphereBloom.fx`
- `shaders/Dalashade_WeatherAtmosphere.fx`
- `shaders/Dalashade_SmartSharpen.fx`

Diagnostic first-party shaders are:

- `shaders/Dalashade_MaterialDebug.fx`
- `shaders/Dalashade_NormalDebug.fx`

`CustomShaderVariableMapper` writes normalized `SceneIntent` values only when generated preset content contains matching Dalashade custom shader sections and keys. Optional generated-preset-only injection can add known sections and variables, but the plugin does not mutate the base preset, copy `.fx` files, auto-install shaders, or require a custom shader path for normal generation.

## First-Party Shader Mode State

`Configuration.FirstPartyShaderMode` controls how production first-party shaders behave:

- `Supportive` is the default and writes `Dalashade_StandaloneStrength=0`.
- `Standalone` writes `Dalashade_StandaloneStrength=1` to known production first-party shader sections that declare the key.

The mode is user-facing, not a debug mode. It does not affect `MaterialDebug` or `NormalDebug`, does not change shader order, and does not weaken source/receiver or material/safety contracts.

Current implementation status:

- C# plumbing exists in `Configuration.cs`, `Windows/ConfigWindow.cs`, `CustomShaderVariableMapper.cs`, and `PresetWriter.cs`.
- `Dalashade_StandaloneStrength` is declared in AdaptiveGrade, SceneGI, SurfaceReflection, AtmosphereBloom, WeatherAtmosphere, and SmartSharpen.
- Standalone mode currently applies conservative, safety-gated headroom. It is not yet a fully transformative visual profile system.

Next development should treat this as clean mode plumbing plus a modest first shader-side response. If the goal is a more transformative standalone look, implement it as shader-specific scene identity lanes, not as broad global multipliers.

## Material, Water, Receiver, and NormalField Contract

Use `Dalashade_MaterialMasks.fxh` as the shared material/water/safety contract. Do not redefine water, sky, receiver, or source semantics locally unless the shader has an effect-specific shaping reason.

Important distinctions:

- `WaterPixelConfidence` means likely actual water pixel.
- `WaterReceiver` means water can receive reflection/wet response.
- `WaterSource`, `SkySource`, and `HorizonOnlyConfidence` are source/context terms, not receiver masks.
- `ReflectionReceiverConfidence`, `AOReceiverConfidence`, and `StructureReceiverConfidence` are role-specific confidence fields.
- `ReceiverConfidence` is legacy broad fallback only.
- `NormalField` is inferred screen-space support, not true FFXIV normals, material normals, roughness, metallic, motion vectors, or a G-buffer.

## Refactor Constraints

Keep behavior inside its current owner unless the task is explicitly architectural. Context belongs in `GameContext` and `SceneIntent`; visual decisions belong in `VisualProfile` and master style code; preset output belongs in `PresetWriter` and shader mappers; compatibility reporting belongs in analyzer/exporter code; UI files should remain UI-facing.

Avoid unrelated refactors. Visual behavior changes can be hard to validate, so keep commits small and scoped to one purpose.

## Required Workflow For Future Tasks

1. Read `docs/CodebaseIndex.md`.
2. Read `docs/GenerationPipeline.md`.
3. Read the topic doc for the requested change.
4. Read `docs/CodexEditingGuide.md`.
5. Inspect the source files named by those docs before editing.
6. Make the smallest responsible change.
7. Update docs when behavior or architecture changes.
8. Run `dotnet build` after meaningful changes.
9. For release tasks only, run `scripts/ValidateRelease.ps1` after release artifacts and manifest links exist.

## Recommended Next Agent Starting Point

The next sensible visual task is a narrow AdaptiveGrade standalone identity pass:

1. Read `docs/Shaders/ShaderSystemOverview.md`, `docs/Shaders/AdaptiveGrade.md`, `docs/Shaders/MaterialMasks.md`, and `docs/Shaders/NormalField.md`.
2. Inspect `shaders/Dalashade_AdaptiveGrade.fx`, `Dalashade/CustomShaderVariableMapper.cs`, and `Dalashade/PresetWriter.cs`.
3. Keep Supportive mode visually equivalent to current behavior.
4. Use `Dalashade_StandaloneStrength` only for Standalone-specific scene identity shaping.
5. Prefer explicit scene lanes over blunt multipliers: coastal day/night, desert, snow/cold, forest/canopy, aether/Allagan, and interiors/dungeons.
6. Do not touch MaterialMasks, NormalField, debug shader behavior, shader stack order, source/receiver separation, or generated preset safety unless the prompt explicitly asks for it.
7. Run `dotnet build Dalashade.sln` and `git diff --check`.
