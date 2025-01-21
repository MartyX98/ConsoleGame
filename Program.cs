using System.Globalization;

namespace ConsoleGame
{
    public class Program
    {
        public static void Main()
        {
            CharGrid gScreen = new(120, 30); // This is the final layer to be written into the console
            CharGrid gBackgroundLayer = new(gScreen.Width, gScreen.Height); // Holds background texture of floor and sky. Only transforms, but should remain the same.
            CharGrid gRaycastLayer = new(gScreen.Width, gScreen.Height); // Projected results of raycasting. Is updated on every game iteration.
            CharGrid gWallTexture = CharGrid.Load(Path.Combine("textures", "wall.txt"));
            CharGrid gTreeTexture = CharGrid.Load(Path.Combine("textures", "tree.txt"));
            CharGrid gMap = CharGrid.Load(Path.Combine("maps", "test.txt")); // World map to be played on
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
            for (int x = 0; x < gBackgroundLayer.Width; x++)
            {
                for (int y = 0; y < gBackgroundLayer.Height / 2; y++)
                {
                    gBackgroundLayer[y, x] = gRandom.NextSingle() <= fSkyStarDensity ? Icons.Stars[gRandom.Next(0, Icons.Stars.Length)] : Icons.Air;
                }
            }

            // building floor
            for (int x = 0; x < gBackgroundLayer.Width; x++)
            {
                for (int y = 0; y < gBackgroundLayer.Height / 2; y++)
                {
                    gBackgroundLayer[y + gBackgroundLayer.Height / 2, x] = Icons.GetShade((float)y / (gBackgroundLayer.Height / 2 - 1), Icons.ShadingFloor, gRandom.Next(-iFloorTextureRandomOffset, iFloorTextureRandomOffset + 1), flipRange: true);
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
                        gBackgroundLayer.Slide(offsetX: 1);
                        break;

                    // strafe right
                    case ConsoleKey.NumPad9:
                        ePlayer.Walk(fpsManager.deltaTime, angleIncr: (float)(Math.PI / 2));
                        gBackgroundLayer.Slide(offsetX: -1);
                        break;

                    // turn left
                    case ConsoleKey.NumPad4:
                        ePlayer.RotateLeft(fpsManager.deltaTime);
                        gBackgroundLayer.Slide(offsetX: 1);
                        break;

                    // turn right
                    case ConsoleKey.NumPad6:
                        ePlayer.RotateRight(fpsManager.deltaTime);
                        gBackgroundLayer.Slide(offsetX: -1);
                        break;

                    // exit
                    case ConsoleKey.Escape:
                        isRunning = false;
                        break;
                }

                // DDA Raycast loop, 2D to 3D screen projection
                gMinimapLayer.Impose(gMap);
                gRaycastLayer.Fill(Icons.Air);

                foreach ((int x, Raycaster.CastResult castResult) in Raycaster.CastRays(ePlayer, gScreen.Width, gMap, solidObjects, transparentObjects))
                {
                    CharGrid texture;
                    switch (castResult.HitObject)
                    {
                        case Icons.Wall:
                            texture = gWallTexture; break;
                        case Icons.Tree:
                            texture = gTreeTexture; break;
                        default:
                            continue;
                    }

                    // fisheye distortion fix
                    float correctedDistance = castResult.Distance * (float)Math.Cos(((x - gScreen.Width / 2f) / gScreen.Width * ePlayer.FOV) % (Math.PI * 2));
                    gMinimapLayer[castResult.GridIntersection] = Icons.RayCollisionFlag;

                    // Calculate the wall height based on distance
                    int wallSliceHeight = (int)(gScreen.Height / correctedDistance);
                    wallSliceHeight = Math.Clamp(wallSliceHeight, 0, gScreen.Height / 2);

                    // Determine horizontal texture offset
                    int textureX = Math.Round(castResult.ExactIntersection.X, 4) % 1 == 0 ?
                        (int)((castResult.ExactIntersection.Y - (float)Math.Floor(castResult.ExactIntersection.Y)) * texture.Width) :
                        (int)((castResult.ExactIntersection.X - (float)Math.Floor(castResult.ExactIntersection.X)) * texture.Width);

                    // Draw the wall slice with perspective correction
                    for (int y = gScreen.Height / 2 - wallSliceHeight; y < gScreen.Height / 2 + wallSliceHeight; y++)
                    {
                        // Calculate the ray position on the wall in world space (-0.5 to 0.5)
                        float rayDirectionY = (y - gScreen.Height / 2) / (float)gScreen.Height;

                        // Apply perspective correction
                        float perspectiveCorrection = rayDirectionY * correctedDistance / fWallHeight;

                        // Clamp the perspective correction to valid texture coordinates
                        perspectiveCorrection = Math.Clamp(perspectiveCorrection + fTextureVerticalOffset, 0, 1);

                        // Sample the texture with the corrected coordinate
                        int textureY = (int)(perspectiveCorrection * (texture.Height - 1));

                        if (gRaycastLayer[y, x] == Icons.Air)
                            gRaycastLayer[y, x] = texture[textureY, textureX];
                    }
                }

                // Screen updates
                gMinimapLayer[(int)ePlayer.Y, (int)ePlayer.X] = ePlayer.Icon;
                gScreen.Impose(gBackgroundLayer);
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
