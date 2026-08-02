using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Models;

public partial class DictationEngine : ObservableObject
{
    private readonly AudioCaptureService _audio;
    private readonly TranscriptionService _transcription;
    private readonly ICleanupService _cleanup;
    private readonly InsertionService _insertion;
    private readonly FocusService _focus;
    private readonly InsightsRepository _insights;
    private readonly Stopwatch _recordingTimer = new();
    private string _currentAppName = string.Empty;
    private string _currentAppTitle = string.Empty;

    [ObservableProperty] private RecordingState _state = RecordingState.Idle;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _toggleMode = false;

    public event Action? DictationCompleted;

    public DictationEngine(
        AudioCaptureService audio,
        TranscriptionService transcription,
        ICleanupService cleanup,
        InsertionService insertion,
        FocusService focus,
        InsightsRepository insights)
    {
        _audio = audio; _transcription = transcription;
        _cleanup = cleanup; _insertion = insertion;
        _focus = focus; _insights = insights;
    }

    public void OnHotkeyPressed()
    {
        if (ToggleMode)
        {
            if (State == RecordingState.Idle) StartRecording();
            else if (State == RecordingState.Recording) _ = EndRecordingAsync();
        }
        else
        {
            if (State.CanStartRecording()) StartRecording();
        }
    }

    public void OnHotkeyReleased()
    {
        if (!ToggleMode && State == RecordingState.Recording)
            _ = EndRecordingAsync();
    }

    private void StartRecording()
    {
        _focus.CaptureForegroundWindow();
        _currentAppName = _focus.GetForegroundAppName();
        _currentAppTitle = _focus.GetForegroundWindowTitle();
        _recordingTimer.Restart();
        _audio.Start();
        State = RecordingState.Recording;
    }

    private async Task EndRecordingAsync()
    {
        State = RecordingState.Transcribing;
        try
        {
            var pcm = _audio.Stop();
            var durationMs = (int)_recordingTimer.ElapsedMilliseconds;
            var transcriptionResult = await _transcription.TranscribeAsync(pcm);
            var cleanupResult = await _cleanup.CleanAsync(transcriptionResult.Text, _currentAppName);

            State = RecordingState.Inserting;
            bool ok = _insertion.Insert(cleanupResult.Text);
            if (!ok)
                throw new InvalidOperationException("SendInput returned 0 — insertion blocked by target app");

            State = RecordingState.Idle;

            try
            {
                var wordCount = CountWords(cleanupResult.Text);
                var wpm = durationMs > 0 ? wordCount / (durationMs / 60000.0) : 0;
                await _insights.RecordAsync(new DictationRecord(
                    DateTime.UtcNow, _currentAppName, _currentAppTitle,
                    durationMs, wordCount, wpm,
                    transcriptionResult.Text, cleanupResult.Text,
                    cleanupResult.Tier, cleanupResult.FixesCount,
                    true, transcriptionResult.AvgConfidence));
                DictationCompleted?.Invoke();
            }
            catch { /* logging failure must never surface to user */ }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            State = RecordingState.Error;
            await Task.Delay(3000);
            State = RecordingState.Idle;
        }
    }

    private static int CountWords(string s) =>
        s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
