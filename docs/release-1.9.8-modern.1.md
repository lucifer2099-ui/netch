# Netch 1.9.8-modern.1 Release Record

Date: 2026-07-04

This release is a publishable modern-core preview. It keeps the existing Netch 1.x behavior while adding a sing-box based path for current airport/node subscriptions.

## Release Goal

Make the fork releasable for common modern subscription formats and protocols:

- Import Clash/mihomo YAML subscriptions.
- Import sing-box JSON outbound subscriptions.
- Start modern VLESS REALITY / Vision, Hysteria2, and TUIC nodes through sing-box.
- Keep legacy V2Ray-backed server types on the existing runtime path.

## Version

- Product version: `1.9.8-modern.1`
- Assembly version: `1.9.8`
- Release type: prerelease / modern-core preview

## Included Changes

- Adds a core adapter resolver so legacy and modern nodes can choose different runtimes.
- Adds a sing-box adapter and config builder.
- Adds a modern proxy node model for protocols not represented by legacy forms.
- Adds parsers for Clash/mihomo YAML and sing-box JSON subscriptions.
- Adds modern share-link parsing for `hy2://`, `hysteria2://`, `tuic://`, and VLESS REALITY / Vision links.
- Retries subscription downloads through `127.0.0.1:7890` when direct download fails and the local proxy is reachable.
- Retries subscription downloads with provider-compatible User-Agents (`Netch`, `Clash.Meta`, `ClashforWindows`, `mihomo`, `sing-box`) when the configured/default User-Agent receives a provider error or non-subscription response.
- Adds install and smoke-test scripts for sing-box and v2ray-sn.
- Adds CI/release workflow steps for .NET 6 setup, sing-box installation, and tests.
- Copies `Storage\sing-box.exe` and its manifest into release output when present.
- Adds an installer and package copy path for `bin\v2ray-sn.exe`, keeping legacy V2Ray-backed nodes startable.
- Copies default mode, i18n, native DLL/bin/sys runtime files, and core data into managed publish output so preview packages do not lose built-in modes or native dependencies.

## Supported Modern Protocols

| Protocol | Import | Config generation | Runtime smoke |
| --- | --- | --- | --- |
| VLESS REALITY / Vision | Yes | Yes | Yes |
| Hysteria2 | Yes | Yes | Yes |
| TUIC | Yes | Yes | Yes |

## Validation Performed

- `dotnet test .\Tests\Tests.csproj`
- Verified modern share-link imports for `hy2://`, `hysteria2://`, `tuic://`, and VLESS REALITY / Vision nodes.
- Verified a base64 airport subscription imports as SS/VLESS/VMess nodes without parser errors.
- Verified `checkhere.top` returns `0` parsed servers with the browser User-Agent and `71` parsed servers with provider-compatible User-Agents.
- `dotnet publish .\Netch\Netch.csproj --no-restore -c Release -r win-x64 -p:Platform=x64 -p:SelfContained=true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true -o .\artifacts\netch-1.9.8-modern.1`
- `tools\smoke-sing-box.ps1 -SingBoxPath .\artifacts\netch-1.9.8-modern.1\bin\sing-box.exe -Port 29010`
- `tools\smoke-v2ray-sn.ps1 -V2RayPath .\artifacts\netch-1.9.8-modern.1\bin\v2ray-sn.exe -Port 29009`

## Known Limits

- Modern nodes are not editable in the legacy server forms yet.
- Clash/mihomo proxy groups and rules are imported as node data only; policy behavior is not preserved.
- Some newer proxy types, such as anytls, mieru, socks5, and http provider nodes, are reported as unsupported instead of silently accepted.
- Full provider-node connectivity still depends on the subscription values and the user's network environment.

## Packaging Notes

Before packaging, install the managed sing-box runtime:

```powershell
.\tools\install-sing-box.ps1
.\tools\install-v2ray-sn.ps1
```

Then run:

```powershell
.\build.ps1 -Configuration Release -OutputPath release
```

The final package must contain:

- `Netch.exe`
- `mode\`
- `i18n\`
- `bin\sing-box.exe`
- `bin\sing-box.manifest.json`
- `bin\v2ray-sn.exe`
- `bin\v2ray-sn.manifest.json`
- `bin\geoip.dat`
- `bin\geosite.dat`
- `bin\aiodns.bin`
- `bin\aiodns.conf`
- `bin\nfapi.dll`
- `bin\nfdriver.sys`
- `bin\Redirector.bin`
- `bin\RouteHelper.bin`
- `bin\tun2socks.bin`
- `bin\wintun.dll`

## Local Preview Artifact

The local managed-preview package was generated with PowerShell `Compress-Archive` because `7z` is not installed on this machine.

- Package: `artifacts\Netch-1.9.8-modern.1.zip`
- Size: `105520398` bytes
- SHA256: `e64cc331825e74a44c489fc396bac525f81e380c5b5ece66f6ab623eaf4ae693`
