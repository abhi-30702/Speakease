using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace WhisperFlowLocal.Services;

public static class UpdateChecker
{
    private static readonly HttpClient _http = new();

    static UpdateChecker()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("WhisperFlowLocal/1.0");
    }

    /// <summary>Returns (true, "v1.2.3") when a newer release exists, else (false, "").</summary>
    public static async Task<(bool Available, string LatestTag)> CheckAsync()
    {
        try
        {
            var release = await _http.GetFromJsonAsync<GhRelease>(
                "https://api.github.com/repos/abhi-30702/Speakease/releases/latest");

            if (release?.TagName is null) return (false, "");

            var latest  = Version.Parse(release.TagName.TrimStart('v'));
            var current = Assembly.GetEntryAssembly()!.GetName().Version!;
            var trimmed = new Version(current.Major, current.Minor, current.Build);

            return (latest > trimmed, release.TagName);
        }
        catch
        {
            return (false, "");
        }
    }

    private sealed class GhRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
    }
}
