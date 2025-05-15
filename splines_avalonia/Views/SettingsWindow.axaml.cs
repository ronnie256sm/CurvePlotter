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
        public bool AutomaticColor { get; private set; } = false;
        public int PointCount { get; private set; }
        public Color XAxisColor { get; private set; }
        public Color YAxisColor { get; private set; }

        private readonly SettingsWindowViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            _viewModel = new SettingsWindowViewModel
            {
                ShowAxes = Globals.ShowAxes,
                ShowGrid = Globals.ShowGrid,
                DarkMode = Globals.DarkMode,
                PointCountText = Globals.PointCount.ToString(),
                XAxisColor = Globals.XAxisColor,
                YAxisColor = Globals.YAxisColor,
                AutomaticColor = Globals.AutomaticColor
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

            Globals.ShowAxes = _viewModel.ShowAxes;
            Globals.ShowGrid = _viewModel.ShowGrid;
            Globals.DarkMode = _viewModel.DarkMode;
            Globals.PointCount = count;
            Globals.XAxisColor = _viewModel.XAxisColor;
            Globals.YAxisColor = _viewModel.YAxisColor;
            Globals.AutomaticColor = _viewModel.AutomaticColor;
            IsOkClicked = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnDefaultClick(object? sender, RoutedEventArgs e)
        {
            Globals.ShowAxes = true;
            Globals.ShowGrid = true;
            Globals.DarkMode = false;
            Globals.PointCount = 1000;
            Globals.XAxisColor = Colors.DarkGray;
            Globals.YAxisColor = Colors.DarkGray;
            Globals.AutomaticColor = false;
            IsOkClicked = true;
            Close();
        }
    }
}
