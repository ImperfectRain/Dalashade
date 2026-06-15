# Dalashade_AdaptiveGrade

## Current purpose

`shaders/Dalashade_AdaptiveGrade.fx` provides Dalashade's base tonal layer. It adjusts exposure-like tone, contrast, saturation, temperature/tint, black depth, shadow readability, highlight rolloff, and material color preservation using generated scene/material/day/night uniforms.

## Intended purpose

AdaptiveGrade should remain the stable tonal foundation before GI, reflections, bloom, weather, and sharpening. It should express broad scene identity without becoming a LUT replacement or a weather/fog/reflection shader.

## Current implementation summary

The shader samples the backbuffer, resolves shared materials, water, and safety masks through `Dalashade_MaterialMasks.fxh`, then applies conservative grade adjustments. Daytime inputs add shoulder/highlight restraint, chroma restraint, small material-aware identity lanes, and tiny day shadow fill. Night inputs preserve the existing moonlight/artificial-light/ambient-darkness behavior.

## Inputs

- Scene intent uniforms: night/day, weather, combat, duty, GPose, atmosphere, heat, cold, water/coastal context.
- Material uniforms: water, specular, sky/fog, sand, snow, foliage, metal, crystal/aether, neon, skin, void.
- Shared resolvers: `Dalashade_ResolveMaterials`, `Dalashade_ResolveWater`, `Dalashade_ResolveSafety`.
- User controls: grade strength and debug controls.
- Backbuffer color and optional depth assist.

## Outputs

The output is a graded backbuffer color plus debug visualizations. It does not emit material masks for other shaders.

## Core algorithm

1. Sample source color.
2. Resolve shared material, water, and safety masks.
3. Build tonal masks for shadows, mids, highlights, skin, sky, water, snow, sand, foliage, and void.
4. Apply conservative exposure/contrast/saturation/temperature adjustments.
5. Apply night or day contextual layers.
6. Apply highlight shoulder/chroma restraint and source-relative guardrails.
7. Return normal output or selected debug mask.

## Material/Water/Normal dependencies

AdaptiveGrade consumes material and water resolves for protection and color preservation only. It does not use NormalField and should not use water/normal data to create reflection, fog, bloom, or geometry effects.

## Debug modes

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Normal | Normal output. |
| 1 | Night masks | Night/moonlight/artificial-light masks. |
| 2 | Day masks | Daylight shoulder, chroma restraint, shadow fill, and day material response. |
| 3 | Material protection masks | Shared material protection lanes. |
| 4 | Highlight rolloff | Combined highlight rolloff and day shoulder. |
| 5 | Chroma restraint | High-sun chroma restraint. |
| 6 | Skin protection | Skin/tint restraint. |
| 7 | Final grade delta | Amplified difference between graded and source color. |

`Dalashade_ShowDebugMask` remains a compatibility path for older debug behavior.

## Safety and suppression rules

Skin protection restrains tint and harsh sharpening-like contrast. Highlight protection prevents beach, sky, water, snow, sand, and specular areas from clipping. Combat/readability dampening keeps heavy grading from interfering with gameplay.

## Current limitations

- It relies on post-process color/depth heuristics, not true material IDs.
- The day layer is broad and should not be used to simulate sunlight rays, haze, bloom, or reflection.
- Material preservation can only protect detected pixels.

## Future direction

Improve broad material identity only after the shared material debug views prove stable. Any future NormalField use should be limited to extremely broad protection, not detail shading.

## Do not do

- Do not add fog, particles, bloom, SSR, or AO here.
- Do not globally lift daylight exposure.
- Do not make day/night tags replace biome/material identity.
- Do not use pixel-level material logic more detailed than the shared resolver contract.
