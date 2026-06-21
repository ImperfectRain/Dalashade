# Dalapad Addon Contract

Resource contract version: `0.1-diagnostic`

IPC contract version: `0.1-ipc-diagnostic`

This contract defines the current Dalapad bridge boundary. Dalashade can report addon diagnostics, inspect debug scan/pinned candidates through `Dalapad_Debug.fx`, and optionally let first-party shaders consume gated pinned data through `shaders/Dalashade_Dalapad.fxh`. Missing, stale, or disabled Dalapad data must always resolve to neutral shader behavior.

## IPC Status File

Stage 1 uses a status-file handshake before any production shader dependency:

```text
<XIVLauncher plugin config>/Dalashade/Dalapad/dalapad-status.json
```

The plugin reads this file if present. Missing or invalid files are neutral and cannot affect generated presets, shader values, shader compilation, or effect activation.

Minimum fields:

- `ipcContractVersion`: must be `0.1-ipc-diagnostic`
- `contractVersion`: resource contract version, currently `0.1-diagnostic`
- `bridgeVersion`: addon/prototype version string
- `addonProcess`: short producer name
- `status`: `Loaded`, `SelfTest`, `Stopped`, `Unavailable`, `ResourcesUnavailable`, `ResourcesCandidate`, or similar
- `summary`: plain-language status
- `lastUpdateUtc`: ISO-8601 UTC timestamp
- `resources`: array of resource status rows
- `debugVisualization`: debug texture bridge status for synthetic, scan, or pinned candidate views
- `warnings`: array of plain-language warnings

The plugin treats this file as diagnostics and capability evidence. A compatible status file is not authority for shader sampling by itself; shader use is separately gated by generated-preset settings, shader-local toggles, and ReShade semantic texture availability.

## Resources

| Name | Kind | Source Candidate | First Use |
| --- | --- | --- | --- |
| `Dalapad_SurfaceNormal` | optional texture | `RenderTargetManager` G-buffer normal candidate, expected first target `GBuffers[0]` after validation | Compare against NormalField and FrameData surface normal debug output. |
| `Dalapad_SurfaceDiffuse` | optional texture | `RenderTargetManager` diffuse/albedo-like candidate, expected first target `GBuffers[2]` after validation | Compare broad material/color evidence against ScreenshotMaterialEvidence and MaterialIntent. |
| `Dalapad_SurfaceDepth` | optional texture | `RenderTargetManager.DepthStencil` candidate after format/scaling validation | Compare ReShade depth reliability against runtime depth shape. |
| `Dalapad_SurfaceStatus` | status/uniform block | bridge runtime | Report bridge availability, dimensions, freshness, confidence, and active safety state. |

## Availability Flags

| Flag | Meaning |
| --- | --- |
| `Dalapad_BridgeAvailable` | The bridge loaded and reported a compatible contract version. |
| `Dalapad_NormalAvailable` | The normal candidate is present, fresh, and safe enough for diagnostics. |
| `Dalapad_DiffuseAvailable` | The diffuse candidate is present, fresh, and safe enough for diagnostics. |
| `Dalapad_DepthAvailable` | The depth candidate is present, fresh, and safe enough for diagnostics. |

## Required Status Fields

A bridge resource status record should include:

- contract version
- addon version
- frame index or timestamp
- resource dimensions
- resource format names if available
- freshness/staleness state
- confidence per resource
- reason text for disabled or unsafe resources

## Resource Catalog

Stage 1.2 introduced a resource catalog. The catalog can name expected normal, diffuse/albedo, depth, scan, and pinned candidates. It is still a diagnostic contract, not a permission slip for production shaders to depend on a resource.

Each `resources[]` row should include:

- `name`
- `kind`
- `availabilityFlag`
- `available`
- `source`
- `width`
- `height`
- `format`
- `freshness`
- `confidence`
- `safetyState`
- `metadataSource`
- `reason`

Static contract rows should keep:

- `available`: `false`
- `width` / `height`: `0`
- `format`: `unknown`
- `freshness`: `disabled`
- `confidence`: `0`
- `safetyState`: `metadata-only-unavailable`
- `metadataSource`: `static-contract`

Runtime rows may report observed dimensions, freshness, confidence, and reason text when the addon has inspected a candidate. Resource rows still must not send raw handles over IPC. Debug copies and ReShade aliases are exposed only through addon-owned diagnostic resources and named semantic bindings.

## Debug Visualization Bridge

Stage 1.3 added a debug visualization bridge. It is intentionally separate from production shader behavior.

The addon looks for this ReShade FX texture variable:

```text
Dalapad_DebugTexture
```

in:

```text
Dalapad_Debug.fx
```

When the shader is loaded, the addon can upload generated synthetic pixels into that texture variable using ReShade's effect runtime `update_texture` path. Later debug passes can also bind addon-owned scan and pinned candidate copies through `Dalapad_Debug.fx` aliases. This proves visibility and lifetime behavior before any broad production use.

`debugVisualization` should include:

- `version`: currently `0.1-debug-visualization`
- `enabled`
- `status`: `WaitingForShader`, `TextureFound`, `SyntheticUploaded`, or `NoReShadeRuntime`
- `source`: `synthetic`, `scan`, `pinned`, or equivalent diagnostic source label
- `shader`
- `textureName`
- `shaderTextureFound`
- `syntheticTextureUploaded`
- `usesSyntheticTexture`
- scan and pinned candidate availability/dimensions when available
- `width`
- `height`
- `frameCounter`
- `frameAge`
- `readsRenderTargets`
- `copiesRenderTargets`
- `registersGameResources`: must remain `false`
- `reason`

This bridge is allowed to update synthetic pixels and bind addon-owned diagnostic copies. It must not expose raw game handles through IPC or make any copied resource mandatory for normal Dalashade output.

## First-Party Shader Integration

First-party shader consumption must go through:

```text
shaders/Dalashade_Dalapad.fxh
```

The include owns the ReShade semantic texture declarations, availability uniforms, common sampling helpers, normal-like decode helpers, scalar evidence helpers, and zero-confidence fallback rules.

The required gates are:

- global generated-preset gate: `Dalashade_DalapadEnabled`
- shader-local feature gate, for example `Dalashade_DalapadSceneGINormalAssist`
- resource availability and valid dimensions, for example `Dalapad_PinnedNormalAvailable`
- shader-local strength greater than zero

When any gate is closed, helper confidence, presence, and contribution masks must resolve to zero. Debug masks must therefore be blank because there is no authorized Dalapad data to show, not because the debug technique silently hid a valid signal.

Current first-party consumer:

- `Dalashade_SceneGI.fx` can optionally use the pinned normal-like candidate as a conservative structure/normal assist.
- SceneGI debug mode `18` shows the authorized Dalapad contribution mask.
- SceneGI debug mode `19` shows gated raw/evidence data.

## Diagnostic Control Channel

Stage 1.1 adds a diagnostic named pipe:

```text
\\.\pipe\Dalapad.Control.v1
```

The pipe is plugin-to-addon request/response IPC. It currently supports diagnostic commands only:

- `Ping`
- `BridgeSelfTest`
- `QueryStatus`
- `QueryCapabilities`
- `QueryDebugVisualization`
- `SetDebugVisualization`

Requests and responses are newline-terminated JSON objects with:

- `contract`: must be `Dalapad.Control.v1`
- `id`: request/response correlation id
- `type`: command name
- `timestampUtc`: request timestamp when provided by the plugin

`QueryCapabilities` must report the current safety boundary explicitly:

- `supportsStatusFile`: `true`
- `supportsControlPipe`: `true`
- `supportsRealtimeUniforms`: `false`
- `supportsResourceCatalog`: `true`
- `supportsDebugVisualization`: `true`
- `readsRenderTargets`: `false`
- `copiesRenderTargets`: `false`
- `registersShaderResources`: `false`
- `movesRealtimeShaderValues`: `false`

The plugin uses short timeouts. Missing, blocked, invalid, or incompatible pipe responses are neutral diagnostics and cannot change generated presets, shader values, shader compilation, or effect activation.

Reserved future message family:

```text
Dalapad.RealtimeUniforms.v1
```

Realtime names exist only to prevent contract drift. Realtime value movement must stay lower priority than render-layer validation, must be opt-in, and must respect generated preset fallback and first-party shader write gates.

## Safety Rules

- Default unavailable.
- Debug-only until proven stable.
- Status-file IPC before live pipe IPC.
- Diagnostic pipe before resource catalog IPC.
- Never make `.fx` code fail to compile when the bridge is missing.
- Never require production shaders to sample these resources directly.
- First-party shader use must be opt-in and routed through `Dalashade_Dalapad.fxh`.
- Never bypass shader-local safety gates or generated-preset fallback.
- Never treat diffuse as material ID truth.
- Never treat normal/depth data as stable until timing, dimensions, format, and transparency behavior are validated.
