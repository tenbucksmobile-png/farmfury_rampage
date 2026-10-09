"""Crop transparent margins off sprite PNGs (lossless). Animation frame sets share one crop box to stay aligned.

Usage: python trim_sprites.py <Assets/_Project/Art folder>   (Track/ is skipped: the ground is opaque)
"""
import glob
import os
import sys

import numpy as np
from PIL import Image

root = sys.argv[1]
PAD = 2
ALPHA = 8


def bbox(path):
    a = np.asarray(Image.open(path).convert("RGBA"))[..., 3]
    ys, xs = np.where(a > ALPHA)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1, a.shape[1], a.shape[0]


def crop(paths):
    boxes = [bbox(p) for p in paths]
    x0 = max(0, min(b[0] for b in boxes) - PAD)
    y0 = max(0, min(b[1] for b in boxes) - PAD)
    x1 = min(boxes[0][4], max(b[2] for b in boxes) + PAD)
    y1 = min(boxes[0][5], max(b[3] for b in boxes) + PAD)
    for p in paths:
        im = Image.open(p).convert("RGBA")
        before = im.size
        im.crop((x0, y0, x1, y1)).save(p, optimize=True)
        print(f"{os.path.relpath(p, root)}: {before} -> {(x1 - x0, y1 - y0)}")


# Animation sets: every run_/walk_ frame set in a folder shares one box.
for folder in sorted({os.path.dirname(p) for p in glob.glob(os.path.join(root, "**", "*.png"), recursive=True)}):
    pngs = sorted(glob.glob(os.path.join(folder, "*.png")))
    for prefix in ("run_", "walk_"):
        frames = [p for p in pngs if os.path.basename(p).lower().startswith(prefix)]
        if frames:
            crop(frames)
    for p in pngs:
        name = os.path.basename(p).lower()
        if name.startswith(("run_", "walk_")) or os.path.basename(folder).lower() == "track":
            continue  # frames done above; the ground is opaque and must keep its full size
        crop([p])
