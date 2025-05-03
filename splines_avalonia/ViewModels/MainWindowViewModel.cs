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

        public ReactiveCommand<Unit, Unit> AddSplineCommand { get; }
        public ReactiveCommand<Unit, Unit> AddFunctionCommand { get; }
        public ReactiveCommand<ICurve, Unit> EditCurveCommand { get; }
        public ReactiveCommand<ICurve, Unit> DeleteCurveCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveJsonCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadJsonCommand { get; }
        public ReactiveCommand<Unit, Unit> SavePngCommand { get; }
        public MainWindowViewModel()
        {
            // Initialize commands
            AddSplineCommand = ReactiveCommand.Create(AddSpline);
            AddFunctionCommand = ReactiveCommand.Create(AddFunction);
            // Изменяем команды для работы с параметром
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

            var result = await dialog.ShowDialog<string>(mainWindow);

            if (!string.IsNullOrWhiteSpace(result))
            {
                var logic = new SplineLogic();
                var curve = logic.CreateFunction(result);
                if (curve.IsPossible)
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

        private async void AddSpline()
        {
            var chooser = new AddCurveDialog();
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow; // Преобразуем в Window
            var choice = await chooser.ShowDialog<AddCurveDialog.CurveType?>(mainWindow);

            if (choice == null) return; // Если не выбран тип кривой, выходим

            // Создаем диалог для ввода данных в зависимости от типа кривой
            Window inputDialog;
            if (choice == AddCurveDialog.CurveType.InterpolatingSpline)
            {
                inputDialog = new InterpolatingSplineInputDialog();
            }
            else
            {
                inputDialog = new SmoothingSplineInputDialog();
            }

            await inputDialog.ShowDialog(mainWindow); // Передаем правильный тип: window

            // Проверяем, был ли выбран файл точек
            string pointsFile = null;
            string smoothingCoefficientAlpha = null;
            string smoothingCoefficientBeta = null;
            string meshFile = null;

            // Обрабатываем диалог в зависимости от типа
            if (inputDialog is InterpolatingSplineInputDialog interpolatingDialog)
            {
                pointsFile = interpolatingDialog.PointsFile;
            }
            else if (inputDialog is SmoothingSplineInputDialog smoothingDialog)
            {
                pointsFile = smoothingDialog.PointsFile;
                smoothingCoefficientAlpha = smoothingDialog.SmoothingFactorAlpha;
                smoothingCoefficientBeta = smoothingDialog.SmoothingFactorBeta;  // Получаем коэффициент сглаживания как строку
                meshFile = smoothingDialog.MeshFile;
            }

            // Проверка, был ли закрыт диалог без нажатия "ОК"
            if (inputDialog is InterpolatingSplineInputDialog interpolating && !interpolating.IsOkClicked)
            {
                return; // Закрытие окна без ошибок
            }
            if (inputDialog is SmoothingSplineInputDialog smoothing && !smoothing.IsOkClicked)
            {
                return; // Закрытие окна без ошибок
            }

            // Если файл точек не выбран, выходим
            if (string.IsNullOrWhiteSpace(pointsFile))
            {
                var MainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as Avalonia.Controls.Window; // Получаем главное окно

                await ErrorHelper.ShowError("Ошибка", "Пожалуйста, выберите файл с точками.");
                return;
            }

            // Чтение точек из файла
            var points = FileReader.ReadPoints(pointsFile);
            double[] mesh = null;

            // Проверка на наличие файла сетки для сглаживающего сплайна
            if (!string.IsNullOrWhiteSpace(meshFile))
            {
                mesh = await FileReader.ReadGrid(meshFile);
            }
            else if (choice == AddCurveDialog.CurveType.SmoothingSpline)
            {
                var MainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as Avalonia.Controls.Window; // Получаем главное окно
                await ErrorHelper.ShowError("Ошибка", "Для сглаживающего сплайна необходимо выбрать файл сетки.");
                return;
            }

            // Определяем тип сплайна
            string type = choice == AddCurveDialog.CurveType.SmoothingSpline ? "Smoothing Cubic" : "Interpolating Cubic";

            // Логика создания кривой с сохранением путей файлов
            var logic = new SplineLogic();
            if (type == "Interpolating Cubic")
            {
                var curve = logic.CreateInterpolatingSpline(await points);
                if (curve != null)
                {
                    if (curve.IsPossible)
                    {
                        curve.ControlPointsFile = pointsFile;
                        CurveList.Add(curve);
                    }
                }
            }
            else if (type == "Smoothing Cubic")
            {
                var curve = logic.CreateSmoothingSpline(mesh, await points, smoothingCoefficientAlpha, smoothingCoefficientBeta);
                if (curve != null)
                {
                    if (curve.IsPossible && curve != null)
                    {
                        curve.ControlPointsFile = pointsFile;
                        curve.GridFile = meshFile;
                        CurveList.Add(curve);
                    }
                }
                else
                    await ErrorHelper.ShowError("Ошибка", "Не удалось решить СЛАУ. Выберите другой коэффициент сглаживания.");
            }
            else
            {
                await ErrorHelper.ShowError("Ошибка", "Не удалось добавить сплайн.");
            }
            // Перерисовываем кривые
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

            // Проверка типа сплайна
            if (type == "Interpolating Cubic")
            {
                var dialog = new InterpolatingSplineInputDialog();
                
                // Передаем текущий путь к файлу точек
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
                
                // Передаем текущие значения для путей файлов и коэффициента сглаживания
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

            // Читаем новые точки и сетку
            if (string.IsNullOrWhiteSpace(newPointsFile))
            {
                await ErrorHelper.ShowError("Ошибка", "Не выбран файл точек.");
                return;
            }

            var newPoints = FileReader.ReadPoints(newPointsFile);
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

            // Пересоздаем кривую
            var logic = new SplineLogic();
            if (type == "Interpolating Cubic")
            {
                var newCurve = logic.CreateInterpolatingSpline(await newPoints);
                int index = CurveList.IndexOf(SelectedCurve);
                if (newCurve != null)
                {
                    if (index >= 0 && newCurve.IsPossible)
                    {
                        newCurve.ControlPointsFile = newPointsFile;
                        newCurve.Color = SelectedCurve.Color;
                        CurveList[index] = newCurve;
                        DrawCurves(); // Обновляем отрисовку
                    }
                    if (index >= 0 && !newCurve.IsPossible)
                    {
                        CurveList.Remove(SelectedCurve);
                        DrawCurves();
                    }
                }
            }
            else if (type == "Smoothing Cubic" && newPoints != null && newMesh != null)
            {
                var newCurve = logic.CreateSmoothingSpline(newMesh, await newPoints, newSmoothingFactorAlpha, newSmoothingFactorBeta);
                int index = CurveList.IndexOf(SelectedCurve);
                if (newCurve != null)
                {
                    if (index >= 0 && newCurve.IsPossible)
                    {
                        newCurve.ControlPointsFile = newPointsFile;
                        newCurve.GridFile = newMeshFile;
                        newCurve.Color = SelectedCurve.Color;
                        CurveList[index] = newCurve;
                        DrawCurves(); // Обновляем отрисовку
                    }
                    if (index >= 0 && !newCurve.IsPossible)
                    {
                        await ErrorHelper.ShowError("Ошибка", "Не удалось решить СЛАУ. Выберите другой коэффициент сглаживания.");
                        CurveList.Remove(SelectedCurve);
                        DrawCurves();
                    }
                }
            }
            else
            {
                await ErrorHelper.ShowError("Ошибка", "Не удалось изменить сплайн.");
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

                    for (double x = startX; x <= endX; x += step)
                    {
                        double y = curve.CalculateFunctionValue(curve.FunctionString, x);

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
        private const double MaxOffset = 1000000000; // можно изменить при необходимости

        public void HandleZoom(double delta)
        {
            // Центр канваса (куда обычно масштабируется)
            double centerX = CenterX();
            double centerY = CenterY();

            // Мировые координаты центра до изменения зума
            double worldX = (centerX - _offsetX - centerX) / _zoom;
            double worldY = (centerY - _offsetY - centerY) / _zoom;

            // Меняем зум
            _zoom *= delta > 0 ? 1.1 : 0.9;

            // Ограничение зума
            _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoom));

            // Пересчитываем смещения так, чтобы та же мировая точка снова оказалась в центре
            _offsetX = -(worldX * _zoom);
            _offsetY = -(worldY * _zoom);

            DrawCurves();
        }

        public void Scale(double times)
        {
            if (times > 0)
            {
                _zoom *= times;
                DrawCurves();
            }
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

                // Определяем, где разместить подпись по оси X
                double labelY = CenterY() + _offsetY + 2;
                if (labelY < 0 || labelY > height)
                {
                    // Размещаем внизу или вверху в зависимости от четверти
                    labelY = (_offsetY >= 0) ? height - 15 : 2;
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
                    FontSize = 12
                };

                // Определяем, где разместить подпись по оси Y
                double labelX = CenterX() + _offsetX + 2;
                if (labelX < 0 || labelX > width)
                {
                    // !! Поменяли стороны местами !!
                    // Теперь если offsetX >= 0 (центр справа) -> текст СПРАВА,
                    // если offsetX < 0 (центр слева) -> текст СЛЕВА.
                    labelX = (_offsetX >= 0) ? width - 30 : 2;
                }

                Canvas.SetLeft(text, labelX);
                Canvas.SetTop(text, screenY + 2);
                GraphicCanvas.Children.Add(text);
            }

            // Логика для размещения осей по четвертям
            double axisXScreen = CenterY() + _offsetY;
            double axisYScreen = CenterX() + _offsetX;

            bool isXAxisVisible = axisXScreen >= 0 && axisXScreen <= height;
            bool isYAxisVisible = axisYScreen >= 0 && axisYScreen <= width;

            if (!isXAxisVisible)
            {
                // Если Y >= 0 (центр выше), значит мы в 1 или 2 четверти -> ось X внизу
                // Если Y < 0 (центр ниже), значит мы в 3 или 4 четверти -> ось X вверху
                axisXScreen = (_offsetY >= 0) ? height - 1 : 0;
            }

            if (!isYAxisVisible)
            {
                // Если X >= 0 (центр справа), значит 1 или 4 четверть -> ось Y СПРАВА (изменилась логика)
                // Если X < 0 (центр слева), значит 2 или 3 четверть -> ось Y СЛЕВА
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