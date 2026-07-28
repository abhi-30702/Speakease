using System.Text.RegularExpressions;

namespace WhisperFlowLocal.Services;

public class CleanupService
{
    private static readonly string[] CorrectionMarkers =
        ["no wait", "actually no", "i mean", "scratch that",
         "never mind", "forget that", "let me rephrase", "or rather"];

    private static readonly string[] Fillers =
        ["you know", "sort of", "kind of", "um", "uh", "er",
         "basically", "literally"];

    public string Clean(string text)
    {
        text = RemoveSelfCorrections(text);
        text = RemoveFillers(text);
        text = FixCapsAndPunct(text);
        text = NormaliseWhitespace(text);
        return text;
    }

    private static string RemoveSelfCorrections(string text)
    {
        var lower = text.ToLowerInvariant();
        int latestCut = -1;
        string? marker = null;
        foreach (var m in CorrectionMarkers)
        {
            int idx = lower.LastIndexOf(m, StringComparison.Ordinal);
            if (idx > latestCut) { latestCut = idx; marker = m; }
        }
        if (latestCut < 0 || marker is null) return text;
        var after = text[(latestCut + marker.Length)..].TrimStart();
        return after;
    }

    private static string RemoveFillers(string text)
    {
        foreach (var filler in Fillers)
            text = Regex.Replace(text, $@"\b{Regex.Escape(filler)}\b", "", RegexOptions.IgnoreCase);
        return Regex.Replace(text, @" {2,}", " ").Trim();
    }

    private static string FixCapsAndPunct(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        text = char.ToUpperInvariant(text[0]) + text[1..];
        if (!".?!,:".Contains(text[^1])) text += '.';
        return text;
    }

    private static string NormaliseWhitespace(string text)
    {
        text = text.Trim();
        text = Regex.Replace(text, @" {2,}", " ");
        text = Regex.Replace(text, @" ([,.:;!?])", "$1");
        return text;
    }
}
