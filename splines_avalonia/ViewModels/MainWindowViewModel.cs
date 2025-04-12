using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using ReactiveUI;
using splines_avalonia;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using splines_avalonia.Views;

namespace splines_avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
       private Canvas _graphicCanvas;
        private TextBlock _statusBar;
        private double _offsetX;
        private double _offsetY;
        private double _zoom = 50;
        private double _fixedCenterX = 400;
        private double _fixedCenterY = 300;
        private Avalonia.Point _lastPanPosition;

        public ObservableCollection<ICurve> CurveList { get; } = new();
        
        public Canvas GraphicCanvas
        {
            get => _graphicCanvas;
            set => this.RaiseAndSetIfChanged(ref _graphicCanvas, value);
        }

        public ReactiveCommand<Unit, Unit> AddSplineCommand { get; }
        public ReactiveCommand<Unit, Unit> AddFunctionCommand { get; }
        public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
        public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetPositionCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveLeftCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveRightCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveUpCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveDownCommand { get; }

        public MainWindowViewModel()
        {
            // Initialize commands
            AddSplineCommand = ReactiveCommand.Create(AddSpline);
            AddFunctionCommand = ReactiveCommand.Create(AddFunction);
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

        private void AddSpline()
        {
            string type = "Interpolating Cubic";
            var controlPoints = FileReader.ReadPoints("../../../points.txt");
            var grid = FileReader.ReadGrid("../../../mesh.txt");

            var splineLogic = new SplineLogic();
            var spline = splineLogic.CreateCurve("Spline", type, null, grid, controlPoints);
            CurveList.Add(spline);

            DrawCurves();
        }

        private void AddFunction()
        {
            string function = "sin(x)";
            var functionLogic = new SplineLogic();
            var func = functionLogic.CreateCurve("Function", null, function, null, null);
            CurveList.Add(func);
            DrawCurves();
        }
        
        public void DrawCurves()
        {
            if (GraphicCanvas == null)
                return;

            GraphicCanvas.Children.Clear();
            DrawGrid();

            // Отрисовываем кривые
            foreach (var curve in CurveList)
            {
                if (curve.Type == "Spline")
                {
                    var points = new Points();

                    foreach (var p in curve.OutputPoints)
                    {
                        var screenPoint = new Avalonia.Point(
                            (p.X * _zoom) + CenterX() + _offsetX,
                            (-p.Y * _zoom) + CenterY() + _offsetY
                        );
                        points.Add(screenPoint);
                    }

                    if (points.Count >= 2)
                    {
                        var polyline = new Polyline
                        {
                            Points = points,
                            Stroke = Brushes.Black,
                            StrokeThickness = 2
                        };
                        GraphicCanvas.Children.Add(polyline);
                    }
                }
                if (curve.Type == "Function")
                {
                    double width = GraphicCanvas.Bounds.Width;
                    double height = GraphicCanvas.Bounds.Height;

                    double startX = -(CenterX() + _offsetX) / _zoom;
                    double endX = (width - CenterX() - _offsetX) / _zoom;

                    int maxPoints = 2000;
                    double visibleWidth = endX - startX;
                    double step = visibleWidth / maxPoints;

                    var points = new Points();

                    for (double x = startX; x <= endX; x += step)
                    {
                        double y = curve.CalculateFunctionValue(curve.FunctionString, x);

                        if (double.IsNaN(y) || double.IsInfinity(y))
                        {
                            // Прерывание, если значение невалидно — начинаем новую полилинию
                            if (points.Count >= 2)
                            {
                                var polyline = new Polyline
                                {
                                    Points = new Points(points),
                                    Stroke = Brushes.Black,
                                    StrokeThickness = 2
                                };
                                GraphicCanvas.Children.Add(polyline);
                            }

                            points.Clear();
                            continue;
                        }

                        var screenPoint = new Avalonia.Point(
                            (x * _zoom) + CenterX() + _offsetX,
                            (-y * _zoom) + CenterY() + _offsetY
                        );

                        if (screenPoint.Y >= 0 && screenPoint.Y <= height)
                        {
                            points.Add(screenPoint);
                        }
                        else
                        {
                            // точка вне экрана — отрисовываем то, что уже есть
                            if (points.Count >= 2)
                            {
                                var polyline = new Polyline
                                {
                                    Points = new Points(points),
                                    Stroke = Brushes.Black,
                                    StrokeThickness = 2
                                };
                                GraphicCanvas.Children.Add(polyline);
                            }

                            points.Clear();
                        }
                    }

                    // добавляем последнюю часть, если она есть
                    if (points.Count >= 2)
                    {
                        var polyline = new Polyline
                        {
                            Points = points,
                            Stroke = Brushes.Black,
                            StrokeThickness = 2
                        };
                        GraphicCanvas.Children.Add(polyline);
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