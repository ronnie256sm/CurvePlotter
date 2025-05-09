using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using ReactiveUI;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using splines_avalonia.Views;
using splines_avalonia.Helpers;
using System.ComponentModel;
using System.Collections.Specialized;
using DynamicData.Kernel;

#pragma warning disable CS8618, CS8604, CS8600, CS8601, CS8602

namespace splines_avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public Canvas _graphicCanvas;
        private TextBlock _statusBar;
        private double _offsetX;
        private double _offsetY;
        private double _zoom = 50;
        private double _fixedCenterX = 400;
        private double _fixedCenterY = 300;
        private Avalonia.Point _lastPanPosition;

        public static ObservableCollection<ICurve> CurveList { get; } = new();
        private ICurve? _selectedCurve;
        public ICurve? SelectedCurve
        {
            get => _selectedCurve;
            set => this.RaiseAndSetIfChanged(ref _selectedCurve, value);
        }
        
        public Canvas GraphicCanvas
        {
            get => _graphicCanvas;
            set => this.RaiseAndSetIfChanged(ref _graphicCanvas, value);
        }

        public ReactiveCommand<Unit, Unit> AddInterpolatingSplineCommand { get; }
        public ReactiveCommand<Unit, Unit> AddSmoothingSplineCommand { get; }
        public ReactiveCommand<Unit, Unit> AddLinearSplineCommand { get; }
        public ReactiveCommand<Unit, Unit> AddFunctionCommand { get; }
        public ReactiveCommand<ICurve, Unit> EditCurveCommand { get; }
        public ReactiveCommand<ICurve, Unit> DeleteCurveCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveJsonCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadJsonCommand { get; }
        public ReactiveCommand<Unit, Unit> SavePngCommand { get; }
        public MainWindowViewModel()
        {
            AddInterpolatingSplineCommand = ReactiveCommand.Create(() => AddSpline("Interpolating Cubic"));
            AddSmoothingSplineCommand = ReactiveCommand.Create(AddSmoothingSpline);
            AddLinearSplineCommand = ReactiveCommand.Create(() => AddSpline("Linear"));
            AddFunctionCommand = ReactiveCommand.Create(AddFunction);
            EditCurveCommand = ReactiveCommand.Create<ICurve>(curve => 
            {
                SelectedCurve = curve;
                EditCurve();
            });
            
            DeleteCurveCommand = ReactiveCommand.Create<ICurve>(curve => 
            {
                SelectedCurve = curve;
                DeleteSelectedCurve();
            });

            SaveJsonCommand = ReactiveCommand.CreateFromTask(() => IO.SaveJSON(CurveList));
            LoadJsonCommand = ReactiveCommand.CreateFromTask(() => IO.LoadJSON());
            SavePngCommand = ReactiveCommand.CreateFromTask(() => IO.SavePNG());

            CurveList.CollectionChanged += CurveList_CollectionChanged;
        }

        private void CurveList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ICurve curve in e.NewItems)
                {
                    if (curve is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged += Curve_PropertyChanged;
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (ICurve curve in e.OldItems)
                {
                    if (curve is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged -= Curve_PropertyChanged;
                    }
                }
            }
            DrawCurves();
        }
        
        private void Curve_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICurve.IsVisible) || e.PropertyName == nameof(ICurve.Color))
            {
                DrawCurves();
            }
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

        private async void AddFunction()
        {
            var dialog = new FunctionInputDialog();
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            var result = await dialog.ShowDialog<(string FunctionString, string Start, string End)>(mainWindow);

            if (!string.IsNullOrWhiteSpace(result.FunctionString))
            {
                var logic = new SplineLogic();
                var curve = await logic.CreateFunction(result.FunctionString);
                curve.Start = result.Start;
                curve.End = result.End;
                if (curve != null && curve.IsPossible)
                    CurveList.Add(curve);
                DrawCurves();
            }
        }

        private async void EditFunction()
        {
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (SelectedCurve is not ICurve selectedFunction)
            {
                await ErrorHelper.ShowError("Ошибка", "Выберите функцию для редактирования.");
                return;
            }

            var dialog = new FunctionInputDialog();
            dialog.SetInitialFunction(selectedFunction.FunctionString);
            var result = await dialog.ShowDialog<string>(mainWindow);

            if (!string.IsNullOrWhiteSpace(result))
            {
                selectedFunction.FunctionString = result;
                if (!SelectedCurve.IsPossible)
                {
                    CurveList.Remove(SelectedCurve);
                }
                DrawCurves();
            }
        }

        private async void AddSpline(string type)
        {
            var inputDialog = new InterpolatingSplineInputDialog(type);
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            await inputDialog.ShowDialog(mainWindow);

            if (!inputDialog.IsOkClicked)
                return;

            string pointsFile = inputDialog.PointsFile;

            if (string.IsNullOrWhiteSpace(pointsFile))
            {
                await ErrorHelper.ShowError("Ошибка", "Пожалуйста, выберите файл с точками.");
                return;
            }

            var points = await FileReader.ReadPoints(pointsFile);

            ICurve curve = null;
            var logic = new SplineLogic();
            if (type == "Interpolating Cubic")
                curve = logic.CreateInterpolatingSpline(points);
            else if (type == "Linear")
                curve = logic.CreateLinearSpline(points);

            if (curve != null && curve.IsPossible)
            {
                curve.ControlPointsFile = pointsFile;
                CurveList.Add(curve);
            }

            DrawCurves();
        }

        private async void AddSmoothingSpline()
        {
            var inputDialog = new SmoothingSplineInputDialog();
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            await inputDialog.ShowDialog(mainWindow);

            if (!inputDialog.IsOkClicked)
                return;

            string pointsFile = inputDialog.PointsFile;
            string meshFile = inputDialog.MeshFile;
            string smoothingAlpha = inputDialog.SmoothingFactorAlpha;
            string smoothingBeta = inputDialog.SmoothingFactorBeta;

            if (string.IsNullOrWhiteSpace(pointsFile))
            {
                await ErrorHelper.ShowError("Ошибка", "Пожалуйста, выберите файл с точками.");
                return;
            }

            if (string.IsNullOrWhiteSpace(meshFile))
            {
                await ErrorHelper.ShowError("Ошибка", "Пожалуйста, выберите файл сетки.");
                return;
            }

            var points = await FileReader.ReadPoints(pointsFile);
            var mesh = await FileReader.ReadGrid(meshFile);

            var logic = new SplineLogic();
            var curve = logic.CreateSmoothingSpline(mesh, points, smoothingAlpha, smoothingBeta);

            if (curve != null && curve.IsPossible)
            {
                curve.ControlPointsFile = pointsFile;
                curve.GridFile = meshFile;
                CurveList.Add(curve);
            }
            else
            {
                await ErrorHelper.ShowError("Ошибка", "Не удалось построить сглаживающий сплайн. Попробуйте изменить параметры.");
            }

            DrawCurves();
        }

        private async void EditSpline()
        {
            if (SelectedCurve == null)
                return;

            var type = SelectedCurve.SplineType;

            string newPointsFile = null;
            string newMeshFile = null;
            string newSmoothingFactorAlpha = null;
            string newSmoothingFactorBeta = null;

            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (type == "Linear" || type == "Interpolating Cubic")
            {
                var dialog = new InterpolatingSplineInputDialog(type);

                if (SelectedCurve is ICurve spline && !string.IsNullOrWhiteSpace(spline.ControlPointsFile))
                    dialog.SetInitialValues(spline.ControlPointsFile);

                await dialog.ShowDialog(mainWindow);

                if (!dialog.IsOkClicked)
                    return;

                newPointsFile = dialog.PointsFile;
            }
            else if (type == "Smoothing Cubic")
            {
                var dialog = new SmoothingSplineInputDialog();

                if (SelectedCurve is ICurve spline)
                {
                    dialog.SetInitialValues(
                        spline.ControlPointsFile ?? "",
                        spline.GridFile ?? "",
                        spline.SmoothingCoefficientAlpha ?? "",
                        spline.SmoothingCoefficientBeta ?? ""
                    );
                }

                await dialog.ShowDialog(mainWindow);

                if (!dialog.IsOkClicked)
                    return;

                newPointsFile = dialog.PointsFile;
                newMeshFile = dialog.MeshFile;
                newSmoothingFactorAlpha = dialog.SmoothingFactorAlpha;
                newSmoothingFactorBeta = dialog.SmoothingFactorBeta;
            }
            else
            {
                await ErrorHelper.ShowError("Ошибка", "Редактирование доступно только для сплайнов.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newPointsFile))
            {
                await ErrorHelper.ShowError("Ошибка", "Не выбран файл точек.");
                return;
            }

            var newPoints = await FileReader.ReadPoints(newPointsFile);
            double[] newMesh = null;

            if (type == "Smoothing Cubic")
            {
                if (string.IsNullOrWhiteSpace(newMeshFile))
                {
                    await ErrorHelper.ShowError("Ошибка", "Не выбран файл сетки.");
                    return;
                }

                newMesh = await FileReader.ReadGrid(newMeshFile);
            }

            var logic = new SplineLogic();
            ICurve newCurve = null;

            if (type == "Linear")
            {
                newCurve = logic.CreateLinearSpline(newPoints);
            }
            else if (type == "Interpolating Cubic")
            {
                newCurve = logic.CreateInterpolatingSpline(newPoints);
            }
            else if (type == "Smoothing Cubic" && newPoints != null && newMesh != null)
            {
                newCurve = logic.CreateSmoothingSpline(newMesh, newPoints, newSmoothingFactorAlpha, newSmoothingFactorBeta);
            }

            if (newCurve == null)
            {
                await ErrorHelper.ShowError("Ошибка", "Не удалось изменить сплайн.");
                return;
            }

            int index = CurveList.IndexOf(SelectedCurve);
            if (index >= 0 && newCurve.IsPossible)
            {
                newCurve.ControlPointsFile = newPointsFile;
                newCurve.GridFile = newMeshFile;
                newCurve.SmoothingCoefficientAlpha = newSmoothingFactorAlpha;
                newCurve.SmoothingCoefficientBeta = newSmoothingFactorBeta;
                newCurve.Color = SelectedCurve.Color;

                CurveList[index] = newCurve;
                DrawCurves();
            }
            else if (index >= 0 && !newCurve.IsPossible)
            {
                if (type == "Smoothing Cubic")
                {
                    await ErrorHelper.ShowError("Ошибка", "Не удалось решить СЛАУ. Выберите другой коэффициент сглаживания.");
                }

                CurveList.Remove(SelectedCurve);
                DrawCurves();
            }
        }

        private async void EditCurve()
        {            
            if (SelectedCurve != null && SelectedCurve.Type == "Function")
            {
                EditFunction();
            }
            if (SelectedCurve != null && SelectedCurve.Type == "Spline")
            {
                EditSpline();
            }
            if (SelectedCurve == null)
            {
                await ErrorHelper.ShowError("Ошибка", "Выберите кривую для редактирования");
            }
        }

        private void DeleteSelectedCurve()
        {
            if (SelectedCurve != null && CurveList.Contains(SelectedCurve))
            {
                CurveList.Remove(SelectedCurve);
                SelectedCurve = null;
                DrawCurves();
            }
        }
        
        public void DrawCurves()
        {
            if (GraphicCanvas == null)
                return;

            GraphicCanvas.Children.Clear();
            DrawGrid();

            foreach (var curve in CurveList)
            {
                if (curve.Type == "Spline" && curve.IsVisible && curve.IsPossible)
                {
                    var points = new Points();

                    if (curve.SplineType != "Linear")
                    {
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
                                Stroke = new SolidColorBrush(curve.Color),
                                StrokeThickness = 2
                            };
                            GraphicCanvas.Children.Add(polyline);
                        }
                    }
                    if (curve.SplineType == "Linear")
                    {
                        foreach (var p in curve.ControlPoints)
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
                                Stroke = new SolidColorBrush(curve.Color),
                                StrokeThickness = 2
                            };
                            GraphicCanvas.Children.Add(polyline);
                        }
                    }

                    // контрольные точки
                    if (curve.ControlPoints != null)
                    {
                        foreach (var p in curve.ControlPoints)
                        {
                            var screenPoint = new Avalonia.Point(
                                (p.X * _zoom) + CenterX() + _offsetX,
                                (-p.Y * _zoom) + CenterY() + _offsetY
                            );

                            var ellipse = new Ellipse
                            {
                                Width = 6,
                                Height = 6,
                                Fill = new SolidColorBrush(curve.Color),
                                Stroke = new SolidColorBrush(curve.Color),
                                StrokeThickness = 1
                            };

                            Canvas.SetLeft(ellipse, screenPoint.X - 3); // центрируем круг
                            Canvas.SetTop(ellipse, screenPoint.Y - 3);

                            GraphicCanvas.Children.Add(ellipse);
                        }
                    }
                }

                if (curve.Type == "Function" && curve.IsVisible && curve.IsPossible)
                {
                    double width = GraphicCanvas.Bounds.Width;
                    double height = GraphicCanvas.Bounds.Height;

                    double startX = -(CenterX() + _offsetX) / _zoom;
                    double endX = (width - CenterX() - _offsetX) / _zoom;

                    int maxPoints = 1000;
                    double visibleWidth = endX - startX;
                    double step = visibleWidth / maxPoints;

                    var points = new Points();

                    double lastY = double.NaN; // для отслеживания разрывов

                    for (double x = startX; x <= endX; x += step)
                    {
                        double y = curve.CalculateFunctionValue(curve.FunctionString, x);

                        // если значение функции бесконечно или неопределено, разрываем отрисовку
                        if (double.IsNaN(y) || double.IsInfinity(y))
                        {
                            if (points.Count >= 2)
                            {
                                var polyline = new Polyline
                                {
                                    Points = new Points(points),
                                    Stroke = new SolidColorBrush(curve.Color),
                                    StrokeThickness = 2
                                };
                                GraphicCanvas.Children.Add(polyline);
                            }

                            points.Clear();
                            lastY = double.NaN;
                            continue;
                        }

                        var screenPoint = new Avalonia.Point(
                            (x * _zoom) + CenterX() + _offsetX,
                            (-y * _zoom) + CenterY() + _offsetY
                        );

                        if (screenPoint.Y >= -height && screenPoint.Y <= height * 2)
                        {
                            points.Add(screenPoint);
                            lastY = y;
                        }
                        else
                        {
                            if (points.Count >= 2)
                            {
                                var polyline = new Polyline
                                {
                                    Points = new Points(points),
                                    Stroke = new SolidColorBrush(curve.Color),
                                    StrokeThickness = 2
                                };
                                GraphicCanvas.Children.Add(polyline);
                            }

                            points.Clear();
                        }
                    }

                    if (points.Count >= 2)
                    {
                        var polyline = new Polyline
                        {
                            Points = points,
                            Stroke = new SolidColorBrush(curve.Color),
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

        private const double MinZoom = 0.00001;
        private const double MaxZoom = 10000;

        public void HandleZoom(double delta)
        {
            double centerX = CenterX();
            double centerY = CenterY();

            double worldX = (centerX - _offsetX - centerX) / _zoom;
            double worldY = (centerY - _offsetY - centerY) / _zoom;

            _zoom *= delta > 0 ? 1.1 : 0.9;
            _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoom));

            _offsetX = -(worldX * _zoom);
            _offsetY = -(worldY * _zoom);

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

            double exponent = Math.Pow(10, Math.Floor(Math.Log10(targetStep)));
            double[] mantissas = { 1, 2, 5 };

            foreach (var m in mantissas)
            {
                double s = m * exponent;
                if (s >= targetStep)
                    return s;
            }

            return 10 * exponent;
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
                    FontSize = 10
                };

                // определяем, где разместить подпись по оси X
                double labelY = CenterY() + _offsetY + 2;
                if (labelY < 0 || labelY > height)
                {
                    // размещаем внизу или вверху в зависимости от четверти
                    labelY = (_offsetY >= 0) ? height - 20 : 10;
                }

                Canvas.SetLeft(text, screenX + 2);
                Canvas.SetTop(text, labelY);
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
                    FontSize = 10
                };

                // определяем, где разместить подпись по оси Y
                double labelX = CenterX() + _offsetX + 2;
                if (labelX < 0 || labelX > width)
                {
                    labelX = (_offsetX >= 0) ? width - 20 : 10;
                }

                Canvas.SetLeft(text, labelX);
                Canvas.SetTop(text, screenY + 2);
                GraphicCanvas.Children.Add(text);
            }

            // логика для размещения осей по четвертям
            double axisXScreen = CenterY() + _offsetY;
            double axisYScreen = CenterX() + _offsetX;

            bool isXAxisVisible = axisXScreen >= 0 && axisXScreen <= height;
            bool isYAxisVisible = axisYScreen >= 0 && axisYScreen <= width;

            if (!isXAxisVisible)
            {
                // если Y >= 0 (центр выше) -> 1 или 2 четверть -> ось X внизу
                // если Y < 0 (центр ниже) -> 3 или 4 четверть -> ось X сверху
                axisXScreen = (_offsetY >= 0) ? height - 1 : 0;
            }

            if (!isYAxisVisible)
            {
                // если X >= 0 (центр справа) -> 1 или 4 четверть -> ось Y справа
                // если X < 0 (центр слева) -> 2 или 3 четверть -> ось Y слева
                axisYScreen = (_offsetX >= 0) ? width - 1 : 0;
            }

            var xAxis = new Line
            {
                StartPoint = new Avalonia.Point(0, axisXScreen),
                EndPoint = new Avalonia.Point(width, axisXScreen),
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2
            };
            GraphicCanvas.Children.Add(xAxis);

            var yAxis = new Line
            {
                StartPoint = new Avalonia.Point(axisYScreen, 0),
                EndPoint = new Avalonia.Point(axisYScreen, height),
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