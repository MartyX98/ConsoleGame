using System.Numerics;
using System;
namespace ConsoleGame
{
    public static class Extensions
    {
        #region Numeric Extensions

        public static int Mod(this int x, int m)
        {
            return (x % m + m) % m;
        }

        public static float Mod(this float x, float m)
        {
            return (x % m + m) % m;
        }

        #endregion

        #region Vector2 Extensions

        public static bool IsVertex(this Vector2 vector, float vertexThreshold)
        {
            var temp = vector.Mod(1);
            return (temp.X < vertexThreshold || temp.X > 1 - vertexThreshold) && (temp.Y < vertexThreshold || temp.Y > 1 - vertexThreshold);
        }

        public static Vector2 Round(this Vector2 vector, int decimalPlaces = 0)
        {
            return new Vector2(
                MathF.Round(vector.X, decimalPlaces), 
                MathF.Round(vector.Y, decimalPlaces)
                );
        }

        public static float Distance(this Vector2 vector, Vector2 other)
        {
            return Vector2.Distance(vector, other);
        }

        public static float AngleTo(this Vector2 vector, Vector2 other) =>
            MathF.Atan2(other.Y - vector.Y, other.X - vector.X);

        public static Vector2 Mod(this Vector2 vector, int scalar)
        {
            if (scalar == 0) throw new DivideByZeroException("Integer modulo by zero.");
            return new Vector2(vector.X % scalar, vector.Y % scalar);
        }

        // Computes the 2D cross product (scalar result) of vectors a and b.
        public static float Cross(this Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

        public static (float x, float y) ToTuple(this Vector2 vector)
        {
            return (vector.X, vector.Y);
        }

        // vector2 + int
        public static Vector2 Add(this Vector2 vector, int scalar)
        {
            return new Vector2(vector.X + scalar, vector.Y + scalar);
        }

        // normal vector from angle
        public static Vector2 CalculateDirectionVector(float angle)
        {
            return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

        public static Vector2 Floor(this Vector2 vector)
        {
            return new Vector2(MathF.Floor(vector.X), MathF.Floor(vector.Y));
        }

        public static Vector2 Ceiling(this Vector2 vector)
        {
            return new Vector2(MathF.Ceiling(vector.X), MathF.Ceiling(vector.Y));
        }

        public static Vector2 Clamp(this Vector2 vector, float minX, float maxX, float minY, float maxY)
        {
            return new Vector2(Math.Clamp(vector.X, minX, maxX), Math.Clamp(vector.Y, minY, maxY));
        }

        public static Vector2 Clamp(this Vector2 vector, Vector2 min, Vector2 max)
        {
            return vector.Clamp(min.X, max.X, min.Y, max.Y);
        }



        #endregion
    }
}
