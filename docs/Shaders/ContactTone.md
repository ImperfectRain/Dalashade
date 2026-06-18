# Dalashade_ContactTone

`shaders/Dalashade_ContactTone.fx` is a first-party local grounding and readability pass. It strengthens contact edges, darkens plausible grounded transitions, and adds controlled local contrast where FrameData says the pixel is a safe receiver.

## Identity

ContactTone is intentionally separate from SceneGI, SurfaceReflection, WeatherAtmosphere, and AtmosphereBloom.

- It does not add color bounce, emissive pooling, or indirect light.
- It does not project reflections or make surfaces glossy.
- It does not create air haze, fog, or bloom halos.
- It does not classify true FFXIV engine materials.

The shader exists so users can make object-ground contact, terrain seams, hard-surface breaks, foliage-ground transitions, and local depth edges more readable without enabling GI color bounce or reflection behavior.

## Inputs

ContactTone consumes inline `Dalashade_FrameData.fxh`:

- `FrameBaseData`: material, water, sky, skin, receiver, and safety fields.
- `FrameSurfaceData`: optional depth/NormalField edge, structure, wall, ground, and AO receiver fields.
- `FrameSceneData`: shared scene lanes such as wet air, forest canopy, industrial hardness, readability, shadow protection, and standalone strength.

It also consumes scene/material uniforms written by Dalashade when enabled:

- Scene: `Dalashade_Readability`, `Dalashade_Atmosphere`, `Dalashade_HighlightProtection`, `Dalashade_ShadowProtection`, `Dalashade_Wetness`, `Dalashade_FoliageDensity`, `Dalashade_IndustrialHardness`, `Dalashade_CombatPressure`, `Dalashade_CinematicPermission`.
- Materials: foliage, water plane/specular/glint, wet surface context, sand, snow, stone, metal, sky/fog, skin protection, and void darkness.

## Controls

| Uniform | Purpose |
| --- | --- |
| `Dalashade_ContactToneEnabled` | Master pass enable. Generated section default is `0.0`; plugin writes `1` only when ContactTone variable writes are enabled. |
| `Dalashade_ContactToneStrength` | Overall contact tone amount. Default plugin value is intentionally visible at `0.42`. |
| `Dalashade_ContactToneRadius` | Neighborhood radius for depth/local contrast tests. |
| `Dalashade_ContactToneEdgeStrength` | Depth discontinuity contribution. |
| `Dalashade_ContactToneStructureStrength` | FrameData surface/normal/structure contribution. |
| `Dalashade_ContactToneContrastStrength` | Local contrast shaping applied after safe contact darkening. |
| `Dalashade_StandaloneStrength` | Small safety-gated visibility multiplier in standalone first-party mode. |

## Debug Modes

| Mode | Meaning |
| ---: | --- |
| `0` | Off / normal output |
| `1` | Contact mask |
| `2` | Depth edge component |
| `3` | Surface/normal edge component |
| `4` | Receiver/safety mask |
| `5` | Suppression mask |
| `6` | Final contribution |

`Dalashade_ContactToneDebugOpacity` controls diagnostic overlay opacity. Debug modes are effect diagnostics, not material IDs.

## Safety

ContactTone suppresses sky, skin, water receiver risk, snow, bright sand, strong highlights, and UI/depth uncertainty. It treats material and screenshot-derived intent as scene-level permission only; shader-side FrameData still decides where contact tone can appear.

Technique sync is optional. When `SyncDalashadeTechniqueActivation` is enabled, the generated preset can add `Dalashade_ContactTone` only when `EnableDalashadeContactToneShaderVariables` is enabled. Otherwise users enable the ReShade technique manually.
