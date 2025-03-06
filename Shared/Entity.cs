using System;
using System.Numerics;

namespace ConsoleGame
{
    public class Entity(
        float x = 0,
        float y = 0,
        float angle = 0,
        float walkSpeed = 1f,
        float rotationSpeed = 1f,
        float fov = MathF.PI / 2,
        int viewDistance = 16
        )
    {
        private Vector2 _position = new(x, y);

        public float X
        {
            get => _position.X;
            set => _position.X = value;
        }

        public float Y
        {
            get => _position.Y;
            set => _position.Y = value;
        }

        public float Angle { get; set; } = angle;
        public float FOV { get; set; } = fov;
        public float ViewDistance { get; set; } = viewDistance;
        public float WalkSpeed { get; set; } = walkSpeed;
        public float RotationSpeed { get; set; } = rotationSpeed;

        public void Rotate(float deltaAngle)
        {
            Angle = (float)((Angle + deltaAngle) % (Math.PI * 2));
        }

        public void RotateLeft(float fElapsedTime)
        {
            Rotate(-RotationSpeed * fElapsedTime);
        }

        public void RotateRight(float fElapsedTime)
        {
            Rotate(RotationSpeed * fElapsedTime);
        }

        public void Walk(float fElapsedTime, float speedMult = 1f, float angleIncr = 0f)
        {
            float angleAdjusted = Angle + angleIncr;
            float speedAdjusted = WalkSpeed * fElapsedTime * speedMult;
            _position.X += MathF.Cos(angleAdjusted) * speedAdjusted;
            _position.Y += MathF.Sin(angleAdjusted) * speedAdjusted;
        }

        public float Magnitude() => _position.Length();

        public Vector2 Normalize() => Vector2.Normalize(_position);

        public float Dot(Vector2 other) => Vector2.Dot(_position, other);

        public float AngleTo(Vector2 other) => _position.AngleTo(other);

        public override string ToString() => _position.ToString();

        // Implicit conversion to Vector2
        public static implicit operator Vector2(Entity entity) => entity._position;

        // Operator overloads
        public static Entity operator +(Entity entity, Vector2 vector)
        {
            return new Entity(entity.X + vector.X, entity.Y + vector.Y, entity.Angle, entity.WalkSpeed, entity.RotationSpeed, entity.FOV, (int)entity.ViewDistance);
        }

        public static Entity operator -(Entity entity, Vector2 vector)
        {
            return new Entity(entity.X - vector.X, entity.Y - vector.Y, entity.Angle, entity.WalkSpeed, entity.RotationSpeed, entity.FOV, (int)entity.ViewDistance);
        }

        public static Entity operator *(Entity entity, float scalar)
        {
            return new Entity(entity.X * scalar, entity.Y * scalar, entity.Angle, entity.WalkSpeed, entity.RotationSpeed, entity.FOV, (int)entity.ViewDistance);
        }

        public static Entity operator /(Entity entity, float scalar)
        {
            if (scalar == 0) throw new DivideByZeroException("Cannot divide by zero.");
            return new Entity(entity.X / scalar, entity.Y / scalar, entity.Angle, entity.WalkSpeed, entity.RotationSpeed, entity.FOV, (int)entity.ViewDistance);
        }

        public static bool operator ==(Entity a, Entity b)
        {
            return a._position == b._position && a.Angle == b.Angle && a.FOV == b.FOV && a.ViewDistance == b.ViewDistance && a.WalkSpeed == b.WalkSpeed && a.RotationSpeed == b.RotationSpeed;
        }

        public static bool operator !=(Entity a, Entity b) => !(a == b);

        public override bool Equals(object? obj)
        {
            if (obj is Entity other)
                return this == other;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(_position, Angle, FOV, ViewDistance, WalkSpeed, RotationSpeed);
    }
}
