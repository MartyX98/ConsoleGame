using ConsoleGame;
using System.Globalization;
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
            int randShadeOffset = 3;
            for (int colIdx = 0; colIdx < pFloor.Width; colIdx++)
            {
                for (int rowIdx = 0; rowIdx < pFloor.Height; rowIdx++)
                {
                    int p = Math.Max(Math.Min((rowIdx * Icons.ShadingFloor.Length / pFloor.Height) + new Random().Next(-randShadeOffset, randShadeOffset + 1), Icons.ShadingFloor.Length - 1), 0);
                    pFloor[rowIdx, colIdx] = Icons.ShadingFloor[p];
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
            int tempCount = 0;

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
                        tempCount++;
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

                    char shade = Icons.Air;
                    fVector2D vIntersection = vRayStart + vRayDir * fDistance;
                    // fisheye distortion fix
                    fDistance *= (float)Math.Cos((relativeX * ePlayer.FOV) % (Math.PI * 2));

                    if (bTileFound)
                    {
                        // Visualizing POV on minimap
                        var vIntF = vIntersection % 1;
                        pMiniMap[vMapCheck] = Icons.MiniMap_Wall;

                        // Shading
                        int iShade = (int)(fDistance * (Icons.ShadingWall.Length - 1) / ePlayer.fovDepth);
                        // downgrade shade if ray hit corner
                        float limit = 0.075f;
                        if ((vIntF.X < limit || vIntF.X > 1 - limit) && (vIntF.Y < limit || vIntF.Y > 1 - limit))
                        {
                            iShade = Math.Min(iShade + 1, Icons.ShadingWall.Length - 1);
                        }
                        shade = Icons.ShadingWall[iShade];

                        int minWallHeight = 0;
                        int maxWallHeight = 30;

                        float fDistanceUnit = (fDistance / ePlayer.fovDepth);

                        int wallCloseOffset = (pScreen.Height - maxWallHeight) / 2;
                        int wallFarOffset = (pScreen.Height - minWallHeight) / 2;

                        int wallVerticalOffset = (int)(wallCloseOffset + fDistanceUnit * ( wallFarOffset - wallCloseOffset));
                        wallVerticalOffset = Math.Min(Math.Max(0, wallVerticalOffset), pScreen.Height);

                        for (int i = wallVerticalOffset; i < pScreen.Height - wallVerticalOffset; i++)
                        {
                            pWalls[i, x] = shade;
                        }
                    }
                }


                // Screen updates
                pMiniMap[(int)ePlayer.Y, (int)ePlayer.X] = ePlayer.Icon;
                pScreen.Impose(pFloor, y: pScreen.Height - pFloor.Height);
                pScreen.Impose(pWalls, allowTransparency: true);
                pScreen.Impose($"FPS: {fpsManager.CurrentFPS} | Player XYA ({ePlayer.X.ToString("0.0", clt)}, {ePlayer.Y.ToString("0.0", clt)}, {ePlayer.Angle.ToString("0.0", clt)}) | Map size: ({pMiniMap.Width}, {pMiniMap.Height}) | {userInputHandler.pressedKey} {tempCount}");
                pScreen.Impose(pMiniMap, y:1);
                consoleManager.Write(pScreen.Arr);
                pScreen.Fill(' ');
            }

            userInputHandler.StopHandlingInput();

        }
    }
}

// --- Ordered TODO tasks ---
// DONE: Custom vector class
// DONE: Custom 2D Plane class with char array at core
// DONE: Implement some primitive 3D wall rendering (finally)
// DONE: ScreenHandler.AddToScreen should have extra optional param "allowTransparency" to skip Icons.Air to be able to print transparently.
// DONE: Fix fisheye distortion
// DONE: Grid2D renamed to Plane, reimplemented transformations, added richer options
// DONE: Replace MapHandler with Plane
// DONE: Got rid of ScreenHandler
// Shit happens too many times upon single key push. Either expand UserInputHandler to listen for key up/down events, or code in manual control over repetitions
// once ↑ is done, take control of the speed of shifting of the floor texture when turning

// --- Other TODO tasks ---
// preallocate vars before loops to avoid performance overhead with calling constructors (does that even make a difference?)
// rename Icons class to Textures?
// Texture grandient obj / class for texture ranges?
