# Dalashade FrameData Contract

Status:

```text
Internal / experimental
Not yet a public third-party API
Subject to field-name changes until first-party migration proves stable
```

`shaders/Dalashade_FrameData.fxh` is a shared internal wrapper over the existing Dalashade resolver includes:

- `Dalashade_MaterialMasks.fxh`
- `Dalashade_NormalField.fxh`
- `Dalashade_Dalapad.fxh`

FrameData does not add a prepass, render target, motion vector path, temporal accumulation, or native XIV buffer access. It currently runs inline in the consuming shader and packages canonical resolver outputs into clearer role-based structs.

For the short shader-author contract, see [../ShaderContractQuickReference.md](../ShaderContractQuickReference.md). FrameData is the first-party shader-facing surface; production shaders should not bypass it to sample raw Dalapad candidates or duplicate shared MaterialMasks/NormalField classifiers.

## Purpose

FrameData gives first-party shaders one canonical vocabulary for scene tags, material, water, safety, receiver, and optional surface/normal data. `Dalashade_WeatherAtmosphere.fx`, `Dalashade_AdaptiveGrade.fx`, `Dalashade_SmartSharpen.fx`, `Dalashade_AtmosphereBloom.fx`, `Dalashade_SceneGI.fx`, `Dalashade_ContactTone.fx`, and `Dalashade_SurfaceReflection.fx` are the current production consumers. The migrations are intended to keep output visually stable while replacing shader-local material/water/safety resolver consumption with FrameData field names.

FrameData is not a promise to third-party shader authors yet. The contract can still change while Dalashade proves the names and field roles across its own shaders.

## Data Paths

FrameData is split into two paths:

- `Dalashade_ResolveFrameBaseData(...)`: material, water, safety, and receiver data.
- `Dalashade_ResolveFrameSurfaceData(...)`: optional surface/normal data from NormalField plus gated Dalapad pinned surface data when available.
- `Dalashade_ResolveFrameSceneData(...)`: shared scene/tag normalization and derived scene lanes.

Shaders that only need material or safety data should call the base path. The surface path calls NormalField and may sample Dalapad pinned resources, so it should only be used when the effect actually needs surface information.

When first-party performance tiers lower NormalField strength, detail, material influence, or shader-specific sample budgets, FrameData remains the gatekeeper for surface data. Quality keeps existing surface values. Balanced reduces optional inferred surface influence. Performance reduces inferred NormalField influence further so shaders can prefer authorized Dalapad pinned normals/albedo-like evidence when those gates are open. If NormalField and Dalapad surface data are both disabled, the surface path returns default/zero influence rather than fake blank data.

Dalapad status-file and control-pipe IPC do not feed FrameData directly. Only shader-visible semantic resources behind generated-preset and shader-local gates may contribute, and missing/stale/disabled Dalapad data returns zero Dalapad confidence.

## Settings

`Dalashade_FrameDataSettings` mirrors existing shader uniform concepts:

- material intent channels: `MaterialFoliage`, `MaterialWaterSpecular`, `MaterialWaterPlane`, `MaterialSpecularGlint`, `MaterialSandDust`, `MaterialSnowIce`, `MaterialStoneRuins`, `MaterialMetalIndustrial`, `MaterialCrystalAether`, `MaterialNeonGlass`, `MaterialFireLavaHeat`, `MaterialSkyCloudFog`, `MaterialSkinProtection`, `MaterialVoidDarkness`
- water/coastal context channels: `WaterContext`, `CoastalContext`, `OpenOceanContext`, `ShallowWaterContext`, `WetSurfaceContext`
- safety assist: `HighlightProtection`
- depth assist: `DepthAssistEnabled`, `DepthAssistStrength`, `DepthAssistConfidenceFloor`
- NormalField controls: `NormalFieldEnabled`, `NormalFieldStrength`, `NormalDepthStrength`, `NormalDetailStrength`, `NormalMaterialInfluence`, `NormalWaterSuppression`, `NormalSkinSuppression`, `NormalSkySuppression`
- Dalapad surface controls: `DalapadSurfaceDataEnabled`, `DalapadSurfaceDataStrength`

`Dalashade_FrameData_DefaultSettings()` returns conservative zero-output defaults, with NormalField and Dalapad surface data disabled unless generated-preset uniforms opt in.

`Dalashade_FrameSceneSettings` mirrors generated SceneIntent and first-party scene tag concepts without replacing shader-owned effect sliders:

- broad scene tags: `Readability`, `Atmosphere`, `HighlightProtection`, `ShadowProtection`, `Haze`, `Wetness`, `Cold`, `Heat`, `MagicGlow`, `NeonGlow`, `FoliageDensity`, `IndustrialHardness`, `CosmicMood`, `CinematicPermission`, `CombatPressure`
- night tags: `Night`, `Moonlight`, `ArtificialLight`, `AmbientDarkness`, `NightAtmosphere`
- day tags: `Daylight`, `Sunlight`, `OpenSkyLight`, `SurfaceHeat`, `DayAtmosphere`, `DayReflection`, `DayHighlightPressure`
- first-party mode tag: `StandaloneStrength`

`Dalashade_ResolveFrameSceneData(...)` clamps those tags and exposes derived helper lanes such as `GameplayDampen`, `ReadabilityDampen`, `ReflectionDampen`, `StandaloneSafe`, `DayOpenAir`, `NightLocalLight`, `WetAir`, `HeatAir`, `ColdAir`, `AetherTech`, `ForestCanopy`, `Industrial`, and `InteriorMood`. These lanes are shared vocabulary, not material proof or receiver proof.

## Structs

All fields are normalized confidence values in the `0..1` range unless noted.

### Dalashade_FrameSafety

| Field | Role | Meaning |
| --- | --- | --- |
| `SkyReject` | Safety | Avoid material effects, sharpening, GI, reflection, bloom, and haze where appropriate. |
| `SkinReject` | Safety | Protect skin/characters from tint, bloom, sharpening, GI, and reflection. |
| `HighlightProtect` | Safety | Protect bright highlights from clipping or aggressive shaping. |
| `BrightSandProtect` | Safety | Protect bright sand/beach/desert highlights. |
| `SnowProtect` | Safety | Protect snow/ice whites and bright cold highlights. |
| `FoliageNoiseReject` | Safety | Reduce shimmer/noise-sensitive effects on foliage. |
| `UIDepthRisk` | Safety/confidence | Warns that depth may be invalid or UI-like for the pixel. |
| `DepthConfidence` | Confidence | Resolver confidence in depth validity for this pixel. |

### Dalashade_FrameWater

| Field | Role | Meaning |
| --- | --- | --- |
| `WaterPixelConfidence` | Material/confidence | Strict likely-water pixel confidence. |
| `WaterReceiver` | Receiver | Water-local receiver support for water effects. |
| `WaterSource` | Source | Water-related source-color/context support. Not receiver proof. |
| `SkySource` | Source | Sky source-color/context support. Not receiver proof. |
| `WetShoreline` | Material/source | Shoreline or wet boundary support. |
| `FoamOrEdge` | Material/source | Water foam, edge, or shoreline transition support. |
| `WaterSurface` | Material/receiver | Broad water-surface support from the canonical water resolve. |
| `ShallowWater` | Material/receiver | Shallow-water support from the canonical water resolve. |
| `WaterHorizon` | Source/context | Water-horizon support. Not water receiver proof. |
| `SpecularGlint` | Material/source | Small specular glint support from MaterialMasks. |
| `HorizonOnly` | Source/context | Horizon-only support. Not water receiver proof. |
| `SandReject` | Safety/confidence | Sand/warm terrain rejection used to avoid water false positives. |
| `WaterSkyConflict` | Safety/confidence | Ambiguity between water and sky interpretation. |
| `Confidence` | Confidence | Overall water resolve confidence. |

### Dalashade_FrameMaterial

| Field | Role | Meaning |
| --- | --- | --- |
| `Foliage` | Material | Foliage/organic green confidence. |
| `WaterSpecular` | Material/confidence | Canonical water/specular confidence from MaterialMasks. |
| `SandDust` | Material | Sand, dust, desert, beach confidence. |
| `SnowIce` | Material | Snow, ice, bright cold surface confidence. |
| `WaterPlane` | Material/confidence | Canonical material water-plane confidence from MaterialMasks. |
| `StoneRuins` | Material | Stone, ruin, masonry, rock confidence. |
| `MetalIndustrial` | Material | Metal, industrial, hard cool surface confidence. |
| `CrystalAether` | Material/source | Crystal, aether, magical cyan/violet confidence. |
| `NeonGlass` | Material/source | Neon, glass, high-tech luminous confidence. |
| `FireLavaHeat` | Material/source | Fire, lava, warm lamp, heat confidence. |
| `SkyCloudFog` | Material/safety | Sky, cloud, fog, broad atmospheric confidence. |
| `SkinProtection` | Safety | Skin/character protection confidence. |
| `VoidDarkness` | Material/source | Deep void/darkness confidence. |
| `SurfaceSmoothness` | Confidence | Local smoothness estimate from MaterialMasks. |
| `SurfaceHardness` | Confidence | Hard structured texture estimate from MaterialMasks. |

### Dalashade_FrameReceivers

| Field | Role | Meaning |
| --- | --- | --- |
| `BroadReceiver` | Receiver | Legacy broad compatibility receiver evidence. |
| `ReflectionReceiver` | Receiver | Reflection receiver support, not source brightness. |
| `AOReceiver` | Receiver | AO/contact receiver support, not reflection support. |
| `StructureReceiver` | Receiver | Stable structure support, not AO by itself. |
| `LightSourceConfidence` | Source | Light/glow/glint source support, not receiver support. |

### Dalashade_FrameBaseData

ReShade's compiler does not support nested struct members, so the runtime contract uses a flat aggregate with role prefixes. The role structs above document the logical groups and naming intent.

| Field | Role | Meaning |
| --- | --- | --- |
| `Safety*` | Safety | Flat safety fields matching `Dalashade_FrameSafety`. |
| `Water*` | Material/source/receiver | Flat water fields matching `Dalashade_FrameWater`. |
| `Material*` | Material/safety/source | Flat material fields matching `Dalashade_FrameMaterial`. |
| `Receiver*` | Receiver | Flat receiver fields matching `Dalashade_FrameReceivers`. |
| `SourceLightConfidence` | Source | Flat light/glow/glint source support. |

### Dalashade_FrameSceneData

Frame scene data is the shared tag vocabulary used by first-party shaders to interpret generated scene variables consistently.

| Field | Role | Meaning |
| --- | --- | --- |
| `Readability`, `CombatPressure` | Safety/tag | Gameplay pressure tags used to dampen heavy visual work. |
| `Atmosphere`, `Haze`, `Wetness`, `Cold`, `Heat` | Scene tag | Broad air/weather/environment support. |
| `MagicGlow`, `NeonGlow`, `CosmicMood` | Scene/source tag | Aether, high-tech, or cosmic source support; not receiver proof. |
| `FoliageDensity`, `IndustrialHardness` | Scene/material tag | Context support for foliage/industrial scenes; not material proof by itself. |
| `CinematicPermission` | Scene tag | Permission to allow more visible non-gameplay shaping. |
| `Night*`, `Day*` | Scene tag | Shared day/night/open-air/light context. |
| `StandaloneStrength` | Mode tag | First-party mode strength as written by the generated preset. |
| `GameplayDampen`, `ReadabilityDampen`, `ReflectionDampen` | Safety/derived | Shared combat/readability dampening lanes. |
| `StandaloneSafe` | Safety/derived | Standalone strength after common gameplay/readability dampening. |
| `DayOpenAir`, `NightLocalLight`, `WetAir`, `HeatAir`, `ColdAir`, `AetherTech`, `ForestCanopy`, `Industrial`, `InteriorMood` | Scene/derived | Shared scene identity lanes for first-party shader coordination. |

### Dalashade_FrameSurfaceData

Frame surface data can combine two optional sources:

- NormalField, which is inferred screen-space data from backbuffer/depth/material safety.
- Dalapad pinned surface data, which is addon-provided semantic render-layer evidence exposed through `Dalashade_Dalapad.fxh`.

Neither path is a public FFXIV G-buffer contract, material ID path, motion-vector path, or guaranteed world-space normal source. Dalapad data may be treated as stronger normal-like surface evidence when the global Dalapad shader-additions gate, the surface-data gate, availability flags, dimensions, and confidence checks all agree.

| Field | Role | Meaning |
| --- | --- | --- |
| `Normal` | Surface/confidence | Encoded as a direction vector; inferred from depth, detail, and safe texture-relief hints. |
| `NormalFieldNormal` | Surface/confidence | NormalField-only normal before any Dalapad merge. |
| `DalapadNormal` | Surface/confidence | Gated Dalapad pinned normal-like direction, or neutral fallback when unavailable. |
| `NormalConfidence` | Confidence | Stability confidence for the inferred normal. |
| `NormalFieldConfidence` | Confidence | NormalField-only confidence before any Dalapad merge. |
| `DalapadConfidence` | Confidence | Dalapad pinned normal confidence after global, feature, availability, strength, and evidence gates. Dimensions are diagnostic metadata. |
| `DalapadInfluence` | Confidence | Final Dalapad influence applied to the merged surface fields. Zero means no authorized Dalapad data reached the shader. |
| `DalapadPresence` | Confidence | Presence/validity support from the pinned resource sample. |
| `DalapadChroma` | Confidence | Chroma/variation support from the pinned resource sample. |
| `DalapadNeighborDelta` | Confidence | Local neighbor-change support from the pinned resource sample. |
| `DalapadFlatSupport` | Confidence | Stable flat-surface support from the pinned resource sample. |
| `DalapadStructureSupport` | Confidence | Edge/structure support from the pinned resource sample. |
| `SurfaceDataInfluence` | Confidence | Combined optional surface influence that production shaders can use without caring whether the source was NormalField, Dalapad, or both. |
| `OrientationConfidence` | Confidence | Orientation stability confidence from NormalField. |
| `DepthConfidence` | Confidence | Depth-derived normal confidence. |
| `DetailStrength` | Confidence | Detail-derived surface support from NormalField, including safe texture-relief contribution for existing consumers. |
| `TextureReliefStrength` | Confidence | Explicit highlight-ridge/dark-groove relief support after broad-gradient, material, and safety gates. |
| `TextureGrooveLine` | Confidence | Direction-coherent dark groove/seam support for tile gaps, panel lines, cracks, and engraved material boundaries. |
| `TextureCurvatureRidge` | Confidence | Curvature-based raised ridge evidence from the texture-relief pass. |
| `TextureCurvatureValley` | Confidence | Curvature-based valley/groove/indent evidence from the texture-relief pass. |
| `TextureCoherence` | Confidence | Structure-coherence evidence used to separate organized seams from random speckle. |
| `TextureCompositeConfidence` | Confidence | Confidence in the composite screen-space relief-height and normal estimate. |
| `TextureReliefSafety` | Safety/confidence | Final gate showing whether texture-relief evidence survived sky, water, skin, UI, foliage, highlight, emissive, and hard-edge suppression. |
| `EdgeDiscontinuity` | Safety/confidence | Local discontinuity risk. |
| `GroundCandidate` | Surface/confidence | Support-plane candidate, not literal ground ID. |
| `StructureCandidate` | Surface/confidence | Broad structure/object confidence. |
| `WallCandidate` | Surface/confidence | Strict wall/vertical candidate. |
| `ReflectionReceiverSupport` | Receiver | NormalField reflection receiver support. |
| `AOReceiverSupport` | Receiver | NormalField AO receiver support. |

## Invariants

- `WaterSource` is source context only. It must not authorize reflection receivers.
- `WaterReceiver` can support water-local effects.
- `HorizonOnly` is source/context only. It must not become water receiver proof.
- `SkyReject` means avoid material effects, sharpening, GI, reflection, bloom, and haze where appropriate.
- `SkinReject` means protect from tint, bloom, sharpening, GI, and reflection.
- `ReflectionReceiver` is receiver support, not source brightness.
- `AOReceiver` is AO receiver support, not reflection support.
- `StructureReceiver` is structure support, not AO by itself.
- `LightSourceConfidence` is source support, not receiver support.
- `NormalConfidence` is stability evidence, not material identity.
- `SurfaceDataInfluence` is the safe shader-facing surface-data amount. First-party shaders should prefer it over direct Dalapad or NormalField gate duplication.
- `DalapadConfidence` and `DalapadInfluence` must remain zero when global Dalapad shader additions are disabled, Dalapad surface data is disabled, the pinned resource is unavailable, strength is zero, or evidence confidence fails. Pinned dimensions should be reported for health checks, but stale dimensions must not by themselves shut off an available semantic texture.
- Dalapad pinned normals can strongly influence surface-aware effects after gates pass, but they must not override sky, skin, water, UI/depth, highlight, or material safety rejects.
- `TextureReliefStrength` is local surface relief evidence, not a recovered game normal map or material truth.
- `TextureGrooveLine` is seam/groove evidence only. It can support contact, AO, or clarity decisions, but it must not become material identity by itself.
- `TextureReliefSafety` is permission/confidence for relief use, not proof that a pixel has valid game texture normal data.
- `TextureCurvatureRidge`, `TextureCurvatureValley`, and `TextureCoherence` are diagnostic/support signals from the same screen-space relief hypothesis. They are not new material channels.
- `CrystalAether` and `NeonGlass` must not be collapsed into water because they are cyan/blue.
- `BroadReceiver` is legacy/broad compatibility evidence. Production shaders should prefer role-specific receivers.

## FrameDataDebug

`shaders/Dalashade_FrameDataDebug.fx` verifies the contract. It does not replace `MaterialDebug` or `NormalDebug`.

Modes:

| Mode | View | Purpose |
| --- | --- | --- |
| 0 | Off/pass-through | No visual change if the technique is accidentally enabled. |
| 1 | Safety pack | Sky, skin, and highlight protection. |
| 2 | Water pack | Water pixel, receiver, shoreline/glint support. |
| 3 | Material pack | Terrain/hard materials, foliage/snow, aether/neon/void. |
| 4 | Receiver pack | Reflection, AO, and structure receivers. |
| 5 | Surface/normal pack | Optional NormalField surface view. |
| 6 | Source-vs-receiver | Source support, reflection/water receiver, AO/structure receiver. |
| 7 | Water-vs-sky conflict | Water/sky ambiguity and sky rejection. |
| 8 | Aether/metal/water ambiguity | Helps verify cyan aether/metal is not collapsed into water. |
| 9 | Dalapad surface normal | Shows the gated Dalapad surface normal only when authorized data exists. |
| 10 | Dalapad surface confidence | Shows Dalapad influence, flat support, and structure support. |
| 11 | Inline resolver parity | Compares FrameData wrapper fields against direct canonical resolver calls. |

The debug shader is generated-preset aware but not auto-enabled. Base presets are not mutated.

## Diagnostics And Reports

FrameData diagnostics are currently report-only:

- Compatibility reports include `FrameDataMode: Inline`, `FrameDataPrepass: NotImplemented`, and production shader source scans. WeatherAtmosphere, AdaptiveGrade, SmartSharpen, AtmosphereBloom, SceneGI, ContactTone, and SurfaceReflection are the current production consumers; no FrameData prepass exists.
- Debug bundles include `frame-data-diagnostics.json` with installed FrameData file presence, FrameDataDebug preset/technique state, FrameDataDebug debug variables, and production shader source scans.
- Debug bundles include `first-party-depth-assist.json` so depth-assist opt-in state and written first-party depth-assist variables can be audited beside FrameData state.

`Dalashade_FrameDataDebug.fx` remains a manual debug shader. Generated-preset support may create the section and safe debug defaults, but it must not activate the technique. In-game ReShade compile validation is still required for `.fx` files because C# report/export checks only inspect files and preset text.

## Migration Guidance

When production shaders start using FrameData, migrate one shader at a time. The expected order is:

1. WeatherAtmosphere air-layer identity. Complete for the inline FrameData consumer pass.
2. AdaptiveGrade tonal/material protection. Complete for the inline base/surface-data consumer pass.
3. SmartSharpen safety/receiver harmonization. Complete for the inline base/surface-data consumer pass.
4. AtmosphereBloom source-class response. Complete for the inline base/surface-data consumer pass.
5. SurfaceReflection receiver migration. Complete for the inline base/surface-data consumer pass.
6. SceneGI receiver migration. Complete for the inline base/surface-data consumer pass.

Each migration should keep before/after output equivalent unless the pass explicitly targets visuals.

## Future Prepass Direction

The likely next architectural step is a FrameData/Dalapad prepass, but it should not be added until the inline contract has been validated in-game. A prepass would be useful when multiple first-party shaders need the same stable role data or when an effect needs history:

- SceneGI needs stable material, receiver, source, normal, and edge evidence for wider diffuse gathers and temporal smoothing.
- SurfaceReflection needs stable water receiver, reflection receiver, edge, horizon/source-only, and water-vs-sky conflict data for projected reflections.
- WeatherAtmosphere and AtmosphereBloom could reuse source/safety lanes without rerunning every resolver inline.

The first prepass should be conservative: one role/confidence buffer plus one optional surface buffer. It should not promise native FFXIV G-buffer access, motion vectors, or true world-space data. Later history buffers can be considered only after the role buffers prove stable and debug views show near-parity with inline FrameData.
