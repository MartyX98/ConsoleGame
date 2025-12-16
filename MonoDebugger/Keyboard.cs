using Microsoft.Xna.Framework.Input;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace MonoGameDemo
{
    class Keyboard
    {
        static KeyboardState currentKeyState;
        static KeyboardState previousKeyState;

        public static KeyboardState GetState()
        {
            previousKeyState = currentKeyState;
            currentKeyState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            return currentKeyState;
        }

        public static bool IsKeyDown(Keys key, bool oneShot = false)
        {
            if (!oneShot) return currentKeyState.IsKeyDown(key);
            return currentKeyState.IsKeyDown(key) && !previousKeyState.IsKeyDown(key);
        }
    }
}
