namespace WhisperFlowLocal.Models;

public enum RecordingState
{
    Idle,
    Recording,
    Transcribing,
    Inserting,
    Error
}

public static class RecordingStateExtensions
{
    public static bool CanStartRecording(this RecordingState s) => s == RecordingState.Idle;
}
