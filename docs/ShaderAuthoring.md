# Shader Authoring

This page documents the current scaffolding and first prototype for Dalashade custom ReShade shaders.

## Current Status

Implemented plugin support:

- Top-level `shaders/` folder exists for custom `.fx` files.
- `Configuration.EnableDalashadeCustomShaders` gates all custom shader variable writes.
- `CustomShaderVariableMapper` writes stable `SceneIntent` values into Dalashade custom shader sections when those sections and keys already exist in the base preset.
- `Configuration.AutoInjectDalashadeCustomShaderSections` can optionally add known Dalashade custom shader sections to the generated preset only.
- Main window and compatibility reports show whether custom shader support is enabled, which custom sections were detected, and which custom variables were written.

Implemented shader prototype:

- `shaders/Dalashade_WeatherAtmosphere.fx`
- Current tuning is conservative v2: stronger than the first prototype, but bounded by combat/readability dampening and final color-delta guardrails.

Not implemented yet:

- No IPC, named pipe, JSON bridge, or live ReShade bridge exists yet.
- No native ReShade add-on exists yet.
- No automatic shader installation/copying exists yet.
- Release packaging of `shaders/Dalashade_WeatherAtmosphere.fx` is not guaranteed by this document. Check the current release task and zip contents before assuming the shader is included in a published plugin package.

Bridge/add-on integration is planned and not currently implemented. Do not treat this document as a native bridge or IPC implementation reference.

## Safety Rules

Custom shader writes are intentionally conservative:

1. `EnableDalashadeCustomShaders` must be enabled.
2. The preset must already contain a Dalashade custom shader section, or generated-preset-only injection must be explicitly enabled.
3. The section name must start with `Dalashade` or include `/Dalashade` or `\Dalashade`.
4. The variable key must exactly match a known `Dalashade_*` SceneIntent variable.
5. No custom shader is required for normal operation.

When `AutoInjectDalashadeCustomShaderSections` is off, Dalashade does not insert shader sections during generation.

When both `EnableDalashadeCustomShaders` and `AutoInjectDalashadeCustomShaderSections` are on, Dalashade may inject known custom shader sections and variables into the generated preset only. It never mutates the base preset. Initial injection support is limited to `[Dalashade_WeatherAtmosphere.fx]`.

The generated WeatherAtmosphere section includes the weather shader intent variables Dalashade currently knows how to write: `Dalashade_Haze`, `Dalashade_Wetness`, `Dalashade_Cold`, `Dalashade_Heat`, `Dalashade_HighlightProtection`, `Dalashade_ShadowProtection`, `Dalashade_CombatPressure`, `Dalashade_Atmosphere`, `Dalashade_MagicGlow`, `Dalashade_NeonGlow`, `Dalashade_Readability`, and `Dalashade_CinematicPermission`.

Dalashade also does not currently install or copy custom shader files into a ReShade shader directory. For manual testing, place `shaders/Dalashade_WeatherAtmosphere.fx` somewhere ReShade scans for shaders, then enable it in ReShade.

## Manual Installation Diagnostics

The UI and compatibility report distinguish two separate states:

| State | Meaning |
| --- | --- |
| Custom shader support enabled | `EnableDalashadeCustomShaders` is on, so generation is allowed to write supported `Dalashade_*` values. |
| Auto-inject generated preset sections enabled | `AutoInjectDalashadeCustomShaderSections` is on, so generation may add known sections to the generated preset only. |
| Section injected | Dalashade added a supported custom shader section to the generated preset. |
| Variables injected | Dalashade added missing known `Dalashade_*` variables to a supported custom shader section in the generated preset. |
| Technique injected | Dalashade appended the known technique entry to an existing non-empty `Techniques=` line in the generated preset. |
| Generated preset only | Injection happened in the generated output path; the base preset was not modified. |
| Base preset contains a Dalashade custom shader section | The selected base preset has a section such as `[Dalashade_WeatherAtmosphere.fx]`, so Dalashade can inspect it for supported `Dalashade_*` keys. |
| Technique active/inactive/unknown | Dalashade checks whether the section appears active in `Techniques=`. If `Techniques=` is missing, activation is reported as unknown. |
| Known custom variables found | Matching `Dalashade_*` keys were found in a Dalashade custom shader section. |
| Variables detected but unchanged | Keys exist, but generation did not write values. Common causes are disabled custom shader support, inactive/unknown technique state, write-mode settings, or values already matching SceneIntent. |
| Variables written | `SceneIntent` values were written into matching `Dalashade_*` keys during generation. |

This is not the same as shader file installation. ReShade must still be able to find the actual `.fx` file through its own shader search paths. If the section exists but ReShade cannot compile or enable the effect, check ReShade's shader path and install `shaders/Dalashade_WeatherAtmosphere.fx` manually.

Technique injection is intentionally narrow. Dalashade only appends `Dalashade_WeatherAtmosphere@Dalashade_WeatherAtmosphere.fx` when the preset already has a non-empty `Techniques=` line and the entry is not already present. If there is no safe `Techniques=` line to update, Dalashade can still inject the section/variables, but the user may need to enable the technique in ReShade manually.

## Recommended ReShade Order

For `Dalashade_WeatherAtmosphere.fx`, use this order:

1. After AO, MXAO, RTGI, and other depth/lighting effects.
2. Before final sharpening.
3. Before UI restore, KeepUI, or RestoreUI effects if applicable.

The shader is meant to shape world atmosphere. Keeping it before UI restore helps avoid haze/glow affecting protected UI layers in presets that use UI masking.

## Weather Atmosphere Controls

`Dalashade_WeatherAtmosphere.fx` can be driven by Dalashade or tested manually in ReShade.

Dalashade-driven controls:

| Control | Purpose |
| --- | --- |
| `Dalashade_Haze` | Drives bounded depth haze for fog, dust, clouds, and similar atmosphere. |
| `Dalashade_Wetness` | Adds wet-scene specular glow and contributes to storm mood. |
| `Dalashade_Cold` | Cools haze tint and strengthens snow/white highlight protection. |
| `Dalashade_Heat` | Warms haze tint and adds distant heat/dust softness. |
| `Dalashade_HighlightProtection` | Rolls off bright highlights, especially in snow, wet, and hot scenes. |
| `Dalashade_ShadowProtection` | Adds modest shadow lift for dark scenes. |
| `Dalashade_CombatPressure` | Dampens heavy haze, glow, storm mood, and heat softness for gameplay readability. |
| `Dalashade_Readability` | Adds lighter atmosphere dampening when the scene needs readable gameplay space. |
| `Dalashade_Atmosphere` | Scales the scene's general atmosphere allowance. |
| `Dalashade_MagicGlow` | Adds controlled glow for magical/aetherial scenes. |
| `Dalashade_NeonGlow` | Adds controlled glow for neon/high-tech scenes. |
| `Dalashade_CinematicPermission` | Allows a small boost to atmosphere outside gameplay-critical moments. |

Manual testing controls:

| Control | Suggested tuning |
| --- | --- |
| `Manual Overall Strength` | Default `0.35`. Raise only while tuning; high values make all responses more visible. |
| `Manual Haze Boost` | Adds haze without needing Dalashade scene tags. Useful for testing depth behavior. |
| `Manual Glow Boost` | Adds glow without needing wet, magic, or neon intent. |
| `Manual Storm/Dark Mood` | Tests storm darkening and cool mood response. |
| `Show Debug Mask` | Shows red depth haze, green glow, and blue protection/readability pressure. |

The v2 shader intentionally does not blur the frame or disable any ReShade techniques. It shapes color, haze, glow, highlight rolloff, and mild softness through bounded masks. Combat-heavy scenes should visibly reduce the heaviest atmosphere while retaining light weather identity.

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

`Dalashade_WeatherAtmosphere.fx` currently consumes `Readability`, `Atmosphere`, `HighlightProtection`, `ShadowProtection`, `Haze`, `Wetness`, `Cold`, `Heat`, `MagicGlow`, `NeonGlow`, `CombatPressure`, and `CinematicPermission`.

## Example Preset Section

This is the kind of preset section the writer can update once a matching shader exists and custom shader support is enabled. With generated-preset injection enabled, Dalashade can add the WeatherAtmosphere section and known variables to the generated preset automatically:

```ini
[Dalashade_WeatherAtmosphere.fx]
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
