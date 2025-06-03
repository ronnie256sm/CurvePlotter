using System.Threading.Tasks;
using CurvePlotter.Helpers;

namespace CurvePlotter
{
    public class SplineLogic : ILogic
    {
        public async Task<ICurve?> CreateFunction(string functionString)
        {
            if (await FunctionChecker.TryValidateFunctionInput(functionString))
                return new Function(functionString);
            else
                return null;
        }

        public ICurve? CreateInterpolatingSpline(Point[] controlPoints, int type)
        {
            if (controlPoints != null && controlPoints.Length >= 3)
            {
                if (type == 1)
                    return new InterpolatingSpline1(controlPoints);
                if (type == 2)
                    return new InterpolatingSpline2(controlPoints);
                else
                    return null;
            }
            else
                return null;
        }

        public ICurve? CreateSmoothingSpline(double[] grid, Point[] controlPoints, string smoothingCoefficientAlpha, string smoothingCoefficientBeta)
        {
            if (controlPoints != null && grid != null && controlPoints.Length >= 4)
                return new SmoothingSpline(controlPoints, grid, smoothingCoefficientAlpha, smoothingCoefficientBeta);
            else
                return null;
        }

        public ICurve? CreateLinearSpline(Point[] controlPoints)
        {
            if (controlPoints != null)
                return new LinearSpline(controlPoints);
            else
                return null;
        }
    }
}