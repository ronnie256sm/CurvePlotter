using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views
{
    public partial class SettingsWindow : Window
    {
        public bool IsOkClicked { get; private set; }
        public bool ShowAxes { get; private set; }
        public bool ShowGrid { get; private set; }
        public bool DarkMode { get; private set; } = false;
        public int PointCount { get; private set; }
        public Color XAxisColor { get; private set; }
        public Color YAxisColor { get; private set; }

        private readonly SettingsWindowViewModel _viewModel;

        public SettingsWindow(bool currentAxes, bool currentGrid, bool currentColorMode, int currentPointCount, Color currentXAxisColor, Color currentYAxisColor)
        {
            InitializeComponent();
            _viewModel = new SettingsWindowViewModel
            {
                ShowAxes = currentAxes,
                ShowGrid = currentGrid,
                DarkMode = currentColorMode,
                PointCountText = currentPointCount.ToString(),
                XAxisColor = currentXAxisColor,
                YAxisColor = currentYAxisColor
            };
            DataContext = _viewModel;
        }

        private async void OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (!_viewModel.TryGetValidatedPointCount(out int count))
            {
                await Helpers.ErrorHelper.ShowError("Ошибка", "Введите целое число ≥ 2 для количества точек.");
                return;
            }

            ShowAxes = _viewModel.ShowAxes;
            ShowGrid = _viewModel.ShowGrid;
            DarkMode = _viewModel.DarkMode;
            PointCount = count;
            XAxisColor = _viewModel.XAxisColor;
            YAxisColor = _viewModel.YAxisColor;
            IsOkClicked = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnDefaultClick(object? sender, RoutedEventArgs e)
        {
            ShowAxes = true;
            ShowGrid = true;
            DarkMode = false;
            PointCount = 1000;
            XAxisColor = Colors.DarkGray;
            YAxisColor = Colors.DarkGray;
            IsOkClicked = true;
            Close();
        }
    }
}
