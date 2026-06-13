# Codebase Index

This map lists implemented files and their current responsibilities. Do not invent new ownership without checking the code first.

| Area | File path | Main classes/types | What it owns | Safe to edit? | Notes |
| --- | --- | --- | --- | --- | --- |
| Plugin entrypoint | `Dalashade/Plugin.cs` | `Plugin` | Dalamud services, command registration, window setup, generation orchestration, report export, reload calls, folder defaults | Yes, carefully | Do not add feature logic here if it belongs in a service/helper. |
| Configuration | `Dalashade/Configuration.cs` | `Configuration`, config enums | User settings, paths, modes, tuning fields | Yes | Preserve existing config fields unless explicitly migrating. |
| Main UI | `Dalashade/Windows/MainWindow.cs` | `MainWindow` | Runtime status, generation controls, diagnostics panels | Yes | UI only; avoid generation logic here. |
| Settings UI | `Dalashade/Windows/ConfigWindow.cs` | `ConfigWindow` | Paths, library dropdown, behavior settings, reload settings, tuning controls | Yes | UI should call helpers/services, not own algorithms. |
| UI section helper | `Dalashade/Windows/UiSection.cs` | `UiSection` | Collapsible section rendering | Yes | Keep generic. |
| Game context and tags | `Dalashade/GameContext.cs` | `GameContextService`, `SceneClassifier`, `GameContext`, `SceneTags` | Territory, weather, time, combat, duty, GPose, cutscene, biome tags | Yes, with build testing | Uses Dalamud/Lumina/FFXIVClientStructs data. |
| scene intent and tag diagnostics | `Dalashade/SceneIntent.cs` | `SceneIntent`, `SceneIntentBuilder`, `SceneIntentContribution`, `TagStackDiagnostics`, `TagStackContribution` | Stable normalized scene-language contract and tag-stack diagnostics | Yes, with build testing | Future shaders/bridge code should consume this model instead of scattered tag logic. |
| Material profile and intent diagnostics | `Dalashade/MaterialProfile.cs`, `Dalashade/MaterialProfileBuilder.cs`, `Dalashade/MaterialProfileContribution.cs`, `Dalashade/MaterialIntent.cs`, `Dalashade/MaterialIntentBuilder.cs`, `Dalashade/MaterialIntentContribution.cs` | `MaterialProfile`, `MaterialProfileBuilder`, `MaterialIntent`, `MaterialIntentBuilder` | Optional inferred scene material plausibility, material likelihoods, and contribution diagnostics | Yes, carefully | Zero-impact by default. MaterialProfile feeds MaterialIntent diagnostics; shader variables are written only when MaterialIntent shader mapping is explicitly enabled. |
| Screenshot/image analysis | `Dalashade/ImageAnalysis.cs` | `ImageAnalysisService`, `ImageAnalysisResult`, `ColorFamilyStats` | Screenshot sampling, luminance percentiles, tone/color-family stats | Yes, carefully | Image math affects master style and screenshot feedback. |
| Visual profile generation | `Dalashade/VisualProfile.cs` | `ProfileEngine`, `VisualProfile`, `AppliedRule` | Converts context/tags/image/master style into normalized target values | Yes, carefully | Central behavior file. Avoid unrelated refactors. |
| Master style selection | `Dalashade/MasterStyle.cs` | `MasterStyleService` | Finds/analyzes master images and selects reference style | Yes | Does not write presets. |
| Master style matching | `Dalashade/MasterStyleMatcher.cs` | `MasterStyleMatcher`, `MasterStyleMatchResult`, `MasterStyleProfileDeltas` | Applies master style tone/color/family deltas to profile values | Yes, carefully | Keep conservative; changes can affect many shaders. |
| Master diagnostics | `Dalashade/MasterStyleDiagnostics.cs` | `MasterStyleDiagnostics`, `MasterStyleTonalDeltas` | Effective strength, scene similarity, diagnostic values | Yes | UI/report surface depends on it. |
| Master tuning presets | `Dalashade/MasterStyleTuningPresets.cs` | `MasterStyleTuningPresets` | Subtle/Balanced/Strong/Cinematic/Aggressive GPose values | Yes | Keep UI-independent. |
| Preset analyzer | `Dalashade/PresetAnalyzer.cs` | `PresetAnalyzer`, `PresetTechnique`, `PresetRiskReport` | Techniques parsing, role/risk/support classification, authority detection | Yes, carefully | Detection/reporting only; does not write presets. |
| Preset writer | `Dalashade/PresetWriter.cs` | `PresetWriter`, `PresetWriteResult`, `ChangedShaderVariable`, `ShaderSupportScan` | Reads base preset, applies mapped values, writes generated preset, backups, support scan | Yes, carefully | Do not overwrite base preset. |
| Shader variable mapping | `Dalashade/ShaderVariableMapper.cs` | `ShaderVariableMapper`, `ShaderVariableDefinition`, `ShaderAdjustment` | Known shader section/key mappings, clamps, vector/scalar value math | Yes, carefully | Prefer strict section mappings. Avoid LooseKeys changes. |
| Custom shader intent mapping | `Dalashade/CustomShaderVariableMapper.cs` | `CustomShaderVariableMapper` | Writes normalized `SceneIntent` values into Dalashade custom shader sections when enabled | Yes, for future custom shader tasks. | No custom shader is required for normal operation. |
| Custom shader diagnostics | `Dalashade/CustomShaderBridgeDiagnostics.cs` | `CustomShaderBridgeDiagnosticsBuilder`, `CustomShaderBridgeDiagnostics` | Static bridge status for custom shader support, section/key detection, activation state, and written variables | Yes | Diagnostic only; do not add live IPC or auto-install behavior here. |
| Weather atmosphere shader prototype | `shaders/Dalashade_WeatherAtmosphere.fx` | ReShade technique `Dalashade_WeatherAtmosphere` | Custom `.fx` prototype for depth-aware haze, glow, weather mood, and highlight protection | Yes, for shader authoring tasks. | Manual ReShade shader-file install/use for now; generated-preset section injection is optional. |
| Adaptive grade shader prototype | `shaders/Dalashade_AdaptiveGrade.fx` | ReShade technique `Dalashade_AdaptiveGrade` | Custom `.fx` prototype for SceneIntent-driven exposure, contrast, saturation, temperature, highlight rolloff, shadow lift, and cinematic bias | Yes, for shader authoring tasks. | Manual ReShade shader-file install/use for now; generated-preset section injection is optional. |
| Atmosphere bloom shader prototype | `shaders/Dalashade_AtmosphereBloom.fx` | ReShade technique `Dalashade_AtmosphereBloom` | Custom `.fx` prototype for controlled atmospheric bloom with magic/neon tint, combat dampening, and highlight restraint | Yes, for shader authoring tasks. | Manual ReShade shader-file install/use for now; generated-preset section injection is optional. |
| Smart sharpen shader prototype | `shaders/Dalashade_SmartSharpen.fx` | ReShade technique `Dalashade_SmartSharpen` | Custom `.fx` prototype for conservative clarity that dampens haze, wet highlights, foliage shimmer, far-depth detail, and combat clutter | Yes, for shader authoring tasks. | Manual ReShade shader-file install/use for now; generated-preset section injection is optional. |
| Material debug shader utility | `shaders/Dalashade_MaterialDebug.fx`, `shaders/Dalashade_MaterialMasks.fxh` | ReShade technique `Dalashade_MaterialDebug`, MaterialMasks v2 helpers | Optional false-color screen-space material heuristic visualizer; shared raw/gated/final material masks with optional depth assist | Yes, for shader authoring tasks. | Manual ReShade shader-file install/use; disabled by default and never auto-activated. Depth assist is shader-owned and off by default. |
| Gameplay sanitize | `Dalashade/SanitizeActionPipeline.cs` | `SanitizeActionPipeline`, `SanitizeAction` | Separate GameplaySanitize-only reductions | Yes | Do not disable shaders unless clearly safe and requested. |
| Authority policy | `Dalashade/GenerationAuthorityPolicy.cs` | `GenerationAuthorityPolicy`, `CompatibilityRolePolicies` | Primary/secondary authority dampening and role policies | Yes, carefully | Changes alter how multiple shaders share roles. |
| Compatibility reports | `Dalashade/CompatibilityReportExporter.cs` | `CompatibilityReportExporter` | Markdown compatibility report export and mapping validation | Yes | Diagnostic only. Should not change generation. |
| Color-family diagnostics | `Dalashade/ColorFamilyComparisonRows.cs` | `ColorFamilyComparisonRows` | Shared color-family comparison rows for UI/report | Yes | Keep display logic consistent. |
| ReShade reload | `Dalashade/ReShadeController.cs` | `ReShadeController`, `ReloadDiagnostics`, `ReloadResult` | ReShade.ini detection, KeyReload sync, hotkey send/test reload | Yes, carefully | Current reload is best-effort. |
| Keybind capture | `Dalashade/Keybind.cs` | `Keybind`, `KeybindCapture` | Reload hotkey config/capture/format | Yes | Windows virtual key behavior. |
| Base preset library | `Dalashade/BasePresetLibrary.cs` | `BasePresetLibrary`, `BasePresetLibraryItem` | Scans top-level `.ini` files in the Base folder | Yes | Dropdown selection sets `Configuration.BasePresetPath`. |
| Regression reports | `Dalashade/PresetRegressionReportHarness.cs` | `PresetRegressionReportHarness`, `PresetRegressionSummary` | Scans a test folder and creates markdown regression reports | Yes | Does not require ReShade running. |
| Scene tag regression checks | `Dalashade/SceneTagRegressionHarness.cs` | `SceneTagRegressionHarness`, `SceneTagRegressionCase` | Verifies representative territory/weather/tag outputs and final profile clamps | Yes | Covers key FFXIV zone families for tag-system edits. |
| Plugin manifest | `Dalashade/Dalashade.json` | JSON manifest | Plugin metadata inside build output | Only for release tasks | Keep version/release text aligned. |
| Custom repo manifest | `repo.json` | JSON manifest | Dalamud custom repository metadata/download links | Only for release tasks | Verify URLs and zip names before changing. |
| Release zips | `releases/` | Zip files | Published plugin artifacts | Only for release tasks | Do not alter during normal code tasks. |
| CI | `.github/workflows/pr-build.yml` | GitHub Actions workflow | Restore, Release build, repo JSON, optional zip validation | Yes | Keep Windows runner because Dalamud path setup is Windows-oriented. |

## Common Tasks -> Files To Inspect First

| Task | Inspect first |
| --- | --- |
| Change weather behavior | `Dalashade/GameContext.cs`, `Dalashade/VisualProfile.cs`, `docs/SceneTagsAndIntent.md` |
| Add shader support | `Dalashade/ShaderVariableMapper.cs`, `Dalashade/PresetAnalyzer.cs`, `Dalashade/PresetWriter.cs`, `docs/ShaderMapping.md` |
| Add custom Dalashade shader work | `shaders/`, `Dalashade/CustomShaderVariableMapper.cs`, `docs/ShaderAuthoring.md` |
| Fix generated preset output | `Dalashade/PresetWriter.cs`, `Dalashade/ShaderVariableMapper.cs`, `Dalashade/CompatibilityReportExporter.cs` |
| Improve master style | `Dalashade/MasterStyle.cs`, `Dalashade/MasterStyleMatcher.cs`, `Dalashade/MasterStyleDiagnostics.cs`, `Dalashade/Windows/MainWindow.cs` |
| Fix ReShade reload | `Dalashade/ReShadeController.cs`, `Dalashade/Keybind.cs`, `docs/ReShadeReload.md` |
| Fix install/update | `repo.json`, `Dalashade/Dalashade.csproj`, `Dalashade/Dalashade.json`, `releases/`, `docs/ReleaseChecklist.md` |
| Improve UI layout | `Dalashade/Windows/MainWindow.cs`, `Dalashade/Windows/ConfigWindow.cs`, `Dalashade/Windows/UiSection.cs` |
| Add diagnostics/report output | `Dalashade/CompatibilityReportExporter.cs`, `Dalashade/PresetRegressionReportHarness.cs`, UI diagnostics in `Dalashade/Windows/MainWindow.cs` |
