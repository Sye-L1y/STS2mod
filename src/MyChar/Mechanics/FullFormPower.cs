using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「完全形态」的载体：
///   1) 迭代牌完成一次迭代时，给所有迭代牌 +1 计数（每回合一次）；
///   2) 响应牌触发响应时，把本次消耗的一条随机传参返还到池里。
/// </summary>
public sealed class FullFormPower : CustomPowerModel, IAlphaIterationListener, IAlphaResponseListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("full_form");

    public override string? CustomBigIconPath => MyCharAssets.Power("full_form");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "完全形态"),
        ("description", "迭代牌完成迭代时，所有迭代牌计数+1（每回合一次）；响应牌响应时，返还一条随机已消耗传参。"),
    };

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

    public Task OnAlphaIteration(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (Core.UsedThisTurn)
        {
            return Task.CompletedTask;
        }

        Player? player = Owner.Player;
        AlphaCorePower? core = player == null ? null : AlphaCorePower.Of(player);
        if (player == null || core == null)
        {
            return Task.CompletedTask;
        }

        bool any = false;
        foreach (PileType pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            foreach (CardModel candidate in CardPile.GetCards(player, pile))
            {
                if (candidate is AlphaCard { IterationEvery: > 0 })
                {
                    core.AddPlayCount(candidate, 1);
                    any = true;
                }
            }
        }
        if (any)
        {
            Core.UsedThisTurn = true;
        }
        return Task.CompletedTask;
    }

    public Task OnAlphaResponse(PlayerChoiceContext choiceContext, CardModel card)
    {
        Player? player = Owner.Player;
        AlphaCorePower? core = player == null ? null : AlphaCorePower.Of(player);
        if (player == null || core == null || core.LastConsumed.Count == 0)
        {
            return Task.CompletedTask;
        }

        ParamEntry? entry = player.RunState.Rng.CombatTargets.NextItem(core.LastConsumed);
        if (entry != null)
        {
            core.AddParam(entry);
        }
        return Task.CompletedTask;
    }
}