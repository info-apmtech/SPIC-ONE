"""
Draws realistic-looking test leaf photos for the farmer portal Task 6 leaf check (smoke tests and screenshots).
Nothing is downloaded: every image is drawn here with Pillow + numpy.

    python tools/farmer-portal-check/make_leaves.py --out <dir>

Writes (JPEG, 1600 x 1200 unless noted):
  healthy.jpg        dark green leaf with lighter veins on a white sheet
  n_deficient.jpg    pale yellow-green leaf, yellow over the whole blade (older-leaf nitrogen pattern)
  interveinal.jpg    green veins with yellow tissue between them (iron / magnesium pattern)
  k_margin.jpg       green leaf with brown, dry, scorched edges (potassium pattern)
  not_leaf.jpg       a printed page on a desk (no leaf)
  rotated_exif.jpg   the healthy leaf stored sideways with EXIF orientation 6 and a GPS block (strip / turn test)
  big.png            2400 x 1800 PNG of the interveinal leaf (resize test)
  fake.jpg           a PDF header saved as .jpg (magic-byte test)
"""
import argparse
import os

import numpy as np
from PIL import Image, ImageFilter

W, H = 1600, 1200
rng = np.random.default_rng(7)


def smooth_noise(scale, sigma):
    n = rng.random((H // scale + 2, W // scale + 2)).astype(np.float32)
    img = Image.fromarray((n * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC).filter(ImageFilter.GaussianBlur(sigma))
    return np.asarray(img).astype(np.float32) / 255.0


def leaf_geometry(angle_deg=-18):
    """Leaf mask, along-axis u (0 base .. 1 tip), signed across v (-1 .. 1), vein strength."""
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    cx, cy = W * 0.5, H * 0.52
    a = np.deg2rad(angle_deg)
    x = (xs - cx) * np.cos(a) + (ys - cy) * np.sin(a)
    y = -(xs - cx) * np.sin(a) + (ys - cy) * np.cos(a)
    length = W * 0.78
    u = (x + length / 2) / length
    uc = np.clip(u, 0, 1)
    half = H * 0.31 * np.clip(np.sin(np.pi * uc ** 0.8), 0, 1) ** 0.72 * (1 - 0.15 * uc)
    # a slightly wavy edge
    half = half * (1 + 0.025 * np.sin(u * 46))
    v = np.where(half > 1, y / np.maximum(half, 1), 9)
    mask = (u > 0) & (u < 1) & (np.abs(v) <= 1)
    # midrib and lateral veins (curving towards the tip)
    mid = np.exp(-(y / (3.5 + 6 * (1 - u))) ** 2)
    phase = (u * 13 - np.abs(v) ** 0.8 * 1.6) % 1.0
    lateral = np.exp(-((np.minimum(phase, 1 - phase)) / 0.035) ** 2) * (np.abs(v) < 0.93)
    fine_phase = (u * 52 + np.abs(v) * 7) % 1.0
    fine = 0.15 * np.exp(-((np.minimum(fine_phase, 1 - fine_phase)) / 0.05) ** 2)
    veins = np.clip(np.maximum(mid, lateral * 0.85) + fine * (np.abs(v) < 0.9), 0, 1)
    # stalk
    stalk = (u < 0) & (u > -0.12) & (np.abs(y) < 6)
    return mask, stalk, u, v, veins


def edge_distance(mask, px):
    """1 at the leaf edge falling to 0 at px pixels inside."""
    m = Image.fromarray((mask * 255).astype(np.uint8))
    inner = np.asarray(m.filter(ImageFilter.MinFilter(px | 1))) > 127
    soft = np.asarray(Image.fromarray(((mask & ~inner) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(px / 3))).astype(np.float32) / 255
    return np.clip(soft * 1.6, 0, 1)


def background():
    base = np.ones((H, W, 3), np.float32) * np.array([244, 242, 236], np.float32)
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    vignette = 1 - 0.10 * (((xs - W / 2) / W) ** 2 + ((ys - H / 2) / H) ** 2) * 4
    grain = (smooth_noise(2, 0.6) - 0.5) * 6
    return base * vignette[..., None] + grain[..., None]


def compose(leaf_rgb, mask, stalk, shadow_offset=(26, 30)):
    bg = background()
    m = Image.fromarray(((mask | stalk) * 255).astype(np.uint8))
    shadow = np.asarray(m.transform(m.size, Image.AFFINE, (1, 0, -shadow_offset[0], 0, 1, -shadow_offset[1])).filter(ImageFilter.GaussianBlur(28))).astype(np.float32) / 255
    bg = bg * (1 - 0.28 * shadow[..., None])
    alpha = np.asarray(m.filter(ImageFilter.GaussianBlur(1.2))).astype(np.float32)[..., None] / 255
    out = bg * (1 - alpha) + leaf_rgb * alpha
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def shade(u, v):
    """Light from the top-left and a gentle fold along the midrib."""
    fold = 1 - 0.10 * np.abs(v) + 0.06 * np.sign(v) * (np.abs(v) < 1)
    return (0.92 + 0.10 * (1 - u)) * fold


def leaf(kind):
    mask, stalk, u, v, veins = leaf_geometry()
    tex = smooth_noise(6, 2.5)
    tex2 = smooth_noise(24, 8)
    s = shade(u, v)[..., None]
    dark = np.array([34, 92, 36], np.float32)
    mid = np.array([58, 128, 50], np.float32)
    vein_col = np.array([118, 170, 92], np.float32)
    yellow = np.array([226, 204, 72], np.float32)
    pale = np.array([196, 196, 92], np.float32)
    brown = np.array([118, 72, 34], np.float32)

    t = (0.45 + 0.55 * tex2)[..., None]
    rgb = dark * t + mid * (1 - t)
    vv = veins[..., None]
    if kind == "healthy":
        rgb = rgb * (1 - 0.65 * vv) + vein_col * 0.65 * vv
    elif kind == "n_deficient":
        # yellow all over, a little greener at the base, veins pale too
        k = np.clip(0.75 + 0.25 * u, 0, 1)[..., None]
        rgb = pale * (1 - k) + yellow * k
        rgb = rgb * (1 - 0.35 * vv) + np.array([214, 214, 140], np.float32) * 0.35 * vv
    elif kind == "interveinal":
        between = np.clip(1 - veins * 2.2, 0, 1)
        between = np.clip(between * (0.85 + 0.3 * tex2), 0, 1)[..., None]
        green_veins = rgb * (1 - 0.3 * vv) + np.array([50, 120, 44], np.float32) * 0.3 * vv
        rgb = green_veins * (1 - between) + yellow * between
    elif kind == "k_margin":
        rgb = rgb * (1 - 0.6 * vv) + vein_col * 0.6 * vv
        e = edge_distance(mask, 60)
        e = np.clip(e * (0.45 + 1.0 * smooth_noise(10, 3)) * (0.8 + 0.4 * u), 0, 1)
        halo = np.clip(e * 1.8 - 0.3, 0, 1)[..., None]
        burnt = np.clip(e * 2.4 - 1.1, 0, 1)[..., None]
        rgb = rgb * (1 - halo) + yellow * halo
        rgb = rgb * (1 - burnt) + brown * burnt
    rgb = rgb * s * (0.94 + 0.12 * tex[..., None])
    stalk_rgb = np.array([88, 130, 60], np.float32)
    rgb = np.where(stalk[..., None], stalk_rgb, rgb)
    return compose(rgb, mask, stalk)


def not_leaf():
    bg = np.ones((H, W, 3), np.float32) * np.array([120, 92, 70], np.float32)  # wooden desk
    bg = bg * (0.85 + 0.3 * smooth_noise(40, 2)[..., None])
    img = Image.fromarray(np.clip(bg, 0, 255).astype(np.uint8))
    page = Image.new("RGB", (900, 1050), (248, 248, 244))
    px = np.asarray(page).copy()
    for i in range(22):
        y = 110 + i * 40
        length = 640 if i % 5 else 380
        px[y:y + 10, 120:120 + length] = (70, 70, 80)
    px[40:80, 120:520] = (30, 60, 140)
    page = Image.fromarray(px).rotate(6, expand=True, fillcolor=(120, 92, 70))
    img.paste(page, (320, 60))
    return img


def exif_bytes(orientation):
    # IFD0: Orientation + GPS pointer; GPS IFD: latitude ref "N" (enough to prove a GPS block is there)
    import struct
    entries = [(0x0112, 3, 1, struct.pack(">HH", orientation, 0)), (0x8825, 4, 1, struct.pack(">I", 8 + 2 + 2 * 12 + 4))]
    ifd = struct.pack(">H", len(entries)) + b"".join(struct.pack(">HHI", t, ty, c) + val for t, ty, c, val in entries) + struct.pack(">I", 0)
    gps = struct.pack(">H", 1) + struct.pack(">HHI", 0x0001, 2, 2) + b"N\0\0\0" + struct.pack(">I", 0)
    return b"Exif\0\0" + b"MM\0*" + struct.pack(">I", 8) + ifd + gps


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    out = ap.parse_args().out
    os.makedirs(out, exist_ok=True)
    for kind in ("healthy", "n_deficient", "interveinal", "k_margin"):
        leaf(kind).save(os.path.join(out, f"{kind}.jpg"), quality=90)
        print("wrote", kind)
    not_leaf().save(os.path.join(out, "not_leaf.jpg"), quality=88)
    # stored sideways (rotated 90 deg counter-clockwise) so that orientation 6 turns it upright again
    healthy = Image.open(os.path.join(out, "healthy.jpg"))
    healthy.rotate(90, expand=True).save(os.path.join(out, "rotated_exif.jpg"), quality=90, exif=exif_bytes(6))
    Image.open(os.path.join(out, "interveinal.jpg")).resize((2400, 1800), Image.BICUBIC).save(os.path.join(out, "big.png"))
    with open(os.path.join(out, "fake.jpg"), "wb") as f:
        f.write(b"%PDF-1.7\n% not a photo\n" + b"0" * 2000)
    print("done:", out)


if __name__ == "__main__":
    main()
