"""为「阿尔法机」的可见 Power 生成占位图标（64x64，透明底）。

本体 power_atlas 的图元就是 64x64（见 images/atlases/power_atlas.sprites/*.tres 的 region），
所以这里也按 64x64 出图，换真美术时保持文件名不变即可。

用法： python tools/make_power_icons.py
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "src" / "MyChar" / "assets" / "images" / "powers"

SIZE = 64
SS = 4  # 超采样倍数，画完再缩回去，边缘更干净

# (文件名, 主色, 画法)
JOBS = [
    ("param_pool", (86, 156, 214)),
    ("protocol", (120, 132, 200)),
    ("pending_energy", (224, 190, 84)),
    ("pending_draw", (120, 196, 150)),
    ("protocol_resonance", (140, 160, 236)),
    ("sustained_protocol", (176, 128, 214)),
    ("full_form", (176, 128, 214)),
    # —— 批次 1 / 批次 2 能力 ——
    ("duospace_positioning", (198, 84, 72)),
    ("infiltration", (96, 196, 196)),
    ("armor_forging", (200, 170, 96)),
    ("personality_training", (196, 110, 196)),
    ("fast_iteration", (120, 196, 150)),
    # —— 补漏（v1.9 反馈） ——
    # 「紧急护壁」的封锁 Power：原来引用 no_attack.png，该图从未生成。
    ("no_attack", (198, 96, 88)),
    # —— 遗留牌补做（v1.9 第十五轮） ——
    ("inertia", (120, 196, 150)),
    ("pipeline", (150, 170, 210)),
    ("infinite_injection", (176, 128, 214)),
]


def new_canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    img = Image.new("RGBA", (SIZE * SS, SIZE * SS), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def draw_param_pool(accent: tuple[int, int, int]) -> Image.Image:
    """传参池：一个容器里浮着三条「参数」。"""
    img, d = new_canvas()
    s = SIZE * SS
    d.ellipse([2 * SS, 2 * SS, s - 2 * SS, s - 2 * SS],
              fill=(24, 26, 34, 235), outline=(*accent, 255), width=2 * SS)

    # 池底
    d.pieslice([14 * SS, 26 * SS, 50 * SS, 52 * SS], 0, 180, fill=(*accent, 70))

    # 三条参数条
    for i, y in enumerate((20, 27, 34)):
        w = 22 - i * 3
        d.rounded_rectangle([32 * SS - w * SS // 2, y * SS - 2 * SS,
                             32 * SS + w * SS // 2, y * SS + 2 * SS],
                            radius=2 * SS, fill=(*accent, 255))

    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def draw_protocol(accent: tuple[int, int, int]) -> Image.Image:
    """协议：三层叠起来的权限层。"""
    img, d = new_canvas()
    s = SIZE * SS
    d.ellipse([2 * SS, 2 * SS, s - 2 * SS, s - 2 * SS],
              fill=(24, 26, 34, 235), outline=(*accent, 255), width=2 * SS)

    for i, y in enumerate((20, 29, 38)):
        inset = 15 + i * 4
        alpha = 255 - i * 55
        d.rounded_rectangle([inset * SS, (y - 4) * SS, (SIZE - inset) * SS, (y + 4) * SS],
                            radius=2 * SS, fill=(*accent, alpha))

    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def draw_pending_energy(accent: tuple[int, int, int]) -> Image.Image:
    """待发能量：一个朝下的能量箭头。"""
    img, d = new_canvas()
    s = SIZE * SS
    d.ellipse([2 * SS, 2 * SS, s - 2 * SS, s - 2 * SS],
              fill=(24, 26, 34, 235), outline=(*accent, 255), width=2 * SS)

    # 下箭头
    d.polygon([(32 * SS, 46 * SS), (16 * SS, 28 * SS), (24 * SS, 28 * SS),
               (24 * SS, 16 * SS), (40 * SS, 16 * SS), (40 * SS, 28 * SS),
               (48 * SS, 28 * SS)], fill=(*accent, 255))
    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def draw_pending_draw(accent: tuple[int, int, int]) -> Image.Image:
    """待发抽牌：两张叠起来的卡。"""
    img, d = new_canvas()
    s = SIZE * SS
    d.ellipse([2 * SS, 2 * SS, s - 2 * SS, s - 2 * SS],
              fill=(24, 26, 34, 235), outline=(*accent, 255), width=2 * SS)

    d.rounded_rectangle([22 * SS, 18 * SS, 42 * SS, 44 * SS], radius=2 * SS,
                        fill=(*accent, 130))
    d.rounded_rectangle([26 * SS, 22 * SS, 46 * SS, 48 * SS], radius=2 * SS,
                        fill=(*accent, 255))
    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def draw_no_attack(accent: tuple[int, int, int]) -> Image.Image:
    """封锁：一把被斜线划掉的剑。"""
    img, d = new_canvas()
    s = SIZE * SS
    d.ellipse([2 * SS, 2 * SS, s - 2 * SS, s - 2 * SS],
              fill=(24, 26, 34, 235), outline=(*accent, 255), width=2 * SS)

    # 剑：竖着的剑身 + 护手 + 剑首
    d.rounded_rectangle([30 * SS, 14 * SS, 34 * SS, 44 * SS], radius=1 * SS,
                        fill=(*accent, 235))
    d.rounded_rectangle([23 * SS, 20 * SS, 41 * SS, 24 * SS], radius=1 * SS,
                        fill=(*accent, 235))
    d.rounded_rectangle([29 * SS, 44 * SS, 35 * SS, 49 * SS], radius=1 * SS,
                        fill=(*accent, 235))

    # 禁止斜线
    d.line([(15 * SS, 15 * SS), (49 * SS, 49 * SS)], fill=(232, 88, 80, 255),
           width=4 * SS)
    return img.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    drawers = {
        "param_pool": draw_param_pool,
        "protocol": draw_protocol,
        "pending_energy": draw_pending_energy,
        "pending_draw": draw_pending_draw,
        "protocol_resonance": draw_protocol,
        "sustained_protocol": draw_protocol,
        "full_form": draw_protocol,
        "duospace_positioning": draw_protocol,
        "infiltration": draw_protocol,
        "armor_forging": draw_protocol,
        "personality_training": draw_protocol,
        "fast_iteration": draw_protocol,
        "no_attack": draw_no_attack,
        "inertia": draw_protocol,
        "pipeline": draw_protocol,
        "infinite_injection": draw_protocol,
    }
    for stem, accent in JOBS:
        img = drawers[stem](accent)
        target = OUT / f"{stem}.png"
        img.save(target, "PNG")
        print(f"{stem:14s} {img.width}x{img.height}  {target.stat().st_size / 1024:5.1f} KB")


if __name__ == "__main__":
    main()