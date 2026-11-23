"""Recorta las hojas 3x3 de assets/sprites en sprites individuales con transparencia.

Uso:  python3 DragonMasters/Tools/slice_sprites.py
Salida: DragonMasters/Assets/Resources/Sprites/<hoja>/<nombre>.png
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "assets", "sprites")
DST = os.path.join(ROOT, "DragonMasters", "Assets", "Resources", "Sprites")
MAX_SIZE = 512

SHEETS = {
    "dragon_expressions": ["waving", "pointing", "celebrating", "sad", "sleeping", "surprised", "thinking", "angry", "winking"],
    "dragon_costumes": ["pirate", "aviator", "cyber", "king", "chef", "astronaut", "ninja", "knight", "wizard"],
    "dragon_elements": ["water", "plant", "star", "ice", "stone", "thunder", "red_adult", "elder", "baby_egg"],
    "characters_npcs": ["adventurer", "knight", "pirate", "wizard", "princess", "blacksmith", "merchant", "aviator", "farmer"],
    "enemies": ["slime", "mushroom", "ghost", "skeleton_pirate", "raccoon", "robot", "storm_cloud", "golem", "frog_king"],
    "items_collectibles": ["coin", "coin_pile", "gem", "star", "heart", "potion", "key", "chest", "gift"],
    "weapons": ["sword", "shield", "bow", "axe", "hammer", "staff", "cannon", "slingshot", "spear"],
    "buildings": ["house", "tower", "castle", "windmill", "shop", "tavern", "forge", "lighthouse", "egg_cave"],
    "environment_props": ["fruit_tree", "pine", "palm", "rocks", "flower_bush", "fence", "bridge", "well", "barrel_crate"],
    "vehicles_boats": ["rowboat", "pirate_ship", "galleon", "sailboat", "raft", "submarine", "airship", "steamboat", "drakkar"],
    "effects_vfx": ["explosion", "star_burst", "smoke", "fire", "impact", "confetti", "magic_swirl", "splash", "lightning"],
    "ui_buttons": ["btn_orange", "btn_teal", "btn_purple", "icon_play", "icon_pause", "icon_settings", "icon_home", "icon_sound", "panel_frame"],
}


def cutout(rgb: np.ndarray) -> np.ndarray:
    """Devuelve RGBA quitando el fondo blanco conectado al borde."""
    rgb = rgb.astype(np.float32)
    mn = rgb.min(axis=2)
    mx = rgb.max(axis=2)
    whiteish = (mn > 228) & ((mx - mn) < 26)
    lab, _ = ndimage.label(whiteish)
    border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    border = border[border > 0]
    bg = np.isin(lab, border)
    # quita también huecos blancos grandes encerrados (p.ej. entre brazos) si son muy blancos
    alpha = (~bg).astype(np.float32)
    # borde suave: 1px de difuminado
    alpha = ndimage.gaussian_filter(alpha, 0.8)
    alpha = np.where(bg & (alpha < 0.5), 0, alpha)
    alpha = np.clip(alpha, 0, 1)
    # descontaminar el halo blanco
    a = np.maximum(alpha, 1e-3)[..., None]
    col = (rgb - (1 - alpha[..., None]) * 255.0) / a
    col = np.where(alpha[..., None] > 0.05, col, rgb)
    col = np.clip(col, 0, 255)
    out = np.dstack([col, alpha * 255]).astype(np.uint8)
    return out


def crop_and_save(rgba: np.ndarray, path: str):
    a = rgba[..., 3]
    # descarta motas sueltas muy pequeñas
    lab, n = ndimage.label(a > 20)
    if n > 1:
        sizes = ndimage.sum(np.ones_like(a), lab, range(1, n + 1))
        keep = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s >= 60])
        rgba[..., 3] = np.where(keep | (a <= 20), a, 0) * (keep | (a > 20))
        a = rgba[..., 3]
    ys, xs = np.where(a > 12)
    if len(xs) == 0:
        print("  (vacío)", path)
        return
    pad = 4
    y0, y1 = max(ys.min() - pad, 0), min(ys.max() + pad + 1, a.shape[0])
    x0, x1 = max(xs.min() - pad, 0), min(xs.max() + pad + 1, a.shape[1])
    im = Image.fromarray(rgba[y0:y1, x0:x1], "RGBA")
    im.thumbnail((MAX_SIZE, MAX_SIZE), Image.LANCZOS)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path, optimize=True)
    print("  ", os.path.relpath(path, ROOT), im.size)


def main():
    for sheet, names in SHEETS.items():
        img = np.array(Image.open(os.path.join(SRC, sheet + ".png")).convert("RGB"))
        h, w = img.shape[:2]
        print(sheet)
        for i, name in enumerate(names):
            r, c = divmod(i, 3)
            cell = img[r * h // 3:(r + 1) * h // 3, c * w // 3:(c + 1) * w // 3]
            crop_and_save(cutout(cell), os.path.join(DST, sheet, name + ".png"))
    img = np.array(Image.open(os.path.join(SRC, "dragon_mascot.png")).convert("RGB"))
    print("dragon_mascot")
    crop_and_save(cutout(img), os.path.join(DST, "dragon_mascot", "mascot.png"))


if __name__ == "__main__":
    main()
