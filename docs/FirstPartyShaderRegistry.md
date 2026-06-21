# First-Party Shader Registry

`Dalashade/FirstPartyShaderRegistry.cs` is the current read-only metadata layer for Dalashade first-party shader families.

It records:

- shader family and display name
- `.fx` file name
- ReShade technique and generated preset section name
- production vs manual debug classification
- technique-sync eligibility
- expected include contracts
- known generated uniforms
- performance-tier uniforms
- debug uniforms
- short role/notes text used by diagnostics and UI labels

## Current Boundary

The registry is diagnostics and presentation metadata only. It does not write generated preset values, activate techniques, inject sections, tune visuals, copy shader files, or change shader behavior.

Preset output still flows through the existing mapper and writer:

`CustomShaderVariableMapper` -> `PresetWriter` -> generated ReShade preset

The registry is intentionally introduced before moving output behavior onto it so reports, bundles, and UI labels can converge on one vocabulary first.

## Current Consumers

- `ShaderUniformParityDiagnosticsBuilder` uses the registry to decide which first-party shader files to scan and which files are manual debug shaders.
- `DebugBundleExporter` writes `first-party-shader-registry.json` and uses registry-owned performance-tier uniform names for bundle summaries.
- `CompatibilityReportExporter` uses the registry for production FrameData scans and reports which manual debug techniques are excluded from technique sync.
- `MainWindow` uses registry role text for first-party shader status cards where available.

## Production vs Debug

Production shaders are eligible for technique sync when their existing feature gates permit it. Manual debug shaders are tracked for parity and diagnostics, but they should not be auto-enabled:

- `Dalashade_MaterialDebug.fx`
- `Dalashade_NormalDebug.fx`
- `Dalashade_FrameDataDebug.fx`
- `Dalapad_Debug.fx`

## Future Migration

The next safe step is to make diagnostics compare mapper/writer behavior against this registry more deeply. Preset writing should move onto registry metadata only after output regression tests cover current generated preset behavior.
