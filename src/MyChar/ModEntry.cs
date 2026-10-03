using BaseLib.Extensions;
using BaseLib.Patches.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

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

        RegisterRelicIcons();
        Log.Info("[MyChar] mod initialized.");
    }

    /// <summary>
    /// 遗物图标没有像角色/卡牌那样的 override 点，BaseLib 提供了 RelicImageOverridePatch 注册表。
    /// RelicIconData(图鉴大图, 局内小图标, 描边图) —— 暂时三格都用同一张 256x256 占位图。
    /// 注册要发生在图标第一次被加载之前，所以放在 Initialize 里。
    /// </summary>
    private static void RegisterRelicIcons()
    {
        string ember = MyCharAssets.Relic("ember_heart");
        RelicImageOverridePatch.AddOverride<MyCharStarterRelic>(new RelicIconData(ember, ember, ember));
        RelicImageOverridePatch.AddOverride<MyCharStarterRelicAncient>(new RelicIconData(ember, ember, ember));

        // 本轮新增的 7 个专属遗物：真图标待补，先统一复用 old_anchor 占位。
        string placeholder = MyCharAssets.Relic("old_anchor");
        var icon = new RelicIconData(placeholder, placeholder, placeholder);
        RelicImageOverridePatch.AddOverride<MyCharRelicResponseFeedback>(icon);
        RelicImageOverridePatch.AddOverride<MyCharRelicAccelerator>(icon);
        RelicImageOverridePatch.AddOverride<MyCharRelicPreProtocol>(icon);
        RelicImageOverridePatch.AddOverride<MyCharRelicOmegaProtocol>(icon);
        RelicImageOverridePatch.AddOverride<MyCharRelicAccumulation>(icon);
        RelicImageOverridePatch.AddOverride<MyCharRelicEnsemble>(icon);
        RelicImageOverridePatch.AddOverride<MyCharRelicSuperStart>(icon);
    }
}