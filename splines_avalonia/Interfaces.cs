using NCalc;

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

    public interface ICurve
    {
        string Name { get; set; }
        string Type { get; }
        string FunctionString { get; }
        string SplineType { get; }
        double[] Grid { get; }
        Point[] ControlPoints { get; }
        Point[] OutputPoints { get; }
        bool IsVisible { get; set; }
        public double CalculateFunctionValue(string functionString, double x);
    }

    public interface ILogic
    {
        public ICurve CreateCurve(string type, string splineType, string functionString, double[] grid, Point[] controlPoints);
    }
}