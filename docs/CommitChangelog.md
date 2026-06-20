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

### 1781919472 - Add Dalapad synthetic debug visualization bridge

- Changed: Added `shaders/Dalapad_Debug.fx` and extended the Dalapad addon to upload a synthetic 256x256 RGBA debug texture into `Dalapad_DebugTexture` through ReShade's effect runtime. The addon now reports `debugVisualization` status over the status file and control pipe, and the plugin surfaces that status in Developer Mode, compatibility reports, and debug bundles.
- Why: The typed shape probe proved candidate XIV render-layer pointers and dimensions, but the next safe visual step is proving addon-to-FX texture updates with generated pixels before copying or binding any game render target.
- Related goals: Prepare for a future debug-only render-layer visualization while keeping real G-buffer/depth copies, shader-resource registration, FrameData influence, and realtime uniform movement disabled.
- Documentation: Updated `DalapadAddon/README.md`, `DalapadAddon/CONTRACT.md`, `DalapadAddon/dalapad-addon-contract.json`, `DalapadAddon/sample-status.json`, `docs/Dalapad.md`, `docs/CodebaseIndex.md`, and this changelog.
- Verification: `Get-Content -Raw DalapadAddon\sample-status.json | ConvertFrom-Json` and `Get-Content -Raw DalapadAddon\dalapad-addon-contract.json | ConvertFrom-Json` passed; `clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64` rebuilt the addon; `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with LF-to-CRLF warnings only.
- Next steps: Install/reload `Dalapad_Debug.fx`, copy/reload the rebuilt addon, run Developer Mode > Dalapad diagnostics, and confirm `debugVisualization.syntheticTextureUploaded=true` before any real render-target copy work.

### 1781918628 - Make Dalapad shape probe use typed ClientStructs

- Changed: Reviewed three in-game debug bundles and updated the developer-only Dalapad resource shape probe so it tries a narrow typed ClientStructs path before falling back to reflection. The typed path reads `RenderTargetManager.Instance()`, candidate `GBuffers[0]`, `GBuffers[2]`, `DepthStencil`, and `Texture.AllocatedWidth/AllocatedHeight` only, with redacted pointer state.
- Why: The bundles proved status-file IPC, control-pipe IPC, metadata catalog rows, and the opt-in shape gate were healthy, but the reflection-only probe invoked `Instance` without observing any candidate pointers because it could not dereference the unsafe pointer shape.
- Related goals: Get reliable diagnostic evidence for candidate render-layer shape before any resource copying, shader-resource registration, debug visualization, or FrameData influence.
- Documentation: Updated `docs/Dalapad.md`, `docs/CodexSessionHandoff.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors after the typed probe change. Full `dotnet test` and `git diff --check` should be rerun before commit.
- Next steps: Reload the plugin build in-game, enable Developer Mode > Dalapad resource shape probe, run it, export a new debug bundle, and confirm whether typed rows observe non-null candidate pointers and dimensions.

### 1781917220 - Add Dalapad developer resource shape probe

- Changed: Added an explicit Developer Mode Dalapad resource shape probe. The default diagnostics still stay metadata/control-pipe only, while the opt-in probe may invoke `RenderTargetManager.Instance` and reports redacted candidate shape rows for normal, diffuse, and depth candidates without copying, sampling, registering, or exposing textures.
- Why: The Stage 1.2 IPC and metadata-only catalog path was healthy enough to start the next safe evidence pass before any render-layer bridge or debug visualization work.
- Related goals: Determine whether candidate render-layer resources can be observed consistently before attempting native resource registration, shader sampling, FrameData consumption, or production visual influence.
- Documentation: Updated `docs/Dalapad.md`, `docs/CodebaseIndex.md`, `docs/CodexSessionHandoff.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors during implementation. Full `dotnet test` and `git diff --check` should be rerun before commit.
- Next steps: In-game, run Developer Mode > Dalapad default diagnostics, enable the developer-only resource shape probe, run it, export a debug bundle, then repeat across login, zone change, resolution change, ReShade reload, and plugin reload before any resource copy or shader exposure work.

### 1781915259 - Add Dalapad metadata-only resource catalog

- Changed: Added Stage 1.2 metadata-only resource catalog rows to the Dalapad addon status file and `QueryStatus` pipe response. The plugin now parses catalog rows from both IPC paths, shows them in Developer Mode, includes them in compatibility reports and debug bundles, and updates health-check next steps when the catalog is healthy.
- Why: The status-file and control-pipe handshakes are validated; the next safe layer is proving a stable resource catalog schema without touching live render targets.
- Related goals: Prepare for a later developer-only pointer/resource shape probe while keeping all render-target reads, copies, shader-resource registration, realtime values, and FrameData influence disabled.
- Documentation: Updated `DalapadAddon/CONTRACT.md`, `DalapadAddon/README.md`, `DalapadAddon/dalapad-addon-contract.json`, `DalapadAddon/sample-status.json`, `docs/Dalapad.md`, `docs/CodebaseIndex.md`, `docs/CodexSessionHandoff.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64` rebuilt the addon. Full `dotnet test` and `git diff --check` should be rerun before commit.
- Next steps: Close the game if loaded, copy the rebuilt addon into the game folder, relaunch, export a debug bundle, and confirm both status-file and control-pipe resource catalog rows appear with unavailable/zero-confidence values.

### 1781912998 - Fix Dalapad status JSON pipe escaping

- Changed: Escaped the diagnostic control-pipe path when the addon writes `dalapad-status.json`, and updated Dalapad endpoint wording so diagnostics describe the current short-timeout control pipe instead of a future-only pipe.
- Why: The first in-game Stage 1.1 debug bundle proved the control pipe was healthy, but the status file was invalid JSON because `\\.\pipe\Dalapad.Control.v1` was written without JSON escaping.
- Related goals: Complete the safe status-file plus control-pipe IPC handshake before any resource catalog or render-target work.
- Documentation: Updated this changelog. No user docs needed because the contract/sample JSON were already correct.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64` rebuilt the addon; `git diff --check` passed with Git LF-to-CRLF warnings only. Copying the fixed addon to the game folder was blocked because the game currently has `Dalapad.addon64` loaded.
- Next steps: Close the game, copy the rebuilt addon to the game folder, relaunch, and export one more debug bundle to confirm `IpcStatus` becomes contract-compatible while the pipe remains healthy.

### 1781906404 - Add diagnostic Dalapad control pipe IPC

- Changed: Added a diagnostic-only `\\.\pipe\Dalapad.Control.v1` path. The addon now starts a worker-thread pipe server that answers `Ping`, `BridgeSelfTest`, `QueryStatus`, and `QueryCapabilities`; the plugin now has a short-timeout pipe client, control-pipe health diagnostics, capability rows, next-step diagnosis, compatibility-report output, and debug-bundle output.
- Why: The addon direction needed the next safe IPC layer after status-file handshaking, without sending shader values, render-target pointers, texture handles, or resource data.
- Related goals: Prove live plugin-to-addon communication before any resource catalog, render-target bridge, or realtime shader adaptation work.
- Documentation: Updated `DalapadAddon/CONTRACT.md`, `DalapadAddon/README.md`, `DalapadAddon/dalapad-addon-contract.json`, `DalapadAddon/sample-status.json`, `docs/Dalapad.md`, `docs/CodebaseIndex.md`, `docs/CodexSessionHandoff.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64` rebuilt the addon; `git diff --check` passed with Git LF-to-CRLF warnings only.
- Next steps: Load the rebuilt addon in-game, press `Probe Dalapad diagnostics`, confirm status-file IPC and control-pipe capability negotiation are both healthy, and keep resources unavailable before starting metadata-only resource catalog work.

### 1781904284 - Match Dalapad addon registration to installed ReShade

- Changed: Updated the Stage 1 Dalapad addon source so it registers with ReShade add-on API version `18` directly instead of using the vendored SDK helper version `20`. Added the requested API version to the status JSON and documented the ReShade `6.7.3.2148` compatibility note.
- Why: In-game ReShade logs showed `Failed to register add-on, because the requested API version (20) is not supported (18)!`, then unloaded `Dalapad.addon64`.
- Related goals: Keep the Stage 1 addon limited to load/unload and status-file IPC while making the handshake testable on the installed ReShade runtime.
- Documentation: Updated `DalapadAddon/README.md`, `docs/Dalapad.md`, `docs/CodexSessionHandoff.md`, and this changelog.
- Verification: Rebuilt `DalapadAddon/build/Dalapad.addon64` with `clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64`; copied it to the FFXIV game folder and confirmed matching SHA-256 hashes; `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only.
- Next steps: Relaunch the game and confirm ReShade registers `Dalapad.addon64` using API version `18` while all resource rows remain unavailable.

### 1781885055 - Prepare Dalapad handoff and addon build

- Changed: Updated handoff docs, Dalapad docs, addon docs, and the commit log to reflect the current Stage 1 Dalapad state. Cleaned the addon external folder so the ReShade SDK headers are vendored as plain build headers instead of a nested checkout, documented the header source, and kept the local Stage 1 test addon artifact at `DalapadAddon/build/Dalapad.addon64`.
- Why: The repo is being handed to a new Codex instance and needs an accurate split between implemented diagnostics, built addon prototype work, and future render-target bridge work.
- Related goals: Preserve all plugin, shader, and addon progress while keeping Dalapad removable and preventing the next agent from mistaking Stage 1 IPC for real G-buffer sampling.
- Documentation: Updated `docs/CodexSessionHandoff.md`, `docs/Dalapad.md`, `DalapadAddon/README.md`, added `DalapadAddon/external/README.md`, and updated this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully; `clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64` rebuilt the addon; `DalapadAddon/sample-status.json` parsed as JSON; `git diff --check` passed with Git LF-to-CRLF warnings only.
- Next steps: Run the final build/test/diff checks, commit and push, then validate the addon in-game by loading `Dalapad.addon64` and confirming Developer Mode > Dalapad sees the status-file handshake while resources remain unavailable.

### 1781875329 - Make Dalapad addon source testable

- Changed: Replaced the compile-blocked Dalapad addon skeleton with a first-test native source file that writes `dalapad-status.json` on DLL load/unload, optionally registers with ReShade when `reshade.hpp` is available, reports render-target resources as unavailable, and keeps realtime uniform movement disabled. Updated addon docs, sample status JSON, README, and codebase index to match the new test path.
- Why: Dalapad needs a concrete addon file that can begin validating load status and IPC before any render-target copy, resource registration, or live shader-value movement is attempted.
- Related goals: Prove the bridge handshake safely, keep render-layer validation as the priority, and preserve a clean removal boundary for the addon experiment.
- Documentation: Updated `DalapadAddon/README.md`, `DalapadAddon/CONTRACT.md`, `DalapadAddon/sample-status.json`, `README.md`, `docs/Dalapad.md`, `docs/CodebaseIndex.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully; `DalapadAddon/sample-status.json` parsed as JSON; `clang-cl /std:c++17 /EHsc /c DalapadAddon/src/dalapad_reshade_addon_skeleton.cpp` passed; `git diff --check` passed with Git LF-to-CRLF warnings only.
- Next steps: Build this source in a separate ReShade addon project, load it in-game, then confirm Developer Mode > Dalapad changes from `NotConnected` to `Loaded` or `SelfTest` while all resource rows remain unavailable.

### 1781851760 - Add Dalapad Stage 1 IPC contract groundwork

- Changed: Added plugin-side Dalapad status-file IPC diagnostics, endpoint/realtime contract rows, optional status parsing, addon scaffold IPC constants, a sample status payload, and report/debug/UI output for bridge status.
- Why: Dalapad needs a safe first handshake before any render-target resource bridge or realtime shader-value movement is attempted.
- Related goals: Keep the G-buffer path optional and removable, make future addon development target one contract, and reserve realtime adaptation without distracting from render-layer validation.
- Documentation: Updated Dalapad docs, addon contract docs, README, compatibility/debug docs, codebase index, reload guidance, handoff guidance, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully; `git diff --check` passed with Git LF-to-CRLF warnings only.
- Next steps: Build a separate experimental ReShade/native addon prototype that writes `dalapad-status.json`, then validate resource availability before any `.fx` sampling or live uniform work.

### 1781840795 - Add safe Dalapad addon scaffold and contract

- Changed: Added a repo-local `DalapadAddon/` scaffold with a human-readable contract, machine-readable contract JSON, and guarded native addon skeleton. Expanded Dalapad diagnostics so compatibility reports, debug bundles, and Developer Mode show the addon contract version, expected optional resources, availability flags, diagnostic routes, and scaffold removal note.
- Why: The project needs a clear, removable first addon direction before any real G-buffer/resource bridge work starts.
- Related goals: Keep Dalapad experimental and diagnostic-only while preparing a safe path for future optional surface data that can fall back to NormalField and FrameData.
- Documentation: Updated Dalapad docs, README, docs index, debug bundle docs, compatibility diagnostics, codebase index, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only.
- Next steps: Build and test the plugin, then only proceed to a separate addon proof-of-concept after validating that the contract and diagnostic routes are enough for an addon developer to start safely.

### 1781836897 - Clean Dalapad and NormalField diagnostics before G-buffer work

- Changed: Added Dalapad implementation-option and backend-step diagnostics, clarified that NormalDebug state is analyzed-preset-only, and fixed NormalField first-party source scans so they use configured ReShade shader paths.
- Why: The current Dalapad pass correctly finds runtime metadata but should not imply shaders can sample G-buffers yet, and the NormalField report could falsely say first-party shader sources were unavailable.
- Related goals: Prepare a safe, removable path toward optional external surface data while keeping FrameData, NormalField, generated presets, and shader behavior stable.
- Documentation: Updated Dalapad, debug bundle, compatibility diagnostics, README, codebase index, and this changelog with the staged G-buffer bridge route.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only.
- Next steps: Use the staged Dalapad backend plan: diagnostic pointer probe, separate bridge/addon spike, compile-guarded shader contract, FrameSurfaceData fallback merge, and debug-only comparison before any production influence.

### 1781823403 - Add Dalapad diagnostic surface-data probe

- Changed: Added the first Dalapad pass as a developer-only diagnostic probe, with compatibility report output, debug bundle JSON, and a Developer Mode diagnostics page.
- Why: The project needs a safe way to inspect whether future external surface data might be available before considering a native/addon bridge or any shader-facing behavior.
- Related goals: Keep FrameData/MaterialMasks/NormalField inline and stable while researching a removable optional backend for real surface data.
- Documentation: Added `docs/Dalapad.md` and linked Dalapad through the README, docs index, codebase index, compatibility diagnostics, and debug bundle docs.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only. In-game Dalapad probing was not run in this pass.
- Next steps: Test in-game debug bundles to see whether the metadata probe reports useful runtime candidates before any deeper prototype.

### 1781807285 - Clean NormalField relief contract

- Changed: Added an explicit neutral NormalField default path and early disabled return, exposed `TextureReliefSafety` through NormalField and FrameData surface data, constrained composite relief neighbor groove samples with the same safety gate as the center sample, reduced the old high-pass relief lane to a small compatibility hint, reduced texture-relief leakage into broad `StructureCandidate`, renamed stale NormalDebug labels, and updated compatibility-report NormalField consumers.
- Why: A top-to-bottom audit found that the newer composite-height relief path was mostly in place, but old high-pass and ungated neighbor paths could still leak noisy or unsafe evidence into production-facing fields.
- Related goals: Keep NormalField useful as expandable screen-space surface support data while making future shader integrations cleaner, safer, and easier to reason about.
- Documentation: Updated NormalField, FrameData, and this changelog to document `TextureReliefSafety`, the early disabled path, neighbor safety gating, and current production consumer order.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only. ReShade shader compile and in-game visual validation were not run after this patch.
- Next steps: Re-test NormalDebug modes `12`, `13`, `15`, `19`, and `20` in ReShade. Confirm disabled NormalField remains neutral, hard-surface seams survive, and sky/emissive/water/foliage/UI-heavy scenes stay quiet.

### 1781800224 - Gate texture relief against sky and emissive false positives

- Changed: Added an internal NormalField texture-relief safety gate that suppresses sky/cosmic fields, water, skin, UI/depth risk, foliage shimmer, bright highlights, and emissive/aether bloom before groove-line and composite-height evidence can drive relief normals.
- Why: Screenshot review showed the tile/stone normal output is usable, but cosmic skies, bright aether rings, combat bloom, foliage texture, and UI-heavy frames can produce convincing but false relief data.
- Related goals: Keep generated normal-map-like support useful for hard surfaces while making risky scenes fail quieter instead of turning sky, bloom, or foliage into fake material normals.
- Documentation: Updated NormalField docs and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only. ReShade shader compile and in-game visual validation were not run after this patch.
- Next steps: Re-test NormalDebug modes `15`, `19`, and `20` on city tiles, cosmic/aether sky scenes, foliage, water, and UI-heavy combat screenshots. The expected result is preserved hard-surface seams with quieter sky/emissive/foliage false positives.

### 1781791988 - Add cleaned-height stage before relief normals

- Changed: Added a confidence-aware cross blur to the NormalField texture-relief height compiler before deriving the composite relief normal. Weak, low-confidence height evidence is blended toward nearby compiled height, while strong groove/ridge/coherence evidence preserves local structure. Removed an obsolete raw compiled-height helper from the normal path.
- Why: NormalMap-style generation works best when the height map is cleaned before Sobel-like normal derivation. The previous pass produced usable support data, but fine grass/stone grain could still become shallow noisy relief.
- Related goals: Make NormalField relief more stable and normal-map-like while preserving hard-surface grooves, tile gaps, panel seams, and engraved lines as screen-space support data.
- Documentation: Updated NormalField docs and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only. ReShade shader compile and in-game visual validation were not run after this patch.
- Next steps: Re-test NormalDebug mode `20` on tile, outdoor cobble, grass, foliage, and slow camera movement. The expected result is slightly smoother broad surfaces with grooves preserved.

### 1781789270 - Improve NormalField relief-height compile readability

- Changed: Tightened NormalField texture-relief confidence so coherence cannot strongly drive relief without paired ridge, groove, valley, or structure evidence. Biased coherent groove lines downward in the compiled height field, made mode `19` a neutral grayscale height view with subtle ridge/valley tint instead of a red wash, increased mode `20` normal readability, and reduced production relief blending so debug visibility can be stronger than shader influence.
- Why: In-game captures showed the early detectors were useful, but the final composite layer still had a misleading red height view, too-hot coherence, and too-flat composite normal output.
- Related goals: Turn visible tile gaps, panel seams, engraved lines, and hard-surface texture detail into usable screen-space normal support while keeping NormalField diagnostic-first and conservative for production consumers.
- Documentation: Updated NormalField and NormalDebug docs plus this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only. ReShade shader compile and in-game visual validation were not run after this patch.
- Next steps: Re-test NormalDebug modes `18`, `19`, and `20`. Mode `18` should be less globally hot, mode `19` should read as neutral grayscale height, and mode `20` should show clearer directional groove normals without making broad tile faces noisy.

### 1781759056 - Compile NormalField relief height more conservatively

- Changed: Replaced the raw composite texture-relief height proxy with a neutral-centered relief-height compiler. The compiler adds structure-scale support checks, clamps raw high-pass texture detail, favors coherent groove/curvature evidence, pulls uncertain pixels back toward `0.5`, and derives the composite relief normal from the cleaned height field.
- Why: In-game NormalDebug captures showed useful groove and curvature data in modes `15` through `17`, but mode `19` was globally biased/saturated and mode `20` stayed too flat to resemble a usable generated normal map.
- Related goals: Move NormalField toward normal-map-like support data for tile gaps, engraved lines, panel seams, and other hard-surface material detail while keeping the system screen-space, optional, and diagnostic-first.
- Documentation: Updated NormalField and NormalDebug docs to describe the relief-height compiler and neutral-centered composite height behavior.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only. ReShade shader compile and in-game visual validation were not run.
- Next steps: Re-test NormalDebug modes `19` and `20` on the tile scenes. Mode `19` should read mostly neutral with clear local raised/lowered detail, and mode `20` should show directional normal variation around grooves without turning whole surfaces noisy.

### 1781755422 - Add NormalField composite relief diagnostics

- Changed: Reworked NormalField texture relief to build a bounded composite height hypothesis from local high-pass polarity, Hessian-style ridge/valley curvature, structure-coherence gating, and the existing groove-line signal. Added explicit curvature, coherence, composite height, and composite confidence fields to NormalField and FrameData surface data. Kept mode `13` as the legacy gradient relief normal for comparison, and added NormalDebug modes `16` through `20` for curvature ridge, curvature valley/groove, structure coherence, composite relief height, and composite relief normal RGB. Widened plugin/report/generated-write debug-mode clamps to allow the new modes.
- Why: The first relief/groove pass made tile seams visible but still produced too much random texture noise. The new pass separates likely crease/indent/ridge evidence from speckle before compiling it into a normal-map-like diagnostic layer.
- Related goals: Make NormalField more useful to current and future first-party shaders while keeping it screen-space, optional, explainable, and safety-gated behind FrameData/MaterialMasks ownership.
- Documentation: Updated NormalField, NormalDebug, FrameData, and this changelog to describe the composite relief model and new debug modes.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` completed successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only. ReShade shader compile and in-game visual validation were not run.
- Next steps: Validate NormalDebug modes `16` through `20` in-game on tile floors, marble/stone, metal panels, foliage, skin, sky, water, UI-heavy scenes, and camera movement before any production shader increases reliance on the new fields.

### 1781750599 - Add NormalField texture relief lane

- Changed: Added a texture-relief lane to `Dalashade_NormalField.fxh` that estimates local raised ridges from bright high-pass texture detail and grooves from dark high-pass creases, rejects broad lighting gradients, applies material/water/skin/sky/UI safety gates, and blends the result into the existing combined NormalField output. Added a direction-aware `TextureGrooveLine` signal for coherent dark seams such as tile gaps, engraved lines, panel joins, and cracks, with stricter diagonal continuity to reject more short scratch chatter. Exposed explicit `TextureReliefStrength` and `TextureGrooveLine` through FrameData and added NormalDebug modes `13`, `14`, and `15` for relief normal, combined ridge/groove/relief, and groove-line-only inspection.
- Why: NormalField needed a stronger but still bounded way to infer realistic surface relief from texture detail without pretending to recover true game normal maps.
- Related goals: Let existing first-party shaders that already consume NormalField benefit through `Normal` and `DetailStrength`, while giving future shaders clean explicit relief and groove-line fields to use only behind material and safety gates.
- Documentation: Updated NormalField, NormalDebug, FrameData, ShaderAuthoring, CodebaseIndex, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed; `dotnet test Dalashade.sln` exited successfully with restore/up-to-date output; `git diff --check` passed with Git LF-to-CRLF warnings only. ReShade shader compile and in-game visual validation were not run.
- Next steps: Validate NormalDebug modes `13`, `14`, and especially `15` on stone, metal, tile seams, foliage, skin, water, sky, UI-heavy, and high-bloom scenes before increasing production shader reliance on `TextureReliefStrength` or `TextureGrooveLine`.

### 1781761357 - Add ContactTone first-party shader

- Changed: Added `Dalashade_ContactTone.fx` as a separate first-party shader for local grounding tone, contact edge darkening, and material readability contrast. Added plugin config fields, generated preset injection, variable mapping, technique sync gating, load-order sorting, User Mode controls, Developer Mode controls, compatibility report diagnostics, debug bundle source scans, and regression harness coverage. Fixed the User Mode feature card so generated-preset injection evidence counts for newly injected first-party sections instead of reporting ContactTone as missing while the generated preset write result says it was written.
- Why: Contact tone needs a unique shader identity instead of being folded into SceneGI. Users should be able to make contact edges and grounded transitions stronger without also enabling GI color bounce, reflection projections, or atmospheric bloom.
- Related goals: Keep first-party shader responsibilities separate, make effects more noticeable while preserving sky/skin/water safety, and keep generated-preset behavior opt-in through explicit shader variable toggles.
- Documentation: Added `docs/Shaders/ContactTone.md` and updated README, shader overview, authoring, preset writing, FrameData, diagnostics, configuration, generation pipeline, scene-tag, debug-bundle, and codebase index docs.
- Verification: `dotnet build Dalashade.sln` passed; `dotnet test Dalashade.sln` completed successfully; `git diff --check` passed with Git LF-to-CRLF warnings only. ReShade shader compile and in-game visual validation were not run.
- Next steps: Regenerate once more and confirm the ContactTone card reports a generated section/variables instead of `Generate to inject section`; then validate `Dalashade_ContactTone.fx` in ReShade, checking normal output plus debug modes 1 through 6 across city, foliage, water, sky-heavy, combat/UI, and interior scenes.

### 1781758754 - Add AtmosphereBloom canopy gap bloom

- Changed: Added a canopy gap bloom detector to `Dalashade_AtmosphereBloom.fx` that looks for bright sky/light openings surrounded by darker foliage-like texture, rejects broad smooth sky, and exposes `CanopyGapBloomStrength` plus debug mode `10` for canopy gap bloom. Added `Dalashade_MaterialFoliage` support for AtmosphereBloom material mapping and generated-section injection.
- Why: The desired effect is bloom around gaps between tree leaves without blooming the entire sky, which belongs in AtmosphereBloom because the output is source-local bloom/halo behavior.
- Related goals: Improve first-party shader scene awareness while preserving shader responsibility boundaries: AtmosphereBloom owns glow eligibility, SceneGI owns indirect-light impression, and WeatherAtmosphere owns broad air/weather.
- Documentation: Updated `docs/Shaders/AtmosphereBloom.md`, `docs/ShaderAuthoring.md`, and this changelog.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` completed successfully with restore/up-to-date output; `git diff --check` passed with line-ending warnings only. ReShade shader compile and visual validation not run in this pass.
- Next steps: Inspect `Dalashade_MaterialDebugMode=10` in a foliage/canopy scene against normal sky and UI-heavy scenes.

### 1781756856 - Strengthen SceneGI material lanes

- Changed: Added explicit SceneGI-local material bounce lanes for foliage, stone, metal, sand/snow climate surfaces, wet surfaces, and emissive pooling. Added sky-safe receiver shaping and debug modes for material bounce lanes, sky-safe receivers, and emissive pooling lanes. Updated plugin UI/report/preset formatting to accept SceneGI debug modes `0` through `17`.
- Why: SceneGI is the safest near-term first-party shader target, and it needed clearer material bounce, AO/contact shaping, and emissive pooling without changing FrameData, MaterialMasks, NormalField, or shader variable contracts.
- Related goals: Improve first-party GI behavior while preserving inline FrameData ownership, keep diagnostics legible, and keep future ContactTone/EmissiveAtmosphere work separate from SceneGI and AtmosphereBloom.
- Documentation: Updated `docs/Shaders/SceneGI.md`, `docs/ShaderAuthoring.md`, and this changelog with the new GI lane behavior, debug modes, and standalone-shader rationale.
- Verification: `dotnet build Dalashade.sln` passed with 0 warnings and 0 errors; `dotnet test Dalashade.sln` completed successfully with restore/up-to-date output; `git diff --check` passed with line-ending warnings only.
- Next steps: Inspect SceneGI debug modes 15, 16, and 17 in a few outdoor, interior, and emissive scenes.

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
