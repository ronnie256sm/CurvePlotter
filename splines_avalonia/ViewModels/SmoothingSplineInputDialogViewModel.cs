using System;
using System.ComponentModel;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
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

        private async Task SelectPointsFileAsync()
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

        private async Task SelectMeshFileAsync()
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

            IsOkClicked = true;
            _parentWindow.Close();
        }

        private void Cancel()
        {
            _parentWindow.Close();
        }
    }
}
