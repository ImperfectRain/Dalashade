# Dalashade NormalField Shader Contract

`shaders/Dalashade_NormalField.fxh` defines Dalashade's optional screen-space inferred surface field. It is diagnostic-first and is not a replacement for real engine normals.

NormalField is:

- built from ReShade depth, image gradients, material masks, water masks, and safety masks
- useful for future GI, reflection, sharpening, weather, combat clarity, and light hierarchy work
- disabled by default at the plugin level
- conservative by design

NormalField is not:

- true FFXIV material normals
- FFXIV G-buffer access
- real texture normal maps
- a production dependency for current first-party shaders unless explicitly wired later

## Dalashade_NormalField

Fields:

- `DepthNormal`: coarse screen-space normal reconstructed from nearby depth samples.
- `DetailNormal`: restrained luma/color-gradient detail normal.
- `CombinedNormal`: blended display-safe normal.
- `SurfaceFacing`: generic facing estimate for debug display.
- `GroundFacing`: compatibility alias for `GroundPlaneCandidate`.
- `WallFacing`: compatibility alias for `WallPlaneCandidate`.
- `EdgeDiscontinuity`: local depth/luma edge discontinuity.
- `DetailStrength`: eligible detail-normal strength after material/safety gates.
- `NormalConfidence`: overall confidence in the inferred normal field.
- `DepthConfidence`: depth-derived normal confidence.
- `DetailConfidence`: image-gradient normal confidence.
- `ShadingReceiver`: plausible receiver for future direct/indirect shading support.
- `ReflectionReceiver`: plausible receiver for future reflection/wet/specular support.
- `AOReceiver`: plausible receiver for future AO/contact shading support.
- `GroundPlaneCandidate`: floor/terrain/water-plane/support-surface candidate, not literal ground ID.
- `StructureCandidate`: broad structure/object/edge/hard-surface confidence.
- `WallPlaneCandidate`: stricter physical-ish wall/vertical orientation candidate.
- `OrientationConfidence`: confidence that plane/wall orientation can be trusted.

## Orientation Semantics

Default normals are display-safe, not semantic evidence. `float3(0, 0, 1)` must not by itself imply ground, wall, AO, or reflection receiver.

`GroundPlaneCandidate` is a plane/support hint. It can show terrain, dock floors, water planes, and broad horizontal-like surfaces, but it is not a hard "ground" material ID.

`StructureCandidate` is the broad geometry/structure mask. Production shaders should generally prefer this for AO/shading support when true physical orientation is uncertain.

`WallPlaneCandidate` is stricter and may be dim. It should only be used when vertical orientation matters. It should not be treated as the primary structure mask.

## Receiver Semantics

NormalField consumes `Dalashade_MaterialResolve`, `Dalashade_WaterResolve`, and `Dalashade_SafetyResolve`.

- `ReflectionReceiver` leans on `material.ReflectionReceiverConfidence` and `water.WaterReceiver`.
- `AOReceiver` leans on `material.AOReceiverConfidence`, `StructureCandidate`, and `GroundPlaneCandidate`.
- `ShadingReceiver` leans on `StructureCandidate`, `GroundPlaneCandidate`, `material.StructureReceiverConfidence`, and safety gates.

`water.WaterSource` and `water.SkySource` are intentionally excluded from receiver logic. They describe sampled reflection source-color/context eligibility, not where a reflection is allowed to appear.

## Detail Normal Rules

Detail normals are deliberately weak. They are increased on hard structured surfaces such as stone, metal, sand, and some crystal/aether surfaces. They are reduced on:

- sky/cloud/fog
- skin
- water planes
- bright highlights
- foliage noise
- strong depth/luma discontinuities

This avoids an embossed relief-map look and reduces shimmer.

## NormalDebug Modes

`Dalashade_NormalDebug.fx` visualizes this contract:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Normal image | Pass-through source image. |
| 1 | Depth normal RGB | Encoded depth-derived normal. |
| 2 | Detail normal RGB | Encoded detail-gradient normal. |
| 3 | Combined normal RGB | Encoded combined normal. |
| 4 | Ground/plane candidate | Support-plane candidate. |
| 5 | Wall-plane candidate | Strict wall/vertical candidate. |
| 6 | Structure candidate | Broad structure/object mask. |
| 7 | Detail eligibility | Detail normal eligibility/strength. |
| 8 | Normal confidence | Overall normal confidence. |
| 9 | Shading receiver | Future shading receiver confidence. |
| 10 | Reflection receiver | Future reflection receiver confidence. |
| 11 | AO receiver | Future AO receiver confidence. |
| 12 | Safety suppression | RGB safety components for sky/skin/water/noise risk. |

NormalDebug is a diagnostic shader. It does not prove that a production shader is currently using NormalField.

## Current Limitations

- Depth normals depend on ReShade depth availability and can fail on UI, sky, or invalid depth.
- Detail normals are screen-space image gradients and can confuse texture, lighting, and geometry.
- Wall-plane orientation is intentionally strict and may be dim.
- Receiver fields are confidence masks, not final visual effects.
- The system has no motion vectors or temporal stability layer.

## Future Direction

Recommended integration order:

1. SurfaceReflection: use NormalField as a minor projection/detail modifier only after material receiver masks are stable.
2. SceneGI: use `StructureCandidate` and `AOReceiver` conservatively for contact/bounce shaping.
3. SmartSharpen: use `DetailStrength`, `StructureCandidate`, and safety gates for shimmer-safe clarity.
4. WeatherAtmosphere: use only broad receiver/safety hints, not normal detail.
5. WeatherParticles, LightHierarchy, CombatClarity: only after debug scenes prove stability.

## Do Not Do

- Do not describe NormalField as real game normals.
- Do not make fallback normals produce semantic receiver masks.
- Do not use `WallPlaneCandidate` as the main structure signal.
- Do not use reflection source-color fields such as `WaterSource` or `SkySource` as receiver evidence.
- Do not integrate NormalField into production shaders without debug screenshots across coastal, snow, desert, city, interior, aether, foliage, and combat scenes.
