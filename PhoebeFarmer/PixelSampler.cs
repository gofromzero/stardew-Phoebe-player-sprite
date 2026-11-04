using System;

namespace PhoebeFarmer;

internal static class PixelSampler
{
    // Keep each outfit consistent when reducing its sheets to game pixels.
    private static readonly int[] DefaultPalette = {
        0x302438, 0x514052, 0x796475, 0xA999AB, 0xD3C7DA, 0xF8F2F4,
        0x937247, 0xBF9864, 0xDFBA80, 0xF0D29D, 0xF9E5BD,
        0xCE967E, 0xE9BCA2, 0xFFE1CA, 0xF49DA6, 0xBC657F,
        0x283B72, 0x3655A0, 0x547DD0, 0x89A6E4,
        0x5D367F, 0x925DB1, 0xC391D4, 0xE3C0EE,
        0x9C742C, 0xCFA54D, 0xF0D37B, 0xE8E6EF
    };

    private static readonly int[] ClassicPalette = { 0x25262C,0x41404B,0x737383,0xE4CF87,0xAD9651,0xFFF2BC,0xF1F0F7,0xB8B7CC,0xD6DEEF,0x182650,0x3155D5,0x6097F2,0x8F70C6,0xBE91DC,0xFFE0CC,0xD8AA95,0xDDA3AE };

    public static uint Sample(uint[] pixels, int stride, int x0, int y0, int x1, int y1, bool classic = false)
    {
        var palette = classic ? ClassicPalette : DefaultPalette;
        Span<int> votes = stackalloc int[palette.Length];
        votes.Clear();
        int count = 0;
        for (int yy = 0; yy < 4; yy++)
            for (int xx = 0; xx < 4; xx++)
            {
                int x = x0 + (2 * xx + 1) * (x1 - x0) / 8;
                int y = y0 + (2 * yy + 1) * (y1 - y0) / 8;
                uint p = pixels[y * stride + x];
                int alpha = (int)(p >> 24);
                if (alpha < 200) continue;
                int r = (int)Math.Min(255, (p & 255) * 255 / alpha);
                int g = (int)Math.Min(255, ((p >> 8) & 255) * 255 / alpha);
                int b = (int)Math.Min(255, ((p >> 16) & 255) * 255 / alpha);
                int nearest = 0, distance = int.MaxValue;
                for (int i = 0; i < palette.Length; i++)
                {
                    int color = palette[i];
                    int dr = r - (color >> 16), dg = g - ((color >> 8) & 255), db = b - (color & 255);
                    int d = dr * dr + dg * dg + db * db;
                    if (d < distance) { distance = d; nearest = i; }
                }
                votes[nearest]++;
                count++;
            }
        if (count < 8) return 0;
        int winner = 0;
        for (int i = 1; i < votes.Length; i++) if (votes[i] > votes[winner]) winner = i;
        int best = palette[winner];
        return 0xFF000000u | (uint)(best >> 16) | (uint)(best & 0xFF00) | (uint)((best & 255) << 16);
    }
}
