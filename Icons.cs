namespace ConsoleGame
{
    internal class Icons
    {
        public static readonly char Wall = '█';
        public static readonly char MiniMap_WallIntersected = '#';
        public static readonly char Air = ' ';
        public static readonly char Player = '☻';
        public static readonly string Stars = "*.°";
        public static readonly string ShadingWall = "█▓▒░";
        public static readonly string ShadingFloor = "YQXz/*cr!+<>;=^,_:'-.` ";
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
    }

}
