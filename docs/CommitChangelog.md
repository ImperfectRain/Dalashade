# Commit Changelog

This file is the plainspeak change log for Codex-assisted work.

Codex should update this file whenever it makes code, shader, configuration, workflow, or documentation changes that are intended to be committed. The goal is not to replace Git history. The goal is to leave a short human-readable trail of what changed, why it changed, what larger goal it supports, what docs were updated, and what the next agent or maintainer should do next.

Use Unix timestamps so entries are easy to sort and compare across local time zones.

## Update Rules

- Add a new entry near the top of `Entries` before committing a code or behavior change.
- Use one entry per coherent change set, not one entry per file.
- Keep the language plain. A future maintainer should understand it without reading the diff first.
- Mention documentation updates directly, including when no docs needed to change.
- Mention verification honestly. If a build or test was not run, say so.
- Do not list generated build output or local debug folders unless they are intentionally committed.
- Keep `.codex-debug/` and other local investigation output out of this changelog unless the task explicitly makes them source artifacts.

## Entry Template

```text
### <unix timestamp> - <short title>

- Changed: <what changed in plain language>
- Why: <problem, goal, or user request>
- Related goals: <larger project direction or issue this supports>
- Documentation: <docs updated, or why none were needed>
- Verification: <builds, tests, manual checks, or not run>
- Next steps: <specific follow-up, or "None">
```

## Entries

### 1781755599 - Fix material calibration false positives

- Changed: Replaced loose territory substring checks with boundary-aware territory keyword matching, added a softer foliage/grass mismatch warning when visible evidence is high but Foliage intent is only modest, and added regression harness coverage for Labyrinthos water intent plus modest foliage warnings.
- Why: A real Labyrinthos debug bundle showed a false coastal/seaside WaterSpecular contribution and foliage evidence that was visually high but no longer produced a calibration warning.
- Related goals: Keep material evidence explainable, avoid cyan/sky/water false positives, and make MaterialIntent diagnostics catch underweighted foliage before shader tuning.
- Documentation: Updated `docs/CompatibilityAndDiagnostics.md`, `docs/SceneTagsAndIntent.md`, `docs/ShaderAuthoring.md`, and this changelog to match the current report columns and warning behavior.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` completed successfully with restore/up-to-date output; `git diff --check` passed with line-ending warnings only. Direct PowerShell reflection execution of `SceneTagRegressionHarness.Run()` was blocked by plugin dependency loading outside the Dalamud host.
- Next steps: Run `/dalashade regression` in-game and export a fresh Labyrinthos debug bundle to confirm the false coastal WaterSpecular contribution is gone.

### 1781751989 - Add safe material evidence controls

- Changed: Added User Mode `Use screenshot material hints` controls with safe wording and a limited strength slider, expanded Developer Mode screenshot evidence diagnostics with current MaterialIntent comparison, cap/strength context, and a copyable evidence block, and added regression coverage for default-off/disabled behavior.
- Why: Material evidence behavior needed to be accessible without making normal users manage low-level MaterialIntent channels.
- Related goals: Keep screenshot evidence scene-level, off by default, and explainable before using it for material calibration decisions.
- Documentation: Updated `docs/Configuration.md`, `docs/CompatibilityAndDiagnostics.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` completed successfully with restore/up-to-date output; `git diff --check` passed with line-ending warnings only.
- Next steps: Use real debug bundles to decide whether the User Mode strength range should stay capped at 0.6 or become more conservative.

### 1781751590 - Cap and explain material tag registry tuning

- Changed: Added MaterialIntent tag registry validation, per-tag and per-channel caps, registry diagnostics for active/inactive/invalid/capped rows, debug bundle JSON, report tables, developer UI display, and regression harness coverage.
- Why: Registry material tuning needed to be safer and explainable alongside screenshot material evidence before using it for more calibration work.
- Related goals: Keep material evidence scene-level, prevent unlimited user-authored tag stacking, and make tag-driven MaterialIntent changes auditable.
- Documentation: Updated `docs/CompatibilityAndDiagnostics.md`, `docs/DebugBundles.md`, `docs/CodebaseIndex.md`, `docs/SceneTagsAndIntent.md`, `docs/GenerationPipeline.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` completed successfully with restore/up-to-date output; `git diff --check` passed with line-ending warnings only.
- Next steps: Review real user tag preset exports and material calibration bundles before changing default registry amounts.

### 1781733623 - Add material calibration diagnostics

- Changed: Added a diagnostics-only Material Calibration report section, structured `material-calibration.json` debug bundle output, per-channel severity warnings, shader mapping availability checks, and a representative scene matrix checklist.
- Why: Material tuning needed one place to compare SceneTags/MaterialProfile, tag registry tuning, ScreenshotMaterialEvidence, MaterialIntent, shader key availability, and mismatch warnings without changing behavior.
- Related goals: Make material calibration explainable before changing formulas, keep screenshot evidence scene-level, and preserve shader-side MaterialMasks/FrameData ownership.
- Documentation: Updated `docs/CompatibilityAndDiagnostics.md`, `docs/DebugBundles.md`, `docs/CodebaseIndex.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` completed successfully; `git diff --check` passed with line-ending warnings only.
- Next steps: Use the calibration section with real scene/debug bundles before changing MaterialIntent caps or shader masks.

### 1781732658 - Add opt-in screenshot material evidence influence

- Changed: Added `EnableScreenshotMaterialEvidenceInfluence`, `ScreenshotMaterialEvidenceStrength`, a capped `ScreenshotMaterialEvidenceIntentAdapter`, MaterialIntent builder wiring, UI/report/debug diagnostics, and regression harness coverage for disabled behavior, caps, low confidence, water/sky/aether separation, sand/skin separation, snow, and aether/neon.
- Why: Phase 2 needs screenshot-derived broad material evidence to become a conservative scene-level MaterialIntent prior without pretending to detect true per-pixel material IDs.
- Related goals: Move from territory-only material guesses toward a material evidence pipeline while preserving shader-side MaterialMasks/FrameData ownership and opt-in generated output behavior.
- Documentation: Updated `README.md`, `docs/Configuration.md`, `docs/SceneTagsAndIntent.md`, `docs/GenerationPipeline.md`, `docs/CompatibilityAndDiagnostics.md`, `docs/DebugBundles.md`, `docs/MaterialIntent.md`, `docs/CodebaseIndex.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `git diff --check` passed with line-ending warnings only; `dotnet test Dalashade.sln` completed after sandbox escalation. Direct scratch execution of `SceneTagRegressionHarness.Run()` was blocked by missing Dalamud runtime assemblies outside the plugin host.
- Next steps: Test against more real shoreline, desert, snow, high-tech/aether, city/interior, and combat-UI screenshots before considering stronger default caps.

### 1781729319 - Calibrate screenshot material evidence against real screenshots

- Changed: Tuned diagnostic-only screenshot material evidence to reduce warm character/dialogue and foliage/stone sand false positives, kept non-context water conservative, and added regression harness coverage for warm character/dialogue not becoming SandDust plus blue sky not becoming water.
- Why: A real screenshot-folder sweep showed the Phase 1 analyzer was useful but too willing to raise SandDust warnings from skin, dialogue panels, warm lighting, beige stone, and foliage scenes.
- Related goals: Keep screenshot material evidence stable before any future MaterialIntent influence, make mismatch warnings more trustworthy, and preserve the diagnostic-only Phase 1 boundary.
- Documentation: Updated this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; sampled 40 screenshots from the configured Steam screenshot folder with the scratch analyzer.
- Next steps: Continue collecting real forest, shoreline, desert, snow, high-tech/aether, city/interior, and combat-UI screenshots before Phase 2 caps are allowed to affect MaterialIntent.

### 1781726018 - Add diagnostic screenshot material evidence

- Changed: Added `ScreenshotMaterialEvidence` and mismatch diagnostics, a separate screenshot material evidence analyzer, developer UI display, compatibility report output, debug bundle JSON output, and regression harness coverage for missing screenshots and visible foliage mismatches.
- Why: MaterialIntent calibration needed a middle diagnostic layer that can say what broad material families are visibly present in the latest screenshot without changing shader output.
- Related goals: Make material mapping more explainable, separate scene-level evidence from shader-side pixel masks, and prepare for future conservative MaterialIntent inputs after diagnostics prove useful.
- Documentation: Updated `README.md`, `docs/CodebaseIndex.md`, `docs/CompatibilityAndDiagnostics.md`, `docs/SceneTagsAndIntent.md`, `docs/DebugBundles.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `git diff --check` passed with line-ending warnings only; `dotnet test Dalashade.sln` completed after sandbox escalation.
- Next steps: Collect real screenshots to tune thresholds, then consider a later opt-in pass where high-confidence evidence conservatively influences MaterialIntent with caps.

### 1781722123 - Add optional Dalashade technique activation sync

- Changed: Added `SyncDalashadeTechniqueActivation`, generated-preset-only management of Dalashade production techniques in `Techniques=`, phase-ordered insertion into `Techniques=`/`TechniqueSorting=`, deactivation when controlling plugin shader options are off, diagnostics wording, and regression harness coverage.
- Why: Users need a quick way to add or remove the influence of first-party Dalashade `.fx` files without hand-editing every generated preset or disturbing third-party effects.
- Related goals: Make first-party shader use easier, keep preset generation non-destructive, preserve debug shaders as manual tools, and make generated stacks safer by default.
- Documentation: Updated `README.md`, `docs/PresetWriting.md`, `docs/Configuration.md`, `docs/GenerationPipeline.md`, `docs/ShaderAuthoring.md`, `docs/Shaders/ShaderSystemOverview.md`, `docs/CodebaseIndex.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `git diff --check` passed with line-ending warnings only; `dotnet test Dalashade.sln` completed after sandbox escalation.
- Next steps: Surface the exact synced technique list in compatibility reports if users need more audit detail, and test against real ReShade presets with missing shader files to confirm user-facing failure messages are clear.

### 1781718938 - Add optional generated preset load-order optimization

- Changed: Added `OptimizeGeneratedPresetLoadOrder`, a settings toggle, and a generated-preset-only optimizer for `Techniques=` and `TechniqueSorting=`. The optimizer preserves the same entries, reports moved entry positions in the write message, and has a scrambled-stack regression harness check.
- Why: Messy base preset stack order can make generated values less reliable even when no effects should be disabled.
- Related goals: Improve generated preset safety, make compatibility cleanup optional, and keep base presets non-destructive.
- Documentation: Updated `README.md`, `docs/PresetWriting.md`, `docs/Configuration.md`, `docs/GenerationPipeline.md`, `docs/CodebaseIndex.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors.
- Next steps: Expose order changes in compatibility reports if users need more than the write message, and tune phase rules against real messy preset examples.

### 1781718347 - Make screenshot analysis opinion-driven and strength-scaled

- Changed: Added named screenshot scene opinions, a `ScreenshotAnalysisStrength` slider, strength-scaled VisualProfile/SceneIntent/MaterialProfile/MaterialIntent influence, legible screenshot diagnostics in UI/reports/debug bundles, and synthetic image regression coverage in the existing harness.
- Why: Screenshot analysis had useful metrics but weak visibility and limited control; users needed opinions that actually affect output and a way to dial them back.
- Related goals: Make scene adaptation more explainable, keep optional analysis safe, improve debug data, and add tests before expanding image-driven behavior further.
- Documentation: Updated `README.md`, `docs/GenerationPipeline.md`, `docs/SceneTagsAndIntent.md`, `docs/Configuration.md`, `docs/CompatibilityAndDiagnostics.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors.
- Next steps: Add a proper test project when repo structure allows it, collect real screenshot pairs to tune opinion thresholds, and keep screenshot analysis below true material/segmentation claims until live frame or ReShade bridge data exists.

### 1781731543 - Add exact territory profiles for risky FFXIV zones

- Changed: Added exact built-in territory profiles for high-risk hubs, under-tagged field zones, field-operation zones, and Cosmic Exploration planets, with keyword rules kept as fallback. Added regression harness expectations for representative risky zones and area classification.
- Why: The audit showed many places players explore would fall to `neutral` or `field` despite having strong visual identities.
- Related goals: Make Dalashade aware of every explorable area, preserve place identity before visual math, and add regression coverage before expanding tag behavior further.
- Documentation: Updated `docs/SceneTagsAndIntent.md`, revised `docs/ZoneTagCoverageAudit.md` from pure audit to implemented-first-pass reference, and updated this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors. Direct PowerShell reflection execution of `SceneTagRegressionHarness.Run()` was blocked by runtime dependency loading outside the Dalamud/.NET 10 context.
- Next steps: Consider moving territory profiles out of `GameContext.cs` during the planned service split, add more exact-profile regression cases, and add territory id support if name matching proves fragile.

### 1781731175 - Audit FFXIV zone tag coverage

- Changed: Added a researched zone tag coverage audit comparing FFXIV persistent exploration zones, hubs, field operations, and Cosmic Exploration zones against Dalashade's current classifier.
- Why: The plugin should become aware of every area a player might explore and adapt visuals to the actual place identity instead of relying only on broad keyword matches.
- Related goals: Expand scene tagging coverage, add tests before major tag changes, make tag behavior diagnosable, and move toward a built-in territory profile registry with keyword fallback.
- Documentation: Added `docs/ZoneTagCoverageAudit.md` and linked it from the docs landing page and codebase index.
- Verification: Documentation-only research/audit change; no build run.
- Next steps: Implement exact territory profiles for high-risk hubs, under-tagged field zones, and exploration zones, then add regression harness cases before changing visual behavior.

### 1781730713 - Align README with current project scope and roadmap

- Changed: Reviewed the README against the current docs and expanded it with implemented scene authoring, optional first-party shaders, inline FrameData scope, project evolution, maintainability pressure, and a bottom roadmap.
- Why: The README was mostly accurate but lagged behind newer systems and did not clearly separate implemented behavior from WIP direction and future architecture ideas.
- Related goals: Keep public docs honest, keep future Codex agents aligned, and make roadmap priorities match current goals around maintainability, tag authoring, User/Developer Mode clarity, SceneGI, AdaptiveGrade, and SurfaceReflection caution.
- Documentation: Updated `README.md` and this changelog.
- Verification: Documentation-only change; no build run.
- Next steps: Use the roadmap as the README-level guide, while topic docs remain the implementation reference for code changes.

### 1781730521 - Add Codex commit changelog convention

- Changed: Added this changelog and linked it from the documentation entry points and Codex workflow guidance.
- Why: Documentation updates were at risk of drifting behind code changes, and future Codex sessions need an obvious place to record what changed and why.
- Related goals: Keep docs, handoff notes, and commit-time context aligned as the repo grows.
- Documentation: Updated the README, docs landing page, codebase index, Codex editing guide, and session handoff guidance. Added current maintainability notes for scene authoring, FrameData, SurfaceReflection, SceneGI, large-file refactor pressure, and mode boundaries.
- Verification: Documentation-only change; no build run.
- Next steps: Future Codex code-change passes should add a new entry here before committing.
