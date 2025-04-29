#pragma warning disable CS8603

namespace splines_avalonia
{
    public class SplineLogic : ILogic
    {
        public ICurve CreateFunction(string functionString)
        {
            return new Function(functionString);
        }

        public ICurve CreateInterpolatingSpline(Point[] controlPoints)
        {
            if (controlPoints != null)
                return new InterpolatingSpline(controlPoints);
            else
                return null;
        }

        public ICurve CreateSmoothingSpline(double[] grid, Point[] controlPoints, string smoothingCoefficientAlpha, string smoothingCoefficientBeta)
        {
            if (controlPoints != null && grid != null)
                return new SmoothingSpline(controlPoints, grid, smoothingCoefficientAlpha, smoothingCoefficientBeta);
            else
                return null;
        }
    }
}