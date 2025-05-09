using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ReactiveUI;
using splines_avalonia.Helpers;

namespace splines_avalonia.ViewModels
{
    public class FunctionInputDialogViewModel : INotifyPropertyChanged
    {
        private string _functionText = "";

        public string FunctionText
        {
            get => _functionText;
            set
            {
                if (_functionText != value)
                {
                    _functionText = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _start = "";
        public string Start
        {
            get => _start;
            set
            {
                if (_start != value)
                {
                    _start = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _end = "";
        public string End
        {
            get => _end;
            set
            {
                if (_end != value)
                {
                    _end = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FunctionString { get; private set; } = "";

        public void AddToFunction(string input)
        {
            FunctionText += input;
        }

        public void ClearFunction()
        {
            FunctionText = "";
        }

        public void Backspace()
        {
            if (!string.IsNullOrEmpty(FunctionText))
            {
                FunctionText = FunctionText[..^1];
            }
        }

        public async Task<bool> ValidateAndSetResultAsync()
        {
            var input = FunctionText;
            var isValid = await FunctionChecker.TryValidateFunctionInput(input);
            if (isValid)
            {
                FunctionString = input;
                return true;
            }
            return false;
        }

        // Метод для получения всех данных
        public (string FunctionString, string Start, string End) GetFunctionDetails()
        {
            return (FunctionString, Start, End);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
