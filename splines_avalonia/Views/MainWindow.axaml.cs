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

        this.AddHandler(LoadedEvent, new EventHandler<RoutedEventArgs>(InitializeWindow), handledEventsToo: true);
        this.SizeChanged += new EventHandler<SizeChangedEventArgs>(OnWindowLoaded);

        // События мыши
        GraphicHolder.PointerPressed += OnMouseDown;
        GraphicHolder.PointerMoved += OnMouseMove;
    }

    private void InitializeWindow(object sender, RoutedEventArgs e)
    {
        _viewModel.GraphicCanvas = GraphicHolder;
        OnWindowLoaded(sender, e);
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.SetInitialCenter(GraphicHolder.Bounds.Width / 2, GraphicHolder.Bounds.Height / 2);
        _viewModel.DrawCurves();
    }

    private void OnAddSpline(object sender, RoutedEventArgs e)
    {
        string type = "Interpolating Cubic";
        var controlPoints = FileReader.ReadPoints("../../../points.txt");
        var grid = FileReader.ReadGrid("../../../mesh.txt");

        //_viewModel.CurveList.Add(new Function("sin(x)") { Name = "Test Function" });
        _viewModel.AddSpline(type, controlPoints, grid);
        _viewModel.AddFunction("sin(x)");
        _viewModel.AddFunction("x*cos(x)");
        _viewModel.AddFunction("pow(x,2)");
        _viewModel.AddFunction("sin(x)+pow(sin(x),2)+lg(pow(x,3))+pow(log(x,3),4)");
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

    private void OnMoveLeft(object sender, RoutedEventArgs e)
    {
        _viewModel.MoveLeft();
    }

    private void OnMoveRight(object sender, RoutedEventArgs e)
    {
        _viewModel.MoveRight();
    }

    private void OnMoveUp(object sender, RoutedEventArgs e)
    {
        _viewModel.MoveUp();
    }

    private void OnMoveDown(object sender, RoutedEventArgs e)
    {
        _viewModel.MoveDown();
    }
}