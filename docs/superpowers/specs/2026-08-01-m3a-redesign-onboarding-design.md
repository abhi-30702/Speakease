# Whisper Flow Local — M3-A: Visual Redesign + Onboarding Design

**Version:** 1.0
**Date:** 2026-08-01
**Author:** Abhishek K
**Status:** Approved for implementation

---

## Goal

Retheme the entire app to the new color palette (#1D1D1D / #E5BDDF), replace the pill window with a Dynamic Island-style floating capsule, and add a 4-step first-run onboarding wizard.

**M3-A exit criteria:**
- All UI surfaces use #1D1D1D background and #E5BDDF accent (no teal, no dark blue)
- Dynamic Island pill appears on record/transcribe, is invisible at idle
- First-run onboarding guides a new user through model download → hotkey → try it
- Returning users skip onboarding entirely (flag persisted in settings.json)

---

## Architecture

```
App.OnStartup
  ├── HasCompletedOnboarding = false → show OnboardingWindow
  │     OnboardingWindow completes → sets flag → opens to idle
  └── HasCompletedOnboarding = true  → idle (pill hidden, tray active)

PillWindow (replaced)
  └── DynamicIslandWindow — transparent, always-on-top, no taskbar
        RecordingState.Idle        → fully transparent (hidden)
        RecordingState.Recording   → black capsule + pink waveform + "Listening"
        RecordingState.Transcribing → capsule + pink spinner + "Transcribing…"
        RecordingState.Error       → capsule + error text, auto-dismiss 3 s
```

---

## Subsystem 1: Color Retheme

**Approach:** Global find-and-replace of color values across all XAML files. No new abstractions — just swap the hex strings.

### Color map

| Old value | New value | Role |
|-----------|-----------|------|
| `#0F172A` | `#1D1D1D` | Window / page background |
| `#0B1120` | `#141414` | Sidebar background |
| `#1E293B` | `#242424` | Cards, nav active state |
| `#0d9488` | `#E5BDDF` | Primary accent (buttons, active nav, progress bar) |
| `#2DD4BF` | `#E5BDDF` | Secondary accent (same — collapse to one accent) |
| `#334155` | `#333333` | Borders, dividers |
| `#64748B` | `#666666` | Muted labels |
| `#94A3B8` | `#888888` | Secondary text |
| `#CBD5E1` | `#CCCCCC` | Primary text (secondary) |
| `#F8FAFC` | `#F0F0F0` | Primary text |
| `#E2E8F0` | `#E0E0E0` | Hover text |

**Button foreground:** Buttons with `#E5BDDF` background use `#1D1D1D` as foreground (dark text on light pink).

**Files to update:**
- `Windows/MainWindow.xaml`
- `Views/InsightsView.xaml`
- `Views/SettingsView.xaml`
- `Windows/PillWindow.xaml` — replaced entirely (see below)
- `Converters/CountToColorConverter.cs` — update streak heatmap colours to match new palette

### Streak heatmap colour ramp (CountToColorConverter)

| Count | Old hex | New hex |
|-------|---------|---------|
| 0 | `#1E293B` | `#242424` |
| 1–2 | `#0f5e58` | `#6b3f6b` |
| 3–5 | `#0d7a72` | `#8f4f8f` |
| 6–9 | `#0d9488` | `#b86ab8` |
| 10+ | `#2dd4bf` | `#E5BDDF` |

---

## Subsystem 2: Dynamic Island Pill

Replaces `PillWindow` entirely. New file: `Windows/DynamicIslandWindow.xaml` + `.cs`.

### Window properties

```xml
<Window ShowInTaskbar="False"
        AllowsTransparency="True"
        WindowStyle="None"
        Background="Transparent"
        ShowActivated="False"
        Topmost="True"
        ResizeMode="NoResize"
        Width="240" Height="50">
```

Positioned at bottom-centre of the primary screen, 24 px above the taskbar edge. Updated on `Loaded` and on `SystemEvents.DisplaySettingsChanged`.

### States and visuals

**Idle** — `Visibility="Collapsed"` on the capsule. Window itself stays open (needed to reshow), but nothing is drawn.

**Recording** — Black rounded capsule (`CornerRadius="25"`, `Background="#000000"`), 200×36 px, containing:
- Animated waveform: 10 `Rectangle` bars, heights driven by a `DispatcherTimer` randomising between 4–16 px at 80ms intervals, `Fill="#E5BDDF"`
- Label: `"Listening"`, `Foreground="#E5BDDF"`, `FontSize="11"`
- Spring-open animation: `DoubleAnimation` on `Width` from 0 → 200, 220ms, `CubicEase Out`

**Transcribing** — Same capsule, 160×36 px, containing:
- Small arc spinner: `Ellipse` with `StrokeDashArray` animated via `DoubleAnimation` on `StrokeDashOffset`, `Stroke="#E5BDDF"`, 16×16 px
- Label: `"Transcribing…"`, `Foreground="#888888"`, `FontSize="11"`
- Width shrinks from 200 → 160 with same spring animation

**Error** — Same capsule, ~220 px wide, containing:
- Short error message text, `Foreground="#E5BDDF"`, `FontSize="10"`, clipped to 1 line
- Auto-dismisses after 3 s via `DispatcherTimer` → sets state back to Idle

**Inserting** — Same as Transcribing (no distinct visual needed; the state is brief).

### ViewModel

`DynamicIslandViewModel` replaces `PillViewModel`. Binds to `DictationEngine.State` and `DictationEngine.ErrorMessage`. Exposes `IsVisible`, `CapsuleWidth`, `ShowWaveform`, `ShowSpinner`, `StatusText`.

`PillViewModel.cs` and `Windows/PillWindow.xaml` + `.cs` are **deleted**.

### Waveform animation

A `DispatcherTimer` fires every 80ms while `State == Recording`. On each tick, set 10 bar heights to `Random.Shared.Next(4, 17)`. Bind bar heights via `ObservableCollection<double> WaveformBars` or directly update named `Rectangle` elements in code-behind (simpler, no binding overhead for 10 elements).

**Code-behind approach** (simpler): Name the 10 rectangles `Bar0`–`Bar9` in XAML. Timer tick sets `BarN.Height` directly. No ViewModel property needed for individual bars.

---

## Subsystem 3: OnboardingWindow

New files: `Windows/OnboardingWindow.xaml` + `.cs`.

### Window properties

```
Width=560, Height=420, ResizeMode=NoResize
Background="#1D1D1D", WindowStartupLocation=CenterScreen
WindowStyle=None (custom chrome — no system title bar)
ShowInTaskbar=True (user can alt-tab to it during model download)
```

Custom close button (× top-right) exits the app if clicked before completion (user chose not to set up).

### First-run detection

`AppSettings` gains one new property:

```csharp
public bool HasCompletedOnboarding { get; set; } = false;
```

In `App.OnStartup`, after `settingsService.Load()`:

```csharp
if (!settingsService.Current.HasCompletedOnboarding)
{
    var onboarding = new OnboardingWindow(transcription, settingsService);
    onboarding.ShowDialog(); // blocks until complete or closed
    if (!settingsService.Current.HasCompletedOnboarding)
    {
        Shutdown(); // user closed without finishing
        return;
    }
}
```

`TranscriptionService.InitializeAsync()` is called **inside** `OnboardingWindow` (Step 2) so the progress bar can report download progress. The `IProgress<string>` callback drives the UI label.

After `ShowDialog()` returns (and for returning users who skip onboarding), `App.OnStartup` still calls `await transcription.InitializeAsync()` — this is a no-op for new users (the guard `if (_processor is not null) return` fires) and the real initialisation for returning users. This means the tray balloon "Loading speech model…" shown to returning users still works unchanged.

### Step layout

Thin pink progress bar at the very top of the window (full width, 3 px, `#E5BDDF`). Width animates: 25% → 50% → 75% → 100% as steps advance.

Each step fills the content area below the bar. Two-button footer (Back / Next) except Step 1 (no Back) and Step 4 (Next → "Done").

### Step 1 — Welcome

- Large app icon or simple microphone SVG path, centred
- Heading: `"Welcome to Whisper Flow"`, `FontSize="22"`, `Foreground="#F0F0F0"`
- Subtext: `"Dictate into any app. Text appears at your cursor — no clipboard, no cloud."`, `Foreground="#888"`, `FontSize="13"`
- Single button: `"Get started →"`, `Background="#E5BDDF"`, `Foreground="#1D1D1D"`

### Step 2 — Model download

- Heading: `"Downloading speech model"`
- Subtext: `"~465 MB · one-time download · never sent to the cloud"`
- `ProgressBar` (WPF), indeterminate while downloading (Whisper.net downloader doesn't report byte progress)
- Status label driven by `IProgress<string>` callback from `TranscriptionService.InitializeAsync`
- If model already exists: label shows `"Model ready ✓"`, Next button enabled immediately
- Next button disabled until `InitializeAsync` completes

### Step 3 — Hotkey tutorial

- Heading: `"Your dictation hotkey"`
- Visual: two `Border` elements styled as keyboard keys — `Ctrl` and `Space` — side by side, `Background="#242424"`, `BorderBrush="#E5BDDF"`, `CornerRadius="6"`, `FontSize="14"`, `Foreground="#F0F0F0"`. A `+` label between them.
- Instruction: `"Hold Ctrl+Space anywhere to start dictating. Release to transcribe and insert."`
- Toggle mode note: `"Toggle mode available via tray menu (hold once to start, once to stop)."`

### Step 4 — Try it

- Heading: `"Give it a try"`
- Instruction: `"Click into any text field on your desktop, then hold Ctrl+Space and say something."`
- Secondary note: `"This window stays on top so you can see it while you dictate."` (`Topmost=True` for OnboardingWindow)
- `"Done"` button: sets `settingsService.Current.HasCompletedOnboarding = true`, calls `settingsService.Save()`, closes window

### Error handling in Step 2

If `InitializeAsync` throws (network error, disk full), show inline error message below the progress bar with a `"Retry"` button. Do not crash or advance.

---

## Files

### New
```
Windows/DynamicIslandWindow.xaml
Windows/DynamicIslandWindow.xaml.cs
Windows/OnboardingWindow.xaml
Windows/OnboardingWindow.xaml.cs
ViewModels/DynamicIslandViewModel.cs
```

### Modified
```
Models/AppSettings.cs              — add HasCompletedOnboarding bool
App.xaml.cs                        — onboarding gate + swap PillWindow → DynamicIslandWindow
Windows/MainWindow.xaml            — color swap
Views/InsightsView.xaml            — color swap
Views/SettingsView.xaml            — color swap
Converters/CountToColorConverter.cs — new heatmap ramp
```

### Deleted
```
Windows/PillWindow.xaml
Windows/PillWindow.xaml.cs
ViewModels/PillViewModel.cs
```

---

## Testing

No new xUnit tests (UI and animation are not unit-testable). Build verification after each task. Manual smoke test after Task 5:

1. Delete `settings.json` → relaunch → onboarding appears
2. Complete all 4 steps → onboarding closes, app idles in tray
3. Relaunch → onboarding does not appear
4. Hold Ctrl+Space → Dynamic Island pill appears at bottom-centre with pink waveform
5. Release → pill shows "Transcribing…" spinner → text inserts → pill disappears
6. Open MainWindow → dark #1D1D1D palette throughout, #E5BDDF accents

---

## Non-goals (deferred to later M3 tasks)

- History view
- Model manager (switch model sizes)
- Auto-learn / personal dictionary
- Command Mode
- Signed installer / auto-update
