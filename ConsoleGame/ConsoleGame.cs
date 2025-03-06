using MonoDebugger;
using System.Globalization;
using static ConsoleGame.Raycaster;

namespace ConsoleGame
{
    class ConsoleGame
    {
        public static float fWallHeight = 1.6f;
        public static float fTextureVerticalOffset = 0.57f;
        public static int iScreenWidth = Console.BufferWidth;
        public static int iScreenHeight = Console.BufferHeight;
        public static CultureInfo cultureInfo = CultureInfo.InvariantCulture;
        public static string mapName = "custom";

        public static CharGrid gScreen;
        public static CharGrid gRaycastLayer;
        public static CharGrid gBgSky;
        public static CharGrid gBgFloor;
        public static CharGrid gWallTexture;
        public static CharGrid gTreeTexture;
        public static MapHelper Map;
        public static ConsoleBufferWriter consoleHandler;
        public static InputHandler inputHandler;
        public static FPSHelper fpsManager;
        public static Entity ePlayer;
        public static bool gameRunning = false;
        public static string currentTarget = "";

        private static void Initialize()
        {
            Console.CursorVisible = false;
            Console.Title = "Console Raycaster";
            gScreen = new CharGrid(iScreenWidth, iScreenHeight);
            gRaycastLayer = new CharGrid(iScreenWidth, iScreenHeight);
            gBgSky = AsciiUtils.GenerateSkyTexture(iScreenWidth, iScreenHeight / 2);
            gBgFloor = AsciiUtils.GenerateFloorTexture(iScreenWidth, iScreenHeight / 2);
            gWallTexture = CharGrid.Load("textures/wall.txt");
            Map = new MapHelper(15, 10).AddBorder();
            consoleHandler = new ConsoleBufferWriter();
            inputHandler = new InputHandler();
            fpsManager = new FPSHelper();
            ePlayer = new(
                x: 1f,
                y: 1f,
                walkSpeed: 2,
                rotationSpeed: 1.5f,
                fov: (float)(Math.PI / 3),
                viewDistance: 100);
        }

        private static void HandleScreenResize()
        {   
            int screenWidth = Console.BufferWidth;
            int screenHeight = Console.BufferHeight;
            if (screenWidth != iScreenWidth || screenHeight != iScreenHeight)
            {
                iScreenWidth = screenWidth;
                iScreenHeight = screenHeight;
                gScreen = new CharGrid(iScreenWidth, iScreenHeight);
                gRaycastLayer = new CharGrid(iScreenWidth, iScreenHeight);
                gBgSky = AsciiUtils.GenerateSkyTexture(iScreenWidth, iScreenHeight / 2);
                gBgFloor = AsciiUtils.GenerateFloorTexture(iScreenWidth, iScreenHeight / 2);
            }
        }

        private static void HandleUserInput()
        {
            fpsManager.Update();
            inputHandler.Update();
            if (inputHandler.IsKeyDown(Keys.Escape))
            {
                gameRunning = false;
            }
            if (inputHandler.IsKeyDown(Keys.W))
            {
                ePlayer.Walk(fpsManager.deltaTime);
            }
            if (inputHandler.IsKeyDown(Keys.S))
            {
                ePlayer.Walk(fpsManager.deltaTime, speedMult: -1f);
            }
            if (inputHandler.IsKeyDown(Keys.Q))
            {
                ePlayer.Walk(fpsManager.deltaTime, angleIncr: (float)(-Math.PI / 2));
            }
            if (inputHandler.IsKeyDown(Keys.E))
            {
                ePlayer.Walk(fpsManager.deltaTime, angleIncr: (float)(Math.PI / 2));
            }
            if (inputHandler.IsKeyDown(Keys.A))
            {
                ePlayer.RotateLeft(fpsManager.deltaTime);
            }
            if (inputHandler.IsKeyDown(Keys.D))
            {
                ePlayer.RotateRight(fpsManager.deltaTime);
            }
        }

        private static void UpdateFrame()
        {
            gRaycastLayer.Fill(AsciiUtils.Air);
            gScreen.Fill(AsciiUtils.Air);
            int x = 0;

            foreach (IEnumerable<RaycastStep> ray in CastRays(Map, ePlayer, gScreen.Width))
            {
                foreach (RaycastStep step in ray)
                {
                    if (step.Distance >= ePlayer.ViewDistance)
                        break;
                    if (step.Shape == null)
                        continue;

                    // fixed fish eye effect for distance traveled
                    float distanceNoFishEye = step.Distance * MathF.Cos(ePlayer.Angle - step.Angle);
                    CharGrid texture = gWallTexture;

                    // If tile is empty, skip
                    if (Map[step.Tile].IsEmpty) continue;

                    // Calculate the wall height based on distance
                    int wallSliceHeight = (int)(gScreen.Height / distanceNoFishEye);
                    //int wallSliceHeight = (int)(gScreen.Height / step.Distance);
                    wallSliceHeight = Math.Clamp(wallSliceHeight, 0, gScreen.Height / 2);

                    // Determine horizontal texture offset depending on which axis a ray
                    int textureSampleX = Math.Abs(step.Position.X - Math.Round(step.Position.X)) < 0.1 ?
                        (int)(step.Position.Y % 1 * texture.Width) :
                        (int)(step.Position.X % 1 * texture.Width);

                    // Draw the wall slice with perspective correction
                    for (int y = gScreen.Height / 2 - wallSliceHeight; y < gScreen.Height / 2 + wallSliceHeight; y++)
                    {
                        // Calculate the ray position on the wall in world space (-0.5 to 0.5)
                        float rayDirectionY = (y - gScreen.Height / 2) / (float)gScreen.Height;

                        // Apply perspective correction
                        float perspectiveCorrection = rayDirectionY * step.Distance / fWallHeight;
                        perspectiveCorrection = Math.Clamp(perspectiveCorrection + fTextureVerticalOffset, 0, 1);

                        int textureSampleY = (int)Math.Round(perspectiveCorrection * (texture.Height - 1), 0);

                        if (gRaycastLayer[y, x] == AsciiUtils.Air)
                            gRaycastLayer[y, x] = texture[textureSampleY, textureSampleX];
                    }
                    break;
                }
                x++;
            }

            // Get what player is aiming at
            currentTarget = "";
            foreach (RaycastStep step in CastRay(Map, ePlayer, ePlayer.Angle))
            {
                if (step.Distance >= ePlayer.ViewDistance || !Map.Map.Validate(step.Tile)) break;
                if (step.Shape == null) continue;
                currentTarget = step.Shape.Name;
                break;
            }
        }

        private static void DrawFrame()
        {
            gScreen.Impose(gBgSky);
            gScreen.Impose(gBgFloor, y: gScreen.Height / 2);
            gScreen.Impose(gRaycastLayer, allowTransparency: true);
            gScreen.Impose($"FPS: {fpsManager.CurrentFPS} | Player XYA ({ePlayer.X.ToString("0.0", cultureInfo)}, {ePlayer.Y.ToString("0.0", cultureInfo)}, {ePlayer.Angle.ToString("0.0", cultureInfo)}) | Screen size ({iScreenWidth}x{iScreenHeight}) | Looking at: {currentTarget}");
            consoleHandler.Write(gScreen, doNotChunk: true);
            gScreen.Fill(' ');
        }

        private static void MainLoop()
        {
            while (gameRunning)
            {
                HandleScreenResize();
                HandleUserInput();
                UpdateFrame();
                DrawFrame();
            }
        }

        public static void Launch()
        {
            Initialize();
            gameRunning = true;
            MainLoop();
        }
    }
}
