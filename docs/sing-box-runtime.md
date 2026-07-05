# sing-box Runtime

Netch modern proxy nodes use `sing-box.exe` as the first modern core adapter.

## Install

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\install-sing-box.ps1
```

The installer uses `http://127.0.0.1:7890` as the default proxy for GitHub downloads. Override or disable it with `-Proxy`.

The script installs:

- `Storage\sing-box.exe`
- `Storage\sing-box.manifest.json`

These generated files are ignored by Git.

If GitHub downloads are blocked, pass a direct mirror URL:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\install-sing-box.ps1 -DownloadUrl "https://example.com/sing-box-windows-amd64.zip"
```

Use a different proxy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\install-sing-box.ps1 -Proxy "http://127.0.0.1:7890"
```

## Build Packaging

`build.ps1` copies `Storage\sing-box.exe` to `release\bin\sing-box.exe` when present.

Debug builds also copy `Storage\sing-box.exe` and `Storage\sing-box.manifest.json` to the app output `bin` folder when the files exist.

If the file is missing, legacy nodes still build and run, but modern nodes that resolve to `SingBoxAdapter` cannot start.

## Runtime Config

The adapter writes the generated client config to:

```text
data\sing-box-last.json
```

The generated config currently supports:

- VLESS REALITY / Vision
- Hysteria2
- TUIC

All three expose a local `mixed` inbound on the configured Netch local address and Socks5 port so the existing mode pipeline can continue to consume a local proxy endpoint.

## Smoke Test

After installing `sing-box.exe`, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\smoke-sing-box.ps1
```

The smoke test validates a generated VLESS REALITY config with `sing-box check`, starts a local mixed inbound on `127.0.0.1:28991`, verifies that the TCP port accepts a connection, and then stops the process.
