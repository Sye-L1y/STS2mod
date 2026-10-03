using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有遗物「共演」：若本回合打出过迭代牌，则回合结束时手里所有迭代牌计数器 +1。
/// 「本回合是否打出过迭代牌」记录在 <see cref="AlphaCorePower"/> 的战斗内数据里，回合开始重置。
/// </summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharRelicEnsemble : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override string IconBaseName => "anchor";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "共演"),
        ("description", "若本回合打出过[gold]迭代[/gold]牌，则回合结束时，手里所有迭代牌计数器+1。"),
        ("flavor", "一台机器动起来，整排机器都跟着共振。"),
    };

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is AlphaCard { IterationEvery: > 0 } && cardPlay.Card.Owner == Owner)
        {
            AlphaCorePower? core = AlphaCorePower.Of(Owner);
            if (core != null)
            {
                core.Core.IterationPlayedThisTurn = true;
            }
        }
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
        {
            AlphaCorePower? core = AlphaCorePower.Of(Owner);
            if (core != null)
            {
                core.Core.IterationPlayedThisTurn = false;
            }
        }
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        AlphaCorePower? core = AlphaCorePower.Of(Owner);
        if (core == null || !core.Core.IterationPlayedThisTurn)
        {
            return Task.CompletedTask;
        }
        if (!participants.Contains(Owner.Creature))
        {
            return Task.CompletedTask;
        }

        bool any = false;
        foreach (CardModel card in CardPile.GetCards(Owner, PileType.Hand))
        {
            if (card is AlphaCard { IterationEvery: > 0 })
            {
                core.AddPlayCount(card, 1);
                any = true;
            }
        }
        if (any)
        {
            Flash();
        }
        return Task.CompletedTask;
    }
}