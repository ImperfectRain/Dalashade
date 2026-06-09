# Dalashade

Dalashade is a Dalamud plugin that writes a ReShade preset for the game state you are actually in.

The idea is simple: FFXIV changes a lot. A preset that looks great in a quiet night scene can get weird in combat, washed out in bright interiors, or crunchy when the weather rolls in. Dalashade tries to keep the preset pointed in the right direction without making you hand-author a giant spreadsheet of zone rules.

This is early. It works by carefully editing a generated `.ini` preset, not by talking to ReShade's renderer directly. That means it is intentionally conservative: small adjustments, known shader variables only, and no touching your base preset.

## What It Does

- Watches territory, combat, cutscene, night/day, and zone-entry weather.
- Classifies the current place as a city, field zone, duty, or interior-ish space.
- Optionally analyzes the newest screenshot in a folder for brightness, contrast, saturation, crushed shadows, and clipped highlights.
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

## Screenshot Analysis

Screenshot analysis is optional.

Turn on `Auto-adjust from screenshots`, set the screenshot folder, then take screenshots as you move around. Dalashade reads the newest image and uses rough scene metrics to nudge the generated preset:

- dark or crushed scenes get more lift and less heavy AO
- bright or clipped scenes back off exposure and bloom
- very muted scenes get a little saturation
- oversaturated scenes get cooled down a bit
- very flat scenes get a little contrast and clarity

It is not live video analysis yet. Think of it as the first rung on the ladder before a ReShade add-on bridge.

## iMMERSE Support

Free iMMERSE support is on by default for installed preset variables such as MXAO and Sharpen.

The Pro/Ultimate toggle only changes values that already exist in your preset. If RTGI, ReGrade+, ReLight, or other paid effects are not in the preset, Dalashade leaves them alone. This keeps the free path free and avoids pretending paid shaders are required.

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
- world and screenshot feedback
- conservative shader mapping
- clean generated-preset workflow

Next sensible steps are live weather refresh between zone changes, better content-type classification, and eventually a ReShade add-on bridge so this stops relying on preset reloads.
