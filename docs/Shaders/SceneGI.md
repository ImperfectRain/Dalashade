# Dalashade_SceneGI

## Current purpose

`shaders/Dalashade_SceneGI.fx` creates a cheap screen-space GI/AO impression using depth, material masks, localized source terms, and safety gates.

## Intended purpose

SceneGI should own conservative contact shading, low-frequency material bounce, localized night light pooling, and material-aware ambient response. It is not RTGI, PTGI, path tracing, or a replacement for game lighting.

## Current implementation summary

The shader samples nearby color/depth, builds receiver/source masks from shared material/water/safety resolves, then applies layered AO and restrained bounce/light pooling. Water and sky are protected from dirty AO; aether/neon/fire/glint sources can contribute localized light.

## Inputs

- Backbuffer color and depth.
- Scene intent uniforms for night, moonlight, artificial light, ambient darkness, day/open sky, combat, duty, weather, and atmosphere.
- Material uniforms and water context.
- Shared material/water/safety resolvers.
- GI/AO strength/radius/debug controls.

## Outputs

Normal output is source color plus conservative AO/bounce modifications. Debug modes show source/receiver/final GI influence where available.

## Core algorithm

1. Resolve material, water, and safety masks.
2. Build GI sources from light/glint/aether/neon/fire and local luminance.
3. Build receivers from material hardness, structure, water/sky/skin gates, and scene context.
4. Estimate local occlusion/bounce with small screen-space taps.
5. Apply conservative darkening/lighting with safety clamps.

## Material/Water/Normal dependencies

SceneGI consumes MaterialMasks shared resolves. NormalField is not a production dependency yet. Water surfaces suppress dirty AO; `WetShoreline` may support local light pooling.

## Debug modes

SceneGI debug modes are intended to show shared material usage, sources, receivers, AO, bounce, and final influence. Treat them as effect diagnostics, not material truth; use MaterialDebug for base material classification.

## Safety and suppression rules

Sky rejects AO/GI. Skin rejects dirty AO/tinting. Water suppresses dirty AO. Foliage uses foliage/noise damping. Combat/readability dampens heavy output. Snow and bright sand are protected from muddy darkening.

## Current limitations

- Screen-space samples cannot see off-screen lights.
- Source/receiver masks are heuristic.
- AO can only approximate contact and crevice behavior.
- It cannot infer true light direction or material albedo.

## Future direction

If NormalField becomes stable, use `StructureCandidate` and `AOReceiver` as minor shaping inputs. Keep material source/receiver separation explicit.

## Do not do

- Do not add expensive ray marching or temporal accumulation.
- Do not dirty sky, skin, water, or snow.
- Do not treat specular glints as broad diffuse bounce.
