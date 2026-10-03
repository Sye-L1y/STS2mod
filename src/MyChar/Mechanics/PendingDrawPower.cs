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
/// 「下回合抽 N 张」的延迟载体（备用协议）。
///
/// 用 <c>AfterPlayerTurnStart</c>：本回合打出时回合开始已经过去，所以会在**下一个**己方回合
/// 开始时抽牌。层数走 Amount，同类效果叠加。
/// </summary>
public sealed class PendingDrawPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("pending_draw");

    public override string? CustomBigIconPath => MyCharAssets.Power("pending_draw");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "待发抽牌"),
        ("description", "下回合开始时，抽{Draw}张牌。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Draw", Amount);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, Amount, player);
        await PowerCmd.Remove(this);
    }
}