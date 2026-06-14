# Material Intent

Dalashade has two material layers:

1. Plugin-side scene plausibility from `MaterialProfile` and `MaterialIntent`.
2. Shader-side pixel classification from `Dalashade_MaterialMasks.fxh`.

These layers solve different problems and should not be collapsed.

## Plugin-side MaterialProfile

Files:

- `Dalashade/MaterialProfile.cs`
- `Dalashade/MaterialProfileBuilder.cs`
- `Dalashade/MaterialProfileContribution.cs`

`MaterialProfileBuilder` reads territory text, biome tags, weather, area context, gameplay state, screenshot metrics, and `SceneIntent` to produce scene-level material priors. Examples include coastal waterline, jungle canopy, snowfield, desert open, neon urban, aetherial landscape, dungeon interior, and raid arena.

MaterialProfile answers: "Could this scene plausibly contain this material family?"

It does not answer: "Is this pixel water/skin/sand/sky?"

## Plugin-side MaterialIntent

Files:

- `Dalashade/MaterialIntent.cs`
- `Dalashade/MaterialIntentBuilder.cs`
- `Dalashade/MaterialIntentContribution.cs`

`MaterialIntentBuilder` converts profile priors, tags, screenshot hints, and intent values into normalized material channels:

- `MaterialFoliage`
- `MaterialWaterPlane`
- `MaterialSpecularGlint`
- `MaterialWaterSpecular`
- `MaterialSandDust`
- `MaterialSnowIce`
- `MaterialStoneRuins`
- `MaterialMetalIndustrial`
- `MaterialCrystalAether`
- `MaterialNeonGlass`
- `MaterialFireLavaHeat`
- `MaterialSkyCloudFog`
- `MaterialSkinProtection`
- `MaterialVoidDarkness`

MaterialIntent records positive and negative contribution reasons. It is useful for reports even when shader mapping is disabled.

## Shader Uniform Mapping

`Dalashade/CustomShaderVariableMapper.cs` writes MaterialIntent and scene-context values into first-party shader sections only when the relevant settings allow it:

- `EnableDalashadeCustomShaders`
- `EnableMaterialIntent`
- `EnableMaterialIntentShaderMapping`
- matching shader sections/keys or generated-preset injection

MaterialIntent uniforms are scene priors. Shader-side `Dalashade_MaterialMasks.fxh` still performs pixel classification from color, luma, saturation, edges, detail, depth, screen position, and local conflict resolution.

## Water Context

Water context uniforms such as `Dalashade_WaterContext`, `Dalashade_CoastalContext`, `Dalashade_OpenOceanContext`, `Dalashade_ShallowWaterContext`, and `Dalashade_WetSurfaceContext` are scene priors. They allow water detection; they do not prove water pixels.

Shader-side `WaterPixelConfidence`, `WaterReceiver`, `WaterSource`, `SkySource`, and `HorizonOnlyConfidence` keep water roles separated. In that vocabulary, `WaterSource` means water-related reflection source-color eligibility; it does not mean "this pixel is a water surface receiver."

## Debugging Material Failures

Use this order:

1. Compatibility report: check MaterialProfile family, tags, priors, suppressions, and MaterialIntent values.
2. MaterialDebug modes: check raw/gated/final material masks and competition modes.
3. Production shader debug: check why a shader applied or suppressed a final effect.

Common diagnosis:

- Scene prior wrong: fix `MaterialProfileBuilder` or `MaterialIntentBuilder`.
- Pixel classification wrong: fix `Dalashade_MaterialMasks.fxh`.
- Production effect wrong despite correct masks: fix the specific shader receiver/source gating.

## Stable vs Experimental

MaterialIntent channels and generated uniform names are stable enough for first-party shader mapping. Shader-side competition and specialized receiver fields are still experimental and should be tuned through debug views before production changes.

## Do Not Do

- Do not treat MaterialIntent as true material ID data.
- Do not make scene priors override pixel evidence by themselves.
- Do not add shader-local copies of base water/sky/skin/sand classification.
- Do not tune production shader output when MaterialDebug shows the upstream material mask is wrong.
