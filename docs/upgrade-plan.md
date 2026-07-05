# Netch Modernization Plan

This document turns the 2026 proxy ecosystem review into an executable upgrade plan for this fork.

## Current Baseline

- Target platform: Windows x64 desktop client.
- UI stack: WinForms on `net6.0-windows`.
- Main modes: ProcessMode, ShareMode, TunMode, WebMode.
- Existing server types: Socks5, Shadowsocks, ShadowsocksR, WireGuard, Trojan, VMess, VLESS, SSH.
- Existing proxy core path: most non-Socks5 servers are converted into a local Socks5 endpoint through `V2rayController`.
- Existing subscription path: plain/base64 share links plus protocol-specific URI parsers.

Important code anchors:

- `Netch/Controllers/MainController.cs`: selects mode controller and starts the server controller.
- `Netch/Servers/V2ray/V2rayController.cs`: starts `v2ray-sn.exe` and exposes local Socks5.
- `Netch/Servers/V2ray/V2rayConfigUtils.cs`: converts Netch server models into v2ray-compatible outbound config.
- `Netch/Utils/ShareLink.cs`: parses imported links and subscription payloads.
- `Netch/Utils/SubscriptionUtil.cs`: downloads subscriptions and replaces group nodes.

## Upgrade Goals

1. Keep Netch's original strength: process-based and game-friendly proxying on Windows.
2. Support modern airport subscription formats without requiring users to manually convert nodes.
3. Add current mainstream protocols through maintained cores instead of extending the legacy v2ray config path forever.
4. Preserve old profiles and subscriptions where practical.
5. Keep GPL-compatible distribution boundaries clear for bundled binaries and third-party cores.

## Non-Goals

- Do not embed or recommend specific commercial airport providers.
- Do not hard-code provider-specific parsing rules unless they are behind a generic compatibility layer.
- Do not rewrite the whole UI before protocol/core compatibility is stable.
- Do not remove legacy V2Ray/VLESS/VMess support in the first migration wave.

## Recommended Core Strategy

Introduce a core adapter layer:

```text
Server model / subscription node
        |
        v
ICoreAdapter
        |
        +-- LegacyV2rayAdapter  -> v2ray-sn.exe
        +-- XrayAdapter         -> xray.exe
        +-- SingBoxAdapter      -> sing-box.exe
        +-- MihomoAdapter       -> mihomo.exe
        |
        v
Local mixed/socks/http inbound
        |
        v
Existing ModeController pipeline
```

The existing mode controllers should continue to consume a local Socks5-compatible endpoint. This avoids rewriting Netfilter, WinTUN, pcap2socks, DNS, and route-management code during the first wave.

### Adapter Responsibilities

- Declare supported protocols and transports.
- Convert a normalized node into the target core's outbound config.
- Start and stop the core process.
- Report startup errors in a user-readable way.
- Expose the local inbound endpoint to existing mode controllers.

## Protocol Priority

### Wave 1

- VLESS + REALITY + Vision flow.
- Hysteria2.
- TUIC v5.
- Trojan over TLS/WebSocket/gRPC compatibility cleanup.
- Shadowsocks 2022 method compatibility.

### Wave 2

- AnyTLS.
- MASQUE / HTTP/3 style transports where core support is stable enough.
- WireGuard through sing-box/mihomo where it gives better config compatibility than the current path.
- Selector and URLTest groups for imported Clash/mihomo profiles.

### Legacy Keep-Alive

- VMess.
- VLESS without REALITY.
- Trojan classic.
- Shadowsocks classic.
- SSR, if current users still depend on it.

## Subscription Compatibility Plan

Add a parser pipeline instead of expanding `ShareLink.ParseText` with more special cases.

```text
SubscriptionUtil
        |
        v
SubscriptionParserRegistry
        |
        +-- Base64ShareLinkParser
        +-- PlainUriParser
        +-- ClashYamlParser
        +-- MihomoYamlParser
        +-- SingBoxJsonParser
        +-- NetchJsonParser
        |
        v
NormalizedNode[]
        |
        v
Server / Core profile materialization
```

### Formats To Support First

- Plain protocol URIs: `ss://`, `trojan://`, `vmess://`, `vless://`, `hysteria2://`, `tuic://`.
- Base64 subscriptions containing those URIs.
- Clash/mihomo YAML profiles with `proxies`.
- sing-box JSON profiles with `outbounds`.

### Node Fields To Normalize

- Protocol.
- Host, port, name, group.
- TLS enabled, SNI, ALPN, fingerprint.
- VLESS UUID, flow, REALITY public key, short ID, spider X.
- Transport type: tcp, ws, grpc, h2, httpupgrade, xhttp, quic.
- Hysteria2 password, obfs, up/down Mbps, insecure flag.
- TUIC UUID, password, congestion control, UDP relay mode.
- Plugin and plugin options for Shadowsocks.

## Data Model Plan

Keep existing `Server` models for legacy behavior, but add a normalized model for imported modern nodes:

- `ProxyNode`: protocol-neutral imported node.
- `ProxyTransport`: common transport settings.
- `TlsOptions`: TLS, REALITY, ALPN, fingerprint.
- `NodeSource`: subscription metadata, provider group, update timestamp.
- `CorePreference`: auto, legacy v2ray, xray, sing-box, mihomo.

Then map:

- Legacy-compatible nodes -> existing `Server` subclasses.
- Modern-only nodes -> `ProxyNode` plus a core adapter requirement.

This avoids forcing fields like REALITY or Hysteria2 into `VMessServer`.

## Implementation Phases

### Phase 0: Safety And Build Baseline

- Confirm build tools on Windows: .NET SDK, MSBuild, VC++ toolchain.
- Run `dotnet restore` and solution build.
- Add a small parser test fixture folder for subscriptions.
- Add sample sanitized nodes for VMess, VLESS REALITY, Hysteria2, TUIC, Clash YAML, and sing-box JSON.

Deliverable:

- Build/test status documented.
- No behavior changes.

### Phase 1: Core Adapter Skeleton

- Add `ICoreAdapter`.
- Move `V2rayController` behind `LegacyV2rayAdapter`.
- Change `MainController` to ask a resolver for the best adapter instead of always creating `new V2rayController()`.
- Keep the default resolver result identical to current behavior for existing nodes.

Deliverable:

- Existing nodes still start through the old core.
- Unit tests cover adapter selection.

### Phase 2: Parser Pipeline

- Add `ISubscriptionParser`.
- Keep current `ShareLink` parser as the first parser.
- Add YAML parser for Clash/mihomo `proxies`.
- Add JSON parser for sing-box `outbounds`.
- Return partial results with structured parse errors instead of dropping bad lines silently.

Deliverable:

- Existing subscriptions still import.
- Clash/mihomo and sing-box samples import into normalized nodes.

### Phase 3: sing-box Adapter

- Bundle or configure `sing-box.exe`.
- Generate a local mixed inbound and one selected outbound.
- Support VLESS REALITY, Hysteria2, TUIC, Trojan, Shadowsocks, WireGuard where practical.
- Start with single-node start behavior; profile group behavior can come later.

Deliverable:

- A Hysteria2/TUIC/VLESS REALITY node can start and expose a local Socks5 endpoint to existing modes.

### Phase 4: mihomo Adapter

- Support importing Clash/mihomo profiles without losing group structure.
- Start selected proxy or generated one-node config.
- Later support selector/url-test UI mapping.

Deliverable:

- Common airport Clash/mihomo subscriptions import and at least one selected node can start.

### Phase 5: UI And UX

- Add a subscription preview before replacing a group.
- Show unsupported nodes with exact reasons.
- Add protocol/core badges in the server list.
- Add filters by protocol, group, latency, and provider.
- Add a per-subscription compatibility report.

Deliverable:

- Users can see why a node did or does not work.

### Phase 6: Runtime And Packaging

- Upgrade target framework from `net6.0-windows` to the current LTS .NET line.
- Update NuGet dependencies.
- Replace ad hoc binary download/copy steps with versioned third-party core manifests.
- Add checksums for bundled cores.
- Add license notices for each bundled executable.

Deliverable:

- Reproducible release package with clear third-party binary inventory.

## Test Plan

- Unit tests for URI parsing.
- Unit tests for Clash/mihomo YAML parsing.
- Unit tests for sing-box JSON parsing.
- Snapshot tests for generated core configs.
- Start/stop integration tests for adapters where binaries exist.
- Manual smoke test for ProcessMode and TunMode on Windows.

## Risks

- Driver and route code may require administrator privileges during real tests.
- Core binaries have independent licenses and release schedules.
- Some providers emit non-standard YAML fields; parser should preserve unknown fields for diagnostics.
- REALITY, TUIC, and Hysteria2 support depends on the selected core version.
- Current `ShareLink.ParseText` silently ignores many parse failures; improving this may expose hidden subscription problems to users.

## First Concrete PR

The first PR should be intentionally small:

1. Add `ICoreAdapter` and `CoreAdapterResolver`.
2. Wrap current `V2rayController` as the default legacy adapter.
3. Add adapter-selection tests.
4. Keep all existing user behavior unchanged.

After that, add the parser pipeline in a separate PR.
