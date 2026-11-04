using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhoebeFarmer;

internal static class SpriteSheet
{
    // Generated source art is normalized once, not scaled or scanned during drawing.
    public static Texture2D Prepare(Texture2D source, int cols, int rows, string name, int[][] bounds, string variant)
    {
        if (source.Width < cols * 8 || source.Height < rows * 8)
            throw new InvalidOperationException("Sprite sheet is too small.");
        if (bounds == null || bounds.Length != cols * rows)
            throw new InvalidOperationException("Sprite frame metadata has the wrong length.");
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        var packed = new uint[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) packed[i] = pixels[i].PackedValue;
        bool transparent = false;
        foreach (var p in pixels) if (p.A == 0) { transparent = true; break; }
        if (!transparent) throw new InvalidOperationException("Sprite sheet requires transparent background.");
        int cell = FrameGeometry.Cell, outputWidth = cols * cell;
        var output = new Color[outputWidth * rows * cell];
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                var box = bounds[row * cols + col];
                if (box == null || box.Length != 4 || box[0] < 0 || box[1] < 0 || box[2] <= 0 || box[3] <= 0
                    || (long)box[0] + box[2] > source.Width || (long)box[1] + box[3] > source.Height)
                    throw new InvalidOperationException($"Invalid frame bounds: {name} {row},{col}.");
                int left = box[0], top = box[1], w = box[2], h = box[3];
                int right = left + w - 1, bottom = top + h - 1;
                int referenceWidth = w, referenceHeight = h;
                // Keep walk frames at one scale so the head does not resize on each step.
                if (name == "walk")
                    for (int c = 0; c < cols; c++)
                    {
                        var other = bounds[row * cols + c];
                        if (other == null || other.Length != 4)
                            throw new InvalidOperationException("Invalid walk frame bounds.");
                        referenceWidth = Math.Max(referenceWidth, other[2]);
                        referenceHeight = Math.Max(referenceHeight, other[3]);
                    }
                var fit = FrameGeometry.Fit(w, h, FrameGeometry.Height(name, row, col, variant), referenceWidth, referenceHeight);
                for (int y = 0; y < fit.Height; y++)
                    for (int x = 0; x < fit.Width; x++)
                    {
                        int sx = left + x * w / fit.Width, sy = top + y * h / fit.Height;
                        int ex = Math.Min(right + 1, Math.Max(sx + 1, left + (x + 1) * w / fit.Width));
                        int ey = Math.Min(bottom + 1, Math.Max(sy + 1, top + (y + 1) * h / fit.Height));
                        var p = new Color { PackedValue = PixelSampler.Sample(packed, source.Width, sx, sy, ex, ey) };
                        output[(row * cell + fit.Y + y) * outputWidth + col * cell + fit.X + x] = p;
                    }
            }
        var texture = new Texture2D(source.GraphicsDevice, outputWidth, rows * cell);
        texture.SetData(output);
        return texture;
    }
}
