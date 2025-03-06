using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
namespace ConsoleGame
{
    public class MapHelper
    {
        public TileGrid Map { get; private set; }
        public Vector2 Size => new(Map.Width, Map.Height);
        public List<Shape> Shapes { get; private set; } = [];

        public MapHelper(TileGrid map) => Map = map;
        public MapHelper(int width, int height) => Map = new(width, height);

        public MapHelper AddShape(Shape shape)
        {
            if (Shapes.Contains(shape)) return this;
            Shapes.Add(shape);
            // adding shape reference to each tile it intersects
            foreach (Vector2 tile in shape.GetIntersectedTiles())
                if (Map.Validate(tile))
                    Map[tile].Add(shape);

            return this;
        }

        public MapHelper RemoveShape(Shape shape)
        {
            if (!Shapes.Contains(shape)) return this;
            Shapes.Remove(shape);
            // removing shape reference from each tile it intersects
            foreach (Vector2 tile in shape.GetIntersectedTiles())
                if (Map.Validate(tile))
                    Map[tile].Remove(shape);
            return this;
        }

        public MapHelper RemoveShape(Vector2 position)
        {
            Vector2 tilePosition = new((int)position.X, (int)position.Y);
            foreach (Shape shape in Shapes)
                foreach (Vector2 tile in shape.GetIntersectedTiles())
                    if (tile == tilePosition)
                    {
                        RemoveShape(shape);
                        return this;
                    }
            return this;
        }

        public Tile this[Vector2 v]
        {
            get => Map[(int)v.Y, (int)v.X];
            //set => Map[(int)v.X, (int)v.Y] = value;
        }
    }

    public static class MapHelperExtensions
    {
        public static MapHelper AddBorder(this MapHelper map, float offset = 0.1f)
        {
            LineSegment north = new(new(offset, offset), new(map.Size.X - offset, offset));
            LineSegment east = new(new(map.Size.X - offset, offset), new(map.Size.X - offset, map.Size.Y - offset));
            LineSegment south = new(new(map.Size.X - offset, map.Size.Y - offset), new(offset, map.Size.Y - offset));
            LineSegment west = new(new(offset, map.Size.Y - offset), new(offset, offset));
            map.AddShape(north);
            map.AddShape(east);
            map.AddShape(south);
            map.AddShape(west);
            return map;
        }
    }
}