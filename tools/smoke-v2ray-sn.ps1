param (
    [Parameter()]
    [ValidateRange(1, 65535)]
    [int]
    $Port = 28995,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $V2RayPath = 'Storage\v2ray-sn.exe'
)

$ErrorActionPreference = 'Stop'

Push-Location (Split-Path $PSScriptRoot -Parent)

try {
    if (-not (Test-Path $V2RayPath)) {
        throw "$V2RayPath not found. Run tools\install-v2ray-sn.ps1 first."
    }

    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ("netch-v2ray-sn-smoke-" + [Guid]::NewGuid())
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    $configPath = Join-Path $tempDir 'config.json'
    $outPath = Join-Path $tempDir 'stdout.log'
    $errPath = Join-Path $tempDir 'stderr.log'

    $config = @"
{
  "log": { "loglevel": "warning" },
  "inbounds": [
    {
      "listen": "127.0.0.1",
      "port": $Port,
      "protocol": "socks",
      "settings": { "udp": true }
    }
  ],
  "outbounds": [
    { "protocol": "freedom" }
  ]
}
"@

    [IO.File]::WriteAllText($configPath, $config, [Text.UTF8Encoding]::new($false))

    $process = Start-Process `
        -FilePath $V2RayPath `
        -ArgumentList @('run', '-c', $configPath) `
        -WorkingDirectory (Split-Path (Resolve-Path $V2RayPath).Path -Parent) `
        -RedirectStandardOutput $outPath `
        -RedirectStandardError $errPath `
        -WindowStyle Hidden `
        -PassThru

    try {
        $connected = $false
        for ($i = 0; $i -lt 20 -and -not $connected; $i++) {
            Start-Sleep -Milliseconds 500

            if ($process.HasExited) {
                throw "v2ray-sn exited early with code $($process.ExitCode)."
            }

            $client = [Net.Sockets.TcpClient]::new()
            try {
                $connect = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
                if ($connect.AsyncWaitHandle.WaitOne(1000)) {
                    $client.EndConnect($connect)
                    $connected = $true
                }
            } catch {
                $connected = $false
            } finally {
                $client.Dispose()
            }
        }

        if (-not $connected) {
            throw "socks inbound did not accept TCP connections on 127.0.0.1:$Port."
        }

        Write-Host "v2ray-sn smoke test passed on 127.0.0.1:$Port"
    } finally {
        if ($process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            $process.WaitForExit()
        }

        Get-Content -Path $outPath, $errPath -ErrorAction SilentlyContinue
    }
} finally {
    if ($tempDir -and (Test-Path $tempDir)) {
        Remove-Item -Path $tempDir -Recurse -Force
    }

    Pop-Location
}
