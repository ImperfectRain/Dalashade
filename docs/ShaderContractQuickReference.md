# Shader Contract Quick Reference

Use this page before adding or changing first-party Dalashade shader code.

## Include Ownership

| Include | Use it for | Do not use it for |
| --- | --- | --- |
| `Dalashade_FrameData.fxh` | Normalized shader-facing scene, material, water, safety, receiver, NormalField, and optional Dalapad surface data. | Replacing `MaterialMasks`, inventing new global material truth, or sampling raw addon textures directly. |
| `Dalashade_Dalapad.fxh` | Approved helper path for gated semantic pinned Dalapad candidates. | Raw scan slots, group/MRT exploration in production shaders, or mandatory shader behavior. |
| `Dalashade_MaterialMasks.fxh` | Pixel material, water, receiver, source, and safety classification. | Scene-level MaterialIntent generation or engine material ID claims. |
| `Dalashade_NormalField.fxh` | Inferred screen-space normal/surface support when true surface data is absent or disabled. | True FFXIV normals, material normals, roughness, metallic, or motion vectors. |

## Required Rules

- Production shaders should start from `Dalashade_FrameData.fxh`.
- Production shaders should not duplicate classifier logic from `Dalashade_MaterialMasks.fxh` unless the shader has a narrow effect-specific shaping reason.
- Production shaders should not sample raw Dalapad semantic textures directly unless the helper path is explicitly approved for that shader.
- Production shaders must fail neutral when optional data is missing, disabled, stale, unavailable, or not present in the generated preset.
- Debug shaders must not be auto-enabled by technique sync.
- Debug shaders should expose what the shared contracts believe without changing production behavior.

## Truth Boundaries

- Scene tags describe context. They are not material IDs.
- `MaterialProfile` is broad plausibility. It is not pixel truth.
- `ScreenshotMaterialEvidence` is inferred visible evidence. It can be wrong with UI-heavy or cropped screenshots.
- `MaterialIntent` is a scene-level shader prior. It is not an engine material ID and should not affect the whole frame by itself.
- Albedo/diffuse candidates are not material ID truth.
- Source/emissive data is not receiver proof.
- Water source, sky source, and horizon-only data are not water receiver proof.
- Pinned Dalapad candidates are semantic candidates. Treat them as confidence-weighted evidence, not as guaranteed XIV render contracts.

## Recommended Production Flow

```text
FrameData
  -> MaterialMasks resolves pixel safety/material/water/receiver roles
  -> NormalField contributes inferred surface support when enabled
  -> Dalapad contributes gated semantic surface evidence when enabled and available
  -> shader-local math applies its own effect budget and safety gates
  -> missing optional data returns neutral behavior
```

## Debug Expectations

- A blank Dalapad contribution mask is correct when Dalapad shader additions, surface data, resource availability, or shader-local gates are off.
- A visible raw pinned debug sample with blank production contribution means the bridge may work but the production shader rejected the data through safety or confidence gates.
- MaterialDebug answers what shared material classification thinks.
- NormalDebug answers what inferred NormalField thinks.
- FrameDataDebug answers what shared FrameData exposes to production shaders.
- Per-shader debug modes answer why that shader affected or suppressed its own output.
