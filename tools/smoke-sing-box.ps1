param (
    [Parameter()]
    [ValidateRange(1, 65535)]
    [int]
    $Port = 28991,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $SingBoxPath = 'Storage\sing-box.exe'
)

$ErrorActionPreference = 'Stop'

function Quote-ProcessArgument([string] $Value) {
    '"' + $Value.Replace('\', '\\').Replace('"', '\"') + '"'
}

Push-Location (Split-Path $PSScriptRoot -Parent)

try {
    if (-not (Test-Path $SingBoxPath)) {
        throw "$SingBoxPath not found. Run tools\install-sing-box.ps1 first."
    }

    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ("netch-sing-box-smoke-" + [Guid]::NewGuid())
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    $configPath = Join-Path $tempDir 'config.json'
    $outPath = Join-Path $tempDir 'stdout.log'
    $errPath = Join-Path $tempDir 'stderr.log'

    $config = @"
{
  "log": { "level": "info", "timestamp": true },
  "inbounds": [
    { "type": "mixed", "tag": "mixed-in", "listen": "127.0.0.1", "listen_port": $Port }
  ],
  "outbounds": [
    {
      "type": "vless",
      "tag": "proxy",
      "server": "reality.example.com",
      "server_port": 443,
      "uuid": "22222222-2222-2222-2222-222222222222",
      "flow": "xtls-rprx-vision",
      "packet_encoding": "xudp",
      "network": "tcp",
      "tls": {
        "enabled": true,
        "server_name": "www.example.com",
        "insecure": false,
        "utls": { "enabled": true, "fingerprint": "chrome" },
        "reality": { "enabled": true, "public_key": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "short_id": "0123456789abcdef" }
      }
    }
  ]
}
"@

    [IO.File]::WriteAllText($configPath, $config, [Text.UTF8Encoding]::new($false))

    & $SingBoxPath check -c $configPath
    if ($LASTEXITCODE -ne 0) {
        throw "sing-box check failed with exit code $LASTEXITCODE."
    }

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = (Resolve-Path $SingBoxPath).Path
    $startInfo.WorkingDirectory = (Get-Location).Path
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Arguments = 'run -c ' + (Quote-ProcessArgument $configPath)

    # Some Windows shells expose both Path and PATH. ProcessStartInfo treats
    # environment keys case-insensitively, so normalize before starting.
    $environment = $startInfo.Environment
    if ($null -eq $environment) {
        $environment = $startInfo.EnvironmentVariables
    }

    foreach ($key in @($environment.Keys)) {
        if ($key -ne 'Path' -and $key.Equals('Path', [StringComparison]::OrdinalIgnoreCase)) {
            $environment.Remove($key) | Out-Null
        }
    }

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $process.Start() | Out-Null

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()

    try {
        $connected = $false
        for ($i = 0; $i -lt 20 -and -not $connected; $i++) {
            Start-Sleep -Milliseconds 500

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
            throw "mixed inbound did not accept TCP connections on 127.0.0.1:$Port."
        }

        Write-Host "sing-box smoke test passed on 127.0.0.1:$Port"
    } finally {
        if ($process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            $process.WaitForExit()
        }

        [IO.File]::WriteAllText($outPath, $stdoutTask.GetAwaiter().GetResult())
        [IO.File]::WriteAllText($errPath, $stderrTask.GetAwaiter().GetResult())
        Get-Content -Path $outPath, $errPath -ErrorAction SilentlyContinue
    }
} finally {
    if ($tempDir -and (Test-Path $tempDir)) {
        Remove-Item -Path $tempDir -Recurse -Force
    }

    Pop-Location
}
