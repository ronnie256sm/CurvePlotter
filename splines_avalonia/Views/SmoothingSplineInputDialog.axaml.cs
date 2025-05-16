using Avalonia.Controls;
using Avalonia.Interactivity;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views
{
    public partial class SmoothingSplineInputDialog : Window
    {
        public SmoothingSplineInputDialogViewModel ViewModel { get; }
        public SmoothingSplineInputDialog(bool showControlPoints)
        {
            InitializeComponent();
            ViewModel = new SmoothingSplineInputDialogViewModel(this);
            DataContext = ViewModel;
            ViewModel.ShowControlPoints = showControlPoints;
        }

        public void SetInitialValues(string pointsPath, string meshPath, string smoothingAlpha, string smoothingBeta, bool showControlPoints)
        {
            if (DataContext is SmoothingSplineInputDialogViewModel vm)
            {
                vm.PointsFile = pointsPath;
                vm.MeshFile = meshPath;
                vm.SmoothingFactorAlpha = smoothingAlpha;
                vm.SmoothingFactorBeta = smoothingBeta;
                vm.ShowControlPoints = showControlPoints;
            }
        }

        public bool IsOkClicked => (DataContext as SmoothingSplineInputDialogViewModel)?.IsOkClicked ?? false;
        public string PointsFile => (DataContext as SmoothingSplineInputDialogViewModel)?.PointsFile ?? "";
        public string MeshFile => (DataContext as SmoothingSplineInputDialogViewModel)?.MeshFile ?? "";
        public string SmoothingFactorAlpha => (DataContext as SmoothingSplineInputDialogViewModel)?.SmoothingFactorAlpha ?? "";
        public string SmoothingFactorBeta => (DataContext as SmoothingSplineInputDialogViewModel)?.SmoothingFactorBeta ?? "";
        public bool ShowControlPoints => (DataContext as SmoothingSplineInputDialogViewModel)?.ShowControlPoints ?? true;
        private async void OnSelectPointsFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.SelectPointsFileAsync();
        }
        private async void OnCreatePointsFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.CreatePointsFile(this, true);
        }
        private async void OnEditPointsFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.EditFile(true);
        }
        private async void OnSelectMeshFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.SelectMeshFileAsync();
        }
        private async void OnCreateMeshFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.CreatePointsFile(this, false);
        }
        private async void OnEditMeshFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.EditFile(false);
        }
    }
}