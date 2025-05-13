using System.ComponentModel;
using Avalonia.Media;

namespace splines_avalonia;
#pragma warning disable CS8618, CS8625

public class LinearSpline : ICurve
{
    public string Name { get; set; }
    public string Type => "Spline";
    public string FunctionString { get; set; }
    public string SplineType => "Linear";
    public double[] Grid { get; }
    public Point[] ControlPoints { get; }
    public string ControlPointsFile { get; set; }
    public string GridFile { get; set; }
    public Point[] OutputPoints { get; }
    public string SmoothingCoefficientAlpha { get; set; }
    public string SmoothingCoefficientBeta { get; set; }
    public string Start { get; set; }
    public string End { get; set; }
    public double ParsedStart { get; set; }
    public double ParsedEnd { get; set; }
    public bool ShowControlPoints { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
    public bool IsPossible { get; set; }
    private Color _color;
    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            OnPropertyChanged(nameof(Color));
        }
    }

    public LinearSpline(Point[] controlPoints)
    {
        Color = Colors.Black;
        ControlPoints = controlPoints;
        Name = "Ломаная";
        IsVisible = true;
        IsPossible = true;
    }
    
    public double CalculateFunctionValue(double x)
    {
        if (ControlPoints == null || ControlPoints.Length < 2)
            return double.NaN;

        // предполагается, что точки отсортированы по X
        for (int i = 0; i < ControlPoints.Length - 1; i++)
        {
            double x0 = ControlPoints[i].X;
            double x1 = ControlPoints[i + 1].X;

            if (x >= x0 && x <= x1)
            {
                double y0 = ControlPoints[i].Y;
                double y1 = ControlPoints[i + 1].Y;

                double t = (x - x0) / (x1 - x0);
                return y0 + t * (y1 - y0); // линейная интерполяция
            }
        }

        // x вне диапазона
        return double.NaN;
    }

    public void GetLimits()
    {
        throw new System.NotImplementedException();
    }
}