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
8. Validate the raw manifest URL.
9. Validate the release zip URL.
10. Test install/update from the custom repository in Dalamud.
11. Commit and push release metadata only when the release artifact is actually available.

## Build Commands

Use:

```powershell
dotnet build -c Release
```

Use the existing project/package workflow for creating the release zip. Do not change release packaging during unrelated feature or documentation tasks.

## Manifest Checks

For `repo.json`, verify:

| Field | Check |
| --- | --- |
| `AssemblyVersion` | Matches the intended release version. |
| `DownloadLinkInstall` | Points to a real zip. |
| `DownloadLinkUpdate` | Points to a real zip. |
| `DownloadLinkTesting` | Points to a real zip if used. |
| `Changelog` | Describes the current release, not an older one. |

## Zip Checks

Confirm the release zip contains at least:

| Required item | Reason |
| --- | --- |
| `Dalashade.dll` | Main plugin assembly. |
| `Dalashade.json` | Dalamud plugin manifest. |

The CI workflow may also verify `releases/Dalashade-v1.1.zip` if present. Keep CI validation in sync with the release artifacts that actually matter.

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
| `repo.json` parse | Ensures the custom repo manifest is valid JSON. |
| Optional zip check | Verifies a release zip contains `Dalashade.dll` if the expected zip is present. |

## TODO

Add a dedicated release validation script that checks:

1. Project version.
2. `repo.json` version.
3. Changelog version text.
4. Download URLs.
5. Zip contents.
6. GitHub release availability.

Do not update `repo.json` unless the task is explicitly a release or install/update task.
