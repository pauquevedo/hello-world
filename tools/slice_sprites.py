"""Recorta las hojas 3x3 de assets/sprites en sprites individuales con fondo transparente.

Uso: python3 tools/slice_sprites.py <carpeta_hojas> <carpeta_salida>
"""
import sys, pathlib
import numpy as np
from PIL import Image
from scipy import ndimage

NAMES = {
    "dragon_expressions": ["wave", "point", "celebrate", "sad", "sleep", "surprised", "think", "angry", "wink"],
    "dragon_costumes": ["pirate", "aviator", "cyber", "king", "chef", "astronaut", "ninja", "knight", "wizard"],
    "dragon_elements": ["water", "plant", "star", "ice", "stone", "thunder", "red_adult", "elder", "egg"],
    "characters_npcs": ["adventurer", "knight", "pirate", "wizard", "princess", "blacksmith", "merchant", "aviator", "farmer"],
    "enemies": ["slime", "mushroom", "ghost", "skeleton_pirate", "raccoon", "robot", "storm_cloud", "golem", "frog_king"],
    "items_collectibles": ["coin", "coin_pile", "gem", "star", "heart", "potion", "key", "chest", "gift"],
    "weapons": ["sword", "shield", "bow", "axe", "hammer", "staff", "cannon", "slingshot", "spear"],
    "buildings": ["house", "tower", "castle", "windmill", "shop", "tavern", "forge", "lighthouse", "cave"],
    "environment_props": ["fruit_tree", "pine", "palm", "rocks", "flower_bush", "fence", "bridge", "well", "barrel_crate"],
    "vehicles_boats": ["rowboat", "pirate_ship", "galleon", "sailboat", "raft", "submarine", "airship", "steamboat", "drakkar"],
    "effects_vfx": ["explosion", "star_burst", "smoke", "fire", "impact", "confetti", "magic_swirl", "splash", "lightning"],
    "ui_buttons": ["button_orange", "button_teal", "button_purple", "play", "pause", "settings", "home", "sound", "panel"],
}
MAX_SIDE = 384


def alpha_mask(rgb):
    whiteish = rgb.min(axis=2) > 232
    labels, _ = ndimage.label(whiteish)
    border = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    bg = np.isin(labels, border[border > 0])
    alpha = (~bg).astype(np.float32)
    # Suaviza el borde: los píxeles claros junto al fondo pasan a semitransparentes.
    near = ndimage.binary_dilation(bg, iterations=2) & ~bg
    soft = np.clip((255 - rgb.min(axis=2).astype(np.float32)) / 60.0, 0, 1)
    alpha[near] = soft[near]
    return (alpha * 255).astype(np.uint8)


def drop_border_fragments(alpha):
    """Borra trozos que tocan el borde de la celda: son restos del sprite vecino."""
    labels, n = ndimage.label(alpha > 8)
    if n <= 1:
        return alpha
    sizes = ndimage.sum(np.ones_like(labels), labels, range(1, n + 1))
    main = int(np.argmax(sizes)) + 1
    edge = set(np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))) - {0, main}
    out = alpha.copy()
    out[np.isin(labels, list(edge))] = 0
    return out


def save(rgb, alpha, path, cell=False):
    if cell:
        alpha = drop_border_fragments(alpha)
    ys, xs = np.nonzero(alpha > 8)
    if len(xs) == 0:
        return
    pad = 4
    y0, y1 = max(ys.min() - pad, 0), min(ys.max() + pad + 1, alpha.shape[0])
    x0, x1 = max(xs.min() - pad, 0), min(xs.max() + pad + 1, alpha.shape[1])
    img = Image.fromarray(np.dstack([rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1]]), "RGBA")
    img.thumbnail((MAX_SIDE, MAX_SIDE), Image.LANCZOS)
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, optimize=True)


def main(src, dst):
    src, dst = pathlib.Path(src), pathlib.Path(dst)
    for png in sorted(src.glob("*.png")):
        rgb = np.array(Image.open(png).convert("RGB"))
        alpha = alpha_mask(rgb)
        sheet = png.stem
        if sheet not in NAMES:
            save(rgb, alpha, dst / "dragon" / f"{sheet}.png")
            continue
        h, w = alpha.shape
        for i, name in enumerate(NAMES[sheet]):
            r, c = divmod(i, 3)
            ys, xs = slice(r * h // 3, (r + 1) * h // 3), slice(c * w // 3, (c + 1) * w // 3)
            save(rgb[ys, xs], alpha[ys, xs], dst / sheet / f"{name}.png", cell=True)
        print("ok", sheet)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
