# Configuration

`Dalashade/Configuration.cs` stores user settings and generation behavior. It is serialized by Dalamud and should remain backward-compatible unless a migration is intentionally added.

## Major Setting Groups

| Group | Purpose |
| --- | --- |
| Paths | Base preset, generated preset, ReShade ini, shader path, screenshot folders, master style folders. |
| Generation | Target style, performance budget, compatibility mode, backup count, generation behavior. |
| Shader mapping | Known shader matching mode, inactive writes, iMMERSE Pro/Ultimate support, custom shader support. |
| First-party shader mode | Supportive vs standalone behavior for production Dalashade shaders. |
| First-party depth assist | Optional global generated-preset write that enables depth assist for Dalashade first-party shaders. |
| Scene and style | Screenshot analysis, master style mode, tuning preset, style strengths. |
| MaterialIntent | Material profile/intent diagnostics and shader mapping controls. |
| NormalField | Optional inferred normal/surface-field diagnostics and shader mapping controls. |
| Debug/export | Compatibility reports, debug bundles, diagnostics visibility. |
| Reload | ReShade reload hotkey and reload behavior. |

## MaterialIntent Settings

MaterialIntent settings control plugin-side material plausibility and generated material uniforms. Disabled MaterialIntent should produce neutral values and no material uniform writes.

MaterialIntent shader mapping must remain section-scoped. Do not force material uniforms into arbitrary shader sections.

## NormalField Settings

NormalField settings are disabled by default:

- `EnableNormalField`
- `EnableNormalFieldDiagnostics`
- `EnableNormalFieldShaderMapping`
- `NormalFieldStrength`
- `NormalFieldDepthStrength`
- `NormalFieldDetailStrength`
- `NormalFieldMaterialInfluence`
- `NormalFieldWaterSuppression`
- `NormalFieldSkinSuppression`
- `NormalFieldSkySuppression`
- `NormalFieldDebugMode`
- `NormalFieldDebugBoost`

When `EnableNormalField` is false, production output and generated preset variable writes must remain unchanged. When NormalField is enabled but shader mapping is disabled, diagnostics may report settings but generated presets should not receive NormalField uniforms.

## Custom Shader Settings

First-party custom shaders are optional. Dalashade may inject known generated-preset sections and write known uniforms when enabled, but it should not copy shader files, install shader packs, or append techniques to `Techniques=`.

`FirstPartyShaderMode` controls how strongly production first-party Dalashade shaders participate once custom shader variable writing is enabled:

- `Supportive` is the default. It writes `Dalashade_StandaloneStrength=0` and keeps AdaptiveGrade, SceneGI, SurfaceReflection, AtmosphereBloom, WeatherAtmosphere, and SmartSharpen close to their conservative base-preset enhancement behavior.
- `Standalone` writes `Dalashade_StandaloneStrength=1` to those production shader sections when the section declares the key. The shaders use it as a small multiplier behind existing material/safety gates so first-party Dalashade shaders can carry more of the stack without weakening sky, skin, water, foliage, snow, sand, highlight, or source/receiver protections.

Debug shaders are intentionally unaffected by `FirstPartyShaderMode`.

`EnableFirstPartyDepthAssist` is an opt-in generation behavior switch. When enabled, generated presets write:

- `Dalashade_EnableDepthAssist=1`
- `Dalashade_DepthAssistStrength=1`
- `Dalashade_DepthAssistConfidenceFloor=0`
- `Dalashade_DepthConfidenceFloor=0`

The write is limited to known first-party Dalashade shader sections that declare those uniforms. It requires custom shader variable writes to be enabled, does not install `.fx` files, and does not activate techniques.

## Path Safety

Exporters and writers must resolve safe defaults before calling path APIs. Empty configured paths should fall back to the plugin config directory:

- Reports: `Reports/Dalashade_CompatibilityReport_yyyyMMdd_HHmmss.md`
- Debug bundles: `DebugBundles/Dalashade_DebugBundle_yyyyMMdd_HHmmss/`

## UI Ownership

`Dalashade/Windows/ConfigWindow.cs` owns settings controls. `Dalashade/Windows/MainWindow.cs` owns status, generation, and diagnostics panels. UI should call services and helpers rather than implementing generation logic.

## Do Not Do

- Do not remove serialized fields without a migration.
- Do not make experimental systems enabled by default.
- Do not change the default `FirstPartyShaderMode` away from `Supportive` without a migration and explicit review.
- Do not enable first-party depth assist by default; it must remain an explicit generation behavior opt-in.
- Do not write NormalField or MaterialIntent shader variables unless their mapping settings allow it.
- Do not store sensitive external paths beyond what the user configured.
