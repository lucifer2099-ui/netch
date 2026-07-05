param (
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $Version = 'v5.0.16',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $OutputPath = 'Storage\v2ray-sn.exe',

    [Parameter()]
    [string]
    $DownloadUrl,

    [Parameter()]
    [string]
    $Proxy = $env:HTTPS_PROXY
)

$ErrorActionPreference = 'Stop'

Push-Location (Split-Path $PSScriptRoot -Parent)

try {
    $webRequestOptions = @{
        Headers = @{
            'User-Agent' = 'Netch v2ray-sn installer'
        }
    }

    if (![string]::IsNullOrWhiteSpace($Proxy)) {
        $webRequestOptions.Proxy = $Proxy
        Write-Host "Using proxy $Proxy"
    }

    if ([string]::IsNullOrWhiteSpace($DownloadUrl)) {
        $releaseUrl = "https://api.github.com/repos/SagerNet/v2ray-core/releases/tags/$Version"

        Write-Host "Fetching SagerNet v2ray-core release metadata: $Version"
        $release = Invoke-RestMethod -Uri $releaseUrl @webRequestOptions

        $asset = $release.assets |
            Where-Object { $_.name -eq 'v2ray-windows-64.zip' } |
            Select-Object -First 1

        if ($null -eq $asset) {
            throw 'Could not find v2ray-windows-64.zip in the SagerNet release assets.'
        }

        $download = $asset.browser_download_url
        $assetName = $asset.name
        $versionName = $release.tag_name
    } else {
        $download = $DownloadUrl
        $assetName = Split-Path $DownloadUrl -Leaf
        $versionName = $Version
    }

    $tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("netch-v2ray-sn-" + [Guid]::NewGuid())
    $zipPath = Join-Path $tempRoot $assetName
    $extractPath = Join-Path $tempRoot 'extract'

    New-Item -ItemType Directory -Path $tempRoot, $extractPath | Out-Null

    Write-Host "Downloading $assetName"
    try {
        Invoke-WebRequest -Uri $download -OutFile $zipPath @webRequestOptions
    } catch {
        Write-Warning "Invoke-WebRequest failed: $($_.Exception.Message). Retrying with curl.exe."
        $curlArgs = @('-L', '--retry', '3', '--retry-delay', '2', '--fail', '-A', 'Netch v2ray-sn installer', '-o', $zipPath)
        if (![string]::IsNullOrWhiteSpace($Proxy)) {
            $curlArgs += @('-x', $Proxy)
        }

        $curlArgs += $download
        curl.exe @curlArgs
        if ($LASTEXITCODE -ne 0) {
            throw "curl.exe failed with exit code $LASTEXITCODE."
        }
    }

    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

    $exe = Get-ChildItem -Path $extractPath -Recurse -Filter 'v2ray.exe' |
        Select-Object -First 1

    if ($null -eq $exe) {
        throw 'Downloaded archive did not contain v2ray.exe.'
    }

    $resolvedOutputPath = Join-Path (Get-Location) $OutputPath
    $resolvedOutputDirectory = Split-Path $resolvedOutputPath -Parent
    New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null
    Copy-Item -Path $exe.FullName -Destination $resolvedOutputPath -Force

    foreach ($assetFileName in @('geoip.dat', 'geosite.dat')) {
        $assetFile = Get-ChildItem -Path $extractPath -Recurse -Filter $assetFileName |
            Select-Object -First 1

        if ($null -ne $assetFile) {
            Copy-Item -Path $assetFile.FullName -Destination (Join-Path $resolvedOutputDirectory $assetFileName) -Force
        }
    }

    $manifestPath = Join-Path $resolvedOutputDirectory 'v2ray-sn.manifest.json'
    $manifest = [ordered]@{
        name = 'v2ray-sn'
        version = $versionName
        source = $download
        file = $OutputPath
        sha256 = (Get-FileHash -Algorithm SHA256 -Path $resolvedOutputPath).Hash.ToLowerInvariant()
        installed_at = (Get-Date).ToUniversalTime().ToString('o')
    }

    $manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

    Write-Host "Installed v2ray-sn $versionName to $OutputPath"
    Write-Host "Wrote manifest to $manifestPath"
} finally {
    if ($tempRoot -and (Test-Path $tempRoot)) {
        Remove-Item -Path $tempRoot -Recurse -Force
    }

    Pop-Location
}
