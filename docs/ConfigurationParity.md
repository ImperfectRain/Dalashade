# Configuration Parity

This page tracks whether major settings are visible, written, reported, bundled, and shader-facing. It is a manual parity table for release review; a future generated table should replace it once configuration metadata is centralized.

Legend:

- User: visible in normal User Mode.
- Dev: visible in Developer Mode.
- Preset: written to the generated ReShade preset.
- Report: included in compatibility/report output.
- Bundle: included in debug bundle output.
- Shader: affects first-party shader uniforms or shader behavior.
- Dalapad: affects Dalapad diagnostics, semantic textures, or first-party Dalapad assist.

| Config area / setting | Default | User | Dev | Preset | Report | Bundle | Shader | Dalapad | Release risk / notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Generated preset paths: `BasePresetPath`, `GeneratedPresetPath`, `ReShadeIniPath`, `ShaderDirectory` | empty or default generated path | yes | yes | path target only | yes | yes | no | no | Base preset is read-only; generated preset is the write target. Missing paths should diagnose, not write elsewhere. |
| Backup/write cadence: `WriteBackups`, `MaxGeneratedPresetBackups`, `MinimumSecondsBetweenWrites` | backups on, max 10, 10 seconds | partial | yes | file cadence only | yes | yes | no | no | Keep write cadence and backups visible because this is a safety boundary. |
| First-party support: `EnableDalashadeCustomShaders` | false | yes | yes | gates all first-party writes | yes | yes | yes | indirect | Off should preserve existing shader behavior and skip generated first-party value changes. |
| First-party mode: `FirstPartyShaderMode` | Supportive | yes | yes | `Dalashade_StandaloneStrength` | yes | yes | yes | no | Supportive is the default conservative path. |
| First-party performance: `FirstPartyPerformanceTier` | Quality | yes | yes | tier and helper-budget uniforms | yes | yes | yes | indirect | Quality preserves current behavior. Lower tiers trim optional helpers only. |
| Shader section injection: `AutoInjectDalashadeCustomShaderSections` | false | yes | yes | adds generated sections only | yes | yes | yes | no | Does not install `.fx` files and does not modify the base preset. |
| Technique sync: `SyncDalashadeTechniqueActivation` | false | yes | yes | generated `Techniques=`/`TechniqueSorting=` only | yes | yes | yes | no | Production first-party allow-list only. Debug shaders stay manual. |
| Load order: `OptimizeGeneratedPresetLoadOrder` | false | no | yes | reorders generated technique lists only | yes | yes | yes | no | Should preserve entries and activation state. |
| Shader matching: `ShaderMatchingMode`, `InactiveShaderWriteMode` | Known fallbacks / supported inactive sections | no | yes | controls which keys can be written | yes | yes | yes | no | Loose matching is risky and should stay Developer Mode. |
| MaterialIntent: `EnableMaterialIntent`, diagnostics, shader mapping, strength | off, diagnostics on, mapping off, 0.25 | partial | yes | only when mapping enabled | yes | yes | yes | no | Scene-level prior only. Not a material ID. |
| Screenshot material evidence: influence and strength | off, 0.35 | yes | yes | only through MaterialIntent mapping | yes | yes | yes when mapped | no | Visible evidence is inferred and capped; UI-heavy screenshots can be wrong. |
| NormalField: enable, diagnostics, shader mapping, strengths, suppressions | off, diagnostics on, mapping off | partial | yes | only when mapping enabled | yes | yes | yes | no | Inferred screen-space support only. Not true FFXIV normals. |
| Dalapad global shader additions: `EnableDalapadShaderIntegration` | false | yes | yes | writes disabling/enabling values | yes | yes | yes when enabled | yes | Off must resolve to neutral/no-data shader behavior. |
| Dalapad surface data: `EnableDalapadSurfaceData`, strength | false, 0.75 | yes | yes | writes surface assist gates/strength | yes | yes | yes | yes | Uses semantic pinned candidates through FrameData only. |
| Dalapad diagnostics: status file, pipe, resource shape probe, scan/copy state | diagnostics neutral; shape probe off | no | yes | no | yes | yes | no production effect | yes | Debug scan/copy paths can cost performance and are developer-only. |
| SceneGI controls: enable writes, strength, AO, bounce, night light, material influence | off, conservative defaults | yes | yes | yes when enabled | yes | yes | yes | indirect | Debug controls are Developer Mode. Compile/live ReShade state remains external. |
| SceneGI debug controls: mode, output, opacity, boost | off/full replacement defaults | no | yes | yes when SceneGI writes enabled | yes | yes | debug only | indirect | Current UI uses integer sliders; shader UI has named modes. Dropdown conversion is a future polish item. |
| SurfaceReflection controls: enable writes, strength, water sheen, glint, wet, aether/neon | off, conservative defaults | yes | yes | yes when enabled | yes | yes | yes | indirect | Experimental visuals; keep User labels simple and developer debug separate. |
| SurfaceReflection debug controls: mode, opacity | off | no | yes | yes when reflection writes enabled | yes | yes | debug only | no | Current UI uses integer sliders. Use shader docs for mode names. |
| ContactTone controls: enable writes, strength, radius, edge, structure, contrast | off, conservative defaults | yes | yes | yes when enabled | yes | yes | yes | indirect | Separate from GI. User Mode should describe first-party controls, not raw variable writes. |
| ContactTone debug controls: mode, opacity | off | no | yes | yes when ContactTone writes enabled | yes | yes | debug only | no | Current UI uses integer sliders. Dropdown conversion is later polish. |
| AtmosphereBloom controls | no dedicated user toggle beyond first-party support/performance/material mapping | partial | yes through mappings | yes | yes | yes | yes | no | Technique and shader-owned bloom controls remain ReShade-side. |
| WeatherAtmosphere controls | weather/territory scene toggles plus first-party support | yes | yes | yes through mappings | yes | yes | yes | no | Weather tuning is mostly scene-driven. |
| SmartSharpen controls | performance budget and first-party support | partial | yes through mappings | yes | yes | yes | yes | no | Cheap shader should stay cheap; performance tiers affect optional shared helpers. |
| AdaptiveGrade controls | target style, first-party mode, scene toggles | yes | yes | yes through mappings | yes | yes | yes | no | Most noticeable visual shader; Quality should preserve behavior. |
| Debug shaders: MaterialDebug, NormalDebug, FrameDataDebug, Dalapad_Debug | manual/off | no | documented/diagnosed | generated sections may exist, techniques not synced | yes | yes | debug only | Dalapad_Debug only | Debug shaders must never be auto-enabled by technique sync. |

## Parity Gaps To Track

| Severity | Gap | Suggested follow-up |
| --- | --- | --- |
| P2 | Developer debug controls use integer sliders while shader docs expose named modes. | Add reusable enum metadata and render named dropdowns without changing written integer values. |
| P2 | Config defaults, UI labels, compatibility sections, and bundle JSON are maintained in separate places. | Introduce a shared diagnostic/config metadata registry after tests cover current output. |
| P2 | First-party shader support is reported across several report sections rather than one compact state summary. | Add a shared first-party assist-state DTO and render it in report, bundle, and UI. |
| P2 | First-party shader metadata now has a read-only registry, but generated preset output still comes from the mapper/writer path. | Keep the registry diagnostics-only until generated-output regression tests can prove a writer migration is behavior-preserving. |
| P3 | Some shader-owned controls are intentionally absent from User Mode, but this is not always obvious. | Continue using User Mode helper text and link to shader docs for raw ReShade controls. |
