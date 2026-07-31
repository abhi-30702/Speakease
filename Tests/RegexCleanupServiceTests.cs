using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Tests;

public class RegexCleanupServiceTests
{
    private readonly RegexCleanupService _svc = new();

    [Fact] public async Task StripsUm() { var r = await _svc.CleanAsync("um hello there"); Assert.Equal("Hello there.", r.Text); }
    [Fact] public async Task StripsYouKnow() { var r = await _svc.CleanAsync("you know it works"); Assert.Equal("It works.", r.Text); }
    [Fact] public async Task SelfCorrectionNoWait() { var r = await _svc.CleanAsync("send to John no wait send to Sarah"); Assert.Equal("Send to Sarah.", r.Text); }
    [Fact] public async Task SelfCorrectionScratchThat() { var r = await _svc.CleanAsync("email the report scratch that call me first"); Assert.Equal("Call me first.", r.Text); }
    [Fact] public async Task CapitalisesFirst() { var r = await _svc.CleanAsync("hello world"); Assert.True(r.Text[0] == 'H'); }
    [Fact] public async Task AddsPeriod() { var r = await _svc.CleanAsync("hello world"); Assert.EndsWith(".", r.Text); }
    [Fact] public async Task NoDoublePeriod() { var r = await _svc.CleanAsync("hello world."); Assert.DoesNotContain("..", r.Text); }
    [Fact] public async Task PreservesQuestion() { var r = await _svc.CleanAsync("what time is it?"); Assert.EndsWith("?", r.Text); }
    [Fact] public async Task NoDoubleSpaces() { var r = await _svc.CleanAsync("hello   world"); Assert.DoesNotContain("  ", r.Text); }
    [Fact] public async Task NoSpaceBeforeComma() { var r = await _svc.CleanAsync("yes , I agree"); Assert.DoesNotContain(" ,", r.Text); }
    [Fact] public async Task TierIsRegex() { var r = await _svc.CleanAsync("hello"); Assert.Equal("regex", r.Tier); }
    [Fact] public async Task FixesCountIsNonNegative() { var r = await _svc.CleanAsync("um uh hello world"); Assert.True(r.FixesCount >= 0); }
}
