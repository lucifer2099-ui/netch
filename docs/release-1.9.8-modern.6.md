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

## Local Preview Artifact

This section is updated when the local package is generated.
