# Zone Tag Coverage Audit

Research date: 2026-06-17

This audit compares Final Fantasy XIV persistent exploration zones against Dalashade's automatic scene classifier. It guided the first exact-territory profile pass so Dalashade can adapt visuals for the places players actually explore, not only for zones whose names happen to contain broad biome keywords.

Sources used:

- FFXIV ConsoleGamesWiki `Zone` page for the main overworld zone list by region: <https://ffxiv.consolegameswiki.com/wiki/Zone>
- FFXIV ConsoleGamesWiki `Field Operations` page for duty-like exploration zones such as Eureka, Bozja, and Occult Crescent: <https://ffxiv.consolegameswiki.com/wiki/Field_Operations>
- FFXIV ConsoleGamesWiki `Cosmic Exploration` page for Cosmic Exploration planets and patch availability: <https://ffxiv.consolegameswiki.com/wiki/Cosmic_Exploration>

## Current Classifier Model

`SceneClassifier` currently infers tags from territory name, weather name, content name, time, duty state, combat state, GPose/cutscene state, and a small hard-coded sanctuary list.

The strongest implemented territory keyword families are:

- `coastal`: La Noscea, Limsa, Ruby Sea, Mist, ocean/beach/sea/coast/isle keywords.
- `forest`: Shroud, Gridania, forest/woods/sylph keywords.
- `jungle`: Rak'tika, Kozama'uka, Yak T'el, jungle/rainforest keywords.
- `desert`: Thanalan, Sagolii, Amh Araeng, Shaaloani, desert/badlands keywords.
- `snow`: Coerthas, Western Highlands, snow/ice/frost/glacier keywords, plus active snow weather override.
- `steppe`: Azim Steppe and generic steppe/grassland keywords.
- `imperial`: Garlemald, Castrum, magitek, factory, steel, ceruleum, Magna Glacies, Tower of Babil.
- `highTech`: Solution Nine, Heritage Found, Alexandria, Living Memory, neon/electrope.
- `lunar`: Mare Lamentorum, moon/lunar/Bestways Burrow.
- `cosmic`: Ultima Thule, Omphalos, star/space/cosmic.
- `aetherial`: Elpis, Lakeland, Crystarium, aether/crystal.
- `fae`: Il Mheg, Voeburt, fae/pixie/dream.
- `ancient`: Amaurot, Allagan, Azys Lla, ruins/ancient.
- `tropical`: island, tropical, Tuliyollal.

The first exact-profile pass now covers the highest-risk gaps from this audit. Remaining coverage work should continue moving from broad string rules toward exact territory profiles plus keyword fallback.

## Coverage Summary

| Coverage | Meaning | Current state |
| --- | --- | --- |
| Strong | Likely receives a useful primary biome and supporting mood/material tags automatically. | ARR coast/forest/desert/snow zones, Ruby Sea, Azim Steppe, Amh Araeng, Il Mheg, Rak'tika, Garlemald, Mare Lamentorum, Ultima Thule, Elpis, Kozama'uka, Yak T'el, Shaaloani, Heritage Found, Living Memory. |
| Partial | Receives a useful area tag, weather tag, or weak fallback but misses important zone identity. | Many hubs, Gyr Abania, Dravania, Sea of Clouds, Thavnair, Labyrinthos, Kholusia, Yanxia, Mor Dhona, The Tempest, Urqopacha. |
| Risky | Likely becomes `neutral`, `field` instead of `city`, or gets a misleading biome from an incidental keyword. | Idyllshire, Rhalgr's Reach, Eulmore, Radz-at-Han, Doman Enclave, most housing/side hubs, several exploration-zone names, Cosmic Exploration planets. |

## Main Persistent Zone Audit

The `Likely automatic result` column describes the current classifier behavior from zone name keywords. It does not include manual scene authoring overrides.

| Region | Zone | Likely automatic result | Coverage | Notes / expected visual direction |
| --- | --- | --- | --- | --- |
| La Noscea | Limsa Lominsa Upper Decks | `city` + `coastal` | Strong | City whitelist plus Limsa/coast keywords. |
| La Noscea | Limsa Lominsa Lower Decks | `city` + `coastal` | Strong | City whitelist plus Limsa/coast keywords. |
| La Noscea | Mist | `field` + `coastal` | Partial | Housing ward likely should be settlement/coastal, not generic field. |
| La Noscea | Wolves' Den Pier | `field` + `neutral` | Risky | Should probably be coastal/naval/pier; current name lacks coast keywords. |
| La Noscea | Middle La Noscea | `field` + `coastal` | Strong | La Noscea keyword. |
| La Noscea | Lower La Noscea | `field` + `coastal` | Strong | La Noscea keyword. |
| La Noscea | Eastern La Noscea | `field` + `coastal` | Strong | La Noscea plus Costa/Bloodshore/Raincatcher keywords when present. |
| La Noscea | Western La Noscea | `field` + `coastal` | Strong | La Noscea keyword. |
| La Noscea | Upper La Noscea | `field` + `coastal` | Strong | La Noscea keyword. |
| La Noscea | Outer La Noscea | `field` + `coastal` | Strong | Current coastal tag is acceptable but could use mining/volcanic hard-surface support later. |
| Black Shroud | New Gridania | `city` + `forest` | Strong | City whitelist plus Gridania forest keyword. |
| Black Shroud | Old Gridania | `city` + `forest` | Strong | City whitelist plus Gridania forest keyword. |
| Black Shroud | The Lavender Beds | `field` + `neutral` | Risky | Housing ward should be settlement/forest, not neutral field. |
| Black Shroud | Central Shroud | `field` + `forest` | Strong | Shroud keyword. |
| Black Shroud | East Shroud | `field` + `forest` | Strong | Shroud keyword. |
| Black Shroud | South Shroud | `field` + `forest` | Strong | Shroud keyword. |
| Black Shroud | North Shroud | `field` + `forest` | Strong | Shroud keyword. |
| Thanalan | Ul'dah - Steps of Nald | `city` + `neutral` | Partial | City whitelist works; biome misses Thanalan/desert because the city name lacks Thanalan. |
| Thanalan | Ul'dah - Steps of Thal | `city` + `neutral` | Partial | Should probably be desert/urban/gold/stone settlement. |
| Thanalan | The Goblet | `field` + `neutral` | Risky | Housing ward should be settlement/desert, not neutral field. |
| Thanalan | The Gold Saucer | `field` + `neutral` | Risky | Should be entertainment/interior/neon/gold, not normal field. |
| Thanalan | Western Thanalan | `field` + `desert` | Strong | Thanalan keyword. |
| Thanalan | Central Thanalan | `field` + `desert` | Strong | Thanalan keyword. |
| Thanalan | Eastern Thanalan | `field` + `desert` | Strong | Thanalan keyword. |
| Thanalan | Southern Thanalan | `field` + `desert` | Strong | Thanalan keyword. |
| Thanalan | Northern Thanalan | `field` + `desert` | Strong | Thanalan keyword. |
| Coerthas | Foundation | `city` + `neutral` | Partial | City whitelist works through Ishgard only if territory name contains Ishgard; Foundation may miss both city and snow depending exact place name. |
| Coerthas | The Pillars | `field` + `neutral` | Risky | Should be Ishgard city/cold/stone. |
| Coerthas | The Firmament | `field` + `neutral` | Risky | Should be Ishgard city/cold/stone/industrial reconstruction. |
| Coerthas | Empyreum | `field` + `neutral` | Risky | Housing ward should be Ishgard settlement/cold. |
| Coerthas | Coerthas Central Highlands | `field` + `snow` | Strong | Coerthas keyword. |
| Coerthas | Coerthas Western Highlands | `field` + `snow` | Strong | Coerthas/Western Highlands keywords. |
| Mor Dhona | Mor Dhona | `field` + `neutral` | Risky | Should be crystal/aetherial/ruins/gloom depending weather; currently under-tagged. |
| Abalathia's Spine | The Sea of Clouds | `field` + `neutral` | Risky | Should be sky/clouds/high-altitude/floating islands; current cloud keyword is weather-only. |
| Abalathia's Spine | Azys Lla | `field` + `ancient` | Strong | Azys Lla/Allagan keywords. |
| Dravania | Idyllshire | `field` + `neutral` | Risky | Major hub should be city/settlement/ruins, but not in sanctuary whitelist. |
| Dravania | The Dravanian Forelands | `field` + `neutral` | Risky | Should be forest/river/dragon highlands. |
| Dravania | The Dravanian Hinterlands | `field` + `neutral` | Risky | Should be ruins/shire/river/stone. |
| Dravania | The Churning Mists | `field` + `neutral` | Risky | Should be clouds/dragon/high-altitude; current mist name does not feed weather tags. |
| Gyr Abania | Rhalgr's Reach | `field` + `neutral` | Risky | Major hub should be city/settlement/monk-temple/arid. |
| Gyr Abania | The Fringes | `field` + `neutral` | Risky | Should be arid forest/highland resistance frontier. |
| Gyr Abania | The Peaks | `field` + `neutral` | Risky | Should be alpine/arid highland. |
| Gyr Abania | The Lochs | `field` + `neutral` | Risky | Should be lake/salt/military/ruins, not neutral. |
| Hingashi | Kugane | `city` + `neutral` | Partial | City whitelist works; missing Far Eastern urban/coastal/night-lit identity. |
| Hingashi | Shirogane | `field` + `neutral` | Risky | Housing ward should be settlement/coastal/Far Eastern. |
| Othard | The Ruby Sea | `field` + `coastal` | Strong | Ruby Sea keyword. |
| Othard | Yanxia | `field` + `neutral` | Risky | Should be Far Eastern river/fields/imperial scars. |
| Othard | The Doman Enclave | `field` + `neutral` | Risky | Reconstruction hub should be city/settlement/Far Eastern. |
| Othard | The Azim Steppe | `field` + `steppe` | Strong | Azim Steppe keyword. |
| Norvrandt | The Crystarium | `city` + `aetherial` | Strong | City whitelist plus Crystarium/crystal keyword. |
| Norvrandt | Lakeland | `field` + `aetherial` | Strong | Lakeland keyword. |
| Norvrandt | Eulmore | `field` + `neutral` | Risky | Major hub should be city/coastal/decadent/bright. |
| Norvrandt | Kholusia | `field` + `neutral` | Risky | Should be coastal/cliff/industrial-poverty split, not neutral. |
| Norvrandt | Amh Araeng | `field` + `desert` | Strong | Amh Araeng keyword. |
| Norvrandt | Il Mheg | `field` + `fae` | Strong | Il Mheg/fae keyword. |
| Norvrandt | The Rak'tika Greatwood | `field` + `jungle` | Strong | Rak'tika/Greatwood keyword. |
| Norvrandt | The Tempest | `field` + `neutral` | Risky | Should be underwater/deep sea/Amaurot/ancient depending subarea; current name misses it. |
| Northern Empty | Old Sharlayan | `city` + `neutral` | Partial | City whitelist works; should add scholarly/coastal/stone/clean. |
| Northern Empty | Labyrinthos | `field` + `neutral` | Risky | Should be artificial/underground/greenhouse/aetherial facility. |
| Ilsabard | Garlemald | `field` + `imperial` | Strong | Garlemald keyword, with cold/alpine mood. |
| Ilsabard | Radz-at-Han | `field` + `neutral` | Risky | Major hub should be city/tropical/coastal/colorful/alchemical. |
| Ilsabard | Thavnair | `field` + `neutral` | Risky | Should be tropical/coastal/colorful/jungle-ish heat. |
| Sea of Stars | Mare Lamentorum | `field` + `lunar` | Strong | Mare/lunar keyword. |
| Sea of Stars | Ultima Thule | `field` + `cosmic` | Strong | Ultima Thule/cosmic keyword. |
| World Unsundered | Elpis | `field` + `aetherial` | Strong | Elpis keyword; could optionally become ancient/aetherial hybrid. |
| Yok Tural | Tuliyollal | `city` + `tropical` | Strong | City whitelist plus Tuliyollal tropical keyword. |
| Yok Tural | Urqopacha | `field` + `neutral` | Risky | Should be mountain/alpine/highland with warm/cold split. |
| Yok Tural | Kozama'uka | `field` + `jungle` | Strong | Kozama'uka keyword. |
| Yok Tural | Yak T'el | `field` + `jungle` | Strong | Yak T'el keyword. |
| Xak Tural | Shaaloani | `field` + `desert` | Strong | Shaaloani keyword. |
| Xak Tural | Heritage Found | `field` + `highTech` | Strong | Heritage Found keyword. |
| Unlost World | Solution Nine | `city` + `highTech` | Strong | City whitelist plus Solution Nine keyword. |
| Unlost World | Living Memory | `field` + `highTech` | Strong | Living Memory keyword, though it may need memory/digital/aetherial nuance. |
| Other | Gangos | `field` + `neutral` | Risky | Should be military/resistance/industrial camp if supported. |
| Other | The Omphalos | `field` + `cosmic` | Strong | Omphalos keyword. |
| Other | Unnamed Island | `field` + `tropical` | Partial | Island keyword works, but Island Sanctuary may need pastoral/coastal/noncombat handling. |

## Exploration And Special Zone Audit

The main `Zone` source excludes field operations because the game classifies them as duties, but users still explore them as large open zones. Dalashade's current duty logic will likely add readability pressure but miss zone identity unless content/territory names hit a keyword.

| Content family | Zone | Likely automatic result | Coverage | Notes / expected visual direction |
| --- | --- | --- | --- | --- |
| Eureka | Eureka Anemos | duty/field-op + `neutral` | Risky | Should be windy island/aetherial/coastal. |
| Eureka | Eureka Pagos | duty/field-op + `neutral` | Risky | Should be snow/ice. Add explicit Pagos rule. |
| Eureka | Eureka Pyros | duty/field-op + `neutral` | Risky | Should be volcanic/fire. Add explicit Pyros rule. |
| Eureka | Eureka Hydatos | duty/field-op + `neutral` | Risky | Should be water/storm/aetherial. Add explicit Hydatos rule. |
| Bozja | The Bozjan Southern Front | duty/field-op + `neutral` | Risky | Should be battlefield/dry/industrial/imperial. |
| Bozja | Zadnor | duty/field-op + `neutral` | Risky | Should be battlefield/highland/industrial. |
| Dawntrail field operation | The Occult Crescent: South Horn | duty + `neutral` | Risky | Should be island/coastal/haunted/aetherial depending actual zone art. |
| Dawntrail field operation | The Occult Crescent: North Horn | upcoming duty + `neutral` | Risky | Add when released and verified. |
| Cosmic Exploration | Sinus Ardorum | `field` + `neutral` | Risky | Should be lunar/moon/cosmic base; the name does not contain current lunar keywords. |
| Cosmic Exploration | Phaenna | `field` + `neutral` | Risky | Should be crystal/cosmic/glass. |
| Cosmic Exploration | Oizys | `field` + `neutral` | Risky | Should be cosmic/ruins/floating rocks. |
| Cosmic Exploration | Auxesia | `field` + `neutral` | Risky | Should be forest/ruins/high-tech remnants. |

## Likely Improper Tags

These are the most important cases where Dalashade may actively present the wrong mental model rather than merely a conservative one.

- Major hubs not in `InferSanctuary`: Idyllshire, Rhalgr's Reach, Eulmore, Radz-at-Han, Doman Enclave, housing wards, and special hubs may be `field` instead of `city`/`settlement`.
- Ul'dah, Kugane, Old Sharlayan, Foundation/Pillars, and similar city areas can be correctly city-like but biome-neutral, losing desert, coastal, scholarly, or cold identity.
- The Sea of Clouds and The Churning Mists are likely `neutral` even though players expect sky/high-altitude/floating-island behavior.
- The Tempest is likely `neutral`, missing underwater, deep-sea, Amaurot, and ancient cues.
- Labyrinthos is likely `neutral`, missing artificial, underground, greenhouse, and research-facility cues.
- Thavnair and Radz-at-Han are likely `neutral`, missing tropical/coastal/colorful/alchemical identity.
- Urqopacha is likely `neutral`, missing the mountain/highland warm-cold split.
- Field operations and Cosmic Exploration zones are currently not first-class visual targets.

## Recommended Next Implementation

The first exact territory profile table is implemented in `SceneClassifier`. The intended end state is still not a giant pile of one-off `if` statements; it should mature into a small built-in territory profile registry with keyword fallback.

1. Keep the current keyword rules as fallback for unknown or future territories.
2. Continue splitting `city` and `settlement` from biome. A hub can be city-like and still have coastal, desert, cold, high-tech, or tropical visual identity.
3. Expand exact profiles as new patches add explorable zones or as screenshots prove better visual identity is needed.
4. Add more regression coverage in `SceneTagRegressionHarness` for every major zone family, then representative individual zones for risk cases.
5. Consider moving the built-in profile table out of `GameContext.cs` once the service split/refactor work starts.
6. Consider optional territory id support later so localized names or punctuation changes cannot break exact matching.
7. Surface whether a zone was exact-profile matched, keyword matched, weather-overridden, or neutral fallback more explicitly in diagnostics if current biome reason text is not enough.

## Proposed Territory Profile Fields

A profile should stay narrow and feed the existing tag vocabulary:

```text
TerritoryName
PrimaryBiome
BiomeConfidence
SecondaryTags
MaterialTags
ArtDirectionTags
AreaContextOverride
Reason
```

Do not encode final shader values per territory. The profile should describe place identity; `SceneIntent`, `MaterialProfile`, `MaterialIntent`, `VisualProfile`, and first-party shader contracts should keep owning the actual visual math.

## Priority Profile Additions

Highest value exact-profile additions:

1. Remaining hubs and side areas discovered from live play that still fall to `field` or `neutral`.
2. Territory id support for exact profiles.
3. Additional special-zone profiles as new Cosmic Exploration, Occult Crescent, or patch exploration areas are added.
4. Nuance upgrades for already-covered zones: Outer La Noscea mining/volcanic, Living Memory digital/memory identity, Elpis ancient+aetherial blend, Garlemald cold+imperial weighting, Island Sanctuary pastoral/coastal/noncombat.
