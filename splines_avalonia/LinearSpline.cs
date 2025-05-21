using System.ComponentModel;
using Avalonia.Media;
using splines_avalonia.ViewModels;

namespace splines_avalonia;

public class LinearSpline : ICurve
{
    public string? Name { get; set; }
    public string Type => "Spline";
    public string? FunctionString { get; set; }
    public string SplineType => "Linear";
    public double[]? Grid { get; }
    public Point[] ControlPoints { get; }
    public string? ControlPointsFile { get; set; }
    public string? GridFile { get; set; }
    public string? SmoothingCoefficientAlpha { get; set; }
    public string? SmoothingCoefficientBeta { get; set; }
    public string? Start { get; set; }
    public string? End { get; set; }
    public double ParsedStart { get; set; }
    public double ParsedEnd { get; set; }
    public bool ShowControlPoints { get; set; }

    public bool IsPossible { get; set; }

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

    private double _thickness = 2;
    public double Thickness
    {
        get => _thickness;
        set
        {
            if (_thickness != value)
            {
                _thickness = value;
                OnPropertyChanged(nameof(Thickness));
            }
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public LinearSpline(Point[] controlPoints)
    {
        ControlPoints = controlPoints;
        Name = "Ломаная";
        IsVisible = true;
        IsPossible = true;
        Color = Globals.DarkMode ? Colors.White : Colors.Black;
        Thickness = 2;
    }

    public double CalculateFunctionValue(double x)
    {
        if (ControlPoints == null || ControlPoints.Length < 2)
            return double.NaN;

        for (int i = 0; i < ControlPoints.Length - 1; i++)
        {
            double x0 = ControlPoints[i].X;
            double x1 = ControlPoints[i + 1].X;

            if (x >= x0 && x <= x1)
            {
                double y0 = ControlPoints[i].Y;
                double y1 = ControlPoints[i + 1].Y;

                double t = (x - x0) / (x1 - x0);
                return y0 + t * (y1 - y0);
            }
        }

        return double.NaN;
    }

    public void GetLimits()
    {
        throw new System.NotImplementedException();
    }
}
