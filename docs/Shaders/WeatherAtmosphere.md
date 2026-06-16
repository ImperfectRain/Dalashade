# Dalashade_WeatherAtmosphere

## Current purpose

`shaders/Dalashade_WeatherAtmosphere.fx` adds scene/weather-aware air, haze, damp diffusion, heat/dust, snow/cold clarity, coastal humidity, and sky/fog response.

## Intended purpose

WeatherAtmosphere should own atmospheric mood and air behavior. It should not create water shimmer, reflection, bloom, particles, rain streaks, or hard material lighting.

## Current implementation summary

The shader samples color/depth, resolves shared material/water/safety masks, and applies depth-aware atmospheric influence according to weather, day/night, sky/fog, water/coastal context, sand/dust, snow/ice, foliage, wetness, heat, aether/neon, industrial/stone structure, void darkness, and fire/heat cues. In Standalone mode it adds stronger weather identity lanes for clear/open air, clear coastal air, coastal/cloudy night air, rain/wet air, storm gloom, fog/mist, cloud/overcast, dust storm, heat wave, snow/cold, humid canopy, aether/umbral/cosmic air, industrial air, stone/interior air, transition air, and gloom while retaining the same safety gates. Optional NormalField data can mildly anchor air to plausible ground/structure and suppress edge buildup; it does not choose weather type.

## Inputs

- Backbuffer color and depth.
- Scene intent: weather, rain/storm, fog/mist, cloud/overcast, day atmosphere, night atmosphere, open sky, surface heat, industrial hardness, cosmic mood, coastal/water context, combat/duty.
- Material uniforms: sky/fog, sand/dust, snow/ice, foliage, water/wet, stone/ruins, metal/industrial, crystal/aether, neon/glass, fire/lava/heat, void/darkness, and skin protection.
- Shared material/water/safety resolvers.
- Optional NormalField resolver for ground/structure anchoring and edge-discontinuity restraint.
- Weather atmosphere strength, depth, dampening, and debug controls.

## Outputs

Normal output is source color with restrained atmospheric modification. Debug modes visualize material/weather air influence and final contribution.

## Core algorithm

1. Resolve material/water/safety masks.
2. Build air influence from sky/fog, depth, weather, and scene context.
3. Add material-specific air lanes for coastal dampness, storm/rain, cloud/overcast, sand/dust, heat, snow/cold clarity, foliage humidity, aether/neon/cosmic veil, industrial hardness, stone/interior air, and warm heat-source air.
4. In Standalone mode, resolve weather identity lanes that strengthen air color, visibility, wet diffusion, mood darkening, highlight rolloff, and final guardrail headroom without changing weather detection.
5. Apply safety gates for skin, highlights, foliage noise, sky/non-sky effects, and combat readability.
6. Blend a conservative atmospheric result.

## Material/Water/Normal dependencies

Consumes `MaterialResolve`, `WaterResolve`, and `SafetyResolve`. Water/coastal context affects air/shoreline humidity, not literal reflection: `water.WaterSource` is only atmosphere/source context, while `water.WaterPixelConfidence`, `WaterReceiver`, and `WetShoreline` are used for local water/wet plausibility. Skin and highlight safety come from `safety.SkinReject`, `HighlightProtect`, `FoliageNoiseReject`, `BrightSandProtect`, and `SnowProtect`.

Optional NormalField support uses `GroundPlaneCandidate` for mild fog/dust/wetness grounding, `StructureCandidate` for subtle structure silhouette anchoring, `NormalConfidence` as a small stability gate, and `EdgeDiscontinuity` to avoid outline/halo buildup. NormalField does not classify water, sky, weather, or material identity.

## First-party shader mode

`Dalashade_StandaloneStrength` is `0` in Supportive mode and `1` in Standalone mode. Supportive keeps the conservative weather-air path. Standalone routes additional influence through `standaloneAtmosphere`, which is dampened by combat/readability, skin, highlight, foliage-noise, and NormalField edge safety before it reaches any color or visibility term.

Standalone weather identity lanes:

| Lane | Inputs | Behavior | Must not do |
| --- | --- | --- | --- |
| Clear coastal air | Daylight, open sky, coastal/water context, water pixel confidence, day reflection, low haze/wetness. | Adds clean blue distance and subtle sea-air depth while preserving foreground clarity. | Turn the whole scene cyan or create reflection/shimmer. |
| Clear open air | Daylight, open sky, day atmosphere, sunlight, low haze/wetness/fog. | Adds restrained clean-sky distance tone for fair-weather scenes. | Turn clear weather into fog or global blue wash. |
| Coastal/cloudy night air | Night, haze, night atmosphere, moonlight, artificial light, coastal/water context, water/specular material, sky/fog. | Adds cool damp sea-air separation and visible atmospheric distance even when weather is Clouds rather than Rain. | Require rain wetness, wash lamps, or turn the scene into gray fog. |
| Rain/wet air | Wetness, water mist, wet shoreline, water/specular material, day/night atmosphere. | Adds wet-air diffusion, glint softening, and cool damp visibility. | Add rain streaks, particles, or bloom-like halos. |
| Storm air | Wetness, haze/atmosphere, sky/fog, night atmosphere, ambient darkness, manual mood. | Adds cool storm veil, stronger distance separation, and mood darkening. | Crush combat readability or flatten black depth into gray. |
| Fog/mist | Haze, sky/fog material, day/night atmosphere, depth. | Adds layered visibility falloff without full-frame milkiness. | Treat all sky/fog evidence as a non-air material effect. |
| Cloud/overcast | Sky/fog, haze, day/night atmosphere, depth. | Adds soft cool cloud mass and mild highlight restraint. | Become full fog or erase local material contrast. |
| Dust storm | Sand/dust, haze, heat, bright sand protection. | Adds warmer distance dust mass and stronger sand highlight shoulder. | Yellow skin, blow sand highlights, or lift the entire screen. |
| Heat wave | Heat, surface heat, distance, daylight. | Adds dry warmth and distance-weighted heat softness. | Add generic haze to wet/foggy scenes. |
| Snow/cold | Cold, snow/ice material, snow protection, open sky, sky/fog. | Adds blue-white cold air and protects white detail. | Gray snow, clip white fields, or crush blue shadow detail. |
| Humid canopy | Foliage, wetness, haze, day/night atmosphere, foliage-noise rejection. | Adds green-gold humid air and canopy opening light. | Make foliage neon or brighten dark trunks globally. |
| Aether/umbral/cosmic | Crystal/aether, neon/glass, magic/neon glow, cosmic mood, night atmosphere, sky/fog. | Adds cyan/violet alien air and local aether atmosphere. | Classify as water, add bloom/reflection, or wash the frame purple. |
| Industrial air | Industrial hardness, metal/industrial, neon/glass, atmosphere. | Adds cooler hard-surface air and restrained specular glow support. | Add reflection or brittle global contrast. |
| Stone/interior air | Stone/ruins, surface hardness, ambient darkness, artificial light, low sky/fog. | Adds grounded interior depth and warm local-air response. | Muddy everything or globally lift shadows. |
| Transition air | Dawn/dusk-style low daylight/night confidence, atmosphere, open sky/moonlight. | Adds warm low-sun air without needing a hand-tuned zone rule. | Override clear day/night identity. |
| Gloom/void | Ambient darkness, night, sky/fog, night atmosphere, void darkness, manual mood. | Adds dark atmospheric weight while preserving black-depth intent. | Become generic fog or globally lift shadows. |

WeatherAtmosphere still does not change weather detection, turn all sky/fog into haze, or apply dampness uniformly.

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

Avoid gray haze wash. Do not treat water source/context as a receiver. Do not apply non-air effects to sky. Protect skin, bright sand/snow, glints, and important combat readability. Depth-assisted and NormalField-assisted effects should weaken when confidence is poor. Standalone weather lanes must remain air/visibility/tint lanes only; they must not simulate particles, reflection, water shimmer, bloom, or hard lighting.

## Validation screenshots

Capture Supportive versus Standalone pairs for:

- Clear/fair coastal day.
- Cloudy coastal night.
- Rain or showers in a coastal/city scene.
- Thunderstorm, storm, or gales.
- Fog/mist/clouds in a forest or open zone.
- Dust storm or heat wave in a desert zone.
- Snow/blizzard/cold scene.
- Humid jungle/rainforest scene.
- Aether/umbral/cosmic scene.
- Gloom/void scene.
- One combat/readability scene in bad weather.

Also capture `Dalashade_MaterialDebugMode` 1-10 and legacy `Dalashade_DebugView` 1-5 for at least one rain, dust/heat, snow/cold, and aether/umbral case.

## Current limitations

- Atmosphere is post-process and cannot know true volumetric lighting.
- Depth-based air depends on ReShade depth.
- Weather tags are scene-level priors and may under/overstate local conditions.

## Future direction

Refine day/night/coastal/storm/desert/snow/aether lanes through screenshot comparisons. NormalField should remain a secondary placement/stability hint only; if stronger weather anchoring is needed, validate it first with debug screenshots.

## Do not do

- Do not add reflection, water shimmer, particles, rain streaks, or bloom here.
- Do not globally lift or gray-wash the scene.
- Do not let sky/fog masks dirty non-air material behavior.
- Do not use NormalField wall/ground/detail as hard weather truth.
