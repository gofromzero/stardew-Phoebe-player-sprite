using System;
using StardewModdingAPI;

namespace PhoebeFarmer;

public sealed class ModConfig
{
    public bool Enabled { get; set; } = true;
    public string Variant { get; set; } = "are";
    public SButton ToggleKey { get; set; } = SButton.F9;
    public int WalkFrameMs { get; set; } = 180;
    public bool OnlyLocalPlayer { get; set; } = true;

    public void Normalize()
    {
        Variant = string.Equals(Variant, "dumpling", StringComparison.OrdinalIgnoreCase) ? "dumpling" : "are";
        WalkFrameMs = Math.Clamp(WalkFrameMs, 60, 1000);
    }
}
