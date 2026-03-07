# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build
dotnet build AlarmApp.sln

# Run
dotnet run --project AlarmApp

# Build release
dotnet build AlarmApp.sln -c Release
```

This is a single-project solution with no tests.

## Architecture

WPF desktop timer/alarm app targeting **net10.0-windows** using MVVM (no framework, hand-rolled).

**MVVM structure:**
- **Models/TimerItem.cs** — Core timer logic. Owns a `DispatcherTimer` for countdown, fires `RemainingSecondsChanged` each tick and `TimerCompleted` when alarm finishes. Alarm sound runs on a background `Task` using `Console.Beep`, cancelable via `CancellationTokenSource`. Auto-resets after alarm completes.
- **Models/TimerData.cs** — Serialization DTOs (`TimerData`, `AppData`) for JSON persistence.
- **ViewModels/MainViewModel.cs** — Manages `ObservableCollection<TimerItem>`, handles add/delete/start-pause commands. Timers are kept sorted by remaining seconds (re-sorted on each tick). Persists timer list to `%LOCALAPPDATA%/AlarmApp/timers.json` on add, delete, and window close.
- **ViewModels/RelayCommand.cs** — Standard `ICommand` implementation wrapping `CommandManager.RequerySuggested`.
- **Converters/BoolToColorConverter.cs** — Currently unused in XAML (background colors use DataTriggers instead).

**Data flow:** MainWindow.xaml instantiates `MainViewModel` as its DataContext. Timer commands (`StartPauseCommand`, `DeleteCommand`) use `CommandParameter` binding to pass the specific `TimerItem`. Enter key is bound to `AddTimerCommand` via `InputBindings`. Window closing triggers `SaveOnExit()`.

**Persistence:** Timers save as JSON (original duration + alarm duration only, not remaining time). On load, all timers restore in paused state at their original duration.
