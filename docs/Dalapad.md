# Dalapad

Dalapad is the name for Dalashade's optional future external surface-data addon direction.

The first implementation started as diagnostic-only. The current state is still experimental, but it now has a narrow shader-consumption path: first-party shaders may opt in to semantic, addon-provided pinned textures through `shaders/Dalashade_Dalapad.fxh`, and production shaders consume the resulting surface data through `shaders/Dalashade_FrameData.fxh`.

## Current Behavior

Dalapad currently:

- Checks loaded runtime assemblies for FFXIVClientStructs metadata.
- Looks for `RenderTargetManager` metadata.
- Reports whether `Instance`, GBuffer-like members, DepthStencil-like members, and texture metadata appear discoverable.
- Writes diagnostics to the compatibility report and debug bundle.
- Shows a developer-only diagnostics page named `Dalapad`.
- Reads an optional Stage 1 IPC status file if a separate addon prototype creates one.
- Probes an optional diagnostic control pipe with short timeouts for capability negotiation.
- Reads diagnostic resource catalog rows for expected normal, diffuse/albedo, depth, scan, and pinned candidates.
- Offers an explicit Developer Mode resource shape probe that may invoke `RenderTargetManager.Instance` and reports redacted candidate presence, nullability, dimensions, and format labels if reflection can read them.
- Includes `Dalapad_Debug.fx`, a manual ReShade debug shader for visualizing a synthetic bridge texture and fallback/status patterns.
- Lets the addon upload a synthetic 256x256 debug texture into `Dalapad_DebugTexture` when `Dalapad_Debug.fx` is loaded.
- Lets the separate ReShade addon prototype copy discovered render-layer candidates into named diagnostic textures for `Dalapad_Debug.fx`.
- Publishes semantic pinned candidates such as `DALAPAD_PINNED_NORMAL`, `DALAPAD_PINNED_ALBEDO`, `DALAPAD_PINNED_MASK`, `DALAPAD_PINNED_NORMAL_ALT`, and `DALAPAD_PINNED_EMISSIVE`.
- Provides `shaders/Dalashade_Dalapad.fxh`, the shared first-party shader helper include for sampling pinned candidates behind global, per-shader, and availability gates.
- Lets `Dalashade_FrameData.fxh` merge the pinned normal-like candidate into `FrameSurfaceData` as confidence-weighted structure/contact/normal support.
- Lets first-party production shaders consume `surface.SurfaceDataInfluence` and related FrameData fields without sampling pinned resources directly.

Dalapad currently does not:

- Invoke `RenderTargetManager.Instance` during the default diagnostics pass or any User Mode flow.
- Treat discovered render-layer candidates as stable production truth.
- Send GPU handles or pointer addresses over IPC.
- Open named pipes except the diagnostic-only `Dalapad.Control.v1` health probe.
- Report raw pointer addresses.
- Move shader values in real time.
- Treat diagnostic resource catalog rows as production render-target guarantees.
- Enable shader consumption unless `Enable Dalapad shader additions` is on and the individual shader feature is enabled.
- Replace FrameData, MaterialMasks, NormalField, or technique activation.
- Add native game hooks, network reads, gameplay automation, or account/gameplay-affecting behavior.

## Why It Exists

NormalField and FrameData are still inline screen-space contracts. They can infer useful surface detail from the backbuffer and ReShade depth, but they are not true game normals or material IDs.

Dalapad is the removable research boundary for a future optional backend. If a later addon proves safe and useful, the long-term shape should be:

```text
External surface data available
  -> report confidence
  -> optional FrameSurfaceData backend
  -> merge with NormalField when available
  -> fallback to NormalField/default behavior when unavailable
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
- diagnostic resource catalog rows
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

The source and binary are deliberately outside `Dalashade.sln`. They can be used to test DLL load, optional ReShade registration, status-file IPC, diagnostic control-pipe IPC, resource catalog shape, synthetic texture upload, scan candidates, pinned candidates, and addon-owned debug copies. This does not make render-layer data required for normal Dalashade output.

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

This is not a required production bridge. Missing, stale, malformed, or incompatible status files are treated as neutral diagnostics. They cannot by themselves affect generated presets, shader uniforms, shader compilation, first-party technique activation, FrameData, MaterialMasks, or NormalField.

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

The plugin uses short timeouts and treats missing, invalid, or incompatible responses as neutral diagnostics. Capability responses are informational; first-party shader behavior is controlled by generated shader uniforms plus ReShade-side resource availability flags, not by trusting IPC text alone.

Stage 1.2 reports `supportsResourceCatalog=true`. This means the addon can publish structured rows for candidate resource names, source strings, dimensions, format labels, freshness, confidence, safety state, metadata source, and reason text. Static rows may stay neutral; runtime rows may show observed debug candidate state.

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

This validates the addon-to-FX texture update path. The control pipe and status file report this under `debugVisualization`, including whether the shader texture was found, whether synthetic pixels were uploaded, dimensions, frame age, safety flags, scan slots, and pinned candidates.

The bridge now has a render-layer discovery path, but it is still diagnostic and semantic-pinning driven. Debug scan slots remain discovery surfaces. First-party shaders should consume only pinned semantic resources through `Dalashade_Dalapad.fxh`, not raw group/MRT slots.

Current pinned meanings are observational and may change as captures improve:

| Semantic | Current role | Production guidance |
| --- | --- | --- |
| `DALAPAD_PINNED_NORMAL` | Dense normal-like surface/detail candidate. | Use as confidence-weighted structure/contact support, not as authoritative world normals. |
| `DALAPAD_PINNED_ALBEDO` | Albedo/luma-like scene color candidate. | Debug/evidence only until stable albedo behavior is proven. |
| `DALAPAD_PINNED_MASK` | Surface/object/material-like mask candidate. | Debug/evidence only until classes are identified. |
| `DALAPAD_PINNED_NORMAL_ALT` | Alternate dense surface/detail candidate. | Reserve for comparison and fallback. |
| `DALAPAD_PINNED_EMISSIVE` | Emissive/lighting-like candidate. | Reserve for source confidence experiments. |

## First-Party Shader Integration

`shaders/Dalashade_Dalapad.fxh` is the only supported first-party shader entry point for Dalapad data. It defines:

- pinned texture bindings and samplers
- pinned availability and dimension uniforms
- a global `Dalashade_DalapadEnabled` gate
- normal-like decode helpers
- raw evidence helpers for albedo, mask, and emissive pins
- confidence-weighted result structs for shader-local use

Every production shader consumer must use all three gates:

```text
global Dalapad shader additions enabled
  AND shader-specific Dalapad feature enabled
  AND pinned resource availability flag is true
```

If any gate is false, helpers return zero confidence. Shader debug masks that visualize Dalapad contribution should then be blank because there is no authorized Dalapad data for that shader path.

`Dalashade_FrameData.fxh` currently consumes `DALAPAD_PINNED_NORMAL` through the include. The pinned candidate is treated as strong normal-like/structure-like surface evidence when gates and confidence checks pass. It can lift surface normal confidence, contact support, AO receiver support, ground support, structure support, detail support, and reflection receiver support for first-party shaders that use `FrameSurfaceData`. It does not create effects where the consuming shader's own sky/skin/water/material safety model rejects them.

Production first-party shaders should prefer these fields:

| Field | Use |
| --- | --- |
| `surface.SurfaceDataInfluence` | Main opt-in amount for surface-aware shaping, regardless of whether NormalField, Dalapad, or both provided the evidence. |
| `surface.Normal` | Merged normal-like direction for effects that already use surface direction. |
| `surface.NormalConfidence` | Merged stability confidence. |
| `surface.DalapadInfluence` | Debug and tuning signal showing how much authorized Dalapad data reached the surface merge. |
| `surface.DalapadConfidence` | Debug and tuning signal for pinned-normal confidence before downstream effect safety. |

When Dalapad shader additions or Dalapad surface data are off, `surface.DalapadInfluence` and `surface.DalapadConfidence` are zero. Debug masks should show a true no-data state, not a simulated black sample.

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

User Mode exposes the global `Enable Dalapad shader additions` toggle and the shared `Use Dalapad surface data in first-party shaders` controls. When the global toggle is off, shader variables resolve to disabling values and first-party shaders behave as if Dalapad does not exist. When the global toggle is on but surface data is off, the addon/debug bridge may still be inspected, but production shader influence remains zero.

## Removal Boundary

Dalapad should stay easy to remove. Current cleanup is limited to:

- delete `Dalashade/DalapadDiagnostics.cs`
- delete `DalapadAddon/`
- remove the diagnostics calls from `Plugin`, `CompatibilityReportExporter`, `DebugBundleExporter`, and `ConfigWindow`
- remove this doc and index links

Shader cleanup is now required if the integration is removed:

- delete `shaders/Dalashade_Dalapad.fxh`
- remove `#include "Dalashade_Dalapad.fxh"` and Dalapad merge logic from `Dalashade_FrameData.fxh`
- remove `surface.SurfaceDataInfluence` Dalapad use assumptions from first-party production shaders if needed
- remove Dalapad shader variables from preset writer/mapper configuration
- remove User Mode and Developer Mode Dalapad shader-addition controls

## Next Safe Prototype

Only proceed if the render-layer and shader-helper evidence is useful in real debug bundles.

The next safe prototype should still be conservative:

- load `DalapadAddon/build/Dalapad.addon64` through ReShade and prove the status-file handshake plus control-pipe capability negotiation in-game
- verify metadata-only candidate resource catalog rows are stable across status-file and control-pipe IPC
- then run the developer-only resource shape probe and report whether candidate render-target manager rows can be observed safely in the plugin diagnostics
- collect lifecycle evidence across login, zone change, resolution change, ReShade reload, plugin reload, and debug shader reload
- do not expand copied GPU resources until the load/IPC/debug-shader lifecycle is stable
- expose only semantic pinned resources to first-party production shaders
- keep every consumer behind global and per-shader opt-in gates
- keep generated output disabling by default
- keep debug scan/group/MRT discovery separate from production shader consumption

## Implementation Route

The current conclusion is that a pure Dalamud plugin can inspect runtime metadata and possibly validate pointer/resource shape, but it cannot by itself make ReShade `.fx` shaders sample FFXIV render targets. ReShade shader sampling needs a bridge that registers or copies selected resources into something ReShade can bind by name.

Recommended stages:

1. Metadata inventory: keep the current reflection-only probe and collect real debug bundles across zones and sessions.
2. Diagnostic pointer probe: implemented as a developer-only opt-in plugin probe that invokes `RenderTargetManager.Instance` and reports whether candidate `GBuffer[0]`, `GBuffer[2]`, and depth/stencil resources are observable and plausibly shaped. It still does not copy, sample, register, or expose them.
3. Dalapad diagnostic control pipe: prove the separate native/ReShade addon can answer ping, self-test, status, and capability queries without touching render targets.
4. Diagnostic resource catalog: report candidate names, dimensions, formats, freshness, confidence, and reasons without sending raw handles or changing shaders by itself.
5. Synthetic debug visualization bridge: implemented through `Dalapad_Debug.fx` and addon-side synthetic texture upload into `Dalapad_DebugTexture`.
6. Dalapad bridge addon spike: implemented as optional named diagnostic textures, scan slots, semantic pinned candidates, and availability flags after metadata, shape, synthetic visualization, and lifecycle observations became useful.
7. Shader contract guard: implemented as `Dalashade_Dalapad.fxh` with global, local, and availability gates. When unavailable, helpers return zero confidence and shaders must behave exactly as they do now.
8. FrameSurfaceData merge: implemented. `FrameData` chooses gated Dalapad surface normals only when the bridge is present and confidence is high; otherwise it falls back to NormalField/default behavior.
9. SceneGI consumer: implemented through the shared FrameData surface contract with debug modes for contribution and raw evidence.
10. Broader first-party consumption: implemented as shared `surface.SurfaceDataInfluence` use in WeatherAtmosphere, AdaptiveGrade, SmartSharpen, AtmosphereBloom, SceneGI, ContactTone, and SurfaceReflection where the shader already has a surface-aware reason.
11. Compare and calibrate: add more debug views comparing external normal/diffuse/depth confidence against NormalField, screenshot material evidence, and MaterialIntent.
12. Realtime value bridge: after render-layer reliability is proven, prototype bounded live first-party shader deltas over the reserved control channel while keeping generated presets as fallback authority.
13. Opt-in production influence: continue tightening user/developer controls and per-shader debug masks before treating Dalapad as more than an experimental backend.

The intended shader-side shape is:

```text
if external surface data is enabled, available, and confidence is high:
    FrameSurfaceData normal/surface support is merged with Dalapad
else:
    FrameSurfaceData normal/surface support comes from NormalField and existing masks
```

This keeps Dalapad removable. Production shaders should not grow direct dependencies on FFXIVClientStructs, unmanaged pointers, or a required addon package.

## G-Buffer Candidates

Current research target:

- `RenderTargetManager.Instance()`: possible entry point for candidate render targets.
- `GBuffers[0]`: likely world-space normal candidate based on FFXIVClientStructs comments.
- `GBuffers[2]`: likely diffuse/albedo-like candidate based on FFXIVClientStructs comments.
- `DepthStencil`: likely depth/stencil candidate, but format, scaling, timing, and reverse-Z behavior still need proof.

These meanings are not production guarantees yet. The current shape probe only begins to verify nullability, dimensions, and format labels. It still needs repeated lifecycle testing, freshness proof, and transparent/chara-view buffer review before any shader consumes the data.
