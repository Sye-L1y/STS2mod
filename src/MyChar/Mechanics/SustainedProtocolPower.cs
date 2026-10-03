using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「持续协议」的载体：己方回合结束时加协议层数。
///
/// 加在回合结束时（<c>AfterSideTurnEnd</c>），层数会一直留到**下个回合开始**被
/// <see cref="ProtocolPower"/> 结算——正好对上「每回合结束攒、下回合兑现」。层数走 Amount，可叠加。
/// </summary>
public sealed class SustainedProtocolPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("sustained_protocol");

    public override string? CustomBigIconPath => MyCharAssets.Power("sustained_protocol");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "持续协议"),
        ("description", "己方回合结束时获得{Amount}层[gold]协议[/gold]，留到下回合结算。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Amount", Amount);

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return Task.CompletedTask;
        }

        if (Owner.Player is { } player)
        {
            ProtocolPower.Of(player)?.AddProtocol(Amount);
        }
        return Task.CompletedTask;
    }
}