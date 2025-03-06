namespace MonoDebugger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var game = new MonoGameApp();
            game.Run();
        }
    }
}