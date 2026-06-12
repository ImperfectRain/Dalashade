# Scene Tags And Intent

This page documents the implemented scene-tag and scene-intent behavior in Dalashade.

## Implemented

Scene context is collected in `Dalashade/GameContext.cs`.

| Area | Implemented location | Notes |
| --- | --- | --- |
| Raw game context | `GameContextService.Refresh()` | Reads territory, content, duty/combat/cutscene/GPose flags, Eorzea time, and weather. |
| Territory lookup | `GameContextService.Refresh()` | Uses `DataManager.GetExcelSheet<TerritoryType>()`. |
| Weather lookup | `GameContextService.GetCurrentWeather()` | Uses `FFXIVClientStructs` weather data first, then zone-init fallback. |
| Time buckets | `GameContextService.GetTimeBucket()` | Converts Eorzea hour into Dawn, Day, Dusk, or Night. |
| Scene tags | `SceneClassifier.Classify(GameContext context)` | Converts raw context into weather, area, biome, mood, confidence, and gameplay tags. |
| Scene intent | `SceneIntentBuilder.Build(...)` in `Dalashade/SceneIntent.cs` | Converts tags, screenshot analysis, target style, and performance budget into stack-aware intent values before profile stack budgets. |
| Visual profile effects | `ProfileEngine.Create()` in `Dalashade/VisualProfile.cs` | Applies context adjustments, scene-intent stack budgets, screenshot analysis, master style, target style, and performance budget. |

## Tag Taxonomy

Dalashade keeps tags hierarchical instead of treating every hint as a primary classification.

| Category | Implementation | Purpose |
| --- | --- | --- |
| Primary biome | `SceneTags.BiomeKey` | One main environment identity such as `coastal`, `jungle`, `desert`, `snow`, `highTech`, `cosmic`, or `imperial`. |
| Secondary tags | `TagStackDiagnostics.SecondaryTags` | Supporting biome/context identity such as `seaside`, `rainforest`, `badlands`, `moonlit`, `neon`, or `industrial`. |
| Weather tags | `ActiveWeatherTags` | Runtime weather state such as `clear`, `fog`, `gloom`, `rain`, `snow`, `dust`, or `heat`. |
| Time-of-day tags | `ActiveTags` and art-direction tags | `Night` and `DawnDusk` alter readability, warmth, and highlight pressure. |
| Mood tags | `SceneTags.MoodTags` | Raw secondary hints from territory and weather, used to build material and art-direction buckets. |
| Material tags | `TagStackDiagnostics.MaterialTags` | Surface/material risk such as `water`, `specular`, `foliage`, `dry`, `dust`, `ice`, `metallic`, `stone`, or `crystal`. |
| Area/context tags | `TagStackDiagnostics.AreaContextTags` | Structural context such as `field`, `city`, `interior`, `dungeon`, or `raid`. |
| Gameplay-state tags | `TagStackDiagnostics.GameplayStateTags` | Safety modifiers such as `combatReadable`, `dutyReadable`, `gameplayRestrained`, and `cinematicAllowed`. |
| Art-direction tags | `TagStackDiagnostics.ArtDirectionTags` | Visual treatment hints such as `sunlit`, `colorful`, `canopyLight`, `haunted`, `luminous`, `highDepth`, `smoky`, or `crisp`. |

Primary biome is the only single-choice bucket. The other buckets are diagnostic and additive, and should be added only when they drive useful intent, profile, shader, or report behavior.

## Current Weather Tags

`SceneClassifier.Classify` emits separate weather signals instead of one broad fog bucket.

| Tag | Trigger examples |
| --- | --- |
| `rain` | Weather name contains `rain`, `showers`, or `drizzle`. |
| `fog` | Weather name contains `fog` or `mist`. |
| `clouds` | Weather name contains `cloud`. |
| `overcast` | Weather name contains `overcast`. |
| `gloom` | Weather name contains `gloom`, `umbral`, or `darkness`. Gloom is kept as a haunted/dark mood signal rather than full fog. |
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

Biome tags are inferred in `SceneClassifier.InferBiome` from territory, weather, and content names. Each biome result also records a confidence value, a reason string, and one or more mood tags when useful. Mood tags are diagnostic/context hints such as `neon`, `industrial`, `dreamlike`, `rainforest`, `cold`, `coastal`, or `cosmic`; they do not replace the single primary `BiomeKey`.

| Tag | Current trigger examples |
| --- | --- |
| `snow` | Snow, ice, frost, glacier, Coerthas, Snowcloak, Western Highlands. Snow weather overrides terrain biome to keep active snow scenes protected. Adds cold/ice/clean/crisp context. Garlemald is primarily imperial but adds cold/alpine/snow mood context. |
| `alpine` | Mountain, alpine, peak, summit. |
| `forest` | Forest, Shroud, woods, Gridania, sylph. |
| `jungle` | Jungle, rainforest, Rak'tika, Yak T'el, Kozama'uka. Adds rainforest/foliage/lush/verdant/humid/canopyLight tags. Jungle nights damp excessive haze and shadow lift so foliage keeps dark depth instead of turning gray. |
| `swamp` | Swamp, marsh, bog, fen. |
| `steppe` | Azim Steppe, steppe, grassland. |
| `desert` | Desert, Thanalan, Sagolii, Amh Araeng, Shaaloani, badlands. Adds desert/badlands/dry/heat/dust/sunScorched tags. Heat remains strong at night, but nighttime heat has less highlight-protection pressure than daytime glare. |
| `wasteland` | Wasteland, wastes. |
| `cave` | Cave, cavern, mine, tunnel, subterrane. |
| `void` | Void, darkness, abyss, Ascian. |
| `aetherial` | Aether, crystal, Lakeland, Crystarium, Elpis. |
| `fae` | Il Mheg, fae, pixie, Voeburt, dream. Adds fae/dreamlike/colorful/magical/pastel tags. |
| `lightFlooded` | The Empty, Lightwarden, sin eater, light-flooded. |
| `lunar` | Mare Lamentorum, Bestways Burrow, moon, lunar. Adds lunar/moonlit/cold/cosmic/cool/highDepth tags. |
| `cosmic` | Ultima Thule, Omphalos, cosmic, star, space. Adds cosmic/alien/aetherial/stars/cool/highDepth tags. |
| `ancient` | Ancient, Amaurot, Allagan, Azys Lla, ruin. Adds ancient/ruins/structured/stone/aetherial context. |
| `imperial` | Garlemald, Castrum, imperial, magitek, factory, steel, ceruleum, Magna Glacies, Tower of Babil. Adds imperial/industrial/metallic/smoky/structured/cold tags; Garlemald also adds alpine/snow mood context. |
| `highTech` | Solution Nine, Heritage Found, Alexandria, Living Memory, neon, electrope. Adds neon/highTech/electrope/urban/clean/luminous tags. |
| `underwater` | Underwater, ocean floor. |
| `volcanic` | Volcano, lava, ember. |
| `coastal` | Ruby Sea, La Noscea, Costa del Sol, Bloodshore, Raincatcher, ocean, beach, sea, Limsa, Mist, coast, isle. Adds coastal/tropical/seaside/beach/water/specular/clean/sunlit/colorful tags and mild foliage pressure for coastal field zones. |
| `tropical` | Island, tropical, Tuliyollal. Adds tropical/coastal/warm/sunlit/colorful/foliage context and mild foliage pressure. |
| `fire` | Fire, flame, inferno. |
| `overcast` | Fallback when fog/cloud/overcast mood is present but no stronger biome matched. Gloom alone does not create this haze fallback. |
| `neutral` | Fallback when no specific biome matched. |

Biome and territory tags affect the visual profile in `ProfileEngine.ApplyTerritory` and `ProfileEngine.ApplyBiome`.

## Tag Audit And Merge Guidance

| Tag family | Trigger | Visual implication | Overlap handling |
| --- | --- | --- | --- |
| `coastal` / `tropical` / `seaside` / `beach` | La Noscea, Costa del Sol, Ruby Sea, Limsa/Mist, coast/sea/beach keywords | Bright, colorful, clean, sunlit, specular, mild foliage, stronger highlight restraint before bloom | Keep `coastal`/`tropical` as primary biomes; keep `seaside`, `beach`, `water`, and `specular` as secondary/material tags. |
| `jungle` / `rainforest` / `lush` / `verdant` / `canopyLight` | Rak'tika, Yak T'el, Kozama'uka, jungle/rainforest keywords | High foliage density, richer greens, humid depth, subtle canopy light, preserved dark trunks/background | Keep `jungle` primary; `rainforest`, `lush`, `verdant`, and `canopyLight` remain secondary/art-direction tags. |
| `desert` / `badlands` / `dry` / `heat` / `dust` | Thanalan, Amh Araeng, Shaaloani, Sagolii, desert/badlands, heat/dust weather | Warm dry contrast, depth-weighted haze, daytime glare protection, lighter night highlight protection | Keep `desert` primary; `heat` can come from biome or weather; `dust` is material/weather pressure. |
| `snow` / `alpine` / `cold` / `ice` | Snow weather, Coerthas, Snowcloak, ice/frost keywords | Cool, clean, crisp, strong highlight protection without gray snow | Snow weather overrides terrain biome; snow-biome contribution is dampened when weather already supplied snow. |
| `neon` / `highTech` / `urban` | Solution Nine, Alexandria, Heritage Found, neon/electrope keywords | Clean contrast, saturated accents, controlled glow, no muddy warmth | Keep `highTech` primary; `neon`/`urban` are secondary/art tags. |
| `cosmic` / `lunar` / `alien` / `moonlit` | Ultima Thule, Mare Lamentorum, moon/star/space keywords | Cool atmosphere, subtle glow, high depth, clean contrast | Keep `cosmic` and `lunar` as separate primary biomes because lunar also carries cold/moonlit identity. |
| `imperial` / `industrial` / `metallic` / `smoky` | Garlemald, Castrum, magitek, factory, steel, ceruleum | Metallic, structured, harder clarity, lower saturation, restrained bloom | Keep `imperial` primary; `industrial`, `metallic`, and `smoky` are material/art tags. |
| `gloom` / `haunted` / `dark` | Gloom, Umbral, darkness, void keywords | Moody and dark with depth preservation | Do not merge with fog; gloom does not create fog/mist haze by itself. |
| `fog` / `mist` / `haze` | Fog or mist weather, overcast fallback | True atmospheric veil and haze pressure | Keep separate from gloom; only fog/mist should create strong haze. |
| `city` / `settlement` / `field` / `dungeon` / `raid` | Sanctuary, duty, content, and world category | Cities stay readable and grounded; dungeons/raids prioritize clarity | These are area/context tags, not biomes. |
| `combatReadable` / `gameplayRestrained` / `cinematicAllowed` | Combat, duty, cutscene, GPose | Combat/duty dampen bloom/haze/cinematic pressure; GPose/cutscene allow more style | Gameplay-state tags always modify biome/weather instead of replacing them. |

## Art Direction Principles

- Protect highlights before increasing bloom, especially in snow, coast, desert, neon, and aetherial scenes.
- Avoid gray haze unless weather specifically calls for fog, mist, overcast, dust, or heat shimmer.
- Use saturation selectively by biome: coast and jungle can be richer; imperial and some ruins can be more restrained.
- Preserve shadow depth in forests, ruins, haunted scenes, and jungle nights instead of lifting the whole frame.
- Use bloom for light sources, sky openings, water/specular sparkle, neon, and aetherial accents, not as a full-screen wash.
- Use warmth for sunlit coast and desert, coolness for snow, night, lunar, and cosmic scenes.
- Prioritize readability in combat, dungeons, and raids; damp cinematic bloom/haze before reducing all identity.
- Avoid excessive sharpening in foliage, rain, haze, wet highlights, and distant detail.
- Let high-confidence mappings push a stronger look; low-confidence or fallback mappings stay conservative.

## Gameplay, Combat, Cutscene, And GPose

`SceneTags` includes:

| Field | Implemented meaning |
| --- | --- |
| `NeedsCombatClarity` | True when the player is in combat. |
| `NeedsDutyReadability` | True when the player is in duty content and not in combat. |
| `NeedsGameplayClarity` | Compatibility property that returns true when combat clarity or duty readability is active. |
| `CinematicAllowed` | True when the player is not in combat and the scene is not constrained by duty readability, or when cutscene/GPose explicitly allows presentation-oriented treatment. |

Combat clarity is stronger. Duty readability is milder and preserves more atmosphere outside combat.

Conflict handling currently works as follows:

| Conflict | Current behavior |
| --- | --- |
| Snow weather plus snow biome | Snow weather supplies the main cold/highlight intent; snow biome contribution is dampened so cold scenes do not double-stack. |
| Gloom versus fog | Fog/mist adds strong haze. Gloom adds dark atmosphere and moderate shadow protection with much less global haze. |
| Jungle/rainforest night | Jungle nights keep strong foliage identity while reducing gray veil and excessive shadow lift. |
| Night heat | Heat stays high, while highlight protection and haze are slightly lower than daytime heat so desert nights stay hot without full-screen lift. |
| Combat plus cinematic/mood tags | Combat adds strong readability and combat pressure, then subtracts cinematic permission, atmosphere, haze, and stylized glow pressure. |
| Duty outside combat | Duty adds mild readability and lightly constrains atmosphere/cinematic permission without flattening the scene. |
| GPose/cutscene | GPose and cutscenes add cinematic permission and atmosphere when not in combat. |
| Low performance budget | Low budget adds mild readability/safety pressure and conservatively reduces expensive-looking intent such as haze and extra glow; noncombat Low profile output is reduced only partially. |

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

Biome intent contributions are confidence-aware. High-confidence territory mappings such as Eastern La Noscea, Rak'tika, Amh Araeng, Solution Nine, Ultima Thule, or Garlemald can push stronger art direction; low-confidence fallback mappings keep smaller changes.

Intent contribution diagnostics are stored as `SceneIntentContribution` records so the UI/report can show which tags or systems contributed to each value.

Diagnostics are exposed through:

1. `TagStackDiagnostics` in `Dalashade/SceneIntent.cs`
2. The main window `Scene Tags` section
3. Compatibility report export

Diagnostics currently include:

| Diagnostic | Meaning |
| --- | --- |
| Territory id/name | Raw territory identity from Dalamud/Lumina. |
| Weather id/name | Current weather from `FFXIVClientStructs` weather lookup or zone-init fallback. |
| Active weather tags | Weather booleans that matched, such as rain, snow, storm, dust, or heat. |
| Biome key | Primary inferred biome. |
| Biome confidence | Relative confidence of the biome keyword match, or lower confidence for fallback tags. |
| Biome reason | The classifier reason and matched keyword when available. |
| Secondary tags | Supporting biome tags such as seaside, rainforest, badlands, moonlit, neon, or industrial. |
| Mood tags | Secondary mood/context hints such as neon, industrial, dreamlike, rainforest, cold, coastal, or cosmic. |
| Material tags | Surface/material hints such as water, specular, foliage, dry, dust, ice, metallic, stone, or crystal. |
| Area/context tags | Field, city, interior, dungeon, and raid context. |
| Gameplay-state tags | Combat readability, duty readability, cinematic permission, and gameplay restraint. |
| Art-direction tags | Treatment hints such as sunlit, colorful, canopyLight, haunted, luminous, highDepth, smoky, or crisp. |
| SceneIntent contributions | Per-intent source, amount, reason, and report grouping by tag category. |

## Expected Regression Examples

| Case | Expected primary biome | Expected supporting tags | Expected visual direction |
| --- | --- | --- | --- |
| Eastern La Noscea / Costa del Sol, clear day | `coastal` | coastal, tropical, seaside, beach, water, specular, clean, sunlit, colorful | Bright, colorful, clean, specular, mild foliage, no haze unless weather adds it. |
| The Rak'tika Greatwood, Umbral Wind night | `jungle` | rainforest, lush, verdant, humid, canopyLight, gloom, haunted | Lush and deep with canopy-light accent; gloom is dark mood, not gray fog. |
| Amh Araeng, Heat Waves night | `desert` | desert, badlands, dry, heat, dust | Hot and dry with distance-weighted haze and lower night highlight restraint. |
| Snowcloak or Coerthas snow | `snow` | snow, alpine, cold, ice, clean, crisp | Cold and crisp with protected highlights. |
| Solution Nine / Alexandria / Heritage Found | `highTech` | neon, highTech, urban, clean, luminous | Clean high contrast, saturated accents, controlled neon glow. |
| Ultima Thule / Mare Lamentorum | `cosmic` or `lunar` | alien, aetherial, moonlit, cold, highDepth | Cool, otherworldly, atmospheric, high depth. |
| Garlemald / Castrum / factory | `imperial` | imperial, industrial, metallic, smoky, structured | Metallic, hard, readable, desaturated, restrained bloom. |

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
