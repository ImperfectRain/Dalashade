# Dalashade MaterialMasks Contract

`shaders/Dalashade_MaterialMasks.fxh` is the shared material resolver contract for first-party Dalashade shaders. It converts backbuffer color, UV, ReShade depth, generated scene/material priors, and cheap local image features into material, water, safety, and receiver confidence values.

The include is intentionally conservative. It should provide shared truth, not final effect styling.

`Dalashade_FrameData.fxh` may wrap these outputs for first-party shader consumption, but MaterialMasks remains the canonical owner of material, water, safety, competition, and receiver formulas. FrameData must call these resolvers rather than copy or reimplement them.

## Pipeline

```text
Dalashade_MaterialSignals
-> Dalashade_RawMaterialCandidates
-> Dalashade_GatedMaterialCandidates
-> Dalashade_MaterialCompetition
-> Dalashade_FinalMaterialMasks
-> Dalashade_MaterialResolve / Dalashade_WaterResolve / Dalashade_SafetyResolve
```

Raw candidates describe screen-space evidence. Gated candidates apply plugin-provided scene/material priors. Competition resolves ambiguous classes such as water versus sky or water versus constructed/aether surfaces. Final resolves expose stable fields to production and debug shaders.

## Dalashade_MaterialSignals

Signals are local image facts and helper measurements:

- `Uv`: current pixel coordinate.
- `Color`: sampled backbuffer color.
- `Luma`: luminance.
- `Saturation`: chroma intensity.
- `Chroma`: max channel minus min channel.
- `Hue*` style terms: color-family evidence used by raw candidates.
- `EdgeStrength`: local contrast/edge evidence.
- `DetailStrength`: high-frequency local detail evidence.
- `Smoothness`: inverse local detail/edge estimate.
- `Depth`, `DepthConfidence`, `DepthFarConfidence`, `DepthInvalidConfidence`: ReShade depth support and confidence.
- `UpperScreen`, `LowerScreen`, `CenterScreen`: screen-position hints for sky, horizon, and ground-like regions.

Signals are not material IDs. They are reusable measurements.

## Dalashade_RawMaterialCandidates

Raw candidates answer "does this pixel look like this material from image evidence alone?"

- `Foliage`: green organic detail and foliage-like texture.
- `WaterPlane`: broad smooth blue/cyan/teal water-like surface evidence.
- `SpecularGlint`: small bright glint/highlight evidence.
- `WaterSpecular`: water-like specular or smooth shine evidence.
- `SandDust`: warm sand, dust, desert, or beach terrain evidence.
- `SnowIce`: bright cold snow/ice evidence.
- `StoneRuins`: stone, ruin, cliff, masonry, or structured rock evidence.
- `MetalIndustrial`: hard neutral/cool industrial or metallic surface evidence.
- `CrystalAether`: luminous cyan/violet/magical crystal/aether evidence.
- `NeonGlass`: neon/glass/high-tech luminous surface evidence.
- `FireLavaHeat`: warm flame, lava, lamp, or heat source evidence.
- `SkyCloudFog`: broad sky, cloud, fog, or atmospheric smooth region evidence.
- `SkyOpen`, `CloudBright`, `FogAtmosphere`, `NightSky`: sub-evidence used by sky competition.
- `SkinProtection`: skin-like color and character protection evidence.
- `VoidDarkness`: deep black/void/night darkness evidence.
- `SurfaceHardTexture`: hard structured texture evidence used to reject false water/sky.

## Dalashade_GatedMaterialCandidates

Gated candidates apply scene priors from the generated preset. They answer "is the raw evidence plausible in this scene?"

Examples:

- Coastal/water priors allow water evidence to survive.
- Ancient/aether/metal priors allow Allagan cyan surfaces to become `CrystalAether`, `NeonGlass`, or `MetalIndustrial`.
- Snow and desert priors prevent bright/blue regions from being over-interpreted as water.
- Skin and sky priors protect high-risk false positives.

Gating must not prove a pixel by itself. Scene water context means water can exist, not that every smooth cyan pixel is water.

## Dalashade_MaterialCompetition

Competition resolves ambiguous candidates before final masks are exposed.

Core fields:

- `SkyScore`: total sky/cloud/fog plausibility.
- `WaterScore`: total water plausibility.
- `WaterSkyConflict`: overlap of sky and water plausibility.
- `WaterPixelConfidence`: likely actual water pixel. This is stricter than `WaterScore`.
- `SkyPixelConfidence`: likely actual sky/cloud/fog pixel.
- `WaterReceiverConfidence`: water suitable as reflection/wet receiver.
- `HorizonOnlyConfidence`: possible horizon/source-only water-sky boundary; not a receiver.
- `ReflectionReceiverConfidence`: wet/smooth/specular/water/metal/ice/glass receiver confidence.
- `AOReceiverConfidence`: hard/contact-likely receiver confidence.
- `StructureReceiverConfidence`: stable object/structure/hard-surface confidence.

Explainability fields:

- `WaterLocalProof`: local water evidence used by the competition formula.
- `StrongWaterLocalProof`: stronger water-plane-dominant proof.
- `ConstructedCyanReject`: cyan/blue constructed/aether/metal evidence that should not become water.
- `ConstructedWinsOverWater`: constructed evidence stronger than water proof.
- `ConstructedWinsOverSky`: constructed evidence stronger than sky evidence.
- `SkyDominance`: sky dominance used to suppress false water.
- `WaterProofBoost`: local proof boost that helps true water recover without using broad scene water score.

Important semantics:

- `WaterPixelConfidence != WaterReceiver != WaterSource`; the last name means reflection source-color eligibility, not "a water surface pixel."
- `HorizonOnlyConfidence` is source/context only, not receiver.
- `SkySource` can provide reflection color context for water, but must not receive reflection.
- `ReceiverConfidence` is legacy broad compatibility.
- `ReflectionReceiverConfidence` is specialized for reflective receivers.
- `StructureReceiverConfidence` is broader structural evidence and is not the same as `AOReceiverConfidence`.
- Constructed/aether rejection suppresses water classification, not reflection eligibility.

## Dalashade_FinalMaterialMasks

Final masks preserve the first-party material vocabulary:

- `Foliage`
- `WaterPlane`
- `SpecularGlint`
- `WaterSpecular`
- `SandDust`
- `SnowIce`
- `StoneRuins`
- `MetalIndustrial`
- `CrystalAether`
- `NeonGlass`
- `FireLavaHeat`
- `SkyCloudFog`
- `SkinProtection`
- `VoidDarkness`

These are shared material masks after raw evidence, priors, and competition. Production shaders may refine them for a role, but should start here.

## Dalashade_MaterialResolve

`Dalashade_MaterialResolve` exposes final material masks plus role-oriented helper fields:

- Material fields mirror final masks.
- `SurfaceSmoothness`: local smoothness suitable for reflection/bloom/sheen gating.
- `SurfaceHardness`: hard/structured surface confidence.
- `ReceiverConfidence`: legacy broad receiver compatibility.
- `LightSourceConfidence`: localized luminous/glint/source confidence.
- `ReflectionReceiverConfidence`: specialized reflective receiver confidence.
- `AOReceiverConfidence`: specialized AO/contact receiver confidence.
- `StructureReceiverConfidence`: stable structure/object confidence.

Use specialized receiver fields when available. Use `ReceiverConfidence` only as a broad compatibility fallback.

## Shared Role Helpers

`Dalashade_MaterialMasks.fxh` also exposes small helper functions for first-party shaders that need a consistent effect-role gate:

- `Dalashade_GetReflectionReceiver(...)`: combines `ReflectionReceiverConfidence` and strict water receiver evidence with sky, skin, and foliage-noise safety. Use this for reflective-surface permission, not for source color.
- `Dalashade_GetAOReceiver(...)`: uses `AOReceiverConfidence` with water/sky/skin/highlight rejection. Use this for AO/contact-like receivers.
- `Dalashade_GetStructureReceiver(...)`: uses `StructureReceiverConfidence` with sky/skin/water suppression. Use this for broad stable structure support.
- `Dalashade_GetWaterReceiverStrict(...)`: returns water receiver evidence after horizon, sky, and skin rejection. `WaterSource`, `SkySource`, and `HorizonOnlyConfidence` are intentionally excluded.
- `Dalashade_GetSkyReceiverReject(...)`: centralizes sky receiver rejection from safety, material, and water resolves.

These helpers are not new material IDs and should not replace shader-specific final shaping. They exist to keep source/receiver, AO/reflection, and legacy/specialized receiver semantics consistent.

## Dalashade_WaterResolve

`Dalashade_WaterResolve` separates water-like evidence into receiver, source, and rejection roles:

- `RawCyanWater`: cyan/teal/turquoise water-like color.
- `RawDeepWater`: darker ocean/deep water color.
- `ShallowWater`: shallow/coastal water confidence.
- `DeepWater`: deep/open water confidence.
- `WaterHorizon`: possible far water/horizon support.
- `WetShoreline`: water/sand boundary or wet shoreline evidence.
- `FoamOrEdge`: foam/wave/shoreline edge support.
- `WaterSurface`: broad coherent water-like surface.
- `WaterReceiver`: water allowed to receive broad reflection.
- `WaterSource`: water-related reflection source-color eligibility. This can include water, foam, and horizon-like source color, so it is not a receiver mask.
- `SkySource`: sky color allowed as source context. It is not a receiver mask.
- `SkyReject`: sky/cloud/fog receiver rejection.
- `SandReject`: warm dry sand/terrain rejection.
- `SkinReject`: skin/character rejection.
- `WaterCoherence`: cheap local coherence for broad water surfaces.
- `WaterPixelConfidence`: actual water-pixel confidence from material competition.
- `HorizonOnlyConfidence`: horizon/source-only confidence, not receiver.
- `WaterSkyConflict`: ambiguous water/sky conflict value.
- `Confidence`: overall water resolve confidence.

Use `WaterReceiver` for receiver masks. Use `WaterSource` and `SkySource` only for sampled reflection color/source context.

## Dalashade_SafetyResolve

`Dalashade_SafetyResolve` provides shared rejection and protection gates:

- `SkyReject`: sky/cloud/fog receiver rejection.
- `SkinReject`: skin/character protection.
- `HighlightProtect`: bright highlight protection.
- `BrightSandProtect`: bright sand/desert protection.
- `SnowProtect`: snow/ice highlight protection.
- `WaterAOReject`: water suppression for AO/dirty shading.
- `FoliageNoiseReject`: foliage shimmer/noise suppression.
- `UIDepthRisk`: UI/depth-risk estimate where feasible.
- `DepthConfidence`: depth confidence passed to consumers.

Production shaders should apply safety gates after role-specific receiver logic.

## Competition Debug Modes

MaterialDebug modes `55` through `65` expose material competition diagnostics.

| Mode | Label | Color meaning | Good result | Common failure |
| --- | --- | --- | --- | --- |
| 55 | Water/sky conflict | Red sky wins, cyan water wins, yellow/white unresolved conflict | Sky and water separate; horizon shows conflict | Full cyan sky or full red water. |
| 56 | Water pixel confidence | Cyan actual likely water pixels | Water visible, sky mostly black | Cyan on aether metal, sky, clouds, or dry sand. |
| 57 | Sky pixel confidence | Blue actual sky/cloud/fog | Sky visible, water mostly dark | Structures or water painted as sky. |
| 58 | Water receiver vs horizon | Cyan receiver water, blue horizon/source-only, red rejected sky | True water receiver distinct from horizon and sky | Horizon becomes cyan receiver or sky contaminates receiver. |
| 59 | Receiver confidence split | Cyan reflection, green structure, yellow/olive AO, faint gray legacy receiver | Receiver categories are distinct | Everything becomes one broad receiver. |
| 60 | Water local proof | Cyan local water proof | True water shows proof | Sky/aether/sand shows proof. |
| 61 | Strong water proof | Bright cyan strong water proof | Water-plane evidence is clear | Specular rails or cyan lights become water. |
| 62 | Constructed/aether reject | Magenta constructed cyan rejection | Aether/metal structures visible, water mostly low | Real water strongly rejected as constructed. |
| 63 | Sky dominance | Red/orange sky dominance | Sky/fog/clouds visible | Aether/metal structures become sky-dominant. |
| 64 | Water proof boost | Cyan-white local water proof boost | Helps true water only | Broad scene-wide cyan boost. |
| 65 | Competition internals overview | Red sky dominance, cyan strong water proof, magenta constructed reject, yellow conflict | Explains classification decisions | Overlapping colors hide the winning reason. |

These modes are diagnostic internals. They are not production material IDs.

## Do Not Do

- Do not add a separate water detector inside production shaders when `Dalashade_ResolveWater` can be improved.
- Do not use scene water context as pixel proof.
- Do not let reflection source-color fields such as `WaterSource`, `SkySource`, or `HorizonOnlyConfidence` become receiver masks.
- Do not make `ReceiverConfidence` the main gate for reflection, AO, or sharpening when specialized fields exist.
- Do not tune formulas reactively without first checking the competition debug modes.
