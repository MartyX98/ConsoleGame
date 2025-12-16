using Sharpie;
using Sharpie.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using Image = SixLabors.ImageSharp.Image;

namespace Shared
{
    public readonly struct Pixel
    {
        public readonly Style style;
        public readonly Rgba32 rgba32;

        public Pixel(Style style, Rgba32 rgba32)
        {
            this.style = style;
            this.rgba32 = rgba32;
        }
        
        public Pixel(Pixel pixel)
        {
            style = pixel.style;
            rgba32 = pixel.rgba32;
        }

        public static bool operator ==(Pixel left, Pixel right)
        {
            return left.style == right.style && left.rgba32 == right.rgba32;
        }

        public static bool operator !=(Pixel left, Pixel right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is Pixel other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(style, rgba32);
        }
    }

    // TODO: Implement texture class into the project, implement mipmap generation and pixel lookup function for different sizes
    public class Texture
    {
        private const int _maxMipmapLevels = 16;
        public string Name { get; set; }

        public float Ratio => LargestMipmapSize.X / LargestMipmapSize.Y;
        public Dictionary<Vector2, Grid<Pixel>> Mipmaps { get; set; } = [];
        public Vector2 SmallestMipmapSize => Mipmaps.Keys.OrderBy(v => v.Length()).First();
        public Vector2 LargestMipmapSize => Mipmaps.Keys.OrderBy(v => v.Length()).Last();
        public Texture(Grid<Pixel> texture, string name)
        {
            Name = name;
            Mipmaps[new Vector2(texture.Width, texture.Height)] = texture;
            Grid<Pixel> lastMipMap = texture;
            for (int i = 1; i < _maxMipmapLevels; i++)
            {
                Vector2 newSize = new(texture.Width >> i, texture.Height >> i);
                Vector2 minSize = new(16, 16);
                if (newSize.X < minSize.X || newSize.Y < minSize.Y)
                    break;
                Grid<Pixel> newMipMap = Textures.GenerateMipMap(lastMipMap, newSize);
                Mipmaps[newSize] = newMipMap;
                lastMipMap = newMipMap;
            }
        }
        public Texture(Texture texture)
        {
            Name = texture.Name;
            foreach (Vector2 size in texture.Mipmaps.Keys)
                Mipmaps[size] = new Grid<Pixel>(texture.Mipmaps[size]);
        }

        // square brackets operator overload
        public Pixel this[int x, int y]
        {
            get => Mipmaps[LargestMipmapSize][y, x];
        }

        public Pixel this[float unitX, float unitY]
        {
            get => GetPixel(new Vector2(unitX, unitY), LargestMipmapSize);
        }

        public Grid<Pixel> GetMipmap(Vector2 size)
        {
            Vector2 smallest = SmallestMipmapSize;
            if (size.Length() <= smallest.Length())
                return Mipmaps[smallest];

            if (Mipmaps.TryGetValue(size, out Grid<Pixel>? value))
                return value;

            Vector2 closestSize = Mipmaps.Keys
                .Where(s => s.X <= size.X && s.Y <= size.Y)
                .OrderBy(s => Math.Abs(s.X - size.X) + Math.Abs(s.Y - size.Y))
                .First();

            return Mipmaps[closestSize];
        }

        public Pixel GetPixel(Vector2 pixelUnitPos, Vector2 textureSize)
        {
            if (pixelUnitPos.X < 0 || pixelUnitPos.X > 1 || pixelUnitPos.Y < 0 || pixelUnitPos.Y > 1)
                throw new ArgumentException("Unit position must be between 0 and 1");
            Grid<Pixel> mipmap = GetMipmap(textureSize);
            int x = (int)(pixelUnitPos.X * (mipmap.Width - 1));
            int y = (int)(pixelUnitPos.Y * (mipmap.Height - 1));
            return mipmap[y, x];
        }
    }

    public class Textures
    {
        public const string ImageFormat = ".png";
        public string PathToTexturesDirectory;
        public int Count => _loadedTextures.Count;
        public string[] LoadedTextures => [.. _loadedTextures.Keys];
        private readonly Dictionary<string, Texture> _loadedTextures = [];
        private readonly Dictionary<Rgba32, Style> styleLookup = [];

        public Textures(string pathToTexturesDirectory, Dictionary<Rgba32, Style> styleLookup)
        {
            PathToTexturesDirectory = pathToTexturesDirectory;
            this.styleLookup = styleLookup;

            if (!Directory.Exists(PathToTexturesDirectory))
                throw new DirectoryNotFoundException($"Textures initialization failed; Directory not found: {PathToTexturesDirectory}");
            
            if (styleLookup.Count == 0)
                throw new ArgumentException("Textures initialization failed; styleLookup dictionary is empty");

            LoadTextures();
        }
        
        public bool TryGetTexture(string name, out Texture texture)
        {
            texture = _loadedTextures["default"];
            if (_loadedTextures.TryGetValue(name, out Texture _texture))
            {
                texture = _texture;
                return true;
            }
            return false;
        }

        private void LoadTextures()
        {
            _loadedTextures.Clear();
            string[] files = Directory.GetFiles(PathToTexturesDirectory);
            foreach (string file in files)
            {
                if (Path.GetExtension(file) != ImageFormat)
                    continue;
                string name = Path.GetFileNameWithoutExtension(file);
                Image<Rgba32> image = Image.Load<Rgba32>(file);
                Grid<Pixel> pixels = new(image.Width, image.Height);
                for (int y = 0; y < image.Height; y++)
                    for (int x = 0; x < image.Width; x++)
                    {
                        Rgba32 color = image[x, y];
                        Style style = GetClosestStyle(color);
                        pixels[y, x] = new Pixel(style, color);
                    }

                _loadedTextures[name] = new(pixels, name);
            }

            if (!LoadedTextures.Contains("default"))
                _loadedTextures["default"] = GenerateDefaultTexture();
        }

        public Style GetClosestStyle(Rgba32 color)
        {
            Style closestStyle = Style.Default;
            float closestDistance = float.MaxValue;
            foreach (Rgba32 c in styleLookup.Keys)
            {
                float distance = MathF.Sqrt(
                    MathF.Pow(color.R - c.R, 2) +
                    MathF.Pow(color.G - c.G, 2) +
                    MathF.Pow(color.B - c.B, 2)
                    );
                if (distance < closestDistance)
                {
                    closestStyle = styleLookup[c];
                    closestDistance = distance;
                }
            }
            return closestStyle;
        }

        public Style GetClosestStyle(Rgba32[] colors)
        {
            Rgba32 averageColor = new(
                r: (byte)colors.Average(c => c.R),
                g: (byte)colors.Average(c => c.G),
                b: (byte)colors.Average(c => c.B)
                );
            return GetClosestStyle(averageColor);
        }

        public Texture GenerateDefaultTexture() 
            => GenerateEmptyTexture(255, 0, 0);

        public Texture GenerateEmptyTexture(byte r, byte g, byte b, byte a = 255, int size = 16)
        {
            Grid<Pixel> pixels = new(size, size);
            Rgba32 rgba32 = new(r: r, g: g, b: b, a: a);
            Style pixelStyle = GetClosestStyle(rgba32);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y, x] = new Pixel(pixelStyle, rgba32);
            Texture texture = new(pixels, "default");
            return texture;
        }

        public static Grid<Pixel> GenerateMipMap(Grid<Pixel> texture, Vector2 size)
        {
            Grid<Pixel> mipmap = new((int)size.X, (int)size.Y);
            float subPixelWidth = (float)texture.Width / mipmap.Width;
            float subPixelHeight = (float)texture.Height / mipmap.Height;

            for (int y = 0; y < mipmap.Height; y++)
                for (int x = 0; x < mipmap.Width; x++)
                {
                    // sample all subpixels that the new pixel is intersecting with
                    // subpixel iteration;
                    List<Pixel> subPixels = [];
                    int fromX = (int)(x * subPixelWidth);
                    int toX = (int)Math.Clamp((MathF.Ceiling((x + 1) * subPixelWidth)), 0, texture.Width - 1);
                    int fromY = (int)(y * subPixelHeight);
                    int toY = (int)Math.Clamp((MathF.Ceiling((y + 1) * subPixelHeight)), 0, texture.Height - 1);

                    for (int subY = fromY; subY < toY; subY++)
                        for (int subX = fromX; subX < toX; subX++)
                            subPixels.Add(texture[subY, subX]);

                    // now we have all subpixels that the new pixel is sampling from
                    // we cannot introduce new colors since we are limited by palette
                    // we can employ different strategies to pick the color
                    // a) We can average the color and pick the closest color from the palette
                    // b) we can pick the most common color
                    // c) since some subpixels are included only partially, we can weight them by the area they cover
                    // and more.. We might also want to consider different strategies for Alpha channel separately from RGB

                    IEnumerable<Pixel> deduplicatedSubPixels = subPixels.Distinct();

                    Pixel[] nonTransparentSubPixels = [.. deduplicatedSubPixels.Where(p => p.rgba32.A == 255)];

                    // if all subpixels are transparent, we pick the first one
                    if (nonTransparentSubPixels.Length == 0)
                    {
                        mipmap[y, x] = subPixels.FirstOrDefault(new Pixel());
                        continue;
                    }

                    // if there is only one non-transparent subpixel, we pick it
                    if (nonTransparentSubPixels.Length == 1)
                    {
                        mipmap[y, x] = nonTransparentSubPixels[0];
                        continue;
                    }

                    Rgba32 mostCommon = nonTransparentSubPixels
                        .GroupBy(p => p.rgba32)
                        .OrderByDescending(g => g.Count())
                        .First().Key;

                    mipmap[y, x] = new Pixel(nonTransparentSubPixels.Where(p => p.rgba32 == mostCommon).First().style, mostCommon);
                }
            return mipmap;
        }
    }
}