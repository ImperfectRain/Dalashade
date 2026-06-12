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
   - Combat/duty dampen cinematic, bloom, and haze pressure without deleting the zone identity.
9. `ProfileEngine.CreateWithRules()` in `Dalashade/VisualProfile.cs` creates a `VisualProfile`, applied rules, and tag-stack diagnostics.
10. If master style is available, `MasterStyleMatcher.Match()` in `Dalashade/MasterStyleMatcher.cs` returns deltas, diagnostics, rules, and color-family adjustments.
11. `Plugin.ScanPresetCompatibility()` runs `PresetWriter.ScanSupportedVariables()` and `PresetAnalyzer.Analyze()`.
    - First-party `Dalashade_*` shader sections are classified as known controlled Dalashade effects when they appear in preset content.
12. `PresetWriter.WriteGeneratedPreset()` reads the base preset and creates shader adjustments from `ShaderVariableMapper.CreateAdjustments()`.
    - If `EnableDalashadeCustomShaders` is enabled, `CustomShaderVariableMapper` can also write normalized `SceneIntent` values into matching Dalashade custom shader sections.
    - If `AutoInjectDalashadeCustomShaderSections` is also enabled, known Dalashade custom shader sections and variables can be inserted into the generated preset only.
    - Custom shader variable writes happen when the generated preset content contains matching Dalashade section/key lines, either from the base preset or from generated-preset-only injection.
    - `Dalashade_SmartSharpen.fx` receives extra authority-aware tuning from preset analysis so it behaves as a secondary, foliage-safe pass when other active sharpeners are present.
    - Dalashade does not append custom shader entries to `Techniques=`, copy `.fx` files, or require custom shaders for normal operation.
13. `GenerationAuthorityPolicy.From()` dampens secondary authorities for selected compatibility modes.
14. The writer edits only matching section/key lines, records `ChangedShaderVariable` entries, and applies `SanitizeActionPipeline` only when allowed by mode.
15. If backups are enabled and the generated preset already exists, `PresetWriter` creates and prunes backups.
16. `PresetWriter` writes a temporary file and replaces the generated preset path.
17. `Plugin.ReloadShadersIfNeeded()` optionally calls `ReShadeController.ReloadAfterPresetWrite()`.

## Pipeline Ownership

| Stage | Owner |
| --- | --- |
| Command/window lifecycle | `Dalashade/Plugin.cs` |
| UI buttons | `Dalashade/Windows/MainWindow.cs`, `Dalashade/Windows/ConfigWindow.cs` |
| Paths/config | `Dalashade/Configuration.cs`, `Plugin.InitializePresetFolders()`, `Plugin.ResolveEffectiveBasePresetPath()` |
| Context and tags | `Dalashade/GameContext.cs` |
| Scene intent and tag diagnostics | `Dalashade/SceneIntent.cs` |
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
