# Dalashade_WeatherAtmosphere

## Current purpose

`shaders/Dalashade_WeatherAtmosphere.fx` adds scene/weather-aware air, haze, damp diffusion, heat/dust, snow/cold clarity, coastal humidity, and sky/fog response.

## Intended purpose

WeatherAtmosphere should own atmospheric mood and air behavior. It should not create water shimmer, reflection, bloom, or hard material lighting.

## Current implementation summary

The shader samples color/depth, resolves shared material/water/safety masks, and applies depth-aware atmospheric influence according to weather, day/night, sky/fog, water/coastal context, sand/dust, snow/ice, foliage, wetness, and heat.

## Inputs

- Backbuffer color and depth.
- Scene intent: weather, rain/storm, fog/mist, day atmosphere, night atmosphere, open sky, surface heat, coastal/water context, combat/duty.
- Material uniforms: sky/fog, sand/dust, snow/ice, foliage, water/wet, crystal/aether.
- Shared material/water/safety resolvers.
- Weather atmosphere strength, depth, dampening, and debug controls.

## Outputs

Normal output is source color with restrained atmospheric modification. Debug modes visualize material/weather air influence and final contribution.

## Core algorithm

1. Resolve material/water/safety masks.
2. Build air influence from sky/fog, depth, weather, and scene context.
3. Add material-specific air lanes for coastal dampness, storm/rain, sand/dust heat, snow/cold clarity, foliage humidity, and aether veil.
4. Apply safety gates for skin, highlights, sky/non-sky effects, and combat readability.
5. Blend a conservative atmospheric result.

## Material/Water/Normal dependencies

Consumes MaterialMasks shared resolves. Does not use NormalField. Water/coastal context affects air/shoreline humidity, not literal reflection.

## Debug modes

`Dalashade_MaterialDebugMode` is used when `Dalashade_ShowDebugMask` is enabled:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Off | Normal output unless `Dalashade_DebugView` is active. |
| 1 | Overview | Composite atmospheric influence. |
| 2 | Foliage humidity | Foliage/canopy dampening and humidity. |
| 3 | Sand/dust depth | Desert/dust/heat air. |
| 4 | Snow/ice air | Snow/cold air. |
| 5 | Water/wet mist | Coastal/wet/rain mist support. |
| 6 | Crystal/aether veil | Aether atmosphere tint. |
| 7 | Sky/fog depth | Sky/fog/depth driver. |
| 8 | Final air influence | Final gated atmospheric mask. |
| 9 | Water plane air | Water/coastal air support. |
| 10 | Specular glint response | Highlight/glint atmospheric response. |

`Dalashade_DebugView` is the older atmosphere diagnostic selector:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Composite | Normal/composite output. |
| 1 | Depth haze | Depth-driven haze amount. |
| 2 | Highlight protection | Highlight rolloff/protection. |
| 3 | Weather glow | Weather/local glow response. |
| 4 | Foliage dampening | Foliage/canopy damping. |
| 5 | Heat/dust | Heat and dust contribution. |

## Safety and suppression rules

Avoid gray haze wash. Do not treat water pixels as reflection. Do not apply non-air effects to sky. Protect skin and important combat readability. Depth-assisted effects should weaken when depth confidence is poor.

## Current limitations

- Atmosphere is post-process and cannot know true volumetric lighting.
- Depth-based air depends on ReShade depth.
- Weather tags are scene-level priors and may under/overstate local conditions.

## Future direction

Refine day/night/coastal/storm/desert/snow lanes through screenshot comparisons. If NormalField is used later, only broad structure/ground hints should inform haze placement.

## Do not do

- Do not add reflection, water shimmer, particles, or bloom here.
- Do not globally lift or gray-wash the scene.
- Do not let sky/fog masks dirty non-air material behavior.
