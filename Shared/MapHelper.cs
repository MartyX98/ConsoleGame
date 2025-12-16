using System.Collections.Generic;
using System.Numerics;

namespace Shared
{
    public class MapHelper
    {
        public TileGrid StaticMap { get; private set; }
        public TileGrid DynamicMap { get; private set; }

        public Vector2 Size => new(StaticMap.Width, StaticMap.Height);
        public List<Shape> Shapes { get; private set; } = [];

        public MapHelper(TileGrid map)
        {
            StaticMap = map;
        }

        public MapHelper(int width, int height)
        {
            StaticMap = new TileGrid(width, height);
        }

        #region Helper Methods

        public Tile this[Vector2 v]
        {
            get => StaticMap[(int)v.Y, (int)v.X];
        }

        #endregion

        #region Shape Management

        public MapHelper AddShape(Shape shape)
        {
            if (Shapes.Contains(shape)) return this;
            Shapes.Add(shape);
            // adding shape reference to each tile it intersects
            foreach (Vector2 tilePosition in shape.GetIntersectedTiles())
                if (StaticMap.Validate(tilePosition))
                    StaticMap[tilePosition].Add(shape);
            return this;
        }

        public MapHelper RemoveShape(Shape shape)
        {
            if (!Shapes.Contains(shape)) return this;
            Shapes.Remove(shape);
            // removing shape reference from each tile it intersects
            foreach (Vector2 tilePositioe in shape.GetIntersectedTiles())
                if (StaticMap.Validate(tilePositioe))
                    StaticMap[tilePositioe].Remove(shape);
            return this;
        }

        public MapHelper RemoveShape(Vector2 position)
        {
            Vector2 tilePosition = new((int)position.X, (int)position.Y);
            if (StaticMap[tilePosition].Shapes.Length == 0) return this;
            if (StaticMap[tilePosition].Shapes.Length == 1)
            {
                RemoveShape(StaticMap[tilePosition].Shapes[0]);
                return this;
            }

            int numOfRays = 100;
            Shape candidate = null;
            float minDistance = float.MaxValue;
            for (int i = 0; i < numOfRays; i++)
            {
                float angle = float.Pi * 2 / numOfRays * i;
                foreach (Shape shape in StaticMap[tilePosition].Shapes)
                {
                    if (shape.CheckIntersection(position, angle, out float distance, out float normPos) && distance < minDistance)
                    {
                        candidate = shape;
                        minDistance = distance;
                    }
                }
            }

            if (candidate != null)
                RemoveShape(candidate);

            return this;
        }

        #endregion

        #region Entity Management

        public MapHelper UpdateDynamicMap(List<Entity> entities)
        {
            DynamicMap = new TileGrid(StaticMap.Width, StaticMap.Height);
            foreach (Entity entity in entities)
            {
                Shape shape = entity.GetShape();
                foreach (Vector2 tilePosition in shape.GetIntersectedTiles())
                    if (DynamicMap.Validate(tilePosition))
                        DynamicMap[tilePosition].Add(shape);
            }
            return this;
        }

        #endregion

        #region Map Generation

        public MapHelper AddBorder(float inwardsOffset = 0.001f, string textureName = "", TextureMappingType mappingType = TextureMappingType.Repeat_FromLeft, bool isSolid = true)
        {
            LineSegment top = new(new(inwardsOffset, inwardsOffset), new(StaticMap.Width - inwardsOffset, inwardsOffset), "Wall") { TextureName = textureName, TextureMapping = mappingType };
            LineSegment right = new(new(StaticMap.Width - inwardsOffset, inwardsOffset), new(StaticMap.Width - inwardsOffset, StaticMap.Height - inwardsOffset), "Wall") { TextureName = textureName, TextureMapping = mappingType };
            LineSegment bottom = new(new(StaticMap.Width - inwardsOffset, StaticMap.Height - inwardsOffset), new(inwardsOffset, StaticMap.Height - inwardsOffset), "Wall") { TextureName = textureName, TextureMapping = mappingType };
            LineSegment left = new(new(inwardsOffset, StaticMap.Height - inwardsOffset), new(inwardsOffset, inwardsOffset), "Wall") { TextureName = textureName, TextureMapping = mappingType };
            top.IsSolid = isSolid;
            right.IsSolid = isSolid;
            bottom.IsSolid = isSolid;
            left.IsSolid = isSolid;
            AddShape(top);
            AddShape(right);
            AddShape(bottom);
            AddShape(left);
            return this;
        }

        #endregion
    }
}