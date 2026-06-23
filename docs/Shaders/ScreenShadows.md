# Dalashade_ScreenShadows

## Current purpose

`shaders/Dalashade_ScreenShadows.fx` is an optional first-pass screen-space shadow impression shader. It tries to add soft source-aware cast-shadow cues from visible bright/source pixels and local occluder/receiver evidence.

## Important limitation

ScreenShadows is not engine shadow mapping, ray tracing, or access to FFXIV light volumes. It only sees the current ReShade frame, depth, FrameData masks, and optional gated Dalapad candidate evidence. It cannot see off-screen lights, hidden occluders, real world-space light positions, or the game's internal shadow maps.

The correct expectation is subtle contact/cast-shadow suggestion, not physically correct cast shadows.

## Inputs

- Backbuffer color and ReShade depth.
- Shared `Dalashade_FrameData.fxh` base, scene, and surface fields.
- Optional NormalField surface confidence through FrameData.
- Optional gated Dalapad albedo/emissive candidate evidence through `Dalashade_Dalapad.fxh` helper paths.
- Generated ScreenShadows uniforms.

## Output

Normal output darkens qualified receiver pixels with a soft shadow mask. When `Dalashade_ScreenShadowsEnabled` is `0`, output is a pass-through.

## Generated uniforms

| Uniform | Meaning |
| --- | --- |
| `Dalashade_ScreenShadowsEnabled` | Master shader-local enable. Defaults to `0`. |
| `Dalashade_ScreenShadowsStrength` | Final shadow contribution strength. |
| `Dalashade_ScreenShadowsReach` | Screen-space tap reach. Performance tiers may scale this down. |
| `Dalashade_ScreenShadowsSoftness` | Softens and broadens the shadow response. |
| `Dalashade_ScreenShadowsSourceSensitivity` | Controls how easily visible bright/source pixels become shadow-casting candidates. |
| `Dalashade_ScreenShadowsDalapadInfluence` | Optional Dalapad source-evidence assist. Resolves to `0` unless Dalapad production shader support and surface data are enabled. |
| `Dalashade_ScreenShadowsDebugMode` | Selects the debug mask. |
| `Dalashade_ScreenShadowsDebugOutputMode` | Selects replacement, overlay, split, black, or amplified-difference debug output. |
| `Dalashade_ScreenShadowsDebugOpacity` | Debug overlay opacity. |
| `Dalashade_ScreenShadowsDebugBoost` | Debug mask boost. |

## Debug modes

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Off | Normal output. |
| 1 | Shadow mask | Final shadow mask after source, occluder, receiver, and safety gates. |
| 2 | Light source mask | Visible source confidence used by the screen-space shadow search. |
| 3 | Occluder mask | Local occluder evidence from depth/surface/frame data. |
| 4 | Receiver safety | Receiver safety mask. Black means the shader refuses to darken the pixel. |
| 5 | Dalapad evidence | Optional Dalapad source evidence allowed into this shader. Blank means Dalapad gates are off or no authorized data is present. |
| 6 | Final contribution | The actual contribution applied to the source image. |

## Performance tiers

| Tier | ScreenShadows behavior |
| --- | --- |
| Quality | Uses cardinal and diagonal screen-space taps. |
| Balanced | Uses the same shader path, with generated reach reduced by the shared first-party performance profile. |
| Performance | Uses cardinal taps only and lower generated reach. This favors cheaper FrameData/Dalapad evidence over expanding inferred screen-space work. |

## Safety rules

- Default-off; it does not affect output until enabled.
- Missing depth, missing Dalapad, stale Dalapad, or disabled Dalapad gates resolve neutral.
- Sky/fog, skin, water, bright highlights, and weak receiver pixels are suppressed.
- Source/emissive evidence is not receiver proof.
- Dalapad candidates are confidence hints, not material IDs or true engine light data.

