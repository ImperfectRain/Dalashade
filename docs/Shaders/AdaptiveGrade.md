# Dalashade_AdaptiveGrade

## Current purpose

`shaders/Dalashade_AdaptiveGrade.fx` provides Dalashade's base tonal layer. It adjusts exposure-like tone, contrast, saturation, temperature/tint, black depth, shadow readability, highlight rolloff, and material color preservation using generated scene/material/day/night uniforms.

## Intended purpose

AdaptiveGrade should remain the stable tonal foundation before GI, reflections, bloom, weather, and sharpening. It should express broad scene identity without becoming a LUT replacement or a weather/fog/reflection shader.

## Current implementation summary

The shader samples the backbuffer, resolves shared materials, water, and safety masks through `Dalashade_MaterialMasks.fxh`, then applies conservative grade adjustments. Daytime inputs add shoulder/highlight restraint, chroma restraint, small material-aware identity lanes, and tiny day shadow fill. Night inputs preserve the existing moonlight/artificial-light/ambient-darkness behavior. Standalone mode now adds bounded scene identity lanes inside the same grade path so AdaptiveGrade can carry more of the first-party stack's tonal identity without becoming a LUT, fog, bloom, reflection, GI, or sharpening pass. When NormalField mapping is enabled, AdaptiveGrade may also resolve `Dalashade_NormalField` for very small structure/detail protection; it does not use NormalField as material identity.

## Inputs

- Scene intent uniforms: night/day, weather, combat, duty, GPose, atmosphere, heat, cold, water/coastal context.
- Material uniforms: water, specular, sky/fog, sand, snow, foliage, stone/ruins, metal, crystal/aether, neon/glass, fire/lava/heat, skin, void.
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

## First-party shader mode

`Dalashade_StandaloneStrength` is `0` in Supportive mode and `1` in Standalone mode. Supportive keeps the existing conservative base-preset enhancement path: the standalone identity lanes evaluate to zero and the shader defaults also leave them inactive. Standalone routes additional scene identity through `standaloneSafe`, which is dampened by combat/readability and material safety before it reaches color temperature, contrast, saturation, highlight shoulder, black depth, material preservation, and source-relative delta caps. Standalone also uses a separate bounded final grade strength so AdaptiveGrade can visibly carry more of the first-party stack without changing Supportive output.

Standalone identity starts in AdaptiveGrade because this shader owns the broad tone/color foundation before GI, reflections, bloom, weather, and sharpening. Later standalone work can expand to WeatherAtmosphere air-layer identity, AtmosphereBloom source-class response, and only then SurfaceReflection or SceneGI after receiver validation.

## Standalone identity lanes

Each lane is Standalone-weighted, uses existing SceneIntent/MaterialIntent uniforms plus shared material/water/safety resolves, and remains bounded by the current clamp and source-relative delta guardrails.

| Lane | Inputs | Behavior | Must not do |
| --- | --- | --- | --- |
| Coastal day | Daylight, day reflection, open sky, coastal/water context, water pixel confidence, water receiver, warm RGB signal, sand, sky/fog, bright sand protection. | Preserves blue/cyan water, separates sky/water/sand, restores sun-warmed wood/sand/foreground mids, and restrains sky lift so open air stays natural blue instead of washed cyan. | Turn all sky cyan, globally cool the scene, make water reflective, or alter SurfaceReflection. |
| Coastal night | Night, moonlight, artificial light, ambient darkness, night atmosphere, coastal/water context, water pixel confidence, warm/aether light materials. | Cools moonlit coastal surfaces, preserves subtle water blues, protects warm lamps, deepens shadows without void crush. | Overcool skin, add reflection behavior, or make night pure void. |
| Desert / heat | Sand/dust, surface heat, heat, fire/heat material, bright sand protection. | Warms dry midtones, preserves sand detail, strengthens bright sand rolloff, slightly separates shadows. | Over-yellow skin, blow highlights, or add generic haze. |
| Snow / cold | Snow/ice, snow protection, cold, daylight/open sky, sky/cloud/fog. | Adds cold clarity, protects white detail, restrains oversaturated highlights, preserves snow/cloud transitions. | Gray out snow, crush blue shadows, or overdarken sky/clouds. |
| Forest / canopy | Foliage, day atmosphere, ambient darkness, foliage noise rejection. | Richens greens, adds canopy depth, preserves dark green readability, keeps shimmer-safe restraint. | Make foliage neon, sharpen foliage, or crush shaded greens. |
| Aether / Allagan / high-tech | Crystal/aether, neon/glass, metal/industrial, magic/neon glow, light source confidence, reflection receiver confidence as surface support only. | Preserves cyan/violet identity, adds controlled local contrast, keeps metallic coolness and black depth. | Classify aether as water, add bloom/reflection, or wash the frame purple/cyan. |
| Dungeon / interior | Ambient darkness, night, artificial light, stone/ruins, hard surface, metal, void/darkness, sky rejection. | Adds grounded contrast, controlled shadow richness, and readable warm pools. | Muddy the scene, crush black detail, globally lift shadows, or over-warm skin. |

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
| 7 | Final grade delta / standalone lanes | Amplified difference between graded and source color plus a Standalone lane overlay: coastal cyan, coastal night blue, desert orange, snow pale blue, forest green, aether/high-tech purple, interior warm gray/brown. |

`Dalashade_ShowDebugMask` remains a compatibility path for older debug behavior.

## Safety and suppression rules

Skin protection restrains tint and harsh sharpening-like contrast. Highlight protection prevents beach, sky, water, snow, sand, and specular areas from clipping. Water, sky/fog, snow, sand, aether/neon, fire/heat, and void material fields restrain only the grade components that would damage those identities. Coastal day uses separate water, sky, and warm-surface lanes so Standalone can keep teal water and cinematic depth while restoring sunlit wood/sand warmth and reducing open-sky whitening. Combat/readability dampening keeps heavy grading from interfering with gameplay and gates all Standalone identity lanes. Standalone allows larger but still capped source-relative deltas, then tightens those caps under highlight, skin, sky/fog, snow, forest, and readability pressure. If NormalField is disabled or unmapped, the NormalField shaping terms remain zero; this pass does not add new NormalField dependence.

`WaterSource`, `SkySource`, and `HorizonOnlyConfidence` are not used as identity proof, receiver proof, or material proof. `ReflectionReceiverConfidence` is used only as weak aether/high-tech surface support for tonal shaping, not as reflection permission.

## Validation screenshots

For the Standalone identity pass, capture Supportive versus Standalone pairs for coastal day, coastal night, desert/heat, snow/cold, forest/canopy, aether/Allagan, dungeon/interior, and one combat/readability scene. Expected Standalone differences are stronger tonal identity with preserved water/sky/sand separation, protected warm night lamps, warm dry desert midtones, cold snow clarity, richer but non-neon canopy greens, preserved cyan/violet aether identity without water confusion, grounded interior depth, and dampened influence during combat. In coastal day specifically, verify that foreground wood/sand keeps believable sun warmth, water remains teal/cyan without becoming reflective, and sky does not become pale or globally cyan-washed.

Also capture AdaptiveGrade debug modes 2, 3, 4, 5, 6, and 7. For coastal/aether ambiguity, capture MaterialDebug modes 55-65 before tuning material formulas.

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
