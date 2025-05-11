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

#pragma warning disable CS8618, CS8604, CS8600, CS8601, CS8602, CS8625

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
        public bool ShowAxes = true;
        public bool ShowGrid = true;
        public int PointCount = 1000;
        public ReactiveCommand<Unit, Unit> AddInterpolatingSpline1Command { get; }
        public ReactiveCommand<Unit, Unit> AddInterpolatingSpline2Command { get; }
        public ReactiveCommand<Unit, Unit> AddSmoothingSplineCommand { get; }
        public ReactiveCommand<Unit, Unit> AddLinearSplineCommand { get; }
        public ReactiveCommand<Unit, Unit> AddFunctionCommand { get; }
        public ReactiveCommand<ICurve, Unit> EditCurveCommand { get; }
        public ReactiveCommand<ICurve, Unit> DeleteCurveCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveJsonCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadJsonCommand { get; }
        public ReactiveCommand<Unit, Unit> SavePngCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
        public MainWindowViewModel()
        {
            AddInterpolatingSpline1Command = ReactiveCommand.Create(() => AddSpline("Interpolating Cubic 1"));
            AddInterpolatingSpline2Command = ReactiveCommand.Create(() => AddSpline("Interpolating Cubic 2"));
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
            OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);

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

        public async void OpenSettings()
        {
            var settingsWindow = new SettingsWindow(ShowAxes, ShowGrid, PointCount);
            await settingsWindow.ShowDialog(App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);

            if (settingsWindow.IsOkClicked)
            {
                ShowAxes = settingsWindow.ShowAxes;
                ShowGrid = settingsWindow.ShowGrid;
                PointCount = settingsWindow.PointCount;
                DrawCurves();
            }
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
                curve.GetLimits();
                if (curve.ParsedStart > curve.ParsedEnd)
                {
                    await ErrorHelper.ShowError("Ошибка", "Начало области определения не может быть больше конца. Ограничения были сброшены.");
                    curve.Start = null;
                    curve.End = null;
                    curve.GetLimits();
                }
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
            dialog.SetInitialFunction(selectedFunction.FunctionString, selectedFunction.Start, selectedFunction.End);
            var result = await dialog.ShowDialog<(string FunctionString, string Start, string End)>(mainWindow);

            if (!string.IsNullOrWhiteSpace(result.FunctionString))
            {
                selectedFunction.FunctionString = result.FunctionString;
                selectedFunction.Start = result.Start;
                selectedFunction.End = result.End;
                if (!SelectedCurve.IsPossible)
                {
                    CurveList.Remove(SelectedCurve);
                }
                SelectedCurve.GetLimits();
                if (SelectedCurve.ParsedStart > SelectedCurve.ParsedEnd)
                {
                    await ErrorHelper.ShowError("Ошибка", "Начало области определения не может быть больше конца. Ограничения были сброшены.");
                    SelectedCurve.Start = null;
                    SelectedCurve.End = null;
                    SelectedCurve.GetLimits();
                }
                DrawCurves();
            }
        }

        private async void AddSpline(string type)
        {
            var inputDialog = new InterpolatingSplineInputDialog(type, true);
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

            var points = await FileService.ReadPoints(pointsFile);

            ICurve curve = null;
            var logic = new SplineLogic();
            if (type == "Interpolating Cubic 2")
            {
                curve = logic.CreateInterpolatingSpline(points, 2);
                curve.ShowControlPoints = inputDialog.ShowControlPoints;
            }
            if (type == "Interpolating Cubic 1")
            {
                curve = logic.CreateInterpolatingSpline(points, 1);
                curve.ShowControlPoints = inputDialog.ShowControlPoints;
            }
            else if (type == "Linear")
            {
                curve = logic.CreateLinearSpline(points);
                curve.ShowControlPoints = inputDialog.ShowControlPoints;
            }

            if (curve != null && curve.IsPossible)
            {
                curve.ControlPointsFile = pointsFile;
                CurveList.Add(curve);
            }

            DrawCurves();
        }

        private async void AddSmoothingSpline()
        {
            var inputDialog = new SmoothingSplineInputDialog(true);
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            await inputDialog.ShowDialog(mainWindow);

            if (!inputDialog.IsOkClicked)
                return;

            string pointsFile = inputDialog.PointsFile;
            string meshFile = inputDialog.MeshFile;
            string smoothingAlpha = inputDialog.SmoothingFactorAlpha;
            string smoothingBeta = inputDialog.SmoothingFactorBeta;
            bool showControlPoints = inputDialog.ShowControlPoints;

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

            var points = await FileService.ReadPoints(pointsFile);
            var mesh = await FileService.ReadGrid(meshFile);

            var logic = new SplineLogic();
            var curve = logic.CreateSmoothingSpline(mesh, points, smoothingAlpha, smoothingBeta);

            if (curve != null && curve.IsPossible)
            {
                curve.ControlPointsFile = pointsFile;
                curve.GridFile = meshFile;
                curve.ShowControlPoints = showControlPoints;
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
            bool newShowControlPoints = true;

            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (type == "Linear" || type == "Interpolating Cubic 2" || type == "Interpolating Cubic 1")
            {
                var dialog = new InterpolatingSplineInputDialog(type, SelectedCurve.ShowControlPoints);

                if (SelectedCurve is ICurve spline && !string.IsNullOrWhiteSpace(spline.ControlPointsFile))
                {
                    dialog.SetInitialValues(spline.ControlPointsFile, spline.ShowControlPoints);
                }

                await dialog.ShowDialog(mainWindow);

                if (!dialog.IsOkClicked)
                    return;

                newPointsFile = dialog.PointsFile;
                newShowControlPoints = dialog.ShowControlPoints;
            }
            else if (type == "Smoothing Cubic")
            {
                var dialog = new SmoothingSplineInputDialog(SelectedCurve.ShowControlPoints);

                if (SelectedCurve is ICurve spline)
                {
                    dialog.SetInitialValues(
                        spline.ControlPointsFile ?? "",
                        spline.GridFile ?? "",
                        spline.SmoothingCoefficientAlpha ?? "",
                        spline.SmoothingCoefficientBeta ?? "",
                        spline.ShowControlPoints
                    );
                }

                await dialog.ShowDialog(mainWindow);

                if (!dialog.IsOkClicked)
                    return;

                newPointsFile = dialog.PointsFile;
                newMeshFile = dialog.MeshFile;
                newSmoothingFactorAlpha = dialog.SmoothingFactorAlpha;
                newSmoothingFactorBeta = dialog.SmoothingFactorBeta;
                newShowControlPoints = dialog.ShowControlPoints;
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

            var newPoints = await FileService.ReadPoints(newPointsFile);
            double[] newMesh = null;

            if (type == "Smoothing Cubic")
            {
                if (string.IsNullOrWhiteSpace(newMeshFile))
                {
                    await ErrorHelper.ShowError("Ошибка", "Не выбран файл сетки.");
                    return;
                }

                newMesh = await FileService.ReadGrid(newMeshFile);
            }

            var logic = new SplineLogic();
            ICurve newCurve = null;

            if (type == "Linear")
            {
                newCurve = logic.CreateLinearSpline(newPoints);
                newCurve.ShowControlPoints = newShowControlPoints;
            }
            else if (type == "Interpolating Cubic 2")
            {
                newCurve = logic.CreateInterpolatingSpline(newPoints, 2);
                newCurve.ShowControlPoints = newShowControlPoints;
            }
            else if (type == "Interpolating Cubic 1")
            {
                newCurve = logic.CreateInterpolatingSpline(newPoints, 1);
                newCurve.ShowControlPoints = newShowControlPoints;
            }
            else if (type == "Smoothing Cubic" && newPoints != null && newMesh != null)
            {
                newCurve = logic.CreateSmoothingSpline(newMesh, newPoints, newSmoothingFactorAlpha, newSmoothingFactorBeta);
                newCurve.ShowControlPoints = newShowControlPoints;
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
                newCurve.ShowControlPoints = newShowControlPoints;
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

        private void DrawGrid()
        {
            double width = GraphicCanvas.Bounds.Width;
            double height = GraphicCanvas.Bounds.Height;

            double step = CalculateGridStep();

            double startX = -(CenterX() + _offsetX) / _zoom;
            double endX = (width - CenterX() - _offsetX) / _zoom;

            double startY = -(CenterY() + _offsetY) / _zoom;
            double endY = (height - CenterY() - _offsetY) / _zoom;

            // Отрисовка линий сетки
            if (ShowGrid)
            {
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
                }
            }

            // Отрисовка осей и числовых подписей
            if (ShowAxes)
            {
                // Подписи вдоль оси X
                for (double x = Math.Floor(startX / step) * step; x <= endX; x += step)
                {
                    double screenX = x * _zoom + CenterX() + _offsetX;

                    string labelText = Math.Abs(x) < 1e-6 ? "0" : x.ToString("G12");
                    var text = new TextBlock
                    {
                        Text = labelText,
                        Foreground = Brushes.Gray,
                        FontSize = 10
                    };

                    double labelY = CenterY() + _offsetY + 2;
                    if (labelY < 0 || labelY > height)
                    {
                        labelY = (_offsetY >= 0) ? height - 20 : 10;
                    }

                    Canvas.SetLeft(text, screenX + 2);
                    Canvas.SetTop(text, labelY);
                    GraphicCanvas.Children.Add(text);
                }

                // Подписи вдоль оси Y
                for (double y = Math.Floor(startY / step) * step; y <= endY; y += step)
                {
                    double screenY = y * _zoom + CenterY() + _offsetY;

                    string labelText = Math.Abs(y) < 1e-6 ? "0" : (-y).ToString("G12");
                    var text = new TextBlock
                    {
                        Text = labelText,
                        Foreground = Brushes.Gray,
                        FontSize = 10
                    };

                    double labelX = CenterX() + _offsetX + 2;
                    if (labelX < 0 || labelX > width)
                    {
                        labelX = (_offsetX >= 0) ? width - 20 : 10;
                    }

                    Canvas.SetLeft(text, labelX);
                    Canvas.SetTop(text, screenY + 2);
                    GraphicCanvas.Children.Add(text);
                }

                // Отрисовка осей
                double axisXScreen = CenterY() + _offsetY;
                double axisYScreen = CenterX() + _offsetX;

                bool isXAxisVisible = axisXScreen >= 0 && axisXScreen <= height;
                bool isYAxisVisible = axisYScreen >= 0 && axisYScreen <= width;

                if (!isXAxisVisible)
                {
                    axisXScreen = (_offsetY >= 0) ? height - 1 : 0;
                }

                if (!isYAxisVisible)
                {
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
                    if (curve.ControlPoints != null && curve.ShowControlPoints)
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

                    // определяем текущие границы видимости на экране
                    double visibleLeft = -(CenterX() + _offsetX) / _zoom;
                    double visibleRight = (width - CenterX() - _offsetX) / _zoom;

                    double funcLeft = curve.ParsedStart;
                    double funcRight = curve.ParsedEnd;

                    // проверяем видимость функции
                    bool shouldRender = 
                        (funcRight > visibleLeft) &&  // правая граница функции правее левой границы экрана
                        (funcLeft < visibleRight);    // левая граница функции левее правой границы экрана

                    if (!shouldRender) continue;

                    // определяем реальные границы отрисовки
                    double renderStart = Math.Max(funcLeft, visibleLeft);
                    double renderEnd = Math.Min(funcRight, visibleRight);
                    double renderWidth = renderEnd - renderStart;

                    if (PointCount <= 0) return;
                    double step = renderWidth / PointCount;

                    // отрисовка функции
                    var points = new Points();
                    for (int i = 0; i <= PointCount; i++)
                    {
                        double x = renderStart + i * step;
                        double y = curve.CalculateFunctionValue(curve.FunctionString, x);

                        if (double.IsNaN(y) || double.IsInfinity(y))
                        {
                            if (points.Count >= 2)
                            {
                                GraphicCanvas.Children.Add(new Polyline
                                {
                                    Points = new Points(points),
                                    Stroke = new SolidColorBrush(curve.Color),
                                    StrokeThickness = 2
                                });
                            }
                            points.Clear();
                            continue;
                        }

                        var screenPoint = new Avalonia.Point(
                            (x * _zoom) + CenterX() + _offsetX,
                            (-y * _zoom) + CenterY() + _offsetY
                        );

                        if (screenPoint.Y >= -height && screenPoint.Y <= height * 2)
                        {
                            points.Add(screenPoint);
                        }
                        else if (points.Count >= 2)
                        {
                            GraphicCanvas.Children.Add(new Polyline
                            {
                                Points = new Points(points),
                                Stroke = new SolidColorBrush(curve.Color),
                                StrokeThickness = 2
                            });
                            points.Clear();
                        }
                    }

                    if (points.Count >= 2)
                    {
                        GraphicCanvas.Children.Add(new Polyline
                        {
                            Points = points,
                            Stroke = new SolidColorBrush(curve.Color),
                            StrokeThickness = 2
                        });
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

        private const double MinZoom = 0.000001;
        private const double MaxZoom = 10000;
        public void ZoomIn()
        {
            _zoom = Math.Min(MaxZoom, _zoom * 2.0);
            DrawCurves();
        }

        public void ZoomOut()
        {
            _zoom = Math.Max(MinZoom, _zoom * 0.5);
            DrawCurves();
        }

        public void MoveLeft()
        {
            _offsetX += CalculateGridStep() * _zoom;
            DrawCurves();
        }

        public void MoveRight()
        {
            _offsetX -= CalculateGridStep() * _zoom;
            DrawCurves();
        }

        public void MoveUp()
        {
            _offsetY += CalculateGridStep() * _zoom;
            DrawCurves();
        }

        public void MoveDown()
        {
            _offsetY -= CalculateGridStep() * _zoom;
            DrawCurves();
        }
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

        public void ResetPosition()
        {
            _offsetX = 0;
            _offsetY = 0;
            _zoom = 50;
            DrawCurves();
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
    }
}