using System.Threading.Tasks;
using splines_avalonia.Helpers;

#pragma warning disable CS8603

namespace splines_avalonia
{
    public class SplineLogic : ILogic
    {
        public async Task<ICurve> CreateFunction(string functionString)
        {
            if (await FunctionChecker.TryValidateFunctionInput(functionString))
                return new Function(functionString);
            else
                return null;
        }

        public ICurve CreateInterpolatingSpline(Point[] controlPoints)
        {
            if (controlPoints != null || controlPoints.Length < 4)
                return new InterpolatingSpline(controlPoints);
            else
                return null;
        }

        public ICurve CreateSmoothingSpline(double[] grid, Point[] controlPoints, string smoothingCoefficientAlpha, string smoothingCoefficientBeta)
        {
            if (controlPoints != null || grid != null || controlPoints.Length < 4)
                return new SmoothingSpline(controlPoints, grid, smoothingCoefficientAlpha, smoothingCoefficientBeta);
            else
                return null;
        }

        public ICurve CreateLinearSpline(Point[] controlPoints)
        {
            if (controlPoints != null)
                return new LinearSpline(controlPoints);
            else
                return null;
        }
    }
}