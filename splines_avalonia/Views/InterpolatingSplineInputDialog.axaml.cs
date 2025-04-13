using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using System;
using System.Threading.Tasks;
using splines_avalonia.Helpers;

namespace splines_avalonia.Views
{
    public partial class InterpolatingSplineInputDialog : Window
    {
        public string PointsFile { get; private set; }
        public bool IsOkClicked { get; private set; } = false;

        public InterpolatingSplineInputDialog()
        {
            InitializeComponent();
        }

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

        private async void OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PointsFilePath.Text))
            {
                await ErrorHelper.ShowError(this, "Пожалуйста, выберите файл с точками.");
                return;
            }

            PointsFile = PointsFilePath.Text;
            IsOkClicked = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
