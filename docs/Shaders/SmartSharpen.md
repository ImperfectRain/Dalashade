# Dalashade_SmartSharpen

## Current purpose

`shaders/Dalashade_SmartSharpen.fx` provides material-aware clarity while suppressing shimmer, halos, sky crunch, skin harshness, water texture noise, and bright snow/sand grain.

## Intended purpose

SmartSharpen should be the final Dalashade clarity pass. It should improve readability without duplicating external sharpeners or making every texture crispy.

## Current implementation summary

The shader separates structural edges from microtexture detail, resolves shared material/safety masks, applies dampening for unsafe regions, then blends a restrained sharpened result.

## Inputs

- Backbuffer color and depth.
- Scene intent uniforms for combat, day highlight pressure, open sky, fog/weather, and readability.
- Material uniforms for foliage, water, specular glint, sky/fog, skin, snow/ice.
- Shared material/water/safety resolvers.
- Sharpen strength, radius, threshold, dampening, and debug controls.

## Outputs

Normal output is source color with controlled clarity. Debug modes show edge/detail/dampening/final sharpen masks.

## Core algorithm

1. Sample source and local neighborhood.
2. Estimate structural edges and microtexture detail.
3. Resolve material/water/safety masks.
4. Dampening suppresses sky/fog, water texture, foliage shimmer, skin, snow, and specular halos.
5. Apply sharpen contribution with source-relative guardrails.

## Material/Water/Normal dependencies

Consumes MaterialMasks shared resolves. Does not currently consume NormalField. Future NormalField use should be limited to structure/detail gating.

## Debug modes

`Dalashade_MaterialDebugMode` is used when `ShowDebugMask` is enabled:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Off | Normal output unless `DebugView` is active. |
| 1 | Overview | Composite material dampening overview. |
| 2 | Foliage dampening | Foliage/organic green dampening. |
| 3 | Water/specular dampening | Water and glint dampening. |
| 4 | Snow/ice dampening | Snow/ice and bright-snow protection. |
| 5 | Sky/fog exclusion | Sky/fog and smooth-gradient exclusion. |
| 6 | Skin protection dampening | Skin/warm smooth protection. |
| 7 | Water plane dampening | Broad water/far-depth dampening. |
| 8 | Specular glint dampening | Specular edge/glint halo protection. |
| 9 | Unused | Reserved/no custom material debug branch. |
| 10 | Final material dampening | Final material/texture/structural dampening. |

`DebugView` is the older sharpen diagnostic selector:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Composite | Normal/composite output. |
| 1 | Structural edge | Stable structural edge mask. |
| 2 | Texture detail | Microtexture/detail mask. |
| 3 | Final sharpen | Final sharpen amount. |
| 4 | Dampening | Combined dampening/halo protection. |
| 5 | Foliage/far-depth | Foliage and far-depth dampening. |

## Safety and suppression rules

Sky/fog is strongly dampened. Skin avoids harsh contrast. Water suppresses texture crunch but keeps shoreline/large structure. Specular glints get halo protection. Foliage suppresses micro-shimmer. Snow/ice avoids grainy white texture.

## Current limitations

- It cannot know true object scale.
- It depends on prior stack order and existing external sharpeners.
- It can only infer UI/depth risk indirectly.

## Future direction

Use NormalField `StructureCandidate` and `DetailStrength` only after NormalDebug proves stable across foliage, water, snow, and combat scenes.

## Do not do

- Do not sharpen sky, fog, skin, or water texture aggressively.
- Do not compensate for soft reflections or GI by increasing sharpen.
- Do not become a second broad color/contrast grade.
