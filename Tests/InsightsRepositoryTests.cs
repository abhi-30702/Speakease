using WhisperFlowLocal.Models;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Tests;

public class InsightsRepositoryTests
{
    private static async Task<InsightsRepository> MakeAsync()
    {
        var repo = new InsightsRepository(":memory:");
        await repo.InitAsync();
        return repo;
    }

    private static DictationRecord MakeRecord(string app = "slack", int wordCount = 10,
        double wpm = 120, string tier = "groq", int durationMs = 5000, bool ok = true)
        => new(DateTime.UtcNow, app, "Window", durationMs, wordCount, wpm,
               "raw text", "cleaned text", tier, 2, ok, 0.95);

    [Fact]
    public async Task RecordAndGetTotalWords_CountsSuccessfulOnly()
    {
        var repo = await MakeAsync();
        await repo.RecordAsync(MakeRecord(wordCount: 10, ok: true));
        await repo.RecordAsync(MakeRecord(wordCount: 5, ok: false));
        Assert.Equal(10, await repo.GetTotalWordsAsync());
    }

    [Fact]
    public async Task GetTotalFixes_SumsAllRecords()
    {
        var repo = await MakeAsync();
        await repo.RecordAsync(MakeRecord());
        await repo.RecordAsync(MakeRecord());
        Assert.Equal(4, await repo.GetTotalFixesAsync());
    }

    [Fact]
    public async Task GetAppBreakdown_GroupsByApp()
    {
        var repo = await MakeAsync();
        await repo.RecordAsync(MakeRecord(app: "slack"));
        await repo.RecordAsync(MakeRecord(app: "slack"));
        await repo.RecordAsync(MakeRecord(app: "chrome"));
        var breakdown = await repo.GetAppBreakdownAsync();
        Assert.Equal(2, breakdown.Count);
        Assert.Equal("slack", breakdown[0].AppName);
        Assert.Equal(2, breakdown[0].Count);
        Assert.True(breakdown[0].Percent > breakdown[1].Percent);
    }

    [Fact]
    public async Task GetStreakData_ReturnsCorrectDayCount()
    {
        var repo = await MakeAsync();
        await repo.RecordAsync(MakeRecord());
        var streak = await repo.GetStreakDataAsync(91);
        Assert.Equal(91, streak.Count);
        // today should have count = 1
        Assert.Equal(1, streak.Last().Count);
    }

    [Fact]
    public async Task GetVoiceStats_ComputesGroqPct()
    {
        var repo = await MakeAsync();
        await repo.RecordAsync(MakeRecord(tier: "groq"));
        await repo.RecordAsync(MakeRecord(tier: "regex"));
        var stats = await repo.GetVoiceStatsAsync();
        Assert.Equal(50.0, stats.GroqUsagePct);
    }
}
