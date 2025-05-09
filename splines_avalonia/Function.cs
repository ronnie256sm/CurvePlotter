using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NCalc;
using Avalonia.Media;
using splines_avalonia.Helpers;
using System.Threading.Tasks;
using CSharpMath.Atom.Atoms;

namespace splines_avalonia
{
    public class Function : ICurve
    {
        #pragma warning disable CS8603, CS8618, CS8602
        private string _functionString;
        public string FunctionString
        {
            get => _functionString;
            set
            {
                _functionString = value;
                Name = value; // обновляем отображаемое имя
                OnPropertyChanged(nameof(FunctionString));
                OnPropertyChanged(nameof(Name));
                PrepareExpression(value);
            }
        }
        private string _start;
        public string Start
        {
            get => _start;
            set
            {
                _start = value;
                OnPropertyChanged(nameof(Start));
            }
        }
        private string _end;
        public string End
        {
            get => _end;
            set
            {
                _end = value;
                OnPropertyChanged(nameof(End));
            }
        }
        public Point[] OutputPoints { get; }
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
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        public string SmoothingCoefficientAlpha { get; set; }
        public string SmoothingCoefficientBeta { get; set; }

        public string Type => "Function";
        public string SplineType => null;
        public double[] Grid => null;
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
        public Point[] ControlPoints => null;

        private Expression _cachedExpr;
        private bool _hasError = false;
        public string ControlPointsFile { get; set; }
        public string GridFile { get; set; }
        public bool ShowControlPoints { get; set; }
        public double ParsedStart { get; set; }
        public double ParsedEnd { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public Function(string functionString)
        {
            Color = Colors.Black;
            IsPossible = true;
            FunctionString = functionString;
            Name = functionString;
            IsVisible = true;
            ParsedStart = Double.NegativeInfinity;
            ParsedEnd = Double.PositiveInfinity;
        }

        public async void GetLimits()
        {
            if (Start != null)
            {
                ParsedStart = await NumberParser.ParseNumber(Start) ?? Double.NegativeInfinity;
            }
            else
            {
                ParsedStart = Double.NegativeInfinity;
            }
            if (End != null)
            {
                ParsedEnd = await NumberParser.ParseNumber(End) ?? Double.PositiveInfinity;
            }
            else
            {
                ParsedEnd = Double.PositiveInfinity;
            }
        }

        private void PrepareExpression(string function)
        {
            Console.SetOut(TextWriter.Null);

            try
            {
                string processed = PreprocessFunctionString(function);
                _cachedExpr = new Expression(processed, EvaluateOptions.IgnoreCase);

                // Добавляем обработчик ДЛЯ ЭТОГО экземпляра Expression
                _cachedExpr.EvaluateFunction += (name, args) =>
                {
                    switch (name.ToLower())
                    {
                        case "cot":
                            args.Result = 1.0 / Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate()));
                            break;
                        case "arccot":
                            args.Result = Math.Atan(-Convert.ToDouble(args.Parameters[0].Evaluate())) + (Math.PI / 2);
                            break;
                        case "sgn":
                            args.Result = Math.Sign(Convert.ToDouble(args.Parameters[0].Evaluate()));
                            break;
                        case "ln":
                            args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()));
                            break;
                        case "sh":
                            args.Result = (Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate())) - Math.Exp(-Convert.ToDouble(args.Parameters[0].Evaluate()))) / 2;
                            break;
                        case "ch":
                            args.Result = (Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate())) + Math.Exp(-Convert.ToDouble(args.Parameters[0].Evaluate()))) / 2;
                            break;
                        case "th":
                            args.Result = (Math.Exp(2 * Convert.ToDouble(args.Parameters[0].Evaluate())) - 1) / (Math.Exp(2 * Convert.ToDouble(args.Parameters[0].Evaluate())) + 1);
                            break;
                        case "cth":
                            args.Result = (Math.Exp(2 * Convert.ToDouble(args.Parameters[0].Evaluate())) + 1) / (Math.Exp(2 * Convert.ToDouble(args.Parameters[0].Evaluate())) - 1);
                            break;
                        case "sch":
                            args.Result = 2 / (Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate())) + Math.Exp(-Convert.ToDouble(args.Parameters[0].Evaluate())));
                            break;
                        case "csch":
                            args.Result = 2 / (Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate())) - Math.Exp(-Convert.ToDouble(args.Parameters[0].Evaluate())));
                            break;
                        case "sec":
                            args.Result = 1 / Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate()));
                            break;
                        case "cosec":
                            args.Result = 1 / Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate()));
                            break;
                        case "arcsec":
                            args.Result = Math.Acos(1 / Convert.ToDouble(args.Parameters[0].Evaluate()));
                            break;
                        case "arccsec":
                            args.Result = Math.Asin(1 / Convert.ToDouble(args.Parameters[0].Evaluate()));
                            break;
                    }
                };

                _hasError = false; // если успешно — сбрасываем флаг
            }
            catch (Exception ex)
            {
                if (!_hasError)
                {
                    Debug.WriteLine($"[Function Error] {ex.Message}");
                    _hasError = true;
                }
                _cachedExpr = new Expression("0");
            }
        }

        private string PreprocessFunctionString(string func)
        {
            return func.Replace("arcsin", "Asin")
                       .Replace("arccos", "Acos")
                       .Replace("arctg", "Atan")
                       .Replace("arcctg", "Arccot")
                       .Replace("ctg", "Cot")
                       .Replace("sin", "Sin")
                       .Replace("cos", "Cos")
                       .Replace("tg", "Tan")
                       .Replace("log", "Log")
                       .Replace("ln", "Ln")
                       .Replace("lg", "Log10")
                       .Replace("pow", "Pow")
                       .Replace("sqrt", "Sqrt")
                       .Replace("exp", "Exp")
                       .Replace("sgn", "Sgn")
                       .Replace("π", "3.1415926535897932");
        }

        public double CalculateFunctionValue(string function, double x)
        {
            Console.SetOut(TextWriter.Null);

            try
            {
                if (_cachedExpr == null || function != FunctionString)
                {
                    PrepareExpression(function);
                }

                _cachedExpr.Parameters["x"] = x;
                var result = _cachedExpr.Evaluate();
                if (result is double d)
                    return d;
                if (result is int i)
                    return i;

                return 0;
            }
            catch (Exception ex)
            {
                if (!_hasError)
                {
                    Debug.WriteLine($"[Evaluate Error] {ex.Message}");
                    _hasError = true;
                }
                _cachedExpr = new Expression("0");
                return double.NaN;
            }
        }
    }
}