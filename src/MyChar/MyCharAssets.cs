namespace MyCharMod;

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

    public static string Image(string relativePath) => Root + relativePath;

    // —— 角色 ——
    public static string CharacterSelect => Image("char/select.png");
    public static string CharacterSelectLocked => Image("char/select_locked.png");
    public static string CharacterHud => Image("char/hud.png");
    public static string CharacterHudOutline => Image("char/hud_outline.png");

    // —— 卡图 ——
    public static string Card(string name) => Image($"cards/{name}.png");

    // —— 遗物图标 ——
    public static string Relic(string name) => Image($"relics/{name}.png");

    // —— 能力（状态栏）图标：本体 power_atlas 的图元是 64x64 ——
    public static string Power(string name) => Image($"powers/{name}.png");
}