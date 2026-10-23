using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace MyCharMod.Patches;

/// <summary>
/// 商店 / 休息场景的战损立绘切换（与战斗的 <see cref="DamageVisualsPatch"/> 同一套思路，但血量来源不同）。
///
/// 战斗里血量可以直接从 <c>NCreature.Entity</c> 拿；商店 / 休息**没有** Creature 节点，
/// 血量走 <see cref="RunPlayerTracker"/>（本局玩家持久 Creature 的血量）。
///
/// 档位：
///   商店（scenes/merchant.tscn）：%Damaged70 / %Damaged50 / %Damaged20（三档全部本地专有素材）；
///   休息（scenes/rest_site.tscn）：%Damaged20（用户只给了 20% 一档）。
/// 取「HP 比例 ≤ 阈值」里阈值最小的那一档；没有可用档位就显示完好立绘。
///
/// ★ 三档商店图与休息 20% 都是**本地专有素材**（用户明确「禁止分发」）：源图与成品都不入公开仓库，
///   场景里放的是**不写 texture 的空壳节点**，贴图运行时按需加载。别人 clone 后 pck 里没有这些图，
///   节点贴图为 null → 该档自动跳过、退回上一档 —— 不会报错。
///
/// ★★ v1.23b 修挂载时机：原来两个场景都挂在各自的 <c>_Ready</c> Postfix 上，但
///   ① 商店的 <c>NMerchantCharacter._Ready</c> 会被 <see cref="StaticBodyVisualsPatch"/> 的 Prefix 整个跳过
///      （静态图场景要拦住「等 Spine 播动画」那段），Prefix 返回 false 时 Postfix 会不会执行
///      取决于 Harmony 实现，**不能赌** → 改成由那个 Prefix 主动调 <see cref="AttachMerchant"/>；
///   ② 休息的 <c>NRestSiteCharacter._Ready</c> 里第一件事就是 <c>Player.RunState.CurrentActIndex</c>，
///      走 BaseLib 工厂的静态图路径时 <c>Player</c> 可能是 null → 抛 NRE → Postfix 不执行
///      → 改挂到它自己覆写的 <c>_EnterTree</c>（没有任何 Prefix 拦截，必定执行）。
///
/// 生命周期：节点挂 <c>TreeExiting</c> 信号自我摘除；血量事件只有一个全局订阅者（<see cref="RefreshAll"/>），
/// 逐个体检活着的节点。这样不需要去给 <c>NMerchantCharacter</c> 打 <c>_ExitTree</c> 补丁
/// （它没有覆写 _ExitTree，硬打会落到 Node 基类上、影响所有节点）。
/// </summary>
[HarmonyPatch]
internal static class SceneryDamagePatch
{
    /// <summary>一个血量档位。<paramref name="Threshold"/> 越大越轻。</summary>
    private readonly record struct Tier(float Threshold, string NodeName, string Path);

    private static readonly Tier[] MerchantTiers =
    {
        new(0.70f, "%Damaged70", MyCharAssets.MerchantDamaged70),
        new(0.50f, "%Damaged50", MyCharAssets.MerchantDamaged50),
        new(0.20f, "%Damaged20", MyCharAssets.MerchantDamaged20),
    };

    private static readonly Tier[] RestTiers =
    {
        new(0.20f, "%Damaged20", MyCharAssets.RestSiteDamaged20),
    };

    /// <summary>一个场景实例的运行时状态（节点 + 解析出来的档位图）。</summary>
    private sealed class State
    {
        public Node2D Root = null!;
        public Tier[] Tiers = Array.Empty<Tier>();
        public Sprite2D?[] Sprites = Array.Empty<Sprite2D?>();
        public Node2D? Intact;
    }

    private static readonly List<State> Active = new();
    private static bool _hooked;
    private static bool _missingLogged;

    // —— 挂载 / 摘除 ——

    /// <summary>商店角色场景就绪 → 挂上三档战损（由 <see cref="StaticBodyVisualsPatch"/> 的 _Ready Prefix 调用）。</summary>
    internal static void AttachMerchant(Node2D root) => Attach(root, MerchantTiers);

    /// <summary>休息角色场景就绪 → 先绑定场景自己就能拿到的 Player，再挂上 20% 档。</summary>
    internal static void AttachRestSite(Node2D root, Creature? creature)
    {
        RunPlayerTracker.Bind(creature);
        Attach(root, RestTiers);
    }

    private static void Attach(Node2D root, Tier[] tiers)
    {
        // 去重：同一个节点只挂一次（商店那边 Prefix / Postfix 都可能调过来）。
        foreach (State existing in Active)
        {
            if (ReferenceEquals(existing.Root, root))
            {
                Refresh(existing);
                return;
            }
        }

        State state = new() { Root = root, Tiers = tiers };
        Resolve(state);
        Active.Add(state);

        // 节点离开场景树时把自己摘掉，避免之后对着已释放的节点调方法。
        root.TreeExiting += () => Active.Remove(state);

        HookOnce();
        Refresh(state);

        float ratio = RunPlayerTracker.HpRatio;
        Log.Info(
            $"[MyChar] 场景战损档位已挂载：{root.Name}"
            + $"（档位 {tiers.Length} 个，完好立绘 {(state.Intact != null ? "ok" : "missing")}，"
            + $"血比例 {(ratio < 0f ? "未知" : ratio.ToString("P0"))}）");
    }

    private static void HookOnce()
    {
        if (_hooked)
        {
            return;
        }

        _hooked = true;
        RunPlayerTracker.HpChanged += RefreshAll;
    }

    /// <summary>血量 / 设置变化：把所有还活着的场景实例都重算一遍。</summary>
    private static void RefreshAll()
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            State state = Active[i];
            if (!GodotObject.IsInstanceValid(state.Root))
            {
                Active.RemoveAt(i); // 兜底：没收到 TreeExiting 就被释放了
                continue;
            }

            Refresh(state);
        }
    }

    // —— 档位解析 ——

    /// <summary>抓齐完好立绘与各档节点，并给空壳节点按需装贴图。</summary>
    private static void Resolve(State state)
    {
        Node2D root = state.Root;
        state.Intact = root.GetNodeOrNull<Node2D>("Visuals");

        Sprite2D?[] sprites = new Sprite2D?[state.Tiers.Length];
        for (int i = 0; i < state.Tiers.Length; i++)
        {
            Tier tier = state.Tiers[i];
            Sprite2D? sprite = root.GetNodeOrNull<Sprite2D>(tier.NodeName);

            // 空壳节点：贴图没固化在场景里，按需装（本地专有素材在别人的 clone 里是缺的）。
            if (sprite != null && sprite.Texture == null)
            {
                if (MyCharAssets.Exists(tier.Path))
                {
                    sprite.Texture = MyCharAssets.TryLoad(tier.Path);
                }
                else if (!_missingLogged)
                {
                    _missingLogged = true;
                    Log.Info($"[MyChar] 场景战损档素材不在 mod 里（本地专有），跳过：{tier.Path}");
                }
            }

            sprites[i] = sprite;
        }

        state.Sprites = sprites;

        System.Text.StringBuilder report = new();
        for (int i = 0; i < sprites.Length; i++)
        {
            report.Append($" {state.Tiers[i].Threshold:P0}={(sprites[i]?.Texture != null ? "ok" : "missing")}");
        }

        Log.Info($"[MyChar] 场景战损档位就绪（{root.Name}）：{report}");
    }

    // —— 按血量切档 ——

    private static void Refresh(State state)
    {
        MyCharSettings.EnsureLoaded();

        // 关掉总开关 → 一律显示完好立绘（战斗 / 商店 / 休息统一受它控制）。
        float ratio = MyCharSettings.DamagedPortraits ? RunPlayerTracker.HpRatio : -1f;

        Sprite2D? chosen = null;
        if (ratio >= 0f)
        {
            float chosenThreshold = float.MaxValue;
            for (int i = 0; i < state.Tiers.Length; i++)
            {
                Sprite2D? sprite = state.Sprites[i];
                if (sprite == null || sprite.Texture == null)
                {
                    continue; // 素材缺失的档位跳过
                }

                if (ratio <= state.Tiers[i].Threshold && state.Tiers[i].Threshold < chosenThreshold)
                {
                    chosen = sprite;
                    chosenThreshold = state.Tiers[i].Threshold;
                }
            }
        }

        if (state.Intact != null)
        {
            state.Intact.Visible = chosen == null;
        }

        foreach (Sprite2D? sprite in state.Sprites)
        {
            if (sprite != null)
            {
                sprite.Visible = ReferenceEquals(sprite, chosen);
            }
        }
    }

    // —— 接线 ——

    /// <summary>
    /// 休息角色场景：挂 <c>_EnterTree</c> 而不是 <c>_Ready</c>。
    /// <c>NRestSiteCharacter._Ready</c> 开头就会读 <c>Player.RunState</c>，Player 为 null 时抛 NRE，
    /// 整个 Postfix 都不会执行；<c>_EnterTree</c> 是它自己覆写的方法、没有任何 Prefix 拦截，必定执行。
    /// 此时 <c>Player</c> 已由 <c>NRestSiteCharacter.Create(player, index)</c> 赋值好了。
    /// </summary>
    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._EnterTree))]
    [HarmonyPostfix]
    private static void RestSiteEnterTree(NRestSiteCharacter __instance)
        => AttachRestSite(__instance, __instance.Player?.Creature);

    /// <summary>
    /// 商店角色场景的兜底接线（正常情况下由 <see cref="StaticBodyVisualsPatch"/> 的 Prefix 调
    /// <see cref="AttachMerchant"/>；这里是万一 Prefix 链被别的 mod 改动时的第二条路，Attach 内部会去重）。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
    [HarmonyPostfix]
    private static void MerchantReady(NMerchantCharacter __instance) => AttachMerchant(__instance);
}
