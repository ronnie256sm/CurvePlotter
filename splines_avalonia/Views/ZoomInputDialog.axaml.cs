using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace splines_avalonia.Views
{
    public partial class ZoomInputDialog : Window
    {
        public string ScaleText => this.FindControl<TextBox>("ScaleInput")?.Text ?? "";

        public ZoomInputDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnOkClicked(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void OnCancelClicked(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
