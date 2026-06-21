# Dalapad Addon

DalapadAddon is the removable, experimental bridge direction for Dalashade external surface data.

This folder is not part of the Dalashade plugin build. It is a first-pass addon scaffold and contract package for future ReShade/native addon work.

## Goal

The addon should eventually answer one narrow question:

Can selected FFXIV render-target candidates be exposed to ReShade `.fx` shaders as optional diagnostic resources without making Dalashade depend on fragile native/render internals?

## First Pass Status

This first pass is intentionally non-production:

- no build integration
- local test binary output only under `DalapadAddon/build/`
- runtime loading only if a developer builds the DLL in a separate addon project
- no production render-target reads
- debug-only addon-owned render-layer candidate copies may occur after ReShade bind observations
- no game render-target handles are sent over IPC
- no generated preset changes
- no required production shader contract changes
- no live uniform writes

The source file now implements the first testable behavior:

- exports ReShade addon metadata for `Dalapad`
- registers with ReShade when built with `reshade.hpp` available
- writes `dalapad-status.json` on DLL load and unload
- opens a diagnostic control pipe for ping, self-test, status, and capability negotiation
- reports resource catalog rows for normal, diffuse, depth, scan slots, and pinned candidates
- uploads a synthetic 256x256 debug texture into `Dalapad_Debug.fx` when `Dalapad_DebugTexture` is loaded
- can bind scan and pinned debug aliases only after an addon-owned copy succeeds
- reports that live realtime uniform movement is reserved but disabled
- supports `DALAPAD_STATUS_DIR` as an override for local IPC testing

The current local build artifact is:

```text
DalapadAddon/build/Dalapad.addon64
```

Rebuild command used during handoff:

```text
clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64
```

That artifact validates addon load/unload, status-file IPC, diagnostic control-pipe IPC, the resource catalog schema, the synthetic debug texture path, and debug-only scan/pinned candidate binding. It does not make render-layer data required for Dalashade or send game resource handles to the plugin.

The Stage 1 source requests ReShade add-on API version `18` directly instead of using the vendored header's current helper version. ReShade `6.7.3.2148` reports API `18` as its supported add-on API; requesting the newer SDK header version `20` causes ReShade to unload the addon with `No add-on was registered`.

The scaffold also defines:

- the resource names Dalashade expects a future bridge to publish
- the Stage 1 status-file IPC contract Dalashade can read safely
- the future named-pipe channel names for opt-in live control
- the diagnostic routes needed to validate each stage
- the safety gates that must stay true before production influence is considered
- a native source file that can be copied into a separate experimental addon project

## Addon Responsibilities

A real Dalapad addon should do only these jobs:

1. Load as a separate experimental bridge, not as a required Dalashade dependency.
2. Report bridge availability and version.
3. Write a small status file matching the `0.1-ipc-diagnostic` IPC contract.
4. Answer diagnostic control-pipe requests for ping, self-test, status, capability negotiation, and resource catalog rows with short, local responses.
5. Publish optional resource/status names, scan slots, pinned candidates, dimensions, freshness, confidence, and reason text that match `dalapad-addon-contract.json`.
6. In synthetic diagnostic mode, upload a generated test texture into `Dalapad_Debug.fx`.
7. In diagnostic mode, expose copied addon-owned candidates for:
   - surface normal candidate
   - diffuse/albedo-like candidate
   - depth candidate
8. Report dimensions, format, freshness, confidence, scan slots, and pinned semantic candidates.
9. Fail closed when resources are missing, stale, wrong-sized, or unsafe.
10. Let first-party shaders and any future FrameData merge fall back to existing behavior when the bridge is absent.
11. Reserve live value movement for a later bounded channel after the surface bridge works.

## Non-Responsibilities

The addon must not:

- detect mechanics
- automate gameplay
- depend on private shader packs
- require network reads
- replace Dalashade's preset writer
- force production shaders to depend on G-buffer data
- make NormalField, MaterialMasks, or FrameData fail when absent

## Diagnostic Route

Use this order:

1. Plugin metadata probe: existing Dalashade reflection probe.
2. Plugin pointer probe: future developer-only opt-in pointer/resource shape probe.
3. Addon self-test: prove the native bridge loads and can write `dalapad-status.json` without resources.
4. Control-pipe self-test: prove live request/response IPC with no render-target or shader authority.
5. Addon resource catalog: report diagnostic resource shape/freshness before any texture exposure.
6. Synthetic debug visualization: upload generated pixels into `Dalapad_Debug.fx` to prove addon-to-FX texture updates.
7. Addon resource probe: expose named diagnostic resources, scan slots, pinned candidates, and flags.
8. Shader compare: debug-only `.fx` views compare bridge data to NormalField and FrameData.
9. Optional first-party consumption: only through `Dalashade_Dalapad.fxh`, default-off global and shader-local gates, and semantic pinned resources.
10. Optional backend: only after stable validation, let FrameSurfaceData use external data behind a default-off setting.
11. Realtime value bridge: only after render-layer diagnostics are stable, test bounded first-party uniform deltas.

## Stage 1 IPC

The current plugin-side Stage 1 reader looks for:

```text
<XIVLauncher plugin config>/Dalashade/Dalapad/dalapad-status.json
```

Missing, stale, or invalid status files are neutral. They do not change generated presets, shader uniforms, or runtime behavior.

The default Windows path is:

```text
%APPDATA%\XIVLauncher\pluginConfigs\Dalashade\Dalapad\dalapad-status.json
```

For local tests, set `DALAPAD_STATUS_DIR` to a writable folder before loading the DLL.

The diagnostic control pipe is:

```text
\\.\pipe\Dalapad.Control.v1
```

Dalashade opens this pipe only during explicit diagnostic probes, with short timeouts. The current command set is `Ping`, `BridgeSelfTest`, `QueryStatus`, `QueryCapabilities`, `QueryDebugVisualization`, and `SetDebugVisualization`. Capability responses are diagnostic. Debug visualization status may report observed/copy counts, but the control pipe must not send texture handles or make generated-preset changes.

Stage 1.2 reports `supportsResourceCatalog=true`. This means the addon can publish structured catalog rows with candidate names, sources, dimensions, format labels, freshness, confidence, safety state, metadata source, and reason text. Static rows may stay neutral; runtime rows may report observed scan or pinned candidate state.

Stage 1.3 reports `supportsDebugVisualization=true`. The current debug visualization path can look for `Dalapad_DebugTexture`, upload generated synthetic pixels, observe candidate render-layer bindings, create addon-owned copies when formats/lifetimes allow, and bind those copies to scan or pinned debug aliases. It does not mean the resources are production-stable or required.

## Removal

Delete this folder to remove the addon scaffold. If an experimental addon wrote a status file, delete `Dalapad/dalapad-status.json` from the Dalashade plugin config. Shader cleanup is required only for the optional first-party integration path: remove `shaders/Dalashade_Dalapad.fxh`, remove SceneGI's include/helper calls, and remove Dalapad shader variable wiring if the feature is abandoned.
