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
/// 「下回合获得协议 N」的延迟载体（协议凝聚 / 全量扫描 / 备用协议）。
///
/// 时机很关键：协议层数在**回合开始**被 <see cref="ProtocolPower"/> 消费掉。
/// 如果在这里也用 <c>AfterPlayerTurnStart</c>，加进去的层数会被同一回合立刻消费，
/// 「下回合获得 N 层」就退化成「下回合白送一张 α协议」。
///
/// 所以走 <c>AfterPlayerTurnStartLate</c>——它排在所有 <c>AfterPlayerTurnStart</c> 之后，
/// 层数在协议结算**之后**入账，于是能撑过整个下一回合，供玩家继续叠加。
/// </summary>
public sealed class PendingProtocolPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("protocol");

    public override string? CustomBigIconPath => MyCharAssets.Power("protocol");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "待发协议"),
        ("description", "下回合开始时，在协议结算之后获得{Layers}层[gold]协议[/gold]。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Layers", Amount);

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        ProtocolPower.Of(player)?.AddProtocol(Amount);
        await PowerCmd.Remove(this);
    }
}