namespace ConsoleGame
{
    public static class Extensions
    {
        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
