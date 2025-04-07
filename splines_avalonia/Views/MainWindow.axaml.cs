using System;
using System.IO;
using Avalonia.Controls;
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

        // Подписка на событие загрузки окна
        this.AddHandler(LoadedEvent, new EventHandler<RoutedEventArgs>(OnWindowLoaded), handledEventsToo: true);
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // После загрузки окна передаем GraphicCanvas в ViewModel
        _viewModel.GraphicCanvas = GraphicHolder;
    }

    private void OnAddSpline(object sender, RoutedEventArgs e)
    {
        // Пример создания сплайна
        string type = "Interpolating Cubic";
        var controlPoints = FileReader.ReadPoints("../../../points.txt");
        var grid = FileReader.ReadGrid("../../../mesh.txt");

        _viewModel.AddSpline(type, controlPoints, grid);
    }
}