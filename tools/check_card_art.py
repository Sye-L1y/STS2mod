"""核对卡牌「本地化名」与「卡图占位图文件名」是否一致。

原理：tools/make_alpha_cards.py 的 JOBS 决定每个 slug 的 PNG 上写的是什么卡名；
卡牌代码里 MyCharAssets.Card("slug") 决定用哪张 PNG。
两者对不上 → 游戏里卡图显示的是别的卡名（素材复用 / 张冠李戴）。

用法： python tools/check_card_art.py
"""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CARDS = ROOT / "src" / "MyChar" / "Cards"
ART = ROOT / "src" / "MyChar" / "assets" / "images" / "cards"
MAKER = ROOT / "tools" / "make_alpha_cards.py"

# JOBS 里的 (slug, 卡名) 映射
job_re = re.compile(r'\(\s*"([a-z0-9_]+)"\s*,\s*"([^"]+)"\s*,')
slug_to_name: dict[str, str] = {}
for line in MAKER.read_text(encoding="utf-8").splitlines():
    stripped = line.strip()
    if stripped.startswith("(") and "PLACEHOLDER" not in stripped:
        m = job_re.match(stripped)
        if m:
            slug_to_name[m.group(1)] = m.group(2)

portrait_re = re.compile(r'CustomPortraitPath\s*=>\s*MyCharAssets\.Card\("([a-z0-9_]+)"\)')
name_re = re.compile(r'new CardLoc\(\s*\n\s*"([^"]+)"')

rows = []
for cs in sorted(CARDS.glob("*.cs")):
    text = cs.read_text(encoding="utf-8")
    pm = portrait_re.search(text)
    nm = name_re.search(text)
    if not pm or not nm:
        continue
    slug = pm.group(1)
    name = nm.group(1)
    rows.append((cs.stem, name, slug, slug_to_name.get(slug, "<未生成>")))

used = {r[2] for r in rows}
files = {p.stem for p in ART.glob("*.png")}

print("=== 卡图错配（卡名 ≠ 图名） ===")
# 这几张用的是真美术（本体打击 / 防御的图），不属于 make_alpha_cards.py 的占位图，跳过
REAL_ART = {"strike", "defend"}
bad = 0
for cls, name, slug, art_name in rows:
    if slug in REAL_ART:
        continue
    if name != art_name:
        bad += 1
        print(f"  {cls:24s} 卡名={name:6s}  图={slug}({art_name})")
if bad == 0:
    print("  （无）")

print(f"\n=== 引用了但没生成的图 ===")
for cls, name, slug, art_name in rows:
    if not (ART / f"{slug}.png").exists():
        print(f"  {cls:24s} -> {slug}.png 不存在")

print("\n=== 生成了但没被引用的图 ===")
for slug in sorted(files - used):
    print(f"  {slug}.png  ({slug_to_name.get(slug, '?')})")

print(f"\n卡牌 {len(rows)} 张 / 图 {len(files)} 张 / 错配 {bad} 张")
