"""把 .artwork 下的原始美术图处理成游戏要求的尺寸，输出到 src/MyChar/assets/images。

尺寸不是猜的，是从游戏/现成角色 mod 的 .pck 里读 CompressedTexture2D 头（GST2 头第 8/12 字节
是宽高）量出来的：
    卡图            500 x 380   （注意不是 4:3 的 500x375）
    选人按钮图      132 x 195
    顶部栏角色图标  128 x 128   （Modder's Manual 写的 88x88 是错的）
    遗物图标        256 x 256
    战斗立绘        512 x 512   （BaseLib 走 .tscn 场景，暂为备用图）
    卡面能量图标    74 x 74     （本体 energy_ironclad.tres 的 atlas region）
    内联能量图标    24 x 24     （本体 sprite_fonts/*_energy_icon.png）

目录约定（2026-10-11 用户整理后）：
    .artwork/人物/   → 角色立绘 / 头像 / 场景立绘 / 选人相关（旧稿带 α 前缀）
    .artwork/遗物/   → 9 个遗物图标
    .artwork/卡牌/   → α / β / γ 协议 Token 卡图
    .artwork/*.png   → 药水 x3 + 能量图标（仍在根目录）
    .artwork/raw/    → 旧暂存区（仅 cards/strike|defend 仍从这里取）

用法： python tools/make_assets.py    （需要 PIL + numpy → 用 E:\\Anaconda\\python.exe）
"""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / ".artwork"
RAW = ART / "raw"
CHARS = ART / "人物"
RELICS = ART / "遗物"
CARDS = ART / "卡牌"
OUT = ROOT / "src" / "MyChar" / "assets" / "images"

CARD = (500, 380)

# 处理方式：
#   crop      = 居中裁剪到目标比例后缩放（源图有实底背景 / 满幅图）
#   cut       = 四角采样抠背景 + 居中留白（源图是纯色背景的单个物件、无 alpha）
#   alpha     = 源图自带透明通道，只做等比缩放 + 居中留白，不抠背景
#   scene_box = 按 alpha bbox 裁掉透明边 → 等比缩放到 (宽,高) 框内（透明立绘图）

# —— 角色立绘 / 头像 / 选人（源在 .artwork/人物/）——
CHAR_JOBS = [
    # 头像：左上角 128² + 图鉴立绘 256²（都是圆形徽记，自带 alpha）
    ("头像.png", "char/hud.png", 128, 128, "alpha"),
    ("头像.png", "char/portrait.png", 256, 256, "alpha"),
    # 选人界面角色砖：用「柔和」优化版（原版 选人按钮.png 留作回退素材）
    ("选人按钮柔和.png", "char/select.png", 132, 195, "crop"),
    # 备用大图（当前代码未引用，保持历史行为）
    ("战斗.png", "char/combat.png", 512, 512, "alpha"),
]

# —— 场景类立绘（源在 .artwork/人物/）——
SCENE_JOBS = [
    # 选人界面背景：char_select_bg 场景矩形 2560x1200（ironclad 原版 offset 实测）
    # 「从 选人1 / 选人2 选一个作为最终」→ 选 选人2（构图更宽、留白更适合 2.13:1）
    ("选人2.png", "scene/select_bg.png", 2560, 1200, "crop"),
    # 战斗立绘：NCreatureVisuals 里 Bounds 约 242x278，立绘高 320 正好占满
    ("战斗.png", "scene/combat.png", 200, 320, "scene_box"),
    # 战损立绘（HP 低于阈值时切换）：同一套构图，只换衣服破损版本
    ("战斗70%.png", "scene/combat_damaged.png", 200, 320, "scene_box"),
    # 失败（倒地）：横躺构图，按高 220 出图
    ("α失败.png", "scene/defeat.png", 340, 220, "scene_box"),
    # 商店：ironclad merchant scale 0.47，高 420 接近原版占比
    ("商店.png", "scene/merchant.png", 220, 420, "scene_box"),
    # 火堆休息：rest_site 场景 root scale 0.76，高 340
    ("休息.png", "scene/rest_site.png", 300, 340, "scene_box"),
]

# —— 遗物（源在 .artwork/遗物/，256²）——
RELICS_JOBS = [
    ("阿尔法芯片.png", "relics/alpha_chip.png", 256, 256, "alpha"),
    ("伽马芯片.png", "relics/gamma_chip.png", 256, 256, "alpha"),
    ("加速器.png", "relics/accelerator.png", 256, 256, "alpha"),
    ("蕴纳.png", "relics/accumulation.png", 256, 256, "alpha"),
    ("共演.png", "relics/ensemble.png", 256, 256, "alpha"),
    ("响应反馈.png", "relics/response_feedback.png", 256, 256, "alpha"),
    ("欧米伽协议.png", "relics/omega_protocol.png", 256, 256, "alpha"),
    # 预协议：源图是不透明的暖灰渐变底 → 需要抠背景
    ("预协议.png", "relics/pre_protocol.png", 256, 256, "cut"),
    ("超级起步.png", "relics/super_start.png", 256, 256, "alpha"),
]

# —— α / β / γ 协议 Token 卡图（源在 .artwork/卡牌/，满幅霓虹字，需居中裁剪）——
CARDS_JOBS = [
    ("阿尔法协议.png", "cards/protocol_alpha.png", *CARD, "crop"),
    ("贝塔协议.jpg", "cards/protocol_beta.png", *CARD, "crop"),
    ("伽马协议.jpg", "cards/protocol_gamma.png", *CARD, "crop"),
]

# —— 基础牌真美术（仍在 raw/）——
BASE_CARD_JOBS = [
    ("card_strike.jpg", "cards/strike.png", *CARD, "crop"),
    ("card_defend.jpg", "cards/defend.png", *CARD, "crop"),
]

# —— 药水 + 能量（源在 .artwork/ 根目录）——
ROOT_JOBS = [
    ("临时协议.png", "potions/temp_protocol.png", 256, 256, "alpha"),
    ("特别参数.png", "potions/special_param.png", 256, 256, "alpha"),
    ("经验记录.png", "potions/exp_record.png", 256, 256, "alpha"),
    ("能量.png", "ui/energy_big.png", 74, 74, "alpha"),
    ("能量.png", "ui/energy_text.png", 24, 24, "alpha"),
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


def fit_box(img: Image.Image, max_w: int, max_h: int, alpha_threshold: int = 16) -> Image.Image:
    """透明立绘图：按 alpha 阈值裁掉透明/杂点边 → 等比缩放到框内（不做画布填充）。
    AI 生成图边缘常有低透明度杂点，getbbox() 会被它们撑大，所以先做阈值化。"""
    alpha = img.getchannel("A").point(lambda a: 255 if a > alpha_threshold else 0)
    bbox = alpha.getbbox()
    if bbox:
        img = img.crop(bbox)
    scale = min(max_w / img.width, max_h / img.height)
    size = (max(1, round(img.width * scale)), max(1, round(img.height * scale)))
    return img.resize(size, Image.Resampling.LANCZOS)


def save(img: Image.Image, dst: str) -> None:
    target = OUT / dst
    target.parent.mkdir(parents=True, exist_ok=True)
    img.save(target, "PNG")
    print(f"{dst:28s} {img.width}x{img.height}  {target.stat().st_size / 1024:6.1f} KB")


def _render(groups, cache: dict, made: dict) -> None:
    """groups: [(base_dir, [(src, dst, w, h, mode), ...]), ...]"""
    for base, jobs in groups:
        for src, dst, w, h, mode in jobs:
            key = str(base / src)
            if key not in cache:
                cache[key] = Image.open(base / src).convert("RGBA")
            img = cache[key]
            if mode == "scene_box":
                img = fit_box(img, w, h)
            elif mode == "cut":
                img = fit_square(cut_background(img), max(w, h))
            elif mode == "alpha":
                img = fit_square(img, max(w, h))
            else:  # crop
                img = center_crop(img, w / h).resize((w, h), Image.Resampling.LANCZOS)
            save(img, dst)
            made[dst] = img


def main() -> int:
    all_jobs = [
        (CHARS, CHAR_JOBS),
        (CHARS, SCENE_JOBS),
        (RELICS, RELICS_JOBS),
        (CARDS, CARDS_JOBS),
        (RAW, BASE_CARD_JOBS),
        (ART, ROOT_JOBS),
    ]
    missing = [str(base / src) for base, jobs in all_jobs for src, *_ in jobs if not (base / src).exists()]
    if missing:
        print(f"缺少原始图：{missing}", file=sys.stderr)
        return 1

    cache: dict[str, Image.Image] = {}
    made: dict[str, Image.Image] = {}
    _render(all_jobs, cache, made)

    # 由已有图派生
    save(make_outline(made["char/hud.png"]), "char/hud_outline.png")
    save(make_locked(made["char/select.png"]), "char/select_locked.png")

    total = sum(len(j) for _, j in all_jobs) + 2
    print(f"\n共 {total} 张 -> {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
