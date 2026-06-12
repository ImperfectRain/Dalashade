# Shader Authoring

This page documents the current scaffolding and Dalashade custom ReShade shader prototypes.

## Current Status

Implemented plugin support:

- Top-level `shaders/` folder exists for custom `.fx` files.
- `Configuration.EnableDalashadeCustomShaders` gates all custom shader variable writes.
- `CustomShaderVariableMapper` writes stable `SceneIntent` values into Dalashade custom shader sections when matching sections and keys exist in generated preset content.
- `Configuration.AutoInjectDalashadeCustomShaderSections` can optionally add known Dalashade custom shader sections to the generated preset only.
- Main window and compatibility reports show whether custom shader support is enabled, which custom sections were detected, and which custom variables were written.

Implemented shader prototypes:

- `shaders/Dalashade_WeatherAtmosphere.fx`
- Scene-aware weather and air pass: depth-weighted fog/dust, wet air glow, coastal highlight control, canopy air, snow/cold separation, and gameplay dampening.
- `shaders/Dalashade_AdaptiveGrade.fx`
- Scene-aware native grade: exposure trim, contrast, selective saturation, biome temperature/tint, highlight shoulder, black-depth preservation, and industrial/cosmic bias.
- `shaders/Dalashade_SmartSharpen.fx`
- Context-aware clarity pass that avoids sharpening haze, sky gradients, wet highlights, foliage shimmer, far-depth detail, and combat clutter.
- `shaders/Dalashade_AtmosphereBloom.fx`
- Selective atmospheric bloom for bright sources, wet speculars, canopy openings, distant heat glow, magic/aether, and neon accents.

Not implemented yet:

- No IPC, named pipe, JSON bridge, or live ReShade bridge exists yet.
- No native ReShade add-on exists yet.
- No automatic shader installation/copying exists yet.
- Release packaging of custom files in `shaders/` is not guaranteed by this document. Check the current release task and zip contents before assuming a shader is included in a published plugin package.

Bridge/add-on integration is planned and not currently implemented. Do not treat this document as a native bridge or IPC implementation reference.

## Safety Rules

Custom shader writes are intentionally conservative:

1. `EnableDalashadeCustomShaders` must be enabled.
2. The preset must already contain a Dalashade custom shader section, or generated-preset-only injection must be explicitly enabled.
3. The section name must start with `Dalashade` or include `/Dalashade` or `\Dalashade`.
4. The variable key must exactly match a known `Dalashade_*` SceneIntent variable.
5. No custom shader is required for normal operation.

When `AutoInjectDalashadeCustomShaderSections` is off, Dalashade does not insert shader sections during generation.

When both `EnableDalashadeCustomShaders` and `AutoInjectDalashadeCustomShaderSections` are on, Dalashade may inject known custom shader sections and variables into the generated preset only. It never mutates the base preset. Current injection support is limited to `[Dalashade_WeatherAtmosphere.fx]`, `[Dalashade_AdaptiveGrade.fx]`, `[Dalashade_SmartSharpen.fx]`, and `[Dalashade_AtmosphereBloom.fx]`.

The generated WeatherAtmosphere section includes the weather shader intent variables Dalashade currently knows how to write: `Dalashade_Haze`, `Dalashade_Wetness`, `Dalashade_Cold`, `Dalashade_Heat`, `Dalashade_HighlightProtection`, `Dalashade_ShadowProtection`, `Dalashade_CombatPressure`, `Dalashade_Atmosphere`, `Dalashade_MagicGlow`, `Dalashade_NeonGlow`, `Dalashade_FoliageDensity`, `Dalashade_Readability`, and `Dalashade_CinematicPermission`.

The generated AdaptiveGrade section includes the grade shader intent variables Dalashade currently knows how to write: `Dalashade_Readability`, `Dalashade_Atmosphere`, `Dalashade_HighlightProtection`, `Dalashade_ShadowProtection`, `Dalashade_Cold`, `Dalashade_Heat`, `Dalashade_MagicGlow`, `Dalashade_NeonGlow`, `Dalashade_FoliageDensity`, `Dalashade_IndustrialHardness`, `Dalashade_CosmicMood`, `Dalashade_CinematicPermission`, and `Dalashade_CombatPressure`.

The generated SmartSharpen section includes the clarity shader intent variables Dalashade currently knows how to write: `Dalashade_Readability`, `Dalashade_Haze`, `Dalashade_Wetness`, `Dalashade_FoliageDensity`, `Dalashade_CombatPressure`, `Dalashade_HighlightProtection`, and `Dalashade_SharpenAuthority`. It can also write SmartSharpen tuning sliders such as `SharpenStrength`, `StructuralClarityStrength`, `TextureDetailStrength`, and dampening controls when those keys exist or are injected.

The generated AtmosphereBloom section includes the bloom shader intent variables Dalashade currently knows how to write: `Dalashade_Atmosphere`, `Dalashade_MagicGlow`, `Dalashade_NeonGlow`, `Dalashade_FoliageDensity`, `Dalashade_Wetness`, `Dalashade_Heat`, `Dalashade_Readability`, `Dalashade_HighlightProtection`, `Dalashade_CombatPressure`, and `Dalashade_CinematicPermission`.

Dalashade also does not currently install or copy custom shader files into a ReShade shader directory, and generated-preset injection does not enable techniques automatically. For manual testing, place the needed files from `shaders/` somewhere ReShade scans for shaders, then enable wanted techniques in ReShade.

## Manual Installation Diagnostics

The UI and compatibility report distinguish two separate states:

| State | Meaning |
| --- | --- |
| Custom shader support enabled | `EnableDalashadeCustomShaders` is on, so generation is allowed to write supported `Dalashade_*` values. |
| Auto-inject generated preset sections enabled | `AutoInjectDalashadeCustomShaderSections` is on, so generation may add known sections to the generated preset only. |
| Section injected | Dalashade added a supported custom shader section to the generated preset. |
| Variables injected | Dalashade added missing known `Dalashade_*` variables to a supported custom shader section in the generated preset. |
| Technique injected | Currently expected to be `no`. Auto-injection adds sections and variables only; users must enable wanted techniques in ReShade. |
| Generated preset only | Injection happened in the generated output path; the base preset was not modified. |
| Base preset contains a Dalashade custom shader section | The selected base preset has a section such as `[Dalashade_WeatherAtmosphere.fx]`, `[Dalashade_AdaptiveGrade.fx]`, `[Dalashade_SmartSharpen.fx]`, or `[Dalashade_AtmosphereBloom.fx]`, so Dalashade can inspect it for supported `Dalashade_*` keys. |
| Base preset technique active/inactive/unknown | Dalashade checks whether base preset sections appear active in `Techniques=`. Generated-preset-only injection does not imply the technique is active. |
| Known custom variables found | Matching `Dalashade_*` keys were found in a Dalashade custom shader section. |
| Variables detected but unchanged | Keys exist, but generation did not write values. Common causes are disabled custom shader support, inactive/unknown technique state, write-mode settings, or values already matching SceneIntent. |
| Variables written | `SceneIntent` values were written into matching `Dalashade_*` keys during generation. |
| SmartSharpen authority | Whether `Dalashade_SmartSharpen.fx` should behave as primary, secondary, or passive based on other active sharpeners in the preset. |

This is not the same as shader file installation. ReShade must still be able to find the actual `.fx` file through its own shader search paths. If the section exists but ReShade cannot compile or enable the effect, check ReShade's shader path and install the relevant file from `shaders/` manually.

Technique auto-injection is disabled. Dalashade can inject known sections and variables into the generated preset, but it does not append Dalashade custom shader entries to `Techniques=`. Base preset technique inactive/unknown means only that the base preset did not confirm activation. Users must install the relevant `.fx` files and enable wanted techniques in ReShade manually.

Compatibility analysis classifies the four `Dalashade_*` first-party shader sections as controlled Dalashade effects when they appear in a preset. This means reports should treat them as known first-party color, bloom, clarity, or atmosphere roles rather than unsupported third-party effects. Variable writes still require matching generated-preset section/key lines and custom shader support enabled.

## Recommended ReShade Order

For `Dalashade_WeatherAtmosphere.fx`, use this order:

1. After AO, MXAO, RTGI, and other depth/lighting effects.
2. Before final sharpening.
3. Before UI restore, KeepUI, or RestoreUI effects if applicable.

The shader is meant to shape world atmosphere. Keeping it before UI restore helps avoid haze/glow affecting protected UI layers in presets that use UI masking.

For `Dalashade_AdaptiveGrade.fx`, use this order:

1. After atmosphere, lighting, bloom, and major tonemapping effects.
2. Before final sharpening.
3. Before UI restore, KeepUI, or RestoreUI effects if applicable.
4. Avoid stacking it after a strong third-party color grade unless you deliberately want both grades active.

The shader is meant to provide a mild Dalashade-native grade. It should usually sit late enough to see the shaped scene, but early enough that sharpening and UI restoration remain clean.

For `Dalashade_AtmosphereBloom.fx`, use this order:

1. After primary lighting and tonemapping effects.
2. Before `Dalashade_AdaptiveGrade.fx` when you want the grade to shape the bloom color.
3. Before `Dalashade_SmartSharpen.fx`.
4. Before UI restore, KeepUI, or RestoreUI effects if applicable.

The shader is a restrained atmospheric glow pass. It should not replace a full creative bloom stack, and it should stay low when the preset already has strong bloom enabled.

For `Dalashade_SmartSharpen.fx`, use this order:

1. After grading, bloom, and deband effects.
2. Before UI restore, KeepUI, or RestoreUI effects if applicable.
3. Avoid stacking after aggressive third-party sharpeners unless their strength is lowered.

The shader is a clarity pass, not anti-aliasing. It can make readable edges a little clearer, but it cannot fix true geometric aliasing, temporal shimmer, or missing AA.

## Weather Atmosphere Controls

`Dalashade_WeatherAtmosphere.fx` can be driven by Dalashade or tested manually in ReShade.

Dalashade-driven controls:

| Control | Purpose |
| --- | --- |
| `Dalashade_Haze` | Drives bounded depth haze for fog, dust, clouds, and similar atmosphere. |
| `Dalashade_Wetness` | Adds wet-scene specular glow and contributes to storm mood. |
| `Dalashade_Cold` | Cools haze tint and strengthens snow/white highlight protection. |
| `Dalashade_Heat` | Warms haze tint and adds distant heat/dust softness. |
| `Dalashade_HighlightProtection` | Rolls off bright highlights, especially in snow, wet, and hot scenes. |
| `Dalashade_ShadowProtection` | Adds modest shadow lift for dark scenes. |
| `Dalashade_CombatPressure` | Dampens heavy haze, glow, storm mood, and heat softness for gameplay readability. |
| `Dalashade_Readability` | Adds lighter atmosphere dampening when the scene needs readable gameplay space. |
| `Dalashade_Atmosphere` | Scales the scene's general atmosphere allowance. |
| `Dalashade_MagicGlow` | Adds controlled glow for magical/aetherial scenes. |
| `Dalashade_NeonGlow` | Adds controlled glow for neon/high-tech scenes. |
| `Dalashade_FoliageDensity` | Reduces gray veil and shadow lift in foliage-heavy scenes, with subtle canopy-light response. |
| `Dalashade_CinematicPermission` | Allows a small boost to atmosphere outside gameplay-critical moments. |

Manual testing controls:

| Control | Suggested tuning |
| --- | --- |
| `Manual Overall Strength` | Default `0.35`. Raise only while tuning; high values make all responses more visible. |
| `Manual Haze Boost` | Adds haze without needing Dalashade scene tags. Useful for testing depth behavior. |
| `Manual Glow Boost` | Adds glow without needing wet, magic, or neon intent. |
| `Manual Storm/Dark Mood` | Tests storm darkening and cool mood response. |
| `Show Debug Mask` | Shows red depth haze, green glow, and blue protection/readability pressure. |
| `Debug View` | Selects composite, depth haze, highlight protection, weather glow, foliage dampening, or heat/dust masks. |

The shader intentionally does not blur the whole frame or disable any ReShade techniques. It shapes color, haze, glow, highlight rolloff, and mild softness through bounded masks. Heat haze and depth haze are weighted toward distance so hot desert nights avoid full-screen lift. Foliage-heavy atmospheric scenes reduce veil haze and get a small canopy-light response instead of gray wash. Combat-heavy scenes should visibly reduce the heaviest atmosphere while retaining light weather identity.

## Adaptive Grade Controls

`Dalashade_AdaptiveGrade.fx` can be driven by Dalashade or tested manually in ReShade.

Dalashade-driven controls:

| Control | Purpose |
| --- | --- |
| `Dalashade_Readability` | Dampens contrast, saturation, and cinematic pressure for readable gameplay. |
| `Dalashade_Atmosphere` | Gives the grade a little more room in atmospheric scenes. |
| `Dalashade_HighlightProtection` | Adds mild exposure trim and highlight rolloff to protect bright weather and snow. |
| `Dalashade_ShadowProtection` | Adds bounded shadow lift without raising the whole frame aggressively. |
| `Dalashade_Cold` | Biases the grade cooler and contributes to highlight protection. |
| `Dalashade_Heat` | Biases the grade warmer with a small heat/dust color response. |
| `Dalashade_MagicGlow` | Adds a subtle magenta/green tint response and modest saturation support. |
| `Dalashade_NeonGlow` | Adds a subtle cool neon tint response and modest saturation support. |
| `Dalashade_FoliageDensity` | Adds restrained green richness and dampens global shadow lift in foliage-heavy scenes. |
| `Dalashade_IndustrialHardness` | Adds harder contrast, cooler tone, restrained saturation, and black-depth preservation for imperial/industrial spaces. |
| `Dalashade_CosmicMood` | Adds cool blue/violet bias and high-depth contrast for lunar/cosmic scenes. |
| `Dalashade_CinematicPermission` | Allows a small cinematic contrast, saturation, and color-bias lift. |
| `Dalashade_CombatPressure` | Dampens heavy grading and slightly reduces exposure, saturation, and shadow lift. |

Manual testing controls:

| Control | Suggested tuning |
| --- | --- |
| `Manual Overall Strength` | Default `0.35`. Keep this below `0.60` for normal gameplay testing. |
| `Manual Exposure Trim` | Small additive exposure trim. Use negative values to verify highlight rolloff behavior. |
| `Manual Contrast` | Adds or removes contrast before safety clamps. |
| `Manual Saturation` | Adds or removes saturation before gameplay dampening. |
| `Manual Temperature` | Warms positive values and cools negative values. |
| `Manual Tint` | Moves the grade toward green or magenta. |
| `Show Debug Mask` | Shows red highlight rolloff, green shadow lift, and blue cinematic/gameplay pressure. |

The shader intentionally clamps per-channel movement relative to the source and clamps final output away from pure black and pure white. It uses selective color response: coastal/desert heat warms bright mids, jungle foliage gets richer greens without gray lift, snow/cosmic scenes cool highlights and shadows, and imperial/industrial scenes become harder and less pastel. It is a native safety grade, not a full creative LUT replacement.

## Atmosphere Bloom Controls

`Dalashade_AtmosphereBloom.fx` can be driven by Dalashade or tested manually in ReShade.

Dalashade-driven controls:

| Control | Purpose |
| --- | --- |
| `Dalashade_Atmosphere` | Gives bloom a little more room in atmospheric scenes. |
| `Dalashade_MagicGlow` | Strengthens and tints aetherial or magical glow. |
| `Dalashade_NeonGlow` | Strengthens and tints neon or high-tech glow. |
| `Dalashade_FoliageDensity` | Allows subtle green-gold canopy/sky-light bloom through foliage when atmosphere is high. |
| `Dalashade_Wetness` | Adds small wet-specular glow on bright rain/water highlights. |
| `Dalashade_Heat` | Adds restrained distant warm atmospheric glow for heat/dust scenes. |
| `Dalashade_Readability` | Raises restraint when the scene needs gameplay clarity. |
| `Dalashade_HighlightProtection` | Raises the bright-pass threshold and reduces washout-prone glow. |
| `Dalashade_CombatPressure` | Dampens bloom during combat and slightly raises threshold. |
| `Dalashade_CinematicPermission` | Allows a small cinematic bloom boost only outside gameplay-critical moments. |

Manual testing controls:

| Control | Suggested tuning |
| --- | --- |
| `BloomStrength` | Default `0.32`. Overall bounded bloom strength. |
| `BloomThreshold` | Default `0.74`. Base bright-pass threshold before Dalashade protection changes. |
| `DiffusionStrength` | Default `0.42`. Cheap blur radius for the fixed sample ring. |
| `MagicGlowStrength` | Default `0.48`. Controls purple/aetherial tint contribution. |
| `NeonGlowStrength` | Default `0.42`. Controls cyan/neon tint contribution. |
| `HighlightRestraint` | Default `0.70`. Limits bright highlight washout. |
| `CombatDampenStrength` | Default `0.72`. Reduces bloom under combat pressure. |
| `CinematicBoostStrength` | Default `0.34`. Adds limited bloom when cinematic permission is high. |
| `ShowDebugMask` | Shows red bloom source, green magic/neon intent, and blue combat/highlight restraint. |

The shader uses a single-pass fixed sample ring rather than a large multi-pass blur. It is meant for cheap scene-aware glow, not large full-screen bloom. Bright coastal scenes should keep clean specular sparkle without nuclear sand/sky bloom; jungle scenes should get subtle canopy openings; rain should pick up small wet highlights; neon and magic should tint local luminous sources; combat and readability pressure should cut strength quickly.

## Smart Sharpen Controls

`Dalashade_SmartSharpen.fx` can be driven by Dalashade or tested manually in ReShade.

Dalashade-driven controls:

| Control | Purpose |
| --- | --- |
| `Dalashade_Readability` | Adds a small readable-edge clarity boost while still respecting safety dampening. |
| `Dalashade_Haze` | Reduces sharpening in fog, dust, cloud, and other hazy scenes. |
| `Dalashade_Wetness` | Reduces sharpening on wet or specular-prone bright edges. |
| `Dalashade_FoliageDensity` | Reduces fine texture sharpening in foliage-heavy scenes to avoid shimmer. |
| `Dalashade_CombatPressure` | Dampens sharpening in combat so clarity does not turn into clutter. |
| `Dalashade_HighlightProtection` | Reduces sharpening on bright highlight edges that are likely to halo. |
| `Dalashade_SharpenAuthority` | `0` passive, `1` secondary when another sharpener is active, `2` primary when SmartSharpen is the main sharpen pass. |

Manual testing controls:

| Control | Suggested tuning |
| --- | --- |
| `SharpenStrength` | Default `0.36`. Overall bounded sharpen strength. |
| `EdgeClarityStrength` | Default `0.42`. Legacy edge-clarity multiplier retained for compatibility. |
| `StructuralClarityStrength` | Default `0.50`. Broad silhouette and geometry clarity for characters, trunks, rocks, buildings, and armor. |
| `TextureDetailStrength` | Default `0.24`. Adds limited fine-detail clarity; generated secondary-authority values reduce this heavily. |
| `AntiCrunchStrength` | Default `0.70`. Limits gritty or outlined high-contrast edges. |
| `DepthDampenStrength` | Default `0.55`. Reduces far-depth sharpening when depth data is usable. |
| `FarDepthDampenStrength` | Default `0.72`. Extra suppression for distant canopy, heat haze, fog, and background texture shimmer. |
| `FoliageDampenStrength` | Default `0.80`. Strongly reduces leaf/grass micro-detail sharpening in forest and jungle scenes. |
| `HighlightDampenStrength` | Default `0.72`. Reduces sharpening on bright and wet highlight edges. |
| `HaloDampenStrength` | Default `0.78`. Reduces halos around bright clouds, lamps, water glints, sand, and white architecture. |
| `SkyDampenStrength` | Default `0.78`. Suppresses sky, fog, and smooth-gradient sharpening. |
| `HazeDampenStrength` | Default `0.68`. Reduces sharpening in haze, fog, dust, and rain pressure. |
| `CombatDampenStrength` | Default `0.66`. Reduces sharpening under combat pressure. |
| `LumaOnlyStrength` | Default `0.80`. Uses luma-safe reconstruction to avoid color ringing and chroma halos. |
| `ShowDebugMask` / `DebugView` | Shows composite, structural edge, texture detail, final sharpen, dampening, or foliage/far-depth suppression masks. |

The shader uses two channels. Structural clarity uses broad, lower-frequency luma edges for silhouettes and readable geometry. Texture detail is separate and much smaller, then strongly dampened by foliage, haze, wetness, far depth, bright edges, sky/smooth gradients, and secondary sharpen authority. If Marty Sharpen or another non-Dalashade sharpener is active, generated SmartSharpen values default to secondary authority: lower overall strength, much lower texture detail, higher anti-crunch, and stronger foliage/far-depth/halo dampening. Do not use this shader as an anti-aliasing replacement.

## Supported SceneIntent Variables

Future Dalashade shaders can expose these scalar variables in a Dalashade section:

| Variable | Source |
| --- | --- |
| `Dalashade_Readability` | `SceneIntent.Readability` |
| `Dalashade_Atmosphere` | `SceneIntent.Atmosphere` |
| `Dalashade_HighlightProtection` | `SceneIntent.HighlightProtection` |
| `Dalashade_ShadowProtection` | `SceneIntent.ShadowProtection` |
| `Dalashade_Haze` | `SceneIntent.Haze` |
| `Dalashade_Wetness` | `SceneIntent.Wetness` |
| `Dalashade_Cold` | `SceneIntent.Cold` |
| `Dalashade_Heat` | `SceneIntent.Heat` |
| `Dalashade_MagicGlow` | `SceneIntent.MagicGlow` |
| `Dalashade_NeonGlow` | `SceneIntent.NeonGlow` |
| `Dalashade_FoliageDensity` | `SceneIntent.FoliageDensity` |
| `Dalashade_IndustrialHardness` | `SceneIntent.IndustrialHardness` |
| `Dalashade_CosmicMood` | `SceneIntent.CosmicMood` |
| `Dalashade_CombatPressure` | `SceneIntent.CombatPressure` |
| `Dalashade_CinematicPermission` | `SceneIntent.CinematicPermission` |
| `Dalashade_SharpenAuthority` | Active preset sharpen authority analysis, not `SceneIntent` |

SceneIntent values are normalized `0.0` to `1.0`. `Dalashade_SharpenAuthority` is preset-analysis-derived and uses `0`, `1`, or `2`.

`Dalashade_WeatherAtmosphere.fx` currently consumes `Readability`, `Atmosphere`, `HighlightProtection`, `ShadowProtection`, `Haze`, `Wetness`, `Cold`, `Heat`, `MagicGlow`, `NeonGlow`, `FoliageDensity`, `CombatPressure`, and `CinematicPermission`.

`Dalashade_AdaptiveGrade.fx` currently consumes `Readability`, `Atmosphere`, `HighlightProtection`, `ShadowProtection`, `Cold`, `Heat`, `MagicGlow`, `NeonGlow`, `FoliageDensity`, `IndustrialHardness`, `CosmicMood`, `CombatPressure`, and `CinematicPermission`.

`Dalashade_AtmosphereBloom.fx` currently consumes `Atmosphere`, `MagicGlow`, `NeonGlow`, `FoliageDensity`, `Wetness`, `Heat`, `Readability`, `HighlightProtection`, `CombatPressure`, and `CinematicPermission`.

`Dalashade_SmartSharpen.fx` currently consumes `Readability`, `Haze`, `Wetness`, `FoliageDensity`, `CombatPressure`, `HighlightProtection`, and preset-derived `SharpenAuthority`.

## Example Preset Section

These are the kinds of preset sections the writer can update once matching shaders exist and custom shader support is enabled. With generated-preset injection enabled, Dalashade can add known Dalashade sections and variables to the generated preset automatically:

```ini
[Dalashade_WeatherAtmosphere.fx]
Dalashade_Readability=0.000000
Dalashade_Atmosphere=0.000000
Dalashade_HighlightProtection=0.000000
Dalashade_ShadowProtection=0.000000
Dalashade_Haze=0.000000
Dalashade_Wetness=0.000000
Dalashade_Cold=0.000000
Dalashade_Heat=0.000000
Dalashade_MagicGlow=0.000000
Dalashade_NeonGlow=0.000000
Dalashade_FoliageDensity=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_CinematicPermission=0.000000

[Dalashade_AdaptiveGrade.fx]
Dalashade_Readability=0.000000
Dalashade_Atmosphere=0.000000
Dalashade_HighlightProtection=0.000000
Dalashade_ShadowProtection=0.000000
Dalashade_Cold=0.000000
Dalashade_Heat=0.000000
Dalashade_MagicGlow=0.000000
Dalashade_NeonGlow=0.000000
Dalashade_FoliageDensity=0.000000
Dalashade_IndustrialHardness=0.000000
Dalashade_CosmicMood=0.000000
Dalashade_CinematicPermission=0.000000
Dalashade_CombatPressure=0.000000

[Dalashade_AtmosphereBloom.fx]
Dalashade_Atmosphere=0.000000
Dalashade_MagicGlow=0.000000
Dalashade_NeonGlow=0.000000
Dalashade_FoliageDensity=0.000000
Dalashade_Wetness=0.000000
Dalashade_Heat=0.000000
Dalashade_Readability=0.000000
Dalashade_HighlightProtection=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_CinematicPermission=0.000000

[Dalashade_SmartSharpen.fx]
Dalashade_Readability=0.000000
Dalashade_Haze=0.000000
Dalashade_Wetness=0.000000
Dalashade_FoliageDensity=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_HighlightProtection=0.000000
Dalashade_SharpenAuthority=0.000000
SharpenStrength=0.000000
EdgeClarityStrength=0.000000
StructuralClarityStrength=0.000000
TextureDetailStrength=0.000000
AntiCrunchStrength=0.000000
DepthDampenStrength=0.000000
FarDepthDampenStrength=0.000000
FoliageDampenStrength=0.000000
HighlightDampenStrength=0.000000
HaloDampenStrength=0.000000
SkyDampenStrength=0.000000
HazeDampenStrength=0.000000
CombatDampenStrength=0.000000
LumaOnlyStrength=0.000000
```

## Regression Fixture

A developer fixture exists at:

`test-presets/custom-shader-fixtures/DalashadeWeatherAtmosphere.ini`

It is not a user default preset. It exists so the preset regression harness can verify that `CustomShaderVariableMapper` detects the `Dalashade_WeatherAtmosphere.fx` section and writes SceneIntent variables when `EnableDalashadeCustomShaders` is enabled.

Regression reports include a custom shader section showing support state, detected Dalashade shader sections, whether the technique is listed/active, detected custom variables, changed custom variables, static bridge proof status, and the synthetic SceneIntent values used for the simulation.

For custom shader fixture presets, the regression harness enables custom shader writes inside the simulation so the static bridge path can be verified. This does not change normal generation behavior.

## Where To Edit

| Task | File |
| --- | --- |
| Add or rename exported intent variables | `Dalashade/CustomShaderVariableMapper.cs` |
| Change custom bridge diagnostics | `Dalashade/CustomShaderBridgeDiagnostics.cs` |
| Change how intent values are produced | `Dalashade/SceneIntent.cs` |
| Change diagnostics UI | `Dalashade/Windows/MainWindow.cs` |
| Change report output | `Dalashade/CompatibilityReportExporter.cs` |
| Add future shader files | `shaders/` |

Do not add custom shader behavior inside `PresetWriter` beyond the existing mapper call unless the writer contract itself needs to change.
