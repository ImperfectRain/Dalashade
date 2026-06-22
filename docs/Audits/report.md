# Dalashade Systems Technical Debt and Bloat Audit

Date: 2026-06-21

Scope: read-only systems audit after the recent P1/P2 release-readiness, diagnostics, documentation, and first-party shader registry passes. This report reviews remaining debt and bloat in the current repository shape. It does not propose visual tuning and does not request broad feature work.

Validation note: this pass intentionally did not run build/test commands because the request was a read-only audit. No code or shader behavior was changed.

## Executive Summary

The recent work materially improved Dalashade's safety posture and supportability. The repository now has clearer docs, a read-only first-party shader registry, generated-uniform/shader-uniform parity diagnostics, top-level debug bundle summaries, better Dalapad production-vs-debug cost reporting, and stronger wording around optional shader/Dalapad behavior.

The remaining debt is no longer mostly "missing release-readiness plumbing." It is now consolidation debt:

- Metadata exists in too many places: `PresetWriter`, `CustomShaderVariableMapper`, `FirstPartyShaderRegistry`, shader source, compatibility reports, debug bundles, and docs all know overlapping first-party shader facts.
- Diagnostics are useful but bloated. `CompatibilityReportExporter.cs` and `DebugBundleExporter.cs` now produce better summaries, but they grew further instead of becoming thinner.
- UI parity improved, but the UI files still own too much workflow, wording, diagnostics routing, and raw control layout.
- Dalapad remains optional and safer than earlier, but the native addon file has become a large mixed-responsibility prototype.
- Shader contracts are clearer, but production shader files still repeat uniform declarations and inline helper work per pass.
- Test coverage is still the main blocker before deeper refactors. The repo has harnesses, but it lacks a focused unit test layer around metadata, preset planning, diagnostics, and safety invariants.

No P0 release-blocking issue was found in this read-only pass. The main P1 risk is future drift: the codebase now has good reporting for mismatches, but still has several places where mismatches can be created.

## Current System Shape

Current flow:

1. Dalamud plugin gathers game context, scene tags, screenshot evidence, material intent, and user configuration.
2. `PresetWriter` reads a base preset and writes a generated preset only.
3. First-party shader sections can be injected into the generated preset.
4. Optional technique sync can activate production first-party techniques only.
5. First-party shaders consume generated uniforms plus shared `.fxh` includes.
6. `FrameData` normalizes shader-facing surface, material, water, safety, receiver, scene, and Dalapad data.
7. `Dalapad` is optional. Status/control IPC reports health and resource candidates, while shader-visible semantic textures are gated and neutral when unavailable or disabled.
8. Diagnostics/exporters create reports, debug bundles, health summaries, performance summaries, generated-variable summaries, registry dumps, and parity scans.

The architecture is safer and more explainable than before, but the implementation still contains multiple overlapping ownership layers.

## What Improved Recently

| Area | Current improvement | Remaining debt |
| --- | --- | --- |
| First-party shader metadata | `FirstPartyShaderRegistry.cs` now centralizes family/section/technique/debug/performance metadata for diagnostics and UI labels. | It is read-only and not yet the source of truth for writer behavior or mapper defaults. |
| Uniform parity | `ShaderUniformParityDiagnostics.cs` scans installed shader uniforms and compares them with known generated variables. | It catches drift after the fact; it does not prevent drift in `PresetWriter`, mapper, registry, and docs. |
| Debug bundles | Added `health-summary.json`, `performance-summary.json`, `generated-variable-summary.json`, `shader-uniform-parity.json`, and registry output. | Bundle exporter grew larger and still owns many feature-specific sections directly. |
| Dalapad cost clarity | Production assist and debug visualization cost are now described separately. | Addon runtime still mixes status IPC, debug copies, candidate cataloging, and texture upload behavior in one file. |
| Docs | Added parity docs, shader contract quick reference, registry docs, diagnostics model plan, and refactor plan. | Some handoff/changelog text is stale or manually maintained. |
| User/Developer UI split | Wording is clearer and debug controls are mostly developer-facing. | `MainWindow.cs` and `ConfigWindow.cs` remain large and still mix page composition, labels, config mutation, and health explanation. |

## Bloat Hotspots

Line counts from the current workspace:

| File | Approx. lines | Debt type |
| --- | ---: | --- |
| `Dalashade/CompatibilityReportExporter.cs` | 3032 | Report generation, feature summaries, material parity, shader scans, and wording all in one exporter. |
| `Dalashade/DebugBundleExporter.cs` | 2402 | Bundle orchestration, JSON schema construction, support summaries, file copying, shader scans, and README generation in one exporter. |
| `Dalashade/Windows/MainWindow.cs` | 2122 | Main UI, user/developer presentation, feature cards, diagnostics surfaces, and status copy. |
| `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp` | 1816 | Native addon lifecycle, ReShade events, IPC, JSON writing, resource catalog, debug visualization, and pinned candidates. |
| `Dalashade/PresetWriter.cs` | 1706 | Preset read/write, known shader definitions, section injection, variable planning, technique sync, and load-order optimization. |
| `Dalashade/Windows/ConfigWindow.cs` | 1662 | Settings page routing, User Mode, Developer Mode, config writes, diagnostics buttons, and explanatory text. |
| `shaders/Dalashade_SurfaceReflection.fx` | 1311 | Large experimental visual system with high conceptual complexity and known visual uncertainty. |
| `shaders/Dalashade_SceneGI.fx` | 843 | Core expensive shader path with several debug, performance, NormalField, MaterialIntent, and Dalapad gates. |

These are not automatically wrong, but they are the files most likely to slow future work or hide regressions.

## Priority Findings

Severity scale:

- P0: release blocker or safety/correctness issue.
- P1: high-priority debt likely to cause regressions, drift, support failures, or future feature friction.
- P2: maintainability or parity issue that should be addressed soon.
- P3: polish, hygiene, or future cleanup.

### P1-1: First-party shader metadata still has multiple sources of truth

Affected files:

- `Dalashade/PresetWriter.cs`
- `Dalashade/CustomShaderVariableMapper.cs`
- `Dalashade/FirstPartyShaderRegistry.cs`
- `Dalashade/CompatibilityReportExporter.cs`
- `Dalashade/DebugBundleExporter.cs`
- `shaders/*.fx`
- `docs/Shaders/*.md`

What is wrong:

The registry is a good read-only seam, but `PresetWriter` still owns `KnownCustomShaders`, section injection variables, and technique sync definitions. `CustomShaderVariableMapper` still owns generated-variable knowledge. `CompatibilityReportExporter` still owns material parity shader/channel tables. `DebugBundleExporter` owns include-file lists and bundle-specific shader scans.

Why it matters:

A new shader uniform, renamed technique, debug mode, performance-tier key, or section name can be added correctly in one location and silently drift elsewhere. The parity scan helps detect this, but only after shader files are present and diagnostics are reviewed.

Suggested fix:

Do not immediately make the registry write presets. First add tests around current output. Then migrate the registry into the source for shader families, section names, technique names, debug/manual flags, known uniforms, performance keys, and include dependencies. Keep value formulas in `PresetWriter` or a future planning service.

Before release: not a blocker if parity diagnostics are green, but it is the next most important maintainability fix.

### P1-2: Diagnostics are richer but still architecturally concentrated

Affected files:

- `Dalashade/CompatibilityReportExporter.cs`
- `Dalashade/DebugBundleExporter.cs`
- `docs/DiagnosticsModelPlan.md`

What is wrong:

The reports are much better after recent passes, but the implementation added more feature-specific code into already large exporters. The planned `DiagnosticSectionModel` is still a document, not code.

Why it matters:

Every new system must be manually threaded through compatibility report sections, debug bundle JSON files, top-level summaries, README text, and health routing. This makes support output drift likely and makes small diagnostic changes risky.

Suggested fix:

Create shared DTOs for health summary, performance summary, generated-variable summary, shader support, Dalapad status, material evidence, and technique sync. Have exporters render those models rather than rediscovering feature state independently.

Before release: not a blocker if current reports are useful, but should be done before the next major feature layer.

### P1-3: Preset writing needs a planning boundary before more features

Affected files:

- `Dalashade/PresetWriter.cs`
- `Dalashade/SceneTagRegressionHarness.cs`
- `docs/ArchitectureRefactorPlan.md`

What is wrong:

`PresetWriter` still combines several responsibilities:

- reading and validating preset lines
- resolving shader support
- choosing values
- injecting sections
- syncing techniques
- optimizing load order
- writing files and backups
- producing changed-variable diagnostics

Why it matters:

Future changes to first-party shaders, technique sync, Dalapad gates, or performance tiers can accidentally alter file-writing behavior. The base-preset safety boundary is strong, but too much behavior is coupled to the writer.

Suggested fix:

Split behavior in this order:

1. `GeneratedPresetPlan`
2. `ShaderSectionInjectionPlanner`
3. `TechniqueSyncPlanner`
4. `PresetFileWriter`

Only split after tests snapshot current output.

Before release: not required, but avoid adding new feature branches directly into `PresetWriter`.

### P1-4: Test coverage is behind the architecture

Affected files:

- `Dalashade/SceneTagRegressionHarness.cs`
- `Dalashade/PresetRegressionReportHarness.cs`
- `Dalashade/ShaderUniformParityDiagnostics.cs`
- solution/test setup generally

What is wrong:

The repo has useful harnesses, but does not appear to have a focused unit test project that exercises pure logic around config defaults, registry parity, generated preset planning, section injection, technique sync, diagnostics summaries, and Dalapad fallback invariants.

Why it matters:

The project is now large enough that behavior-preserving refactors are risky without fast tests. `dotnet test` can pass even when there are no meaningful automated tests for the most fragile seams.

Suggested fix:

Add a small test project around pure C# services before splitting files. First targets:

- default config safety
- base preset never modified
- debug shaders excluded from technique sync
- generated variable names match shader registry expectations
- generated preset writes are stable for fixture inputs
- Dalapad disabled/missing resolves to neutral generated values
- debug bundle summary models include key routing fields

Before release: not a hard blocker, but it is the most important enabler for safe cleanup.

### P1-5: Dalapad native addon has outgrown the prototype file

Affected files:

- `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp`
- `DalapadAddon/README.md`
- `DalapadAddon/CONTRACT.md`
- `Dalashade/DalapadDiagnostics.cs`
- `shaders/Dalapad_Debug.fx`

What is wrong:

The addon file now contains constants, candidate definitions, resource cataloging, status JSON, control pipe handling, ReShade addon lifecycle, runtime events, debug texture logic, copy cadence, pinned candidates, and shader uniform sync.

Why it matters:

This is the area most likely to affect FPS and account-safety concern perception. Even if behavior is gated, the implementation is hard to reason about because debug and production paths are close together in one native file.

Suggested fix:

Do not change behavior yet. First split into files after profiling and tests:

- `dalapad_status_ipc.*`
- `dalapad_control_pipe.*`
- `dalapad_resource_catalog.*`
- `dalapad_debug_visualization.*`
- `dalapad_reshade_lifecycle.*`
- `dalapad_uniform_sync.*`

Before release: not required if docs remain clear that Dalapad is optional/experimental, but should precede expanded render-layer behavior.

### P1-6: Performance diagnostics exist, but shader/addon profiling is still indirect

Affected files:

- `Dalashade/DebugBundleExporter.cs`
- `Dalashade/DalapadDiagnostics.cs`
- `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp`
- `shaders/Dalashade_SceneGI.fx`
- `shaders/Dalashade_SurfaceReflection.fx`
- `shaders/Dalashade_ContactTone.fx`
- `shaders/Dalashade_AtmosphereBloom.fx`

What is wrong:

Debug bundles now report feature gates and Dalapad production/debug cost buckets, but they do not measure GPU timings or isolate per-shader/pass cost. ReShade/in-game FPS issues still require manual correlation.

Why it matters:

Users have reported FPS drops around Dalapad and individual `.fx` files. Without per-pass timing or clear captured state, support can confuse debug-copy cost, production assist cost, shader compile/runtime cost, and unrelated preset stack cost.

Suggested fix:

Short term: add a documented manual profiling protocol using ReShade technique toggles and debug bundle export checkpoints. Medium term: if available through ReShade APIs, add optional timing/counter diagnostics without changing visual output.

Before release: acceptable if Dalapad debug visualization remains clearly manual and performance docs are honest.

## P2 Findings

### P2-1: First-party registry should separate dependency uniforms from generated uniforms

Affected file: `Dalashade/FirstPartyShaderRegistry.cs`

The registry tracks known generated uniforms, debug uniforms, and performance uniforms, but manual debug shaders and include-level uniforms blur the distinction between "plugin writes this," "shader declares this," "addon updates this," and "manual debug only." This is manageable now, but the next registry pass should use typed categories:

- generated preset uniform
- addon/status uniform
- shader-owned manual uniform
- debug-only uniform
- performance-tier uniform
- include dependency uniform

### P2-2: UI page composition improved but still mixes state mutation and explanation

Affected files:

- `Dalashade/Windows/ConfigWindow.cs`
- `Dalashade/Windows/MainWindow.cs`
- `Dalashade/Windows/FeatureStatusCard.cs`

The new User/Developer Mode wording is clearer. The remaining debt is structural: UI code still mutates config, renders low-level controls, explains behavior, routes diagnostics, and formats feature state in the same files. Future cleanup should extract view models for effect cards, health rows, first-party shader controls, Dalapad status, and scene/material evidence.

### P2-3: Debug controls are documented better than they are modeled

Affected files:

- `Dalashade/Windows/ConfigWindow.cs`
- `docs/ConfigurationParity.md`
- `docs/Shaders/*.md`
- `shaders/*.fx`

The current state is honest: some plugin UI debug controls remain integer sliders while ReShade shader UIs have named modes. This is acceptable for Developer Mode, but brittle. A future metadata table should define debug mode names once and let docs/UI/parity checks consume it.

### P2-4: Handoff docs are now partly stale

Affected file: `docs/CodexSessionHandoff.md`

The handoff still says the latest verified baseline is the earlier Dalapad shared shader include / FrameData merge era and does not fully reflect the P1/P2 diagnostics, registry, and latest pushed commit. This is not product behavior debt, but it can mislead future agents.

Suggested fix: update handoff after the next implementation pass or before a new long-running Codex session.

### P2-5: Manual changelog maintenance is showing friction

Affected file: `docs/CommitChangelog.md`

The changelog is useful, but manual timestamped entries are becoming long and easy to misorder. Keep it, but consider adding a shorter "current state" handoff section and relying on Git history for exact chronology.

### P2-6: Release artifacts and build intermediates need a firmer policy

Affected paths:

- `shaders/Dalashaders+Dalapad.zip`
- `releases/`
- `DalapadAddon/build/Dalapad.addon64`
- untracked `DalapadAddon/build/Dalapad.exp`
- untracked `DalapadAddon/build/Dalapad.lib`
- untracked `.codex-debug/`

The repo currently mixes source, release zips, tracked addon artifact, and local build intermediates. The untracked files are not a code problem, but they are recurring workspace noise.

Suggested fix:

Document exactly which build artifacts are intentionally tracked. Add ignore rules for transient native outputs and local debug extraction folders if they are not meant to be committed.

### P2-7: Shader includes are contract-heavy but still compile-time duplicated

Affected files:

- `shaders/Dalashade_FrameData.fxh`
- `shaders/Dalashade_Dalapad.fxh`
- `shaders/Dalashade_MaterialMasks.fxh`
- `shaders/Dalashade_NormalField.fxh`
- production `.fx` files

The include architecture is sound conceptually: FrameData owns normalized shader-facing data, Dalapad helpers own optional semantic texture access, MaterialMasks owns classification, NormalField owns inferred normals. The remaining debt is mechanical: each shader still declares many uniforms and settings locally. ReShade constraints may make this necessary, but parity tests should enforce declarations.

### P2-8: SurfaceReflection remains the riskiest first-party shader to keep extending

Affected file: `shaders/Dalashade_SurfaceReflection.fx`

SurfaceReflection is large, experimental, and already documented as visually uncertain. Do not continue small tweaks expecting true reflections. Treat it as either:

- a conservative water/wet/glint impression shader, or
- a future redesign once data support is mature.

Do not fold more render-layer experiments into it before profiling and debug evidence are stable.

## P3 Findings

| Finding | Affected files | Notes |
| --- | --- | --- |
| Report naming is accumulating | `docs/Audits/` | This file is `report.md` per request, but future audits should use dated names to avoid overwrites. |
| Some docs still repeat "manual" wording many times | `docs/ShaderAuthoring.md`, shader docs | Accurate, but dense. A compact quick-start matrix could reduce repetition. |
| Debug bundle file count is high | `DebugBundleExporter.cs`, `docs/DebugBundles.md` | The new `health-summary.json` helps, but support docs should keep steering users to the first 3 files. |
| FrameData prepass language is stable but frequent | shader docs | Good boundary, but prepass discussion should stay roadmap-only until tests/profiling justify it. |

## Technical Debt by Subsystem

### Core Plugin

Strong:

- Non-destructive generated-preset model remains the correct safety boundary.
- Config model is explicit and defaults are conservative.
- User/Developer Mode split is meaningful.
- Health and debug bundle outputs are now much more actionable.

Debt:

- `Plugin.cs`, UI windows, writer, diagnostics exporters, and scene services still form a broad mesh.
- Several systems know similar concepts under different names: "custom shader variables", "first-party shader support", "shader mapping", "technique sync", "generated variable summary".
- Refactors need tests first.

Recommended next move:

Add pure tests and DTO extraction, not new features.

### Preset Writing

Strong:

- Base preset is not modified.
- Generated preset is the target.
- Technique sync has explicit production/debug separation.
- Section injection is configurable.

Debt:

- Writer owns planning and I/O together.
- Known shader definitions duplicate registry.
- Changed-variable reporting is useful but tied to write execution.

Recommended next move:

Create a read-only `GeneratedPresetPlan` API that can be tested without writing files.

### Diagnostics

Strong:

- Top-level health/performance/generated-variable summaries are a major improvement.
- Dalapad production and debug cost are separated.
- Uniform parity scanner catches an important drift class.

Debt:

- Exporters are still the model.
- JSON schemas are mostly anonymous object construction.
- There is no shared diagnostic DTO source yet.

Recommended next move:

Implement `DiagnosticHealthSummary`, `DiagnosticPerformanceSummary`, and `DiagnosticGeneratedVariableSummary` as shared records consumed by both report and bundle code.

### UI / UX

Strong:

- User Mode now uses better language around first-party shader support and Dalapad.
- Developer Mode keeps low-level controls available.
- Debug controls are generally framed as developer diagnostics.

Debt:

- UI files are large.
- Controls, helper text, config mutation, and status interpretation are mixed.
- Debug modes are still numeric in plugin UI.

Recommended next move:

Extract feature/effect view models and debug mode metadata without changing controls.

### Dalapad

Strong:

- Optional.
- Production and debug paths are documented separately.
- Missing/stale/disabled data resolves neutral in shader helpers.
- Status/control IPC reports useful state without raw handles.

Debt:

- Native file is too large.
- Debug and production concepts are still close in implementation.
- Render-layer candidate names can sound more authoritative than they are.
- FPS cost still needs stronger profiling discipline.

Recommended next move:

Freeze behavior, profile, then split native modules before expanding render-layer behavior.

### Shader Stack

Strong:

- Shared contracts are clear.
- FrameData is the correct integration boundary.
- Dalapad helpers are gated and neutral.
- MaterialIntent is documented as a prior, not truth.

Debt:

- Shader files are growing with debug modes and optional paths.
- SurfaceReflection is large and uncertain.
- Repeated inline work can add cost across the stack.
- No prepass exists, which is currently fine, but repeated work should be measured.

Recommended next move:

Do not add a stack/prepass yet. First collect timing evidence and decide whether duplicated FrameData/MaterialMasks/NormalField work is actually the bottleneck.

## Recommended Fix Order

### Before Next Feature Work

1. Add a focused test project for pure plugin logic.
2. Snapshot generated preset output for representative configurations.
3. Add tests for debug shader technique-sync exclusion.
4. Add tests for registry/mapper/shader uniform parity using repo shader files.
5. Add tests for Dalapad disabled/unavailable generated values resolving neutral.
6. Define tracked vs ignored build/release artifacts.

### Next Stabilization Pass

1. Introduce shared diagnostic DTOs for health, performance, generated variables, shader support, and Dalapad cost.
2. Refactor `DebugBundleExporter` and `CompatibilityReportExporter` to consume those DTOs for one or two sections first.
3. Split `GeneratedPresetPlan` from file writing.
4. Move first-party shader metadata gradually toward `FirstPartyShaderRegistry` after tests are green.
5. Update `docs/CodexSessionHandoff.md` to reflect the current post-P1/P2/registry baseline.

### Later Architecture Work

1. Split `PresetWriter` into planner/injection/sync/file-writer services.
2. Split `ConfigWindow` and `MainWindow` around feature view models.
3. Split Dalapad native addon into IPC, lifecycle, resource catalog, debug visualization, and uniform sync modules.
4. Revisit a prepass only with profiling evidence.
5. Reassess SurfaceReflection as a deliberate redesign, not incremental tuning.

## What Not To Do Yet

- Do not add `Dalashade_Stack.fx`.
- Do not add a prepass until profiling and tests justify it.
- Do not expand Dalapad render-layer behavior while the native addon is still a single large mixed-responsibility file.
- Do not make Dalapad required.
- Do not auto-modify third-party shaders.
- Do not use SurfaceReflection as the landing zone for every render-layer experiment.
- Do not move writer behavior to the registry until generated output is protected by tests.

## Remaining Risk Counts

| Severity | Count | Summary |
| --- | ---: | --- |
| P0 | 0 | No release-blocking safety/correctness issue found in this read-only pass. |
| P1 | 6 | Metadata drift, diagnostic concentration, writer coupling, test gap, Dalapad addon bloat, profiling weakness. |
| P2 | 8 | Registry category clarity, UI structure, debug control metadata, stale handoff, changelog friction, artifact policy, shader duplication, SurfaceReflection risk. |
| P3 | 4 | Naming/report hygiene, docs repetition, bundle file count, roadmap wording. |

## Suggested Next Implementation Prompt

Implement a behavior-preserving stabilization pass focused on tests and diagnostic-model extraction. Add a small test project for generated preset planning, first-party registry/mapper parity, debug shader technique-sync exclusion, and Dalapad neutral fallback values. Then introduce shared diagnostic DTO records for health summary, performance summary, generated-variable summary, and shader support state, and route one compatibility report section plus the matching debug bundle JSON through those DTOs. Do not change generated preset output, shader visuals, Dalapad render-layer behavior, or technique activation defaults. Run `dotnet build`, `dotnet test`, and `git diff --check`, and report any output/schema changes explicitly.

