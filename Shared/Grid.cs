using Sharpie;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Shared
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
            Arr = [.. Enumerable.Repeat(fill, width * height)];
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
            //for (int i = 0; i < Height * Width; i++)
            //{
            //    Arr[i] = c;
            //}

            Array.Fill(Arr, c);
        }

        /// <summary>
        /// Imposes another grid onto this grid at the specified position.
        /// </summary>
        /// <param name="grid">The grid to impose.</param>
        /// <param name="y">The y-coordinate of the top-left corner where the grid will be imposed.</param>
        /// <param name="x">The x-coordinate of the top-left corner where the grid will be imposed.</param>
        public void Impose(Grid<T> grid, int y = 0, int x = 0)
        {
            for (int pY = 0; pY < grid.Height; pY++)
                for (int pX = 0; pX < grid.Width; pX++)
                    this[y + pY, x + pX] = grid[pY, pX];
        }

        /// <summary>
        /// Imposes another grid onto this grid at the specified position, based on a predicate.
        /// </summary>
        /// <param name="grid">The grid to impose.</param>
        /// <param name="predicate">The predicate to determine whether to impose an element.</param>
        /// <param name="y">The y-coordinate of the top-left corner where the grid will be imposed.</param>
        /// <param name="x">The x-coordinate of the top-left corner where the grid will be imposed.</param>
        public void Impose(Grid<T> grid, Func<T, bool> predicate, int y = 0, int x = 0)
        {
            for (int pY = 0; pY < grid.Height; pY++)
                for (int pX = 0; pX < grid.Width; pX++)
                    if (predicate(grid[pY, pX]))
                        this[y + pY, x + pX] = grid[pY, pX];
        }

        /// <summary>
        /// Validates whether the specified position is within the bounds of the grid.
        /// </summary>
        /// <param name="y">The y-coordinate of the position.</param>
        /// <param name="x">The x-coordinate of the position.</param>
        /// <returns><c>true</c> if the position is within bounds; otherwise, <c>false</c>.</returns>
        public bool Validate(int y, int x, int offset = 0)
        {
            return 
                0 - offset <= x && 
                x < Width + offset && 
                0 - offset <= y && 
                y < Height + offset;
        }

        /// <summary>
        /// Validates whether the specified position is within the bounds of the grid.
        /// </summary>
        /// <param name="v">The position as a <see cref="Vector2"/>.</param>
        /// <returns><c>true</c> if the position is within bounds; otherwise, <c>false</c>.</returns>
        public bool Validate(Vector2 v, int offset = 0) => Validate((int)v.Y, (int)v.X, offset);
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
        public void Clear() => Shapes = [];

        //public bool RaycastAgainst(Vector2 origin, Vector2 direction, out float distance)
        //{
        //    // TODO: this should be an iterator of distances ordered from shortest to longest
        //    distance = float.MaxValue;
        //    foreach (Shape shape in Shapes)
        //    {
        //        if (shape.CheckIntersection(origin, direction, out float d, out float normPos))
        //            if (d < distance)
        //                distance = d;
        //    }
        //    return distance < float.MaxValue;
        //}

        //public bool RaycastAgainst(Vector2 origin, float angle, out float distance) =>
        //    RaycastAgainst(origin, new Vector2(MathF.Cos(angle), MathF.Sin(angle)), out distance);
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
        public TileGrid(int width, int height, Tile fill) : base(width, height, fill) { }
        

        /// <summary>
        /// Initializes a new instance of the <see cref="TileGrid"/> class by copying another tile grid.
        /// </summary>
        /// <param name="g">The tile grid to copy.</param>
        public TileGrid(TileGrid g) : base(g)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    this[y, x] = new Tile(g[y, x]);
                }
            }
        }
    }

    public class DisplayGrid : Grid<Pixel>
    {
        private readonly Grid<char> _pixelChars;
        private readonly StringBuilder _styledLine;
        private readonly StyledLine[] _linePool;
        private readonly int _linePoolCapacity;
        private int _poolIndex;

        public DisplayGrid(int width, int height, char fill = ' ')
            : base(width, height, new Pixel())
        {
            _pixelChars = new(width, height, fill);
            _styledLine = new StringBuilder(width * height);
            _linePoolCapacity = width * height;
            _linePool = new StyledLine[_linePoolCapacity];
            _poolIndex = 0;

            for (int i = 0; i < _linePoolCapacity; i++)
                _linePool[i] = new StyledLine();
        }

        /// <summary>
        /// Imposes another grid onto this grid at the specified position.
        /// </summary>
        /// <param name="grid">The grid to impose.</param>
        /// <param name="y">The y-coordinate of the top-left corner where the grid will be imposed.</param>
        /// <param name="x">The x-coordinate of the top-left corner where the grid will be imposed.</param>
        public void Impose(DisplayGrid grid, int y = 0, int x = 0)
        {
            for (int pY = 0; pY < grid.Height; pY++)
                for (int pX = 0; pX < grid.Width; pX++)
                    SetPixel(grid[pY, pX], grid._pixelChars[pY, pX], x + pX, y + pY);
                    //this[y + pY, x + pX] = grid[pY, pX];
        }

        /// <summary>
        /// Imposes another grid onto this grid at the specified position, based on a predicate.
        /// </summary>
        /// <param name="grid">The grid to impose.</param>
        /// <param name="predicate">The predicate to determine whether to impose an element.</param>
        /// <param name="y">The y-coordinate of the top-left corner where the grid will be imposed.</param>
        /// <param name="x">The x-coordinate of the top-left corner where the grid will be imposed.</param>
        public void Impose(DisplayGrid grid, Func<Pixel, bool> predicate, int y = 0, int x = 0)
        {
            for (int pY = 0; pY < grid.Height; pY++)
                for (int pX = 0; pX < grid.Width; pX++)
                    if (predicate(grid[pY, pX]))
                        SetPixel(grid[pY, pX], grid._pixelChars[pY, pX], x + pX, y + pY);
                        //this[y + pY, x + pX] = grid[pY, pX];
        }

        public void Fill(Pixel pixel, char character)
        {
            //for (int i = 0; i < Height * Width; i++)
            //{
            //    Arr[i] = c;
            //}

            Array.Fill(Arr, pixel);
            _pixelChars.Fill(character);
        }

        public void SetPixel(Pixel pixel, char character, int x, int y)
        {
            this[y, x] = pixel;
            _pixelChars[y, x] = character;
        }

        /// <summary>
        /// This function groups chunks of lines that have the same style and returns them as strings, reducing the required terminal write operations.
        /// </summary>
        /// <returns>Strings with style to be printed</returns>
        public IEnumerable<StyledLine> GetStyledLines()
        {
            _styledLine.Clear();
            _poolIndex = 0; // reset for this frame

            for (int y = 0; y < Height; y++)
            {
                Style style = Style.Default;
                int startX = 0;

                for (int x = 0; x < Width; x++)
                {
                    Pixel p = this[y, x];
                    char c = _pixelChars[y, x];
                    if (_styledLine.Length == 0)
                    {
                        style = p.style;
                        startX = x;
                    }

                    if (p.style == style)
                        _styledLine.Append(c);
                    else
                    {
                        var line = _linePool[_poolIndex++];
                        line.Set(_styledLine.ToString(), style, startX, y);
                        yield return line;
                        _styledLine.Clear();

                        _styledLine.Append(c);
                        style = p.style;
                        startX = x;
                    }
                }

                if (_styledLine.Length > 0)
                {
                    var line = _linePool[_poolIndex++];
                    line.Set(_styledLine.ToString(), style, startX, y);
                    yield return line;
                    _styledLine.Clear();
                }
            }
        }

    }
}
