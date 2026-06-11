# ReShade Reload

This page documents the current ReShade reload behavior.

## Implemented

Reload support lives in `Dalashade/ReShadeController.cs`.

Important types and methods:

| Item | Purpose |
| --- | --- |
| `ReShadeController.ReloadAfterPresetWrite(...)` | Attempts reload after generation. |
| `ReShadeController.TestReload(...)` | Runs the same reload path without generating a preset. |
| `ReShadeController.AutoDetectReShadeIni(...)` | Searches likely locations for `ReShade.ini`. |
| `ReloadDiagnostics` | Reports path, key, sync, and input-send results. |

## Current Approach

Dalashade reloads ReShade by using the configured ReShade reload hotkey.

The current implementation can:

1. Find `ReShade.ini`.
2. Read `KeyReload`.
3. Optionally sync ReShade's reload key to Dalashade's configured reload key.
4. Send the reload hotkey with Windows input/message calls.

This is best-effort. It depends on ReShade, the game window, focus, Windows input handling, and hotkey configuration.

## ReShade.ini Path

`Configuration.ReShadeIniPath` is the preferred path when it exists.

Search priority:

1. `Configuration.ReShadeIniPath`
2. Existing auto-detect search behavior in `ReShadeController.TryFindReShadeIni`

The UI exposes:

| Control | Purpose |
| --- | --- |
| ReShade.ini path | Manual path entry. |
| Browse | Select a path. |
| Auto-detect | Search likely locations. |
| Test Reload | Try reload without generating a preset. |

## Reload Key Sync

Hotkey sync is optional. When enabled, Dalashade writes the configured reload key combo to `ReShade.ini` so ReShade and Dalashade use the same key.

If sync is off, Dalashade still attempts to send the configured key, but ReShade may be listening for a different key.

## Diagnostics

Reload diagnostics report:

| Field | Meaning |
| --- | --- |
| ReShade.ini found | Whether a usable ini path was found. |
| Path used | Which path was used. |
| KeyReload value | Reload key found in `ReShade.ini`. |
| Configured Dalashade reload key | The key combo Dalashade will send. |
| Hotkey sync enabled | Whether Dalashade writes the key to ReShade.ini. |
| PostMessage result | Whether the window message attempt succeeded. |
| SendInput result | Whether the SendInput attempt succeeded. |

## Common Failure Cases

| Failure | Explanation |
| --- | --- |
| `ReShade.ini` not found | Base/generated presets may live in the plugin config folder, not near the ReShade install. Set the path manually. |
| `KeyReload` not found | ReShade.ini may not have a reload key configured yet. |
| SendInput failed | Windows input injection failed or focus/window state prevented it. |
| PostMessage succeeded but no reload | ReShade may ignore the message path, or the game window may not be the right target. |
| Correct key but no reload | ReShade may not be focused, the overlay may be open, or another program may intercept the combo. |

Users may still need to manually reload ReShade.

## Planned / Future: ReShade Bridge Or Add-On

This system is planned and not currently implemented. Do not treat this document as an implementation reference yet.

A future bridge, native ReShade add-on, IPC channel, named pipe, or JSON live-state output could provide a stronger reload mechanism. None of those systems currently exist in this repository, and documentation should not describe them as implemented until real code is added.
