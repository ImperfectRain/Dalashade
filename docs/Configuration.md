# Configuration

`Dalashade/Configuration.cs` stores user settings and generation behavior. It is serialized by Dalamud and should remain backward-compatible unless a migration is intentionally added.

## Major Setting Groups

| Group | Purpose |
| --- | --- |
| Interface mode | Chooses the user-facing or developer-facing UI surface without changing generation behavior. |
| Paths | Base preset, generated preset, ReShade ini, shader path, screenshot folders, master style folders. |
| Generation | Target style, performance budget, compatibility mode, backup count, generation behavior. |
| Shader mapping | Known shader matching mode, inactive writes, iMMERSE Pro/Ultimate support, custom shader support. |
| First-party shader mode | Supportive vs standalone behavior for production Dalashade shaders. |
| First-party performance tier | Quality, Balanced, and Performance helper budgets for production Dalashade shaders. |
| First-party depth assist | Optional global generated-preset write that enables depth assist for production Dalashade first-party shaders. |
| Scene and style | Screenshot analysis, master style mode, tuning preset, style strengths. |
| MaterialIntent | Material profile/intent diagnostics and shader mapping controls. |
| NormalField | Optional inferred normal/surface-field diagnostics and shader mapping controls. |
| Debug/export | Compatibility reports, debug bundles, diagnostics visibility. |
| Reload | ReShade reload hotkey and reload behavior. |

## Interface Mode

`InterfaceMode` controls which UI surface Dalashade shows:

- `User` is the default. It groups the main workflow into setup, look, scene awareness, effects, and health sections. It exposes common controls and actions without showing every low-level diagnostic field.
- `Developer` shows the full existing settings and diagnostics surface. Use it for shader authoring, resolver tuning, compatibility analysis, and detailed generated-preset inspection.

The mode only changes UI organization. It does not change preset generation, shader formulas, generated-preset safety, defaults, or technique activation behavior. All advanced controls remain available in Developer Mode.

User Mode is workflow-oriented:

- Home summarizes current scene, preset status, and basic generation health.
- Setup and Generate keeps base/generated preset setup close to the Generate Now action.
- Look owns target style, performance budget, Supportive/Standalone, and optional master-style matching.
- Scene Awareness owns combat/night/weather/territory/cutscene and scene-lock behavior.
- Effects presents first-party shader groups as status cards with a few safe controls.
- Health exposes compatibility scan, debug bundle export, reload testing, and top warnings.

User Mode should keep the primary workflow visible and keep secondary panels collapsed unless they need attention. Generation status should be concise and user-facing. Raw profile multipliers, injected-section details, authority damping, and low-level variable telemetry belong in Developer Mode.

Scene Authoring is a separate window opened from the main UI or `/dalashade tags`. `EnableSceneAuthoringOverrides` is default-off. When enabled, Dalashade applies user scene-tag overrides after automatic detection and before `SceneIntent`, `VisualProfile`, `MaterialProfile`, and `MaterialIntent` are generated. When disabled, the automatic classifier output is used unchanged.

Scene authoring data is stored under the plugin config folder:

- `SceneAuthoring/scene-overrides.json`: current-territory tag overrides.
- `SceneAuthoring/tag-presets.json`: editable tag registry definitions, descriptions, category membership, known influence notes, and SceneIntent/MaterialIntent tuning rows.
- `SceneAuthoring/Exports/scene-overrides-export.json`: fixed export/import file for scene overrides.
- `SceneAuthoring/Exports/tag-presets-export.json`: fixed export/import file for tag registry metadata and tuning rows.

Scene overrides can add/remove grouped tags, reset the current scene override, and set a primary biome override. Tag registry presets can add custom tags to authoring categories, edit display metadata, and add tuning rows that contribute to `SceneIntent` or `MaterialIntent` when the tag is active and scene authoring is enabled. Registry edits are client-side and non-destructive: reset restores shipped defaults, import/export uses fixed config-folder paths, and base presets/shader source are not mutated.

Developer Mode remains system-oriented. It should keep every low-level control reachable for shader authoring, resolver work, custom variable mapping, diagnostics, reports, and regression checks.

Developer Mode uses searchable tabs in the main and settings windows. Search should find panels by title or summary, while tabs group controls by responsibility:

- Main window: overview, preset pipeline, scene system, shader mapping, diagnostics.
- Settings window: setup, generation, shader mapping, scene data, diagnostics.

New UI features should declare where they belong:

- User Mode: task-level decisions, setup state, safe controls, and actionable health.
- Developer Mode: raw variables, debug modes, mapping switches, diagnostics, and shader-authoring details.
- Both: only when the same control is genuinely useful to normal users and maintainers.

First-party effect cards intentionally report what Dalashade can know from local state: inferred ReShade shader-file presence, preset entry/section visibility, technique activation state, variable detection, variable writes, FrameData expectation, and depth-assist state. They do not claim to prove ReShade compile success or true engine material state.

## Screenshot Analysis Settings

Screenshot analysis is disabled by default. When enabled, Dalashade scans the newest supported image in `ScreenshotFolderPath`, samples it according to `ImageSamplingMode`, and converts the metrics into named scene opinions.

`ScreenshotAnalysisStrength` scales all screenshot-driven output. `0.0` keeps the analyzer and diagnostics available but prevents screenshot opinions from changing VisualProfile, SceneIntent, MaterialProfile, or MaterialIntent. `1.0` is the default. Values above `1.0` intentionally make screenshot opinions more assertive.

`EnableScreenshotMaterialEvidenceInfluence` is off by default. When enabled, `ScreenshotMaterialEvidenceStrength` lets the separate screenshot material-evidence layer add or dampen capped scene-level MaterialIntent priors. It does not create per-pixel material truth, write shader variables directly, or bypass shader-side MaterialMasks/FrameData.

User Mode exposes this as `Use screenshot material hints` with safe wording and a limited strength slider. It does not show per-channel raw material values. Developer Mode exposes the raw evidence channels, confidence, mismatch warnings, current MaterialIntent comparison, strength, caps, and copyable evidence block for calibration work.

The analyzer is still image-statistic based. It does not capture live frames, inspect game buffers, segment objects, or identify true engine materials.

## MaterialIntent Settings

MaterialIntent settings control plugin-side material plausibility and generated material uniforms. Disabled MaterialIntent should produce neutral values and no material uniform writes.

MaterialIntent shader mapping must remain section-scoped. Do not force material uniforms into arbitrary shader sections.

## NormalField Settings

NormalField settings are disabled by default:

- `EnableNormalField`
- `EnableNormalFieldDiagnostics`
- `EnableNormalFieldShaderMapping`
- `NormalFieldStrength`
- `NormalFieldDepthStrength`
- `NormalFieldDetailStrength`
- `NormalFieldMaterialInfluence`
- `NormalFieldWaterSuppression`
- `NormalFieldSkinSuppression`
- `NormalFieldSkySuppression`
- `NormalFieldDebugMode`
- `NormalFieldDebugBoost`

When `EnableNormalField` is false, production output and generated preset variable writes must remain unchanged. When NormalField is enabled but shader mapping is disabled, diagnostics may report settings but generated presets should not receive NormalField uniforms.

## Custom Shader Settings

First-party custom shaders are optional. Dalashade may inject known generated-preset sections and write known uniforms when enabled, but it should not copy shader files or install shader packs.

`SyncDalashadeTechniqueActivation` is optional and disabled by default. When enabled, generated presets can add or remove Dalashade production first-party techniques from `Techniques=` based on the plugin shader options. It preserves third-party effects, leaves debug techniques manual, and still does not modify the base preset.

Generated preset load-order optimization is also optional and disabled by default. When enabled, it may reorder existing entries in `Techniques=` and `TechniqueSorting=` in the generated preset only. By itself, it must not add, remove, or activate techniques.

`FirstPartyShaderMode` controls how strongly production first-party Dalashade shaders participate once custom shader variable writing is enabled:

- `Supportive` is the default. It writes `Dalashade_StandaloneStrength=0` and keeps AdaptiveGrade, SceneGI, ContactTone, SurfaceReflection, AtmosphereBloom, WeatherAtmosphere, and SmartSharpen close to their conservative base-preset enhancement behavior.
- `Standalone` writes `Dalashade_StandaloneStrength=1` to those production shader sections when the section declares the key. The shaders use it as a small multiplier behind existing material/safety gates so first-party Dalashade shaders can carry more of the stack without weakening sky, skin, water, foliage, snow, sand, highlight, or source/receiver protections.

Debug shaders are intentionally unaffected by `FirstPartyShaderMode`.

`FirstPartyPerformanceTier` controls optional helper budgets for production first-party Dalashade shaders:

- `Quality` is the default and preserves current behavior. Generated helper scales are `1.0`, SceneGI keeps the full diffuse gather, SurfaceReflection keeps all projection/pseudo-SSR helpers, and AtmosphereBloom keeps the full bloom gather.
- `Balanced` trims expensive optional helpers while keeping the look close to Quality. It modestly reduces generated NormalField detail/relief/material influence, SceneGI sample distance/count, ContactTone radius, SurfaceReflection helper sampling/reach, and AtmosphereBloom far-ring sampling.
- `Performance` favors cheaper paths. It reduces inferred NormalField work further, lets authorized Dalapad surface data carry more relative influence when available, lowers SceneGI gather work, skips more optional reflection helper samples, and keeps bloom/contact budgets lower.

The tier does not enable shader techniques, install `.fx` files, force Dalapad on, or bypass shader safety gates. Lower tiers must not increase visual intensity to compensate for reduced helper work.

`EnableFirstPartyDepthAssist` is an opt-in generation behavior switch. When enabled, generated presets write:

- `Dalashade_EnableDepthAssist=1`
- `Dalashade_DepthAssistStrength=1`
- `Dalashade_DepthAssistConfidenceFloor=0`
- `Dalashade_DepthConfidenceFloor=0`

The write is limited to known first-party Dalashade production shader sections that declare those uniforms: AdaptiveGrade, AtmosphereBloom, WeatherAtmosphere, SmartSharpen, SceneGI, ContactTone, and SurfaceReflection. It requires custom shader variable writes to be enabled and does not install `.fx` files. Technique activation remains manual unless `SyncDalashadeTechniqueActivation` is enabled. Depth assist can improve resolver confidence when ReShade depth is reliable, but it can worsen masks when the depth buffer is flat, unavailable, reversed incorrectly, or contaminated by UI/overlay depth.

Compatibility reports and debug bundles list `EnableFirstPartyDepthAssist`, whether generated-preset custom shader writes and section injection are enabled, and which production first-party sections received each depth-assist variable. Debug shaders remain manual diagnostic viewers. Reporting missing shader files or missing generated-preset sections should be diagnostic-only and must not fail generation.

## Path Safety

Exporters and writers must resolve safe defaults before calling path APIs. Empty configured paths should fall back to the plugin config directory:

- Reports: `Reports/Dalashade_CompatibilityReport_yyyyMMdd_HHmmss.md`
- Debug bundles: `DebugBundles/Dalashade_DebugBundle_yyyyMMdd_HHmmss/`

## UI Ownership

`Dalashade/Windows/ConfigWindow.cs` owns settings controls. `Dalashade/Windows/MainWindow.cs` owns status, generation, and diagnostics panels. UI should call services and helpers rather than implementing generation logic.

## Do Not Do

- Do not remove serialized fields without a migration.
- Do not make experimental systems enabled by default.
- Do not change the default `FirstPartyShaderMode` away from `Supportive` without a migration and explicit review.
- Do not change the default `FirstPartyPerformanceTier` away from `Quality` without a migration and explicit review.
- Do not enable first-party depth assist by default; it must remain an explicit generation behavior opt-in.
- Do not write NormalField or MaterialIntent shader variables unless their mapping settings allow it.
- Do not store sensitive external paths beyond what the user configured.
