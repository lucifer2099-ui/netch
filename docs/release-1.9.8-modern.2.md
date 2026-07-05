# Netch 1.9.8-modern.2 Release Record

Date: 2026-07-05

This release continues the modern-core preview from `1.9.8-modern.1` and focuses on real-world airport subscriptions that return AnyTLS, Hysteria2, TUIC, and VLESS REALITY nodes.

## Release Goal

Make the `yuyun.mhlnf.cn` style subscription usable when imported by Netch:

- Import `anytls://` share links.
- Import AnyTLS nodes from Clash/mihomo YAML subscriptions.
- Import AnyTLS outbounds from sing-box JSON subscriptions.
- Generate sing-box AnyTLS outbound configs with a default `h2` ALPN when the provider omits ALPN.
- Keep Hysteria2 and VLESS REALITY nodes working through the existing sing-box adapter.

## Version

- Product version: `1.9.8-modern.2`
- Assembly version: `1.9.8`
- Release type: prerelease / modern-core preview

## Included Changes Since modern.1

- Adds AnyTLS support to the modern share-link parser.
- Adds AnyTLS support to Clash/mihomo YAML parsing.
- Adds AnyTLS support to sing-box JSON outbound parsing.
- Adds sing-box config generation for AnyTLS.
- Defaults AnyTLS TLS ALPN to `h2` when the subscription does not provide ALPN.
- Adds unit coverage for AnyTLS parsing and config generation.

## Subscription Diagnosis

The tested subscription returns different formats by User-Agent:

- Default / `Netch`: base64 share links with `anytls`, `hysteria2`, and `vless` nodes.
- `Clash.Meta`: Clash/mihomo YAML.
- `sing-box`: sing-box JSON.

Observed node distribution in the base64 response:

- `anytls`: 12
- `hysteria2`: 29
- `vless`: 36

Real connectivity checks through sing-box:

- Hysteria2 node: HTTP 204 via local mixed proxy.
- VLESS node: HTTP 204 via local mixed proxy.
- AnyTLS node: failed without ALPN, passed with `h2` ALPN.

## Validation Performed

- `dotnet test .\Tests\Tests.csproj --no-restore`
- `sing-box check` on a real AnyTLS outbound from the subscription.
- Real AnyTLS local mixed-proxy smoke with `alpn: ["h2"]`.
- Real Hysteria2 local mixed-proxy smoke.
- Real VLESS local mixed-proxy smoke.

## Known Limits

- Modern nodes are not editable in the legacy server forms yet.
- AnyTLS relies on sing-box runtime support.
- If a provider requires an ALPN other than `h2`, the node may still require a future editable advanced option.

## Local Preview Artifact

- Package: `artifacts\Netch-1.9.8-modern.2.zip`
- Size: `105520754` bytes
- SHA256: `e6e88e383c7bb53f7c472e0386bfe087c7019bc214fd94bffe6f9bfe001a6811`
