# Shader Authoring

This page documents the current scaffolding for future Dalashade custom ReShade shaders.

## Current Status

Implemented:

- Top-level `shaders/` folder exists for future `.fx` files.
- `Configuration.EnableDalashadeCustomShaders` gates all custom shader variable writes.
- `CustomShaderVariableMapper` writes stable `SceneIntent` values into Dalashade custom shader sections when those sections and keys already exist in the base preset.
- Main window and compatibility reports show whether custom shader support is enabled, which custom sections were detected, and which custom variables were written.

Not implemented yet:

- No custom `.fx` shader files are shipped yet.
- No native ReShade add-on is required.
- No IPC, named pipe, JSON bridge, or live ReShade bridge exists yet.

Custom shader files and bridge/add-on integration are planned and not currently implemented. Do not treat this document as a `.fx` shader implementation reference yet.

## Safety Rules

Custom shader writes are intentionally conservative:

1. `EnableDalashadeCustomShaders` must be enabled.
2. The preset must already contain a Dalashade custom shader section.
3. The section name must start with `Dalashade` or include `/Dalashade` or `\Dalashade`.
4. The variable key must exactly match a known `Dalashade_*` SceneIntent variable.
5. No custom shader is required for normal operation.

Dalashade does not insert shader sections or create `.fx` files during generation.

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

This is the kind of preset section the writer can update once a matching shader exists:

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

## Where To Edit

| Task | File |
| --- | --- |
| Add or rename exported intent variables | `Dalashade/CustomShaderVariableMapper.cs` |
| Change how intent values are produced | `Dalashade/SceneIntent.cs` |
| Change diagnostics UI | `Dalashade/Windows/MainWindow.cs` |
| Change report output | `Dalashade/CompatibilityReportExporter.cs` |
| Add future shader files | `shaders/` |

Do not add custom shader behavior inside `PresetWriter` beyond the existing mapper call unless the writer contract itself needs to change.
