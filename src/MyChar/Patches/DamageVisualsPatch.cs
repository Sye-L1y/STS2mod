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
/// 战斗场景 <c>scenes/creature_visuals.tscn</c> 里放了若干张同构图的立绘：
///   <c>%Visuals</c>（完好，<c>scene/combat.png</c>）、<c>%DamagedSprite</c>（70%，贴图固化在场景里）、
///   <c>%TierSprite50</c> / <c>%TierSprite20</c>（50% / 20%，空壳节点，贴图运行时按需加载）。
/// 取「HP 比例 ≤ 阈值」里阈值最小的那一档显示；没到任何阈值就显示完好图。
///
/// ★ 50% / 20% 两档的贴图是**本地专有素材**（用户明确「禁止分发」）：源图与成品都不入公开仓库，
///   别人 clone 后 pck 里没有这两张，节点贴图为 null，该档自动跳过、退回上一档——不会报错。
///   往档位表里加档只需加一行 <see cref="Tiers"/> + 场景里加个同名空壳节点。
///
/// 实现方式：在 <see cref="NCreature"/> 的 <c>_Ready</c> 后订阅本体 <c>Creature.CurrentHpChanged</c> 事件
/// （old, new），收到通知就刷新可见性；<c>_ExitTree</c> 时退订，避免跨战斗残留。
///
/// ★ 与 <see cref="DefeatSwapPatch"/> 的分工：死亡时本补丁只负责隐藏战损图，倒地立绘由后者接管；
///   倒地 / 复活时后者会回调 <see cref="OnDefeatChanged"/>，保证「复活后血量仍然低」时档位图不会被完好图盖住。
/// </summary>
[HarmonyPatch]
internal static class DamageVisualsPatch
{
    /// <summary>一个血量档位。</summary>
    /// <param name="Threshold">HP 比例 ≤ 此值时启用该档（0.70 = 70%）。</param>
    /// <param name="NodeName">场景里 Sprite2D 节点的 unique 名。</param>
    /// <param name="TexturePath">
    /// 运行时加载的贴图路径；<c>null</c> = 贴图已固化在场景节点上（可分发的那档）。
    /// </param>
    private readonly record struct Tier(float Threshold, string NodeName, string? TexturePath);

    /// <summary>
    /// 档位表（阈值越大越轻）。刷新时取「满足 ratio ≤ 阈值」里阈值最小的那档。
    /// </summary>
    private static readonly Tier[] Tiers =
    {
        new(0.70f, "%DamagedSprite", null),
        new(0.50f, "%TierSprite50", MyCharAssets.CombatDamaged50),
        new(0.20f, "%TierSprite20", MyCharAssets.CombatDamaged20),
    };

    /// <summary>每个 NCreature 的运行时状态：事件处理器 + 已解析的档位节点。</summary>
    private sealed class State
    {
        public Action<int, int> Handler = null!;
        public Sprite2D?[] Sprites = Array.Empty<Sprite2D?>();
        public bool Resolved;
    }

    private static readonly Dictionary<ulong, State> States = new();

    /// <summary>素材缺失只提示一次，避免每次进战斗刷屏。</summary>
    private static bool _missingLogged;

    /// <summary>档位解析结果打印一次。</summary>
    private static bool _resolvedLogged;

    private static bool IsMyChar(NCreature creature)
    {
        Creature? entity = creature.Entity;
        return entity != null && entity.IsPlayer && entity.Player?.Character is MyChar;
    }

    /// <summary>首次刷新时把档位节点抓齐，并给空壳节点按需装贴图。</summary>
    private static void Resolve(NCreatureVisuals visuals, State state)
    {
        state.Resolved = true;

        Sprite2D?[] sprites = new Sprite2D?[Tiers.Length];
        for (int i = 0; i < Tiers.Length; i++)
        {
            Tier tier = Tiers[i];
            Sprite2D? sprite = visuals.GetNodeOrNull<Sprite2D>(tier.NodeName);
            if (sprite != null && tier.TexturePath != null && sprite.Texture == null)
            {
                sprite.Texture = LoadOptional(tier.TexturePath, tier.Threshold);
            }

            sprites[i] = sprite;
        }

        // 首次解析时打一行「哪几档可用」，方便在 godot.log 里确认本地专有档有没有装上。
        if (!_resolvedLogged)
        {
            _resolvedLogged = true;
            System.Text.StringBuilder report = new();
            for (int i = 0; i < Tiers.Length; i++)
            {
                bool ok = sprites[i] != null && sprites[i]!.Texture != null;
                report.Append($" {Tiers[i].Threshold:P0}={(ok ? "ok" : "missing")}");
            }

            MegaCrit.Sts2.Core.Logging.Log.Info($"[MyChar] 战损档位就绪：{report}");
        }

        state.Sprites = sprites;
    }

    /// <summary>
    /// 加载可选贴图。不在 mod 里（别人的 clone）就返回 null；预载阶段不能做的事这里做，
    /// 所以先探存在性再 Load，避免 Godot 往控制台喷一串 load 失败。存在性探测复用
    /// <see cref="MyCharAssets.Exists"/>（pck 里 png 是「xxx.png.import + .ctex」两条，裸条目不一定在）。
    /// </summary>
    private static Texture2D? LoadOptional(string path, float threshold)
    {
        if (MyCharAssets.Exists(path))
        {
            return ResourceLoader.Load<Texture2D>(path);
        }

        if (!_missingLogged)
        {
            _missingLogged = true;
            MegaCrit.Sts2.Core.Logging.Log.Info(
                $"[MyChar] 战损档位素材不在 mod 里（本地专有），{threshold:P0} 档跳过：{path}");
        }

        return null;
    }

    /// <summary>按当前 HP 比例在「完好 / 各档战损」之间切换。</summary>
    private static void Refresh(NCreature creature)
    {
        NCreatureVisuals? visuals = creature.Visuals;
        if (visuals == null)
        {
            return;
        }

        Node2D? intact = visuals.GetNodeOrNull<Node2D>("%Visuals");
        if (intact == null)
        {
            return;
        }

        ulong id = creature.GetInstanceId();
        if (!States.TryGetValue(id, out State? state))
        {
            return;
        }

        if (!state.Resolved)
        {
            Resolve(visuals, state);
        }

        // 选人界面上的「局内战损立绘」总开关关掉时，一律显示完好立绘（商店 / 休息同受该开关控制）。
        MyCharSettings.EnsureLoaded();
        if (!MyCharSettings.DamagedPortraits)
        {
            HideAll(state);
            intact.Visible = true;
            return;
        }

        Creature? entity = creature.Entity;
        if (entity == null || entity.IsDead)
        {
            // 死亡交给 DefeatSwapPatch：这里只把战损图收起来，不动 %Visuals。
            HideAll(state);
            return;
        }

        Sprite2D? chosen = null;
        if (entity.MaxHp > 0)
        {
            float ratio = (float)entity.CurrentHp / entity.MaxHp;
            float chosenThreshold = float.MaxValue;
            for (int i = 0; i < Tiers.Length; i++)
            {
                Sprite2D? sprite = state.Sprites[i];
                if (sprite == null || sprite.Texture == null)
                {
                    continue; // 素材缺失的档位跳过
                }

                if (ratio <= Tiers[i].Threshold && Tiers[i].Threshold < chosenThreshold)
                {
                    chosen = sprite;
                    chosenThreshold = Tiers[i].Threshold;
                }
            }
        }

        intact.Visible = chosen == null;
        foreach (Sprite2D? sprite in state.Sprites)
        {
            if (sprite != null)
            {
                sprite.Visible = ReferenceEquals(sprite, chosen);
            }
        }
    }

    /// <summary>把所有档位图收起来（不动 %Visuals）。</summary>
    private static void HideAll(State state)
    {
        foreach (Sprite2D? sprite in state.Sprites)
        {
            if (sprite != null)
            {
                sprite.Visible = false;
            }
        }
    }

    /// <summary>
    /// 由 <see cref="DefeatSwapPatch"/> 在倒地 / 复活时回调。
    /// 倒地：把档位图全收掉，避免低血量时那张图压在倒地立绘上。
    /// 复活：血量可能仍很低，重新按档位算一次。
    /// </summary>
    internal static void OnDefeatChanged(NCreature creature, bool defeated)
    {
        if (!States.TryGetValue(creature.GetInstanceId(), out State? state))
        {
            return;
        }

        if (defeated)
        {
            HideAll(state);
        }
        else
        {
            Refresh(creature);
        }
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    [HarmonyPostfix]
    private static void PostReady(NCreature __instance)
    {
        if (!IsMyChar(__instance))
        {
            return;
        }

        State state = new();
        Action<int, int> handler = (_, _) => Refresh(__instance);
        state.Handler = handler;
        States[__instance.GetInstanceId()] = state;

        __instance.Entity.CurrentHpChanged += handler;
        Refresh(__instance);
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature._ExitTree))]
    [HarmonyPostfix]
    private static void PostExitTree(NCreature __instance)
    {
        ulong id = __instance.GetInstanceId();
        if (States.TryGetValue(id, out State? state))
        {
            if (__instance.Entity != null)
            {
                __instance.Entity.CurrentHpChanged -= state.Handler;
            }

            States.Remove(id);
        }
    }
}
