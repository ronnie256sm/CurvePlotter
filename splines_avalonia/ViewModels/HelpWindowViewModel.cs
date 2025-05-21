using Avalonia.Platform;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;

namespace splines_avalonia
{
    public class HelpWindowViewModel : ReactiveObject
    {
        private string _description = "Выберите нужную функцию и тут появится информация о ней";
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        private string? _gifSource;
        public string? GifSource
        {
            get => _gifSource;
            set => this.RaiseAndSetIfChanged(ref _gifSource, value);
        }

        private string? _content;
        public string? Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        public ReactiveCommand<(string GifFile, string Description), Unit> SelectItemCommand { get; }

        public HelpWindowViewModel()
        {
            SelectItemCommand = ReactiveCommand.Create<(string Path, string Content)>(SelectItem);
        }

        private void SelectItem((string Path, string Content) args)
        {
            Content = args.Content;

            if (!string.IsNullOrEmpty(args.Path))
            {
                var gifPath = $"avares://splines_avalonia/Assets/gif/{args.Path}.gif";
                try
                {
                    GifSource = gifPath;
                }
                catch (Exception)
                {
                    GifSource = null;
                }
            }
            else
            {
                GifSource = null;
            }

            var descriptionPath = $"avares://splines_avalonia/Assets/man/{args.Path}.txt";
            try
            {
                using (var stream = AssetLoader.Open(new Uri(descriptionPath)))
                using (var reader = new StreamReader(stream))
                {
                    Description = reader.ReadToEnd();
                }
            }
            catch
            {
                Description = "Выберите нужную функцию и тут появится информация о ней";
            }
        }
    }
}
