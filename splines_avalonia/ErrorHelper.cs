using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using Avalonia.Controls.ApplicationLifetimes;

namespace splines_avalonia.Helpers;

public static class ErrorHelper
{
    public static async Task ShowError(string message)
    {
        var owner = GetActiveWindow();

        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = "Ошибка",
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
            await box.ShowWindowDialogAsync(owner); // если нашли активное окно, показываем модально
        else
            await box.ShowAsync(); // иначе просто показываем
    }

    private static Window? GetActiveWindow()
    {
        // Берём текущее активное окно
        return Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => 
                desktop.Windows.FirstOrDefault(x => x.IsActive),
            _ => null
        };
    }
}
