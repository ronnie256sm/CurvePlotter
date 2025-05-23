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

public static class IO
{
    public static async Task SaveJSON(ObservableCollection<ICurve> curves)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } storageProvider)
            return;

        // открываем диалог сохранения файла
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить как",
            SuggestedFileName = "curves",
            ShowOverwritePrompt = true,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Файл JSON") { Patterns = new[] { "*.json" } }
            }
        });

        if (file is null || Path.GetExtension(file.Path.LocalPath).ToLower() != ".json")
            return;

        // формируем список с нужной информацией о кривых
        var curvesInfo = curves.Select(c => new
        {
            c.Name,
            c.Type,
            c.FunctionString,
            c.SplineType,
            c.Grid,
            c.ControlPoints,
            c.ShowControlPoints,
            c.Start,
            c.End,
            c.IsVisible,
            c.SmoothingCoefficientAlpha,
            c.SmoothingCoefficientBeta,
            c.Thickness,
            Color = new { c.Color.A, c.Color.R, c.Color.G, c.Color.B }
        });

        // сериализуем список в строку JSON
        var json = JsonConvert.SerializeObject(curvesInfo, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        });

        File.WriteAllText(file.Path.LocalPath, json);
    }

    public static async Task LoadJSON()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } storageProvider)
            return;

        // открываем диалог выбора файла
        var file = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть файл",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Файл JSON") { Patterns = new[] { "*.json" } } }
        });

        if (file.Count == 0)
            return;

        var filePath = file[0].Path.LocalPath;
        var json = File.ReadAllText(filePath);

        // десериализация JSON в список словарей
        var curvesData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

        if (curvesData == null)
        {
            await ErrorHelper.ShowError("Ошибка", "Не удалось прочитать файл");
            return;
        }

        var logic = new SplineLogic();

        foreach (var data in curvesData)
        {
            // извлечение имени кривой
            if (!data.TryGetValue("Name", out var nameObj) || nameObj is not string name)
                continue;

            // попытка создать кривую
            ICurve? curve = await TryCreateCurve(data, logic, name);
            if (curve == null || !curve.IsPossible)
            {
                Console.WriteLine($"Кривая не была загружена: {name}");
                continue;
            }

            ApplyOptionalProperties(curve, data, name); // применение дополнительных параметров

            // добавление кривой в список и перерисовка
            MainWindowViewModel.CurveList.Add(curve);
            new MainWindowViewModel().DrawCurves();
        }

        static async Task<ICurve?> TryCreateCurve(Dictionary<string, object> data, SplineLogic logic, string name)
        {
            if (!data.TryGetValue("Type", out var typeObj) || typeObj is not string type)
                return null;

            // создание функции
            if (type == "Function" && data.TryGetValue("FunctionString", out var funcStrObj) && funcStrObj is string funcStr)
            {
                var function = await logic.CreateFunction(funcStr);
                if (function == null)
                    return null;

                function.Name = name;
                if (data.TryGetValue("Start", out var s) && s is string start) function.Start = string.IsNullOrEmpty(start) ? null : start;
                if (data.TryGetValue("End", out var e) && e is string end) function.End = string.IsNullOrEmpty(end) ? null : end;
                function.GetLimits(); // вычисление границ графика
                Console.WriteLine($"Загружена функция: {name}");
                return function;
            }

            // создание сплайна
            if (type == "Spline" && data.TryGetValue("SplineType", out var splineTypeObj) && splineTypeObj is string splineType)
            {
                var points = GetControlPoints(data, "ControlPoints");
                if (points == null)
                    return null;

                switch (splineType)
                {
                    // интерполяционные сплайны
                    case "Interpolating Cubic 1":
                    case "Interpolating Cubic 2":
                        if (points.Length >= 3)
                        {
                            var spline = logic.CreateInterpolatingSpline(points, splineType.EndsWith("2") ? 2 : 1);
                            if (spline != null)
                            {
                                spline.Name = name;
                                Console.WriteLine($"Загружен интерполяционный сплайн: {name}");
                                return spline;
                            }
                        }
                        break;

                    // сглаживающий сплайн
                    case "Smoothing Cubic":
                        var grid = ((JArray?)data["Grid"])?.ToObject<double[]>();
                        var alpha = data.TryGetValue("SmoothingCoefficientAlpha", out var a) ? a?.ToString() : null;
                        var beta = data.TryGetValue("SmoothingCoefficientBeta", out var b) ? b?.ToString() : null;

                        if (grid != null && alpha != null && beta != null && grid.Length >= 2 && points.Length >= 3)
                        {
                            var spline = logic.CreateSmoothingSpline(grid, points, alpha, beta);
                            if (spline != null)
                            {
                                if (!spline.IsPossible)
                                    await ErrorHelper.ShowError("Ошибка", "Не удалось построить сглаживающий сплайн.");
                                spline.Name = name;
                                Console.WriteLine($"Загружен сглаживающий сплайн: {name}");
                                return spline;
                            }
                        }
                        break;

                    // ломаная
                    case "Linear":
                        if (points.Length >= 3)
                        {
                            var linear = logic.CreateLinearSpline(points);
                            if (linear != null)
                            {
                                linear.Name = name;
                                Console.WriteLine($"Загружена ломаная: {name}");
                                return linear;
                            }
                        }
                        break;
                }
            }

            return null;
        }

        static Point[]? GetControlPoints(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var cpObj)) return null;
            var controlPointList = ((JArray?)cpObj)?.ToObject<List<Dictionary<string, double>>>();
            return controlPointList?.Select(p => new Point(p["X"], p["Y"])).ToArray();
        }

        static void ApplyOptionalProperties(ICurve curve, Dictionary<string, object> data, string name)
        {
            if (data.TryGetValue("IsVisible", out var v) && v is bool vis)
                curve.IsVisible = vis;

            if (data.TryGetValue("Color", out var colorObj) && colorObj is JObject color)
            {
                byte a = color["A"]?.ToObject<byte>() ?? 255;
                byte r = color["R"]?.ToObject<byte>() ?? 0;
                byte g = color["G"]?.ToObject<byte>() ?? 0;
                byte b = color["B"]?.ToObject<byte>() ?? 0;
                curve.Color = Color.FromArgb(a, r, g, b);
                Console.WriteLine($"Цвет кривой {name}: {a}, {r}, {g}, {b}");
            }

            if (data.TryGetValue("Thickness", out var t) && t is double thickness)
                curve.Thickness = thickness;

            if (data.TryGetValue("ShowControlPoints", out var scp) && scp is bool show)
                curve.ShowControlPoints = show;
        }
    }

    public static async Task SavePNG()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow mainWindow })
            return;

        var storageProvider = mainWindow.StorageProvider;
        if (storageProvider == null)
            return;

        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить как",
            SuggestedFileName = "curves",
            ShowOverwritePrompt = true,
            FileTypeChoices = new[] { new FilePickerFileType("Файл PNG") { Patterns = new[] { "*.png" } } }
        });

        if (result?.Path is not { } path || Path.GetExtension(path.LocalPath).ToLower() != ".png")
            return;

        var bounds = mainWindow.GraphicHolder.Bounds;
        var size = new Size(bounds.Width, bounds.Height);
        var bitmap = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height));

        mainWindow.GraphicHolder.Measure(size);
        mainWindow.GraphicHolder.Arrange(new Rect(size));
        bitmap.Render(mainWindow.GraphicHolder);

        using var stream = File.Create(path.LocalPath);
        bitmap.Save(stream);
    }
}