using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MyCharMod.Patches;

/// <summary>
/// 战损立绘切换（血量档位）。
///
/// 战斗场景 <c>scenes/creature_visuals.tscn</c> 里放了两张同构图的立绘：
///   <c>%Visuals</c>（完好，<c>scene/combat.png</c>）与 <c>%DamagedSprite</c>（战损，<c>scene/combat_damaged.png</c>）。
/// HP 比例 ≤ <see cref="DamagedThreshold"/> 时显示战损、隐藏完好；回到阈值以上再切回来。
///
/// 实现方式：在 <see cref="NCreature"/> 的 <c>_Ready</c> 后订阅本体 <c>Creature.CurrentHpChanged</c> 事件
/// （old, new），收到通知就刷新可见性；<c>_ExitTree</c> 时退订，避免跨战斗残留。
///
/// ★ 档位预留：目前只用 70% 一档（素材只有这一张非露骨战损图）。以后要加档，
///   把 <see cref="DamagedThreshold"/> 换成「阈值 → 素材」的有序表即可，刷新逻辑不变。
/// ★ 与 <see cref="DefeatSwapPatch"/> 的分工：死亡时本补丁只负责隐藏战损图，倒地立绘由后者接管。
/// </summary>
[HarmonyPatch]
internal static class DamageVisualsPatch
{
    /// <summary>HP 比例 ≤ 此值时换成战损立绘（0.70 = 70%）。</summary>
    private const float DamagedThreshold = 0.70f;

    /// <summary>NCreature 实例 id → 订阅的处理器，用于 <c>_ExitTree</c> 时退订。</summary>
    private static readonly Dictionary<ulong, Action<int, int>> Subscriptions = new();

    private static bool IsMyChar(NCreature creature)
    {
        Creature? entity = creature.Entity;
        return entity != null && entity.IsPlayer && entity.Player?.Character is MyChar;
    }

    /// <summary>按当前 HP 比例在「完好 / 战损」两张立绘之间切换。</summary>
    private static void Refresh(NCreature creature)
    {
        NCreatureVisuals? visuals = creature.Visuals;
        if (visuals == null)
        {
            return;
        }

        Node2D? body = visuals.GetNodeOrNull<Node2D>("%Visuals");
        Node2D? damaged = visuals.GetNodeOrNull<Node2D>("%DamagedSprite");
        if (body == null || damaged == null)
        {
            return;
        }

        Creature? entity = creature.Entity;
        if (entity == null || entity.IsDead)
        {
            // 死亡交给 DefeatSwapPatch：这里只把战损图收起来。
            damaged.Visible = false;
            return;
        }

        bool low = entity.MaxHp > 0 && (float)entity.CurrentHp <= entity.MaxHp * DamagedThreshold;
        body.Visible = !low;
        damaged.Visible = low;
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    [HarmonyPostfix]
    private static void PostReady(NCreature __instance)
    {
        if (!IsMyChar(__instance))
        {
            return;
        }

        Action<int, int> handler = (_, _) => Refresh(__instance);
        Subscriptions[__instance.GetInstanceId()] = handler;
        __instance.Entity.CurrentHpChanged += handler;
        Refresh(__instance);
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature._ExitTree))]
    [HarmonyPostfix]
    private static void PostExitTree(NCreature __instance)
    {
        ulong id = __instance.GetInstanceId();
        if (Subscriptions.TryGetValue(id, out Action<int, int>? handler))
        {
            if (__instance.Entity != null)
            {
                __instance.Entity.CurrentHpChanged -= handler;
            }
            Subscriptions.Remove(id);
        }
    }
}
