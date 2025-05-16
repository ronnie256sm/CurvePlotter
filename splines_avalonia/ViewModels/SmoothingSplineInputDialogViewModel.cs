using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ReactiveUI;
using splines_avalonia.Helpers;

namespace splines_avalonia.ViewModels
{
    public class SmoothingSplineInputDialogViewModel : ReactiveObject
    {
        private string? _pointsFile;
        private string? _meshFile;
        private string? _smoothingFactorAlpha;
        private string? _smoothingFactorBeta;
        private bool _showControlPoints;
        public bool ShowControlPoints
        {
            get => _showControlPoints;
            set => this.RaiseAndSetIfChanged(ref _showControlPoints, value);
        }

        public string? PointsFile
        {
            get => _pointsFile;
            set => this.RaiseAndSetIfChanged(ref _pointsFile, value);
        }

        public string? MeshFile
        {
            get => _meshFile;
            set => this.RaiseAndSetIfChanged(ref _meshFile, value);
        }

        public string? SmoothingFactorAlpha
        {
            get => _smoothingFactorAlpha;
            set => this.RaiseAndSetIfChanged(ref _smoothingFactorAlpha, value);
        }

        public string? SmoothingFactorBeta
        {
            get => _smoothingFactorBeta;
            set => this.RaiseAndSetIfChanged(ref _smoothingFactorBeta, value);
        }

        public bool IsOkClicked { get; private set; } = false;

        public ReactiveCommand<Unit,Unit> SelectPointsFileCommand { get; }
        public ReactiveCommand<Unit,Unit> SelectMeshFileCommand { get; }
        public ReactiveCommand<Unit,Unit> OkCommand { get; }
        public ReactiveCommand<Unit,Unit> CancelCommand { get; }

        private readonly Window _parentWindow;

        public SmoothingSplineInputDialogViewModel(Window parentWindow)
        {
            _parentWindow = parentWindow;

            SelectPointsFileCommand = ReactiveCommand.CreateFromTask(SelectPointsFileAsync);
            SelectMeshFileCommand = ReactiveCommand.CreateFromTask(SelectMeshFileAsync);
            OkCommand = ReactiveCommand.CreateFromTask(OkAsync);
            CancelCommand = ReactiveCommand.Create(Cancel);
        }

        public async Task SelectPointsFileAsync()
        {
            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "Выберите файл с точками",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } } }
            };

            var fileResult = await _parentWindow.StorageProvider.OpenFilePickerAsync(filePickerOptions);
            if (fileResult.Count > 0)
            {
                PointsFile = fileResult[0].Path.LocalPath;
            }
        }

        public async Task SelectMeshFileAsync()
        {
            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "Выберите файл сетки",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Grid files") { Patterns = new[] { "*.txt" } } }
            };

            var fileResult = await _parentWindow.StorageProvider.OpenFilePickerAsync(filePickerOptions);
            if (fileResult.Count > 0)
            {
                MeshFile = fileResult[0].Path.LocalPath;
            }
        }

        private async Task OkAsync()
        {
            if (string.IsNullOrWhiteSpace(PointsFile))
            {
                await ErrorHelper.ShowError("Ошибка", "Пожалуйста, выберите файл с точками.");
                return;
            }

            if (string.IsNullOrWhiteSpace(MeshFile))
            {
                await ErrorHelper.ShowError("Ошибка", "Пожалуйста, выберите файл сетки.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SmoothingFactorAlpha))
            {
                await ErrorHelper.ShowError("Ошибка", "Пожалуйста, введите коэффициент сглаживания альфа.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SmoothingFactorBeta))
            {
                await ErrorHelper.ShowError("Ошибка", "Пожалуйста, введите коэффициент сглаживания бета.");
                return;
            }

            ShowControlPoints = _showControlPoints;
            IsOkClicked = true;
            _parentWindow.Close();
        }

        public async Task CreatePointsFile(Window parent, bool points)
        {
            string _suggestedFileName = "mesh.txt";
            if (points)
                _suggestedFileName = "points.txt";

            var savePickerOptions = new FilePickerSaveOptions
            {
                Title = "Создать файл",
                SuggestedFileName = _suggestedFileName,
                FileTypeChoices = new[] { new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } } }
            };

            var result = await parent.StorageProvider.SaveFilePickerAsync(savePickerOptions);
            if (result != null)
            {
                var path = result.Path.LocalPath;
                try
                {
                    File.WriteAllText(path, ""); // создаем пустой файл
                    if (points)
                        PointsFile = path;
                    else
                        MeshFile = path;
                    await Helpers.ErrorHelper.ShowError("Предупреждение", "Создан пустой файл. Пожалуйста, отредактируйте его, прежде чем продолжить.");
                }
                catch (Exception ex)
                {
                    await Helpers.ErrorHelper.ShowError("Ошибка", $"Не удалось создать файл: {ex.Message}");
                }
            }
        }

        public async Task EditFile(bool points)
        {
            var path = MeshFile;
            if (points)
                path = PointsFile;
            
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

        private void Cancel()
        {
            _parentWindow.Close();
        }
    }
}
