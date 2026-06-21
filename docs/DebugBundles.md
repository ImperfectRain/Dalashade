# Debug Bundles

`Export Debug Bundle` creates a timestamped diagnostics folder under the Dalashade plugin config directory, usually:

```text
DebugBundles/Dalashade_DebugBundle_yyyyMMdd_HHmmss/
```

The bundle is intended to capture enough context to debug user reports, shader behavior, preset generation, material parity, and future feature planning without collecting unrelated user files.

## Owner

`Dalashade/DebugBundleExporter.cs` owns bundle creation. UI buttons live in `Dalashade/Windows/MainWindow.cs` and/or `Dalashade/Windows/ConfigWindow.cs` depending on the current UI path.

`Dalashade/CompatibilityReportExporter.cs` owns the fresh compatibility report that the bundle includes when report generation succeeds.

## Required Outputs

Current bundle contents include:

- `compatibility-report.md` or `compatibility-report-missing.txt`
- `base-preset.ini` or `base-preset-missing.txt`
- `generated-preset.ini` or `generated-preset-missing.txt`
- `active-preset.ini` or `active-preset-unavailable.txt`
- `plugin-config.json`
- `scene-context.json`
- `scene-authoring.json`
- `scene-intent.json`
- `screenshot-material-evidence.json`
- `material-tag-registry.json`
- `material-calibration.json`
- `material-intent.json`
- `normal-field-diagnostics.json`
- `frame-data-diagnostics.json`
- `dalapad-diagnostics.json`
- `first-party-depth-assist.json`
- `first-party-performance.json`
- `health-summary.json`
- `performance-summary.json`
- `generated-variable-summary.json`
- `shader-uniform-parity.json`
- `first-party-shader-registry.json`
- `material-parity-audit.md`
- `shader-stack-summary.md`
- `installed-dalashade-shaders.txt`
- `paths-and-environment.txt`
- `manifest.json`
- `bundle-export-log.txt`

Zip creation is optional. If zipping fails, the folder should remain and the zip failure should be listed as skipped rather than failing the bundle.

## Failure Model

The exporter is partial-success safe:

- Hard failure: output root/folder cannot be resolved or created, core manifest cannot be written, or no useful files can be written.
- Skipped item: missing preset, missing shader file, optional diagnostics failure, SHA256 failure, zip failure, or compatibility report failure inside the bundle.

Optional failures are written to `manifest.json` and `bundle-export-log.txt`.

## Privacy and Safety

The bundle should not include arbitrary logs or unrelated user files by default. Presets and Dalashade config are included because the user explicitly requested a debug export. Full logs should remain opt-in or limited if added later.

Do not include credentials, tokens, or unrelated folders.

## How to Read a Bundle

Start with:

1. `health-summary.json`: compact pass/warning routing summary and the first detailed files to open next.
2. `manifest.json`: what succeeded and what was skipped.
3. `bundle-export-log.txt`: exact stage-level failures.
4. `compatibility-report.md`: preset stack, material parity, NormalField diagnostics, shader support.
5. `plugin-config.json`: settings that controlled the generation.
6. `scene-context.json`, `scene-authoring.json`, `scene-intent.json`, `screenshot-material-evidence.json`, `material-tag-registry.json`, `material-calibration.json`, `material-intent.json`: scene, override, visible material evidence, registry tuning, calibration, and material reasoning.
7. `normal-field-diagnostics.json`: NormalField settings, shader presence, debug technique state, and first-party consumption.
8. `frame-data-diagnostics.json`: FrameData include/debug shader presence, FrameDataDebug section/variables, inline/prepass status, and production migration status.
9. `dalapad-diagnostics.json`: Dalapad runtime metadata probe status plus addon contract version, IPC status-file/control-pipe diagnostics, optional resource rows, scan/pinned candidate status, debug visualization state, shader integration gates, diagnostic routes, implementation options, backend steps, and separate production-vs-debug cost reporting for the optional surface-data addon path.
10. `first-party-depth-assist.json`: opt-in depth-assist setting state and known first-party sections that received depth-assist writes.
11. `first-party-performance.json`: selected Quality/Balanced/Performance tier, expected per-shader behavior, known performance uniforms, sections that received generated tier values, and generated preset values for relevant first-party shader sections.
12. `performance-summary.json`: compact first-party tier, feature-gate, generated performance value, and Dalapad production/debug cost summary.
13. `generated-variable-summary.json`: known generated variables, generated preset values, and changed generated variables grouped by shader section.
14. `shader-uniform-parity.json`: installed first-party shader uniform scan comparing known generated variables against shader-side uniforms.
15. `first-party-shader-registry.json`: read-only first-party shader metadata used by parity diagnostics, report summaries, and UI labels.
16. `installed-dalashade-shaders.txt`: installed first-party shader files and hashes.

`scene-authoring.json` records whether scene authoring was enabled, the override file path, active territory override metadata, detected area/weather/biome/mood tags, effective area/weather/biome/mood tags, added/removed override maps, suppressed diagnostic tags, and authoring warnings. This file is the first place to check when a user says tags were removed but still appeared to influence the generated profile.

`screenshot-material-evidence.json` records broad visible material-family evidence, confidence, evidence notes, current MaterialIntent comparison values, and mismatch warnings. It is useful for cases where screenshots show foliage, water, sand, snow, sky, or aether/neon cues but MaterialIntent stayed low. When `EnableScreenshotMaterialEvidenceInfluence` is enabled, `material-intent.json` also records the capped screenshot evidence contributions that were allowed into MaterialIntent. This still does not mean Dalashade detected true engine material IDs.

`material-calibration.json` records one row per MaterialIntent channel with profile prior, tag registry contribution, screenshot evidence, current MaterialIntent, shader mapping availability, relevant shader keys/sections, warnings, and the representative scene matrix. It is diagnostics-only and does not change generation.

`material-tag-registry.json` records the active, inactive, invalid, and capped MaterialIntent registry tunings for the current scene. Registry material tunings are capped at +/-0.20 per tag row and +/-0.35 per MaterialIntent channel total. Invalid target/channel/amount rows are ignored and reported instead of being applied.

Editable tag preset metadata lives in `SceneAuthoring/tag-presets.json`. It is not a shader formula dump; it documents and extends the authoring vocabulary. Exported tag preset metadata uses `SceneAuthoring/Exports/tag-presets-export.json`.

FrameData is currently inline only. `frame-data-diagnostics.json` should report `FrameDataMode: Inline`, `FrameDataPrepass: NotImplemented`, and WeatherAtmosphere, AdaptiveGrade, SmartSharpen, AtmosphereBloom, SceneGI, ContactTone, and SurfaceReflection as production FrameData consumers. `Dalashade_FrameDataDebug.fx` is a manual debug shader; section/variable injection must not make the technique active by default.

`normal-field-diagnostics.json` reports NormalDebug technique state from analyzed preset text only. It cannot observe whether the live ReShade UI checkbox is currently enabled after the report was generated.

`dalapad-diagnostics.json` records the diagnostic-only Dalapad surface-data probe. It reports runtime metadata availability, addon contract version, optional Stage 1 status-file/control-pipe IPC state, resource rows, availability flags, scan/pinned candidate status, debug visualization state, diagnostic routes, implementation options, realtime-contract placeholders, and staged backend steps. Its `CostReporting` section separates production shader assist from debug visualization copies, because copied debug layers can cost FPS independently of whether production shader gates are enabled. Debug visualization cost buckets include `CopyFrameInterval`, observed source count, copied source count, copied pinned candidate count, and frame age when the addon reports them. The addon may upload synthetic pixels or addon-owned diagnostic copies for debug visualization. It must not expose raw game handles over IPC, move realtime shader values, or make shader/preset behavior depend on Dalapad unless the global shader-additions gate and shared surface-data gate are enabled.

`health-summary.json` is the first stop for broad triage. It summarizes generated preset write health, first-party shader feature gates, shader-uniform parity status, compatibility report availability, and Dalapad production/debug cost status. It intentionally points to deeper files instead of duplicating every report.

`first-party-performance.json` is the first stop when a user reports that Balanced or Performance did not change shader cost. It should show `SelectedTier`, whether custom shader writing/injection were enabled, `QualityPreservesCurrentBehavior`, the expected behavior summary, per-shader tier notes, and the exact generated uniforms written to sections such as SceneGI, SurfaceReflection, AtmosphereBloom, ContactTone, and NormalField consumers. Missing section values usually mean the shader section or expected key was absent from the generated preset, custom shader support was disabled, or injection was off.

`performance-summary.json` is the first stop when a user reports a broad FPS drop. It records the selected first-party performance tier, first-party feature gates, generated performance values, and Dalapad production/debug cost buckets. Use `DalapadDebugVisualizationCost` to tell whether render-layer debug copies were active, how often the addon reports copies through `CopyFrameInterval`, and `DalapadProductionAssist` to tell whether production first-party shaders were allowed to sample Dalapad data.

`generated-variable-summary.json` is the first stop when a generated control appears dead. It records the known generated-variable list, values observed in the generated preset, changed variables from the last write, and missing known variables. Missing known variables are not automatically bugs; they can mean the related shader section was not installed, not injected, disabled, inactive under the current write mode, or unchanged.

`shader-uniform-parity.json` is the first stop when a shader uniform/generation mismatch is suspected. It scans installed first-party shader files from inferred ReShade shader search paths and reports warnings when known generated variables have no installed shader uniform or when an installed first-party shader exposes a Dalashade-managed uniform that the mapper does not know how to write. The file is diagnostic-only and does not change preset generation.

`first-party-shader-registry.json` records the read-only shader registry snapshot for production and manual debug shaders. It includes family names, files, sections, techniques, sync eligibility, manual-debug flags, known generated uniforms, debug uniforms, and performance-tier uniforms. It is a diagnostic contract snapshot only; generated preset writing still comes from the mapper/writer path.

## Do Not Do

- Do not throw hard failures for missing optional files.
- Do not pass empty paths to path APIs.
- Do not change generated preset behavior except where a fresh report already does so.
- Do not include unrelated logs/files without explicit user opt-in.
