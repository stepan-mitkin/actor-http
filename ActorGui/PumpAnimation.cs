using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace ActorGui
{
    public class PumpAnimation : Control
    {
        private class Cinematics
        {
            public double BeamAngle;

            public double ConrodEndX;
            public double ConrodEndY;

            public double BeamEndX;
            public double BeamEndY;

            public static Cinematics Calculate(
                double motorAngle,
                double beamX,
                double beamY,
                double beamLength,
                double conrodX,
                double conrodY,
                double conrodLength,
                double rodLength)
            {
                Cinematics result = new Cinematics();
                result.ConrodEndX = conrodX + Math.Cos(motorAngle) * conrodLength;
                result.ConrodEndY = conrodY - Math.Sin(motorAngle) * conrodLength;

                double cx1 = result.ConrodEndX;
                double cy1 = result.ConrodEndY;

                double cx2 = beamX;
                double cy2 = beamY;

                double l1 = rodLength;
                double l2 = beamLength;

                double dx = cx2 - cx1;
                double dy = cy2 - cy1;
                double d = Math.Sqrt(dx * dx + dy * dy);

                double beta = CalculateAngleInTriangle(l2, l1, d);
                double alfa = Math.Acos(dx / d);

                double gamma = alfa + beta;

                result.BeamEndX = cx1 + Math.Cos(gamma) * l1;
                result.BeamEndY = cy1 - Math.Sin(gamma) * l1;

                double bDy = result.BeamEndY - cy2;

                double sinBeanAngle = bDy / l2;
                result.BeamAngle = Math.Asin(sinBeanAngle);

                return result;
            }


        }

        private readonly DoubleAnimation _animation;
        private double _angleRadians = 0;

        public double Angle
        {
            get { return (double)this.GetValue(AngleProperty); }
            set
            {
                this.SetValue(AngleProperty, value);
            }
        }

        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
            "Angle", typeof(double), typeof(PumpAnimation), new PropertyMetadata(0.0));


        public PumpAnimation()
        {
            Width = 300;
            Height = 300;

            _animation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(4)));
            _animation.AutoReverse = false;
            _animation.RepeatBehavior = RepeatBehavior.Forever;

        }


        public void Start()
        {
            BeginAnimation(PumpAnimation.AngleProperty, _animation);
        }

        public void Stop()
        {
            BeginAnimation(PumpAnimation.AngleProperty, null);
            
        }

        private static double CalculateAngleInTriangle(double a, double b, double c)
        {
            double cosA = (b * b + c * c - a * a) / (2.0 * b * c);
            return Math.Acos(cosA);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == PumpAnimation.AngleProperty)
            {
                double value = (double)e.NewValue;
                _angleRadians = ToRadians(value);
                InvalidateVisual();
            }
        }

        private static double ToRadians(double value)
        {
            double norm = value;
            if (norm < 0)
            {
                norm = 0;
            }
            else if (norm > 1)
            {
                norm = 1;
            }

            return norm * Math.PI * 2;
        }

        protected override void OnRender(DrawingContext context)
        {
            SolidColorBrush fore = new SolidColorBrush(Colors.Maroon);

            Cinematics state = Cinematics.Calculate(_angleRadians,
                150, 130, 100,
                55, 220, 40,
                90);
            DrawStationaryParts(context, fore);
            DrawСonrod(context, fore, 55, 220, RadiansToDegrees(_angleRadians));
            DrawBeam(context, fore, 150, 130, RadiansToDegrees(state.BeamAngle));

            context.DrawLine(
                new Pen(fore, 3),
                new Point(state.BeamEndX, state.BeamEndY),
                new Point(state.ConrodEndX, state.ConrodEndY));

            //DrawAxis(context, 150, 130);
            //DrawAxis(context, 55, 250);
            //DrawAxis(context, state.ConrodEndX, state.ConrodEndY);
            //DrawAxis(context, state.BeamEndX, state.BeamEndY);
        }

        private void DrawBeam(DrawingContext context, SolidColorBrush fore, int x, int y, double angle)
        {
            Geometry beam = CreatePolygon(
                new Point(x - 105, y + 5),
                new Point(x - 105, y - 5),
                new Point(x + 110, y - 5),
                new Point(x + 110, y - 60),
                new Point(x + 120, y - 60),
                new Point(x + 135, y - 25),
                new Point(x + 135, y + 25),
                new Point(x + 120, y + 60),
                new Point(x + 110, y + 60),
                new Point(x + 110, y + 5),
                new Point(x, y + 5));

            TransformGroup transforms = Rotate(x, y, angle);
            beam.Transform = transforms;

            context.DrawGeometry(fore, null, beam);
        }

        private static double RadiansToDegrees(double value)
        {
            return value / Math.PI * 180;
        }

        private void DrawСonrod(DrawingContext context, SolidColorBrush fore, double x, double y, double angle)
        {
            Geometry conrod = CreatePolygon(
                new Point(x - 20, y + 5),
                new Point(x - 20, y - 5),
                new Point(x, y - 5),
                new Point(x + 20, y - 20),
                new Point(x + 50, y - 20),
                new Point(x + 50, y + 20),
                new Point(x + 20, y + 20),
                new Point(x, y + 5));

            TransformGroup transforms = Rotate(x, y, angle);
            conrod.Transform = transforms;

            context.DrawGeometry(fore, null, conrod);
        }

        private static TransformGroup Rotate(double x, double y, double angle)
        {
            TransformGroup transforms = new TransformGroup();
            transforms.Children.Add(new TranslateTransform(-x, -y));
            transforms.Children.Add(new RotateTransform(-angle));
            transforms.Children.Add(new TranslateTransform(x, y));
            return transforms;
        }

        private void DrawStationaryParts(DrawingContext context, SolidColorBrush fore)
        {
            //context.DrawRectangle(new SolidColorBrush(C1), null, new Rect(0, 0, Width, Height));
            Geometry basis = CreatePolygon(
                new Point(100, 290),
                new Point(150, 120),
                new Point(200, 290),
                new Point(190, 290),
                new Point(150, 150),
                new Point(110, 290)
                );
            context.DrawGeometry(fore, null, basis);

            Geometry motor = CreatePolygon(
                new Point(40, 290),
                new Point(50, 220),
                new Point(60, 220),
                new Point(70, 290)
                );
            context.DrawGeometry(fore, null, motor);
        }

        private static void DrawAxis(DrawingContext context, double x, double y)
        {
            Pen red = new Pen(new SolidColorBrush(Colors.Red), 1);
            Pen blue = new Pen(new SolidColorBrush(Colors.Blue), 1);
            double size = 20;
            double left = x - size;
            double right = x + size;
            double top = y - size;
            double bottom = y + size;
            context.DrawLine(red, new Point(left, y), new Point(right, y));
            context.DrawLine(blue, new Point(x, top), new Point(x, bottom));
        }

        private static Geometry CreatePolygon(params Point[] points)
        {
            if (points.Length < 3)
            {
                throw new ArgumentException("At least 3 points expected.");
            }
            PathFigure figure = new PathFigure();
            figure.StartPoint = points[0];
            figure.Segments = new PathSegmentCollection();
            for (int i = 1; i < points.Length; i++)
            {
                Point p = points[i];
                figure.Segments.Add(new LineSegment { Point = p });
            }
            PathGeometry geom = new PathGeometry();
            geom.Figures.Add(figure);
            return geom;
        }

    }
}
