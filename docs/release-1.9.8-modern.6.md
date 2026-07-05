# Netch 1.9.8-modern.6 Release Record

Date: 2026-07-05

This release fixes a UI freeze when running HTTP connectivity test from the speed/test icon after startup.

## Release Goal

Keep the main window responsive while testing real HTTP connectivity.

## Version

- Product version: `1.9.8-modern.6`
- Assembly version: `1.9.8`
- Release type: prerelease / modern-core preview

## Included Changes Since modern.5

- Running-state speed/test icon no longer disables the entire main window.
- Running-state speed/test icon updates the bottom HTTP status label instead of opening a blocking message box.
- HTTP connectivity test now has a hard UI-side timeout and displays `HTTP: Timeout` when the underlying socks test does not return quickly.
- Waiting-state delay test behavior is unchanged.

## Validation Performed

- `dotnet test .\Tests\Tests.csproj --no-restore`
- `tools\smoke-sing-box.ps1 -SingBoxPath .\artifacts\netch-1.9.8-modern.6\bin\sing-box.exe -Port 29062`
- `tools\smoke-v2ray-sn.ps1 -V2RayPath .\artifacts\netch-1.9.8-modern.6\bin\v2ray-sn.exe -Port 29061`

## Local Preview Artifact

- Package: `artifacts\Netch-1.9.8-modern.6.zip`
- Size: `105525510` bytes
- SHA256: `227344b432833818c593a67a97939a71fc40861a5bf185df51910211884b3eb9`
