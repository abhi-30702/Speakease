using System.Text.RegularExpressions;
using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Services;

public class RegexCleanupService : ICleanupService
{
    private static readonly string[] CorrectionMarkers =
        ["no wait", "actually no", "i mean", "scratch that",
         "never mind", "forget that", "let me rephrase", "or rather"];

    private static readonly string[] Fillers =
        ["you know", "sort of", "kind of", "um", "uh", "er",
         "basically", "literally"];

    private static readonly Regex FillerRegex = new(
        $@"\b({string.Join('|', Fillers.Select(Regex.Escape))})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<CleanupResult> CleanAsync(string rawText, string? appContext = null)
    {
        var raw = rawText;
        var cleaned = NormaliseWhitespace(FixCapsAndPunct(RemoveFillers(RemoveSelfCorrections(rawText))));
        int fixes = Math.Abs(CountWords(raw) - CountWords(cleaned));
        return Task.FromResult(new CleanupResult(cleaned, fixes, "regex"));
    }

    private static int CountWords(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

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
        // if marker was at the end, keep what came before it
        return after.Length > 0 ? after : text[..latestCut].TrimEnd();
    }

    private static string RemoveFillers(string text)
        => FillerRegex.Replace(text, "").Trim();

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
