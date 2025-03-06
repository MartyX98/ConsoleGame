using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace ConsoleGame
{
    /// <summary>
    /// Represents a 2D grid of type T.
    /// </summary>
    /// <typeparam name="T">The type of elements in the grid.</typeparam>
    public class Grid<T>
    {
        public T[] Arr;
        public int Width;
        public int Height;

        /// <summary>
        /// Initializes a new instance of the <see cref="Grid{T}"/> class with the specified width, height, and fill value.
        /// </summary>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="fill">The fill value for the grid elements.</param>
        public Grid(int width, int height, T fill = default(T))
        {
            Arr = Enumerable.Repeat(fill, width * height).ToArray();
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Grid{T}"/> class with the specified width, height, and array of elements.
        /// </summary>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="arr">The array of elements to initialize the grid with.</param>
        public Grid(int width, int height, T[] arr) : this(width, height, default(T))
        {
            Array.Copy(arr, Arr, width * height);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Grid{T}"/> class by copying another grid.
        /// </summary>
        /// <param name="p">The grid to copy.</param>
        public Grid(Grid<T> p) : this(p.Width, p.Height, default(T))
        {
            Array.Copy(p.Arr, Arr, p.Width * p.Height);
        }

        /// <summary>
        /// Gets or sets the element at the specified position in the grid.
        /// </summary>
        /// <param name="y">The y-coordinate of the element.</param>
        /// <param name="x">The x-coordinate of the element.</param>
        /// <returns>The element at the specified position.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when the specified position is out of bounds.</exception>
        public T this[int y, int x]
        {
            get
            {
                if (y < 0 || y >= Height || x < 0 || x >= Width)
                    throw new IndexOutOfRangeException("Index out of bounds.");
                return Arr[y * Width + x];
            }
            set
            {
                if (y < 0 || y >= Height || x < 0 || x >= Width)
                    throw new IndexOutOfRangeException("Index out of bounds.");
                Arr[y * Width + x] = value;
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified position in the grid.
        /// </summary>
        /// <param name="v">The position as a <see cref="Vector2"/>.</param>
        /// <returns>The element at the specified position.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when the specified position is out of bounds.</exception>
        public T this[Vector2 v]
        {
            get
            {
                int y = (int)v.Y;
                int x = (int)v.X;
                if (y < 0 || y >= Height || x < 0 || x >= Width)
                    throw new IndexOutOfRangeException("Index out of bounds.");
                return this[y, x];
            }
            set
            {
                int y = (int)v.Y;
                int x = (int)v.X;
                if (y < 0 || y >= Height || x < 0 || x >= Width)
                    throw new IndexOutOfRangeException("Index out of bounds.");
                this[y, x] = value;
            }
        }

        /// <summary>
        /// Transforms the grid by applying positional transformations.
        /// </summary>
        /// <param name="newHeight">The new height of the grid.</param>
        /// <param name="newWidth">The new width of the grid.</param>
        /// <param name="calculateNewY">The function to calculate the new y-coordinate.</param>
        /// <param name="calculateNewX">The function to calculate the new x-coordinate.</param>
        private void PositionalTransform(int newHeight,
                                         int newWidth,
                                         Func<int, int, int> calculateNewY,
                                         Func<int, int, int> calculateNewX)
        {
            T[] newArr = Enumerable.Repeat(default(T), newWidth * newHeight).ToArray();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int newY = calculateNewY(y, x);
                    int newX = calculateNewX(y, x);
                    newArr[newY * newWidth + newX] = this[y, x];
                }
            }
            (Arr, Height, Width) = (newArr, newHeight, newWidth);
        }

        /// <summary>
        /// Rotates the grid 90 degrees clockwise.
        /// </summary>
        public void Rotate90()
        {
            PositionalTransform(
                newWidth: Height,
                newHeight: Width,
                calculateNewY: (_, x) => x,
                calculateNewX: (y, _) => Height - 1 - y
            );
        }

        /// <summary>
        /// Slides the grid by the specified offsets.
        /// </summary>
        /// <param name="offsetX">The offset in the x-direction.</param>
        /// <param name="offsetY">The offset in the y-direction.</param>
        public void Slide(int offsetX = 0, int offsetY = 0)
        {
            PositionalTransform(
                newWidth: Width,
                newHeight: Height,
                calculateNewY: (y, _) => (y + offsetY).Mod(Height),
                calculateNewX: (_, x) => (x + offsetX).Mod(Width)
            );
        }

        /// <summary>
        /// Fills the grid with the specified value.
        /// </summary>
        /// <param name="c">The value to fill the grid with.</param>
        public void Fill(T c)
        {
            for (int i = 0; i < Height * Width; i++)
            {
                Arr[i] = c;
            }
        }

        /// <summary>
        /// Imposes another grid onto this grid at the specified position.
        /// </summary>
        /// <param name="p">The grid to impose.</param>
        /// <param name="y">The y-coordinate of the top-left corner where the grid will be imposed.</param>
        /// <param name="x">The x-coordinate of the top-left corner where the grid will be imposed.</param>
        public void Impose(Grid<T> p, int y = 0, int x = 0)
        {
            for (int pY = 0; pY < p.Height; pY++)
            {
                for (int pX = 0; pX < p.Width; pX++)
                {
                    this[y + pY, x + pX] = p[pY, pX];
                }
            }
        }

        /// <summary>
        /// Validates whether the specified position is within the bounds of the grid.
        /// </summary>
        /// <param name="y">The y-coordinate of the position.</param>
        /// <param name="x">The x-coordinate of the position.</param>
        /// <returns><c>true</c> if the position is within bounds; otherwise, <c>false</c>.</returns>
        public bool Validate(float y, float x)
        {
            return 0 <= x && x < Width && 0 <= y && y < Height;
        }

        /// <summary>
        /// Validates whether the specified position is within the bounds of the grid.
        /// </summary>
        /// <param name="v">The position as a <see cref="Vector2"/>.</param>
        /// <returns><c>true</c> if the position is within bounds; otherwise, <c>false</c>.</returns>
        public bool Validate(Vector2 v) => Validate(v.Y, v.X);
    }

    /// <summary>
    /// Represents a 2D grid of characters.
    /// </summary>
    public class CharGrid : Grid<char>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CharGrid"/> class with the specified width, height, and fill character.
        /// </summary>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="fill">The fill character for the grid elements.</param>
        public CharGrid(int width, int height, char fill = ' ') : base(width, height, fill) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharGrid"/> class with the specified width, height, and array of characters.
        /// </summary>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="arr">The array of characters to initialize the grid with.</param>
        public CharGrid(int width, int height, char[] arr) : base(width, height, arr) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharGrid"/> class by copying another character grid.
        /// </summary>
        /// <param name="p">The character grid to copy.</param>
        public CharGrid(CharGrid p) : base(p) { }

        /// <summary>
        /// Loads a character grid from a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A new <see cref="CharGrid"/> initialized with the contents of the file.</returns>
        public static CharGrid Load(string path)
        {
            string[] lines = File.ReadAllLines(path);
            CharGrid grid = new CharGrid(lines.Max(line => line.Length), lines.Length);
            for (int y = 0; y < lines.Length; y++)
            {
                grid.Impose(lines[y], y);
            }
            return grid;
        }

        /// <summary>
        /// Imposes another character grid onto this grid at the specified position.
        /// </summary>
        /// <param name="p">The character grid to impose.</param>
        /// <param name="y">The y-coordinate of the top-left corner where the grid will be imposed.</param>
        /// <param name="x">The x-coordinate of the top-left corner where the grid will be imposed.</param>
        /// <param name="allowTransparency">Whether to allow transparency when imposing the grid.</param>
        /// <param name="emptyChar">The character representing an empty cell.</param>
        public void Impose(CharGrid p, int y = 0, int x = 0, bool allowTransparency = false, char emptyChar = '_')
        {
            for (int pY = 0; pY < p.Height; pY++)
            {
                for (int pX = 0; pX < p.Width; pX++)
                {
                    if (allowTransparency && p[pY, pX] == ' ') continue;
                    else if (allowTransparency && p[pY, pX] == emptyChar) this[y + pY, x + pX] = ' ';
                    else this[y + pY, x + pX] = p[pY, pX];
                }
            }
        }

        /// <summary>
        /// Imposes a string onto this grid at the specified position.
        /// </summary>
        /// <param name="s">The string to impose.</param>
        /// <param name="y">The y-coordinate of the top-left corner where the string will be imposed.</param>
        /// <param name="x">The x-coordinate of the top-left corner where the string will be imposed.</param>
        /// <param name="allowTransparency">Whether to allow transparency when imposing the string.</param>
        public void Impose(string s, int y = 0, int x = 0, bool allowTransparency = false)
        {
            for (int pX = 0; pX < s.Length; pX++)
            {
                if (allowTransparency && s[pX] == ' ') continue;
                this[y, x + pX] = s[pX];
            }
        }
    }

    /// <summary>
    /// Represents an integral chunk of a 2D grid.
    /// </summary>
    /// <remarks>
    /// A tile is a 2D shape composed of faces that can be used to create a grid-based map.
    /// </remarks>
    public class Tile
    {
        public Shape[] Shapes = [];
        public bool IsEmpty => Shapes.Length == 0;

        #region Constructors
        public Tile() => Shapes = [];

        public Tile(Shape shape) => Shapes = [.. Shapes, shape];

        public Tile(Shape[] shapes) => Shapes = shapes;

        public Tile(Tile tile) => Shapes = tile.Shapes;

        #endregion

        public void Add(Shape shape) => Shapes = [.. Shapes, shape];
        public void Add(Shape[] shapes) => Shapes = [.. Shapes, .. shapes];
        public void Remove(Shape shape) => Shapes = [.. Shapes.Where(s => s != shape)];
        public void Remove(Shape[] shapes) => Shapes = [.. Shapes.Except(shapes)];

        public void RemoveTest(Shape shape)
        {
            for (int i = 0; i < Shapes.Length; i++)
            {
                if (Shapes[i] == shape)
                {
                    shape = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    break;
                }
            }
        }

        public void Clear() => Shapes = [];

        public bool RaycastAgainst(Vector2 origin, Vector2 direction, out float distance)
        {
            // TODO: this should be an iterator of distances ordered from shortest to longest
            distance = float.MaxValue;
            foreach (Shape shape in Shapes)
            {
                if (shape.CheckIntersection(origin, direction, out float d))
                    if (d < distance)
                        distance = d;
            }
            return distance < float.MaxValue;
        }

        public bool RaycastAgainst(Vector2 origin, float angle, out float distance) =>
            RaycastAgainst(origin, new Vector2(MathF.Cos(angle), MathF.Sin(angle)), out distance);
    }

    /// <summary>
    /// Represents a 2D grid of tiles.
    /// </summary>
    public class TileGrid : Grid<Tile>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TileGrid"/> class with the specified width and height, and fills it with empty tiles.
        /// </summary>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        public TileGrid(int width, int height) : base(width, height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    this[y, x] = new Tile();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TileGrid"/> class with the specified width and height, and fills it with the specified tile.
        /// </summary>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="fill">The tile to fill the grid with.</param>
        public TileGrid(int width, int height, Tile fill) : base(width, height, fill)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    this[y, x] = new Tile(fill);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TileGrid"/> class by copying another tile grid.
        /// </summary>
        /// <param name="g">The tile grid to copy.</param>
        
    }
}
