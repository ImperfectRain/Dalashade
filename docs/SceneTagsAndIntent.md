# Scene Tags And Intent

This page documents the implemented scene-tag behavior in Dalashade.

## Implemented

Scene context is collected in `Dalashade/GameContext.cs`.

| Area | Implemented location | Notes |
| --- | --- | --- |
| Raw game context | `GameContextService.Refresh()` | Reads territory, content, duty/combat/cutscene/GPose flags, Eorzea time, and weather. |
| Territory lookup | `GameContextService.Refresh()` | Uses `DataManager.GetExcelSheet<TerritoryType>()`. |
| Weather lookup | `GameContextService.RefreshWeather()` | Uses `FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUIModule()->GetWeatherModule()` first, then a territory fallback. |
| Time buckets | `GameContextService.GetTimeBucket()` | Converts Eorzea hour into Dawn, Day, Dusk, or Night. |
| Scene tags | `SceneClassifier.Classify(GameContext context)` | Converts raw context into weather, area, biome, and gameplay tags. |
| Visual profile effects | `ProfileEngine.Create()` in `Dalashade/VisualProfile.cs` | Applies context-based adjustments through methods such as `ApplyWeather`, `ApplyTerritory`, `ApplyBiome`, `ApplyGameplayClarity`, and `ApplyCutscene`. |

## Current Weather Tags

`SceneClassifier.Classify` currently emits broad weather tags:

| Tag | Trigger examples |
| --- | --- |
| `rain` | Weather name contains `rain` or `showers`. |
| `fog` | Weather name contains `fog`, `gloom`, `cloud`, `overcast`, or `mist`. |
| `snow` | Weather name contains `snow` or `blizzard`. |
| `storm` | Weather name contains `storm`, `thunder`, or `gales`. |
| `clear` | No rain, fog, snow, or storm tag matched. |

Weather affects the visual profile in `ProfileEngine.ApplyWeather`.

## Current Territory, Area, And Biome Tags

Area tags are inferred in `SceneClassifier.Classify`:

| Tag | Current behavior |
| --- | --- |
| `dungeon` | In duty and content name resembles dungeon, deep dungeon, variant, or criterion. |
| `raid` | In duty and content name resembles raid, trial, ultimate, savage, or unreal. |
| `city` | In sanctuary and not in duty. |
| `interior` | Territory world category is Interior, or the scene is tagged dungeon or raid. |
| `field` | Not duty, not city, and not interior. |

Biome tags are inferred in `SceneClassifier.InferBiome` from territory and region names:

| Tag | Current trigger examples |
| --- | --- |
| `snow` | Snow, ice, coerthas, garlemald. |
| `forest` | Forest, shroud, gridania, rak'tika, sylph. |
| `desert` | Desert, thanalan, uldah, amh araeng. |
| `cave` | Cave, cavern, mine, underground. |
| `void` | Void, darkness, world of darkness. |
| `aetherial` | Aether, crystal, elpis, ultima thule. |
| `coastal` | Sea, coast, ocean, limsa, island. |
| `fire` | Fire, volcano, inferno. |
| `overcast` | Gloom, fog, cloud. |
| `neutral` | Fallback when no specific biome matched. |

Biome and territory tags affect the visual profile in `ProfileEngine.ApplyTerritory` and `ProfileEngine.ApplyBiome`.

## Gameplay, Combat, Cutscene, And GPose

`SceneTags` includes:

| Field | Implemented meaning |
| --- | --- |
| `NeedsGameplayClarity` | True when the player is in combat or duty content. |
| `CinematicAllowed` | True when the player is not in combat and is either not in a cutscene or is in GPose. |

These values feed `ProfileEngine.ApplyGameplayClarity` and `ProfileEngine.ApplyCutscene`.

## Current Stacking Behavior

Profile creation is additive and clamped. `ProfileEngine.Create()` starts from neutral values, applies context rules in sequence, then clamps through `VisualProfile.Clamp()`.

This means tags can stack. For example, night, rain, combat, and dungeon adjustments can all influence the same profile. This is intentional, but broad tag changes can have wide effects.

## Planned / Future: SceneIntent

This system is planned and not currently implemented. Do not treat this document as an implementation reference yet.

`SceneIntent` is intended to become the normalized intermediate layer between game context tags and visual profile output. The likely purpose is to convert raw tags such as weather, biome, combat, and cutscene state into a smaller set of explicit visual intents before `VisualProfile` values are changed.

Expected future connection points:

| Future area | Likely connection |
| --- | --- |
| Input | `GameContext` and `SceneTags` in `Dalashade/GameContext.cs`. |
| Output | `VisualProfile` generation in `Dalashade/VisualProfile.cs`. |
| Diagnostics | Main window diagnostics and compatibility reports. |

## When Editing Tags, Inspect These Files First

1. `Dalashade/GameContext.cs`
2. `Dalashade/VisualProfile.cs`
3. `Dalashade/Windows/MainWindow.cs`
4. `docs/CompatibilityAndDiagnostics.md`

Do not add new context-derived behavior directly in the preset writer. Scene meaning belongs in context/profile code; preset output belongs in writer and mapper code.
