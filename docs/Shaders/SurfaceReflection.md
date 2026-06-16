# Dalashade_SurfaceReflection

## Current purpose

`shaders/Dalashade_SurfaceReflection.fx` simulates first-party screen-space reflection impressions for water, wet hard surfaces, metal/glass, ice, and aether/neon surfaces.

## Intended purpose

SurfaceReflection should become a XIV-focused reflection simulator that uses plugin-provided scene/material context and shared material/normal systems to place believable reflected light on plausible receivers. It should remain gameplay-safe and should not attempt full ray-traced reflection.

## Current implementation summary

The shader builds receiver masks from inline FrameData water, material, receiver, safety, scene, and optional surface fields. It then runs a layered post-process reflection model: water environment sheen, a water column-projected reflection source, a bounded colored reflection-ink compositor for water, water planar/structure support traces, wet hard-surface projection, hard/aether specular projection, and a bounded pseudo-SSR fallback.

This is pseudo-SSR. It does not reconstruct full world-space rays and it has no off-screen information.

## Inputs

- Backbuffer color and ReShade depth.
- Scene uniforms for water/coastal/open ocean/shallow water/wet surface/rain/day/night/aether.
- Material uniforms for water plane, water specular, specular glint, metal, crystal/aether, neon, fire/heat, sky/fog, skin, sand, snow/ice, foliage.
- Shared `Dalashade_FrameData.fxh` base fields for canonical material/water/safety/receiver data.
- Shared FrameData scene tags for readability, combat pressure, wetness, day/night, aether/neon, and Standalone mode strength.
- Optional FrameData surface fields from NormalField for receiver-valid projection shaping, edge leak suppression, and roughness/stability hints.
- User sliders for water sheen/reflection, specular glint/reflection, wet reflection, aether/neon/ice strength, sampling radius, softness, depth tolerance, and debug mode.

## Outputs

Normal output is source color plus clamped reflection contribution. Debug modes show sources, receivers, projected components, pseudo-SSR contribution, and final influence.

## Core algorithm

1. Resolve shared FrameData base, scene, and optional surface data.
2. Use optional surface data only to support already-valid receiver classes; it does not create water, metal, or wet-surface identity.
3. Build receiver categories and material-class quality terms:
   - water receiver from `frame.WaterReceiver`
   - wet hard-surface receiver from wet context, smoothness, hard material, and safety gates
   - metal/glass/aether receiver from `frame.ReceiverReflection`, metal, neon, crystal/aether, glints, and safety gates
   - glancing-angle/Fresnel response from the screen-space depth normal
   - polish terms for water quality, wet hard surfaces, metal, aether/glass, and ice
4. Build source categories:
   - water/sky color sources for water
   - specular/glint/fire/lamp sources
   - aether/neon sources
   - structure-biased pseudo-SSR sources
   - column-projected water sources sampled upward from valid water pixels
   - water-local planar approximation trace sources
5. Sample projected offsets:
   - water column projection that accepts above-water dark structure silhouettes without requiring water-depth continuity
   - water near/mid/far vertical projection as secondary support
   - water planar-approx trace for on-screen projected forms as secondary support
   - dark vertical structure sampling for pier posts/supports on valid water receivers, with a dedicated water-local silhouette contribution so detected posts can remain visible in normal output
   - wet hard-surface short and wide floor projection
   - metal/aether tight and directional streak projection
   - pseudo-SSR structure reflection sample
6. Apply sample safety:
   - UV bounds
   - depth continuity
   - silhouette/edge risk
   - sky/skin/foliage risk
   - source energy/source type qualification
   - NormalField edge-discontinuity risk when enabled
7. Combine positive contribution with conservative caps and small negative/contrast shaping where appropriate.
8. Composite a bounded water reflection-ink layer from column-projected source color so dark wood/structure silhouettes remain visible on bright or opaque water.
9. Return normal output or selected debug mode.

## Receiver/source separation

This shader depends on strict separation:

- `frame.WaterReceiver` is receiver evidence.
- `frame.ReceiverReflection` is shared reflection receiver support.
- `surface.ReflectionReceiverSupport` is small receiver-valid support, never a receiver class by itself.
- `WaterSource` is reflection source-color/context eligibility, not a water-surface receiver mask.
- `SkySource` is source color only.
- `HorizonOnlyConfidence` is source/context only.

Never use source-only color fields as receiver masks. Horizon water may color water reflection but must not become a reflective surface.

## First-party shader mode

`Dalashade_StandaloneStrength` is `0` in Supportive mode and `1` in Standalone mode. SurfaceReflection uses it to make existing valid water, wet hard-surface, metal/glass/aether, and pseudo-SSR contributions modestly more visible while preserving source/receiver separation. It never lets `WaterSource`, `SkySource`, `HorizonOnlyConfidence`, sky/fog, skin, foliage noise, or NormalField alone grant receiver permission.

## Debug modes

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Normal | Final output. |
| 1 | WaterPlane sheen | Water sheen component. |
| 2 | SpecularGlint | Thin glint component. |
| 3 | Wet reflection | Wet hard-surface component. |
| 4 | Aether/neon reflection | Aether/neon/glass component. |
| 5 | Sky rejection | Sky safety rejection. |
| 6 | Skin protection | Skin safety rejection. |
| 7 | Final reflection influence | Final contribution only. |
| 8 | Contribution over black | Reflection contribution on black. |
| 9 | Reflection source mask | Qualified source evidence. |
| 10 | Reflection receiver mask | Combined receiver evidence. |
| 11 | Water projected reflection | Primary water column projection plus secondary vertical, planar-approx, and water-local dark structure/post support. |
| 12 | Wet hard projected reflection | Short wet hard-surface projection component. |
| 13 | Metal/aether projected reflection | Tight streak projection component. |
| 14 | Pseudo SSR contribution | Structure-biased pseudo-SSR component. |

`Dalashade_SurfaceReflectionDebugOutputMode` controls how nonzero debug modes are displayed:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Full replacement diagnostic | Replace the image with the selected diagnostic. |
| 1 | Alpha overlay over original | Blend the diagnostic over the source image. |
| 2 | Side-by-side split | Diagnostic on the left, original on the right. |
| 3 | Contribution over black | Show contribution/debug over black. |
| 4 | Amplified difference view | Amplify result-vs-source difference with debug context. |

## Safety and suppression rules

Sky is source-only and must not receive reflection. Skin is rejected. UI/depth-risk regions are suppressed through depth/safety heuristics. Foliage and matte terrain are dampened. Water source, sky source, and horizon terms may color or qualify source samples but cannot create receiver permission. The column projection, planar-approx trace, dark structure sampling, and reflection-ink compositor are water-receiver-only and exist for on-screen forms such as pier posts/supports that are visible in the current backbuffer. The column projector intentionally does not require source-depth continuity with the water pixel because real reflected posts and platforms are above the water surface. When NormalField is enabled, `EdgeDiscontinuity` further suppresses pseudo-SSR ghosting and silhouette leaks, while `NormalConfidence`, `OrientationConfidence`, `StructureCandidate`, and `GroundPlaneCandidate` only shape projection stability inside existing receiver-valid regions. Reflection contribution is clamped to prevent full-screen gloss, broad fogging, and dark ghost silhouettes.

## Current limitations

- No real ray tracing, motion vectors, normals, or off-screen data.
- Object reflection depends on screen-space sampling and can miss expected sources, especially off-screen or occluded objects.
- Water reflection shape is inferred from upward screen-space column scans; it is not a real world-space water plane or render-target reflection.
- Depth continuity is approximate and can fail with ReShade depth issues.
- Dark scenes may show little visible reflected object shape unless source energy is present.
- Projection offsets are heuristic and receiver-type-specific.
- NormalField improves shape/stability hints only when configured and mapped; disabled NormalField keeps the shader on the material/water/safety path.

## Future direction

Next useful work should be guided by debug modes:

- If receiver is wrong, inspect FrameDataDebug first, then fix `MaterialMasks`/`WaterResolve` if the canonical resolver is wrong.
- If source is wrong, tune source qualification and pseudo-SSR sample safety.
- If projection is too weak, tune receiver-type projection weights, polish terms, and source qualification, not material classification.
- If ghosting returns, tighten sample safety before increasing strength.

NormalField can shape reflection roughness/projection and dampen edge leaks, but material/water receiver truth remains primary.

## Do not do

- Do not implement engine-side planar reflections, expensive ray marching, render targets, or temporal accumulation in this shader.
- Do not reflect UI, skin, or sky.
- Do not make all smooth/cyan surfaces reflective.
- Do not use reflection source-color fields such as `WaterSource`, `SkySource`, or horizon evidence as receiver masks.
- Do not use broad `ReceiverConfidence` as a major reflection boost.
- Do not use NormalField as material identity or let it create reflective surfaces without `frame.WaterReceiver` or `frame.ReceiverReflection` support.
