# Netch 1.9.8-modern.3 Release Record

Date: 2026-07-05

This release fixes a sing-box startup timeout that was actually caused by a local port bind failure.

## Release Goal

Make sing-box startup failures actionable and avoid failing when the configured local SOCKS port is blocked by Windows or endpoint security software.

## Version

- Product version: `1.9.8-modern.3`
- Assembly version: `1.9.8`
- Release type: prerelease / modern-core preview

## Included Changes Since modern.2

- Detects `FATAL`, `ERROR`, and other sing-box failure keywords case-insensitively.
- Logs the last failed startup line so the real sing-box error is visible instead of only reporting timeout.
- Checks whether the configured local SOCKS port can actually be bound before generating the sing-box config.
- Falls back to the next available local port within a nearby range when the preferred port is unavailable.
- Returns the actual fallback port to Netch mode controllers.

## Diagnosed Cause

The observed startup timeout was caused by:

```text
listen tcp 127.0.0.1:2801: bind: An attempt was made to access a socket in a way forbidden by its access permissions.
```

PID `19468` was `aTrustAgent`, indicating local endpoint/VPN/security software was involved around port `2801`. The process should not be force-killed by Netch.

## Validation Performed

- `dotnet test .\Tests\Tests.csproj --no-restore`
- `tools\smoke-sing-box.ps1 -SingBoxPath .\artifacts\netch-1.9.8-modern.3\bin\sing-box.exe -Port 29032`
- `tools\smoke-v2ray-sn.ps1 -V2RayPath .\artifacts\netch-1.9.8-modern.3\bin\v2ray-sn.exe -Port 29031`

## Local Preview Artifact

- Package: `artifacts\Netch-1.9.8-modern.3.zip`
- Size: `105521911` bytes
- SHA256: `5414b9bef24bd145610d54e02bea85ec6586fc53de49525198f440fc5712815b`
