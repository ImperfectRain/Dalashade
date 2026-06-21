# Architecture Refactor Plan

This plan records low-risk seams for future architecture work. It is not an implementation request for the current pass.

## Current Pressure Points

| Area | Current pressure | Why it matters |
| --- | --- | --- |
| `PresetWriter.cs` | Plans custom sections, writes generated files, manages backups, applies technique sync, and optimizes load order. | Hard to test planning separately from file I/O and backup behavior. |
| `CompatibilityReportExporter.cs` | Owns many feature summaries and duplicated diagnostic language. | Report behavior can drift from debug bundle and UI wording. |
| `DebugBundleExporter.cs` | Knows many feature-specific filenames, summaries, and status shapes. | Bundle schema can drift from compatibility report and support expectations. |
| `CustomShaderVariableMapper.cs` | Owns first-party shader uniform names, defaults, performance tier writes, and section-scoped logic. | Shader registry metadata is spread between mapper, writer, docs, and reports. |
| `SceneAuthoringService.cs` | Owns storage, default registry, override application, tuning validation, import/export, and reset behavior. | Scene authoring and material tuning are related but not the same responsibility. |
| `MainWindow.cs` / `ConfigWindow.cs` | Mix user workflow, developer diagnostics, raw config editing, and feature education. | UI consistency is harder to preserve as features grow. |

## Proposed Future Services

| Service | Responsibility |
| --- | --- |
| `GeneratedPresetPlan` | Pure plan for sections, key writes, technique sync changes, load-order changes, and backup/write decisions. |
| `PresetFileWriter` | File I/O, backups, atomic generated-preset write, and write-cadence enforcement. |
| `ShaderSectionInjectionPlanner` | Decide which first-party sections and defaults should be injected into the generated preset. |
| `TechniqueSyncPlanner` | Production allow-list technique activation/sorting decisions and debug-shader exclusions. |
| `FirstPartyShaderRegistry` | Single metadata source for shader families, sections, techniques, known uniforms, defaults, debug/manual flags, and performance-tier keys. |
| `DiagnosticSectionModel` | Shared DTOs for report, bundle, and UI diagnostic sections. |
| `SceneTagAuthoringStore` | Storage, import/export, and reset behavior for scene overrides and tag registry data. |
| `MaterialTuningRegistry` | Validation, caps, and diagnostics for registry rows that target MaterialIntent or shader priors. |

## Migration Order

1. Add tests around generated preset output, technique sync, debug shader exclusion, and debug bundle summary presence.
2. Extract `FirstPartyShaderRegistry` as read-only metadata while keeping mapper output identical. This first extraction now exists; the next step is deeper parity comparison before any writer behavior moves onto the registry.
3. Add shared diagnostic DTOs and render one report/bundle section from them.
4. Split `GeneratedPresetPlan` from `PresetFileWriter` behind existing `PresetWriter` public methods.
5. Extract `TechniqueSyncPlanner` and `ShaderSectionInjectionPlanner` once preset-output tests are stable.
6. Split `SceneTagAuthoringStore` from `MaterialTuningRegistry` after scene authoring fixtures cover override and registry behavior.

## Low-Risk First Extraction

Start with read-only shader metadata:

- shader family display name
- section name
- technique name
- production vs debug flag
- technique-sync eligible flag
- known generated uniforms
- debug/manual-only uniforms
- performance-tier uniforms

This can power docs, parity validation, reports, and UI labels before any writer behavior changes.

Current state: the read-only registry exists and is consumed by parity diagnostics, compatibility reports, debug bundles, and User Mode labels. It deliberately does not own section injection, generated uniform values, technique activation, or visual formulas.

## Tests Needed Before Extraction

- Config defaults and migration behavior.
- Generated variable parity for every known first-party shader section.
- Technique sync allow-list and debug shader exclusion.
- Preset section injection output with injection on/off.
- Debug bundle expected filenames and top-level summaries.
- Material evidence neutral/fallback behavior.
- Dalapad disabled/missing/stale fallback behavior.
- User Mode vs Developer Mode control visibility snapshots or metadata checks.

## What Not To Refactor Yet

- Do not add a FrameData prepass.
- Do not add `Dalashade_Stack.fx`.
- Do not tune shader visuals as part of architecture extraction.
- Do not expand Dalapad render-layer behavior while splitting diagnostics.
- Do not broaden third-party shader integration until first-party registry metadata is stable.
- Do not change default generated preset behavior during read-only metadata extraction.
