namespace ConsoleGame
{
    public class Entity : fVector2D
    {
        public float Angle;
        public float FOV;
        public float viewDistance;
        public float WalkSpeed;
        public float RotationSpeed;
        public char Icon;

        public Entity(
            char icon,
            float x = 0,
            float y = 0,
            float angle = 0,
            float walkSpeed = 1f,
            float rotationSpeed = 1f,
            float fov = (float)Math.PI / 2,
            int fovDepth = 16
            )
        {
            X = x;
            Y = y;
            Angle = angle;
            Icon = icon;
            WalkSpeed = walkSpeed;
            FOV = fov;
            this.viewDistance = fovDepth;
            RotationSpeed = rotationSpeed;
        }

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
            X += (float)Math.Cos(angleAdjusted) * speedAdjusted;
            Y += (float)Math.Sin(angleAdjusted) * speedAdjusted;
        }
    }
}
