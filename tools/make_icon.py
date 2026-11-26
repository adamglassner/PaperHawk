"""Generates the PaperHawk icon: src/PaperHawk.Desktop/Assets/paperhawk.ico + .png and assets/paperhawk-256.png.
Run: python tools/make_icon.py   (needs Pillow)

A crimson tile holding a white PDF page with a folded corner, a few lines of text with one highlighted line,
a signature swoosh near the bottom and an amber bookmark ribbon.
"""
from PIL import Image, ImageDraw, ImageFilter
import os

S = 1024
here = os.path.dirname(os.path.abspath(__file__))
root = os.path.dirname(here)


def lerp(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def grad(box, c0, c1):
    x0, y0, x1, y1 = box
    g = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    px = g.load()
    for y in range(max(0, int(y0)), min(S, int(y1))):
        for x in range(max(0, int(x0)), min(S, int(x1))):
            t = (x - x0) / (x1 - x0) * 0.35 + (y - y0) / (y1 - y0) * 0.65
            px[x, y] = lerp(c0, c1, t)
    return g


def mask_of(draw_fn):
    m = Image.new("L", (S, S), 0)
    draw_fn(ImageDraw.Draw(m))
    return m


def shadow(m, color, blur, offset):
    sh = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    sh.paste(color, (0, 0), m)
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(blur)), offset)


img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

# Tile
tile = [96, 96, 928, 928]
tile_mask = mask_of(lambda d: d.rounded_rectangle(tile, radius=196, fill=255))
shadow(mask_of(lambda d: d.rounded_rectangle([110, 130, 914, 934], radius=190, fill=255)), (60, 0, 10, 130), 34, (0, 0))
img.paste(grad(tile, (238, 84, 92, 255), (132, 18, 38, 255)), (0, 0), tile_mask)

# Soft highlight across the top of the tile
hl = Image.new("RGBA", (S, S), (0, 0, 0, 0))
ImageDraw.Draw(hl).ellipse([60, -260, 964, 360], fill=(255, 255, 255, 40))
hl = hl.filter(ImageFilter.GaussianBlur(40))
img.paste(hl, (0, 0), Image.composite(hl.split()[3], Image.new("L", (S, S), 0), tile_mask))

# Page with a folded top-right corner
px0, py0, px1, py1, fold = 268, 200, 756, 840, 150
page_poly = [(px0, py0), (px1 - fold, py0), (px1, py0 + fold), (px1, py1), (px0, py1)]


def page_shape(d):
    d.polygon(page_poly, fill=255)
    # round the three plain corners a little
    for cx, cy in [(px0, py0), (px0, py1), (px1, py1)]:
        pass


page_mask = mask_of(page_shape)
shadow(page_mask, (70, 0, 12, 150), 22, (0, 16))
img.paste(grad([px0, py0, px1, py1], (255, 255, 255, 255), (244, 238, 240, 255)), (0, 0), page_mask)

# Fold
d = ImageDraw.Draw(img)
fold_poly = [(px1 - fold, py0), (px1 - fold, py0 + fold - 18), (px1 - fold + 18, py0 + fold), (px1, py0 + fold)]
fm = mask_of(lambda m: m.polygon(fold_poly, fill=255))
shadow(fm, (90, 10, 20, 110), 10, (-6, 8))
img.paste(grad([px1 - fold, py0, px1, py0 + fold], (250, 214, 218, 255), (226, 170, 178, 255)), (0, 0), fm)

# Text lines
d = ImageDraw.Draw(img)
line = (196, 176, 182, 255)
LX0 = px0 + 70
d.rounded_rectangle([LX0, 286, LX0 + 220, 318], radius=16, fill=(120, 32, 46, 255))  # title
rows = [(392, 420), (462, 380), (602, 400), (672, 300)]
for y, w in rows:
    d.rounded_rectangle([LX0, y, LX0 + w, y + 26], radius=13, fill=line)

# Highlighted line (yellow marker behind a text line)
hy = 532
d.rounded_rectangle([LX0 - 18, hy - 16, LX0 + 372, hy + 42], radius=14, fill=(255, 214, 92, 255))
d.rounded_rectangle([LX0, hy, LX0 + 340, hy + 26], radius=13, fill=(150, 92, 40, 255))

# Signature swoosh
sig = []
import math
for i in range(0, 101):
    t = i / 100
    x = LX0 + 10 + t * 300
    y = 772 - math.sin(t * math.pi * 2.6) * 26 * (1 - t * 0.4) - t * 18
    sig.append((x, y))
d.line(sig, fill=(200, 40, 60, 255), width=16, joint="curve")
d.line([(LX0, 800), (LX0 + 330, 800)], fill=(214, 196, 200, 255), width=6)

# Amber bookmark ribbon hanging from the top edge
bx0, bx1, by1 = 574, 650, 420
rib = [(bx0, py0 - 34), (bx1, py0 - 34), (bx1, by1), ((bx0 + bx1) / 2, by1 - 40), (bx0, by1)]
rm = mask_of(lambda m: m.polygon(rib, fill=255))
shadow(rm, (80, 30, 0, 130), 10, (0, 8))
img.paste(grad([bx0, py0 - 34, bx1, by1], (255, 210, 100, 255), (240, 140, 40, 255)), (0, 0), rm)

out_dir = os.path.join(root, "src", "PaperHawk.Desktop", "Assets")
os.makedirs(out_dir, exist_ok=True)
os.makedirs(os.path.join(root, "assets"), exist_ok=True)
small = img.resize((256, 256), Image.LANCZOS)
small.save(os.path.join(out_dir, "paperhawk.png"))
small.save(os.path.join(root, "assets", "paperhawk-256.png"))
img.save(os.path.join(out_dir, "paperhawk.ico"), sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("icon written")
