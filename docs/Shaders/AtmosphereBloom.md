# Dalashade_AtmosphereBloom

## Current purpose

`shaders/Dalashade_AtmosphereBloom.fx` adds controlled bloom and atmospheric glow eligibility based on material, source, highlight, sky/fog, and safety masks.

## Intended purpose

AtmosphereBloom should own broad glow eligibility and material-aware bloom restraint. It should not become a reflection shader, fog shader, or global brightness pass.

## Current implementation summary

The shader samples local color, resolves shared material/water/safety/source data through the inline `Dalashade_FrameData.fxh` contract, separates broad glow from thin glints, then gates bloom by material type and highlight safety. Aether, neon, fire/lamp, and shared `SourceLightConfidence` sources may bloom more strongly. Water and sky can provide source context, but water surfaces do not bloom broadly by themselves; foam/specular/glints contribute lightly. Canopy gap bloom looks for bright sky/light openings surrounded by darker foliage-textured samples, then rejects broad smooth sky so the entire sky dome does not bloom. Optional FrameData surface data is used only to suppress unstable haloing around noisy edges.

## Inputs

- Backbuffer color and depth.
- Scene/weather/day/night/combat uniforms.
- Material uniforms for foliage, specular glint, water, foam/shoreline, crystal/aether, neon/glass, fire/heat, sky/fog, and skin protection.
- Shared FrameData base resolver: `Dalashade_ResolveFrameBaseData`, which wraps canonical material, water, safety, source, and receiver resolves.
- Optional FrameData surface resolver: `Dalashade_ResolveFrameSurfaceData`, used only for the existing NormalField-backed bloom-stability suppression.
- Bloom strength, radius, threshold, tint, and debug controls.

## Outputs

Normal output is source color plus restrained bloom contribution. Debug modes visualize material bloom eligibility and final bloom masks.

## Core algorithm

1. Sample source color and a small blur neighborhood.
2. Resolve shared FrameData base fields and optional FrameData surface fields.
3. Build source eligibility from luma, shared light-source confidence, glints, aether/neon/fire, canopy gap checks, water/sky source context, and sky/fog.
4. Apply skin/highlight/sand/snow/foliage/sky/weather/combat dampening.
5. Blend a conservative bloom contribution into the source.

## Material/Water/Normal dependencies

Consumes FrameData base fields. `frame.SourceLightConfidence`, `frame.MaterialFireLavaHeat`, `frame.MaterialCrystalAether`, `frame.MaterialNeonGlass`, and `frame.WaterSpecularGlint` qualify bloom sources. `frame.MaterialFoliage`, `Dalashade_FoliageDensity`, daylight/atmosphere context, and local surround samples qualify canopy gap bloom. `frame.MaterialSkyCloudFog` and `frame.WaterSkySource` shape atmospheric sky/fog bloom only. `frame.WaterSource` is source context only and is not receiver evidence.

When NormalField mapping is enabled, AtmosphereBloom uses FrameData surface fields `surface.StructureCandidate`, `surface.NormalConfidence`, and `surface.EdgeDiscontinuity` only to reduce unstable halos. NormalField never creates glow sources and does not classify materials.

## First-party shader mode

`Dalashade_StandaloneStrength` is `0` in Supportive mode and `1` in Standalone mode. AtmosphereBloom uses it to slightly increase qualified source glow and clamp headroom after source safety, combat/readability dampening, and material highlight protection have already agreed. It does not lower bloom thresholds globally and does not make NormalField, water, sky, skin, or broad highlights into new glow sources.

## First-party performance tiers

`Dalashade_FirstPartyPerformanceTier` records the selected tier and `Dalashade_BloomSampleQuality` controls optional blur-ring work.

| Tier | AtmosphereBloom behavior |
| --- | --- |
| Quality | Preserves the current full bloom gather: center, near ring, and far ring samples. |
| Balanced | Keeps center and near ring samples, skipping the far ring. Bloom eligibility, safety gates, and strengths are unchanged. |
| Performance | Uses the same cheap center-plus-near path as Balanced and relies on existing source qualification instead of adding extra gather reach. |

Lower tiers do not boost bloom strength, threshold, or tint to compensate for fewer samples.

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
| 10 | Canopy gap bloom | Red shows final canopy gap source, green shows canopy permission, blue shows source safety. |

## Safety and suppression rules

Skin rejection, highlight protection, bright sand protection, snow protection, foliage/noise suppression, combat dampening, and sky/fog restraint prevent broad overbloom. Sky/cloud/fog can diffuse only when atmosphere intends it. Canopy gap bloom must have a bright center, darker foliage-like surrounding samples, local contrast, and broad-sky rejection. NormalField edge discontinuity can further reduce bloom on unstable silhouettes, but it never increases bloom eligibility.

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
