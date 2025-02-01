using System.Runtime.InteropServices;
namespace ConsoleGame
{
    /// <summary>
    /// Provides a class for handling console buffer writing, allowing you to write text
    /// directly to the console screen buffer with more control and efficiency.
    /// </summary>
    public class ConsoleHandler
    {
        private readonly IntPtr consoleOutputHandle;
        private readonly IntPtr consoleInputHandle;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_INPUT_HANDLE = -10;
        private const uint CP_UTF8 = 65001;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool WriteConsoleOutputCharacter(
            IntPtr hConsoleOutput, string lpCharacter, uint nLength,
            COORD dwWriteCoord, out uint lpNumberOfCharsWritten);

        /// <summary>
        /// Writes a string of characters to the console screen buffer at the specified position.
        /// </summary>
        /// <param name="text">The string of characters to be written to the console.</param>
        /// <param name="x">The X-coordinate of the starting position for writing.</param>
        /// <param name="y">The Y-coordinate of the starting position for writing.</param>
        /// <exception cref="InvalidOperationException">Thrown if writing to the console buffer fails.</exception>
        public void Write(string text, int x = 0, int y = 0)
        {
            var startPosition = new COORD { X = (short)x, Y = (short)y };
            if (!WriteConsoleOutputCharacter(consoleOutputHandle, text,
                (uint)text.Length, startPosition, out _))
            {
                throw new InvalidOperationException("WriteConsoleOutputCharacter failed.");
            }
        }

        public void Write(CharGrid g, int x = 0, int y = 0, bool doNotChunk = false)
        {

            if (doNotChunk)
            {
                Write(g.Arr, x, y);
                return;
            }

            int n = 0;
            for (int i = 0; i < g.Arr.Length; i += g.Width)
            {
                Write(g.Arr[i..(i + g.Width)], x, y + n++);
            }

            //int n = 0;
            //foreach (char[] arr in g.Arr.Chunk(g.Width))
            //{
            //    Write(arr, x, y + n++);
            //}

            // ~ 500 fps
            //string[] s = ;
            //for (int i = 0; i < g.Height; i++)
            //{
            //    Write(s[i], x, y + i);
            //}

            // ~ 500 fps
            //for (int i = 0; i < g.Height; i++)
            //{
            //    Write([.. g.Arr.Take(new Range(g.Width * i, g.Width * (i + 1)))], x, y + i);
            //}

            // < 10 fps
            //for (int i = 0; i < g.Width; i++)
            //{
            //    for (int j = 0; j < g.Height; j++)
            //    {
            //        Write(g[j, i], x + i, y + j);
            //    }
            //}
        }



        // Write a single character at specific coordinates
        public void Write(char c, int x = 0, int y = 0)
        {
            var startPosition = new COORD { X = (short)x, Y = (short)y };
            if (!WriteConsoleOutputCharacter(consoleOutputHandle, c.ToString(), 1, startPosition, out _))
            {
                throw new InvalidOperationException("WriteConsoleOutputCharacter failed.");
            }
        }

        // Write a char[] to the console buffer at specific coordinates (no need to iterate)
        public void Write(char[] chars, int x = 0, int y = 0)
        {
            var startPosition = new COORD { X = (short)x, Y = (short)y };
            if (!WriteConsoleOutputCharacter(consoleOutputHandle, new string(chars), (uint)chars.Length, startPosition, out _))
            {
                throw new InvalidOperationException("WriteConsoleOutputCharacter failed.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleHandler"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if unable to access the console handle.</exception>
        public ConsoleHandler()
        {
            // Set the console's output code page to UTF-8
            SetConsoleOutputCP(CP_UTF8);

            // Initialize the console output handle
            consoleOutputHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            consoleInputHandle = GetStdHandle(STD_INPUT_HANDLE);

            if (consoleOutputHandle != IntPtr.Zero && consoleInputHandle != IntPtr.Zero)
            {
                // Enable virtual terminal processing to support advanced text rendering
                SetConsoleMode(consoleOutputHandle, ENABLE_VIRTUAL_TERMINAL_PROCESSING);

                // Set both input and output code pages to UTF-8
                SetConsoleOutputCP(CP_UTF8);
                SetConsoleCP(CP_UTF8);

                Console.Clear();
            }
            else
            {
                throw new InvalidOperationException("Unable to access console handles.");
            }
        }
    }
}
