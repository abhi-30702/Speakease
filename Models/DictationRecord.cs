namespace WhisperFlowLocal.Models;

public record DictationRecord(
    DateTime Timestamp,
    string AppName,
    string AppTitle,
    int DurationMs,
    int WordCount,
    double Wpm,
    string RawText,
    string CleanedText,
    string CleanupTier,
    int FixesCount,
    bool InsertionOk,
    double AvgConfidence);
