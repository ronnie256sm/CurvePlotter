using System;
using NCalc;

namespace splines_avalonia;

public class Function : IFunction
{
    public string FunctionString { get; }
    public Expression FunctionExpression { get; }
    public Point[] OutputPoints { get; }

    public Function(string functionString)
    {
        FunctionString = functionString;
        double xValue = 0;
        double yValue = CalculateFunctionValue(functionString, xValue);
    }

    public double CalculateFunctionValue(string function, double x)
    {
        try
        {
            function = function.Replace("sin", "Sin")
                                .Replace("cos", "Cos")
                                .Replace("tg", "Tan")
                                .Replace("ln", "Ln")
                                .Replace("log", "Log")
                                .Replace("lg", "Log10")
                                .Replace("pow", "Pow")
                                .Replace("sqrt", "Sqrt")
                                .Replace("exp", "Exp")
                                .Replace("π", "3.1415926535897932");

            ValidateFunctionArguments(function);
            Expression expression = new Expression(function);
            expression.Parameters["x"] = x;
            return Convert.ToSingle(expression.Evaluate());
        }
        catch
        {
            return 0;
        }
    }

    private void ValidateFunctionArguments(string function)
    {
        string[] singleArgFunctions = { "sin", "cos", "tg", "sqrt", "ln", "lg", "exp" };
        string[] doubleArgFunctions = { "pow", "log" };

        int index = 0;
        while (index < function.Length)
        {
            foreach (string func in singleArgFunctions)
            {
                if (function.Substring(index).StartsWith(func + "("))
                {
                    int args = CountArgumentsInParentheses(function, index + func.Length);
                    if (args != 1)
                    {
                    throw new Exception($"Функция '{func}' должна иметь ровно 1 аргумент.");
                    }
                    index += func.Length;
                    break;
                }
            }

            foreach (string func in doubleArgFunctions)
            {
                if (function.Substring(index).StartsWith(func + "("))
                {
                    int args = CountArgumentsInParentheses(function, index + func.Length);
                    if (args != 2)
                    {
                    throw new Exception($"Функция '{func}' должна иметь ровно 2 аргумента.");
                    }
                    index += func.Length;
                    break;
                }
            }

            index++;
        }
    }

    private int CountArgumentsInParentheses(string function, int startIndex)
    {
        int openBracketIndex = function.IndexOf('(', startIndex);
        int closeBracketIndex = function.IndexOf(')', openBracketIndex);

        if (openBracketIndex == -1 || closeBracketIndex == -1)
        {
            throw new Exception("Некорректное использование скобок в функции.");
        }

        string arguments = function.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);

        if (string.IsNullOrWhiteSpace(arguments))
        {
            return 0; // Нет аргументов
        }

        return arguments.Split(',').Length;
    }
}