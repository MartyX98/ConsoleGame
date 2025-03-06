using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static ConsoleGame.Raycaster;
using ConsoleGame;
using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;
using System.Reflection.Metadata.Ecma335;

namespace MonoDebugger
{
    public class MonoGameApp : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _debugFont;
        const int gridWidth = 15;
        const int gridHeight = 10;
        const int tileSize = 75;
        private MapHelper map;
        private Entity ePlayer;
        private List<RaycastStep> castResults;
        private bool mouseLock = false;
        private Vector2 lastMousePosition;
        private float shapeSize = 1;

        public MonoGameApp()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            // Set the window size
            Console.WriteLine("Initializing Raycasting Vizualizer");
            _graphics.PreferredBackBufferWidth = gridWidth * tileSize;  
            _graphics.PreferredBackBufferHeight = gridHeight * tileSize;
            _graphics.ApplyChanges();

            Window.Title = "Raycasting Vizualizer";
            map = new MapHelper(gridWidth, gridHeight);
            ePlayer = new(
                x: 5,
                y: 5,
                angle: (float)(-Math.PI / 2),
                walkSpeed: 0.005f,
                viewDistance: 5,
                fov: 1
            );

            castResults = [];
            lastMousePosition = new();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _debugFont = Content.Load<SpriteFont>("DebugFont");
        }

        protected override void Update(GameTime gameTime)
        {
            // Mouse and Keyboard input
            if (IsActive) HandleUserInput(gameTime);

            // Raycasting
            Vector2 playerScreenPos = ePlayer * tileSize;
            castResults.Clear();
            foreach (IEnumerable<RaycastStep> ray in CastRays(
                map: map,
                origin: ePlayer,
                angle: playerScreenPos.AngleTo(lastMousePosition),
                fov: ePlayer.FOV,
                numRays: 1000))
            {
                foreach (RaycastStep step in ray)
                {
                    // if the ray is out of bounds or exceeds the view distance, clamp it
                    if (!map.Map.Validate(step.Tile) || step.Distance >= ePlayer.ViewDistance)
                    {
                        step.Distance = ePlayer.ViewDistance;
                        step.Position = ePlayer + step.Direction * step.Distance;
                        castResults.Add(step);
                        break;
                    }

                    // if the ray hits a shape, add it to the results and stop casting
                    // TODO: Here we can define different behavior for different shapes
                    if (step.Shape != null)
                    {
                        castResults.Add(step);
                        break;
                    }

                    continue;
                }
            }

            base.Update(gameTime);
        }

        public void HandleUserInput(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            Keyboard.GetState();
            var mState = Mouse.GetState();
            Vector2 mouseScreenPos = new(mState.X, mState.Y);

            if (!mouseLock)
                lastMousePosition = mouseScreenPos;

            if (Keyboard.IsKeyDown(Keys.Escape))
                Exit();
            if (Keyboard.IsKeyDown(Keys.W))
                ePlayer.Walk(elapsed);
            if (Keyboard.IsKeyDown(Keys.S))
                ePlayer.Walk(elapsed, angleIncr: (float)Math.PI);
            if (Keyboard.IsKeyDown(Keys.A))
                ePlayer.Walk(elapsed, angleIncr: (float)(-Math.PI / 2));
            if (Keyboard.IsKeyDown(Keys.D))
                ePlayer.Walk(elapsed, angleIncr: (float)(Math.PI / 2));
            //if (Keyboard.IsKeyDown(Keys.L, true))
            //    gMap = FileHelper.LoadMap(mapName);
            //if (Keyboard.IsKeyDown(Keys.P, true))
            //    FileHelper.SaveMap(gMap, mapName);
            if (Keyboard.IsKeyDown(Keys.M, true))
                mouseLock = !mouseLock;

            if (mouseScreenPos.X >= 0 && mouseScreenPos.X < gridWidth * tileSize && mouseScreenPos.Y >= 0 && mouseScreenPos.Y < gridHeight * tileSize)
            {
                shapeSize = Math.Abs((float)(Mouse.GetScrollWheelValue() / (float)10));
                if (Mouse.IsButtonDown(Mouse.Button.LeftButton, true))
                {
                    Vector2 mouseGridPosition = mouseScreenPos / tileSize;
                    //Shape shape = new Polygon([
                    //    mouseGridPosition,
                    //        mouseGridPosition + new Vector2(1, 0),
                    //        mouseGridPosition + new Vector2(1, 1),
                    //        mouseGridPosition + new Vector2(0, 1)
                    //]);
                    Shape shape = new Circle(mouseGridPosition, shapeSize);
                    map.AddShape(shape);
                }
                if (Mouse.IsButtonDown(Mouse.Button.RightButton, true))
                {
                    Vector2 mouseGridPosition = mouseScreenPos / tileSize;
                    map.RemoveShape(mouseGridPosition);
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            int AlmostBlackShade = 50;
            Color AlmostBlack = Color.FromNonPremultiplied(AlmostBlackShade, AlmostBlackShade, AlmostBlackShade, 255);

            DrawBackground(_spriteBatch, AlmostBlack);
            DrawShapes(_spriteBatch, map.Shapes, Color.White);
            DrawRays(_spriteBatch, castResults, Color.Crimson);
            DrawPlayer(_spriteBatch, ePlayer, Color.Crimson);

            // drawing shape placement preview
            var mouseState = Mouse.GetState();

            int DarkerGrayShade = 100;
            Color DarkerGray = Color.FromNonPremultiplied(DarkerGrayShade, DarkerGrayShade, DarkerGrayShade, 255);
            DrawShapePlacementPreview(_spriteBatch, new Vector2(mouseState.X, mouseState.Y), DarkerGray);

            //drawing controls description
            DrawInfo(_spriteBatch, [
                $"L to lock focus ({mouseLock})",
                $"Size: {shapeSize:n1}",
            ]);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    
        public void DrawBackground(SpriteBatch sb, Color color)
        {
            // Drawing grid lines
            for (int x = 0; x < gridWidth; x++)
                sb.DrawLine(new Vector2(x, 0) * tileSize, new Vector2(x, gridHeight) * tileSize, color);

            for (int y = 0; y < gridHeight; y++)
                sb.DrawLine(new Vector2(0, y) * tileSize, new Vector2(gridWidth, y) * tileSize, color);

            // drawing cell coords in cell's upper left corner
            int offset = 3;
            for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                sb.DrawString(_debugFont, $"{x},{y}", new Vector2(x * tileSize, y * tileSize).Add(offset), color);
                // writing the number of shapes in the cell
                int shapesInCell = map[new Vector2(x, y)].Shapes.Length;
                Vector2 shapesInCellV = new Vector2(x * tileSize, y * tileSize).Add(offset);
                shapesInCellV.Y += 20;
                sb.DrawString(_debugFont, $"S: {shapesInCell}", shapesInCellV, color);
            }
        }
        
        public void DrawShape(SpriteBatch sb, Shape shape, Color color)
        {
            if (shape is LineSegment line)
                sb.DrawLine(line.A * tileSize, line.B * tileSize, color);
            else if (shape is Polygon polygon)
                foreach (LineSegment segment in polygon.Segments)
                    sb.DrawLine(segment.A * tileSize, segment.B * tileSize, color);
            else if (shape is Circle circle)
                sb.DrawCircle(circle.Center * tileSize, circle.Radius * tileSize, 32, color);
        }

        public void DrawShapes(SpriteBatch sb, List<Shape> shapes, Color color)
        {
            // Drawing shapes
            foreach (Shape shape in shapes)
                DrawShape(sb, shape, color);
        }

        public void DrawPlayer(SpriteBatch sb, Entity player, Color color)
        {
            sb.FillCircle(new Vector2(player.X, player.Y) * tileSize, 5, 16, color);
        }

        public void DrawRays(SpriteBatch sb, IEnumerable<RaycastStep> rays, Color color)
        {
            foreach (RaycastStep ray in rays)
            {
                sb.DrawLine((Vector2)(ePlayer * tileSize), ray.Position * tileSize, color, 2);
            }
        }

        public void DrawShapePlacementPreview(SpriteBatch sb, Vector2 mouseScreenPos, Color color)
        {
            Vector2 mouseGridPosition = mouseScreenPos / tileSize;
            //Shape shape = new Polygon([
            //    mouseGridPosition,
            //        mouseGridPosition + new Vector2(1, 0),
            //        mouseGridPosition + new Vector2(1, 1),
            //        mouseGridPosition + new Vector2(0, 1)
            //]);
            Shape shape = new Circle(mouseGridPosition, shapeSize);
            DrawShape(sb, shape, color);

        }

        public void DrawInfo(SpriteBatch sb, string[] lines)
        {
            int lineHeight = 25;
            int i = 0;
            foreach (string line in lines)
            {
                _spriteBatch.DrawString(_debugFont, line, new Vector2(0, i * lineHeight), Color.White);
                i++;
            }
        }
    }
}
