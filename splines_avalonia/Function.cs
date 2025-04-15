using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NCalc;

namespace splines_avalonia;

public class Function : ICurve
{
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
    public Point[] OutputPoints { get; }
    public bool IsPossible { get; set; }
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
    public string SmoothingCoefficient { get; set; }

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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public Function(string functionString)
    {
        IsPossible = true;
        SmoothingCoefficient = null;
        FunctionString = functionString;
        Name = functionString; 
        IsVisible = true;
        PrepareExpression(FunctionString);
    }

    private void PrepareExpression(string function)
    {
        Console.SetOut(TextWriter.Null);

        try
        {
            string processed = PreprocessFunctionString(function);
            ValidateFunctionArguments(processed);
            _cachedExpr = new Expression(processed, EvaluateOptions.IgnoreCase);
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
        return func.Replace("sin", "Sin")
                   .Replace("cos", "Cos")
                   .Replace("tg", "Tan")
                   .Replace("tg", "Tan")
                   .Replace("log", "Log")
                   .Replace("lg", "Log10")
                   .Replace("pow", "Pow")
                   .Replace("sqrt", "Sqrt")
                   .Replace("exp", "Exp")
                   .Replace("π", "3.1415926535897932");
    }

    public double CalculateFunctionValue(string function, double x)
    {
        Console.SetOut(TextWriter.Null);

        try
        {
            if (_cachedExpr == null || function != FunctionString)
                PrepareExpression(function);

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

    private int CountArgumentsInFunctionCall(string argsSubstring)
    {
        int level = 0;
        int argCount = 1; // по умолчанию 1 аргумент
        for (int i = 0; i < argsSubstring.Length; i++)
        {
            char c = argsSubstring[i];
            if (c == '(') level++;
            else if (c == ')') level--;
            else if (c == ',' && level == 0)
                argCount++;
        }

        // если аргументов нет (например, пустые скобки), то возвращаем 0
        if (string.IsNullOrWhiteSpace(argsSubstring))
            return 0;

        return argCount;
    }

    private void ValidateFunctionArguments(string expression)
    {
        for (int i = 0; i < expression.Length; i++)
        {
            if (char.IsLetter(expression[i]))
            {
                int funcStart = i;
                while (i < expression.Length && char.IsLetter(expression[i]))
                    i++;

                string funcName = expression.Substring(funcStart, i - funcStart);

                // Пропускаем пробелы
                while (i < expression.Length && char.IsWhiteSpace(expression[i])) i++;

                if (i < expression.Length && expression[i] == '(')
                {
                    int start = i + 1;
                    int level = 1;

                    while (i + 1 < expression.Length && level > 0)
                    {
                        i++;
                        if (expression[i] == '(') level++;
                        else if (expression[i] == ')') level--;
                    }

                    if (level == 0)
                    {
                        string args = expression.Substring(start, i - start);
                        int argCount = CountArgumentsInFunctionCall(args);

                        ValidateFunction(funcName, argCount);
                    }
                    else
                    {
                        throw new Exception($"Невалидная скобочная структура в функции '{funcName}'.");
                    }
                }
            }
        }
    }

    private void ValidateFunction(string func, int args)
    {
        Console.SetOut(TextWriter.Null);

        string lowerFunc = func.ToLower();
        string[] singleArg = { "sin", "cos", "tg", "sqrt", "lg", "exp" };
        string[] doubleArg = { "pow", "log" };

        if (Array.Exists(singleArg, f => f == lowerFunc) && args != 1)
            throw new Exception($"Функция '{func}' должна иметь 1 аргумент, но получено {args}.");

        if (Array.Exists(doubleArg, f => f == lowerFunc) && args != 2)
            throw new Exception($"Функция '{func}' должна иметь 2 аргумента, но получено {args}.");
    }
}
