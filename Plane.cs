namespace ConsoleGame
{
    /// <summary>
    /// Represents a 2D plane of chars.
    /// </summary>
    public class Plane
    {
        public char[] Arr;
        public int Width;
        public int Height;

        public Plane(int width, int height, char fill = ' ')
        {
            if (width <= 0 || height <= 0) throw new Exception("Invalid grid parameters provided.");
            Width = width;
            Height = height;
            Arr = GetCharArray(height * width, fill);
        }

        public Plane(int width, int height, char[] arr) : this(width, height)
        {
            Array.Copy(arr, Arr, width * height);
        }

        public Plane(Plane p) : this(p.Width, p.Height)
        {
            Array.Copy(p.Arr, Arr, p.Width * p.Height);
        }

        public Plane(string path)
        {
            string[] n = File.ReadAllLines(path);
            Height = n.Length;
            Width = n[0].Length;
            Arr = String.Join("", n).ToArray();
        }

        // Overrides
        public char this[int y, int x]
        {
            get
            {
                return Arr[y * Width + x];
            }
            set
            {
                Arr[y * Width + x] = value;
            }
        }

        public char this[iVector2D v]
        {
            get
            {
                return Arr[v.Y * Width + v.X];
            }
            set
            {
                Arr[v.Y * Width + v.X] = value;
            }
        }

        // Positional Transformations
        private void PositionalTransform(int newHeight,
                                         int newWidth,
                                         Func<int, int, int> calculateNewY,
                                         Func<int, int, int> calculateNewX)
        {
            char[] newArr = GetCharArray(newWidth * newHeight);
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
        public void Fill(char c)
        {
            for (int i = 0; i < Height * Width; i++)
            {
                Arr[i] = c;
            }
        }

        public void Impose(Plane p, int y = 0, int x = 0, bool allowTransparency = false)
        {
            for (int pY = 0; pY < p.Height; pY++)
            {
                for (int pX = 0; pX < p.Width; pX++)
                {
                    if (allowTransparency && p[pY, pX] == ' ') continue;
                    this[y + pY, x + pX] = p[pY, pX];
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

        // other methods
        public void DebugPrint()
        {
            var s = new string(Arr);
            for (int y = 0; y < Height; y++)
            {
                Console.WriteLine(s.Substring(y * Width, Width));
            }
        }

        private static char[] GetCharArray(int length, char fill = ' ')
        {
            return new string(fill, length).ToArray();
        }
    }
}
