# Codex Editing Guide

Read this before editing Dalashade.

This guide is for future Codex runs and contributors making code changes in this repository.

## Inspection Order

Before editing, read:

1. `docs/CodebaseIndex.md`
2. `docs/GenerationPipeline.md`
3. The specific topic doc for the requested change.
4. This guide.

Then inspect the actual source files named by the docs. The docs are navigation help; source code remains the implementation authority.

## Rules

| Rule | Reason |
| --- | --- |
| Inspect existing files before changing behavior. | Dalashade has several linked systems, and guessing causes regressions. |
| Prefer small commits. | Compatibility work is easier to review when each change has one purpose. |
| Do not refactor unrelated systems. | Broad refactors make visual regressions hard to trace. |
| Preserve user-facing settings unless explicitly asked. | Existing configs may belong to active users. |
| Build after meaningful changes. | Compile errors are easy to introduce across UI, diagnostics, and mapper code. |
| Update docs when architecture changes. | Future edits depend on accurate navigation. |
| Update `docs/CommitChangelog.md` before committing Codex changes. | Git history says what changed; this file says why in plainspeak. |
| Avoid changing `repo.json` unless the task is explicitly a release task. | Bad release metadata breaks installs. |
| Avoid loose shader-key changes without diagnostics. | Generic keys often mean different things across shaders. |
| Do not implement a native ReShade bridge unless explicitly requested. | Current reload is hotkey-based. Bridge work is a separate system. |
| Do not add custom shaders during tag-system tasks unless explicitly requested. | Scene tags and shader files are separate responsibilities. |

## Documentation-Only Tasks

If the task says documentation-only:

1. Do not change runtime behavior.
2. Do not add `.fx` shaders.
3. Do not add bridge, IPC, named pipe, or native add-on code.
4. Do not alter release packaging.
5. Clearly label planned systems as planned.

Use this warning for planned pages:

```text
This system is planned and not currently implemented. Do not treat this document as an implementation reference yet.
```

## Task Routing

| Request | Read first |
| --- | --- |
| Change weather or tags | `docs/SceneTagsAndIntent.md`, then `Dalashade/GameContext.cs` and `Dalashade/VisualProfile.cs`. |
| Add shader variables | `docs/ShaderMapping.md`, then `Dalashade/ShaderVariableMapper.cs` and `Dalashade/PresetAnalyzer.cs`. |
| Fix generated presets | `docs/PresetWriting.md`, then `Dalashade/PresetWriter.cs`, `Dalashade/ShaderVariableMapper.cs`, and reports. |
| Improve master style | `docs/MasterStyle.md`, then `Dalashade/MasterStyle.cs`, `Dalashade/MasterStyleMatcher.cs`, and diagnostics UI. |
| Fix install/update | `docs/ReleaseChecklist.md`, then `repo.json`, `Dalashade/Dalashade.csproj`, and release zip contents. |
| Improve reload | `docs/ReShadeReload.md`, then `Dalashade/ReShadeController.cs` and reload UI. |
| Improve UI only | `Dalashade/Windows/MainWindow.cs`, `Dalashade/Windows/ConfigWindow.cs`, and UI helpers. Do not touch generation logic. |

## Ownership Boundaries

| Area | Owner |
| --- | --- |
| Raw game state | `GameContextService` |
| Scene tags | `SceneClassifier` |
| Profile values | `ProfileEngine` and `MasterStyleMatcher` |
| Shader variable mappings | `ShaderVariableMapper` |
| Preset file output | `PresetWriter` |
| Compatibility classification | `PresetAnalyzer` |
| Report export | `CompatibilityReportExporter` and `PresetRegressionReportHarness` |
| ReShade reload | `ReShadeController` |
| Base preset selection | `BasePresetLibrary` and `Plugin.ResolveEffectiveBasePresetPath` |

Do not move behavior across these boundaries without a specific architecture task.

## Final Checklist

Before finishing a change:

1. Did you inspect the relevant docs first?
2. Did you inspect the current source before editing?
3. Did you edit the smallest responsible file set?
4. Did you avoid unrelated refactors?
5. Did you preserve user-facing settings unless explicitly asked?
6. Did you avoid broad loose-key shader changes?
7. Did you build?
8. Did you update docs if behavior changed?
9. Did you add or update the `docs/CommitChangelog.md` entry for committed work?
10. Did you avoid release manifest changes unless this was a release task?
