# Dalashade_AtmosphereBloom

## Current purpose

`shaders/Dalashade_AtmosphereBloom.fx` adds controlled bloom and atmospheric glow eligibility based on material, source, highlight, sky/fog, and safety masks.

## Intended purpose

AtmosphereBloom should own broad glow eligibility and material-aware bloom restraint. It should not become a reflection shader, fog shader, or global brightness pass.

## Current implementation summary

The shader samples local color, resolves shared material/water/safety masks, separates broad glow from thin glints, then gates bloom by material type and highlight safety. Aether, neon, fire/lamp, and shared `LightSourceConfidence` sources may bloom more strongly. Water and sky can provide source context, but water surfaces do not bloom broadly by themselves; foam/specular/glints contribute lightly. Optional NormalField data is used only to suppress unstable haloing around noisy edges.

## Inputs

- Backbuffer color and depth.
- Scene/weather/day/night/combat uniforms.
- Material uniforms for specular glint, water, foam/shoreline, crystal/aether, neon/glass, fire/heat, sky/fog, and skin protection.
- Shared `Dalashade_MaterialMasks.fxh` resolvers.
- Optional `Dalashade_NormalField.fxh` resolver for bloom-stability suppression only.
- Bloom strength, radius, threshold, tint, and debug controls.

## Outputs

Normal output is source color plus restrained bloom contribution. Debug modes visualize material bloom eligibility and final bloom masks.

## Core algorithm

1. Sample source color and a small blur neighborhood.
2. Resolve material/water/safety masks.
3. Build source eligibility from luma, shared light-source confidence, glints, aether/neon/fire, water/sky source context, and sky/fog.
4. Apply skin/highlight/sand/snow/foliage/sky/weather/combat dampening.
5. Blend a conservative bloom contribution into the source.

## Material/Water/Normal dependencies

Consumes `MaterialResolve`, `WaterResolve`, and `SafetyResolve`. `material.LightSourceConfidence`, `FireLavaHeat`, `CrystalAether`, `NeonGlass`, and `SpecularGlint` qualify bloom sources. `SkyCloudFog` and `water.SkySource` shape atmospheric sky/fog bloom only. `water.WaterSource` is source context only and is not receiver evidence.

When NormalField mapping is enabled, AtmosphereBloom resolves `Dalashade_NormalField` and uses `StructureCandidate`, `NormalConfidence`, and `EdgeDiscontinuity` only to reduce unstable halos. NormalField never creates glow sources and does not classify materials.

## Debug modes

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Off | Normal output. |
| 1 | Overview | Composite material bloom eligibility. |
| 2 | Water/specular | Water-related glint/foam eligibility. |
| 3 | Crystal/aether | Aether bloom support. |
| 4 | Neon/glass | Neon/glass bloom support. |
| 5 | Fire/heat | Lamp/fire/heat source support. |
| 6 | Sky/fog | Atmosphere-aware sky/fog diffusion support. |
| 7 | Final bloom eligibility | Final gated bloom mask. |
| 8 | Water plane | Water plane support. |
| 9 | Specular glint | Thin glint support. |

## Safety and suppression rules

Skin rejection, highlight protection, bright sand protection, snow protection, foliage/noise suppression, combat dampening, and sky/fog restraint prevent broad overbloom. Sky/cloud/fog can diffuse only when atmosphere intends it. NormalField edge discontinuity can further reduce bloom on unstable silhouettes, but it never increases bloom eligibility.

## Current limitations

- Bloom sources are inferred from screen-space color and material priors.
- It cannot distinguish every lamp from every bright surface.
- It depends on earlier stack color, so upstream grading/bloom can affect eligibility.
- NormalField stabilization is deliberately weak and depends on inferred screen-space confidence.

## Future direction

Use improved source classification from MaterialMasks when available. Keep broad glow and glint glow separate so water/foliage texture does not bloom noisily.

## Do not do

- Do not add reflection or SSR here.
- Do not make water surfaces bloom broadly.
- Do not use bloom to compensate for dark reflections or GI.
- Do not overbloom sky-wide haze unless weather/atmosphere explicitly supports it.
- Do not use NormalField as a light source, material classifier, or fake lighting pass.
