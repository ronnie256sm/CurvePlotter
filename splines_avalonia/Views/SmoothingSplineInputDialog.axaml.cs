using Avalonia.Controls;
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
    }
}