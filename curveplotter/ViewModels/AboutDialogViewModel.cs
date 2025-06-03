using ReactiveUI;

namespace CurvePlotter.ViewModels
{
    public class AboutDialogViewModel : ReactiveObject
    {
        public string AppName { get; }
        public string Version { get; }

        public AboutDialogViewModel()
        {
            AppName = "CurvePlotter";
            Version = "Версия: 0.0.1";
        }
    }
}