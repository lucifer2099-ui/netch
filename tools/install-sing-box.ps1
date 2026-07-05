param (
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $Version = 'latest',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $OutputPath = 'Storage\sing-box.exe',

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
            'User-Agent' = 'Netch sing-box installer'
        }
    }

    if (![string]::IsNullOrWhiteSpace($Proxy)) {
        $webRequestOptions.Proxy = $Proxy
        Write-Host "Using proxy $Proxy"
    }

    if ([string]::IsNullOrWhiteSpace($DownloadUrl)) {
        $releaseUrl = if ($Version -eq 'latest') {
            'https://api.github.com/repos/SagerNet/sing-box/releases/latest'
        } else {
            "https://api.github.com/repos/SagerNet/sing-box/releases/tags/$Version"
        }

        Write-Host "Fetching sing-box release metadata: $Version"
        $release = Invoke-RestMethod -Uri $releaseUrl @webRequestOptions

        $asset = $release.assets |
            Where-Object { $_.name -match '^sing-box-.*-windows-amd64\.zip$' } |
            Select-Object -First 1

        if ($null -eq $asset) {
            throw 'Could not find a windows-amd64 sing-box release asset.'
        }

        $download = $asset.browser_download_url
        $assetName = $asset.name
        $versionName = $release.tag_name
    } else {
        $download = $DownloadUrl
        $assetName = Split-Path $DownloadUrl -Leaf
        $versionName = $Version
    }

    $tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("netch-sing-box-" + [Guid]::NewGuid())
    $zipPath = Join-Path $tempRoot $assetName
    $extractPath = Join-Path $tempRoot 'extract'

    New-Item -ItemType Directory -Path $tempRoot, $extractPath | Out-Null

    Write-Host "Downloading $assetName"
    try {
        Invoke-WebRequest -Uri $download -OutFile $zipPath @webRequestOptions
    } catch {
        Write-Warning "Invoke-WebRequest failed: $($_.Exception.Message). Retrying with curl.exe."
        $curlArgs = @('-L', '--retry', '3', '--retry-delay', '2', '--fail', '-A', 'Netch sing-box installer', '-o', $zipPath)
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

    $exe = Get-ChildItem -Path $extractPath -Recurse -Filter 'sing-box.exe' |
        Select-Object -First 1

    if ($null -eq $exe) {
        throw 'Downloaded archive did not contain sing-box.exe.'
    }

    $resolvedOutputPath = Join-Path (Get-Location) $OutputPath
    $resolvedOutputDirectory = Split-Path $resolvedOutputPath -Parent
    New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null
    Copy-Item -Path $exe.FullName -Destination $resolvedOutputPath -Force

    $manifestPath = Join-Path $resolvedOutputDirectory 'sing-box.manifest.json'
    $manifest = [ordered]@{
        name = 'sing-box'
        version = $versionName
        source = $download
        file = $OutputPath
        sha256 = (Get-FileHash -Algorithm SHA256 -Path $resolvedOutputPath).Hash.ToLowerInvariant()
        installed_at = (Get-Date).ToUniversalTime().ToString('o')
    }

    $manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

    Write-Host "Installed sing-box $versionName to $OutputPath"
    Write-Host "Wrote manifest to $manifestPath"
} finally {
    if ($tempRoot -and (Test-Path $tempRoot)) {
        Remove-Item -Path $tempRoot -Recurse -Force
    }

    Pop-Location
}
