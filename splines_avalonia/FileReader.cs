using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

#pragma warning disable CS8604

namespace splines_avalonia
{
    public static class FileReader
    {
        public static Point[] ReadPoints(string pointsFile)
        {
            string[] lines = File.ReadAllLines(pointsFile);
            if (lines.Length == 0)
                throw new InvalidDataException("Файл пустой.");

            int numPoints = int.Parse(lines[0].Trim());
            if (lines.Length - 1 < numPoints)
                throw new InvalidDataException("Недостаточно точек в файле.");

            if (lines.Length - 1 > numPoints)
                Console.WriteLine($"Имеются лишние точки в файле. Будут считаны только первые {numPoints} точек.");

            if (numPoints <= 3)
                throw new InvalidDataException("Требуется как минимум 3 точки.");

            Point[] controlPoints = new Point[numPoints];
            var format = CultureInfo.InvariantCulture;

            for (int i = 0; i < numPoints; i++)
            {
                string[] parts = lines[i + 1].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    throw new InvalidDataException($"Ошибка в строке {i + 2}: недостаточно координат");

                double x = double.Parse(parts[0], format);
                double y = double.Parse(parts[1], format);
                controlPoints[i] = new Point(x, y);
            }

            return controlPoints;
        }

        public static double[] ReadGrid(string gridFile)
        {
            string[] lines = File.ReadAllLines(gridFile);
            if (lines.Length == 0)
                throw new InvalidDataException("Файл пустой.");
            
            var reader = new StreamReader(gridFile);
            int gridSize = int.Parse(reader.ReadLine());

            if (lines.Length - 1 < gridSize)
                throw new InvalidDataException("Недостаточно элементов в файле.");

            if (lines.Length - 1 > gridSize)
                Console.WriteLine($"Имеются лишние элементы. Будут прочитаны только первые {gridSize} элементов.");

            var mesh = new List<double>();
            for (int i = 0; i < gridSize; i++)
            {
                var line = reader.ReadLine();
                mesh.Add(double.Parse(line));
            }
            return mesh.ToArray();
        }
    }
}