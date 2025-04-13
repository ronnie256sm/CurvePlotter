using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views;

public partial class AddCurveDialog : Window
{
    public enum CurveType { InterpolatingSpline, SmoothingSpline }

    public CurveType? Result { get; private set; }

    public AddCurveDialog()
    {
        InitializeComponent();
    }

    private void OnInterpolatingClick(object? sender, RoutedEventArgs e)
    {
        Result = CurveType.InterpolatingSpline;
        Close(Result);
    }

    private void OnSmoothingClick(object? sender, RoutedEventArgs e)
    {
        Result = CurveType.SmoothingSpline;
        Close(Result);
    }
}
