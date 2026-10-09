using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「完全形态」的载体：
///   1) 迭代牌完成一次迭代时，给所有迭代牌 +N 计数（每回合一次，N = 份数）；
///   2) 响应牌触发响应时，按份数返还本次消耗的传参 —— **每次独立随机取一条，可以重复**
///      （不去重、也不要求互不重复：同一份消耗可以被返还多次）。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，份数记在 <see cref="PowerModel.Amount"/>，
/// 推进格数与返还条数都 = 份数（推进每回合仍只触发一次），状态栏数字即份数。
/// </summary>
public sealed class FullFormPower : CustomPowerModel, IAlphaIterationListener, IAlphaResponseListener,
    IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("full_form");

    public override string? CustomBigIconPath => MyCharAssets.Power("full_form");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "完全形态"),
        ("description", "迭代牌完成迭代时，所有迭代牌计数+{Advance}（每回合一次）；响应牌响应时，按本牌层数随机返还本次消耗的[gold]传参[/gold]（每次独立随机，可重复）。"),
    };

    /// <summary>本牌重复打出的份数（Counter 叠层层数）：先打出的那张为 1。</summary>
    private int Copies => System.Math.Max(1, Amount);

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Advance", Copies);

    private sealed class Data
    {
        public bool UsedThisTurn;
    }

    protected override object? InitInternalData() => new Data();

    private Data Core => GetInternalData<Data>();

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            Core.UsedThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public async Task OnAlphaIteration(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (Core.UsedThisTurn)
        {
            return;
        }

        Player? player = Owner.Player;
        AlphaCorePower? core = player == null ? null : AlphaCorePower.Of(player);
        if (player == null || core == null)
        {
            return;
        }

        // 先置位再推进：AdvanceIteration 会广播迭代完成 → 又回到本监听者，
        // 不先置位会无限递归。
        Core.UsedThisTurn = true;

        // 先把要推进的牌收集成快照，再逐张推进：推进过程中某张牌的迭代效果可能移动牌
        // （抽牌 / 生成 / 回手），惰性遍历牌堆会在牌堆变动时漏项甚至中断，表现为「只更新了一半的堆」。
        List<AlphaCard> targets = new();
        foreach (PileType pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            targets.AddRange(CardPile.GetCards(player, pile)
                .OfType<AlphaCard>()
                .Where(candidate => candidate.IterationEvery > 0));
        }

        foreach (AlphaCard target in targets)
        {
            await core.AdvanceIteration(choiceContext, target, Copies);
        }
    }

    public async Task OnAlphaResponse(PlayerChoiceContext choiceContext, CardModel card)
    {
        Player? player = Owner.Player;
        AlphaCorePower? core = player == null ? null : AlphaCorePower.Of(player);
        if (player == null || core == null || core.LastConsumed.Count == 0)
        {
            return;
        }

        // 快照候选：AddParamWithHook 会往池里写并可能改动 LastConsumed，
        // 直接遍历会读到自己刚写进去的内容。
        List<ParamEntry> consumed = core.LastConsumed.ToList();

        // 每次独立随机取一条，允许重复 —— 同一条消耗可能被返还多次。
        core.BeginParamBatch();
        for (int i = 0; i < Copies; i++)
        {
            ParamEntry? entry = player.RunState.Rng.CombatTargets.NextItem(consumed);
            if (entry == null)
            {
                break;
            }
            await core.AddParamWithHook(choiceContext, entry);
        }
    }
}
