# Codex Session Handoff

Latest verified baseline: `master` at commit `2cd52c3`. `dotnet build` passed with 0 warnings and 0 errors.

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

Custom Dalashade shader support is optional and manual. `shaders/Dalashade_WeatherAtmosphere.fx` is a prototype shader, and normal operation does not require it.

`CustomShaderVariableMapper` writes normalized `SceneIntent` values only when the base preset already contains matching Dalashade custom shader sections and keys. The plugin does not copy `.fx` files, insert custom shader sections, auto-install shaders, or require a custom shader path for normal generation.

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
