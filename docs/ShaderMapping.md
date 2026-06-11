# Shader Mapping

This page documents how Dalashade detects shader support and maps visual profile values to ReShade preset variables.

## Implemented

Shader variable mapping lives in `Dalashade/ShaderVariableMapper.cs`.

Preset effect detection lives mainly in `Dalashade/PresetAnalyzer.cs`.

| File | Owns |
| --- | --- |
| `Dalashade/ShaderVariableMapper.cs` | Supported sections, variable mappings, value modes, clamps, and per-key adjustment logic. |
| `Dalashade/PresetAnalyzer.cs` | Active technique parsing, effect role classification, support level, risk, and authority detection. |
| `Dalashade/GenerationAuthorityPolicy.cs` | Primary/secondary role policy and secondary authority dampening. |
| `Dalashade/CompatibilityReportExporter.cs` | Human-readable shader support and mapping validation output. |

## How Effects Are Detected

`PresetAnalyzer.Analyze(Configuration configuration)` reads the selected preset and parses:

| INI data | Purpose |
| --- | --- |
| `Techniques=` | Determines confirmed active techniques when present. |
| `TechniqueSorting=` | Helps identify available/sorted techniques. |
| INI section names | Finds shader/effect sections. |

Each detected shader becomes a `PresetTechnique` with:

| Field | Meaning |
| --- | --- |
| `TechniqueName` | Technique or section-derived name. |
| `ShaderFile` | `.fx` file or section name where available. |
| `Role` | Visual role such as ColorGrade, Bloom, AoGi, Sharpen, LUT, Dof, or Utility. |
| `SupportLevel` | Fully controlled, partially controlled, detected only, or unsupported. |
| `Risk` | Safe, moderate, high, GPose-only, or utility-ignore. |
| `ActivationState` | Active, inactive, or unknown. |

## Strict Matching, Fallbacks, And Loose Keys

`Configuration.ShaderMatchingMode` controls how aggressively mappings are applied.

| Mode | Meaning |
| --- | --- |
| `StrictSections` | Safest mode. Only exact known section/key mappings are used. |
| `KnownFallbacks` | Allows controlled fallback behavior for known shader families. |
| `LooseKeys` | Riskier mode. Can match by key more broadly when section identity is less certain. |

Loose key matching is risky because many shaders reuse generic names such as `Strength`, `Exposure`, `Gamma`, or `Saturation` with different ranges and meanings. Do not expand loose key behavior without diagnostics and real preset examples.

## Supported Shader Families Currently In Code

The mapper includes Marty/iMMERSE-oriented support plus broader free shader stack support. Inspect `ShaderVariableMapper.CreateDefinitions` and related `Add...Definitions` methods for the current exact list.

Implemented families include, but are not limited to:

| Family | Examples |
| --- | --- |
| Marty/iMMERSE | `MartysMods_REGRADE.fx`, `MartysMods_REGRADE+.fx`, `MartysMods_MXAO.fx`, `MartysMods_RTGI.fx`, `MartysMods_FFTBLOOM.fx`, `MartysMods_RELIGHT.fx`, `MartysMods_SHARPEN.fx`, `MartysMods_CLARITY.fx`. |
| qUINT | `qUINT_lightroom.fx`, `qUINT_mxao.fx`, `qUINT_bloom.fx`, `qUINT_dof.fx`. |
| Bloom | `GaussianBloom.fx`, `BloomingHDR.fx`, `Pirate_Bloom.fx`, `PirateBloom.fx`. |
| Sharpen | `FilmicSharpen.fx`, `FineSharp.fx`, `LumaSharpen.fx`, `CAS.fx`, `HighPassSharpen.fx`, `AdaptiveSharpen.fx`. |
| Color and LUT | `LUT.fx`, `MultiLUT.fx`, `MultiLUTFaustus86.fx`, `DPX.fx`, `Vibrance.fx`, `Colourfulness.fx`, `Technicolor2.fx`, `FilmicPass.fx`, `Levels.fx`, `Tonemap.fx`. |
| PD80/prod80 | `PD80_04_Color_Temperature.fx`, `PD80_04_Contrast_Brightness_Saturation.fx`, `PD80_03_Shadows_Midtones_Highlights.fx`, `PD80_02_Cinetools_LUT.fx`, `PD80_03_Filmic_Adaptation.fx`. |
| Utility/high-risk detection | DOF, Prism, Vignette, FilmGrain, StageDepth, AspectRatioComposition, VerticalPreviewer, ColorIsolation, KeepUI/RestoreUI style utilities. |

This table is an index, not a complete implementation reference. Use the mapper source as the authority for exact section names, keys, clamps, and modes.

## Partial Vs Full Support

Support level is reported by `PresetAnalyzer`.

| Level | Meaning |
| --- | --- |
| `FullyControlled` | Dalashade can adjust the primary useful variables for that effect family. |
| `PartiallyControlled` | Some useful variables are mapped, but not the whole shader. |
| `DetectedOnly` | Dalashade recognizes the shader but does not safely change it yet. |
| `Unsupported` | No meaningful support is currently known. |

Detected-only is still useful. It helps diagnostics warn about high-risk or style-defining effects without changing them.

## Effect Authorities

Authority detection is implemented in `PresetAnalyzer.BuildAuthorities` and policy handling is implemented in `GenerationAuthorityPolicy`.

Current authority roles include:

| Role | Purpose |
| --- | --- |
| ColorGrade | Main color/tone authority. |
| Bloom | Main bloom authority. |
| Sharpen | Main sharpening authority. |
| AoGi | Main ambient occlusion/global illumination authority. |

For selected compatibility modes, secondary authorities can receive reduced adjustment strength. This is intended to avoid several active shaders fighting for the same visual role.

## Adding Support For A New Shader Safely

Checklist:

1. Identify exact `.fx` filename and section names from real presets.
2. Collect real preset variable keys and current value ranges.
3. Add analyzer detection only where the shader identity is clear.
4. Add strict section mappings with conservative clamps.
5. Do not change selectors, texture names, quality presets, debug keys, or pass toggles during normal generation.
6. Add report labels or mapping validation support if the shader should appear as controlled instead of detected-only.
7. Test against a real preset and inspect changed variables.
8. Avoid generic loose-key edits unless the section is known and diagnostics are clear.

## Do Not Edit This Unless...

Do not change `ShaderVariableMapper` unless the task is specifically about generated preset values, shader support, clamps, or compatibility behavior. UI-only tasks should not touch it.
