using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;
using splines_avalonia.ViewModels;

#pragma warning disable CS8618

namespace splines_avalonia
{
    public class InterpolatingSpline2 : ICurve
    {
        private record SplineSegment(double X, double A, double B, double C, double D);
        public string Type => "Spline";
        public string SplineType => "Interpolating Cubic 2";
        public Point[] ControlPoints { get; }
        public string ControlPointsFile { get; set; }
        public string GridFile { get; set; }
        public string FunctionString { get; set; }
        public string Name { get; set; }
        public string SmoothingCoefficientAlpha { get; set; }
        public string SmoothingCoefficientBeta { get; set; }
        public bool IsPossible { get; set; } = true;
        public string Start { get; set; }
        public string End { get; set; }
        public bool ShowControlPoints { get; set; }
        public double ParsedStart { get; set; }
        public double ParsedEnd { get; set; }
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
        private double _thickness = 2;
        public double Thickness
        {
            get => _thickness;
            set
            {
                if (_thickness != value)
                {
                    _thickness = value;
                    OnPropertyChanged(nameof(Thickness));
                }
            }
        }
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
        public double[] Grid { get; set; }
        private SplineSegment[] _segments;
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public InterpolatingSpline2(Point[] controlPoints)
        {
            Color = Globals.DarkMode ? Colors.White : Colors.Black;
            ControlPoints = controlPoints;
            Name = "Интерполяционный сплайн с непрерывными вторыми производными";

            int n = controlPoints.Length;
            double[] x = new double[n];
            double[] y = new double[n];

            for (int i = 0; i < n; i++)
            {
                x[i] = controlPoints[i].X;
                y[i] = controlPoints[i].Y;
            }

            _segments = ComputeSegments(x, y);
        }

        public double CalculateFunctionValue(double x)
        {
            if (_segments is null || _segments.Length == 0)
                return double.NaN;

            if (x < _segments[0].X || x > _segments[^1].X)
                return double.NaN;

            for (int i = 0; i < _segments.Length; i++)
            {
                var seg = _segments[i];
                if (x >= seg.X && (i == _segments.Length - 1 || x <= _segments[i + 1].X))
                {
                    double dx = x - seg.X;
                    return seg.A + seg.B * dx + seg.C * dx * dx + seg.D * dx * dx * dx;
                }
            }

            return double.NaN;
        }

        public void GetLimits() => throw new System.NotImplementedException();

        private static SplineSegment[] ComputeSegments(double[] x, double[] y)
        {
            int n = x.Length;
            double[] h = new double[n - 1];
            for (int i = 0; i < n - 1; i++)
                h[i] = x[i + 1] - x[i];

            double[] lower = new double[n];
            double[] main = new double[n];
            double[] upper = new double[n];
            double[] rhs = new double[n];

            main[0] = main[n - 1] = 1.0;

            rhs[0] = 0.5 * (
                -((3 * h[0] + 2 * h[1]) / (h[0] * (h[0] + h[1]))) * y[0]
                + ((h[0] + 2 * h[1]) / (h[0] * h[1])) * y[1]
                - (h[0] / ((h[0] + h[1]) * h[1])) * y[2]);

            rhs[n - 1] = 0.5 * (
                (h[n - 2] / (h[n - 3] * (h[n - 3] + h[n - 2]))) * y[n - 3]
                - ((2 * h[n - 3] + h[n - 2]) / (h[n - 2] * h[n - 3])) * y[n - 2]
                + ((3 * h[n - 2] + 2 * h[n - 3]) / (h[n - 2] * (h[n - 3] + h[n - 2]))) * y[n - 1]);

            for (int i = 1; i < n - 1; i++)
            {
                lower[i] = 2.0 / h[i - 1];
                main[i] = 4.0 * (1.0 / h[i - 1] + 1.0 / h[i]);
                upper[i] = 2.0 / h[i];

                rhs[i] = -y[i - 1] * (6.0 / (h[i - 1] * h[i - 1]))
                         + y[i] * 6.0 * (1.0 / (h[i - 1] * h[i - 1]) - 1.0 / (h[i] * h[i]))
                         + y[i + 1] * (6.0 / (h[i] * h[i]));
            }

            double[] fPrime = SolveTridiagonal(lower, main, upper, rhs);

            var segments = new List<SplineSegment>(n - 1);
            for (int i = 0; i < n - 1; i++)
            {
                double dx = h[i];
                double a = y[i];
                double b = fPrime[i];
                double c = (3.0 / (dx * dx)) * (y[i + 1] - y[i]) - (fPrime[i + 1] + 2.0 * fPrime[i]) / dx;
                double d = (2.0 / (dx * dx * dx)) * (y[i] - y[i + 1]) + (fPrime[i + 1] + fPrime[i]) / (dx * dx);
                segments.Add(new SplineSegment(x[i], a, b, c, d));
            }

            return segments.ToArray();
        }

        private static double[] SolveTridiagonal(double[] a, double[] b, double[] c, double[] d)
        {
            int n = d.Length;
            double[] cPrime = new double[n];
            double[] dPrime = new double[n];
            double[] x = new double[n];

            cPrime[0] = c[0] / b[0];
            dPrime[0] = d[0] / b[0];

            for (int i = 1; i < n; i++)
            {
                double denom = b[i] - a[i] * cPrime[i - 1];
                cPrime[i] = c[i] / denom;
                dPrime[i] = (d[i] - a[i] * dPrime[i - 1]) / denom;
            }

            x[n - 1] = dPrime[n - 1];
            for (int i = n - 2; i >= 0; i--)
                x[i] = dPrime[i] - cPrime[i] * x[i + 1];

            return x;
        }
    }
}
