using Sharpie;
using Sharpie.Abstractions;
using Sharpie.Backend;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Shared
{
    public struct StyledLine
    {
        public string Line;
        public Style Style;
        public int X;
        public int Y;

        public void Set(string text, Style style, int x, int y)
        {
            Line = text;
            Style = style;
            X = x;
            Y = y;
        }
    }

    class TerminalSharpie
    {
        public const int uniqueColorsLimit = 250;
        public ICursesBackend backend;
        public TerminalOptions options;
        public Sharpie.Terminal terminal;
        public Dictionary<Rgba32, Style> colorLookup = [];
        public Dictionary<Rgba32, Style> colorLookupFg = [];
        public Style defaultStyle;
        public Style defaultStyleInverted;
        public Pixel defaultPixel;
        public char defaultChar;
        public int ColorCount => colorLookup.Count;
        public IWindow window;

        public TerminalSharpie(CursesBackendFlavor flavor = CursesBackendFlavor.Any)
        {
            backend = CursesBackend.Load(flavor);
            options = new(
                UseColors: true,
                EchoInput: false,
                UseInputBuffering: false,
                UseMouse: false,
                CaretMode: CaretMode.Invisible
                );
            terminal = new Sharpie.Terminal(backend, options);
            var rec = new Rectangle(0, 0, terminal.Screen.Size.Width, terminal.Screen.Size.Height);
            window = terminal.Screen.Window(rec);

            if (terminal == null)
                throw new InvalidOperationException("Terminal failed to initialize.");

            defaultStyle = new()
            {
                ColorMixture = terminal.Colors.MixColors((short)StandardColor.White, (short)StandardColor.Black)
            };
            defaultStyleInverted = new()
            {
                ColorMixture = terminal.Colors.MixColors((short)StandardColor.Black, (short)StandardColor.White)
            };
            defaultPixel = new Pixel();
            defaultChar = '█';
            LoadColorLookupTable();
        }

        public void LoadColorLookupTable()
        {
            colorLookup.Clear();
            for (short i = 0; i < 255; i++)
            {
                StandardColor color = (StandardColor)i;
                if (color == StandardColor.Default)
                    continue;
                try
                {
                    (short red, short green, short blue) = terminal.Colors.BreakdownColor(color);
                    red = (byte)(red / 1000f * 255);
                    green = (byte)(green / 1000f * 255);
                    blue = (byte)(blue / 1000f * 255);
                    Style styleFg = new()
                    {
                        ColorMixture = terminal.Colors.MixColors(i, (short)StandardColor.Black)
                    };
                    colorLookup[new Rgba32((byte)red, (byte)green, (byte)blue)] = styleFg;

                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        public void Update() => window.Refresh();

        public void Dispose() => terminal?.Dispose();

        public void Resize(TerminalResizeEvent e)
        {
            window.Size = new(e.Size.Width, e.Size.Height);

            using (terminal.AtomicRefresh())
            {
                terminal.Screen.MarkDirty();
                terminal.Screen.Refresh();

                window.MarkDirty();
                window.Refresh();
            }
        }

        public void Write(string text, int x = 0, int y = 0, Style? style = null)
        {
            if (terminal == null)
                throw new InvalidOperationException("Terminal is not initialized.");
            if (style == null)
                style = defaultStyle;
            if (x < 0 || y < 0 || x >= window.Size.Width - 1 || y >= window.Size.Height - 1)
                return;
            window.CaretLocation = new(x, y);
            window.WriteText(text, (Style)style);
        }

        public void Write(char c, int x = 0, int y = 0, Style? style = default)
            => Write(c.ToString(), x, y, style);

        public void Write(char[] chars, int x = 0, int y = 0, Style? style = default)
            => Write(new string(chars), x, y, style);

        public void Write(Pixel pixel, char character, int x = 0, int y = 0) 
            => Write(character, x, y, pixel.style);

        public void Write(Grid<Pixel> pixels, char character = '█', int x = 0, int y = 0)
        {
            for (int _x = 0; _x < pixels.Width; _x++)
                for (int _y = 0; _y < pixels.Height; _y++)
                    Write(character, x + _x, y + _y, pixels[_y, _x].style);
        }

        public void Write(DisplayGrid pixels, int x = 0, int y = 0)
        {
            foreach (StyledLine t in pixels.GetStyledLines())
            {
                Write(t.Line, t.X + x, t.Y + y, t.Style);
            }
        }

        public void Clear()
        {
            window.Clear();
        }
    }
}