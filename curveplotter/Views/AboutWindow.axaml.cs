// AboutDialog.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;
using CurvePlotter.ViewModels;

namespace CurvePlotter.Views
{
    public partial class AboutDialog : Window
    {
        public AboutDialogViewModel ViewModel { get; }

        public AboutDialog()
        {
            InitializeComponent();
            ViewModel = new AboutDialogViewModel();
            DataContext = ViewModel;
        }

        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}