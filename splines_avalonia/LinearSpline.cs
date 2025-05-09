using System.Collections.Generic;
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
        SmoothingCoefficientAlpha = null;
        SmoothingCoefficientBeta = null;
        ControlPoints = controlPoints;
        Name = "Ломаная";
        IsVisible = true;
        IsPossible = true;
        // if (ControlPoints.Length < 4)
        // {
        //     IsPossible = false;
        // }
    }
    public double CalculateFunctionValue(string functionString, double x)
    {
        throw new System.NotImplementedException();
    }
}