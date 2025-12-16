using System;
using System.Numerics;

namespace Shared
{
    public abstract class Entity
    {
        public Vector2 Position;
        private float _angle;
        public float X
        {
            get => Position.X;
            set => Position.X = value;
        }
        public float Y
        {
            get => Position.Y;
            set => Position.Y = value;
        }
        public float Angle
        {
            get => _angle;
            set => _angle = value.Mod(MathF.PI * 2);
        }
        public Vector2 Direction => new(MathF.Cos(Angle), MathF.Sin(Angle));
        public float FOV { get; set; }
        public float ViewDistance { get; set; }
        public float WalkSpeed { get; set; }
        public float RotationSpeed { get; set; }
        public abstract Shape GetShape();
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
            Position.X += MathF.Cos(angleAdjusted) * speedAdjusted;
            Position.Y += MathF.Sin(angleAdjusted) * speedAdjusted;
        }
        //public float AngleTo(Vector2 other) => Position.AngleTo(other);

        public static implicit operator Vector2(Entity entity) => entity.Position;
    }

    public class PointEntity : Entity
    {
        public override Shape GetShape() => new Point(Position);
    }

    public class BillboardEntity : Entity
    {
        public float Width { get; set; }
        public string Name { get; set; } = "Billboard Entity";
        public string TextureName { get; set; } = "";
        public TextureMappingType MappingType { get; set; } = TextureMappingType.Invisible;
        public bool IsSolid { get; set; } = false;
        public override Shape GetShape()
        {
            Vector2 a = Position + new Vector2(
                    MathF.Cos(Angle + MathF.PI / 2) * (Width / 2),
                    MathF.Sin(Angle + MathF.PI / 2) * (Width / 2)
                    );
            Vector2 b = Position + new Vector2(
                MathF.Cos(Angle - MathF.PI / 2) * (Width / 2),
                MathF.Sin(Angle - MathF.PI / 2) * (Width / 2)
                );
            return new LineSegment(a, b, Name)
            {
                TextureName = TextureName,
                TextureMapping = MappingType,
                IsSolid = IsSolid
            };
        }
    }
}
