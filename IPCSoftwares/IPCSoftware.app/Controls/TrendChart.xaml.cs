using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IPCSoftware.App.Controls
{
    public partial class TrendChart : UserControl
    {
        public TrendChart()
        {
            InitializeComponent();

            Loaded += (_, __) => Redraw();
            SizeChanged += (_, __) => Redraw();

            PlotCanvas.MouseMove += PlotCanvas_MouseMove;
            PlotCanvas.MouseLeave += (_, __) => TooltipBox.Visibility = Visibility.Collapsed;

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Values = new double[] { 3, 5, 4, 6, 3, 7, 6 };
                Title = "Cycle Time Trend";
                XAxisText = "Last 7 Days";
                YAxisText = "Seconds";
                Redraw();
            }
        }

        // ===================== Dependency Properties =====================

        public IEnumerable<double> Values
        {
            get => (IEnumerable<double>)GetValue(ValuesProperty);
            set => SetValue(ValuesProperty, value);
        }
        public static readonly DependencyProperty ValuesProperty =
            DependencyProperty.Register(nameof(Values), typeof(IEnumerable<double>),
            typeof(TrendChart), new PropertyMetadata(null, OnValuesChanged));

        private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((TrendChart)d).Redraw();

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string),
            typeof(TrendChart), new PropertyMetadata("Trend Chart"));

        public string XAxisText
        {
            get => (string)GetValue(XAxisTextProperty);
            set => SetValue(XAxisTextProperty, value);
        }
        public static readonly DependencyProperty XAxisTextProperty =
            DependencyProperty.Register(nameof(XAxisText), typeof(string),
            typeof(TrendChart), new PropertyMetadata("X Axis"));

        public string YAxisText
        {
            get => (string)GetValue(YAxisTextProperty);
            set => SetValue(YAxisTextProperty, value);
        }
        public static readonly DependencyProperty YAxisTextProperty =
            DependencyProperty.Register(nameof(YAxisText), typeof(string),
            typeof(TrendChart), new PropertyMetadata("Y Axis"));

        // ===================== Drawing Engine =====================

        private void Redraw()
        {
            PlotCanvas.Children.Clear();
            if (Values == null || !Values.Any()) return;

            double w = PlotCanvas.ActualWidth;
            double h = PlotCanvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            var list = Values.ToList();
            double min = list.Min();
            double max = list.Max();
            double range = max - min;
            if (range < 0.001) range = 1;

            int count = list.Count;
            if (count < 2) return;

            var points = new List<Point>();
            for (int i = 0; i < count; i++)
            {
                double x = w * i / (count - 1);
                double norm = (list[i] - min) / range;
                double y = h - norm * h;
                points.Add(new Point(x, y));
            }

            DrawGridLines(w, h);
            DrawSmoothSpline(points);
        }

        private void DrawGridLines(double w, double h)
        {
            int lines = 4;
            for (int i = 1; i <= lines; i++)
            {
                double y = h * i / (lines + 1);
                PlotCanvas.Children.Add(new Line
                {
                    X1 = 0,
                    X2 = w,
                    Y1 = y,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 3 }
                });
            }
        }

        private void DrawSmoothSpline(List<Point> points)
        {
            var geom = new PathGeometry();
            var figure = new PathFigure { StartPoint = points[0] };

            for (int i = 1; i < points.Count; i++)
            {
                double midX = (points[i - 1].X + points[i].X) / 2;
                var seg = new QuadraticBezierSegment(points[i - 1], new Point(midX, points[i - 1].Y), true);
                figure.Segments.Add(seg);
            }

            geom.Figures.Add(figure);

            PlotCanvas.Children.Add(new Path
            {
                Stroke = Brushes.LimeGreen,
                StrokeThickness = 2,
                Data = geom
            });
        }

        // ===================== Tooltip Logic =====================
        private void PlotCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (Values == null || !Values.Any()) return;

            var pos = e.GetPosition(PlotCanvas);

            double w = PlotCanvas.ActualWidth;
            var list = Values.ToList();
            int idx = (int)Math.Round((list.Count - 1) * pos.X / w);
            idx = Math.Max(0, Math.Min(idx, list.Count - 1));

            TooltipText.Text = $"Value: {list[idx]}";
            TooltipBox.Visibility = Visibility.Visible;

            Canvas.SetLeft(TooltipBox, pos.X + 10);
            Canvas.SetTop(TooltipBox, pos.Y - 30);
        }
    }
}
