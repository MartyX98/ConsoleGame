using Shared;
using Sharpie;
using Sharpie.Backend;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using WindowsConsoleShared;
using static Shared.Raycaster;
using TerminalSharpie = Shared.TerminalSharpie;

namespace ConsoleGameDemo
{
    class ConsoleGame
    {
        public static float fTextureVerticalOffset = 0.57f;
        public static int iScreenWidth = Console.BufferWidth;
        public static int iScreenHeight = Console.BufferHeight;
        public static int newScreenWidth = Console.BufferWidth;
        public static int newScreenHeight = Console.BufferHeight;
        public static CultureInfo cultureInfo = CultureInfo.InvariantCulture;
        public static CursesBackendFlavor flavor = CursesBackendFlavor.Any;
        public static string mapName = "custom";
        public static string texturesDirectory = "textures";

        public static DisplayGrid screenBufferMain;
        public static DisplayGrid screenBufferShapes;
        public static DisplayGrid screenBufferBackground;
        public static Grid<Pixel> textureFloor;
        public static Grid<Pixel> textureCeiling;
        public static MapHelper Map;
        public static Textures textures;
        public static TerminalSharpie terminal;
        public static Keyboard keyboard;
        public static FPSHelper fpsHelper;
        public static PointEntity playerEntity;
        public static List<Entity> entities = [];
        public static List<string> logs = [];
        public static int logsCapacity = 4;
        public static bool gameRunning = false;
        public static Shape? currentTarget;
        public static bool tempToggle = false;
        private static void Initialize()
        {
            // screen initialization
            Console.Title = "Console Raycaster";
            Console.WriteLine("Resize the console window to your liking and press any key to start the game.");
            Console.ReadKey(true);
            Console.CursorVisible = false;
            iScreenWidth = Console.BufferWidth;
            iScreenHeight = Console.BufferHeight;
            terminal = new();

            // screen drawing buffers initialization
            screenBufferMain = new(iScreenWidth, iScreenHeight);
            screenBufferShapes = new(iScreenWidth, iScreenHeight);
            screenBufferBackground = new(iScreenWidth, iScreenHeight);

            // textures initialization
            textures = new Textures(texturesDirectory, terminal.colorLookup);
            textures.TryGetTexture("floor_road", out Texture _textureFloor);
            textureFloor = _textureFloor.GetMipmap(_textureFloor.LargestMipmapSize);
            //textures.TryGetTexture("ceiling_dots", out Texture _textureCeiling);
            Texture _textureCeiling = textures.GenerateEmptyTexture(0, 0, 0);
            textureCeiling = _textureCeiling.GetMipmap(_textureCeiling.LargestMipmapSize);
            Style yellow = textures.GetClosestStyle(new Rgba32(193, 156, 0, 255));
            Style black = textures.GetClosestStyle(new Rgba32(0, 0, 0, 255));
            terminal.defaultStyle = yellow;
            terminal.defaultChar = ' ';
            terminal.defaultPixel = default;

            // other
            keyboard = new Keyboard();
            fpsHelper = new FPSHelper();
            playerEntity = new()
            {
                X = 1f,
                Y = 1f,
                WalkSpeed = 2,
                RotationSpeed = 1.5f,
                FOV = MathF.PI / 3f,
                ViewDistance = 15,
            };

            Raycaster.Init(iScreenWidth, (int)playerEntity.ViewDistance);
            //Raycaster.Init(1, 1);

            // map initialization
            int mapSize = 25;
            Map = new MapHelper(mapSize, mapSize);
            Map.AddBorder(textureName: "fence", isSolid: false);

            // generating bunch of trees on the map
            Random rng = new();
            int treeCount = 50;
            for (int i = 0; i < treeCount; i++)
            {
                int Xt = rng.NextSingle() < 0.5 ? -1 : 1;
                int Yt = rng.NextSingle() < 0.5 ? -1 : 1;
                BillboardEntity tree = new()
                {
                    Name = "Tree",
                    //X = (float)(1 + rng.NextDouble() * (Map.StaticMap.Width - 2)),
                    //Y = (float)(1 + rng.NextDouble() * (Map.StaticMap.Height - 2)),
                    X = (float)(1 + (0.5 + Xt * (0.2 + rng.NextSingle() * 0.3)) * (Map.StaticMap.Width - 2)),
                    Y = (float)(1 + (0.5 + Yt * (0.2 + rng.NextSingle() * 0.3)) * (Map.StaticMap.Height - 2)),
                    Width = 1f,
                    TextureName = "tree",
                    MappingType = TextureMappingType.Stretch,
                };
                entities.Add(tree);
            }

            // generating couple of pillars on the map
            int pillarCount = 5;
            for (int i = 0; i < pillarCount; i++)
            {
                int Xt = rng.NextSingle() < 0.5 ? -1 : 1;
                float X = (float)(1 + (0.5 + Xt * (0.1 + rng.NextSingle() * 0.1)) * (Map.StaticMap.Width - 2));
                int Yt = rng.NextSingle() < 0.5 ? -1 : 1;
                float Y = (float)(1 + (0.5 + Yt * (0.1 + rng.NextSingle() * 0.1)) * (Map.StaticMap.Height - 2));
                Circle pillar = new(
                    center: new(X, Y),
                    radius: 0.2f,
                    name: "Pillar")
                {
                    TextureName = "pillar_text",
                    TextureMapping = TextureMappingType.Stretch,
                    IsSolid = false
                };
                Map.AddShape(pillar);
            }

            Square square = new(
                center: new(mapSize / 2, mapSize / 2), 2)
            {
                TextureName = "wall_smile",
                TextureMapping = TextureMappingType.Stretch,
            };
            Map.AddShape(square);
        }

        // TODO: Implement this method to handle screen resizing with Sharpie?
        private static void HandleScreenResize()
        {
            //newScreenWidth = Console.WindowWidth;
            //newScreenHeight = Console.WindowHeight;
            //if (screenWidth != iScreenWidth || screenHeight != iScreenHeight)
            //{
            //    Debug.WriteLine($"Handling screen resize from {iScreenWidth}x{iScreenHeight} to {screenWidth}x{screenHeight}");
            //    iScreenWidth = screenWidth;
            //    iScreenHeight = screenHeight;
            //    screenBufferMain = new(iScreenWidth, iScreenHeight);
            //    screenBufferShapes = new(iScreenWidth, iScreenHeight);
            //    terminal.window.Destroy();
            //    terminal.terminal.Screen.Destroy();
            //    terminal.Dispose();
            //    terminal = new(flavor);
            //}
        }

        private static void HandleUserInput()
        {
            keyboard.Update();
            if (keyboard.IsKeyDown(Keys.Escape, true)) // Exit game
            {
                gameRunning = false;
            }
            if (keyboard.IsKeyDown(Keys.W)) // Move player forward
            {
                playerEntity.Walk(fpsHelper.deltaTime);
            }
            if (keyboard.IsKeyDown(Keys.S)) // Move player backward
            {
                playerEntity.Walk(fpsHelper.deltaTime, speedMult: -1f);
            }
            if (keyboard.IsKeyDown(Keys.A)) // Rotate player left
            {
                playerEntity.RotateLeft(fpsHelper.deltaTime);
            }
            if (keyboard.IsKeyDown(Keys.D)) // Rotate player right
            {
                playerEntity.RotateRight(fpsHelper.deltaTime);
            }
            if (keyboard.IsKeyDown(Keys.Q)) // Strafe left
            {
                playerEntity.Walk(fpsHelper.deltaTime, angleIncr: (float)(-Math.PI / 2));
            }
            if (keyboard.IsKeyDown(Keys.E)) // Strafe right
            {
                playerEntity.Walk(fpsHelper.deltaTime, angleIncr: (float)(Math.PI / 2));
            }
            if (keyboard.IsKeyDown(Keys.R, true)) // Reset player position
            {
                playerEntity.X = 1f;
                playerEntity.Y = 1f;
            }
            if (keyboard.IsKeyDown(Keys.F, true)) // spawn entity
            {
                Vector2 pos = playerEntity.Position - playerEntity.Direction * 2;
                BillboardEntity entity = new()
                {
                    X = pos.X,
                    Y = pos.Y,
                    Angle = playerEntity.Angle + MathF.PI / 2,
                    Width = 1f,
                    TextureName = "monster",
                    MappingType = TextureMappingType.Stretch,
                };
                entities.Add(entity);
                logs.Add($"Entity spawned at {pos.X.ToString("0.0", cultureInfo)}, {pos.Y.ToString("0.0", cultureInfo)}");
            }

            if (keyboard.IsKeyDown(Keys.T, true)) // toggle debug info
            {
                tempToggle = !tempToggle;
            }
        }

        private static void Raycast()
        {
            screenBufferShapes.Fill(terminal.defaultPixel, terminal.defaultChar);
            screenBufferBackground.Fill(terminal.defaultPixel, terminal.defaultChar);
            
            // Main raycasting loop
            int x = 0;
            foreach (IEnumerable<RayIntersection> ray in CastRays(Map, playerEntity, screenBufferMain.Width))
            {
                Vector2 lastTileIntersectionPos = playerEntity.Position;
                int lastTileIntersRoomHalfHeight = screenBufferMain.Height / 2;
                bool stopAfterTileIntersection = false;
                foreach (RayIntersection step in ray)
                {
                    if (step.Distance >= playerEntity.ViewDistance)
                        break;
                    if (!Map.StaticMap.Validate((Vector2)step.Position, 1))
                        continue;

                    // Calculate the wall height based on distance & fixed fish eye effect for distance traveled
                    float distanceNoFishEye = step.Distance * MathF.Cos(playerEntity.Angle - step.Angle);
                    int roomHalfHeight = (int)Math.Clamp(screenBufferMain.Height / distanceNoFishEye, 0, screenBufferMain.Height / 2);

                    if (step.Type == RayIntersectionType.Tile)
                    {
                        int roomHalfHeightDiff = lastTileIntersRoomHalfHeight - roomHalfHeight;
                        Vector2 lastFloorTxtPoint = new(
                            x: (int)Math.Clamp(lastTileIntersectionPos.X / (Map.Size.X - 1) * (float)(textureFloor.Width - 1), 0, textureFloor.Width - 1),
                            y: (int)Math.Clamp(lastTileIntersectionPos.Y / (Map.Size.Y - 1) * (textureFloor.Height - 1), 0, textureFloor.Height - 1)
                            );
                        Vector2 currentFloorTxtPoint = new(
                            x: (int)Math.Clamp(step.Position.X / (Map.Size.X - 1) * (textureFloor.Width - 1), (float)0, (float)(textureFloor.Width - 1)),
                            y: (int)Math.Clamp(step.Position.Y / (Map.Size.Y - 1) * (textureFloor.Height - 1), (float)0, (float)(textureFloor.Height - 1))
                            );
                        Vector2 lastCeilingTxtPoint = new(
                            x: (int)Math.Clamp(lastTileIntersectionPos.X / (Map.Size.X - 1) * (textureCeiling.Width - 1), 0, textureCeiling.Width - 1),
                            y: (int)Math.Clamp(lastTileIntersectionPos.Y / (Map.Size.Y - 1) * (textureCeiling.Height - 1), 0, textureCeiling.Height - 1)
                            );
                        Vector2 currentCeilingTxtPoint = new(
                            x: (int)Math.Clamp(step.Position.X / (Map.Size.X - 1) * (textureCeiling.Width - 1), (float)0, (float)(textureCeiling.Width - 1)),
                            y: (int)Math.Clamp(step.Position.Y / (Map.Size.Y - 1) * (textureCeiling.Height - 1), (float)0, (float)(textureCeiling.Height - 1))
                            );

                        float floorDist = lastFloorTxtPoint.Distance(currentFloorTxtPoint) * MathF.Cos(playerEntity.Angle - step.Angle);
                        float ceilingDist = lastCeilingTxtPoint.Distance(currentCeilingTxtPoint) * MathF.Cos(playerEntity.Angle - step.Angle);
                        float gridDist = lastTileIntersectionPos.Distance((Vector2)step.Position) * MathF.Cos(playerEntity.Angle - step.Angle);
                        float floorStepSize = roomHalfHeight >= 1 ? floorDist / roomHalfHeightDiff : 1;
                        float ceilingStepSize = roomHalfHeight >= 1 ? ceilingDist / roomHalfHeightDiff : 1;
                        float gridStepSize = roomHalfHeight >= 1 ? gridDist / roomHalfHeightDiff : 1;

                        for (int i = 0; i < roomHalfHeightDiff; i++)
                        {
                            Vector2 floorPixelCoords = currentFloorTxtPoint - step.Direction * i * floorStepSize;
                            Vector2 ceilingPixelCoords = currentCeilingTxtPoint - step.Direction * i * ceilingStepSize;
                            int floorPixelX = (int)Math.Clamp(MathF.Round(floorPixelCoords.X), 0, textureFloor.Width - 1);
                            int floorPixelY = (int)Math.Clamp(MathF.Round(floorPixelCoords.Y), 0, textureFloor.Height - 1);
                            int ceilingPixelX = (int)Math.Clamp(MathF.Round(ceilingPixelCoords.X), 0, textureCeiling.Width - 1);
                            int ceilingPixelY = (int)Math.Clamp(MathF.Round(ceilingPixelCoords.Y), 0, textureCeiling.Height - 1);
                            Pixel floorPixel = new(textureFloor[floorPixelY, floorPixelX]);
                            Pixel ceilingPixel = new(textureCeiling[ceilingPixelY, ceilingPixelX]);

                            //shading based on distance
                            Vector2 currentGridPos = lastTileIntersectionPos - step.Direction * i * gridStepSize;
                            float currentGridDist = playerEntity.Position.Distance(currentGridPos) * MathF.Cos(playerEntity.Angle - step.Angle);
                            float distNorm = Math.Clamp(currentGridDist / playerEntity.ViewDistance, 0, 1);
                            char pixelChar = distNorm switch
                            {
                                > 0.5f => '░',
                                > 0.4f => '▒',
                                > 0.3f => '▓',
                                _ => '█',
                            };

                            int floorScreenY = screenBufferMain.Height / 2 + roomHalfHeight + i;
                            int ceilingScreenY = screenBufferMain.Height / 2 - roomHalfHeight - i;
                            if (screenBufferBackground.Validate(floorScreenY, x))
                                //screenBufferBackground[floorScreenY, x] = floorPixel;
                                screenBufferBackground.SetPixel(floorPixel, pixelChar, x, floorScreenY);
                            if (screenBufferBackground.Validate(ceilingScreenY, x))
                                //screenBufferBackground[ceilingScreenY, x] = ceilingPixel;
                                screenBufferBackground.SetPixel(ceilingPixel, pixelChar, x, ceilingScreenY);
                        }

                        lastTileIntersectionPos = step.Position;
                        lastTileIntersRoomHalfHeight = roomHalfHeight;

                        if (stopAfterTileIntersection)
                            break;
                    }

                    if (step.Type == RayIntersectionType.Shape && 
                        step.Shape.TextureMapping != TextureMappingType.Invisible)
                    {
                        textures.TryGetTexture(step.Shape.TextureName, out Texture texture);
                        Vector2 textureSize = new(
                            x: (int)MathF.Round(roomHalfHeight * 2 * texture.Ratio),
                            y: roomHalfHeight * 2
                            );
                        //Grid<Pixel> textureMipmap = texture.GetMipmap(textureSize);
                        Grid<Pixel> textureMipmap = texture.GetMipmap(texture.LargestMipmapSize);

                        // Calculate the texture sample X position based on the texture mapping type
                        float repeatedPos = (step.Shape.Length * step.NormalizedPosition).Mod(1);
                        float normalizedPosition = step.Shape.TextureMapping switch
                        {
                            TextureMappingType.Stretch => step.NormalizedPosition,
                            TextureMappingType.Repeat_FromLeft => repeatedPos,
                            TextureMappingType.Repeat_FromRight => 1 - repeatedPos,
                            TextureMappingType.Repeat_Centered => repeatedPos + step.Shape.Length.Mod(1) / 2f,
                            _ => step.NormalizedPosition
                        };

                        int textureSampleX = (int)Math.Round(normalizedPosition * (textureMipmap.Width - 1), 0);
                        // Draw the wall slice with perspective correction
                        for (int y = screenBufferMain.Height / 2 - roomHalfHeight; y < screenBufferMain.Height / 2 + roomHalfHeight; y++)
                        {
                            // Calculate the ray position on the wall in world space (-0.5 to 0.5)
                            float rayDirectionY = (y - screenBufferMain.Height / 2) / (float)screenBufferMain.Height;

                            // Apply perspective correction
                            float perspectiveCorrection = rayDirectionY * distanceNoFishEye / 1.6f;
                            perspectiveCorrection = Math.Clamp(perspectiveCorrection + fTextureVerticalOffset, 0, 1);

                            int textureSampleY = (int)MathF.Floor(perspectiveCorrection * (textureMipmap.Height - 1));
                            Pixel pixel = new(textureMipmap[textureSampleY, textureSampleX]);
                            if (pixel.rgba32.A == 0)
                                continue;

                            // shading based on distance 
                            float distNorm = Math.Clamp(step.Distance / playerEntity.ViewDistance, 0, 1);
                            char pixelChar = distNorm switch
                            {
                                > 0.5f => '░',
                                > 0.4f => '▒',
                                > 0.3f => '▓',
                                _ => '█',
                            };

                            if (screenBufferShapes[y, x] == terminal.defaultPixel)
                                screenBufferShapes.SetPixel(pixel, pixelChar, x, y);
                        }
                        if (step.Shape.IsSolid)
                            stopAfterTileIntersection = true;
                        continue;
                    }
                }
                x++;
            }

            // Get what player is aiming at
            currentTarget = null;
            foreach (RayIntersection step in CastRay(Map, playerEntity, playerEntity.Angle))
            {
                if (step.Distance >= playerEntity.ViewDistance) break;
                if (!Map.StaticMap.Validate(step.Position) || step.Type != RayIntersectionType.Shape) continue;
                if (step.Shape == null) continue;
                currentTarget = step.Shape;
                break;
            }
        }

        public static void UpdateEntities()
        {
            foreach (Entity entity in entities)
            {
                // checking if entity is of type BillboardEntity
                if (entity is BillboardEntity)
                    entity.Angle = entity.Position.AngleTo(playerEntity.Position);
            }
            Map.UpdateDynamicMap(entities);
        }

        private static void DrawFrame()
        {
            screenBufferMain.Fill(terminal.defaultPixel, terminal.defaultChar);
            screenBufferMain.Impose(
                grid: screenBufferBackground
                );
            screenBufferMain.Impose(
                grid: screenBufferShapes,
                predicate: a => a.rgba32.A != 0
                );
            terminal.Write(screenBufferMain);

            //Draw debug info to the left of the screen
            if (tempToggle)
            {
                string[] debugInfo =
                [
                    $"FPS: {fpsHelper.CurrentFPS}",
                    $"Player XYA ({playerEntity.X.ToString("0.0", cultureInfo)}, {playerEntity.Y.ToString("0.0", cultureInfo)}, {playerEntity.Angle.ToString("0.0", cultureInfo)})",
                    $"Screen size ({iScreenWidth}x{iScreenHeight})",
                    $"New screen size ({newScreenWidth}x{newScreenHeight})",
                    $"Aiming at: " + (currentTarget == null ? "" : $"{currentTarget.TextureName}, {currentTarget.TextureMapping}"),
                    $"Entities: {entities.Count}",
                    $"Terminal mode: {terminal.terminal.Description}",
                    $"Colors available: {terminal.ColorCount}",
                    $"Temp toggle: {tempToggle}",
                ];
                for (int i = 0; i < debugInfo.Length; i++)
                {
                    terminal.Write(debugInfo[i], 0, i, terminal.defaultStyle);
                }

                // Draw logs to the right of the screen
                for (int i = 0; i < logs.Count; i++)
                {
                    int j = logs.Count - i - 1;
                    terminal.Write(logs[j], iScreenWidth - logs[j].Length, i, terminal.defaultStyle);
                }

                //// draw available colors to lower left corner
                for (int i = 0; i < terminal.colorLookup.Count; i++)
                {
                    if (i >= iScreenWidth) break;
                    terminal.Write("█", i, iScreenHeight - 1, terminal.colorLookup.Values.ElementAt(i));
                }

                // draw row indexes to the right of the screen
                for (int i = 0; i < iScreenHeight - 1; i++)
                {
                    terminal.Write(i.ToString(), iScreenWidth - 2, i, terminal.defaultStyle);
                }
            }


            terminal.Update();
        }

        private static void BeforeDrawingActions()
        {
            while (logs.Count > logsCapacity)
                logs.RemoveAt(0);
        }

        public static void Launch()
        {
            Initialize();
            gameRunning = true;
            while (gameRunning)
            {
                fpsHelper.Update();
                HandleScreenResize();
                HandleUserInput();
                UpdateEntities();
                Raycast();
                BeforeDrawingActions();
                DrawFrame();
            }
        }
    }
}
