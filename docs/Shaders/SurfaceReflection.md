# Dalashade_SurfaceReflection

## Current purpose

`shaders/Dalashade_SurfaceReflection.fx` simulates first-party screen-space reflection impressions for water, wet hard surfaces, metal/glass, ice, and aether/neon surfaces.

## Intended purpose

SurfaceReflection should become a XIV-focused reflection simulator that uses plugin-provided scene/material context and shared material/normal systems to place believable reflected light on plausible receivers. It should remain gameplay-safe and should not attempt full ray-traced reflection.

## Current implementation summary

The shader builds receiver masks from `WaterResolve`, `MaterialResolve.ReflectionReceiverConfidence`, wet/hard/smooth material support, and safety gates. It samples shifted backbuffer positions as pseudo reflection sources, qualifies those samples by source energy, depth continuity, source type, and silhouette safety, then applies restrained projected reflection components.

This is pseudo-SSR. It does not reconstruct full world-space rays and it has no off-screen information.

## Inputs

- Backbuffer color and ReShade depth.
- Scene uniforms for water/coastal/open ocean/shallow water/wet surface/rain/day/night/aether.
- Material uniforms for water plane, water specular, specular glint, metal, crystal/aether, neon, fire/heat, sky/fog, skin, sand, snow/ice, foliage.
- Shared `Dalashade_MaterialResolve`, `Dalashade_WaterResolve`, and `Dalashade_SafetyResolve`.
- User sliders for water sheen/reflection, specular glint/reflection, wet reflection, aether/neon/ice strength, sampling radius, softness, depth tolerance, and debug mode.

## Outputs

Normal output is source color plus clamped reflection contribution. Debug modes show sources, receivers, projected components, pseudo-SSR contribution, and final influence.

## Core algorithm

1. Resolve shared material, water, and safety masks.
2. Build receiver categories:
   - water receiver from `water.WaterReceiver`
   - wet hard-surface receiver from wet context, smoothness, hard material, and safety gates
   - metal/glass/aether receiver from `material.ReflectionReceiverConfidence`, metal, neon, crystal/aether, glints, and safety gates
3. Build source categories:
   - water/sky color sources for water
   - specular/glint/fire/lamp sources
   - aether/neon sources
   - structure-biased pseudo-SSR sources
4. Sample projected offsets:
   - water vertical projection
   - wet hard-surface short projection
   - metal/aether tight streak projection
   - pseudo-SSR structure reflection sample
5. Apply sample safety:
   - UV bounds
   - depth continuity
   - silhouette/edge risk
   - sky/skin/foliage risk
   - source energy/source type qualification
6. Combine positive contribution with conservative caps and small negative/contrast shaping where appropriate.
7. Return normal output or selected debug mode.

## Receiver/source separation

This shader depends on strict separation:

- `WaterReceiver` is receiver evidence.
- `MaterialResolve.ReflectionReceiverConfidence` is shared reflection receiver support.
- `WaterSource` is reflection source-color/context eligibility, not a water-surface receiver mask.
- `SkySource` is source color only.
- `HorizonOnlyConfidence` is source/context only.

Never use source-only color fields as receiver masks. Horizon water may color water reflection but must not become a reflective surface.

## Debug modes

| Mode | Meaning |
| --- | --- |
| Normal | Final output. |
| WaterPlane sheen | Water sheen component. |
| SpecularGlint | Thin glint component. |
| Wet reflection | Wet hard-surface component. |
| Aether/neon reflection | Aether/neon/glass component. |
| Sky rejection | Sky safety rejection. |
| Skin protection | Skin safety rejection. |
| Final reflection influence | Final contribution only. |
| Contribution over black | Reflection contribution on black. |
| Reflection source mask | Qualified source evidence. |
| Reflection receiver mask | Combined receiver evidence. |
| Water projected reflection | Water vertical projection component. |
| Wet hard projected reflection | Short wet hard-surface projection component. |
| Metal/aether projected reflection | Tight streak projection component. |
| Pseudo SSR contribution | Structure-biased pseudo-SSR component. |

## Safety and suppression rules

Sky is source-only and must not receive reflection. Skin is rejected. UI/depth-risk regions are suppressed through depth/safety heuristics. Foliage and matte terrain are dampened. Reflection contribution is clamped to prevent full-screen gloss, broad fogging, and dark ghost silhouettes.

## Current limitations

- No real ray tracing, motion vectors, normals, or off-screen data.
- Object reflection depends on screen-space sampling and can miss expected sources.
- Depth continuity is approximate and can fail with ReShade depth issues.
- Dark scenes may show little visible reflected object shape unless source energy is present.
- Projection offsets are heuristic and receiver-type-specific.

## Future direction

Next useful work should be guided by debug modes:

- If receiver is wrong, fix `MaterialMasks`/`WaterResolve`.
- If source is wrong, tune source qualification and pseudo-SSR sample safety.
- If projection is too weak, tune receiver-type projection weights, not material classification.
- If ghosting returns, tighten sample safety before increasing strength.

NormalField may later help shape reflection roughness/projection, but material receiver truth should remain primary.

## Do not do

- Do not implement expensive ray marching or temporal accumulation.
- Do not reflect UI, skin, or sky.
- Do not make all smooth/cyan surfaces reflective.
- Do not use reflection source-color fields such as `WaterSource`, `SkySource`, or horizon evidence as receiver masks.
- Do not use broad `ReceiverConfidence` as a major reflection boost.
