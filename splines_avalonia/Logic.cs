using System;

namespace splines_avalonia
{
    public class SplineLogic : ILogic
    {
        public ISpline CreateSpline(string type, double[] grid, Point[] controlPoints)
        {
            return type switch
            {
                "Interpolating Cubic" => new CubicSpline(controlPoints, grid),
                "Smoothing Cubic" => new SmoothingSpline(controlPoints, grid),
                _ => throw new NotSupportedException($"Тип сплайна '{type}' не поддерживается.")
            };
        }

        public IFunction CreateFunction(string functionString)
        {
            return new Function(functionString);
        }
    }
}