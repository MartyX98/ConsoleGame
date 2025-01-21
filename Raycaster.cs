using static ConsoleGame.Raycaster;

namespace ConsoleGame
{
    public static class Raycaster
    {
        // Pre-initializing vectors for performance
        private static readonly fVector2D vRayDir = new();
        private static readonly fVector2D vRayUnitStepSize = new();
        private static readonly iVector2D vMapCheck = new();
        private static readonly fVector2D vRayLength1D = new();
        private static readonly iVector2D vStep = new();
        private static float fDistance = 0f;

        public static IEnumerable<CastResult> CastRay(CharGrid gMap, fVector2D vRayStart, float fRayAngle, float fMaxRayDistance, char[] solidObjects, char[] transparentObjects)
        {
            vRayDir.X = (float)Math.Cos(fRayAngle);
            vRayDir.Y = (float)Math.Sin(fRayAngle);
            vRayUnitStepSize.X = (float)Math.Sqrt(1 + (vRayDir.Y / vRayDir.X) * (vRayDir.Y / vRayDir.X));
            vRayUnitStepSize.Y = (float)Math.Sqrt(1 + (vRayDir.X / vRayDir.Y) * (vRayDir.X / vRayDir.Y));
            vMapCheck.X = (int)vRayStart.X;
            vMapCheck.Y = (int)vRayStart.Y;
            vRayLength1D.X = 0;
            vRayLength1D.Y = 0;
            vStep.X = 0;
            vStep.Y = 0;
            fDistance = 0f;

            // determine step directions on x and y axis
            if (vRayDir.X < 0)
            {
                vStep.X = -1;
                vRayLength1D.X = (vRayStart.X - vMapCheck.X) * vRayUnitStepSize.X;
            }
            else
            {
                vStep.X = 1;
                vRayLength1D.X = (vMapCheck.X + 1 - vRayStart.X) * vRayUnitStepSize.X;
            }

            if (vRayDir.Y < 0)
            {
                vStep.Y = -1;
                vRayLength1D.Y = (vRayStart.Y - vMapCheck.Y) * vRayUnitStepSize.Y;
            }
            else
            {
                vStep.Y = 1;
                vRayLength1D.Y = (vMapCheck.Y + 1 - vRayStart.Y) * vRayUnitStepSize.Y;
            }

            // DDA walk
            while (fDistance < fMaxRayDistance)
            {
                if (vRayLength1D.X < vRayLength1D.Y)
                {
                    vMapCheck.X += vStep.X;
                    fDistance = vRayLength1D.X;
                    vRayLength1D.X += vRayUnitStepSize.X;
                }
                else
                {
                    vMapCheck.Y += vStep.Y;
                    fDistance = vRayLength1D.Y;
                    vRayLength1D.Y += vRayUnitStepSize.Y;
                }

                if (gMap.Validate(vMapCheck))
                {
                    if (solidObjects.Contains(gMap[vMapCheck.Y, vMapCheck.X]))
                    {
                        yield return new CastResult()
                        {
                            HitObject = gMap[vMapCheck.Y, vMapCheck.X],
                            Distance = fDistance,
                            GridIntersection = new iVector2D(vMapCheck),
                            ExactIntersection = vRayStart + vRayDir * fDistance,
                        };
                        yield break;
                    }

                    if (transparentObjects.Contains(gMap[vMapCheck.Y, vMapCheck.X]))
                    {
                        yield return new CastResult()
                        {
                            HitObject = gMap[vMapCheck.Y, vMapCheck.X],
                            Distance = fDistance,
                            GridIntersection = new iVector2D(vMapCheck),
                            ExactIntersection = vRayStart + vRayDir * fDistance,
                        };
                    }
                }
            }
        }

        public static IEnumerable<(int, CastResult)> CastRays(fVector2D origin, float direction, float fov, float maxDepth, int numRays, CharGrid gMap, char[] solidObjects, char[] transparentObjects, bool correctFisheEye = true)
        {
            float angleStep = fov / (numRays - 1);

            for (int i = 0; i < numRays; i++)
            {
                float rayAngle = direction - (fov / 2) + (i * angleStep);
                foreach (var result in CastRay(gMap, origin, rayAngle, maxDepth, solidObjects, transparentObjects))
                {
                    yield return (i, result);
                }
            }
        }

        public static IEnumerable<(int, CastResult)> CastRays(Entity origin, int numRays, CharGrid gMap, char[] solidObjects, char[] transparentObjects) =>
            CastRays(origin, origin.Angle, origin.FOV, origin.fovDepth, numRays, gMap, solidObjects, transparentObjects);

        public struct CastResult
        {
            public char HitObject { get; set; }
            public float Distance { get; set; }
            public required iVector2D GridIntersection { get; set; }
            public required fVector2D ExactIntersection { get; set; }
        }
    }
}
