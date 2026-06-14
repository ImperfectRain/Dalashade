# Safety and Scope

Dalashade is a local visual adaptation tool. It generates a separate ReShade preset and provides optional first-party ReShade shaders for visual tone, material-aware diagnostics, and restrained post-process effects.

## Boundaries

Dalashade does not:

- automate gameplay
- read or inject network packets
- control player input for gameplay
- track players
- identify mechanics
- make decisions for the player
- provide combat automation
- require paid shader files
- modify the user's base preset in place

The ReShade reload hotkey path is a best-effort local reload convenience after preset generation. It should not be expanded into gameplay input automation.

## Dalamud Boundary

The plugin reads local Dalamud/game context such as territory, weather, time bucket, combat/duty/cutscene/GPose state, and configured paths. It writes Dalashade configuration, generated presets, reports, and debug bundles.

It should remain a passive local tool that converts context into visual preset values.

## ReShade Boundary

Dalashade writes known variables in generated `.ini` preset sections. It does not directly control ReShade rendering internals, copy third-party shader files, or install shader packs. First-party Dalashade shaders are optional `.fx` files that users install/enable through normal ReShade workflows.

## Combat and Readability

Combat/readability systems must remain visual comfort and clarity aids:

- reduce overly cinematic pressure
- dampen haze/bloom/sharpen intensity
- preserve visibility
- avoid excessive darkness or contrast

They must not identify mechanics, call out safe spots, automate responses, or provide non-visual gameplay advantage.

## Debug Bundle Safety

Debug bundles include Dalashade config, generated/base/active presets when available, scene/material diagnostics, compatibility reports, shader file status, and environment/path summaries.

They should not include arbitrary user files, credentials, tokens, or full logs by default. Log excerpts, if added later, should be opt-in or clearly limited.

## Official Review Notes

When preparing for review or release, emphasize:

- local preset generation
- no base preset mutation
- optional shader support
- no gameplay automation
- no network hooks
- no mechanic detection
- no paid shader bundling
- conservative defaults and disabled experimental features

## Do Not Do

- Do not add gameplay-triggered automation.
- Do not make visual debug systems active by default.
- Do not include unrelated user files in exports.
- Do not blur the distinction between local visual adaptation and game-assist tooling.
