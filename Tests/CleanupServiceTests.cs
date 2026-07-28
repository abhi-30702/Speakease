using WhisperFlowLocal.Services;
using Xunit;

namespace WhisperFlowLocal.Tests;

public class CleanupServiceTests
{
    private readonly CleanupService _svc = new();

    [Fact] public void StripsUm() => Assert.Equal("Hello there.", _svc.Clean("um hello there"));
    [Fact] public void StripsYouKnow() => Assert.Equal("It works.", _svc.Clean("you know it works"));
    [Fact] public void SelfCorrectionNoWait() => Assert.Equal("Send to Sarah.", _svc.Clean("send to John no wait send to Sarah"));
    [Fact] public void SelfCorrectionScratchThat() => Assert.Equal("Call me first.", _svc.Clean("email the report scratch that call me first"));
    [Fact] public void CapitalisesFirst() => Assert.True(_svc.Clean("hello world")[0] == 'H');
    [Fact] public void AddsPeriod() => Assert.EndsWith(".", _svc.Clean("hello world"));
    [Fact] public void NoDoublePeriod() => Assert.DoesNotContain("..", _svc.Clean("hello world."));
    [Fact] public void PreservesQuestion() => Assert.EndsWith("?", _svc.Clean("what time is it?"));
    [Fact] public void NoDoubleSpaces() => Assert.DoesNotContain("  ", _svc.Clean("hello   world"));
    [Fact] public void NoSpaceBeforeComma() => Assert.DoesNotContain(" ,", _svc.Clean("yes , I agree"));
}
