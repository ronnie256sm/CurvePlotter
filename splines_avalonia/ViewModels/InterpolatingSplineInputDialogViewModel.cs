using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using splines_avalonia.Helpers;

namespace splines_avalonia.ViewModels
{
    public class InterpolatingSplineInputDialogViewModel : ReactiveObject
    {
        private string? _pointsFilePath;
        public string? PointsFilePath
        {
            get => _pointsFilePath;
            set => this.RaiseAndSetIfChanged(ref _pointsFilePath, value);
        }

        public async Task SelectPointsFile(Window parent)
        {
            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "Выберите файл с точками",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } }
                }
            };

            var fileResult = await parent.StorageProvider.OpenFilePickerAsync(filePickerOptions);
            if (fileResult.Count > 0)
            {
                PointsFilePath = fileResult[0].Path.LocalPath;
            }
        }
    }
}
