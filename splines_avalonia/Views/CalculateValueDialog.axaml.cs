using Avalonia.Controls;
using Avalonia.Interactivity;
using splines_avalonia.Helpers;
using splines_avalonia.ViewModels;

namespace splines_avalonia.Views
{
    public partial class CalculateValueDialog : Window
    {
        public CalculateValueDialogViewModel ViewModel { get; }

        public CalculateValueDialog()
        {
            InitializeComponent();
            ViewModel = new CalculateValueDialogViewModel();
            DataContext = ViewModel;
        }

        private async void OnCalculateClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.XValue))
            {
                await ErrorHelper.ShowError("Ошибка", "Введите значение X.");
                return;
            }

            double? xValue = await NumberParser.ParseNumber(ViewModel.XValue);
            if (xValue == null)
            {
                await ErrorHelper.ShowError("Ошибка", "Некорректное значение X.");
                return;
            }

            ViewModel.CalculateYValue(xValue.Value);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void SetCurve(ICurve curve)
        {
            ViewModel.Curve = curve;
        }
    }
}
