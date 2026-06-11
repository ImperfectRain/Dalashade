# Master Style

This page documents implemented master style analysis and matching.

## What Master Style Is

Master style lets users point Dalashade at reference screenshots so the generated preset can bias the current scene toward the reference look. It is not a full image-to-image match. It is a conservative profile bias applied before shader variable mapping.

## Implemented Files

| File | Owns |
| --- | --- |
| `Dalashade/MasterStyle.cs` | Scans and analyzes master image folders. |
| `Dalashade/MasterStyleMatcher.cs` | Compares current scene analysis to master analysis and produces profile adjustments, diagnostics, and rules. |
| `Dalashade/MasterStyleDiagnostics.cs` | Diagnostic models for effective strength, tonal deltas, tonal color deltas, and family comparison rows. |
| `Dalashade/ImageAnalysis.cs` | Image statistics used by current scene and master style analysis. |
| `Dalashade/VisualProfile.cs` | Calls the matcher during profile generation. |
| `Dalashade/Windows/MainWindow.cs` | Shows master style diagnostics. |
| `Dalashade/Windows/ConfigWindow.cs` | Shows master style settings and tuning controls. |

## Master Image Analysis

`MasterStyleService.Refresh(Configuration configuration, ImageAnalysisResult currentScene, bool force = false)` handles master analysis.

Implemented behavior:

| Feature | Notes |
| --- | --- |
| Master folder | Controlled by `Configuration.MasterPresetImageFolderPath`. |
| Supported images | The service enumerates image files from the configured folder. |
| Subfolders | Optional through `Configuration.MasterPresetIncludeSubfolders`. |
| Max images | Controlled by `Configuration.MasterPresetMaxImages`. |
| Modes | `NewestImageOnly`, `AverageFolder`, `MedianFolder`, and `ClosestToCurrentScene`. |
| Current-scene comparison | Used when closest-scene mode or scene similarity diagnostics need current image stats. |

## Effective Strength

The global user control is `Configuration.MasterPresetStyleStrength`.

The matcher also uses tuning fields from configuration:

| Config field | Purpose |
| --- | --- |
| `MasterTonalMatchStrength` | Strength for luminance and contrast matching. |
| `MasterTonalColorStrength` | Strength for shadow/midtone/highlight hue and saturation bias. |
| `MasterColorFamilyStrength` | Strength for per-family color adjustments. |
| `MasterMaxHueShift` | Maximum generated hue shift. |
| `MasterMaxSaturationShift` | Maximum generated saturation shift. |
| `MasterMaxLuminanceShift` | Maximum generated luminance shift. |
| `MasterSceneSimilarityDampening` | Enables scene similarity dampening. |
| `MasterStyleTuningPreset` | Subtle, Balanced, Strong, Cinematic, AggressiveGpose, or Custom. |

Effective strength is reported through `MasterStyleDiagnostics`.

Conceptually:

```text
raw strength * scene similarity multiplier * compatibility mode multiplier = effective strength
```

Compatibility modes dampen matching differently. Preserve modes keep changes subtle; GPose-oriented modes allow stronger matching.

## What Master Style Can Influence

Implemented matching can influence:

| Area | Examples |
| --- | --- |
| Tonal/lighting | Shadow lift, black point, white point, highlight recovery, contrast, midtone contrast. |
| Tonal color bias | Shadow, midtone, and highlight hue/saturation bias fields used by ReGrade+ mappings. |
| Color families | Red, orange, yellow, green, cyan, blue, purple, and magenta family adjustments. |

Final shader output still depends on whether the selected preset has supported variables for these profile values.

## Why Changes Can Be Subtle

Subtle output is expected when:

| Cause | Explanation |
| --- | --- |
| Low global strength | `MasterPresetStyleStrength` scales all master matching. |
| Scene similarity dampening | Similar current/master scenes reduce the effective delta. |
| Conservative compatibility mode | PreserveBase and GameplaySanitize dampen style matching. |
| Unsupported shader variables | The profile can change, but a preset may not expose variables Dalashade can safely write. |
| Clamp limits | Hue, saturation, luminance, and tonal changes are intentionally clamped. |

## Diagnostics

The UI shows:

| Diagnostic | Source |
| --- | --- |
| Master enabled and available state | `MasterStyleAnalysisResult`. |
| Raw and effective strength | `MasterStyleDiagnostics`. |
| Scene similarity multiplier | `MasterStyleDiagnostics`. |
| Tonal percentiles | `ImageAnalysisResult` for current and master analysis. |
| Tonal deltas | `MasterStyleDiagnostics.TonalDeltas`. |
| Tonal color bias deltas | `MasterStyleDiagnostics.TonalColorDeltas`. |
| Color-family comparison rows | `ColorFamilyComparisonRows.Build(...)`. |

Compatibility reports also include master style diagnostics when available.

## Testing Whether Master Style Is Doing Anything

1. Enable screenshot analysis and master style matching.
2. Confirm the master folder has supported image files.
3. Generate a preset.
4. Check the Master Style section in the main window.
5. Check Applied Rules for lines beginning with master style behavior.
6. Check Changed Variables to confirm supported shader variables were written.
7. Export a compatibility report and inspect the master style diagnostics section.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Enabled but no master images | Verify `MasterPresetImageFolderPath` and supported image files. |
| Effective strength near zero | Check raw strength, compatibility mode, and scene similarity dampening. |
| Current and master look similar | The matcher may correctly produce small deltas. |
| Variables changed but image barely changes | The active shader may have weak visual response or ReShade may not have reloaded. |
| Strong reference but weak output | Check supported shader mappings and compatibility mode dampening. |

## Planned / Future

This system is planned and not currently implemented. Do not treat this document as an implementation reference yet.

A future version may add more explicit scene intent and deeper style matching controls, but no custom Dalashade `.fx` shader or ReShade bridge is currently documented here as implemented.
