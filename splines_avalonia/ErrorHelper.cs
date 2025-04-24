using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;

namespace splines_avalonia.Helpers;

public static class ErrorHelper
{
    public static async Task ShowError(string message)
    {
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
            Topmost = false,
        });
        await box.ShowAsync();
    }
}
