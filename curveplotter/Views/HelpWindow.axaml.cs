using System.Reactive.Linq;
using Avalonia.Controls;

namespace CurvePlotter;

public partial class HelpWindow : Window
{
    private HelpWindowViewModel ViewModel => (HelpWindowViewModel)DataContext!;

    public HelpWindow()
    {
        InitializeComponent();
        DataContext = new HelpWindowViewModel();
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private async void HelpTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (HelpTreeView.SelectedItem is TreeViewItem selectedItem)
        {
            var gifFile = selectedItem.Tag?.ToString() ?? string.Empty;
            var description = selectedItem.Header?.ToString() ?? "Выбран элемент";

            await ViewModel.SelectItemCommand.Execute((gifFile, description));
        }
    }
}
