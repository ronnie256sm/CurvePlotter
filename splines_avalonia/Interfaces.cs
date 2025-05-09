using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Media;

namespace splines_avalonia
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Point (double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public interface ICurve : INotifyPropertyChanged
    {
        string Name { get; set; }
        string Type { get; }
        string FunctionString { get; set; }
        string SplineType { get; }
        double[] Grid { get; }
        Point[] ControlPoints { get; }
        string ControlPointsFile { get; set; }
        string GridFile { get; set; }
        Point[] OutputPoints { get; }
        bool IsVisible { get; set; }
        string SmoothingCoefficientAlpha { get; set; }
        string SmoothingCoefficientBeta { get; set; }
        double CalculateFunctionValue(string functionString, double x);
        bool IsPossible { get; set; }
        Color Color { get; set; }
        string Start { get; set; }
        string End { get; set; }
        bool ShowControlPoints { get; set; }
    }

    public interface ILogic
    {
        public Task<ICurve> CreateFunction(string functionString);
        public ICurve CreateInterpolatingSpline(Point[] controlPoints);
        public ICurve CreateSmoothingSpline(double[] grid, Point[] controlPoints, string smoothingCoefficientAlpha, string smoothingCoefficientBeta);
        public ICurve CreateLinearSpline(Point[] controlPoints);
    }
}