using Avalonia.Controls;
using Avalonia.Interactivity;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views
{
    public partial class InterpolatingSplineInputDialog : Window
    {
        public bool IsOkClicked { get; private set; }
        public string PointsFile { get; private set; } = string.Empty;
        public bool ShowControlPoints { get; private set; } = true;
        public InterpolatingSplineInputDialogViewModel ViewModel { get; }

        public InterpolatingSplineInputDialog(string type, bool showControlPoints)
        {
            InitializeComponent();
            ViewModel = new InterpolatingSplineInputDialogViewModel();
            DataContext = ViewModel;
            if (type == "Interpolating Cubic 2" || type == "Interpolating Cubic 1")
                Title = "Выберите файлы для сплайна";
            if (type == "Linear")
                Title = "Выберите файлы для ломаной";
            ViewModel.ShowControlPoints = showControlPoints;
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
            ShowControlPoints = ViewModel.ShowControlPoints;
            IsOkClicked = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void SetInitialValues(string pointsPath, bool showControlPoints)
        {
            ViewModel.PointsFilePath = pointsPath;
            ViewModel.ShowControlPoints = showControlPoints;
        }

        private async void OnCreateNewFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.CreatePointsFile(this);
        }

        private async void OnEditFileClick(object? sender, RoutedEventArgs e)
        {
            await ViewModel.EditPointsFile();
        }
    }
}
