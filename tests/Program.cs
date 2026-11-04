using PhoebeFarmer;

static void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
}

foreach (int w in new[] { 1, 20, 50, 1000 })
foreach (int h in new[] { 1, 20, 50, 1000 })
{
    var f = FrameGeometry.Fit(w, h, 30);
    Check(f.Width >= 1 && f.Width <= 36, "Width fits cell");
    Check(f.Height >= 1 && f.Height <= 30, "Height fits cell");
    Check(f.Y + f.Height == 40, "Feet remain anchored");
    Check(f.X >= 0 && f.X + f.Width <= 40, "No horizontal clipping");
}
var state = (Enabled: true, Variant: "are");
state = FrameGeometry.NextAppearance(state.Enabled, state.Variant);
Check(state == (true, "classic"), "Outfit 1 advances to outfit 2");
state = FrameGeometry.NextAppearance(state.Enabled, state.Variant);
Check(state == (false, "classic"), "Outfit 2 advances to off");
state = FrameGeometry.NextAppearance(state.Enabled, state.Variant);
Check(state == (true, "are"), "Off returns to outfit 1");
Check(FrameGeometry.NextAppearance(false, "are") == (true, "are"), "Legacy disabled config starts at outfit 1");
Check(FrameGeometry.Height("crouch", 0, 0) < FrameGeometry.Height("walk", 0, 0), "Crouch stays shorter");
Check(FrameGeometry.Height("misc", 0, 3) == 12, "Lying pose height");
try { FrameGeometry.Fit(0, 10, 30); throw new Exception("Empty frame accepted"); }
catch (ArgumentOutOfRangeException) { }
Console.WriteLine("Frame geometry and variant cycle checks passed.");
Check(PixelSampler.Sample(new uint[] { 0x010000FF, 0x000000FF, 0, 0 }, 2, 0, 0, 2, 2) == 0,
    "Transparent colored fringe must not become visible");
uint white = PixelSampler.Sample(new uint[] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF }, 2, 0, 0, 2, 2);
Check((white >> 24) == 255 && (white & 255) > 220, "Opaque interior remains opaque and light");
Check(PixelSampler.Sample(new uint[] { 0xFFFFFFFF, 0, 0, 0 }, 2, 0, 0, 2, 2) == 0,
    "Sparse isolated noise stays transparent");
Console.WriteLine("Pixel sampling checks passed.");
uint split = PixelSampler.Sample(new uint[] { 0xFF382430, 0xFF382430, 0xFFF4F2F8, 0xFFF4F2F8 }, 2, 0, 0, 2, 2);
Check((split & 255) < 100 || (split & 255) > 220, "A hard light/dark edge must not become an averaged grey");

var smallStep = FrameGeometry.Fit(200, 280, 28, 240, 300);
var tallStep = FrameGeometry.Fit(200, 300, 28, 240, 300);
Check(smallStep.Width == tallStep.Width, "Walking must preserve width when only frame height changes");
Check(smallStep.Height < tallStep.Height, "Shorter walk frame must not be stretched to full height");
Check(smallStep.Y + smallStep.Height == 40 && tallStep.Y + tallStep.Height == 40, "Shared scale keeps feet anchored");

Check(FrameGeometry.NormalizeVariant("dumpling") == "classic", "Existing outfit 2 configs migrate to classic");
Check(FrameGeometry.NormalizeVariant("CLASSIC") == "classic", "Classic config is case insensitive");
Check(FrameGeometry.NormalizeVariant("unknown") == "are", "Unknown config falls back to outfit 1");
