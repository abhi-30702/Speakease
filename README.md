# Speakease

A Windows desktop dictation app that transcribes speech to text and inserts it into any active window — all locally, with no clipboard. Built with .NET 8 + WPF.

## How it works

Hold **Ctrl+Space** to record. Release to transcribe and insert. The text appears directly at your cursor in whatever app you were in.

```
Microphone → Whisper (local) → Groq cleanup → Insert at cursor
                                    ↓ (offline fallback)
                               Regex cleanup
```

- **Transcription:** [Whisper.net](https://github.com/sandrohanea/whisper.net) running the `small.en` model locally — no audio leaves your machine
- **Cleanup:** Groq API removes filler words, resolves self-corrections, fixes punctuation. Falls back to regex if Groq is unavailable or unconfigured
- **Insertion:** Win32 `SendInput` — types directly into the focused window, no clipboard involved
- **Insights:** Every dictation is logged to a local SQLite database

## Features

- Hold-to-dictate or toggle mode (via tray menu)
- Floating pill indicator shows recording/transcribing/error state
- Tray icon with quick access to Settings and quit
- Groq API key configurable via Settings panel (optional — app works fully offline without it)
- Insights dashboard: words dictated, WPM, app usage breakdown, 91-day streak heatmap, voice stats

## Requirements

- Windows 10/11 (x64)
- .NET 8 Desktop Runtime
- ~500 MB disk space (Whisper model auto-downloads on first run)
- Groq API key (optional, for smarter cleanup)

## Getting started

1. Clone the repo
2. Build: `dotnet build WhisperFlowLocal.sln`
3. Run: `dotnet run --project WhisperFlowLocal`
4. On first launch the Whisper `small.en` model (~465 MB) downloads automatically
5. Optionally open Settings (tray → Settings) and enter a Groq API key

## Project structure

```
Models/          Domain models and DictationEngine orchestrator
Services/        Audio, transcription, cleanup, insertion, SQLite repository
ViewModels/      MVVM view models (CommunityToolkit.Mvvm)
Views/           InsightsView, SettingsView UserControls
Windows/         MainWindow (nav shell), PillWindow (floating indicator)
Converters/      WPF value converters
Interop/         Win32 P/Invoke declarations
Tests/           xUnit test project (33 tests)
```

## Tech stack

| Layer | Technology |
|---|---|
| UI | WPF (.NET 8), CommunityToolkit.Mvvm 8.3.2 |
| Transcription | Whisper.net (ggml-small.en model) |
| Cleanup | Groq API (`llama-3.3-70b-versatile`) + regex fallback |
| Storage | Microsoft.Data.Sqlite 8.0 |
| Audio | NAudio (16kHz mono capture) |
| Hotkey | Win32 `SetWindowsHookEx` (WH_KEYBOARD_LL) |
| Insertion | Win32 `SendInput` |

## Running tests

```powershell
dotnet test Tests\WhisperFlowLocal.Tests.csproj
```

## Author

Abhishek K
