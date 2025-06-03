using Avalonia.Controls;
using Avalonia.Interactivity;
using CurvePlotter.ViewModels;

namespace CurvePlotter.Views
{
    public partial class FunctionInputDialog : Window
    {
        public FunctionInputDialogViewModel ViewModel { get; }

        public FunctionInputDialog()
        {
            InitializeComponent();
            ViewModel = new FunctionInputDialogViewModel();
            DataContext = ViewModel;
        }

        public void SetInitialFunction(string function, string start, string end)
        {
            ViewModel.FunctionText = function;
            ViewModel.Start = start;
            ViewModel.End = end;
        }

        private void OnInputClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                ViewModel.AddToFunction(btn.Content?.ToString() ?? "");
            }
        }

        private void OnClearAllClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.ClearFunction();
        }

        private void OnBackspaceClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.Backspace();
        }

        private async void OnOkClick(object? sender, RoutedEventArgs e)
        {
            var success = await ViewModel.ValidateAndSetResultAsync();
            if (success)
            {
                var result = ViewModel.GetFunctionDetails();
                Close(result);
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
