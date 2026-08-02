using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Services;

public class TieredCleanupService : ICleanupService
{
    private readonly GroqCleanupService   _groq;
    private readonly OpenAiCleanupService _openAi;
    private readonly RegexCleanupService  _regex;
    private readonly SettingsService      _settings;

    public TieredCleanupService(
        GroqCleanupService groq, OpenAiCleanupService openAi,
        RegexCleanupService regex, SettingsService settings)
    {
        _groq = groq; _openAi = openAi; _regex = regex; _settings = settings;
    }

    public async Task<CleanupResult> CleanAsync(string rawText, string? appContext = null)
    {
        switch (_settings.Current.CleanupProvider)
        {
            case CleanupProvider.Groq when _groq.IsAvailable:
                try { return await _groq.CleanAsync(rawText, appContext); } catch { }
                break;

            case CleanupProvider.OpenAI when _openAi.IsAvailable:
                try { return await _openAi.CleanAsync(rawText, appContext); } catch { }
                break;
        }
        return await _regex.CleanAsync(rawText, appContext);
    }
}
