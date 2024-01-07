using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Common.DataStructures;

namespace NoxusBoss.Common.Utilities
{
    public static class EarClippingTriangulation
    {
        public static List<Triangle> GenerateMesh(List<Vector2> points)
        {
            List<Triangle> triangles = new();

            // Sort the points clockwise or counterclockwise based on their position.
            List<Vector2> sortedPoints = SortPoints(points);

            // Triangulate the sorted points using the ear clipping algorithm.
            while (sortedPoints.Count >= 3)
            {
                // Find an ear and create a triangle.
                int index = FindEar(sortedPoints);
                if (index == -1)
                {
                    // No ear found, break the loop.
                    break;
                }

                Vector2 p1 = sortedPoints[index];
                Vector2 p2 = sortedPoints[(index + 1) % sortedPoints.Count];
                Vector2 p3 = sortedPoints[(index + 2) % sortedPoints.Count];

                triangles.Add(new Triangle(p1, p2, p3));

                // Remove the ear point from the sorted points list.
                sortedPoints.RemoveAt((index + 1) % sortedPoints.Count);
            }

            return triangles;
        }

        private static List<Vector2> SortPoints(List<Vector2> points)
        {
            // Sort the points based on their angle with the centroid.
            Vector2 centroid = GetCentroid(points);
            points.Sort((p1, p2) =>
            {
                float angle1 = Atan2(p1.Y - centroid.Y, p1.X - centroid.X);
                float angle2 = Atan2(p2.Y - centroid.Y, p2.X - centroid.X);
                return angle1.CompareTo(angle2);
            });

            return points;
        }

        private static Vector2 GetCentroid(List<Vector2> points)
        {
            float totalX = 0;
            float totalY = 0;

            foreach (Vector2 point in points)
            {
                totalX += point.X;
                totalY += point.Y;
            }

            float centerX = totalX / points.Count;
            float centerY = totalY / points.Count;

            return new Vector2(centerX, centerY);
        }

        private static int FindEar(List<Vector2> points)
        {
            int count = points.Count;

            for (int i = 0; i < count; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[(i + 1) % count];
                Vector2 p3 = points[(i + 2) % count];

                // Return the middle point index.
                if (IsEar(p1, p2, p3, points))
                    return (i + 1) % count;
            }

            return -1;
        }

        private static bool IsEar(Vector2 p1, Vector2 p2, Vector2 p3, List<Vector2> points)
        {
            // Check if the triangle is an ear.
            bool isEar = true;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];

                // Skip the triangle vertices.
                if (point.Equals(p1) || point.Equals(p2) || point.Equals(p3))
                    continue;

                // Check if the point lies inside the triangle.
                if (IsPointInTriangle(point, p1, p2, p3))
                {
                    isEar = false;
                    break;
                }
            }

            return isEar;
        }

        private static bool IsPointInTriangle(Vector2 point, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float d1 = Sign(point, p1, p2);
            float d2 = Sign(point, p2, p3);
            float d3 = Sign(point, p3, p1);

            bool hasNegative = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPositive = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNegative && hasPositive);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }
    }
}
