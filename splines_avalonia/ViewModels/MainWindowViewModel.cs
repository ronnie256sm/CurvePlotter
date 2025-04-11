using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using splines_avalonia;
using System.Linq;
using Avalonia.Controls.Shapes;
using Avalonia;
using System;
using splines_avalonia.Views;
using Avalonia.Controls.ApplicationLifetimes;

namespace splines_avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<ISpline> SplineList { get; set; }
        public ObservableCollection<IFunction> FunctionList { get; set; }
        public Canvas GraphicCanvas { get; set; }
        private TextBlock _statusBar;
        private double _offsetX = 0;
        private double _offsetY = 0;
        private double _zoom = 50;
        private double _fixedCenterX = 400;
        private double _fixedCenterY = 300;

        private Avalonia.Point _lastPanPosition;

        public MainWindowViewModel()
        {
            SplineList = new ObservableCollection<ISpline>();
            FunctionList = new ObservableCollection<IFunction>();
        }

        public void ZoomIn()
        {
            HandleZoom(1);
            DrawCurves();
        }

        public void ZoomOut()
        {
            HandleZoom(-1);
            DrawCurves();
        }

        public void MoveLeft()
        {
            _offsetX += 10;
            DrawCurves();
        }

        public void MoveRight()
        {
            _offsetX -= 10;
            DrawCurves();
        }

        public void MoveUp()
        {
            _offsetY += 10;
            DrawCurves();
        }

        public void MoveDown()
        {
            _offsetY -= 10;
            DrawCurves();
        }

        public void AddSpline(string type, Point[] points, double[] grid)
        {
            var _splineLogic = new SplineLogic();
            var spline = _splineLogic.CreateSpline(type, grid, points);
            SplineList.Add(spline);
            DrawCurves();
        }

        public void AddFunction(string function)
        {
            var _functionLogic = new SplineLogic();
            var _function = _functionLogic.CreateFunction(function);
            FunctionList.Add(_function);
            DrawCurves();
        }

        public void DrawCurves()
        {
            if (GraphicCanvas == null)
                return;

            GraphicCanvas.Children.Clear();

            DrawGrid();

            // Отрисовываем сплайны
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

            // Отрисовываем функции
            foreach (var function in FunctionList)
            {
                double width = GraphicCanvas.Bounds.Width;
                double height = GraphicCanvas.Bounds.Height;

                // Определяем начальные и конечные значения X
                double startX = -(CenterX() + _offsetX) / _zoom;
                double endX = (width - CenterX() - _offsetX) / _zoom;

                int maxPoints = 1000;
                double visibleWidth = endX - startX;
                double step = visibleWidth / maxPoints;

                // Для хранения последней точки
                Avalonia.Point? lastPoint = null;

                // Идем по пикселям вдоль оси X только в пределах видимой области
                for (double x = startX; x <= endX; x += step)
                {
                    // Вычисляем значение функции для данной точки x
                    double y = function.CalculateFunctionValue(function.FunctionString, x);

                    // Переводим мировые координаты в пиксели
                    var screenPoint = new Avalonia.Point(
                        (x * _zoom) + CenterX() + _offsetX,
                        (-y * _zoom) + CenterY() + _offsetY
                    );

                    // Проверяем, что точка лежит в пределах видимой области графика
                    if (screenPoint.Y >= 0 && screenPoint.Y <= height)
                    {
                        // Если это не первая точка, рисуем линию
                        if (lastPoint != null)
                        {
                            var line = new Line
                            {
                                StartPoint = (Avalonia.Point)lastPoint,
                                EndPoint = screenPoint,
                                Stroke = Brushes.Black,
                                StrokeThickness = 2
                            };
                            GraphicCanvas.Children.Add(line);
                        }

                        // Запоминаем текущую точку для рисования следующей линии
                        lastPoint = screenPoint;
                    }
                    else
                    {
                        // Если точка выходит за пределы видимой области, сбрасываем lastPoint
                        lastPoint = null;
                    }
                }
            }
        }

        private double CenterX() => _fixedCenterX;
        private double CenterY() => _fixedCenterY;
        public void SetInitialCenter(double x, double y)
        {
            _fixedCenterX = x;
            _fixedCenterY = y;
        }

        public void HandleZoom(double delta)
        {
            _zoom *= delta > 0 ? 1.1 : 0.9;
            DrawCurves();
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

            DrawCurves();
        }

        private double CalculateGridStep()
        {
            double minPixelStep = 40;
            double targetStep = minPixelStep / _zoom;

            // Получаем порядок величины, например для 0.123 → 0.1
            double exponent = Math.Pow(10, Math.Floor(Math.Log10(targetStep)));
            double[] mantissas = { 1, 2, 5 };

            foreach (var m in mantissas)
            {
                double s = m * exponent;
                if (s >= targetStep)
                    return s;
            }

            return 10 * exponent; // fallback
        }

        private void DrawGrid()
        {
            double width = GraphicCanvas.Bounds.Width;
            double height = GraphicCanvas.Bounds.Height;

            double step = CalculateGridStep();

            double startX = -(CenterX() + _offsetX) / _zoom;
            double endX = (width - CenterX() - _offsetX) / _zoom;

            double startY = -(CenterY() + _offsetY) / _zoom;
            double endY = (height - CenterY() - _offsetY) / _zoom;

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

                string labelText = Math.Abs(x) < 1e-6 ? "0" : x.ToString("G5");
                var text = new TextBlock
                {
                    Text = labelText,
                    Foreground = Brushes.Gray,
                    FontSize = 12
                };
                Canvas.SetLeft(text, screenX + 2);
                Canvas.SetTop(text, CenterY() + _offsetY + 2);
                GraphicCanvas.Children.Add(text);
            }

            for (double y = Math.Floor(startY / step) * step; y <= endY; y += step)
            {
                double screenY = y * _zoom + CenterY() + _offsetY;

                var line = new Line
                {
                    StartPoint = new Avalonia.Point(0, screenY),
                    EndPoint = new Avalonia.Point(width, screenY),
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                GraphicCanvas.Children.Add(line);

                string labelText = Math.Abs(y) < 1e-6 ? "0" : (-y).ToString("G5");
                var text = new TextBlock
                {
                    Text = labelText,
                    Foreground = Brushes.Gray,
                    FontSize = 12
                };
                Canvas.SetLeft(text, CenterX() + _offsetX + 2);
                Canvas.SetTop(text, screenY + 2);
                GraphicCanvas.Children.Add(text);
            }

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

        public void UpdateStatusBar(Avalonia.Point mousePosition)
        {
            if (_statusBar == null)
            {
                if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow is MainWindow mainWindow)
                {
                    _statusBar = mainWindow.FindControl<TextBlock>("StatusBar");
                }
            }

            if (_statusBar != null)
            {
                double x = (mousePosition.X - CenterX() - _offsetX) / _zoom;
                double y = -(mousePosition.Y - CenterY() - _offsetY) / _zoom;
                _statusBar.Text = $"X: {x:0.###}, Y: {y:0.###}";
            }
        }

        public void ResetPosition()
        {
            _offsetX = 0;
            _offsetY = 0;
            _zoom = 50;
            DrawCurves();
        }
    }
}