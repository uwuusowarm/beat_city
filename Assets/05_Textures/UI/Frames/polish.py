"""Procedural texturing pass for the Beat City UI frames.

Renders are flat because cairosvg ignores most SVG filters, so all the
surface detail (grain, brushed metal, scratches, patina, vignette) is
composited here instead.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W, H = 1024, 768
rng = np.random.default_rng(7)


# ---------------------------------------------------------------- helpers
def fbm(shape, octaves=5, base_res=64, seed=None):
    """Fractal value noise in [0,1], smooth-interpolated from coarse to fine."""
    r = np.random.default_rng(seed) if seed is not None else rng
    h, w = shape
    out = np.zeros(shape, np.float32)
    amp, total, res = 1.0, 0.0, float(base_res)
    for _ in range(octaves):
        gh, gw = max(2, int(h / res)), max(2, int(w / res))
        g = (r.random((gh, gw)) * 255).astype(np.uint8)
        up = Image.fromarray(g).resize((w, h), Image.BICUBIC)
        out += np.asarray(up, np.float32) / 255.0 * amp
        total += amp
        amp *= 0.5
        res = max(2.0, res / 2)
    return out / total


def directional_noise(shape, length=48, angle=0, seed=None):
    """White noise smeared along one axis -> brushed-metal streaks."""
    r = np.random.default_rng(seed) if seed is not None else rng
    h, w = shape
    n = (r.random((h, w)) * 255).astype(np.uint8)
    img = Image.fromarray(n)
    if angle == 90:
        img = img.rotate(90, expand=True)
    img = img.filter(ImageFilter.GaussianBlur(0.6))
    # smear: repeated box blur along x
    a = np.asarray(img, np.float32)
    k = np.ones(length, np.float32) / length
    a = np.apply_along_axis(lambda m: np.convolve(m, k, mode="same"), 1, a)
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    if angle == 90:
        img = img.rotate(-90, expand=True)
    a = np.asarray(img, np.float32) / 255.0
    a = a[:h, :w]
    return (a - a.mean()) / (a.std() + 1e-6)  # normalised, mean 0


def scratches(shape, count=260, seed=1):
    """Thin bright/dark hairlines, returned as signed field."""
    r = np.random.default_rng(seed)
    h, w = shape
    img = Image.new("F", (w, h), 0.0)
    d = ImageDraw.Draw(img)
    for _ in range(count):
        x0, y0 = r.integers(0, w), r.integers(0, h)
        ln = r.integers(12, 150)
        ang = r.random() * np.pi
        x1, y1 = x0 + ln * np.cos(ang), y0 + ln * np.sin(ang)
        val = float(r.choice([-1.0, 1.0]) * r.uniform(0.35, 1.0))
        d.line([x0, y0, x1, y1], fill=val, width=int(r.integers(1, 3)))
    a = np.asarray(img, np.float32)
    # blur in 8-bit space (PIL cannot gaussian-blur mode "F"), keeping the sign
    enc = np.clip((a * 0.5 + 0.5) * 255, 0, 255).astype(np.uint8)
    enc = Image.fromarray(enc, "L").filter(ImageFilter.GaussianBlur(0.7))
    return (np.asarray(enc, np.float32) / 255.0 - 0.5) * 2.0


def poly_mask(shapes):
    """shapes: list of ('rect', x,y,w,h,r) or ('poly', [(x,y),...])"""
    m = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(m)
    for s in shapes:
        if s[0] == "rect":
            _, x, y, w, h, rad = s
            d.rounded_rectangle([x, y, x + w, y + h], radius=rad, fill=255)
        else:
            d.polygon(s[1], fill=255)
    return np.asarray(m, np.float32) / 255.0


def soften(mask, px=1.2):
    im = Image.fromarray((mask * 255).astype(np.uint8))
    im = im.filter(ImageFilter.GaussianBlur(px))
    return np.asarray(im, np.float32) / 255.0


def edge_band(mask, px=3):
    """Bright rim right at the inside of a mask boundary."""
    im = Image.fromarray((mask * 255).astype(np.uint8))
    inner = np.asarray(im.filter(ImageFilter.MinFilter(2 * px + 1)), np.float32) / 255.0
    return np.clip(mask - inner, 0, 1)


# ---------------------------------------------------------------- main pass
def polish(src, dst, style):
    base = Image.open(src).convert("RGBA")
    arr = np.asarray(base, np.float32) / 255.0
    rgb, alpha = arr[..., :3].copy(), arr[..., 3]

    sprite = (alpha > 0.05).astype(np.float32)

    if style == "neon":
        panel_shapes = [
            ("rect", 66, 116, 892, 596, 14),
            ("poly", [(366, 36), (658, 36), (678, 56), (678, 112), (658, 132),
                      (366, 132), (346, 112), (346, 56)]),
            ("poly", [(416, 710), (610, 710), (628, 726), (610, 742),
                      (416, 742), (398, 726)]),
        ]
        rail_tint = np.array([0.62, 0.68, 0.74], np.float32)   # cool steel
        patina_tint = np.array([0.55, 0.34, 0.18], np.float32)  # rust
        panel_tint = np.array([0.55, 0.60, 0.70], np.float32)
    else:
        panel_shapes = [
            ("poly", [(90, 98), (934, 98), (982, 143), (982, 625), (934, 670),
                      (90, 670), (42, 625), (42, 143)]),
            ("poly", [(344, 68), (680, 68), (680, 116), (344, 116)]),
            ("poly", [(398, 660), (626, 660), (656, 692), (626, 724),
                      (398, 724), (368, 692)]),
        ]
        rail_tint = np.array([0.95, 0.80, 0.45], np.float32)    # warm gold
        patina_tint = np.array([0.42, 0.30, 0.14], np.float32)  # tarnish
        panel_tint = np.array([0.70, 0.66, 0.55], np.float32)

    panel = soften(poly_mask(panel_shapes), 1.0) * sprite
    metal = np.clip(sprite - panel, 0, 1)

    # Keep texture off the emissive bits (neon line, LEDs, bright gold edges):
    # those carry the read of the frame and must stay crisp.
    mx, mn = rgb.max(2), rgb.min(2)
    sat = (mx - mn) / (mx + 1e-5)
    lum = rgb @ np.array([0.299, 0.587, 0.114], np.float32)
    emissive = np.clip(sat * 1.6 - 0.35, 0, 1) * np.clip(lum * 2.4, 0, 1)
    emissive = soften(emissive, 1.5)
    keep = 1.0 - np.clip(emissive * 1.5, 0, 0.92)   # 1 = texture freely, 0 = protect
    metal_t = metal * keep                          # texture-receiving metal

    # ---- 1. global fine grain (film / print noise, keeps flats alive)
    grain = fbm((H, W), octaves=6, base_res=5, seed=11) - 0.5
    rgb += (grain * 0.038 * sprite)[..., None]

    # ---- 2. brushed metal on the rails, streaked along each rail's axis
    brush_h = directional_noise((H, W), length=26, angle=0, seed=21)
    brush_v = directional_noise((H, W), length=26, angle=90, seed=22)
    # horizontal rails = top/bottom bands, vertical rails = left/right bands
    xs = np.linspace(0, W - 1, W)[None, :].repeat(H, 0)
    ys = np.linspace(0, H - 1, H)[:, None].repeat(W, 1)
    horiz_zone = np.clip(1.0 - np.minimum(ys, H - 1 - ys) / 190.0, 0, 1)
    vert_zone = np.clip(1.0 - np.minimum(xs, W - 1 - xs) / 190.0, 0, 1)
    s = horiz_zone + vert_zone + 1e-6
    brush = (brush_h * horiz_zone + brush_v * vert_zone) / s
    rgb += (brush * 0.042 * metal_t)[..., None] * rail_tint

    # ---- 3. mid-scale mottling / cast unevenness on metal
    mottle = fbm((H, W), octaves=4, base_res=110, seed=31) - 0.5
    rgb *= (1.0 + (mottle * 0.15 * metal_t))[..., None]

    # ---- 4. patina & grime: coarse blotches, biased to corners and seams
    blot = fbm((H, W), octaves=4, base_res=150, seed=41)
    blot = np.clip((blot - 0.60) * 3.6, 0, 1)
    corner_bias = np.clip((np.abs(xs - W / 2) / (W / 2)) ** 2 +
                          (np.abs(ys - H / 2) / (H / 2)) ** 2, 0, 1)
    patina = blot * (0.25 + 0.75 * corner_bias) * metal_t
    rgb = rgb * (1 - patina * 0.24)[..., None] + patina[..., None] * patina_tint * 0.24

    # ---- 5. scratches and edge wear on metal
    sc = scratches((H, W), count=300, seed=51) * metal_t
    rgb += (sc * 0.065)[..., None]
    rim = edge_band(metal, 2) * (fbm((H, W), 3, 40, 61) > 0.5)
    rgb += (rim * 0.11 * keep)[..., None] * rail_tint   # polished highlights
    rgb -= (edge_band(metal, 4) * 0.035 * keep)[..., None]  # soot in the seams

    # ---- 6. broad specular sweep across the whole sprite
    sweep = np.clip(1.0 - np.abs((xs / W * 0.75 + ys / H * 0.25) - 0.34) * 2.6, 0, 1) ** 2
    rgb += (sweep * 0.06 * metal_t)[..., None] * rail_tint

    # ---- 7. panel interior: sheen, vignette, dust
    pgrain = fbm((H, W), octaves=5, base_res=10, seed=71) - 0.5
    rgb += (pgrain * 0.030 * panel)[..., None] * panel_tint
    pmot = fbm((H, W), octaves=3, base_res=200, seed=81) - 0.5
    rgb *= (1.0 + (pmot * 0.11 * panel))[..., None]
    vign = np.clip(1.0 - ((xs - W / 2) / (W * 0.70)) ** 2
                       - ((ys - H / 2) / (H * 0.70)) ** 2, 0, 1)
    rgb *= (1.0 - (1.0 - vign) * 0.24 * panel)[..., None]
    psheen = np.clip(1.0 - np.abs((xs / W * 0.6 + ys / H * 0.4) - 0.22) * 3.0, 0, 1) ** 2
    rgb += (psheen * 0.045 * panel)[..., None] * panel_tint
    # faint horizontal scanlines, neon panel only
    if style == "neon":
        scan = (np.sin(ys * np.pi / 3.0) * 0.5 + 0.5)
        rgb *= (1.0 - scan * 0.022 * panel)[..., None]

    # ---- 8. inner shadow so the panel reads as recessed behind the frame
    inner = soften(panel, 7) * panel
    rgb *= (1.0 - (1.0 - inner) * 0.30 * panel)[..., None]

    # ---- 9. re-assert the emissive elements so texture never dulls them
    rgb += (emissive * 0.14)[..., None] * (rail_tint if style == "gold"
                                           else np.array([1.0, 0.55, 0.28], np.float32))

    out = np.clip(np.dstack([rgb, alpha]) * 255, 0, 255).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(dst)
    print("wrote", dst)


if __name__ == "__main__":
    polish("frame_neon.png", "frame_neon_polished.png", "neon")
    polish("frame_gold.png", "frame_gold_polished.png", "gold")
