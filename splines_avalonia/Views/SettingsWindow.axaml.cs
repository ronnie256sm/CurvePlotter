using Avalonia.Controls;
using Avalonia.Interactivity;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views
{
    public partial class SettingsWindow : Window
    {
        public bool IsOkClicked { get; private set; }
        public bool ShowAxes { get; private set; }
        public bool ShowGrid { get; private set; }
        public int PointCount { get; private set; }

        private readonly SettingsWindowViewModel _viewModel;

        public SettingsWindow(bool currentAxes, bool currentGrid, int currentPointCount)
        {
            InitializeComponent();
            _viewModel = new SettingsWindowViewModel
            {
                ShowAxes = currentAxes,
                ShowGrid = currentGrid,
                PointCountText = currentPointCount.ToString()
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
            PointCount = count;
            IsOkClicked = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
