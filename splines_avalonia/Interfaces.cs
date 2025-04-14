using System.ComponentModel;

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
        string SmoothingCoefficient { get; set; }
        public double CalculateFunctionValue(string functionString, double x);
        bool IsPossible { get; set; }
    }

    public interface ILogic
    {
        public ICurve CreateCurve(string type, string splineType, string functionString, double[] grid, Point[] controlPoints, string smoothingCoefficient);
    }
}