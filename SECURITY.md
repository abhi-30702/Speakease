# Security Policy

## Supported Versions

Only the latest release receives security fixes.

| Version | Supported |
| ------- | --------- |
| 1.0.x   | ✅        |
| < 1.0   | ❌        |

## Reporting a Vulnerability

**Do not open a public GitHub issue for security vulnerabilities.**

Email: **abhi30702@gmail.com**  
Subject line: `[Speakease Security] <brief description>`

Include:
- Description of the vulnerability and its potential impact
- Steps to reproduce or a proof-of-concept
- Affected version(s)

You will receive an acknowledgement within **48 hours** and a status update within **7 days**. If the report is confirmed, a patched release will be issued as quickly as possible and you will be credited in the release notes (unless you prefer to remain anonymous).

Please do not disclose the issue publicly until a fix has been released.

## Security Architecture

Speakease is a local-first Windows desktop application. Below is a summary of its security model.

### What runs locally (no network)
- Speech recognition — Whisper model runs entirely on-device
- Text insertion — `SendInput` Win32 API, no clipboard used
- Dictation history — SQLite database at `%AppData%\Speakease\insights.db`
- Settings — `%AppData%\Speakease\settings.json`

### What leaves the device
| Data | Destination | Condition |
|------|------------|-----------|
| Cleaned transcription text | Groq API (HTTPS) | Only if a Groq API key is configured in Settings |
| GitHub release tag | `api.github.com` (HTTPS) | On startup, for update check |

### Implemented protections
| Threat | Mitigation |
|--------|-----------|
| API key theft from disk | Groq API key encrypted with Windows DPAPI (`ProtectedData`, current-user scope) |
| Hotkey injection via transcription output | All control characters (0x00–0x1F, 0x7F) stripped before `SendInput` |
| Whisper model tampering | SHA-256 sidecar (`.sha256`) written on first download; verified on every launch; mismatch triggers automatic re-download |
| Keyboard hook loss | 5-minute health timer re-installs `WH_KEYBOARD_LL` hook if the OS drops it |
| Network interception | All outbound connections use system TLS (no certificate pinning) |

### Known limitations
- **Unsigned binary** — `SpeakeaseSetup.exe` and `Speakease.exe` are not code-signed. Windows SmartScreen will show a warning on first run. A code-signing certificate is planned for a future release.
- **Plaintext dictation database** — `insights.db` is not encrypted. If you dictate sensitive information, use the **Clear history** button in the Insights tab to delete all stored records.
- **Global keyboard hook** — `WH_KEYBOARD_LL` receives every keystroke system-wide to detect the Ctrl+Space hotkey. Only that combination triggers any action; no other input is read or stored. To protect against DLL-injection attacks that could abuse this hook, enable **Memory Integrity (HVCI)** in Windows Security → Device Security → Core Isolation.
