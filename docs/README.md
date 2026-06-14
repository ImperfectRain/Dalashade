# Dalashade Documentation

Dalashade is a Dalamud plugin that generates a separate ReShade preset from a selected base preset. It reads current game context, optional screenshot analysis, optional master-style references, and compatibility data, then writes adjusted values only for known shader variables that already exist in the preset.

These docs are for users who want to understand what Dalashade is doing, maintainers editing the plugin, and future Codex runs that need to find the correct code before changing it.

## Recommended Reading Order

1. [CodebaseIndex.md](CodebaseIndex.md)
2. [GenerationPipeline.md](GenerationPipeline.md)
3. The topic page for the change being made
4. [CodexEditingGuide.md](CodexEditingGuide.md)

## Quick Links

| Topic | Doc |
| --- | --- |
| Code map and ownership | [CodebaseIndex.md](CodebaseIndex.md) |
| Generate button to written preset flow | [GenerationPipeline.md](GenerationPipeline.md) |
| Territory, weather, time, combat tags | [SceneTagsAndIntent.md](SceneTagsAndIntent.md) |
| Generated preset write behavior | [PresetWriting.md](PresetWriting.md) |
| Shader support and mappings | [ShaderMapping.md](ShaderMapping.md) |
| Custom shader authoring scaffold | [ShaderAuthoring.md](ShaderAuthoring.md) |
| Optional NormalField diagnostics and test plan | [NormalField.md](NormalField.md) |
| Master style matching | [MasterStyle.md](MasterStyle.md) |
| Compatibility reports and diagnostics | [CompatibilityAndDiagnostics.md](CompatibilityAndDiagnostics.md) |
| ReShade reload behavior | [ReShadeReload.md](ReShadeReload.md) |
| Release checklist | [ReleaseChecklist.md](ReleaseChecklist.md) |
| Codex editing rules | [CodexEditingGuide.md](CodexEditingGuide.md) |

## For Codex Agents

Before editing code, read:

1. `docs/CodebaseIndex.md`
2. `docs/GenerationPipeline.md`
3. The specific topic doc for the requested change
4. `docs/CodexEditingGuide.md`

Do not treat planned-roadmap docs as implementation references. Planned systems are labeled as planned and should not be implemented unless the user explicitly asks for that work.
