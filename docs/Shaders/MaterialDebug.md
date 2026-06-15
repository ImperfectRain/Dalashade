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

The shader includes a broad mode list for raw materials, final materials, water resolver, shared safety, and production-consumer previews. The SurfaceReflection and SceneGI preview modes use shared receiver helpers so they remain aligned with the material contract instead of relying on the legacy broad receiver field.

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Off/pass-through | Normal source image. |
| 1 | Overview final masks | Composite final material overview. |
| 2 | Combined confidence | Overall final mask confidence. |
| 3 | Raw sky/fog | Raw sky/fog pixel evidence. |
| 4 | Gated sky/fog | Scene-gated sky/fog evidence. |
| 5 | Final sky/fog | Final resolved sky/fog mask. |
| 6 | Raw foliage strong | Raw strong foliage evidence. |
| 7 | Organic green surface | Organic green foliage-like surface evidence. |
| 8 | Final foliage influence | Final resolved foliage mask. |
| 9 | Raw water/specular combined | Raw broad water/specular evidence. |
| 10 | Gated water/specular combined | Scene-gated water/specular evidence. |
| 11 | Final water/specular combined | Final resolved water/specular mask. |
| 12 | Raw snow/ice | Raw snow/ice evidence. |
| 13 | Gated snow/ice | Scene-gated snow/ice evidence. |
| 14 | Final snow/ice | Final resolved snow/ice mask. |
| 15 | Raw sand/dust | Raw sand/dust evidence. |
| 16 | Gated sand/dust | Scene-gated sand/dust evidence. |
| 17 | Final sand/dust | Final resolved sand/dust mask. |
| 18 | Depth confidence | ReShade depth confidence. |
| 19 | Depth-assisted sky/fog | Depth-assisted sky/fog contribution. |
| 20 | Stone/ruins | Final stone/ruins material mask. |
| 21 | Metal/industrial | Final metal/industrial material mask. |
| 22 | Crystal/aether | Final crystal/aether material mask. |
| 23 | Neon/glass | Final neon/glass material mask. |
| 24 | Fire/lava/heat | Final fire/lava/heat material mask. |
| 25 | Skin-protection | Final skin protection mask. |
| 26 | Void/darkness | Final void/darkness mask. |
| 27 | Raw water plane | Raw broad water-plane evidence. |
| 28 | Gated water plane | Scene-gated water-plane evidence. |
| 29 | Final water plane | Final resolved water-plane mask. |
| 30 | Raw specular glint | Raw thin glint evidence. |
| 31 | Gated specular glint | Scene-gated thin glint evidence. |
| 32 | Final specular glint | Final resolved glint mask. |
| 33 | Water resolver overview | Composite `WaterResolve` overview. |
| 34 | Raw cyan water | Cyan/turquoise water-like evidence. |
| 35 | Raw deep water | Dark/deep water evidence. |
| 36 | Shallow water | Shallow water confidence. |
| 37 | Deep water | Deep/open-water confidence. |
| 38 | Water horizon | Horizon/far-water support. |
| 39 | Wet shoreline | Shoreline/wet boundary evidence. |
| 40 | Foam/edge | Foam/wave/edge evidence. |
| 41 | Water receiver | Actual water reflection receiver. |
| 42 | Water source | Water source-color eligibility, not receiver. |
| 43 | Sky source vs reject | Blue source color vs red receiver rejection. |
| 44 | Sand/skin reject | Dry sand and skin rejection. |
| 45 | Water coherence | Local water coherence cue. |
| 46 | Shared safety overview | Composite `SafetyResolve` overview. |
| 47 | Shared receiver confidence | Legacy broad receiver confidence. |
| 48 | Shared light source confidence | Shared light/glint/emissive confidence. |
| 49 | SurfaceReflection receiver preview | Reflection receiver preview using shared role helpers. |
| 50 | SceneGI receiver/source preview | GI receiver/source preview using AO/structure helpers. |
| 51 | AtmosphereBloom eligibility preview | Bloom/glow eligibility preview. |
| 52 | WeatherAtmosphere air influence preview | Weather/air influence preview. |
| 53 | SmartSharpen dampening preview | Sharpen dampening preview. |
| 54 | AdaptiveGrade protection preview | Grade protection preview. |
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

`Dalashade_MaterialDebugOverlayMode` controls how the selected mode is shown:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Full debug replacement | Replace the image with the debug view. |
| 1 | Alpha blend over image | Blend debug over the source image. |
| 2 | Additive/tint overlay | Add/tint debug over the source image. |

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
