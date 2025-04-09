using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using splines_avalonia;
using System.Linq;
using Avalonia.Controls.Shapes;
using Avalonia;
using System;

namespace splines_avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<ISpline> SplineList { get; set; }
        public Canvas GraphicCanvas { get; set; }
        private double _offsetX = 0;
        private double _offsetY = 0;
        private double _zoom = 50;

        private Avalonia.Point _lastPanPosition;

        public MainWindowViewModel()
        {
            SplineList = new ObservableCollection<ISpline>();
        }

        public void AddSpline(string type, Point[] points, double[] grid)
        {
            var _splineLogic = new SplineLogic();
            var spline = _splineLogic.Create(type, grid, points);
            SplineList.Add(spline);
            DrawSplines();
        }

        public void DrawSplines()
        {
            if (GraphicCanvas == null)
                return;

            GraphicCanvas.Children.Clear();

            DrawGrid();

            foreach (var spline in SplineList)
            {
                var points = spline.OutputPoints
                    .Select(p => new Avalonia.Point(
                        (p.X * _zoom) + CenterX() + _offsetX,
                        (-p.Y * _zoom) + CenterY() + _offsetY))
                    .ToArray();

                for (int i = 0; i < points.Length - 1; i++)
                {
                    var line = new Line
                    {
                        StartPoint = points[i],
                        EndPoint = points[i + 1],
                        Stroke = Brushes.Black,
                        StrokeThickness = 2
                    };
                    GraphicCanvas.Children.Add(line);
                }
            }
        }

        private double CenterX() => GraphicCanvas.Bounds.Width / 2;
        private double CenterY() => GraphicCanvas.Bounds.Height / 2;

        public void HandleZoom(double delta)
        {
            _zoom *= delta > 0 ? 1.1 : 0.9;
            DrawSplines();
        }

        public void StartPan(Avalonia.Point point)
        {
            _lastPanPosition = point;
        }

        public void DoPan(Avalonia.Point current)
        {
            var dx = current.X - _lastPanPosition.X;
            var dy = current.Y - _lastPanPosition.Y;

            _offsetX += dx;
            _offsetY += dy;
            _lastPanPosition = current;

            DrawSplines();
        }

        private void DrawGrid()
        {
            double width = GraphicCanvas.Bounds.Width;
            double height = GraphicCanvas.Bounds.Height;

            // адаптивный шаг сетки на основе масштаба
            double[] baseSteps = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
            double step = baseSteps.First(s => s * _zoom >= 40); // минимум 40 пикселей между линиями

            double labelStep = step;

            double startX = -(CenterX() + _offsetX) / _zoom;
            double endX = (width - CenterX() - _offsetX) / _zoom;

            double startY = -(CenterY() + _offsetY) / _zoom;
            double endY = (height - CenterY() - _offsetY) / _zoom;

            // Вертикальные линии и подписи по X
            for (double x = Math.Floor(startX / step) * step; x <= endX; x += step)
            {
                double screenX = x * _zoom + CenterX() + _offsetX;
                var line = new Line
                {
                    StartPoint = new Avalonia.Point(screenX, 0),
                    EndPoint = new Avalonia.Point(screenX, height),
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                GraphicCanvas.Children.Add(line);

                if (Math.Abs(x) > 1e-6) // исключаем 0.00 в центре
                {
                    var text = new TextBlock
                    {
                        Text = x.ToString("0.##"),
                        Foreground = Brushes.Gray,
                        FontSize = 12
                    };
                    Canvas.SetLeft(text, screenX + 2);
                    Canvas.SetTop(text, CenterY() + _offsetY + 2);
                    GraphicCanvas.Children.Add(text);
                }
            }

            // Горизонтальные линии и подписи по Y
            for (double y = Math.Floor(startY / step) * step; y <= endY; y += step)
            {
                double screenY = -y * _zoom + CenterY() + _offsetY;
                var line = new Line
                {
                    StartPoint = new Avalonia.Point(0, screenY),
                    EndPoint = new Avalonia.Point(width, screenY),
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                GraphicCanvas.Children.Add(line);

                if (Math.Abs(y) > 1e-6) // исключаем 0.00 в центре
                {
                    var text = new TextBlock
                    {
                        Text = y.ToString("0.##"),
                        Foreground = Brushes.Gray,
                        FontSize = 12
                    };
                    Canvas.SetLeft(text, CenterX() + _offsetX + 2);
                    Canvas.SetTop(text, screenY + 2);
                    GraphicCanvas.Children.Add(text);
                }
            }

            // Оси
            var xAxis = new Line
            {
                StartPoint = new Avalonia.Point(0, CenterY() + _offsetY),
                EndPoint = new Avalonia.Point(width, CenterY() + _offsetY),
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2
            };
            GraphicCanvas.Children.Add(xAxis);

            var yAxis = new Line
            {
                StartPoint = new Avalonia.Point(CenterX() + _offsetX, 0),
                EndPoint = new Avalonia.Point(CenterX() + _offsetX, height),
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2
            };
            GraphicCanvas.Children.Add(yAxis);
        }
    }
}