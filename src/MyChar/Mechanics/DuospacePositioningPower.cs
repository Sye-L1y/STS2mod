using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「复空间定位」的载体：本回合内，每当持有者受到攻击，就对所有敌人造成 Amount 点伤害。
/// 回合结束移除（与本体 FlameBarrier 同款时序）。
/// </summary>
public sealed class DuospacePositioningPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("duospace_positioning");

    public override string? CustomBigIconPath => MyCharAssets.Power("duospace_positioning");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "复空间定位"),
        ("description", "本回合受到攻击时，对所有敌人造成{Amount}点伤害。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Amount", Amount);

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || !props.IsPoweredAttack())
        {
            return;
        }
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }
        await CreatureCmd.Damage(choiceContext, combatState.HittableEnemies, Amount,
            ValueProp.Unpowered, Owner);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner.Side != side)
        {
            await PowerCmd.Remove(this);
        }
    }
}