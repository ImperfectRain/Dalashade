# Scene Tags And Intent

This page documents the implemented scene-tag and scene-intent behavior in Dalashade.

## Implemented

Scene context is collected in `Dalashade/GameContext.cs`.

| Area | Implemented location | Notes |
| --- | --- | --- |
| Raw game context | `GameContextService.Refresh()` | Reads territory, content, duty/combat/cutscene/GPose flags, Eorzea time, and weather. |
| Territory lookup | `GameContextService.Refresh()` | Uses `DataManager.GetExcelSheet<TerritoryType>()`. |
| Weather lookup | `GameContextService.RefreshWeather()` | Uses `FFXIVClientStructs` weather data first, then zone-init fallback. |
| Time buckets | `GameContextService.GetTimeBucket()` | Converts Eorzea hour into Dawn, Day, Dusk, or Night. |
| Scene tags | `SceneClassifier.Classify(GameContext context)` | Converts raw context into weather, area, biome, and gameplay tags. |
| Scene intent | `SceneIntentBuilder.Build(...)` in `Dalashade/SceneIntent.cs` | Converts tags, screenshot analysis, target style, and performance budget into stack-aware intent values before profile stack budgets. |
| Visual profile effects | `ProfileEngine.Create()` in `Dalashade/VisualProfile.cs` | Applies context adjustments, scene-intent stack budgets, screenshot analysis, master style, target style, and performance budget. |

## Current Weather Tags

`SceneClassifier.Classify` emits separate weather signals instead of one broad fog bucket.

| Tag | Trigger examples |
| --- | --- |
| `rain` | Weather name contains `rain`, `showers`, or `drizzle`. |
| `fog` | Weather name contains `fog` or `mist`. |
| `clouds` | Weather name contains `cloud`. |
| `overcast` | Weather name contains `overcast`. |
| `gloom` | Weather name contains `gloom`, `umbral`, or `darkness`. |
| `snow` | Weather name contains `snow` or `blizzard`. |
| `storm` | Weather name contains `storm`, `thunder`, or `gales`. |
| `dust` | Weather name contains `dust`, `sandstorm`, `sand storm`, or `dust storm`. |
| `heat` | Weather name contains `heat`, `heat wave`, or `heatwave`. |
| `clear` | No rain, fog, clouds, overcast, gloom, snow, storm, dust, or heat tag matched. |

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

Biome tags are inferred in `SceneClassifier.InferBiome` from territory, weather, and content names.

| Tag | Current trigger examples |
| --- | --- |
| `snow` | Snow, ice, frost, glacier, Coerthas, Snowcloak. |
| `alpine` | Mountain, alpine, peak, summit. |
| `forest` | Forest, Shroud, woods, Gridania, sylph. |
| `jungle` | Jungle, rainforest, Rak'tika, Yak T'el, Kozama'uka. |
| `swamp` | Swamp, marsh, bog, fen. |
| `steppe` | Azim Steppe, steppe, grassland. |
| `desert` | Desert, Thanalan, Sagolii, Amh Araeng, Shaaloani. |
| `wasteland` | Badlands, wasteland, wastes. |
| `cave` | Cave, cavern, mine, tunnel, subterrane. |
| `void` | Void, darkness, abyss, Ascian. |
| `aetherial` | Aether, crystal, Lakeland, Crystarium, Elpis. |
| `fae` | Il Mheg, fae, pixie, Voeburt, dream. |
| `lightFlooded` | The Empty, Lightwarden, sin eater, light-flooded. |
| `lunar` | Mare Lamentorum, moon, lunar. |
| `cosmic` | Ultima Thule, cosmic, star, space. |
| `ancient` | Ancient, Amaurot, Allagan, Azys Lla, ruin. |
| `imperial` | Garlemald, Castrum, imperial, magitek, factory, steel, ceruleum. |
| `highTech` | Solution Nine, Heritage Found, Alexandria, neon, electrope. |
| `underwater` | Underwater, ocean floor. |
| `volcanic` | Volcano, lava, ember. |
| `coastal` | Ruby Sea, ocean, beach, sea, Limsa, Mist, coast. |
| `tropical` | Island, tropical, Tuliyollal. |
| `fire` | Fire, flame, inferno. |
| `overcast` | Fallback when fog/cloud/gloom mood is present but no stronger biome matched. |
| `neutral` | Fallback when no specific biome matched. |

Biome and territory tags affect the visual profile in `ProfileEngine.ApplyTerritory` and `ProfileEngine.ApplyBiome`.

## Gameplay, Combat, Cutscene, And GPose

`SceneTags` includes:

| Field | Implemented meaning |
| --- | --- |
| `NeedsCombatClarity` | True when the player is in combat. |
| `NeedsDutyReadability` | True when the player is in duty content and not in combat. |
| `NeedsGameplayClarity` | Compatibility property that returns true when combat clarity or duty readability is active. |
| `CinematicAllowed` | True when the player is not in combat and is either not in a cutscene or is in GPose. |

Combat clarity is stronger. Duty readability is milder and preserves more atmosphere outside combat.

## Scene Intent And Stack Budgets

`SceneIntent` is implemented in `Dalashade/SceneIntent.cs`. It is the stable internal scene-language contract intended for future Dalashade shaders and future ReShade bridge work.

`SceneIntentBuilder.Build(...)` feeds it from:

1. `GameContext`
2. refined weather tags
3. refined biome/mood tags
4. combat, duty, cutscene, and GPose state
5. screenshot analysis when enabled and available
6. target style
7. performance budget

All intent values are normalized from `0` to `1`.

| Intent value | Purpose |
| --- | --- |
| `Readability` | How much the scene needs readability help. |
| `Atmosphere` | How much atmosphere should be preserved. |
| `HighlightProtection` | Risk of blown highlights or white detail loss. |
| `ShadowProtection` | Risk of crushed shadows. |
| `Haze` | Fog, dust, clouds, or other haze pressure. |
| `Wetness` | Rain/storm wet-scene pressure. |
| `Cold` | Snow, ice, lunar, or cold-scene pressure. |
| `Heat` | Desert, dust, heatwave, fire, or volcanic pressure. |
| `MagicGlow` | Aetherial, fae, cosmic, lunar, or light-flooded pressure. |
| `NeonGlow` | High-tech/neon pressure. |
| `FoliageDensity` | Forest, jungle, or swamp density. |
| `IndustrialHardness` | Imperial, factory, ruin, or hard-surface structural pressure. |
| `CosmicMood` | Lunar and cosmic scene mood. |
| `CombatPressure` | How much combat should dominate visual safety. |
| `CinematicPermission` | How much cinematic treatment is allowed. |

Current stack budgets protect bloom, AO, shadow lift, bloom dirt, and saturation from repeated context reductions/additions. Snow weather also dampens snow-biome handling so snow does not apply full-strength twice.

Intent contribution diagnostics are stored as `SceneIntentContribution` records so the UI/report can show which tags or systems contributed to each value.

Diagnostics are exposed through:

1. `TagStackDiagnostics` in `Dalashade/SceneIntent.cs`
2. The main window `Scene Tags` section
3. Compatibility report export

## Planned / Future

This system is planned and not currently implemented. Do not treat this document as an implementation reference yet.

Future work may move more direct weather/biome mutations into `SceneIntent` so tags contribute to a smaller set of normalized visual goals before variables are changed.

## When Editing Tags, Inspect These Files First

1. `Dalashade/GameContext.cs`
2. `Dalashade/SceneIntent.cs`
3. `Dalashade/VisualProfile.cs`
4. `Dalashade/Windows/MainWindow.cs`
5. `docs/CompatibilityAndDiagnostics.md`

Do not add new context-derived behavior directly in the preset writer. Scene meaning belongs in context/profile code; preset output belongs in writer and mapper code.
