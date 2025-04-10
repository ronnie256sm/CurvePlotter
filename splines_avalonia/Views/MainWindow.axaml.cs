using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        this.AddHandler(LoadedEvent, new EventHandler<RoutedEventArgs>(OnWindowLoaded), handledEventsToo: true);

        // События мыши
        GraphicHolder.PointerPressed += OnMouseDown;
        GraphicHolder.PointerMoved += OnMouseMove;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.GraphicCanvas = GraphicHolder;
        _viewModel.SetInitialCenter(GraphicHolder.Bounds.Width / 2, GraphicHolder.Bounds.Height / 2);
        _viewModel.DrawSplines();
    }

    private void OnAddSpline(object sender, RoutedEventArgs e)
    {
        string type = "Interpolating Cubic";
        var controlPoints = FileReader.ReadPoints("../../../points.txt");
        var grid = FileReader.ReadGrid("../../../mesh.txt");

        _viewModel.AddSpline(type, controlPoints, grid);
    }

    private void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        _viewModel.StartPan(e.GetPosition(this));
    }

    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(GraphicHolder);
        _viewModel.UpdateStatusBar(pos);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _viewModel.DoPan(pos);
        }
    }

    private void OnZoomIn(object sender, RoutedEventArgs e)
    {
        _viewModel.ZoomIn();
    }

    private void OnZoomOut(object sender, RoutedEventArgs e)
    {
        _viewModel.ZoomOut();
    }

    private void OnResetPosition(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetPosition();
    }
}