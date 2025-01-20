namespace ConsoleGame
{
    /// <summary>
    /// Represents a 2D grid of type T.
    /// </summary>
    public class Grid<T>(int width, int height, T fill = default(T))
    {
        public T[] Arr = [.. Enumerable.Repeat(fill, width * height)];
        public int Width = width;
        public int Height = height;

        public Grid(int width, int height, T[] arr) : this(width, height, default(T))
        {
            Array.Copy(arr, Arr, width * height);
        }

        public Grid(Grid<T> p) : this(p.Width, p.Height, default(T))
        {
            Array.Copy(p.Arr, Arr, p.Width * p.Height);
        }

        // Overrides
        public T this[int y, int x]
        {
            get { return Arr[y * Width + x]; }
            set { Arr[y * Width + x] = value; }
        }

        public T this[float y_norm, float x_norm]
        {
            get { return this[(int)(y_norm * Height), (int)(x_norm * Width)]; }
            set { this[(int)(y_norm * Height), (int)(x_norm * Width)] = value; }
        }

        public T this[iVector2D v]
        {
            get { return this[v.Y, v.X]; }
            set { this[v.Y, v.X] = value; }
        }

        public T this[fVector2D v_norm]
        {
            get { return this[v_norm.Y, v_norm.X]; }
            set { this[v_norm.Y, v_norm.X] = value; }
        }

        // Positional Transformations
        private void PositionalTransform(int newHeight,
                                         int newWidth,
                                         Func<int, int, int> calculateNewY,
                                         Func<int, int, int> calculateNewX)
        {
            T[] newArr = [.. Enumerable.Repeat(default(T), newWidth * newHeight)];
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

        public void Rotate90() // WIP, switch to rotation by skewing? https://www.youtube.com/watch?v=1LCEiVDHJmc
        {
            PositionalTransform(
                newWidth: Height,
                newHeight: Width,
                calculateNewY: (_, x) => x,
                calculateNewX: (y, _) => Height - 1 - y
            );
        }

        public void Slide(int offsetX = 0, int offsetY = 0)
        {
            PositionalTransform(
                newWidth: Width,
                newHeight: Height,
                calculateNewY: (y, _) => Extensions.Mod(y + offsetY, Height),
                calculateNewX: (_, x) => Extensions.Mod(x + offsetX, Width)
            );
        }

        // Content Transformations
        public void Fill(T c)
        {
            for (int i = 0; i < Height * Width; i++)
            {
                Arr[i] = c;
            }
        }

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

        // Position validation
        public bool Validate(float y, float x)
        {
            return 0 <= x && x < Width && 0 <= y && y < Height;
        }

        public bool Validate(fVector2D v)
        {
            return 0 <= v.X && v.X < Width && 0 <= v.Y && v.Y < Height;
        }

        public bool Validate(iVector2D v)
        {
            return 0 <= v.X && v.X < Width && 0 <= v.Y && v.Y < Height;
        }
    }

    public class CharGrid : Grid<char>
    {
        public CharGrid(int width, int height, char fill = ' ') : base(width, height, fill) { }
        public CharGrid(int width, int height, char[] arr) : base(width, height, arr) { }
        public CharGrid(CharGrid p) : base(p) { }

        public static CharGrid Load(string path)
        {
            string[] n = File.ReadAllLines(path);
            CharGrid g = new(n.Max(n => n.Length), n.Length);
            for (int y = 0; y < n.Length; y++) 
            {
                g.Impose(n[y], y);
            }
            return g;
        }

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

        public void Impose(string s, int y = 0, int x = 0, bool allowTransparency = false)
        {
            for (int pX = 0; pX < s.Length; pX++)
            {
                if (allowTransparency && s[pX] == ' ') continue;
                this[y, x + pX] = s[pX];
            }
        }
    }
}
