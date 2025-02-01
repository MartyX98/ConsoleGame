using System.Collections.Generic;
using System.Globalization;
using static ConsoleGame.Raycaster;

namespace ConsoleGame
{
    public class Program
    {
        public static void Main()
        {
            CharGrid gScreen = new(120, 30); // This is the final layer to be written into the console
            CharGrid gBgSky = new(gScreen.Width, gScreen.Height); // Holds background texture of sky.
            CharGrid gBgFloor = new(gScreen.Width, gScreen.Height); // Holds background texture of floor.
            CharGrid gRaycastLayer = new(gScreen.Width, gScreen.Height); // Projected results of raycasting. Is updated on every game iteration.
            CharGrid gWallTexture = CharGrid.Load(Path.Combine("textures", "wall.txt"));
            CharGrid gTreeTexture = CharGrid.Load(Path.Combine("textures", "debug_texture.txt"));
            CharGrid gMap = CharGrid.Load(Path.Combine("maps", "default.txt")); // World map to be played on
            CharGrid gMinimapLayer = new(gMap); // Copy of world map to be projected as minimap
            char[] solidObjects = [Icons.Wall];
            char[] transparentObjects = [Icons.Tree];
            ConsoleHandler consoleHandler = new();
            FPSManager fpsManager = new();
            UserInputHandler inputHandler = new();
            CultureInfo cultureInfo = CultureInfo.InvariantCulture; // used for some printing for debugging.
            Random gRandom = new();

            float fWallHeight = 1.6f;
            float fTextureVerticalOffset = 0.57f;

            Entity ePlayer = new(
                icon: Icons.Player,
                x: 1f,
                y: 1f,
                walkSpeed: 6,
                rotationSpeed: 3,
                fov: (float)(Math.PI / 3),
                fovDepth: 100);

            int iFloorTextureRandomOffset = 3;
            float fSkyStarDensity = 1f / 40;

            // building sky
            for (int x = 0; x < gBgSky.Width; x++)
            {
                for (int y = 0; y < gBgSky.Height / 2; y++)
                {
                    gBgSky[y, x] = gRandom.NextSingle() <= fSkyStarDensity ? Icons.Stars[gRandom.Next(0, Icons.Stars.Length)] : Icons.Air;
                }
            }

            // building floor
            for (int x = 0; x < gBgFloor.Width; x++)
            {
                for (int y = 0; y < gBgFloor.Height / 2; y++)
                {
                    gBgFloor[y + gBgFloor.Height / 2, x] = Icons.GetShade((float)y / (gBgFloor.Height / 2 - 1), Icons.ShadingFloor, gRandom.Next(-iFloorTextureRandomOffset, iFloorTextureRandomOffset + 1), flipRange: true);
                }
            }

            inputHandler.StartHandlingInputAsync();

            bool isRunning = true;
            while (isRunning)
            {
                fpsManager.Update();

                // Detect user input
                switch (inputHandler.pressedKey)
                {
                    // walk forward
                    case ConsoleKey.NumPad8:
                        ePlayer.Walk(fpsManager.deltaTime);
                        break;

                    // walk backwards
                    case ConsoleKey.NumPad5:
                        ePlayer.Walk(fpsManager.deltaTime, speedMult: -1f);
                        break;

                    // strafe left
                    case ConsoleKey.NumPad7:
                        ePlayer.Walk(fpsManager.deltaTime, angleIncr: (float)(-Math.PI / 2));
                        gBgFloor.Slide(offsetX: 1);
                        break;

                    // strafe right
                    case ConsoleKey.NumPad9:
                        ePlayer.Walk(fpsManager.deltaTime, angleIncr: (float)(Math.PI / 2));
                        gBgFloor.Slide(offsetX: -1);
                        break;

                    // turn left
                    case ConsoleKey.NumPad4:
                        ePlayer.RotateLeft(fpsManager.deltaTime);
                        gBgSky.Slide(offsetX: 1);
                        gBgFloor.Slide(offsetX: 1);
                        break;

                    // turn right
                    case ConsoleKey.NumPad6:
                        ePlayer.RotateRight(fpsManager.deltaTime);
                        gBgSky.Slide(offsetX: -1);
                        gBgFloor.Slide(offsetX: -1);
                        break;

                    // exit
                    case ConsoleKey.Escape:
                        isRunning = false;
                        break;
                }

                gMinimapLayer.Impose(gMap);
                gRaycastLayer.Fill(Icons.Air);

                // DDA Raycast loop, 2D to 3D screen projection

                int x = 0;
                foreach (IEnumerable<RayIntersection> ray in CastRaysWithinFOV(ePlayer, gScreen.Width, gMap))
                {
                    foreach (RayIntersection castResult in ray)
                    {
                        if (castResult.Distance >= ePlayer.viewDistance)
                            break;
                        if (castResult.IntersectedObj == Icons.Air)
                            continue;

                        CharGrid texture;
                        switch (castResult.IntersectedObj)
                        {
                            case Icons.Wall:
                                texture = gWallTexture; break;
                            case Icons.Tree:
                                texture = gTreeTexture; break;
                            default:
                                continue;
                        }

                        gMinimapLayer[castResult.IvIntersection] = Icons.RayCollisionFlag;

                        if (castResult.IntersectedObj == Icons.Tree)
                        {
                            fVector2D spriteOrigin = new(castResult.IvIntersection + 0.5f);
                            float spriteAngleOfOrigin = (float)Math.Atan2(spriteOrigin.Y, spriteOrigin.X) - ePlayer.Angle;
                            float spriteDistance = ePlayer.Distance(spriteOrigin);

                            // Calculate the screen position of the sprite
                            int spriteScreenX = (int)((0.5f * (1 + (float)Math.Tan(spriteAngleOfOrigin - ePlayer.Angle) / ePlayer.FOV) * gScreen.Width));
                            int spriteHeight = (int)(gScreen.Height / spriteDistance);
                            spriteHeight = Math.Clamp(spriteHeight, 0, gScreen.Height);

                            // Draw the sprite
                            for (int y = gScreen.Height / 2 - spriteHeight / 2; y < gScreen.Height / 2 + spriteHeight / 2; y++)
                            {
                                for (int _x = spriteScreenX - spriteHeight / 2; _x < spriteScreenX + spriteHeight / 2; _x++)
                                {
                                    if (_x >= 0 && _x < gScreen.Width && y >= 0 && y < gScreen.Height)
                                    {
                                        int textureSampleX = (_x - (spriteScreenX - spriteHeight / 2)) * gTreeTexture.Width / spriteHeight;
                                        int textureSampleY = (y - (gScreen.Height / 2 - spriteHeight / 2)) * gTreeTexture.Height / spriteHeight;

                                        if (gTreeTexture[textureSampleY, textureSampleX] != Icons.Air)
                                        {
                                            gRaycastLayer[y, _x] = gTreeTexture[textureSampleY, textureSampleX];
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        else if (castResult.IntersectedObj == Icons.Wall)
                        {
                            // Calculate the wall height based on distance
                            int wallSliceHeight = (int)(gScreen.Height / castResult.Distance);
                            wallSliceHeight = Math.Clamp(wallSliceHeight, 0, gScreen.Height / 2);

                            // Determine horizontal texture offset depending on which axis a ray
                            int textureSampleX = Math.Abs(castResult.FvIntersection.X - Math.Round(castResult.FvIntersection.X)) < 0.1 ?
                                (int)(castResult.FvIntersection.Y % 1 * texture.Width) :
                                (int)(castResult.FvIntersection.X % 1 * texture.Width);

                            // Draw the wall slice with perspective correction
                            for (int y = gScreen.Height / 2 - wallSliceHeight; y < gScreen.Height / 2 + wallSliceHeight; y++)
                            {
                                // Calculate the ray position on the wall in world space (-0.5 to 0.5)
                                float rayDirectionY = (y - gScreen.Height / 2) / (float)gScreen.Height;

                                // Apply perspective correction
                                float perspectiveCorrection = rayDirectionY * castResult.Distance / fWallHeight;
                                perspectiveCorrection = Math.Clamp(perspectiveCorrection + fTextureVerticalOffset, 0, 1);

                                int textureSampleY = (int)(perspectiveCorrection * (texture.Height - 1));

                                if (gRaycastLayer[y, x] == Icons.Air)
                                    gRaycastLayer[y, x] = texture[textureSampleY, textureSampleX];
                            }

                            break;
                        }
                    }
                    x++;
                }

                // Screen updates
                gMinimapLayer[(int)ePlayer.Y, (int)ePlayer.X] = ePlayer.Icon;
                gScreen.Impose(gBgSky);
                gScreen.Impose(gBgFloor, allowTransparency: true);
                gScreen.Impose(gRaycastLayer, allowTransparency: true);
                gScreen.Impose($"FPS: {fpsManager.CurrentFPS} | Player XYA ({ePlayer.X.ToString("0.0", cultureInfo)}, {ePlayer.Y.ToString("0.0", cultureInfo)}, {ePlayer.Angle.ToString("0.0", cultureInfo)}) | Map size: ({gMinimapLayer.Width}, {gMinimapLayer.Height})");
                gScreen.Impose(gMinimapLayer, y: 1);
                consoleHandler.Write(gScreen, doNotChunk: true);
                gScreen.Fill(' ');
            }
            inputHandler.StopHandlingInput();
        }
    }
}
