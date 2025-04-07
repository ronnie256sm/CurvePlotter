using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using splines_avalonia;
using System.Linq;
using Avalonia.Controls.Shapes;

namespace splines_avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<ISpline> SplineList { get; set; }
        public Canvas GraphicCanvas { get; set; }

        public MainWindowViewModel()
        {
            SplineList = new ObservableCollection<ISpline>();
        }

        public double CenterX()
        {
            return (GraphicCanvas.Bounds.Width / 2);
        }

        public double CenterY()
        {
            return (GraphicCanvas.Bounds.Height / 2);
        }

        public double Zoom() => 100;

        public void AddSpline(string type, Point[] points, double[] grid)
        {
            var _splineLogic = new SplineLogic();
            var spline = _splineLogic.Create(type, grid, points);
            SplineList.Add(spline);
            DrawSplines();
        }

        public void DrawSplines()
        {
            if (GraphicCanvas == null)
                return;

            GraphicCanvas.Children.Clear();

            foreach (var spline in SplineList)
            {
                var points = spline.OutputPoints;

                foreach(var point in points)
                {
                    
                    point.X *= Zoom();
                    point.Y *= -Zoom();
                    point.X += CenterX();
                    point.Y += CenterY();
                }

                for (int i = 0; i < points.Length - 1; i++)
                {
                    var line = new Line
                    {
                        StartPoint = new Avalonia.Point(points[i].X, points[i].Y),
                        EndPoint = new Avalonia.Point(points[i + 1].X, points[i + 1].Y),
                        Stroke = Brushes.Black,
                        StrokeThickness = 2
                    };
                    GraphicCanvas.Children.Add(line);
                }
            }
        }
    }
}
