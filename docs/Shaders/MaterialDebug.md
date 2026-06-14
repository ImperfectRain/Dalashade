# Dalashade_MaterialDebug

## Current purpose

`shaders/Dalashade_MaterialDebug.fx` is the truth viewer for shared material, water, safety, receiver, and competition masks.

## Intended purpose

MaterialDebug should explain why production shaders behave differently from raw image evidence. It should remain diagnostic-only and visually inert when disabled.

## Current implementation summary

For each pixel, the shader samples backbuffer/depth, calls the shared resolver pipeline in `Dalashade_MaterialMasks.fxh`, and returns a false-color visualization selected by `Dalashade_MaterialDebugMode`.

## Inputs

- Backbuffer color and ReShade depth.
- All shared material/context uniforms written by the plugin.
- Depth assist controls.
- Debug mode and debug boost.
- Shared material/water/safety resolver functions.

## Outputs

Debug-only false-color images. It should not be used as production output.

## Core algorithm

1. Sample source color.
2. Resolve material signals, raw candidates, gated candidates, competition, final masks, water, material, and safety.
3. Switch on debug mode.
4. Return the selected diagnostic color or pass-through output.

## Material/Water/Normal dependencies

MaterialDebug directly visualizes `Dalashade_MaterialMasks.fxh`. It does not consume NormalField. NormalDebug should be used separately when inspecting inferred normal/receiver fields.

## Debug modes

The shader includes a broad mode list for raw materials, final materials, water resolver, shared safety, and production-consumer previews. Key current competition modes:

| Mode | Label | Meaning |
| --- | --- | --- |
| 55 | Water/sky conflict | Red sky wins, cyan water wins, yellow/white unresolved conflict. |
| 56 | Water pixel confidence | Actual likely water pixels. |
| 57 | Sky pixel confidence | Actual likely sky/cloud/fog pixels. |
| 58 | Water receiver vs horizon | Cyan receiver water, blue horizon/source-only, red rejected sky. |
| 59 | Receiver confidence split | Cyan reflection, green structure, yellow/olive AO, faint gray legacy receiver. |
| 60 | Water local proof | Local water proof used by competition. |
| 61 | Strong water proof | Strong water-plane-dominant proof. |
| 62 | Constructed/aether reject | Constructed cyan/aether/metal rejection. |
| 63 | Sky dominance | Sky dominance used by competition. |
| 64 | Water proof boost | Local proof boost for true water. |
| 65 | Competition internals overview | Composite competition explanation. |

## Safety and suppression rules

MaterialDebug should show unsafe regions rather than hide them. Its output can be intentionally bright or stark. It must not auto-enable production effects or alter generated material values.

## Current limitations

- Debug colors are symbolic; they are not final effect strength.
- Some modes overlap visually when multiple masks are high.
- It does not reveal shader-specific local refinements unless a preview mode exists.

## Future direction

Add targeted preview modes only when a production shader needs explainability. Prefer improving shared debug helpers over duplicating formulas inside production shaders.

## Do not do

- Do not tune material formulas from production output alone; inspect MaterialDebug first.
- Do not use MaterialDebug as a gameplay effect.
- Do not add debug modes that approximate formulas differently from the actual resolver values.
