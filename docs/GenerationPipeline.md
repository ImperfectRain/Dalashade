# Generation Pipeline

This describes implemented behavior from `/dalashade` to a generated ReShade preset.

## Numbered Flow

1. The user opens Dalashade with `/dalashade` or Dalamud UI callbacks in `Dalashade/Plugin.cs`.
2. The user clicks `Generate Now` in `Dalashade/Windows/MainWindow.cs` or `Dalashade/Windows/ConfigWindow.cs`.
3. `Plugin.GenerateNow()` resolves the effective base preset path, refreshes context, screenshot analysis, and master style state.
4. `GameContextService.Refresh()` in `Dalashade/GameContext.cs` reads territory, weather, time, combat, duty, GPose, and cutscene state.
5. `SceneClassifier.Classify()` converts `GameContext` into `SceneTags`.
   - Classification is hierarchical: one primary biome, supporting mood/material/art-direction tags, weather tags, area/context tags, and gameplay-state modifiers.
   - Territory keyword confidence is preserved so strong XIV zone-family matches can push clearer art direction while fallback matches stay conservative.
6. If enabled, `ImageAnalysisService.Refresh()` in `Dalashade/ImageAnalysis.cs` analyzes the latest screenshot.
7. If enabled, `MasterStyleService.Refresh()` in `Dalashade/MasterStyle.cs` analyzes selected master style images.
8. `SceneIntentBuilder.Build(...)` in `Dalashade/SceneIntent.cs` summarizes context, tags, screenshot analysis, target style, and performance budget into stack-aware intent values.
   - Biome intent is confidence-aware.
   - Fog/mist drives haze; gloom drives dark mood with much less haze.
   - Night is a contextual layer: derived tags such as `moonlitNight`, `lamplitNight`, `canopyNight`, `coastalNight`, `industrialNight`, `snowNight`, and `desertNight` add light-hierarchy intent without replacing biome/weather/material identity.
   - Day is also a contextual layer: derived tags such as `sunlitDay`, `openSkyDay`, `coastalDay`, `canopyDay`, `snowDay`, `desertDay`, `stormDay`, and `highTechDay` add daylight/highlight/reflection/atmosphere intent without globally brightening the scene.
   - Combat/duty dampen cinematic, bloom, and haze pressure without deleting the zone identity.
9. `MaterialProfileBuilder.Build(...)` can derive report-visible material plausibility priors from the tag stack, territory/weather text, area context, screenshot metrics, gameplay state, and `SceneIntent`.
   - This is scene plausibility, not true engine material ID detection.
   - It sits between SceneTags/MaterialIntent and shader-side pixel masks.
   - It does not change `SceneIntent`, `VisualProfile`, or generated shader values by itself.
10. `MaterialIntentBuilder.Build(...)` consumes the tag stack and `MaterialProfile` priors when MaterialIntent diagnostics or shader mapping are enabled.
11. `ProfileEngine.CreateWithRules()` in `Dalashade/VisualProfile.cs` creates a `VisualProfile`, applied rules, and tag-stack diagnostics.
12. If master style is available, `MasterStyleMatcher.Match()` in `Dalashade/MasterStyleMatcher.cs` returns deltas, diagnostics, rules, and color-family adjustments.
13. `Plugin.ScanPresetCompatibility()` runs `PresetWriter.ScanSupportedVariables()` and `PresetAnalyzer.Analyze()`.
    - First-party `Dalashade_*` shader sections are classified as known controlled Dalashade effects when they appear in preset content.
14. `PresetWriter.WriteGeneratedPreset()` reads the base preset and creates shader adjustments from `ShaderVariableMapper.CreateAdjustments()`.
    - If `EnableDalashadeCustomShaders` is enabled, `CustomShaderVariableMapper` can also write normalized `SceneIntent` values into matching Dalashade custom shader sections.
    - If `AutoInjectDalashadeCustomShaderSections` is also enabled, known Dalashade custom shader sections and variables can be inserted into the generated preset only.
    - Custom shader variable writes happen when the generated preset content contains matching Dalashade section/key lines, either from the base preset or from generated-preset-only injection.
    - `Dalashade_SmartSharpen.fx` receives extra authority-aware tuning from preset analysis so it behaves as a secondary, foliage-safe pass when other active sharpeners are present.
    - First-party custom shaders can receive `Night`, `Moonlight`, `ArtificialLight`, `AmbientDarkness`, `NightAtmosphere`, `Daylight`, `Sunlight`, `OpenSkyLight`, `SurfaceHeat`, `DayAtmosphere`, `DayReflection`, and `DayHighlightPressure` intent values when their sections/keys exist or are injected into the generated preset.
    - Material-aware first-party shader behavior is section-scoped: SmartSharpen uses final masks for suppression, AtmosphereBloom uses glow eligibility masks, WeatherAtmosphere uses air/haze masks, AdaptiveGrade uses only subtle protection/preservation masks, SceneGI uses masks for optional contact AO, ambient bounce, and night light pooling, and SurfaceReflection uses masks for water sheen, wet glints, ice sheen, and neon/aether reflection impressions.
    - `Dalashade_MaterialMasks.fxh` exposes raw candidates, scene-gated candidates, final masks, optional depth assist, and the shared `Dalashade_ResolveMaterials`, `Dalashade_ResolveWater`, and `Dalashade_ResolveSafety` contracts for first-party shaders. Depth assist is shader-owned, disabled by default, and never required.
    - `Dalashade_MaterialDebug.fx` and `Dalashade_NormalDebug.fx` are optional utility shaders for material and NormalField diagnostics. `Dalashade_SceneGI.fx` is an optional screen-space GI-style shader, not path tracing, RTGI, or PTGI. `Dalashade_SurfaceReflection.fx` is an optional reflection-impression shader with a restrained pseudo-SSR component, not full SSR or ray tracing. Dalashade can inject sections/variables for them, but does not add them to `Techniques=` or require them for normal output.
    - Dalashade does not append custom shader entries to `Techniques=`, copy `.fx` files, or require custom shaders for normal operation.
15. `GenerationAuthorityPolicy.From()` dampens secondary authorities for selected compatibility modes.
16. The writer edits only matching section/key lines, records `ChangedShaderVariable` entries, and applies `SanitizeActionPipeline` only when allowed by mode.
17. If backups are enabled and the generated preset already exists, `PresetWriter` creates and prunes backups.
18. `PresetWriter` writes a temporary file and replaces the generated preset path.
19. `Plugin.ReloadShadersIfNeeded()` optionally calls `ReShadeController.ReloadAfterPresetWrite()`.

## Shader-side Pipeline

First-party shaders consume generated uniforms through a shared shader contract rather than separate local material detectors.

```text
Generated preset uniforms
-> Dalashade_MaterialMasks.fxh
-> MaterialResolve / WaterResolve / SafetyResolve
-> shader-specific receiver/source/effect logic
```

`Dalashade_MaterialMasks.fxh` owns raw pixel evidence, scene-gated candidates, material competition, final material masks, water resolve, material resolve, and safety resolve. `Dalashade_NormalField.fxh` optionally consumes those resolves to produce inferred screen-space normal/structure/receiver diagnostics.

See:

- `docs/Shaders/MaterialMasks.md`
- `docs/Shaders/NormalField.md`
- `docs/Shaders/ShaderSystemOverview.md`

## Export Pipeline

Standalone report export is owned by `CompatibilityReportExporter`. If no path is configured, it writes to:

```text
<plugin config directory>/Reports/Dalashade_CompatibilityReport_yyyyMMdd_HHmmss.md
```

Debug bundle export is owned by `DebugBundleExporter`. It creates a timestamped folder under:

```text
<plugin config directory>/DebugBundles/
```

The debug bundle includes a fresh compatibility report when possible and records optional failures in `manifest.json` and `bundle-export-log.txt`. See `docs/DebugBundles.md`.

## Report-Only Diagnostics

Compatibility report export can build `MaterialProfile` and `MaterialIntent` diagnostics from the existing tag stack, screenshot metrics, and SceneIntent context.

- MaterialProfile is the scene plausibility stage. It chooses a broad family such as `jungle/rainforest`, `coastal/tropical`, `snow/cold`, `desert/badlands`, `neon/high-tech`, or `aetherial/cosmic`, adds profile tags such as `forestCanopy`, `coastalWaterline`, `snowfield`, `desertOpen`, `neonUrban`, `aetherialLandscape`, `dungeonInterior`, or `raidArena`, then records material priors and suppressions.
- MaterialIntent is inferred material likelihood, not true engine material ID detection.
- MaterialIntent consumes MaterialProfile priors plus older tag, territory, screenshot, and SceneIntent evidence. MaterialProfile shapes plausibility and caps rather than stacking directly, so reports separate profile prior, non-profile evidence, final value, and suppressions.
- `EnableMaterialIntent` controls whether MaterialIntent is calculated; disabled returns neutral values.
- `EnableMaterialIntentDiagnostics` controls whether reports/UI show the diagnostics.
- `EnableMaterialIntentShaderMapping` allows generated-preset MaterialIntent uniform writes only when MaterialIntent is enabled, strength is greater than `0.0`, and matching known Dalashade custom shader keys exist.
- When MaterialIntent shader mapping is disabled, MaterialIntent variables are skipped entirely. Generated-preset-only injection does not add material keys in that state.
- MaterialProfile and MaterialIntent do not directly change `SceneIntent` or `VisualProfile`; they shape scene material plausibility and optional shader uniform output.
- Screenshot analysis can affect `VisualProfile` and `SceneIntent` when `AutoAdjustFromScreenshots` is enabled. Its impact is scaled by `ScreenshotAnalysisStrength`.
- Screenshot region hints and named screenshot opinions are still weak priors: upper-third smooth blue/bright regions can support sky, lower blue/cyan can support water, lower warm regions can support sand, lower/mid bright cold regions can support snow, middle green can support foliage, warm center/global colors can support skin protection, and cyan/purple/magenta can support neon/aether. Scene context and shader-side masks still decide whether those priors are useful.
- Material debug mode, overlay mode, opacity, and strength are owned by the relevant `.fx` shader UI in ReShade. Dalashade writes scene-level material channel uniforms only.
- Reports use the terms `RawCandidate`, `SceneGatedCandidate`, and `FinalMask` for shader-side mask tuning. Plugin-side MaterialProfile and MaterialIntent provide scene gates; the `.fx` masks still decide pixel-level material influence.
- Compatibility reports also list first-party custom shader activation state, sections receiving MaterialIntent uniforms, injected shader-owned depth controls, and likely failure sources for material calibration: scene plausibility, MaterialIntent gating, raw pixel heuristic, final conflict suppression, optional depth assist, or production shader behavior.
- Compatibility reports include a `Material Parity Audit` that compares first-party shader source declarations, generated-preset write coverage, expected material use, shared `Dalashade_MaterialMasks.fxh` resolver use, local material logic, and debug visibility. This is diagnostics-only and does not change generated preset output.

## Pipeline Ownership

| Stage | Owner |
| --- | --- |
| Command/window lifecycle | `Dalashade/Plugin.cs` |
| UI buttons | `Dalashade/Windows/MainWindow.cs`, `Dalashade/Windows/ConfigWindow.cs` |
| Paths/config | `Dalashade/Configuration.cs`, `Plugin.InitializePresetFolders()`, `Plugin.ResolveEffectiveBasePresetPath()` |
| Context and tags | `Dalashade/GameContext.cs` |
| Scene intent and tag diagnostics | `Dalashade/SceneIntent.cs` |
| Material profile and intent report diagnostics | `Dalashade/MaterialProfile.cs`, `Dalashade/MaterialProfileBuilder.cs`, `Dalashade/MaterialIntent.cs`, `Dalashade/MaterialIntentBuilder.cs` |
| Screenshot analysis | `Dalashade/ImageAnalysis.cs` |
| Master image selection | `Dalashade/MasterStyle.cs` |
| Master style deltas | `Dalashade/MasterStyleMatcher.cs` |
| Profile generation | `Dalashade/VisualProfile.cs` |
| Compatibility analysis | `Dalashade/PresetAnalyzer.cs` |
| Shader definitions | `Dalashade/ShaderVariableMapper.cs` |
| Custom shader intent definitions | `Dalashade/CustomShaderVariableMapper.cs` |
| Preset write | `Dalashade/PresetWriter.cs` |
| Gameplay sanitize | `Dalashade/SanitizeActionPipeline.cs` |
| Reload | `Dalashade/ReShadeController.cs` |

## Common Failure Points

| Symptom | Likely cause | Inspect |
| --- | --- | --- |
| Base preset missing | `Configuration.BasePresetPath` or dropdown selection points to a missing file | Config UI, `BasePresetLibrary`, `Plugin.ResolveEffectiveBasePresetPath()` |
| Generated path equals base path | User selected the same file for base and generated preset | `PresetWriter.WriteGeneratedPreset()` |
| No supported shader variables changed | Base preset has unsupported sections/keys or inactive sections are skipped | `ShaderVariableMapper`, `PresetWriter.ScanSupportedVariables()`, compatibility report |
| Too many variables changed | Loose key mode or broad active stack | `Configuration.ShaderMatchingMode`, `GenerationAuthorityPolicy`, changed variables UI |
| ReShade reload failed | ReShade.ini path/key mismatch or hotkey send failed | `ReShadeController`, `ReShadeReload.md` |
| Release manifest points to missing zip | `repo.json` download links do not match GitHub release asset | `docs/ReleaseChecklist.md` |
