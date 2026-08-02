using System.Net;
using System.Net.Http;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Tests;

public class TieredCleanupServiceTests
{
    private static TieredCleanupService GroqTiered(string responseBody, HttpStatusCode code = HttpStatusCode.OK)
    {
        var settings = new SettingsService();
        settings.Current.GroqApiKey = "gsk_test";
        settings.Current.CleanupProvider = CleanupProvider.Groq;
        var groq   = new GroqCleanupService(new HttpClient(new FakeHandler2(responseBody, code)), settings);
        var openAi = new OpenAiCleanupService(new HttpClient(new FakeHandler2("", HttpStatusCode.OK)), settings);
        return new TieredCleanupService(groq, openAi, new RegexCleanupService(), settings);
    }

    private static TieredCleanupService GroqUnavailableTiered()
    {
        var settings = new SettingsService();
        settings.Current.CleanupProvider = CleanupProvider.Groq; // no key → IsAvailable=false
        var groq   = new GroqCleanupService(new HttpClient(new FakeHandler2("", HttpStatusCode.OK)), settings);
        var openAi = new OpenAiCleanupService(new HttpClient(new FakeHandler2("", HttpStatusCode.OK)), settings);
        return new TieredCleanupService(groq, openAi, new RegexCleanupService(), settings);
    }

    [Fact]
    public async Task CleanAsync_GroqSucceeds_ReturnsGroqResult()
    {
        const string json = """{"choices":[{"message":{"content":"Groq cleaned."}}]}""";
        var result = await GroqTiered(json).CleanAsync("raw text");
        Assert.Equal("groq", result.Tier);
        Assert.Equal("Groq cleaned.", result.Text);
    }

    [Fact]
    public async Task CleanAsync_GroqThrows_FallsBackToRegex()
    {
        var result = await GroqTiered("err", HttpStatusCode.InternalServerError).CleanAsync("hello world");
        Assert.Equal("regex", result.Tier);
    }

    [Fact]
    public async Task CleanAsync_GroqUnavailable_UsesRegexDirectly()
    {
        var result = await GroqUnavailableTiered().CleanAsync("hello world");
        Assert.Equal("regex", result.Tier);
    }
}

internal class FakeHandler2 : HttpMessageHandler
{
    private readonly string _body;
    private readonly HttpStatusCode _code;
    public FakeHandler2(string body, HttpStatusCode code) { _body = body; _code = code; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage _, CancellationToken __)
        => Task.FromResult(new HttpResponseMessage(_code) { Content = new StringContent(_body) });
}
