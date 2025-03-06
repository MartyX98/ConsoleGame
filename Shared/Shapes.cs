using System;
using System.Numerics;
using System.Collections.Generic;
using static ConsoleGame.Raycaster;
namespace ConsoleGame
{
    /// <summary>
    /// Represents a 2D shape that can be raycasted against.
    /// </summary>
    public abstract class Shape(string name = "Shape")
    {
        public string Name = name;
        public abstract bool CheckIntersection(Vector2 origin, Vector2 direction, out float distance);
        public abstract IEnumerable<Vector2> GetIntersectedTiles();
        public bool CheckIntersection(Vector2 origin, float angle, out float distance) => 
            CheckIntersection(origin, new Vector2(MathF.Cos(angle), MathF.Sin(angle)), out distance);
    }

    /// <summary>
    /// Represents a line segment in 2D space.
    /// </summary>
    /// <param name="a"> The first endpoint of the segment.</param>
    /// <param name="b"> The second endpoint of the segment.</param>
    public class LineSegment(Vector2 a, Vector2 b, string name = "LineSegment") : Shape(name)
    {
        public Vector2 A = a;
        public Vector2 B = b;

        public override bool CheckIntersection(Vector2 origin, Vector2 direction, out float distance)
        {
            distance = 0;
            // The segment's vector.
            Vector2 r = B - A;

            // Calculate the denominator (using the 2D cross product).
            float denom = direction.Cross(r);

            // If the denominator is near zero, the ray and the segment are parallel (or collinear).
            if (Math.Abs(denom) < 1e-6f)
                return false;

            // Vector from segment's start (a) to the ray's origin.
            Vector2 fAtoOrigin = origin - A;

            // Solve for parameters t and u using cross products.
            distance = fAtoOrigin.Cross(r) / -denom;
            float u = fAtoOrigin.Cross(direction) / -denom;

            // distanceTraveled must be >= 0 for the ray (forward direction)
            // u must be between 0 and 1 for the intersection to lie on the segment.
            return distance >= 0 && u >= 0 && u <= 1;
        }

        public override IEnumerable<Vector2> GetIntersectedTiles()
        {
            foreach (RaycastStep step in CastRayFromTo(A, B))
                yield return step.Tile;
        }
    }

    /// <summary>
    /// Represents a polygon in 2D space.
    /// </summary>
    /// <param name="segments"> The line segments that make up the polygon.</param>
    public class Polygon : Shape
    {
        public LineSegment[] Segments;

        public Polygon(LineSegment[] segments, string name = "Polygon") : base(name)
        {
            Segments = segments;
        }
        public Polygon(Vector2[] vertices)
        {
            Segments = new LineSegment[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Segments[i] = new LineSegment(vertices[i], vertices[(i + 1) % vertices.Length]);
            }
        }
        public Polygon(Vector2 center, int numOfSides, double radius, string name = "Polygon") : base(name)
        {
            Segments = new LineSegment[numOfSides];
            float angleStep = (float)(Math.PI * 2) / numOfSides;
            for (int i = 0; i < numOfSides; i++)
            {
                float angle = angleStep * i;
                Vector2 a = center + new Vector2((float)(Math.Cos(angle) * radius), (float)(Math.Sin(angle) * radius));
                Vector2 b = center + new Vector2((float)(Math.Cos(angle + angleStep) * radius), (float)(Math.Sin(angle + angleStep) * radius));
                Segments[i] = new LineSegment(a, b);
            }
        }

        public override bool CheckIntersection(Vector2 origin, Vector2 direction, out float distance)
        {
            distance = float.MaxValue;
            foreach (LineSegment segment in Segments)
            {
                if (segment.CheckIntersection(origin, direction, out float t))
                {
                    if (t < distance)
                    {
                        distance = t;
                    }
                }
            }
            return distance < float.MaxValue;
        }

        public override IEnumerable<Vector2> GetIntersectedTiles()
        {
            foreach (LineSegment segment in Segments)
            {
                foreach (Vector2 tile in segment.GetIntersectedTiles())
                    yield return tile;
            }
        }
    }

    /// <summary>
    /// Represents a circle in 2D space.
    /// </summary>
    /// <param name="center"> The center of the circle.</param>
    /// <param name="radius"> The radius of the circle.</param>
    public class Circle(Vector2 center, float radius, string name = "Circle") : Shape(name)
    {
        public Vector2 Center = center;
        public float Radius = radius;
        public override bool CheckIntersection(Vector2 origin, Vector2 direction, out float distance)
        {
            distance = 0;
            Vector2 f = Center - origin;
            float t = Vector2.Dot(f, direction);
            float dSquared = Vector2.Dot(f, f) - t * t;
            if (dSquared > Radius * Radius)
                return false;
            float thc = MathF.Sqrt(Radius * Radius - dSquared);
            float t0 = t - thc;
            float t1 = t + thc;
            if (t0 < 0 && t1 < 0)
                return false;
            distance = t0 < 0 ? t1 : t0;
            return true;
        }

        public override IEnumerable<Vector2> GetIntersectedTiles()
        {
            for (float x = MathF.Floor(Center.X - Radius); x <= Math.Ceiling(Center.X + Radius); x++)
            for (float y = MathF.Floor(Center.Y - Radius); y <= Math.Ceiling(Center.Y + Radius); y++)
            {
                Vector2 tile = new(x, y);
                Vector2 closestPoint = Center.Clamp(tile, tile.Add(1));
                Vector2 farthestPoint = (tile + (tile + new Vector2(1, 1) - closestPoint)).Round();
                if (closestPoint.Distance(Center) <= Radius && farthestPoint.Distance(Center) >= Radius)
                    yield return tile;
                }
        }
    }
}