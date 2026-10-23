namespace MyCharMod;

using Godot;

/// <summary>
/// mod 内资源路径的统一入口。
///
/// 目录映射（见 csproj 的 PckPackerSourceDir / PckPackerResPrefix）：
///     src/MyChar/assets/images/char/select.png
///         -> pck 内 MyChar/images/char/select.png
///         -> 游戏里 res://MyChar/images/char/select.png
///
/// 注意不要用游戏自带的 ImageHelper.GetImagePath()：它固定拼 "res://images/"，
/// 指向的是本体资源，不是本 mod 的资源。
/// </summary>
public static class MyCharAssets
{
    private const string Root = "res://MyChar/images/";
    private const string SceneRoot = "res://MyChar/scenes/";

    public static string Image(string relativePath) => Root + relativePath;

    /// <summary>场景类资源（.tscn），由 tools/pck_append.py 追加进 pck（PckPacker 不支持 .tscn）。</summary>
    public static string Scene(string name) => SceneRoot + name + ".tscn";

    // —— 角色 ——
    public static string CharacterSelect => Image("char/select.png");
    public static string CharacterSelectLocked => Image("char/select_locked.png");
    public static string CharacterHud => Image("char/hud.png");
    public static string CharacterHudOutline => Image("char/hud_outline.png");

    // —— 卡图 ——
    public static string Card(string name) => Image($"cards/{name}.png");

    // —— 遗物图标 ——
    public static string Relic(string name) => Image($"relics/{name}.png");

    // —— 药水图标 ——
    public static string Potion(string name) => Image($"potions/{name}.png");

    // —— 能力（状态栏）图标：本体 power_atlas 的图元是 64x64 ——
    public static string Power(string name) => Image($"powers/{name}.png");

    // —— 能量图标 ——
    /// <summary>卡面费用 / 大图标，本体 74x74。</summary>
    public static string EnergyBig => Image("ui/energy_big.png");

    /// <summary>描述内联 [E] 图标，本体 24x24。</summary>
    public static string EnergyText => Image("ui/energy_text.png");

    // —— 战斗立绘战损档位（HP 比例 ≤ 阈值时切换，见 Patches/DamageVisualsPatch.cs）——
    /// <summary>70% 档。贴图直接写在 scenes/creature_visuals.tscn 的 %DamagedSprite 上（可分发）。</summary>
    public static string CombatDamaged => Image("scene/combat_damaged.png");

    /// <summary>50% 档。★ 本地专有素材，不入公开仓库；文件缺失时该档自动退回上一档。</summary>
    public static string CombatDamaged50 => Image("scene/combat_damaged_50.png");

    /// <summary>20% 档。★ 本地专有素材，不入公开仓库；文件缺失时该档自动退回上一档。</summary>
    public static string CombatDamaged20 => Image("scene/combat_damaged_20.png");

    // —— 商店 / 休息立绘战损档位（见 Patches/SceneryDamagePatch.cs）——
    // ★ 商店三档与休息 20% 全部是本地专有素材（用户明确「禁止分发」）：场景里只有空壳节点，
    //   贴图运行时按需加载；别人的 clone 里这三档/一档直接跳过，显示完好立绘。
    public static string MerchantDamaged70 => Image("scene/merchant_damaged_70.png");

    public static string MerchantDamaged50 => Image("scene/merchant_damaged_50.png");

    public static string MerchantDamaged20 => Image("scene/merchant_damaged_20.png");

    public static string RestSiteDamaged20 => Image("scene/rest_site_damaged_20.png");

    // —— 选人界面立绘版本（选人界面上的按钮按 2 / 2+ / 3 / 3+ 切换，见 Patches/SelectScreenPanelPatch.cs）——
    /// <summary>版本「2」——默认版本（改造前一直用的那张），贴图固化在 scenes/select_bg.tscn 里。</summary>
    public static string SelectBg => Image("scene/select_bg.png");

    /// <summary>版本「3」。</summary>
    public static string SelectBg3 => Image("scene/select_bg_3.png");

    /// <summary>版本「2+」（选人2 的露骨差分）。★ 本地专有素材，不入公开仓库；缺失时切换自动跳过。</summary>
    public static string SelectBg2Nsfw => Image("scene/select_bg_2nsfw.png");

    /// <summary>版本「3+」（选人3 的露骨差分）。★ 本地专有素材，不入公开仓库；缺失时切换自动跳过。</summary>
    public static string SelectBg3Nsfw => Image("scene/select_bg_3nsfw.png");

    // —— 可选素材探测 ——
    /// <summary>
    /// 资源是否在当前 mod 里。本地专有素材在别人的 clone 里是缺的，所以加载前一律先探存在性。
    /// pck 里 png 是「xxx.png.import + .godot/imported/*.ctex」两条，裸 .png 条目不一定在，
    /// 所以三条都探一遍（Exists 走重映射、FileExists 走真实条目）。
    /// 注意：FileAccess 要写全 <c>Godot.</c> 前缀（ImplicitUsings 带进来的 System.IO.FileAccess 会撞名）。
    /// </summary>
    public static bool Exists(string path) =>
        ResourceLoader.Exists(path)
        || Godot.FileAccess.FileExists(path)
        || Godot.FileAccess.FileExists(path + ".import");

    /// <summary>按需加载贴图；不在 mod 里（别人的 clone）返回 null，不报错、不刷屏。</summary>
    public static Texture2D? TryLoad(string path) => Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
}