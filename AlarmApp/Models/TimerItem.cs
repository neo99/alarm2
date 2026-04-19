using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace AlarmApp.Models;

public class TimerItem : INotifyPropertyChanged
{
    [DllImport("winmm.dll")]
    private static extern int midiOutOpen(out IntPtr handle, int deviceID, IntPtr callback, IntPtr instance, int flags);

    [DllImport("winmm.dll")]
    private static extern int midiOutShortMsg(IntPtr handle, int message);

    [DllImport("winmm.dll")]
    private static extern int midiOutClose(IntPtr handle);

    // Für Elise opening melody: (MIDI note, duration ms), 0 = rest
    private static readonly (int Note, int Ms)[] FurElise =
    [
        // Pickup
        (76, 180), (75, 180),
        // E5 D#5 E5 B4 D5 C5
        (76, 180), (75, 180), (76, 180), (71, 180), (74, 180), (72, 180),
        // A4 (held), rest, C4 E4 A4
        (69, 360), (0, 180), (60, 180), (64, 180), (69, 180),
        // B4 (held), rest, E4 G#4 B4
        (71, 360), (0, 180), (64, 180), (68, 180), (71, 180),
        // C5 (held), rest, E4 E5 D#5
        (72, 360), (0, 180), (64, 180), (76, 180), (75, 180),
        // E5 D#5 E5 B4 D5 C5
        (76, 180), (75, 180), (76, 180), (71, 180), (74, 180), (72, 180),
        // A4 (held), rest, C4 E4 A4
        (69, 360), (0, 180), (60, 180), (64, 180), (69, 180),
        // B4 (held), rest, E4 C5 B4
        (71, 360), (0, 180), (64, 180), (72, 180), (71, 180),
        // A4 (ending)
        (69, 540),
    ];

    private int _remainingSeconds;
    private int _alarmDurationSeconds = 2;
    private bool _isRunning;
    private bool _isRinging;
    private string _shortcut = "";
    private readonly DispatcherTimer _timer;
    private CancellationTokenSource? _alarmCts;

    public TimerItem(int totalSeconds)
    {
        TotalSeconds = totalSeconds;
        _remainingSeconds = totalSeconds;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
    }

    public int TotalSeconds { get; }

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        private set
        {
            if (_remainingSeconds != value)
            {
                _remainingSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RemainingDisplay));
                RemainingSecondsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public int AlarmDurationSeconds
    {
        get => _alarmDurationSeconds;
        set
        {
            if (_alarmDurationSeconds != value && value >= 1 && value <= 60)
            {
                _alarmDurationSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StartPauseText));
                OnPropertyChanged(nameof(StartPauseWithShortcut));
            }
        }
    }

    public bool IsRinging
    {
        get => _isRinging;
        private set
        {
            if (_isRinging != value)
            {
                _isRinging = value;
                OnPropertyChanged();
            }
        }
    }

    public string RemainingDisplay
    {
        get
        {
            var ts = TimeSpan.FromSeconds(RemainingSeconds);
            return ts.Hours > 0
                ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    public string OriginalDisplay
    {
        get
        {
            var ts = TimeSpan.FromSeconds(TotalSeconds);
            return ts.Hours > 0
                ? $"({ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2})"
                : $"({ts.Minutes:D2}:{ts.Seconds:D2})";
        }
    }

    public string Shortcut
    {
        get => _shortcut;
        set
        {
            if (_shortcut != value)
            {
                _shortcut = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StartPauseWithShortcut));
            }
        }
    }

    public string StartPauseText => IsRunning ? "Pause" : "Start";

    public string StartPauseWithShortcut =>
        string.IsNullOrEmpty(_shortcut) ? StartPauseText : $"{StartPauseText} [{_shortcut}]";

    public event EventHandler? RemainingSecondsChanged;
    public event EventHandler? TimerCompleted;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Start()
    {
        if (RemainingSeconds > 0 && !IsRinging)
        {
            _timer.Start();
            IsRunning = true;
        }
    }

    public void Pause()
    {
        _timer.Stop();
        IsRunning = false;
    }

    public void ToggleStartPause()
    {
        if (IsRunning)
            Pause();
        else
            Start();
    }

    public void Reset()
    {
        StopAlarm();
        Pause();
        RemainingSeconds = TotalSeconds;
    }

    public void StopAlarm()
    {
        _alarmCts?.Cancel();
        IsRinging = false;
    }

    public void Cleanup()
    {
        _timer.Stop();
        StopAlarm();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (RemainingSeconds > 0)
        {
            RemainingSeconds--;

            if (RemainingSeconds == 0)
            {
                Pause();
                TriggerAlarm();
            }
        }
    }

    private void TriggerAlarm()
    {
        IsRinging = true;
        _alarmCts = new CancellationTokenSource();
        var token = _alarmCts.Token;
        var duration = AlarmDurationSeconds;

        Task.Run(() =>
        {
            int velocity = 80;
            IntPtr midi;
            bool midiOpened = midiOutOpen(out midi, 0, IntPtr.Zero, IntPtr.Zero, 0) == 0;

            var endTime = DateTime.Now.AddSeconds(duration);
            int i = 0;
            int lastNote = 0;
            while (DateTime.Now < endTime && !token.IsCancellationRequested)
            {
                try
                {
                    if (midiOpened)
                    {
                        var (note, ms) = FurElise[i % FurElise.Length];
                        // Release previous note
                        if (lastNote > 0)
                            midiOutShortMsg(midi, 0x80 | (lastNote << 8));
                        if (note > 0)
                        {
                            midiOutShortMsg(midi, 0x90 | (note << 8) | (velocity << 16));
                            lastNote = note;
                        }
                        else
                        {
                            lastNote = 0;
                        }
                        Thread.Sleep(ms);
                        i++;
                    }
                    else
                    {
                        Console.Beep(1000, 200);
                        Thread.Sleep(100);
                    }
                }
                catch { break; }
            }

            if (midiOpened)
            {
                if (lastNote > 0)
                    midiOutShortMsg(midi, 0x80 | (lastNote << 8));
                midiOutClose(midi);
            }

            // Wait 1 second after alarm finishes
            if (!token.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsRinging = false;
                Reset();
                TimerCompleted?.Invoke(this, EventArgs.Empty);
            });
        }, token);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
