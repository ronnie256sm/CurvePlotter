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
        this.PointerWheelChanged += OnMouseWheel;
        this.PointerMoved += OnMouseMove;
        this.PointerPressed += OnMouseDown;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.GraphicCanvas = GraphicHolder;
        _viewModel.DrawSplines();
    }

    private void OnAddSpline(object sender, RoutedEventArgs e)
    {
        string type = "Interpolating Cubic";
        var controlPoints = FileReader.ReadPoints("../../../points.txt");
        var grid = FileReader.ReadGrid("../../../mesh.txt");

        _viewModel.AddSpline(type, controlPoints, grid);
    }

    private void OnMouseWheel(object? sender, PointerWheelEventArgs e)
    {
        _viewModel.HandleZoom(e.Delta.Y);
    }

    private void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        _viewModel.StartPan(e.GetPosition(this));
    }

    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _viewModel.DoPan(e.GetPosition(this));
        }
    }
}