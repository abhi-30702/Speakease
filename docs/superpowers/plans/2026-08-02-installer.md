# M3-B Windows Installer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Package Whisper Flow Local into a self-contained `.exe` installer via Inno Setup, with a GitHub Actions workflow that builds and publishes it automatically on every version tag.

**Architecture:** `dotnet publish -r win-x64 --self-contained` produces a folder with the .NET 8 runtime bundled. Inno Setup packages that folder into a single `WhisperFlowSetup.exe`. A GitHub Actions workflow on `windows-latest` runs both steps when a `v*.*.*` tag is pushed, then creates a GitHub Release and uploads the installer as an asset.

**Tech Stack:** Inno Setup 6, GitHub Actions (`windows-latest`), `softprops/action-gh-release@v2`, .NET 8 self-contained win-x64 publish

---

## File Structure

**New files:**
- `.gitignore` — ignores build artifacts (`publish/`, `installer/output/`, standard .NET artifacts)
- `installer/WhisperFlow.iss` — Inno Setup 6 script; reads `publish/` folder, outputs `installer/output/WhisperFlowSetup.exe`
- `.github/workflows/release.yml` — triggers on `v*.*.*` tags; publishes, compiles installer, creates GitHub Release

---

## Task 1: .gitignore

**Files:**
- Create: `.gitignore`

No tests (config file).

- [ ] **Step 1: Create `.gitignore`**

```
# .NET build artifacts
bin/
obj/
publish/

# Inno Setup output
installer/output/

# Whisper model (too large to commit)
Resources/Models/

# User settings (local only)
*.user
.vs/
```

- [ ] **Step 2: Commit**

```
git add .gitignore
git commit -m "chore: add .gitignore"
```

---

## Task 2: Inno Setup script

**Files:**
- Create: `installer/WhisperFlow.iss`

No unit tests (packaging script). Verification is by compiling the script locally if Inno Setup 6 is installed, or by the CI run in Task 3.

- [ ] **Step 1: Create `installer/WhisperFlow.iss`**

```pascal
; WhisperFlow.iss — Inno Setup 6 script
; Version is injected at build time via: iscc /DMyAppVersion=0.1.0 WhisperFlow.iss

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#define MyAppName      "Whisper Flow"
#define MyAppExeName   "WhisperFlowLocal.exe"
#define MyAppPublisher "Abhishek K"
#define MyAppURL       "https://github.com/abhi-30702/Speakease"
#define MyAppRoot      "{#SourcePath}\.."

[Setup]
AppId={{B8F4C2A1-9D3E-4F7B-A5C6-8E2D1F0B9A3C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\WhisperFlowLocal
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#SourcePath}\output
OutputBaseFilename=WhisperFlowSetup
SetupIconFile={#MyAppRoot}\Resources\tray-icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MyAppRoot}\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent
```

Key decisions:
- `PrivilegesRequired=lowest` — installs to `%LocalAppData%\Programs\WhisperFlowLocal\`, no UAC prompt
- `ArchitecturesAllowed=x64compatible` — blocks installation on 32-bit Windows (correct: .NET 8 win-x64 won't run there)
- `AppId` GUID — do NOT change this after first release; Windows uses it to identify the app for upgrades/uninstall
- `OutputDir={#SourcePath}\output` — output goes to `installer/output/` (relative to .iss file location)
- `Source: "{#MyAppRoot}\publish\*"` — `{#SourcePath}` is the `installer/` directory; `\..` steps up to the repo root; `publish\*` is the dotnet publish output
- Desktop shortcut checkbox is **unchecked by default** (no `; Flags: unchecked` means it defaults to unchecked via the Tasks system — actually `Tasks:` in [Icons] means it's optional, not checked by default)

- [ ] **Step 2: Commit**

```
git add installer/WhisperFlow.iss
git commit -m "feat(installer): add Inno Setup script"
```

---

## Task 3: GitHub Actions release workflow

**Files:**
- Create: `.github/workflows/release.yml`

No unit tests (CI config). Verified by pushing a tag and watching the workflow.

- [ ] **Step 1: Create `.github/workflows/release.yml`**

```yaml
name: Release

on:
  push:
    tags:
      - 'v*.*.*'

permissions:
  contents: write   # needed to create releases and upload assets

jobs:
  build-and-release:
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Publish self-contained win-x64
        run: |
          dotnet publish WhisperFlowLocal.csproj `
            -c Release `
            -r win-x64 `
            --self-contained true `
            -p:PublishReadyToRun=true `
            -o publish

      - name: Extract version from tag
        id: version
        shell: pwsh
        run: |
          $version = "${{ github.ref_name }}".TrimStart('v')
          "APP_VERSION=$version" | Out-File -FilePath $env:GITHUB_ENV -Append

      - name: Compile installer
        shell: pwsh
        run: |
          & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
            /DMyAppVersion=$env:APP_VERSION `
            installer\WhisperFlow.iss

      - name: Create GitHub Release and upload installer
        uses: softprops/action-gh-release@v2
        with:
          name: "Whisper Flow v${{ env.APP_VERSION }}"
          generate_release_notes: true
          files: installer/output/WhisperFlowSetup.exe
```

Notes on key steps:
- `dotnet publish` runs from the repo root (checkout dir); `.csproj` is at the repo root ✓
- `-o publish` outputs to `publish/` at the repo root, which matches `{#MyAppRoot}\publish\*` in the .iss script ✓
- `TrimStart('v')` converts `v0.1.0` → `0.1.0` for Inno Setup's `AppVersion` field
- Inno Setup 6 is pre-installed on `windows-latest` at `C:\Program Files (x86)\Inno Setup 6\ISCC.exe` ✓
- `generate_release_notes: true` auto-generates release notes from commits since the last tag
- `files: installer/output/WhisperFlowSetup.exe` must match `OutputDir` + `OutputBaseFilename` in the .iss script ✓

- [ ] **Step 2: Commit and push**

```
git add .github/workflows/release.yml
git commit -m "feat(ci): add GitHub Actions release workflow"
git push origin master
```

---

## Task 4: First release smoke test

No code changes. Verify the full pipeline end-to-end.

- [ ] **Step 1: Tag and push**

```
git tag v0.1.0
git push origin v0.1.0
```

- [ ] **Step 2: Watch the workflow**

Go to: `https://github.com/abhi-30702/Speakease/actions`

Expected: workflow named "Release" appears, runs for ~5–8 minutes (dotnet publish + installer compile), turns green.

If it fails:
- Red on "Publish self-contained win-x64" → check .csproj path (must be at repo root)
- Red on "Compile installer" → check ISCC.exe path, check .iss script paths
- Red on "Create GitHub Release" → check `permissions: contents: write` is in the workflow

- [ ] **Step 3: Verify the GitHub Release**

Go to: `https://github.com/abhi-30702/Speakease/releases`

Expected:
- Release titled "Whisper Flow v0.1.0" exists
- Asset `WhisperFlowSetup.exe` is attached (~150–180 MB)
- Release notes auto-generated from recent commits

- [ ] **Step 4: Verify the download URL**

Open in browser: `https://github.com/abhi-30702/Speakease/releases/latest/download/WhisperFlowSetup.exe`

Expected: file downloads (GitHub redirects `/releases/latest/download/` to the most recent release's asset with that filename)

This is the URL the Netlify website download button will use.

- [ ] **Step 5: Manual install test**

Run `WhisperFlowSetup.exe` on a Windows machine (or the dev machine itself):

1. SmartScreen warning appears → click "More info" → "Run anyway" ✓
2. Installer wizard opens — no UAC/admin prompt ✓
3. Default install path shows `C:\Users\<name>\AppData\Local\Programs\WhisperFlowLocal` ✓
4. Desktop shortcut checkbox shown (unchecked by default) ✓
5. Complete install → "Launch Whisper Flow" checkbox shown
6. App launches → OnboardingWindow appears (first run, no settings.json yet) ✓
7. Open "Apps & features" in Windows Settings → "Whisper Flow" listed with uninstaller ✓
8. Uninstall → app removed; verify `%AppData%\WhisperFlowLocal\` still exists (user data preserved) ✓
