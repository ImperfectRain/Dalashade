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
- no ReShade resource registration
- no generated preset changes
- no shader contract changes
- no live uniform writes

The source file now implements the first testable behavior:

- exports ReShade addon metadata for `Dalapad`
- registers with ReShade when built with `reshade.hpp` available
- writes `dalapad-status.json` on DLL load and unload
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

That artifact only validates addon load/unload and status-file IPC. It does not expose render targets.

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
4. Publish optional resource/status names that match `dalapad-addon-contract.json`.
5. In diagnostic mode, expose copied or registered candidates for:
   - surface normal candidate
   - diffuse/albedo-like candidate
   - depth candidate
6. Report dimensions, format, freshness, and confidence.
7. Fail closed when resources are missing, stale, wrong-sized, or unsafe.
8. Let Dalashade/FrameData fall back to NormalField when the bridge is absent.
9. Reserve live value movement for a later bounded channel after the surface bridge works.

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
4. Addon resource probe: expose named diagnostic resources and flags.
5. Shader compare: debug-only `.fx` views compare bridge data to NormalField and FrameData.
6. Optional backend: only after stable validation, let FrameSurfaceData use external data behind a default-off setting.
7. Realtime value bridge: only after render-layer diagnostics are stable, test bounded first-party uniform deltas.

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

The reserved future live-control pipe is:

```text
\\.\pipe\Dalapad.Control.v1
```

Dalashade does not open this pipe in Stage 1. It is documented now so a future addon does not invent a second IPC route for live values.

## Removal

Delete this folder to remove the addon scaffold. If an experimental addon wrote a status file, delete `Dalapad/dalapad-status.json` from the Dalashade plugin config. No Dalashade plugin or shader cleanup should be required unless future work explicitly wires the bridge into production behavior.
