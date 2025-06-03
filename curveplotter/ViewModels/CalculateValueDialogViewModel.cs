using ReactiveUI;
using System;

namespace CurvePlotter.ViewModels
{
    public class CalculateValueDialogViewModel : ReactiveObject
    {
        private string _xValue = "";
        private string _yValue = "";
        private ICurve? _curve;

        public string XValue
        {
            get => _xValue;
            set => this.RaiseAndSetIfChanged(ref _xValue, value);
        }

        public string YValue
        {
            get => _yValue;
            set => this.RaiseAndSetIfChanged(ref _yValue, value);
        }

        public ICurve? Curve
        {
            get => _curve;
            set => this.RaiseAndSetIfChanged(ref _curve, value);
        }

        public void CalculateYValue(double x)
        {
            if(Curve != null)
            {
                double y = Curve.CalculateFunctionValue(x);

                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    YValue = "Значения нет";
                }
                else if (Math.Abs(y) < 1e-10) // Обработка очень маленьких чисел
                {
                    YValue = "0";
                }
                else
                {
                    YValue = y.ToString("G10"); // Форматирование с 10 значащими цифрами
                }
            }
        }
    }
}
