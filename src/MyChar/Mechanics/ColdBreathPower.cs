using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「冷息」的载体：己方回合结束时，手牌中的[gold]迭代[/gold]牌获得[gold]保留[/gold]（不进弃牌堆）。
///
/// 用 <c>BeforeSideTurnEnd</c>：它在引擎「清手牌」之前触发（见 <c>CombatManager.EndPlayerTurnPhaseOne</c>
/// → PhaseTwo 的 FlushPlayerHand），所以这里打的**单回合保留**标记（<c>CardModel.GiveSingleTurnRetain</c>）
/// 能赶在弃手牌之前生效；标记本身由引擎在每个回合结束时自动清除，不会留到下下个回合。
/// </summary>
public sealed class ColdBreathPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>开关型光环：有它就行，多份不叠。</summary>
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("cold_breath");

    public override string? CustomBigIconPath => MyCharAssets.Power("cold_breath");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "冷息"),
        ("description", "每回合结束时，手牌中的[gold]迭代[/gold]牌获得[gold]保留[/gold]。"),
    };

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player is not { } player)
        {
            return Task.CompletedTask;
        }

        foreach (CardModel card in PileType.Hand.GetPile(player).Cards)
        {
            if (card is AlphaCard { IterationEvery: > 0 })
            {
                card.GiveSingleTurnRetain();
            }
        }
        return Task.CompletedTask;
    }
}
