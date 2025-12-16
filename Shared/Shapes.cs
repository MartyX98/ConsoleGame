using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Shared.Raycaster;

namespace Shared
{
    /// <summary>
    /// Represents a 2D shape that can be raycasted against.
    /// </summary>
    public abstract class Shape(string name = "Shape")
    {
        public string Name = name;
        public string TextureName = "";
        public TextureMappingType TextureMapping = TextureMappingType.Invisible;
        public bool IsSolid = true;
        public abstract float Length { get; }
        /// <summary>
        /// Checks if a ray intersects with the shape.
        /// </summary>
        /// <param name="origin"> The origin of the ray.</param>
        /// <param name="direction"> The direction of the ray.</param>
        /// <param name="rayDistance"> The distance from the origin to the intersection point.</param>
        /// <param name="normalizedPosition"> The normalized position of the intersection point along the shape.</param>
        /// <returns> True if the ray intersects with the shape, false otherwise.</returns>
        public abstract bool CheckIntersection(Vector2 origin, Vector2 direction, out float rayDistance, out float normalizedPosition);
        /// <summary>
        /// Returns the tiles that are intersected by the shape.
        /// </summary>
        /// <returns> The intersected tiles.</returns>
        public abstract IEnumerable<Vector2> GetIntersectedTiles();
        /// <summary>
        /// Checks if a ray intersects with the shape.
        /// </summary>
        /// <param name="origin"> The origin of the ray.</param>
        /// <param name="angle"> The angle of the ray.</param>
        /// <param name="rayDistance"> The distance from the origin to the intersection point.</param>
        /// <param name="normalizedPosition"> The normalized position of the intersection point along the shape.</param>
        /// <returns> True if the ray intersects with the shape, false otherwise.</returns>
        public bool CheckIntersection(Vector2 origin, float angle, out float rayDistance, out float normalizedPosition) => 
            CheckIntersection(origin, new Vector2(MathF.Cos(angle), MathF.Sin(angle)), out rayDistance, out normalizedPosition);
    }

    /// <summary>
    /// Represents the type of texture mapping for a shape.
    /// </summary>
    public enum TextureMappingType
    {
        /// <summary>
        /// No texture mapping.
        /// </summary>
        Invisible,
        /// <summary>
        /// Stretch the texture to fit the shape horizontally.
        /// </summary>
        Stretch,
        /// <summary>
        /// Repeat the texture horizontally from the left.
        /// </summary>
        Repeat_FromLeft,
        /// <summary>
        /// Repeat the texture horizontally from the right.
        /// </summary>
        Repeat_FromRight,
        /// <summary>
        /// Repeat the texture horizontally centered.
        /// </summary>
        Repeat_Centered,
    }

    #region Elementary shapes

    public class Point(Vector2 position, string name = "Point") : Shape(name)
    {
        public Vector2 Position = position;
        public override float Length => 0;
        public override bool CheckIntersection(Vector2 origin, Vector2 direction, out float rayDistance, out float normalizedPosition)
        {
            rayDistance = 0;
            normalizedPosition = 0;
            return Position == origin;
        }
        public override IEnumerable<Vector2> GetIntersectedTiles()
        {
            yield return new Vector2((int)Position.X, (int)Position.Y);
        }
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
        public override float Length => A.Distance(B);

        public override bool CheckIntersection(Vector2 origin, Vector2 direction, out float rayDistance, out float normalizedPosition)
        {
            rayDistance = 0;
            normalizedPosition = 0;
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
            rayDistance = fAtoOrigin.Cross(r) / -denom;
            normalizedPosition = fAtoOrigin.Cross(direction) / -denom;

            bool isHit = rayDistance >= 0 && normalizedPosition is >= 0 and <= 1;
            if (isHit) normalizedPosition = 1 - normalizedPosition;
            // distanceTraveled must be >= 0 for the ray (forward direction)
            // u must be between 0 and 1 for the intersection to lie on the segment.
            return isHit;
        }

        public override IEnumerable<Vector2> GetIntersectedTiles()
        {
            foreach (RayIntersection step in CastRayFromTo(A, B))
                yield return step.TilePosition;
        }
    }

    #endregion

    #region Polygon shapes

    /// <summary>
    /// Represents a polygon in 2D space.
    /// </summary>
    /// <param name="segments"> The line segments that make up the polygon.</param>
    public class Polygon : Shape
    {
        public LineSegment[] Segments;
        public override float Length => Segments.Sum(s => s.Length);

        public Polygon(LineSegment[] segments, string name = "Polygon") : base(name)
        {
            Segments = segments;
        }
        public Polygon(Vector2[] vertices, string name = "Polygon") : base(name)
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

        public override bool CheckIntersection(Vector2 origin, Vector2 direction, out float rayDistance, out float normalizedPosition)
        {
            rayDistance = float.MaxValue;
            normalizedPosition = 0;
            foreach (LineSegment segment in Segments)
            {
                if (segment.CheckIntersection(origin, direction, out float rd, out float np))
                {
                    if (rd < rayDistance)
                    {
                        rayDistance = rd;
                        normalizedPosition = np;
                    }
                }
            }
            return rayDistance < float.MaxValue;
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

    public class Square : Polygon
    {
        public Square(Vector2 center, float size, string name = "Square") : base(center, 4, size / 2, name) { }
        public Square(Vector2 min, Vector2 max, string name = "Square") : base([min, new(max.X, min.Y), max, new(min.X, max.Y)], name) { }
    }

    #endregion

    #region Curve based shapes

    /// <summary>
    /// Represents a circle in 2D space.
    /// </summary>
    /// <param name="center"> The center of the circle.</param>
    /// <param name="radius"> The radius of the circle.</param>
    public class Circle(Vector2 center, float radius, string name = "Circle") : Shape(name)
    {
        public Vector2 Center = center;
        public float Radius = radius;
        public override float Length => MathF.PI * 2 * Radius;
        public override bool CheckIntersection(Vector2 origin, Vector2 direction, out float rayDistance, out float normalizedPosition)
        {
            rayDistance = 0;
            normalizedPosition = 0;

            // Compute the vector from the ray's origin to the circle's center.
            Vector2 toCircleCenter = Center - origin;

            // Project this vector onto the ray's direction.
            float centerProjection = Vector2.Dot(toCircleCenter, direction);

            // Calculate the squared perpendicular distance from the circle center to the ray.
            float perpendicularDistanceSquared = Vector2.Dot(toCircleCenter, toCircleCenter) - centerProjection * centerProjection;

            // If the ray is too far from the circle, there's no intersection.
            if (perpendicularDistanceSquared > Radius * Radius)
                return false;

            // Calculate the half-chord length: the distance from the closest approach to the actual intersection points.
            float halfChordLength = MathF.Sqrt(Radius * Radius - perpendicularDistanceSquared);

            // Determine the entry and exit points along the ray.
            float entryPoint = centerProjection - halfChordLength;
            float exitPoint = centerProjection + halfChordLength;

            // If both intersection points are behind the ray's origin, the ray does not hit the circle.
            if (entryPoint < 0 && exitPoint < 0)
                return false;

            // Use the closest valid intersection point.
            rayDistance = entryPoint < 0 ? exitPoint : entryPoint;

            // Calculate the actual intersection point on the circle.
            Vector2 intersectionPoint = origin + rayDistance * direction;

            // Determine the vector from the circle's center to the intersection point.
            Vector2 hitOffset = intersectionPoint - Center;

            // Calculate the angle (in radians) relative to the positive x-axis.
            float angle = MathF.Atan2(hitOffset.Y, hitOffset.X);
            if (angle < 0)
                angle += MathF.PI * 2;

            // Normalize the angle to a value between 0 and 1.
            normalizedPosition = 1 - (angle / (MathF.PI * 2));

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

    #endregion

}