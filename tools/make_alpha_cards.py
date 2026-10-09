"""为「阿尔法机」的新卡生成占位卡图（500x380），输出到 src/MyChar/assets/images/cards。

只做占位：深色渐变 + 类型配色描边 + 卡名。等有真美术再换掉，命名保持不变即可。

用法： python tools/make_alpha_cards.py
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "src" / "MyChar" / "assets" / "images" / "cards"

CARD = (500, 380)

FONT_CANDIDATES = [
    r"C:\Windows\Fonts\msyh.ttc",
    r"C:\Windows\Fonts\msyhbd.ttc",
    r"C:\Windows\Fonts\simhei.ttf",
]

# 配色：攻击=红，技能=蓝，能力=紫，协议系=青
ATTACK = (198, 84, 72)
SKILL = (86, 156, 214)
POWER = (156, 110, 196)
PROTO = (96, 196, 196)

# (文件名, 卡名, 主色)
JOBS = [
    # —— 起始 / 早期 ——
    ("param_activation", "参数激活", SKILL),
    ("alpha_strike", "阿尔法突袭", ATTACK),
    ("protocol_alpha", "α协议", PROTO),
    ("protocol_beta", "β协议", PROTO),
    ("protocol_gamma", "γ协议", PROTO),
    ("full_response", "完全响应", ATTACK),
    ("inherited_iteration", "继承迭代", SKILL),
    # —— 普通 A 级 ——
    ("protocol_attack", "协议攻击", ATTACK),
    ("foreshadow", "预演", SKILL),
    ("overload_state", "过载状态", SKILL),
    ("assessment", "评估", SKILL),
    ("direct_hit", "正击", ATTACK),
    ("disparity_count", "悬殊计数", SKILL),
    ("param_gather", "参数采集", SKILL),
    ("quick_search", "速检激发", SKILL),
    ("cache_refresh", "缓存刷新", SKILL),
    ("protocol_sample", "协议采样", PROTO),
    ("recursive_practice", "递归练习", SKILL),
    ("step_load", "逐步加载", ATTACK),
    ("full_scan", "全量扫描", ATTACK),
    ("firewall", "防火墙", SKILL),
    ("point_execution", "定点执行", ATTACK),
    # —— 罕见 A 级 ——
    ("protocol_cohesion", "协议凝聚", PROTO),
    ("condense_qi", "凝神聚气", SKILL),
    ("protocol_iteration", "协议迭代", PROTO),
    ("protocol_response", "协议发掘", SKILL),
    ("call", "调用", SKILL),
    ("data_collection", "数据收集", ATTACK),
    ("warmup_loop", "预热循环", SKILL),
    ("loop_compile", "循环编译", SKILL),
    ("protocol_resonance", "协议共振", POWER),
    ("backup_protocol", "备用协议", PROTO),
    # —— 稀有 A 级 ——
    ("protocol_activation", "协议激活", PROTO),
    ("sustained_protocol", "持续协议", POWER),
    # —— 本轮扩池（v0.7） ——
    ("tool_distribution", "小刀塑形", SKILL),
    ("zero_base_start", "零基启动", ATTACK),
    ("double_reconstruct", "双重验证", ATTACK),
    ("all_protocol_broadcast", "全协议广播", ATTACK),
    ("overclock_protocol", "超频协议", SKILL),
    ("protocol_extract", "龙吼模块", ATTACK),
    ("protocol_bulwark", "紧急护壁", SKILL),
    ("protocol_settlement", "歼灭龙息", ATTACK),
    ("alpha_anchor", "阿尔法锚点", ATTACK),
    ("full_form", "完全形态", POWER),
    # —— 本轮扩池（v0.8） ——
    ("infinite_imagination", "无限创想", SKILL),
    ("virtual_real_integration", "虚实结合", SKILL),
    ("storage_reservation", "存储预留", SKILL),
    ("advance_future", "预支未来", SKILL),
    # —— 批次 1（v1.6） ——
    ("duospace_positioning", "复空间定位", SKILL),
    ("group_echo", "群响", ATTACK),
    ("more_the_merrier", "多多益善", SKILL),
    # —— 批次 2（v1.6） ——
    ("weakness_param", "弱点参数", SKILL),
    ("data_injection", "数据注入", ATTACK),
    ("broadcast_command", "广播指令", SKILL),
    ("infiltration", "渗透", POWER),
    ("armor_forging", "甲胄铸造", POWER),
    ("personality_training", "人格训练", POWER),
    ("batch_compile", "批量编译", SKILL),
    ("fast_iteration", "快速迭代", POWER),
    ("concentrated_evolution", "聚神进化", SKILL),
    ("accelerated_compile", "加速编译", SKILL),
    ("high_energy_attachment", "高能附加", SKILL),
    # —— 批次 C（v1.9） ——
    ("stack_grudge", "叠仇", ATTACK),
    # —— 补漏（v1.9 反馈） ——
    # CalamityCoverage 原来引用 "disaster_habit"，该图从未生成 →
    # BaseLib 的 CustomCardPortraitPath 在 ResourceLoader.Load 返回 null 后取 .ResourcePath，
    # 直接抛 NullReferenceException 并中断 ModelDb.Preload 的整段卡面预加载。
    ("calamity_coverage", "厄难覆盖", POWER),
    # —— 遗留牌补做（v1.9 第十五轮） ——
    ("dual_channel", "双通道", SKILL),
    ("inertia", "惯性", POWER),
    ("pipeline", "流水线", POWER),
    ("infinite_injection", "无限注入", POWER),
    ("battle_preset", "战斗预设", ATTACK),
    # —— 卡图错配修正（v1.9 第十六轮 · 2026-10-09） ——
    # 以下卡此前 CustomPortraitPath 复用了别的卡的占位图（卡面上写着别人的卡名）；
    # 本轮各补一张自己的图，并把代码指向新 slug。旧图若仍被原卡使用则保留。
    ("parameter_tuning", "调参", ATTACK),
    ("target_feedback", "目标回馈", ATTACK),
    ("mirror_construction", "镜像构建", SKILL),
    ("iteration_broadcast", "高阶洞察", POWER),
    ("high_speed_upgrade", "高速升级", SKILL),
    ("elegant_formula", "优美公式", POWER),
    ("protocol_core", "协议核心", POWER),
    ("final_echo_stimulus", "终响激发", POWER),
    ("parasitic_disruption", "寄生扰乱", SKILL),
    ("gradient_regression", "梯度回归", POWER),
    ("double_translation", "双重译制", ATTACK),
    ("disaster_habit", "灾难习性", ATTACK),
    ("block_displacement", "格挡置换", SKILL),
    ("protocol_matrix", "协议矩阵", POWER),
    ("redundant_cleanup", "冗余清理", SKILL),
    # —— 遗留牌第二批（v1.11 · 2026-10-09） ——
    ("strike_test", "击打测试", ATTACK),
    ("pre_input", "预输入", SKILL),
    ("data_simulation", "数据模拟", ATTACK),
    # —— 遗留牌第三批（v1.12 · 2026-10-09） ——
    ("cold_breath", "冷息", POWER),
    ("offense_defense", "攻守兼备", ATTACK),
    ("anomaly_analysis", "异常解析", SKILL),
    ("second_input", "二次输入", ATTACK),
    ("multi_layer_output", "多层网络输出", ATTACK),
    ("soul_memory", "灵魂记忆", SKILL),
    # —— 遗留牌第四批（v1.13 · 2026-10-09） ——
    ("adjustment", "调节", POWER),
    ("semantic_attach", "语义附加", SKILL),
]


def load_font(size: int) -> ImageFont.FreeTypeFont:
    for path in FONT_CANDIDATES:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def vertical_gradient(size: tuple[int, int], top: tuple[int, int, int],
                      bottom: tuple[int, int, int]) -> Image.Image:
    w, h = size
    img = Image.new("RGB", size)
    draw = ImageDraw.Draw(img)
    for y in range(h):
        t = y / max(h - 1, 1)
        color = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        draw.line([(0, y), (w, y)], fill=color)
    return img


def make_card(name: str, accent: tuple[int, int, int]) -> Image.Image:
    w, h = CARD
    base = vertical_gradient(CARD, (34, 34, 42), (18, 18, 24)).convert("RGBA")

    overlay = Image.new("RGBA", CARD, (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)

    # 顶部主色光带
    od.rectangle([0, 0, w, 14], fill=(*accent, 220))

    # 中央菱形装饰
    cx, cy, r = w // 2, h // 2 - 20, 62
    od.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)],
               outline=(*accent, 200), width=4)
    od.polygon([(cx, cy - r + 16), (cx + r - 16, cy), (cx, cy + r - 16), (cx - r + 16, cy)],
               fill=(*accent, 60))

    # 外描边
    od.rectangle([3, 3, w - 4, h - 4], outline=(*accent, 180), width=3)

    img = Image.alpha_composite(base, overlay)

    draw = ImageDraw.Draw(img)
    font_title = load_font(40)
    font_tag = load_font(20)

    title = name
    bbox = draw.textbbox((0, 0), title, font=font_title)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text(((w - tw) / 2 - bbox[0], h - th - 46 - bbox[1]), title,
              font=font_title, fill=(238, 238, 244))

    tag = "PLACEHOLDER"
    bbox = draw.textbbox((0, 0), tag, font=font_tag)
    tw = bbox[2] - bbox[0]
    draw.text(((w - tw) / 2 - bbox[0], 26), tag, font=font_tag, fill=(*accent, 235))

    return img.convert("RGB")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for stem, name, accent in JOBS:
        img = make_card(name, accent)
        target = OUT / f"{stem}.png"
        img.save(target, "PNG")
        print(f"{stem:20s} {img.width}x{img.height}  {target.stat().st_size / 1024:6.1f} KB")


if __name__ == "__main__":
    main()