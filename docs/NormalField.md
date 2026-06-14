# Dalashade NormalField

NormalField is an optional screen-space inferred surface-normal layer for first-party Dalashade shader work.

It is built from ReShade depth, image gradients, shared material masks, water masks, and safety masks. It is useful for future GI, reflection, sharpening, weather, combat clarity, and light hierarchy improvements, but it is disabled by default and diagnostic-first.

NormalField is not true FFXIV material normals, not G-buffer access, and not actual texture normal maps. It is a conservative inferred surface field that should fail safely when depth or screen-space evidence is unreliable.

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
| `shaders/Dalashade_NormalField.fxh` | Shared include that resolves depth normals, detail normals, combined normal confidence, receiver masks, and debug colors. It creates no visible effect by itself. |
| `shaders/Dalashade_NormalDebug.fx` | Optional first-party debug visualizer for NormalField. It is disabled/pass-through by default and must be manually enabled in ReShade for inspection. |

## Debug Modes

`Dalashade_NormalDebug.fx` exposes shader-owned debug modes:

| Mode | View |
| --- | --- |
| `0` | Normal image / pass-through |
| `1` | Depth normal RGB |
| `2` | Detail normal RGB |
| `3` | Combined normal RGB |
| `4` | Ground-facing mask |
| `5` | Wall-facing mask |
| `6` | Detail eligibility |
| `7` | Normal confidence |
| `8` | Shading receiver |
| `9` | Reflection receiver |
| `10` | AO receiver |
| `11` | Safety suppression |

Normal RGB views encode normals as `normal * 0.5 + 0.5`. Mask views use grayscale or simple false color depending on the helper output.

## Recommended Debugging Order

For debugging:

1. Place `Dalashade_NormalDebug` near the bottom of the ReShade order.
2. Place `Dalashade_MaterialDebug` after it when comparing NormalField output against material masks.
3. Disable heavy bloom, sharpening, or other strong post-process effects if masks are hard to read.

For normal gameplay, keep `Dalashade_NormalDebug` disabled.

## Future Production Consumers

NormalField is intentionally not wired into production shaders yet. Planned follow-up integration order:

1. SurfaceReflection
2. SceneGI
3. SmartSharpen
4. WeatherAtmosphere
5. WeatherParticles
6. LightHierarchy
7. CombatClarity

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

### Ground-Facing Mask

Expected:

- Floors, terrain, and broad water surfaces are mostly visible.
- Walls and vertical objects are reduced.

### Wall-Facing Mask

Expected:

- Walls, trunks, structures, and vertical surfaces are visible.
- Flat ground is reduced.

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
