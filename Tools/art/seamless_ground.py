"""Make a ground image tile seamlessly top-to-bottom and save it at game size.

Usage: python seamless_ground.py <input.png> <output ground.png> [band_px=256] [width=1024]
The bottom band is cross-faded into the top, so the last row flows into the first.
"""
import sys

import numpy as np
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]
band = int(sys.argv[3]) if len(sys.argv) > 3 else 256
width = int(sys.argv[4]) if len(sys.argv) > 4 else 1024

im = np.asarray(Image.open(src).convert("RGB"), dtype=np.float32)
h = im.shape[0]
a = np.linspace(0, 1, band, dtype=np.float32)[:, None, None]
top = im[h - band:] * (1 - a) + im[:band] * a
out = np.concatenate([top, im[band:h - band]], axis=0)
print("seam diff", np.abs(out[0] - out[-1]).mean().round(1), "adjacent rows", np.abs(out[0] - out[1]).mean().round(1))
img = Image.fromarray(out.clip(0, 255).astype(np.uint8))
img = img.resize((width, round(width * img.height / img.width)), Image.LANCZOS)
img.save(dst, optimize=True)
print("saved", dst, img.size)
