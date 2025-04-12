using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using Avalonia.Media;

namespace splines_avalonia.Helpers;

public static class ErrorHelper
{
    public static async Task ShowError(Window owner, string message, string title = "Ошибка")
    {
        var dialog = new Window
        {
            Title = title,
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };
        okButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(10),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap
                },
                okButton
            }
        };

        await dialog.ShowDialog(owner);
    }
}
