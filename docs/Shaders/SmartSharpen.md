# Dalashade_SmartSharpen

## Current purpose

`shaders/Dalashade_SmartSharpen.fx` provides material-aware clarity while suppressing shimmer, halos, sky crunch, skin harshness, water texture noise, and bright snow/sand grain.

## Intended purpose

SmartSharpen should be the final Dalashade clarity pass. It should improve readability without duplicating external sharpeners or making every texture crispy.

## Current implementation summary

The shader separates structural edges from microtexture detail, resolves shared material/water/safety/receiver data through the inline `Dalashade_FrameData.fxh` contract, applies dampening for unsafe regions, then blends a restrained sharpened result. Optional FrameData surface data can add mild stable-structure support and suppress unstable haloing, but it never classifies materials or creates new edges.

## Inputs

- Backbuffer color and depth.
- Scene intent uniforms for combat, day highlight pressure, open sky, fog/weather, and readability.
- Material uniforms for foliage, water, specular glint, sand/dust, snow/ice, stone/ruins, metal/industrial, crystal/aether, neon/glass, sky/fog, and skin.
- Shared FrameData base resolver: `Dalashade_ResolveFrameBaseData`, which wraps canonical material, water, safety, and receiver resolves.
- Optional FrameData surface resolver: `Dalashade_ResolveFrameSurfaceData`, used only for SmartSharpen's existing NormalField-backed stable-structure and edge-discontinuity shaping.
- Sharpen strength, radius, threshold, dampening, and debug controls.

## Outputs

Normal output is source color with controlled clarity. Debug modes show edge/detail/dampening/final sharpen masks.

## Core algorithm

1. Sample source and local neighborhood.
2. Estimate structural edges and microtexture detail.
3. Resolve shared FrameData base fields and optional FrameData surface fields.
4. Dampening suppresses sky/fog, water shimmer, foliage shimmer, skin, snow/sand highlights, specular glints, and aether/neon halos.
5. Apply sharpen contribution with source-relative guardrails.

## Material/Water/Normal dependencies

Consumes FrameData base fields. Unsafe sharpening is dampened through `frame.SafetySkyReject`, `frame.SafetySkinReject`, `frame.SafetyFoliageNoiseReject`, `frame.SafetyHighlightProtect`, `frame.SafetyBrightSandProtect`, and `frame.SafetySnowProtect`; `frame.WaterPixelConfidence` and `frame.WaterReceiver`; and material fields including `frame.MaterialFoliage`, `frame.WaterSpecularGlint`, `frame.MaterialCrystalAether`, `frame.MaterialNeonGlass`, and `frame.MaterialSkyCloudFog`. `WaterSource`, `WaterSkySource`, and `WaterHorizonOnly` are not sharpening permission.

Stable structure support comes from `frame.ReceiverStructure`, `frame.MaterialSurfaceHardness`, `frame.MaterialStoneRuins`, and `frame.MaterialMetalIndustrial`. Optional FrameData surface support uses `surface.StructureCandidate`, `surface.NormalConfidence`, and `surface.OrientationConfidence` as small stability gates, while `surface.EdgeDiscontinuity` and risky `surface.DetailStrength` suppress halos/noisy texture. NormalField does not authorize sharpening water, foliage, skin, or sky.

## First-party shader mode

`Dalashade_StandaloneStrength` is `0` in Supportive mode and `1` in Standalone mode. SmartSharpen uses it to add a modest stable-structure boost and slightly higher safe delta limits while also increasing unsafe material dampening. It does not increase sharpening on sky, water shimmer, foliage noise, skin, snow/sand highlights, glints, or aether/neon halos.

## Debug modes

`Dalashade_MaterialDebugMode` is used when `ShowDebugMask` is enabled:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Off | Normal output unless `DebugView` is active. |
| 1 | Overview | Composite material dampening overview. |
| 2 | Foliage dampening | Foliage/organic green dampening. |
| 3 | Water/specular dampening | Water and glint dampening. |
| 4 | Snow/ice dampening | Snow/ice and bright-snow protection. |
| 5 | Sky/fog exclusion | Sky/fog and smooth-gradient exclusion. |
| 6 | Skin protection dampening | Skin/warm smooth protection. |
| 7 | Water plane dampening | Broad water/far-depth dampening. |
| 8 | Specular glint dampening | Specular edge/glint halo protection. |
| 9 | Unused | Reserved/no custom material debug branch. |
| 10 | Final material dampening | Final material/texture/structural dampening. |

`DebugView` is the older sharpen diagnostic selector:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Composite | Normal/composite output. |
| 1 | Structural edge | Stable structural edge mask. |
| 2 | Texture detail | Microtexture/detail mask. |
| 3 | Final sharpen | Final sharpen amount. |
| 4 | Dampening | Combined dampening/halo protection. |
| 5 | Foliage/far-depth | Foliage and far-depth dampening. |

## Safety and suppression rules

Sky/fog is strongly dampened. Skin avoids harsh contrast. Water suppresses shimmer and texture crunch but keeps large structure. Specular glints and aether/neon halos get halo protection. Foliage suppresses micro-shimmer. Snow/ice and bright sand avoid grainy high-contrast texture. NormalField edge discontinuity reduces halo risk rather than increasing sharpening.

## Current limitations

- It cannot know true object scale.
- It depends on prior stack order and existing external sharpeners.
- It can only infer UI/depth risk indirectly.
- NormalField data is screen-space inferred and can be wrong on foliage, water, transparency, and silhouettes.

## Future direction

Continue validating NormalField shaping across foliage, water, snow, city, and combat scenes before increasing its influence. Any future use should remain secondary to shared material/safety suppression.

## Do not do

- Do not sharpen sky, fog, skin, or water texture aggressively.
- Do not compensate for soft reflections or GI by increasing sharpen.
- Do not become a second broad color/contrast grade.
- Do not use NormalField detail as permission to sharpen foliage, water shimmer, or sky gradients.
