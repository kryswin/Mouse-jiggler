using System;
using System.Collections.Generic;
using System.Windows;

namespace KSWorks
{
    internal static class PathGenerator
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates a human-like path between two points using a Cubic Bezier Curve
        /// with non-linear (eased) timing for natural acceleration and deceleration.
        /// </summary>
        public static List<Point> GenerateBezierPath(Point start, Point end, int steps)
        {
            var path = new List<Point>();

            // Generate two control points roughly between start and end, with some random perpendicular offset
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;

            // Control points with random jitter to create a natural curve
            Point p1 = new Point(
                start.X + dx * 0.3 + (_random.NextDouble() - 0.5) * dx * 0.5,
                start.Y + dy * 0.3 + (_random.NextDouble() - 0.5) * dy * 0.5
            );

            Point p2 = new Point(
                start.X + dx * 0.7 + (_random.NextDouble() - 0.5) * dx * 0.5,
                start.Y + dy * 0.7 + (_random.NextDouble() - 0.5) * dy * 0.5
            );

            for (int i = 1; i <= steps; i++)
            {
                // Linear time
                double linearT = (double)i / steps;

                // Eased time for acceleration/deceleration
                double t = EaseInOutCubic(linearT);

                // Bezier formula
                double u = 1 - t;
                double tt = t * t;
                double uu = u * u;
                double uuu = uu * u;
                double ttt = tt * t;

                double x = uuu * start.X;
                x += 3 * uu * t * p1.X;
                x += 3 * u * tt * p2.X;
                x += ttt * end.X;

                double y = uuu * start.Y;
                y += 3 * uu * t * p1.Y;
                y += 3 * u * tt * p2.Y;
                y += ttt * end.Y;

                path.Add(new Point(x, y));
            }

            return path;
        }

        /// <summary>
        /// Easing function to simulate human hand inertia (slow start, fast middle, slow end).
        /// </summary>
        private static double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }
    }
}
