"""Kling run video -> background-free, size-normalised run-loop frames.

Usage: python kling_run_loop.py <video.mp4> <out_dir> <debug_sheet.png>
Keys the background by colour saturation (works for saturated characters on Kling's neutral grey background;
white/grey characters need a different key), removes drift as the character moves away, finds the cleanest
loop (8..30 frames at 24 fps) and writes 8 frames run_00..run_07.png on one shared canvas.
"""
import sys, os
import numpy as np
import imageio.v3 as iio
from PIL import Image

src, outdir, debug = sys.argv[1], sys.argv[2], sys.argv[3]
frames = np.stack([f for f in iio.imiter(src)]).astype(np.float32) / 255.0  # (N,H,W,3)
N, H, W, _ = frames.shape

def key(f):
    """Alpha from saturation: Kling background and shadow are neutral grey, Cluck is all saturated colour."""
    mx, mn = f.max(-1), f.min(-1)
    sat = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0)
    a = np.clip((sat - 0.13) / (0.26 - 0.13), 0, 1)
    border = np.concatenate([f[0], f[-1], f[:, 0], f[:, -1]])
    bg = np.median(border, axis=0)
    # un-mix the grey background from semi-transparent edge pixels
    rgb = np.where(a[..., None] > 0.02, (f - (1 - a[..., None]) * bg) / np.maximum(a[..., None], 0.02), 0)
    return np.clip(rgb, 0, 1), a

keyed = [key(f) for f in frames]

def bbox(a):
    ys, xs = np.where(a > 0.5)
    return xs.min(), xs.max(), ys.min(), ys.max()

boxes = np.array([bbox(a) for _, a in keyed], dtype=np.float32)  # x0,x1,y0,y1
heights = boxes[:, 3] - boxes[:, 2]
t = np.arange(N)
# Smooth trend of size and position (removes drift-away, keeps per-frame bob)
hfit = np.polyval(np.polyfit(t, heights, 2), t)
cx = (boxes[:, 0] + boxes[:, 1]) / 2
cxfit = np.polyval(np.polyfit(t, cx, 2), t)
bottom = boxes[:, 3]
botfit = np.polyval(np.polyfit(t, bottom, 2), t)

CANVAS = 384
TARGET_H = 300  # trend height maps to this
def normalise(i):
    rgb, a = keyed[i]
    s = TARGET_H / hfit[i]
    rgba = np.dstack([rgb, a])
    im = Image.fromarray((rgba * 255).astype(np.uint8), 'RGBA')
    im = im.resize((max(1, int(W * s)), max(1, int(H * s))), Image.LANCZOS)
    out = Image.new('RGBA', (CANVAS, CANVAS))
    # place so trend centre-x sits at canvas centre and trend bottom sits 20 px above canvas bottom
    ox = int(CANVAS / 2 - cxfit[i] * s)
    oy = int(CANVAS - 20 - botfit[i] * s)
    out.alpha_composite(im, (ox, oy)) if (ox >= 0 and oy >= 0) else out.paste(im, (ox, oy), im)
    return out

norm = [normalise(i) for i in range(N)]
small = np.stack([np.asarray(n.resize((96, 96)), dtype=np.float32) / 255.0 for n in norm])

# Best loop: frames i..j-1 where frame j looks most like frame i, period 8..30 frames (0.33..1.25 s)
best = None
for i in range(5, N - 31):
    for p in range(8, 31):
        d = np.abs(small[i] - small[i + p]).mean()
        if best is None or d < best[0]:
            best = (d, i, p)
d, i0, period = best
print(f'loop start {i0}, period {period} frames ({period/24:.2f} s), diff {d:.4f}')

COUNT = 8
picks = [i0 + round(k * period / COUNT) for k in range(COUNT)]
os.makedirs(outdir, exist_ok=True)
for k, idx in enumerate(picks):
    im = norm[idx]
    # final tight crop shared by all frames is unnecessary: same canvas keeps alignment
    im.save(os.path.join(outdir, f'run_{k:02d}.png'))
print('frames', picks)

# debug sheet on checkerboard
sheet = Image.new('RGBA', (CANVAS * COUNT // 2, CANVAS * 2), (230, 230, 230, 255))
for k, idx in enumerate(picks):
    sheet.alpha_composite(norm[idx], ((k % (COUNT // 2)) * CANVAS, (k // (COUNT // 2)) * CANVAS))
sheet.convert('RGB').save(debug)
