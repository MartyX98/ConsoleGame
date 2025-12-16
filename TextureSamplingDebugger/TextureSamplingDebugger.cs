using Shared;
using WindowsConsoleShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Sharpie;
using TerminalSharpie = Shared.TerminalSharpie;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TextureSamplingDebugger
{
    public class TextureSamplingDebugger
    {
        Vector2 imagePreviewPos = new(50, 2);
        Vector2 pixelSelectionPos = new(0, 0);
        Vector2 imageResize = new(0, 0);
        int imageSelected = 0;
        bool mainLoopRunning = false;
        bool whiteBackground = false;
        DebuggerMode mode = DebuggerMode.ImageSelection;
        SamplingStrategy samplingStrategy = SamplingStrategy.Average;
        TerminalSharpie terminal;
        Textures textures;
        Keyboard keyboard;
        Grid<Pixel> texture;
        Grid<Pixel> texture_resized;

        private enum DebuggerMode
        {
            ImageSelection,
            ImageResize,
            PixelSelection
        }

        private enum SamplingStrategy
        {
            Average,
            MostCommon,
        }

        public TextureSamplingDebugger()
        {
            terminal = new TerminalSharpie();
            textures = new Textures("textures", terminal.colorLookup);
            keyboard = new Keyboard();
        }

        public void Run()
        {
            MainLoop();
        }

        private void MainLoop()
        {
            mainLoopRunning = true;
            while (mainLoopRunning)
            {
                HandleInput();
                UpdateSelectedTexture();
                Draw();
            }
        }

        #region Sampling

        private void UpdateSelectedTexture()
        {
            textures.TryGetTexture(textures.LoadedTextures[imageSelected], out Texture texture_t);
            texture = texture_t.GetMipmap(texture_t.LargestMipmapSize);
            if (imageResize.X == 0 && imageResize.Y == 0)
            {
                texture_resized = texture;
                return;
            }

            int newWidth = texture.Width + (int)imageResize.X;
            int newHeight = texture.Height + (int)imageResize.Y;
            texture_resized = new Grid<Pixel>(newWidth, newHeight);

            // imagine original texture as grid of pixels of some total size
            // and the new mipmap as a grid of pixels on top of it with the same total size
            // but different pixel size (e.g. original 4x4, new 3x3)
            // subpixel is the pixel on the original texture that the new pixel is sampling from
            float subPixelWidth = (float)texture.Width / newWidth;
            float subPixelHeight = (float)texture.Height / newHeight;

            // now we iterate over each pixel in the new mipmap

            for (int y = 0; y < newHeight; y++)
                for (int x = 0; x < newWidth; x++)
                {
                    // sample all subpixels that the new pixel is intersecting with
                    // subpixel iteration;
                    List<Pixel> subPixels = [];
                    int fromX = (int)(x * subPixelWidth);
                    int toX = (int)Math.Clamp((MathF.Ceiling((x + 1) * subPixelWidth)), 0, texture.Width - 1);
                    int fromY = (int)(y * subPixelHeight);
                    int toY = (int)Math.Clamp((MathF.Ceiling((y + 1) * subPixelHeight)), 0, texture.Height - 1);

                    for (int subY = fromY; subY < toY; subY++)
                        for (int subX = fromX; subX < toX; subX++)
                            subPixels.Add(texture[subY, subX]);

                    // now we have all subpixels that the new pixel is sampling from
                    // we cannot introduce new colors since we are limited by palette
                    // we can employ different strategies to pick the color
                    // a) We can average the color and pick the closest color from the palette
                    // b) we can pick the most common color
                    // c) since some subpixels are included only partially, we can weight them by the area they cover
                    // and more.. We might also want to consider different strategies for Alpha channel separately from RGB

                    Pixel[] deduplicatedSubPixels = subPixels.Distinct().ToArray();

                    Pixel[] nonTransparentSubPixels = [.. deduplicatedSubPixels.Where(p => p.rgba32.A == 255)];

                    if (nonTransparentSubPixels.Length == 0)
                    {
                        // if all subpixels are transparent, we pick the first one
                        texture_resized[y, x] = subPixels.FirstOrDefault(terminal.defaultPixel);
                        continue;
                    }

                    if (nonTransparentSubPixels.Length == 1)
                    {
                        // if there is only one non-transparent subpixel, we pick it
                        texture_resized[y, x] = nonTransparentSubPixels[0];
                        continue;
                    }

                    switch (samplingStrategy)
                    {
                        case SamplingStrategy.Average:
                            texture_resized[y, x] = AverageSampleStrategy(nonTransparentSubPixels);
                            break;
                        case SamplingStrategy.MostCommon:
                            texture_resized[y, x] = MostCommonSampleStrategy(nonTransparentSubPixels);
                            break;
                    }
                }
        }

        private Pixel AverageSampleStrategy(IEnumerable<Pixel> pixels)
        {
            return new Pixel(
                textures.GetClosestStyle(new Rgba32(
                    (byte)pixels.Average(p => p.rgba32.R),
                    (byte)pixels.Average(p => p.rgba32.G),
                    (byte)pixels.Average(p => p.rgba32.B)
                    )),
                new Rgba32(
                    (byte)pixels.Average(p => p.rgba32.R),
                    (byte)pixels.Average(p => p.rgba32.G),
                    (byte)pixels.Average(p => p.rgba32.B)
                    )

                );
        }

        private Pixel MostCommonSampleStrategy(IEnumerable<Pixel> pixels)
        {
            var mostCommon = pixels
                .GroupBy(p => p.rgba32)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            Rgba32 mostCommonColor = mostCommon != default ? mostCommon.Key : terminal.defaultPixel.rgba32;
            return new Pixel(textures.GetClosestStyle(mostCommonColor), mostCommonColor);
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            keyboard.Update();
            if (keyboard.IsKeyDown(Keys.Escape, true))
                mainLoopRunning = false;

            if (keyboard.IsKeyDown(Keys.M, true))
            {
                DebuggerMode[] modes = Enum.GetValues<DebuggerMode>();
                mode = modes[((int)mode + 1) % modes.Length];
            }

            if (keyboard.IsKeyDown(Keys.B, true))
            {
                whiteBackground = !whiteBackground;
            }

            switch (mode)
            {
                case DebuggerMode.ImageSelection:
                    ImageSelectionInputHandle();
                    break;
                case DebuggerMode.PixelSelection:
                    PixelSelectionInputHandle();
                    break;
                case DebuggerMode.ImageResize:
                    ImageResizeInputHandle();
                    break;
            }

        }

        private void ImageSelectionInputHandle()
        {
            if (keyboard.IsKeyDown(Keys.ControlKey))
            {
                if (keyboard.IsKeyDown(Keys.Up, true))
                    imagePreviewPos.Y -= 1;
                if (keyboard.IsKeyDown(Keys.Down, true))
                    imagePreviewPos.Y += 1;
                if (keyboard.IsKeyDown(Keys.Left, true))
                    imagePreviewPos.X -= 1;
                if (keyboard.IsKeyDown(Keys.Right, true))
                    imagePreviewPos.X += 1;
            }
            else
            {
                if (keyboard.IsKeyDown(Keys.Up, true))
                {
                    imageSelected--;
                    if (imageSelected < 0)
                        imageSelected = textures.Count - 1;
                    pixelSelectionPos = new Vector2(0, 0);
                }
                if (keyboard.IsKeyDown(Keys.Down, true))
                {
                    imageSelected++;
                    if (imageSelected >= textures.Count)
                        imageSelected = 0;
                    pixelSelectionPos = new Vector2(0, 0);
                }
            }
        }

        private void PixelSelectionInputHandle()
        {
            int increment = 1;
            if (keyboard.IsKeyDown(Keys.ControlKey))
                increment = 10;

            if (keyboard.IsKeyDown(Keys.Up, true))
                pixelSelectionPos.Y -= increment;
            if (keyboard.IsKeyDown(Keys.Down, true))
                pixelSelectionPos.Y += increment;
            if (keyboard.IsKeyDown(Keys.Left, true))
                pixelSelectionPos.X -= increment;
            if (keyboard.IsKeyDown(Keys.Right, true))
                pixelSelectionPos.X += increment;

            pixelSelectionPos.X = pixelSelectionPos.X.Mod(texture_resized.Width);
            pixelSelectionPos.Y = pixelSelectionPos.Y.Mod(texture_resized.Height);

        }

        private void ImageResizeInputHandle()
        {
            int increment = 1;
            if (keyboard.IsKeyDown(Keys.ControlKey))
                increment = 10;

            if (keyboard.IsKeyDown(Keys.Up, true))
                imageResize.Y -= increment;
            if (keyboard.IsKeyDown(Keys.Down, true))
                imageResize.Y += increment;
            if (keyboard.IsKeyDown(Keys.Left, true))
                imageResize.X -= increment;
            if (keyboard.IsKeyDown(Keys.Right, true))
                imageResize.X += increment;

            int minX = (texture.Width - 1) * -1;
            int minY = (texture.Height - 1) * -1;
            if (imageResize.X < minX)
                imageResize.X = minX;
            if (imageResize.Y < minY)
                imageResize.Y = minY;

            if (keyboard.IsKeyDown(Keys.R, true))
                imageResize = new Vector2(0, 0);
            // change sampling strategy
            if (keyboard.IsKeyDown(Keys.S, true))
            {
                SamplingStrategy[] strategies = Enum.GetValues<SamplingStrategy>();
                samplingStrategy = strategies[((int)samplingStrategy + 1) % strategies.Length];
            }
        }

        #endregion

        #region Drawing

        private void Draw()
        {
            terminal.Clear();

            List<(string text, Style style)> controlsInfo = new()
            {
                ("[ESC]: Exit", terminal.defaultStyle),
                ("[M]: Switch mode", terminal.defaultStyle),
                ("[B]: Toggle background (black/white)", whiteBackground ? terminal.defaultStyleInverted : terminal.defaultStyle)
            };

            DrawImage(texture_resized);

            switch (mode)
            {
                case DebuggerMode.ImageSelection:
                    DrawImageSelectionUI();
                    controlsInfo.Add(("[↑↓]: Change texture selection", terminal.defaultStyle));
                    controlsInfo.Add(("[Ctrl] + [↑↓←→]: Move texture position", terminal.defaultStyle));
                    break;
                case DebuggerMode.PixelSelection:
                    DrawPixelSelectionUI();
                    controlsInfo.Add(("[↑↓←→]: Move pixel selection by 1", terminal.defaultStyle));
                    controlsInfo.Add(("[Ctrl] + [↑↓←→]: Move pixel selection by 10", terminal.defaultStyle));
                    break;
                case DebuggerMode.ImageResize:
                    DrawImageResizeUI();
                    controlsInfo.Add(("[↑↓←→]: Resize by 1", terminal.defaultStyle));
                    controlsInfo.Add(("[Ctrl] + [↑↓←→]: Resize by 10", terminal.defaultStyle));
                    controlsInfo.Add(("[R]: Reset texture size", terminal.defaultStyle));
                    controlsInfo.Add(("[S]: Change sampling strategy", terminal.defaultStyle));
                    break;
            }

            for (int i = 0; i < controlsInfo.Count; i++)
            {
                terminal.Write(
                    text: controlsInfo[i].text, 
                    y: terminal.window.Size.Height - 1 - controlsInfo.Count + i, 
                    style: controlsInfo[i].style
                    );
            }

            terminal.Update();
        }

        private void DrawImageSelectionUI()
        {
            terminal.Write("Mode: Texture Selection");
            if (textures.Count == 0)
            {
                terminal.Write("No textures loaded", y: 1);
                terminal.Update();
                return;
            }

            for (int i = 0; i < textures.Count; i++)
            {
                terminal.Write(
                    text: $"Texture {i}: {textures.LoadedTextures[i]}",
                    y: i + 1,
                    style: i == imageSelected ? terminal.defaultStyleInverted : terminal.defaultStyle
                    );
            }
        }

        private void DrawPixelSelectionUI()
        {
            if (DateTime.Now.Second % 2 < 1)
                terminal.Write(' ', (int)(imagePreviewPos.X + pixelSelectionPos.X), (int)(imagePreviewPos.Y + pixelSelectionPos.Y));

            terminal.Write('→', (int)(imagePreviewPos.X - 1), (int)(imagePreviewPos.Y + pixelSelectionPos.Y));
            terminal.Write('←', (int)(imagePreviewPos.X + texture_resized.Width), (int)(imagePreviewPos.Y + pixelSelectionPos.Y));
            terminal.Write('↑', (int)(imagePreviewPos.X + pixelSelectionPos.X), (int)(imagePreviewPos.Y + texture_resized.Height));
            terminal.Write('↓', (int)(imagePreviewPos.X + pixelSelectionPos.X), (int)(imagePreviewPos.Y - 1));

            int y = 0;
            string[] info =
            [
                "Mode: Pixel Selection",
                $"Texture name: {textures.LoadedTextures[imageSelected]}",
                $"Texture size: {texture_resized.Width}x{texture_resized.Height}",
                $"Pixel position: {pixelSelectionPos}",
                $"Pixel style: {texture_resized[pixelSelectionPos].style}",
                $"Pixel color: {texture_resized[pixelSelectionPos].rgba32}",
            ];

            for (; y < info.Length; y++)
                terminal.Write(info[y], y: y);

            y++;
            terminal.Write("Color Pallete:", y: y);
            y++;

            // listing colors to styles mappings and highlighting the selected color
            foreach (var (color, style) in terminal.colorLookup)
            {
                terminal.Write('█', y: y, style: style);

                bool isSelected = color == texture_resized[pixelSelectionPos].rgba32 || style == texture_resized[pixelSelectionPos].style;
                Style colorInfoStyle = isSelected ? terminal.defaultStyleInverted : terminal.defaultStyle;
                string colorInfoText = $"{color}: {style}";
                terminal.Write(colorInfoText, x: 2, y: y, style: colorInfoStyle);
                y++;
            }
            bool isTransparent = texture_resized[pixelSelectionPos].rgba32.A == 0;
            Style transpacencyInfoStyle = isTransparent ? terminal.defaultStyleInverted : terminal.defaultStyle;
            string transparencyInfoText = "  Rgba32(Any, Any, Any, 0): Any [Transparent]";
            terminal.Write(transparencyInfoText, y: y, style: transpacencyInfoStyle);
        }

        private void DrawImageResizeUI()
        {
            string[] info =
            [
                "Mode: Image Resize",
                $"Texture name: {textures.LoadedTextures[imageSelected]}",
                $"Texture size: {texture.Width}x{texture.Height}",
                $"Resize: {imageResize}",
                $"New size: {texture_resized.Width}x{texture_resized.Height} ({(int)(texture_resized.Width / (float)texture.Width * 100)}%, {(int)(texture_resized.Height / (float)texture.Height * 100)}%)",
                $"Sampling strategy: {samplingStrategy}",
            ];

            for (int i = 0; i < info.Length; i++)
                terminal.Write(info[i], y: i);
        }

        private void DrawImage(Grid<Pixel> texture)
        {
            if (whiteBackground)
            {
                Style white = textures.GetClosestStyle(new Rgba32(255, 255, 255, 255));
                for (int y = 0; y < texture.Height; y++)
                {
                    for (int x = 0; x < texture.Width; x++)
                    {
                        terminal.Write('█', (int)imagePreviewPos.X + x, (int)imagePreviewPos.Y + y, white);
                    }
                }
            }
            terminal.Write(pixels:texture, x:(int)imagePreviewPos.X, y:(int)imagePreviewPos.Y);

        }

        #endregion

     
    }
}
