# Dalashade Architecture, Quality, UX Parity, and Performance Audit

Audit date: 2026-06-21

Scope: current repository state, including the Dalamud plugin, generated preset pipeline, first-party shaders, shared shader includes, Dalapad addon scaffold/bridge, diagnostics, UI controls, documentation, and release metadata.

Constraints followed: this is an audit only. No runtime behavior, shader behavior, generated preset behavior, release packaging, or visual tuning was changed.

Severity scale:

- P0 = release blocker / correctness or safety issue
- P1 = high priority / likely user-facing confusion or broken behavior
- P2 = medium priority / inconsistency or maintainability issue
- P3 = low priority / polish or future cleanup

Finding summary: 0 P0, 9 P1. The system is not blocked from internal testing, but it should not be treated as release-polished until the P1 items are addressed or explicitly accepted.

## Executive Summary

Dalashade is now a layered product rather than a single preset writer. The current architecture has strong safety instincts: it writes a generated preset instead of modifying the base preset, keeps Dalapad optional, keeps raw render-target handles out of IPC, and has unusually rich diagnostics for an experimental graphics plugin. The main risk is no longer "can this be done?" It is "can this be kept understandable, performant, and truthfully described as the system grows?"

The most important architectural shape is:

`GameContext / SceneTags -> SceneIntent -> MaterialProfile -> ScreenshotMaterialEvidence -> MaterialIntent -> VisualProfile -> PresetWriter -> generated ReShade preset -> first-party shaders -> FrameData / MaterialMasks / NormalField / Dalapad`

That map is mostly real, but boundaries are blurred in several places:

- `Plugin.cs` orchestrates nearly every high-level subsystem.
- `PresetWriter.cs` owns write safety, shader section injection, technique sync, custom shader definitions, defaults, and load-order optimization.
- `CompatibilityReportExporter.cs` and `DebugBundleExporter.cs` each know too much about every feature.
- Shader contracts exist, but production shaders still carry duplicated uniforms and contract assumptions.
- Dalapad is optional and safety-gated, but its debug/copy path can add cost and the production meaning of "normal", "albedo", "water", and "reflection" is still candidate evidence, not ground truth.

The highest-priority fixes before future feature work are:

- Make the performance story measurable and explicit, especially Dalapad copy cadence and NormalField/FrameData duplicate work.
- Create parity checks for config, UI, generated variables, shader uniforms, diagnostics, and docs.
- Split responsibilities at service boundaries before adding more shader features.
- Tighten user-facing wording so inferred material evidence, Dalapad pinned candidates, and debug views are never presented as exact engine truth.
- Validate release packaging against the manifest and shader/addon bundle expectations.

## Current Architecture Map

### Plain-Term Flow

1. `GameContext.cs` reads territory, weather, time, combat, GPose/cutscene, and related runtime context.
2. `SceneTags` and `SceneAuthoringService.cs` combine automatic classification with optional user-authored overrides and tag registry tuning.
3. `SceneIntent.cs` converts tags, screenshot opinions, style mode, performance budget, and authoring diagnostics into normalized scene channels.
4. `MaterialProfileBuilder.cs` creates broad material priors from tags, context, scene intent, and screenshots.
5. `ScreenshotMaterialEvidence.cs` creates weak visible-material evidence from screenshot metrics and scene context.
6. `ScreenshotMaterialEvidenceIntentAdapter.cs` can contribute capped screenshot evidence into `MaterialIntent` when explicitly enabled.
7. `MaterialIntentBuilder.cs` combines material profile, tag registry tuning, screenshot evidence, and scene context into normalized material intent channels.
8. `VisualProfile.cs` creates shader-facing target values from scene intent, material intent, style, and performance choices.
9. `PresetWriter.cs` reads the base preset, applies mapped values, optionally injects first-party shader sections, optionally syncs techniques, optionally optimizes load order, writes only the generated preset, and optionally creates backups.
10. First-party shaders read generated uniforms and shared includes.
11. `Dalashade_FrameData.fxh` merges scene/material fields, NormalField, and Dalapad surface evidence into shader-facing structures.
12. `Dalashade_MaterialMasks.fxh` classifies material/water/receiver behavior.
13. `Dalashade_NormalField.fxh` infers screen-space normals and relief from depth/color/luma.
14. `Dalashade_Dalapad.fxh` reads optional Dalapad semantic textures through gates and returns zero-confidence data when unavailable.

### Actual Boundaries

| Boundary | Current owner | Status |
| --- | --- | --- |
| Runtime scene capture | `GameContext.cs` | Stable enough, direct Dalamud/Lumina dependency. |
| Scene tag override and registry | `SceneAuthoringService.cs`, `SceneTagOverride.cs` | Useful but broad; becoming its own domain. |
| Scene intent | `SceneIntent.cs` | Strong central layer. |
| Material profile / material intent | `MaterialProfile*`, `MaterialIntent*`, `ScreenshotMaterialEvidence*` | Conceptually strong but user-facing truth needs careful wording. |
| Generated preset writing | `PresetWriter.cs` | Safety-oriented, too concentrated. |
| Custom shader variable mapping | `CustomShaderVariableMapper.cs` | Centralized but manually mirrored with writer injection lists and docs. |
| Shader shared contracts | `Dalashade_FrameData.fxh`, `Dalashade_MaterialMasks.fxh`, `Dalashade_NormalField.fxh`, `Dalashade_Dalapad.fxh` | Correct direction, needs stricter consumption rules. |
| Dalapad IPC and diagnostics | `DalapadDiagnostics.cs`, `DalapadIpcClient.cs`, `DalapadAddon/` | Good safety posture, still experimental and performance-sensitive. |
| Diagnostics | `CompatibilityReportExporter.cs`, `DebugBundleExporter.cs` | Very useful, but too verbose and duplicated. |
| UI | `Windows/ConfigWindow.cs`, `Windows/MainWindow.cs`, `SceneAuthoringWindow.cs` | User/developer split exists, but parity and labeling are uneven. |

### Blurred Responsibilities

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `Plugin.cs`, `PresetWriter.cs`, `CompatibilityReportExporter.cs`, `DebugBundleExporter.cs` | Major orchestration and feature knowledge is concentrated in a few large classes. | Every new feature must touch many hot files, increasing regression risk. | Split orchestration, write planning, diagnostics planning, and export formatting into focused services after this audit. | Yes, at least plan and start with low-risk seams. |
| P1 | `PresetWriter.cs`, `CustomShaderVariableMapper.cs`, shader docs | Shader variables are manually enumerated in multiple places. | Uniform drift can produce UI options that do nothing or shader uniforms that are never written. | Add a generated-variable parity check/harness that compares writer definitions, mapper keys, shader uniforms, and docs. | Yes. |
| P2 | `SceneAuthoringService.cs`, `MaterialIntentBuilder.cs`, `MaterialTagRegistryDiagnostics.cs` | Scene authoring owns both user override flow and material-tuning influence. | Tag editing and material calibration have different risk models. | Split tag override storage from material registry/tuning evaluation. | Soon. |
| P2 | `CompatibilityReportExporter.cs`, `DebugBundleExporter.cs` | Diagnostic output is assembled independently in two places. | Report and bundle parity will keep drifting. | Build shared diagnostic section models, then render to Markdown/JSON. | Soon. |

## Major Strengths

- Generated-preset safety is strong: base and generated paths are checked, the generated preset is the write target, backups are optional, temp writes are used before replacement, and third-party shader files are not modified.
- The user/developer UI split is meaningful. User Mode covers setup, look, scene awareness, effects, and health. Developer Mode exposes shader mapping, diagnostics, Dalapad, and advanced controls.
- Shared shader includes now exist for the right concepts: FrameData, Dalapad, MaterialMasks, and NormalField.
- Dalapad is optional and treated as experimental. Shader integration has global gates, surface-data gates, and shader-local confidence behavior.
- Diagnostics are unusually complete: compatibility reports and debug bundles include scene context, material evidence, MaterialIntent, NormalField, FrameData, Dalapad, first-party performance, shader stack summaries, and changed variables.
- Debug shaders exist for major shared contracts: material, normals, FrameData, and Dalapad.
- Performance tiers exist and Quality preserves current scalar behavior by using scale 1.0 values.
- The addon bridge avoids sending raw handles over IPC and uses addon-owned debug copies for shader-visible candidates.

## Major Risks

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp`, `Dalashade_Dalapad.fxh`, `Dalashade_FrameData.fxh` | Dalapad can add work on top of inferred paths instead of replacing them, especially while debug copies are active. | Users are already reporting large FPS drops; optional data must be cheaper or clearly optional. | Add explicit Dalapad performance diagnostics, copy cadence controls, and a production mode that avoids broad debug scanning/copying unless debug visualization is enabled. | Yes. |
| P1 | `Dalashade_NormalField.fxh`, `Dalashade_FrameData.fxh`, production `.fx` files | NormalField and FrameData work can be recomputed per shader. | Multiple first-party shaders compound backbuffer/depth taps and luma/relief sampling. | Keep current behavior, but prioritize a measured prepass/stack design proposal after release readiness items. | Yes for measurement, later for prepass. |
| P1 | `docs/ShaderAuthoring.md`, `PresetWriter.cs` | Some docs say generated section injection does not enable techniques automatically, but technique sync can now auto-activate production first-party techniques when enabled. | Users and future agents can misunderstand whether debug/production effects will appear. | Update wording to describe `SyncDalashadeTechniqueActivation`, debug exclusions, and `Dalapad_Debug.fx` being manual. | Yes. |
| P1 | `repo.json`, `releases/`, `shaders/`, `DalapadAddon/build/` | Local release artifacts do not obviously match the v4 manifest URL/package expectations. | A release can install stale plugin files or omit shader/addon bundle contents. | Run and document a release package validation pass before release. | Yes. |
| P2 | UI labels and docs around MaterialIntent/Dalapad | Some labels are short enough that inferred evidence may look like engine truth. | Users may tune effects around false assumptions. | Rename or subtitle high-risk terms as "inferred", "candidate", or "debug evidence" in UI/docs. | Soon. |

## System Parity Findings

| ID | Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- | --- |
| SP-1 | P1 | `Configuration.cs`, `Windows/ConfigWindow.cs`, `CustomShaderVariableMapper.cs`, `DebugBundleExporter.cs` | Settings do not all have the same lifecycle: some are in UI, some in diagnostics, some in bundles, and some only affect generated variables. | Users and maintainers cannot always answer "what is active and why?" from one report. | Add a config/UI/diagnostic parity table generated by code or a small test. | Yes. |
| SP-2 | P1 | `PresetWriter.cs`, shader files | Generated variables and shader uniforms are manually synchronized. | Missing keys silently degrade or create dead UI controls. | Add a uniform scanner parity harness for first-party `.fx` and `.fxh` files. | Yes. |
| SP-3 | P2 | `Windows/ConfigWindow.cs`, shader files | Debug controls vary between sliders, dropdown-like integer sliders, booleans, opacity controls, and boost controls. | Debug workflows feel inconsistent and are harder to document. | Standardize debug mode controls as named dropdowns plus optional opacity/boost where useful. | Soon. |
| SP-4 | P2 | `PresetWriter.cs` | Production first-party shaders are auto-activatable; Material/Normal/FrameData debug shaders are excluded; `Dalapad_Debug.fx` is not in `KnownCustomShaders`. | This is mostly correct, but easy to misread. | Document and test the auto-activation allow/deny list. | Yes. |
| SP-5 | P2 | `CompatibilityReportExporter.cs`, `DebugBundleExporter.cs` | Compatibility reports and debug bundles overlap heavily but do not share a common section model. | One can report a value or safety claim that the other omits. | Add shared diagnostic DTOs with Markdown/JSON renderers. | Soon. |
| SP-6 | P2 | `CustomShaderVariableMapper.cs`, docs | Quality/Balanced/Performance values are implemented, but docs and UI should make per-shader impact easier to inspect. | Performance mode may feel vague if users cannot see which shader was reduced. | Include tier values and changed variables in a compact debug bundle summary. | Soon. |
| SP-7 | P3 | `docs/*`, UI labels | Some feature names use `NormalField`, `MaterialIntent`, and `Dalapad surface data` without the same wording in every place. | Searchability and user understanding suffer. | Adopt one glossary and reuse labels. | No. |

## UI / UX Consistency Findings

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `Windows/ConfigWindow.cs` | User Mode still exposes technical toggles such as "Enable Dalashade custom shader variables", "Enable SceneGI variable writes", and "Enable Normal Field Shader Mapping". | Normal users can enable plumbing without understanding shader consequences. | Replace with task-based controls, for example "Use Dalashade first-party shader support", with advanced details collapsed. | Yes. |
| P1 | `Windows/ConfigWindow.cs` | Dalapad global opt-in and surface-data opt-in are clear, but debug/testing concepts are still close to production shader support. | Users may enable expensive debug behavior while trying to improve visuals. | Keep Dalapad debug visualization and resource shape probe developer-only and visually separate from production shader assist. | Yes. |
| P2 | `Windows/ConfigWindow.cs` | Debug modes use integer sliders in developer pages instead of named dropdowns. | Numeric modes are hard to operate and hard to screenshot/report. | Use named enum dropdowns for SceneGI, ContactTone, SurfaceReflection, MaterialDebug, NormalDebug, FrameDataDebug, and Dalapad_Debug. | Soon. |
| P2 | `Windows/ConfigWindow.cs` | SceneGI has extensive debug modes while other production shaders expose fewer or differently named debug controls. | Feature parity is unclear. | Standardize "Off, contribution, mask, input evidence, final delta" where applicable. | Soon. |
| P2 | `Windows/ConfigWindow.cs`, docs | First-party performance tiers exist, but the user needs an obvious short explanation of the active tier in generated output and bundles. | Users comparing FPS changes need a clear "why changed" record. | Show selected tier and per-shader expected behavior in User Health and bundle manifest. | Soon. |
| P3 | `Windows/ConfigWindow.cs` | Some normal-user helper text says "variables" and "mapping", which are developer terms. | It weakens the User Mode simplification goal. | Use "shader support", "optional surface detail", and "debug evidence" in User Mode. | No. |

## Shader Contract Findings

The shader contract direction is good:

- `Dalashade_FrameData.fxh` owns normalized shader-facing scene/base/surface fields.
- `Dalashade_Dalapad.fxh` owns semantic Dalapad texture reads and zero-confidence fallbacks.
- `Dalashade_MaterialMasks.fxh` owns material, water, receiver, and safety classification.
- `Dalashade_NormalField.fxh` owns inferred screen-space normal and relief estimation.

The weak point is enforcement. Production shaders include the shared files, but each `.fx` still carries a large set of uniform declarations and local interpretations. There is no automated contract test that proves each generated key exists, each shader-side key is writable or intentionally manual, and each debug mode is documented.

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `shaders/Dalashade_*.fx`, `CustomShaderVariableMapper.cs`, `PresetWriter.cs` | Contract parity is manual. | A small uniform rename can silently break a shader path. | Add shader uniform scanning and generated-key parity checks. | Yes. |
| P1 | `Dalashade_FrameData.fxh`, production shaders | FrameData can centralize more, but production shaders still do local material/surface interpretation. | Contract drift increases as more data sources are added. | Define a rule: production shaders consume `FrameData`/`MaterialMasks`/`NormalField`/`Dalapad` helpers, not raw duplicated classifier logic, unless explicitly justified. | Yes. |
| P2 | `Dalashade_SceneGI.fx` | SceneGI has Dalapad debug modes, but the user observed missing masks in SceneGI debug. | Debug output may not prove shader integration is working. | Add a validation mode that shows "global off", "local off", "available but stale", and "sampled confidence" as distinct colors. | Soon. |
| P2 | `Dalashade_Dalapad.fxh` | Pinned candidate names imply useful semantics but are still candidate evidence. | Future shader tuning may overtrust them. | Keep helper result fields named as evidence/confidence, not truth. | Yes. |
| P2 | `Dalashade_NormalField.fxh` | It is powerful but expensive and inferred. | Users may treat it as real engine normals. | Keep docs/UI explicit: inferred normals, optional, zero when off. | Yes. |

## Dalapad Findings

Dalapad currently has three distinct layers:

1. Plugin diagnostics: `DalapadDiagnostics.cs` and `DalapadIpcClient.cs` read status-file IPC, control-pipe IPC, resource catalog rows, and optional developer shape probes.
2. Addon bridge: `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp` observes render targets from ReShade events and can copy candidates into addon-owned shader resources for debug visualization.
3. Shader helpers: `Dalashade_Dalapad.fxh`, `Dalashade_FrameData.fxh`, and `Dalapad_Debug.fx` consume semantic pinned resources through availability and dimension gates.

### Safety Boundary Checks

| Claim | Current assessment | Evidence / caveat |
| --- | --- | --- |
| Base preset is not modified | True by design | `PresetWriter.cs` validates base/generated paths and writes generated path through temp replacement. |
| Generated preset is the only write target | True for preset writing | Release packaging and shader copying are separate workflows. |
| Backups are created when configured | True | `WriteBackups`, `CreateBackupPath`, and pruning logic exist. |
| Dalapad does not make shaders fail when unavailable | Mostly true | Shader helpers return zero-confidence/neutral values; generated variables can also resolve disabled. Needs compile validation after every shader change. |
| Dalapad data resolves neutral when missing/stale/disabled | True by contract | `Dalashade_Dalapad.fxh` and `Dalashade_FrameData.fxh` gate by enabled, strength, availability, dimensions, confidence. |
| Raw game resource handles are not sent over IPC | True | Addon status and pipe output describe debug observations/copies and explicitly avoid raw handles. |
| Debug shaders are not auto-enabled | Mostly true | Material/Normal/FrameData debug shaders are excluded from auto-activation. `Dalapad_Debug.fx` is not in `KnownCustomShaders`. |
| Third-party shaders are not modified | True | Preset variables only; no third-party file rewrite path found. |
| Third-party shaders do not magically consume Dalapad | True | Dalapad helpers are only in first-party shader code. |
| Material intent is not true material ID | True in docs/diagnostics, needs UI care | Several docs say inferred; user labels should keep that caveat visible. |
| Diffuse/albedo is not material ID truth | True by current framing | Pinned albedo/luma is candidate evidence only. |
| Source/emissive data is not receiver proof | True by current framing | Needs to remain explicit in reflection docs. |
| Water source/horizon data is not water receiver proof | True by current framing | Candidate water/reflection source should not become a receiver mask without additional gates. |
| RenderTargetManager/GBuffer usage is developer/bridge-controlled | True | Shape probe is developer-only; addon copy path is bridge/debug controlled. |

### Dalapad Risk Findings

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp` | Debug copy path scans up to 8 groups x 8 MRT slots and copies every 2 frames when active. | This can be a material FPS cost, especially if debug visualization remains enabled. | Add explicit addon modes: metadata-only, pinned-only, debug-scan. Default to metadata-only or pinned-only for normal users. | Yes. |
| P1 | `DebugBundleExporter.cs`, `CompatibilityReportExporter.cs` | Diagnostics show availability/copy state, but do not summarize expected FPS cost or current copy cadence. | User reports of FPS loss need direct evidence. | Bundle `copyFrameInterval`, observed source count, copied source count, active mode, and whether debug shader is enabled. | Yes. |
| P2 | `DalapadIpcClient.cs`, `DalapadDiagnostics.cs` | Status-file and control-pipe data are diagnostic, while shader resources are ReShade semantic bindings. | Users may assume IPC is feeding shader data directly. | Add one-line docs/report wording: IPC reports health; semantic textures feed shaders. | Soon. |
| P2 | `Dalashade_Dalapad.fxh` | Pinned candidates are useful enough for shaders, but the contract still needs a "confidence first" consumption style. | Shader authors may bypass gates for stronger visuals. | Make helper functions the only approved production access path. | Yes. |

## Material Evidence / MaterialIntent Findings

MaterialIntent is the correct abstraction for first-party shader flexibility. It intentionally sits above true material IDs and should stay that way. Screenshot material evidence is useful because it helps catch scene mismatches, but it is weak evidence from visible color/luma/region/opinion analysis.

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `ScreenshotMaterialEvidence.cs`, `MaterialIntentBuilder.cs`, docs/UI | Material evidence can look more authoritative than it is. | Users may overfit to screenshot color or assume albedo/material truth. | Keep all UI/report labels as "inferred material evidence" and show confidence/mismatch warnings. | Yes. |
| P2 | `MaterialTagRegistryDiagnostics.cs`, `SceneAuthoringService.cs` | Tag registry tuning is powerful but close to scene authoring UX. | Users can change scene semantics and material behavior in one conceptual area. | Split "scene tags" from "material tuning" in UI and docs. | Soon. |
| P2 | `DebugBundleExporter.cs` | Bundle has material profile, material intent, calibration, evidence, and parity outputs. | It is complete but hard to scan. | Add one `material-summary.json` or top-level summary Markdown. | Soon. |
| P3 | `docs/MaterialIntent.md`, shader docs | The docs are thorough but spread across many files. | New shader authors need a short contract. | Add a one-page "MaterialIntent shader author quick contract." | No. |

## Preset Writing and Technique Sync Findings

The generated preset writer is safety-focused and currently handles:

- base/generated path validation
- preset parsing and section updates
- mapped third-party variables
- first-party custom shader variables
- optional section injection
- optional technique activation sync
- optional load-order optimization
- backup and pruning behavior
- temp write before replacement

This is correct behaviorally, but it is too much responsibility for one class.

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `PresetWriter.cs` | Writer owns too many concerns. | Write safety and shader feature behavior are coupled. | Split into `PresetReadModel`, `GeneratedPresetPlan`, `ShaderSectionInjectionPlanner`, `TechniqueSyncPlanner`, and `PresetFileWriter`. | Start soon; do not block on full split. |
| P1 | `PresetWriter.cs`, docs | Technique sync behavior is not fully reflected in older docs. | Users may not know when production techniques are auto-added. | Update docs and add a test around debug exclusion and production activation. | Yes. |
| P2 | `PresetWriter.cs`, `CustomShaderVariableMapper.cs` | Default injected values are separate from mapper values. | Injection defaults can diverge from generated values. | Centralize defaults in one first-party shader contract table. | Soon. |
| P2 | `PresetAnalyzer.cs` | Compatibility warnings include load-order and role risks, but future shader roles require manual updates. | New shaders can be missed by analysis. | Add first-party shader registry metadata consumed by analyzer, writer, docs, and bundle. | Later. |

## Performance Findings

### Likely Hotspots

| Area | Why it is hot | Current controls |
| --- | --- | --- |
| `Dalashade_SurfaceReflection.fx` | Multiple reflection source samples, pseudo-SSR, water column/mirror projection, safety gates, depth gates, and sample-quality branches. | `Dalashade_ReflectionSampleQuality`, tier scales. |
| `Dalashade_SceneGI.fx` | Screen-space GI and AO sampling plus debug paths and optional surface data. | `Dalashade_GISampleCountScale`, `Dalashade_GISampleDistanceScale`, tier scales. |
| `Dalashade_NormalField.fxh` | Many backbuffer/depth/luma taps for detail, relief, and texture-gradient normals. | NormalField global gate, strength, detail strength, tier scales. |
| Dalapad addon debug copy | Observes and copies render-layer candidates every few frames when debug visualization is active. | Current constants in addon source, not user-exposed. |
| `Dalashade_ContactTone.fx` | Contact radius, edge/structure evaluation, optional NormalField/FrameData. | Contact tone radius scale. |
| `Dalashade_AtmosphereBloom.fx` | Bloom sampling and optional quality scale. | `Dalashade_BloomSampleQuality`. |

### Easy Wins

- Add a diagnostic "active cost summary" to debug bundles: first-party tier, enabled first-party techniques, NormalField enabled/mapping, Dalapad copy mode, copied candidate count, and per-shader sample quality values.
- Make Dalapad debug copy mode visible and default it away from broad scanning for non-debug usage.
- Ensure Performance tier actually avoids expensive inferred work when Dalapad confidence is available, instead of adding Dalapad and NormalField work together.
- Add named UI text for Quality/Balanced/Performance and include the selected tier in User Health.
- Add shader compile and uniform parity validation to CI or release checklist.

### Medium-Risk Optimizations

- Skip NormalField detail/relief work early when tier, strength, or confidence gates make it unable to affect output.
- Cache or share material/receiver classification inside shader functions where repeated.
- Split SurfaceReflection into strict quality branches that avoid calling high-cost functions when sample quality is below thresholds.
- Add a Dalapad pinned-only production path that reads only selected semantic resources and does not run scan-grid debug copies.
- Make debug modes compile or branch as cheaply as possible when off.

### Architectural Optimizations

- A future `Dalashade_Stack.fx` or prepass could be justified if multiple first-party effects remain enabled together and re-resolve the same FrameData/MaterialMasks/NormalField state per pass.
- A prepass should not be implemented yet. It needs evidence: per-shader timing, enabled-technique combinations, and before/after cost of shared surface fields.
- First split contracts and diagnostics, then consider a stack/prepass design.

### Not Worth Optimizing Yet

- Small scalar-only shaders like `Dalashade_AdaptiveGrade.fx`, `Dalashade_SmartSharpen.fx`, and `Dalashade_WeatherAtmosphere.fx` unless profiling proves otherwise.
- Release packaging scripts before the shader/addon content contract is frozen.
- Third-party shader performance, beyond load-order guidance and documentation. Dalashade should not fork third-party shaders automatically.

## Diagnostics and Debug Bundle Findings

Diagnostics are broad and useful, but they need summarization and parity.

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `DebugBundleExporter.cs`, `CompatibilityReportExporter.cs` | Reports are rich but not optimized for the first question: "why is this slow or not working?" | Debug bundles become hard to use under pressure. | Add top-level `health-summary.json` and `performance-summary.json`. | Yes. |
| P1 | `DebugBundleExporter.cs` | Bundle includes first-party performance values, but not enough expected-behavior prose per shader. | The user asked for selected profile, generated uniform value, and shader expected behavior summaries. | Add a concise generated-variable and expected-behavior summary. | Yes. |
| P2 | `CompatibilityReportExporter.cs` | Safety claims are present but sometimes spread across Dalapad, FrameData, material parity, and stack sections. | A reader cannot quickly see why a path is safe. | Add one "Safety boundary" table mirroring this audit. | Soon. |
| P2 | `DebugBundleExporter.cs` | Debug bundle has many JSON files with overlapping material concepts. | Complete but overwhelming. | Add index and summaries before raw details. | Soon. |
| P3 | `CompatibilityReportExporter.cs` | Some wording is developer-heavy. | User Mode health output should be simpler. | Separate user summary from developer detail. | No. |

## Documentation and Release Findings

The docs are extensive and generally careful, especially around optional Dalapad and inferred material limits. The main issue is drift from fast-moving implementation.

| Severity | Affected files | What is wrong | Why it matters | Suggested fix | Before release |
| --- | --- | --- | --- | --- | --- |
| P1 | `repo.json`, `releases/`, release docs | Manifest points to v4.0 download URLs, but local `releases/` contains older zip names. | Maintainers need to verify actual release artifact contents and URL state. | Run release validation against the real package and update local release docs. | Yes. |
| P1 | `docs/ShaderAuthoring.md`, `docs/PresetWriting.md` | Some wording predates optional technique sync. | Incorrect docs can lead to accidental technique activation assumptions. | Update docs to describe sync gates and debug exclusions. | Yes. |
| P2 | `README.md`, `docs/Dalapad.md`, `DalapadAddon/README.md` | Dalapad is described as scaffold/experimental; user manually released addon builds may make this partially stale. | Docs must match release packaging exactly. | Decide whether Dalapad is packaged, optional separate, or developer-only for v4, then align wording. | Yes. |
| P2 | `docs/CommitChangelog.md`, docs index | Documentation is complete but hard to use as a release readiness checklist. | Future agents may miss the highest-risk files. | Link this audit and add release-readiness checklist references. | Soon. |

### Release Readiness

Ready:

- Generated-preset safety model.
- Optional first-party shader support model.
- Material/NormalField/Dalapad caveats in core docs.
- Diagnostic bundle breadth.
- User/developer UI split foundation.

Risky but acceptable for internal testing:

- Dalapad pinned candidate exploration and debug visualization.
- SceneGI/SurfaceReflection optional Dalapad assist.
- Performance tiers if documented as experimental and Quality remains default.
- MaterialIntent as broad scene-level shader influence.

Must fix before release:

- Validate v4 package contents and manifest download links.
- Document technique sync accurately.
- Add/verify debug bundle performance and generated-variable summaries.
- Confirm Dalapad addon default mode is not expensive for normal users.
- Add uniform/generated-variable parity validation or manually complete it before release.

Should fix after release:

- Split large orchestrator/writer/exporter classes.
- Convert debug integer sliders to named dropdowns.
- Add shared diagnostic DTOs.
- Evaluate prepass/stack only after profiling evidence.

## Test Coverage Gaps

| Priority | Group | Tests that should exist |
| --- | --- | --- |
| P1 | Config defaults | Verify base safety defaults, Dalapad off by default, debug paths off/manual, Quality tier default, User Mode defaults. |
| P1 | Preset writing | Base preset never modified, generated path only, temp write replacement, backup count pruning, inactive write mode behavior. |
| P1 | Generated variables | Mapper keys match shader uniforms, injection lists match mapper keys, no orphan generated keys. |
| P1 | Technique sync | Production first-party techniques can be synced only when gates allow; debug shaders are never auto-enabled; `Dalapad_Debug.fx` remains manual. |
| P1 | Shader section injection | Injection respects global custom shader gate, MaterialIntent gate, NormalField gate, and Dalapad gates. |
| P1 | Dalapad fallback | Missing/stale/disabled Dalapad produces zero-confidence shader values and neutral debug masks. |
| P1 | Debug bundle/report parity | Same selected tier, generated variables, Dalapad state, material evidence, and technique sync state appear in report and bundle. |
| P2 | UI/control parity | Every config field is either user-facing, developer-facing, diagnostics-only, or hidden with reason. |
| P2 | Material evidence | Screenshot evidence caps, mismatch warnings, and no true-material-ID claims. |
| P2 | Shader compile/fallback | Compile every first-party shader with default and generated settings, including debug shaders. |
| P2 | Release packaging | Manifest version, plugin JSON, DLL, shader bundle, addon bundle, docs, and release zip contents. |

## Prioritized Fix List

### Must Fix Before Release

1. P1: Add a generated-variable and shader-uniform parity validation pass.
2. P1: Add a debug bundle performance summary with active tier, enabled shader features, Dalapad copy mode, and copied candidate count.
3. P1: Validate and document v4 release package contents against `repo.json`.
4. P1: Update docs for optional technique sync and debug shader exclusion.
5. P1: Make Dalapad normal-user mode cheap by default; keep broad scan/copy debug behavior developer-only.
6. P1: Add a clear UI/diagnostic distinction between production Dalapad assist and debug visualization.
7. P1: Ensure SceneGI Dalapad debug modes distinguish "disabled", "missing", "stale", and "sampled zero confidence".
8. P1: Add a first-party performance expected-behavior summary to diagnostics.
9. P1: Keep all material/albedo/water/reflection wording as inferred or candidate evidence.

### Should Fix Soon

1. Split `PresetWriter.cs` into planning and file-write layers.
2. Split compatibility/debug bundle section construction into shared diagnostic DTOs.
3. Convert debug integer sliders to named dropdowns.
4. Add a config/UI/diagnostic parity table.
5. Add a one-page shader-author contract for FrameData, MaterialMasks, NormalField, and Dalapad.
6. Add profiling instructions and expected FPS-cost fields to Dalapad diagnostics.
7. Clarify whether Dalapad addon binaries are part of the public release package.

### Later Architectural Work

1. Evaluate a first-party stack/prepass only after profiling shows repeated FrameData/NormalField cost across active shaders.
2. Move shader registry metadata into a single source consumed by writer, mapper, analyzer, docs, and diagnostics.
3. Split scene authoring from material registry/tuning UI.
4. Consider third-party helper injection only as documented authoring guidance, not automatic third-party shader rewriting.
5. Add optional shader compile automation outside the Dalamud build.

## Recommended Refactor Plan

Phase 1: Stabilize contracts and validation.

- Add parity validation for config/UI/generated uniforms/shader uniforms/docs.
- Add bundle summaries for safety, performance, and generated variables.
- Update docs that drifted from technique sync and Dalapad packaging state.
- Keep all runtime behavior unchanged.

Phase 2: Reduce high-risk concentration.

- Extract `GeneratedPresetPlan` from `PresetWriter.cs`.
- Extract shader registry metadata from writer hardcoded lists.
- Extract diagnostic section models shared by reports and debug bundles.
- Add small tests around path safety, technique sync, and variable parity.

Phase 3: Make Dalapad production mode distinct from debug mode.

- Add addon mode reporting and configuration.
- Default normal users to cheap metadata/pinned-only behavior.
- Keep broad scan/quad debug visualization developer-only.
- Add timing/copy count diagnostics.

Phase 4: Profile first-party shaders.

- Measure Quality/Balanced/Performance with and without Dalapad and NormalField.
- Identify repeated FrameData/NormalField work across active shader stacks.
- Only then design a prepass or stack shader.

Phase 5: Future shader architecture.

- Move production shaders toward `FrameData` and `MaterialMasks` as the only shared classification path.
- Keep Dalapad raw semantic reads inside `Dalashade_Dalapad.fxh`.
- Add per-shader debug modes that prove inputs, gates, and final contribution.

## Recommended Validation Matrix

| Scenario | Expected result |
| --- | --- |
| Default config, no Dalapad addon | Build succeeds, generated preset writes, Dalapad diagnostics report unavailable, shaders remain neutral. |
| Dalapad addon loaded, shader integration off | Diagnostics can report addon health; first-party shader Dalapad masks show no enabled data. |
| Dalapad addon loaded, shader integration on, surface data off | IPC/debug data may exist; production shaders do not use Dalapad surface data. |
| Dalapad surface data on, no semantic resources bound | Shader helpers return zero confidence; debug modes distinguish missing resource from disabled gate. |
| Quality tier | Current scalar behavior preserved. |
| Balanced tier | Only optional expensive helpers reduce: NormalField detail, reflection quality, GI count/distance, contact radius, bloom quality. |
| Performance tier | Cheaper paths preferred; no visual intensity compensation; Dalapad should reduce inferred work where confidence is available. |
| Auto-injection on, technique sync off | Sections can be injected, techniques are not auto-activated. |
| Auto-injection on, technique sync on | Production techniques can sync; debug shaders remain manual. |
| Debug bundle export | Includes selected tier, generated variables, expected behavior summary, Dalapad copy/cadence state, and safety summary. |
| Release package validation | Manifest version and download zip contain plugin DLL, manifest, required shaders/docs, and optional addon package exactly as documented. |

## Appendix A: Shader-by-Shader Audit Table

| Shader/include | Responsibilities | Main inputs | Generated variables | Debug modes | FrameData usage | Dalapad usage | MaterialIntent usage | NormalField usage | Safety gates and known risks |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Dalashade_FrameData.fxh` | Normalize shader-facing scene/base/surface data. | Backbuffer, depth, MaterialMasks, NormalField, Dalapad. | Shared uniforms from multiple shaders. | Via `Dalashade_FrameDataDebug.fx`. | Owner. | Calls Dalapad helper and merges confidence. | Carries material fields into frame structures. | Calls NormalField helper. | Strong neutral fallback; risk is repeated per-shader resolve cost. |
| `Dalashade_Dalapad.fxh` | Read semantic Dalapad resources behind gates. | `DALAPAD_PINNED_*` textures, availability uniforms, dimensions, local gates. | Dalapad enable/strength/availability uniforms. | Via `Dalapad_Debug.fx` and SceneGI debug. | Consumed by FrameData. | Owner. | None directly. | None directly. | Must remain optional; no raw semantic sampling in production outside helpers. |
| `Dalashade_MaterialMasks.fxh` | Material, water, receiver, safety classification. | Color, depth, scene/material fields. | Material/water context uniforms. | Via material debug shader. | Helper for FrameData and production shaders. | No direct raw usage expected. | Yes. | May use surface context. | Inferred classification, not material ID truth. |
| `Dalashade_NormalField.fxh` | Inferred screen-space normal/relief field. | Depth, backbuffer luma/color, material/water gates. | NormalField enable/strength/detail/material uniforms. | Via normal debug shader. | Consumed by FrameData. | None directly. | Uses material/water safety. | Owner. | Expensive; inferred only; should early-out aggressively. |
| `Dalashade_AdaptiveGrade.fx` | Adaptive color grading and readability. | Backbuffer, scene intent, material intent. | Many scene/material/style uniforms, performance tier. | Limited debug controls. | Includes FrameData. | Receives Dalapad vars in injection list but should not pay meaningful cost unless used. | Yes. | Injection list includes NormalField vars; should stay cheap. | High visual value; optimize only if profiling proves cost. |
| `Dalashade_SceneGI.fx` | Screen-space indirect lighting/AO/bounce. | Depth, backbuffer, FrameData, MaterialMasks, NormalField, Dalapad. | GI strength/radius/AO/bounce/sample scales/debug/material/Dalapad vars. | Modes 0-21 including Dalapad contribution/FrameData/bridge/raw normal. | Yes. | Through FrameData plus raw debug sample mode. | Yes. | Yes. | Hotspot; debug must prove disabled/missing/stale/sampled states. |
| `Dalashade_SurfaceReflection.fx` | Water/wet/specular/aether/neon reflection approximation. | Depth, backbuffer, FrameData, MaterialMasks, NormalField, material/water context. | Reflection strengths, sample quality, debug vars, material/Dalapad/NormalField vars. | Modes 0-14 plus output modes. | Yes. | Through FrameData surface influence. | Yes. | Yes. | Major hotspot; many texture taps and conditional high-quality branches. |
| `Dalashade_ContactTone.fx` | Contact shadow/tone/edge/structure. | Depth, backbuffer, FrameData, MaterialMasks. | Strength/radius/edge/structure/debug/material/performance vars. | Modes 0-6. | Yes. | Through FrameData if enabled. | Yes. | Yes. | Medium hotspot; radius/sample work should scale by tier. |
| `Dalashade_AtmosphereBloom.fx` | Context-aware bloom/glow. | Backbuffer, scene/material fields. | Bloom sample quality, atmosphere/glow/material vars. | Limited. | Includes shared data. | Injection list includes Dalapad vars. | Yes. | Injection list includes NormalField vars. | Keep cheap; sample quality tier is good. |
| `Dalashade_WeatherAtmosphere.fx` | Weather/haze/wet/cold/heat atmosphere. | Scene intent, depth assist, material intent. | Weather/scene/material vars. | Limited. | Includes shared data. | Injection list includes Dalapad vars. | Yes. | Injection list includes NormalField vars. | Should remain cheap; avoid hidden surface-data cost. |
| `Dalashade_SmartSharpen.fx` | Readability-aware sharpening. | Backbuffer, depth assist, scene/material fields. | Sharpen/detail/dampen/material/performance vars. | Limited. | Includes shared data. | Injection list includes Dalapad vars. | Yes. | Injection list includes NormalField vars. | Cheap unless extra surface work is invoked. |
| `Dalashade_MaterialDebug.fx` | Visualize material classification. | MaterialMasks/material uniforms. | Debug mode/strength/depth assist. | Material debug mode. | Indirect via material helpers. | No production Dalapad role. | Yes. | No. | Should never auto-enable. |
| `Dalashade_NormalDebug.fx` | Visualize NormalField. | NormalField, depth/backbuffer. | Normal debug mode/boost/NormalField vars. | Normal debug modes. | Indirect. | No production Dalapad role. | Material safety gates. | Yes. | Should never auto-enable; can be expensive when enabled. |
| `Dalashade_FrameDataDebug.fx` | Visualize FrameData including Dalapad fields. | FrameData, MaterialMasks, NormalField, Dalapad. | FrameData debug mode/boost/opacity plus surface vars. | FrameData debug modes. | Yes. | Through FrameData. | Yes. | Yes. | Should never auto-enable; best truth shader for contracts. |
| `Dalapad_Debug.fx` | Manual Dalapad render-layer visualization. | Addon-owned debug texture, scan textures, pinned candidates, availability uniforms. | Manual ReShade uniforms, not normal writer-owned variables. | Pass-through, status, synthetic, scan, pinned, channel, quad page. | No. | Owner debug view. | No. | No. | Manual only; broad debug scan/copy can cost FPS. |

## Appendix B: Config and UI Parity Table

| Config area | UI exposure | Diagnostics exposure | Notes |
| --- | --- | --- | --- |
| Base/generated preset paths | User setup and developer setup | Reports/bundles through config and write state | Good safety model. |
| Interface mode | Setup | Config JSON | Good, but User Mode still contains technical labels. |
| `EnableDalashadeCustomShaders` | User Effects and Developer Shader Mapping | Compatibility, debug bundle, bridge diagnostics | Needs friendlier user wording. |
| First-party shader mode | User Look / developer mapping | Bundle/report | Good concept; needs stronger shader-by-shader expected behavior summary. |
| First-party performance tier | Config/model and mapper | Bundle/report partially | UI and docs should show clear per-tier impact. |
| SceneGI/ContactTone/SurfaceReflection variable writes | User Effects and developer mapping | Bridge diagnostics and changed variables | Technical "variable writes" wording should move deeper or be renamed. |
| MaterialIntent enable/diagnostics/mapping | User Scene Awareness and developer pages | Reports/bundles/calibration | Good caveats, but should stay clearly inferred. |
| NormalField enable/diagnostics/mapping | User Effects and developer pages | Reports/bundles | Too technical for normal user labels. |
| Dalapad shader integration/surface data | User Effects and developer pages | Dalapad diagnostics, FrameData, reports/bundles | Production and debug concepts need clearer separation. |
| Dalapad resource shape probe | Developer diagnostics | Dalapad diagnostics | Correctly developer-only. |
| Auto-injection and technique sync | Developer shader mapping | Reports/bundles | Needs docs parity and tests. |
| Backup/write cadence | Developer generation/setup | Reports/bundles | Safety behavior is good; UI could summarize current write target more clearly. |

## Appendix C: Generated Variable and Shader Uniform Parity Table

| Group | Writer/mapper source | Shader-side source | Current parity risk |
| --- | --- | --- | --- |
| First-party performance | `CustomShaderVariableMapper.FirstPartyPerformanceVariables`, `PresetWriter.FirstPartyPerformanceShaderVariables` | `Dalashade_FirstPartyPerformanceTier`, sample scale/quality uniforms in production shaders | Manual list drift. |
| SceneGI | `PresetWriter` SceneGI definition, `CustomShaderVariableMapper` SceneGI values | `Dalashade_SceneGI.fx` | Large key surface; debug mode max must match mapper clamp. |
| ContactTone | Writer definition, mapper values | `Dalashade_ContactTone.fx` | Moderate key surface; debug mode max must match UI and docs. |
| SurfaceReflection | Writer definition, mapper values | `Dalashade_SurfaceReflection.fx` | Very large key surface; high drift risk. |
| MaterialIntent | `WithMaterialIntentVariables`, mapper known material variables | Production shaders and MaterialMasks | Good central intent, but many names. |
| NormalField | `NormalFieldShaderVariables`, mapper known normal variables | `Dalashade_NormalField.fxh`, production/debug shaders | Expensive if accidentally enabled; parity needed. |
| Dalapad | `DalapadSurfaceShaderVariables`, mapper known Dalapad variables | `Dalashade_Dalapad.fxh`, `Dalashade_FrameData.fxh`, SceneGI/FrameData debug | Must remain gated and zero-confidence when off. |
| Debug shaders | Writer debug definitions/defaults | `Dalashade_MaterialDebug.fx`, `Dalashade_NormalDebug.fx`, `Dalashade_FrameDataDebug.fx` | Excluded from auto-sync; test this. |
| `Dalapad_Debug.fx` | Not in `KnownCustomShaders` | Manual shader uniforms | Correctly manual; docs must state this. |

## Appendix D: Documentation Mismatch Table

| Severity | File | Mismatch | Suggested fix |
| --- | --- | --- | --- |
| P1 | `docs/ShaderAuthoring.md` | Older wording can imply injected sections never activate techniques, but `SyncDalashadeTechniqueActivation` can auto-sync production shaders. | Update to describe injection and technique sync separately. |
| P1 | `repo.json`, `releases/` | v4 manifest URL/package expectations are not represented by local old release zips. | Validate release artifact and update local release docs/assets. |
| P2 | `README.md`, `docs/Dalapad.md`, `DalapadAddon/README.md` | Dalapad scaffold/experimental wording may be stale if addon binaries are manually released. | Decide packaging status and align all docs. |
| P2 | Shader docs | Performance tier behavior is present but should be summarized per shader and surfaced in debug bundles. | Add per-shader tier table and bundle summary. |
| P2 | UI helper text vs docs | User Mode still says "variables" and "mapping" in places where docs aim for normal-user clarity. | Rename user labels and keep technical terms in Developer Mode. |
| P3 | `docs/CodebaseIndex.md` | Large and useful, but future agents need this audit linked as a risk roadmap. | Link audit from docs index/README. |

