using ConsoleGame;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;


namespace MonoDebugger
{
    public class FileHelper
    {
        public static readonly bool AllowDefaultOverwrite = true;
        public static readonly string cwd = Directory.GetCurrentDirectory();
        public static readonly string MapsDirectory = Path.Combine(cwd, "maps");
        public static readonly string MapsExtension = ".bin";
        public static void SaveMap(TileGrid grid, string name)
        {
            if (!AllowDefaultOverwrite && name == "default")
            {
                Debug.WriteLine("Cannot overwrite with name 'default'");
                return;
            }

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                Debug.WriteLine("Invalid file name");
                return;
            }

            if (!Directory.Exists(MapsDirectory))
                Directory.CreateDirectory(MapsDirectory);

            string path = Path.Combine(MapsDirectory, name + MapsExtension);
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            writer.Write(grid.Width);
            writer.Write(grid.Height);

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Tile tile = grid[y, x];

                    foreach (Shape shape in tile.Shapes)
                    {
                        if (shape is LineSegment line)
                        {
                            writer.Write("LineSegment");
                            writer.Write(line.A.X);
                            writer.Write(line.A.Y);
                            writer.Write(line.B.X);
                            writer.Write(line.B.Y);
                        }
                        else if (shape is Polygon polygon)
                        {
                            writer.Write("Polygon");
                            writer.Write(polygon.Segments.Length);
                            foreach (LineSegment segment in polygon.Segments)
                            {
                                writer.Write(segment.A.X);
                                writer.Write(segment.A.Y);
                                writer.Write(segment.B.X);
                                writer.Write(segment.B.Y);
                            }
                        }
                        else if (shape is Circle circle)
                        {
                            writer.Write("Circle");
                            writer.Write(circle.Center.X);
                            writer.Write(circle.Center.Y);
                            writer.Write(circle.Radius);
                        }
                    }
                }
            }
        }

        public static TileGrid LoadMap(string name)
        {
            string path = Path.Combine(MapsDirectory, name + MapsExtension);
            if (!File.Exists(path))
            {
                Debug.WriteLine($"File not found: {path}. Loading default map.");
                path = Path.Combine(MapsDirectory, "default" + MapsExtension);
                if (!File.Exists(path))
                {
                    Debug.WriteLine("Default map not found.");
                    return new TileGrid(30, 20);
                }
            }
            using var reader = new BinaryReader(File.Open(path, FileMode.Open));
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            TileGrid grid = new(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Tile tile = grid[y, x];

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        string shapeType = reader.ReadString();
                        switch (shapeType)
                        {
                            case "LineSegment":
                                Vector2 a = new(reader.ReadSingle(), reader.ReadSingle());
                                Vector2 b = new(reader.ReadSingle(), reader.ReadSingle());
                                tile.Add(new LineSegment(a, b));
                                break;
                            case "Polygon":
                                int numOfSides = reader.ReadInt32();
                                LineSegment[] segments = new LineSegment[numOfSides];
                                for (int i = 0; i < numOfSides; i++)
                                {
                                    Vector2 c = new(reader.ReadSingle(), reader.ReadSingle());
                                    Vector2 d = new(reader.ReadSingle(), reader.ReadSingle());
                                    segments[i] = new LineSegment(c, d);
                                }
                                tile.Add(new Polygon(segments));
                                break;
                            case "Circle":
                                Vector2 center = new(reader.ReadSingle(), reader.ReadSingle());
                                float radius = reader.ReadSingle();
                                tile.Add(new Circle(center, radius));
                                break;
                        }
                    }
                }
            }

            return grid;
        }
    }
}
