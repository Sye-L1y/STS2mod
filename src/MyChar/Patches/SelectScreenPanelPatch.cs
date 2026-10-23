using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace MyCharMod.Patches;

/// <summary>
/// 选人界面上的模组开关面板（场景：<c>scenes/select_bg.tscn</c> 里的 <c>MyCharPanel</c>）。
///
/// 只有选中阿尔法时，引擎才会把我们的背景场景 <c>CustomCharacterSelectBg</c> 挂进
/// <c>NCharacterSelectScreen</c> 的 AnimatedBg 容器，所以面板**只有阿尔法的选人界面才有**。
/// 引擎挂完背景后不能再靠 _Ready 取节点，所以补丁挂在
/// <see cref="NCharacterSelectScreen.SelectCharacter"/> 之后，用面板节点名做路标递归找：
/// 不去猜背景节点的名字（引擎里叫「角色ID_bg」），也不碰它的 private 字段。
///
/// 面板两项（互相独立，都全局记忆，见 <see cref="MyCharSettings"/>）：
///   1. <c>DamagedToggle</c> —— 局内是否使用战损立绘切换（战斗 / 商店 / 休息统一受它控制）；
///   2. <c>ArtCycle</c>      —— 手动切换选人界面立绘版本（按 2 / 2+ / 3 / 3+ 循环，含本地专有的
///                              nsfw 差分，缺图的版本自动跳过）。
///
/// 按钮在场景里就设了 <c>focus_mode = 0</c>：不能抢走手柄 / 键盘在选人界面的焦点导航。
///
/// ★ 位置：面板在场景里的 offset 只是编辑器预览值 —— 本场景挂在引擎的 <c>AnimatedBg</c> 下，
///   而 AnimatedBg 带 <c>scale = 1.1</c>，内坐标会被放大约 1.1 倍并整体平移，
///   写死在场景里很容易跑到屏幕外（v1.23 就踩过：内坐标 y=24 → 屏幕 y≈-29，上面的按钮整条看不见）。
///   所以运行时统一用 <see cref="PlacePanel"/> 按**真实视口**把面板贴到屏幕右上，并避让右上角的 ActDropdown。
/// </summary>
[HarmonyPatch]
internal static class SelectScreenPanelPatch
{
    private const string PanelName = "MyCharPanel";
    private const string ToggleName = "DamagedToggle";
    private const string CycleName = "ArtCycle";

    /// <summary>背景贴图节点名（select_bg.tscn 里的 Bg）。</summary>
    private const string BgNodeName = "Bg";

    /// <summary>
    /// 承载本场景的引擎节点 <c>AnimatedBg</c> 的缩放（见引擎
    /// <c>scenes/screens/character_select_screen.tscn</c>，就写死成 1.1）。
    /// 面板在它里面，所以「面板局部尺寸 → 屏幕尺寸」要乘这个系数。
    /// </summary>
    private const float BgScale = 1.1f;

    /// <summary>面板在场景里的局部宽度（= select_bg.tscn 里 MyCharPanel 的 offset 差值）。</summary>
    private const float PanelLocalWidth = 260f;

    /// <summary>面板距屏幕右边缘的距离（屏幕像素）。</summary>
    private const float RightMargin = 24f;

    /// <summary>
    /// 面板距屏幕上边缘的距离：右上角 <c>ActDropdown</c> 的底边在 y=77（另见 ActLabel 34~64），
    /// 留到 110 才不会被它压住。
    /// </summary>
    private const float TopMargin = 110f;

    /// <summary>标记「这个面板实例已经接过线了」，避免重复连信号。</summary>
    private const string WiredMeta = "mychar_wired";

    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
    [HarmonyPostfix]
    private static void PostSelectCharacter(NCharacterSelectScreen __instance)
    {
        Node? panel = __instance.FindChild(PanelName, recursive: true, owned: false);
        if (panel == null)
        {
            return; // 选中的不是阿尔法（或背景还没挂上），没有我们的面板
        }

        Node? bgRoot = panel.GetParent();
        TextureRect? bg = bgRoot?.GetNodeOrNull<TextureRect>(BgNodeName);

        if (!panel.HasMeta(WiredMeta))
        {
            panel.SetMeta(WiredMeta, true);
            Wire(panel);
        }

        MyCharSettings.EnsureLoaded();
        PlacePanel(panel);
        UpdateText(panel);
        ApplySelectArt(bg);
    }

    /// <summary>
    /// 把面板贴到屏幕右上角。
    /// 用 <c>GlobalPosition</c> 而不是 offset：它会自己把祖先（AnimatedBg）的缩放与位移算进去，
    /// 所以写的是「屏幕坐标」；再按当前视口宽度做右对齐 → 换分辨率也不会跑到屏幕外。
    /// </summary>
    private static void PlacePanel(Node panel)
    {
        if (panel is not Control control)
        {
            return;
        }

        Vector2 viewport = control.GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;
        if (viewport.X <= 0f || viewport.Y <= 0f)
        {
            return; // 拿不到视口就保持场景里的位置，别乱动
        }

        // 面板会继承 AnimatedBg 的 1.1 缩放，屏幕上的实际宽度要乘一下，才能真的贴住右边缘。
        float width = PanelLocalWidth * BgScale;
        control.GlobalPosition = new Vector2(viewport.X - width - RightMargin, TopMargin);
    }

    /// <summary>接线：两个按钮各连一个处理函数（信号连接随按钮一起消亡，不用手动退订）。</summary>
    private static void Wire(Node panel)
    {
        Button? toggle = panel.GetNodeOrNull<Button>(ToggleName);
        if (toggle != null)
        {
            toggle.Pressed += () =>
            {
                MyCharSettings.ToggleDamagedPortraits();
                UpdateText(panel);
            };
        }

        Button? cycle = panel.GetNodeOrNull<Button>(CycleName);
        if (cycle != null)
        {
            Node? bgRoot = panel.GetParent();
            TextureRect? bg = bgRoot?.GetNodeOrNull<TextureRect>(BgNodeName);
            cycle.Pressed += () =>
            {
                MyCharSettings.CycleSelectArt();
                UpdateText(panel);
                ApplySelectArt(bg);
            };
        }
    }

    /// <summary>按当前设置刷新按钮文案。</summary>
    private static void UpdateText(Node panel)
    {
        Button? toggle = panel.GetNodeOrNull<Button>(ToggleName);
        if (toggle != null)
        {
            toggle.Text = MyCharSettings.DamagedPortraits ? "局内战损立绘：开" : "局内战损立绘：关";
        }

        Button? cycle = panel.GetNodeOrNull<Button>(CycleName);
        if (cycle != null)
        {
            cycle.Text = $"选人立绘：{MyCharSettings.SelectArtLabel}";
        }
    }

    /// <summary>把设置里选中的选人立绘贴到背景上（缺图就保持原样，不动）。</summary>
    private static void ApplySelectArt(TextureRect? bg)
    {
        if (bg == null)
        {
            return;
        }

        Texture2D? texture = MyCharAssets.TryLoad(MyCharSettings.SelectArts[MyCharSettings.SelectArt].Path);
        if (texture != null)
        {
            bg.Texture = texture;
        }
    }
}
