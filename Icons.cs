namespace ConsoleGame
{
    internal class Icons
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
    }

}
