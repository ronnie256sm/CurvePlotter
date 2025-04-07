using System.IO;

namespace splines_avalonia
{
    public static class FileWriter
    {
        public static void WriteSplineOutput(string filename, Point[] points)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                foreach (var point in points)
                {
                    writer.WriteLine($"{point.X:F6} {point.Y:F6}");
                }
            }
        }
    }
}
