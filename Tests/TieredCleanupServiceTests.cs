using System.Net;
using System.Net.Http;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Tests;

public class TieredCleanupServiceTests
{
    private static GroqCleanupService GroqWith(string responseBody, HttpStatusCode code = HttpStatusCode.OK)
    {
        var settings = new SettingsService();
        settings.Current.GroqApiKey = "gsk_test";
        return new GroqCleanupService(new HttpClient(new FakeHandler2(responseBody, code)), settings);
    }

    private static GroqCleanupService GroqUnavailable()
    {
        var settings = new SettingsService(); // no key → IsAvailable = false
        return new GroqCleanupService(new HttpClient(new FakeHandler2("", HttpStatusCode.OK)), settings);
    }

    [Fact]
    public async Task CleanAsync_GroqSucceeds_ReturnsGroqResult()
    {
        const string json = """{"choices":[{"message":{"content":"Groq cleaned."}}]}""";
        var svc = new TieredCleanupService(GroqWith(json), new RegexCleanupService());
        var result = await svc.CleanAsync("raw text");
        Assert.Equal("groq", result.Tier);
        Assert.Equal("Groq cleaned.", result.Text);
    }

    [Fact]
    public async Task CleanAsync_GroqThrows_FallsBackToRegex()
    {
        var svc = new TieredCleanupService(
            GroqWith("err", HttpStatusCode.InternalServerError),
            new RegexCleanupService());
        var result = await svc.CleanAsync("hello world");
        Assert.Equal("regex", result.Tier);
    }

    [Fact]
    public async Task CleanAsync_GroqUnavailable_UsesRegexDirectly()
    {
        var svc = new TieredCleanupService(GroqUnavailable(), new RegexCleanupService());
        var result = await svc.CleanAsync("hello world");
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
