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
            return new InterpolatingSpline(controlPoints);
        }

        public ICurve CreateSmoothingSpline(double[] grid, Point[] controlPoints, string smoothingCoefficientAlpha, string smoothingCoefficientBeta)
        {
            return new SmoothingSpline(controlPoints, grid, smoothingCoefficientAlpha, smoothingCoefficientBeta);
        }
    }
}