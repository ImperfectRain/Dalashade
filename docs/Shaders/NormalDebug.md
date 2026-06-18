# Dalashade_NormalDebug

## Current purpose

`shaders/Dalashade_NormalDebug.fx` visualizes the optional NormalField screen-space inferred surface data.

## Intended purpose

NormalDebug should make future NormalField integration auditable before production shaders consume it. It is not required for gameplay.

## Current implementation summary

The shader samples color/depth, resolves material/water/safety masks, calls `Dalashade_ResolveNormalField`, and returns one of the shared NormalField debug views. NormalField now includes an explicit texture-relief lane that combines local high-pass polarity, curvature, structure coherence, groove-line evidence, and structure-scale rejection into a bounded screen-space height hypothesis.

## Inputs

- Backbuffer color and depth.
- Shared material/context uniforms.
- NormalField controls: strength, depth strength, detail strength, material influence, water/skin/sky suppression, debug mode, debug boost.
- `Dalashade_MaterialMasks.fxh` and `Dalashade_NormalField.fxh`.

## Outputs

Debug-only visualization of inferred normals, candidates, receivers, and safety suppression.

## Core algorithm

1. Sample source color.
2. Resolve `MaterialResolve`, `WaterResolve`, and `SafetyResolve`.
3. Resolve `Dalashade_NormalField`.
4. Return `Dalashade_GetNormalDebugColor(field, mode, boost)`.

## Material/Water/Normal dependencies

NormalDebug is the direct consumer of `Dalashade_NormalField.fxh`. It depends on MaterialMasks for sky/skin/water/receptor gates. It does not prove any production shader is using NormalField.

## Debug modes

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Normal image | Pass-through source image. |
| 1 | Depth normal RGB | Encoded coarse depth normal. |
| 2 | Detail normal RGB | Encoded restrained detail normal. |
| 3 | Combined normal RGB | Encoded blended normal. |
| 4 | Ground/plane candidate | Plane/support candidate. |
| 5 | Wall-plane candidate | Strict vertical/wall candidate. |
| 6 | Structure candidate | Broad structure/object confidence. |
| 7 | Detail eligibility | Detail-normal trust/strength. |
| 8 | Normal confidence | Overall NormalField confidence. |
| 9 | Shading receiver | Future shading receiver mask. |
| 10 | Reflection receiver | Future reflection receiver mask. |
| 11 | AO receiver | Future AO receiver mask. |
| 12 | Safety suppression | RGB safety suppression components. |
| 13 | Texture relief normal RGB | Encoded legacy gradient relief normal from local highlight/groove detail. |
| 14 | Texture ridge/groove/relief | RGB view of ridge, coherent groove, and final relief/groove strength. |
| 15 | Texture groove line only | Clean coherent groove/seam line view. |
| 16 | Curvature ridge | Hessian-style raised ridge evidence. |
| 17 | Curvature valley/groove | Hessian-style valley, indent, and groove evidence. |
| 18 | Structure coherence | RGB curvature coherence, composite confidence, and groove-line support. |
| 19 | Composite relief height | Neutral-centered grayscale height hypothesis with subtle ridge/valley tint. |
| 20 | Composite relief normal RGB | Encoded normal generated from the composite relief-height hypothesis used by NormalField; debug visibility is stronger than production blending. |

## Safety and suppression rules

Sky, skin, water, highlight, foliage-noise, broad lighting gradients, UI/depth risk, and edge discontinuity gates suppress unsafe detail, relief, and receiver values. Default/fallback normals are display-safe only and should not create receiver masks.

## Current limitations

- Depth quality depends on ReShade depth.
- ReflectionReceiver can be intentionally darker than MaterialDebug receiver views.
- Detail and texture-relief normals are bounded by confidence gates to avoid shimmer and embossed lighting artifacts.
- UI/depth risk remains heuristic.

## Future direction

Use NormalDebug across representative scenes before increasing NormalField influence in SurfaceReflection, SceneGI, SmartSharpen, WeatherAtmosphere, AtmosphereBloom, or ContactTone.

## Do not do

- Do not treat NormalDebug as production output.
- Do not boost stored receiver values just to make debug brighter.
- Do not describe the output as real FFXIV normals.
