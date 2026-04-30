param (
    [Parameter(Mandatory=$false)][string]$repository,
    [Parameter(Mandatory=$true)][string]$tag
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if( -not $repository )
{
    $repository="https://api.github.com/repos/ImGuiNET/ImGui.NET-nativebuild"
}

Write-Host Downloading native binaries from GitHub Releases...

if (Test-Path $PSScriptRoot\deps\cimgui\)
{
    Remove-Item $PSScriptRoot\deps\cimgui\ -Force -Recurse | Out-Null
}
New-Item -ItemType Directory -Force -Path $PSScriptRoot\deps\cimgui\linux-x64 | Out-Null
New-Item -ItemType Directory -Force -Path $PSScriptRoot\deps\cimgui\osx | Out-Null
New-Item -ItemType Directory -Force -Path $PSScriptRoot\deps\cimgui\win-x86 | Out-Null
New-Item -ItemType Directory -Force -Path $PSScriptRoot\deps\cimgui\win-x64 | Out-Null
New-Item -ItemType Directory -Force -Path $PSScriptRoot\deps\cimgui\win-arm64 | Out-Null

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

if ($repository -match "^https://api\.github\.com/repos/")
{
    $releaseRepositoryUrl = $repository -replace "^https://api\.github\.com/repos/", "https://github.com/"
}
else
{
    $releaseRepositoryUrl = $repository.TrimEnd("/")
    $repository = $releaseRepositoryUrl -replace "^https://github\.com/", "https://api.github.com/repos/"
}

$headers = @{
    "Accept" = "application/vnd.github+json"
    "User-Agent" = "ImGui.NET-download-native-deps"
}

function Get-StatusCode {
    param([System.Exception]$Exception)

    if ($null -eq $Exception.Response)
    {
        return $null
    }

    if ($Exception.Response -is [System.Net.HttpWebResponse])
    {
        return [int]$Exception.Response.StatusCode
    }

    if ($null -ne $Exception.Response.StatusCode)
    {
        return [int]$Exception.Response.StatusCode
    }

    return $null
}

function Get-Release {
    param(
        [string]$RepositoryApiUrl,
        [string]$ReleaseTag
    )

    try
    {
        return Invoke-RestMethod -Headers $headers -Uri "$RepositoryApiUrl/releases/tags/$ReleaseTag"
    }
    catch
    {
        $statusCode = Get-StatusCode $_.Exception
        if ($statusCode -eq 404)
        {
            Write-Error @"
Couldn't find release '$ReleaseTag' in $releaseRepositoryUrl.

This repository no longer publishes current native binaries through the legacy ImGui.NET-nativebuild releases.
For current versions, native cimgui binaries are built in CI and packed directly from this repository.
Use CI-produced artifacts or build cimgui locally using the steps in .github/workflows/build.yml.
"@
            exit 1
        }

        throw
    }
}

function Download-ReleaseAsset {
    param(
        [hashtable]$AssetUrlsByName,
        [string]$AssetName,
        [string]$DestinationPath,
        [string]$MissingAssetMessage
    )

    if (-not $AssetUrlsByName.ContainsKey($AssetName))
    {
        Write-Error $MissingAssetMessage
        exit 1
    }

    $assetUrl = $AssetUrlsByName[$AssetName]
    $attempt = 0
    while ($attempt -lt 3)
    {
        $attempt++
        try
        {
            Invoke-WebRequest -Headers $headers -Uri $assetUrl -OutFile $DestinationPath
            return
        }
        catch
        {
            if ($attempt -ge 3)
            {
                throw
            }
            Start-Sleep -Seconds (2 * $attempt)
        }
    }
}

$release = Get-Release -RepositoryApiUrl $repository -ReleaseTag $tag
$assetUrlsByName = @{}
foreach ($asset in $release.assets)
{
    $assetUrlsByName[$asset.name] = $asset.browser_download_url
}

Download-ReleaseAsset `
    -AssetUrlsByName $assetUrlsByName `
    -AssetName "cimgui.win-x86.dll" `
    -DestinationPath "$PSScriptRoot/deps/cimgui/win-x86/cimgui.dll" `
    -MissingAssetMessage "Couldn't find x86 cimgui.dll in release '$tag'. This most likely indicates the Windows native build failed."

Write-Host "- cimgui.dll (x86)"

Download-ReleaseAsset `
    -AssetUrlsByName $assetUrlsByName `
    -AssetName "cimgui.win-x64.dll" `
    -DestinationPath "$PSScriptRoot/deps/cimgui/win-x64/cimgui.dll" `
    -MissingAssetMessage "Couldn't find x64 cimgui.dll in release '$tag'. This most likely indicates the Windows native build failed."

Write-Host "- cimgui.dll (x64)"

Download-ReleaseAsset `
    -AssetUrlsByName $assetUrlsByName `
    -AssetName "cimgui.win-arm64.dll" `
    -DestinationPath "$PSScriptRoot/deps/cimgui/win-arm64/cimgui.dll" `
    -MissingAssetMessage "Couldn't find arm64 cimgui.dll in release '$tag'. This most likely indicates the Windows native build failed."

Write-Host "- cimgui.dll (arm64)"

Download-ReleaseAsset `
    -AssetUrlsByName $assetUrlsByName `
    -AssetName "cimgui.so" `
    -DestinationPath "$PSScriptRoot/deps/cimgui/linux-x64/cimgui.so" `
    -MissingAssetMessage "Couldn't find cimgui.so in release '$tag'. This most likely indicates the Linux native build failed."

Write-Host - cimgui.so

Download-ReleaseAsset `
    -AssetUrlsByName $assetUrlsByName `
    -AssetName "cimgui.dylib" `
    -DestinationPath "$PSScriptRoot/deps/cimgui/osx/cimgui.dylib" `
    -MissingAssetMessage "Couldn't find cimgui.dylib in release '$tag'. This most likely indicates the macOS native build failed."

Write-Host "- cimgui.dylib"

Download-ReleaseAsset `
    -AssetUrlsByName $assetUrlsByName `
    -AssetName "definitions.json" `
    -DestinationPath "$PSScriptRoot/src/CodeGenerator/definitions/cimgui/definitions.json" `
    -MissingAssetMessage "Couldn't find definitions.json in release '$tag'."

Write-Host - definitions.json

Download-ReleaseAsset `
    -AssetUrlsByName $assetUrlsByName `
    -AssetName "structs_and_enums.json" `
    -DestinationPath "$PSScriptRoot/src/CodeGenerator/definitions/cimgui/structs_and_enums.json" `
    -MissingAssetMessage "Couldn't find structs_and_enums.json in release '$tag'."

Write-Host - structs_and_enums.json
