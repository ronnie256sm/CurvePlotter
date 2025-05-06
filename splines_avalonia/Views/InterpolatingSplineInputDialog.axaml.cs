using Avalonia.Controls;
using Avalonia.Interactivity;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views
{
    public partial class InterpolatingSplineInputDialog : Window
    {
        public bool IsOkClicked { get; private set; }
        public string PointsFile { get; private set; } = string.Empty;
        public InterpolatingSplineInputDialogViewModel ViewModel { get; }

        public InterpolatingSplineInputDialog()
        {
            InitializeComponent();
            ViewModel = new InterpolatingSplineInputDialogViewModel();
            DataContext = ViewModel;
        }

        private async void OnSelectPointsFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.SelectPointsFile(this);
        }

        private async void OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.PointsFilePath))
            {
                await Helpers.ErrorHelper.ShowError("Ошибка", "Пожалуйста, выберите файл с точками.");
                return;
            }

            PointsFile = ViewModel.PointsFilePath!;
            IsOkClicked = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void SetInitialValues(string pointsPath)
        {
            ViewModel.PointsFilePath = pointsPath;
        }
    }
}
