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

FrameData does not add a prepass, render target, motion vector path, temporal accumulation, or native XIV buffer access. It currently runs inline in the consuming shader and packages canonical resolver outputs into clearer role-based structs.

## Purpose

FrameData gives first-party shaders one canonical vocabulary for scene tags, material, water, safety, receiver, and optional surface/normal data. `Dalashade_WeatherAtmosphere.fx`, `Dalashade_AdaptiveGrade.fx`, `Dalashade_SmartSharpen.fx`, `Dalashade_AtmosphereBloom.fx`, `Dalashade_SurfaceReflection.fx`, and `Dalashade_SceneGI.fx` are the current production consumers. The migrations are intended to keep output visually stable while replacing shader-local material/water/safety resolver consumption with FrameData field names.

FrameData is not a promise to third-party shader authors yet. The contract can still change while Dalashade proves the names and field roles across its own shaders.

## Data Paths

FrameData is split into two paths:

- `Dalashade_ResolveFrameBaseData(...)`: material, water, safety, and receiver data.
- `Dalashade_ResolveFrameSurfaceData(...)`: optional surface/normal data from NormalField.
- `Dalashade_ResolveFrameSceneData(...)`: shared scene/tag normalization and derived scene lanes.

Shaders that only need material or safety data should call the base path. The surface path calls NormalField and should only be used when the effect actually needs inferred screen-space surface information.

## Settings

`Dalashade_FrameDataSettings` mirrors existing shader uniform concepts:

- material intent channels: `MaterialFoliage`, `MaterialWaterSpecular`, `MaterialWaterPlane`, `MaterialSpecularGlint`, `MaterialSandDust`, `MaterialSnowIce`, `MaterialStoneRuins`, `MaterialMetalIndustrial`, `MaterialCrystalAether`, `MaterialNeonGlass`, `MaterialFireLavaHeat`, `MaterialSkyCloudFog`, `MaterialSkinProtection`, `MaterialVoidDarkness`
- water/coastal context channels: `WaterContext`, `CoastalContext`, `OpenOceanContext`, `ShallowWaterContext`, `WetSurfaceContext`
- safety assist: `HighlightProtection`
- depth assist: `DepthAssistEnabled`, `DepthAssistStrength`, `DepthAssistConfidenceFloor`
- NormalField controls: `NormalFieldEnabled`, `NormalFieldStrength`, `NormalDepthStrength`, `NormalDetailStrength`, `NormalMaterialInfluence`, `NormalWaterSuppression`, `NormalSkinSuppression`, `NormalSkySuppression`

`Dalashade_FrameData_DefaultSettings()` returns conservative zero-output defaults, with NormalField disabled.

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

NormalField is inferred screen-space data. It is not true FFXIV normals, material IDs, G-buffer access, or texture normal maps.

| Field | Role | Meaning |
| --- | --- | --- |
| `Normal` | Surface/confidence | Encoded as a direction vector; inferred from depth/detail. |
| `NormalConfidence` | Confidence | Stability confidence for the inferred normal. |
| `OrientationConfidence` | Confidence | Orientation stability confidence from NormalField. |
| `DepthConfidence` | Confidence | Depth-derived normal confidence. |
| `DetailStrength` | Confidence | Detail-derived surface support from NormalField. |
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
| 9 | Inline resolver parity | Compares FrameData wrapper fields against direct canonical resolver calls. |

The debug shader is generated-preset aware but not auto-enabled. Base presets are not mutated.

## Diagnostics And Reports

FrameData diagnostics are currently report-only:

- Compatibility reports include `FrameDataMode: Inline`, `FrameDataPrepass: NotImplemented`, and production shader source scans. WeatherAtmosphere, AdaptiveGrade, SmartSharpen, AtmosphereBloom, SurfaceReflection, and SceneGI are the current production consumers; no prepass or render target exists.
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
