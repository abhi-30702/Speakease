using System.IO;
using Microsoft.Data.Sqlite;
using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Services;

// ponytail: single persistent connection — avoids :memory: DB-per-connection problem and removes open/close overhead per query
public class InsightsRepository : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _conn;

    public InsightsRepository(string dbPath) => _dbPath = dbPath;

    public async Task InitAsync()
    {
        if (_dbPath != ":memory:")
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

        _conn = new SqliteConnection($"Data Source={_dbPath}");
        await _conn.OpenAsync();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS dictations (
                id             INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp      TEXT    NOT NULL,
                app_name       TEXT    NOT NULL,
                app_title      TEXT    NOT NULL,
                duration_ms    INTEGER NOT NULL,
                word_count     INTEGER NOT NULL,
                wpm            REAL    NOT NULL,
                raw_text       TEXT    NOT NULL,
                cleaned_text   TEXT    NOT NULL,
                cleanup_tier   TEXT    NOT NULL,
                fixes_count    INTEGER NOT NULL,
                insertion_ok   INTEGER NOT NULL,
                avg_confidence REAL    NOT NULL DEFAULT 0
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private SqliteConnection Conn => _conn ?? throw new InvalidOperationException("Call InitAsync first.");

    public async Task RecordAsync(DictationRecord r)
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dictations
            (timestamp, app_name, app_title, duration_ms, word_count, wpm,
             raw_text, cleaned_text, cleanup_tier, fixes_count, insertion_ok, avg_confidence)
            VALUES
            ($ts, $app, $title, $dur, $wc, $wpm, $raw, $clean, $tier, $fixes, $ok, $conf)
            """;
        cmd.Parameters.AddWithValue("$ts",    r.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("$app",   r.AppName);
        cmd.Parameters.AddWithValue("$title", r.AppTitle);
        cmd.Parameters.AddWithValue("$dur",   r.DurationMs);
        cmd.Parameters.AddWithValue("$wc",    r.WordCount);
        cmd.Parameters.AddWithValue("$wpm",   r.Wpm);
        cmd.Parameters.AddWithValue("$raw",   r.RawText);
        cmd.Parameters.AddWithValue("$clean", r.CleanedText);
        cmd.Parameters.AddWithValue("$tier",  r.CleanupTier);
        cmd.Parameters.AddWithValue("$fixes", r.FixesCount);
        cmd.Parameters.AddWithValue("$ok",    r.InsertionOk ? 1 : 0);
        cmd.Parameters.AddWithValue("$conf",  r.AvgConfidence);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetTotalWordsAsync()
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(word_count), 0) FROM dictations WHERE insertion_ok = 1";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<double> GetTodayWpmAsync()
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(AVG(wpm), 0) FROM dictations WHERE date(timestamp) = date('now')";
        return Convert.ToDouble(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> GetTotalFixesAsync()
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(fixes_count), 0) FROM dictations";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<List<AppUsageItem>> GetAppBreakdownAsync()
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "SELECT app_name, COUNT(*) FROM dictations GROUP BY app_name ORDER BY COUNT(*) DESC LIMIT 10";
        var rows = new List<(string, int)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add((reader.GetString(0), reader.GetInt32(1)));
        int total = rows.Sum(x => x.Item2);
        return rows.Select(x => new AppUsageItem(x.Item1, x.Item2, total > 0 ? x.Item2 * 100.0 / total : 0)).ToList();
    }

    public async Task<List<StreakDayItem>> GetStreakDataAsync(int days = 91)
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT date(timestamp) as d, COUNT(*) as cnt
            FROM dictations
            WHERE timestamp >= datetime('now', '-{days} days')
            GROUP BY date(timestamp)
            """;
        var dict = new Dictionary<string, int>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            dict[reader.GetString(0)] = reader.GetInt32(1);

        return Enumerable.Range(0, days)
            .Select(i => DateTime.UtcNow.Date.AddDays(-days + 1 + i))
            .Select(d => new StreakDayItem(d, dict.GetValueOrDefault(d.ToString("yyyy-MM-dd"), 0)))
            .ToList();
    }

    public async Task<VoiceStats> GetVoiceStatsAsync()
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "SELECT duration_ms, wpm, cleanup_tier, avg_confidence FROM dictations";
        var durations = new List<double>();
        var wpms     = new List<double>();
        var confs    = new List<double>();
        int groqCount = 0;
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            durations.Add(reader.GetDouble(0) / 1000.0);
            wpms.Add(reader.GetDouble(1));
            confs.Add(reader.GetDouble(3));
            if (reader.GetString(2) == "groq") groqCount++;
        }
        double avgDur  = durations.Count > 0 ? durations.Average() : 0;
        double avgConf = confs.Count > 0 ? confs.Average() * 100.0 : 0;
        double avgWpm  = wpms.Count > 0 ? wpms.Average() : 0;
        double stdDev  = wpms.Count > 1
            ? Math.Sqrt(wpms.Select(w => Math.Pow(w - avgWpm, 2)).Average())
            : 0;
        double groqPct = wpms.Count > 0 ? groqCount * 100.0 / wpms.Count : 0;
        return new VoiceStats(avgDur, avgConf, stdDev, groqPct);
    }

    public void Dispose() => _conn?.Dispose();
}
