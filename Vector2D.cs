using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleGame
{
    public class fVector2D
    {
        public float X { get; set; }
        public float Y { get; set; }

        // Constructors
        public fVector2D(float x = 0, float y = 0)
        {
            X = x;
            Y = y;
        }
        public fVector2D(iVector2D v)
        {
            X = v.X;
            Y = v.Y;
        }

        public fVector2D(fVector2D v)
        {
            X = v.X;
            Y = v.Y;
        }

        public float Magnitude() => (float)Math.Sqrt(X * X + Y * Y);

        public fVector2D Normalize()
        {
            float magnitude = Magnitude();
            return magnitude == 0 ? new fVector2D(0, 0) : new fVector2D(X / magnitude, Y / magnitude);
        }

        public float Dot(fVector2D other) => X * other.X + Y * other.Y;

        // Addition
        public static fVector2D operator +(fVector2D a, fVector2D b) => new fVector2D(a.X + b.X, a.Y + b.Y);

        // Subtraction
        public static fVector2D operator -(fVector2D a, fVector2D b) => new fVector2D(a.X - b.X, a.Y - b.Y);

        // Scalar multiplication
        public static fVector2D operator *(fVector2D a, float scalar) => new fVector2D(a.X * scalar, a.Y * scalar);

        public static fVector2D operator *(float scalar, fVector2D a) => a * scalar;

        // Scalar division
        public static fVector2D operator /(fVector2D a, float scalar)
        {
            if (scalar == 0) throw new DivideByZeroException("Cannot divide by zero.");
            return new fVector2D(a.X / scalar, a.Y / scalar);
        }
        public static fVector2D operator %(fVector2D a, int scalar)
        {
            if (scalar == 0) throw new DivideByZeroException("Integer modulo by zero.");
            return new fVector2D(a.X % scalar, a.Y % scalar);
        }

        // Equality
        public static bool operator ==(fVector2D a, fVector2D b) => a.X == b.X && a.Y == b.Y;

        public static bool operator !=(fVector2D a, fVector2D b) => !(a == b);

        // Override Equals and GetHashCode for proper behavior in collections
        public override bool Equals(object? obj)
        {
            if (obj is fVector2D other)
                return this == other;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(X, Y);

        // ToString override for debugging
        public override string ToString() => $"({X}, {Y})";

        // Implicit conversion to integer vector
        public static implicit operator fVector2D((int x, int y) intVector) => new iVector2D(intVector.x, intVector.y);

        // Implicit conversion to tuple
        public static implicit operator (float x, float y)(fVector2D vector) => (vector.X, vector.Y);

        // Custom
        public bool IsVertex(float vertexThreshold)
        {
            var temp = this % 1;
            return (temp.X < vertexThreshold || temp.X > 1 - vertexThreshold) && (temp.Y < vertexThreshold || temp.Y > 1 - vertexThreshold);
        }
    }
    public class iVector2D
    {
        public int X { get; set; }
        public int Y { get; set; }

        // Constructors
        public iVector2D(int x = 0, int y = 0)
        {
            X = x;
            Y = y;
        }
        public iVector2D(fVector2D v)
        {
            X = (int)v.X;
            Y = (int)v.Y;
        }

        public iVector2D(iVector2D v)
        {
            X = v.X;
            Y = v.Y;
        }

        // Magnitude (as float for precision)
        public float Magnitude() => (float)Math.Sqrt(X * X + Y * Y);

        // Normalize (returns a float-based vector)
        public fVector2D Normalize()
        {
            float magnitude = Magnitude();
            return magnitude == 0 ? new fVector2D(0, 0) : new fVector2D(X / magnitude, Y / magnitude);
        }

        // Dot product
        public int Dot(iVector2D other) => X * other.X + Y * other.Y;

        // Addition
        public static iVector2D operator +(iVector2D a, iVector2D b) => new iVector2D(a.X + b.X, a.Y + b.Y);

        // Subtraction
        public static iVector2D operator -(iVector2D a, iVector2D b) => new iVector2D(a.X - b.X, a.Y - b.Y);

        // Scalar multiplication
        public static iVector2D operator *(iVector2D a, int scalar) => new iVector2D(a.X * scalar, a.Y * scalar);

        public static iVector2D operator *(int scalar, iVector2D a) => a * scalar;

        // Scalar division
        public static iVector2D operator /(iVector2D a, int scalar)
        {
            if (scalar == 0) throw new DivideByZeroException("Cannot divide by zero.");
            return new iVector2D(a.X / scalar, a.Y / scalar);
        }

        // Equality
        public static bool operator ==(iVector2D a, iVector2D b) => a.X == b.X && a.Y == b.Y;

        public static bool operator !=(iVector2D a, iVector2D b) => !(a == b);

        // Override Equals and GetHashCode for proper behavior in collections
        public override bool Equals(object? obj)
        {
            if (obj is iVector2D other)
                return this == other;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(X, Y);

        // ToString override for debugging
        public override string ToString() => $"({X}, {Y})";

        // Implicit conversion to float vector
        public static implicit operator fVector2D(iVector2D intVector) => new fVector2D(intVector.X, intVector.Y);

        // Implicit conversion to tuple
        public static implicit operator (int x, int y)(iVector2D vector) => (vector.X, vector.Y);
    }

    // Not used - unecessary performance overhead at runtime
    public class Vector2D<T> where T : struct
    {
        public T X { get; set; }
        public T Y { get; set; }

        // Constructors
        public Vector2D(T x = default, T y = default)
        {
            X = x;
            Y = y;
        }

        // Magnitude (only for numeric types, requires explicit handling)
        public double Magnitude()
        {
            dynamic x = X;
            dynamic y = Y;
            return Math.Sqrt((double)(x * x + y * y));
        }

        // Normalize (returns a double-based vector)
        public Vector2D<double> Normalize()
        {
            double magnitude = Magnitude();
            dynamic x = X;
            dynamic y = Y;
            return magnitude == 0 ? new Vector2D<double>(0, 0) : new Vector2D<double>(x / magnitude, y / magnitude);
        }

        // Addition
        public static Vector2D<T> operator +(Vector2D<T> a, Vector2D<T> b)
        {
            dynamic x1 = a.X;
            dynamic y1 = a.Y;
            dynamic x2 = b.X;
            dynamic y2 = b.Y;
            return new Vector2D<T>((T)(x1 + x2), (T)(y1 + y2));
        }

        // Subtraction
        public static Vector2D<T> operator -(Vector2D<T> a, Vector2D<T> b)
        {
            dynamic x1 = a.X;
            dynamic y1 = a.Y;
            dynamic x2 = b.X;
            dynamic y2 = b.Y;
            return new Vector2D<T>((T)(x1 - x2), (T)(y1 - y2));
        }

        // Scalar multiplication
        public static Vector2D<T> operator *(Vector2D<T> a, T scalar)
        {
            dynamic x = a.X;
            dynamic y = a.Y;
            dynamic s = scalar;
            return new Vector2D<T>((T)(x * s), (T)(y * s));
        }

        public static Vector2D<T> operator *(T scalar, Vector2D<T> a) => a * scalar;

        // Scalar division
        public static Vector2D<T> operator /(Vector2D<T> a, T scalar)
        {
            if (EqualityComparer<T>.Default.Equals(scalar, default))
                throw new DivideByZeroException("Cannot divide by zero.");
            dynamic x = a.X;
            dynamic y = a.Y;
            dynamic s = scalar;
            return new Vector2D<T>((T)(x / s), (T)(y / s));
        }

        // Equality
        public static bool operator ==(Vector2D<T> a, Vector2D<T> b)
        {
            return EqualityComparer<T>.Default.Equals(a.X, b.X) && EqualityComparer<T>.Default.Equals(a.Y, b.Y);
        }

        public static bool operator !=(Vector2D<T> a, Vector2D<T> b) => !(a == b);

        // Override Equals and GetHashCode for proper behavior in collections
        public override bool Equals(object? obj)
        {
            if (obj is Vector2D<T> other)
                return this == other;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(X, Y);

        // ToString override for debugging
        public override string ToString() => $"({X}, {Y})";
    }
}
