using System.Net;
using System.Net.Http;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Tests;

public class GroqCleanupServiceTests
{
    private static GroqCleanupService Make(string responseBody, HttpStatusCode code = HttpStatusCode.OK)
    {
        var http = new HttpClient(new FakeHandler(responseBody, code));
        var settings = new SettingsService();
        settings.Current.GroqApiKey = "gsk_test";
        return new GroqCleanupService(http, settings);
    }

    [Fact]
    public async Task CleanAsync_ValidResponse_ReturnsParsedText()
    {
        const string json = """{"choices":[{"message":{"content":"Hello world."}}]}""";
        var result = await Make(json).CleanAsync("um hello world");
        Assert.Equal("Hello world.", result.Text);
        Assert.Equal("groq", result.Tier);
    }

    [Fact]
    public async Task CleanAsync_ServerError_Throws()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Make("error", HttpStatusCode.InternalServerError).CleanAsync("test"));
    }

    [Fact]
    public void IsAvailable_NoKey_ReturnsFalse()
    {
        var http = new HttpClient(new FakeHandler("", HttpStatusCode.OK));
        var settings = new SettingsService(); // no key set
        Assert.False(new GroqCleanupService(http, settings).IsAvailable);
    }

    [Fact]
    public void IsAvailable_WithKey_ReturnsTrue()
    {
        var http = new HttpClient(new FakeHandler("", HttpStatusCode.OK));
        var settings = new SettingsService();
        settings.Current.GroqApiKey = "gsk_test";
        Assert.True(new GroqCleanupService(http, settings).IsAvailable);
    }

    [Fact]
    public async Task CleanAsync_FixesCountIsAbsDelta()
    {
        // raw = 3 words, cleaned = 2 words → fixes = 1
        const string json = """{"choices":[{"message":{"content":"Hello world."}}]}""";
        var result = await Make(json).CleanAsync("um hello world");
        Assert.Equal(1, result.FixesCount); // "um hello world" (3) vs "Hello world." (2) = 1
    }
}

internal class FakeHandler : HttpMessageHandler
{
    private readonly string _body;
    private readonly HttpStatusCode _code;
    public FakeHandler(string body, HttpStatusCode code) { _body = body; _code = code; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage _, CancellationToken __)
        => Task.FromResult(new HttpResponseMessage(_code) { Content = new StringContent(_body) });
}
