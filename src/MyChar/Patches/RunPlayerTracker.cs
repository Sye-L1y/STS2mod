using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;

namespace MyCharMod.Patches;

/// <summary>
/// 记住「本局本地玩家」的持久 <see cref="Creature"/>，供**非战斗场景**（商店 / 休息）读血量。
///
/// 战斗里血量可以从 <c>NCreature.Entity</c> 直接拿到，但商店 / 休息场景里根本没有 Creature 节点；
/// 玩家的血量实际存在 <c>Player.Creature</c>（RunState 里的持久实体，休息回血也是走
/// <c>CreatureCmd.Heal(player.Creature, …)</c>），只是 <c>RunManager.State</c> 是 private。
///
/// ★★ v1.23b 修：只在 RunManager.SetUp* 的 Postfix 里抓一次是**不够**的 ——
///   读档路径（SetUpSavedSingleplayer / SetUpSavedMultiplayer）是 async，Postfix 落在**第一个 await 之前**，
///   那时玩家集合 / <c>LocalContext.NetId</c> 还没就绪，<c>LocalContext.GetMe</c> 会返回 null（甚至在
///   找不到玩家时直接抛 InvalidOperationException），一次没抓到就再也没有第二次机会 ——
///   现象就是「战斗立绘正常、商店 / 休息永远显示完好立绘」。
///
/// 现在三层保险：
///   ① SetUp* 的 Postfix 先抓一次（新局同步路径能立刻命中）；
///   ② <see cref="Creature"/> 每次被读时若手里还没有，就现场反查
///      <c>RunManager.Instance.DebugOnlyGetState()</c>（进商店 / 休息时会读，那时一定已就绪）；
///   ③ 场景侧能直接拿到 Creature 的（休息场景的 <c>NRestSiteCharacter.Player</c>）用 <see cref="Bind"/> 喂进来。
/// </summary>
internal static class RunPlayerTracker
{
    private static Creature? _creature;
    private static bool _resolveWarned;

    /// <summary>玩家血量变化（场景侧订阅它重算档位）。</summary>
    public static event Action? HpChanged;

    /// <summary>本局本地玩家的持久 Creature；手里没有就现场反查一次，仍拿不到返回 null。</summary>
    public static Creature? Creature
    {
        get
        {
            Creature? creature = _creature;
            return creature ?? Resolve();
        }
    }

    /// <summary>当前 HP 比例；没拿到玩家 / MaxHp 异常时返回 <c>-1</c>（调用方视作不切档）。</summary>
    public static float HpRatio
    {
        get
        {
            Creature? creature = Creature;
            if (creature == null || creature.MaxHp <= 0)
            {
                return -1f;
            }

            return (float)creature.CurrentHp / creature.MaxHp;
        }
    }

    /// <summary>从 RunState 里认出本地玩家并开始跟踪（同一 Creature 重复调用会直接返回）。</summary>
    internal static void Capture(RunState state)
    {
        try
        {
            SetCreature(FindLocalPlayer(state)?.Creature);
        }
        catch (Exception e)
        {
            // 这个路径允许失败：② 的懒解析会在真正要用血量时补上。
            Log.Warn($"[MyChar] 抓取本局玩家失败：{e.GetType().Name} {e.Message}");
        }
    }

    /// <summary>场景侧能直接拿到 Creature 时（休息场景的 NRestSiteCharacter.Player）显式绑定。</summary>
    internal static void Bind(Creature? creature)
    {
        if (creature != null)
        {
            SetCreature(creature);
        }
    }

    /// <summary>
    /// 反查当前局玩家。★ 必须走这条兜底：SetUp* 的 Postfix 有时跑在「玩家集合 / NetId 还没就绪」的时机，
    /// 一次拿不到就再也没机会了（见类型注释）。
    /// </summary>
    private static Creature? Resolve()
    {
        try
        {
            RunState? state = RunManager.Instance.DebugOnlyGetState();
            if (state == null)
            {
                return null;
            }

            Creature? creature = FindLocalPlayer(state)?.Creature;
            if (creature != null)
            {
                SetCreature(creature);
                return creature;
            }

            if (!_resolveWarned)
            {
                _resolveWarned = true;
                Log.Info("[MyChar] 反查本局玩家失败：非战斗场景暂时退回完好立绘。");
            }
        }
        catch (Exception e)
        {
            if (!_resolveWarned)
            {
                _resolveWarned = true;
                Log.Warn($"[MyChar] 反查本局玩家出错：{e.GetType().Name} {e.Message}");
            }
        }

        return null;
    }

    /// <summary>
    /// 本地玩家优先按 NetId 认（<c>LocalContext.IsMe</c> 是纯比较、不会抛异常 ——
    /// <c>LocalContext.GetMe</c> 在找不到玩家时会抛 InvalidOperationException，这里刻意不用它）；
    /// NetId 还没设置的窗口期退化成「第一个玩家」——那时基本只有单人局。
    /// </summary>
    private static Player? FindLocalPlayer(RunState state)
    {
        IReadOnlyList<Player> players = state.Players;
        if (players.Count == 0)
        {
            return null;
        }

        foreach (Player player in players)
        {
            if (LocalContext.IsMe(player))
            {
                return player;
            }
        }

        return players[0];
    }

    private static void SetCreature(Creature? creature)
    {
        if (ReferenceEquals(creature, _creature))
        {
            return;
        }

        if (_creature != null)
        {
            _creature.CurrentHpChanged -= OnHp;
        }

        _creature = creature;

        if (_creature != null)
        {
            _creature.CurrentHpChanged += OnHp;
            _resolveWarned = false;
            Log.Info($"[MyChar] 本局玩家已跟踪：{_creature.CurrentHp}/{_creature.MaxHp}");
        }
        else
        {
            Log.Info("[MyChar] 本局玩家引用已清空（新局装配中 / 暂时拿不到）。");
        }

        // 场景可能已经挂好了（先建场景后装配的路径），通知一次让它们立刻按新血量重算。
        HpChanged?.Invoke();
    }

    private static void OnHp(int oldHp, int newHp) => HpChanged?.Invoke();
}

/// <summary>
/// <see cref="RunPlayerTracker.Capture"/> 的接线：RunManager 的每个装配入口都以 <see cref="RunState"/>
/// 作第一个参数（新局 / 读档 / 多人 / 回放），所以用位置注入 <c>__0</c> 而不是参数名，避免名字对不上时炸补丁。
/// 注意这只是「第一层保险」—— async 路径的 Postfix 可能跑在状态就绪之前，真正的兜底在
/// <see cref="RunPlayerTracker"/> 的懒解析里。
/// </summary>
[HarmonyPatch]
internal static class RunPlayerCapturePatch
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewSingleplayer))]
    [HarmonyPostfix]
    private static void AfterSetUpNewSingleplayer(RunState __0) => RunPlayerTracker.Capture(__0);

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedSingleplayer))]
    [HarmonyPostfix]
    private static void AfterSetUpSavedSingleplayer(RunState __0) => RunPlayerTracker.Capture(__0);

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiplayer))]
    [HarmonyPostfix]
    private static void AfterSetUpNewMultiplayer(RunState __0) => RunPlayerTracker.Capture(__0);

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiplayer))]
    [HarmonyPostfix]
    private static void AfterSetUpSavedMultiplayer(RunState __0) => RunPlayerTracker.Capture(__0);

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpReplay))]
    [HarmonyPostfix]
    private static void AfterSetUpReplay(RunState __0) => RunPlayerTracker.Capture(__0);
}
