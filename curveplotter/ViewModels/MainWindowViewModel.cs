using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using ReactiveUI;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CurvePlotter.Views;
using CurvePlotter.Helpers;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS8604

namespace CurvePlotter.ViewModels
{
    public static class Globals // глобальные настройки
    {
        public static bool ShowAxes = true;
        public static bool ShowGrid = true;
        public static int PointCount = 1000;
        public static Color XAxisColor = Colors.DarkGray;
        public static Color YAxisColor = Colors.DarkGray;
        public static bool DarkMode = false;
        public static bool AutomaticColor = false;
    }

    public class MainWindowViewModel : ViewModelBase
    {
        public Canvas? _graphicCanvas;
        private TextBlock? _statusBar;
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

        public Canvas? GraphicCanvas
        {
            get => _graphicCanvas;
            set => this.RaiseAndSetIfChanged(ref _graphicCanvas, value);
        }
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
        public ReactiveCommand<ICurve, Unit> CalculateValueCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowAboutDialogCommand { get; }
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

            CalculateValueCommand = ReactiveCommand.Create<ICurve>(curve =>
            {
                SelectedCurve = curve;
                CalculateValue(curve);
            });

            SaveJsonCommand = ReactiveCommand.CreateFromTask(() => IO.SaveJSON(CurveList));
            LoadJsonCommand = ReactiveCommand.CreateFromTask(() => IO.LoadJSON());
            SavePngCommand = ReactiveCommand.CreateFromTask(() => IO.SavePNG());
            OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);
            ShowAboutDialogCommand = ReactiveCommand.CreateFromTask(ShowAboutDialogAsync);

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
            if (e.PropertyName == nameof(ICurve.IsVisible) || e.PropertyName == nameof(ICurve.Color) || e.PropertyName == nameof(ICurve.Thickness))
            {
                DrawCurves();
            }
        }

        private async void CalculateValue(ICurve curve) // окно нахождения значения в точке
        {
            var dialog = new CalculateValueDialog();
            dialog.SetCurve(curve);
            await dialog.ShowDialog((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        }

        public async void OpenSettings()
        {
            var settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);

            if (settingsWindow.IsOkClicked)
            {
                DrawCurves();
            }
        }

        private async Task ShowAboutDialogAsync()
        {
            var dialog = new AboutDialog();
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            await dialog.ShowDialog(mainWindow);
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
                if (curve != null)
                {
                    curve.Start = result.Start;
                    curve.End = result.End;
                    if (curve.IsPossible)
                    {
                        CurveList.Add(curve);
                        curve.GetLimits();
                        if (curve.ParsedStart > curve.ParsedEnd)
                        {
                            await ErrorHelper.ShowError("Ошибка", "Начало области определения не может быть больше конца. Ограничения были сброшены.");
                            curve.Start = null;
                            curve.End = null;
                            curve.GetLimits();
                        }
                    }
                    DrawCurves();
                }
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

            ICurve? curve = null;
            var logic = new SplineLogic();
            if (type == "Interpolating Cubic 2" && points != null)
            {
                curve = logic.CreateInterpolatingSpline(points, 2);
                if (curve != null)
                    curve.ShowControlPoints = inputDialog.ShowControlPoints;
            }
            if (type == "Interpolating Cubic 1" && points != null)
            {
                curve = logic.CreateInterpolatingSpline(points, 1);
                if (curve != null)
                    curve.ShowControlPoints = inputDialog.ShowControlPoints;
            }
            else if (type == "Linear" && points != null)
            {
                curve = logic.CreateLinearSpline(points);
                if (curve != null)
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
            if (mesh != null && points != null)
            {
                var logic = new SplineLogic();
                var curve = logic.CreateSmoothingSpline(mesh, points, smoothingAlpha, smoothingBeta);

                if (curve != null && curve.IsPossible && points != null && mesh != null)
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
            }

            DrawCurves();
        }

        private async void EditSpline()
        {
            if (SelectedCurve == null)
                return;

            var type = SelectedCurve.SplineType;
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            string? newPointsFile = null;
            string? newMeshFile = null;
            string? newSmoothingFactorAlpha = null;
            string? newSmoothingFactorBeta = null;
            bool newShowControlPoints = true;

            // Ввод данных
            switch (type)
            {
                case "Linear":
                case "Interpolating Cubic 1":
                case "Interpolating Cubic 2":
                    {
                        var dialog = new InterpolatingSplineInputDialog(type, SelectedCurve.ShowControlPoints);
                        if (SelectedCurve is ICurve spline && !string.IsNullOrWhiteSpace(spline.ControlPointsFile))
                            dialog.SetInitialValues(spline.ControlPointsFile, spline.ShowControlPoints);

                        await dialog.ShowDialog(mainWindow);
                        if (!dialog.IsOkClicked) return;

                        newPointsFile = dialog.PointsFile;
                        newShowControlPoints = dialog.ShowControlPoints;
                        break;
                    }

                case "Smoothing Cubic":
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
                        if (!dialog.IsOkClicked) return;

                        newPointsFile = dialog.PointsFile;
                        newMeshFile = dialog.MeshFile;
                        newSmoothingFactorAlpha = dialog.SmoothingFactorAlpha;
                        newSmoothingFactorBeta = dialog.SmoothingFactorBeta;
                        newShowControlPoints = dialog.ShowControlPoints;
                        break;
                    }

                default:
                    await ErrorHelper.ShowError("Ошибка", "Редактирование доступно только для сплайнов.");
                    return;
            }

            if (string.IsNullOrWhiteSpace(newPointsFile))
            {
                await ErrorHelper.ShowError("Ошибка", "Не выбран файл точек.");
                return;
            }

            var newPoints = await FileService.ReadPoints(newPointsFile);
            double[]? newMesh = null;

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
            ICurve? newCurve = type switch
            {
                "Linear" when newPoints != null => logic.CreateLinearSpline(newPoints),
                "Interpolating Cubic 1" when newPoints != null => logic.CreateInterpolatingSpline(newPoints, 1),
                "Interpolating Cubic 2" when newPoints != null => logic.CreateInterpolatingSpline(newPoints, 2),
                "Smoothing Cubic" when newPoints != null && newMesh != null &&
                                    newSmoothingFactorAlpha != null && newSmoothingFactorBeta != null =>
                    logic.CreateSmoothingSpline(newMesh, newPoints, newSmoothingFactorAlpha, newSmoothingFactorBeta),
                _ => null
            };

            if (newCurve == null)
            {
                await ErrorHelper.ShowError("Ошибка", "Не удалось изменить сплайн.");
                return;
            }

            newCurve.ShowControlPoints = newShowControlPoints;

            int index = CurveList.IndexOf(SelectedCurve);
            string? SmoothingFactorAlphaBackup = newCurve.SmoothingCoefficientAlpha;
            string? SmoothingFactorBetaBackup = newCurve.SmoothingCoefficientBeta;
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
            else if (index >= 0)
            {
                if (type == "Smoothing Cubic")
                {
                    await ErrorHelper.ShowError("Ошибка", "Не удалось решить СЛАУ. Выберите другой коэффициент сглаживания.");
                }

                newCurve.SmoothingCoefficientAlpha = SmoothingFactorAlphaBackup;
                newCurve.SmoothingCoefficientBeta = SmoothingFactorBetaBackup;
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
            if (GraphicCanvas == null)
                return;

            double width = GraphicCanvas.Bounds.Width;
            double height = GraphicCanvas.Bounds.Height;

            double step = CalculateGridStep(); // расчет шага сетки в мировых координатах

            // определение видимого диапазона
            double startX = -(CenterX() + _offsetX) / _zoom;
            double endX = (width - CenterX() - _offsetX) / _zoom;

            double startY = -(CenterY() + _offsetY) / _zoom;
            double endY = (height - CenterY() - _offsetY) / _zoom;

            // отрисовка линий сетки
            if (Globals.ShowGrid)
            {
                // вертикальные линии сетки
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

                // горизонтальные линии сетки
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

            // отрисовка осей и числовых подписей
            if (Globals.ShowAxes)
            {
                // подписи вдоль оси X
                for (double x = Math.Floor(startX / step) * step; x <= endX; x += step)
                {
                    double screenX = x * _zoom + CenterX() + _offsetX;

                    // форматирование значения подписи
                    string labelText = Math.Abs(x) < 1e-6 ? "0" : x.ToString("G12");
                    var text = new TextBlock
                    {
                        Text = labelText,
                        Foreground = new SolidColorBrush(Globals.XAxisColor),
                        FontSize = 10
                    };

                    // расположение текста по оси Y
                    double labelY = CenterY() + _offsetY + 2;
                    if (labelY < 0 || labelY > height)
                    {
                        labelY = (_offsetY >= 0) ? height - 20 : 10;
                    }

                    Canvas.SetLeft(text, screenX + 2);
                    Canvas.SetTop(text, labelY);
                    GraphicCanvas.Children.Add(text);
                }

                // подписи вдоль оси Y
                for (double y = Math.Floor(startY / step) * step; y <= endY; y += step)
                {
                    double screenY = y * _zoom + CenterY() + _offsetY;

                    // форматирование значения подписи
                    string labelText = Math.Abs(y) < 1e-6 ? "0" : (-y).ToString("G12");
                    var text = new TextBlock
                    {
                        Text = labelText,
                        Foreground = new SolidColorBrush(Globals.YAxisColor),
                        FontSize = 10
                    };

                    // расположение текста по оси X
                    double labelX = CenterX() + _offsetX + 2;
                    if (labelX < 0 || labelX > width)
                    {
                        labelX = (_offsetX >= 0) ? width - 20 : 10;
                    }

                    Canvas.SetLeft(text, labelX);
                    Canvas.SetTop(text, screenY + 2);
                    GraphicCanvas.Children.Add(text);
                }

                // расчет положения
                double axisXScreen = CenterY() + _offsetY;
                double axisYScreen = CenterX() + _offsetX;

                // видимы ли оси на экране
                bool isXAxisVisible = axisXScreen >= 0 && axisXScreen <= height;
                bool isYAxisVisible = axisYScreen >= 0 && axisYScreen <= width;

                if (!isXAxisVisible)
                {
                    // если Y >= 0 (центр выше) -> 1 или 2 четверть -> ось X внизу
                    // если Y < 0 (центр ниже) -> 3 или 4 четверть -> ось X вверху
                    axisXScreen = (_offsetY >= 0) ? height - 1 : 0;
                }

                if (!isYAxisVisible)
                {
                    // если X >= 0 (центр справа) -> 1 или 4 четверть -> ось Y справа
                    // если X < 0 (центр слева) -> 2 или 3 четверть -> ось Y слева
                    axisYScreen = (_offsetX >= 0) ? width - 1 : 0;
                }

                // отрисовка оси X
                var xAxis = new Line
                {
                    StartPoint = new Avalonia.Point(0, axisXScreen),
                    EndPoint = new Avalonia.Point(width, axisXScreen),
                    Stroke = new SolidColorBrush(Globals.XAxisColor),
                    StrokeThickness = 2
                };
                GraphicCanvas.Children.Add(xAxis);

                // отрисовка оси Y
                var yAxis = new Line
                {
                    StartPoint = new Avalonia.Point(axisYScreen, 0),
                    EndPoint = new Avalonia.Point(axisYScreen, height),
                    Stroke = new SolidColorBrush(Globals.YAxisColor),
                    StrokeThickness = 2
                };
                GraphicCanvas.Children.Add(yAxis);
            }
        }

        public void DrawCurves()
        {
            if (GraphicCanvas == null)
                return;

            // очистка канваса
            GraphicCanvas.Children.Clear();
            GraphicCanvas.Background = new SolidColorBrush(Globals.DarkMode ? Colors.Black : Colors.White);

            DrawGrid();

            double width = GraphicCanvas.Bounds.Width;
            double height = GraphicCanvas.Bounds.Height;

            // вычисление границ видимой области по X
            double visibleLeft = -(CenterX() + _offsetX) / _zoom;
            double visibleRight = (width - CenterX() - _offsetX) / _zoom;

            foreach (var curve in CurveList)
            {
                if (!curve.IsVisible || !curve.IsPossible)
                    continue;

                // автоматическая коррекция цвета
                if (curve.Color == Colors.White && !Globals.DarkMode && Globals.AutomaticColor)
                    curve.Color = Colors.Black;
                if (curve.Color == Colors.Black && Globals.DarkMode && Globals.AutomaticColor)
                    curve.Color = Colors.White;

                // вычисление области определения кривой
                double funcLeft = 0;
                double funcRight = 0;
                if (curve.Type == "Function")
                {
                    funcLeft = curve.ParsedStart;
                    funcRight = curve.ParsedEnd;
                }
                else if (curve.ControlPoints != null)
                {
                    funcLeft = curve.ControlPoints[0].X;
                    funcRight = curve.ControlPoints.Last().X;
                }

                // проверка, находится ли кривая в пределах экрана
                bool shouldRender = (funcRight > visibleLeft) && (funcLeft < visibleRight);

                if (!shouldRender)
                    continue;

                // определение интервала, который реально будет отрисован
                double renderStart = Math.Max(funcLeft, visibleLeft);
                double renderEnd = Math.Min(funcRight, visibleRight);
                double renderWidth = renderEnd - renderStart;

                if (Globals.PointCount <= 0)
                    return;

                // вычисление шага по X
                double step = renderWidth / Globals.PointCount;

                var points = new Points();

                // вычисление и преобразование координат всех точек кривой
                for (int i = 0; i <= Globals.PointCount; i++)
                {
                    double x = renderStart + i * step;
                    double y = curve.CalculateFunctionValue(x);

                    // если NaN или бесконечность, отрисовываем предыдущий сегмент
                    if (double.IsNaN(y) || double.IsInfinity(y))
                    {
                        if (points.Count >= 2)
                        {
                            GraphicCanvas.Children.Add(new Polyline
                            {
                                Points = new Points(points),
                                Stroke = new SolidColorBrush(curve.Color),
                                StrokeThickness = curve.Thickness
                            });
                        }
                        points.Clear();
                        continue;
                    }

                    // преобразование координат из математических в экранные
                    var screenPoint = new Avalonia.Point(
                        (x * _zoom) + CenterX() + _offsetX,
                        (-y * _zoom) + CenterY() + _offsetY
                    );

                    // проверка на выход за экран по Y
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
                            StrokeThickness = curve.Thickness
                        });
                        points.Clear();
                    }
                }

                // финальный сегмент
                if (points.Count >= 2)
                {
                    GraphicCanvas.Children.Add(new Polyline
                    {
                        Points = points,
                        Stroke = new SolidColorBrush(curve.Color),
                        StrokeThickness = curve.Thickness
                    });
                }

                // контрольные точки только для сплайнов
                if (curve.Type == "Spline" && curve.ShowControlPoints && curve.ControlPoints != null)
                {
                    foreach (var p in curve.ControlPoints)
                    {
                        var screenPoint = new Avalonia.Point(
                            (p.X * _zoom) + CenterX() + _offsetX,
                            (-p.Y * _zoom) + CenterY() + _offsetY
                        );

                        double diameter = Math.Max(6, curve.Thickness * 2.5);
                        double strokeThickness = Math.Max(1, curve.Thickness);

                        var ellipse = new Ellipse
                        {
                            Width = diameter,
                            Height = diameter,
                            Fill = new SolidColorBrush(curve.Color),
                            Stroke = new SolidColorBrush(curve.Color),
                            StrokeThickness = strokeThickness
                        };

                        Canvas.SetLeft(ellipse, screenPoint.X - diameter / 2);
                        Canvas.SetTop(ellipse, screenPoint.Y - diameter / 2);

                        GraphicCanvas.Children.Add(ellipse);
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
            // получаем координаты центра
            double centerX = CenterX();
            double centerY = CenterY();

            // преобразуем экранные координаты центра в мировые для удержания фокуса в центре экрана
            double worldX = (centerX - _offsetX - centerX) / _zoom;
            double worldY = (centerY - _offsetY - centerY) / _zoom;

            _zoom *= delta > 0 ? 1.1 : 0.9;

            // ограничиваем масштаб
            _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoom));

            // пересчитываем смещения
            _offsetX = -(worldX * _zoom);
            _offsetY = -(worldY * _zoom);

            DrawCurves();
        }

        // положение курсора в момент начала перемещения
        public void StartPan(Avalonia.Point point)
        {
            _lastPanPosition = point;
        }

        public void DoPan(Avalonia.Point current)
        {
            // разница между текущей и последней позицией мыши
            var dx = current.X - _lastPanPosition.X;
            var dy = current.Y - _lastPanPosition.Y;

            // обновляем смещения канваса на сдвиг
            _offsetX += dx;
            _offsetY += dy;

            // обновляем последнюю позицию и перерисовываем
            _lastPanPosition = current;
            DrawCurves();
        }

        private double CalculateGridStep()
        {
            double minPixelStep = 40; // минимальное расстояние между линиями сетки в пикселях
            double targetStep = minPixelStep / _zoom; // целевой шаг в мировых координатах, который даст minPixelStep на экране

            // вычисляем порядок числа
            double exponent = Math.Pow(10, Math.Floor(Math.Log10(targetStep)));
            double[] mantissas = { 1, 2, 5 };

            // ищем минимальный шаг, который превышает targetStep
            foreach (var m in mantissas)
            {
                double s = m * exponent;
                if (s >= targetStep)
                    return s;
            }

            // если ни один из стандартных шагов не подошёл
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
                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
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