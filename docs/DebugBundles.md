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
- `scene-intent.json`
- `material-intent.json`
- `material-parity-audit.md`
- `shader-stack-summary.md`
- `installed-dalashade-shaders.txt`
- `paths-and-environment.txt`
- `normal-field-diagnostics.json`
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
5. `scene-context.json`, `scene-intent.json`, `material-intent.json`: scene and material reasoning.
6. `installed-dalashade-shaders.txt`: installed first-party shader files and hashes.

## Do Not Do

- Do not throw hard failures for missing optional files.
- Do not pass empty paths to path APIs.
- Do not change generated preset behavior except where a fresh report already does so.
- Do not include unrelated logs/files without explicit user opt-in.
