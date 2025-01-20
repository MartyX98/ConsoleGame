using System.Globalization;

namespace ConsoleGame
{
    public class Program
    {
        public static void Main()
        {
            CharGrid gScreen = new(120, 30);
            CharGrid gBackground = new(gScreen.Width, gScreen.Height);
            CharGrid gWallTexture = CharGrid.Load(Path.Combine("textures", "wall.txt"));
            CharGrid gTreeTexture = CharGrid.Load(Path.Combine("textures", "tree.txt"));
            float worldWallHeight = 1.6f;
            float verticalTextureOffset = 0.57f;

            Random r = new();

            int iFloorTextureRandOffset = 3;
            float fSkyStarPopulation = 1f / 40;

            // building sky
            for (int x = 0; x < gBackground.Width; x++)
            {
                for (int y = 0; y < gBackground.Height / 2; y++)
                {
                    gBackground[y, x] = r.NextSingle() <= fSkyStarPopulation ? Icons.Stars[r.Next(0, Icons.Stars.Length)] : Icons.Air;
                }
            }

            // building floor
            for (int x = 0; x < gBackground.Width; x++)
            {
                for (int y = 0; y < gBackground.Height / 2; y++)
                {
                    gBackground[y + gBackground.Height / 2, x] = Icons.GetShade((float)y / (gBackground.Height / 2 - 1), Icons.ShadingFloor, r.Next(-iFloorTextureRandOffset, iFloorTextureRandOffset + 1), flipRange: true);
                }
            }

            CharGrid gMap = CharGrid.Load(Path.Combine("maps", "test.txt"));
            CharGrid gMinimap = new(gMap);

            Entity ePlayer = new(
                icon: Icons.Player,
                x: 1f,
                y: 1f,
                walkSpeed: 6,
                rotationSpeed: 3,
                fov: (float)(Math.PI / 3),
                fovDepth: 16);

            ConsoleHandler Window = new();
            FPSManager fpsManager = new();
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
                        gBackground.Slide(offsetX: 1);
                        break;

                    // strafe right
                    case ConsoleKey.NumPad9:
                        ePlayer.Walk(fpsManager.deltaTime, angleIncr: (float)(Math.PI / 2));
                        gBackground.Slide(offsetX: -1);
                        break;

                    // turn left
                    case ConsoleKey.NumPad4:
                        ePlayer.RotateLeft(fpsManager.deltaTime);
                        gBackground.Slide(offsetX: 1);
                        break;

                    // turn right
                    case ConsoleKey.NumPad6:
                        ePlayer.RotateRight(fpsManager.deltaTime);
                        gBackground.Slide(offsetX: -1);
                        break;

                    // add to var
                    case ConsoleKey.NumPad1:
                        worldWallHeight += 0.001f;
                        break;

                    // sub from var
                    case ConsoleKey.NumPad3:
                        worldWallHeight -= 0.001f;
                        break;

                    // exit
                    case ConsoleKey.Escape:
                        mainLoop = false;
                        break;
                }

                // DDA Raycast loop, 2D to 3D screen projection
                gMinimap.Impose(gMap);
                CharGrid pWalls = new(gScreen.Width, gScreen.Height);
                fVector2D vRayStart = new(ePlayer.X, ePlayer.Y);
                for (int x = 0; x < gScreen.Width; x++)
                {
                    float relativeX = (x - gScreen.Width / 2f) / gScreen.Width;
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

                        if (gMap.Validate(vMapCheck) && gMap[vMapCheck.Y, vMapCheck.X] == Icons.Wall)
                        {
                            bTileFound = true;
                        }
                    }

                    fVector2D vIntersection = vRayStart + vRayDir * fDistance;
                    
                    // fisheye distortion fix
                    fDistance *= (float)Math.Cos((relativeX * ePlayer.FOV) % (Math.PI * 2));

                    if (bTileFound)
                    {
                        gMinimap[vMapCheck] = Icons.MiniMap_WallIntersected;

                        // Calculate the wall height based on distance
                        int wallHeight = (int)(gScreen.Height / fDistance);
                        wallHeight = Math.Clamp(wallHeight, 0, gScreen.Height / 2);

                        // Determine horizontal texture offset (unchanged)
                        int x_texture = Math.Round(vIntersection.X, 4) % 1 == 0 ?
                            (int)((vIntersection.Y - (float)Math.Floor(vIntersection.Y)) * gWallTexture.Width) :
                            (int)((vIntersection.X - (float)Math.Floor(vIntersection.X)) * gWallTexture.Width);

                        // Draw the wall slice with perspective correction
                        for (int y = gScreen.Height / 2 - wallHeight; y < gScreen.Height / 2 + wallHeight; y++)
                        {
                            // Calculate the ray position on the wall in world space (-0.5 to 0.5)
                            float rayDirectionY = (y - gScreen.Height / 2) / (float)gScreen.Height;

                            // Apply perspective correction
                            float perspectiveCorrection = rayDirectionY * fDistance / worldWallHeight;

                            // Clamp the perspective correction to valid texture coordinates
                            perspectiveCorrection = Math.Clamp(perspectiveCorrection + verticalTextureOffset, 0, 1);

                            // Sample the texture with the corrected coordinate
                            int y_texture = (int)(perspectiveCorrection * (gWallTexture.Height - 1));

                            pWalls[y, x] = gWallTexture[y_texture, x_texture];
                        }
                    }
                }

                // Screen updates
                gMinimap[(int)ePlayer.Y, (int)ePlayer.X] = ePlayer.Icon;
                gScreen.Impose(gBackground);
                gScreen.Impose(pWalls, allowTransparency: true);
                gScreen.Impose($"FPS: {fpsManager.CurrentFPS} | Player XYA ({ePlayer.X.ToString("0.0", clt)}, {ePlayer.Y.ToString("0.0", clt)}, {ePlayer.Angle.ToString("0.0", clt)}) | Map size: ({gMinimap.Width}, {gMinimap.Height}) | var: {worldWallHeight}");
                gScreen.Impose(gMinimap, y: 1);
                Window.Write(gScreen, doNotChunk: true);
                gScreen.Fill(' ');
            }
            userInputHandler.StopHandlingInput();
        }
    }
}
