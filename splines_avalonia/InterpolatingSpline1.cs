using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;

namespace splines_avalonia
{
    public class InterpolatingSpline1 : ICurve
    {
        #pragma warning disable CS8618, CS8625
        public string Type => "Spline";
        public double[] Grid { get; }
        public Point[] ControlPoints { get; }
        public Point[] OutputPoints { get; }
        public string ControlPointsFile { get; set; }
        public string GridFile { get; set; }
        public string FunctionString { get; set; }
        public string SplineType => "Interpolating Cubic 1";

        public string Name { get; set; }
        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public string SmoothingCoefficientAlpha { get; set; }
        public string SmoothingCoefficientBeta { get; set; }
        public bool IsPossible { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public bool ShowControlPoints { get; set; }
        public double ParsedStart { get; set; }
        public double ParsedEnd { get; set; }
        private Color _color;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
            }
        }

        public InterpolatingSpline1(Point[] controlPoints)
        {
            Color = Colors.Black;
            ControlPoints = controlPoints;
            Name = "Интерполяционный сплайн с производными, построенными с помощью полиномов Лагранжа";
            IsVisible = true;
            IsPossible = true;

            // Преобразуем в список для удобства
            var points = new List<PointData>();
            foreach (var p in controlPoints)
            {
                points.Add(new PointData { X = p.X, Y = p.Y });
            }

            // Вычисляем производные
            ComputeDerivatives(points);

            // Генерируем выходные точки
            var outputPoints = new List<Point>();
            int segments = 50; // Количество точек на сегмент

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = points[i];
                var p1 = points[i + 1];
                double h = p1.X - p0.X;

                for (int j = 0; j < segments; j++)
                {
                    double t = j / (double)segments;
                    double x = p0.X + t * h;
                    double y = HermiteInterpolate(p0, p1, t, h);
                    outputPoints.Add(new Point(x, y));
                }
            }

            // Добавляем последнюю точку
            outputPoints.Add(new Point(points[^1].X, points[^1].Y));
            OutputPoints = outputPoints.ToArray();
        }

        private class PointData
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Dy { get; set; }
        }

        private void ComputeDerivatives(List<PointData> pts)
        {
            int n = pts.Count;

            for (int i = 0; i < n; i++)
            {
                double dy;

                if (i == 0)
                {
                    double h1 = pts[1].X - pts[0].X;
                    double h2 = pts[2].X - pts[1].X;
                    dy = -pts[0].Y * (2 * h1 + h2) / (h1 * (h1 + h2)) +
                          pts[1].Y * (h1 + h2) / (h1 * h2) -
                          pts[2].Y * h1 / ((h1 + h2) * h2);
                }
                else if (i == n - 1)
                {
                    double h1 = pts[n - 1].X - pts[n - 2].X;
                    double h2 = pts[n - 2].X - pts[n - 3].X;
                    dy = pts[n - 3].Y * (h1 / (h2 * (h1 + h2))) -
                         pts[n - 2].Y * ((h1 + h2) / (h1 * h2)) +
                         pts[n - 1].Y * ((2 * h1 + h2) / (h1 * (h1 + h2)));
                }
                else
                {
                    double h1 = pts[i].X - pts[i - 1].X;
                    double h2 = pts[i + 1].X - pts[i].X;
                    dy = -pts[i - 1].Y * (h2 / (h1 * (h1 + h2))) +
                          pts[i].Y * ((h2 - h1) / (h1 * h2)) +
                          pts[i + 1].Y * (h1 / (h2 * (h1 + h2)));
                }

                pts[i].Dy = dy;
            }
        }

        private double HermiteInterpolate(PointData p0, PointData p1, double t, double h)
        {
            double t2 = t * t;
            double t3 = t2 * t;

            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;

            return h00 * p0.Y + h10 * h * p0.Dy + h01 * p1.Y + h11 * h * p1.Dy;
        }

        public double CalculateFunctionValue(double x)
        {
            // Преобразуем ControlPoints в список PointData с производными
            var points = new List<PointData>();
            foreach (var p in ControlPoints)
            {
                points.Add(new PointData { X = p.X, Y = p.Y });
            }
            ComputeDerivatives(points);

            // Если x вне диапазона — возвращаем NaN
            if (x < points[0].X || x > points[^1].X)
                return double.NaN;

            // Ищем сегмент, к которому принадлежит x
            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = points[i];
                var p1 = points[i + 1];

                if (x >= p0.X && x <= p1.X)
                {
                    double h = p1.X - p0.X;
                    double t = (x - p0.X) / h;
                    return HermiteInterpolate(p0, p1, t, h);
                }
            }

            return double.NaN;
        }

        public void GetLimits()
        {
            throw new System.NotImplementedException();
        }
    }
}