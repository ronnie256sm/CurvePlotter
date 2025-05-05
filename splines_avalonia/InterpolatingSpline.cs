using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;

namespace splines_avalonia
{
    public class InterpolatingSpline : ICurve
    {
        #pragma warning disable CS8618, CS8625
        public string Type => "Spline";
        public double[] Grid { get; }
        public Point[] ControlPoints { get; }
        public Point[] OutputPoints { get; }
        public string ControlPointsFile { get; set; }
        public string GridFile { get; set; }
        public string FunctionString { get; set; }
        public string SplineType => "Interpolating Cubic";

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

        public InterpolatingSpline(Point[] controlPoints)
        {
            Color = Colors.Black;
            SmoothingCoefficientAlpha = null;
            SmoothingCoefficientBeta = null;
            ControlPoints = controlPoints;
            Name = "Интерполяционный сплайн";
            IsVisible = true;
            IsPossible = true;

            int n = controlPoints.Length;

            double[] x = new double[n];
            double[] y = new double[n];

            // Заполняем массивы x и y из ControlPoints
            for (int i = 0; i < n; i++)
            {
                x[i] = controlPoints[i].X;
                y[i] = controlPoints[i].Y;
            }

            var fPrime = new double[n];
            var a = new double[n - 1];
            var b = new double[n - 1];
            var c = new double[n - 1];
            var d = new double[n - 1];

            // Вычисление коэффициентов
            ComputeSpline(x, y, fPrime, a, b, c, d);

            // Вычисление точек сплайна
            List<Point> output = new();
            int numPoints = 50;

            for (int i = 0; i < n - 1; i++)
            {
                double h = (x[i + 1] - x[i]) / numPoints;
                for (int j = 0; j <= numPoints; j++)
                {
                    double xVal = x[i] + j * h;
                    double dx = xVal - x[i];
                    double yVal = a[i] + b[i] * dx + c[i] * dx * dx + d[i] * dx * dx * dx;
                    output.Add(new Point(xVal, yVal));
                }
            }

            OutputPoints = output.ToArray();
        }

        private static void ComputeSpline(double[] x, double[] y, double[] fPrime,
            double[] a, double[] b, double[] c, double[] d)
        {
            int n = x.Length;
            double[] h = new double[n - 1];
            for (int i = 0; i < n - 1; i++)
                h[i] = x[i + 1] - x[i];

            double[] diagLower = new double[n];
            double[] diagMain = new double[n];
            double[] diagUpper = new double[n];
            double[] rhs = new double[n];

            diagMain[0] = diagMain[n - 1] = 1.0;

            // Граничные условия
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
                diagLower[i] = 2.0 / h[i - 1];
                diagMain[i] = 4.0 * (1.0 / h[i - 1] + 1.0 / h[i]);
                diagUpper[i] = 2.0 / h[i];

                rhs[i] = -y[i - 1] * (6.0 / (h[i - 1] * h[i - 1]))
                         + y[i] * 6.0 * (1.0 / (h[i - 1] * h[i - 1]) - 1.0 / (h[i] * h[i]))
                         + y[i + 1] * (6.0 / (h[i] * h[i]));
            }

            fPrime = SolveTridiagonal(diagLower, diagMain, diagUpper, rhs);

            for (int i = 0; i < n - 1; i++)
            {
                a[i] = y[i];
                b[i] = fPrime[i];
                c[i] = (3.0 / (h[i] * h[i])) * (y[i + 1] - y[i]) - (fPrime[i + 1] + 2.0 * fPrime[i]) / h[i];
                d[i] = (2.0 / (h[i] * h[i] * h[i])) * (y[i] - y[i + 1]) + (fPrime[i + 1] + fPrime[i]) / (h[i] * h[i]);
            }
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

        public double CalculateFunctionValue(string functionString, double x)
        {
            throw new System.NotImplementedException();
        }
    }
}