# Dalashade

Dalashade is a Dalamud plugin that writes a ReShade preset for the game state you are actually in.

The idea is simple: FFXIV changes a lot. A preset that looks great in a quiet night scene can get weird in combat, washed out in bright interiors, or crunchy when the weather rolls in. Dalashade tries to keep the preset pointed in the right direction without making you hand-author a giant spreadsheet of zone rules.

This is early. It works by carefully editing a generated `.ini` preset, not by talking to ReShade's renderer directly. That means it is intentionally conservative: small adjustments, known shader variables only, and no touching your base preset.

## What It Does

- Watches territory, combat, cutscene, night/day, and current weather.
- Uses Dalamud state like GPose, duty state, content finder info, Eorzea time, and client weather data to infer what kind of scene you are in.
- Classifies the current place as a city, field zone, dungeon/trial/raid-like duty, or interior-ish space.
- Optionally analyzes the newest screenshot in a folder for brightness, contrast, saturation, crushed shadows, and clipped highlights.
- Optionally analyzes a master image folder, so you can point it at a look you like and let Dalashade bias the generated preset toward it.
- Shows the actual preset variables it changed, with old and new values, plus whether that shader is active in ReShade.
- Scans your base preset for shader variables Dalashade can really control.
- Analyzes the whole active preset stack for compatibility risk, unsupported effects, high-risk effects, and visual authorities.
- Generates a separate ReShade preset from your chosen base preset.
- Supports free iMMERSE variables by default.
- Can also adjust installed iMMERSE Pro/Ultimate variables when you turn that option on.

## What It Does Not Do

- It does not bundle iMMERSE, iMMERSE Pro, iMMERSE Ultimate, RTGI, or any paid shader files.
- It does not modify your base preset in place.
- It does not capture live frames yet.
- It does not magically know taste. It has opinions, but they are intentionally mild.

## Basic Setup

1. Install ReShade and your shader packs normally.
2. In ReShade, make or pick a base preset you already like.
3. In Dalashade, set `Base preset path` to that preset.
4. Click `Use Dalamud config folder` for the generated preset path.
5. Click `Generate Now`.
6. In ReShade, load the generated preset.

The generated preset should live somewhere writable, usually Dalamud's plugin config folder. Keeping it away from the game folder avoids a lot of Windows permission nonsense.

Dalashade can also try to reload ReShade after it writes the preset. By default it syncs ReShade's reload hotkey to `F5` in `ReShade.ini`, then sends that key after a successful generation. If you already use another reload key, click `Set reload hotkey` in settings and press the key or button you want. Hold Ctrl, Shift, or Alt while pressing it if you use a combo. The settings window also has `Test Reload` so you can check the reload path without regenerating a preset.

Reload is best-effort because ReShade input can depend on focus, overlays, Windows permissions, and whether the game/plugin are running at the same privilege level. If it does not work, manually bind ReShade reload to the same key Dalashade shows and keep both processes at the same privilege level.

## Screenshot Analysis

Screenshot analysis is optional.

Turn on `Auto-adjust from screenshots`, set the screenshot folder, then take screenshots as you move around. Dalashade reads the newest image and uses rough scene metrics to nudge the generated preset:

- dark or crushed scenes get more lift and less heavy AO
- bright or clipped scenes back off exposure and bloom
- very muted scenes get a little saturation
- oversaturated scenes get cooled down a bit
- very flat scenes get a little contrast and clarity

It is not live video analysis yet. Think of it as the first rung on the ladder before a ReShade add-on bridge.

The screenshot sampler can use the full image, but the default is center-weighted because game UI, chat, hotbars, ReShade windows, and letterboxing can lie to the analysis. If your screenshots are clean GPose captures, the GPose clean sampler is there too.

## Passive Scene Classification

Dalashade tries hard not to become a giant zone table.

Instead of hand-tuning `Central Shroud = these values` forever, it reads what Dalamud already knows and turns that into reusable tags:

- night, dawn, day, dusk
- rain, fog, snow, storm, clear
- city/social, field/exploration, dungeon, raid/trial, interior
- combat, duty, cutscene, GPose

Those tags feed generic rules. Rain pulls back bloom and saturation a little. Duties favor readability. GPose can go prettier. Night lifts shadows and relaxes AO. Weather is refreshed from the client weather manager during normal updates, with zone-entry weather only kept as a fallback. It is all category-based, so a new zone should still get sane behavior without anyone manually adding it.

## Master Preset Images

This is the part for stealing a vibe, in the harmless color-grading sense.

Set `Master preset image folder` to a folder with one or more reference screenshots. It can be an FFXV screenshot, a movie frame, a moody GPose shot, whatever. Dalashade analyzes the image style, then nudges the generated preset toward that look.

Right now it looks at broad visual traits:

- overall brightness
- contrast
- saturation
- shadow crush
- highlight clipping
- warm/cool color bias
- green/magenta-ish bias

If `Include master preset subfolders` is on, you can make a folder of looks and toss subfolders inside it. Dalashade can use the newest image, average the folder, take median values, or pick the reference closest to the current scene. Closest-to-current is the default because a dark blue night reference usually makes more sense for a dark blue scene than averaging it together with a sunny beach shot.

This will not perfectly recreate another game's renderer. It is more like, "this reference is warmer, punchier, and less shadow-crushed than my current scene, so move the ReShade values that direction." It is allowed to be visible now, especially at higher strength, but it is still working through shader variables instead of cloning a whole renderer.

## iMMERSE Support

Free iMMERSE support is on by default for installed preset variables such as MXAO and Sharpen.

The Pro/Ultimate toggle only changes values that already exist in your preset. If RTGI, ReGrade+, ReLight, or other paid effects are not in the preset, Dalashade leaves them alone. This keeps the free path free and avoids pretending paid shaders are required.

The writer is section-aware and defaults to strict section matching, so it edits variables inside the matching shader section, like `[MartysMods_MXAO.fx]`. You can loosen this to known fallbacks or loose key matching for compatibility, but strict is safer for normal use because generic names like `Exposure` and `Contrast` can exist in more than one shader. Loose key matching is there for experimenting with unsupported presets, not because it is the clever default.

The mapper also treats shader values differently depending on what the INI value represents. Strength/amount/radius values are multiplied. Zero-centered grading offsets like exposure, contrast, saturation, gamma, tone curve, tint, and temperature are added to. Thresholds and black/white point controls get small relative offsets instead of raw scaling, because scaling a value like `INPUT_BLACK_LVL=0` or a delicate threshold can either do nothing or jump too hard.

The mapper has scalar and vector value plumbing now. Most mappings are still scalar, so Rain/iMMERSE behavior should stay familiar, but Dalashade can also handle ReGrade+ Colorista HSL vectors without pretending they are single floats.

Use `Scan Shader Support` to see what Dalashade found in your base preset. Dalashade reads the top-level `Techniques=` list and marks supported variables as active or inactive. That matters because a preset can contain a `[MartysMods_MXAO.fx]` section while MXAO itself is not currently enabled, so the value is real but will not be visible yet.

The `Inactive shader writes` option controls how much of that preset scaffolding Dalashade edits:

- `Never` only writes variables for currently active techniques.
- `Supported inactive sections` also updates known shader sections that exist in the preset but are not active yet.
- `Always matching keys` is the loosest mode and is mainly for testing odd presets.

Use the `Changed variables` panel after generation to see the exact values it wrote. If a generated value hits a shader's min or max, Dalashade marks it as clamped so you can tell when a rule wanted to go further than the shader variable safely allows.

Backups are capped by `Max generated preset backups` so automatic generation does not fill the config folder forever. The default keeps the latest 10 generated backups.

## Preset Compatibility Report

`Scan Preset Compatibility` looks at the preset as a full visual stack before Dalashade tries to get clever with it.

It reads:

- active techniques from `Techniques=`
- sorted or available techniques from `TechniqueSorting=`
- shader sections already present in the preset
- known controlled variables
- risky ReGrade+ hue, saturation, and Colorista HSL entries

The report classifies effects by role: color grade, tonemap, bloom, AO/GI, sharpen, anti-aliasing, deband, clarity, LUT, diffusion, DOF, film grain, vignette, UI/utility, or unknown. It also marks support as fully controlled, partially controlled, detected-only, or unsupported, then picks primary/secondary authorities for the main visual roles.

Detected-only effects are important: Dalashade recognizes what they probably do, but does not control their variables yet. They can still dominate the image, so the UI separates them from fully controlled, partially controlled, and unknown effects.

The compatibility mode selector now controls the first small slice of ReGrade+ color safety. It scales ReGrade+ scalar tonal hue/saturation values toward neutral: `E_SHADOWS_HUE`, `E_SHADOWS_SAT`, `E_MIDTONES_HUE`, `E_MIDTONES_SAT`, `E_HIGHLIGHTS_HUE`, and `E_HIGHLIGHTS_SAT`. It also moves ReGrade+ Colorista HSL vectors like `E_COLORISTA_HSL_GREEN_V2` toward neutral proportionally. `Preserve base` and `GPose preserve` keep those values intact, `Cinematic preserve` keeps most of them, `Adaptive balanced` softens them, and `Gameplay sanitize` pulls them down hard.

`Export Compatibility Report` writes a Markdown report into the plugin config folder with active techniques, authorities, warnings, shader support, changed variables, inactive edits, and clamp hits. This should make preset debugging much less hand-wavy.

This still does not disable techniques or broadly sanitize every color shader. The current sanitize behavior is limited to ReGrade+ scalar tonal color controls and ReGrade+ Colorista HSL vectors, so Rain/iMMERSE-style presets stay protected while heavier third-party presets can start becoming safer in balanced/gameplay modes.

## Optional NormalField Diagnostics

NormalField is optional and disabled by default. It is currently a diagnostic/shared-data layer for future first-party shader improvements, not true FFXIV material normals, G-buffer access, or texture normal maps. See `docs/NormalField.md` for configuration, debug modes, and the test plan.

## Scene Lock

`Lock current generated preset` pauses automatic regeneration. Manual `Generate Now` still works. This is for the moments where the preset looks good and you want Dalashade to stop reacting to time, weather, screenshots, or combat for a while.

## Biome Hints

Dalashade still avoids a giant zone table, but it now infers a primary biome plus supporting tags from territory names, content names, and weather. Strong matches such as La Noscea/coastal, Rak'tika/rainforest, Amh Araeng/desert, Coerthas/snow, Solution Nine/neon, Ultima Thule/cosmic, Mare Lamentorum/lunar, and Garlemald/industrial get clearer visual identity. Lower-confidence matches stay conservative.

The tag system is meant to give each environment its own direction without making every zone look the same: coast stays bright and clean, jungle stays lush and deep, desert stays warm and dry, snow stays crisp, high-tech zones keep controlled neon, and combat/dungeons still prioritize readability.

## Building

Open `Dalashade.sln` or run:

```powershell
dotnet build
```

The debug build outputs to:

```text
Dalashade/bin/x64/Debug/Dalashade.dll
```

For dev loading, add that DLL path in Dalamud's dev plugin settings.

## Current Shape

This is the practical MVP:

- context-aware preset generation
- world, screenshot, and reference-image feedback
- conservative shader mapping
- clean generated-preset workflow

Next sensible steps are live weather refresh between zone changes, better content-type classification, and eventually a ReShade add-on bridge so this stops relying on preset reloads.
