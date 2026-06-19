# External Headers

This folder contains public ReShade SDK headers used to build the experimental Dalapad addon prototype.

Current source:

```text
https://github.com/crosire/reshade
```

Only the local test addon uses these headers. They are not referenced by `Dalashade.sln`, not loaded by the Dalamud plugin, and not required for normal Dalashade builds.

The vendored headers are included so a new Codex session can rebuild `DalapadAddon/build/Dalapad.addon64` without rediscovering the SDK layout. Keep upstream license headers intact when updating them.
