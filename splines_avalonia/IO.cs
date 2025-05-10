using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using splines_avalonia.Views;
using splines_avalonia.Helpers;

namespace splines_avalonia.ViewModels;

#pragma warning disable CS8602, CS8600, CS8604, CS8625

public static class IO
{
    public static async Task SaveJSON(ObservableCollection<ICurve> curves)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
            return;
        // получаем главное окно
        var storageProvider = desktop.MainWindow.StorageProvider;
        if (storageProvider == null)
            return;

            var filePickerOptions = new FilePickerSaveOptions
            {
                Title = "Сохранить как",
                SuggestedFileName = "curves", //дефолтное имя файла
                ShowOverwritePrompt = true, // предупреждение о перезаписи файла
                FileTypeChoices = new[] // забиваем доступные форматы в выпадающий список
                {
                    new FilePickerFileType("Файл JSON") { Patterns = new[] { "*.json" } }
                }
            };

            //открываем проводник
            var res = await storageProvider.SaveFilePickerAsync(filePickerOptions);

            if (res != null)
            {
                var filePath = res.Path.LocalPath; // получаем путь
                var extension = Path.GetExtension(filePath).ToLower(); // определяем расширение файла

                switch (extension)
                {
                    case ".json":
                        var curvesInfo = curves.Select(curve => new
                        {
                            Name = curve.Name,
                            Type = curve.Type,
                            FunctionString = curve.FunctionString,
                            SplineType = curve.SplineType,
                            Grid = curve.Grid,
                            ControlPoints = curve.ControlPoints,
                            ShowControlPoints = curve.ShowControlPoints,
                            Start = curve.Start,
                            End = curve.End,
                            IsVisible = curve.IsVisible,
                            SmoothingCoefficientAlpha = curve.SmoothingCoefficientAlpha,
                            SmoothingCoefficientBeta = curve.SmoothingCoefficientBeta,
                            Color = new
                            {
                                A = curve.Color.A,
                                R = curve.Color.R,
                                G = curve.Color.G,
                                B = curve.Color.B
                            }
                        }).ToList();

                        var jsonString = JsonConvert.SerializeObject(curvesInfo, new JsonSerializerSettings 
                        { 
                            Formatting = Formatting.Indented,
                            NullValueHandling = NullValueHandling.Ignore
                        });
                        File.WriteAllText(filePath, jsonString);
                        break;
                    default:
                        break;
                }
            }
    }

    public static async Task LoadJSON()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
            return;

        var storageProvider = desktop.MainWindow.StorageProvider;
        if (storageProvider == null)
            return;

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "Открыть файл",
            AllowMultiple = false, // только один файл
            FileTypeFilter = new[] { new FilePickerFileType("Файл JSON") { Patterns = new[] { "*.json" } } }
        };

        var res = await storageProvider.OpenFilePickerAsync(filePickerOptions);
        if (res.Count > 0)
        {
            var filePath = res[0].Path.LocalPath;
            var jsonString = File.ReadAllText(filePath);
            var curvesData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonString);

            var logic = new SplineLogic();

            foreach (var curveData in curvesData)
            {
                if (!curveData.TryGetValue("Name", out var nameObj) || nameObj is not string name)
                    continue;

                ICurve curve = null;

                if (curveData.TryGetValue("Type", out var typeObj) && typeObj is string type)
                {
                    if (type == "Function" && curveData.TryGetValue("FunctionString", out var funcStrObj) && funcStrObj is string funcStr)
                    {
                        curve = await logic.CreateFunction(funcStr);
                        if (curve != null)
                        {
                            curve.Name = name;
                            Console.WriteLine($"Загружена функция: {name}");
                        }
                        if (curveData.TryGetValue("Start", out var StartObj) && StartObj is string Start)
                        {
                            if (Start == "")
                                curve.Start = null;
                            else
                                curve.Start = Start;
                        }
                            
                        if (curveData.TryGetValue("End", out var EndObj) && EndObj is string End)
                        {
                            if (End == "")
                                curve.End = null;
                            else
                                curve.End = End;
                        }
                            
                        curve.GetLimits();
                    }
                    else if (type == "Spline")
                    {
                        if (curveData.TryGetValue("SplineType", out var splineTypeObj) && splineTypeObj is string splineType)
                        {
                            if ((splineType == "Interpolating Cubic 2" || splineType == "Interpolating Cubic 1") && curveData.TryGetValue("ControlPoints", out var controlPointsObj))
                            {
                                var controlPoints = ((JArray)controlPointsObj).ToObject<List<Dictionary<string, double>>>();
                                var points = controlPoints.Select(p => new Point((double)p["X"], (double)p["Y"])).ToArray();
                                if (points.Length < 3 && points.Length > 0)
                                {
                                    await ErrorHelper.ShowError("Ошибка", "Сплайн должен содержать минимум 3 контрольные точки");
                                    curve = null;
                                }
                                if (points.Length == 0)
                                {
                                    await ErrorHelper.ShowError("Ошибка", "Отсутствуют контрольные точки у сплайна");
                                    curve = null;
                                }
                                if (points.Length >= 3)
                                {
                                    if (splineType == "Interpolating Cubic 2")
                                        curve = logic.CreateInterpolatingSpline(points, 2);
                                    else if (splineType == "Interpolating Cubic 1")
                                        curve = logic.CreateInterpolatingSpline(points, 1);
                                    curve.Name = name;
                                    Console.WriteLine($"Загружен интерполяционный сплайн: {name}");
                                }
                            }
                            else if (splineType == "Smoothing Cubic" && curveData.TryGetValue("Grid", out var gridObj) && curveData.TryGetValue("ControlPoints", out var controlPointsObj2))
                            {
                                var grid = ((JArray)gridObj).ToObject<double[]>();
                                var controlPoints = ((JArray)controlPointsObj2).ToObject<List<Dictionary<string, double>>>();
                                var points = controlPoints.Select(p => new Point((double)p["X"], (double)p["Y"])).ToArray();
                                if (curveData.TryGetValue("SmoothingCoefficientAlpha", out var alphaObj) && curveData.TryGetValue("SmoothingCoefficientBeta", out var betaObj))
                                {
                                    var alpha = alphaObj?.ToString();
                                    var beta = betaObj?.ToString();
                                    bool incorrect = false;
                                    if (alpha == null || alpha == "")
                                    {
                                        await ErrorHelper.ShowError("Ошибка", "Коэффициент альфа не может быть пустым.");
                                        incorrect = true;
                                    }
                                    if (beta == null || beta == "")
                                    {
                                        await ErrorHelper.ShowError("Ошибка", "Коэффициент бета не может быть пустым.");
                                        incorrect = true;
                                    }
                                    if (grid.Length < 2)
                                    {
                                        await ErrorHelper.ShowError("Ошибка", "Сетка должна содержать хотя бы один конечный элемент.");
                                        incorrect = true;
                                    }
                                    if (points.Length < 3 && points.Length > 0)
                                    {
                                        await ErrorHelper.ShowError("Ошибка", "Сплайн должен содержать минимум 3 контрольные точки");
                                        incorrect = true;
                                    }
                                    if (points.Length == 0)
                                    {
                                        await ErrorHelper.ShowError("Ошибка", "Отсутствуют контрольные точки у сплайна");
                                        incorrect = true;
                                    }
                                    if (!incorrect)
                                    {
                                        curve = logic.CreateSmoothingSpline(grid, points, alpha, beta);
                                        if (!curve.IsPossible)
                                        {
                                            await ErrorHelper.ShowError("Ошибка", "Не удалось построить сглаживающий сплайн. Попробуйте изменить параметры.");
                                        }
                                        curve.Name = name;
                                        Console.WriteLine($"Загружен сглаживающий сплайн: {name}");
                                    }
                                }
                            }
                            if (splineType == "Linear" && curveData.TryGetValue("ControlPoints", out var controlPointsObj3))
                            {
                                var controlPoints = ((JArray)controlPointsObj3).ToObject<List<Dictionary<string, double>>>();
                                var points = controlPoints.Select(p => new Point((double)p["X"], (double)p["Y"])).ToArray();
                                if (points.Length < 3 && points.Length > 0)
                                {
                                    await ErrorHelper.ShowError("Ошибка", "Сплайн должен содержать минимум 3 контрольные точки");
                                    curve = null;
                                }
                                if (points.Length == 0)
                                {
                                    await ErrorHelper.ShowError("Ошибка", "Отсутствуют контрольные точки у сплайна");
                                    curve = null;
                                }
                                if (points.Length >= 3)
                                {
                                    curve = logic.CreateLinearSpline(points);
                                    curve.Name = name;
                                    Console.WriteLine($"Загружена ломаная: {name}");
                                }
                            }
                        }
                        if (curveData.TryGetValue("ShowControlPoints", out var ShowControlPointsObj) && ShowControlPointsObj is bool ShowControlPoints)
                            curve.ShowControlPoints = ShowControlPoints;
                    }
                }

                if (curve != null && curve.IsPossible)
                {
                    // Обработка видимости
                    if (curveData.TryGetValue("IsVisible", out var isVisibleObj) && isVisibleObj is bool isVisible)
                        curve.IsVisible = isVisible;

                    // Обработка цвета
                    if (curveData.TryGetValue("Color", out var colorObj) && colorObj is JObject colorDict)
                    {
                        byte a = colorDict["A"]?.ToObject<byte>() ?? 255;
                        byte r = colorDict["R"]?.ToObject<byte>() ?? 0;
                        byte g = colorDict["G"]?.ToObject<byte>() ?? 0;
                        byte b = colorDict["B"]?.ToObject<byte>() ?? 0;
                        curve.Color = Color.FromArgb(a, r, g, b);
                        Console.WriteLine($"Цвет кривой {name}: {a}, {r}, {g}, {b}");
                    }

                    // Добавление кривой в список
                    MainWindowViewModel.CurveList.Add(curve);
                    var mainWindowViewModel = new MainWindowViewModel();  // Создание экземпляра
                    mainWindowViewModel.DrawCurves();  // Вызов метода
                }
                else
                {
                    Console.WriteLine($"Кривая не была загружена: {name}");
                }
            }
        }
    }

    public static async Task SavePNG()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
            return;

        // Получаем главное окно
        var mainWindow = desktop.MainWindow as MainWindow;
        if (mainWindow == null)
            return;

        var storageProvider = mainWindow.StorageProvider;
        if (storageProvider == null)
            return;

        var filePickerOptions = new FilePickerSaveOptions
        {
            Title = "Сохранить как",
            SuggestedFileName = "curves", // Дефолтное имя файла
            ShowOverwritePrompt = true, // Предупреждение о перезаписи файла
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Файл PNG") { Patterns = new[] { "*.png" } }
            }
        };

        // Открываем проводник
        var res = await storageProvider.SaveFilePickerAsync(filePickerOptions);

        if (res != null)
        {
            var filePath = res.Path.LocalPath; // Получаем путь
            var extension = Path.GetExtension(filePath).ToLower(); // Определяем расширение файла

            switch (extension)
            {
                case ".png":
                    // Определяем размер канваса
                    var size = new Size(mainWindow.GraphicHolder.Bounds.Width, mainWindow.GraphicHolder.Bounds.Height);

                    // Создаем RenderTargetBitmap
                    var bitmap = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height));

                    // Отрисовываем Canvas в битмап
                    mainWindow.GraphicHolder.Measure(size);
                    mainWindow.GraphicHolder.Arrange(new Rect(size));
                    bitmap.Render(mainWindow.GraphicHolder);

                    // Сохраняем в PNG
                    using (var stream = File.Create(filePath))
                    {
                        bitmap.Save(stream);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}