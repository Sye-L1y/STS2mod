using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「渗透」的载体：每当有响应牌真的触发一次响应，就对所有敌人造成 Amount 点伤害。
/// 走 <see cref="IAlphaResponseListener"/> 监听，不自己产出响应。
/// </summary>
public sealed class InfiltrationPower : CustomPowerModel, IAlphaResponseListener, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("infiltration");

    public override string? CustomBigIconPath => MyCharAssets.Power("infiltration");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "渗透"),
        ("description", "触发[gold]响应[/gold]后，对所有敌人造成{Amount}点伤害。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Amount", Amount);

    public async Task OnAlphaResponse(PlayerChoiceContext choiceContext, CardModel card)
    {
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }
        await CreatureCmd.Damage(choiceContext, combatState.HittableEnemies, Amount,
            ValueProp.Unpowered, Owner);
    }
}