using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using AlarmApp.Models;

namespace AlarmApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private int? _hours;
    private int? _minutes;
    private int? _seconds;
    private ObservableCollection<TimerItem> _timers = new();
    private static readonly string SavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AlarmApp",
        "timers.json");

    public MainViewModel()
    {
        AddTimerCommand = new RelayCommand(AddTimer, CanAddTimer);
        StartPauseCommand = new RelayCommand(StartPauseTimer);
        ResetCommand = new RelayCommand(ResetTimer);
        DeleteCommand = new RelayCommand(DeleteTimer);
        LoadTimers();
    }

    public int? Hours
    {
        get => _hours;
        set
        {
            if (_hours != value && (value == null || (value >= 0 && value <= 23)))
            {
                _hours = value;
                OnPropertyChanged();
            }
        }
    }

    public int? Minutes
    {
        get => _minutes;
        set
        {
            if (_minutes != value && (value == null || (value >= 0 && value <= 59)))
            {
                _minutes = value;
                OnPropertyChanged();
            }
        }
    }

    public int? Seconds
    {
        get => _seconds;
        set
        {
            if (_seconds != value && (value == null || (value >= 0 && value <= 59)))
            {
                _seconds = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<TimerItem> Timers
    {
        get => _timers;
        set
        {
            _timers = value;
            OnPropertyChanged();
        }
    }

    public ICommand AddTimerCommand { get; }
    public ICommand StartPauseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand DeleteCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool CanAddTimer()
    {
        return (Hours ?? 0) > 0 || (Minutes ?? 0) > 0 || (Seconds ?? 0) > 0;
    }

    private void AddTimer()
    {
        var totalSeconds = (Hours ?? 0) * 3600 + (Minutes ?? 0) * 60 + (Seconds ?? 0);
        if (totalSeconds <= 0) return;

        var timer = new TimerItem(totalSeconds);
        timer.RemainingSecondsChanged += Timer_RemainingSecondsChanged;
        timer.TimerCompleted += Timer_Completed;

        InsertTimerSorted(timer);
        SaveTimers();
    }

    private void InsertTimerSorted(TimerItem timer)
    {
        int index = 0;
        for (int i = 0; i < Timers.Count; i++)
        {
            if (Timers[i].TotalSeconds > timer.TotalSeconds)
            {
                index = i;
                break;
            }
            index = i + 1;
        }
        Timers.Insert(index, timer);
    }

    private void Timer_RemainingSecondsChanged(object? sender, EventArgs e)
    {
    }

    private void Timer_Completed(object? sender, EventArgs e)
    {
        // Timer completed - it stays in the list, user can reset or delete
    }

    private void SortTimers()
    {
        var sorted = Timers.OrderBy(t => t.TotalSeconds).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var currentIndex = Timers.IndexOf(sorted[i]);
            if (currentIndex != i)
            {
                Timers.Move(currentIndex, i);
            }
        }
    }

    private void StartPauseTimer(object? parameter)
    {
        if (parameter is TimerItem timer)
        {
            timer.ToggleStartPause();
        }
    }

    private void ResetTimer(object? parameter)
    {
        if (parameter is TimerItem timer)
        {
            timer.Reset();
        }
    }

    private void DeleteTimer(object? parameter)
    {
        if (parameter is TimerItem timer)
        {
            timer.Cleanup();
            timer.RemainingSecondsChanged -= Timer_RemainingSecondsChanged;
            timer.TimerCompleted -= Timer_Completed;
            Timers.Remove(timer);
            SaveTimers();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SaveOnExit()
    {
        SaveTimers();
    }

    private void SaveTimers()
    {
        try
        {
            var data = new AppData
            {
                Timers = Timers.Select(t => new TimerData
                {
                    TotalSeconds = t.TotalSeconds,
                    AlarmDurationSeconds = t.AlarmDurationSeconds
                }).ToList()
            };

            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavePath, json);
        }
        catch
        {
            // Silently ignore save errors
        }
    }

    private void LoadTimers()
    {
        try
        {
            if (!File.Exists(SavePath)) return;

            var json = File.ReadAllText(SavePath);
            var data = JsonSerializer.Deserialize<AppData>(json);

            if (data?.Timers == null) return;

            foreach (var timerData in data.Timers)
            {
                var timer = new TimerItem(timerData.TotalSeconds)
                {
                    AlarmDurationSeconds = timerData.AlarmDurationSeconds
                };
                timer.RemainingSecondsChanged += Timer_RemainingSecondsChanged;
                timer.TimerCompleted += Timer_Completed;
                InsertTimerSorted(timer);
            }
        }
        catch
        {
            // Silently ignore load errors
        }
    }
}
