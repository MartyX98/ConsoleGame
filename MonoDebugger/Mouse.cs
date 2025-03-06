using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoDebugger
{
    class Mouse
    {
        static MouseState currentKeyState;
        static MouseState previousKeyState;

        public static MouseState GetState()
        {
            previousKeyState = currentKeyState;
            currentKeyState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            return currentKeyState;
        }

        public static bool IsButtonDown(Button button, bool oneShot = false)
        {
            switch (button)
            {
                case Button.LeftButton:
                    if (!oneShot) return currentKeyState.LeftButton == ButtonState.Pressed;
                    return currentKeyState.LeftButton == ButtonState.Pressed && previousKeyState.LeftButton == ButtonState.Released;

                case Button.RightButton:
                    if (!oneShot) return currentKeyState.RightButton == ButtonState.Pressed;
                    return currentKeyState.RightButton == ButtonState.Pressed && previousKeyState.RightButton == ButtonState.Released;

                case Button.MiddleButton:
                    if (!oneShot) return currentKeyState.MiddleButton == ButtonState.Pressed;
                    return currentKeyState.MiddleButton == ButtonState.Pressed && previousKeyState.MiddleButton == ButtonState.Released;

                case Button.XButton1:
                    if (!oneShot) return currentKeyState.XButton1 == ButtonState.Pressed;
                    return currentKeyState.XButton1 == ButtonState.Pressed && previousKeyState.XButton1 == ButtonState.Released;

                case Button.XButton2:
                    if (!oneShot) return currentKeyState.XButton2 == ButtonState.Pressed;
                    return currentKeyState.XButton2 == ButtonState.Pressed && previousKeyState.XButton2 == ButtonState.Released;

                default:
                    throw new ArgumentOutOfRangeException(nameof(button), "Invalid mouse button.");
            }
        }

        public static int GetScrollWheelValue()
        {
            return currentKeyState.ScrollWheelValue / 120;
        }

        public enum Button
        {
            LeftButton,
            RightButton,
            MiddleButton,
            XButton1,
            XButton2
        }
    }
}
