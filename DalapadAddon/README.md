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
- no render-target reads
- no G-buffer copies
- no game render-target ReShade resource registration
- no generated preset changes
- no production shader contract changes
- no live uniform writes

The source file now implements the first testable behavior:

- exports ReShade addon metadata for `Dalapad`
- registers with ReShade when built with `reshade.hpp` available
- writes `dalapad-status.json` on DLL load and unload
- opens a diagnostic control pipe for ping, self-test, status, and capability negotiation
- reports a metadata-only resource catalog for the normal, diffuse, and depth candidates
- uploads a synthetic 256x256 debug texture into `Dalapad_Debug.fx` when `Dalapad_DebugTexture` is loaded
- reports all render-target resources as unavailable in Stage 1
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

That artifact validates addon load/unload, status-file IPC, diagnostic control-pipe IPC, the resource catalog schema, and the synthetic debug texture path. It does not expose render targets.

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
4. Answer diagnostic control-pipe requests for ping, self-test, status, capability negotiation, and metadata-only resource catalog rows with short, local responses.
5. Publish optional resource/status names and metadata-only catalog rows that match `dalapad-addon-contract.json`.
6. In synthetic diagnostic mode, upload a generated test texture into `Dalapad_Debug.fx`.
7. In a later diagnostic mode, expose copied or registered candidates for:
   - surface normal candidate
   - diffuse/albedo-like candidate
   - depth candidate
8. Report dimensions, format, freshness, and confidence.
9. Fail closed when resources are missing, stale, wrong-sized, or unsafe.
10. Let Dalashade/FrameData fall back to NormalField when the bridge is absent.
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
5. Addon resource catalog: report metadata-only resource shape/freshness before any texture exposure.
6. Synthetic debug visualization: upload generated pixels into `Dalapad_Debug.fx` to prove addon-to-FX texture updates.
7. Addon resource probe: expose named diagnostic resources and flags.
8. Shader compare: debug-only `.fx` views compare bridge data to NormalField and FrameData.
9. Optional backend: only after stable validation, let FrameSurfaceData use external data behind a default-off setting.
10. Realtime value bridge: only after render-layer diagnostics are stable, test bounded first-party uniform deltas.

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

Dalashade opens this pipe only during explicit diagnostic probes, with short timeouts. The current command set is `Ping`, `BridgeSelfTest`, `QueryStatus`, `QueryCapabilities`, `QueryDebugVisualization`, and `SetDebugVisualization`. Capability responses must report render-target reads, copies, game shader-resource registration, realtime uniforms, and generated-preset changes as disabled.

Stage 1.2 reports `supportsResourceCatalog=true`. This means only that the addon can publish structured catalog rows with candidate names, sources, neutral dimensions, `unknown` formats, disabled freshness, zero confidence, safety state, metadata source, and disabled reasons. It does not mean the addon has inspected live render targets.

Stage 1.3 reports `supportsDebugVisualization=true`. This means only that the addon can look for `Dalapad_DebugTexture` in loaded ReShade effects and upload generated synthetic pixels. It does not mean the addon can copy or bind XIV render targets.

## Removal

Delete this folder to remove the addon scaffold. If an experimental addon wrote a status file, delete `Dalapad/dalapad-status.json` from the Dalashade plugin config. No Dalashade plugin or shader cleanup should be required unless future work explicitly wires the bridge into production behavior.
