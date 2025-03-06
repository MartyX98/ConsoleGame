using System;
namespace ConsoleGame
{
    internal class AsciiUtils
    {
        public const char Wall = '█';
        public const char Tree = 'T';
        public const char RayCollisionFlag = '#';
        public const char Air = ' ';
        public const char Player = '☻';
        public const string Stars = "*.°";
        public const string ShadingWall = "█▓▒░";
        public const string ShadingFloor = "YQXz/*cr!+<>;=^,_:'-.` ";
        //public static readonly string ShadingFloor = """.'`^",:;Il!i><~+_-?][}{1)(|\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$""";

        public static char GetShade(float n, string g, int offset = 0, bool flipRange = false)
        {
            if (flipRange)
            {
                n = 1 - n;
            }

            if (offset > 0)
            {
                return g[Math.Min(GetShadeIndex(n, g) + offset, g.Length - 1)];
            }
            else if (offset < 0)
            {
                return g[Math.Max(GetShadeIndex(n, g) + offset, 0)];
            }
            else
            {
                return g[GetShadeIndex(n, g)];
            }
        }

        private static int GetShadeIndex(float n, string g)
        {
            return (int)(n * (g.Length - 1));
        }

        public static CharGrid GenerateSkyTexture(int width, int height, float starDensity = 0.025f)
        {
            Random rand = new();
            CharGrid sky = new CharGrid(width, height);
            for (int x = 0; x < sky.Width; x++)
            {
                for (int y = 0; y < sky.Height; y++)
                {
                    sky[y, x] = rand.NextSingle() <= starDensity ? Stars[rand.Next(0, Stars.Length)] : Air;
                }
            }
            return sky;
        }

        public static CharGrid GenerateFloorTexture(int width, int height, int offset = 3)
        {
            Random rand = new();
            CharGrid floor = new CharGrid(width, height);
            for (int x = 0; x < floor.Width; x++)
            {
                for (int y = 0; y < floor.Height; y++)
                {
                    floor[y, x] = GetShade((float)y / (floor.Height - 1), ShadingFloor, rand.Next(-offset, offset + 1), flipRange: true);
                }
            }
            return floor;
        }
    }

}
