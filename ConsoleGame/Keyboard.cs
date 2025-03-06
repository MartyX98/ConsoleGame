using System.Runtime.InteropServices;

namespace ConsoleGame
{
    public class InputHandler
    {
        [DllImport("user32.dll")]
        static extern short GetKeyState(int key);
        public Dictionary<Keys, (bool IsDown, bool IsToggled)> currentState = [];
        public Dictionary<Keys, (bool IsDown, bool IsToggled)> previousState = [];
        public List<Keys> allKeys = [.. Enum.GetValues<Keys>().Cast<Keys>()];

        public InputHandler()
        {
            foreach (Keys key in allKeys)
            {
                currentState[key] = (false, false);
                previousState[key] = (false, false);
            }
        }

        public void Update()
        {
            foreach (Keys key in allKeys)
            {
                previousState[key] = currentState[key];
                short keyState = GetKeyState((int)key);
                currentState[key] = ((keyState & 0x8000) != 0, (keyState & 0x0001) != 0);
            }
        }

        public bool IsKeyDown(Keys key, bool oneShot = false)
        {
            if (!oneShot) return currentState[key].IsDown;
            return !previousState[key].IsDown && currentState[key].IsDown;
        }

        public bool IsKeyToggled(Keys key)
        {
            return currentState[key].IsToggled;
        }
    }
}
