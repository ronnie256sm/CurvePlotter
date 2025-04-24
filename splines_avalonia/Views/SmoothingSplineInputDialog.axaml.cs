using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using splines_avalonia.Helpers;

namespace splines_avalonia.Views
{
    public partial class SmoothingSplineInputDialog : Window
    {
        #pragma warning disable CS8618, CS8601
        // Сохраняем строки для путей файлов и коэффициента сглаживания
        public string PointsFile { get; private set; }
        public string MeshFile { get; private set; }
        public string SmoothingFactorAlpha { get; private set; }
        public string SmoothingFactorBeta { get; private set; } // Сохраняем строку коэффициента сглаживания
        public bool IsOkClicked { get; private set; } = false;

        public SmoothingSplineInputDialog()
        {
            InitializeComponent();
        }

        public void SetInitialValues(string pointsPath, string meshPath, string smoothingAlpha, string smoothingBeta)
        {
            PointsFilePath.Text = pointsPath;
            MeshFilePath.Text = meshPath;
            SmoothingFactorAlphaValue.Text = smoothingAlpha;
            SmoothingFactorBetaValue.Text = smoothingBeta;
        }

        // Выбор файла с точками
        private async void OnSelectPointsFileClick(object? sender, RoutedEventArgs e)
        {
            var desktop = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow is null)
                return;

            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "Выберите файл с точками",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } } }
            };

            var fileResult = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(filePickerOptions);
            if (fileResult.Count > 0)
            {
                PointsFilePath.Text = fileResult[0].Path.LocalPath;
            }
        }

        // Выбор файла сетки
        private async void OnSelectMeshFileClick(object? sender, RoutedEventArgs e)
        {
            var desktop = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow is null)
                return;

            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "Выберите файл сетки",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Grid files") { Patterns = new[] { "*.txt" } } }
            };

            var fileResult = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(filePickerOptions);
            if (fileResult.Count > 0)
            {
                MeshFilePath.Text = fileResult[0].Path.LocalPath;
            }
        }

        // Обработка нажатия на кнопку "OK"
        private async void OnOkClick(object? sender, RoutedEventArgs e)
        {
            // Проверка на выбор файла с точками
            if (string.IsNullOrWhiteSpace(PointsFilePath.Text))
            {
                await ErrorHelper.ShowError("Пожалуйста, выберите файл с точками.");
                return;
            }

            PointsFile = PointsFilePath.Text;

            // Проверка на выбор файла сетки
            if (string.IsNullOrWhiteSpace(MeshFilePath.Text))
            {
                await ErrorHelper.ShowError("Пожалуйста, выберите файл сетки.");
                return;
            }

            MeshFile = MeshFilePath.Text;


            // Сохраняем введенную строку для коэффициента сглаживания (без парсинга)
            SmoothingFactorAlpha = SmoothingFactorAlphaValue.Text;
            SmoothingFactorBeta = SmoothingFactorBetaValue.Text; 

            // Проверка, что строка для коэффициента не пустая
            if (string.IsNullOrWhiteSpace(SmoothingFactorAlpha))
            {
                await ErrorHelper.ShowError("Пожалуйста, введите коэффициент сглаживания альфа.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SmoothingFactorBeta))
            {
                await ErrorHelper.ShowError("Пожалуйста, введите коэффициент сглаживания бета.");
                return;
            }
            IsOkClicked = true;
            Close(); // Закрываем диалог
        }

        // Обработка нажатия на кнопку "Отмена"
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
