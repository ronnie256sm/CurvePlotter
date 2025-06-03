using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace CurvePlotter.ViewModels
{
    public class InterpolatingSplineInputDialogViewModel : ReactiveObject
    {
        private string? _pointsFilePath;
        private bool _showControlPoints;

        public string? PointsFilePath
        {
            get => _pointsFilePath;
            set => this.RaiseAndSetIfChanged(ref _pointsFilePath, value);
        }

        public bool ShowControlPoints
        {
            get => _showControlPoints;
            set => this.RaiseAndSetIfChanged(ref _showControlPoints, value);
        }

        public async Task SelectPointsFile(Window parent)
        {
            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "Выберите файл с точками",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } } }
            };

            var fileResult = await parent.StorageProvider.OpenFilePickerAsync(filePickerOptions);
            if (fileResult.Count > 0)
            {
                PointsFilePath = fileResult[0].Path.LocalPath;
            }
        }

        public async Task CreatePointsFile(Window parent)
        {
            var savePickerOptions = new FilePickerSaveOptions
            {
                Title = "Создать файл",
                SuggestedFileName = "points.txt",
                FileTypeChoices = new[] { new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } } }
            };

            var result = await parent.StorageProvider.SaveFilePickerAsync(savePickerOptions);
            if (result != null)
            {
                var path = result.Path.LocalPath;
                try
                {
                    File.WriteAllText(path, ""); // создаем пустой файл
                    PointsFilePath = path;
                    await Helpers.ErrorHelper.ShowError("Предупреждение", "Создан пустой файл. Пожалуйста, отредактируйте его, прежде чем продолжить.");
                }
                catch (Exception ex)
                {
                    await Helpers.ErrorHelper.ShowError("Ошибка", $"Не удалось создать файл: {ex.Message}");
                }
            }
        }

        public async Task EditPointsFile()
        {
            var path = PointsFilePath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                await Helpers.ErrorHelper.ShowError("Ошибка", "Файл не выбран или не существует.");
                return;
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad", path) { UseShellExecute = true });
                }
                else if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("xdg-open", path);
                }
                else
                {
                    await Helpers.ErrorHelper.ShowError("Ошибка", "Редактирование файла поддерживается только в Windows и Linux.");
                }
            }
            catch (Exception ex)
            {
                await Helpers.ErrorHelper.ShowError("Ошибка", $"Не удалось открыть файл для редактирования: {ex.Message}");
            }
        }
    }
}
