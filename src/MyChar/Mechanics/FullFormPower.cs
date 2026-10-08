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
///   1) 迭代牌完成一次迭代时，给所有迭代牌 +1 计数（每回合一次）；
///   2) 响应牌触发响应时，把本次消耗的一条随机传参返还到池里。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，份数记在 <see cref="PowerModel.Amount"/>，
/// 每次推进的格数 = 份数（每回合仍只触发一次），状态栏数字即份数。
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
        ("description", "迭代牌完成迭代时，所有迭代牌计数+{Advance}（每回合一次）；响应牌响应时，返还一条随机已消耗传参。"),
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

        ParamEntry? entry = player.RunState.Rng.CombatTargets.NextItem(core.LastConsumed);
        if (entry != null)
        {
            await core.AddParamWithHook(choiceContext, entry);
        }
    }
}