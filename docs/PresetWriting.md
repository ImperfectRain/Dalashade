# Preset Writing

This page documents how Dalashade writes generated ReShade presets.

## Implemented

Preset output is owned by `Dalashade/PresetWriter.cs`.

Important types:

| Type | Purpose |
| --- | --- |
| `PresetWriter` | Reads a base preset, applies mapped shader variable changes, writes the generated preset, and records changed variables. |
| `PresetWriteResult` | Reports success, message, changed variable count, normal changes, and sanitize actions. |
| `ChangedShaderVariable` | Captures section, key, old value, new value, activity state, clamp state, and reason. |

## Generated Preset Path Behavior

`Plugin.InitializeConfigDefaults()` sets default folders when paths are empty:

| Config value | Default |
| --- | --- |
| `BasePresetFolderPath` | `<plugin config>/Base` |
| `GeneratedPresetPath` | `<plugin config>/Generated/Dalashade_Generated.ini` |

The effective base preset path is resolved by `Plugin.ResolveEffectiveBasePresetPath(bool updateConfiguration)`. This supports both manual `BasePresetPath` mode and the base preset library dropdown.

## Base Preset Preservation

The generated preset should not overwrite the base preset.

`PresetWriter.WriteGeneratedPreset` rejects writes when the generated path and base path resolve to the same full path. This protects the user's original preset from being modified by adaptive generation.

## Section And Key Editing Strategy

`PresetWriter` reads the base preset line by line and tracks the current INI section. For each `key=value` line, it asks `ShaderVariableMapper` whether that section/key can be changed.

The writer changes only variable values. It does not reorder sections, disable techniques, edit texture names, or rewrite the whole preset structure.

## Shader Activation State

`PresetAnalyzer` classifies each section as:

| State | Meaning |
| --- | --- |
| `Active` | The technique is confirmed active from the preset's `Techniques=` line. |
| `Inactive` | The technique is known and not active. |
| `Unknown` | The preset did not provide enough technique information. |

`PresetWriter.ShouldWriteSection` treats unknown state conservatively. Unknown sections are not called active in reports, and exact-section writes are allowed only when the inactive shader write mode permits them.

## Inactive Shader Write Modes

`Configuration.InactiveShaderWriteMode` controls how far generation may go when a shader section is not active.

| Mode | Behavior |
| --- | --- |
| `Never` | Do not write inactive or unknown sections. |
| `SupportedInactiveSections` | Allow known supported inactive sections. |
| `AlwaysMatchingKeys` | Allow broader matching when the mapper finds a supported key. |

Use conservative modes when testing unfamiliar presets.

## Backups

If `Configuration.WriteBackups` is true and the generated preset already exists, the writer copies the existing generated preset to a timestamped backup before replacement.

Backup pruning is controlled by `Configuration.MaxGeneratedPresetBackups`.

## Safe Write Behavior

`PresetWriter` writes to a temporary path first and then replaces the generated preset. This reduces the chance of leaving a partial generated preset if a write fails.

Common write failures:

| Failure | Likely cause |
| --- | --- |
| Access denied | Generated path is inside a protected folder or the file is locked. |
| Base preset missing | `BasePresetPath` or selected dropdown preset points to a missing file. |
| Same base/generated path | The generated preset path was set to the base preset path. |

## Clamp Behavior

Mappings define safe output ranges. When a calculated value exceeds the allowed range, the mapper clamps it and records the clamp in `ChangedShaderVariable`.

Clamp warnings are visible in the UI and exported compatibility reports.

## Changed Variable Reporting

Changed variables are shown in:

| Location | Implemented file |
| --- | --- |
| Main window | `Dalashade/Windows/MainWindow.cs` |
| Compatibility report export | `Dalashade/CompatibilityReportExporter.cs` |
| Regression reports | `Dalashade/PresetRegressionReportHarness.cs` |

Use changed variable output to verify whether a visual change was actually written.

## Troubleshooting Bad Generated Output

| Symptom | First checks |
| --- | --- |
| Output looks wrong | Inspect changed variables, clamped variables, compatibility risk, and active authorities. |
| Output is too dark | Check black point, white point, exposure, contrast, and master style diagnostics. |
| No visible change | Check changed variable count, active preset in ReShade, and reload diagnostics. |
| Too many changes | Check shader matching mode and compatibility mode. Avoid loose key matching for broad presets. |
| Preset was overwritten | Verify generated path is not the same as base path. |

Do not fix generated output by broadly rewriting `PresetWriter` first. Most visual behavior belongs in `ShaderVariableMapper`, `VisualProfile`, compatibility policy, or master style code.
