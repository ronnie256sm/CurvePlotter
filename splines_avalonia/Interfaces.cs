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

    public interface ISpline
    {
        string Type { get; }
        double[] Grid { get; }
        Point[] ControlPoints { get; }
        Point[] OutputPoints { get; }
    }

    public interface IFunction
    {
        string FunctionString { get; }
        Expression FunctionExpression { get; }
        Point[] OutputPoints { get; }
        public double CalculateFunctionValue(string functionString, double x);
    }

    public interface ILogic
    {
        public ISpline CreateSpline(string type, double[] grid, Point[] controlPoints);
        public IFunction CreateFunction(string FunctionString);
    }

    interface IGrid
    {
        void Move(double x, double y);
        void ScrollChanged(int delta);
        double ScrollValue();
        void DrawGrid();
        void DrawGraphic(int step, double deltaX, double deltaY, double start, double end);
        void UnselectGraph();
        void RerenderGrid();
        void RerenderGraphics();
    }
}