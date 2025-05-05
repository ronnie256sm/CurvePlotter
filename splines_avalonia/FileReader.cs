using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using splines_avalonia.Helpers;

#pragma warning disable CS8604, CS8603

namespace splines_avalonia
{
    public static class FileReader
    {
        public static async Task<Point[]> ReadPoints(string pointsFile)
        {
            var rawLines = File.ReadAllLines(pointsFile);
            var lines = rawLines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            if (lines.Length == 0)
            {
                await ErrorHelper.ShowError("Ошибка", "Файл с точками пустой.");
                return null;
            }

            int numPoints = int.Parse(lines[0].Trim());
            int actualDataLines = lines.Length - 1;

            if (actualDataLines < numPoints)
            {
                await ErrorHelper.ShowError("Ошибка", "Недостаточно точек в файле.");
                return null;
            }

            if (actualDataLines > numPoints)
                await ErrorHelper.ShowError("Предупреждение", $"Имеются лишние точки в файле. Будут считаны только первые {numPoints} точек.");

            if (numPoints <= 3)
            {
                await ErrorHelper.ShowError("Ошибка", "Требуется как минимум 3 точки.");
                return null;
            }

            Point[] controlPoints = new Point[numPoints];
            var format = CultureInfo.InvariantCulture;

            for (int i = 0; i < numPoints; i++)
            {
                string[] parts = lines[i + 1].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    await ErrorHelper.ShowError("Ошибка", $"Ошибка в строке {i + 2}: недостаточно координат");
                    return null;
                }

                double x = double.Parse(parts[0], format);
                double y = double.Parse(parts[1], format);
                controlPoints[i] = new Point(x, y);
            }

            return controlPoints;
        }

        public static async Task<double[]> ReadGrid(string gridFile)
        {
            var rawLines = File.ReadAllLines(gridFile);
            var lines = rawLines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            if (lines.Length == 0)
            {
                await ErrorHelper.ShowError("Ошибка", "Файл с сеткой пустой.");
                return null;
            }

            int gridSize = int.Parse(lines[0].Trim());
            int actualDataLines = lines.Length - 1;

            if (actualDataLines < gridSize)
            {
                await ErrorHelper.ShowError("Ошибка", "Недостаточно элементов в файле.");
                return null;
            }

            if (actualDataLines > gridSize)
                await ErrorHelper.ShowError("Предупреждение", $"Имеются лишние элементы. Будут прочитаны только первые {gridSize} элементов.");

            var mesh = new double[gridSize];
            for (int i = 0; i < gridSize; i++)
            {
                mesh[i] = double.Parse(lines[i + 1], CultureInfo.InvariantCulture);
            }

            return mesh;
        }
    }
}