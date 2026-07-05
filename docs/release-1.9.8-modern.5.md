# Netch 1.9.8-modern.5 Release Record

Date: 2026-07-05

This release fixes the HTTP connectivity test entry point after startup.

## Release Goal

Make the real HTTP connectivity test usable while a server is already running.

## Version

- Product version: `1.9.8-modern.5`
- Assembly version: `1.9.8`
- Release type: prerelease / modern-core preview

## Included Changes Since modern.4

- Keeps the Server menu entry for `HTTP Connectivity Test`.
- Adds a running-state shortcut: when Netch is already started, clicking the speed/test icon runs the HTTP connectivity test instead of only doing TCP ping.
- Keeps the original waiting-state behavior: clicking the speed/test icon still runs delay testing, and Ctrl+Click still runs a single selected-node ping.

## Validation Performed

- `dotnet test .\Tests\Tests.csproj --no-restore`
- `tools\smoke-sing-box.ps1 -SingBoxPath .\artifacts\netch-1.9.8-modern.5\bin\sing-box.exe -Port 29052`
- `tools\smoke-v2ray-sn.ps1 -V2RayPath .\artifacts\netch-1.9.8-modern.5\bin\v2ray-sn.exe -Port 29051`

## Local Preview Artifact

- Package: `artifacts\Netch-1.9.8-modern.5.zip`
- Size: `105525516` bytes
- SHA256: `a834dc5d6a8e842779f99120dab6a8004513f3017cf1e253bd854a1564158ee6`
