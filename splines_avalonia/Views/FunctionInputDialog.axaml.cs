using Avalonia.Controls;
using Avalonia.Interactivity;
using splines_avalonia.Helpers;

namespace splines_avalonia.Views
{
    public partial class FunctionInputDialog : Window
    {
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

            if (await FunctionChecker.TryValidateFunctionInput(input))
            {
                FunctionString = input;
                Close(FunctionString);
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
