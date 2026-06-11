# Release Checklist

This page documents the current release process and common release risks.

## Implemented Release Files

| File | Purpose |
| --- | --- |
| `Dalashade/Dalashade.csproj` | Project metadata and assembly version. |
| `Dalashade/Dalashade.json` | Dalamud plugin manifest embedded with the plugin. |
| `repo.json` | Custom repository manifest for Dalamud browser install/update. |
| `releases/` | Local release zip storage. |
| `.github/workflows/pr-build.yml` | CI build and manifest validation. |

## Release Steps

1. Update the project version in `Dalashade/Dalashade.csproj`.
2. Update `Dalashade/Dalashade.json` if manifest metadata changed.
3. Build or publish the plugin in Release configuration.
4. Create the release zip.
5. Confirm the zip contains the plugin DLL and required plugin files.
6. Update `repo.json`.
7. Confirm `repo.json` version, changelog, and download URLs match the release.
8. Run `scripts/ValidateRelease.ps1` to validate the manifest and release downloads.
9. Validate the raw manifest URL.
10. Validate the release zip URL.
11. Test install/update from the custom repository in Dalamud.
12. Commit and push release metadata only when the release artifact is actually available.

## Build Commands

Use:

```powershell
dotnet build -c Release
```

Validate the release downloads declared by `repo.json`:

```powershell
.\scripts\ValidateRelease.ps1
```

If local PowerShell policy blocks script execution, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\ValidateRelease.ps1
```

Use the existing project/package workflow for creating the release zip. Do not change release packaging during unrelated feature or documentation tasks.

## Manifest Checks

For `repo.json`, verify:

| Field | Check |
| --- | --- |
| `AssemblyVersion` | Matches the intended release version. |
| `DownloadLinkInstall` | Points to a real zip that contains `Dalashade.dll`. |
| `DownloadLinkUpdate` | Points to a real zip that contains `Dalashade.dll`. |
| `DownloadLinkTesting` | Points to a real zip that contains `Dalashade.dll`. |
| `Changelog` | Describes the current release, not an older one. |

## Zip Checks

Confirm the release zip contains at least:

| Required item | Reason |
| --- | --- |
| `Dalashade.dll` | Main plugin assembly. |
| `Dalashade.json` | Dalamud plugin manifest. |

CI validates the download links declared in `repo.json`; it does not rely on a hardcoded local release zip name. If `repo.json` points to a GitHub Release asset, that asset must already exist and be downloadable.

## Common Mistakes

| Mistake | Result |
| --- | --- |
| Manifest points to a missing zip | Dalamud install/update fails. |
| Changelog says old version | Users cannot tell what changed. |
| Project version and manifest version mismatch | Confusing update behavior. |
| Zip missing DLL | Plugin cannot load. |
| Release task changes unrelated runtime code | Harder to diagnose release issues. |

## Existing Validation

CI currently exists in `.github/workflows/pr-build.yml`.

It validates:

| Check | Purpose |
| --- | --- |
| `dotnet restore` | Restore project dependencies. |
| `dotnet build -c Release` | Build validation. |
| `scripts/ValidateRelease.ps1` | Parses `repo.json`, downloads each manifest release URL, verifies each file is a zip, verifies `Dalashade.dll` exists, and checks the DLL assembly version against `AssemblyVersion`. |

## TODO

Potential future release checks:

1. Project version matches `repo.json` version.
2. Changelog version text matches the release version.
3. Release zip contains optional shipped assets such as custom shader files if the release task requires them.

Do not update `repo.json` unless the task is explicitly a release or install/update task.
