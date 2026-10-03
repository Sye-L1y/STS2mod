"""把 .artwork/raw 下的原始美术图处理成游戏要求的尺寸，输出到 src/MyChar/assets/images。

尺寸不是猜的，是从游戏/现成角色 mod 的 .pck 里读 CompressedTexture2D 头（GST2 头第 8/12 字节
是宽高）量出来的：
    卡图            500 x 380   （注意不是 4:3 的 500x375）
    选人按钮图      132 x 195
    顶部栏角色图标  128 x 128   （Modder's Manual 写的 88x88 是错的）
    遗物图标        256 x 256
    战斗立绘        512 x 512   （BaseLib 走 .tscn 场景，暂为备用图）

用法： python tools/make_assets.py
"""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
RAW = ROOT / ".artwork" / "raw"
OUT = ROOT / "src" / "MyChar" / "assets" / "images"

CARD = (500, 380)

# (源文件, 目标路径, 宽, 高, 是否抠背景)
JOBS = [
    ("char_portrait.jpg", "char/portrait.png", 256, 256, False),
    ("char_portrait.jpg", "char/select.png", 132, 195, False),
    ("char_portrait.jpg", "char/hud.png", 128, 128, False),
    ("char_portrait.jpg", "char/combat.png", 512, 512, False),
    ("card_strike.jpg", "cards/strike.png", *CARD, False),
    ("card_defend.jpg", "cards/defend.png", *CARD, False),
    ("card_blast.jpg", "cards/blast.png", *CARD, False),
    ("card_ward.jpg", "cards/ward.png", *CARD, False),
    ("card_signature.jpg", "cards/signature.png", *CARD, False),
    ("relic_ember_heart.jpg", "relics/ember_heart.png", 256, 256, True),
    ("relic_old_anchor.jpg", "relics/old_anchor.png", 256, 256, True),
]


def center_crop(img: Image.Image, ratio: float) -> Image.Image:
    w, h = img.size
    if w / h > ratio:
        new_w = int(round(h * ratio))
        left = (w - new_w) // 2
        return img.crop((left, 0, left + new_w, h))
    new_h = int(round(w / ratio))
    top = (h - new_h) // 2
    return img.crop((0, top, w, top + new_h))


def fit_square(img: Image.Image, size: int, pad_ratio: float = 0.06) -> Image.Image:
    """等比缩放到 size 内，居中放到透明画布上。"""
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
    inner = int(size * (1 - 2 * pad_ratio))
    img.thumbnail((inner, inner), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(img, ((size - img.width) // 2, (size - img.height) // 2), img)
    return canvas


def cut_background(img: Image.Image) -> Image.Image:
    """按四角采样出的背景色做软阈值抠图，输出带 alpha 的图。"""
    import numpy as np

    rgb = np.asarray(img.convert("RGB")).astype(np.float32)
    h, w, _ = rgb.shape

    patch = max(4, min(h, w) // 20)
    corners = np.concatenate(
        [
            rgb[:patch, :patch].reshape(-1, 3),
            rgb[:patch, -patch:].reshape(-1, 3),
            rgb[-patch:, :patch].reshape(-1, 3),
            rgb[-patch:, -patch:].reshape(-1, 3),
        ]
    )
    bg = np.median(corners, axis=0)

    dist = np.linalg.norm(rgb - bg, axis=2)
    t0 = float(np.percentile(dist, 12))
    t1 = t0 + 60.0
    alpha = np.clip((dist - t0) / max(t1 - t0, 1e-3), 0.0, 1.0)

    out = img.convert("RGBA")
    a = Image.fromarray((alpha * 255).astype(np.uint8), mode="L")
    a = a.filter(ImageFilter.GaussianBlur(1.0))
    out.putalpha(a)
    return out


def make_outline(img: Image.Image) -> Image.Image:
    """由图标 alpha 生成纯白剪影，供顶部栏描边着色器使用。"""
    alpha = img.convert("RGBA").getchannel("A")
    solid = Image.new("RGBA", img.size, (255, 255, 255, 0))
    solid.putalpha(alpha)
    return solid


def make_locked(img: Image.Image) -> Image.Image:
    """选人界面的未解锁版本：去饱和 + 压暗。"""
    from PIL import ImageEnhance

    rgb = img.convert("RGB")
    rgb = ImageEnhance.Color(rgb).enhance(0.15)
    rgb = ImageEnhance.Brightness(rgb).enhance(0.45)
    return rgb.convert("RGBA")


def save(img: Image.Image, dst: str) -> None:
    target = OUT / dst
    target.parent.mkdir(parents=True, exist_ok=True)
    img.save(target, "PNG")
    print(f"{dst:28s} {img.width}x{img.height}  {target.stat().st_size / 1024:6.1f} KB")


def main() -> int:
    missing = [src for src, *_ in JOBS if not (RAW / src).exists()]
    if missing:
        print(f"缺少原始图：{missing}", file=sys.stderr)
        return 1

    cache: dict[str, Image.Image] = {}
    made: dict[str, Image.Image] = {}

    for src, dst, w, h, cut in JOBS:
        if src not in cache:
            cache[src] = Image.open(RAW / src).convert("RGBA")

        img = cache[src]
        if cut:
            img = fit_square(cut_background(img), max(w, h))
        else:
            img = center_crop(img, w / h).resize((w, h), Image.Resampling.LANCZOS)

        save(img, dst)
        made[dst] = img

    # 由已有图派生
    save(make_outline(made["char/hud.png"]), "char/hud_outline.png")
    save(make_locked(made["char/select.png"]), "char/select_locked.png")

    print(f"\n共 {len(JOBS) + 2} 张 -> {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())