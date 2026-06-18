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
- `first-party-depth-assist.json`
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

1. `manifest.json`: what succeeded and what was skipped.
2. `bundle-export-log.txt`: exact stage-level failures.
3. `compatibility-report.md`: preset stack, material parity, NormalField diagnostics, shader support.
4. `plugin-config.json`: settings that controlled the generation.
5. `scene-context.json`, `scene-authoring.json`, `scene-intent.json`, `screenshot-material-evidence.json`, `material-tag-registry.json`, `material-calibration.json`, `material-intent.json`: scene, override, visible material evidence, registry tuning, calibration, and material reasoning.
6. `normal-field-diagnostics.json`: NormalField settings, shader presence, debug technique state, and first-party consumption.
7. `frame-data-diagnostics.json`: FrameData include/debug shader presence, FrameDataDebug section/variables, inline/prepass status, and production migration status.
8. `first-party-depth-assist.json`: opt-in depth-assist setting state and known first-party sections that received depth-assist writes.
9. `installed-dalashade-shaders.txt`: installed first-party shader files and hashes.

`scene-authoring.json` records whether scene authoring was enabled, the override file path, active territory override metadata, detected area/weather/biome/mood tags, effective area/weather/biome/mood tags, added/removed override maps, suppressed diagnostic tags, and authoring warnings. This file is the first place to check when a user says tags were removed but still appeared to influence the generated profile.

`screenshot-material-evidence.json` records broad visible material-family evidence, confidence, evidence notes, current MaterialIntent comparison values, and mismatch warnings. It is useful for cases where screenshots show foliage, water, sand, snow, sky, or aether/neon cues but MaterialIntent stayed low. When `EnableScreenshotMaterialEvidenceInfluence` is enabled, `material-intent.json` also records the capped screenshot evidence contributions that were allowed into MaterialIntent. This still does not mean Dalashade detected true engine material IDs.

`material-calibration.json` records one row per MaterialIntent channel with profile prior, tag registry contribution, screenshot evidence, current MaterialIntent, shader mapping availability, relevant shader keys/sections, warnings, and the representative scene matrix. It is diagnostics-only and does not change generation.

`material-tag-registry.json` records the active, inactive, invalid, and capped MaterialIntent registry tunings for the current scene. Registry material tunings are capped at +/-0.20 per tag row and +/-0.35 per MaterialIntent channel total. Invalid target/channel/amount rows are ignored and reported instead of being applied.

Editable tag preset metadata lives in `SceneAuthoring/tag-presets.json`. It is not a shader formula dump; it documents and extends the authoring vocabulary. Exported tag preset metadata uses `SceneAuthoring/Exports/tag-presets-export.json`.

FrameData is currently inline only. `frame-data-diagnostics.json` should report `FrameDataMode: Inline`, `FrameDataPrepass: NotImplemented`, and WeatherAtmosphere, AdaptiveGrade, SmartSharpen, AtmosphereBloom, SurfaceReflection, and SceneGI as production FrameData consumers. `Dalashade_FrameDataDebug.fx` is a manual debug shader; section/variable injection must not make the technique active by default.

## Do Not Do

- Do not throw hard failures for missing optional files.
- Do not pass empty paths to path APIs.
- Do not change generated preset behavior except where a fresh report already does so.
- Do not include unrelated logs/files without explicit user opt-in.
