# Modern Core Preview Release Notes

This release is a publishable preview for modern proxy-core support.

## Highlights

- Adds a core adapter layer.
- Keeps legacy V2Ray behavior for existing server types.
- Adds sing-box as the first modern core adapter.
- Imports modern Clash/mihomo YAML nodes as `ModernProxyServer`.
- Imports sing-box JSON `outbounds` as `ModernProxyServer`.
- Generates sing-box configs for:
  - VLESS REALITY / Vision
  - Hysteria2
  - TUIC
- Adds repeatable sing-box install and smoke-test scripts.
- CI/release workflows install sing-box and run tests before packaging.

## Supported Import Formats

- Existing Netch/share-link subscriptions.
- Clash/mihomo YAML `proxies`.
- sing-box JSON `outbounds`.

## Modern Protocol Status

| Protocol | Import | Config generation | Runtime smoke |
| --- | --- | --- | --- |
| VLESS REALITY / Vision | Yes | Yes | Yes |
| Hysteria2 | Yes | Yes | Yes |
| TUIC | Yes | Yes | Yes |

## Known Limits

- Modern nodes are not editable in the legacy server forms yet.
- Full UI preview/reporting for unsupported fields is still pending.
- Clash/mihomo `proxy-groups` and `rules` are not preserved yet.
- Real provider-node connectivity still depends on the user's subscription values and network environment.
- The app is still targeting .NET 6 for this preview to minimize release risk.

## Release Checklist

1. Install .NET SDK 6.
2. Run `tools\install-sing-box.ps1`.
3. Run `tools\smoke-sing-box.ps1`.
4. Run `dotnet test .\Tests\Tests.csproj`.
5. Run `.\build.ps1 -Configuration Release -OutputPath release`.
6. Confirm `release\bin\sing-box.exe` exists.
