# Codex Session Handoff

Latest verified baseline: working tree after the Dalapad developer-only resource shape probe pass. `dotnet build Dalashade.sln` passed locally during this pass; run the full test set again before committing. The stage 1 native addon artifact remains `DalapadAddon/build/Dalapad.addon64`.

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
| Dalapad diagnostics, IPC status-file reader, and control-pipe client | `Dalashade/DalapadDiagnostics.cs`, `Dalashade/DalapadIpcClient.cs` |
| Experimental Dalapad addon source/contract | `DalapadAddon/` |
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
- Do not add render-target bridges, shader-resource exposure, live-frame capture, or realtime shader-value movement unless explicitly requested. Dalapad now has diagnostic-only status-file IPC, a short-timeout diagnostic control pipe, and an explicit Developer Mode resource shape probe, but it must not be treated as a render-target bridge or live shader-value path yet.

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
- `Dalashade_StandaloneStrength` is declared in AdaptiveGrade, SceneGI, ContactTone, SurfaceReflection, AtmosphereBloom, WeatherAtmosphere, and SmartSharpen.
- Standalone mode currently applies conservative, safety-gated headroom across the first-party stack.
- `Dalashade_AdaptiveGrade.fx` now has the first Standalone identity-lane pass for coastal day, coastal night, desert/heat, snow/cold, forest/canopy, aether/Allagan/high-tech, and dungeon/interior tone/color shaping.

Next development should validate the AdaptiveGrade lane behavior before expanding Standalone identity to other shaders. Keep future work shader-specific and scene-lane driven, not broad global multipliers.

## Material, Water, Receiver, and NormalField Contract

Use `Dalashade_FrameData.fxh` as the first-party shader-facing contract for shared scene tags, material, water, safety, receiver, and optional surface data. `Dalashade_MaterialMasks.fxh` and `Dalashade_NormalField.fxh` remain the canonical formula owners underneath FrameData. Do not redefine water, sky, receiver, source, scene tag, or surface semantics locally unless the shader has an effect-specific shaping reason.

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

## Current Maintainability Notes

The docs are mostly accurate, but the scene authoring and tag registry system should be treated as implemented rather than architecturally mature. Scene overrides and registry tuning exist, and the UI is usable, but `SceneAuthoringService` currently owns storage, registry defaults, override resolution, tuning validation, import/export, and reset behavior. The best next move is a behavior-preserving split with tests around tag behavior before adding more scene authoring UI features.

FrameData is currently inline-only. All first-party production shaders consume `Dalashade_FrameData.fxh`, but there is no prepass, render target, temporal accumulation, production G-buffer path, native XIV buffer path, or ReShade resource bridge. `Dalashade_MaterialMasks.fxh` and `Dalashade_NormalField.fxh` remain the canonical formula owners; FrameData is a contract/wrapper, not a formula owner.

Dalapad is now the fenced research path for optional external surface data. Implemented pieces are:

- `Dalashade/DalapadDiagnostics.cs` reflection metadata probing.
- `Dalashade/DalapadIpcClient.cs` short-timeout diagnostic control-pipe client.
- optional status-file IPC read from `<XIVLauncher plugin config>/Dalashade/Dalapad/dalapad-status.json`.
- optional diagnostic control-pipe probe at `\\.\pipe\Dalapad.Control.v1`.
- metadata-only resource catalog rows from the status file and `QueryStatus` pipe response.
- explicit Developer Mode resource shape probe that may invoke `RenderTargetManager.Instance` and reports redacted candidate presence, dimensions, format labels, confidence, and failure reasons only. After the 2026-06-20 `011115`, `011238`, and `011449` bundles showed reflection could invoke `Instance` but could not observe candidate pointers, the probe now tries typed ClientStructs access first and falls back to reflection.
- `shaders/Dalapad_Debug.fx` plus addon-side synthetic texture upload into `Dalapad_DebugTexture`, reported through `debugVisualization` status-file/control-pipe fields.
- Developer Mode `Dalapad` diagnostics panel.
- compatibility report and debug bundle output, including `dalapad-diagnostics.json`.
- `DalapadAddon/` with the Stage 1 ReShade/native addon source, contract docs, sample status JSON, vendored ReShade SDK headers, and a local test build at `DalapadAddon/build/Dalapad.addon64`.

Not implemented yet:

- no `RenderTargetManager.Instance()` invocation from default diagnostics, User Mode, or production plugin behavior.
- no G-buffer read/copy/registration.
- no named texture exposed to `.fx` code.
- no XIV render target uploaded to `.fx` code; current debug visualization uses generated synthetic pixels only.
- no raw pointer reporting.
- no live uniform movement over `\\.\pipe\Dalapad.Control.v1`; current pipe commands are diagnostic-only `Ping`, `BridgeSelfTest`, `QueryStatus`, and `QueryCapabilities`.
- no shader-ready live resource dimensions, formats, freshness, or confidence yet; Stage 1.2 catalog rows are static-contract rows with neutral unavailable values, and the Developer Mode shape probe is diagnostic evidence only.
- no generated preset, FrameData, NormalField, MaterialMasks, or first-party shader behavior change from Dalapad.

The next Dalapad validation step is to reload this plugin build, install/reload `Dalapad_Debug.fx`, load the built addon in ReShade, confirm it writes `dalapad-status.json`, and confirm Developer Mode > Dalapad reports status-file IPC, control-pipe capability negotiation, metadata-only catalog rows, the typed resource shape probe result, and synthetic debug visualization upload. Resource registration/copying should still wait until metadata, shape observation, synthetic visualization, lifetime, format, and freshness are stable across lifecycle tests.

In-game validation on ReShade `6.7.3.2148` showed that API version `20` registration is rejected because the runtime supports add-on API `18`. The addon source now requests API `18` directly for Stage 1 registration instead of relying on the vendored SDK helper constant.

`Dalashade_SurfaceReflection.fx` is the weakest visual system. It has useful debug masks, but normal output has repeatedly failed to create convincing object reflections. Do not keep small-tuning it while expecting mirror-like behavior. Future reflection work should be a deliberate algorithm redesign or wait for better prepass/data support.

`Dalashade_SceneGI.fx` is a safer near-term shader target. It fits the current FrameData model better and can improve through scene lanes, material bounce, contact/AO shaping, and emissive pooling without requiring true world-space reflection data.

The largest maintainability pressure points are `CompatibilityReportExporter.cs`, `DebugBundleExporter.cs`, `MainWindow.cs`, `SceneAuthoringService.cs`, and `SurfaceReflection.fx`. Refactors should be staged, behavior-preserving, and backed by focused checks or tests where possible.

User Mode should explain what is happening and expose safe choices. Developer Mode should keep raw variables, diagnostics, shader controls, and low-level mapping. Tag authoring should move toward shipped defaults plus non-destructive user overrides that can be shared.

Do not commit `.codex-debug/` or similar local investigation output unless a task explicitly promotes it to source material.

## Required Workflow For Future Tasks

1. Read `docs/CodebaseIndex.md`.
2. Read `docs/GenerationPipeline.md`.
3. Read the topic doc for the requested change.
4. Read `docs/CodexEditingGuide.md`.
5. Inspect the source files named by those docs before editing.
6. Make the smallest responsible change.
7. Update docs when behavior or architecture changes.
8. Add a plainspeak entry to `docs/CommitChangelog.md` before committing code, shader, configuration, workflow, or documentation changes.
9. Run `dotnet build` after meaningful changes.
10. For release tasks only, run `scripts/ValidateRelease.ps1` after release artifacts and manifest links exist.

## Recommended Next Agent Starting Point

If continuing Dalapad, start here:

1. Read `docs/Dalapad.md`, `DalapadAddon/README.md`, and `DalapadAddon/CONTRACT.md`.
2. Inspect `Dalashade/DalapadDiagnostics.cs`, `Dalashade/DalapadIpcClient.cs`, `Dalashade/CompatibilityReportExporter.cs`, `Dalashade/DebugBundleExporter.cs`, `Dalashade/Windows/ConfigWindow.cs`, and `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp`.
3. Rebuild the addon if needed with `clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64`.
4. Load `DalapadAddon/build/Dalapad.addon64` in the ReShade addon path for the FFXIV game install.
5. In-game, open Developer Mode > Dalapad, press `Probe Dalapad diagnostics`, optionally enable the developer-only resource shape probe, press `Run Dalapad shape probe`, and export a debug bundle.
  6. Confirm only diagnostic behavior works: status-file IPC is seen, control-pipe capability negotiation answers, metadata-only catalog rows appear, resource shape rows are redacted/diagnostic-only, and no preset/shader behavior changes.
  7. If the handshake and shape probe are stable, the next code pass should be lifecycle observation/debug visualization planning, not resource registration/copying or production FrameData consumption.
8. Run `dotnet build Dalashade.sln`, `dotnet test Dalashade.sln`, addon compile/build checks, and `git diff --check`.

If continuing visual shader work instead, the next sensible task is still AdaptiveGrade Standalone validation. Keep Supportive mode visually equivalent, use `Dalashade_StandaloneStrength` only for Standalone-specific scene identity shaping, and do not touch MaterialMasks, NormalField, debug shader behavior, shader stack order, source/receiver separation, or generated preset safety unless the prompt explicitly asks for it.
