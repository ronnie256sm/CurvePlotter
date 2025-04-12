using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views;

public partial class SplineInputDialog : Window
{
    public string PointsFile { get; private set; }
    public string MeshFile { get; private set; }

    public SplineInputDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        PointsFile = PointsFilePath.Text;
        MeshFile = MeshFilePath.Text;
        Close();
    }
}
