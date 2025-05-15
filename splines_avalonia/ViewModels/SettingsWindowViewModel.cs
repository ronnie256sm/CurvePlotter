using Avalonia.Media;
using ReactiveUI;

namespace splines_avalonia.ViewModels
{
    public class SettingsWindowViewModel : ReactiveObject
    {
        private bool _showAxes;
        private bool _showGrid;
        private bool _darkMode = false;
        private bool _automaticColor = false;
        private string _pointCountText = "1000";
        private Color _xAxisColor = Colors.DarkGray;
        private Color _yAxisColor = Colors.DarkGray;

        public bool ShowAxes
        {
            get => _showAxes;
            set => this.RaiseAndSetIfChanged(ref _showAxes, value);
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set => this.RaiseAndSetIfChanged(ref _showGrid, value);
        }

        public bool DarkMode
        {
            get => _darkMode;
            set => this.RaiseAndSetIfChanged(ref _darkMode, value);
        }

        public string PointCountText
        {
            get => _pointCountText;
            set => this.RaiseAndSetIfChanged(ref _pointCountText, value);
        }

        public Color XAxisColor
        {
            get => _xAxisColor;
            set => this.RaiseAndSetIfChanged(ref _xAxisColor, value);
        }

        public Color YAxisColor
        {
            get => _yAxisColor;
            set => this.RaiseAndSetIfChanged(ref _yAxisColor, value);
        }

        public bool AutomaticColor
        {
            get => _automaticColor;
            set => this.RaiseAndSetIfChanged(ref _automaticColor, value);
        }

        public bool TryGetValidatedPointCount(out int count)
        {
            if (int.TryParse(PointCountText, out count) && count >= 2)
                return true;

            count = 0;
            return false;
        }
    }
}
