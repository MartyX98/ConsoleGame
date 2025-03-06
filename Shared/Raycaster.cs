using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace ConsoleGame
{
    public static class Raycaster
    {
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
        public class RaycastStep(Vector2 position, Vector2 tile, Vector2 direction, float angle, float distance, float distanceToNextTile, int stepIndex, Shape? shape = null)
        {
            /// <summary>
            /// The position of the intersection point.
            /// </summary>
            public Vector2 Position { get; set; } = position;
            /// <summary>
            /// The tile position of the ntersection point. (integer values)
            /// </summary>
            public Vector2 Tile { get; set; } = tile;
            /// <summary>
            /// The shape that was hit by the raycast step.
            /// </summary>
            /// <remarks>
            /// This value is null if the raycast step did not intersect with any shape.
            /// </remarks>
            public Shape? Shape { get; set; } = shape;
            /// <summary>
            /// The angle of the ray.
            /// </summary>
            public float Angle { get; set; } = angle;
            /// <summary>
            /// The direction vector of the ray.
            /// </summary>
            public Vector2 Direction { get; set; } = direction;
            /// <summary>
            /// The distance from the origin to the intersection point.
            /// </summary>
            public float Distance { get; set; } = distance;
            /// <summary>
            /// The distance from the intersection point to the next tile.
            /// </summary>
            public float DistanceToNextTile { get; set; } = distanceToNextTile;
            /// <summary>
            /// The index of the step in the ray's lifetime.
            /// </summary>
            /// <remarks>
            /// Shape intersection steps within tiles are not included. Only the steps between tiles are counted.
            /// </remarks>
            public int StepIndex { get; set; } = stepIndex;
        }

        #region Base Raycasting Methods

        public static IEnumerable<RaycastStep> CastRay(Vector2 origin, float angle)
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
                yield return new RaycastStep(
                    new Vector2(origin.X + distance * direction.X, origin.Y + distance * direction.Y),
                    new Vector2(tilePos.X, tilePos.Y),
                    direction,
                    angle,
                    distance,
                    distanceToNextTile,
                    step
                    );

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

        public static IEnumerable<IEnumerable<RaycastStep>> CastRays(Vector2 origin, float angle, float fov, int numRays)
        {
            float halfFov = fov / 2;
            float rayAngleStep = fov / (numRays - 1);

            for (int i = 0; i < numRays; i++)
            {
                float currentAngle = numRays > 1 ? angle - halfFov + i * rayAngleStep : angle;
                yield return CastRay(origin, currentAngle);
            }
        }
        public static IEnumerable<IEnumerable<RaycastStep>> CastRays(Entity entity, int numRays) =>
           CastRays(entity, entity.Angle, entity.FOV, numRays);

        #endregion

        #region Additional Raycasting Methods

        public static IEnumerable<RaycastStep> CastRayFromTo(Vector2 origin, Vector2 target)
        {
            Vector2 direction = Vector2.Normalize(target - origin);
            float angle = MathF.Atan2(direction.Y, direction.X);
            float distance = Vector2.Distance(origin, target);
            foreach (RaycastStep step in CastRay(origin, angle))
            {
                if (step.Distance > distance)
                    break;
                yield return step;
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
        public static IEnumerable<RaycastStep> CastRay(MapHelper map, Vector2 origin, float angle)
        {
            foreach (RaycastStep step in CastRay(origin, angle))
            {
                yield return step;
                if (!map.Map.Validate(step.Tile))
                    continue;
                Tile tile = map[step.Tile];
                //List<float> stepsWithinTile = [];
                // we also need to have the ref to the shape that was hit
                List<(float distance, Shape shape)> stepsWithinTile = [];

                foreach (Shape shape in tile.Shapes)
                    if (shape.CheckIntersection(origin, step.Angle, out float distanceToShape) && distanceToShape < step.DistanceToNextTile)
                        stepsWithinTile.Add((distanceToShape, shape));

                // yield steps in order from shortest to longest
                foreach (var (distance, shape) in stepsWithinTile.OrderBy(s => s.distance))
                    yield return new RaycastStep(
                        position: origin + (step.Direction * distance),
                        tile: step.Tile,
                        direction: step.Direction,
                        angle: step.Angle,
                        distance: distance,
                        distanceToNextTile: step.DistanceToNextTile - distance,
                        stepIndex: step.StepIndex,
                        shape: shape
                        );
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
        public static IEnumerable<IEnumerable<RaycastStep>> CastRays(MapHelper map, Vector2 origin, float angle, float fov, int numRays)
        {
            float halfFov = fov / 2;
            float rayAngleStep = fov / (numRays - 1);

            for (int i = 0; i < numRays; i++)
            {
                float currentAngle = numRays > 1 ? angle - halfFov + i * rayAngleStep : angle;
                yield return CastRay(map, origin, currentAngle);
            }
        }

        public static IEnumerable<IEnumerable<RaycastStep>> CastRays(MapHelper map, Entity entity, int numRays) =>
            CastRays(map, entity, entity.Angle, entity.FOV, numRays);

        #endregion
    }
}
