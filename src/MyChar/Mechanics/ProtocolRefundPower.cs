using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「备用协议」的延迟返还载体：下回合协议结算之后，返还**该回合实际结算消耗总量的一半**（向上取整）。
///
/// 时机同 <see cref="PendingProtocolPower"/>：走 <c>AfterPlayerTurnStartLate</c>，
/// 确保在协议被 <see cref="ProtocolPower"/> 消费并生成协议牌之后再入账，撑过整个下一回合。
/// 消耗量从 <see cref="ProtocolPower.LastConsumed"/> 读取（每次结算时记录）。
///
/// 层数（Amount）= 待返还份数：每张备用协议各返还一份「消耗量的一半」。
/// </summary>
public sealed class ProtocolRefundPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("protocol");

    public override string? CustomBigIconPath => MyCharAssets.Power("protocol");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "待发协议·返还"),
        ("description", "下回合[gold]协议[/gold]结算后，返还该回合结算消耗量一半的[gold]协议[/gold]（每张备用协议各一份）。"),
    };

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        ProtocolPower? protocol = ProtocolPower.Of(player);
        int consumed = protocol?.LastConsumed ?? 0;
        int refund = (consumed + 1) / 2 * Amount;
        if (refund > 0)
        {
            protocol?.AddProtocol(refund);
        }
        await PowerCmd.Remove(this);
    }
}