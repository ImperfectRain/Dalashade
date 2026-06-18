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
- material truth for production shaders

`Dalashade_FrameData.fxh` exposes NormalField through an optional surface data path. The base FrameData path does not call NormalField, so shaders that only need material, water, safety, or receiver data do not pay the normal-field cost. Current first-party production shaders that opt into FrameData surface data should treat NormalField as secondary support behind material, water, and safety gates.

## Dalashade_NormalField

Fields:

- `DepthNormal`: coarse screen-space normal reconstructed from nearby depth samples.
- `DetailNormal`: restrained luma/color-gradient detail normal.
- `TextureGradientReliefNormal`: legacy local Sobel/gradient relief normal for comparison.
- `TextureReliefNormal`: composite relief normal generated from the bounded screen-space height hypothesis.
- `CombinedNormal`: blended display-safe normal.
- `SurfaceFacing`: generic facing estimate for debug display.
- `GroundFacing`: compatibility alias for `GroundPlaneCandidate`.
- `WallFacing`: compatibility alias for `WallPlaneCandidate`.
- `EdgeDiscontinuity`: local depth/luma edge discontinuity.
- `DetailStrength`: eligible detail-normal strength after material/safety gates, including safe texture-relief support for existing consumers.
- `TextureReliefStrength`: explicit texture-relief support for shaders that need to distinguish relief from general detail.
- `TextureRidge`: local bright ridge evidence.
- `TextureGroove`: local dark groove evidence.
- `TextureGrooveLine`: direction-coherent groove/seam evidence for tile gaps, panel lines, cracks, and engraved material boundaries.
- `TextureCurvatureRidge`: Hessian-style local-maxima evidence for raised detail.
- `TextureCurvatureValley`: Hessian-style local-minima evidence for indents, seams, and grooves.
- `TextureCoherence`: structure-tensor-style confidence that nearby gradients form an organized line or surface pattern instead of random noise.
- `TextureHeightComposite`: centered `0..1` composite relief-height hypothesis used for diagnostics and texture-relief normals.
- `TextureCompositeConfidence`: confidence that the composite height/normal estimate is usable after broad-gradient and noise rejection.
- `TextureReliefSafety`: final safety gate for texture-relief use after sky, water, skin, UI, foliage, highlight, emissive, and hard-edge rejection.
- `NormalConfidence`: overall confidence in the inferred normal field.
- `DepthConfidence`: depth-derived normal confidence.
- `DetailConfidence`: image-gradient normal confidence.
- `TextureReliefConfidence`: confidence in the texture-relief lane before final ridge/groove strength.
- `ShadingReceiver`: plausible receiver for future direct/indirect shading support.
- `ReflectionReceiver`: plausible receiver for future reflection/wet/specular support.
- `AOReceiver`: plausible receiver for future AO/contact shading support.
- `GroundPlaneCandidate`: floor/terrain/water-plane/support-surface candidate, not literal ground ID.
- `StructureCandidate`: broad structure/object/edge/hard-surface confidence. Texture relief is only a small support hint here; use `TextureReliefStrength` or `TextureGrooveLine` directly when a shader specifically wants relief/seam data.
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

Detail normals are deliberately weak. They are increased on hard structured surfaces such as stone, metal, sand, and some crystal/aether surfaces. The texture-relief lane now builds a bounded composite height hypothesis with a small relief-height compiler:

1. NormalField returns a neutral field before expensive sampling when disabled, so optional surface-data consumers do not pay the relief pass when the feature is off
2. local high-pass luminance gives a rough bright-ridge/dark-groove polarity signal, but only as a capped compatibility hint
3. Hessian-style curvature separates likely raised local maxima from likely indented local minima
4. structure-tensor-style coherence favors organized tile seams, engraved rings, panel gaps, and cracks over random speckle
5. a structure-scale gate checks whether the signal persists beyond one-pixel grain before it is allowed to affect height
6. coherence is allowed to support the height estimate only when paired with actual ridge, valley, groove, or structure energy
7. raw high-pass detail is clamped and weighted by coherence so broad lighting and noisy texture cannot dominate the height field
8. a confidence-aware cross blur cleans the compiled height before normal derivation, smoothing weak noisy texture while preserving confident grooves and ridges
9. coherent grooves are biased downward more strongly than ridges are raised, which makes tile gaps, panel seams, and engraved lines read more like real indent data
10. a dedicated texture-relief safety gate suppresses sky/cosmic fields, water, skin, UI/depth risk, foliage shimmer, bright highlights, emissive/aether bloom, and unsafe hard edges before groove lines or composite height can drive relief
11. neighbor groove samples are constrained by the same relief safety gate as the center sample, so unsafe adjacent sky, bloom, water, or UI pixels cannot steer the compiled normal
12. broad-gradient, safety, material, and edge gates suppress lighting ramps, sky, skin, water, foliage shimmer, UI, and isolated noise
13. the final composite height stays neutral-centered around `0.5` and is converted into a relief normal for debug and optional surface support

This resembles the workflow used to derive a traditional normal map from a height map, but the height map is only a screen-space estimate from the current frame. It must not be treated as recovered game texture data.

Detail and texture-relief normals are reduced on:

- sky/cloud/fog
- skin
- water planes
- bright highlights
- emissive/aether bloom fields
- foliage noise
- broad lighting gradients
- UI/depth risk
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
| 12 | Safety/relief suppression | RGB safety components for sky, skin/highlight, and water/noise/texture-relief rejection risk. |
| 13 | Legacy relief normal RGB | Encoded legacy local gradient relief normal for comparison only. |
| 14 | Texture ridge/groove/relief | RGB ridge, coherent groove, and final relief/groove strength. |
| 15 | Texture groove line only | Clean coherent groove/seam line view. |
| 16 | Curvature ridge | Hessian-style raised ridge evidence. |
| 17 | Curvature valley/groove | Hessian-style valley, indent, and groove evidence. |
| 18 | Structure coherence | RGB curvature coherence, composite confidence, and groove-line support. |
| 19 | Composite relief height | Neutral-centered grayscale height hypothesis with subtle ridge/valley tint. |
| 20 | Composite relief normal RGB | Encoded normal generated from the composite relief-height hypothesis used by `TextureReliefNormal`; debug visibility is stronger than production blending. |

NormalDebug is a diagnostic shader. It does not prove that a production shader is currently using NormalField.

## Current Limitations

- Depth normals depend on ReShade depth availability and can fail on UI, sky, or invalid depth.
- Detail normals are screen-space image gradients and can confuse texture, lighting, and geometry.
- Texture relief has no temporal history, so dense repeating textures can still shimmer or over-report in single frames.
- Wall-plane orientation is intentionally strict and may be dim.
- Receiver fields are confidence masks, not final visual effects.
- The system has no motion vectors or temporal stability layer.

## Future Direction

Recommended integration order:

1. ContactTone: use `AOReceiver`, `StructureCandidate`, and plane candidates for grounded edge tone; do not use it as GI or bloom.
2. SceneGI: use `StructureCandidate`, `AOReceiver`, and explicit material intent conservatively for contact/bounce shaping.
3. SmartSharpen: prefer `DetailStrength` for broad clarity and `TextureReliefStrength`/`TextureReliefSafety` only for relief-aware restraint.
4. AtmosphereBloom and WeatherAtmosphere: use only broad receiver/safety hints; do not let texture relief drive atmospheric glow directly.
5. SurfaceReflection: keep NormalField as a minor support input only after receiver masks are stable.
6. WeatherParticles, LightHierarchy, CombatClarity: only after debug scenes prove stability.

## Do Not Do

- Do not describe NormalField as real game normals.
- Do not describe texture relief as recovered texture normal maps.
- Do not make fallback normals produce semantic receiver masks.
- Do not use `WallPlaneCandidate` as the main structure signal.
- Do not use reflection source-color fields such as `WaterSource` or `SkySource` as receiver evidence.
- Do not integrate NormalField into production shaders without debug screenshots across coastal, snow, desert, city, interior, aether, foliage, and combat scenes.
