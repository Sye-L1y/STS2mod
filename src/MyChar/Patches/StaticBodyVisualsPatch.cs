using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace MyCharMod.Patches;

/// <summary>
/// 静态立绘场景的兼容补丁（场景美术接入，见 docs/02 §场景类资源）。
///
/// 本体 NMerchantCharacter._Ready / PlayAnimation 会对 GetChild(0) 直接包 MegaSprite，
/// 而 MegaSprite 构造时校验节点必须是 SpineSprite 类，否则抛 InvalidOperationException。
/// 我们的商店场景（scenes/merchant.tscn）第一个子节点是 Sprite2D 静态图，
/// 不拦住的话一进商店就炸。
///
/// 判定条件是「第一个子节点不是 SpineSprite」而不是角色 id：
/// 本体和 Spine 皮肤 mod 的商店场景永远以 SpineSprite 开头，行为不受影响；
/// 只有走静态图场景的（目前只有我们）会被跳过。
/// </summary>
[HarmonyPatch]
internal static class StaticBodyVisualsPatch
{
    private static bool FirstChildIsSpine(NMerchantCharacter instance)
    {
        return instance.GetChildCount(false) > 0
            && instance.GetChild(0, false).GetClass() == "SpineSprite";
    }

    [HarmonyPatch(typeof(NMerchantCharacter), "_Ready")]
    [HarmonyPrefix]
    private static bool SkipMerchantReady(NMerchantCharacter __instance)
    {
        // 原方法只做一件事：等 Spine 就绪后播 relaxed_loop。静态图直接跳过。
        return FirstChildIsSpine(__instance);
    }

    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
    [HarmonyPrefix]
    private static bool SkipMerchantPlayAnimation(NMerchantCharacter __instance)
    {
        return FirstChildIsSpine(__instance);
    }
}

/// <summary>
/// 失败立绘切换：玩家角色死亡时把战斗场景里的 %Visuals（站姿）隐藏、%DefeatSprite（倒地）显示，
/// 复活时换回来。场景里没有这两个节点（比如沿用 ironclad 占位）时什么都不做。
/// </summary>
[HarmonyPatch]
internal static class DefeatSwapPatch
{
    private static bool IsMyChar(NCreature creature)
    {
        var entity = creature.Entity;
        return entity?.IsPlayer == true && entity.Player?.Character is MyChar;
    }

    private static void SetDefeat(NCreature creature, bool defeated)
    {
        var visuals = creature.Visuals;
        if (visuals == null)
        {
            return;
        }

        var body = visuals.GetNodeOrNull<Godot.Node2D>("%Visuals");
        var defeat = visuals.GetNodeOrNull<Godot.Node2D>("%DefeatSprite");
        if (body == null || defeat == null)
        {
            return;
        }

        body.Visible = !defeated;
        defeat.Visible = defeated;
        MegaCrit.Sts2.Core.Logging.Log.Info($"[MyChar] defeat visual {(defeated ? "on" : "off")}.");
    }

    [HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
    [HarmonyPostfix]
    private static void OnDeath(NCreature __instance)
    {
        if (IsMyChar(__instance))
        {
            SetDefeat(__instance, defeated: true);
        }
    }

    [HarmonyPatch(typeof(NCreature), "StartReviveAnim")]
    [HarmonyPostfix]
    private static void OnRevive(NCreature __instance)
    {
        if (IsMyChar(__instance))
        {
            SetDefeat(__instance, defeated: false);
        }
    }
}
