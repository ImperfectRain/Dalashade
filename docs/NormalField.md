# Dalashade NormalField

NormalField is an optional screen-space inferred surface-normal layer for first-party Dalashade shader work.

It is built from ReShade depth, image gradients, shared material masks, water masks, and safety masks. It is useful for future GI, reflection, sharpening, weather, combat clarity, and light hierarchy improvements, but it is disabled by default and diagnostic-first.

NormalField is not true FFXIV material normals, not G-buffer access, and not actual texture normal maps. It is a conservative inferred surface field that should fail safely when depth or screen-space evidence is unreliable.

For the shader include contract and field semantics, see [Shaders/NormalField.md](Shaders/NormalField.md).

## Configuration

| Setting | Meaning |
| --- | --- |
| `EnableNormalField` | Enables NormalField diagnostics and allows the system to participate in future shader mapping. When false, NormalField produces no visual changes and writes no NormalField shader variables. |
| `EnableNormalFieldDiagnostics` | Allows reports and debug bundle exports to include NormalField status. |
| `EnableNormalFieldShaderMapping` | Allows Dalashade to write NormalField uniforms into known first-party shader sections. When false, NormalField is diagnostics-only. |
| `NormalFieldStrength` | Global scale for inferred normal influence. A value of `0` disables shader-side normal influence even if mapping is enabled. |
| `NormalFieldDepthStrength` | Weight for coarse normals reconstructed from ReShade depth. Requires usable depth to help. |
| `NormalFieldDetailStrength` | Weight for detail normals inferred from image luma/color gradients. Keep low to avoid embossed shimmer. |
| `NormalFieldMaterialInfluence` | Amount of material and water masks used to decide where inferred normals are trusted. |
| `NormalFieldWaterSuppression` | Suppresses noisy detail normals on water-like surfaces. |
| `NormalFieldSkinSuppression` | Suppresses dirty normal/detail influence on likely skin-like areas. |
| `NormalFieldSkySuppression` | Suppresses normal/detail influence on sky, fog, and smooth atmospheric areas. |
| `NormalFieldDebugMode` | Optional generated debug mode value for NormalDebug when shader mapping is enabled. |
| `NormalFieldDebugBoost` | Debug-only amplification for NormalDebug views. It does not change production output. |

Default behavior is neutral: `EnableNormalField` is false and production shader output is unchanged. If NormalField is enabled but `EnableNormalFieldShaderMapping` is false, reports and debug bundles can describe the settings, but generated presets do not receive NormalField uniforms.

`Dalashade_NormalDebug.fx` is optional. Missing NormalDebug files should not be treated as an error unless the user is trying to inspect NormalField. Invalid, missing, flat, or unreliable depth should fall back to a neutral forward normal with low confidence.

## Shader Files

| File | Responsibility |
| --- | --- |
| `shaders/Dalashade_NormalField.fxh` | Shared include that resolves depth normals, detail normals, composite texture-relief normals, combined normal confidence, receiver masks, and debug colors. It creates no visible effect by itself. |
| `shaders/Dalashade_NormalDebug.fx` | Optional first-party debug visualizer for NormalField. It is disabled/pass-through by default and must be manually enabled in ReShade for inspection. |

## Debug Modes

`Dalashade_NormalDebug.fx` exposes shader-owned debug modes:

| Mode | View |
| --- | --- |
| `0` | Normal image / pass-through |
| `1` | Depth normal RGB |
| `2` | Detail normal RGB |
| `3` | Combined normal RGB |
| `4` | Ground/plane candidate |
| `5` | Wall-plane candidate |
| `6` | Structure candidate |
| `7` | Detail eligibility |
| `8` | Normal confidence |
| `9` | Shading receiver |
| `10` | Reflection receiver |
| `11` | AO receiver |
| `12` | Safety/relief suppression |
| `13` | Legacy relief normal RGB |
| `14` | Texture ridge/groove/relief |
| `15` | Texture groove line only |
| `16` | Curvature ridge |
| `17` | Curvature valley/groove |
| `18` | Structure coherence |
| `19` | Composite relief height |
| `20` | Composite relief normal RGB, current composite height normal |

Normal RGB views encode normals as `normal * 0.5 + 0.5`. Mask views use grayscale or simple false color depending on the helper output. The texture relief views are still inferred screen-space diagnostics: bright local high-pass detail can read as raised ridges, curvature can mark likely raised/indented structure, and direction-coherent dark valleys can read as grooves/seams. The current height compiler keeps uncertain areas near neutral, requires structure-scale support before texture detail contributes strongly, treats raw high-pass detail as a capped compatibility hint, smooths weak height evidence before deriving normals, and depresses coherent grooves more strongly than it raises ridges. Broad lighting gradients, sky/cosmic fields, skin, water, foliage shimmer, UI/depth risk, bright highlights, emissive/aether bloom, isolated speckle, unsafe hard edges, and unsafe neighbor grooves suppress the result.

When relief looks noisy, inspect modes `16` through `19` before judging mode `20`: curvature ridge/valley should find organized local maxima/minima, structure coherence should be strongest on continuous seams or repeated hard-surface patterns, and composite relief height should stay near neutral gray where the system is uncertain.

`Ground/plane candidate` and `Wall-plane candidate` are compatibility views for the existing `GroundFacing` and `WallFacing` fields, but they are now backed by explicit plane candidates. `Structure candidate` is the broader screen-space geometry/edge/hard-surface confidence mask. Future production shaders should generally prefer `StructureCandidate` for AO or broad shading support and use `WallPlaneCandidate` only when vertical-orientation evidence matters.

## Recommended Debugging Order

For debugging:

1. Place `Dalashade_NormalDebug` near the bottom of the ReShade order.
2. Place `Dalashade_MaterialDebug` after it when comparing NormalField output against material masks.
3. Disable heavy bloom, sharpening, or other strong post-process effects if masks are hard to read.

For normal gameplay, keep `Dalashade_NormalDebug` disabled.

## Future Production Consumers

NormalField is now available to first-party shaders through the optional FrameData surface path. Existing consumers should keep using `Normal`, `DetailStrength`, `StructureCandidate`, `EdgeDiscontinuity`, and receiver support fields as secondary evidence behind material/water/safety gates. Future consumers can use `TextureReliefStrength` when they need to distinguish relief-style detail from general detail, `TextureGrooveLine` when they specifically need tile gaps, panel seams, cracks, or engraved material boundaries, `TextureReliefSafety` when they need to know whether relief data was allowed, and the curvature/coherence fields when they need to debug or softly gate relief use. These fields are support/confidence signals, not material identity.

Planned follow-up integration order for stronger use:

1. ContactTone
2. SceneGI
3. SmartSharpen
4. AtmosphereBloom / WeatherAtmosphere
5. SurfaceReflection
6. WeatherParticles
7. LightHierarchy
8. CombatClarity

Each production integration should remain opt-in, bounded, and diagnosable. NormalField should improve existing first-party effects; it should not become a global normal-map stylizer.

## Test Plan

Use these scenes for a first calibration pass:

| Scene family | What to inspect |
| --- | --- |
| Costa/coastal day | Water surface smoothness, beach highlight safety, sky rejection, shallow water receiver behavior. |
| Costa/coastal night | Dark water and lamp/glint receiver behavior without lifting the whole scene. |
| Rak'tika/jungle | Foliage detail suppression, trunk/wall separation, sky/fog safety through canopy gaps. |
| Desert/heat zone | Sand/stone hard-surface response without orange shimmer or sky confusion. |
| Snow/cold zone | Snow highlight safety, cold hard-surface normals, cloud/sky rejection. |
| High-tech/Solution Nine | Metal/glass receiver masks and neon/aether surface separation. |
| Allagan/cosmic/metal zone | Hard surface normals, crystal/aether detail restraint, sky rejection. |
| Interior/dungeon | Depth normal stability, wall/floor masks, reduced open-sky assumptions. |
| Combat scene | Stability under effects clutter and readable receiver masks. |
| UI-heavy scene | UI should not dominate normal/detail confidence or receiver masks. |
| Cosmic/aether sky scene | Nebula, sky texture, bloom rings, and bright emissive fields should not become hard-surface relief normals. |

### Depth Normal RGB

Expected:

- Broad geometry orientation is visible when depth works.
- The view does not explode into noise.
- Invalid, missing, or flat depth produces neutral-looking normals and low confidence.

### Detail Normal RGB

Expected:

- Hard surfaces show restrained texture/detail.
- Sky, fog, skin, and water are suppressed.
- Foliage does not shimmer aggressively or become embossed.

### Combined Normal RGB

Expected:

- Output is stable during camera movement.
- There is no strong relief-map or embossed look.
- Material and safety suppression visibly restrain risky regions.

### Ground/Plane Candidate

Expected:

- Floors, terrain, and broad water surfaces are mostly visible.
- Walls and vertical objects are reduced.

### Wall-Plane Candidate

Expected:

- Walls, trunks, structures, and vertical surfaces are visible when depth/normal evidence supports them.
- Flat ground is reduced.
- This view may be dimmer than the structure candidate because it is stricter about physical-ish orientation.

### Structure Candidate

Expected:

- Railings, posts, hut/boat/building structures, cliffs, silhouettes, and hard-surface detail are visible.
- This view may look like a geometry/edge map. That is intentional: it is structural evidence, not a true wall-orientation mask.
- Sky, water, skin, and high-risk highlights are reduced.

### Reflection Receiver

Expected:

- Water, wet surfaces, specular surfaces, metal, and glass are plausible.
- Sky, skin, and UI are suppressed.

### AO Receiver

Expected:

- Hard surfaces and terrain are plausible.
- Sky, fog, water, and skin are reduced.

### Safety Suppression

Expected:

- Sky, fog, skin, water, and strong highlights show appropriate suppression.
- Suppression should explain why NormalField is quiet in risky areas.

## Acceptance Checks

- Docs state that NormalField is not true game normals.
- Disabled/default behavior is documented as visually neutral.
- Debug mode descriptions are present.
- The test plan covers coastal, jungle, desert, snow, high-tech, Allagan/cosmic, interior, combat, and UI-heavy cases.
- README mentions NormalField without overstating it.
