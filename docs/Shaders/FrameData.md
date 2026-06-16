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

FrameData gives first-party shaders one canonical vocabulary for material, water, safety, receiver, and optional surface/normal data. This first pass is foundational only. Existing production shaders are not migrated yet, and their output should not change.

FrameData is not a promise to third-party shader authors yet. The contract can still change while Dalashade proves the names and field roles across its own shaders.

## Data Paths

FrameData is split into two paths:

- `Dalashade_ResolveFrameBaseData(...)`: material, water, safety, and receiver data.
- `Dalashade_ResolveFrameSurfaceData(...)`: optional surface/normal data from NormalField.

Shaders that only need material or safety data should call the base path. The surface path calls NormalField and should only be used when the effect actually needs inferred screen-space surface information.

## Settings

`Dalashade_FrameDataSettings` mirrors existing shader uniform concepts:

- material intent channels: `MaterialFoliage`, `MaterialWaterSpecular`, `MaterialWaterPlane`, `MaterialSpecularGlint`, `MaterialSandDust`, `MaterialSnowIce`, `MaterialStoneRuins`, `MaterialMetalIndustrial`, `MaterialCrystalAether`, `MaterialNeonGlass`, `MaterialFireLavaHeat`, `MaterialSkyCloudFog`, `MaterialSkinProtection`, `MaterialVoidDarkness`
- water/coastal context channels: `WaterContext`, `CoastalContext`, `OpenOceanContext`, `ShallowWaterContext`, `WetSurfaceContext`
- safety assist: `HighlightProtection`
- depth assist: `DepthAssistEnabled`, `DepthAssistStrength`, `DepthAssistConfidenceFloor`
- NormalField controls: `NormalFieldEnabled`, `NormalFieldStrength`, `NormalDepthStrength`, `NormalDetailStrength`, `NormalMaterialInfluence`, `NormalWaterSuppression`, `NormalSkinSuppression`, `NormalSkySuppression`

`Dalashade_FrameData_DefaultSettings()` returns conservative zero-output defaults, with NormalField disabled.

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
| `SpecularGlint` | Material/source | Small specular glint support from MaterialMasks. |
| `HorizonOnly` | Source/context | Horizon-only support. Not water receiver proof. |
| `WaterSkyConflict` | Safety/confidence | Ambiguity between water and sky interpretation. |
| `Confidence` | Confidence | Overall water resolve confidence. |

### Dalashade_FrameMaterial

| Field | Role | Meaning |
| --- | --- | --- |
| `Foliage` | Material | Foliage/organic green confidence. |
| `SandDust` | Material | Sand, dust, desert, beach confidence. |
| `SnowIce` | Material | Snow, ice, bright cold surface confidence. |
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

### Dalashade_FrameSurfaceData

NormalField is inferred screen-space data. It is not true FFXIV normals, material IDs, G-buffer access, or texture normal maps.

| Field | Role | Meaning |
| --- | --- | --- |
| `Normal` | Surface/confidence | Encoded as a direction vector; inferred from depth/detail. |
| `NormalConfidence` | Confidence | Stability confidence for the inferred normal. |
| `DepthConfidence` | Confidence | Depth-derived normal confidence. |
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

## Migration Guidance

When production shaders start using FrameData, migrate one shader at a time. The expected order is:

1. WeatherAtmosphere air-layer identity.
2. AtmosphereBloom source-class response.
3. SmartSharpen safety/receiver harmonization.
4. AdaptiveGrade role-name cleanup if needed.
5. SurfaceReflection and SceneGI only after receiver validation.

Each migration should keep before/after output equivalent unless the pass explicitly targets visuals.
