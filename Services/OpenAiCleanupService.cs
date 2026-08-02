using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Services;

public class OpenAiCleanupService : ICleanupService
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private const string SystemPrompt =
        "You are a dictation cleanup assistant. Given raw speech-to-text output, return only the cleaned text — no explanation, no preamble.\n\n" +
        "Rules:\n" +
        "1. Remove filler words: um, uh, er, you know, sort of, kind of, basically, literally.\n" +
        "2. Resolve self-corrections: if the speaker says \"no wait\", \"actually no\", \"scratch that\", \"never mind\", \"forget that\", \"let me rephrase\", or \"or rather\", discard everything before that phrase and keep what comes after.\n" +
        "3. Fix capitalisation: first word of each sentence capitalised, proper nouns.\n" +
        "4. Add punctuation where natural pauses suggest it.\n" +
        "5. Fix grammar and word order lightly — preserve the speaker's voice.\n" +
        "6. Do not add words, summarise, or change meaning.";

    private readonly HttpClient _http;
    private readonly SettingsService _settings;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_settings.Current.OpenAiApiKey);

    public OpenAiCleanupService(HttpClient http, SettingsService settings)
    {
        _http     = http;
        _settings = settings;
    }

    public async Task<CleanupResult> CleanAsync(string rawText, string? appContext = null)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.Current.OpenAiApiKey);
        request.Content = JsonContent.Create(new
        {
            model    = _settings.Current.OpenAiModel,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user",   content = rawText }
            },
            temperature = 0,
            max_tokens  = 1024
        });

        var response = await _http.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
        var cleaned = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? rawText;

        int fixes = Math.Abs(CountWords(rawText) - CountWords(cleaned));
        return new CleanupResult(cleaned.Trim(), fixes, "openai");
    }

    private static int CountWords(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
