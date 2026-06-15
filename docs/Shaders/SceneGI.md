# Dalashade_SceneGI

## Current purpose

`shaders/Dalashade_SceneGI.fx` creates a cheap screen-space GI/AO impression using depth, material masks, localized source terms, and safety gates.

## Intended purpose

SceneGI should own conservative contact shading, low-frequency material bounce, localized night light pooling, and material-aware ambient response. It is not RTGI, PTGI, path tracing, or a replacement for game lighting.

## Current implementation summary

The shader samples nearby color/depth, builds receiver/source masks from shared material/water/safety resolves, then applies layered AO and restrained bounce/light pooling. Water and sky are protected from dirty AO; aether/neon/fire/glint sources can contribute localized light. Optional NormalField support adds small contact/structure grounding only after material and safety gates allow it.

## Inputs

- Backbuffer color and depth.
- Scene intent uniforms for night, moonlight, artificial light, ambient darkness, day/open sky, combat, duty, weather, and atmosphere.
- Material uniforms and water context.
- Shared material/water/safety resolvers.
- Optional NormalField resolver for conservative contact/structure shaping.
- GI/AO strength/radius/debug controls.

## Outputs

Normal output is source color plus conservative AO/bounce modifications. Debug modes show source/receiver/final GI influence where available.

## Core algorithm

1. Resolve material, water, and safety masks.
2. Build GI sources from light/glint/aether/neon/fire and local luminance.
3. Build receivers from shared AO/structure receiver helpers, material hardness, water/sky/skin gates, and scene context.
4. Optionally add small NormalField structure, AO, ground/contact, and edge-discontinuity support behind the same safety gates.
5. Estimate local occlusion/bounce with small screen-space taps.
6. Apply conservative darkening/lighting with safety clamps.

## Material/Water/Normal dependencies

SceneGI consumes MaterialMasks shared resolves. The shared material confidence path prefers `Dalashade_GetAOReceiver(...)` and `Dalashade_GetStructureReceiver(...)` over legacy broad `ReceiverConfidence`; local material terms still shape the final GI role. Water surfaces suppress dirty AO; `WetShoreline` may support local light pooling.

Optional NormalField support uses `StructureCandidate` as mild structure grounding, `AOReceiver` as mild AO/contact support, `GroundPlaneCandidate` as mild ground/contact shaping, `EdgeDiscontinuity` as localized contact support only under safety gates, and `NormalConfidence`/`OrientationConfidence` as stability terms. NormalField cannot override sky, skin, water AO, or material safety rejects.

## Debug modes

SceneGI debug modes are intended to show shared material usage, sources, receivers, AO, bounce, and final influence. Treat them as effect diagnostics, not material truth; use MaterialDebug for base material classification.

`Dalashade_GIDebugMode`:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Off | Normal output. |
| 1 | AO only | Layered AO mask. |
| 2 | Bounce only | Material bounce contribution. |
| 3 | Night light pooling | Local night/emissive pooling. |
| 4 | Material influence | Shared material receiver/source influence, with NormalField support shown subtly when enabled. |
| 5 | Sky rejection | Sky/fog safety rejection. |
| 6 | Skin protection | Skin safety rejection. |
| 7 | Final GI influence | Final combined GI/AO influence. |
| 8 | Depth-normal confidence | Depth normal and confidence support, including NormalField confidence when enabled. |
| 9 | Emissive source | Local/aether/neon/fire/glint source confidence. |
| 10 | Bounce receiver | Final bounce receiver mask. |
| 11 | Adaptive limits/safety | Positive/negative contribution and safety clamps. |
| 12 | Layered AO breakdown | Micro/medium/broad AO channels plus NormalField contact support when enabled. |

`Dalashade_GIDebugOutputMode`:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Full replacement | Replace output with the selected debug view. |
| 1 | Alpha overlay over original | Blend debug over the source image. |
| 2 | Side-by-side split | Debug on the left, original on the right. |
| 3 | Contribution over black | Show debug/contribution without source image. |
| 4 | Amplified difference | Show amplified result-vs-source difference plus debug context. |

## Safety and suppression rules

Sky rejects AO/GI. Skin rejects dirty AO/tinting. Water suppresses dirty AO. Foliage uses foliage/noise damping. Combat/readability dampens heavy output. Snow and bright sand are protected from muddy darkening. NormalField is multiplied by the same sky/skin/water safety gates and remains a secondary shaping input.

## Current limitations

- Screen-space samples cannot see off-screen lights.
- Source/receiver masks are heuristic.
- AO can only approximate contact and crevice behavior.
- It cannot infer true light direction or material albedo.

## Future direction

Validate NormalField-assisted contact in dock, ruin, city, snow, desert, foliage, and combat scenes before increasing weights. Keep material source/receiver separation explicit.

## Do not do

- Do not add expensive ray marching or temporal accumulation.
- Do not dirty sky, skin, water, or snow.
- Do not treat specular glints as broad diffuse bounce.
- Do not let NormalField create AO where material/safety rejects it.
