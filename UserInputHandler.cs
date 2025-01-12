namespace ConsoleGame
{
    public class UserInputHandler
    {
        private CancellationTokenSource cancellationTokenSource;
        public ConsoleKey? pressedKey;

        public UserInputHandler()
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        public ConsoleKey? GetPressedKey()
        {
            return null;
        }

        public async Task StartHandlingInputAsync()
        {
            await Task.Yield(); // Allow the calling context to continue

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                pressedKey = null;
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                    // Update the list to add the pressed key
                    pressedKey = keyInfo.Key;
                }

                // Sleep briefly to avoid high CPU usage in the input handling loop
                await Task.Delay(10); // Delay for 10 milliseconds (adjust as needed)
            }
        }

        public void StopHandlingInput()
        {
            cancellationTokenSource.Cancel();
        }
    }


}
