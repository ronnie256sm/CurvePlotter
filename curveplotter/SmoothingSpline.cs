using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;
using CurvePlotter.ViewModels;

namespace CurvePlotter
{
    public class SmoothingSpline : ICurve
    {
        public string Type => "Spline";
        public double[] Grid { get; }
        public Point[] ControlPoints { get; }
        public string? Name { get; set; }
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
        public string? SmoothingCoefficientAlpha { get; set; }
        public string? SmoothingCoefficientBeta { get; set; }
        private Function AlphaFunction { get; set; }
        private Function BetaFunction { get; set; }
        public string? ControlPointsFile { get; set; }
        public string? GridFile { get; set; }
        public bool IsPossible { get; set; }
        public string? Start { get; set; }
        public string? End { get; set; }
        public double ParsedStart { get; set; }
        public double ParsedEnd { get; set; }
        public bool ShowControlPoints { get; set; }
        private readonly SLAE slae;
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

        public string SplineType => "Smoothing Cubic";

        public string? FunctionString { get; set; }

        public SmoothingSpline(Point[] controlPoints, double[] grid, string smoothingCoefficientAlpha, string smoothingCoefficientBeta)
        {
            if (Globals.DarkMode)
                Color = Colors.White;
            else
                Color = Colors.Black;
            Thickness = 2;
            SmoothingCoefficientAlpha = smoothingCoefficientAlpha;
            SmoothingCoefficientBeta = smoothingCoefficientBeta;
            AlphaFunction = new Function(smoothingCoefficientAlpha);
            BetaFunction = new Function(SmoothingCoefficientBeta);
            ControlPoints = controlPoints;
            Name = "Сглаживающий сплайн";
            Grid = grid;
            IsVisible = true;

            var mesh = Grid;
            var points = ControlPoints;
            var n_points = points.Length;
            var n_mesh = mesh.Length;

            // СЛАУ для сплайна
            slae = new SLAE(n_mesh * 2);
            slae.Initialize();

            // заполнение СЛАУ (поставить интегралы и уравнения)
            BuildSLAE(slae, points, mesh, n_points, n_mesh, AlphaFunction, BetaFunction);

            // решение СЛАУ методом Гаусса
            IsPossible = SolveSLAE(slae);
        }

        // построение СЛАУ
        private static void BuildSLAE(SLAE slae, Point[] points, double[] mesh, int n_points, int n_mesh, Function AlphaFunction, Function BetaFunction)
        {
            int index = 0;
            for (int k = 0; k < n_mesh - 1; ++k)
            {
                double start = mesh[k];
                double end = mesh[k + 1];
                double h = end - start;

                while (index < n_points && points[index].X >= start && points[index].X < end)
                {
                    double t = (points[index].X - start) / h; // нормированная координата

                    // формирование уравнений по точкам
                    for (int i = 0; i < 4; i++)
                    {
                        double psiValue = Psi(i, t, h);
                        for (int j = 0; j < 4; j++)
                            slae.A[2 * k + i][2 * k + j] += psiValue * Psi(j, t, h);

                        slae.F[2 * k + i] += psiValue * points[index].Y;
                    }

                    index++;
                }

                // добавление интегральных членов для сглаживания
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 4; j++)
                        slae.A[2 * k + i][2 * k + j] += SumPsiAlpha(i, j, h, AlphaFunction) + SumPsiBeta(i, j, h, BetaFunction);
            }
        }

        // решение СЛАУ, возвращает true, если успешно
        private static bool SolveSLAE(SLAE slae)
        {
            bool IsPossible = true;
            if (!slae.Solve())
            {
                IsPossible = false;
            }
            return IsPossible;
        }

        private static double Psi(int l, double t, double h)
        {
            switch (l)
            {
                case 0: return 1 - 3 * Math.Pow(t, 2) + 2 * Math.Pow(t, 3);
                case 1: return h * (t - 2 * Math.Pow(t, 2) + Math.Pow(t, 3));
                case 2: return 3 * Math.Pow(t, 2) - 2 * Math.Pow(t, 3);
                case 3: return h * (-Math.Pow(t, 2) + Math.Pow(t, 3));
                default: return 0;
            }
        }

        // вычисление интеграла для альфа методом трапеций
        private static double SumPsiAlpha(int i, int j, double h, Function AlphaFunction)
        {
            const double h_i = 0.01;
            double sum = 0.0;
            for (int k = 0; k < 100; ++k)
            {
                double x_k = k * h_i;
                double x_k1 = (k + 1) * h_i;

                double f_xk = Alpha(x_k, AlphaFunction) * D1Psi(i, x_k, h) * D1Psi(j, x_k, h);
                double f_xk1 = Alpha(x_k1, AlphaFunction) * D1Psi(i, x_k1, h) * D1Psi(j, x_k1, h);

                sum += (f_xk + f_xk1) * h_i / 2;
            }

            sum += Alpha(0, AlphaFunction) * D1Psi(i, 0, h) * D1Psi(j, 0, h) * h_i / 2;
            sum += Alpha(1, AlphaFunction) * D1Psi(i, 1, h) * D1Psi(j, 1, h) * h_i / 2;

            return sum;
        }

        // вычисление интеграла для бета методом трапеций
        private static double SumPsiBeta(int i, int j, double h, Function BetaFunction)
        {
            const double h_i = 0.01;
            double sum = 0.0;
            for (int k = 0; k < 100; ++k)
            {
                double x_k = k * h_i;
                double x_k1 = (k + 1) * h_i;

                double f_xk = Beta(x_k, BetaFunction) * D2Psi(i, x_k, h) * D2Psi(j, x_k, h);
                double f_xk1 = Beta(x_k1, BetaFunction) * D2Psi(i, x_k1, h) * D2Psi(j, x_k1, h);

                sum += (f_xk + f_xk1) * h_i / 2;
            }

            sum += Beta(0, BetaFunction) * D2Psi(i, 0, h) * D2Psi(j, 0, h) * h_i / 2;
            sum += Beta(1, BetaFunction) * D2Psi(i, 1, h) * D2Psi(j, 1, h) * h_i / 2;

            return sum;
        }

        private static double Alpha(double x, Function AlphaFunction)
        {
            return AlphaFunction.CalculateFunctionValue(x);
        }

        private static double Beta(double x, Function BetaFunction)
        {
            return BetaFunction.CalculateFunctionValue(x);
        }

        private static double D1Psi(int l, double t, double h)
        {
            switch (l)
            {
                case 0: return (-6 * t + 6 * Math.Pow(t, 2)) / h;
                case 1: return (1 - 4 * t + 3 * Math.Pow(t, 2));
                case 2: return (6 * t - 6 * Math.Pow(t, 2)) / h;
                case 3: return (-2 * t + 3 * Math.Pow(t, 2));
                default: return 0;
            }
        }

        private static double D2Psi(int l, double t, double h)
        {
            switch (l)
            {
                case 0: return (-6 + 12 * t) / Math.Pow(h, 2);
                case 1: return (-4 + 6 * t) / h;
                case 2: return (6 - 12 * t) / Math.Pow(h, 2);
                case 3: return (-1 + 6 * t) / h;
                default: return 0;
            }
        }

        public double CalculateFunctionValue(double x)
        {
            // Проверка на допустимость значения x
            if (x < Grid[0] || x > Grid[^1] || !IsPossible)
                return double.NaN;

            // Поиск подходящего отрезка в сетке
            int elem = 0;
            while (elem < Grid.Length - 2 && x >= Grid[elem + 1])
                elem++;

            double h = Grid[elem + 1] - Grid[elem];
            double t = (x - Grid[elem]) / h;

            double result = 0.0;
            for (int i = 0; i < 4; ++i)
                result += slae.X[2 * elem + i] * Psi(i, t, h);

            return result;
        }

        public void GetLimits()
        {
            throw new NotImplementedException();
        }
    }

    public class SLAE
    {
        public double[][] A { get; set; } // матрица коэффициентов
        public double[] F { get; set; } // вектор свободных членов
        public double[] X { get; set; } // решение
        public int N { get; set; } // размер системы

        public SLAE(int size)
        {
            N = size;
            A = new double[size][];
            for (int i = 0; i < size; i++)
                A[i] = new double[size];
            F = new double[size];
            X = new double[size];
        }

        // инициализация матрицы и вектора нулями
        public void Initialize()
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                    A[i][j] = 0;
                F[i] = 0;
            }
        }

        public bool Solve()
        {
            // метод Гаусса с выбором ведущего элемента
            for (int k = 0; k < N - 1; ++k)
            {
                // поиск максимального элемента в столбце
                int i_max = k;
                double max_val = Math.Abs(A[k][k]);
                for (int i = k; i < N; ++i)
                {
                    if (Math.Abs(A[i][k]) > max_val)
                    {
                        i_max = i;
                        max_val = Math.Abs(A[i][k]);
                    }
                }

                // перестановка строк
                if (i_max != k)
                {
                    var temp = A[k];
                    A[k] = A[i_max];
                    A[i_max] = temp;

                    double tempF = F[k];
                    F[k] = F[i_max];
                    F[i_max] = tempF;
                }

                // деление строки
                double div = A[k][k];
                if (div == 0) return false;

                for (int j = k; j < N; ++j)
                    A[k][j] /= div;
                F[k] /= div;

                for (int i = k + 1; i < N; ++i)
                {
                    double factor = A[i][k];
                    for (int j = k; j < N; ++j)
                        A[i][j] -= factor * A[k][j];
                    F[i] -= factor * F[k];
                }
            }

            // обратный ход
            double last = A[N - 1][N - 1];
            if (last == 0) return false;
            X[N - 1] = F[N - 1] / last;

            for (int i = N - 2; i >= 0; --i)
            {
                double sum = 0.0;
                for (int j = i + 1; j < N; ++j)
                    sum += A[i][j] * X[j];
                X[i] = F[i] - sum;
            }

            return true;
        }
    }
}