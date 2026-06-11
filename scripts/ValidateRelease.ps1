param(
    [string] $ManifestPath = "repo.json"
)

$ErrorActionPreference = "Stop"

function Resolve-PathForDisplay {
    param([string] $Path)

    try {
        return (Resolve-Path -LiteralPath $Path).Path
    }
    catch {
        return $Path
    }
}

function Get-ManifestEntries {
    param([string] $Path)

    if (!(Test-Path -LiteralPath $Path)) {
        throw "Manifest not found: $Path"
    }

    $raw = Get-Content -Raw -LiteralPath $Path
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "Manifest is empty: $Path"
    }

    $parsed = $raw | ConvertFrom-Json
    $entries = @($parsed)
    if ($entries.Count -eq 0) {
        throw "Manifest contains no plugin entries: $Path"
    }

    return $entries
}

function Save-Download {
    param(
        [string] $Url,
        [string] $Destination
    )

    Write-Host "Downloading $Url"
    try {
        Invoke-WebRequest -Uri $Url -OutFile $Destination -MaximumRedirection 10
    }
    catch {
        throw "Download failed for $Url. $($_.Exception.Message)"
    }

    if (!(Test-Path -LiteralPath $Destination)) {
        throw "Download did not create a file for $Url"
    }

    $file = Get-Item -LiteralPath $Destination
    if ($file.Length -le 0) {
        throw "Downloaded file is empty for $Url"
    }
}

function Test-ReleaseZip {
    param(
        [string] $ZipPath,
        [string] $Url,
        [string] $ExpectedVersion,
        [string] $WorkDirectory
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    try {
        $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    }
    catch {
        throw "Downloaded file is not a valid zip for $Url. $($_.Exception.Message)"
    }

    try {
        $dllEntry = $zip.Entries |
            Where-Object { $_.FullName -match '(^|/)Dalashade\.dll$' } |
            Select-Object -First 1

        if ($null -eq $dllEntry) {
            throw "Release zip from $Url does not contain Dalashade.dll"
        }

        $extractDirectory = Join-Path $WorkDirectory ([Guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path $extractDirectory | Out-Null
        $dllPath = Join-Path $extractDirectory "Dalashade.dll"
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($dllEntry, $dllPath, $true)

        if (![string]::IsNullOrWhiteSpace($ExpectedVersion)) {
            try {
                $actualVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath).Version.ToString()
                if ($actualVersion -ne $ExpectedVersion) {
                    throw "Dalashade.dll assembly version $actualVersion does not match repo.json AssemblyVersion $ExpectedVersion for $Url"
                }

                Write-Host "Validated Dalashade.dll version $actualVersion"
            }
            catch {
                throw "Could not validate Dalashade.dll assembly version for $Url. $($_.Exception.Message)"
            }
        }
    }
    finally {
        if ($null -ne $zip) {
            $zip.Dispose()
        }
    }
}

$manifestDisplayPath = Resolve-PathForDisplay $ManifestPath
Write-Host "Validating release manifest: $manifestDisplayPath"

$entries = Get-ManifestEntries $ManifestPath
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DalashadeReleaseValidation-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

try {
    foreach ($entry in $entries) {
        $name = [string] $entry.Name
        if ([string]::IsNullOrWhiteSpace($name)) {
            $name = "<unnamed>"
        }

        $assemblyVersion = [string] $entry.AssemblyVersion
        if ([string]::IsNullOrWhiteSpace($assemblyVersion)) {
            throw "Manifest entry $name is missing AssemblyVersion."
        }

        Write-Host "Entry: $name $assemblyVersion"

        $downloadFields = @(
            "DownloadLinkInstall",
            "DownloadLinkUpdate",
            "DownloadLinkTesting"
        )

        foreach ($field in $downloadFields) {
            $url = [string] $entry.$field
            if ([string]::IsNullOrWhiteSpace($url)) {
                throw "Manifest entry $name is missing $field."
            }

            $fileName = "$($name)-$($field).zip"
            $downloadPath = Join-Path $tempRoot $fileName
            Save-Download -Url $url -Destination $downloadPath
            Test-ReleaseZip -ZipPath $downloadPath -Url $url -ExpectedVersion $assemblyVersion -WorkDirectory $tempRoot
            Write-Host "Validated $field"
        }
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "Release manifest downloads validated."
