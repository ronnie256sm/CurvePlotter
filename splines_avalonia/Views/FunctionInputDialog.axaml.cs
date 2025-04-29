using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using splines_avalonia.Helpers;

namespace splines_avalonia.Views
{
    public partial class FunctionInputDialog : Window
    {
        #pragma warning disable CS8603
        public string FunctionString { get; private set; } = "";

        public FunctionInputDialog()
        {
            InitializeComponent();
        }

        public void SetInitialFunction(string function)
        {
            FunctionDisplay.Text = function;
        }

        private void OnInputClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && FunctionDisplay != null)
            {
                FunctionDisplay.Text += btn.Content?.ToString();
            }
        }

        private void OnClearAllClick(object? sender, RoutedEventArgs e)
        {
            if (FunctionDisplay != null)
                FunctionDisplay.Text = "";
        }

        private void OnBackspaceClick(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FunctionDisplay?.Text))
            {
                FunctionDisplay.Text = FunctionDisplay.Text[..^1];
            }
        }

        private async void OnOkClick(object? sender, RoutedEventArgs e)
        {
            var input = FunctionDisplay?.Text ?? "";

            if (string.IsNullOrWhiteSpace(input))
            {
                await ErrorHelper.ShowError("Ошибка", "Функция не может быть пустой.");
                return;
            }

            if (!AreBracketsBalanced(input))
            {
                await ErrorHelper.ShowError("Ошибка", "Количество открывающих и закрывающих скобок не совпадает.");
                return;
            }

            if (!ContainsOnlyValidCharacters(input))
            {
                await ErrorHelper.ShowError("Ошибка", "Выражение содержит недопустимые символы.");
                return;
            }

            string validationError = ValidateFunctionArguments(input);
            if (validationError != null)
            {
                await ErrorHelper.ShowError("Ошибка", validationError);
                return;
            }

            FunctionString = input;
            Close(FunctionString);
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private bool AreBracketsBalanced(string expression)
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

        private bool ContainsOnlyValidCharacters(string expression)
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

        private string ValidateFunctionArguments(string expression)
        {
            // Упрощаем разбор функции с рекурсивной обработкой вложенных функций.
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

                        // Разбираем вложенные скобки
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

                            string validationError = ValidateFunction(funcName, argCount);
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
                    i++; // Пропускаем любые другие символы (например, пробелы или знаки)
                }
            }
            return null;
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

            if (string.IsNullOrWhiteSpace(argsSubstring))
                return 0;

            return argCount;
        }

        private string ValidateFunction(string func, int args)
        {
            string lowerFunc = func.ToLower();
            string[] singleArg = { "sin", "cos", "tg", "sqrt", "lg", "exp" };
            string[] doubleArg = { "pow", "log" };

            if (Array.Exists(singleArg, f => f == lowerFunc) && args != 1)
                return $"Функция '{func}' должна иметь 1 аргумент, но получено {args}.";

            if (Array.Exists(doubleArg, f => f == lowerFunc) && args != 2)
                return $"Функция '{func}' должна иметь 2 аргумента, но получено {args}.";

            return null;
        }
    }
}
