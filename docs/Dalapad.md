# Dalapad

Dalapad is the name for Dalashade's optional future external surface-data addon direction.

The first implementation is diagnostic-only. It exists to answer one question: does the current runtime expose enough metadata to justify a later, separate addon bridge for real surface data such as external normals, diffuse-like buffers, or depth targets?

## Current Behavior

Dalapad currently:

- Checks loaded runtime assemblies for FFXIVClientStructs metadata.
- Looks for `RenderTargetManager` metadata.
- Reports whether `Instance`, GBuffer-like members, DepthStencil-like members, and texture metadata appear discoverable.
- Writes diagnostics to the compatibility report and debug bundle.
- Shows a developer-only diagnostics page named `Dalapad`.
- Reads an optional Stage 1 IPC status file if a separate addon prototype creates one.

Dalapad currently does not:

- Invoke `RenderTargetManager.Instance`.
- Read, copy, map, or sample game render targets.
- Expose textures to ReShade `.fx` shaders.
- Open named pipes.
- Move shader values in real time.
- Change generated preset values.
- Change shader code, FrameData, MaterialMasks, NormalField, or technique activation.
- Add native hooks, network reads, gameplay automation, or render-target bridges.

## Why It Exists

NormalField and FrameData are still inline screen-space contracts. They can infer useful surface detail from the backbuffer and ReShade depth, but they are not true game normals or material IDs.

Dalapad is the removable research boundary for a future optional backend. If a later addon proves safe and useful, the long-term shape should be:

```text
External surface data available
  -> report confidence
  -> optional FrameSurfaceData backend
  -> fallback to NormalField when unavailable
```

The first pass intentionally stops before that point. It only reports whether the runtime appears to have candidate metadata.

## Reports

Compatibility reports include a `Dalapad Diagnostics` section with:

- probe status
- runtime assembly name
- RenderTargetManager type discovery
- Instance/GBuffer/DepthStencil/Texture metadata discovery
- capability rows
- addon contract version
- IPC contract version
- optional IPC status-file state
- future endpoint names
- addon resource contract rows
- realtime adaptation contract rows
- diagnostic route rows
- implementation option rows
- next backend implementation steps
- safety notes
- removal notes

Debug bundles include:

```text
dalapad-diagnostics.json
```

That JSON is structured for later comparison between machines and Dalamud/runtime versions. It also records the same implementation options, addon contract rows, diagnostic routes, and backend steps shown in the compatibility report so the roadmap does not drift between docs and runtime diagnostics.

## Addon Scaffold

The repo includes a non-production scaffold under:

```text
DalapadAddon/
```

That folder is not referenced by `Dalashade.sln`, is not built, is not loaded by Dalamud, and is not installed with the plugin. It exists so future addon work starts from a stable, reviewable contract instead of scattered notes.

The scaffold contains:

- `README.md`: addon purpose, responsibilities, non-responsibilities, diagnostic route, and removal boundary.
- `CONTRACT.md`: human-readable `0.1-diagnostic` resource contract and `0.1-ipc-diagnostic` status-file contract.
- `dalapad-addon-contract.json`: machine-readable contract and diagnostic route.
- `sample-status.json`: example status-file payload for a separate addon prototype.
- `src/dalapad_reshade_addon_skeleton.cpp`: first-test native source for a future separate prototype project.
- `external/reshade-sdk/include`: public ReShade SDK headers needed by the local test build.
- `build/Dalapad.addon64`: local Stage 1 test build produced during handoff.

The source and binary are deliberately outside `Dalashade.sln`. They can be used to test DLL load, optional ReShade registration, and status-file IPC. The addon still reports normal, diffuse, and depth resources as unavailable because Stage 1 must not read, copy, register, or expose render targets.

Current rebuild command:

```text
clang-cl /std:c++17 /EHsc /LD /I DalapadAddon\external\reshade-sdk\include DalapadAddon\src\dalapad_reshade_addon_skeleton.cpp /Fe:DalapadAddon\build\Dalapad.addon64
```

## Stage 1 IPC

The plugin now has a diagnostic-only status-file reader. It looks for:

```text
<XIVLauncher plugin config>/Dalashade/Dalapad/dalapad-status.json
```

Expected IPC contract:

```text
0.1-ipc-diagnostic
```

This is not a production bridge. Missing, stale, malformed, or incompatible status files are treated as neutral diagnostics. They cannot affect generated presets, shader uniforms, shader compilation, first-party technique activation, FrameData, MaterialMasks, or NormalField.

The status file can report:

- bridge version
- addon producer name
- plain-language bridge status
- resource catalog rows for normal, diffuse, and depth candidates
- warnings and failure reasons

The plugin also reports reserved endpoint names for future work:

```text
\\.\pipe\Dalapad.Control.v1
Dalapad.RealtimeUniforms.v1
```

Those names are contract groundwork only. Dalashade does not open the pipe in this pass. Real-time shader value movement should come after render-layer validation and should remain bounded, opt-in, and removable.

## UI

Developer Mode includes a `Dalapad` diagnostics page. It is intentionally plain:

- press `Probe Dalapad diagnostics`
- inspect status and capability rows
- inspect addon contract/resource rows
- inspect diagnostic route rows
- read the safety and removal boundaries

There is no User Mode control because this pass does not provide a visual feature.

## Removal Boundary

Dalapad should stay easy to remove. Current cleanup is limited to:

- delete `Dalashade/DalapadDiagnostics.cs`
- delete `DalapadAddon/`
- remove the diagnostics calls from `Plugin`, `CompatibilityReportExporter`, `DebugBundleExporter`, and `ConfigWindow`
- remove this doc and index links

No shader cleanup should be required because this pass does not touch shader files, shader uniforms, FrameData, MaterialMasks, NormalField, or preset output.

## Next Safe Prototype

Only proceed if the metadata probe is useful in real debug bundles.

The next safe prototype should still be diagnostic-only:

- load `DalapadAddon/build/Dalapad.addon64` through ReShade and prove the status-file handshake in-game
- report whether a candidate render-target manager instance can be observed safely from an addon-side diagnostic path
- do not copy GPU resources until the load/IPC handshake is stable
- do not expose textures to ReShade until resource lifetime, size, and format are proven
- do not change generated output
- keep all behavior behind developer diagnostics

## Implementation Route

The current conclusion is that a pure Dalamud plugin can inspect runtime metadata and possibly validate pointer/resource shape, but it cannot by itself make ReShade `.fx` shaders sample FFXIV render targets. ReShade shader sampling needs a bridge that registers or copies selected resources into something ReShade can bind by name.

Recommended stages:

1. Metadata inventory: keep the current reflection-only probe and collect real debug bundles across zones and sessions.
2. Diagnostic pointer probe: add a developer-only opt-in probe that invokes `RenderTargetManager.Instance` and reports whether candidate `GBuffer[0]`, `GBuffer[2]`, and depth/stencil resources are present, stable, and plausibly sized. Do not copy or sample them.
3. Dalapad bridge addon spike: prototype a separate native/ReShade addon that exposes candidate normal, diffuse, and depth resources as optional named textures plus availability flags.
4. Shader contract guard: add compile-guarded `.fx` access such as `DALAPAD_SURFACE_DATA_AVAILABLE`. When unavailable, shaders must compile and behave exactly as they do now.
5. FrameSurfaceData merge: let `FrameData` choose external surface normals only when the bridge is present and confidence is high; otherwise use `NormalField` fallback.
6. Compare and calibrate: add debug views comparing external normal/diffuse/depth confidence against NormalField, screenshot material evidence, and MaterialIntent.
7. Realtime value bridge: after render-layer reliability is proven, prototype bounded live first-party shader deltas over the reserved control channel while keeping generated presets as fallback authority.
8. Opt-in production influence: only after diagnostics prove stability, add an experimental user/developer toggle that lets production shaders consume the external backend through the existing `FrameSurfaceData` contract.

The intended shader-side shape is:

```text
if external surface data is available and confidence is high:
    FrameSurfaceData normal/material hints come from Dalapad
else:
    FrameSurfaceData normal/material hints come from NormalField and existing masks
```

This keeps Dalapad removable. Production shaders should not grow direct dependencies on FFXIVClientStructs, unmanaged pointers, or a required addon package.

## G-Buffer Candidates

Current research target:

- `RenderTargetManager.Instance()`: possible entry point for candidate render targets.
- `GBuffers[0]`: likely world-space normal candidate based on FFXIVClientStructs comments.
- `GBuffers[2]`: likely diffuse/albedo-like candidate based on FFXIVClientStructs comments.
- `DepthStencil`: likely depth/stencil candidate, but format, scaling, timing, and reverse-Z behavior still need proof.

These meanings are not production guarantees yet. The next probe needs to verify lifetime, nullability, dimensions, format, and whether transparent/chara-view buffers matter before any shader consumes the data.
