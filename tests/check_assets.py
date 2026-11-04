"""Inspect PNGs and record frame rectangles; never modifies an image."""
from pathlib import Path
import argparse
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
LAYOUTS = {"walk": (4, 4), "tools": (4, 3), "melee": (4, 3),
           "crouch": (4, 3), "fishing": (3, 3), "eat": (6, 1), "misc": (4, 2)}

def bands(occupied, count):
    result, start = [], None
    for i, yes in enumerate(list(occupied) + [False]):
        if yes and start is None:
            start = i
        elif not yes and start is not None:
            if i - start >= 3:
                result.append([start, i])
            start = None
    if len(result) < count:
        raise ValueError(f"Expected {count} separated bands; found {len(result)}")
    while len(result) > count:
        i = min(range(len(result)-1), key=lambda j: result[j+1][0]-result[j][1])
        result[i:i+2] = [[result[i][0], result[i+1][1]]]
    return result

assert bands([False]*2 + [True]*4 + [False]*5 + [True]*3, 2) == [[2,6],[11,14]]
assert bands([True]*3 + [False] + [True]*3, 1) == [[0,7]]
try:
    bands([False]*20, 1)
    raise AssertionError("Empty atlas accepted")
except ValueError:
    pass

parser = argparse.ArgumentParser()
parser.add_argument("--write-layout", action="store_true")
parser.add_argument("--partial", action="store_true")
args = parser.parse_args()
total = 0
for variant in ("are", "classic"):
    folder = ROOT / "PhoebeFarmer" / "assets" / variant
    atlas = {}
    for name, (cols, rows) in LAYOUTS.items():
        path = folder / (name + ".png")
        if args.partial and not path.exists():
            continue
        with Image.open(path) as src:
            im = src.convert("RGBA")
        alpha = im.getchannel("A")
        assert alpha.getextrema()[0] == 0, f"No transparent background: {path}"
        solid = alpha.point(lambda p: 255 if p >= 200 else 0)
        row_bands = bands([sum(solid.crop((0,y,im.width,y+1)).tobytes()) >= 3*255
                           for y in range(im.height)], rows)
        rectangles = []
        for top, bottom in row_bands:
            column_bands = bands([sum(solid.crop((x,top,x+1,bottom)).tobytes()) >= 3*255
                                  for x in range(im.width)], cols)
            for left, right in column_bands:
                box = solid.crop((left,top,right,bottom)).getbbox()
                assert box
                l,t,r,b = box
                rectangles.append([left+l,top+t,r-l,b-t])
        assert len(rectangles) == cols*rows
        for x,y,w,h in rectangles:
            assert w >= 8 and h >= 8 and 0 <= x < x+w <= im.width and 0 <= y < y+h <= im.height
        atlas[name] = rectangles
        total += len(rectangles)
        print(f"{variant}/{name}: {im.width}x{im.height}, {len(rectangles)} frames, bounds verified")
    layout_path = folder / "frames.json"
    if args.write_layout:
        layout_path.write_text(json.dumps(atlas,indent=2)+"\n",encoding="utf-8")
    else:
        assert json.loads(layout_path.read_text(encoding="utf-8")) == atlas, f"Stale frame metadata: {variant}"
print(f"Verified {total} frames. " + ("Partial development check." if args.partial else "All 14 sheets present."))
