# Shader Authoring

This page documents the current scaffolding and Dalashade custom ReShade shader prototypes.

## Current Status

Implemented plugin support:

- Top-level `shaders/` folder exists for custom `.fx` files.
- `Configuration.EnableDalashadeCustomShaders` gates all custom shader variable writes.
- `CustomShaderVariableMapper` writes stable `SceneIntent` values into Dalashade custom shader sections when matching sections and keys exist in generated preset content.
- MaterialProfile builds scene-level material plausibility priors for diagnostics and MaterialIntent. MaterialIntent uniforms are optional and zero-impact by default. Dalashade writes them only when `EnableMaterialIntent`, `EnableMaterialIntentShaderMapping`, and positive `MaterialIntentStrength` are all active.
- `Configuration.AutoInjectDalashadeCustomShaderSections` can optionally add known Dalashade custom shader sections to the generated preset only.
- Main window and compatibility reports show whether custom shader support is enabled, which custom sections were detected, and which custom variables were written.
- User Mode surfaces first-party shader families as effect status cards. These cards should stay user-facing: setup state, local shader-file presence, active/inactive status, safe controls, and actionable warnings only. Raw shader variables, debug modes, resolver diagnostics, and authoring details belong in Developer Mode.

Implemented shader prototypes:

- `shaders/Dalashade_WeatherAtmosphere.fx`
- Scene-aware weather and air pass: depth-weighted fog/dust, wet air glow, coastal highlight control, canopy air, snow/cold separation, and gameplay dampening.
- `shaders/Dalashade_AdaptiveGrade.fx`
- Scene-aware native grade: exposure trim, contrast, selective saturation, biome temperature/tint, highlight shoulder, black-depth preservation, and industrial/cosmic bias.
- `shaders/Dalashade_SmartSharpen.fx`
- Context-aware clarity pass that avoids sharpening haze, sky gradients, wet highlights, foliage shimmer, far-depth detail, and combat clutter.
- `shaders/Dalashade_AtmosphereBloom.fx`
- Selective atmospheric bloom for bright sources, wet speculars, canopy openings, distant heat glow, magic/aether, and neon accents.
- `shaders/Dalashade_MaterialDebug.fx`
- Optional false-color MaterialIntent/debug visualizer for screen-space material heuristics. It includes `shaders/Dalashade_MaterialMasks.fxh` and is disabled by default.
- `shaders/Dalashade_NormalDebug.fx`
- Optional NormalField visualizer for screen-space inferred normals, receiver masks, and safety suppression. It includes `shaders/Dalashade_NormalField.fxh` and is disabled by default.
- `shaders/Dalashade_SceneGI.fx`
- Optional screen-space GI-style pass for shallow contact AO, material-aware ambient bounce, and night light pooling. It is not path tracing, RTGI, or PTGI.
- `shaders/Dalashade_SurfaceReflection.fx`
- Optional material-aware reflection-impression pass for water-plane sheen, wet glints, ice sheen, localized neon/aether/fire reflections, receiver-type projection, and a restrained pseudo-SSR component. It is not full SSR, ray tracing, RTGI, or PTGI.

Not implemented yet:

- No IPC, named pipe, JSON bridge, or live ReShade bridge exists yet.
- No native ReShade add-on exists yet.
- No automatic shader installation/copying exists yet.
- Release packaging of custom files in `shaders/` is not guaranteed by this document. Check the current release task and zip contents before assuming a shader is included in a published plugin package.

Bridge/add-on integration is planned and not currently implemented. Do not treat this document as a native bridge or IPC implementation reference.

## Safety Rules

UI controls for new shader features must respect the User/Developer split:

1. Add normal-player controls to User Mode only when they are safe, task-level decisions with clear wording.
2. Keep raw variable names, debug modes, resolver gates, and diagnostics in Developer Mode.
3. Reuse the shared UI page registry and feature status card patterns instead of adding one-off sections.
4. Do not make a User Mode toggle imply ReShade compile success or true runtime effect visibility. Local `.fx` file presence only means a file was found in an inferred ReShade shader search path.

Custom shader writes are intentionally conservative:

1. `EnableDalashadeCustomShaders` must be enabled.
2. The preset must already contain a Dalashade custom shader section, or generated-preset-only injection must be explicitly enabled.
3. The section name must start with `Dalashade` or include `/Dalashade` or `\Dalashade`.
4. The variable key must exactly match a known `Dalashade_*` SceneIntent variable.
5. No custom shader is required for normal operation.

When `AutoInjectDalashadeCustomShaderSections` is off, Dalashade does not insert shader sections during generation.

When both `EnableDalashadeCustomShaders` and `AutoInjectDalashadeCustomShaderSections` are on, Dalashade may inject known custom shader sections and variables into the generated preset only. It never mutates the base preset. Current injection support is limited to `[Dalashade_WeatherAtmosphere.fx]`, `[Dalashade_AdaptiveGrade.fx]`, `[Dalashade_SmartSharpen.fx]`, `[Dalashade_AtmosphereBloom.fx]`, `[Dalashade_MaterialDebug.fx]`, `[Dalashade_NormalDebug.fx]`, `[Dalashade_SceneGI.fx]`, and `[Dalashade_SurfaceReflection.fx]`.

MaterialIntent variable injection is skipped unless MaterialIntent shader mapping is explicitly enabled. Disabled mapping does not write zeroes into existing material keys and does not add material keys during generated-preset-only injection. Current MaterialIntent shader behavior is section-scoped per first-party shader so each shader receives only the material channels it actually uses.

Material flow is layered:

1. SceneTags describe biome, weather, mood, area, gameplay, material, and art-direction context.
2. MaterialProfile turns those tags plus territory/weather text, screenshot metrics, and SceneIntent values into scene plausibility priors such as `jungleCanopy`, `coastalWaterline`, `snowfield`, `desertOpen`, `neonUrban`, `aetherialLandscape`, `dungeonInterior`, or `raidArena`.
3. MaterialIntent consumes those priors and outputs normalized scene-level material channels. MaterialProfile is treated as a plausibility prior/gate instead of a direct additive boost; reports separate profile prior, non-profile evidence, final value, and suppressions.
4. Shader-side MaterialMasks still decide pixel-level influence. Debug docs use `RawCandidate` for local pixel evidence, `SceneGatedCandidate` for local evidence scaled by scene plausibility, and `FinalMask` for the shader-specific result after conflicts and depth/smoothness checks.

MaterialProfile and MaterialIntent are plausibility gates, not true engine material IDs. A high scene-level value should make matching pixels eligible; it should not tint or alter the whole frame by itself.

Initial MaterialIntent uniforms reserved for first-party Dalashade shaders:

| Uniform | Meaning |
| --- | --- |
| `Dalashade_MaterialFoliage` | Effective foliage likelihood. |
| `Dalashade_MaterialWaterSpecular` | Effective water/wet/specular likelihood. Backward-compatible combined channel. |
| `Dalashade_MaterialWaterPlane` | Optional split alias written from WaterSpecular for broad water-surface masks. |
| `Dalashade_MaterialSpecularGlint` | Optional split alias written from WaterSpecular for thin reflective/glint masks. |
| `Dalashade_MaterialSandDust` | Effective sand/dust likelihood. |
| `Dalashade_MaterialSnowIce` | Effective snow/ice likelihood. |
| `Dalashade_MaterialStoneRuins` | Effective stone/ruins likelihood. |
| `Dalashade_MaterialMetalIndustrial` | Effective metal/industrial likelihood. |
| `Dalashade_MaterialCrystalAether` | Effective crystal/aether likelihood. |
| `Dalashade_MaterialNeonGlass` | Effective neon/glass likelihood. |
| `Dalashade_MaterialFireLavaHeat` | Effective fire/lava/heat likelihood. |
| `Dalashade_MaterialSkyCloudFog` | Effective sky/cloud/fog likelihood. |
| `Dalashade_MaterialSkinProtection` | Effective skin/character protection likelihood. |
| `Dalashade_MaterialVoidDarkness` | Effective void/darkness likelihood. |

Final written material channel values are raw inferred `MaterialIntent` values multiplied by `MaterialIntentStrength`. Missing uniforms and missing custom shader sections are skipped safely.

Custom shader variables are grouped by ownership:

| Class | Examples | Written by Dalashade? |
| --- | --- | --- |
| Dalashade-controlled SceneIntent variables | `Dalashade_Readability`, `Dalashade_Atmosphere`, `Dalashade_Haze`, `Dalashade_CombatPressure` | Yes, when custom shader support is enabled and matching keys exist. |
| MaterialIntent channel uniforms | `Dalashade_MaterialFoliage`, `Dalashade_MaterialSkyCloudFog`, `Dalashade_MaterialWaterSpecular` | Yes, only when MaterialIntent, MaterialIntent shader mapping, custom shader support, and matching section-scoped keys are all enabled. |
| NormalField diagnostic uniforms | `Dalashade_NormalDebugEnabled`, `Dalashade_NormalDebugMode`, `Dalashade_NormalFieldStrength`, `Dalashade_NormalDepthStrength`, `Dalashade_NormalDetailStrength` | Yes, only when NormalField, NormalField shader mapping, custom shader support, and matching section-scoped keys are all enabled. NormalDebug remains a manual diagnostic technique. |
| Shader-owned controls | `Dalashade_EnableDepthAssist`, `Dalashade_DepthAssistStrength`, `Dalashade_DepthAssistConfidenceFloor`, `Dalashade_DepthConfidenceFloor`, MaterialDebug debug mode/opacity/strength controls | No active writes. These may be known or injected with default values, but users control them in ReShade. SceneGI debug controls are the exception: they can be written when `EnableDalashadeSceneGIShaderVariables` is enabled. |

The generated WeatherAtmosphere section includes the weather shader intent variables Dalashade currently knows how to write: `Dalashade_Haze`, `Dalashade_Wetness`, `Dalashade_Cold`, `Dalashade_Heat`, `Dalashade_HighlightProtection`, `Dalashade_ShadowProtection`, `Dalashade_CombatPressure`, `Dalashade_Atmosphere`, `Dalashade_MagicGlow`, `Dalashade_NeonGlow`, `Dalashade_FoliageDensity`, `Dalashade_IndustrialHardness`, `Dalashade_CosmicMood`, `Dalashade_Readability`, `Dalashade_Night`, `Dalashade_Moonlight`, `Dalashade_ArtificialLight`, `Dalashade_AmbientDarkness`, `Dalashade_NightAtmosphere`, and `Dalashade_CinematicPermission`. When MaterialIntent shader mapping is enabled, WeatherAtmosphere may also receive `Dalashade_MaterialFoliage`, `Dalashade_MaterialSandDust`, `Dalashade_MaterialSnowIce`, `Dalashade_MaterialStoneRuins`, `Dalashade_MaterialMetalIndustrial`, `Dalashade_MaterialWaterSpecular`, `Dalashade_MaterialWaterPlane`, `Dalashade_MaterialSpecularGlint`, `Dalashade_WaterContext`, `Dalashade_CoastalContext`, `Dalashade_OpenOceanContext`, `Dalashade_ShallowWaterContext`, `Dalashade_WetSurfaceContext`, `Dalashade_MaterialCrystalAether`, `Dalashade_MaterialSkyCloudFog`, and `Dalashade_MaterialVoidDarkness`.

The generated AdaptiveGrade section includes the grade shader intent variables Dalashade currently knows how to write: `Dalashade_Readability`, `Dalashade_Atmosphere`, `Dalashade_HighlightProtection`, `Dalashade_ShadowProtection`, `Dalashade_Cold`, `Dalashade_Heat`, `Dalashade_MagicGlow`, `Dalashade_NeonGlow`, `Dalashade_FoliageDensity`, `Dalashade_IndustrialHardness`, `Dalashade_CosmicMood`, `Dalashade_Night`, `Dalashade_Moonlight`, `Dalashade_ArtificialLight`, `Dalashade_AmbientDarkness`, `Dalashade_NightAtmosphere`, `Dalashade_CinematicPermission`, and `Dalashade_CombatPressure`. When MaterialIntent shader mapping is enabled, AdaptiveGrade may also receive `Dalashade_MaterialFoliage`, `Dalashade_MaterialSandDust`, `Dalashade_MaterialSnowIce`, `Dalashade_MaterialMetalIndustrial`, `Dalashade_MaterialCrystalAether`, `Dalashade_MaterialSkinProtection`, and `Dalashade_MaterialVoidDarkness`.

The generated SmartSharpen section includes the clarity shader intent variables Dalashade currently knows how to write: `Dalashade_Readability`, `Dalashade_Haze`, `Dalashade_Wetness`, `Dalashade_FoliageDensity`, `Dalashade_CombatPressure`, `Dalashade_HighlightProtection`, `Dalashade_Night`, `Dalashade_AmbientDarkness`, `Dalashade_ArtificialLight`, and `Dalashade_SharpenAuthority`. It can also write SmartSharpen tuning sliders such as `SharpenStrength`, `StructuralClarityStrength`, `TextureDetailStrength`, and dampening controls when those keys exist or are injected. When MaterialIntent shader mapping is enabled, SmartSharpen may also receive `Dalashade_MaterialFoliage`, `Dalashade_MaterialWaterSpecular`, `Dalashade_MaterialWaterPlane`, `Dalashade_MaterialSpecularGlint`, `Dalashade_MaterialSnowIce`, `Dalashade_MaterialSkyCloudFog`, and `Dalashade_MaterialSkinProtection`.

The generated AtmosphereBloom section includes the bloom shader intent variables Dalashade currently knows how to write: `Dalashade_Atmosphere`, `Dalashade_MagicGlow`, `Dalashade_NeonGlow`, `Dalashade_FoliageDensity`, `Dalashade_Wetness`, `Dalashade_Heat`, `Dalashade_Readability`, `Dalashade_HighlightProtection`, `Dalashade_Night`, `Dalashade_Moonlight`, `Dalashade_ArtificialLight`, `Dalashade_AmbientDarkness`, `Dalashade_NightAtmosphere`, `Dalashade_CombatPressure`, and `Dalashade_CinematicPermission`. When MaterialIntent shader mapping is enabled, AtmosphereBloom may also receive `Dalashade_MaterialWaterSpecular`, `Dalashade_MaterialWaterPlane`, `Dalashade_MaterialSpecularGlint`, `Dalashade_MaterialCrystalAether`, `Dalashade_MaterialNeonGlass`, `Dalashade_MaterialFireLavaHeat`, and `Dalashade_MaterialSkyCloudFog`.

The generated MaterialDebug section contains MaterialIntent channel variables only. It may receive `Dalashade_MaterialFoliage`, `Dalashade_MaterialWaterSpecular`, `Dalashade_MaterialWaterPlane`, `Dalashade_MaterialSpecularGlint`, `Dalashade_MaterialSandDust`, `Dalashade_MaterialSnowIce`, `Dalashade_MaterialStoneRuins`, `Dalashade_MaterialMetalIndustrial`, `Dalashade_MaterialCrystalAether`, `Dalashade_MaterialNeonGlass`, `Dalashade_MaterialFireLavaHeat`, `Dalashade_MaterialSkyCloudFog`, `Dalashade_MaterialSkinProtection`, and `Dalashade_MaterialVoidDarkness` when MaterialIntent shader mapping is enabled. Debug mode, overlay mode, opacity, and strength stay in the `.fx` UI.

The generated NormalDebug section is optional and diagnostic. It can receive the same MaterialIntent material/context variables as MaterialDebug when MaterialIntent shader mapping is enabled. It can also receive `Dalashade_NormalDebugEnabled`, `Dalashade_NormalDebugMode`, `Dalashade_NormalDebugBoost`, `Dalashade_NormalFieldStrength`, `Dalashade_NormalDepthStrength`, `Dalashade_NormalDetailStrength`, `Dalashade_NormalMaterialInfluence`, `Dalashade_NormalWaterSuppression`, `Dalashade_NormalSkinSuppression`, and `Dalashade_NormalSkySuppression` only when NormalField shader mapping is explicitly enabled. The technique is never auto-enabled.

The generated SceneGI section includes conservative GI controls and `Dalashade_Intent*` aliases for the SceneIntent values the shader consumes. `EnableDalashadeSceneGIShaderVariables` controls whether Dalashade actively rewrites those GI controls during generation; technique activation remains manual. When MaterialIntent shader mapping is enabled, SceneGI may also receive `Dalashade_MaterialFoliage`, `Dalashade_MaterialWaterPlane`, `Dalashade_MaterialSpecularGlint`, `Dalashade_MaterialSandDust`, `Dalashade_MaterialSnowIce`, `Dalashade_MaterialStoneRuins`, `Dalashade_MaterialMetalIndustrial`, `Dalashade_MaterialCrystalAether`, `Dalashade_MaterialNeonGlass`, `Dalashade_MaterialFireLavaHeat`, `Dalashade_MaterialSkyCloudFog`, `Dalashade_MaterialSkinProtection`, and `Dalashade_MaterialVoidDarkness`.

The generated SurfaceReflection section includes reflection-impression controls and the SceneIntent values the shader consumes. `EnableDalashadeSurfaceReflectionShaderVariables` controls whether Dalashade actively rewrites those reflection controls during generation; technique activation remains manual. When MaterialIntent shader mapping is enabled, SurfaceReflection may also receive `Dalashade_MaterialWaterPlane`, `Dalashade_MaterialSpecularGlint`, `Dalashade_WaterContext`, `Dalashade_CoastalContext`, `Dalashade_OpenOceanContext`, `Dalashade_ShallowWaterContext`, `Dalashade_WetSurfaceContext`, `Dalashade_MaterialSandDust`, `Dalashade_MaterialSnowIce`, `Dalashade_MaterialMetalIndustrial`, `Dalashade_MaterialCrystalAether`, `Dalashade_MaterialNeonGlass`, `Dalashade_MaterialFireLavaHeat`, `Dalashade_MaterialSkyCloudFog`, and `Dalashade_MaterialSkinProtection`.

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
| Base preset contains a Dalashade custom shader section | The selected base preset has a section such as `[Dalashade_WeatherAtmosphere.fx]`, `[Dalashade_AdaptiveGrade.fx]`, `[Dalashade_SmartSharpen.fx]`, `[Dalashade_AtmosphereBloom.fx]`, `[Dalashade_MaterialDebug.fx]`, `[Dalashade_SceneGI.fx]`, or `[Dalashade_SurfaceReflection.fx]`, so Dalashade can inspect it for supported `Dalashade_*` keys. |
| Base preset technique active/inactive/unknown | Dalashade checks whether base preset sections appear active in `Techniques=`. Generated-preset-only section injection does not imply the technique is active unless Dalashade technique activation sync is enabled. |
| Known custom variables found | Matching `Dalashade_*` keys were found in a Dalashade custom shader section. |
| Variables detected but unchanged | Keys exist, but generation did not write values. Common causes are disabled custom shader support, inactive/unknown technique state, write-mode settings, or values already matching SceneIntent. |
| Variables written | `SceneIntent` values were written into matching `Dalashade_*` keys during generation. |
| SmartSharpen authority | Whether `Dalashade_SmartSharpen.fx` should behave as primary, secondary, or passive based on other active sharpeners in the preset. |

This is not the same as shader file installation. ReShade must still be able to find the actual `.fx` file through its own shader search paths. If the section exists but ReShade cannot compile or enable the effect, check ReShade's shader path and install the relevant file from `shaders/` manually.

Technique activation sync is disabled by default. Dalashade can inject known sections and variables into the generated preset without enabling techniques, or it can opt-in manage Dalashade production entries in `Techniques=` when `SyncDalashadeTechniqueActivation` is enabled. Base preset technique inactive/unknown means only that the base preset did not confirm activation. Users must still install the relevant `.fx` files through ReShade shader search paths.

Compatibility analysis classifies the known `Dalashade_*` first-party shader sections as controlled Dalashade effects when they appear in a preset. This means reports should treat them as known first-party color, bloom, clarity, atmosphere, AO/GI, or debug utility roles rather than unsupported third-party effects. Variable writes still require matching generated-preset section/key lines and custom shader support enabled.

## Recommended ReShade Order

Recommended gameplay order for a full Dalashade-aware stack:

8. iMMERSE Launchpad
9. Deband
10. iMMERSE Pro Clarity
11. iMMERSE Pro ReGrade
12. Dalashade_AdaptiveGrade
13. Dalashade_SceneGI
14. Dalashade_SurfaceReflection
15. MagicBloom
16. Dalashade_AtmosphereBloom
17. Dalashade_WeatherAtmosphere
18. iMMERSE Sharpen
19. Dalashade_SmartSharpen
20. Dalashade_NormalDebug
21. Dalashade_MaterialDebug

When debugging a first-party shader, temporarily put that shader last or near-last and use its full replacement or contribution-over-black debug output. Restore the gameplay order after tuning. `Dalashade_NormalDebug` and `Dalashade_MaterialDebug` should remain manual debug utilities, not normal production passes.

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

For initial `Dalashade_SceneGI.fx` testing, use this order:

1. After `Dalashade_AdaptiveGrade.fx`.
2. Before `Dalashade_SurfaceReflection.fx`, `Dalashade_AtmosphereBloom.fx`, `Dalashade_WeatherAtmosphere.fx`, and `Dalashade_SmartSharpen.fx`.
3. Before UI restore, KeepUI, or RestoreUI effects if applicable.
4. Enable `Dalashade_SceneGI` manually in ReShade only after copying the `.fx` file plus shared Dalashade include files into a ReShade shader search folder.

SceneGI is a screen-space approximation for contact AO, local ambient bounce, and night light pooling. It is not true path tracing, RTGI, or PTGI, and it does not replace paid third-party GI shaders.

For `Dalashade_SurfaceReflection.fx`, use this order:

1. After `Dalashade_SceneGI.fx`.
2. Before `Dalashade_AtmosphereBloom.fx`, `Dalashade_WeatherAtmosphere.fx`, and final sharpeners.
3. Before UI restore, KeepUI, or RestoreUI effects if applicable.
4. Enable `Dalashade_SurfaceReflection` manually in ReShade only after copying the `.fx` file plus shared Dalashade include files into a ReShade shader search folder.

SurfaceReflection is a screen-space reflection-impression pass for water-plane sheen, wet glints, snow/ice sheen, polished metal/glass glints, and localized neon/aether/fire reflection streaks. It is not true SSR or ray-traced reflection, and it should remain separate from SceneGI.

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

For `Dalashade_MaterialDebug.fx`, use this order only while debugging:

1. Place normal Dalashade and preset effects first.
2. Place `Dalashade_MaterialDebug` last or near-last.
3. Enable the `Dalashade_MaterialDebug` technique manually in ReShade.
4. Disable the technique or set `Dalashade_MaterialDebugMode=0` for normal gameplay.

The debug shader is a false-color overlay. It does not improve the image and is not required for normal Dalashade generation.

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
| `Dalashade_Night` | Activates nighttime air behavior: darker baseline and less generic haze. |
| `Dalashade_Moonlight` | Adds cool moonlit depth and canopy/sky separation where open-sky or cold/cosmic tags support it. |
| `Dalashade_ArtificialLight` | Adds localized lamp/window/neon/crystal light response without full-frame brightening. |
| `Dalashade_AmbientDarkness` | Preserves darker unlit areas and reduces shadow lift/haze in night scenes. |
| `Dalashade_NightAtmosphere` | Adds weather/air pressure for misty, stormy, humid, coastal, or moonlit night scenes. |
| `Dalashade_CinematicPermission` | Allows a small boost to atmosphere outside gameplay-critical moments. |
| `Dalashade_MaterialFoliage` | Supports humid canopy air in forest/jungle scenes without raising gray veil. |
| `Dalashade_MaterialSandDust` | Supports warm distance haze and heat/dust air while keeping foreground clear. |
| `Dalashade_MaterialSnowIce` | Supports cold air, snow/blizzard atmosphere, and white highlight protection. |
| `Dalashade_MaterialWaterSpecular` | Backward-compatible combined water/wet/specular likelihood. |
| `Dalashade_MaterialWaterPlane` | Supports coastal mist or water-plane atmosphere only when wetness/fog/haze also supports it. |
| `Dalashade_MaterialSpecularGlint` | Supports small wet highlight response; it should not create broad mist by itself. |
| `Dalashade_MaterialCrystalAether` | Supports subtle cosmic/aetherial depth veil and cool magical air. |
| `Dalashade_MaterialSkyCloudFog` | Strengthens actual fog/mist/sky depth behavior without turning gloom into fog by itself. |

Manual testing controls:

| Control | Suggested tuning |
| --- | --- |
| `Manual Overall Strength` | Default `0.35`. Raise only while tuning; high values make all responses more visible. |
| `Manual Haze Boost` | Adds haze without needing Dalashade scene tags. Useful for testing depth behavior. |
| `Manual Glow Boost` | Adds glow without needing wet, magic, or neon intent. |
| `Manual Storm/Dark Mood` | Tests storm darkening and cool mood response. |
| `Show Debug Mask` | Shows red depth haze, green glow, and blue protection/readability pressure. |
| `Debug View` | Selects composite, depth haze, highlight protection, weather glow, foliage dampening, or heat/dust masks. |
| `Dalashade_MaterialDebugMode` | Material-aware debug override: `0` off, `1` overview, `2` foliage humidity, `3` sand/dust depth, `4` snow/ice air, `5` water/wet mist, `6` crystal/aether veil, `7` sky/fog depth, `8` final air influence, `9` water plane air, `10` specular glint response. |
| `Dalashade_MaterialDebugStrength` | Scales material debug-mask visibility. Debug masks show inferred influence, not true engine material IDs. |

The shader intentionally does not blur the whole frame or disable any ReShade techniques. It shapes color, haze, glow, highlight rolloff, and mild softness through bounded masks. Heat haze and depth haze are weighted toward distance so hot desert nights avoid full-screen lift. Foliage-heavy atmospheric scenes reduce veil haze and get a small canopy-light response instead of gray wash. Night inputs make air darker and more local: moonlight can tint distant/open-sky air, artificial light can affect bright local pools, and ambient darkness suppresses gray haze in unlit areas. MaterialIntent adds selective air identity: foliage contributes humid canopy air, sand/dust contributes distance haze, snow/ice contributes cold air and white protection, water/specular contributes wet/coastal mist only with weather support, crystal/aether contributes subtle depth veil, and sky/cloud/fog reinforces real fog/mist/sky depth. Gloom remains mood/darkness; it does not become fog unless fog, mist, haze, or sky/fog signals are present. Combat-heavy scenes should visibly reduce the heaviest atmosphere while retaining light weather identity.

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
| `Dalashade_Night` | Enables night grading that deepens ambient darkness instead of globally lifting midtones. |
| `Dalashade_Moonlight` | Adds cool-neutral moonlit separation for open-sky/cold/cosmic night scenes. |
| `Dalashade_ArtificialLight` | Adds warm or emissive local light-pool bias for lamps, windows, fire, neon, and crystal scenes. |
| `Dalashade_AmbientDarkness` | Preserves deeper unlit shadows and black depth. |
| `Dalashade_NightAtmosphere` | Supports night air diagnostics/debug color without becoming a full haze control. |
| `Dalashade_CinematicPermission` | Allows a small cinematic contrast, saturation, and color-bias lift. |
| `Dalashade_CombatPressure` | Dampens heavy grading and slightly reduces exposure, saturation, and shadow lift. |
| `Dalashade_MaterialFoliage` | Supports richer greens while preserving trunk/background black depth. |
| `Dalashade_MaterialSandDust` | Supports warm midtones and highlight rolloff without pushing orange mud. |
| `Dalashade_MaterialSnowIce` | Supports cool clarity and white rolloff without gray snow. |
| `Dalashade_MaterialMetalIndustrial` | Supports cooler, harder contrast while restraining brittle highlights. |
| `Dalashade_MaterialCrystalAether` | Protects saturated glow colors and allows subtle aether/cosmic tint support. |
| `Dalashade_MaterialSkinProtection` | Reduces extreme tint/saturation shifts on likely smooth foreground skin-tone regions. |
| `Dalashade_MaterialVoidDarkness` | Preserves black depth and reduces gray shadow wash in void/dark scenes. |

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

The shader intentionally clamps per-channel movement relative to the source and clamps final output away from pure black and pure white. It uses selective color response: coastal/desert heat warms bright mids, jungle foliage gets richer greens without gray lift, snow/cosmic scenes cool highlights and shadows, and imperial/industrial scenes become harder and less pastel. Night grading deepens unlit regions, adds cool-neutral moonlit mids when supported, and lets artificial light affect localized bright pools instead of raising the whole frame. It is a native safety grade, not a full creative LUT replacement.

MaterialIntent influences are intentionally subordinate to SceneIntent. They bias existing grade behavior instead of replacing it: foliage, sand/dust, snow/ice, metal/industrial, crystal/aether, skin protection, and void/darkness make small adjustments to color identity, highlight rolloff, tint restraint, and black-depth preservation. There are no AdaptiveGrade-specific material debug modes yet; use compatibility report written-variable diagnostics to confirm MaterialIntent values.

## SceneGI Controls

`Dalashade_SceneGI.fx` can be tested manually in ReShade or driven by generated Dalashade variables when `EnableDalashadeSceneGIShaderVariables` is enabled.

Dalashade-driven controls:

| Control | Purpose |
| --- | --- |
| `Dalashade_GIEnabled` | Master enable for the SceneGI pass. Generated section default is `1.0`; technique activation still remains manual. |
| `Dalashade_GIStrength` | Overall bounded strength for AO, bounce, and night pooling. Default `0.35`. |
| `Dalashade_GIRadius` | Small screen-space sampling radius for local GI-style response. Default `0.65`. |
| `Dalashade_GIBounceStrength` | Material-aware ambient bounce intensity. Default `0.20`. |
| `Dalashade_GIAOIntensity` | Contact AO intensity. Default `0.25`. |
| `Dalashade_GIAORadius` | Small-radius AO sampling size. Default `0.45`. |
| `Dalashade_GINightLightStrength` | Localized night light pooling strength. Default `0.30`. |
| `Dalashade_GIMaterialInfluence` | How much MaterialIntent masks steer bounce color and protection. Default `0.50`. |
| `Dalashade_GISkyReject` | Suppresses AO/bounce/light pooling on sky, fog, and broad atmosphere. Default `1.0`. |
| `Dalashade_GISkinProtect` | Suppresses tinting and dirty AO on likely skin/character regions. Default `1.0`. |
| `Dalashade_GIDebugMode` | Integer enum: `0` normal, `1` AO, `2` bounce, `3` night light pooling, `4` material influence, `5` sky rejection, `6` skin protection, `7` final GI influence, `8` depth-normal confidence, `9` emissive source, `10` bounce receiver, `11` adaptive limits/safety, `12` layered AO breakdown, `13` clamp pressure, `14` SSGI diffuse gather. Dalashade writes it as `0` through `14`, not as a normalized float. |
| `Dalashade_GIDebugOutputMode` | Integer enum: `0` full replacement diagnostic, `1` alpha overlay over original scene, `2` side-by-side split, `3` contribution over black, `4` amplified difference view. Default `0` makes debug modes true diagnostic masks. |
| `Dalashade_GIDebugOpacity` | Debug overlay opacity. Default `0.75`. |
| `Dalashade_GIDebugBoost` | Debug-only amplification/gamma helper for low and mid-strength GI masks. Default `2.50`; it does not affect normal SceneGI output. |
| `Dalashade_Intent*` aliases | SceneIntent inputs for readability, atmosphere, highlight/shadow protection, haze, weather, glow, foliage density, industrial/cosmic mood, combat pressure, and cinematic permission. |
| `Dalashade_Daylight`, `Dalashade_Sunlight`, `Dalashade_OpenSkyLight`, `Dalashade_SurfaceHeat`, `Dalashade_DayAtmosphere`, `Dalashade_DayReflection`, `Dalashade_DayHighlightPressure` | Daytime context inputs. They are not exposure boosts; use them to steer highlight rolloff, open-sky reflection, daytime atmosphere, surface heat, GI receiver/source permission, and sharpening restraint. |
| `Dalashade_Material*` channels | Section-scoped MaterialIntent inputs for foliage, water plane, specular glints, sand/dust, snow/ice, stone/ruins, metal/industrial, crystal/aether, neon/glass, fire/heat, sky/fog, skin protection, and void/darkness. |

SceneGI uses cheap local screen-space samples, depth, depth-normal confidence, SceneIntent, and MaterialIntent masks to provide a stronger but still bounded GI-style lighting layer. Contact AO is layered into micro contact, medium crevice, and broad grounding responses; it is stronger on stone, ruins, industrial, foliage, and hard-surface areas, and reduced on sky/fog, skin, broad water, snow, bright sand, combat-heavy scenes, and high-highlight regions. Emissive source detection now gives saturated aether/crystal, neon, fire, lamp-like highlights, and specular glints more influence even when they are not pure white, then uses nearby local samples to spread restrained color onto plausible receiver surfaces such as grass, paths, trunks, stone, water planes, and structures. SceneGI separates high-frequency glints/detail from a lower-frequency material region term so broad GI receiver eligibility and material color influence are less edge-only. Bounce is restrained to shadows and midtones and uses material masks for foliage, sand, snow, water plane, fire, aether, neon, metal, stone, and void behavior. Night light pooling looks for localized lamp/fire/aether/neon/specular/moonlit candidates instead of globally lifting dark scenes. The final output uses adaptive positive/negative contribution limits rather than a fixed narrow clamp, so night, cinematic, emissive, ruin, industrial, and aetherial scenes can show more GI while beaches, snow, sky/fog, skin, water, combat, and readability-heavy scenes stay restrained. Debug output mode `0` returns replacement diagnostics instead of tinting the beautified scene, and `Dalashade_GIDebugBoost` makes low and mid-strength AO, bounce, night light, material, sky rejection, skin protection, final influence, depth-normal confidence, emissive source, receiver, safety, and layered-AO masks easier to inspect.

Normal gameplay output is unchanged unless the `Dalashade_SceneGI` technique is enabled in ReShade or added to the generated preset through opt-in Dalashade technique activation sync. Generated-preset injection may add the section and variables without activating the technique.

## Surface Reflection Controls

`Dalashade_SurfaceReflection.fx` can be tested manually in ReShade or driven by generated Dalashade variables when `EnableDalashadeSurfaceReflectionShaderVariables` is enabled.

Dalashade-driven controls:

| Control | Purpose |
| --- | --- |
| `Dalashade_SurfaceReflectionEnabled` | Master enable for the pass. Generated section default is `0.0` so injection alone cannot change visuals; technique activation remains manual. |
| `Dalashade_SurfaceReflectionStrength` | Overall bounded strength for water sheen, wet glints, ice sheen, and emissive reflection impressions. |
| `Dalashade_WaterSheenStrength` | Broad water-plane sheen strength. Uses `WaterPlane`, not thin `SpecularGlint`, for water surfaces. |
| `Dalashade_WaterReflectionStrength` | Water-plane reflection-impression strength. Uses cheap vertical screen samples on broad water receivers; object-like shape comes from the separate pseudo-SSR/projection path. |
| `Dalashade_WaterSheenRadius` | Small directional sample radius for water/surface sheen impression. This is not full SSR tracing. |
| `Dalashade_WaterContext` | Scene-level water plausibility prior for the staged water resolver. This is not a pixel mask. |
| `Dalashade_CoastalContext` | Scene-level coastal/beach/harbor prior used to permit shoreline and shallow-water detection. |
| `Dalashade_OpenOceanContext` | Scene-level open-ocean/horizon prior used for distant water and sky-source reflection color. |
| `Dalashade_ShallowWaterContext` | Scene-level shallow/turquoise water prior used for Costa-style water surfaces. |
| `Dalashade_WetSurfaceContext` | Scene-level rain/wet-surface prior; currently written from SceneIntent wetness and used for wet hard-surface receivers. |
| `Dalashade_SpecularGlintStrength` | Tight highlight/glint shaping for rails, lamps, wet edges, water sparkles, polished metal, and aether edges. |
| `Dalashade_SpecularReflectionStrength` | Tight reflected-highlight/streak strength for glint sources. It should stay localized and never create broad water reflection alone. |
| `Dalashade_WetReflectionStrength` | Wet-surface reflection impression for roads, stone, wood, and metal when `Dalashade_Wetness` supports it. |
| `Dalashade_AetherReflectionStrength` | Cyan/violet/magenta aether reflection impression for crystal or magical scenes. |
| `Dalashade_NeonReflectionStrength` | Cyan/magenta neon/glass reflection impression for high-tech scenes. |
| `Dalashade_IceSheenStrength` | Cold crisp sheen for snow/ice scenes without dirtying bright snow. |
| `Dalashade_SurfaceReflectionSkyReject` | Suppresses water/reflection influence on sky, cloud, fog, and broad atmosphere. |
| `Dalashade_SurfaceReflectionSkinProtect` | Suppresses glint/tint on likely skin or character-smooth regions. |
| `Dalashade_ReflectionSampleOffset` | Small screen-space sampling offset for the reflection impression. Default `0.018`; larger values are more obvious but less stable. |
| `Dalashade_ReflectionSoftness` | Blends nearby taps for softened reflection color. |
| `Dalashade_ReflectionDepthReject` | Depth-discontinuity rejection strength for cheap reflection samples. |
| `Dalashade_SurfaceReflectionDebugMode` | Integer enum: `0` normal, `1` WaterPlane sheen, `2` SpecularGlint, `3` wet reflection, `4` aether/neon reflection, `5` sky rejection, `6` skin protection, `7` final reflection influence, `8` contribution over black, `9` reflection source mask, `10` reflection receiver mask, `11` water projected reflection, `12` wet hard projected reflection, `13` metal/aether projected reflection, `14` pseudo SSR contribution. |
| `Dalashade_SurfaceReflectionDebugOutputMode` | Integer enum: `0` full replacement diagnostic, `1` alpha overlay over original scene, `2` side-by-side split, `3` contribution over black, `4` amplified difference view. Default `0` makes debug modes true diagnostic masks. |
| `Dalashade_SurfaceReflectionDebugOpacity` | Debug overlay opacity. |
| `Dalashade_SurfaceReflectionDebugBoost` | Debug-only amplification helper for low and mid-strength masks. |
| `Dalashade_Wetness` | Allows wet-surface reflection impression when rain/wet tags support it. |
| `Dalashade_HighlightProtection` | Restrains bright sand, snow, sky, and clipped highlight response. |
| `Dalashade_Readability` / `Dalashade_CombatPressure` | Dampens reflection contribution in gameplay-critical scenes. |
| `Dalashade_MagicGlow` / `Dalashade_NeonGlow` | Lets aether/neon material masks respond more visibly when scene tags support them. |
| `Dalashade_Night` / `Dalashade_ArtificialLight` | Allows slightly stronger localized night lamp/neon/aether glints without global exposure lift. |
| `Dalashade_MaterialWaterPlane` | Broad water-surface eligibility for restrained cyan/teal sheen. |
| `Dalashade_MaterialSpecularGlint` | Thin glint eligibility for highlights, rails, lamps, wet edges, and water sparkles. |
| `Dalashade_MaterialSandDust` | Suppresses fake water response on bright warm sand while allowing tiny supported glints. |
| `Dalashade_MaterialSnowIce` | Supports cold ice/snow sheen and protects bright snow from dirty reflection. |
| `Dalashade_MaterialMetalIndustrial` | Supports polished metal/glass glints without broad water-plane behavior. |
| `Dalashade_MaterialCrystalAether` / `Dalashade_MaterialNeonGlass` / `Dalashade_MaterialFireLavaHeat` | Localized emissive reflection color for aether, neon, and fire/heat sources. |
| `Dalashade_MaterialSkyCloudFog` | Rejects sky/fog contamination. |
| `Dalashade_MaterialSkinProtection` | Protects likely skin/character regions. |

SurfaceReflection intentionally separates `WaterPlane` from `SpecularGlint`. The staged water resolver first uses scene-level context uniforms such as `Dalashade_WaterContext`, `Dalashade_CoastalContext`, `Dalashade_OpenOceanContext`, and `Dalashade_ShallowWaterContext`, then performs shader-side pixel classification for raw cyan water, raw deep water, shallow water, deep water, water horizon, wet shoreline, foam/edge, water receiver, reflection source-color candidates, sky source-color candidates, and rejection masks. WaterPlane drives broad cyan/teal water or shallow-water sheen and receives a cheap vertical screen-space reflection impression. SpecularGlint drives thin reflective highlights on rails, wet edges, lamps, polished metal, water sparkles, and aether edges. Wetness allows stronger reflection impressions on dark smooth hard surfaces, while CrystalAether, NeonGlass, and FireLavaHeat can create localized cyan/violet/magenta/warm reflection streaks on plausible receiver surfaces. The shader keeps separate internal receiver masks for water, wet hard surfaces, metal, aether/glass, and ice, plus a rejection mask for sky, skin, warm dry terrain, and bright matte surfaces. Reflection source-color masks and receiver masks both need confidence before reflection is applied, so bright terrain, sky/horizon color, or broad receiver regions alone should not turn the scene glossy. Debug mode output is routed before normal final blending; output mode `0` is a full replacement mask for tuning.

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
| `Dalashade_Night` | Raises bloom selectivity at night so dark scenes do not turn milky. |
| `Dalashade_Moonlight` | Allows subtle cool moonlit diffusion only when night atmosphere supports it. |
| `Dalashade_ArtificialLight` | Emphasizes localized lamps, windows, neon, fire, and crystals. |
| `Dalashade_AmbientDarkness` | Restrains broad wash and protects dark baseline. |
| `Dalashade_NightAtmosphere` | Allows limited weather/moonlit diffusion without global bloom. |
| `Dalashade_MaterialWaterSpecular` | Backward-compatible combined water/wet/specular likelihood. |
| `Dalashade_MaterialWaterPlane` | Adds restrained broad water/coastal shimmer eligibility when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialSpecularGlint` | Adds tight reflective highlight/glint bloom eligibility when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialCrystalAether` | Adds color-selective aether glow instead of generic full-screen bloom when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialNeonGlass` | Adds small-radius colored neon/glass bloom with strong highlight control when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialFireLavaHeat` | Adds warm source glow and distant heat eligibility while respecting combat dampening when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialSkyCloudFog` | Allows broad sky/atmospheric glow but increases wash restraint to avoid milky haze when MaterialIntent mapping is enabled. |

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
| `Dalashade_MaterialDebugMode` | Material-aware debug override: `0` off, `1` overview, `2` water/specular, `3` crystal/aether, `4` neon/glass, `5` fire/heat, `6` sky/fog, `7` final bloom eligibility, `8` water plane, `9` specular glint. |
| `Dalashade_MaterialDebugStrength` | Scales material debug-mask visibility. Debug masks show inferred influence, not true engine material IDs. |

The shader uses a single-pass fixed sample ring rather than a large multi-pass blur. It is meant for cheap scene-aware glow, not large full-screen bloom. Bright coastal scenes should keep clean specular sparkle without nuclear sand/sky bloom; jungle scenes should get subtle canopy openings; rain should pick up small wet highlights; neon and magic should tint local luminous sources; combat and readability pressure should cut strength quickly. At night, bloom becomes more selective: ambient darkness raises restraint, artificial light lowers eligibility only around bright local pools, and moonlight adds only subtle atmospheric diffusion. MaterialIntent only makes bloom eligibility more selective: water/specular softens glints, crystal/aether and neon/glass require color/luma cues, fire/heat favors warm sources, and sky/fog gets extra wash restraint.

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
| `Dalashade_Night` | Enables dark-region sharpening restraint for night scenes. |
| `Dalashade_AmbientDarkness` | Suppresses sharpening in deep unlit shadows so night foliage and shadow noise do not crunch. |
| `Dalashade_ArtificialLight` | Allows modest structural clarity on lit silhouettes and architecture without boosting texture detail. |
| `Dalashade_SharpenAuthority` | `0` passive, `1` secondary when another sharpener is active, `2` primary when SmartSharpen is the main sharpen pass. |
| `Dalashade_MaterialFoliage` | Further reduces leaf, grass, and canopy micro-sharpening when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialWaterSpecular` | Backward-compatible combined water/wet/specular likelihood. |
| `Dalashade_MaterialWaterPlane` | Reduces broad water-surface shimmer sharpening when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialSpecularGlint` | Protects thin reflective highlights from sharpening halos when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialSnowIce` | Reduces snow noise and black-on-white haloing when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialSkyCloudFog` | Excludes smooth sky, fog, cloud, and atmospheric gradients from sharpening when MaterialIntent mapping is enabled. |
| `Dalashade_MaterialSkinProtection` | Reduces aggressive sharpening on likely smooth warm foreground skin-tone regions when MaterialIntent mapping is enabled. |

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
| `Dalashade_MaterialDebugMode` | Material-aware debug override: `0` off, `1` overview, `2` foliage, `3` water/specular, `4` snow/ice, `5` sky/fog, `6` skin protection, `7` water plane, `8` specular glint, `10` final material dampening. |
| `Dalashade_MaterialDebugStrength` | Scales material debug-mask visibility. Debug masks show inferred influence, not true engine material IDs. |
| `Dalashade_EnableDepthAssist` | Optional material-mask helper. Default `false`; depth is never required for SmartSharpen material dampening. |
| `Dalashade_DepthAssistStrength` | Default `0.0`. Allows valid depth to help sky/fog, water, snow, and foreground protection separation. |
| `Dalashade_DepthAssistConfidenceFloor` / `Dalashade_DepthConfidenceFloor` | Default `0.0`. Minimum confidence only when depth assist is enabled and depth has been verified. The shorter name is an alias for generated presets and future shader revisions. |

The shader uses two channels. Structural clarity uses broad, lower-frequency luma edges for silhouettes and readable geometry. Texture detail is separate and much smaller, then strongly dampened by foliage, haze, wetness, far depth, bright edges, sky/smooth gradients, deep night shadows, and secondary sharpen authority. Artificial-light intent can preserve modest structural clarity on lit subjects, but it does not increase micro-texture sharpening. MaterialIntent only adds additional dampening: foliage reduces leaf/grass/canopy crunch, water/specular reduces glint shimmer, snow/ice reduces bright snow noise and haloing, sky/cloud/fog excludes smooth gradients, and skin protection reduces aggressive smoothing-region sharpening. If Marty Sharpen or another non-Dalashade sharpener is active, generated SmartSharpen values default to secondary authority: lower overall strength, much lower texture detail, higher anti-crunch, and stronger foliage/far-depth/halo dampening. Do not use this shader as an anti-aliasing replacement.

## Material Debug Overlay

`Dalashade_MaterialDebug.fx` is a dedicated diagnostic overlay. Copy both `Dalashade_MaterialDebug.fx` and `Dalashade_MaterialMasks.fxh` into a ReShade shader search folder, regenerate the preset with custom shader support, MaterialIntent, MaterialIntent shader mapping, material debug masks, and generated-preset section injection enabled, then enable the `Dalashade_MaterialDebug` technique manually in ReShade. Keep it last or near-last in the stack while debugging.

## Normal Field Debug Overlay

`Dalashade_NormalDebug.fx` is a dedicated NormalField diagnostic overlay. Copy `Dalashade_NormalDebug.fx`, `Dalashade_NormalField.fxh`, and `Dalashade_MaterialMasks.fxh` into a ReShade shader search folder, regenerate the preset with custom shader support and generated-preset section injection enabled, then enable `Dalashade_NormalDebug` manually in ReShade. For debugging, place `Dalashade_NormalDebug` near the bottom, usually before or beside `Dalashade_MaterialDebug`.

NormalDebug visualizes inferred screen-space depth normals, detail normals, combined normals, ground/wall facing masks, normal confidence, shading receivers, reflection receivers, AO receivers, and safety suppression. It is not true game material normals, FFXIV G-buffer access, or real normal maps. Mode `0` and disabled `Dalashade_NormalDebugEnabled` are pass-through for normal gameplay.

The overlay visualizes shader-side material heuristic influence. It is not true FFXIV engine material-ID detection, so false positives are expected. Scene-level MaterialProfile/MaterialIntent priors gate each pixel mask; high scene-level foliage, water, snow, or aether values do not tint the whole screen unless individual pixels also match the local color/luma/saturation/edge/depth heuristics.

The shared mask include separates raw candidates, scene-level gates, and final conflict-resolved masks. `Dalashade_ResolveMaterials`, `Dalashade_ResolveWater`, and `Dalashade_ResolveSafety` are the first-party shader contract: production shaders should start from these shared material, water, and safety resolver outputs, then apply role-specific gates rather than reinventing base sky, water, skin, sand, foliage, or specular detection. `SkyCloudFog` uses smoothness, upper-screen prior, color families, clouds/overcast, warm dawn/dusk sky, night sky, canopy gaps, and optional depth; depth can help but is not required. `Foliage` separates strong leaves/grass/canopy from weak `OrganicGreenSurface` influence so mossy rocks or green-lit bark can damp sharpening slightly without appearing as full foliage in the master overlay.

Depth assist is shader-owned and disabled by default through `Dalashade_EnableDepthAssist=false` and `Dalashade_DepthAssistStrength=0.0`. When enabled, valid depth can boost sky/fog confidence in smooth far or missing-depth regions, suppress upper-screen water false positives, help separate snow fields from clouds, and help foreground skin protection. Depth is only supporting evidence: if depth is unavailable, flat, inverted, or unreliable, masks still run from color, smoothness, texture, screen region, and scene profile gates. DLSS, FSR, dynamic resolution, ReShade depth-buffer restrictions, or UI/depth mismatches can make depth unreliable. Depth confidence means "usable signal confidence" for the heuristic, not guaranteed correct FFXIV engine depth.

| Mode | Meaning | Color |
| --- | --- | --- |
| `0` | Off / pass-through | normal image |
| `1` | Overview final masks | mixed material colors |
| `2` | Combined final confidence | grayscale/white |
| `3` | Raw sky/fog candidate | blue |
| `4` | Scene-gated sky/fog candidate | cyan-blue |
| `5` | Final sky/fog mask | blue |
| `6` | Raw strong foliage candidate | green |
| `7` | Raw organic-green surface candidate | olive/brown-green |
| `8` | Final foliage influence | green plus olive weak influence |
| `9` | Raw water/specular candidate | cyan |
| `10` | Scene-gated water/specular candidate | cyan |
| `11` | Final water/specular mask | cyan |
| `12` | Raw snow/ice candidate | pale blue / white-blue |
| `13` | Scene-gated snow/ice candidate | pale blue / white-blue |
| `14` | Final snow/ice mask | pale blue / white-blue |
| `15` | Raw sand/dust candidate | orange/yellow |
| `16` | Scene-gated sand/dust candidate | orange/yellow |
| `17` | Final sand/dust mask | orange/yellow |
| `18` | Depth confidence | red near-depth, green far-depth, blue invalid/missing depth |
| `19` | Depth-assisted sky/fog comparison | red no-depth final, green depth-assisted final, blue delta |
| `20` | Final stone/ruins mask | stone gray |
| `21` | Final metal/industrial mask | steel blue-gray |
| `22` | Final crystal/aether mask | violet |
| `23` | Final neon/glass mask | hot magenta |
| `24` | Final fire/lava/heat mask | red/orange |
| `25` | Final skin-protection mask | peach/pink |
| `26` | Final void/darkness mask | purple |
| `27` | Raw water plane | cyan |
| `28` | Scene-gated water plane | cyan |
| `29` | Final water plane | cyan |
| `30` | Raw specular glint | white-blue |
| `31` | Scene-gated specular glint | white-blue |
| `32` | Final specular glint | white-blue |
| `33` | Water resolver overview | cyan/deep blue/white/red rejection |
| `34` | Raw cyan water | turquoise |
| `35` | Raw deep water | dark blue |
| `36` | Shallow water | bright turquoise |
| `37` | Deep water | dark teal-blue |
| `38` | Water horizon | blue |
| `39` | Wet shoreline | white-cyan |
| `40` | Foam/edge | white-cyan |
| `41` | Water receiver | cyan |
| `42` | Water reflection source-color eligibility | blue-cyan |
| `43` | Sky source vs sky reject | blue source, red reject |
| `44` | Sand/skin rejection | orange sand, pink skin |
| `45` | Water coherence | cyan |
| `46` | Shared safety overview | blue sky reject, peach skin reject, yellow highlight protect, cyan water AO reject, green foliage noise reject |
| `47` | Shared receiver confidence | green |
| `48` | Shared light source confidence | warm orange |
| `49` | SurfaceReflection receiver preview | cyan water, blue-gray wet hard surface, violet aether/glass |
| `50` | SceneGI receiver/source preview | green receiver, orange source |
| `51` | AtmosphereBloom eligibility preview | violet glow, blue sky diffusion |
| `52` | WeatherAtmosphere air influence preview | mixed air family colors |
| `53` | SmartSharpen dampening preview | shared safety and shimmer suppression colors |
| `54` | AdaptiveGrade material protection preview | foliage/sand/snow/skin/aether/void protection colors |
| `55` | Water/sky conflict | red sky wins, cyan water wins, yellow/white unresolved conflict |
| `56` | Water pixel confidence | cyan actual likely water pixels |
| `57` | Sky pixel confidence | blue actual sky/cloud/fog pixels |
| `58` | Water receiver vs horizon | cyan receiver water, blue horizon/source-only, red rejected sky |
| `59` | Receiver confidence split | cyan reflection, green structure, yellow/olive AO, faint gray legacy receiver |
| `60` | Water local proof | cyan local water proof |
| `61` | Strong water proof | bright cyan strong water-plane proof |
| `62` | Constructed/aether reject | magenta constructed cyan/aether/metal rejection |
| `63` | Sky dominance | red/orange sky dominance |
| `64` | Water proof boost | cyan-white local water proof boost |
| `65` | Competition internals overview | red sky dominance, cyan strong water proof, magenta constructed reject, yellow conflict |

Overlay mode `0` replaces the image with the debug mask, `1` alpha-blends over the game image, and `2` applies an additive/tint overlay. `Dalashade_MaterialDebugOpacity` controls visibility, and `Dalashade_MaterialDebugStrength` can disable the overlay without changing the selected mode.

## Material Calibration Workflow

Use this workflow when tuning MaterialProfile, MaterialIntent, or shader-side masks:

1. Generate a preset with custom shader support, generated-preset section injection, MaterialIntent, MaterialIntent diagnostics, and MaterialIntent shader mapping enabled.
2. Install `Dalashade_MaterialDebug.fx` and `Dalashade_MaterialMasks.fxh` in a ReShade shader search folder, then enable `Dalashade_MaterialDebug` manually in ReShade.
3. Check overview mode first. It should be color-coded and should not turn the whole screen white unless many final material masks genuinely overlap.
4. Check raw sky/fog, gated sky/fog, and final sky/fog. If raw is wrong, tune shader heuristics; if gated is wrong, inspect MaterialProfile/MaterialIntent; if final is wrong, inspect conflict suppression or depth assist.
5. Check raw strong foliage, organic green surface, and final foliage influence. Mossy rocks or green-lit bark may show weak organic-green influence, but should not read as full foliage in overview.
6. Check shader-specific SmartSharpen material debug modes to confirm foliage, sky/fog, water/specular, snow/ice, and skin protection are suppressing sharpening only where useful.
7. Test with depth assist off. This is the default and must remain usable.
8. If the ReShade depth buffer is known to work, enable depth assist in the relevant `.fx` UI and compare sky/water/snow/foreground separation.
9. Compare screenshots with the compatibility report. The report should make clear whether a failure came from scene profile plausibility, MaterialIntent strength/gating, raw pixel heuristics, final conflict suppression, optional depth assist, or production shader behavior.

Representative calibration scenes are Rak'tika or Yak T'el for foliage/canopy/mossy stone, Costa or La Noscea for water/sand/sky/palms, Coerthas or Garlemald for snow/cloud/stone/metal, Thanalan or Amh Araeng for sand/stone/heat sky, Solution Nine or Heritage Found for neon/glass/metal/artificial light, Ultima Thule/Mare/Elpis/Il Mheg for sky/aether/cosmic light, Azys Lla or Allagan areas for ruins/metal/aether, rainy Limsa-style city scenes for wet stone/specular separation, and explicit void/gothic scenes for VoidDarkness.

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
| `Dalashade_Night` | `SceneIntent.Night` |
| `Dalashade_Moonlight` | `SceneIntent.Moonlight` |
| `Dalashade_ArtificialLight` | `SceneIntent.ArtificialLight` |
| `Dalashade_AmbientDarkness` | `SceneIntent.AmbientDarkness` |
| `Dalashade_NightAtmosphere` | `SceneIntent.NightAtmosphere` |
| `Dalashade_CombatPressure` | `SceneIntent.CombatPressure` |
| `Dalashade_CinematicPermission` | `SceneIntent.CinematicPermission` |
| `Dalashade_SharpenAuthority` | Active preset sharpen authority analysis, not `SceneIntent` |

SceneIntent values are normalized `0.0` to `1.0`. `Dalashade_SharpenAuthority` is preset-analysis-derived and uses `0`, `1`, or `2`.

`Dalashade_WeatherAtmosphere.fx` currently consumes `Readability`, `Atmosphere`, `HighlightProtection`, `ShadowProtection`, `Haze`, `Wetness`, `Cold`, `Heat`, `MagicGlow`, `NeonGlow`, `FoliageDensity`, `IndustrialHardness`, `CosmicMood`, `Night`, `Moonlight`, `ArtificialLight`, `AmbientDarkness`, `NightAtmosphere`, `CombatPressure`, `CinematicPermission`, and the WeatherAtmosphere-only MaterialIntent channels listed above.

`Dalashade_AdaptiveGrade.fx` currently consumes `Readability`, `Atmosphere`, `HighlightProtection`, `ShadowProtection`, `Cold`, `Heat`, `MagicGlow`, `NeonGlow`, `FoliageDensity`, `IndustrialHardness`, `CosmicMood`, `Night`, `Moonlight`, `ArtificialLight`, `AmbientDarkness`, `NightAtmosphere`, `CombatPressure`, `CinematicPermission`, and the AdaptiveGrade-only MaterialIntent channels listed above.

`Dalashade_AtmosphereBloom.fx` currently consumes `Atmosphere`, `MagicGlow`, `NeonGlow`, `FoliageDensity`, `Wetness`, `Heat`, `Readability`, `HighlightProtection`, `Night`, `Moonlight`, `ArtificialLight`, `AmbientDarkness`, `NightAtmosphere`, `CombatPressure`, `CinematicPermission`, and the AtmosphereBloom-only MaterialIntent channels listed above.

`Dalashade_SmartSharpen.fx` currently consumes `Readability`, `Haze`, `Wetness`, `FoliageDensity`, `CombatPressure`, `HighlightProtection`, `Night`, `AmbientDarkness`, `ArtificialLight`, preset-derived `SharpenAuthority`, and the SmartSharpen-only MaterialIntent dampening channels listed above.

`Dalashade_SceneGI.fx` currently consumes `Dalashade_Intent*` aliases for `Readability`, `Atmosphere`, `HighlightProtection`, `ShadowProtection`, `Haze`, `Wetness`, `Cold`, `Heat`, `MagicGlow`, `NeonGlow`, `FoliageDensity`, `IndustrialHardness`, `CosmicMood`, `CombatPressure`, and `CinematicPermission`; the night aliases `Dalashade_Night`, `Dalashade_Moonlight`, `Dalashade_ArtificialLight`, `Dalashade_AmbientDarkness`, and `Dalashade_NightAtmosphere`; the daytime aliases `Dalashade_Daylight`, `Dalashade_Sunlight`, `Dalashade_OpenSkyLight`, `Dalashade_SurfaceHeat`, `Dalashade_DayAtmosphere`, `Dalashade_DayReflection`, and `Dalashade_DayHighlightPressure`; plus SceneGI controls and the SceneGI-only MaterialIntent channels listed above. SceneGI routes those through FrameData scene lanes before applying AO, bounce, and local-light behavior.

`Dalashade_SurfaceReflection.fx` currently consumes `Wetness`, `HighlightProtection`, `Readability`, `CombatPressure`, `MagicGlow`, `NeonGlow`, `Night`, `ArtificialLight`, `CinematicPermission`, SurfaceReflection controls, and the SurfaceReflection-only MaterialIntent channels listed above.

`Dalashade_MaterialDebug.fx` consumes only MaterialIntent/debug uniforms and uses `Dalashade_MaterialMasks.fxh` for screen-space heuristic masks. Mode `0` is pass-through, so normal output is unchanged when the debug mode is disabled.

The depth-assist controls `Dalashade_EnableDepthAssist`, `Dalashade_DepthAssistStrength`, `Dalashade_DepthAssistConfidenceFloor`, and the alias `Dalashade_DepthConfidenceFloor` are shader-owned. Generated-preset section injection can include them with zero-impact defaults, but Dalashade does not enable depth assist or append any debug technique to `Techniques=`.

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
Dalashade_Night=0.000000
Dalashade_Moonlight=0.000000
Dalashade_ArtificialLight=0.000000
Dalashade_AmbientDarkness=0.000000
Dalashade_NightAtmosphere=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_CinematicPermission=0.000000
Dalashade_MaterialFoliage=0.000000
Dalashade_MaterialSandDust=0.000000
Dalashade_MaterialSnowIce=0.000000
Dalashade_MaterialWaterSpecular=0.000000
Dalashade_MaterialCrystalAether=0.000000
Dalashade_MaterialSkyCloudFog=0.000000

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
Dalashade_Night=0.000000
Dalashade_Moonlight=0.000000
Dalashade_ArtificialLight=0.000000
Dalashade_AmbientDarkness=0.000000
Dalashade_NightAtmosphere=0.000000
Dalashade_CinematicPermission=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_MaterialFoliage=0.000000
Dalashade_MaterialSandDust=0.000000
Dalashade_MaterialSnowIce=0.000000
Dalashade_MaterialMetalIndustrial=0.000000
Dalashade_MaterialCrystalAether=0.000000
Dalashade_MaterialSkinProtection=0.000000
Dalashade_MaterialVoidDarkness=0.000000

[Dalashade_AtmosphereBloom.fx]
Dalashade_Atmosphere=0.000000
Dalashade_MagicGlow=0.000000
Dalashade_NeonGlow=0.000000
Dalashade_FoliageDensity=0.000000
Dalashade_Wetness=0.000000
Dalashade_Heat=0.000000
Dalashade_Readability=0.000000
Dalashade_HighlightProtection=0.000000
Dalashade_Night=0.000000
Dalashade_Moonlight=0.000000
Dalashade_ArtificialLight=0.000000
Dalashade_AmbientDarkness=0.000000
Dalashade_NightAtmosphere=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_CinematicPermission=0.000000
Dalashade_MaterialWaterSpecular=0.000000
Dalashade_MaterialCrystalAether=0.000000
Dalashade_MaterialNeonGlass=0.000000
Dalashade_MaterialFireLavaHeat=0.000000
Dalashade_MaterialSkyCloudFog=0.000000

[Dalashade_SmartSharpen.fx]
Dalashade_Readability=0.000000
Dalashade_Haze=0.000000
Dalashade_Wetness=0.000000
Dalashade_FoliageDensity=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_HighlightProtection=0.000000
Dalashade_Night=0.000000
Dalashade_AmbientDarkness=0.000000
Dalashade_ArtificialLight=0.000000
Dalashade_SharpenAuthority=0.000000
Dalashade_MaterialFoliage=0.000000
Dalashade_MaterialWaterSpecular=0.000000
Dalashade_MaterialSnowIce=0.000000
Dalashade_MaterialSkyCloudFog=0.000000
Dalashade_MaterialSkinProtection=0.000000
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

[Dalashade_MaterialDebug.fx]
Dalashade_MaterialFoliage=0.000000
Dalashade_MaterialWaterSpecular=0.000000
Dalashade_MaterialSandDust=0.000000
Dalashade_MaterialSnowIce=0.000000
Dalashade_MaterialStoneRuins=0.000000
Dalashade_MaterialMetalIndustrial=0.000000
Dalashade_MaterialCrystalAether=0.000000
Dalashade_MaterialNeonGlass=0.000000
Dalashade_MaterialFireLavaHeat=0.000000
Dalashade_MaterialSkyCloudFog=0.000000
Dalashade_MaterialSkinProtection=0.000000
Dalashade_MaterialVoidDarkness=0.000000

[Dalashade_SceneGI.fx]
Dalashade_GIEnabled=1.000000
Dalashade_GIStrength=0.350000
Dalashade_GIRadius=0.650000
Dalashade_GIBounceStrength=0.200000
Dalashade_GIAOIntensity=0.250000
Dalashade_GIAORadius=0.450000
Dalashade_GINightLightStrength=0.300000
Dalashade_GIMaterialInfluence=0.500000
Dalashade_GISkyReject=1.000000
Dalashade_GISkinProtect=1.000000
Dalashade_GIDebugMode=0
Dalashade_GIDebugOutputMode=0
Dalashade_GIDebugOpacity=0.750000
Dalashade_GIDebugBoost=2.500000
Dalashade_IntentReadability=0.000000
Dalashade_IntentAtmosphere=0.000000
Dalashade_IntentHighlightProtection=0.000000
Dalashade_IntentShadowProtection=0.000000
Dalashade_IntentHaze=0.000000
Dalashade_IntentWetness=0.000000
Dalashade_IntentCold=0.000000
Dalashade_IntentHeat=0.000000
Dalashade_IntentMagicGlow=0.000000
Dalashade_IntentNeonGlow=0.000000
Dalashade_IntentFoliageDensity=0.000000
Dalashade_IntentIndustrialHardness=0.000000
Dalashade_IntentCosmicMood=0.000000
Dalashade_Daylight=0.000000
Dalashade_Sunlight=0.000000
Dalashade_OpenSkyLight=0.000000
Dalashade_SurfaceHeat=0.000000
Dalashade_DayAtmosphere=0.000000
Dalashade_DayReflection=0.000000
Dalashade_DayHighlightPressure=0.000000
Dalashade_IntentCombatPressure=0.000000
Dalashade_IntentCinematicPermission=0.000000

[Dalashade_SurfaceReflection.fx]
Dalashade_SurfaceReflectionEnabled=0.000000
Dalashade_SurfaceReflectionStrength=0.320000
Dalashade_WaterSheenStrength=0.380000
Dalashade_WaterReflectionStrength=0.450000
Dalashade_WaterSheenRadius=1.350000
Dalashade_SpecularGlintStrength=0.320000
Dalashade_SpecularReflectionStrength=0.300000
Dalashade_WetReflectionStrength=0.300000
Dalashade_AetherReflectionStrength=0.360000
Dalashade_NeonReflectionStrength=0.340000
Dalashade_IceSheenStrength=0.240000
Dalashade_SurfaceReflectionSkyReject=1.000000
Dalashade_SurfaceReflectionSkinProtect=1.000000
Dalashade_ReflectionSampleOffset=0.018000
Dalashade_ReflectionSoftness=0.500000
Dalashade_ReflectionDepthReject=0.650000
Dalashade_SurfaceReflectionDebugMode=0
Dalashade_SurfaceReflectionDebugOutputMode=0
Dalashade_SurfaceReflectionDebugOpacity=0.750000
Dalashade_SurfaceReflectionDebugBoost=2.250000
Dalashade_Wetness=0.000000
Dalashade_HighlightProtection=0.000000
Dalashade_Readability=0.000000
Dalashade_CombatPressure=0.000000
Dalashade_MagicGlow=0.000000
Dalashade_NeonGlow=0.000000
Dalashade_Night=0.000000
Dalashade_ArtificialLight=0.000000
Dalashade_CinematicPermission=0.000000
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
