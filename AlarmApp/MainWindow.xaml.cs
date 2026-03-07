using System.Windows;
using System.Windows.Controls;
using AlarmApp.ViewModels;

namespace AlarmApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
        MaxHeight = SystemParameters.WorkArea.Height;
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
