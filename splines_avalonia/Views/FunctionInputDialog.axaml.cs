using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views;

public partial class FunctionInputDialog : Window
{
    public string FunctionString { get; private set; }

    public FunctionInputDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        FunctionString = FunctionInput.Text;
        Close(FunctionString);
    }
}
