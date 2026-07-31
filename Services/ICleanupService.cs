using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Services;

public interface ICleanupService
{
    Task<CleanupResult> CleanAsync(string rawText, string? appContext = null);
}
