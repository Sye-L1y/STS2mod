using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「紧急护壁」的载体（E14）：本回合不能打出攻击牌。
/// 走 <c>ShouldPlay</c> 钩子（与本体 RingingPower 同款）——返回 false 即禁止打出，回合结束自毁。
/// </summary>
public sealed class NoAttackPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("no_attack");

    public override string? CustomBigIconPath => MyCharAssets.Power("no_attack");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "封锁"),
        ("description", "本回合不能打出攻击牌。"),
    };

    public override bool ShouldPlay(CardModel card, AutoPlayType _)
    {
        if (card.Owner.Creature != Owner)
        {
            return true;
        }
        return card.Type != CardType.Attack;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}