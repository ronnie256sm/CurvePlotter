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
        public ReactiveCommand<Unit, Unit> DeleteCurveCommand { get; }
        public ReactiveCommand<Unit, Unit> EditCurveCommand { get; }

        public MainWindowViewModel()
        {
            // Initialize commands
            AddSplineCommand = ReactiveCommand.Create(AddSpline);
            AddFunctionCommand = ReactiveCommand.Create(AddFunction);
            DeleteCurveCommand = ReactiveCommand.Create(DeleteSelectedCurve);
            EditCurveCommand = ReactiveCommand.Create(EditCurve);

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
        }

        private void Curve_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICurve.IsVisible))
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
                var curve = logic.CreateCurve("Function", null, result, null, null, null);
                if (curve.IsPossible)
                    CurveList.Add(curve);
                DrawCurves();
            }
        }

        private async void EditFunction()
        {
            if (SelectedCurve is not ICurve selectedFunction)
            {
                await ErrorHelper.ShowError(null, "Выберите функцию для редактирования.");
                return;
            }

            var dialog = new FunctionInputDialog();
            dialog.SetInitialFunction(selectedFunction.FunctionString);

            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
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
            string smoothingCoefficient = null;
            string meshFile = null;

            // Обрабатываем диалог в зависимости от типа
            if (inputDialog is InterpolatingSplineInputDialog interpolatingDialog)
            {
                pointsFile = interpolatingDialog.PointsFile;
            }
            else if (inputDialog is SmoothingSplineInputDialog smoothingDialog)
            {
                pointsFile = smoothingDialog.PointsFile;
                smoothingCoefficient = smoothingDialog.SmoothingFactor;  // Получаем коэффициент сглаживания как строку
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
                await ErrorHelper.ShowError(MainWindow, "Пожалуйста, выберите файл с точками.");
                return;
            }

            // Чтение точек из файла
            var points = FileReader.ReadPoints(pointsFile);
            double[] mesh = null;

            // Проверка на наличие файла сетки для сглаживающего сплайна
            if (!string.IsNullOrWhiteSpace(meshFile))
            {
                mesh = FileReader.ReadGrid(meshFile);
            }
            else if (choice == AddCurveDialog.CurveType.SmoothingSpline)
            {
                var MainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as Avalonia.Controls.Window; // Получаем главное окно
                await ErrorHelper.ShowError(MainWindow, "Для сглаживающего сплайна необходимо выбрать файл сетки.");
                return;
            }

            // Определяем тип сплайна
            string type = choice == AddCurveDialog.CurveType.SmoothingSpline ? "Smoothing Cubic" : "Interpolating Cubic";

            // Логика создания кривой с сохранением путей файлов
            var logic = new SplineLogic();
            var curve = logic.CreateCurve("Spline", type, null, mesh, points, smoothingCoefficient);
            curve.ControlPointsFile = pointsFile;
            curve.GridFile = meshFile;
            
            // Добавляем кривую в список
            if (curve.IsPossible)
                CurveList.Add(curve);

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
            string newSmoothingFactor = null;

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
                        spline.SmoothingCoefficient ?? ""
                    );
                }

                await dialog.ShowDialog(mainWindow);

                if (!dialog.IsOkClicked)
                    return;

                newPointsFile = dialog.PointsFile;
                newMeshFile = dialog.MeshFile;
                newSmoothingFactor = dialog.SmoothingFactor;
            }
            else
            {
                await ErrorHelper.ShowError(mainWindow, "Редактирование доступно только для сплайнов.");
                return;
            }

            // Читаем новые точки и сетку
            if (string.IsNullOrWhiteSpace(newPointsFile))
            {
                await ErrorHelper.ShowError(mainWindow, "Не выбран файл точек.");
                return;
            }

            var newPoints = FileReader.ReadPoints(newPointsFile);
            double[] newMesh = null;

            if (type == "Smoothing Cubic")
            {
                if (string.IsNullOrWhiteSpace(newMeshFile))
                {
                    await ErrorHelper.ShowError(mainWindow, "Не выбран файл сетки.");
                    return;
                }
                newMesh = FileReader.ReadGrid(newMeshFile);
            }

            // Пересоздаем кривую
            var logic = new SplineLogic();
            var newCurve = logic.CreateCurve("Spline", type, null, newMesh, newPoints, newSmoothingFactor);
            newCurve.ControlPointsFile = newPointsFile;
            newCurve.GridFile = newMeshFile;

            // Заменяем в списке
            int index = CurveList.IndexOf(SelectedCurve);
            if (index >= 0 && newCurve.IsPossible)
            {
                CurveList[index] = newCurve;
                DrawCurves(); // Обновляем отрисовку
            }
            if (index >= 0 && !newCurve.IsPossible)
            {
                CurveList.Remove(SelectedCurve);
                DrawCurves();
            }
        }

        private async void EditCurve()
        {
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            
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
                await ErrorHelper.ShowError(mainWindow, "Выберите кривую для редактирования");
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

            // Отрисовываем кривые
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
                            Stroke = Brushes.Black,
                            StrokeThickness = 2
                        };
                        GraphicCanvas.Children.Add(polyline);
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