# Shader Authoring

This page documents the current scaffolding and first prototype for Dalashade custom ReShade shaders.

## Current Status

Implemented plugin support:

- Top-level `shaders/` folder exists for custom `.fx` files.
- `Configuration.EnableDalashadeCustomShaders` gates all custom shader variable writes.
- `CustomShaderVariableMapper` writes stable `SceneIntent` values into Dalashade custom shader sections when those sections and keys already exist in the base preset.
- Main window and compatibility reports show whether custom shader support is enabled, which custom sections were detected, and which custom variables were written.

Implemented shader prototype:

- `shaders/Dalashade_WeatherAtmosphere.fx`

Not implemented yet:

- No IPC, named pipe, JSON bridge, or live ReShade bridge exists yet.
- No native ReShade add-on exists yet.
- No automatic shader installation/copying exists yet.
- Release packaging of `shaders/Dalashade_WeatherAtmosphere.fx` is not guaranteed by this document. Check the current release task and zip contents before assuming the shader is included in a published plugin package.

Bridge/add-on integration is planned and not currently implemented. Do not treat this document as a native bridge or IPC implementation reference.

## Safety Rules

Custom shader writes are intentionally conservative:

1. `EnableDalashadeCustomShaders` must be enabled.
2. The preset must already contain a Dalashade custom shader section.
3. The section name must start with `Dalashade` or include `/Dalashade` or `\Dalashade`.
4. The variable key must exactly match a known `Dalashade_*` SceneIntent variable.
5. No custom shader is required for normal operation.

Dalashade does not insert shader sections or create `.fx` files during generation.

Dalashade also does not currently install or copy custom shader files into a ReShade shader directory. For manual testing, place `shaders/Dalashade_WeatherAtmosphere.fx` somewhere ReShade scans for shaders, then enable it in ReShade.

## Manual Installation Diagnostics

The UI and compatibility report distinguish two separate states:

| State | Meaning |
| --- | --- |
| Custom shader support enabled | `EnableDalashadeCustomShaders` is on, so generation is allowed to write supported `Dalashade_*` values. |
| Base preset contains a Dalashade custom shader section | The selected base preset has a section such as `[Dalashade_WeatherAtmosphere.fx]`, so Dalashade can inspect it for supported `Dalashade_*` keys. |
| Technique active/inactive/unknown | Dalashade checks whether the section appears active in `Techniques=`. If `Techniques=` is missing, activation is reported as unknown. |
| Known custom variables found | Matching `Dalashade_*` keys were found in a Dalashade custom shader section. |
| Variables detected but unchanged | Keys exist, but generation did not write values. Common causes are disabled custom shader support, inactive/unknown technique state, write-mode settings, or values already matching SceneIntent. |
| Variables written | `SceneIntent` values were written into matching `Dalashade_*` keys during generation. |

This is not the same as shader file installation. ReShade must still be able to find the actual `.fx` file through its own shader search paths. If the section exists but ReShade cannot compile or enable the effect, check ReShade's shader path and install `shaders/Dalashade_WeatherAtmosphere.fx` manually.

## Recommended ReShade Order

For `Dalashade_WeatherAtmosphere.fx`, use this order:

1. After AO, MXAO, RTGI, and other depth/lighting effects.
2. Before final sharpening.
3. Before UI restore, KeepUI, or RestoreUI effects if applicable.

The shader is meant to shape world atmosphere. Keeping it before UI restore helps avoid haze/glow affecting protected UI layers in presets that use UI masking.

## Supported SceneIntent Variables

Future Dalashade shaders can expose these scalar variables in a Dalashade section:

| Variable | Source |
| --- | --- |
| `Dalashade_Readability` | `SceneIntent.Readability` |
| `Dalashade_Atmosphere` | `SceneIntent.Atmosphere` |
| `Dalashade_HighlightProtection` | `SceneIntent.HighlightProtection` |
| `Dalashade_ShadowProtection` | `SceneIntent.ShadowProtection` |
| `Dalashade_Haze` | `SceneIntent.Haze` |
| `Dalashade_Wetness` | `SceneIntent.Wetness` |
| `Dalashade_Cold` | `SceneIntent.Cold` |
| `Dalashade_Heat` | `SceneIntent.Heat` |
| `Dalashade_MagicGlow` | `SceneIntent.MagicGlow` |
| `Dalashade_NeonGlow` | `SceneIntent.NeonGlow` |
| `Dalashade_FoliageDensity` | `SceneIntent.FoliageDensity` |
| `Dalashade_CombatPressure` | `SceneIntent.CombatPressure` |
| `Dalashade_CinematicPermission` | `SceneIntent.CinematicPermission` |

All values are normalized `0.0` to `1.0`.

## Example Preset Section

This is the kind of preset section the writer can update once a matching shader exists and custom shader support is enabled:

```ini
[Dalashade_Intent.fx]
Dalashade_Readability=0.000000
Dalashade_Atmosphere=0.000000
Dalashade_HighlightProtection=0.000000
Dalashade_ShadowProtection=0.000000
Dalashade_Haze=0.000000
Dalashade_Wetness=0.000000
Dalashade_Cold=0.000000
Dalashade_Heat=0.000000
Dalashade_MagicGlow=0.000000
Dalashade_NeonGlow=0.000000
Dalashade_FoliageDensity=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_CinematicPermission=0.000000
```

## Regression Fixture

A developer fixture exists at:

`test-presets/custom-shader-fixtures/DalashadeWeatherAtmosphere.ini`

It is not a user default preset. It exists so the preset regression harness can verify that `CustomShaderVariableMapper` detects the `Dalashade_WeatherAtmosphere.fx` section and writes SceneIntent variables when `EnableDalashadeCustomShaders` is enabled.

Regression reports include a custom shader section showing support state, detected Dalashade shader sections, whether the technique is listed/active, detected custom variables, changed custom variables, static bridge proof status, and the synthetic SceneIntent values used for the simulation.

For custom shader fixture presets, the regression harness enables custom shader writes inside the simulation so the static bridge path can be verified. This does not change normal generation behavior.

## Where To Edit

| Task | File |
| --- | --- |
| Add or rename exported intent variables | `Dalashade/CustomShaderVariableMapper.cs` |
| Change custom bridge diagnostics | `Dalashade/CustomShaderBridgeDiagnostics.cs` |
| Change how intent values are produced | `Dalashade/SceneIntent.cs` |
| Change diagnostics UI | `Dalashade/Windows/MainWindow.cs` |
| Change report output | `Dalashade/CompatibilityReportExporter.cs` |
| Add future shader files | `shaders/` |

Do not add custom shader behavior inside `PresetWriter` beyond the existing mapper call unless the writer contract itself needs to change.
