using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Services;

public class TieredCleanupService : ICleanupService
{
    private readonly GroqCleanupService _groq;
    private readonly RegexCleanupService _regex;

    public TieredCleanupService(GroqCleanupService groq, RegexCleanupService regex)
    {
        _groq = groq;
        _regex = regex;
    }

    public async Task<CleanupResult> CleanAsync(string rawText, string? appContext = null)
    {
        if (_groq.IsAvailable)
        {
            try { return await _groq.CleanAsync(rawText, appContext); }
            catch { /* fall through to regex */ }
        }
        return await _regex.CleanAsync(rawText, appContext);
    }
}
