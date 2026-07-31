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

    [ObservableProperty] private RecordingState _state = RecordingState.Idle;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _toggleMode = false;

    public DictationEngine(
        AudioCaptureService audio,
        TranscriptionService transcription,
        ICleanupService cleanup,
        InsertionService insertion,
        FocusService focus)
    {
        _audio = audio; _transcription = transcription;
        _cleanup = cleanup; _insertion = insertion; _focus = focus;
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
        _focus.CaptureForegroudWindow();
        _audio.Start();
        State = RecordingState.Recording;
    }

    private async Task EndRecordingAsync()
    {
        State = RecordingState.Transcribing;
        try
        {
            var pcm = _audio.Stop();
            var raw = await _transcription.TranscribeAsync(pcm);
            var cleanupResult = await _cleanup.CleanAsync(raw);

            State = RecordingState.Inserting;
            bool ok = _insertion.Insert(cleanupResult.Text);
            if (!ok) throw new InvalidOperationException("SendInput returned 0 — insertion blocked by target app");

            State = RecordingState.Idle;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            State = RecordingState.Error;
            await Task.Delay(3000);
            State = RecordingState.Idle;
        }
    }
}
