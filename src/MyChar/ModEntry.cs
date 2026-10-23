using BaseLib.Extensions;
using BaseLib.Patches.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod;

/// <summary>
/// mod 入口。带 [ModInitializer] 时，游戏加载完程序集后会反射调用这里指定的静态无参方法。
/// 没有这个特性的话，游戏会退化成对程序集做一次 Harmony.PatchAll。
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        // 角色 / 卡池 / 卡牌 / 遗物都不需要在这里手动注册：
        // ModelDb.Init 会扫描 mod 程序集里所有 AbstractModel 子类并 Activator.CreateInstance，
        // 基类构造函数（BaseLib 的 CustomCharacterModel / CustomCardModel / CustomRelicModel）
        // 会把自己登记进 CustomContentDictionary。
        // 带 [ModInitializer] 时游戏不会自动 PatchAll，得自己打。
        // 迭代的卡面数值显示（IterationPreviewPatch）靠这里生效。
        new Harmony("MyCharMod").TryPatchAll(typeof(ModEntry).Assembly);

        // 先把玩家设置读进来（局内战损立绘开关 / 选人立绘版本），
        // 免得第一场战斗的立绘先按默认值渲染再被纠正。
        MyCharSettings.EnsureLoaded();

        RegisterRelicIcons();
        Log.Info("[MyChar] mod initialized.");
    }

    /// <summary>
    /// 遗物图标没有像角色/卡牌那样的 override 点，BaseLib 提供了 RelicImageOverridePatch 注册表。
    /// RelicIconData(图鉴大图, 局内小图标, 描边图) —— 三格都用同一张 256x256 真图标。
    /// 注册要发生在图标第一次被加载之前，所以放在 Initialize 里。
    /// 图标源图见 .artwork/遗物/，由 tools/make_assets.py 处理成 256² 输出。
    /// </summary>
    private static void RegisterRelicIcons()
    {
        // 九个遗物各自一张真图标（2026-10-11 接入，替代 ember_heart / old_anchor 占位）。
        AddRelicIcon<MyCharStarterRelic>("alpha_chip");           // 起始遗物「阿尔法芯片」
        AddRelicIcon<MyCharStarterRelicAncient>("gamma_chip");    // 先古版「伽马芯片」
        AddRelicIcon<MyCharRelicAccelerator>("accelerator");
        AddRelicIcon<MyCharRelicAccumulation>("accumulation");
        AddRelicIcon<MyCharRelicEnsemble>("ensemble");
        AddRelicIcon<MyCharRelicResponseFeedback>("response_feedback");
        AddRelicIcon<MyCharRelicOmegaProtocol>("omega_protocol");
        AddRelicIcon<MyCharRelicPreProtocol>("pre_protocol");
        AddRelicIcon<MyCharRelicSuperStart>("super_start");
    }

    private static void AddRelicIcon<TRelic>(string pngName) where TRelic : RelicModel
    {
        string path = MyCharAssets.Relic(pngName);
        RelicImageOverridePatch.AddOverride<TRelic>(new RelicIconData(path, path, path));
    }
}