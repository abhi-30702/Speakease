using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.ViewModels;

public partial class InsightsViewModel : ObservableObject
{
    private readonly InsightsRepository _repo;

    [ObservableProperty] private int _totalWords;
    [ObservableProperty] private double _todayWpm;
    [ObservableProperty] private int _totalFixes;
    [ObservableProperty] private ObservableCollection<AppUsageItem> _appBreakdown = [];
    [ObservableProperty] private ObservableCollection<StreakDayItem> _streakDays = [];
    [ObservableProperty] private double _avgDurationSec;
    [ObservableProperty] private string _wpmConsistency = "—";
    [ObservableProperty] private double _groqUsagePct;
    [ObservableProperty] private bool _isLoading;

    public InsightsViewModel(InsightsRepository repo) => _repo = repo;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            TotalWords   = await _repo.GetTotalWordsAsync();
            TodayWpm     = Math.Round(await _repo.GetTodayWpmAsync(), 0);
            TotalFixes   = await _repo.GetTotalFixesAsync();

            AppBreakdown = new ObservableCollection<AppUsageItem>(await _repo.GetAppBreakdownAsync());
            StreakDays   = new ObservableCollection<StreakDayItem>(await _repo.GetStreakDataAsync(91));

            var voice = await _repo.GetVoiceStatsAsync();
            AvgDurationSec  = Math.Round(voice.AvgDurationSec, 1);
            GroqUsagePct    = Math.Round(voice.GroqUsagePct, 0);
            WpmConsistency  = voice.WpmStdDev switch
            {
                < 20  => "High",
                < 50  => "Medium",
                _     => "Low"
            };
        }
        finally { IsLoading = false; }
    }
}
