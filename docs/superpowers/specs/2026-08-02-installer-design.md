# Whisper Flow — Windows Installer Design

**Date:** 2026-08-02
**Status:** Approved
**Scope:** M3-B — Inno Setup `.exe` installer + GitHub Actions release pipeline

---

## Goal

Package Whisper Flow Local into a self-contained Windows `.exe` installer distributed via GitHub Releases, with a static download URL suitable for a Netlify website download button. Releases are triggered by a git tag — no manual build steps.

---

## Architecture

Two new files, zero changes to app source code:

```
installer/
  WhisperFlow.iss          # Inno Setup script
.github/
  workflows/
    release.yml            # GitHub Actions release workflow
```

**Release flow:**

```
git tag v0.1.0 && git push --tags
        ↓
GitHub Actions (windows-latest)
  1. dotnet publish -r win-x64 --self-contained → publish/
  2. iscc /DMyAppVersion=0.1.0 installer\WhisperFlow.iss → WhisperFlowSetup.exe
  3. Create GitHub Release, upload WhisperFlowSetup.exe
        ↓
https://github.com/abhi-30702/Speakease/releases/latest/download/WhisperFlowSetup.exe
        ↓
Netlify download button → that URL (always resolves to latest release)
```

---

## dotnet publish configuration

```
dotnet publish WhisperFlowLocal.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishReadyToRun=true \
  -o publish/
```

- **Self-contained:** bundles .NET 8 Desktop Runtime — no runtime prereq for end users
- **win-x64:** targets 64-bit Windows (Windows 10 1809+ required by .NET 8 WPF)
- **PublishReadyToRun:** pre-JITs for faster cold-start
- **Folder publish (not single-file):** avoids native DLL extraction latency from Whisper.net.Runtime on first launch
- Output: `publish/` directory (~150–180 MB with runtime)

---

## Inno Setup Script (`installer/WhisperFlow.iss`)

### Key settings

| Setting | Value | Reason |
|---------|-------|--------|
| `PrivilegesRequired` | `lowest` | Per-user install, no UAC prompt |
| `DefaultDirName` | `{localappdata}\Programs\WhisperFlowLocal` | Per-user, no admin |
| `Compression` | `lzma2/ultra64` | Smallest output for large self-contained bundle |
| `OutputBaseFilename` | `WhisperFlowSetup` | Static filename → stable Netlify download URL |
| `AppVersion` | `#define` injected via CLI | Sourced from git tag at build time |
| `SetupIconFile` | `Resources\tray-icon.ico` | Reuse existing app icon |

### Installer behaviour

- **Install location:** `%LocalAppData%\Programs\WhisperFlowLocal\`
- **Start Menu:** `Whisper Flow` shortcut → `WhisperFlowLocal.exe`
- **Desktop shortcut:** optional checkbox during install (default: checked)
- **Uninstaller:** registered in Windows "Apps & features"; removes installed files only
- **User data preserved on uninstall:** `%AppData%\WhisperFlowLocal\` (settings.json, insights.db, Whisper model) is NOT touched by the uninstaller — user data survives reinstall/upgrade

### Whisper model

The ggml-small.en model (~465 MB) is gitignored and therefore not included in the installer. The OnboardingWindow (built in M3-A) downloads it on first launch. The installer only ships the app binaries.

### Version injection

The Inno Setup script declares `MyAppVersion` as an empty define:
```pascal
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
```

GitHub Actions passes the real version via the `iscc` CLI:
```
iscc /DMyAppVersion=0.1.0 installer\WhisperFlow.iss
```

---

## GitHub Actions Workflow (`.github/workflows/release.yml`)

### Trigger

```yaml
on:
  push:
    tags:
      - 'v*.*.*'
```

### Permissions

```yaml
permissions:
  contents: write   # required to create releases and upload assets
```

### Runner

`windows-latest` — required for WPF publish; Inno Setup 6 is pre-installed.

### Steps

| Step | Action |
|------|--------|
| Checkout | `actions/checkout@v4` |
| Setup .NET 8 | `actions/setup-dotnet@v4` with `dotnet-version: 8.0.x` |
| Restore & publish | `dotnet publish` with self-contained win-x64 flags, output to `publish/` |
| Extract version | Strip `v` prefix from `github.ref_name` → `APP_VERSION` env var |
| Compile installer | `iscc /DMyAppVersion=$env:APP_VERSION installer\WhisperFlow.iss` |
| Create release + upload | `softprops/action-gh-release@v2` — creates release from tag, uploads `WhisperFlowSetup.exe` |

### Release naming

- Release title: `Whisper Flow v{version}` (e.g., `Whisper Flow v0.1.0`)
- Asset filename: `WhisperFlowSetup.exe` (consistent across all releases)
- Release notes: auto-generated from commits since last tag (`generate_release_notes: true`)

---

## Netlify Website Integration

The download button on the Netlify site links to:

```
https://github.com/abhi-30702/Speakease/releases/latest/download/WhisperFlowSetup.exe
```

GitHub's `/releases/latest/download/<filename>` redirect always resolves to the most recent release's asset with that filename. No URL update needed on each release — tag and push is the entire release process.

---

## Code Signing

Not implemented. Windows SmartScreen will show a one-time "Windows protected your PC" warning on first run. Users click "More info → Run anyway." Standard for unsigned indie tools distributed via GitHub. Can be revisited if the app goes commercial.

---

## Testing

Manual smoke test after first release:

1. Run `git tag v0.1.0 && git push --tags`
2. Verify GitHub Actions workflow passes (green)
3. Verify GitHub Release created with `WhisperFlowSetup.exe` asset
4. Download and run installer on a clean Windows machine (or VM)
5. Verify: no UAC prompt, installs to `%LocalAppData%\Programs\WhisperFlowLocal\`
6. Verify: Start Menu shortcut and optional Desktop shortcut appear
7. Launch app → OnboardingWindow appears (first run)
8. Uninstall via "Apps & features" → app removed, `%AppData%\WhisperFlowLocal\` preserved

---

## Out of Scope

- Code signing / EV certificate
- Microsoft Store submission
- Auto-update (separate M3 sub-project)
- macOS packaging (M4)
