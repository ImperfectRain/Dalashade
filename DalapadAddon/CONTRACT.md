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

## Future Live-Control Channel

Reserved pipe:

```text
\\.\pipe\Dalapad.Control.v1
```

Reserved message family:

```text
Dalapad.RealtimeUniforms.v1
```

These names exist only to prevent contract drift. The plugin does not open this pipe in Stage 1. Future realtime value movement must stay lower priority than render-layer validation, must be opt-in, and must respect generated preset fallback and first-party shader write gates.

## Safety Rules

- Default unavailable.
- Debug-only until proven stable.
- Status-file IPC before live pipe IPC.
- Never make `.fx` code fail to compile when the bridge is missing.
- Never require production shaders to sample these resources directly.
- Never bypass `FrameData`; production consumers should only see merged surface data after an explicit integration pass.
- Never treat diffuse as material ID truth.
- Never treat normal/depth data as stable until timing, dimensions, format, and transparency behavior are validated.
