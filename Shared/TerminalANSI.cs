using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace TerminalANSI
{
    public static class ConsoleBuffer
    {
        private static SafeFileHandle outputHandle;

        // Public API --------------------------------------------------------

        public static void Initialize()
        {
            outputHandle = CreateFile(
                "CONOUT$",
                0x40000000, // GENERIC_WRITE
                2,          // FILE_SHARE_WRITE
                IntPtr.Zero,
                FileMode.Open,
                0,
                IntPtr.Zero);

            if (outputHandle.IsInvalid)
                throw new Exception("Failed to acquire console output handle.");
        }

        public static void DrawBuffer(CharInfo[] buffer, int width, int height, int left = 0, int top = 0)
        {
            if (outputHandle == null || outputHandle.IsInvalid)
                throw new InvalidOperationException("ConsoleBuffer.Initialize() must be called first.");

            var rect = new SmallRect((short)left, (short)top, (short)(left + width - 1), (short)(top + height - 1));

            bool success = WriteConsoleOutputW(
                outputHandle,
                buffer,
                new Coord((short)width, (short)height),
                new Coord(0, 0),
                ref rect);

            if (!success)
                throw new Exception("WriteConsoleOutputW failed.");
        }

        // Internal structs -------------------------------------------------

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;
            public Coord(short x, short y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)] public ushort Char;
            [FieldOffset(2)] public short Attributes;

            public CharInfo(char character, short attributes)
            {
                Char = character;
                Attributes = attributes;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;

            public SmallRect(short left, short top, short right, short bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        // P/Invoke ---------------------------------------------------------

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteConsoleOutputW(
            SafeFileHandle hConsoleOutput,
            [MarshalAs(UnmanagedType.LPArray), In] CharInfo[] lpBuffer,
            Coord dwBufferSize,
            Coord dwBufferCoord,
            ref SmallRect lpWriteRegion);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);
    }
}
