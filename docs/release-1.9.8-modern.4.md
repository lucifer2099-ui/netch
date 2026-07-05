# Netch 1.9.8-modern.4 Release Record

Date: 2026-07-05

This release upgrades the modern-core preview from protocol support to day-to-day usability.

## Release Goal

Complete the first usability layer for imported modern nodes:

- Modern node editing.
- Human-readable startup diagnostics.
- Subscription import report.
- Real HTTP connectivity test entry.

## Version

- Product version: `1.9.8-modern.4`
- Assembly version: `1.9.8`
- Release type: prerelease / modern-core preview

## Included Changes Since modern.3

- Adds `ModernProxyForm` for editing imported Modern nodes.
- Modern editable fields include protocol, UUID, password/auth, flow, packet encoding, transport, host/path, TLS, SNI, fingerprint, ALPN, insecure, Reality, Hysteria2, and TUIC fields.
- Replaces the Modern util placeholder edit action with the real form.
- Adds startup diagnostic explanations for blocked local ports, occupied ports, AnyTLS ALPN failures, TLS/SNI/certificate failures, and missing required fields.
- Adds subscription update reports with imported count, warnings/skipped nodes, and failed subscription details.
- Adds `HTTP Connectivity Test` to the Server menu for checking real proxy traffic after startup.
- Rewrites Guard startup output handling with case-insensitive started/failed keyword detection and stable failure messages.

## Validation Performed

- `dotnet test .\Tests\Tests.csproj --no-restore`
- `tools\smoke-sing-box.ps1 -SingBoxPath .\artifacts\netch-1.9.8-modern.4\bin\sing-box.exe -Port 29042`
- `tools\smoke-v2ray-sn.ps1 -V2RayPath .\artifacts\netch-1.9.8-modern.4\bin\v2ray-sn.exe -Port 29041`

## Local Preview Artifact

- Package: `artifacts\Netch-1.9.8-modern.4.zip`
- Size: `105525713` bytes
- SHA256: `7729c6bc5a97637d968e76744e7dd3cbe0cf318ca85d5f8a6728c6a9e52339a5`
