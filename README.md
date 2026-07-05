# `In preparation for 2.0, this repository will be cleared of all 1.0 related releases and code`
<p align="center"><img src="https://github.com/NetchX/Netch/blob/main/Netch/Resources/Netch.png?raw=true" width="128" /></p>

<div align="center">

# Netch
A simple proxy client

[![](https://img.shields.io/badge/telegram-group-green?style=flat-square)](https://t.me/netch_group)
[![](https://img.shields.io/badge/telegram-channel-blue?style=flat-square)](https://t.me/netch_channel)
[![](https://img.shields.io/github/downloads/netchx/netch/total.svg?style=flat-square)](https://github.com/netchx/netch/releases)
[![](https://img.shields.io/github/v/release/netchx/netch?style=flat-square)](https://github.com/netchx/netch/releases)
</div>

## Features
Some features may not be implemented in version 1

### Modes
- `ProcessMode` - Use Netfilter driver to intercept process traffic
- `ShareMode` - Share your network based on WinPcap / Npcap
- `TunMode` - Use WinTUN driver to create virtual adapter
- `WebMode` - Web proxy mode

### Protocols
- [`Socks5`](https://www.wikiwand.com/en/SOCKS)
- [`Shadowsocks`](https://shadowsocks.org)
- [`ShadowsocksR`](https://github.com/shadowsocksrr/shadowsocksr-libev)
- [`WireGuard`](https://www.wireguard.com)
- [`Trojan`](https://trojan-gfw.github.io/trojan)
- [`VMess`](https://www.v2fly.org)
- [`VLESS`](https://xtls.github.io)

### Others
- UDP NAT FullCone (Limited by your server)
- .NET 6.0 x64

## Modern Core Preview
Version `1.9.8-modern.6` adds a publishable preview path for current airport/node subscriptions without removing the legacy Netch behavior.

### Modern imports
- Clash/mihomo YAML `proxies`
- sing-box JSON `outbounds`
- Existing Netch and share-link subscriptions
- Modern share links: `anytls://`, `hy2://`, `hysteria2://`, `tuic://`, and VLESS REALITY / Vision links

### Modern protocols
- AnyTLS through sing-box, with default `h2` ALPN when the subscription omits ALPN
- VLESS REALITY / Vision through sing-box
- Hysteria2 through sing-box
- TUIC through sing-box

### Runtime notes
- The release package includes `bin\sing-box.exe` when `tools\install-sing-box.ps1` has been run before packaging.
- Modern nodes are imported as `ModernProxyServer` entries and started through the sing-box adapter.
- Modern nodes can be edited from the server edit button, including SNI, ALPN, TLS, Reality, Hysteria2, and TUIC fields.
- Subscription updates show an import report with imported count, skipped/unsupported warnings, and failed subscriptions.
- The Server menu includes `HTTP Connectivity Test`; after startup, the speed/test icon runs the real HTTP connectivity test without disabling the whole window.
- If the configured local SOCKS port is blocked by Windows or endpoint security software, the sing-box adapter falls back to a nearby available local port and reports the actual port to the mode controller.
- Legacy Socks5, Shadowsocks, ShadowsocksR, Trojan, VMess, VLESS, WireGuard, and existing modes continue to use the existing controllers.
- See `docs\release-1.9.8-modern.6.md`, `docs\release-1.9.8-modern.5.md`, `docs\release-1.9.8-modern.4.md`, `docs\release-1.9.8-modern.3.md`, `docs\release-1.9.8-modern.2.md`, `docs\release-1.9.8-modern.1.md`, `docs\release-modern-core-preview.md`, and `docs\sing-box-runtime.md` for release scope, runtime setup, and validation commands.

## Sponsor
<a href="https://www.jetbrains.com/?from=Netch"><img src="jetbrains.svg" alt="JetBrains" width="200"/></a>

## License
Netch is licensed under the [GPLv3](https://raw.githubusercontent.com/netchx/netch/main/LICENSE) license
