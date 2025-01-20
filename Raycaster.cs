using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static CastResult CastRay(CharGrid gMap, fVector2D vRayStart, float fRayAngle, float fMaxRayDistance, char[] rayCollidables)
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
            bool bIntersectionFlag = false;
            float fDistance = 0f;
            while (!bIntersectionFlag && fDistance < fMaxRayDistance)
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

                if (gMap.Validate(vMapCheck) && rayCollidables.Contains(gMap[vMapCheck.Y, vMapCheck.X]))
                {
                    bIntersectionFlag = true;
                }
            }

            // return result
            return new CastResult()
            {
                HitObject = gMap[vMapCheck.Y, vMapCheck.X],
                Distance = fDistance,
                GridIntersection = new iVector2D(vMapCheck),
                ExactIntersection = vRayStart + vRayDir * fDistance,
            };
        }

        public struct CastResult
        {
            public char HitObject { get; set; }
            public float Distance { get; set; }
            public required iVector2D GridIntersection { get; set; }
            public required fVector2D ExactIntersection { get; set; }
        }
    }
}
