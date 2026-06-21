# Dalashade_SceneGI

## Current purpose

`shaders/Dalashade_SceneGI.fx` creates a cheap screen-space GI/AO impression using depth, material masks, localized source terms, and safety gates.

## Intended purpose

SceneGI should own conservative contact shading, low-frequency material bounce, localized night light pooling, and material-aware ambient response. It is not RTGI, PTGI, path tracing, or a replacement for game lighting.

## Current implementation summary

The shader samples nearby color/depth, builds receiver/source masks from inline FrameData material/water/safety/receiver and scene fields, then applies layered AO and restrained bounce/light pooling. Water and sky are protected from dirty AO; aether/neon/fire/glint sources can contribute localized light. Optional FrameData surface support adds contact/structure grounding only after material and safety gates allow it.

For shader-author rules, see [../ShaderContractQuickReference.md](../ShaderContractQuickReference.md). SceneGI should consume Dalapad only through FrameData fields and must keep the no-data debug state blank when Dalapad gates are off or unavailable.

The current GI path now includes a bounded screen-space diffuse gather. It samples visible nearby scene color in multiple directions, weights those samples by depth continuity, approximate facing, source brightness/chroma, emissive confidence, and receiver safety, then feeds the result into the material bounce lane. This is still screen-space GI, not world-space GI: it can only bounce color from visible current-frame pixels.

SceneGI routes its major scene decisions through shared FrameData scene lanes before applying GI-specific math. `DayOpenAir`, `NightLocalLight`, `WetAir`, `HeatAir`, `ColdAir`, `AetherTech`, `ForestCanopy`, `Industrial`, and `InteriorMood` are the first stage for environment-sensitive AO, bounce, and local-light behavior. Shader-local terms still shape the final GI role, but they should not redefine scene tags independently.

Material bounce now has explicit GI-local lanes for foliage, hard stone, metal/industrial surfaces, snow/sand climate surfaces, wet surfaces, and emissive pooling. These lanes do not create new material truth; they only decide how much existing FrameData and MaterialIntent evidence is allowed to steer low-frequency bounce, receiver eligibility, and debug output. Sky-safe receiver shaping remains separate so broad sky/fog/open-air evidence cannot turn into receiver material just because it has color or structure in the current frame.

## Inputs

- Backbuffer color and depth.
- Scene intent uniforms for night, moonlight, artificial light, ambient darkness, day/open sky, combat, duty, weather, and atmosphere.
- Material uniforms and water context.
- Shared `Dalashade_FrameData.fxh` base fields for canonical material/water/safety/receiver data.
- Shared FrameData scene tags for readability, atmosphere, combat, wetness, cold, heat, aether/neon, day/night lighting, and Standalone mode strength.
- Optional FrameData surface fields from NormalField and gated Dalapad pinned surface data for conservative contact/structure shaping.
- GI/AO strength/radius/debug controls.

## Outputs

Normal output is source color plus conservative AO/bounce modifications. Debug modes show source/receiver/final GI influence where available.

## Core algorithm

1. Resolve shared FrameData base, scene, and optional surface data.
2. Build GI scene lanes from shared FrameData derived tags: day/open air, night/local light, wet, heat, cold, aether/high-tech, forest/canopy, industrial, and interior.
3. Build GI sources from light/glint/aether/neon/fire and local luminance. `frame.SourceLightConfidence` can strengthen source/light pooling, but it does not authorize receivers by itself.
4. Build receivers from shared AO/structure receiver helpers, material hardness, water/sky/skin gates, surface support, and scene lanes.
5. Optionally add FrameData surface structure, AO, ground/contact, and edge-discontinuity support behind the same safety gates. That surface data may come from NormalField, Dalapad, or both.
6. Estimate local occlusion with layered depth taps.
7. Estimate screen-space diffuse bounce from depth-aware visible-color gathers.
8. Apply lane-shaped darkening/lighting with safety clamps.
9. Expose material bounce lane, sky-safe receiver, and emissive pooling diagnostics without adding new preset variables.

## Material/Water/Normal dependencies

SceneGI consumes inline FrameData fields. The shared material confidence path prefers `frame.ReceiverAO` and `frame.ReceiverStructure` over legacy broad `ReceiverBroad`; local material terms still shape the final GI role. Water surfaces suppress dirty AO; `frame.WaterWetShoreline` may support local light pooling.

SceneGI consumes the complete FrameData day/night scene contract: `Night`, `Moonlight`, `ArtificialLight`, `AmbientDarkness`, `NightAtmosphere`, `Daylight`, `Sunlight`, `OpenSkyLight`, `SurfaceHeat`, `DayAtmosphere`, `DayReflection`, and `DayHighlightPressure`. The generated preset can inject these keys for SceneGI, and the shader folds them into shared derived lanes instead of maintaining a separate night/day interpretation.

Optional FrameData surface support uses `surface.StructureCandidate` as structure grounding, `surface.AOReceiverSupport` as AO/contact support, `surface.GroundCandidate` as ground/contact shaping, `surface.EdgeDiscontinuity` as localized contact support only under safety gates, and `surface.NormalConfidence`/`surface.OrientationConfidence` as stability terms. FrameData owns the merge between NormalField and Dalapad. SceneGI consumes the merged fields and `surface.SurfaceDataInfluence`; it does not sample raw scan slots, group/MRT indices, or pinned resources directly.

When Dalapad surface data is enabled and the pinned normal candidate is available, FrameData treats it as strong normal-like and structure-like surface evidence. It can lift depth-normal confidence, structure/contact support, AO receiver support, and ground support after SceneGI's existing sky/skin/water/material safety gates. If global Dalapad shader additions are off, Dalapad surface data is off, strength is zero, or the pinned resource is unavailable, FrameData returns zero Dalapad confidence and SceneGI follows the NormalField/default path.

The current visibility pass deliberately gives more weight to valid GI lanes instead of using a broad global multiplier. Interior, industrial, forest, heat, cold, wet, and aether/high-tech lanes can now lift bounce, contact, and night pooling when FrameData receiver/safety fields agree. Standalone mode adds extra bounce/night visibility behind the same gates.

## First-party shader mode

`Dalashade_StandaloneStrength` is `0` in Supportive mode and `1` in Standalone mode. SceneGI uses it to modestly increase AO/contact, bounce, night light pooling, and final contribution allowances after combat/readability and material/safety gates. It does not make reflection receivers into AO receivers, dirty water, or create GI where sky/skin/water safety rejects it.

## First-party performance tiers

`Dalashade_FirstPartyPerformanceTier` records the selected tier and generated `Dalashade_GISampleCountScale` / `Dalashade_GISampleDistanceScale` control optional work.

| Tier | SceneGI behavior |
| --- | --- |
| Quality | Preserves the current full diffuse-gather path and generated GI/AO radii. The shader uses all cardinal, mid diagonal, and far diagonal gather taps. |
| Balanced | Keeps the cardinal and mid diagonal gather taps, skips the far diagonal pair, and modestly reduces generated GI sample distance/radii. This targets the expensive optional visible-color gather work without changing receiver/source gates. |
| Performance | Uses the cardinal gather taps only, further reduces generated GI sample distance/radii, and relies more on already-authorized FrameData/Dalapad surface evidence when available instead of expanding inferred screen-space work. |

Lower tiers must not increase AO, bounce, or night-light intensity to compensate for fewer samples. Blank Dalapad debug masks remain correct when Dalapad shader additions, surface data, strength, or pinned resources are unavailable.

## Debug modes

SceneGI debug modes are intended to show shared material usage, sources, receivers, AO, bounce, and final influence. Treat them as effect diagnostics, not material truth; use MaterialDebug for base material classification.

`Dalashade_GIDebugMode`:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Off | Normal output. |
| 1 | AO only | Layered AO mask. |
| 2 | Bounce only | Material bounce contribution. |
| 3 | Night light pooling | Local night/emissive pooling. |
| 4 | Material influence | Shared material receiver/source influence, with NormalField support shown subtly when enabled. |
| 5 | Sky rejection | Sky/fog safety rejection. |
| 6 | Skin protection | Skin safety rejection. |
| 7 | Final GI influence | Final combined GI/AO influence. |
| 8 | Depth-normal confidence | Depth normal and confidence support, including NormalField confidence when enabled. |
| 9 | Emissive source | Local/aether/neon/fire/glint source confidence. |
| 10 | Bounce receiver | Final bounce receiver mask. |
| 11 | Adaptive limits/safety | Positive/negative contribution and safety clamps. |
| 12 | Layered AO breakdown | Micro/medium/broad/structure AO channels plus NormalField contact support when enabled. |
| 13 | Clamp pressure | Red shows negative/AO clamp pressure, green shows safety pressure, blue shows positive/bounce clamp pressure. |
| 14 | SSGI diffuse gather | Shows the screen-space diffuse gather color and confidence before final bounce/clamp shaping. |
| 15 | Material bounce lanes | Shows foliage, stone, metal, climate, and wet material lanes used to shape low-frequency bounce. |
| 16 | Sky-safe receivers | Shows receiver sky safety, material safety, and final bounce receiver confidence. |
| 17 | Emissive pooling lanes | Shows base emissive pooling, propagated/source-supported pooling, and propagated source pressure. |
| 18 | Dalapad used contribution | Shows the Dalapad contribution actually allowed into SceneGI through FrameData after global/surface/availability gates and receiver safety. Red is normal confidence contribution, green is structure/contact contribution, blue is resulting depth-normal confidence. Blank means no authorized Dalapad contribution survived SceneGI safety. |
| 19 | Dalapad FrameData evidence | Shows the gated FrameData Dalapad normal direction and evidence channels before SceneGI receiver safety. Blank means Dalapad shader additions are off, Dalapad surface data is off, strength is zero, or the pinned normal resource is unavailable. |
| 20 | Dalapad bridge gate | Shows direct shader-side gate state for the pinned normal. Red is the global/surface/availability gate, green is sampled presence, blue means dimensions are known. Blank means there is no authorized Dalapad data for SceneGI. |
| 21 | Dalapad raw normal sample | Shows the direct pinned-normal texture sample behind the same gate. Blank means the gate is closed; visible color with mode 18 blank means the bridge works but SceneGI safety/confidence rejected contribution. |

`Dalashade_GIDebugOutputMode`:

| Mode | Label | Meaning |
| --- | --- | --- |
| 0 | Full replacement | Replace output with the selected debug view. |
| 1 | Alpha overlay over original | Blend debug over the source image. |
| 2 | Side-by-side split | Debug on the left, original on the right. |
| 3 | Contribution over black | Show debug/contribution without source image. |
| 4 | Amplified difference | Show amplified result-vs-source difference plus debug context. |

## Safety and suppression rules

Sky rejects AO/GI. Skin rejects dirty AO/tinting. Water suppresses dirty AO. Foliage uses foliage/noise damping. Combat/readability dampens heavy output. Snow and bright sand are protected from muddy darkening. FrameData surface contribution is multiplied by the same sky/skin/water safety gates and remains a shaping input, even when Dalapad provides strong normal-like evidence.

Source-vs-receiver separation is required: `frame.SourceLightConfidence` and emissive material fields may source local light/bounce, but `frame.ReceiverAO`, `frame.ReceiverStructure`, material support, and optional surface support decide where GI can land.

## Standalone shader boundaries

ContactTone should be standalone instead of folded into SceneGI. SceneGI owns indirect-light impression: AO, visible-color bounce, and local light pooling. ContactTone would own image-space grounding tone, local contrast, and object/terrain contact readability. Those controls need different user expectations, different strength budgets, and tighter combat/readability safety. Keeping it standalone means users can add crisp grounding without adding GI color bounce, and developers can tune contact contrast without disturbing SceneGI source/receiver math.

EmissiveAtmosphere should be standalone instead of folded into AtmosphereBloom. AtmosphereBloom owns highlight-local bloom and source glow eligibility. EmissiveAtmosphere would own broader air response around visible emissive families: aether haze, neon wash, fire warmth, and localized atmospheric color pooling. Those are not the same operation as blooming bright pixels. Keeping it standalone avoids making AtmosphereBloom responsible for scene-scale haze, reduces the risk of bloom spam, and lets emissive air color be gated by scene/material evidence separately from highlight thresholds.

## Current limitations

- Screen-space samples cannot see off-screen lights.
- Source/receiver masks are heuristic.
- AO can only approximate contact and crevice behavior.
- Diffuse gather can only use visible current-frame color; it cannot know hidden geometry, true albedo, or off-camera emitters.
- It cannot infer true light direction or material albedo.
- Dalapad pinned normal data is useful enough to treat as strong normal-like surface evidence, but it is still not a public or guaranteed FFXIV world-space normal contract.

## Future direction

Validate the GI visibility pass in coastal day/night, rain/storm, fog/overcast, desert/heat, snow/cold, forest/canopy, aether/high-tech, dungeon/interior, and combat scenes. Use debug mode 13 when normal output looks too subtle; high green means safety gates are expected to suppress output, while high red/blue means the final contribution clamps are limiting visible AO or bounce.

### Prepass path for GI and reflections

A future Dalapad prepass would help both SceneGI and SurfaceReflection by moving repeated inline inference into stable intermediate buffers. The first useful prepass should export:

- material/safety/receiver roles: sky reject, skin reject, water receiver, reflection receiver, AO receiver, structure receiver, source light confidence, aether/neon/fire source confidence.
- surface evidence: depth confidence, inferred normal, normal confidence, edge discontinuity, ground/wall/structure support.
- water/reflection support: water plane confidence, water receiver confidence, wet shoreline, horizon/source-only flags, water-vs-sky conflict.
- scene lanes: day/open air, night/local light, wet, heat, cold, aether/high-tech, forest/canopy, industrial, interior, combat/readability dampening.
- optional history targets after the first buffer proves stable: previous diffuse gather, previous reflection projection, and confidence/variance.

SceneGI would use this to perform cheaper, broader, more stable diffuse gathers with less duplicated resolver work. SurfaceReflection would use it to distinguish actual water receivers from source/context hints and to stabilize projected/reflected shapes over multiple frames. This requires render targets and likely temporal handling, so it is intentionally outside the current inline FrameData pass.

## Do not do

- Do not add expensive ray marching or temporal accumulation.
- Do not dirty sky, skin, water, or snow.
- Do not treat specular glints as broad diffuse bounce.
- Do not let NormalField create AO where material/safety rejects it.
