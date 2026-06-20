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
- Probes an optional diagnostic control pipe with short timeouts for capability negotiation.
- Reads metadata-only resource catalog rows for the expected normal, diffuse, and depth candidates.
- Offers an explicit Developer Mode resource shape probe that may invoke `RenderTargetManager.Instance` and reports redacted candidate presence, nullability, dimensions, and format labels if reflection can read them.
- Includes `Dalapad_Debug.fx`, a manual ReShade debug shader for visualizing a synthetic bridge texture and fallback/status patterns.
- Lets the addon upload a synthetic 256x256 debug texture into `Dalapad_DebugTexture` when `Dalapad_Debug.fx` is loaded.

Dalapad currently does not:

- Invoke `RenderTargetManager.Instance` during the default diagnostics pass or any User Mode flow.
- Read, copy, map, or sample game render targets.
- Upload XIV render targets into ReShade.
- Expose textures to ReShade `.fx` shaders.
- Open named pipes except the diagnostic-only `Dalapad.Control.v1` health probe.
- Report raw pointer addresses.
- Move shader values in real time.
- Treat metadata-only resource catalog rows as live render-target availability.
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
- optional control-pipe health and capability negotiation state
- metadata-only resource catalog rows
- optional developer-only resource shape probe rows
- optional synthetic debug visualization bridge status
- endpoint names
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
- `src/dalapad_reshade_addon_skeleton.cpp`: first-test native source for a separate prototype project.
- `external/reshade-sdk/include`: public ReShade SDK headers needed by the local test build.
- `build/Dalapad.addon64`: local Stage 1 test build produced during handoff.

The source and binary are deliberately outside `Dalashade.sln`. They can be used to test DLL load, optional ReShade registration, status-file IPC, diagnostic control-pipe IPC, and metadata-only resource catalog shape. The addon still reports normal, diffuse, and depth resources as unavailable because Stage 1 must not read, copy, register, or expose render targets.

The Stage 1 addon requests ReShade add-on API version `18` directly. ReShade `6.7.3.2148` supports API `18`; using the vendored SDK helper version `20` causes registration failure and ReShade unloads the addon with `No add-on was registered`.

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

The plugin also probes the diagnostic control pipe:

```text
\\.\pipe\Dalapad.Control.v1
```

Supported diagnostic command types are:

- `Ping`
- `BridgeSelfTest`
- `QueryStatus`
- `QueryCapabilities`

The plugin uses short timeouts and treats missing, invalid, or incompatible responses as neutral diagnostics. Current capability responses must report realtime uniform movement, render-target reads, render-target copies, and shader-resource registration as disabled.

Stage 1.2 reports `supportsResourceCatalog=true`. This only means the addon can publish structured rows for candidate resource names, source strings, neutral dimensions, unknown format, disabled freshness, zero confidence, safety state, metadata source, and disabled reason text. It does not mean the addon has read live render target metadata.

## Developer Resource Shape Probe

Developer Mode can explicitly run a resource shape probe. This pass is disabled by default and is not shown as a User Mode feature. When enabled, the plugin attempts a one-shot reflection call to `RenderTargetManager.Instance` and reports rows for:

- `Dalapad_SurfaceNormal` from `RenderTargetManager.GBuffers[0]`
- `Dalapad_SurfaceDiffuse` from `RenderTargetManager.GBuffers[2]`
- `Dalapad_SurfaceDepth` from `RenderTargetManager.DepthStencil`

The probe reports only shape-level diagnostics: candidate found, redacted pointer observation, width, height, format label, freshness label, confidence, safety state, metadata source, and failure reason. It does not copy, map, sample, register, expose, serialize, or send texture handles. The preferred path is a narrow typed ClientStructs read of `RenderTargetManager.Instance()`, `GBuffers`, `DepthStencil`, and `Texture.AllocatedWidth/AllocatedHeight`; reflection remains a fallback. If the current client struct shape cannot be inspected safely, the probe fails closed and writes neutral unavailable rows.

The expected test output is not "shader-ready resources". The expected output is enough repeated evidence to decide whether lifecycle tests are worth running before a native bridge attempts any resource registration or copy.

The first three shape-probe bundles on 2026-06-20 proved the opt-in gate, `RenderTargetManager.Instance` invocation, status-file IPC, control-pipe IPC, and catalog paths were healthy, but the reflection-only pointer path could not dereference candidate texture pointers. The current probe therefore uses typed ClientStructs access first and leaves reflection as a fallback.

## Debug Visualization Bridge

`shaders/Dalapad_Debug.fx` is the first debug visualization surface. It is manual and diagnostic-only. Mode 0 is pass-through; nonzero modes show bridge status, the synthetic texture, channel inspection, alpha inspection, and missing/stale patterns.

The addon-side bridge currently uploads only a generated synthetic checker/gradient texture into the ReShade FX texture variable:

```text
Dalapad_DebugTexture
```

This validates the addon-to-FX texture update path without copying, sampling, registering, or exposing XIV render targets. The control pipe and status file report this under `debugVisualization`, including whether the shader texture was found, whether synthetic pixels were uploaded, dimensions, frame age, and safety flags.

This is not a render-layer bridge yet. Real `GBuffers[0]`, `GBuffers[2]`, or `DepthStencil` copies should come only after the synthetic texture path is stable across reload and lifecycle tests.

The realtime message family remains reserved only:

```text
Dalapad.RealtimeUniforms.v1
```

Real-time shader value movement should come after render-layer validation and should remain bounded, opt-in, and removable.

## UI

Developer Mode includes a `Dalapad` diagnostics page. It is intentionally plain:

- press `Probe Dalapad diagnostics`
- optionally enable `Enable developer-only resource shape probe` and press `Run Dalapad shape probe`
- install/reload `Dalapad_Debug.fx` manually when testing the synthetic visualization bridge
- inspect status and capability rows
- inspect status-file and control-pipe health
- inspect resource shape rows and warnings
- inspect debug visualization status rows
- read the health-check next steps
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

- load `DalapadAddon/build/Dalapad.addon64` through ReShade and prove the status-file handshake plus control-pipe capability negotiation in-game
- verify metadata-only candidate resource catalog rows are stable across status-file and control-pipe IPC
- then run the developer-only resource shape probe and report whether candidate render-target manager rows can be observed safely in the plugin diagnostics
- collect lifecycle evidence across login, zone change, resolution change, ReShade reload, plugin reload, and debug shader reload
- do not copy GPU resources until the load/IPC handshake is stable
- do not expose textures to ReShade until resource lifetime, size, and format are proven
- do not change generated output
- keep all behavior behind developer diagnostics

## Implementation Route

The current conclusion is that a pure Dalamud plugin can inspect runtime metadata and possibly validate pointer/resource shape, but it cannot by itself make ReShade `.fx` shaders sample FFXIV render targets. ReShade shader sampling needs a bridge that registers or copies selected resources into something ReShade can bind by name.

Recommended stages:

1. Metadata inventory: keep the current reflection-only probe and collect real debug bundles across zones and sessions.
2. Diagnostic pointer probe: implemented as a developer-only opt-in plugin probe that invokes `RenderTargetManager.Instance` and reports whether candidate `GBuffer[0]`, `GBuffer[2]`, and depth/stencil resources are observable and plausibly shaped. It still does not copy, sample, register, or expose them.
3. Dalapad diagnostic control pipe: prove the separate native/ReShade addon can answer ping, self-test, status, and capability queries without touching render targets.
4. Metadata-only resource catalog: report candidate names, dimensions, formats, freshness, confidence, and disabled reasons without sending texture handles or changing shaders. Stage 1.2 implements the schema with neutral unavailable values only.
5. Synthetic debug visualization bridge: implemented through `Dalapad_Debug.fx` and addon-side synthetic texture upload into `Dalapad_DebugTexture`.
6. Dalapad bridge addon spike: prototype optional named diagnostic textures plus availability flags only after metadata, shape, synthetic visualization, and lifecycle observations are stable.
6. Shader contract guard: add compile-guarded `.fx` access such as `DALAPAD_SURFACE_DATA_AVAILABLE`. When unavailable, shaders must compile and behave exactly as they do now.
7. FrameSurfaceData merge: let `FrameData` choose external surface normals only when the bridge is present and confidence is high; otherwise use `NormalField` fallback.
8. Compare and calibrate: add debug views comparing external normal/diffuse/depth confidence against NormalField, screenshot material evidence, and MaterialIntent.
9. Realtime value bridge: after render-layer reliability is proven, prototype bounded live first-party shader deltas over the reserved control channel while keeping generated presets as fallback authority.
10. Opt-in production influence: only after diagnostics prove stability, add an experimental user/developer toggle that lets production shaders consume the external backend through the existing `FrameSurfaceData` contract.

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

These meanings are not production guarantees yet. The current shape probe only begins to verify nullability, dimensions, and format labels. It still needs repeated lifecycle testing, freshness proof, and transparent/chara-view buffer review before any shader consumes the data.
