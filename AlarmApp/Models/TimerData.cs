namespace AlarmApp.Models;

public class TimerData
{
    public int TotalSeconds { get; set; }
    public int AlarmDurationSeconds { get; set; } = 2;
}

public class AppData
{
    public List<TimerData> Timers { get; set; } = new();
}
