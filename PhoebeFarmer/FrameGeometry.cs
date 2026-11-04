using System;

namespace PhoebeFarmer;

// Pure geometry shared by texture preparation and the headless checks.
internal static class FrameGeometry
{
    public const int Cell = 40;

    public static (int X, int Y, int Width, int Height) Fit(int width, int height, int targetHeight)
    {
        if (width <= 0 || height <= 0 || targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Frame bounds must be positive.");
        double scale = Math.Min((double)targetHeight / height, 36d / width);
        int w = Math.Max(1, (int)Math.Round(width * scale, MidpointRounding.AwayFromZero));
        int h = Math.Max(1, (int)Math.Round(height * scale, MidpointRounding.AwayFromZero));
        return ((Cell - w) / 2, Cell - h, w, h);
    }

    public static string NextVariant(string current) => current == "are" ? "dumpling" : "are";

    public static int Height(string sheet, int row, int col)
    {
        if (sheet == "crouch" && col < 2) return 23;
        if (sheet == "misc" && row == 0) return col == 3 ? 12 : 22;
        if (sheet == "tools" && col == 2) return 23;
        return 28;
    }
}
