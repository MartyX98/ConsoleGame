using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Shared
{
    public static class Raycaster
    {
        public static int RayMaxCount;
        public static int RayMaxDist;

        // RayIntersection pool
        private static RayIntersection[] _rayIntersectionPool;
        private static int _rayIntersectionPoolCapacity;
        private static int _rayIntersectionPoolIndex;

        public static void Init(int rayMaxCount, int rayMaxDist)
        {
            RayMaxCount = rayMaxCount;
            RayMaxDist = rayMaxDist;

            _rayIntersectionPoolIndex = 0;
            _rayIntersectionPoolCapacity = RayMaxCount * RayMaxDist;
            _rayIntersectionPool = new RayIntersection[_rayIntersectionPoolCapacity];
            for (int i = 0;  i < _rayIntersectionPoolCapacity; i++)
            {
                _rayIntersectionPool[i] = new RayIntersection();
            }
        }

        /// <summary>
        /// Represents a single step in a ray's lifetime.
        /// </summary>
        /// <param name="position">The position of the intersection point.</param>
        /// <param name="tile">The tile position of the intersection point. (integer values)</param>
        /// <param name="direction">The direction vector of the ray.</param>
        /// <param name="angle">The angle of the ray.</param>
        /// <param name="distance">The distance from the origin to the intersection point.</param>
        /// <param name="distanceToNextTile">The distance from the intersection point to the next tile.</param>
        /// <param name="stepIndex">The index of the step in the ray's lifetime. Shape intersection steps are not included.</param>
        /// <param name="shape">The shape that was hit by the raycast step. If no shape was hit, this value is null.</param>
        //public abstract class RayIntersection
        //{
        //    /// <summary>
        //    /// The position of the intersection point.
        //    /// </summary>
        //    public Vector2 Position { get; set; }
        //    /// <summary>
        //    /// The tile position of the ntersection point. (integer values)
        //    /// </summary>
        //    public Vector2 TilePosition { get; set; }

        //    private float _angle;
        //    /// <summary>
        //    /// The angle of the ray.
        //    /// </summary>
        //    public float Angle
        //    {
        //        get => _angle;
        //        set => _angle = value.Mod(MathF.PI * 2);
        //    }
        //    /// <summary>
        //    /// The direction vector of the ray.
        //    /// </summary>
        //    public Vector2 Direction { get; set; }
        //    /// <summary>
        //    /// The distance from the origin to the intersection point.
        //    /// </summary>
        //    public float Distance { get; set; }
        //    /// <summary>
        //    /// The distance from the intersection point to the next tile.
        //    /// </summary>
        //    public float DistanceToNextTile { get; set; }
        //    /// <summary>
        //    /// The index of the step in the ray's lifetime.
        //    /// </summary>
        //    /// <remarks>
        //    /// Shape intersection steps within tiles are not included. Only the steps between tiles are counted.
        //    /// </remarks>
        //    public int StepIndex { get; set; }
        //}

        //public class TileIntersection : RayIntersection { }

        //public class ShapeIntersection : RayIntersection
        //{
        //    public required Shape Shape { get; set; }
        //    public required float NormalizedPosition { get; set; }
        //}

        public enum RayIntersectionType
        {
            Tile,
            Shape,
        }

        public struct RayIntersection
        {
            /// <summary>
            /// The type of intersection
            /// </summary>
            public RayIntersectionType Type;
            /// <summary>
            /// The position of the intersection point.
            /// </summary>
            public Vector2 Position;
            /// <summary>
            /// The tile position of the ntersection point. (integer values)
            /// </summary>
            public Vector2 TilePosition;
            /// <summary>
            /// The angle of the ray.
            /// </summary>
            public float Angle;
            /// <summary>
            /// The direction vector of the ray.
            /// </summary>
            public Vector2 Direction;
            /// <summary>
            /// The distance from the origin to the intersection point.
            /// </summary>
            public float Distance;
            /// <summary>
            /// The distance from the intersection point to the next tile.
            /// </summary>
            public float DistanceToNextTile;
            /// <summary>
            /// The index of the step in the ray's lifetime.
            /// </summary>
            /// <remarks>
            /// Shape intersection steps within tiles are not included. Only the steps between tiles are counted.
            /// </remarks>
            public int StepIndex;
            /// <summary>
            /// The shape that was intersected if intersection type is Shape
            /// </summary>
            public Shape Shape;
            /// <summary>
            /// Not sure..
            /// </summary>
            public float NormalizedPosition;
        }

        private static RayIntersection GetPooledRayIntersection()
        {
            _rayIntersectionPoolIndex++;
            if (_rayIntersectionPoolIndex >= _rayIntersectionPoolCapacity)
                _rayIntersectionPoolIndex = 0;
            return _rayIntersectionPool[_rayIntersectionPoolIndex];
        }

        #region Base Raycasting Methods
        
        public static IEnumerable<RayIntersection> CastRay(Vector2 origin, float angle)
        {
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 unitStepSize = new(
                MathF.Sqrt(1 + (direction.Y / direction.X) * (direction.Y / direction.X)),
                MathF.Sqrt(1 + (direction.X / direction.Y) * (direction.X / direction.Y))
            );
            Vector2 tilePos = new((int)origin.X, (int)origin.Y);
            // Calculate the step direction
            Vector2 length1D = new Vector2(
                direction.X < 0 ? origin.X - tilePos.X : tilePos.X + 1 - origin.X,
                direction.Y < 0 ? origin.Y - tilePos.Y : tilePos.Y + 1 - origin.Y
            ) * unitStepSize;
            Vector2 tileStep = new(
                direction.X < 0 ? -1 : 1,
                direction.Y < 0 ? -1 : 1
            );
            float distance = 0f;

            // DDA walk
            int step = 0;
            while (true)
            {
                float distanceToNextTile = Math.Min(length1D.X, length1D.Y);
                RayIntersection ri = GetPooledRayIntersection();
                ri.Type = RayIntersectionType.Tile;
                ri.Position = new Vector2(origin.X + distance * direction.X, origin.Y + distance * direction.Y);
                ri.TilePosition = new Vector2(tilePos.X, tilePos.Y);
                ri.Direction = direction;
                ri.Angle = angle;
                ri.Distance = distance;
                ri.DistanceToNextTile = distanceToNextTile;
                ri.StepIndex = step;
                yield return ri;

                // Calculate the next step
                distance = distanceToNextTile;
                if (length1D.X < length1D.Y)
                {
                    tilePos.X += tileStep.X;
                    length1D.X += unitStepSize.X;
                }
                else
                {
                    tilePos.Y += tileStep.Y;
                    length1D.Y += unitStepSize.Y;
                }
                step++;
            }
        }

        //public static IEnumerable<IEnumerable<TileIntersection>> CastRays(Vector2 origin, float angle, float fov, int numRays)
        //{
        //    float halfFov = fov / 2;
        //    float rayAngleStep = fov / (numRays - 1);

        //    for (int i = 0; i < numRays; i++)
        //    {
        //        float currentAngle = numRays > 1 ? angle - halfFov + i * rayAngleStep : angle;
        //        yield return CastRay(origin, currentAngle);
        //    }
        //}

        //public static IEnumerable<IEnumerable<TileIntersection>> CastRays(Entity entity, int numRays) =>
        //   CastRays(entity, entity.Angle, entity.FOV, numRays);

        #endregion

        #region Additional Raycasting Methods

        public static IEnumerable<RayIntersection> CastRayFromTo(Vector2 origin, Vector2 target)
        {
            Vector2 direction = Vector2.Normalize(target - origin);
            float angle = MathF.Atan2(direction.Y, direction.X);
            float distance = Vector2.Distance(origin, target);
            foreach (RayIntersection step in CastRay(origin, angle))
            {
                if (step.Distance > distance)
                    break;
                yield return step;
            }
        }

        public static IEnumerable<RayIntersection> CastRayFromTo(Vector2 origin, Vector2 target, float stepSize)
        {
            Vector2 direction = Vector2.Normalize(target - origin);
            float angle = MathF.Atan2(direction.Y, direction.X);
            float distance = Vector2.Distance(origin, target);
            for (float i = 0; i < distance; i += stepSize)
            {
                Vector2 currentPos = origin + direction * i;
                RayIntersection ri = GetPooledRayIntersection();
                ri.Type = RayIntersectionType.Tile;
                ri.Position = currentPos;
                ri.TilePosition = new Vector2((int)currentPos.X, (int)currentPos.Y);
                ri.Direction = direction;
                ri.Angle = angle;
                ri.Distance = i;
                ri.DistanceToNextTile = Math.Min(stepSize, distance - i);
                ri.StepIndex = (int)(i / stepSize);
                yield return ri;
            }
        }

        #endregion

        #region MapHelper Raycasting Methods

        /// <summary>
        /// Casts a ray from the origin in the specified angle and returns the steps of the ray.
        /// </summary>
        /// <param name="map">The map to cast the ray on.</param>
        /// <param name="origin">The origin of the ray.</param>
        /// <param name="angle">The angle of the ray.</param>
        /// <returns>The steps of the ray. Both the intersections with tile borders and shapes are included.</returns>
        public static IEnumerable<RayIntersection> CastRay(MapHelper map, Vector2 origin, float angle)
        {
            foreach (RayIntersection step in CastRay(origin, angle))
            {
                yield return step;
                if (!map.StaticMap.Validate(step.TilePosition))
                    continue;

                List<(float distance, float normPos, Shape shape)> stepsWithinTile = [];
                foreach (Shape shape in map.StaticMap[step.TilePosition].Shapes)
                    if (shape.CheckIntersection(origin, step.Angle, out float distanceToShape, out float normPos) && distanceToShape < step.DistanceToNextTile)
                        stepsWithinTile.Add((distanceToShape, normPos, shape));
                foreach (Shape shape in map.DynamicMap[step.TilePosition].Shapes)
                    if (shape.CheckIntersection(origin, step.Angle, out float distanceToShape, out float normPos) && distanceToShape < step.DistanceToNextTile)
                        stepsWithinTile.Add((distanceToShape, normPos, shape));

                // yield steps in order from shortest to longest
                foreach (var (distance, normPos, shape) in stepsWithinTile.OrderBy(s => s.distance))
                {
                    RayIntersection ri = GetPooledRayIntersection();
                    ri.Type = RayIntersectionType.Shape;
                    ri.Position = origin + (step.Direction * distance);
                    ri.TilePosition = step.TilePosition;
                    ri.Direction = step.Direction;
                    ri.Angle = step.Angle;
                    ri.Distance = distance;
                    ri.DistanceToNextTile = step.DistanceToNextTile - distance;
                    ri.StepIndex = step.StepIndex;
                    ri.Shape = shape;
                    ri.NormalizedPosition = normPos;
                    yield return ri;
                }
            }
        }

        /// <summary> 
        /// Casts rays from the entity in the specified angle and returns the steps of the rays.
        /// </summary>
        /// <param name="map">The map to cast the rays on.</param>
        /// <param name="entity">The entity to cast the rays from.</param>
        /// <param name="angle">The angle of the rays.</param>
        /// <param name="fov">The field of view of the rays.</param>
        /// <param name="numRays">The number of rays to cast.</param>
        /// <returns>The steps of the rays. Both the intersections with tile borders and shapes are included.</returns>
        public static IEnumerable<IEnumerable<RayIntersection>> CastRays(MapHelper map, Vector2 origin, float angle, float fov, int numRays)
        {
            float halfFov = fov / 2;
            float rayAngleStep = fov / (numRays - 1);

            for (int i = 0; i < numRays; i++)
            {
                float currentAngle = numRays > 1 ? angle - halfFov + i * rayAngleStep : angle;
                yield return CastRay(map, origin, currentAngle);
            }
        }

        public static IEnumerable<IEnumerable<RayIntersection>> CastRays(MapHelper map, Entity entity, int numRays) =>
            CastRays(map, entity, entity.Angle, entity.FOV, numRays);

        #endregion

        #region Helper Methods

        public static IEnumerable<(int Index, float Distance)> PerspectiveSteps(
            float rayLength,
            int numSteps,
            float near = 0.1f,
            float exponent = 0.004f
            )
        {
            float start = 1 / near;
            float end = 1 / rayLength;

            for (int i = 0; i < numSteps; i++)
            {
                float t = MathF.Pow((float)i / (numSteps - 1), exponent);
                float invD = start + (end - start) * t;
                yield return (i, 1 / invD);
            }
        }

        #endregion
    }
}
