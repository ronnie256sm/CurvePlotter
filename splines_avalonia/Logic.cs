using System;

namespace splines_avalonia
{
    public class SplineLogic : ILogic
    {
        public ICurve CreateCurve(string type, string splineType, string functionString, double[] grid, Point[] controlPoints, string smoothingCoefficientAlpha, string smoothingCoefficientBeta)
        {
            if (type == "Function")
            {
                return new Function(functionString);
            }
            else if (type == "Spline")
            {
                return splineType switch
                {
                    "Interpolating Cubic" => new CubicSpline(controlPoints, grid),
                    "Smoothing Cubic" => new SmoothingSpline(controlPoints, grid, smoothingCoefficientAlpha, smoothingCoefficientBeta),
                    _ => throw new NotSupportedException($"Тип сплайна '{splineType}' не поддерживается.")
                };
            }
            else
            {
                throw new NotSupportedException($"Тип кривой '{type}' не поддерживается.");
            }
        }
    }
}