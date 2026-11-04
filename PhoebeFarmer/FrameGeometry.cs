using System;

namespace PhoebeFarmer;

// Pure geometry shared by texture preparation and the headless checks.
internal static class FrameGeometry
{
    public const int Cell = 40;

    public static (int X, int Y, int Width, int Height) Fit(int width, int height, int targetHeight, int referenceWidth = 0, int referenceHeight = 0)
    {
        if (width <= 0 || height <= 0 || targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Frame bounds must be positive.");
        double scale = Math.Min((double)targetHeight / Math.Max(height, referenceHeight), 36d / Math.Max(width, referenceWidth));
        int w = Math.Max(1, (int)Math.Round(width * scale, MidpointRounding.AwayFromZero));
        int h = Math.Max(1, (int)Math.Round(height * scale, MidpointRounding.AwayFromZero));
        return ((Cell - w) / 2, Cell - h, w, h);
    }

    public static string NormalizeVariant(string variant) =>
        string.Equals(variant, "classic", StringComparison.OrdinalIgnoreCase)
        || string.Equals(variant, "dumpling", StringComparison.OrdinalIgnoreCase) ? "classic" : "are";

    public static (bool Enabled, string Variant) NextAppearance(bool enabled, string variant) =>
        !enabled ? (true, "are") : variant == "are" ? (true, "classic") : (false, "classic");

    public static int Height(string sheet, int row, int col, string variant = "are")
    {
        int extra = variant == "classic" ? 4 : 0;
        if (sheet == "crouch" && col < 2) return 23 + extra;
        if (sheet == "misc" && row == 0) return col == 3 ? 12 : 22 + extra;
        if (sheet == "tools" && col == 2) return 23 + extra;
        return 28 + extra;
    }
}
