using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;
using static Shared.Raycaster;
using Shared;
using Keyboard = MonoGameDemo.Keyboard;
using Color = Microsoft.Xna.Framework.Color;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace MonoGameDemo
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
        private PointEntity ePlayer;
        private List<Entity> entities = [];
        private List<RayIntersection> castResults = [];
        private bool mouseLock = false;
        private Vector2 lastMousePosition = new();
        private float shapeSize = 1;

        public MonoGameApp()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        #region Monogame Methods

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = gridWidth * tileSize;  
            _graphics.PreferredBackBufferHeight = gridHeight * tileSize;
            _graphics.ApplyChanges();

            Window.Title = "Raycasting Vizualizer";
            map = new MapHelper(gridWidth, gridHeight);
            ePlayer = new()
            {
                X = 5,
                Y = 5,
                Angle = -MathF.PI / 2f,
                WalkSpeed = 0.005f,
                ViewDistance = 5,
                FOV = 1
            };

            Raycaster.Init(1, 1);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _debugFont = Content.Load<SpriteFont>("DebugFont");
        }

        protected override void Update(GameTime gameTime)
        {
            if (IsActive) HandleUserInput(gameTime);
            UpdateEntities();
            Raycast();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            int AlmostBlackShade = 50;
            Color AlmostBlack = Color.FromNonPremultiplied(AlmostBlackShade, AlmostBlackShade, AlmostBlackShade, 255);

            DrawBackground(_spriteBatch, AlmostBlack);
            DrawShapes(_spriteBatch, map.Shapes, Color.White);
            DrawEntities(_spriteBatch);
            DrawRays(_spriteBatch, castResults, Color.Crimson);
            DrawPlayer(_spriteBatch, ePlayer, Color.Crimson);

            // drawing shape placement preview
            var mouseState = Mouse.GetState();

            int DarkerGrayShade = 100;
            Color DarkerGray = Color.FromNonPremultiplied(DarkerGrayShade, DarkerGrayShade, DarkerGrayShade, 255);
            DrawShapePlacementPreview(_spriteBatch, new Vector2(mouseState.X, mouseState.Y), DarkerGray);
            //drawing controls description
            string temp = "";
            if (castResults[0].Type == RayIntersectionType.Shape)
            {
                temp = $"I({castResults[0].NormalizedPosition})";
            }
            DrawInfo(_spriteBatch, [
                $"Size: {shapeSize:n1}",
                $"Entities: {entities.Count}" + (entities.Count == 0 ? "" : $", XYA ({entities[0].X:n1}, {entities[0].Y:n1}, {entities[0].Angle:n1})"),
                $"Intersection:" + (castResults.Count == 0 ? "" : $", D ({castResults[0].Distance:n1}), {temp}")
            ]);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        #endregion

        #region Game Logic

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

            if (mouseScreenPos.X is >= 0 and < gridWidth * tileSize && 
                mouseScreenPos.Y is >= 0 and < gridHeight * tileSize)
            {
                shapeSize = Math.Abs((float)(Mouse.GetScrollWheelValue() / (float)10) + 1f);
                if (Mouse.IsButtonDown(Mouse.Button.LeftButton, true))
                {
                    Vector2 mouseGridPosition = mouseScreenPos / tileSize;
                    //Shape shape = new Polygon([
                    //    mouseGridPosition,
                    //        mouseGridPosition + new Vector2(shapeSize, 0),
                    //        mouseGridPosition + new Vector2(shapeSize, shapeSize),
                    //        mouseGridPosition + new Vector2(0, shapeSize)
                    //]);
                    //Shape shape = new Circle(mouseGridPosition, shapeSize);
                    //map.AddShape(shape);

                    BillboardEntity entity = new()
                    {
                        X = mouseGridPosition.X,
                        Y = mouseGridPosition.Y,
                        Angle = ePlayer.Angle + MathF.PI,
                        Width = 1
                    };
                    entities.Add(entity);
                }
                if (Mouse.IsButtonDown(Mouse.Button.RightButton, true))
                {
                    Vector2 mouseGridPosition = mouseScreenPos / tileSize;
                    map.RemoveShape(mouseGridPosition);
                }
            }
        }

        public void UpdateEntities()
        {
            // _staticMap is the map that never changes
            // Map is a copy of _staticMap with added entities in their current state
            //Map = new(_staticMap); // perhaps we can implement a clone method instead of reinitializing the map
            foreach (Entity entity in entities)
            {
                // checking if entity is of type BillboardEntity
                if (entity is BillboardEntity)
                    entity.Angle = entity.Position.AngleTo(ePlayer);
            }
            map.UpdateDynamicMap(entities);
        }

        public void Raycast()
        {
            Vector2 playerScreenPos = ePlayer.Position * tileSize;
            castResults.Clear();
            foreach (IEnumerable<RayIntersection> ray in CastRays(
                map: map,
                origin: ePlayer,
                angle: playerScreenPos.AngleTo(lastMousePosition),
                fov: ePlayer.FOV,
                numRays: 1))
            {
                foreach (RayIntersection step in ray)
                {
                    // if the ray is out of bounds or exceeds the view distance, clamp it
                    if (!map.StaticMap.Validate(step.Position) || step.Distance >= ePlayer.ViewDistance)
                    {
                        // TODO: I cannot modify the step var. Copying it should not be permanent solution
                        RayIntersection clampedStep = step;
                        //step.Distance = ePlayer.ViewDistance;
                        //step.Position = ePlayer + step.Direction * step.Distance;
                        clampedStep.Distance = ePlayer.ViewDistance;
                        clampedStep.Position = ePlayer.Position + step.Direction * clampedStep.Distance;
                        castResults.Add(clampedStep);
                        break;
                    }

                    // if the ray hits a shape, add it to the results and stop casting
                    // TODO: Here we can define different behavior for different shapes
                    if (step.Type == RayIntersectionType.Shape)
                    {
                        castResults.Add(step);
                        break;
                    }

                    continue;
                }
            }
        }

        #endregion

        #region Drawing Methods

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
                    var lines = new string[]
                    {
                        $"XY:{x},{y}",
                        $"S: {map.StaticMap[new Vector2(x, y)].Shapes.Length}",
                        $"E: {map.DynamicMap[new Vector2(x, y)].Shapes.Length}"
                    };

                    for (int i = 0; i < lines.Length; i++)
                    {
                        sb.DrawString(_debugFont, lines[i], new Vector2(x * tileSize + offset, y * tileSize + offset + i*20), color);
                    }
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

        public void DrawPlayer(SpriteBatch sb, PointEntity player, Color color)
        {
            sb.FillCircle(player.Position * tileSize, 5, 16, color);
        }

        public void DrawEntities(SpriteBatch sb)
        {
            Color entityColor = Color.Coral;
            foreach (Entity entity in entities)
                DrawShape(sb, entity.GetShape(), entityColor);
        }

        public void DrawRays(SpriteBatch sb, IEnumerable<RayIntersection> rays, Color color)
        {
            foreach (RayIntersection ray in rays)
            {
                sb.DrawLine(ePlayer.Position * tileSize, ray.Position * tileSize, color, 2);
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
                sb.DrawString(_debugFont, line, new Vector2(0, i * lineHeight), Color.White);
                i++;
            }
        }

        #endregion
    }
}
