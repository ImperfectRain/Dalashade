# Codebase Index

This index lists the significant files in the repository and the ownership boundaries around them. Use it before editing code so changes land in the correct layer.

Status meanings:

- **Stable:** core behavior or contract; edit with regression checks.
- **Experimental:** active shader/heuristic work; edit narrowly and verify debug views.
- **Debug-only:** diagnostics and reporting only; should not change visuals.
- **Release asset:** release metadata or packaged output; edit only for release tasks.

## Repository Root

| File path | Purpose | Runtime role | Inputs | Outputs | Main dependencies | Used by | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `.editorconfig` | Repository editor formatting rules. | Tooling metadata. | Editor/IDE support. | Consistent indentation/line-ending behavior where supported. | EditorConfig-aware tools. | Contributors. | Stable. |
| `.gitignore` | Git ignore rules. | Source control hygiene. | Local build/output artifacts. | Keeps generated/local files untracked. | Git. | Contributors. | Stable. |
| `.github/workflows/pr-build.yml` | Pull request build workflow. | CI validation. | Repository checkout. | Restore/build result. | GitHub Actions, `dotnet`. | Pull requests. | Stable. |
| `README.md` | User-facing project overview and setup notes. | Documentation only. | Current feature set. | Setup and scope guidance. | Docs pages. | Users/contributors. | Stable. |
| `LICENSE.md` | License text. | Legal metadata. | None. | License terms. | None. | Repository. | Stable. |
| `Dalashade.sln` | Visual Studio solution. | Build entry point. | C# project. | Build graph. | `Dalashade/Dalashade.csproj`. | `dotnet build`. | Stable. |
| `repo.json` | Dalamud custom repository manifest. | Release metadata. | Release zip URL/version. | Dalamud repo entry. | `releases/`. | Plugin installers. | Release asset. |
| `scripts/ValidateRelease.ps1` | Release validation helper. | Manual release check. | `repo.json`, release zips. | Validation output. | PowerShell. | Maintainers. | Stable. |

## Dalapad Addon Scaffold

| File path | Purpose | Runtime role | Inputs | Outputs | Main dependencies | Used by | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `DalapadAddon/README.md` | Safe addon direction and removal boundary. | Documentation only. | Dalapad bridge goals. | Addon implementation guidance, IPC status route, diagnostic control pipe, resource catalog, scan/pinned candidate debug visualization, optional shader include path, reserved live channel. | `docs/Dalapad.md`. | Future addon work. | Experimental scaffold, separate addon build only. |
| `DalapadAddon/CONTRACT.md` | Human-readable Dalapad bridge contract. | Documentation only. | Candidate G-buffer/depth goals. | Resource names, availability flags, IPC status fields, diagnostic control commands, diagnostic catalog schema, debug visualization schema, `Dalashade_Dalapad.fxh` shader gates, reserved realtime channel, safety rules. | `Dalashade/DalapadDiagnostics.cs` contract names. | Future addon work, diagnostics. | Experimental scaffold, separate addon build only. |
| `DalapadAddon/dalapad-addon-contract.json` | Machine-readable Dalapad bridge contract. | Documentation/data only. | Contract rows, IPC names, and diagnostic routes. | Versioned resource/flag/route contract. | None. | Future addon tools. | Experimental scaffold, not built. |
| `DalapadAddon/sample-status.json` | Example Dalapad Stage 1 status-file payload. | Documentation/data only. | IPC parser target. | Example resource catalog, synthetic debug visualization, and realtime-disabled state. | `Dalashade/DalapadDiagnostics.cs`. | Future addon work, diagnostics. | Experimental scaffold, not built. |
| `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp` | First-test native/ReShade addon source. | Not compiled by Dalashade. | Future separate addon project. | DLL load/unload status-file IPC, diagnostic control-pipe responses, optional ReShade registration when `reshade.hpp` is available, diagnostic resource rows, scan/pinned candidate bindings, addon-owned debug copies for `Dalapad_Debug.fx`, and disabled realtime channel state. | ReShade/native addon APIs when explicitly prototyped. | Future addon work. | Experimental scaffold, not wired into the plugin build. |

## C# Plugin Files

| File path | Purpose | Runtime role | Inputs | Outputs | Main dependencies | Used by | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Dalashade/Plugin.cs` | Plugin entry point and orchestration. | Registers Dalamud services/windows/commands and coordinates generation, scanning, reports, reload. | Dalamud services, config, UI commands. | Generated preset, UI state, reports, reload call. | Most service classes. | Dalamud runtime. | Stable. |
| `Dalashade/Dalashade.csproj` | Plugin project file. | Build/package config. | NuGet/Dalamud references. | Plugin assembly. | `packages.lock.json`. | `dotnet build`. | Stable. |
| `Dalashade/Dalashade.json` | Plugin manifest. | Dalamud metadata. | Version/name/author. | Plugin metadata. | Build output. | Dalamud. | Release asset. |
| `Dalashade/Configuration.cs` | Serialized user settings and enums. | Stores interface mode, paths, modes, first-party shader mode, first-party performance tier, shader mapping, MaterialIntent, NormalField, reload, debug/export settings. | UI edits, defaults. | Persisted configuration. | Dalamud config. | Plugin, UI, exporters, writer. | Stable. |
| `Dalashade/GameContext.cs` | Game state and scene tag classification. | Reads territory/weather/time/combat/duty/GPose/cutscene and derives tags. | Dalamud/Lumina/client state. | `GameContext`, `SceneTags`. | FFXIV/Dalamud APIs. | SceneIntent, MaterialProfile, reports. | Stable. |
| `Dalashade/SceneAuthoring/SceneAuthoringService.cs` | Scene authoring and tag registry service. | Loads/saves current-territory tag overrides and editable tag registry definitions, applies overrides after automatic classification, emits suppression maps/conflict warnings, and handles fixed-path import/export. | Plugin config folder, `GameContext`, detected `SceneTags`, UI edits. | Effective `SceneTags`, authoring state, override JSON, tag registry JSON. | System.Text.Json, SceneIntent, MaterialIntent. | Plugin, SceneAuthoringWindow, profile/material builders, reports. | Experimental. |
| `Dalashade/SceneAuthoring/SceneTagOverride.cs` | Scene authoring models. | Stores override sets, tag registry definitions/tunings, per-territory added/removed tag groups, primary biome overrides, warnings, and applied state. | JSON storage, UI edits. | Serializable override, registry, tuning, and diagnostics state. | SceneAuthoringService. | Plugin/UI/reports/profile generation. | Experimental. |
| `Dalashade/SceneIntent.cs` | SceneIntent and tag-stack diagnostics. | Converts scene tags/screenshot/style/performance into normalized intent channels. | `GameContext`, `SceneTags`, image analysis, config. | `SceneIntent`, contribution breakdowns. | `ImageAnalysis`, config. | Shader mappers, reports, VisualProfile. | Stable. |
| `Dalashade/VisualProfile.cs` | Visual profile generation. | Converts context/intent/style into target values for known shader variables. | Scene intent, screenshot, master style, config. | `VisualProfile`, applied rules. | SceneIntent, MasterStyle. | ShaderVariableMapper. | Stable. |
| `Dalashade/ImageAnalysis.cs` | Screenshot analysis. | Samples screenshots for luminance, contrast, saturation, clipping, regions, color families, and named scene opinions. | Screenshot files. | `ImageAnalysisResult` with metrics, regions, and opinions. | ImageSharp/system drawing utilities as available. | VisualProfile, SceneIntent, MaterialProfile, MaterialIntent, reports. | Stable. |
| `Dalashade/ScreenshotMaterialEvidence.cs` | Screenshot material-evidence layer. | Converts existing screenshot region/color/opinion metrics plus weak scene context into broad visible material-family evidence and mismatch warnings against current MaterialIntent. | ImageAnalysisResult, SceneTags, GameContext, MaterialIntent. | `ScreenshotMaterialEvidenceDiagnostics`. | ImageAnalysis, MaterialIntent. | UI/report/debug bundle. | Experimental, influence off by default. |
| `Dalashade/ScreenshotMaterialEvidenceIntentAdapter.cs` | Optional screenshot evidence to MaterialIntent adapter. | Turns broad screenshot material evidence into capped scene-level MaterialIntent contributions when explicitly enabled. | Configuration, TagStackDiagnostics, ScreenshotMaterialEvidence. | MaterialIntentContribution rows. | ScreenshotMaterialEvidence, MaterialIntent. | MaterialIntentBuilder. | Experimental, opt-in only. |
| `Dalashade/MasterStyle.cs` | Master image discovery/analysis. | Finds and analyzes reference images. | Master style folder/config. | Master style image metrics. | ImageAnalysis. | MasterStyleMatcher. | Stable. |
| `Dalashade/MasterStyleMatcher.cs` | Master-style deltas. | Compares current scene to master images and creates profile deltas. | Image analysis, master style metrics, config. | `MasterStyleMatchResult`. | MasterStyleDiagnostics. | VisualProfile. | Stable. |
| `Dalashade/MasterStyleDiagnostics.cs` | Master style report data. | Explains master style strength/similarity/deltas. | Master style results. | Diagnostics rows. | MasterStyleMatcher. | UI/report. | Stable. |
| `Dalashade/MasterStyleTuningPresets.cs` | Named master style tuning presets. | Provides preset parameter sets. | Config enum. | Strength/behavior defaults. | Configuration. | Config UI. | Stable. |
| `Dalashade/MaterialProfile.cs` | Scene material profile model. | Stores scene-level material plausibility family/tags/priors. | MaterialProfileBuilder. | Profile values. | None. | MaterialIntent/report. | Stable. |
| `Dalashade/MaterialProfileBuilder.cs` | Scene material profile builder. | Infers material family/priors from tags, weather, territory, screenshot, SceneIntent. | Scene tags, context, image stats. | `MaterialProfile`. | GameContext, SceneIntent. | MaterialIntent/report. | Experimental. |
| `Dalashade/MaterialProfileContribution.cs` | Profile contribution diagnostics. | Records positive/negative material profile evidence. | Builder events. | Contribution list. | MaterialProfileBuilder. | Reports/debug bundle. | Stable. |
| `Dalashade/MaterialIntent.cs` | MaterialIntent model. | Stores normalized scene material channels. | MaterialIntentBuilder. | Intent values. | None. | CustomShaderVariableMapper, reports. | Stable. |
| `Dalashade/MaterialIntentBuilder.cs` | MaterialIntent builder. | Builds material likelihoods and suppressions from tags/profile/context plus optional screenshot material-evidence contributions. | MaterialProfile, SceneIntent, tags, screenshot, optional evidence adapter rows. | `MaterialIntent`. | MaterialProfileBuilder, ScreenshotMaterialEvidenceIntentAdapter. | Shader mapping/report. | Experimental. |
| `Dalashade/MaterialIntentContribution.cs` | MaterialIntent diagnostics. | Records contribution reasons per material channel. | MaterialIntentBuilder. | Contribution rows. | MaterialIntentBuilder. | Reports/debug bundle. | Stable. |
| `Dalashade/MaterialTagRegistryDiagnostics.cs` | Material tag-registry safety and diagnostics. | Validates active MaterialIntent tag registry tunings, caps per-tag/per-channel registry influence, and reports active/inactive/invalid/capped rows. | TagStackDiagnostics, scene tag presets. | Registry contributions and diagnostics. | SceneAuthoring, MaterialIntent. | MaterialIntentBuilder, UI, report/debug exporters. | Experimental. |
| `Dalashade/MaterialCalibrationDiagnostics.cs` | Material calibration report model/builder. | Compares profile priors, tag registry tuning, screenshot material evidence, MaterialIntent values, shader mapping availability, and warnings per material channel. | Config, TagStackDiagnostics, MaterialProfile, ScreenshotMaterialEvidence, MaterialIntent, shader support, write result. | Calibration channel diagnostics and scene matrix. | MaterialIntent, report/debug exporters. | Compatibility report, debug bundle. | Diagnostics-only. |
| `Dalashade/ShaderVariableMapper.cs` | Known third-party shader variable mapping. | Maps `VisualProfile` to known ReShade section/key changes with clamps. | VisualProfile, preset sections. | `ShaderAdjustment` list. | PresetWriter. | Generated preset. | Stable. |
| `Dalashade/CustomShaderVariableMapper.cs` | First-party shader uniform mapping. | Maps SceneIntent, MaterialIntent, first-party shader mode, first-party performance tier, water/day/night/NormalField settings into Dalashade shader sections. | Config, SceneIntent, MaterialIntent, preset content. | Custom shader variable writes. | PresetWriter. | Generated preset. | Experimental. |
| `Dalashade/FirstPartyShaderRegistry.cs` | Read-only first-party shader metadata registry. | Centralizes shader family, section, technique, production/debug/manual, known generated uniform, debug uniform, and performance-tier uniform metadata for diagnostics and UI labels only. | Static first-party shader contract metadata. | Metadata rows for parity scans, report/bundle summaries, and User Mode labels. | CustomShaderVariableMapper contract, shader docs. | ShaderUniformParityDiagnostics, CompatibilityReportExporter, DebugBundleExporter, MainWindow. | P2 diagnostics-only; does not write presets. |
| `Dalashade/PresetWriter.cs` | Generated preset writer, custom section injector, optional Dalashade technique activation sync, and optional technique load-order optimizer. | Reads base preset, applies mapped values, optionally syncs first-party production techniques, optionally reorders technique lists, writes generated preset safely. | Base preset, VisualProfile, config, mappings. | Generated `.ini`, changed variable list, custom shader injection/sync result, load-order result, backups. | ShaderVariableMapper, CustomShaderVariableMapper. | Generate button. | Stable. |
| `Dalashade/PresetAnalyzer.cs` | Preset compatibility analysis. | Parses techniques/sections and classifies roles, risk, authorities, support. | Active/base preset text. | `PresetAnalysisResult`, warnings. | Shader definitions. | UI/report/debug bundle. | Stable. |
| `Dalashade/CompatibilityReportExporter.cs` | Markdown report exporter. | Generates compatibility, material parity, NormalField, FrameData, first-party depth-assist, stack, and diagnostics report. | Config, analysis, context, material intent, shader files. | Markdown report. | PresetAnalyzer, source scanners. | Export Report, DebugBundleExporter. | Debug-only. |
| `Dalashade/DebugBundleExporter.cs` | Debug bundle exporter. | Writes timestamped diagnostic folder and optional zip, including scene authoring, FrameData, depth-assist state, and first-party performance tier writes. | Config, context, reports, presets, shader files. | Bundle files and manifest. | CompatibilityReportExporter, path helpers. | Export Debug Bundle. | Debug-only. |
| `Dalashade/DalapadDiagnostics.cs` | Dalapad runtime metadata plus optional status-file/control-pipe IPC and developer resource shape probe. | Reports candidate FFXIVClientStructs RenderTargetManager/GBuffer/DepthStencil/Texture metadata and, when explicitly requested, redacted shape/nullability rows without exposing raw GPU handles. | Loaded runtime assemblies, optional `Dalapad/dalapad-status.json`, optional diagnostic control pipe, explicit Developer Mode shape probe request. | Dalapad diagnostics model, addon resource contract, IPC status, pipe health, resource catalog rows, resource shape rows, scan/pinned candidate status, debug visualization status, diagnostic routes, implementation options, backend steps. | Reflection, optional JSON status read, short-timeout diagnostic pipe, and opt-in `RenderTargetManager.Instance` shape observation only. | ConfigWindow Developer Mode, compatibility report, debug bundle. | Experimental diagnostic-only. |
| `shaders/Dalapad_Debug.fx` | Manual Dalapad debug visualization shader. | ReShade debug shader. | `Dalapad_DebugTexture`, scan slots, pinned candidates, and manual debug uniforms. | Pass-through, status, synthetic texture, render-layer candidates, channel, alpha, quad page, pinned water/reflection candidate, and missing/stale visual modes. | ReShade.fxh. | Manual ReShade testing only. | Experimental diagnostic-only. |
| `shaders/Dalashade_Dalapad.fxh` | Shared first-party Dalapad shader helper include. | Exposes semantic pinned addon resources behind global, local, and availability gates. | `DALAPAD_PINNED_*` ReShade resources, Dalapad availability uniforms, shader-local feature gates. | Normal-like, albedo-like, mask-like, emissive-like, and water/reflection-like evidence structs with zero-confidence fallback. | ReShade.fxh caller context. | `FrameData.fxh` surface merge and debug/bridge experiments. | Experimental optional production helper; must remain removable. |
| `Dalashade/DalapadIpcClient.cs` | Diagnostic Dalapad control-pipe client. | Sends a short-timeout `QueryStatus` request to `\\.\pipe\Dalapad.Control.v1` and parses one JSON response. | Optional separate Dalapad addon pipe. | `DalapadControlPipeStatus`, capability booleans, resource catalog rows, scan/pinned candidate state, and debug visualization state. | `System.IO.Pipes`. | DalapadDiagnostics, ConfigWindow, reports, debug bundles. | Experimental diagnostic-only; no raw handle or realtime shader authority. |
| `Dalashade/CustomShaderBridgeDiagnostics.cs` | Custom shader bridge diagnostics. | Reports shader section/key support, activation, and writes. | Preset content, config, changed variables. | Diagnostic summary. | PresetWriter, mapper. | UI/report. | Debug-only. |
| `Dalashade/ShaderFileLocator.cs` | ReShade shader file locator. | Infers ReShade shader search paths and checks whether first-party `.fx`/`.fxh` files are present. | Configured ReShade.ini, base/generated preset paths. | Shader file presence diagnostics. | ReShade path conventions. | User Mode effect cards, future reports. | Debug-only. |
| `Dalashade/GenerationAuthorityPolicy.cs` | Multi-authority policy. | Dampens secondary role owners according to compatibility mode. | Preset analysis, config. | Authority policy. | PresetAnalyzer. | ShaderVariableMapper/writer. | Stable. |
| `Dalashade/SanitizeActionPipeline.cs` | Gameplay sanitize actions. | Applies separate gameplay-safe reductions where allowed. | Preset analysis, config. | Sanitize action list/changes. | PresetWriter. | Generated preset. | Stable. |
| `Dalashade/ReShadeController.cs` | ReShade reload support. | Finds ReShade ini, syncs reload key, sends reload hotkey. | Config, ReShade.ini. | Reload result/diagnostics. | Keybind. | Plugin reload path. | Stable. |
| `Dalashade/Keybind.cs` | Keybind model/capture. | Stores and formats reload keybinds. | UI key capture. | Keybind config. | Windows virtual keys. | Config UI, ReShadeController. | Stable. |
| `Dalashade/BasePresetLibrary.cs` | Base preset scanner. | Finds selectable `.ini` presets in base folder. | Base preset folder. | Library items. | File system. | Config UI. | Stable. |
| `Dalashade/ColorFamilyComparisonRows.cs` | Color-family report rows. | Formats color-family comparison diagnostics. | Image/master style analysis. | Rows for UI/report. | ImageAnalysis. | UI/report. | Stable. |
| `Dalashade/PresetRegressionReportHarness.cs` | Preset regression harness. | Batch-runs preset analysis/report summaries over fixture folders. | Test preset folder. | Markdown regression summaries. | PresetAnalyzer. | Maintainers. | Debug-only. |
| `Dalashade/SceneTagRegressionHarness.cs` | Scene tag regression harness. | Checks representative territory/weather/tag/profile outputs plus scene-authoring behavior, screenshot opinions, screenshot material evidence, optional evidence influence, and generated-preset safety helpers. | Hard-coded cases. | Regression result/failures. | GameContext, SceneIntent, SceneAuthoringService, ScreenshotMaterialEvidenceAnalyzer. | Maintainers. | Debug-only. |
| `Dalashade/SmartSharpenAuthority.cs` | Sharpen authority helper. | Identifies how Dalashade SmartSharpen should behave with other sharpeners. | Preset analysis. | Authority diagnostic. | PresetAnalyzer. | Writer/report. | Stable. |
| `Dalashade/Windows/MainWindow.cs` | Main plugin UI. | Routes User vs Developer mode; User Mode shows workflow health/setup/look/effects, Developer Mode uses searchable tabs for status, pipeline, scene, mapping, and diagnostics. | Plugin services/results. | ImGui UI. | UiSection, Plugin. | User. | Stable. |
| `Dalashade/Windows/ConfigWindow.cs` | Settings UI. | Routes User vs Developer mode; User Mode shows curated setup/look/scene/effects/health settings, Developer Mode uses searchable tabs for setup, generation, mapping, scene data, and diagnostics. | Configuration. | ImGui UI and config updates. | UiSection, Plugin services. | User. | Stable. |
| `Dalashade/Windows/SceneAuthoringWindow.cs` | Scene authoring UI. | Separate window for viewing detected/effective tags, enabling overrides, adding/removing grouped current-scene tags, resetting current scene overrides, and editing tag registry tuning rows. | Plugin scene authoring state. | ImGui UI, override edits, tag registry edits. | SceneAuthoringService. | User/developer scene authoring. | Experimental. |
| `Dalashade/Windows/UiSection.cs` | UI section helper. | Renders collapsible sections consistently. | ImGui calls. | UI blocks. | Dalamud ImGui. | Windows. | Stable. |
| `Dalashade/Windows/UiPages/` | UI page registry primitives. | Defines mode-aware page metadata and shared rendering so User/Developer pages can be added consistently. | UI page delegates. | Collapsible UI sections. | UiSection. | MainWindow, ConfigWindow. | Stable. |
| `Dalashade/Windows/FeatureStatusCard.cs` | User-facing effect health card. | Renders first-party effect status from preset analysis and custom shader diagnostics. | Preset analysis, bridge diagnostics. | ImGui status rows. | PresetAnalyzer, CustomShaderBridgeDiagnostics. | MainWindow User Mode. | Stable. |
| `Dalashade/packages.lock.json` | NuGet lock file. | Dependency reproducibility. | Restore operation. | Locked packages. | `Dalashade.csproj`. | `dotnet restore/build`. | Stable. |

## Shader Files

| File path | Purpose | Runtime role | Inputs | Outputs | Main dependencies | Used by | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `shaders/.gitkeep` | Keeps the shader source folder present in empty checkouts. | Repository structure marker. | None. | Folder retention. | Git. | Contributors. | Stable. |
| `shaders/Dalashade_MaterialMasks.fxh` | Shared material/water/safety resolver contract. | Converts color/depth/uniforms into material resolves and debug colors. | Backbuffer, depth, scene/material priors. | `MaterialResolve`, `WaterResolve`, `SafetyResolve`, debug colors. | `ReShade.fxh`. | `FrameData.fxh`, MaterialDebug, NormalDebug. | Experimental contract. |
| `shaders/Dalashade_NormalField.fxh` | Optional inferred NormalField contract. | Produces depth/detail/texture-relief/combined normals and receiver candidates. | Backbuffer, depth, material/water/safety resolves, NormalField settings. | `Dalashade_NormalField`, debug colors. | `MaterialMasks.fxh`. | `FrameData.fxh`, `Dalashade_NormalDebug.fx`. | Experimental/debug-first. |
| `shaders/Dalashade_FrameData.fxh` | Internal FrameData wrapper contract. | Packages canonical material/water/safety/receiver resolves, shared scene/tag interpretation, optional NormalField data, and gated Dalapad surface data into shared structs; lower performance tiers reduce optional inferred surface influence before consumers use it. | Backbuffer, depth, material/water/depth/NormalField/Dalapad settings, scene tag settings. | `FrameBaseData`, `FrameSurfaceData`, `FrameSceneData`, merged `SurfaceDataInfluence`. | `MaterialMasks.fxh`, `NormalField.fxh`, `Dalashade_Dalapad.fxh`. | `Dalashade_FrameDataDebug.fx`, production first-party shaders. | Experimental internal contract. |
| `shaders/Dalashade_AdaptiveGrade.fx` | Base tonal adaptation shader. | Applies scene/day/night/material-aware tone and color adjustments; performance tiers only affect optional shared surface influence. | Scene/material/day/night uniforms, FrameData base/surface data. | Graded color and debug masks. | `FrameData.fxh`. | ReShade technique `Dalashade_AdaptiveGrade`. | Production-oriented. |
| `shaders/Dalashade_SceneGI.fx` | Screen-space GI/AO impression. | Applies conservative AO, bounce, source pooling, optional contact shadows, and optional FrameData surface structure assist; lower performance tiers reduce diffuse-gather taps and generated GI distance/radii. | FrameData base/surface/scene data, optional Dalapad surface influence through FrameData, depth, GI/contact-shadow sliders. | GI/AO modified color and debug masks, including contact shadow, Dalapad contribution, FrameData evidence, bridge gate, and raw pinned-normal views. | `FrameData.fxh`. | ReShade technique `Dalashade_SceneGI`. | Experimental production. |
| `shaders/Dalashade_ScreenShadows.fx` | Optional source-aware screen-space shadows. | Applies subtle visible-source shadow impressions from source, occluder, receiver, FrameData, and optional gated Dalapad evidence; lower performance tiers reduce reach and tap cost. | FrameData base/surface/scene data, depth, source sensitivity, shadow strength/reach/softness, optional Dalapad albedo/emissive candidate evidence. | Shadowed color and debug masks for source, occluder, receiver, Dalapad evidence, and final contribution. | `FrameData.fxh`. | ReShade technique `Dalashade_ScreenShadows`. | Experimental production, default-off. |
| `shaders/Dalashade_ContactTone.fx` | Local contact tone shader. | Applies grounded edge darkening and local material readability contrast; lower performance tiers reduce generated radius and shared optional surface influence. | FrameData base/surface/scene data, depth, contact tone sliders. | Contact-toned color and debug masks. | `FrameData.fxh`. | ReShade technique `Dalashade_ContactTone`. | Experimental production. |
| `shaders/Dalashade_SurfaceReflection.fx` | Pseudo-SSR and material reflection simulation. | Applies water/wet/metal/aether reflection projections and source-qualified pseudo SSR; lower performance tiers skip optional far/wide projection and extra pseudo-SSR helper samples. | FrameData base/surface/scene data, depth, reflection sliders. | Reflected color contribution and debug modes. | `FrameData.fxh`. | ReShade technique `Dalashade_SurfaceReflection`. | Experimental production. |
| `shaders/Dalashade_AtmosphereBloom.fx` | Material-aware atmospheric bloom. | Applies restrained bloom/glow from qualified sources; lower performance tiers skip the far bloom gather ring. | FrameData base/surface data, bloom sliders, scene uniforms. | Bloomed color and debug masks. | `FrameData.fxh`. | ReShade technique `Dalashade_AtmosphereBloom`. | Production-oriented. |
| `shaders/Dalashade_WeatherAtmosphere.fx` | Weather/air/haze shader. | Applies scene/weather/day/night/coastal/heat/snow/fog air; performance tiers only affect optional shared surface influence. | FrameData base/surface data, depth, weather uniforms. | Atmospheric color contribution and debug masks. | `FrameData.fxh`. | ReShade technique `Dalashade_WeatherAtmosphere`. | Experimental production. |
| `shaders/Dalashade_SmartSharpen.fx` | Material-aware sharpen shader. | Applies controlled clarity with material/safety dampening; performance tiers only affect optional shared surface/detail influence. | FrameData base/surface data, sharpen sliders, scene uniforms. | Sharpened color and debug masks. | `FrameData.fxh`. | ReShade technique `Dalashade_SmartSharpen`. | Production-oriented. |
| `shaders/Dalashade_MaterialDebug.fx` | Shared material truth viewer. | Visualizes material, water, safety, receiver, and competition masks. | Shared material uniforms, depth, debug mode. | False-color debug output. | `MaterialMasks.fxh`. | ReShade technique `Dalashade_MaterialDebug`. | Debug-only. |
| `shaders/Dalashade_NormalDebug.fx` | NormalField truth viewer. | Visualizes inferred normals, structure, receivers, and safety. | Shared material uniforms, NormalField settings, depth. | False-color debug output. | `MaterialMasks.fxh`, `NormalField.fxh`. | ReShade technique `Dalashade_NormalDebug`. | Debug-only. |
| `shaders/Dalashade_FrameDataDebug.fx` | FrameData contract truth viewer. | Visualizes FrameData safety, water, material, receiver, source-vs-receiver, ambiguity, surface, Dalapad surface influence, and parity views. | Shared material uniforms, depth, NormalField/Dalapad settings, debug mode. | False-color debug output. | `FrameData.fxh`. | ReShade technique `Dalashade_FrameDataDebug`. | Debug-only. |

## Documentation Files

| File path | Purpose | Used by | Status |
| --- | --- | --- | --- |
| `docs/README.md` | Documentation landing page. | Contributors/Codex. | Stable. |
| `docs/CodebaseIndex.md` | File ownership and audit map. | Contributors/Codex. | Stable. |
| `docs/Audits/ArchitectureQualityPerformanceAudit.md` | Current architecture, UX parity, shader contract, diagnostics, release, and performance audit. | Release planning/refactor planning. | Stable audit doc. |
| `docs/ConfigurationParity.md` | Manual parity table for config visibility, generated writes, reports, bundles, shader impact, and Dalapad impact. | Config/UI/report parity work. | Stable audit support doc. |
| `docs/DiagnosticsModelPlan.md` | Shared diagnostic DTO plan for future compatibility report/debug bundle cleanup. | Diagnostics refactor planning. | Stable plan doc. |
| `docs/ShaderContractQuickReference.md` | Short first-party shader contract and truth-boundary reference. | Shader authors/Codex. | Stable contract doc. |
| `docs/ArchitectureRefactorPlan.md` | Behavior-preserving refactor roadmap for preset writing, shader registry, diagnostics, and scene/material services. | Refactor planning. | Stable plan doc. |
| `docs/FirstPartyShaderRegistry.md` | Read-only first-party shader registry boundary and consumer reference. | Diagnostics/UI metadata work. | Stable P2 support doc. |
| `docs/GenerationPipeline.md` | Generate-button to preset-write flow. | Maintainers/Codex. | Stable. |
| `docs/SceneTagsAndIntent.md` | Scene tags, night/day layers, SceneIntent behavior. | Scene/tag work. | Stable. |
| `docs/ZoneTagCoverageAudit.md` | Researched FFXIV zone coverage gaps for current tag classifier. | Scene/tag work. | Stable audit doc. |
| `docs/MaterialIntent.md` | MaterialProfile/MaterialIntent and shader-side material distinction. | Material/shader work. | Stable. |
| `docs/ShaderMapping.md` | Known shader variable mapping behavior. | Preset writer work. | Stable. |
| `docs/ShaderAuthoring.md` | First-party shader authoring notes. | Shader work. | Stable. |
| `docs/Shaders/ShaderSystemOverview.md` | First-party shader stack and shared contracts. | Shader work. | Stable. |
| `docs/Shaders/FrameData.md` | Internal FrameData wrapper contract reference. | Future first-party shader contract work. | Experimental internal contract doc. |
| `docs/Shaders/MaterialMasks.md` | Shared material/water/safety contract reference. | Material/shader work. | Experimental contract doc. |
| `docs/Shaders/NormalField.md` | Shared NormalField include contract reference. | NormalField work. | Experimental contract doc. |
| `docs/Shaders/AdaptiveGrade.md` | AdaptiveGrade shader reference. | Shader work. | Stable. |
| `docs/Shaders/AtmosphereBloom.md` | AtmosphereBloom shader reference. | Shader work. | Stable. |
| `docs/Shaders/MaterialDebug.md` | MaterialDebug shader reference. | Debug work. | Debug-only. |
| `docs/Shaders/NormalDebug.md` | NormalDebug shader reference. | Debug work. | Debug-only. |
| `docs/Shaders/SceneGI.md` | SceneGI shader reference. | Shader work. | Experimental. |
| `docs/Shaders/ScreenShadows.md` | ScreenShadows shader reference. | Shader work. | Experimental. |
| `docs/Shaders/ContactTone.md` | ContactTone shader reference. | Shader work. | Experimental. |
| `docs/Shaders/SmartSharpen.md` | SmartSharpen shader reference. | Shader work. | Stable. |
| `docs/Shaders/SurfaceReflection.md` | SurfaceReflection shader reference. | Reflection work. | Experimental. |
| `docs/Shaders/WeatherAtmosphere.md` | WeatherAtmosphere shader reference. | Weather shader work. | Experimental. |
| `docs/NormalField.md` | User-facing NormalField config/test plan. | NormalField diagnostics. | Stable. |
| `docs/Dalapad.md` | Optional Dalapad diagnostic probe boundary, addon scaffold, removal plan, and staged G-buffer bridge route. | Future external surface-data research. | Experimental diagnostic-only. |
| `docs/DebugBundles.md` | Debug bundle content and failure model. | Export/report work. | Stable. |
| `docs/Configuration.md` | Configuration field groups and safety rules. | Config/UI work. | Stable. |
| `docs/SafetyAndScope.md` | Plugin scope, non-automation, export safety. | Review/release work. | Stable. |
| `docs/CompatibilityAndDiagnostics.md` | Compatibility report and diagnostic panels. | Report/debug work. | Stable. |
| `docs/PresetWriting.md` | Generated preset write rules. | Preset writer work. | Stable. |
| `docs/ReShadeReload.md` | Reload hotkey behavior. | Reload work. | Stable. |
| `docs/MasterStyle.md` | Master style system. | Style work. | Stable. |
| `docs/ReleaseChecklist.md` | Release process. | Release tasks. | Stable. |
| `docs/CodexEditingGuide.md` | Repo-specific Codex editing rules. | Codex runs. | Stable. |
| `docs/CodexSessionHandoff.md` | Handoff notes. | Codex runs. | Stable. |
| `docs/CommitChangelog.md` | Plainspeak Codex-maintained change log for committed work. | Codex runs/maintainers. | Stable. |

## Test Fixtures and Release Support

| File path | Purpose | Runtime role | Inputs | Outputs | Used by | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `test-presets/free-shader-fixtures/Okami-like.ini` | Representative Okami-style free shader preset fixture. | Test data. | Preset analyzer. | Regression output. | Regression harness. | Stable. |
| `test-presets/free-shader-fixtures/WiFi-like.ini` | Representative WiFi-style free shader preset fixture. | Test data. | Preset analyzer. | Regression output. | Regression harness. | Stable. |
| `test-presets/free-shader-fixtures/ipsusuGameplay-like.ini` | Representative ipsusu gameplay-like preset fixture. | Test data. | Preset analyzer. | Regression output. | Regression harness. | Stable. |
| `test-presets/free-shader-fixtures/ipsusuQuesting-like.ini` | Representative ipsusu questing-like preset fixture. | Test data. | Preset analyzer. | Regression output. | Regression harness. | Stable. |
| `test-presets/custom-shader-fixtures/DalashadeWeatherAtmosphere.ini` | Custom shader fixture. | Test data. | Preset analyzer/writer. | Regression output. | Regression harness. | Stable. |
| `releases/Dalashade-v1.zip` | Packaged Dalashade v1 release artifact. | Release artifact. | Built plugin output. | Installable zip. | Users/Dalamud repo. | Release asset. |
| `releases/Dalashade-v1.1.zip` | Packaged Dalashade v1.1 release artifact. | Release artifact. | Built plugin output. | Installable zip. | Users/Dalamud repo. | Release asset. |

## Common Tasks -> Files To Inspect First

| Task | Inspect first |
| --- | --- |
| Change scene/time/weather tags | `Dalashade/GameContext.cs`, `Dalashade/SceneIntent.cs`, `docs/SceneTagsAndIntent.md` |
| Change broad tonal output | `Dalashade/VisualProfile.cs`, `Dalashade/ShaderVariableMapper.cs`, `shaders/Dalashade_AdaptiveGrade.fx` |
| Change MaterialIntent priors | `Dalashade/MaterialProfileBuilder.cs`, `Dalashade/MaterialIntentBuilder.cs`, `docs/MaterialIntent.md` |
| Change shader pixel material classification | `shaders/Dalashade_MaterialMasks.fxh`, `docs/Shaders/MaterialMasks.md`, `shaders/Dalashade_MaterialDebug.fx` |
| Change SurfaceReflection visuals | `shaders/Dalashade_SurfaceReflection.fx`, `docs/Shaders/SurfaceReflection.md`, MaterialDebug modes 58-59 |
| Change NormalField diagnostics | `shaders/Dalashade_NormalField.fxh`, `shaders/Dalashade_NormalDebug.fx`, `docs/Shaders/NormalField.md` |
| Change Dalapad diagnostics | `Dalashade/DalapadDiagnostics.cs`, `Dalashade/CompatibilityReportExporter.cs`, `Dalashade/DebugBundleExporter.cs`, `Dalashade/Windows/ConfigWindow.cs`, `docs/Dalapad.md` |
| Change Dalapad addon scaffold | `DalapadAddon/README.md`, `DalapadAddon/CONTRACT.md`, `DalapadAddon/dalapad-addon-contract.json`, `DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp`, `shaders/Dalapad_Debug.fx`, `shaders/Dalashade_Dalapad.fxh`, `docs/Dalapad.md` |
| Change generated preset output | `Dalashade/PresetWriter.cs`, `Dalashade/ShaderVariableMapper.cs`, `Dalashade/CustomShaderVariableMapper.cs`, `docs/PresetWriting.md` |
| Change first-party shader metadata/parity labels | `Dalashade/FirstPartyShaderRegistry.cs`, `Dalashade/ShaderUniformParityDiagnostics.cs`, `docs/FirstPartyShaderRegistry.md` |
| Change compatibility reports | `Dalashade/CompatibilityReportExporter.cs`, `docs/CompatibilityAndDiagnostics.md` |
| Change debug bundle export | `Dalashade/DebugBundleExporter.cs`, `docs/DebugBundles.md` |
| Change config fields/UI | `Dalashade/Configuration.cs`, `Dalashade/Windows/ConfigWindow.cs`, `Dalashade/Windows/MainWindow.cs`, `Dalashade/Windows/UiPages/`, `docs/Configuration.md` |
| Check config/UI/diagnostic parity | `docs/ConfigurationParity.md`, `Dalashade/Configuration.cs`, `Dalashade/Windows/ConfigWindow.cs`, `Dalashade/CompatibilityReportExporter.cs`, `Dalashade/DebugBundleExporter.cs` |
| Add or change first-party shader contracts | `docs/ShaderContractQuickReference.md`, `shaders/Dalashade_FrameData.fxh`, `shaders/Dalashade_MaterialMasks.fxh`, `shaders/Dalashade_NormalField.fxh`, `shaders/Dalashade_Dalapad.fxh` |
| Plan diagnostics/exporter cleanup | `docs/DiagnosticsModelPlan.md`, `Dalashade/CompatibilityReportExporter.cs`, `Dalashade/DebugBundleExporter.cs` |
| Change release packaging | `repo.json`, `Dalashade/Dalashade.csproj`, `Dalashade/Dalashade.json`, `releases/`, `docs/ReleaseChecklist.md` |

## Files intentionally not documented in detail

Generated binaries, build intermediates, local ReShade preset outputs, and `.codex-remote-attachments/` are not documented as repository behavior. They are local or generated artifacts and should not be used as source-of-truth design references.
