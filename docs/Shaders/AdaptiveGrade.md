# Dalashade_AdaptiveGrade

## Current purpose

`shaders/Dalashade_AdaptiveGrade.fx` provides Dalashade's base tonal layer. It adjusts exposure-like tone, contrast, saturation, temperature/tint, black depth, shadow readability, highlight rolloff, and material color preservation using generated scene/material/day/night uniforms.

## Intended purpose

AdaptiveGrade should remain the stable tonal foundation before GI, reflections, bloom, weather, and sharpening. It should express broad scene identity without becoming a LUT replacement or a weather/fog/reflection shader.

## Current implementation summary

The shader samples the backbuffer, resolves shared materials, water, and safety masks through `Dalashade_MaterialMasks.fxh`, then applies conservative grade adjustments. Daytime inputs add shoulder/highlight restraint, chroma restraint, small material-aware identity lanes, and tiny day shadow fill. Night inputs preserve the existing moonlight/artificial-light/ambient-darkness behavior. When NormalField mapping is enabled, AdaptiveGrade may also resolve `Dalashade_NormalField` for very small structure/detail protection; it does not use NormalField as material identity.

## Inputs

- Scene intent uniforms: night/day, weather, combat, duty, GPose, atmosphere, heat, cold, water/coastal context.
- Material uniforms: water, specular, sky/fog, sand, snow, foliage, metal, crystal/aether, neon/glass, fire/lava/heat, skin, void.
- Shared resolvers: `Dalashade_ResolveMaterials`, `Dalashade_ResolveWater`, `Dalashade_ResolveSafety`.
- Optional NormalField resolver: `Dalashade_ResolveNormalField` when NormalField is enabled and mapped.
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

AdaptiveGrade consumes material and water resolves for protection and color preservation only. Material influence protects skin, water, foliage, sand, snow/ice, sky/fog, aether/neon/glass, fire/lava/heat, and void/darkness from harsh tonal drift. It uses `WaterPixelConfidence` and `WaterReceiver` as tonal-protection inputs only; `WaterSource`, `SkySource`, and horizon/source-only evidence are not receiver or identity masks here.

NormalField is optional and secondary. When `Dalashade_NormalFieldEnabled` and the generated NormalField uniforms are active, AdaptiveGrade uses `StructureCandidate`, `NormalConfidence`, `DetailStrength`, and `EdgeDiscontinuity` as mild structure/detail protection. It does not classify water, sky, metal, skin, or foliage from NormalField, and it does not create lighting or geometry effects from inferred normals.

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

Skin protection restrains tint and harsh sharpening-like contrast. Highlight protection prevents beach, sky, water, snow, sand, and specular areas from clipping. Water, sky/fog, snow, sand, aether/neon, fire/heat, and void material fields restrain only the grade components that would damage those identities. Combat/readability dampening keeps heavy grading from interfering with gameplay. If NormalField is disabled or unmapped, the NormalField shaping terms remain zero.

## Current limitations

- It relies on post-process color/depth heuristics, not true material IDs.
- The day layer is broad and should not be used to simulate sunlight rays, haze, bloom, or reflection.
- Material preservation can only protect detected pixels.
- NormalField protection depends on inferred screen-space confidence and is intentionally too weak to act as a standalone effect.

## Future direction

Improve broad material identity only after the shared material debug views prove stable. Any future NormalField use should remain limited to broad protection and should not become detail shading, AO, reflection, or material classification.

## Do not do

- Do not add fog, particles, bloom, SSR, or AO here.
- Do not globally lift daylight exposure.
- Do not make day/night tags replace biome/material identity.
- Do not use pixel-level material logic more detailed than the shared resolver contract.
- Do not use NormalField as a material classifier or source of fake lighting.
