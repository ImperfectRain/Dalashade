# Dalapad Addon Contract

Resource contract version: `0.1-diagnostic`

IPC contract version: `0.1-ipc-diagnostic`

This contract defines what the first Dalapad bridge addon should expose when it eventually exists. Current Dalashade code only reports this contract; it does not consume these resources.

## IPC Status File

Stage 1 uses a status-file handshake before any live pipe or texture bridge:

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
- `debugVisualization`: synthetic debug texture bridge status
- `warnings`: array of plain-language warnings

The plugin currently treats this as diagnostics only. A compatible status file proves only that an addon can report itself; it does not prove that `.fx` code can sample render targets.

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

## Metadata-Only Resource Catalog

Stage 1.2 supports a resource catalog, but the catalog is still metadata-only. It names the expected normal, diffuse, and depth candidates and reports neutral shape fields until a later opt-in resource probe exists.

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

Stage 1.2 rows must keep:

- `available`: `false`
- `width` / `height`: `0`
- `format`: `unknown`
- `freshness`: `disabled`
- `confidence`: `0`
- `safetyState`: `metadata-only-unavailable`
- `metadataSource`: `static-contract`

This proves the resource catalog shape without sending texture handles, copying render targets, registering shader resources, or changing FrameData.

## Debug Visualization Bridge

Stage 1.3 adds a debug-only synthetic visualization bridge. It is intentionally separate from the real render-target resource catalog.

The addon looks for this ReShade FX texture variable:

```text
Dalapad_DebugTexture
```

in:

```text
Dalapad_Debug.fx
```

When the shader is loaded, the addon uploads a generated 256x256 RGBA checker/gradient into that texture variable using ReShade's effect runtime `update_texture` path. This proves that the addon can feed a ReShade `.fx` texture without touching XIV render targets.

`debugVisualization` should include:

- `version`: currently `0.1-synthetic-texture`
- `enabled`
- `status`: `WaitingForShader`, `TextureFound`, `SyntheticUploaded`, or `NoReShadeRuntime`
- `source`: currently `synthetic`
- `shader`
- `textureName`
- `shaderTextureFound`
- `syntheticTextureUploaded`
- `usesSyntheticTexture`
- `width`
- `height`
- `frameCounter`
- `frameAge`
- `readsRenderTargets`: must remain `false`
- `copiesRenderTargets`: must remain `false`
- `registersGameResources`: must remain `false`
- `reason`

This bridge is allowed to update only synthetic pixels. It must not copy, sample, register, or expose `GBuffers`, `DepthStencil`, or any other XIV resource.

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
- Never bypass `FrameData`; production consumers should only see merged surface data after an explicit integration pass.
- Never treat diffuse as material ID truth.
- Never treat normal/depth data as stable until timing, dimensions, format, and transparency behavior are validated.
