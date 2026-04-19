using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlarmApp.ViewModels;

namespace AlarmApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
        MaxHeight = SystemParameters.WorkArea.Height;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void NumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var focused = Keyboard.FocusedElement;
        bool isNumberBox = focused == HoursBox || focused == MinutesBox || focused == SecondsBox;
        if (focused is TextBox && !isNumberBox) return;
        if (DataContext is not MainViewModel vm) return;

        char? letter = e.Key switch
        {
            >= Key.A and <= Key.Z => (char)('a' + (e.Key - Key.A)),
            _ => null
        };

        if (letter is not null)
        {
            vm.ToggleTimerByShortcut(letter.Value);
            e.Handled = true;
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SaveOnExit();
        }
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Dispatcher.BeginInvoke(() => textBox.SelectAll());
        }
    }
}
