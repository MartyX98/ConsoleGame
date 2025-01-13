using ConsoleGame;
using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;

namespace ConsoleGame
{
    public class Program
    {
        public static void Main()
        {
            Plane pScreen = new(120, 30);

            // building floor texture
            Plane pFloor = new(pScreen.Width, 15);
            Random random = new();
            for (int x = 0; x < pFloor.Width; x++)
            {
                for (int y = 0; y < pFloor.Height; y++)
                {
                    pFloor[y, x] = Icons.GetShade((float)y / (pFloor.Height - 1), Icons.ShadingFloor, random.Next(-3, 4), flipRange: true);
                }
            }

            Plane pMap = new(path: "maps/test.txt");
            Plane pMiniMap = new(pMap);
            ConsoleHandler consoleManager = new();
            FPSManager fpsManager = new();
            Entity ePlayer = new(icon: Icons.Player, x: 1f, y: 1f, walkSpeed: 6, rotationSpeed: 3, fov: (float)(Math.PI / 3), fovDepth: 16);
            UserInputHandler userInputHandler = new();
            CultureInfo clt = CultureInfo.InvariantCulture;
            userInputHandler.StartHandlingInputAsync();

            bool mainLoop = true;
            while (mainLoop)
            {
                fpsManager.Update();

                // Detect user input
                switch (userInputHandler.pressedKey)
                {
                    // walk forward
                    case ConsoleKey.NumPad8:
                        ePlayer.Walk(fpsManager.deltaTime);
                        //gFloor.Slide(offsetY: 1);
                        break;

                    // walk backwards
                    case ConsoleKey.NumPad5:
                        ePlayer.Walk(fpsManager.deltaTime, speedMult: -1f);
                        //gFloor.Slide(offsetY: -1);
                        break;

                    // strafe left
                    case ConsoleKey.NumPad7:
                        ePlayer.Walk(fpsManager.deltaTime, angleIncr: (float)(-Math.PI / 2));
                        pFloor.Slide(offsetX: 1);
                        break;

                    // strafe right
                    case ConsoleKey.NumPad9:
                        ePlayer.Walk(fpsManager.deltaTime, angleIncr: (float)(Math.PI / 2));
                        pFloor.Slide(offsetX: -1);
                        break;

                    // turn left
                    case ConsoleKey.NumPad4:
                        ePlayer.RotateLeft(fpsManager.deltaTime);
                        pFloor.Slide(offsetX: 1);
                        break;

                    // turn right
                    case ConsoleKey.NumPad6:
                        ePlayer.RotateRight(fpsManager.deltaTime);
                        pFloor.Slide(offsetX: -1);
                        break;

                    // exit
                    case ConsoleKey.Escape:
                        mainLoop = false;
                        break;
                }

                // DDA Raycast loop, 2D to 3D screen projection
                pMiniMap.Impose(pMap);
                Plane pWalls = new(pScreen.Width, pScreen.Height);
                fVector2D vRayStart = new(ePlayer.X, ePlayer.Y);
                for (int x = 0; x < pScreen.Width; x++)
                {
                    float relativeX = (x - pScreen.Width / 2f) / pScreen.Width;
                    float rayAngle = ePlayer.Angle + relativeX * ePlayer.FOV;
                    fVector2D vRayDir = new((float)Math.Cos(rayAngle), (float)Math.Sin(rayAngle));
                    fVector2D vRayUnitStepSize = new((float)Math.Sqrt(1 + (vRayDir.Y / vRayDir.X) * (vRayDir.Y / vRayDir.X)),
                                                   (float)Math.Sqrt(1 + (vRayDir.X / vRayDir.Y) * (vRayDir.X / vRayDir.Y)));
                    iVector2D vMapCheck = new(vRayStart);
                    fVector2D vRayLength1D = new(0, 0);
                    iVector2D vStep = new(0, 0);

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

                    bool bTileFound = false;
                    float fMaxDistance = ePlayer.fovDepth;
                    float fDistance = 0f;
                    while (!bTileFound && fDistance < fMaxDistance)
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

                        if (pMap.Validate(vMapCheck) && pMap[vMapCheck.Y, vMapCheck.X] == Icons.Wall)
                        {
                            bTileFound = true;
                        }
                    }

                    fVector2D vIntersection = vRayStart + vRayDir * fDistance;
                    // fisheye distortion fix
                    fDistance *= (float)Math.Cos((relativeX * ePlayer.FOV) % (Math.PI * 2));

                    if (bTileFound)
                    {
                        // Visualizing POV on minimap
                        pMiniMap[vMapCheck] = Icons.MiniMap_Wall;

                        // Drawing wall column
                        int minWallHeight = 0;
                        int maxWallHeight = 28;

                        float fDistanceUnit = (fDistance / ePlayer.fovDepth);

                        int wallCloseOffset = (pScreen.Height - maxWallHeight) / 2;
                        int wallFarOffset = (pScreen.Height - minWallHeight) / 2;

                        int wallVerticalOffset = (int)(wallCloseOffset + fDistanceUnit * ( wallFarOffset - wallCloseOffset));
                        wallVerticalOffset = Math.Min(Math.Max(0, wallVerticalOffset), pScreen.Height);

                        for (int i = wallVerticalOffset; i < pScreen.Height - wallVerticalOffset; i++)
                        {
                            pWalls[i, x] = Icons.GetShade(fDistance / ePlayer.fovDepth, Icons.ShadingWall, vIntersection.IsVertex(0.075f) ? +1 : 0);
                        }
                    }
                }


                // Screen updates
                pMiniMap[(int)ePlayer.Y, (int)ePlayer.X] = ePlayer.Icon;
                pScreen.Impose(pFloor, y: pScreen.Height - pFloor.Height);
                pScreen.Impose(pWalls, allowTransparency: true);
                pScreen.Impose($"FPS: {fpsManager.CurrentFPS} | Player XYA ({ePlayer.X.ToString("0.0", clt)}, {ePlayer.Y.ToString("0.0", clt)}, {ePlayer.Angle.ToString("0.0", clt)}) | Map size: ({pMiniMap.Width}, {pMiniMap.Height}) | {userInputHandler.pressedKey}");
                pScreen.Impose(pMiniMap, y:1);
                consoleManager.Write(pScreen.Arr);
                pScreen.Fill(' ');
            }

            userInputHandler.StopHandlingInput();

        }
    }
}
