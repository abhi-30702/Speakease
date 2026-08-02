using WhisperFlowLocal.Interop;
using WhisperFlowLocal.Services;
using Xunit;

namespace WhisperFlowLocal.Tests;

public class InsertionServiceTests
{
    [Fact]
    public void BuildInputArray_EmptyString_ReturnsEmptyArray()
    {
        var inputs = InsertionService.BuildInputArray("");
        Assert.Empty(inputs);
    }

    [Fact]
    public void BuildInputArray_SingleChar_ReturnsTwoInputs()
    {
        var inputs = InsertionService.BuildInputArray("a");
        Assert.Equal(2, inputs.Length);
    }

    [Fact]
    public void BuildInputArray_ThreeChars_ReturnsSixInputs()
    {
        var inputs = InsertionService.BuildInputArray("abc");
        Assert.Equal(6, inputs.Length);
    }

    [Fact]
    public void BuildInputArray_SetsUnicodeFlagOnKeyDown()
    {
        var inputs = InsertionService.BuildInputArray("A");
        var keyDown = inputs[0];
        Assert.Equal(NativeMethods.INPUT_KEYBOARD, keyDown.type);
        Assert.Equal(NativeMethods.KEYEVENTF_UNICODE, keyDown.U.ki.dwFlags & NativeMethods.KEYEVENTF_UNICODE);
    }

    [Fact]
    public void BuildInputArray_SetsKeyUpFlag()
    {
        var inputs = InsertionService.BuildInputArray("A");
        var keyUp = inputs[1];
        Assert.Equal(NativeMethods.KEYEVENTF_KEYUP, keyUp.U.ki.dwFlags & NativeMethods.KEYEVENTF_KEYUP);
    }

    [Fact]
    public void BuildInputArray_SetsScanToCharValue()
    {
        var inputs = InsertionService.BuildInputArray("H");
        Assert.Equal((ushort)'H', inputs[0].U.ki.wScan);
    }

    [Fact]
    public void StripControlChars_RemovesEscapeAndBackspace()
    {
        var result = InsertionService.StripControlChars("hello\x1b\x08world");
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void StripControlChars_ReplacesNewlineWithSpace()
    {
        var result = InsertionService.StripControlChars("hello\nworld");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void StripControlChars_AllowsPrintableUnicode()
    {
        var result = InsertionService.StripControlChars("café résumé");
        Assert.Equal("café résumé", result);
    }
}
