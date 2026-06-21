# Diagnostics Model Plan

`CompatibilityReportExporter.cs` and `DebugBundleExporter.cs` currently describe many of the same features independently. This pass does not refactor them; it records the low-risk model seams to extract later while keeping current report and bundle output stable.

## Repeated Concepts

| Concept | Current duplication | Proposed shared model |
| --- | --- | --- |
| Safety boundary summary | Compatibility report safety notes, debug bundle health/manifest files, Dalapad sections. | `DiagnosticSafetyBoundary` with booleans and plain-language evidence for base-preset immutability, generated-only writes, debug shader manual state, Dalapad optionality, and third-party non-mutation. |
| Performance summary | Performance tier report text, generated variables, debug bundle performance summary. | `DiagnosticPerformanceSummary` with selected tier, quality-preserving flag, scaled helper budgets, Dalapad production/debug cost rows, and known expensive shaders. |
| Generated variable summary | Changed variable list, shader support scan, generated bundle JSON. | `DiagnosticGeneratedVariableSummary` keyed by section and shader family, with expected keys, present keys, written keys, skipped reasons, and default-neutral state. |
| Dalapad status | `DalapadDiagnostics`, compatibility report text, debug bundle JSON, UI panel. | `DiagnosticDalapadStatus` split into health/status-file IPC, control-pipe IPC, semantic pinned resources, debug visualization, production assist gates, and developer-only copy/scan cost. |
| Material evidence/calibration | Screenshot evidence panel, material calibration report, debug bundle material JSON. | `DiagnosticMaterialEvidenceSummary` with SceneTags, MaterialProfile, ScreenshotMaterialEvidence, MaterialIntent, shader MaterialMasks guidance, and mismatch severity. |
| Shader support | Preset analysis, custom shader support scan, feature cards, report sections. | `DiagnosticShaderSupportSummary` with file found, preset section present, technique activation, known keys, written keys, sync eligibility, and debug/manual-only flags. |
| Technique sync/load order | Preset writer result, report warning text, debug bundle manifest. | `DiagnosticTechniqueSyncSummary` with production allow-list result, debug shader exclusion result, inserted/removed/sorted entries, and base-preset untouched proof. |
| First-party assist state | User Mode cards, custom shader diagnostics, performance summary, Dalapad and NormalField sections. | `DiagnosticFirstPartyAssistState` with global first-party gate, per-shader write gate, MaterialIntent gate, NormalField gate, Dalapad gate, and performance tier. |

## Proposed Shape

The safest extraction path is read-only first:

1. Add DTOs under a diagnostics namespace without changing exporter output.
2. Build DTOs from the same inputs currently passed to both exporters.
3. Add tests that compare old report/bundle critical fields with DTO-rendered fields.
4. Switch compatibility report sections one at a time to render from DTOs.
5. Switch debug bundle JSON writers to serialize the same DTOs or a stable projection.

## Output Stability Rules

- Keep existing filenames and top-level JSON fields stable until a release notes entry explicitly announces a schema change.
- Keep Markdown headings stable where support workflows depend on them.
- Do not merge developer-only Dalapad debug/copy cost with production shader assist cost.
- Do not convert material evidence wording into material truth wording.
- Do not add live ReShade state claims to report sections that only inspect generated preset text.

## First Extraction Candidate

`DiagnosticFirstPartyAssistState` is the lowest-risk starting point. It can unify:

- User Mode feature cards.
- Compatibility report first-party shader support summaries.
- Debug bundle generated-variable/performance summaries.
- P1 shader-uniform parity validation.

It should not own shader formulas, generated values, or preset writing. It should only describe whether each assist path is configured, written, detected, and expected to be neutral.

`FirstPartyShaderRegistry` now supplies the first read-only metadata seam for this extraction. Future diagnostic DTOs should consume the registry for shader family names, sections, techniques, manual-debug flags, known generated uniforms, and performance-tier uniforms instead of duplicating those lists in exporters.
