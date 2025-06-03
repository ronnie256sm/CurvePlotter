using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using CurvePlotter.ViewModels;

namespace CurvePlotter.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        
        DataContext = new MainWindowViewModel();
        this.AddHandler(LoadedEvent, InitializeWindow, handledEventsToo: true);
        this.SizeChanged += OnWindowLoaded;

        GraphicHolder.PointerWheelChanged += OnMouseWheel;
        GraphicHolder.PointerPressed += OnMouseDown;
        GraphicHolder.PointerMoved += OnMouseMove;
    }

    private void InitializeWindow(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.GraphicCanvas = GraphicHolder;
            OnWindowLoaded(sender, null);
        }
    }

    private void OnWindowLoaded(object? sender, SizeChangedEventArgs? e)
    {
        ViewModel?.SetInitialCenter(GraphicHolder.Bounds.Width / 2, GraphicHolder.Bounds.Height / 2);
        ViewModel?.DrawCurves();
    }

    private void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.StartPan(e.GetPosition(this));
    }

    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        if (ViewModel == null) return;
        
        var pos = e.GetPosition(GraphicHolder);
        ViewModel.UpdateStatusBar(pos);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ViewModel.DoPan(pos);
        }
    }

    private void OnMouseWheel(object? sender, PointerWheelEventArgs e)
    {
        ViewModel?.HandleZoom(e.Delta.Y);
    }

    private void OnZoomIn(object sender, RoutedEventArgs e)
    {
        ViewModel?.ZoomIn();
    }

    private void OnZoomOut(object sender, RoutedEventArgs e)
    {
        ViewModel?.ZoomOut();
    }

    private void OnResetPosition(object sender, RoutedEventArgs e)
    {
        ViewModel?.ResetPosition();
    }

    private void OnMoveLeft(object sender, RoutedEventArgs e)
    {
        ViewModel?.MoveLeft();
    }

    private void OnMoveRight(object sender, RoutedEventArgs e)
    {
        ViewModel?.MoveRight();
    }

    private void OnMoveUp(object sender, RoutedEventArgs e)
    {
        ViewModel?.MoveUp();
    }

    private void OnMoveDown(object sender, RoutedEventArgs e)
    {
        ViewModel?.MoveDown();
    }

    private void OpenHelpWindow(object sender, RoutedEventArgs e)
    {
        var helpWindow = new HelpWindow();
        helpWindow.Show();
    }
}