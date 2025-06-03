using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using NCalc;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CurvePlotter.Helpers;

public static class ErrorHelper
{
    public static async Task ShowError(string contentTitle, string message)
    {
        var owner = GetActiveWindow();

        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = contentTitle,
            ContentMessage = message,
            ButtonDefinitions = new[]
            {
                new ButtonDefinition { Name = "ОK"},
            },
            Icon = MsBox.Avalonia.Enums.Icon.Question,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            MaxWidth = 500,
            MaxHeight = 800,
            SizeToContent = SizeToContent.WidthAndHeight,
            ShowInCenter = true,
            Topmost = true,
        });

        if (owner is not null)
            await box.ShowWindowDialogAsync(owner);
        else
            await box.ShowAsync();
    }

    private static Window? GetActiveWindow()
    {
        // берём текущее активное окно
        return Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => 
                desktop.Windows.FirstOrDefault(x => x.IsActive),
            _ => null
        };
    }
}

public static class FunctionChecker
{
    public static async Task<bool> TryValidateFunctionInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            await ErrorHelper.ShowError("Ошибка", "Функция не может быть пустой.");
            return false;
        }

        if (!AreBracketsBalanced(input))
        {
            await ErrorHelper.ShowError("Ошибка", "Количество открывающих и закрывающих скобок не совпадает.");
            return false;
        }

        if (!ContainsOnlyValidCharacters(input))
        {
            await ErrorHelper.ShowError("Ошибка", "Выражение содержит недопустимые символы.");
            return false;
        }

        string? validationError = ValidateFunctionArguments(input);
        if (validationError != null)
        {
            await ErrorHelper.ShowError("Ошибка", validationError);
            return false;
        }

        return true;
    }

    private static bool AreBracketsBalanced(string expression)
    {
        int balance = 0;
        foreach (char c in expression)
        {
            if (c == '(') balance++;
            if (c == ')') balance--;
            if (balance < 0) return false;
        }
        return balance == 0;
    }

    private static bool ContainsOnlyValidCharacters(string expression)
    {
        foreach (char c in expression)
        {
            if (!char.IsLetterOrDigit(c) &&
                !"()+-*/^.,_".Contains(c))
            {
                return false;
            }
        }

        return true;
    }

    private static string? ValidateFunctionArguments(string expression)
    {
        int i = 0;
        while (i < expression.Length)
        {
            if (char.IsLetter(expression[i]))
            {
                int funcStart = i;
                while (i < expression.Length && char.IsLetter(expression[i]))
                    i++;

                string funcName = expression.Substring(funcStart, i - funcStart);

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

                        string? validationError = ValidateFunction(funcName, argCount);
                        if (validationError != null)
                            return validationError;
                    }
                    else
                    {
                        return $"Невалидная скобочная структура в функции '{funcName}'.";
                    }
                }
            }
            else
            {
                i++;
            }
        }
        return null;
    }

    private static int CountArgumentsInFunctionCall(string argsSubstring)
    {
        int level = 0;
        int argCount = 1;
        for (int i = 0; i < argsSubstring.Length; i++)
        {
            char c = argsSubstring[i];
            if (c == '(') level++;
            else if (c == ')') level--;
            else if (c == ',' && level == 0)
                argCount++;
        }

        if (string.IsNullOrWhiteSpace(argsSubstring))
            return 0;

        return argCount;
    }

    private static string? ValidateFunction(string func, int args)
    {
        string lowerFunc = func.ToLower();
        string[] singleArg = {
            "sin", "cos", "tg", "ctg",
            "arcsin", "arccos", "arctg", "arcctg",
            "sqrt", "lg", "exp", "sgn", "ln"
        };
        string[] doubleArg = { "pow", "log" };

        if (Array.Exists(singleArg, f => f == lowerFunc) && args != 1)
            return $"Функция '{func}' должна иметь 1 аргумент, но получено {args}.";

        if (Array.Exists(doubleArg, f => f == lowerFunc) && args != 2)
            return $"Функция '{func}' должна иметь 2 аргумента, но получено {args}.";

        return null;
    }
}

public static class NumberParser
{
    public static async Task<double?> ParseNumber(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // Заменяем pi на значение числа пи
        input = Regex.Replace(input, @"\bpi\b", Math.PI.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);

        // проверка на допустимые символы
        if (!Regex.IsMatch(input, @"^[0-9+\-*/().\s]+$"))
        {
            await ErrorHelper.ShowError("Ошибка ввода", "Разрешены только числа, операции (+ - * /), скобки и 'pi'.");
            return null;
        }

        try
        {
            var expr = new Expression(input);
            var result = expr.Evaluate();

            return result switch
            {
                double d => d,
                int i => i,
                decimal m => (double)m,
                _ => await ReturnError()
            };
        }
        catch (Exception ex)
        {
            await ErrorHelper.ShowError("Ошибка разбора", $"Невозможно обработать выражение: {ex.Message}");
            return null;
        }

        async Task<double?> ReturnError()
        {
            await ErrorHelper.ShowError("Ошибка", "Выражение не является числом.");
            return null;
        }
    }
}